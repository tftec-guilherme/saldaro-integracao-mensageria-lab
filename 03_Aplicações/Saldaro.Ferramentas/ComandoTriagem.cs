using System.Diagnostics;
using System.Globalization;
using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Saldaro.Ferramentas;

internal sealed record OpcoesDaTriagem(string Namespace, string Fila, int Max, bool Reprocessar, string? DescartarMotivo)
{
    public bool SomenteLeitura => !Reprocessar && DescartarMotivo is null;

    public static OpcoesDaTriagem De(ArgumentosAnalisados argumentos) => new(
        NomeDoNamespace.Normalizar(argumentos.Texto("--namespace")),
        argumentos.Texto("--fila"),
        argumentos.Inteiro("--max"),
        argumentos.Ligada("--reprocessar"),
        argumentos.TextoOpcional("--descartar-motivo"));
}

/// <summary>Subcomando <c>triagem</c>: lê a DLQ e, só se pedido, reprocessa ou descarta.</summary>
internal static class ComandoTriagem
{
    private const int ExemplosPorMotivo = 3;
    private const int TamanhoDoLote = 100;
    private const int TamanhoMaximoDaDescricao = 160;
    private static readonly TimeSpan EsperaPorLote = TimeSpan.FromSeconds(5);

    public static async Task<int> ExecutarAsync(
        OpcoesDaTriagem opcoes, TokenCredential credencial, TextWriter saida, CancellationToken ct)
    {
        saida.WriteLine($"Triagem da DLQ de {opcoes.Fila} em {opcoes.Namespace}");

        await using var cliente = new ServiceBusClient(opcoes.Namespace, credencial);

        // CONCEITO: a DLQ é uma subfila ($DeadLetterQueue) da própria fila. Lê-se com um receptor
        // comum apontado para SubQueue.DeadLetter, mesmo quando a fila principal exige sessions.
        await using var receptor = cliente.CreateReceiver(opcoes.Fila, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter,
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            PrefetchCount = 0,
        });

