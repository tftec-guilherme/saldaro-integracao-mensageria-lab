using System.Globalization;
using Azure.Messaging.ServiceBus;

namespace Saldaro.Ferramentas;

/// <summary>
/// O que a triagem precisa saber de uma mensagem da DLQ, desacoplado do SDK para poder ser
/// testado sem Azure.
/// </summary>
internal sealed record MensagemDaDlq(
    long NumeroDeSequencia,
    string MessageId,
    string? SessionId,
    string? Motivo,
    string? Descricao,
    DateTimeOffset EnfileiradaEm,
    int Entregas,
    string? ContentType,
    string? Subject,
    string? CorrelationId,
    IReadOnlyDictionary<string, object> Propriedades,
    BinaryData Corpo)
{
    // CONCEITO: DeadLetterReason e DeadLetterErrorDescription viajam como propriedades de aplicação
    // da mensagem morta. O motivo pode ser nosso (SchemaInvalida, TentativasEsgotadas) ou da
    // plataforma (MaxDeliveryCountExceeded, TTLExpiredException).
    public static MensagemDaDlq De(ServiceBusReceivedMessage mensagem)
    {
        ArgumentNullException.ThrowIfNull(mensagem);
        return new MensagemDaDlq(
            mensagem.SequenceNumber,
            mensagem.MessageId ?? string.Empty,
            mensagem.SessionId,
            mensagem.DeadLetterReason,
            mensagem.DeadLetterErrorDescription,
            mensagem.EnqueuedTime,
            mensagem.DeliveryCount,
            mensagem.ContentType,
            mensagem.Subject,
            mensagem.CorrelationId,
            new Dictionary<string, object>(mensagem.ApplicationProperties, StringComparer.Ordinal),
            mensagem.Body);
    }

    /// <summary>Valor de uma propriedade de aplicação como texto, ou nulo se não existir.</summary>
    public string? Propriedade(string nome) =>
        Propriedades.TryGetValue(nome, out var valor) ? Convert.ToString(valor, CultureInfo.InvariantCulture) : null;
}
