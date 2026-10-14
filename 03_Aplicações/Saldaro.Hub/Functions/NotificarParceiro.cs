using System.Net.Http.Headers;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Functions;

/// <summary>
/// Worker de <c>sbq-notificacoes-parceiro</c>. O Event Grid entrega aqui o CloudEvent <c>FaturaBaixada</c>;
/// se a fatura tem <c>callbackUrl</c>, o hub repassa o evento ao ERP do cliente. Depois, status <c>notificada</c>.
/// </summary>
public sealed class NotificarParceiro(
    IRepositorioFaturas faturas,
    IHttpClientFactory clientesHttp,
    ITelemetria telemetria,
    IOptions<CenarioOptions> cenario,
    TimeProvider relogio,
    ILogger<NotificarParceiro> logger)
{
    public const string ClienteHttpDoCallback = "callback-parceiro";

    [Function(nameof(NotificarParceiro))]
    public Task Run(
        [ServiceBusTrigger(Filas.NotificacoesParceiro, Connection = Conexoes.ServiceBus, AutoCompleteMessages = false)]
        ServiceBusReceivedMessage mensagem,
        ServiceBusMessageActions acoes,
        CancellationToken cancellationToken) =>
        AbandonExplicito.ProcessarAsync(mensagem, acoes, rastreadas => NotificarAsync(mensagem, rastreadas, cancellationToken), logger);

    private async Task NotificarAsync(ServiceBusReceivedMessage mensagem, ServiceBusMessageActions acoes, CancellationToken cancellationToken)
    {
        var evento = CloudEvent.Parse(mensagem.Body)
            ?? throw new InvalidOperationException($"Mensagem {mensagem.MessageId} sem CloudEvent.");
        var dados = evento.Data?.ToObjectFromJson<FaturaBaixadaDados>(Json.Opcoes)
            ?? throw new InvalidOperationException($"CloudEvent {evento.Id} sem data.");

        var fatura = await faturas.ObterAsync(dados.FaturaId, cancellationToken);
        if (fatura is null)
        {
            logger.LogWarning("FaturaBaixada {IdDoCloudEvent} para a fatura inexistente {FaturaId}: concluída sem notificar.", evento.Id, dados.FaturaId);
            await acoes.CompleteMessageAsync(mensagem, cancellationToken);
            return;
        }

        // TOGGLE Cenario:Inbox (cenário 3.3): o consumidor da notificação também é idempotente.
        // CONCEITO: entrega pelo menos uma vez. O Event Grid pode entregar o mesmo FaturaBaixada mais de uma vez
        // e esta fila não tem detecção de duplicatas. Com a inbox ligada, a máquina de estados só deixa uma baixa
        // por fatura, então "notificada" quer dizer que o FaturaBaixada dessa baixa já foi entregue ao parceiro.
        // No baseline o parceiro é notificado a cada entrega (parte do sintoma do Incidente 2).
        // Este teste poupa o callback repetido no caso comum (entregas em sequência); com entregas simultâneas,
        // quem decide é o teste de novo dentro do laço com ETag, lá embaixo.
        if (cenario.Value.Inbox && fatura.Status == StatusFatura.Notificada)
        {
            telemetria.NotificacaoDuplicadaIgnorada(dados.FaturaId, dados.IdEvento);
            logger.LogInformation(
                "FaturaBaixada {IdDoCloudEvent} (idEvento {IdEvento}) repetido: a fatura {FaturaId} já está notificada; sem callback.",
                evento.Id, dados.IdEvento, dados.FaturaId);
            await acoes.CompleteMessageAsync(mensagem, cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(fatura.CallbackUrl))
        {
            // O corpo do callback é o próprio CloudEvent. Falha (timeout ou status ≠ 2xx) vira exceção, abandon e
            // reentrega; o ERP também deve deduplicar pelo id do CloudEvent.
            using var conteudo = new ByteArrayContent(mensagem.Body.ToArray());
            conteudo.Headers.ContentType = new MediaTypeHeaderValue("application/cloudevents+json");
            using var resposta = await clientesHttp.CreateClient(ClienteHttpDoCallback)
                .PostAsync(fatura.CallbackUrl, conteudo, cancellationToken);
            resposta.EnsureSuccessStatusCode();
        }

        // TOGGLE Cenario:Inbox (cenário 3.3), de novo. Com a inbox, o teste decisivo é este, na versão que vai ser
        // gravada: de duas entregas simultâneas, o ETag só deixa uma gravar "notificada" e emitir notificacao.enviada;
        // a outra relê, vê "notificada" e para. Os dois callbacks já saíram: pelo menos uma vez; o ERP precisa ser
        // idempotente.
        var outraEntregaJaNotificou = false;
        await faturas.AtualizarComConcorrenciaAsync(dados.FaturaId, atual =>
        {
            outraEntregaJaNotificou = cenario.Value.Inbox && atual.Status == StatusFatura.Notificada;
            if (outraEntregaJaNotificou)
            {
                return false;
            }

            atual.Status = StatusFatura.Notificada;
            atual.AtualizadaEm = relogio.GetUtcNow();
            return true;
        }, cancellationToken);

        if (outraEntregaJaNotificou)
        {
            telemetria.NotificacaoDuplicadaIgnorada(dados.FaturaId, dados.IdEvento);
            logger.LogInformation(
                "FaturaBaixada {IdDoCloudEvent} (idEvento {IdEvento}) entregue em paralelo: outra execução já marcou a fatura {FaturaId} como notificada; sem gravar de novo.",
                evento.Id, dados.IdEvento, dados.FaturaId);
            await acoes.CompleteMessageAsync(mensagem, cancellationToken);
            return;
        }

        telemetria.NotificacaoEnviada(dados.FaturaId);
        await acoes.CompleteMessageAsync(mensagem, cancellationToken);
    }
}