        return opcoes.SomenteLeitura
            ? await ListarAsync(opcoes, credencial, receptor, saida, ct)
            : await AgirAsync(opcoes, cliente, receptor, saida, ct);
    }

    private static async Task<int> ListarAsync(
        OpcoesDaTriagem opcoes, TokenCredential credencial, ServiceBusReceiver receptor, TextWriter saida, CancellationToken ct)
    {
        saida.WriteLine("Modo somente leitura (peek): nada é alterado. Para agir, use --reprocessar e/ou --descartar-motivo.");
        saida.WriteLine();

        var lidas = new List<MensagemDaDlq>();
        while (lidas.Count < opcoes.Max)
        {
            // CONCEITO: peek lê sem travar e sem remover; cada chamada continua de onde a anterior parou.
            var lote = await receptor.PeekMessagesAsync(Math.Min(TamanhoDoLote, opcoes.Max - lidas.Count), cancellationToken: ct);
            if (lote.Count == 0)
            {
                break;
            }

            lidas.AddRange(lote.Select(MensagemDaDlq.De));
        }

        var totalNoNamespace = await ContarDlqAsync(opcoes, credencial, ct);
        saida.WriteLine(totalNoNamespace is long total
            ? $"Mensagens lidas da DLQ: {Formatos.Inteiro(lidas.Count)} (o namespace informa {Formatos.Inteiro(total)} na DLQ)."
            : $"Mensagens lidas da DLQ: {Formatos.Inteiro(lidas.Count)}.");
        if (lidas.Count >= opcoes.Max)
        {
            saida.WriteLine($"Atenção: a leitura parou no limite de --max {opcoes.Max}; pode haver mais mensagens.");
        }

        if (lidas.Count == 0)
        {
            saida.WriteLine("A DLQ está vazia.");
            return CodigosDeSaida.Sucesso;
        }

        var grupos = ResumoDaDlq.Agrupar(lidas, ExemplosPorMotivo);
        saida.WriteLine();
        saida.WriteLine(Tabela.Montar(
            ["Motivo (DeadLetterReason)", "Qtde", "Corrigíveis", "Sem correção", "Não corrigíveis", "Mais antiga", "Mais recente"],
            grupos.Select(g => new[]
            {
                g.Motivo,
                Formatos.Inteiro(g.Quantidade),
                Formatos.Inteiro(g.Corrigiveis),
                Formatos.Inteiro(g.SemCorrecao),
                Formatos.Inteiro(g.NaoCorrigiveis),
                Hora(g.MaisAntiga),
                Hora(g.MaisRecente),
            }),
            [false, true, true, true, true, false, false]));
        saida.WriteLine();
        saida.WriteLine("  Corrigíveis: valor em texto pt-BR (\"1.250,00\"); o --reprocessar converte para número.");
        saida.WriteLine("  Sem correção: valor já numérico ou ausente; o --reprocessar reprocessa como está.");
        saida.WriteLine("  Não corrigíveis: o --reprocessar deixa na DLQ para análise.");

        saida.WriteLine();
        saida.WriteLine($"Exemplos (até {ExemplosPorMotivo} por motivo):");
        foreach (var grupo in grupos)
        {
            saida.WriteLine($"  {grupo.Motivo}");
            foreach (var exemplo in grupo.Exemplos)
            {
                var correcao = CorrecaoDeValor.Corrigir(exemplo.Corpo);
                var tipo = exemplo.Propriedade("tipo") ?? "-";
                saida.WriteLine(
                    $"    - {exemplo.MessageId} | sessão {exemplo.SessionId ?? "-"} | tipo {tipo} | valor {correcao.ValorParaExibir} | entregas {exemplo.Entregas}");
                if (!string.IsNullOrWhiteSpace(exemplo.Descricao))
                {
                    saida.WriteLine($"      {Encurtar(exemplo.Descricao)}");
                }
            }
        }

        saida.WriteLine();
        saida.WriteLine("Próximos passos:");
        saida.WriteLine("  --reprocessar                corrige e reprocessa as corrigíveis e as sem correção (MessageId {original}-r{n})");
        saida.WriteLine("  --descartar-motivo <motivo>  descarta as do motivo informado; combinado com --reprocessar, reprocessa as demais");
        return CodigosDeSaida.Sucesso;
    }

    private static async Task<int> AgirAsync(
        OpcoesDaTriagem opcoes, ServiceBusClient cliente, ServiceBusReceiver receptor, TextWriter saida, CancellationToken ct)
    {
        var acoes = new List<string>();
        if (opcoes.Reprocessar)
        {
            acoes.Add("reprocessar (corrige o valor e envia a cópia para a fila principal)");
        }

        if (opcoes.DescartarMotivo is not null)
        {
            acoes.Add($"descartar as do motivo {opcoes.DescartarMotivo}");
        }

        saida.WriteLine($"Modo de ação: {string.Join(" e ", acoes)}.");
        saida.WriteLine();

        await using var remetente = cliente.CreateSender(opcoes.Fila);
        var rodada = new RodadaDeAcao(receptor, remetente, saida);
        var vistas = new HashSet<long>();

        while (rodada.Placar.Total < opcoes.Max)
        {
            var lote = await receptor.ReceiveMessagesAsync(Math.Min(TamanhoDoLote, opcoes.Max - rodada.Placar.Total), EsperaPorLote, ct);
            if (lote.Count == 0)
            {
                break;
            }

            var novas = 0;
            foreach (var recebida in lote)
            {
                if (!vistas.Add(recebida.SequenceNumber))
                {
                    // Voltou porque o lock de uma mensagem mantida expirou: já foi decidida.
                    await DevolverAsync(receptor, recebida);
                    continue;
                }

                novas++;
                var mensagem = MensagemDaDlq.De(recebida);
                var acao = DecisaoDeTriagem.Decidir(mensagem, opcoes.Reprocessar, opcoes.DescartarMotivo);
                switch (acao)
                {
                    case AcaoDeTriagem.Descartar descartar:
                        await rodada.DescartarAsync(recebida, mensagem, descartar, ct);
                        break;

                    case AcaoDeTriagem.Manter manter:
                        rodada.Manter(recebida, mensagem, manter);
                        break;

                    case AcaoDeTriagem.Reprocessar reprocessar:
                        await rodada.ReprocessarAsync(recebida, mensagem, reprocessar, ct);
                        break;

                    default:
                        throw new UnreachableException($"Ação não tratada: {acao.GetType().Name}");
                }
            }

            if (novas == 0)
            {
                break;
            }
        }

        await rodada.DevolverMantidasAsync();

        var placar = rodada.Placar;
        saida.WriteLine();
        saida.WriteLine("Resultado:");
        saida.WriteLine($"  Reprocessadas:    {Formatos.Inteiro(placar.Reprocessadas)} ({Formatos.Inteiro(placar.Corrigidas)} com valor corrigido, {Formatos.Inteiro(placar.SemCorrecao)} sem correção)");
        saida.WriteLine($"  Descartadas:      {Formatos.Inteiro(placar.Descartadas)}");
        saida.WriteLine($"  Mantidas na DLQ:  {Formatos.Inteiro(placar.Mantidas)}");
        saida.WriteLine($"  Falhas de envio:  {Formatos.Inteiro(placar.FalhasDeEnvio)}");
        if (placar.Total == 0)
        {
            saida.WriteLine("A DLQ estava vazia.");
        }

        if (placar.Total >= opcoes.Max)
        {
            saida.WriteLine($"Atenção: parou no limite de --max {opcoes.Max}; rode de novo para continuar.");
        }

        saida.WriteLine("Confira rodando a triagem sem opções (somente leitura) ou no Service Bus Explorer do Portal.");
        return placar.FalhasDeEnvio > 0 ? CodigosDeSaida.Falha : CodigosDeSaida.Sucesso;
    }

    private static async Task DevolverAsync(ServiceBusReceiver receptor, ServiceBusReceivedMessage recebida)
    {
        try
        {
            await receptor.AbandonMessageAsync(recebida);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageLockLost)
        {
            // O lock já expirou: a mensagem já voltou sozinha para a DLQ.
        }
    }

    private static async Task<long?> ContarDlqAsync(OpcoesDaTriagem opcoes, TokenCredential credencial, CancellationToken ct)
    {
        try
        {
            var administracao = new ServiceBusAdministrationClient(opcoes.Namespace, credencial);
            var propriedades = await administracao.GetQueueRuntimePropertiesAsync(opcoes.Fila, ct);
            return propriedades.Value.DeadLetterMessageCount;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or ServiceBusException or RequestFailedException)
        {
            // Sem a permissão Manage, segue só com o que o peek leu.
            return null;
        }
    }

    private static string Hora(DateTimeOffset instante) =>
        instante.ToLocalTime().ToString("dd/MM HH:mm:ss", CultureInfo.InvariantCulture);

    private static string Encurtar(string texto)
    {
        var umaLinha = texto.ReplaceLineEndings(" ").Trim();
        return umaLinha.Length <= TamanhoMaximoDaDescricao ? umaLinha : umaLinha[..(TamanhoMaximoDaDescricao - 3)] + "...";
    }

    /// <summary>
    /// Uma rodada do modo de ação: o que passa de mensagem em mensagem (o receptor da DLQ, o remetente para a fila
    /// principal, a saída, o placar e as mensagens mantidas) e o que fazer com cada mensagem, conforme a ação decidida.
    /// </summary>
    private sealed class RodadaDeAcao(ServiceBusReceiver receptor, ServiceBusSender remetente, TextWriter saida)
    {
        private readonly List<ServiceBusReceivedMessage> _mantidas = [];

        public PlacarDaTriagem Placar { get; } = new();

        public async Task DescartarAsync(
            ServiceBusReceivedMessage recebida, MensagemDaDlq mensagem, AcaoDeTriagem.Descartar descartar, CancellationToken ct)
        {
            await receptor.CompleteMessageAsync(recebida, ct);
            Placar.Descartadas++;
            saida.WriteLine($"  [descartada]   {mensagem.MessageId}  {descartar.Explicacao}");
        }

        public void Manter(ServiceBusReceivedMessage recebida, MensagemDaDlq mensagem, AcaoDeTriagem.Manter manter)
        {
            // O lock fica conosco até o fim, para a mesma mensagem não voltar no próximo lote.
            _mantidas.Add(recebida);
            Placar.Mantidas++;
            saida.WriteLine($"  [mantida]      {mensagem.MessageId}  {manter.Explicacao}");
        }

        public async Task ReprocessarAsync(
            ServiceBusReceivedMessage recebida, MensagemDaDlq mensagem, AcaoDeTriagem.Reprocessar reprocessar, CancellationToken ct)
        {
            var nova = reprocessar.NovaMensagem;
            try
            {
                await remetente.SendMessageAsync(nova.ParaServiceBus(), ct);
            }
            catch (ServiceBusException ex)
            {
                _mantidas.Add(recebida);
                Placar.FalhasDeEnvio++;
                saida.WriteLine($"  [falha]        {mensagem.MessageId}  não foi reprocessada ({ex.Reason}): {Encurtar(ex.Message)}");
                return;
            }

            // CONCEITO: só tira da DLQ depois que a fila principal aceitou a cópia. Se cair no meio, o
            // pior caso é a mensagem continuar na DLQ, nunca sumir.
            try
            {
                await receptor.CompleteMessageAsync(recebida, ct);
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageLockLost)
            {
                saida.WriteLine($"  [aviso]        {mensagem.MessageId}  a cópia foi enviada, mas o lock expirou e a original continua na DLQ.");
                saida.WriteLine($"                 Um novo --reprocessar reprocessa a original com o mesmo {nova.MessageId}; o duplicate detection descarta a repetição.");
            }

            if (nova.ValorCorrigido)
            {
                Placar.Corrigidas++;
            }
            else
            {
                Placar.SemCorrecao++;
            }

            saida.WriteLine($"  [reprocessada] {mensagem.MessageId} -> {nova.MessageId}  sessão {nova.SessionId ?? "-"}  {reprocessar.Explicacao}");
        }

        /// <summary>Fim da rodada: devolve à DLQ as mensagens mantidas e as que falharam no envio.</summary>
        public async Task DevolverMantidasAsync()
        {
            foreach (var recebida in _mantidas)
            {
                await DevolverAsync(receptor, recebida);
            }
        }
    }

    private sealed class PlacarDaTriagem
    {
        public int Corrigidas { get; set; }

        public int SemCorrecao { get; set; }

        public int Descartadas { get; set; }

        public int Mantidas { get; set; }

        public int FalhasDeEnvio { get; set; }

        public int Reprocessadas => Corrigidas + SemCorrecao;

        public int Total => Reprocessadas + Descartadas + Mantidas + FalhasDeEnvio;
    }
}
