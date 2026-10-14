using Saldaro.BancoSimulado.Dominio;

namespace Saldaro.BancoSimulado.Tests;

public class MensagemDeWebhookTests
{
    private const string Url = "https://apim-saldaro-exemplo.invalid/saldaro/webhooks/banco";

    [Fact]
    public void ParaTexto_gera_o_formato_do_contrato()
    {
        var texto = new MensagemDeWebhook("{\"a\":1.50}", Url, AssinaturaInvalida: false).ParaTexto();

        Assert.Equal($"{{\"evento\":{{\"a\":1.50}},\"webhookUrl\":\"{Url}\",\"assinaturaInvalida\":false}}", texto);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Ida_e_volta_preserva_o_corpo_byte_a_byte(bool valorComVirgula, bool assinaturaInvalida)
    {
        var corpo = valorComVirgula
            ? VetorCompartilhado.Corpo.Replace("\"valor\":1250.00", "\"valor\":\"1.250,00\"")
            : VetorCompartilhado.Corpo;

        var lida = MensagemDeWebhook.Ler(new MensagemDeWebhook(corpo, Url, assinaturaInvalida).ParaTexto());

        Assert.Equal(corpo, lida.Corpo);
        Assert.Equal(Url, lida.WebhookUrl);
        Assert.Equal(assinaturaInvalida, lida.AssinaturaInvalida);
    }

    [Fact]
    public void Ler_sem_assinaturaInvalida_assume_false()
    {
        var lida = MensagemDeWebhook.Ler($"{{\"evento\":{{}},\"webhookUrl\":\"{Url}\"}}");

        Assert.False(lida.AssinaturaInvalida);
    }

    [Theory]
    [InlineData("isto não é JSON")]
    [InlineData("[]")]
    [InlineData("{\"webhookUrl\":\"https://x\"}")]
    [InlineData("{\"evento\":\"texto\",\"webhookUrl\":\"https://x\"}")]
    [InlineData("{\"evento\":{}}")]
    [InlineData("{\"evento\":{},\"webhookUrl\":\"\"}")]
    public void Ler_fora_do_formato_lanca_FormatException(string texto)
    {
        Assert.Throws<FormatException>(() => MensagemDeWebhook.Ler(texto));
    }

    [Fact]
    public void Identificar_devolve_idEvento_tipo_e_fatura()
    {
        var identificacao = new MensagemDeWebhook(VetorCompartilhado.Corpo, Url, false).Identificar();

        Assert.Equal(new IdentificacaoDoEvento("11111111-1111-1111-1111-111111111111", TiposDeEvento.CobrancaPaga, "22222222-2222-2222-2222-222222222222"), identificacao);
    }
}
