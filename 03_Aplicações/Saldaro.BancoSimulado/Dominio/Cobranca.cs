namespace Saldaro.BancoSimulado.Dominio;

/// <summary>
/// Cobrança registrada no banco simulado. Vira uma linha da tabela <c>cobrancas</c>
/// (PartitionKey = <c>faturaId</c>, RowKey = <c>idCobranca</c>).
/// </summary>
public sealed record Cobranca(
    string FaturaId,
    string IdCobranca,
    string ClienteId,
    decimal Valor,
    string Vencimento,
    string PagadorCnpj,
    string WebhookUrl,
    string LinhaDigitavel,
    string QrCode,
    string Caos,
    DateTimeOffset CriadaEm);

/// <summary>Corpo de <c>POST /api/cobrancas</c> como chega do hub (contrato, seção 6).</summary>
public sealed record PedidoDeCobranca(
    string? FaturaId,
    string? ClienteId,
    decimal? Valor,
    string? Vencimento,
    string? PagadorCnpj,
    string? WebhookUrl)
{
    /// <summary>Teto do valor: cabe nos 10 dígitos de valor da linha digitável fictícia.</summary>
    public const decimal ValorMaximo = 99_999_999.99m;

    /// <summary>Lista os problemas do pedido; vazia quando o pedido é válido.</summary>
    public IReadOnlyList<string> Validar()
    {
        var erros = new List<string>();

        if (string.IsNullOrWhiteSpace(FaturaId))
        {
            erros.Add("faturaId é obrigatório");
        }
        else if (!ChaveDeTabela.EhValida(FaturaId))
        {
            erros.Add($"faturaId precisa ter até {ChaveDeTabela.TamanhoMaximo} caracteres, sem / \\ # ? nem caracteres de controle");
        }

        if (Valor is null)
        {
            erros.Add("valor é obrigatório");
        }
        else if (Valor < 0.01m || Valor > ValorMaximo)
        {
            erros.Add("valor precisa estar entre 0.01 e 99999999.99");
        }

        if (string.IsNullOrWhiteSpace(WebhookUrl))
        {
            erros.Add("webhookUrl é obrigatório");
        }
        else if (!Uri.TryCreate(WebhookUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            erros.Add("webhookUrl precisa ser uma URL absoluta http ou https");
        }

        return erros;
    }
}
