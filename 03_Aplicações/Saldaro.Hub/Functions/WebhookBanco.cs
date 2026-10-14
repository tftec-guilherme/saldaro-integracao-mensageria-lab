using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Functions;

/// <summary>
/// <c>POST /api/webhooks/banco</c>: o banco avisa que a cobrança foi registrada ou paga.
/// Valida a assinatura (toggle <c>Cenario:WebhookHmac</c>) e enfileira o corpo bruto em
/// <c>sbq-confirmacoes-banco</c>. Não processa nada aqui: responde 202 e a baixa acontece no worker.
/// </summary>
public sealed class WebhookBanco(
    IMensageria mensageria,
    ITelemetria telemetria,
    IOptions<CenarioOptions> cenario,
    IOptions<HubOptions> hub,
    TimeProvider relogio,
    ILogger<WebhookBanco> logger)
{
    /// <summary>Limite do corpo: o tamanho máximo de mensagem do Service Bus Standard (256 KB).</summary>
    public const int TamanhoMaximoDoCorpo = 256 * 1024;

    // Simplificação de laboratório: HTTP trigger anônimo (AuthorizationLevel.Anonymous). A defesa do webhook no
    // Incidente 2 é a assinatura HMAC do corpo (Cenario:WebhookHmac). Em produção, ela e o timestamp ficariam, com
    // ip-filter (IPs do banco) ou mTLS no APIM, conforme o contrato com o parceiro, e o backend aceitaria só o APIM.
    [Function(nameof(WebhookBanco))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhooks/banco")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        // Corpo bruto, byte a byte: é sobre ele que o banco calculou o HMAC.
        using var memoria = new MemoryStream();
        await req.Body.CopyToAsync(memoria, cancellationToken);
        if (memoria.Length > TamanhoMaximoDoCorpo)
        {
            return new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
        }

        var corpo = memoria.ToArray();

        // TOGGLE Cenario:WebhookHmac (cenário 3.3). No baseline qualquer POST é aceito, inclusive uma
        // confirmação forjada por quem só conhece a URL.
        if (cenario.Value.WebhookHmac)
        {
            var resultado = AssinaturaWebhook.Validar(
                hub.Value.WebhookSecret,
                req.Headers[Cabecalhos.Timestamp].ToString(),
                req.Headers[Cabecalhos.Assinatura].ToString(),
                corpo,
                relogio.GetUtcNow());

            if (resultado != ResultadoDaAssinatura.Valida)
            {
                logger.LogWarning("Webhook recusado com 401: {Resultado}.", resultado);
                return new UnauthorizedObjectResult(new { erro = $"Assinatura do webhook recusada: {resultado}." });
            }
        }

        // Só os campos de roteamento; o valor nem é lido aqui (um valor fora do schema é problema do worker).
        if (!IdentificacaoDoEvento.TentarExtrair(corpo, out var idEvento, out var faturaId, out var tipo, out var erro))
        {
            return new BadRequestObjectResult(new { erro });
        }

        var mensagem = new ServiceBusMessage(corpo)
        {
            // CONCEITO: detecção de duplicatas. Com requiresDuplicateDetection na fila, o Service Bus descarta
            // em silêncio outra mensagem com o mesmo MessageId dentro da janela (10 min). O id do evento do
            // banco vira o MessageId: a confirmação repetida não entra na fila duas vezes.
            MessageId = idEvento,

            // CONCEITO: sessions. SessionId = faturaId agrupa as confirmações de cada fatura; numa fila com
            // sessions, cada sessão tem um consumidor por vez e as mensagens dela saem na ordem de chegada.
            SessionId = faturaId,
            ContentType = "application/json",
        };
        mensagem.ApplicationProperties[PropriedadesDaMensagem.Tipo] = tipo;

        await mensageria.EnviarAsync(Filas.ConfirmacoesBanco, mensagem, cancellationToken);
        telemetria.ConfirmacaoRecebida(faturaId, idEvento, tipo);

        return new AcceptedResult();
    }
}
