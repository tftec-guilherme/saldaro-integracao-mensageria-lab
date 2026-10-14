using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Saldaro.Ferramentas.Tests;

public class CorrecaoDeValorTests
{
    // Evento da seção 7.1 do contrato, como o banco passa a mandar com valorComVirgula=true.
    private const string EventoComValorEmTexto = """
        {
          "idEvento": "3f2b8c1e-9d4a-4c7e-8a1b-2f6d5e4c3b2a",
          "tipo": "cobranca.paga",
          "faturaId": "9a7e1c52-0b3d-4f8e-a6c1-7d2e4b9f0a13",
          "idCobranca": "COB-1a2b3c4d",
          "valor": "1.250,00",
          "ocorridoEm": "2026-11-10T09:30:00Z",
          "simulaTransiente": false,
          "transienteAte": null
        }
        """;

    [Theory]
    [InlineData("1.250,00", "1250.00")]
    [InlineData("1250,00", "1250.00")]
    [InlineData("1.250", "1250")]
    [InlineData("R$ 1.250,00", "1250.00")]
    [InlineData("R$1.250,00", "1250.00")]
    [InlineData("0,99", "0.99")]
    [InlineData("12.345.678,90", "12345678.90")]
    [InlineData(" 1.250,5 ", "1250.5")]
    [InlineData("-1.250,00", "-1250.00")]
    [InlineData("1250", "1250")]
    public void Converte_texto_pt_br_para_decimal(string texto, string esperado)
    {
        Assert.True(CorrecaoDeValor.TentarConverterTextoPtBr(texto, out var valor));

        Assert.Equal(decimal.Parse(esperado, CultureInfo.InvariantCulture), valor);
        // Mantém as casas decimais do original (1.250,00 vira 1250.00, não 1250).
        Assert.Equal(esperado, valor.ToString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("1250.00")] // formato americano: lido como pt-BR viraria 125000
    [InlineData("12.50")]
    [InlineData("1.25,00")]
    [InlineData("1,2,3")]
    [InlineData("1.250,")]
    [InlineData("abc")]
    [InlineData("R$")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Recusa_texto_fora_do_formato_pt_br(string? texto)
    {
        Assert.False(CorrecaoDeValor.TentarConverterTextoPtBr(texto, out var valor));
        Assert.Equal(0m, valor);
    }

    [Fact]
    public void Corrige_o_valor_do_evento_do_banco_e_preserva_os_demais_campos()
    {
        var resultado = CorrecaoDeValor.Corrigir(BinaryData.FromString(EventoComValorEmTexto));

        Assert.Equal(SituacaoDoValor.TextoPtBr, resultado.Situacao);
        Assert.True(resultado.Corrigiu);
        Assert.True(resultado.PodeReprocessar);
        Assert.Equal("1.250,00", resultado.ValorOriginal);
        Assert.Equal(1250.00m, resultado.ValorCorrigido);

        using var documento = JsonDocument.Parse(resultado.Corpo.ToString());
        var raiz = documento.RootElement;
        var valor = raiz.GetProperty("valor");
        Assert.Equal(JsonValueKind.Number, valor.ValueKind);
        Assert.Equal(1250.00m, valor.GetDecimal());
        Assert.Equal("1250.00", valor.GetRawText());

        Assert.Equal("3f2b8c1e-9d4a-4c7e-8a1b-2f6d5e4c3b2a", raiz.GetProperty("idEvento").GetString());
        Assert.Equal("cobranca.paga", raiz.GetProperty("tipo").GetString());
        Assert.Equal("9a7e1c52-0b3d-4f8e-a6c1-7d2e4b9f0a13", raiz.GetProperty("faturaId").GetString());
        Assert.Equal("COB-1a2b3c4d", raiz.GetProperty("idCobranca").GetString());
        Assert.Equal("2026-11-10T09:30:00Z", raiz.GetProperty("ocorridoEm").GetString());
        Assert.False(raiz.GetProperty("simulaTransiente").GetBoolean());
        Assert.Equal(JsonValueKind.Null, raiz.GetProperty("transienteAte").ValueKind);
        Assert.Equal(8, raiz.EnumerateObject().Count());
    }

    [Fact]
    public void Valor_numerico_nao_muda_o_corpo()
    {
        var corpo = BinaryData.FromString("""{"idEvento":"e1","valor":1250.0}""");

        var resultado = CorrecaoDeValor.Corrigir(corpo);

        Assert.Equal(SituacaoDoValor.Numerico, resultado.Situacao);
        Assert.True(resultado.PodeReprocessar);
        Assert.False(resultado.Corrigiu);
        Assert.Same(corpo, resultado.Corpo);
        Assert.Equal("1250.0", resultado.ValorOriginal);
    }

    [Theory]
    [InlineData("""{"idEvento":"e1"}""")]
    [InlineData("""{"idEvento":"e1","valor":null}""")]
    [InlineData("""{"specversion":"1.0","type":"Saldaro.Faturas.FaturaBaixada","data":{"valor":1250.0}}""")]
    public void Sem_valor_no_topo_reprocessa_como_esta(string json)
    {
        var corpo = BinaryData.FromString(json);

        var resultado = CorrecaoDeValor.Corrigir(corpo);

        Assert.Equal(SituacaoDoValor.Ausente, resultado.Situacao);
        Assert.True(resultado.PodeReprocessar);
        Assert.Same(corpo, resultado.Corpo);
    }

    [Theory]
    [InlineData("""{"valor":"abc"}""", nameof(SituacaoDoValor.TextoInvalido))]
    [InlineData("""{"valor":"1250.00"}""", nameof(SituacaoDoValor.TextoInvalido))]
    [InlineData("""{"valor":true}""", nameof(SituacaoDoValor.TipoInvalido))]
    [InlineData("""{"valor":{"reais":1250}}""", nameof(SituacaoDoValor.TipoInvalido))]
    [InlineData("isto não é JSON", nameof(SituacaoDoValor.CorpoInvalido))]
    [InlineData("[1, 2, 3]", nameof(SituacaoDoValor.CorpoInvalido))]
    [InlineData("", nameof(SituacaoDoValor.CorpoInvalido))]
    public void O_que_nao_da_para_corrigir_fica_na_DLQ(string json, string esperada)
    {
        var resultado = CorrecaoDeValor.Corrigir(BinaryData.FromString(json));

        Assert.Equal(Enum.Parse<SituacaoDoValor>(esperada), resultado.Situacao);
        Assert.False(resultado.PodeReprocessar);
    }

    [Fact]
    public void Aceita_corpo_com_BOM_e_preserva_acentos()
    {
        var bytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes("""{"descricao":"cobrança pagadora","valor":"1.250,00"}"""))
            .ToArray();

        var resultado = CorrecaoDeValor.Corrigir(BinaryData.FromBytes(bytes));

        Assert.Equal(SituacaoDoValor.TextoPtBr, resultado.Situacao);
        Assert.Equal("""{"descricao":"cobrança pagadora","valor":1250.00}""", resultado.Corpo.ToString());
    }
}
