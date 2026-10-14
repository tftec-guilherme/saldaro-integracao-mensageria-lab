using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Tests;

// Dublês de teste das portas do hub, em memória: os testes rodam sem Azure, sem rede e sem Azurite.

/// <summary>Relógio controlado pelo teste.</summary>
public sealed class RelogioControlado(DateTimeOffset agora) : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = agora;

    public override DateTimeOffset GetUtcNow() => Agora;
}

/// <summary>Tabela <c>faturas</c> em memória, com ETag simulado por um contador de versão.</summary>
public sealed class RepositorioFaturasEmMemoria : IRepositorioFaturas
{
    private readonly Dictionary<string, (Fatura Fatura, int Versao)> _linhas = [];
    private readonly Lock _trava = new();

    /// <summary>Executado uma vez, logo antes da próxima gravação: simula outra execução gravando no meio.</summary>
    public Func<Task>? AntesDaProximaGravacao { get; set; }

    /// <summary>Se definido, o próximo <see cref="SalvarAsync"/> lança esta exceção sem gravar nada.</summary>
    public Exception? FalhaAoSalvar { get; set; }

    /// <summary>Se definido, o próximo <see cref="RemoverAsync"/> lança esta exceção sem apagar nada.</summary>
    public Exception? FalhaAoRemover { get; set; }

    public int Conflitos { get; private set; }

    public Task<Fatura?> ObterAsync(string faturaId, CancellationToken cancellationToken)
    {
        lock (_trava)
        {
            return Task.FromResult(_linhas.TryGetValue(faturaId, out var linha) ? Copiar(linha.Fatura, linha.Versao) : null);
        }
    }

