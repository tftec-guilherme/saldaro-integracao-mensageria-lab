using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Azure.Core;
using Azure.Messaging.ServiceBus;

namespace Saldaro.Ferramentas;

internal sealed record OpcoesDaRajada(string Namespace, string Fila, int Remetentes, int MensagensPorRemetente)
{
    /// <summary>Filas do hub: a rajada nunca escreve nelas (os workers consumiriam lixo).</summary>
    public static IReadOnlySet<string> FilasDoHub { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "sbq-faturas-recebidas",
        "sbq-confirmacoes-banco",
        "sbq-notificacoes-parceiro",
    };

    public int Total => Remetentes * MensagensPorRemetente;

    public static OpcoesDaRajada De(ArgumentosAnalisados argumentos)
    {
        var fila = argumentos.Texto("--fila");
        if (FilasDoHub.Contains(fila))
        {
            throw new ArgumentException(
                $"a rajada usa uma fila descartável (ex.: sbq-rajada), nunca {fila}: os workers do hub consumiriam as mensagens de teste.");
        }

        return new OpcoesDaRajada(
            NomeDoNamespace.Normalizar(argumentos.Texto("--namespace")),
            fila,
            argumentos.Inteiro("--remetentes"),
            argumentos.Inteiro("--mensagens"));
    }
}

/// <summary>Subcomando <c>rajada</c>: N remetentes concorrentes, cada um enviando M mensagens.</summary>
internal static class ComandoRajada
{
    private static readonly TimeSpan IntervaloDoProgresso = TimeSpan.FromSeconds(2);

    public static async Task<int> ExecutarAsync(
        OpcoesDaRajada opcoes, TokenCredential credencial, TextWriter saida, CancellationToken ct)
    {
        saida.WriteLine($"Rajada em {opcoes.Namespace} / {opcoes.Fila}");
        saida.WriteLine($"  {opcoes.Remetentes} remetentes x {opcoes.MensagensPorRemetente} mensagens = {Formatos.Inteiro(opcoes.Total)} mensagens");

        // CONCEITO: no Standard, cada mensagem enviada gasta 1 crédito de um orçamento de 1.000 por
        // segundo por namespace. Passou disso, o Service Bus devolve ServiceBusy (erro 50009) e o SDK
        // espera e tenta de novo. A política abaixo conta essas retentativas para você ver o efeito.
        var politica = new PoliticaDeRetryQueConta();
        await using var cliente = new ServiceBusClient(opcoes.Namespace, credencial, new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions { CustomRetryPolicy = politica },
        });

        var execucao = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        // Aquecimento: abre a conexão AMQP e confirma que a fila existe antes de medir.
        await using (var aquecimento = cliente.CreateSender(opcoes.Fila))
        {
            await aquecimento.SendMessageAsync(CriarMensagem(execucao, 0, 0), ct);
        }

        saida.WriteLine("  Aquecimento ok (1 mensagem extra). Disparando...");

        var contadores = new Contadores();
        var falhas = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
        var relogio = Stopwatch.StartNew();
        var remetentes = Enumerable.Range(1, opcoes.Remetentes)
            .Select(r => EnviarAsync(cliente, opcoes, execucao, r, contadores, falhas, ct))
            .ToArray();
        var todos = Task.WhenAll(remetentes);
        while (await Task.WhenAny(todos, Task.Delay(IntervaloDoProgresso, CancellationToken.None)) != todos
            && !ct.IsCancellationRequested)
        {
            var enviadas = Volatile.Read(ref contadores.Enviadas);
            saida.WriteLine($"  {Formatos.Inteiro(enviadas)}/{Formatos.Inteiro(opcoes.Total)} enviadas | {Formatos.Inteiro((long)(enviadas / relogio.Elapsed.TotalSeconds))} msg/s");
        }

        var latencias = (await todos).SelectMany(l => l).Order().ToArray();
        relogio.Stop();

