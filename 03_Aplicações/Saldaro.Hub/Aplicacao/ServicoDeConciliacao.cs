using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Aplicacao;

/// <summary>
/// Conciliação periódica: conta as faturas por status, marca como divergência as que estão em
/// <c>cobranca-emitida</c> há mais de 10 minutos sem confirmação do banco e grava o resumo em <c>conciliacao</c>.
/// </summary>
public sealed class ServicoDeConciliacao(
    IRepositorioFaturas faturas,
    IRepositorioConciliacao conciliacao,
    ITelemetria telemetria,
    IOptions<CenarioOptions> cenario,
    TimeProvider relogio,
    ILogger<ServicoDeConciliacao> logger)
{
    public static readonly TimeSpan PrazoDeConfirmacao = TimeSpan.FromMinutes(10);
    private const int ExemplosNoLog = 20;

    public async Task<ResumoConciliacao> ExecutarAsync(string idExecucao, CancellationToken cancellationToken)
    {
        var inicio = relogio.GetTimestamp();
        var agora = relogio.GetUtcNow();
        var limite = agora - PrazoDeConfirmacao;

        var contagem = StatusFatura.Todos.ToDictionary(status => status, _ => 0);
        var verificadas = 0;
        var divergentes = new List<string>();

        await foreach (var fatura in faturas.ListarResumosAsync(cancellationToken))
        {
            verificadas++;
            contagem[fatura.Status] = contagem.GetValueOrDefault(fatura.Status) + 1;
            if (fatura.Status == StatusFatura.CobrancaEmitida && fatura.AtualizadaEm < limite)
            {
                divergentes.Add(fatura.FaturaId);
            }
        }

        var duracaoMs = (long)relogio.GetElapsedTime(inicio).TotalMilliseconds;
        var resumo = new ResumoConciliacao(idExecucao, agora, contagem, verificadas, divergentes.Count, duracaoMs);

        if (divergentes.Count > 0)
        {
            logger.LogWarning(
                "Conciliação {IdExecucao}: {Divergencias} fatura(s) em cobranca-emitida há mais de {Prazo} min sem confirmação. Exemplos: {Exemplos}",
                idExecucao, divergentes.Count, PrazoDeConfirmacao.TotalMinutes, string.Join(", ", divergentes.Take(ExemplosNoLog)));
        }

        logger.LogInformation(
            "Conciliação {IdExecucao}: {Faturas} fatura(s) verificada(s), {Divergencias} divergência(s), {DuracaoMs} ms.",
            idExecucao, verificadas, divergentes.Count, duracaoMs);

        // Se a gravação falhar, a exceção sobe daqui e o heartbeat abaixo não acontece.
        await conciliacao.GravarAsync(resumo, cancellationToken);

        // TOGGLE Cenario:Heartbeat (cenário 3.6). É a última coisa da execução.
        if (cenario.Value.Heartbeat)
        {
            // CONCEITO: heartbeat ao concluir, nunca no início. Ele é a prova de que o ciclo inteiro rodou e foi
            // gravado: se o timer parar, ou se a execução falhar no meio (exceção antes desta linha), o heartbeat
            // some e o alerta por ausência de sinal dispara. Um heartbeat emitido ao começar ficaria verde
            // mesmo com a conciliação quebrando todo dia.
            telemetria.ConciliacaoHeartbeat(verificadas, divergentes.Count, duracaoMs);
        }

        return resumo;
    }
}
