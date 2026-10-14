using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Aplicacao;

/// <summary>
/// Log estruturado de cada confirmação recebida pelos workers. As chaves (<c>Tipo</c>, <c>FaturaId</c>,
/// <c>SessionId</c>, <c>SequenceNumber</c>, <c>DeliveryCount</c>, <c>MessageId</c>) viram colunas de
/// <c>customDimensions</c> na tabela <c>traces</c>: é com elas que o guia do Incidente 2 mostra a ordem por sessão.
/// </summary>
public static class LogDeConfirmacao
{
    public static void Registrar(ILogger logger, ServiceBusReceivedMessage mensagem, DateTimeOffset? sessaoTravadaAte = null)
    {
        var tipo = mensagem.ApplicationProperties.TryGetValue(PropriedadesDaMensagem.Tipo, out var valor) ? valor?.ToString() : null;
        var faturaId = mensagem.SessionId ?? IdentificacaoDoEvento.MelhorEsforco(mensagem.Body.ToMemory()).FaturaId;

        if (sessaoTravadaAte is { } travadaAte)
        {
            logger.LogInformation(
                "Confirmação {Tipo} da fatura {FaturaId}: SessionId={SessionId} SequenceNumber={SequenceNumber} DeliveryCount={DeliveryCount} MessageId={MessageId} SessionLockedUntil={SessionLockedUntil}",
                tipo, faturaId, mensagem.SessionId, mensagem.SequenceNumber, mensagem.DeliveryCount, mensagem.MessageId, travadaAte);
            return;
        }

        logger.LogInformation(
            "Confirmação {Tipo} da fatura {FaturaId}: SessionId={SessionId} SequenceNumber={SequenceNumber} DeliveryCount={DeliveryCount} MessageId={MessageId}",
            tipo, faturaId, mensagem.SessionId, mensagem.SequenceNumber, mensagem.DeliveryCount, mensagem.MessageId);
    }
}
