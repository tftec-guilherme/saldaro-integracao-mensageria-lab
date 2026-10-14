using Microsoft.AspNetCore.Http;

namespace Saldaro.Hub.Configuracao;

/// <summary>URLs públicas usadas nos headers <c>Location</c> e no <c>webhookUrl</c> enviado ao banco.</summary>
public static class UrlsPublicas
{
    /// <summary>
    /// <c>UrlBasePublica</c> (a URL do APIM, ex.: <c>https://apim-saldaro-&lt;sufixo&gt;.azure-api.net/saldaro</c>).
    /// Sem o setting (execução local), cai para o endereço da própria requisição.
    /// </summary>
    public static string Base(HubOptions hub, HttpRequest requisicao) =>
        string.IsNullOrWhiteSpace(hub.UrlBasePublica)
            ? $"{requisicao.Scheme}://{requisicao.Host}/api"
            : hub.UrlBasePublica.TrimEnd('/');

    /// <summary><c>WebhookUrlPublica</c>; sem o setting, <c>{UrlBasePublica}/webhooks/banco</c>.</summary>
    public static string Webhook(HubOptions hub)
    {
        if (!string.IsNullOrWhiteSpace(hub.WebhookUrlPublica))
        {
            return hub.WebhookUrlPublica;
        }

        return string.IsNullOrWhiteSpace(hub.UrlBasePublica)
            ? string.Empty
            : $"{hub.UrlBasePublica.TrimEnd('/')}/webhooks/banco";
    }
}
