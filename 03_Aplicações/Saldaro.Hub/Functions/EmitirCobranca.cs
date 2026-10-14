using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Functions;

/// <summary>
/// Worker da fila <c>sbq-faturas-recebidas</c> (modo assíncrono do cenário 3.2): chama o banco no ritmo dele,
/// grava o <c>idCobranca</c> e o status <c>cobranca-emitida</c>.
/// </summary>
public sealed class EmitirCobranca(ServicoDeFaturas servico, ILogger<EmitirCobranca> logger)
{
    [Function(nameof(EmitirCobranca))]
    public Task Run(
        [ServiceBusTrigger(Filas.FaturasRecebidas, Connection = Conexoes.ServiceBus, AutoCompleteMessages = false)]
        ServiceBusReceivedMessage mensagem,
        ServiceBusMessageActions acoes,
        CancellationToken cancellationToken) =>
        // Um 503 do banco (botão falhaTransiente) sobe como BancoIndisponivelException: abandon explícito e
        // exceção relançada, e o Service Bus reentrega na hora (até o maxDeliveryCount da fila).
        AbandonExplicito.ProcessarAsync(mensagem, acoes, async rastreadas =>
        {
            var dados = JsonSerializer.Deserialize<FaturaRecebidaMensagem>(mensagem.Body.ToMemory().Span, Json.Opcoes)
                ?? throw new InvalidOperationException($"Mensagem {mensagem.MessageId} sem corpo.");

            await servico.EmitirCobrancaAsync(dados, cancellationToken);

            // CONCEITO: peek-lock. Completar é o "pode apagar" para o Service Bus; só acontece depois que a cobrança
            // está gravada. Se o processo cair antes desta linha, o lock expira e outra execução refaz o trabalho.
            await rastreadas.CompleteMessageAsync(mensagem, cancellationToken);
        }, logger);
}
