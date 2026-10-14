using Microsoft.Azure.Functions.Worker;
using Saldaro.Hub.Aplicacao;

namespace Saldaro.Hub.Functions;

/// <summary>
/// Timer de conciliação, a cada 5 minutos (<c>0 */5 * * * *</c>, em UTC). A lógica, incluindo o heartbeat do
/// toggle <c>Cenario:Heartbeat</c> (último passo, depois de gravar o resumo), está em <see cref="ServicoDeConciliacao"/>.
/// Para o desafio 3.6, o timer é desligado com o app setting <c>AzureWebJobs.Conciliacao.Disabled = true</c>.
/// </summary>
public sealed class Conciliacao(ServicoDeConciliacao servico)
{
    [Function(nameof(Conciliacao))]
    public Task Run(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
        FunctionContext contexto,
        CancellationToken cancellationToken) =>
        servico.ExecutarAsync(contexto.InvocationId, cancellationToken);
}
