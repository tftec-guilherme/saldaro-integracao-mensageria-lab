namespace Saldaro.Ferramentas.Tests;

public class ArgumentosTests
{
    private const string Fqdn = "sb-saldaro-ab12c.servicebus.windows.net";

    // Os nomes de subcomandos e opções vêm da seção 8 do contrato do kit: guias e scripts usam
    // exatamente estes. Se este teste quebrar, alguém mudou o contrato sem querer.
    [Theory]
    [InlineData("triagem", "--namespace --fila --max --reprocessar --descartar-motivo")]
    [InlineData("legado-auto-create", "--namespace --mensagens")]
    [InlineData("rajada", "--namespace --fila --remetentes --mensagens")]
    public void Subcomandos_e_opcoes_seguem_o_contrato(string subcomando, string opcoes)
    {
        var definicao = Argumentos.Encontrar(subcomando);

        Assert.NotNull(definicao);
        Assert.Equal(opcoes.Split(' '), definicao.Opcoes.Select(o => o.Nome));
    }

    [Fact]
    public void Analisa_a_triagem_completa()
    {
        var resultado = Argumentos.Analisar(
            ["triagem", "--namespace", Fqdn, "--fila", "sbq-confirmacoes-banco", "--max", "50", "--reprocessar", "--descartar-motivo", "TentativasEsgotadas"]);

        var argumentos = Assert.IsType<ArgumentosAnalisados>(resultado.Argumentos);
        Assert.Null(resultado.Erro);
        Assert.Equal(Argumentos.Triagem, argumentos.Subcomando);
        Assert.Equal(Fqdn, argumentos.Texto("--namespace"));
        Assert.Equal("sbq-confirmacoes-banco", argumentos.Texto("--fila"));
        Assert.Equal(50, argumentos.Inteiro("--max"));
        Assert.True(argumentos.Ligada("--reprocessar"));
        Assert.Equal("TentativasEsgotadas", argumentos.TextoOpcional("--descartar-motivo"));
    }

    [Fact]
    public void Aplica_os_padroes_do_contrato()
    {
        var triagem = Argumentos.Analisar(["triagem", "--namespace", Fqdn, "--fila", "sbq-confirmacoes-banco"]).Argumentos!;
        Assert.Equal(500, triagem.Inteiro("--max"));
        Assert.False(triagem.Ligada("--reprocessar"));
        Assert.Null(triagem.TextoOpcional("--descartar-motivo"));

        var legado = Argumentos.Analisar(["legado-auto-create", "--namespace", Fqdn]).Argumentos!;
        Assert.Equal(20, legado.Inteiro("--mensagens"));

        var rajada = Argumentos.Analisar(["rajada", "--namespace", Fqdn, "--fila", "sbq-rajada"]).Argumentos!;
        Assert.Equal(50, rajada.Inteiro("--remetentes"));
        Assert.Equal(100, rajada.Inteiro("--mensagens"));
    }

    [Fact]
    public void Aceita_a_forma_com_igual_e_ignora_maiusculas()
    {
        var resultado = Argumentos.Analisar(["TRIAGEM", $"--Namespace={Fqdn}", "--fila=sbq-confirmacoes-banco", "--max=20"]);

        var argumentos = Assert.IsType<ArgumentosAnalisados>(resultado.Argumentos);
        Assert.Equal(Argumentos.Triagem, argumentos.Subcomando);
        Assert.Equal(Fqdn, argumentos.Texto("--namespace"));
        Assert.Equal(20, argumentos.Inteiro("--max"));
    }

    [Theory]
    [InlineData("--fila sbq-confirmacoes-banco", "--namespace")]
    [InlineData("--namespace x --fila", "--fila")]
    [InlineData("--namespace x --fila f --max 0", "--max")]
    [InlineData("--namespace x --fila f --max 5001", "--max")]
    [InlineData("--namespace x --fila f --max abc", "--max")]
    [InlineData("--namespace x --fila f --reprocessar=sim", "--reprocessar")]
    [InlineData("--namespace x --fila f --fila g", "--fila")]
    [InlineData("--namespace x --fila f --tudo", "--tudo")]
    [InlineData("--namespace x --fila f solto", "solto")]
    [InlineData("--namespace --fila f", "--namespace")]
    public void Recusa_argumentos_invalidos_com_mensagem_clara(string resto, string citado)
    {
        var resultado = Argumentos.Analisar(["triagem", .. resto.Split(' ')]);

        Assert.Null(resultado.Argumentos);
        Assert.False(resultado.PedeAjuda);
        Assert.Contains(citado, resultado.Erro);
        Assert.Equal(Argumentos.Triagem, resultado.SubcomandoDaAjuda);
    }

    [Fact]
    public void Recusa_subcomando_desconhecido()
    {
        var resultado = Argumentos.Analisar(["reprocessar-tudo"]);

        Assert.Null(resultado.Argumentos);
        Assert.Contains("reprocessar-tudo", resultado.Erro);
        Assert.Null(resultado.SubcomandoDaAjuda);
    }

    [Theory]
    [InlineData(new string[0], null)]
    [InlineData(new[] { "--ajuda" }, null)]
    [InlineData(new[] { "-h" }, null)]
    [InlineData(new[] { "triagem", "--ajuda" }, "triagem")]
    [InlineData(new[] { "rajada", "--namespace", "x", "--help" }, "rajada")]
    public void Reconhece_pedido_de_ajuda(string[] args, string? subcomando)
    {
        var resultado = Argumentos.Analisar(args);

        Assert.True(resultado.PedeAjuda);
        Assert.Equal(subcomando, resultado.SubcomandoDaAjuda);
    }

    [Fact]
    public void A_ajuda_mostra_a_sintaxe_de_cada_subcomando()
    {
        Assert.Contains("triagem --namespace <fqdn> --fila <nome> [--max <n>] [--reprocessar] [--descartar-motivo <motivo>]", Ajuda.Texto("triagem"));
        Assert.Contains("legado-auto-create --namespace <fqdn> [--mensagens <n>]", Ajuda.Texto("legado-auto-create"));
        Assert.Contains("rajada --namespace <fqdn> --fila <nome> [--remetentes <n>] [--mensagens <n>]", Ajuda.Texto("rajada"));
        Assert.Contains("legado-auto-create", Ajuda.Texto());
    }
}
