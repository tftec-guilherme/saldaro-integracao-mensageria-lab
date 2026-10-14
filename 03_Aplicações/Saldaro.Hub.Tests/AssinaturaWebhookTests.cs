using System.Text;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Tests;

/// <summary>Assinatura do webhook (seção 7.2). O vetor é o mesmo que o banco simulado testa ao assinar.</summary>
public class AssinaturaWebhookTests
{
    // Vetor de teste compartilhado com o Saldaro.BancoSimulado: não altere sem alterar lá também.
    public const string Segredo = "segredo-de-teste-saldaro";
    public const string Timestamp = "1760000000";
    public const string Corpo =
        "{\"idEvento\":\"11111111-1111-1111-1111-111111111111\",\"tipo\":\"cobranca.paga\",\"faturaId\":\"22222222-2222-2222-2222-222222222222\",\"idCobranca\":\"COB-0a1b2c3d\",\"valor\":1250.00,\"ocorridoEm\":\"2026-11-10T09:30:00Z\",\"simulaTransiente\":false,\"transienteAte\":null}";
    public const string AssinaturaEsperada = "sha256=a34d32f6a9230ca8a5e9dfc0bd8241bc95ff7c3aabc38f6efc032dffa40816a9";

    private static readonly DateTimeOffset MomentoDoTimestamp = DateTimeOffset.FromUnixTimeSeconds(1760000000);
    private static byte[] Bytes(string texto) => Encoding.UTF8.GetBytes(texto);

    [Fact]
    public void Calcula_a_assinatura_do_vetor_compartilhado_com_o_banco()
    {
        Assert.Equal(AssinaturaEsperada, AssinaturaWebhook.Calcular(Segredo, Timestamp, Bytes(Corpo)));
    }

    [Fact]
    public void Assinatura_do_vetor_compartilhado_e_valida()
    {
        var resultado = AssinaturaWebhook.Validar(Segredo, Timestamp, AssinaturaEsperada, Bytes(Corpo), MomentoDoTimestamp);

        Assert.Equal(ResultadoDaAssinatura.Valida, resultado);
    }

    [Theory]
    [InlineData(-300)]
    [InlineData(300)]
    public void Assinatura_no_limite_da_janela_de_300_segundos_e_valida(int deslocamentoEmSegundos)
    {
        var agora = MomentoDoTimestamp.AddSeconds(deslocamentoEmSegundos);

        var resultado = AssinaturaWebhook.Validar(Segredo, Timestamp, AssinaturaEsperada, Bytes(Corpo), agora);

        Assert.Equal(ResultadoDaAssinatura.Valida, resultado);
    }

    [Theory]
    [InlineData(301)]
    [InlineData(-301)]
    [InlineData(86_400)]
    public void Timestamp_fora_da_janela_e_recusado_mesmo_com_assinatura_correta(int deslocamentoEmSegundos)
    {
        // Ataque de repetição (replay): a confirmação capturada continua com assinatura correta, mas o timestamp
        // assinado envelheceu.
        var agora = MomentoDoTimestamp.AddSeconds(deslocamentoEmSegundos);

        var resultado = AssinaturaWebhook.Validar(Segredo, Timestamp, AssinaturaEsperada, Bytes(Corpo), agora);

        Assert.Equal(ResultadoDaAssinatura.ForaDaJanela, resultado);
    }

    [Fact]
    public void Corpo_adulterado_invalida_a_assinatura()
    {
        var adulterado = Corpo.Replace("\"valor\":1250.00", "\"valor\":9250.00", StringComparison.Ordinal);

        var resultado = AssinaturaWebhook.Validar(Segredo, Timestamp, AssinaturaEsperada, Bytes(adulterado), MomentoDoTimestamp);

        Assert.Equal(ResultadoDaAssinatura.AssinaturaInvalida, resultado);
    }

    [Fact]
    public void Corpo_reformatado_invalida_a_assinatura()
    {
        // Mesmo JSON, outros bytes: é por isso que a validação usa o corpo bruto, antes de desserializar.
        var reformatado = Corpo.Replace("1250.00", "1250.0", StringComparison.Ordinal);

        var resultado = AssinaturaWebhook.Validar(Segredo, Timestamp, AssinaturaEsperada, Bytes(reformatado), MomentoDoTimestamp);

        Assert.Equal(ResultadoDaAssinatura.AssinaturaInvalida, resultado);
    }

    [Fact]
    public void Hex_corrompido_invalida_a_assinatura()
    {
        // O botão de caos assinatura=invalida do banco corrompe o hex.
        var corrompida = AssinaturaEsperada[..^1] + (AssinaturaEsperada[^1] == '0' ? '1' : '0');

        var resultado = AssinaturaWebhook.Validar(Segredo, Timestamp, corrompida, Bytes(Corpo), MomentoDoTimestamp);

        Assert.Equal(ResultadoDaAssinatura.AssinaturaInvalida, resultado);
    }

    [Fact]
    public void Segredo_diferente_invalida_a_assinatura()
    {
        var resultado = AssinaturaWebhook.Validar("outro-segredo", Timestamp, AssinaturaEsperada, Bytes(Corpo), MomentoDoTimestamp);

        Assert.Equal(ResultadoDaAssinatura.AssinaturaInvalida, resultado);
    }

    [Fact]
    public void Timestamp_trocado_invalida_a_assinatura()
    {
        // O timestamp faz parte do conteúdo assinado: atualizar só o header não "rejuvenesce" a mensagem.
        var resultado = AssinaturaWebhook.Validar(Segredo, "1760000100", AssinaturaEsperada, Bytes(Corpo), MomentoDoTimestamp);

        Assert.Equal(ResultadoDaAssinatura.AssinaturaInvalida, resultado);
    }

    [Theory]
    [InlineData(null, AssinaturaEsperada, ResultadoDaAssinatura.CabecalhosAusentes)]
    [InlineData(Timestamp, null, ResultadoDaAssinatura.CabecalhosAusentes)]
    [InlineData("", "", ResultadoDaAssinatura.CabecalhosAusentes)]
    [InlineData("agora", AssinaturaEsperada, ResultadoDaAssinatura.TimestampInvalido)]
    [InlineData("-1760000000", AssinaturaEsperada, ResultadoDaAssinatura.TimestampInvalido)]
    [InlineData(Timestamp, "a34d32f6a9230ca8a5e9dfc0bd8241bc95ff7c3aabc38f6efc032dffa40816a9", ResultadoDaAssinatura.AssinaturaInvalida)]
    [InlineData(Timestamp, "sha256=nao-e-hex", ResultadoDaAssinatura.AssinaturaInvalida)]
    [InlineData(Timestamp, "segredo-de-teste-saldaro", ResultadoDaAssinatura.AssinaturaInvalida)]
    public void Cabecalhos_ausentes_ou_malformados_sao_recusados(string? timestamp, string? assinatura, ResultadoDaAssinatura esperado)
    {
        var resultado = AssinaturaWebhook.Validar(Segredo, timestamp, assinatura, Bytes(Corpo), MomentoDoTimestamp);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void Sem_segredo_configurado_nada_passa()
    {
        var resultado = AssinaturaWebhook.Validar("", Timestamp, AssinaturaEsperada, Bytes(Corpo), MomentoDoTimestamp);

        Assert.Equal(ResultadoDaAssinatura.SegredoNaoConfigurado, resultado);
    }
}
