namespace Saldaro.BancoSimulado.Dominio;

/// <summary>Uma entrega de webhook já decidida: qual evento, com que corpo, para onde e com que atraso.</summary>
public sealed record EntregaPlanejada(
    EventoDeCobranca Evento,
    string Corpo,
    TimeSpan Atraso,
    string WebhookUrl,
    bool AssinaturaInvalida)
{
    public MensagemDeWebhook ParaMensagem() => new(Corpo, WebhookUrl, AssinaturaInvalida);
}

/// <summary>
/// Decide os webhooks de uma cobrança a partir dos botões de caos. Função pura: o relógio (via
/// <see cref="Cobranca.CriadaEm"/>), o sorteio da falha transiente e o gerador de ids entram como parâmetros.
/// </summary>
public static class PlanejadorDeWebhooks
{
    public static readonly TimeSpan AtrasoDoPrimeiroEvento = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan AcrescimoAoEmbaralhar = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan JanelaDaFalhaTransiente = TimeSpan.FromSeconds(45);

    /// <summary>
    /// Devolve as entregas em ordem de envio, com os tempos contados a partir da resposta ao hub
    /// (<c>latencia</c> segundos depois do POST). Sem <c>embaralhar</c>: <c>cobranca.registrada</c> 1 s e
    /// <c>cobranca.paga</c> <c>atrasoWebhook</c> s depois da resposta. Com <c>embaralhar</c>: <c>cobranca.paga</c>
    /// 1 s e <c>cobranca.registrada</c> <c>atrasoWebhook + 3</c> s depois da resposta. Cada evento sai <c>duplicar</c> vezes.
    /// </summary>
    /// <param name="pagaComFalhaTransiente">Resultado do sorteio de <c>falhaTransiente</c> para o <c>cobranca.paga</c>.</param>
    /// <param name="novoIdEvento">Gerador de <c>idEvento</c>; os testes passam um gerador fixo.</param>
    public static IReadOnlyList<EntregaPlanejada> Planejar(
        Cobranca cobranca,
        BotoesDeCaos botoes,
        bool pagaComFalhaTransiente,
        Func<Guid>? novoIdEvento = null)
    {
        novoIdEvento ??= Guid.NewGuid;
        var resposta = TimeSpan.FromSeconds(botoes.Latencia);
        var atrasoWebhook = TimeSpan.FromSeconds(botoes.AtrasoWebhook);

        // CONCEITO: a ordem de chegada não é garantida. Com embaralhar, o banco manda "paga" antes
        // de "registrada", como um parceiro real que repete as confirmações em lote depois de um timeout.
        var (depoisDaRespostaRegistrada, depoisDaRespostaPaga) = botoes.Embaralhar
            ? (atrasoWebhook + AcrescimoAoEmbaralhar, AtrasoDoPrimeiroEvento)
            : (AtrasoDoPrimeiroEvento, atrasoWebhook);

        // Os webhooks entram na fila antes da espera de latência, mas só ficam visíveis depois da resposta:
        // o hub recebe o idCobranca antes de qualquer webhook, mesmo com o banco lento.
        var atrasoDaRegistrada = resposta + depoisDaRespostaRegistrada;
        var atrasoDaPaga = resposta + depoisDaRespostaPaga;

        // ocorridoEm = instante planejado de envio; a janela da falha transiente conta a partir dele.
        var envioDaRegistrada = TruncarNoSegundo(cobranca.CriadaEm + atrasoDaRegistrada);
        var envioDaPaga = TruncarNoSegundo(cobranca.CriadaEm + atrasoDaPaga);

        var registrada = new EventoDeCobranca(
            novoIdEvento(), TiposDeEvento.CobrancaRegistrada, cobranca.FaturaId, cobranca.IdCobranca,
            cobranca.Valor, envioDaRegistrada, SimulaTransiente: false, TransienteAte: null);

        var paga = new EventoDeCobranca(
            novoIdEvento(), TiposDeEvento.CobrancaPaga, cobranca.FaturaId, cobranca.IdCobranca,
            cobranca.Valor, envioDaPaga, SimulaTransiente: pagaComFalhaTransiente,
            TransienteAte: pagaComFalhaTransiente ? envioDaPaga + JanelaDaFalhaTransiente : null);

        var eventosEmOrdemDeEnvio = new[] { (Evento: registrada, Atraso: atrasoDaRegistrada), (Evento: paga, Atraso: atrasoDaPaga) }
            .OrderBy(item => item.Atraso);

        var entregas = new List<EntregaPlanejada>();
        foreach (var (evento, atraso) in eventosEmOrdemDeEnvio)
        {
            var corpo = evento.ParaJson(botoes.ValorComVirgula);

            // CONCEITO: entrega "pelo menos uma vez". As cópias têm o MESMO idEvento e o mesmo corpo:
            // é por esse id que o hub reconhece a duplicata (MessageId no Service Bus e tabela inbox).
            for (var copia = 0; copia < botoes.Duplicar; copia++)
            {
                entregas.Add(new EntregaPlanejada(evento, corpo, atraso, cobranca.WebhookUrl, botoes.AssinaturaInvalida));
            }
        }

        return entregas;
    }

    private static DateTimeOffset TruncarNoSegundo(DateTimeOffset instante) =>
        new(instante.Ticks - (instante.Ticks % TimeSpan.TicksPerSecond), instante.Offset);
}
