using System.Net;
using Saldaro.BancoSimulado.Dominio;
using Saldaro.BancoSimulado.Servicos;

namespace Saldaro.BancoSimulado.Tests;

/// <summary>Relógio parado num instante conhecido.</summary>
internal sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;
}

/// <summary>Sorteio com respostas pré-definidas, na ordem das chamadas; depois delas, <c>false</c>.</summary>
internal sealed class SorteioRoteirizado(params bool[] respostas) : ISorteio
{
    private readonly Queue<bool> _respostas = new(respostas);

    public List<int> PercentuaisConsultados { get; } = [];

    public bool Dispara(int percentual)
    {
        PercentuaisConsultados.Add(percentual);
        return _respostas.Count > 0 && _respostas.Dequeue();
    }
}

/// <summary>Gera os GUIDs 00000000-0000-0000-0000-000000000001, ...0002, ... em sequência.</summary>
internal sealed class GeradorDeIdsSequencial
{
    private int _proximo;

    public Guid Proximo() => new($"00000000-0000-0000-0000-{++_proximo:D12}");
}

/// <summary>Registra a ordem dos efeitos de <see cref="RegistroDeCobranca"/> numa única lista.</summary>
internal sealed class RegistroDeEfeitos
{
    public List<string> Efeitos { get; } = [];
}

internal sealed class RepositorioEmMemoria(RegistroDeEfeitos registro) : IRepositorioDeCobrancas
{
    public List<Cobranca> Cobrancas { get; } = [];

    public Task GravarAsync(Cobranca cobranca, CancellationToken cancellationToken)
    {
        registro.Efeitos.Add("gravar");
        Cobrancas.Add(cobranca);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Cobranca>> ListarPorFaturaAsync(string faturaId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Cobranca>>(Cobrancas.Where(c => c.FaturaId == faturaId).ToList());
}

internal sealed class FilaEmMemoria(RegistroDeEfeitos registro) : IFilaDePagamentos
{
    public List<(string Mensagem, TimeSpan Atraso)> Mensagens { get; } = [];

    public Task EnfileirarAsync(string mensagem, TimeSpan atraso, CancellationToken cancellationToken)
    {
        registro.Efeitos.Add($"enfileirar:{atraso.TotalSeconds:0}");
        Mensagens.Add((mensagem, atraso));
        return Task.CompletedTask;
    }
}

internal sealed class EsperaRegistrada(RegistroDeEfeitos registro) : IEspera
{
    public Task EsperarAsync(TimeSpan duracao, CancellationToken cancellationToken)
    {
        registro.Efeitos.Add($"esperar:{duracao.TotalSeconds:0}");
        return Task.CompletedTask;
    }
}

/// <summary>HttpMessageHandler que guarda as requisições e responde com o que o teste mandar.</summary>
internal sealed class HandlerDeCaptura(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<RequisicaoCapturada> Requisicoes { get; } = [];

    public HandlerDeCaptura(HttpStatusCode status)
        : this(_ => new HttpResponseMessage(status))
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Copia tudo na hora: quem chamou descarta a requisição logo depois.
        var corpo = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        Requisicoes.Add(new RequisicaoCapturada(request.Method, request.RequestUri!, headers, request.Content?.Headers.ContentType?.ToString(), corpo));
        return responder(request);
    }
}

internal sealed record RequisicaoCapturada(
    HttpMethod Metodo,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    string? ContentType,
    byte[] Corpo)
{
    public string Header(string nome) => Headers.TryGetValue(nome, out var valor) ? valor : string.Empty;
}
