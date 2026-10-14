using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Saldaro.Hub.Aplicacao;

/// <summary>
/// Repassa as ações de settlement para as do runtime e lembra se a mensagem já recebeu um destino
/// (complete, abandon, dead-letter ou defer).
/// </summary>
public sealed class AcoesRastreadas(ServiceBusMessageActions acoes) : ServiceBusMessageActions
{
    public bool DestinoDefinido { get; private set; }

    public override async Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        await acoes.CompleteMessageAsync(message, cancellationToken);
        DestinoDefinido = true;
    }

    public override async Task AbandonMessageAsync(
        ServiceBusReceivedMessage message,
        IDictionary<string, object>? propertiesToModify = default,
        CancellationToken cancellationToken = default)
    {
        await acoes.AbandonMessageAsync(message, propertiesToModify, cancellationToken);
        DestinoDefinido = true;
    }

    public override async Task DeadLetterMessageAsync(
        ServiceBusReceivedMessage message,
        Dictionary<string, object>? propertiesToModify = default,
        string? deadLetterReason = default,
        string? deadLetterErrorDescription = default,
        CancellationToken cancellationToken = default)
    {
        await acoes.DeadLetterMessageAsync(message, propertiesToModify, deadLetterReason, deadLetterErrorDescription, cancellationToken);
        DestinoDefinido = true;
    }

    public override async Task DeferMessageAsync(
        ServiceBusReceivedMessage message,
        IDictionary<string, object>? propertiesToModify = default,
        CancellationToken cancellationToken = default)
    {
        await acoes.DeferMessageAsync(message, propertiesToModify, cancellationToken);
        DestinoDefinido = true;
    }

    public override Task RenewMessageLockAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default) =>
        acoes.RenewMessageLockAsync(message, cancellationToken);
}

/// <summary>Processa uma mensagem e, se falhar antes de dar destino a ela, faz abandon explícito e relança a exceção.</summary>
public static class AbandonExplicito
{
    /// <remarks>
    /// CONCEITO: abandon explícito. Com <c>AutoCompleteMessages = false</c>, quem decide o destino da mensagem é o
    /// código. Quando o processamento falha sem conserto, o abandon devolve a mensagem para a fila na hora (o
    /// Service Bus soma 1 no DeliveryCount e reentrega), em vez de ela esperar o lock de 1 minuto expirar. A exceção
    /// é relançada para a execução aparecer como falha no Application Insights. É assim que, no baseline, as
    /// maxDeliveryCount tentativas acabam em segundos e a mensagem cai na DLQ com <c>MaxDeliveryCountExceeded</c>.
    /// </remarks>
    public static async Task ProcessarAsync(
        ServiceBusReceivedMessage mensagem,
        ServiceBusMessageActions acoes,
        Func<ServiceBusMessageActions, Task> processar,
        ILogger logger)
    {
        var rastreadas = acoes as AcoesRastreadas ?? new AcoesRastreadas(acoes);
        try
        {
            await processar(rastreadas);
        }
        catch (Exception erro) when (!rastreadas.DestinoDefinido)
        {
            try
            {
                await rastreadas.AbandonMessageAsync(mensagem, cancellationToken: CancellationToken.None);
            }
            catch (Exception falhaNoAbandon)
            {
                logger.LogWarning(falhaNoAbandon, "Abandon da mensagem {MessageId} falhou; ela volta à fila quando o lock expirar.", mensagem.MessageId);
            }

            // Sem o objeto da exceção no log: o runtime já registra a falha da execução em "exceptions".
            logger.LogWarning(
                "Mensagem {MessageId} devolvida à fila com abandon (DeliveryCount={DeliveryCount}) por {TipoDoErro}: {Erro}",
                mensagem.MessageId, mensagem.DeliveryCount, erro.GetType().Name, erro.Message);
            throw;
        }
    }
}
