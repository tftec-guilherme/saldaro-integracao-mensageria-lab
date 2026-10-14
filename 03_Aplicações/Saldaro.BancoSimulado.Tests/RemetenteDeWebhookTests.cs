using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Saldaro.BancoSimulado.Dominio;
using Saldaro.BancoSimulado.Servicos;

namespace Saldaro.BancoSimulado.Tests;

public class RemetenteDeWebhookTests
{
    private const string Url = "https://apim-saldaro-exemplo.invalid/saldaro/webhooks/banco";

    // 1760000000 em Unix seconds: o relógio do teste para no timestamp do vetor compartilhado.
    private static readonly RelogioFixo RelogioDoVetor = new(DateTimeOffset.FromUnixTimeSeconds(VetorCompartilhado.Timestamp));

    private static RemetenteDeWebhook CriarRemetente(HandlerDeCaptura handler, string segredo = VetorCompartilhado.Segredo) =>
        new(new HttpClient(handler), Options.Create(new ConfiguracaoDoBanco { WebhookSecret = segredo }), RelogioDoVetor);

    [Fact]
    public async Task Enviar_faz_POST_com_o_corpo_bruto_e_os_headers_do_vetor()
    {
        var handler = new HandlerDeCaptura(HttpStatusCode.Accepted);

        var resultado = await CriarRemetente(handler).EnviarAsync(new MensagemDeWebhook(VetorCompartilhado.Corpo, Url, false), CancellationToken.None);

        Assert.Equal(new ResultadoDoEnvio(202, ResultadoDaEntrega.Entregue), resultado);
        var requisicao = Assert.Single(handler.Requisicoes);
        Assert.Equal(HttpMethod.Post, requisicao.Metodo);
        Assert.Equal(new Uri(Url), requisicao.Uri);
        Assert.Equal(VetorCompartilhado.Corpo, Encoding.UTF8.GetString(requisicao.Corpo));
        Assert.Equal("application/json; charset=utf-8", requisicao.ContentType);
        Assert.Equal("1760000000", requisicao.Header(AssinaturaDeWebhook.HeaderTimestamp));
        Assert.Equal(VetorCompartilhado.AssinaturaEsperada, requisicao.Header(AssinaturaDeWebhook.HeaderAssinatura));
    }

    [Fact]
    public async Task Enviar_com_assinatura_invalida_manda_o_hex_corrompido()
    {
        var handler = new HandlerDeCaptura(HttpStatusCode.Unauthorized);

        var resultado = await CriarRemetente(handler).EnviarAsync(new MensagemDeWebhook(VetorCompartilhado.Corpo, Url, true), CancellationToken.None);

        Assert.Equal(new ResultadoDoEnvio(401, ResultadoDaEntrega.Rejeitada), resultado);
        var enviada = Assert.Single(handler.Requisicoes).Header(AssinaturaDeWebhook.HeaderAssinatura);
        Assert.NotEqual(VetorCompartilhado.AssinaturaEsperada, enviada);
        Assert.Equal(AssinaturaDeWebhook.Corromper(VetorCompartilhado.AssinaturaEsperada), enviada);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, ResultadoDaEntrega.Entregue)]
    [InlineData(HttpStatusCode.BadRequest, ResultadoDaEntrega.Rejeitada)]
    [InlineData(HttpStatusCode.TooManyRequests, ResultadoDaEntrega.FalhaTransiente)]
    [InlineData(HttpStatusCode.InternalServerError, ResultadoDaEntrega.FalhaTransiente)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ResultadoDaEntrega.FalhaTransiente)]
    public async Task Enviar_classifica_a_resposta_do_hub(HttpStatusCode status, ResultadoDaEntrega esperado)
    {
        var resultado = await CriarRemetente(new HandlerDeCaptura(status))
            .EnviarAsync(new MensagemDeWebhook(VetorCompartilhado.Corpo, Url, false), CancellationToken.None);

        Assert.Equal(esperado, resultado.Resultado);
    }

    [Fact]
    public async Task Enviar_sem_WebhookSecret_falha_sem_chamar_o_hub()
    {
        var handler = new HandlerDeCaptura(HttpStatusCode.Accepted);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CriarRemetente(handler, segredo: "").EnviarAsync(new MensagemDeWebhook(VetorCompartilhado.Corpo, Url, false), CancellationToken.None));
        Assert.Empty(handler.Requisicoes);
    }
}
