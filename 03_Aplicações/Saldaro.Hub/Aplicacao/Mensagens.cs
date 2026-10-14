using System.Diagnostics.CodeAnalysis;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Aplicacao;

/// <summary>
/// Corpo de <c>POST /faturas</c> enviado pelo ERP:
/// <c>{ "valor": 1250.00, "vencimento": "2026-11-10", "pagadorCnpj": "...", "referenciaErp": "ERP-0001", "callbackUrl": null, "caos": "latencia=45&amp;duplicar=3" }</c>.
/// <c>valor</c> e <c>vencimento</c> são obrigatórios; <c>referenciaErp</c>, <c>callbackUrl</c> e <c>caos</c> são opcionais.
/// O <c>clienteId</c> vem do header <c>x-cliente-id</c> e o <c>faturaId</c> é gerado pelo hub; campos a mais no
/// corpo são ignorados (leitor tolerante).
/// </summary>
public sealed class NovaFatura
{
    public decimal? Valor { get; set; }
    public string? Vencimento { get; set; }
    public string? PagadorCnpj { get; set; }
    public string? ReferenciaErp { get; set; }
    public string? CallbackUrl { get; set; }

    /// <summary>Botões de caos para o banco, no formato de query string: <c>latencia=45&amp;duplicar=3</c>.</summary>
    public string? Caos { get; set; }

    public const int TamanhoMaximoDoTexto = 500;

    /// <summary>
    /// Valida o corpo; quando não é aceitável, <paramref name="problemas"/> lista tudo o que está errado
    /// (vira o <c>detalhes</c> da resposta 400).
    /// </summary>
    public static bool EhValida([NotNullWhen(true)] NovaFatura? pedido, out IReadOnlyList<string> problemas)
    {
        problemas = Validar(pedido);
        return problemas.Count == 0;
    }

    private static List<string> Validar(NovaFatura? pedido)
    {
        if (pedido is null)
        {
            return ["Corpo obrigatório: { valor, vencimento, pagadorCnpj, referenciaErp?, callbackUrl?, caos? }."];
        }

        var problemas = new List<string>();
        if (pedido.Valor is not > 0)
        {
            problemas.Add("Campo 'valor' obrigatório: número JSON maior que zero (ex.: 1250.00).");
        }

        if (string.IsNullOrWhiteSpace(pedido.Vencimento))
        {
            problemas.Add("Campo 'vencimento' obrigatório (ex.: \"2026-11-10\").");
        }

        string?[] textos = [pedido.Vencimento, pedido.PagadorCnpj, pedido.ReferenciaErp, pedido.CallbackUrl, pedido.Caos];
        if (textos.Any(texto => texto is { Length: > TamanhoMaximoDoTexto }))
        {
            problemas.Add($"Campos de texto têm no máximo {TamanhoMaximoDoTexto} caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(pedido.CallbackUrl)
            && !(Uri.TryCreate(pedido.CallbackUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)))
        {
            problemas.Add("Campo 'callbackUrl' precisa ser uma URL http(s) absoluta.");
        }

        return problemas;
    }
}

/// <summary>Mensagem da fila <c>sbq-faturas-recebidas</c> (seção 7.1), com <c>MessageId = faturaId</c>.</summary>
public sealed record FaturaRecebidaMensagem(
    string FaturaId,
    string ClienteId,
    decimal Valor,
    string? Vencimento,
    string? PagadorCnpj,
    string? ReferenciaErp,
    string? CallbackUrl,
    string? Caos)
{
    public static FaturaRecebidaMensagem De(Fatura fatura) => new(
        fatura.FaturaId,
        fatura.ClienteId,
        fatura.Valor,
        fatura.Vencimento,
        fatura.PagadorCnpj,
        fatura.ReferenciaErp,
        fatura.CallbackUrl,
        fatura.Caos);
}

/// <summary>Representação JSON da fatura em <c>GET /faturas/{id}</c>.</summary>
public sealed record FaturaResposta(
    string FaturaId,
    string ClienteId,
    decimal Valor,
    string? Vencimento,
    string? PagadorCnpj,
    string? ReferenciaErp,
    string Status,
    string? IdCobranca,
    int QuantidadeBaixas,
    DateTimeOffset? BaixadaEm,
    string? CallbackUrl,
    string? Caos,
    DateTimeOffset CriadaEm,
    DateTimeOffset AtualizadaEm)
{
    public static FaturaResposta De(Fatura f) => new(
        f.FaturaId, f.ClienteId, f.Valor, f.Vencimento, f.PagadorCnpj, f.ReferenciaErp, f.Status, f.IdCobranca,
        f.QuantidadeBaixas, f.BaixadaEm, f.CallbackUrl, f.Caos, f.CriadaEm, f.AtualizadaEm);
}
