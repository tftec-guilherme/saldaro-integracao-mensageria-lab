namespace Saldaro.Hub.Dominio;

/// <summary>Efeito colateral que acompanha uma transição.</summary>
public enum EfeitoDaTransicao
{
    Nenhum,

    /// <summary>Grava <c>baixadaEm</c>, soma <c>quantidadeBaixas</c>, publica <c>FaturaBaixada</c>.</summary>
    Baixa,
}

/// <summary>Decisão da máquina de estados para um evento sobre um status atual.</summary>
public sealed record DecisaoDeEstado(bool Transiciona, string StatusResultante, EfeitoDaTransicao Efeito, string Motivo)
{
    public static DecisaoDeEstado Para(string novoStatus, EfeitoDaTransicao efeito, string motivo) =>
        new(true, novoStatus, efeito, motivo);

    public static DecisaoDeEstado Ignorar(string statusAtual, string motivo) =>
        new(false, statusAtual, EfeitoDaTransicao.Nenhum, motivo);
}

/// <summary>
/// Máquina de estados da fatura: <c>recebida → cobranca-emitida → registrada → baixada → notificada</c>.
/// </summary>
/// <remarks>
/// CONCEITO: máquina de estados tolerante a desordem. A pergunta não é "qual evento chegou?", e sim
/// "esse evento ainda muda alguma coisa a partir de onde a fatura está?". Uma confirmação repetida ou
/// atrasada vira no-op em vez de baixar de novo ou "desbaixar" a fatura.
/// </remarks>
public static class MaquinaDeEstados
{
    /// <summary>Com guarda (<c>Cenario:Inbox = true</c>): decide a partir do status atual.</summary>
    public static DecisaoDeEstado AvaliarConfirmacao(string statusAtual, string tipoEvento)
    {
        return tipoEvento switch
        {
            TiposDeEvento.CobrancaPaga => statusAtual switch
            {
                // "recebida" entra porque o banco pode confirmar antes de responder à emissão
                // (latência alta): o pagamento é real e não pode ser descartado.
                StatusFatura.Recebida or StatusFatura.CobrancaEmitida or StatusFatura.Registrada =>
                    DecisaoDeEstado.Para(StatusFatura.Baixada, EfeitoDaTransicao.Baixa, "pagamento confirmado pelo banco"),
                StatusFatura.Baixada or StatusFatura.Notificada =>
                    DecisaoDeEstado.Ignorar(statusAtual, "fatura já baixada: confirmação repetida não baixa de novo"),

                // O status chega da tabela como texto. Um valor fora da lista (ex.: uma linha editada à mão no
                // Storage browser) não transiciona e não derruba o worker: a decisão vai para o log com o motivo.
                _ => DecisaoDeEstado.Ignorar(statusAtual, $"status desconhecido '{statusAtual}'"),
            },

            TiposDeEvento.CobrancaRegistrada => statusAtual switch
            {
                StatusFatura.Recebida or StatusFatura.CobrancaEmitida =>
                    DecisaoDeEstado.Para(StatusFatura.Registrada, EfeitoDaTransicao.Nenhum, "cobrança registrada no banco"),
                StatusFatura.Registrada =>
                    DecisaoDeEstado.Ignorar(statusAtual, "cobrança já registrada"),
                StatusFatura.Baixada or StatusFatura.Notificada =>
                    DecisaoDeEstado.Ignorar(statusAtual, "'cobranca.registrada' chegou depois da baixa: não desbaixa a fatura"),
                _ => DecisaoDeEstado.Ignorar(statusAtual, $"status desconhecido '{statusAtual}'"),
            },

            _ => throw new ArgumentOutOfRangeException(nameof(tipoEvento), tipoEvento, "Tipo de evento desconhecido."),
        };
    }

    /// <summary>
    /// Baseline (<c>Cenario:Inbox = false</c>): sem guarda nenhuma. Cada <c>cobranca.paga</c> baixa e publica
    /// de novo; um <c>cobranca.registrada</c> atrasado devolve a fatura para <c>registrada</c> ("desbaixa").
    /// É de propósito: são os sintomas do Incidente 2.
    /// </summary>
    public static DecisaoDeEstado AvaliarSemGuarda(string tipoEvento) => tipoEvento switch
    {
        TiposDeEvento.CobrancaPaga => DecisaoDeEstado.Para(StatusFatura.Baixada, EfeitoDaTransicao.Baixa, "baseline: toda confirmação de pagamento baixa"),
        TiposDeEvento.CobrancaRegistrada => DecisaoDeEstado.Para(StatusFatura.Registrada, EfeitoDaTransicao.Nenhum, "baseline: grava 'registrada' sem olhar o status atual"),
        _ => throw new ArgumentOutOfRangeException(nameof(tipoEvento), tipoEvento, "Tipo de evento desconhecido."),
    };

    /// <summary>
    /// Resposta do banco à emissão da cobrança. Só avança de <c>recebida</c>: se a confirmação do banco
    /// chegou antes da resposta (banco lento), o status não volta para <c>cobranca-emitida</c>.
    /// </summary>
    public static DecisaoDeEstado AvaliarEmissao(string statusAtual) => statusAtual == StatusFatura.Recebida
        ? DecisaoDeEstado.Para(StatusFatura.CobrancaEmitida, EfeitoDaTransicao.Nenhum, "cobrança emitida no banco")
        : DecisaoDeEstado.Ignorar(statusAtual, "o banco confirmou antes de responder à emissão: o status não volta");
}
