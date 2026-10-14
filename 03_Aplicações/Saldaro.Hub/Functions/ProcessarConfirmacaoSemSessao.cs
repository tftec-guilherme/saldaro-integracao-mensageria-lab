using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;

namespace Saldaro.Hub.Functions;

/// <summary>
/// Worker de <c>sbq-confirmacoes-banco</c> numa fila <b>sem</b> sessions (baseline). Ativo quando
/// <c>workerComSessao = false</c>: o Bicep grava <c>AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled = true</c>.
/// A lógica de baixa é a mesma do trigger com sessão: <see cref="ProcessadorDeBaixa"/>.
/// </summary>
/// <remarks>
/// Sem sessions, até <c>maxConcurrentCalls = 4</c> (host.json) mensagens são processadas ao mesmo tempo, inclusive
/// duas da mesma fatura: é o "dois consumidores disputando a mesma fatura" do Incidente 2, sem escalar instâncias.
/// </remarks>
public sealed class ProcessarConfirmacaoSemSessao(ProcessadorDeBaixa processador, ILogger<ProcessarConfirmacaoSemSessao> logger)
{
    [Function(nameof(ProcessarConfirmacaoSemSessao))]
    public Task Run(
        [ServiceBusTrigger(Filas.ConfirmacoesBanco, Connection = Conexoes.ServiceBus, AutoCompleteMessages = false)]
        ServiceBusReceivedMessage mensagem,
        ServiceBusMessageActions acoes,
        CancellationToken cancellationToken)
    {
        LogDeConfirmacao.Registrar(logger, mensagem);
        return processador.ProcessarAsync(mensagem, acoes, cancellationToken);
    }
}
