using System.Globalization;
using Saldaro.BancoSimulado.Dominio;

namespace Saldaro.BancoSimulado.Tests;

public class EventoDeCobrancaTests
{
    private static EventoDeCobranca EventoDoVetor() => new(
        IdEvento: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Tipo: TiposDeEvento.CobrancaPaga,
        FaturaId: "22222222-2222-2222-2222-222222222222",
        IdCobranca: "COB-0a1b2c3d",
        Valor: 1250m,
        OcorridoEm: new DateTimeOffset(2026, 11, 10, 9, 30, 0, TimeSpan.Zero),
        SimulaTransiente: false,
        TransienteAte: null);

    [Fact]
    public void ParaJson_com_os_dados_do_vetor_gera_exatamente_o_corpo_do_vetor()
    {
        var corpo = EventoDoVetor().ParaJson();

        Assert.Equal(VetorCompartilhado.Corpo, corpo);
    }

    [Fact]
    public void ParaJson_com_os_dados_do_vetor_assina_com_a_assinatura_esperada()
    {
        // Liga as duas pontas: o corpo que o banco monta é o mesmo que o vetor assina.
        var corpo = EventoDoVetor().ParaJson();

        Assert.Equal(VetorCompartilhado.AssinaturaEsperada, AssinaturaDeWebhook.Calcular(VetorCompartilhado.Segredo, VetorCompartilhado.Timestamp, corpo));
    }

    [Fact]
    public void ParaJson_com_valorComVirgula_envia_o_valor_como_texto_brasileiro()
    {
        var corpo = EventoDoVetor().ParaJson(valorComVirgula: true);

        Assert.Contains("\"valor\":\"1.250,00\"", corpo);
        Assert.Equal(VetorCompartilhado.Corpo.Replace("\"valor\":1250.00", "\"valor\":\"1.250,00\""), corpo);
    }

    [Fact]
    public void ParaJson_com_falha_transiente_envia_simulaTransiente_e_transienteAte()
    {
        var evento = EventoDoVetor() with
        {
            SimulaTransiente = true,
            TransienteAte = new DateTimeOffset(2026, 11, 10, 9, 30, 45, TimeSpan.Zero),
        };

        var corpo = evento.ParaJson();

        Assert.EndsWith("\"simulaTransiente\":true,\"transienteAte\":\"2026-11-10T09:30:45Z\"}", corpo);
    }

    [Theory]
    [InlineData("1250", "1.250,00")]
    [InlineData("1250.5", "1.250,50")]
    [InlineData("1234567.89", "1.234.567,89")]
    [InlineData("0.5", "0,50")]
    [InlineData("999.999", "1.000,00")]
    public void FormatarValorPtBr_usa_ponto_no_milhar_e_virgula_nos_centavos(string valor, string esperado)
    {
        Assert.Equal(esperado, EventoDeCobranca.FormatarValorPtBr(decimal.Parse(valor, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("1250", "1250.00")]
    [InlineData("1250.5", "1250.50")]
    [InlineData("0.1", "0.10")]
    [InlineData("1234567.891", "1234567.89")]
    public void FormatarValorNumerico_sempre_tem_duas_casas_e_ponto(string valor, string esperado)
    {
        Assert.Equal(esperado, EventoDeCobranca.FormatarValorNumerico(decimal.Parse(valor, CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void FormatarInstante_converte_para_UTC_sem_fracoes_de_segundo()
    {
        var instanteEmBrasilia = new DateTimeOffset(2026, 11, 10, 6, 30, 0, 123, TimeSpan.FromHours(-3));

        Assert.Equal("2026-11-10T09:30:00Z", EventoDeCobranca.FormatarInstante(instanteEmBrasilia));
    }
}
