using Azure.Messaging.ServiceBus;

namespace Saldaro.Ferramentas;

/// <summary>
/// O que fazer com uma mensagem da DLQ. Hierarquia fechada (o construtor é privado, como o <c>ResultadoDoRegistro</c>
/// do banco simulado): só existem os três casos abaixo, e só o reprocessamento carrega a cópia para a fila principal.
/// </summary>
internal abstract record AcaoDeTriagem
{
    private AcaoDeTriagem(string explicacao) => Explicacao = explicacao;

    /// <summary>Por que a triagem escolheu esta ação; sai na tela ao lado do MessageId.</summary>
    public string Explicacao { get; }

    /// <summary>Reprocessa: envia uma cópia (corrigida, se preciso) para a fila principal e tira da DLQ.</summary>
    public sealed record Reprocessar(string Explicacao, MensagemDeReprocesso NovaMensagem) : AcaoDeTriagem(Explicacao);

    /// <summary>Completa a mensagem da DLQ: ela some de vez.</summary>
    public sealed record Descartar(string Explicacao) : AcaoDeTriagem(Explicacao);

    /// <summary>Devolve a mensagem para a DLQ, sem alteração.</summary>
    public sealed record Manter(string Explicacao) : AcaoDeTriagem(Explicacao);
}

/// <summary>A cópia que vai para a fila principal no reprocessamento.</summary>
internal sealed record MensagemDeReprocesso(
    string MessageId,
    string Original,
    int Numero,
    string? SessionId,
    BinaryData Corpo,
    bool ValorCorrigido,
    string? ContentType,
    string? Subject,
    string? CorrelationId,
    IReadOnlyDictionary<string, object> Propriedades)
{
    public ServiceBusMessage ParaServiceBus()
    {
        var mensagem = new ServiceBusMessage(Corpo)
        {
            MessageId = MessageId,
            SessionId = SessionId,
            ContentType = ContentType,
            Subject = Subject,
            CorrelationId = CorrelationId,
        };
        foreach (var (chave, valor) in Propriedades)
        {
            mensagem.ApplicationProperties[chave] = valor;
        }

        return mensagem;
    }
}

/// <summary>Decide, sem tocar no Service Bus, o que fazer com cada mensagem da DLQ.</summary>
internal static class DecisaoDeTriagem
{
    /// <summary>
    /// Propriedades que não vão para a cópia: o motivo da morte (é da mensagem antiga), a contagem
    /// de tentativas do retry agendado (a cópia recomeça do zero) e o reprocessadoDe (é regravado).
    /// </summary>
    public static IReadOnlySet<string> PropriedadesQueNaoViajam { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "DeadLetterReason",
        "DeadLetterErrorDescription",
        "tentativa",
        IdDeReprocesso.Propriedade,
    };

    public static AcaoDeTriagem Decidir(MensagemDaDlq mensagem, bool reprocessar, string? descartarMotivo)
    {
        ArgumentNullException.ThrowIfNull(mensagem);

        if (!string.IsNullOrWhiteSpace(descartarMotivo)
            && string.Equals(mensagem.Motivo, descartarMotivo.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return new AcaoDeTriagem.Descartar($"motivo {mensagem.Motivo} (descartada a pedido)");
        }

        if (!reprocessar)
        {
            return new AcaoDeTriagem.Manter($"motivo {mensagem.Motivo ?? ResumoDaDlq.SemMotivo} (fora do --descartar-motivo)");
        }

        var correcao = CorrecaoDeValor.Corrigir(mensagem.Corpo);
        if (!correcao.PodeReprocessar)
        {
            return new AcaoDeTriagem.Manter(correcao.Explicacao);
        }

        var id = IdDeReprocesso.Montar(mensagem.MessageId, mensagem.Propriedade(IdDeReprocesso.Propriedade));
        var propriedades = mensagem.Propriedades
            .Where(p => !PropriedadesQueNaoViajam.Contains(p.Key))
            .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        propriedades[IdDeReprocesso.Propriedade] = id.Original;

        // CONCEITO: mesmo SessionId (= faturaId). A cópia entra na mesma sessão da fatura e respeita
        // a ordem por fatura; numa fila com sessions, mensagem sem SessionId nem seria aceita.
        var nova = new MensagemDeReprocesso(
            id.MessageId,
            id.Original,
            id.Numero,
            mensagem.SessionId,
            correcao.Corpo,
            correcao.Corrigiu,
            mensagem.ContentType,
            mensagem.Subject,
            mensagem.CorrelationId,
            propriedades);
        return new AcaoDeTriagem.Reprocessar(correcao.Explicacao, nova);
    }
}
