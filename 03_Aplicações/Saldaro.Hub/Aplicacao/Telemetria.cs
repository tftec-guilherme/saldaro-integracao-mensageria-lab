using System.Globalization;

namespace Saldaro.Hub.Aplicacao;

/// <summary>Nomes dos eventos customizados do hub (seção 10). Os KQL de <c>kql/</c> filtram por eles.</summary>
public static class EventosDeTelemetria
{
    public const string FaturaRecebida = "fatura.recebida";
    public const string CobrancaEmitida = "cobranca.emitida";
    public const string ConfirmacaoRecebida = "confirmacao.recebida";
    public const string ConfirmacaoDuplicadaIgnorada = "confirmacao.duplicada.ignorada";
    public const string FaturaBaixada = "fatura.baixada";
    public const string MensagemDeadLetter = "mensagem.deadletter";
    public const string MensagemRetryAgendado = "mensagem.retry.agendado";
    public const string NotificacaoEnviada = "notificacao.enviada";
    public const string ConciliacaoHeartbeat = "conciliacao.heartbeat";

    /// <summary>
    /// Incluído na seção 10 pela Atualização 2 do contrato (A2.9): o mesmo <c>FaturaBaixada</c> entregue de novo
    /// pelo Event Grid, ignorado pela <c>NotificarParceiro</c> com <c>Cenario:Inbox = true</c>.
    /// Propriedades: <c>faturaId</c>, <c>idEvento</c>.
    /// </summary>
    public const string NotificacaoDuplicadaIgnorada = "notificacao.duplicada.ignorada";
}

/// <summary>
/// Um método por evento, com as propriedades exatas do contrato (viram <c>customDimensions</c> no KQL).
/// </summary>
public static class TelemetriaDoHub
{
    // referenciaErp (incluída na seção 10 pela Atualização 2 do contrato, A2.9): a prova do Incidente 1 agrupa
    // por ela ("a mesma referenciaErp virou N faturas/cobranças no baseline e 1 depois do conserto").
    public static void FaturaRecebida(this ITelemetria telemetria, string faturaId, string clienteId, string modo, string? referenciaErp) =>
        telemetria.Registrar(EventosDeTelemetria.FaturaRecebida, new Dictionary<string, string?>
        {
            ["faturaId"] = faturaId,
            ["clienteId"] = clienteId,
            ["modo"] = modo,
            ["referenciaErp"] = referenciaErp,
        });

    public static void CobrancaEmitida(this ITelemetria telemetria, string faturaId, string idCobranca, string? referenciaErp) =>
        telemetria.Registrar(EventosDeTelemetria.CobrancaEmitida, new Dictionary<string, string?>
        {
            ["faturaId"] = faturaId,
            ["idCobranca"] = idCobranca,
            ["referenciaErp"] = referenciaErp,
        });

    public static void ConfirmacaoRecebida(this ITelemetria telemetria, string faturaId, string idEvento, string tipo) =>
        telemetria.Registrar(EventosDeTelemetria.ConfirmacaoRecebida, new Dictionary<string, string?>
        {
            ["faturaId"] = faturaId,
            ["idEvento"] = idEvento,
            ["tipo"] = tipo,
        });

    public static void ConfirmacaoDuplicadaIgnorada(this ITelemetria telemetria, string faturaId, string idEvento) =>
        telemetria.Registrar(EventosDeTelemetria.ConfirmacaoDuplicadaIgnorada, new Dictionary<string, string?>
        {
            ["faturaId"] = faturaId,
            ["idEvento"] = idEvento,
        });

    public static void FaturaBaixada(this ITelemetria telemetria, string faturaId, string idEvento, string clienteId) =>
        telemetria.Registrar(EventosDeTelemetria.FaturaBaixada, new Dictionary<string, string?>
        {
            ["faturaId"] = faturaId,
            ["idEvento"] = idEvento,
            ["clienteId"] = clienteId,
        });

    public static void MensagemDeadLetter(this ITelemetria telemetria, string? faturaId, string? idEvento, string motivo) =>
        telemetria.Registrar(EventosDeTelemetria.MensagemDeadLetter, new Dictionary<string, string?>
        {
            ["faturaId"] = faturaId,
            ["idEvento"] = idEvento,
            ["motivo"] = motivo,
        });

    public static void MensagemRetryAgendado(this ITelemetria telemetria, string faturaId, string idEvento, int tentativa, int atrasoSegundos) =>
        telemetria.Registrar(EventosDeTelemetria.MensagemRetryAgendado, new Dictionary<string, string?>
        {
            ["faturaId"] = faturaId,
            ["idEvento"] = idEvento,
            ["tentativa"] = tentativa.ToString(CultureInfo.InvariantCulture),
            ["atrasoSegundos"] = atrasoSegundos.ToString(CultureInfo.InvariantCulture),
        });

    public static void NotificacaoEnviada(this ITelemetria telemetria, string faturaId) =>
        telemetria.Registrar(EventosDeTelemetria.NotificacaoEnviada, new Dictionary<string, string?>
        {
            ["faturaId"] = faturaId,
        });

    public static void NotificacaoDuplicadaIgnorada(this ITelemetria telemetria, string faturaId, string idEvento) =>
        telemetria.Registrar(EventosDeTelemetria.NotificacaoDuplicadaIgnorada, new Dictionary<string, string?>
        {
            ["faturaId"] = faturaId,
            ["idEvento"] = idEvento,
        });

    public static void ConciliacaoHeartbeat(this ITelemetria telemetria, int faturasVerificadas, int divergencias, long duracaoMs) =>
        telemetria.Registrar(EventosDeTelemetria.ConciliacaoHeartbeat, new Dictionary<string, string?>
        {
            ["faturasVerificadas"] = faturasVerificadas.ToString(CultureInfo.InvariantCulture),
            ["divergencias"] = divergencias.ToString(CultureInfo.InvariantCulture),
            ["duracaoMs"] = duracaoMs.ToString(CultureInfo.InvariantCulture),
        });
}
