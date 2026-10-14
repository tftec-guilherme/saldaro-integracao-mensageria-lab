using Saldaro.BancoSimulado.Dominio;
using Saldaro.Testes;

namespace Saldaro.BancoSimulado.Tests;

public class PlanejadorDeWebhooksTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 9, 30, 0, 400, TimeSpan.Zero);

    private static Cobranca CobrancaDeExemplo() => new(
        FaturaId: "22222222-2222-2222-2222-222222222222",
        IdCobranca: "COB-0a1b2c3d",
        ClienteId: "cliente-a",
        Valor: 1250.00m,
        Vencimento: "2026-11-20",
        PagadorCnpj: CnpjDeTeste.Gerar(),
        WebhookUrl: "https://apim-saldaro-exemplo.invalid/saldaro/webhooks/banco",
        LinhaDigitavel: "linha",
        QrCode: "qr",
        Caos: "",
        CriadaEm: Agora);

    private static IReadOnlyList<EntregaPlanejada> Planejar(BotoesDeCaos botoes, bool pagaComFalhaTransiente = false) =>
        PlanejadorDeWebhooks.Planejar(CobrancaDeExemplo(), botoes, pagaComFalhaTransiente, new GeradorDeIdsSequencial().Proximo);

    [Fact]
    public void Planejar_no_padrao_manda_registrada_em_1_s_e_paga_em_5_s()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao);

        Assert.Collection(entregas,
            registrada =>
            {
                Assert.Equal(TiposDeEvento.CobrancaRegistrada, registrada.Evento.Tipo);
                Assert.Equal(TimeSpan.FromSeconds(1), registrada.Atraso);
                Assert.Equal(new DateTimeOffset(2026, 11, 10, 9, 30, 1, TimeSpan.Zero), registrada.Evento.OcorridoEm);
            },
            paga =>
            {
                Assert.Equal(TiposDeEvento.CobrancaPaga, paga.Evento.Tipo);
                Assert.Equal(TimeSpan.FromSeconds(5), paga.Atraso);
                Assert.Equal(new DateTimeOffset(2026, 11, 10, 9, 30, 5, TimeSpan.Zero), paga.Evento.OcorridoEm);
            });
    }

    [Fact]
    public void Planejar_com_atrasoWebhook_manda_o_paga_no_atraso_informado()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao with { AtrasoWebhook = 12 });

        Assert.Equal([TiposDeEvento.CobrancaRegistrada, TiposDeEvento.CobrancaPaga], entregas.Select(e => e.Evento.Tipo));
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(12)], entregas.Select(e => e.Atraso));
    }

    [Theory]
    [InlineData(5, 8)]
    [InlineData(10, 13)]
    public void Planejar_embaralhado_manda_paga_em_1_s_e_registrada_em_atrasoWebhook_mais_3(int atrasoWebhook, int atrasoEsperadoDaRegistrada)
    {
        var entregas = Planejar(BotoesDeCaos.Padrao with { Embaralhar = true, AtrasoWebhook = atrasoWebhook });

        Assert.Equal([TiposDeEvento.CobrancaPaga, TiposDeEvento.CobrancaRegistrada], entregas.Select(e => e.Evento.Tipo));
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(atrasoEsperadoDaRegistrada)], entregas.Select(e => e.Atraso));
    }

    [Fact]
    public void Planejar_com_latencia_conta_os_tempos_a_partir_da_resposta()
    {
        // latencia=45: o banco responde em t+45 s; registrada 1 s depois (46 s) e paga 5 s depois (50 s).
        var entregas = Planejar(BotoesDeCaos.Padrao with { Latencia = 45 });

        Assert.Equal([TiposDeEvento.CobrancaRegistrada, TiposDeEvento.CobrancaPaga], entregas.Select(e => e.Evento.Tipo));
        Assert.Equal([TimeSpan.FromSeconds(46), TimeSpan.FromSeconds(50)], entregas.Select(e => e.Atraso));
        Assert.Equal(new DateTimeOffset(2026, 11, 10, 9, 30, 50, TimeSpan.Zero), entregas[1].Evento.OcorridoEm);
    }

    [Fact]
    public void Planejar_embaralhado_com_latencia_inverte_a_ordem_depois_da_resposta()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao with { Latencia = 10, Embaralhar = true });

        Assert.Equal([TiposDeEvento.CobrancaPaga, TiposDeEvento.CobrancaRegistrada], entregas.Select(e => e.Evento.Tipo));
        Assert.Equal([TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(18)], entregas.Select(e => e.Atraso));
    }

    [Fact]
    public void Planejar_paga_transiente_com_latencia_conta_a_janela_a_partir_do_envio()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao with { Latencia = 20 }, pagaComFalhaTransiente: true);

        var paga = entregas.Single(e => e.Evento.Tipo == TiposDeEvento.CobrancaPaga);
        Assert.Equal(new DateTimeOffset(2026, 11, 10, 9, 30, 25, TimeSpan.Zero), paga.Evento.OcorridoEm);
        Assert.Equal(new DateTimeOffset(2026, 11, 10, 9, 31, 10, TimeSpan.Zero), paga.Evento.TransienteAte);
    }

    [Fact]
    public void Planejar_com_duplicar_3_gera_tres_entregas_por_evento_com_o_mesmo_idEvento_e_corpo()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao with { Duplicar = 3 });

        Assert.Equal(6, entregas.Count);
        var porTipo = entregas.GroupBy(e => e.Evento.Tipo).ToDictionary(g => g.Key, g => g.ToList());
        Assert.Equal(3, porTipo[TiposDeEvento.CobrancaRegistrada].Count);
        Assert.Equal(3, porTipo[TiposDeEvento.CobrancaPaga].Count);

        foreach (var copias in porTipo.Values)
        {
            Assert.Single(copias.Select(e => e.Evento.IdEvento).Distinct());
            Assert.Single(copias.Select(e => e.Corpo).Distinct());
            Assert.Single(copias.Select(e => e.Atraso).Distinct());
        }

        Assert.NotEqual(porTipo[TiposDeEvento.CobrancaRegistrada][0].Evento.IdEvento, porTipo[TiposDeEvento.CobrancaPaga][0].Evento.IdEvento);
    }

    [Fact]
    public void Planejar_duplicado_e_embaralhado_mantem_as_copias_juntas_na_ordem_invertida()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao with { Duplicar = 2, Embaralhar = true });

        Assert.Equal(
            [TiposDeEvento.CobrancaPaga, TiposDeEvento.CobrancaPaga, TiposDeEvento.CobrancaRegistrada, TiposDeEvento.CobrancaRegistrada],
            entregas.Select(e => e.Evento.Tipo));
    }

    [Fact]
    public void Planejar_usa_os_dados_da_cobranca_em_cada_evento()
    {
        var cobranca = CobrancaDeExemplo();

        var entregas = Planejar(BotoesDeCaos.Padrao);

        Assert.All(entregas, e =>
        {
            Assert.Equal(cobranca.FaturaId, e.Evento.FaturaId);
            Assert.Equal(cobranca.IdCobranca, e.Evento.IdCobranca);
            Assert.Equal(cobranca.Valor, e.Evento.Valor);
            Assert.Equal(cobranca.WebhookUrl, e.WebhookUrl);
            Assert.Equal(e.Evento.ParaJson(), e.Corpo);
        });
    }

    [Fact]
    public void Planejar_com_assinatura_invalida_marca_todas_as_entregas()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao with { AssinaturaInvalida = true, Duplicar = 2 });

        Assert.All(entregas, e => Assert.True(e.AssinaturaInvalida));
        Assert.All(entregas, e => Assert.True(e.ParaMensagem().AssinaturaInvalida));
    }

    [Fact]
    public void Planejar_com_valorComVirgula_manda_o_valor_como_texto_nos_dois_eventos()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao with { ValorComVirgula = true });

        Assert.All(entregas, e => Assert.Contains("\"valor\":\"1.250,00\"", e.Corpo));
    }

    [Fact]
    public void Planejar_sem_valorComVirgula_manda_o_valor_como_numero()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao);

        Assert.All(entregas, e => Assert.Contains("\"valor\":1250.00,", e.Corpo));
    }

    [Fact]
    public void Planejar_paga_com_falha_transiente_marca_so_o_paga_com_janela_de_45_s()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao with { FalhaTransiente = 30 }, pagaComFalhaTransiente: true);

        var registrada = entregas.Single(e => e.Evento.Tipo == TiposDeEvento.CobrancaRegistrada);
        var paga = entregas.Single(e => e.Evento.Tipo == TiposDeEvento.CobrancaPaga);

        Assert.False(registrada.Evento.SimulaTransiente);
        Assert.Null(registrada.Evento.TransienteAte);
        Assert.Contains("\"simulaTransiente\":false,\"transienteAte\":null", registrada.Corpo);

        Assert.True(paga.Evento.SimulaTransiente);
        Assert.Equal(paga.Evento.OcorridoEm + TimeSpan.FromSeconds(45), paga.Evento.TransienteAte);
        Assert.Contains("\"ocorridoEm\":\"2026-11-10T09:30:05Z\",\"simulaTransiente\":true,\"transienteAte\":\"2026-11-10T09:30:50Z\"", paga.Corpo);
    }

    [Fact]
    public void Planejar_sem_falha_transiente_nao_marca_nenhum_evento()
    {
        var entregas = Planejar(BotoesDeCaos.Padrao with { FalhaTransiente = 30 }, pagaComFalhaTransiente: false);

        Assert.All(entregas, e => Assert.False(e.Evento.SimulaTransiente));
        Assert.All(entregas, e => Assert.Null(e.Evento.TransienteAte));
    }

    [Fact]
    public void Planejar_sem_gerador_de_ids_usa_GUIDs_novos_e_diferentes_por_evento()
    {
        var entregas = PlanejadorDeWebhooks.Planejar(CobrancaDeExemplo(), BotoesDeCaos.Padrao, pagaComFalhaTransiente: false);

        Assert.Equal(2, entregas.Select(e => e.Evento.IdEvento).Distinct().Count());
        Assert.DoesNotContain(entregas, e => e.Evento.IdEvento == Guid.Empty);
    }
}