        var segundos = Math.Max(relogio.Elapsed.TotalSeconds, 0.001);
        var totalDeFalhas = falhas.Values.Sum();
        saida.WriteLine();
        saida.WriteLine($"Rajada concluída em {Formatos.UmaCasa(segundos)} s");
        saida.WriteLine($"  Mensagens enviadas:   {Formatos.Inteiro(contadores.Enviadas)} de {Formatos.Inteiro(opcoes.Total)}");
        saida.WriteLine($"  Falhas definitivas:   {Formatos.Inteiro(totalDeFalhas)}{Detalhar(falhas)}");
        saida.WriteLine($"  Vazão média:          {Formatos.Inteiro((long)(contadores.Enviadas / segundos))} msg/s");
        if (latencias.Length > 0)
        {
            saida.WriteLine(
                $"  Latência do envio:    p50 {Ms(Estatistica.Percentil(latencias, 50))} | p95 {Ms(Estatistica.Percentil(latencias, 95))} | máx. {Ms(latencias[^1])}");
        }

        var retentativas = politica.Retentativas;
        var throttling = retentativas.GetValueOrDefault(nameof(ServiceBusFailureReason.ServiceBusy));
        var outras = retentativas.Where(r => r.Key != nameof(ServiceBusFailureReason.ServiceBusy)).Sum(r => r.Value);
        saida.WriteLine($"  Retentativas do SDK:  ServiceBusy (throttling, erro 50009): {Formatos.Inteiro(throttling)} | outras: {Formatos.Inteiro(outras)}{Detalhar(retentativas.Where(r => r.Key != nameof(ServiceBusFailureReason.ServiceBusy)))}");
        saida.WriteLine();
        saida.WriteLine(throttling > 0
            ? "  Houve throttling: o namespace recusou parte dos envios por falta de créditos e o SDK repetiu."
            : "  Sem throttling desta máquina: uma máquina só raramente passa de 1.000 créditos/s.");
        saida.WriteLine("  No Portal: namespace > Metrics > Throttled Requests e Incoming Messages (últimos 30 min).");
        saida.WriteLine($"  Ao terminar, apague a fila {opcoes.Fila}.");
        return totalDeFalhas > 0 ? CodigosDeSaida.Falha : CodigosDeSaida.Sucesso;
    }

    private static async Task<List<double>> EnviarAsync(
        ServiceBusClient cliente,
        OpcoesDaRajada opcoes,
        string execucao,
        int remetente,
        Contadores contadores,
        ConcurrentDictionary<string, int> falhas,
        CancellationToken ct)
    {
        var latencias = new List<double>(opcoes.MensagensPorRemetente);
        await using var sender = cliente.CreateSender(opcoes.Fila);
        for (var i = 1; i <= opcoes.MensagensPorRemetente; i++)
        {
            var inicio = Stopwatch.GetTimestamp();
            try
            {
                await sender.SendMessageAsync(CriarMensagem(execucao, remetente, i), ct);
                latencias.Add(Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
                Interlocked.Increment(ref contadores.Enviadas);
            }
            catch (ServiceBusException ex)
            {
                // Falha depois das retentativas do SDK: conta e segue com a próxima mensagem.
                falhas.AddOrUpdate(PoliticaDeRetryQueConta.Motivo(ex), 1, (_, n) => n + 1);
            }
        }

        return latencias;
    }

    internal static ServiceBusMessage CriarMensagem(string execucao, int remetente, int sequencia)
    {
        var corpo = new
        {
            execucao,
            remetente,
            sequencia,
            enviadaEm = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
        };
        return new ServiceBusMessage(BinaryData.FromObjectAsJson(corpo))
        {
            MessageId = $"rajada-{execucao}-{remetente}-{sequencia}",
            // SessionId por remetente: funciona também se a fila descartável tiver sessions.
            SessionId = $"remetente-{remetente}",
            ContentType = "application/json",
        };
    }

    private static string Ms(double milissegundos) => $"{Formatos.Inteiro((long)Math.Round(milissegundos))} ms";

    private static string Detalhar(IEnumerable<KeyValuePair<string, int>> contagens)
    {
        var partes = contagens.Where(c => c.Value > 0).OrderByDescending(c => c.Value).Select(c => $"{c.Key}: {c.Value}").ToList();
        return partes.Count == 0 ? string.Empty : $" ({string.Join(", ", partes)})";
    }

    private sealed class Contadores
    {
        public int Enviadas;
    }
}
