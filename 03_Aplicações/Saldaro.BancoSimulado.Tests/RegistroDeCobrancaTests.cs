using Microsoft.Extensions.Logging.Abstractions;
using Saldaro.BancoSimulado.Dominio;
using Saldaro.BancoSimulado.Servicos;
using Saldaro.Testes;

namespace Saldaro.BancoSimulado.Tests;

public class RegistroDeCobrancaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 9, 30, 0, TimeSpan.Zero);

    private static readonly PedidoDeCobranca Pedido = new(
        FaturaId: "22222222-2222-2222-2222-222222222222",
        ClienteId: "cliente-a",
        Valor: 1250.00m,
        Vencimento: "2026-11-20",
        PagadorCnpj: CnpjDeTeste.Gerar(),
        WebhookUrl: "https://apim-saldaro-exemplo.invalid/saldaro/webhooks/banco");

    private sealed class Cenario
    {
        public RegistroDeEfeitos Registro { get; } = new();
        public RepositorioEmMemoria Repositorio { get; }
        public FilaEmMemoria Fila { get; }
        public SorteioRoteirizado Sorteio { get; }
        public RegistroDeCobranca Servico { get; }

        public Cenario(params bool[] respostasDoSorteio)
        {
            Repositorio = new RepositorioEmMemoria(Registro);
            Fila = new FilaEmMemoria(Registro);
            Sorteio = new SorteioRoteirizado(respostasDoSorteio);
            Servico = new RegistroDeCobranca(
                Repositorio, Fila, new EsperaRegistrada(Registro), Sorteio, new RelogioFixo(Agora),
                NullLogger<RegistroDeCobranca>.Instance);
        }

        public Task<ResultadoDoRegistro> RegistrarAsync(BotoesDeCaos botoes, PedidoDeCobranca? pedido = null) =>
            Servico.RegistrarAsync(pedido ?? Pedido, botoes, CancellationToken.None);
    }

    [Fact]
    public async Task Registrar_grava_enfileira_e_so_depois_espera_a_latencia()
    {
        var cenario = new Cenario();

        var resultado = await cenario.RegistrarAsync(BotoesDeCaos.Padrao with { Latencia = 45 });

        Assert.IsType<ResultadoDoRegistro.Registrada>(resultado);
        // Enfileirados antes da espera, mas visíveis só depois da resposta: 45 + 1 s e 45 + 5 s.
        Assert.Equal(["gravar", "enfileirar:46", "enfileirar:50", "esperar:45"], cenario.Registro.Efeitos);
    }

    [Fact]
    public async Task Registrar_com_duplicar_3_enfileira_seis_mensagens_antes_da_espera()
    {
        var cenario = new Cenario();

        await cenario.RegistrarAsync(BotoesDeCaos.Padrao with { Duplicar = 3, Latencia = 10 });

        Assert.Equal(
            ["gravar", "enfileirar:11", "enfileirar:11", "enfileirar:11", "enfileirar:15", "enfileirar:15", "enfileirar:15", "esperar:10"],
            cenario.Registro.Efeitos);
    }

    [Fact]
    public async Task Registrar_embaralhado_enfileira_o_paga_primeiro()
    {
        var cenario = new Cenario();

        await cenario.RegistrarAsync(BotoesDeCaos.Padrao with { Embaralhar = true });

        Assert.Equal(["gravar", "enfileirar:1", "enfileirar:8", "esperar:0"], cenario.Registro.Efeitos);
        var tipos = cenario.Fila.Mensagens.Select(m => MensagemDeWebhook.Ler(m.Mensagem).Identificar().Tipo);
        Assert.Equal([TiposDeEvento.CobrancaPaga, TiposDeEvento.CobrancaRegistrada], tipos);
    }

    [Fact]
    public async Task Registrar_com_falha_transiente_sorteada_responde_503_sem_nenhum_efeito()
    {
        var cenario = new Cenario(true);

        var resultado = await cenario.RegistrarAsync(BotoesDeCaos.Padrao with { FalhaTransiente = 30, Latencia = 45 });

        var falha = Assert.IsType<ResultadoDoRegistro.FalhaTransienteSimulada>(resultado);
        Assert.Equal(30, falha.Percentual);
        Assert.Empty(cenario.Registro.Efeitos);
        Assert.Equal([30], cenario.Sorteio.PercentuaisConsultados);
    }

    [Fact]
    public async Task Registrar_sorteia_separado_o_503_e_o_paga_transiente()
    {
        // 1º sorteio (503): não dispara; 2º sorteio (cobranca.paga transiente): dispara.
        var cenario = new Cenario(false, true);

        var resultado = await cenario.RegistrarAsync(BotoesDeCaos.Padrao with { FalhaTransiente = 30 });

        var registrada = Assert.IsType<ResultadoDoRegistro.Registrada>(resultado);
        Assert.Equal([30, 30], cenario.Sorteio.PercentuaisConsultados);
        var paga = registrada.Entregas.Single(e => e.Evento.Tipo == TiposDeEvento.CobrancaPaga);
        Assert.True(paga.Evento.SimulaTransiente);
        Assert.Equal(Agora.AddSeconds(5 + 45), paga.Evento.TransienteAte);
    }

    [Fact]
    public async Task Registrar_pedido_invalido_devolve_os_erros_sem_nenhum_efeito()
    {
        var cenario = new Cenario(true);

        var resultado = await cenario.RegistrarAsync(BotoesDeCaos.Padrao, Pedido with { FaturaId = null, Valor = 0m });

        var invalido = Assert.IsType<ResultadoDoRegistro.PedidoInvalido>(resultado);
        Assert.Equal(2, invalido.Erros.Count);
        Assert.Empty(cenario.Registro.Efeitos);
        Assert.Empty(cenario.Sorteio.PercentuaisConsultados);
    }

    [Fact]
    public async Task Registrar_grava_a_cobranca_com_dados_ficticios_e_os_botoes_efetivos()
    {
        var cenario = new Cenario();
        var botoes = BotoesDeCaos.Padrao with { Duplicar = 2, ValorComVirgula = true };

        var resultado = await cenario.RegistrarAsync(botoes);

        var registrada = Assert.IsType<ResultadoDoRegistro.Registrada>(resultado);
        var gravada = Assert.Single(cenario.Repositorio.Cobrancas);
        Assert.Equal(registrada.Cobranca, gravada);
        Assert.Equal(Pedido.FaturaId, gravada.FaturaId);
        Assert.Matches("^COB-[0-9a-f]{8}$", gravada.IdCobranca);
        Assert.Equal(1250.00m, gravada.Valor);
        Assert.Equal(DadosFicticios.LinhaDigitavel(gravada.IdCobranca, 1250.00m), gravada.LinhaDigitavel);
        Assert.Equal(DadosFicticios.QrCode(gravada.IdCobranca, 1250.00m), gravada.QrCode);
        Assert.Equal(botoes.Descrever(), gravada.Caos);
        Assert.Equal(Agora, gravada.CriadaEm);
    }

    [Fact]
    public async Task Registrar_as_mensagens_da_fila_trazem_o_corpo_planejado()
    {
        var cenario = new Cenario();

        var resultado = await cenario.RegistrarAsync(BotoesDeCaos.Padrao with { AssinaturaInvalida = true });

        var registrada = Assert.IsType<ResultadoDoRegistro.Registrada>(resultado);
        var lidas = cenario.Fila.Mensagens.Select(m => MensagemDeWebhook.Ler(m.Mensagem)).ToList();
        Assert.Equal(registrada.Entregas.Select(e => e.Corpo), lidas.Select(m => m.Corpo));
        Assert.All(lidas, m => Assert.Equal(Pedido.WebhookUrl, m.WebhookUrl));
        Assert.All(lidas, m => Assert.True(m.AssinaturaInvalida));
        Assert.All(lidas, m => Assert.Contains($"\"idCobranca\":\"{registrada.Cobranca.IdCobranca}\"", m.Corpo));
    }
}
