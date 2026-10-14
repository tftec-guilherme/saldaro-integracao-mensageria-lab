using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Saldaro.BancoSimulado.Dominio;

namespace Saldaro.BancoSimulado.Servicos;

public sealed record ResultadoDoEnvio(int StatusCode, ResultadoDaEntrega Resultado);

/// <summary>Assina o evento (contrato, seção 7.2) e faz o POST no <c>webhookUrl</c> do hub.</summary>
public sealed class RemetenteDeWebhook(HttpClient http, IOptions<ConfiguracaoDoBanco> segredos, TimeProvider relogio)
{
    public async Task<ResultadoDoEnvio> EnviarAsync(MensagemDeWebhook mensagem, CancellationToken cancellationToken)
    {
        var segredo = segredos.Value.WebhookSecret;
        if (string.IsNullOrEmpty(segredo))
        {
            throw new InvalidOperationException("O setting WebhookSecret está vazio: o banco não tem como assinar o webhook.");
        }

        // Assina no momento do envio (e não ao enfileirar): o timestamp precisa estar dentro dos ±300 s
        // que o hub tolera, mesmo quando o atrasoWebhook é longo ou a entrega é uma nova tentativa.
        var timestamp = relogio.GetUtcNow().ToUnixTimeSeconds();
        var assinatura = AssinaturaDeWebhook.Calcular(segredo, timestamp, mensagem.Corpo);
        if (mensagem.AssinaturaInvalida)
        {
            assinatura = AssinaturaDeWebhook.Corromper(assinatura);
        }

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, mensagem.WebhookUrl)
        {
            // Os mesmos bytes que foram assinados: nada de reserializar o evento aqui.
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes(mensagem.Corpo)),
        };
        requisicao.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        requisicao.Headers.Add(AssinaturaDeWebhook.HeaderTimestamp, timestamp.ToString(CultureInfo.InvariantCulture));
        requisicao.Headers.Add(AssinaturaDeWebhook.HeaderAssinatura, assinatura);

        using var resposta = await http.SendAsync(requisicao, cancellationToken);
        var statusCode = (int)resposta.StatusCode;
        return new ResultadoDoEnvio(statusCode, ClassificacaoDaEntrega.Classificar(statusCode));
    }
}