    public Task SalvarAsync(Fatura fatura, CancellationToken cancellationToken)
    {
        if (FalhaAoSalvar is { } falha)
        {
            FalhaAoSalvar = null;
            return Task.FromException(falha);
        }

        lock (_trava)
        {
            var versao = _linhas.TryGetValue(fatura.FaturaId, out var atual) ? atual.Versao + 1 : 1;
            _linhas[fatura.FaturaId] = (Copiar(fatura, versao), versao);
            fatura.ETag = Etag(versao);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> AtualizarAsync(Fatura fatura, bool exigirMesmaVersao, CancellationToken cancellationToken)
    {
        if (AntesDaProximaGravacao is { } gancho)
        {
            AntesDaProximaGravacao = null;
            await gancho();
        }

        lock (_trava)
        {
            if (!_linhas.TryGetValue(fatura.FaturaId, out var atual))
            {
                throw new InvalidOperationException($"404: fatura {fatura.FaturaId} não existe.");
            }

            if (exigirMesmaVersao && fatura.ETag != Etag(atual.Versao))
            {
                Conflitos++;
                return false;
            }

            var versao = atual.Versao + 1;
            _linhas[fatura.FaturaId] = (Copiar(fatura, versao), versao);
            fatura.ETag = Etag(versao);
            return true;
        }
    }

    public Task RemoverAsync(string faturaId, CancellationToken cancellationToken)
    {
        // Como o SDK: com o token cancelado, a chamada nem sai.
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (FalhaAoRemover is { } falha)
        {
            FalhaAoRemover = null;
            return Task.FromException(falha);
        }

        lock (_trava)
        {
            _linhas.Remove(faturaId);
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<ResumoFatura> ListarResumosAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        List<ResumoFatura> resumos;
        lock (_trava)
        {
            resumos = _linhas.Values.Select(l => new ResumoFatura(l.Fatura.FaturaId, l.Fatura.Status, l.Fatura.AtualizadaEm)).ToList();
        }

        foreach (var resumo in resumos)
        {
            await Task.Yield();
            yield return resumo;
        }
    }

    /// <summary>Estado atual, para as asserções.</summary>
    public Fatura? Ler(string faturaId) => ObterAsync(faturaId, CancellationToken.None).Result;

    public int Quantidade
    {
        get
        {
            lock (_trava)
            {
                return _linhas.Count;
            }
        }
    }

    /// <summary>Grava uma linha direto, como se outra execução tivesse gravado (sem conferir ETag).</summary>
    public void GravarPorFora(Action<Fatura> alterar, string faturaId)
    {
        lock (_trava)
        {
            var atual = _linhas[faturaId];
            var copia = Copiar(atual.Fatura, atual.Versao);
            alterar(copia);
            _linhas[faturaId] = (Copiar(copia, atual.Versao + 1), atual.Versao + 1);
        }
    }

    private static string Etag(int versao) => $"W/\"v{versao.ToString(CultureInfo.InvariantCulture)}\"";

    private static Fatura Copiar(Fatura f, int versao) => new()
    {
        FaturaId = f.FaturaId,
        ClienteId = f.ClienteId,
        Valor = f.Valor,
        Vencimento = f.Vencimento,
        PagadorCnpj = f.PagadorCnpj,
        ReferenciaErp = f.ReferenciaErp,
        Status = f.Status,
        IdCobranca = f.IdCobranca,
        QuantidadeBaixas = f.QuantidadeBaixas,
        BaixadaEm = f.BaixadaEm,
        CallbackUrl = f.CallbackUrl,
        Caos = f.Caos,
        CriadaEm = f.CriadaEm,
        AtualizadaEm = f.AtualizadaEm,
        ETag = Etag(versao),
    };
}

public sealed class RepositorioInboxEmMemoria : IRepositorioInbox
{
    private readonly Lock _trava = new();
    public List<(string FaturaId, string IdEvento, string Tipo)> Linhas { get; } = [];

    public Task<bool> ExisteAsync(string faturaId, string idEvento, CancellationToken cancellationToken)
    {
        lock (_trava)
        {
            return Task.FromResult(Linhas.Any(l => l.FaturaId == faturaId && l.IdEvento == idEvento));
        }
    }

    public Task RegistrarAsync(string faturaId, string idEvento, string tipo, DateTimeOffset processadoEm, CancellationToken cancellationToken)
    {
        lock (_trava)
        {
            Linhas.RemoveAll(l => l.FaturaId == faturaId && l.IdEvento == idEvento);
            Linhas.Add((faturaId, idEvento, tipo));
        }

        return Task.CompletedTask;
    }
}

/// <summary>Insert condicional: só o primeiro registro de (clienteId, chave) vence, como o 409 do Table Storage.</summary>
public sealed class RepositorioIdempotenciaEmMemoria : IRepositorioIdempotencia
{
    private readonly Dictionary<(string, string), string> _chaves = [];
    private readonly Lock _trava = new();

    public async Task<(bool ChaveNova, string FaturaId)> RegistrarAsync(
        string clienteId, string chave, string faturaId, DateTimeOffset criadaEm, CancellationToken cancellationToken)
    {
        await Task.Yield();
        lock (_trava)
        {
            if (_chaves.TryGetValue((clienteId, chave), out var existente))
            {
                return (false, existente);
            }

            _chaves[(clienteId, chave)] = faturaId;
            return (true, faturaId);
        }
    }

    public Task RemoverAsync(string clienteId, string chave, string faturaId, CancellationToken cancellationToken)
    {
        // Como o SDK: com o token cancelado, a chamada nem sai.
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_trava)
        {
            // Só desfaz a própria reserva: se a chave aponta para outra fatura, fica como está.
            if (_chaves.TryGetValue((clienteId, chave), out var existente) && existente == faturaId)
            {
                _chaves.Remove((clienteId, chave));
            }
        }

        return Task.CompletedTask;
    }
}

public sealed class RepositorioConciliacaoEmMemoria : IRepositorioConciliacao
{
    public List<ResumoConciliacao> Resumos { get; } = [];
    public bool Falhar { get; set; }

    public Task GravarAsync(ResumoConciliacao resumo, CancellationToken cancellationToken)
    {
        if (Falhar)
        {
            throw new InvalidOperationException("Falha simulada ao gravar a conciliação.");
        }

        Resumos.Add(resumo);
        return Task.CompletedTask;
    }
}

public sealed class MensageriaEmMemoria : IMensageria
{
    private readonly Lock _trava = new();
    public List<(string Fila, ServiceBusMessage Mensagem)> Enviadas { get; } = [];

    /// <summary>Se definido, o próximo envio lança esta exceção (por padrão, sem a mensagem chegar à fila).</summary>
    public Exception? FalhaNoProximoEnvio { get; set; }

    /// <summary>
    /// Com <see cref="FalhaNoProximoEnvio"/>: a fila aceita a mensagem e só a confirmação se perde, como num
    /// timeout depois de o Service Bus gravar.
    /// </summary>
    public bool FilaAceitaAntesDaFalha { get; set; }

    /// <summary>Executado no início de cada envio (ex.: simular o APIM desistindo enquanto a fila responde).</summary>
    public Action? AoEnviar { get; set; }

    /// <summary>Como o SDK: com o token cancelado, o envio termina em <see cref="TaskCanceledException"/>.</summary>
    public bool RespeitaCancelamento { get; set; }

    public Task EnviarAsync(string fila, ServiceBusMessage mensagem, CancellationToken cancellationToken)
    {
        AoEnviar?.Invoke();
        if (RespeitaCancelamento && cancellationToken.IsCancellationRequested)
        {
            return Task.FromException(new TaskCanceledException("O envio para a fila foi cancelado pelo token recebido."));
        }

        var falha = FalhaNoProximoEnvio;
        FalhaNoProximoEnvio = null;
        if (falha is null || FilaAceitaAntesDaFalha)
        {
            lock (_trava)
            {
                Enviadas.Add((fila, mensagem));
            }
        }

        return falha is null ? Task.CompletedTask : Task.FromException(falha);
    }

    public IEnumerable<ServiceBusMessage> Na(string fila) => Enviadas.Where(e => e.Fila == fila).Select(e => e.Mensagem);
}

public sealed class PublicadorEmMemoria : IPublicadorDeEventos
{
    private readonly Lock _trava = new();
    public List<FaturaBaixadaDados> Publicados { get; } = [];

    public Task PublicarFaturaBaixadaAsync(FaturaBaixadaDados dados, CancellationToken cancellationToken)
    {
        lock (_trava)
        {
            Publicados.Add(dados);
        }

        return Task.CompletedTask;
    }
}

public sealed class TelemetriaEmMemoria : ITelemetria
{
    private readonly Lock _trava = new();
    public List<(string Nome, IReadOnlyDictionary<string, string?> Propriedades)> Eventos { get; } = [];

    public void Registrar(string evento, IReadOnlyDictionary<string, string?> propriedades)
    {
        lock (_trava)
        {
            Eventos.Add((evento, propriedades));
        }
    }

    public int Contar(string nome) => Eventos.Count(e => e.Nome == nome);

    public IReadOnlyDictionary<string, string?> Unico(string nome) => Assert.Single(Eventos, e => e.Nome == nome).Propriedades;
}

/// <summary>Banco simulado em memória: conta as chamadas (é a "quantidade" do GET /cobrancas/{faturaId}).</summary>
public sealed class BancoEmMemoria : IBancoClient
{
    private readonly Lock _trava = new();
    private int _sequencia;

    public List<SolicitacaoDeCobranca> Chamadas { get; } = [];

    /// <summary>Se definido, a próxima chamada lança esta exceção antes de "registrar".</summary>
    public Exception? Falha { get; set; }

    /// <summary>Executado no início de cada chamada (ex.: simular o APIM desistindo enquanto o banco responde).</summary>
    public Action? AoReceber { get; set; }

    /// <summary>Como o <c>HttpClient</c>: com o token cancelado, a chamada termina em <see cref="TaskCanceledException"/>.</summary>
    public bool RespeitaCancelamento { get; set; }

    public Task<CobrancaEmitida> EmitirCobrancaAsync(SolicitacaoDeCobranca solicitacao, string? caos, CancellationToken cancellationToken)
    {
        AoReceber?.Invoke();
        if (RespeitaCancelamento && cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException("A chamada ao banco foi cancelada pelo token recebido.");
        }

        if (Falha is { } falha)
        {
            throw falha;
        }

        lock (_trava)
        {
            Chamadas.Add(solicitacao);
            _sequencia++;
            return Task.FromResult(new CobrancaEmitida($"COB-{_sequencia:x8}", "00190.00009 01234.567890", "qr-ficticio"));
        }
    }

    public int QuantidadePara(string faturaId) => Chamadas.Count(c => c.FaturaId == faturaId);
}

/// <summary>Registra o settlement em vez de falar com o host (o construtor protegido existe para isso).</summary>
public sealed class AcoesDeMensagemEmMemoria : ServiceBusMessageActions
{
    public int Completadas { get; private set; }
    public int Abandonadas { get; private set; }
    public List<(string? Motivo, string? Descricao)> DeadLetters { get; } = [];

    public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        Completadas++;
        return Task.CompletedTask;
    }

    public override Task AbandonMessageAsync(
        ServiceBusReceivedMessage message,
        IDictionary<string, object>? propertiesToModify = default,
        CancellationToken cancellationToken = default)
    {
        Abandonadas++;
        return Task.CompletedTask;
    }

    public override Task DeadLetterMessageAsync(
        ServiceBusReceivedMessage message,
        Dictionary<string, object>? propertiesToModify = default,
        string? deadLetterReason = default,
        string? deadLetterErrorDescription = default,
        CancellationToken cancellationToken = default)
    {
        DeadLetters.Add((deadLetterReason, deadLetterErrorDescription));
        return Task.CompletedTask;
    }

    public int DestinosDefinidos => Completadas + Abandonadas + DeadLetters.Count;
}

/// <summary>Handler HTTP que só registra as requisições e responde o status configurado.</summary>
public sealed class HandlerDeCaptura(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
{
    public List<(Uri? Uri, string? ContentType)> Requisicoes { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requisicoes.Add((request.RequestUri, request.Content?.Headers.ContentType?.MediaType));
        return Task.FromResult(new HttpResponseMessage(status));
    }
}

/// <summary>Fábrica que entrega sempre um cliente com o mesmo handler, qualquer que seja o nome pedido.</summary>
public sealed class FabricaHttpFixa(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}

public static class Configuracoes
{
    public static IOptions<CenarioOptions> Cenario(
        bool asyncRequestReply = false,
        bool webhookHmac = false,
        bool inbox = false,
        bool dlqComMotivo = false,
        bool retryAgendado = false,
        bool heartbeat = false) =>
        Options.Create(new CenarioOptions
        {
            AsyncRequestReply = asyncRequestReply,
            WebhookHmac = webhookHmac,
            Inbox = inbox,
            DlqComMotivo = dlqComMotivo,
            RetryAgendado = retryAgendado,
            Heartbeat = heartbeat,
        });

    public static IOptions<HubOptions> Hub(string webhookSecret = "segredo-de-teste-saldaro") =>
        Options.Create(new HubOptions
        {
            BancoBaseUrl = "https://banco.exemplo.test/api",
            BancoApiKey = "chave-de-teste",
            WebhookSecret = webhookSecret,
            UrlBasePublica = "https://apim-saldaro-teste.azure-api.net/saldaro",
            WebhookUrlPublica = "https://apim-saldaro-teste.azure-api.net/saldaro/webhooks/banco",
        });
}
