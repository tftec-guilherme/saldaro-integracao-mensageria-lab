using Saldaro.BancoSimulado.Dominio;

namespace Saldaro.BancoSimulado.Tests;

public class BotoesDeCaosTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("?")]
    [InlineData("   ")]
    public void Interpretar_sem_query_string_usa_os_padroes_do_contrato(string? queryString)
    {
        var leitura = BotoesDeCaos.Interpretar(queryString);

        Assert.Empty(leitura.Avisos);
        Assert.Equal(0, leitura.Botoes.Latencia);
        Assert.Equal(5, leitura.Botoes.AtrasoWebhook);
        Assert.Equal(1, leitura.Botoes.Duplicar);
        Assert.False(leitura.Botoes.Embaralhar);
        Assert.False(leitura.Botoes.AssinaturaInvalida);
        Assert.False(leitura.Botoes.ValorComVirgula);
        Assert.Equal(0, leitura.Botoes.FalhaTransiente);
    }

    [Fact]
    public void Interpretar_todos_os_botoes_le_cada_valor()
    {
        var leitura = BotoesDeCaos.Interpretar(
            "?latencia=45&atrasoWebhook=10&duplicar=3&embaralhar=true&assinatura=invalida&valorComVirgula=true&falhaTransiente=30");

        Assert.Empty(leitura.Avisos);
        Assert.Equal(new BotoesDeCaos
        {
            Latencia = 45,
            AtrasoWebhook = 10,
            Duplicar = 3,
            Embaralhar = true,
            AssinaturaInvalida = true,
            ValorComVirgula = true,
            FalhaTransiente = 30,
        }, leitura.Botoes);
    }

    [Fact]
    public void Interpretar_aceita_ponto_e_virgula_como_separador()
    {
        // O -Caos do erp-simulado.ps1 usa ";"; o hub converte para "&", mas o banco aceita os dois.
        var leitura = BotoesDeCaos.Interpretar("duplicar=3;embaralhar=true");

        Assert.Empty(leitura.Avisos);
        Assert.Equal(3, leitura.Botoes.Duplicar);
        Assert.True(leitura.Botoes.Embaralhar);
    }

    [Fact]
    public void Interpretar_le_o_nome_sem_diferenciar_maiusculas()
    {
        var leitura = BotoesDeCaos.Interpretar("LATENCIA=10&ValorComVirgula=TRUE&Assinatura=INVALIDA");

        Assert.Empty(leitura.Avisos);
        Assert.Equal(10, leitura.Botoes.Latencia);
        Assert.True(leitura.Botoes.ValorComVirgula);
        Assert.True(leitura.Botoes.AssinaturaInvalida);
    }

    [Theory]
    [InlineData("latencia=500", nameof(BotoesDeCaos.Latencia), 200)]
    [InlineData("latencia=-1", nameof(BotoesDeCaos.Latencia), 0)]
    [InlineData("duplicar=10", nameof(BotoesDeCaos.Duplicar), 5)]
    [InlineData("duplicar=0", nameof(BotoesDeCaos.Duplicar), 1)]
    [InlineData("falhaTransiente=150", nameof(BotoesDeCaos.FalhaTransiente), 100)]
    [InlineData("falhaTransiente=-5", nameof(BotoesDeCaos.FalhaTransiente), 0)]
    [InlineData("atrasoWebhook=1", nameof(BotoesDeCaos.AtrasoWebhook), 2)]
    [InlineData("atrasoWebhook=99999", nameof(BotoesDeCaos.AtrasoWebhook), 3600)]
    public void Interpretar_valor_fora_da_faixa_limita_e_avisa(string queryString, string propriedade, int esperado)
    {
        var leitura = BotoesDeCaos.Interpretar(queryString);

        Assert.Equal(esperado, LerInteiro(leitura.Botoes, propriedade));
        Assert.Single(leitura.Avisos);
    }

    [Theory]
    [InlineData("latencia=abc", nameof(BotoesDeCaos.Latencia), 0)]
    [InlineData("latencia=4.5", nameof(BotoesDeCaos.Latencia), 0)]
    [InlineData("duplicar=", nameof(BotoesDeCaos.Duplicar), 1)]
    [InlineData("atrasoWebhook=muito", nameof(BotoesDeCaos.AtrasoWebhook), 5)]
    [InlineData("falhaTransiente=99999999999", nameof(BotoesDeCaos.FalhaTransiente), 0)]
    public void Interpretar_numero_invalido_volta_ao_padrao_e_avisa(string queryString, string propriedade, int esperado)
    {
        var leitura = BotoesDeCaos.Interpretar(queryString);

        Assert.Equal(esperado, LerInteiro(leitura.Botoes, propriedade));
        Assert.Single(leitura.Avisos);
    }

    [Theory]
    [InlineData("embaralhar", true)]
    [InlineData("embaralhar=", true)]
    [InlineData("embaralhar=1", true)]
    [InlineData("embaralhar=false", false)]
    [InlineData("embaralhar=0", false)]
    public void Interpretar_booleano_aceita_true_false_um_zero_e_presenca(string queryString, bool esperado)
    {
        var leitura = BotoesDeCaos.Interpretar(queryString);

        Assert.Empty(leitura.Avisos);
        Assert.Equal(esperado, leitura.Botoes.Embaralhar);
    }

    [Fact]
    public void Interpretar_booleano_invalido_volta_ao_padrao_e_avisa()
    {
        var leitura = BotoesDeCaos.Interpretar("valorComVirgula=talvez");

        Assert.False(leitura.Botoes.ValorComVirgula);
        Assert.Single(leitura.Avisos);
    }

    [Theory]
    [InlineData("assinatura=valida", false)]
    [InlineData("assinatura=invalida", true)]
    [InlineData("assinatura=inv%C3%A1lida", true)]
    public void Interpretar_assinatura_aceita_valida_e_invalida(string queryString, bool esperadoInvalida)
    {
        var leitura = BotoesDeCaos.Interpretar(queryString);

        Assert.Empty(leitura.Avisos);
        Assert.Equal(esperadoInvalida, leitura.Botoes.AssinaturaInvalida);
    }

    [Fact]
    public void Interpretar_assinatura_desconhecida_fica_valida_e_avisa()
    {
        var leitura = BotoesDeCaos.Interpretar("assinatura=quebrada");

        Assert.False(leitura.Botoes.AssinaturaInvalida);
        Assert.Single(leitura.Avisos);
    }

    [Fact]
    public void Interpretar_parametro_desconhecido_e_ignorado_com_aviso()
    {
        var leitura = BotoesDeCaos.Interpretar("duplicat=3&duplicar=2");

        Assert.Equal(2, leitura.Botoes.Duplicar);
        Assert.Contains(leitura.Avisos, aviso => aviso.Contains("duplicat"));
    }

    [Fact]
    public void Interpretar_chave_de_funcao_nao_gera_aviso()
    {
        var leitura = BotoesDeCaos.Interpretar("code=qualquer&latencia=3");

        Assert.Empty(leitura.Avisos);
        Assert.Equal(3, leitura.Botoes.Latencia);
    }

    [Fact]
    public void Descrever_e_interpretar_de_novo_devolve_os_mesmos_botoes()
    {
        var botoes = new BotoesDeCaos
        {
            Latencia = 60,
            AtrasoWebhook = 7,
            Duplicar = 4,
            Embaralhar = true,
            AssinaturaInvalida = true,
            ValorComVirgula = true,
            FalhaTransiente = 25,
        };

        var leitura = BotoesDeCaos.Interpretar(botoes.Descrever());

        Assert.Empty(leitura.Avisos);
        Assert.Equal(botoes, leitura.Botoes);
    }

    private static int LerInteiro(BotoesDeCaos botoes, string propriedade) => propriedade switch
    {
        nameof(BotoesDeCaos.Latencia) => botoes.Latencia,
        nameof(BotoesDeCaos.AtrasoWebhook) => botoes.AtrasoWebhook,
        nameof(BotoesDeCaos.Duplicar) => botoes.Duplicar,
        nameof(BotoesDeCaos.FalhaTransiente) => botoes.FalhaTransiente,
        _ => throw new ArgumentOutOfRangeException(nameof(propriedade), propriedade, null),
    };
}
