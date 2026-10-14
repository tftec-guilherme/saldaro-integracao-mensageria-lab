using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Tests;

/// <summary>Máquina de estados da fatura (seção 5.3, passo 4).</summary>
public class MaquinaDeEstadosTests
{
    [Fact]
    public void Registrada_e_depois_paga_baixa_a_fatura_com_efeito()
    {
        var registrada = MaquinaDeEstados.AvaliarConfirmacao(StatusFatura.CobrancaEmitida, TiposDeEvento.CobrancaRegistrada);
        var paga = MaquinaDeEstados.AvaliarConfirmacao(registrada.StatusResultante, TiposDeEvento.CobrancaPaga);

        Assert.True(registrada.Transiciona);
        Assert.Equal(StatusFatura.Registrada, registrada.StatusResultante);
        Assert.Equal(EfeitoDaTransicao.Nenhum, registrada.Efeito);

        Assert.True(paga.Transiciona);
        Assert.Equal(StatusFatura.Baixada, paga.StatusResultante);
        Assert.Equal(EfeitoDaTransicao.Baixa, paga.Efeito);
    }

    [Fact]
    public void Paga_e_depois_registrada_nao_desbaixa_a_fatura()
    {
        // Fora de ordem (embaralhar=true): "paga" chegou antes de "registrada".
        var paga = MaquinaDeEstados.AvaliarConfirmacao(StatusFatura.CobrancaEmitida, TiposDeEvento.CobrancaPaga);
        var registradaAtrasada = MaquinaDeEstados.AvaliarConfirmacao(paga.StatusResultante, TiposDeEvento.CobrancaRegistrada);

        Assert.Equal(StatusFatura.Baixada, paga.StatusResultante);
        Assert.False(registradaAtrasada.Transiciona);
        Assert.Equal(StatusFatura.Baixada, registradaAtrasada.StatusResultante);
        Assert.Equal(EfeitoDaTransicao.Nenhum, registradaAtrasada.Efeito);
    }

    [Theory]
    [InlineData(StatusFatura.Baixada)]
    [InlineData(StatusFatura.Notificada)]
    public void Paga_duplicada_nao_baixa_de_novo(string statusAtual)
    {
        var decisao = MaquinaDeEstados.AvaliarConfirmacao(statusAtual, TiposDeEvento.CobrancaPaga);

        Assert.False(decisao.Transiciona);
        Assert.Equal(statusAtual, decisao.StatusResultante);
        Assert.Equal(EfeitoDaTransicao.Nenhum, decisao.Efeito);
    }

    [Theory]
    [InlineData(StatusFatura.Recebida)]
    [InlineData(StatusFatura.CobrancaEmitida)]
    [InlineData(StatusFatura.Registrada)]
    public void Paga_baixa_a_partir_de_qualquer_estado_anterior_a_baixa(string statusAtual)
    {
        // "recebida": o banco lento confirmou antes de responder à emissão; o pagamento não pode ser perdido.
        var decisao = MaquinaDeEstados.AvaliarConfirmacao(statusAtual, TiposDeEvento.CobrancaPaga);

        Assert.True(decisao.Transiciona);
        Assert.Equal(StatusFatura.Baixada, decisao.StatusResultante);
        Assert.Equal(EfeitoDaTransicao.Baixa, decisao.Efeito);
    }

    [Fact]
    public void Registrada_duplicada_e_ignorada()
    {
        var decisao = MaquinaDeEstados.AvaliarConfirmacao(StatusFatura.Registrada, TiposDeEvento.CobrancaRegistrada);

        Assert.False(decisao.Transiciona);
        Assert.Equal(StatusFatura.Registrada, decisao.StatusResultante);
    }

    [Fact]
    public void Baseline_sem_guarda_baixa_toda_vez_e_desbaixa_com_registrada_atrasada()
    {
        // É o sintoma do Incidente 2, de propósito: sem olhar o status atual.
        var paga = MaquinaDeEstados.AvaliarSemGuarda(TiposDeEvento.CobrancaPaga);
        var registrada = MaquinaDeEstados.AvaliarSemGuarda(TiposDeEvento.CobrancaRegistrada);

        Assert.True(paga.Transiciona);
        Assert.Equal(EfeitoDaTransicao.Baixa, paga.Efeito);
        Assert.Equal(StatusFatura.Registrada, registrada.StatusResultante);
    }

    [Theory]
    [InlineData(StatusFatura.Recebida, true, StatusFatura.CobrancaEmitida)]
    [InlineData(StatusFatura.Registrada, false, StatusFatura.Registrada)]
    [InlineData(StatusFatura.Baixada, false, StatusFatura.Baixada)]
    [InlineData(StatusFatura.Notificada, false, StatusFatura.Notificada)]
    public void Emissao_so_avanca_de_recebida(string statusAtual, bool transiciona, string esperado)
    {
        var decisao = MaquinaDeEstados.AvaliarEmissao(statusAtual);

        Assert.Equal(transiciona, decisao.Transiciona);
        Assert.Equal(esperado, decisao.StatusResultante);
    }

    [Fact]
    public void Tipo_desconhecido_e_erro_de_programacao()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MaquinaDeEstados.AvaliarConfirmacao(StatusFatura.Registrada, "cobranca.estornada"));
    }
}
