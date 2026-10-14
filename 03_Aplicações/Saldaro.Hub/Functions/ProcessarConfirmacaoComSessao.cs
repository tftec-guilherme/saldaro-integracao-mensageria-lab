using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;

namespace Saldaro.Hub.Functions;

/// <summary>
/// Worker de <c>sbq-confirmacoes-banco</c> numa fila <b>com</b> sessions (consertado). Ativo quando
/// <c>workerComSessao = true</c>: o Bicep grava <c>AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled = true</c>.
/// Um receptor sem sessão numa fila com sessão falharia, por isso só um dos dois fica ligado.
/// </summary>
public sealed class ProcessarConfirmacaoComSessao(ProcessadorDeBaixa processador, ILogger<ProcessarConfirmacaoComSessao> logger)
{
    [Function(nameof(ProcessarConfirmacaoComSessao))]
    public Task Run(
        [ServiceBusTrigger(Filas.ConfirmacoesBanco, Connection = Conexoes.ServiceBus, IsSessionsEnabled = true, AutoCompleteMessages = false)]
        ServiceBusReceivedMessage mensagem,
        ServiceBusMessageActions acoes,
        ServiceBusSessionMessageActions acoesDaSessao,
        CancellationToken cancellationToken)
    {
        // CONCEITO: sessions. Este worker detém o lock da sessão (a fatura) inteira, não só da mensagem:
        // enquanto ele processa, nenhuma outra execução recebe mensagens da mesma fatura, e elas saem na ordem
        // de chegada (SequenceNumber crescente). Até maxConcurrentSessions = 8 faturas diferentes andam em paralelo.
        LogDeConfirmacao.Registrar(logger, mensagem, acoesDaSessao.SessionLockedUntil);

        // O settlement (complete, dead-letter, abandon) usa as mesmas ServiceBusMessageActions do trigger sem sessão;
        // as ServiceBusSessionMessageActions servem para o estado e o lock da sessão.
        return processador.ProcessarAsync(mensagem, acoes, cancellationToken);
    }
}
