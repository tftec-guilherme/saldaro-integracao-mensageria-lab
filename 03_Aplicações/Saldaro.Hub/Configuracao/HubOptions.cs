namespace Saldaro.Hub.Configuracao;

/// <summary>
/// App settings simples do hub (seção 5.2), lidos da raiz da configuração.
/// Com <c>identidadeGerenciada = true</c>, <c>BancoApiKey</c> e <c>WebhookSecret</c> passam a ser
/// referências do Key Vault (<c>@Microsoft.KeyVault(...)</c>): a plataforma resolve a referência e o
/// código continua lendo o mesmo setting, sem mudar uma linha.
/// </summary>
public sealed class HubOptions
{
    /// <summary>URL base do banco simulado: <c>https://&lt;defaultHostName do banco&gt;/api</c>.</summary>
    public string BancoBaseUrl { get; set; } = string.Empty;

    /// <summary>Chave enviada no header <c>x-api-key</c> para o banco.</summary>
    public string BancoApiKey { get; set; } = string.Empty;

    /// <summary>Segredo do HMAC do webhook (a string do setting, sem decodificar base64).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>URL pública da API (a do APIM), base dos headers <c>Location</c>.</summary>
    public string UrlBasePublica { get; set; } = string.Empty;

    /// <summary>URL que o banco chama de volta: <c>{apimBaseUrl}/webhooks/banco</c>.</summary>
    public string WebhookUrlPublica { get; set; } = string.Empty;
}
