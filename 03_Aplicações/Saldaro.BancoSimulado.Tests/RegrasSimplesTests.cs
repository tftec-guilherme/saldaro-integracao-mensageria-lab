using System.Globalization;
using System.Text.RegularExpressions;
using Saldaro.BancoSimulado.Dominio;
using Saldaro.Testes;

namespace Saldaro.BancoSimulado.Tests;

public class SorteioTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(30, 30)]
    [InlineData(100, 100)]
    [InlineData(-5, 0)]
    [InlineData(150, 100)]
    public void Dispara_em_exatamente_N_dos_cem_valores_possiveis(int percentual, int disparosEsperados)
    {
        var disparos = Enumerable.Range(0, 100).Count(valor => Sorteio.Dispara(percentual, valor));

        Assert.Equal(disparosEsperados, disparos);
    }

    [Fact]
    public void SorteioAleatorio_com_0_nunca_dispara_e_com_100_sempre_dispara()
    {
        var sorteio = new SorteioAleatorio();

        Assert.All(Enumerable.Range(0, 200), _ => Assert.False(sorteio.Dispara(0)));
        Assert.All(Enumerable.Range(0, 200), _ => Assert.True(sorteio.Dispara(100)));
    }
}

public class ClassificacaoDaEntregaTests
{
    [Theory]
    [InlineData(200, ResultadoDaEntrega.Entregue)]
    [InlineData(202, ResultadoDaEntrega.Entregue)]
    [InlineData(400, ResultadoDaEntrega.Rejeitada)]
    [InlineData(401, ResultadoDaEntrega.Rejeitada)]
    [InlineData(404, ResultadoDaEntrega.Rejeitada)]
    [InlineData(408, ResultadoDaEntrega.FalhaTransiente)]
    [InlineData(429, ResultadoDaEntrega.FalhaTransiente)]
    [InlineData(500, ResultadoDaEntrega.FalhaTransiente)]
    [InlineData(503, ResultadoDaEntrega.FalhaTransiente)]
    [InlineData(504, ResultadoDaEntrega.FalhaTransiente)]
    public void Classificar_separa_entregue_rejeitada_e_transiente(int statusCode, ResultadoDaEntrega esperado)
    {
        Assert.Equal(esperado, ClassificacaoDaEntrega.Classificar(statusCode));
    }
}

public class ChaveDeApiTests
{
    [Fact]
    public void Confere_com_a_chave_igual_passa() => Assert.True(ChaveDeApi.Confere("chave-de-teste", "chave-de-teste"));

    [Theory]
    [InlineData("chave-errada", "chave-de-teste")]
    [InlineData("CHAVE-DE-TESTE", "chave-de-teste")]
    [InlineData(null, "chave-de-teste")]
    [InlineData("", "chave-de-teste")]
    [InlineData("chave-de-teste", "")]
    [InlineData("chave-de-teste", null)]
    [InlineData(null, null)]
    public void Confere_com_a_chave_diferente_ou_ausente_recusa(string? recebida, string? esperada) =>
        Assert.False(ChaveDeApi.Confere(recebida, esperada));
}

public class PedidoDeCobrancaTests
{
    private static readonly PedidoDeCobranca PedidoValido = new(
        FaturaId: "22222222-2222-2222-2222-222222222222",
        ClienteId: "cliente-a",
        Valor: 1250.00m,
        Vencimento: "2026-11-20",
        PagadorCnpj: CnpjDeTeste.Gerar(),
        WebhookUrl: "https://apim-saldaro-exemplo.invalid/saldaro/webhooks/banco");

    [Fact]
    public void Validar_pedido_completo_nao_tem_erros() => Assert.Empty(PedidoValido.Validar());

    [Fact]
    public void Validar_so_com_os_campos_obrigatorios_nao_tem_erros() =>
        Assert.Empty((PedidoValido with { ClienteId = null, Vencimento = null, PagadorCnpj = null }).Validar());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fatura/1")]
    [InlineData("fatura#1")]
    [InlineData("fatura?1")]
    [InlineData("fatura\\1")]
    public void Validar_faturaId_ausente_ou_invalido_para_a_tabela_tem_erro(string? faturaId) =>
        Assert.Single((PedidoValido with { FaturaId = faturaId }).Validar());

    [Theory]
    [InlineData(128, 0)]
    [InlineData(129, 1)]
    public void Validar_faturaId_aceita_ate_128_caracteres_como_o_hub(int tamanho, int erros) =>
        Assert.Equal(erros, (PedidoValido with { FaturaId = new string('f', tamanho) }).Validar().Count);

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("100000000")]
    public void Validar_valor_ausente_ou_fora_da_faixa_tem_erro(string? valor)
    {
        var pedido = PedidoValido with { Valor = valor is null ? null : decimal.Parse(valor, CultureInfo.InvariantCulture) };

        Assert.Single(pedido.Validar());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/saldaro/webhooks/banco")]
    [InlineData("ftp://exemplo.invalid/webhook")]
    public void Validar_webhookUrl_ausente_ou_nao_http_tem_erro(string? webhookUrl) =>
        Assert.Single((PedidoValido with { WebhookUrl = webhookUrl }).Validar());
}

public class DadosFicticiosTests
{
    [Fact]
    public void NovoIdCobranca_tem_o_prefixo_COB_e_8_hex_minusculos()
    {
        var ids = Enumerable.Range(0, 50).Select(_ => DadosFicticios.NovoIdCobranca()).ToList();

        Assert.All(ids, id => Assert.Matches(new Regex("^COB-[0-9a-f]{8}$"), id));
        Assert.True(ids.Distinct().Count() > 1);
    }

    [Fact]
    public void LinhaDigitavel_tem_o_desenho_de_boleto_com_banco_000_e_digitos_zerados()
    {
        Assert.Equal(
            "00000.00000 00001.000000 00000.000000 0 00000000125000",
            DadosFicticios.LinhaDigitavel("COB-00000001", 1250.00m));
    }

    [Fact]
    public void LinhaDigitavel_tem_47_digitos()
    {
        var linha = DadosFicticios.LinhaDigitavel(DadosFicticios.NovoIdCobranca(), 99_999_999.99m);

        Assert.Matches(new Regex(@"^00000\.\d{5} \d{5}\.000000 00000\.000000 0 0000\d{10}$"), linha);
        Assert.Equal(47, linha.Count(char.IsAsciiDigit));
    }

    [Fact]
    public void QrCode_e_texto_marcado_como_ficticio()
    {
        Assert.Equal("PIX-FICTICIO|SALDARO-LAB|COB-0a1b2c3d|1250.00", DadosFicticios.QrCode("COB-0a1b2c3d", 1250m));
    }
}
