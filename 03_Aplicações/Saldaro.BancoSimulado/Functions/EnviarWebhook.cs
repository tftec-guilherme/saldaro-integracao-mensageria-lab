using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Saldaro.BancoSimulado.Dominio;
using Saldaro.BancoSimulado.Infraestrutura;
using Saldaro.BancoSimulado.Servicos;

namespace Saldaro.BancoSimulado.Functions;

/// <summary>Lançada para o Functions devolver a mensagem à fila e tentar de novo.</summary>
public sealed class WebhookNaoEntregueException(string mensagem) : Exception(mensagem);

/// <summary>
/// Trigger da fila <c>banco-pagamentos</c>: assina o evento (contrato, seção 7.2) e faz o POST no
/// <c>webhookUrl</c> do hub. Cada mensagem é uma entrega; duplicatas são mensagens separadas.
/// </summary>
public sealed class EnviarWebhook(RemetenteDeWebhook remetente, ILogger<EnviarWebhook> logger)
{
    [Function(nameof(EnviarWebhook))]
    public async Task Run(
        [QueueTrigger(FilaDePagamentos.NomeDaFila, Connection = FilaDePagamentos.SettingDaConexao)] string textoDaMensagem,
        FunctionContext contexto,
        CancellationToken cancellationToken)
    {
        // Mensagem fora do formato lança FormatException: depois de maxDequeueCount tentativas,
        // o Functions a move para a fila banco-pagamentos-poison.
        var mensagem = MensagemDeWebhook.Ler(textoDaMensagem);
        var evento = mensagem.Identificar();
        var tentativa = LerTentativa(contexto);

        var envio = await remetente.EnviarAsync(mensagem, cancellationToken);
        switch (envio.Resultado)
        {
            case ResultadoDaEntrega.Entregue:
                logger.LogInformation(
                    "Webhook {Tipo} (idEvento {IdEvento}) da fatura {FaturaId} entregue: HTTP {StatusCode}, tentativa {Tentativa}, assinaturaInvalida={AssinaturaInvalida}.",
                    evento.Tipo, evento.IdEvento, evento.FaturaId, envio.StatusCode, tentativa, mensagem.AssinaturaInvalida);
                break;

            case ResultadoDaEntrega.Rejeitada:
                // O banco não insiste num evento rejeitado: não lança, então a mensagem sai da fila sem nova tentativa.
                logger.LogWarning(
                    "Webhook {Tipo} (idEvento {IdEvento}) da fatura {FaturaId} rejeitado pelo hub: HTTP {StatusCode}, assinaturaInvalida={AssinaturaInvalida}. " +
                    "Sem nova tentativa. Num 401 sem assinatura=invalida, confira se o WebhookSecret é igual no hub e no banco.",
                    evento.Tipo, evento.IdEvento, evento.FaturaId, envio.StatusCode, mensagem.AssinaturaInvalida);
                break;

            default:
                throw new WebhookNaoEntregueException(
                    $"Webhook {evento.Tipo} (idEvento {evento.IdEvento}) da fatura {evento.FaturaId} não entregue: " +
                    $"HTTP {envio.StatusCode} na tentativa {tentativa}. A mensagem volta para a fila e o Functions tenta de novo.");
        }
    }

    private static string LerTentativa(FunctionContext contexto) =>
        contexto.BindingContext.BindingData.TryGetValue("DequeueCount", out var valor) && valor is not null
            ? valor.ToString() ?? "?"
            : "?";
}
