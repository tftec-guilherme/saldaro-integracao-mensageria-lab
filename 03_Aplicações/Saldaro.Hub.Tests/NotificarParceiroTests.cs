using System.Net;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Dominio;
using Saldaro.Hub.Functions;

namespace Saldaro.Hub.Tests;

/// <summary>NotificarParceiro: callback com o CloudEvent e idempotência com Cenario:Inbox (Atualização 1 do contrato).</summary>
public class NotificarParceiroTests
{
    private const string FaturaId = "22222222-2222-2222-2222-222222222222";
    private const string IdEvento = "11111111-1111-1111-1111-111111111111";
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 9, 31, 0, TimeSpan.Zero);

    private readonly RepositorioFaturasEmMemoria _faturas = new();
    private readonly TelemetriaEmMemoria _telemetria = new();

    private void Semear(string status, string? callbackUrl = "https://erp.exemplo.test/callback") =>
        _faturas.SalvarAsync(new Fatura
        {
            FaturaId = FaturaId,
            ClienteId = "cliente-a",
            Valor = 1250.00m,
            Status = status,
            QuantidadeBaixas = 1,
            BaixadaEm = Agora,
            CallbackUrl = callbackUrl,
            CriadaEm = Agora.AddMinutes(-5),
            AtualizadaEm = Agora,
        }, CancellationToken.None).Wait();

    private NotificarParceiro Funcao(HandlerDeCaptura handler, bool inbox) => new(
        _faturas, new FabricaHttpFixa(handler), _telemetria, Configuracoes.Cenario(inbox: inbox), new RelogioControlado(Agora),
        NullLogger<NotificarParceiro>.Instance);

    private static ServiceBusReceivedMessage MensagemDoEventGrid()
    {
        var evento = FaturaBaixadaEvento.Criar(new FaturaBaixadaDados(FaturaId, "cliente-a", 1250.00m, Agora, IdEvento));
        return ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromObjectAsJson(evento), messageId: evento.Id);
    }

    [Fact]
    public async Task Faz_o_callback_com_o_CloudEvent_e_marca_notificada()
    {
        Semear(StatusFatura.Baixada);
        var handler = new HandlerDeCaptura();
        var acoes = new AcoesDeMensagemEmMemoria();

        await Funcao(handler, inbox: true).Run(MensagemDoEventGrid(), acoes, CancellationToken.None);

        var (uri, contentType) = Assert.Single(handler.Requisicoes);
        Assert.Equal("https://erp.exemplo.test/callback", uri?.ToString());
        Assert.Equal("application/cloudevents+json", contentType);
        Assert.Equal(StatusFatura.Notificada, _faturas.Ler(FaturaId)!.Status);
        Assert.Equal(FaturaId, _telemetria.Unico(EventosDeTelemetria.NotificacaoEnviada)["faturaId"]);
        Assert.Equal(1, acoes.Completadas);
    }

    [Fact]
    public async Task Com_inbox_FaturaBaixada_repetido_e_ignorado_sem_callback()
    {
        Semear(StatusFatura.Notificada);
        var handler = new HandlerDeCaptura();
        var acoes = new AcoesDeMensagemEmMemoria();

        await Funcao(handler, inbox: true).Run(MensagemDoEventGrid(), acoes, CancellationToken.None);

        Assert.Empty(handler.Requisicoes);
        Assert.Equal(1, acoes.Completadas);
        var evento = _telemetria.Unico(EventosDeTelemetria.NotificacaoDuplicadaIgnorada);
        Assert.Equal(FaturaId, evento["faturaId"]);
        Assert.Equal(IdEvento, evento["idEvento"]);
        Assert.Equal(0, _telemetria.Contar(EventosDeTelemetria.NotificacaoEnviada));
    }

    [Fact]
    public async Task Com_inbox_duas_entregas_simultaneas_gravam_e_emitem_notificacao_enviada_uma_vez()
    {
        // O Event Grid entregou o mesmo FaturaBaixada duas vezes, e as duas execuções leram "baixada" antes de
        // qualquer gravação. Os dois callbacks saem (pelo menos uma vez), mas só uma execução grava "notificada".
        Semear(StatusFatura.Baixada);
        var handler = new HandlerDeCaptura();
        var funcao = Funcao(handler, inbox: true);
        var mensagem = MensagemDoEventGrid();
        var acoesDaPrimeira = new AcoesDeMensagemEmMemoria();
        var acoesDaSegunda = new AcoesDeMensagemEmMemoria();
        string? versaoGravadaPelaSegunda = null;
        _faturas.AntesDaProximaGravacao = async () =>
        {
            // A segunda entrega roda inteira entre a leitura e a gravação da primeira.
            await funcao.Run(mensagem, acoesDaSegunda, CancellationToken.None);
            versaoGravadaPelaSegunda = _faturas.Ler(FaturaId)!.ETag;
        };

        await funcao.Run(mensagem, acoesDaPrimeira, CancellationToken.None);

        Assert.Equal(2, handler.Requisicoes.Count);
        Assert.Equal(1, _telemetria.Contar(EventosDeTelemetria.NotificacaoEnviada));
        Assert.Equal(1, _telemetria.Contar(EventosDeTelemetria.NotificacaoDuplicadaIgnorada));
        var fatura = _faturas.Ler(FaturaId)!;
        Assert.Equal(StatusFatura.Notificada, fatura.Status);
        Assert.Equal(versaoGravadaPelaSegunda, fatura.ETag);
        Assert.Equal(1, _faturas.Conflitos);
        Assert.Equal(1, acoesDaPrimeira.Completadas);
        Assert.Equal(1, acoesDaSegunda.Completadas);
    }

    [Fact]
    public async Task Baseline_notifica_de_novo_a_cada_entrega()
    {
        // Parte do sintoma do Incidente 2: o ERP é notificado em dobro.
        Semear(StatusFatura.Notificada);
        var handler = new HandlerDeCaptura();

        await Funcao(handler, inbox: false).Run(MensagemDoEventGrid(), new AcoesDeMensagemEmMemoria(), CancellationToken.None);

        Assert.Single(handler.Requisicoes);
        Assert.Equal(1, _telemetria.Contar(EventosDeTelemetria.NotificacaoEnviada));
    }

    [Fact]
    public async Task Callback_com_erro_lanca_excecao_para_reentregar_sem_marcar_notificada()
    {
        Semear(StatusFatura.Baixada);
        var acoes = new AcoesDeMensagemEmMemoria();

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Funcao(new HandlerDeCaptura(HttpStatusCode.InternalServerError), inbox: true).Run(MensagemDoEventGrid(), acoes, CancellationToken.None));

        Assert.Equal(1, acoes.Abandonadas);
        Assert.Equal(0, acoes.Completadas);
        Assert.Equal(StatusFatura.Baixada, _faturas.Ler(FaturaId)!.Status);
    }

    [Fact]
    public async Task Sem_callbackUrl_so_marca_notificada()
    {
        Semear(StatusFatura.Baixada, callbackUrl: null);
        var handler = new HandlerDeCaptura();

        await Funcao(handler, inbox: true).Run(MensagemDoEventGrid(), new AcoesDeMensagemEmMemoria(), CancellationToken.None);

        Assert.Empty(handler.Requisicoes);
        Assert.Equal(StatusFatura.Notificada, _faturas.Ler(FaturaId)!.Status);
    }
}
