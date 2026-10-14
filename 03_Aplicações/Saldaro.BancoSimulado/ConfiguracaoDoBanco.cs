namespace Saldaro.BancoSimulado;

/// <summary>
/// Segredos do banco, lidos dos app settings de mesmo nome (padrão Options). Em Azure, o Bicep
/// grava os mesmos valores que o hub usa; localmente, vêm do <c>local.settings.json</c>.
/// </summary>
public sealed class ConfiguracaoDoBanco
{
    /// <summary>Chave que o hub precisa mandar no header <c>x-api-key</c>.</summary>
    public string BancoApiKey { get; set; } = string.Empty;

    /// <summary>Segredo do HMAC dos webhooks (a mesma string configurada no hub).</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
