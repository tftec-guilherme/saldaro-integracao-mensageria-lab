namespace Saldaro.Ferramentas.Tests;

public class IdDeReprocessoTests
{
    private const string IdEvento = "3f2b8c1e-9d4a-4c7e-8a1b-2f6d5e4c3b2a";

    [Fact]
    public void Primeiro_reprocessamento_acrescenta_r1_ao_MessageId_original()
    {
        var id = IdDeReprocesso.Montar(IdEvento, reprocessadoDe: null);

        Assert.Equal($"{IdEvento}-r1", id.MessageId);
        Assert.Equal(IdEvento, id.Original);
        Assert.Equal(1, id.Numero);
    }

    [Theory]
    [InlineData("evt-1-r1", "evt-1", "evt-1-r2", 2)]
    [InlineData("evt-1-r2", "evt-1", "evt-1-r3", 3)]
    [InlineData("evt-1-r9", "evt-1", "evt-1-r10", 10)]
    public void Copia_que_voltou_para_a_DLQ_ganha_o_proximo_numero(
        string messageId, string reprocessadoDe, string esperado, int numero)
    {
        var id = IdDeReprocesso.Montar(messageId, reprocessadoDe);

        Assert.Equal(esperado, id.MessageId);
        Assert.Equal(reprocessadoDe, id.Original);
        Assert.Equal(numero, id.Numero);
    }

    [Fact]
    public void Copia_do_retry_agendado_e_tratada_como_original()
    {
        // O retry agendado do hub usa {idEvento}-t{n}; se ela esgotar e cair na DLQ, é a "original".
        var id = IdDeReprocesso.Montar($"{IdEvento}-t3", reprocessadoDe: null);

        Assert.Equal($"{IdEvento}-t3-r1", id.MessageId);
        Assert.Equal($"{IdEvento}-t3", id.Original);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sem_reprocessadoDe_o_sufixo_do_id_nao_e_interpretado(string? reprocessadoDe)
    {
        var id = IdDeReprocesso.Montar("abc-r1", reprocessadoDe);

        Assert.Equal("abc-r1-r1", id.MessageId);
        Assert.Equal("abc-r1", id.Original);
    }

    [Fact]
    public void ReprocessadoDe_sem_o_padrao_no_id_recomeca_do_1()
    {
        var id = IdDeReprocesso.Montar("outro-id", "evt-1");

        Assert.Equal("evt-1-r1", id.MessageId);
        Assert.Equal("evt-1", id.Original);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Recusa_MessageId_vazio(string messageId)
    {
        Assert.ThrowsAny<ArgumentException>(() => IdDeReprocesso.Montar(messageId, null));
    }

    [Fact]
    public void Recusa_MessageId_que_passaria_do_limite_do_Service_Bus()
    {
        var longo = new string('a', IdDeReprocesso.TamanhoMaximo - 2);

        Assert.Throws<ArgumentException>(() => IdDeReprocesso.Montar(longo, null));
    }

    [Fact]
    public void MessageId_no_limite_e_aceito()
    {
        var limite = new string('a', IdDeReprocesso.TamanhoMaximo - 3);

        var id = IdDeReprocesso.Montar(limite, null);

        Assert.Equal(IdDeReprocesso.TamanhoMaximo, id.MessageId.Length);
    }
}
