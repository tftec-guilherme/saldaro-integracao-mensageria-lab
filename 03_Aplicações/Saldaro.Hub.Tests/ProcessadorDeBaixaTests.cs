using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Tests;

/// <summary>Lógica de baixa (seção 5.3) com os dublês em memória: baseline ingênuo × consertos por toggle.</summary>
public class ProcessadorDeBaixaTests
{
    private const string FaturaId = "22222222-2222-2222-2222-222222222222";
    private const string IdEventoPaga = "11111111-1111-1111-1111-111111111111";
    private const string IdEventoRegistrada = "33333333-3333-3333-3333-333333333333";
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 9, 30, 0, TimeSpan.Zero);

    private readonly RepositorioFaturasEmMemoria _faturas = new();
    private readonly RepositorioInboxEmMemoria _inbox = new();
    private readonly MensageriaEmMemoria _mensageria = new();
    private readonly PublicadorEmMemoria _publicador = new();
    private readonly TelemetriaEmMemoria _telemetria = new();
    private readonly RelogioControlado _relogio = new(Agora);

    public ProcessadorDeBaixaTests()
    {
        _faturas.SalvarAsync(new Fatura
        {
            FaturaId = FaturaId,
            ClienteId = "cliente-a",
            Valor = 1250.00m,
            Vencimento = "2026-11-10",
            Status = StatusFatura.CobrancaEmitida,
            IdCobranca = "COB-0a1b2c3d",
            CriadaEm = Agora.AddMinutes(-5),
            AtualizadaEm = Agora.AddMinutes(-5),
        }, CancellationToken.None).Wait();
    }

    private ProcessadorDeBaixa Processador(bool inbox = false, bool dlqComMotivo = false, bool retryAgendado = false) => new(
        _faturas, _inbox, _mensageria, _publicador, _telemetria,
        Configuracoes.Cenario(inbox: inbox, dlqComMotivo: dlqComMotivo, retryAgendado: retryAgendado),
        _relogio, NullLogger<ProcessadorDeBaixa>.Instance);

    private static ServiceBusReceivedMessage Mensagem(string corpo, string? messageId = null, int? tentativa = null, string tipo = TiposDeEvento.CobrancaPaga)
    {
        var propriedades = new Dictionary<string, object> { [PropriedadesDaMensagem.Tipo] = tipo };
        if (tentativa is not null)
        {
            propriedades[PropriedadesDaMensagem.Tentativa] = tentativa.Value;
        }

        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(corpo),
            messageId: messageId ?? IdEventoPaga,
            sessionId: FaturaId,
            contentType: "application/json",
            properties: propriedades,
            deliveryCount: 1,
            sequenceNumber: 1);
    }

    private static string Paga(string valor = "1250.00", bool simulaTransiente = false, DateTimeOffset? transienteAte = null) =>
        ClassificacaoERetryTests.Evento(valor: valor, simulaTransiente: simulaTransiente, transienteAte: transienteAte, idEvento: IdEventoPaga, faturaId: FaturaId);

    private static string Registrada() =>
        ClassificacaoERetryTests.Evento(tipo: TiposDeEvento.CobrancaRegistrada, idEvento: IdEventoRegistrada, faturaId: FaturaId);

    // ---------- Incidente 2: duplicata e desordem ----------

    [Fact]
    public async Task Baseline_cada_confirmacao_duplicada_baixa_e_publica_de_novo()
    {
        var processador = Processador();
        var acoes = new AcoesDeMensagemEmMemoria();

        for (var i = 0; i < 3; i++)
        {
            await processador.ProcessarAsync(Mensagem(Paga()), acoes, CancellationToken.None);
        }

        Assert.Equal(3, _publicador.Publicados.Count);
        Assert.Equal(3, _telemetria.Contar(EventosDeTelemetria.FaturaBaixada));
        Assert.Equal(3, _faturas.Ler(FaturaId)!.QuantidadeBaixas);
        Assert.Equal(3, acoes.Completadas);
    }

    [Fact]
    public async Task Baseline_registrada_atrasada_desbaixa_a_fatura()
    {
        var processador = Processador();
        var acoes = new AcoesDeMensagemEmMemoria();

        await processador.ProcessarAsync(Mensagem(Paga()), acoes, CancellationToken.None);
        await processador.ProcessarAsync(Mensagem(Registrada(), IdEventoRegistrada, tipo: TiposDeEvento.CobrancaRegistrada), acoes, CancellationToken.None);

        Assert.Equal(StatusFatura.Registrada, _faturas.Ler(FaturaId)!.Status);
    }

    [Fact]
    public async Task Baseline_com_tres_consumidores_simultaneos_publica_tres_vezes()
    {
        // maxConcurrentCalls = 4 sem sessions: três cópias da mesma confirmação ao mesmo tempo.
        var processador = Processador();

        await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
            Task.Run(() => processador.ProcessarAsync(Mensagem(Paga()), new AcoesDeMensagemEmMemoria(), CancellationToken.None))));

        Assert.Equal(3, _publicador.Publicados.Count);
    }

    [Fact]
    public async Task Com_inbox_tres_duplicatas_viram_uma_baixa_uma_publicacao_e_uma_linha_na_inbox()
    {
        var processador = Processador(inbox: true);
        var acoes = new AcoesDeMensagemEmMemoria();

        for (var i = 0; i < 3; i++)
        {
            await processador.ProcessarAsync(Mensagem(Paga()), acoes, CancellationToken.None);
        }

        var fatura = _faturas.Ler(FaturaId)!;
        Assert.Equal(StatusFatura.Baixada, fatura.Status);
        Assert.Equal(1, fatura.QuantidadeBaixas);
        Assert.Equal(Agora, fatura.BaixadaEm);
        var publicado = Assert.Single(_publicador.Publicados);
        Assert.Equal(IdEventoPaga, publicado.IdEvento);
        Assert.Equal(1, _telemetria.Contar(EventosDeTelemetria.FaturaBaixada));
        Assert.Equal(2, _telemetria.Contar(EventosDeTelemetria.ConfirmacaoDuplicadaIgnorada));
        Assert.Single(_inbox.Linhas);
        Assert.Equal(3, acoes.Completadas);
    }

    [Fact]
    public async Task Com_inbox_registrada_atrasada_nao_desbaixa_e_fica_registrada_na_inbox()
    {
        var processador = Processador(inbox: true);
        var acoes = new AcoesDeMensagemEmMemoria();

        await processador.ProcessarAsync(Mensagem(Paga()), acoes, CancellationToken.None);
        await processador.ProcessarAsync(Mensagem(Registrada(), IdEventoRegistrada, tipo: TiposDeEvento.CobrancaRegistrada), acoes, CancellationToken.None);

        Assert.Equal(StatusFatura.Baixada, _faturas.Ler(FaturaId)!.Status);
        Assert.Single(_publicador.Publicados);
        Assert.Equal(2, _inbox.Linhas.Count);
        Assert.Equal(2, acoes.Completadas);
    }

    [Fact]
    public async Task Com_inbox_tres_consumidores_simultaneos_publicam_uma_vez_gracas_ao_ETag()
    {
        // Os três passam pela inbox antes de qualquer um gravar; quem segura a duplicata é o ETag.
        var processador = Processador(inbox: true);

        await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
            Task.Run(() => processador.ProcessarAsync(Mensagem(Paga()), new AcoesDeMensagemEmMemoria(), CancellationToken.None))));

        Assert.Single(_publicador.Publicados);
        Assert.Equal(1, _faturas.Ler(FaturaId)!.QuantidadeBaixas);
    }

    [Fact]
    public async Task ETag_quem_perde_a_corrida_rele_e_reavalia()
    {
        // Entre a leitura e a gravação, outra execução baixa a fatura: a nossa gravação recebe 412,
        // relê, vê "baixada" e não baixa nem publica de novo.
        var processador = Processador(inbox: true);
        _faturas.AntesDaProximaGravacao = () =>
        {
            _faturas.GravarPorFora(f =>
            {
                f.Status = StatusFatura.Baixada;
                f.QuantidadeBaixas = 1;
            }, FaturaId);
            return Task.CompletedTask;
        };

        await processador.ProcessarAsync(Mensagem(Paga()), new AcoesDeMensagemEmMemoria(), CancellationToken.None);

        Assert.Equal(1, _faturas.Conflitos);
        Assert.Empty(_publicador.Publicados);
        Assert.Equal(1, _faturas.Ler(FaturaId)!.QuantidadeBaixas);
        Assert.Single(_inbox.Linhas);
    }

    [Fact]
    public async Task ETag_conflito_irrelevante_ainda_baixa_depois_de_reler()
    {
        var processador = Processador(inbox: true);
        _faturas.AntesDaProximaGravacao = () =>
        {
            _faturas.GravarPorFora(f => f.AtualizadaEm = Agora.AddSeconds(-1), FaturaId);
            return Task.CompletedTask;
        };

        await processador.ProcessarAsync(Mensagem(Paga()), new AcoesDeMensagemEmMemoria(), CancellationToken.None);

        Assert.Equal(1, _faturas.Conflitos);
        Assert.Single(_publicador.Publicados);
        Assert.Equal(StatusFatura.Baixada, _faturas.Ler(FaturaId)!.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Confirmacao_de_fatura_inexistente_e_concluida_sem_efeito(bool inbox)
    {
        var processador = Processador(inbox: inbox);
        var acoes = new AcoesDeMensagemEmMemoria();
        var orfa = ClassificacaoERetryTests.Evento(faturaId: "99999999-9999-9999-9999-999999999999");

        await processador.ProcessarAsync(Mensagem(orfa), acoes, CancellationToken.None);

        Assert.Equal(1, acoes.Completadas);
        Assert.Empty(_publicador.Publicados);
        Assert.Empty(_inbox.Linhas);
    }

    // ---------- Incidente 3: schema inválido e falha transiente ----------

    [Fact]
    public async Task Baseline_schema_invalido_faz_abandon_e_relanca_para_reentrega_imediata()
    {
        var acoes = new AcoesDeMensagemEmMemoria();

        await Assert.ThrowsAsync<ConfirmacaoInvalidaException>(() =>
            Processador().ProcessarAsync(Mensagem(Paga(valor: "\"1.250,00\"")), acoes, CancellationToken.None));

        Assert.Equal(1, acoes.Abandonadas);
        Assert.Equal(0, acoes.Completadas);
        Assert.Empty(acoes.DeadLetters);
    }

    [Fact]
    public async Task Schema_invalido_vai_para_a_DLQ_na_primeira_entrega_com_motivo()
    {
        var acoes = new AcoesDeMensagemEmMemoria();

        await Processador(dlqComMotivo: true).ProcessarAsync(Mensagem(Paga(valor: "\"1.250,00\"")), acoes, CancellationToken.None);

        var (motivo, descricao) = Assert.Single(acoes.DeadLetters);
        Assert.Equal(MotivosDeadLetter.SchemaInvalida, motivo);
        Assert.Contains("$.valor", descricao);
        Assert.True(descricao!.Length <= MotivosDeadLetter.TamanhoMaximoDaDescricao);
        Assert.Equal(0, acoes.Completadas);
        Assert.Empty(_publicador.Publicados);

        var evento = _telemetria.Unico(EventosDeTelemetria.MensagemDeadLetter);
        Assert.Equal(FaturaId, evento["faturaId"]);
        Assert.Equal(IdEventoPaga, evento["idEvento"]);
        Assert.Equal(MotivosDeadLetter.SchemaInvalida, evento["motivo"]);
    }

    [Fact]
    public async Task Baseline_falha_transiente_faz_abandon_e_relanca_sem_agendar_nada()
    {
        var acoes = new AcoesDeMensagemEmMemoria();
        var transiente = Paga(simulaTransiente: true, transienteAte: Agora.AddSeconds(45));

        var erro = await Assert.ThrowsAsync<FalhaTransienteSimuladaException>(() =>
            Processador().ProcessarAsync(Mensagem(transiente), acoes, CancellationToken.None));

        // O kql/erros-transientes-por-operacao.kql reconhece a falha pelo tipo ou pela mensagem.
        Assert.StartsWith("Falha transiente simulada", erro.Message, StringComparison.Ordinal);
        Assert.Empty(_mensageria.Enviadas);
        Assert.Equal(1, acoes.Abandonadas);
        Assert.Equal(1, acoes.DestinosDefinidos);
    }

    [Fact]
    public async Task Baseline_tentativa_alta_ainda_transiente_lanca_a_mesma_excecao()
    {
        // Sem retry agendado não existe TentativasEsgotadas: é a mesma falha transiente do baseline.
        var acoes = new AcoesDeMensagemEmMemoria();
        var transiente = Paga(simulaTransiente: true, transienteAte: Agora.AddMinutes(10));

        var erro = await Assert.ThrowsAsync<FalhaTransienteSimuladaException>(() =>
            Processador().ProcessarAsync(Mensagem(transiente, tentativa: 3), acoes, CancellationToken.None));

        Assert.StartsWith("Falha transiente simulada", erro.Message, StringComparison.Ordinal);
        Assert.Empty(acoes.DeadLetters);
        Assert.Equal(1, acoes.Abandonadas);
    }

    [Fact]
    public async Task Falha_inesperada_tambem_faz_abandon_e_relanca()
    {
        // Ex.: a tabela fora do ar. Sem o abandon explícito, a mensagem só voltaria quando o lock expirasse.
        var acoes = new AcoesDeMensagemEmMemoria();
        _faturas.AntesDaProximaGravacao = () => throw new InvalidOperationException("Table Storage indisponível (simulado).");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Processador().ProcessarAsync(Mensagem(Paga()), acoes, CancellationToken.None));

        Assert.Equal(1, acoes.Abandonadas);
        Assert.Equal(0, acoes.Completadas);
    }

    [Fact]
    public async Task Retry_agendado_envia_copia_para_daqui_a_10_segundos_e_completa_a_original()
    {
        var acoes = new AcoesDeMensagemEmMemoria();
        var transiente = Paga(simulaTransiente: true, transienteAte: Agora.AddSeconds(45));
        var original = Mensagem(transiente);

        await Processador(retryAgendado: true).ProcessarAsync(original, acoes, CancellationToken.None);

        var (fila, copia) = Assert.Single(_mensageria.Enviadas);
        Assert.Equal(Filas.ConfirmacoesBanco, fila);
        Assert.Equal($"{IdEventoPaga}-t1", copia.MessageId);
        Assert.Equal(FaturaId, copia.SessionId);
        Assert.Equal(1, copia.ApplicationProperties[PropriedadesDaMensagem.Tentativa]);
        Assert.Equal(TiposDeEvento.CobrancaPaga, copia.ApplicationProperties[PropriedadesDaMensagem.Tipo]);
        Assert.Equal(Agora.AddSeconds(10), copia.ScheduledEnqueueTime);
        Assert.Equal(original.Body.ToArray(), copia.Body.ToArray());
        Assert.Equal(1, acoes.Completadas);

        var evento = _telemetria.Unico(EventosDeTelemetria.MensagemRetryAgendado);
        Assert.Equal("1", evento["tentativa"]);
        Assert.Equal("10", evento["atrasoSegundos"]);
        Assert.Equal(StatusFatura.CobrancaEmitida, _faturas.Ler(FaturaId)!.Status);
    }

    [Fact]
    public async Task Retry_agendado_da_segunda_para_a_terceira_tentativa_espera_40_segundos()
    {
        var transiente = Paga(simulaTransiente: true, transienteAte: Agora.AddSeconds(15));

        await Processador(retryAgendado: true).ProcessarAsync(
            Mensagem(transiente, $"{IdEventoPaga}-t2", tentativa: 2), new AcoesDeMensagemEmMemoria(), CancellationToken.None);

        var copia = Assert.Single(_mensageria.Enviadas).Mensagem;
        Assert.Equal($"{IdEventoPaga}-t3", copia.MessageId);
        Assert.Equal(3, copia.ApplicationProperties[PropriedadesDaMensagem.Tentativa]);
        Assert.Equal(Agora.AddSeconds(40), copia.ScheduledEnqueueTime);
    }

    [Fact]
    public async Task Retry_agendado_esgotado_vai_para_a_DLQ_com_TentativasEsgotadas()
    {
        var acoes = new AcoesDeMensagemEmMemoria();
        var transiente = Paga(simulaTransiente: true, transienteAte: Agora.AddMinutes(10));

        await Processador(retryAgendado: true).ProcessarAsync(
            Mensagem(transiente, $"{IdEventoPaga}-t3", tentativa: 3), acoes, CancellationToken.None);

        Assert.Empty(_mensageria.Enviadas);
        Assert.Equal(MotivosDeadLetter.TentativasEsgotadas, Assert.Single(acoes.DeadLetters).Motivo);
        Assert.Equal(MotivosDeadLetter.TentativasEsgotadas, _telemetria.Unico(EventosDeTelemetria.MensagemDeadLetter)["motivo"]);
    }

    [Fact]
    public async Task Copia_agendada_que_chega_depois_da_falha_baixa_normalmente()
    {
        var processador = Processador(inbox: true, retryAgendado: true);
        var transiente = Paga(simulaTransiente: true, transienteAte: Agora.AddSeconds(45));
        _relogio.Agora = Agora.AddSeconds(70);

        await processador.ProcessarAsync(Mensagem(transiente, $"{IdEventoPaga}-t3", tentativa: 3), new AcoesDeMensagemEmMemoria(), CancellationToken.None);

        Assert.Empty(_mensageria.Enviadas);
        Assert.Single(_publicador.Publicados);
        Assert.Equal(StatusFatura.Baixada, _faturas.Ler(FaturaId)!.Status);
    }
}
