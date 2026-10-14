using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Dominio;
using Saldaro.Hub.Functions;

namespace Saldaro.Hub.Tests;

/// <summary>Abandon explícito antes de relançar: reentrega imediata em vez de esperar o lock expirar.</summary>
public class AbandonExplicitoTests
{
    private readonly RepositorioFaturasEmMemoria _faturas = new();
    private readonly BancoEmMemoria _banco = new();

    private static ServiceBusReceivedMessage Mensagem(string corpo = "{}") =>
        ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString(corpo), messageId: "m-1", deliveryCount: 1);

    [Fact]
    public async Task Falha_sem_destino_definido_faz_abandon_e_relanca_a_mesma_excecao()
    {
        var acoes = new AcoesDeMensagemEmMemoria();
        var erro = new InvalidOperationException("falhou");

        var lancada = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AbandonExplicito.ProcessarAsync(Mensagem(), acoes, _ => throw erro, NullLogger.Instance));

        Assert.Same(erro, lancada);
        Assert.Equal(1, acoes.Abandonadas);
    }

    [Fact]
    public async Task Falha_com_destino_ja_definido_nao_faz_abandon()
    {
        var acoes = new AcoesDeMensagemEmMemoria();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AbandonExplicito.ProcessarAsync(Mensagem(), acoes, async rastreadas =>
            {
                await rastreadas.CompleteMessageAsync(Mensagem());
                throw new InvalidOperationException("depois do complete");
            }, NullLogger.Instance));

        Assert.Equal(1, acoes.Completadas);
        Assert.Equal(0, acoes.Abandonadas);
    }

    [Fact]
    public async Task Sucesso_nao_faz_abandon()
    {
        var acoes = new AcoesDeMensagemEmMemoria();

        await AbandonExplicito.ProcessarAsync(Mensagem(), acoes, rastreadas => rastreadas.CompleteMessageAsync(Mensagem()), NullLogger.Instance);

        Assert.Equal(1, acoes.Completadas);
        Assert.Equal(0, acoes.Abandonadas);
    }

    private async Task<(EmitirCobranca Funcao, ServiceBusReceivedMessage Mensagem)> PrepararEmissaoAsync()
    {
        var servico = new ServicoDeFaturas(
            _faturas, new RepositorioIdempotenciaEmMemoria(), new MensageriaEmMemoria(), _banco, new TelemetriaEmMemoria(),
            Configuracoes.Cenario(asyncRequestReply: true), Configuracoes.Hub(), TimeProvider.System,
            NullLogger<ServicoDeFaturas>.Instance);
        var recebida = await servico.ReceberAsync(
            new NovaFatura { Valor = 10m, Vencimento = "2026-11-10", ReferenciaErp = "ERP-0001" }, "cliente-a", null, CancellationToken.None);
        var corpo = BinaryData.FromObjectAsJson(FaturaRecebidaMensagem.De(_faturas.Ler(recebida.FaturaId)!), Json.Opcoes);
        var mensagem = ServiceBusModelFactory.ServiceBusReceivedMessage(body: corpo, messageId: recebida.FaturaId, deliveryCount: 1);
        return (new EmitirCobranca(servico, NullLogger<EmitirCobranca>.Instance), mensagem);
    }

    [Fact]
    public async Task EmitirCobranca_com_503_do_banco_faz_abandon_e_relanca()
    {
        var (funcao, mensagem) = await PrepararEmissaoAsync();
        _banco.Falha = new BancoIndisponivelException("503");
        var acoes = new AcoesDeMensagemEmMemoria();

        await Assert.ThrowsAsync<BancoIndisponivelException>(() => funcao.Run(mensagem, acoes, CancellationToken.None));

        Assert.Equal(1, acoes.Abandonadas);
        Assert.Equal(0, acoes.Completadas);
    }

    [Fact]
    public async Task EmitirCobranca_com_sucesso_completa_a_mensagem()
    {
        var (funcao, mensagem) = await PrepararEmissaoAsync();
        var acoes = new AcoesDeMensagemEmMemoria();

        await funcao.Run(mensagem, acoes, CancellationToken.None);

        Assert.Equal(1, acoes.Completadas);
        Assert.Equal(0, acoes.Abandonadas);
        Assert.Single(_banco.Chamadas);
    }
}
