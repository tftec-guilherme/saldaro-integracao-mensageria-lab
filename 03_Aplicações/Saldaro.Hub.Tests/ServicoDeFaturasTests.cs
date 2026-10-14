using System.Text.Json;
using Azure;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;
using Saldaro.Hub.Functions;
using Saldaro.Testes;

namespace Saldaro.Hub.Tests;

/// <summary>Recebimento da fatura (síncrono × assíncrono), idempotência por Idempotency-Key e emissão da cobrança.</summary>
public class ServicoDeFaturasTests
{
    private readonly RepositorioFaturasEmMemoria _faturas = new();
    private readonly RepositorioIdempotenciaEmMemoria _idempotencia = new();
    private readonly MensageriaEmMemoria _mensageria = new();
    private readonly BancoEmMemoria _banco = new();
    private readonly TelemetriaEmMemoria _telemetria = new();
    private readonly RelogioControlado _relogio = new(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));

    private ServicoDeFaturas Servico(bool asyncRequestReply) => new(
        _faturas, _idempotencia, _mensageria, _banco, _telemetria,
        Configuracoes.Cenario(asyncRequestReply: asyncRequestReply), Configuracoes.Hub(), _relogio,
        NullLogger<ServicoDeFaturas>.Instance);

    // Um CNPJ gerado por algoritmo para a classe toda: o reenvio do mesmo pedido leva o mesmo pagador.
    private static readonly string CnpjDoPagador = CnpjDeTeste.Gerar();

    private static NovaFatura Pedido(string? referenciaErp = "ERP-0001") => new()
    {
        Valor = 1250.00m,
        Vencimento = "2026-11-10",
        PagadorCnpj = CnpjDoPagador,
        ReferenciaErp = referenciaErp,
        Caos = "latencia=45&duplicar=3",
    };

    [Fact]
    public async Task Mesma_IdempotencyKey_devolve_o_mesmo_faturaId_sem_reenfileirar()
    {
        var servico = Servico(asyncRequestReply: true);

        var primeiro = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);
        var reenvio = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);

        Assert.IsType<ResultadoDoRecebimento.AceitaAssincrona>(primeiro);
        Assert.IsType<ResultadoDoRecebimento.ChaveRepetida>(reenvio);
        Assert.Equal(primeiro.FaturaId, reenvio.FaturaId);
        Assert.Single(_mensageria.Na(Filas.FaturasRecebidas));
        Assert.Equal(1, _telemetria.Contar(EventosDeTelemetria.FaturaRecebida));
    }

    [Fact]
    public async Task Mesma_chave_vale_mesmo_se_o_reenvio_vier_com_outro_corpo()
    {
        var servico = Servico(asyncRequestReply: true);

        var primeiro = await servico.ReceberAsync(Pedido("ERP-0001"), "cliente-a", "chave-0001", CancellationToken.None);
        var reenvio = await servico.ReceberAsync(Pedido("ERP-9999"), "cliente-a", "chave-0001", CancellationToken.None);

        Assert.Equal(primeiro.FaturaId, reenvio.FaturaId);
        Assert.Single(_mensageria.Na(Filas.FaturasRecebidas));
    }

    [Fact]
    public async Task Reenvios_simultaneos_com_a_mesma_chave_enfileiram_uma_vez()
    {
        var servico = Servico(asyncRequestReply: true);

        var resultados = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None))));

        Assert.Single(resultados, r => r is ResultadoDoRecebimento.AceitaAssincrona);
        Assert.Equal(9, resultados.Count(r => r is ResultadoDoRecebimento.ChaveRepetida));
        Assert.Single(resultados.Select(r => r.FaturaId).Distinct());
        Assert.Single(_mensageria.Na(Filas.FaturasRecebidas));
    }

    // ---------- Idempotency-Key: compensação quando gravar ou enfileirar falha ----------

    [Fact]
    public async Task Falha_ao_gravar_a_fatura_libera_a_chave_e_o_reenvio_cria_a_fatura_e_enfileira_uma_vez()
    {
        var servico = Servico(asyncRequestReply: true);
        var erro = new RequestFailedException(503, "Table Storage indisponível");
        _faturas.FalhaAoSalvar = erro;

        var lancada = await Assert.ThrowsAsync<RequestFailedException>(() =>
            servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None));
        var reenvio = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);

        Assert.Same(erro, lancada);
        Assert.IsType<ResultadoDoRecebimento.AceitaAssincrona>(reenvio);
        Assert.Equal(StatusFatura.Recebida, _faturas.Ler(reenvio.FaturaId)?.Status);
        Assert.Equal(1, _faturas.Quantidade);
        var mensagem = Assert.Single(_mensageria.Na(Filas.FaturasRecebidas));
        Assert.Equal(reenvio.FaturaId, mensagem.MessageId);
    }

    [Fact]
    public async Task Falha_ao_enfileirar_apaga_a_fatura_e_o_reenvio_com_a_mesma_chave_enfileira_uma_vez()
    {
        var servico = Servico(asyncRequestReply: true);
        var erro = new ServiceBusException("namespace indisponível", ServiceBusFailureReason.ServiceCommunicationProblem);
        _mensageria.FalhaNoProximoEnvio = erro;

        var lancada = await Assert.ThrowsAsync<ServiceBusException>(() =>
            servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None));

        // A fatura que não entrou na fila não fica "recebida" para sempre.
        Assert.Same(erro, lancada);
        Assert.Equal(0, _faturas.Quantidade);

        var reenvio = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);

        Assert.IsType<ResultadoDoRecebimento.AceitaAssincrona>(reenvio);
        Assert.Equal(StatusFatura.Recebida, _faturas.Ler(reenvio.FaturaId)?.Status);
        Assert.Equal(1, _faturas.Quantidade);
        var mensagem = Assert.Single(_mensageria.Na(Filas.FaturasRecebidas));
        Assert.Equal(reenvio.FaturaId, mensagem.MessageId);
        Assert.Equal(1, _telemetria.Contar(EventosDeTelemetria.FaturaRecebida));
    }

    [Fact]
    public async Task Envio_que_falha_depois_de_a_fila_aceitar_deixa_a_mensagem_antiga_sem_efeito()
    {
        // Timeout na confirmação do Service Bus: a mensagem entrou na fila, mas o hub não soube e desfez tudo.
        // O reenvio do ERP cria outra fatura; a mensagem antiga aponta para uma fatura que já não existe.
        var servico = Servico(asyncRequestReply: true);
        _mensageria.FalhaNoProximoEnvio = new ServiceBusException("a fila não confirmou o envio a tempo", ServiceBusFailureReason.ServiceTimeout);
        _mensageria.FilaAceitaAntesDaFalha = true;

        await Assert.ThrowsAsync<ServiceBusException>(() =>
            servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None));
        var reenvio = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);

        var mensagens = _mensageria.Na(Filas.FaturasRecebidas).ToList();
        Assert.Equal(2, mensagens.Count);
        var antiga = Assert.Single(mensagens, m => m.MessageId != reenvio.FaturaId);
        var nova = Assert.Single(mensagens, m => m.MessageId == reenvio.FaturaId);

        // O EmitirCobranca acha a fatura antiga inexistente e completa a mensagem sem chamar o banco.
        var funcao = new EmitirCobranca(servico, NullLogger<EmitirCobranca>.Instance);
        var acoesDaAntiga = new AcoesDeMensagemEmMemoria();
        await funcao.Run(Recebida(antiga), acoesDaAntiga, CancellationToken.None);
        await funcao.Run(Recebida(nova), new AcoesDeMensagemEmMemoria(), CancellationToken.None);

        Assert.Equal(1, acoesDaAntiga.Completadas);
        Assert.Equal(0, acoesDaAntiga.Abandonadas);
        var chamada = Assert.Single(_banco.Chamadas);
        Assert.Equal(reenvio.FaturaId, chamada.FaturaId);
    }

    [Fact]
    public async Task Compensacao_roda_mesmo_com_a_requisicao_cancelada()
    {
        // O APIM desiste e fecha a conexão enquanto a fila demora, e o host pode cancelar o token da requisição.
        // Desfazer com esse token falharia na hora e deixaria a chave presa.
        using var requisicao = new CancellationTokenSource();
        _mensageria.AoEnviar = requisicao.Cancel;
        _mensageria.RespeitaCancelamento = true;
        var servico = Servico(asyncRequestReply: true);

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", requisicao.Token));
        _mensageria.AoEnviar = null;
        var reenvio = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);

        Assert.IsType<ResultadoDoRecebimento.AceitaAssincrona>(reenvio);
        Assert.Equal(1, _faturas.Quantidade);
        Assert.Single(_mensageria.Na(Filas.FaturasRecebidas));
    }

    [Fact]
    public async Task Compensacao_e_melhor_esforco_e_relanca_a_excecao_original()
    {
        // Apagar a fatura também falhou: o erro da compensação vai para o log, a chave é liberada mesmo assim
        // e quem chamou recebe o erro original, o do envio.
        var servico = Servico(asyncRequestReply: true);
        var erro = new ServiceBusException("namespace indisponível", ServiceBusFailureReason.ServiceCommunicationProblem);
        _mensageria.FalhaNoProximoEnvio = erro;
        _faturas.FalhaAoRemover = new RequestFailedException(503, "Table Storage indisponível");

        var lancada = await Assert.ThrowsAsync<ServiceBusException>(() =>
            servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None));
        var reenvio = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);

        Assert.Same(erro, lancada);
        Assert.IsType<ResultadoDoRecebimento.AceitaAssincrona>(reenvio);
        var mensagem = Assert.Single(_mensageria.Na(Filas.FaturasRecebidas));
        Assert.Equal(reenvio.FaturaId, mensagem.MessageId);
    }

    [Fact]
    public async Task Sem_chave_a_falha_ao_enfileirar_tambem_apaga_a_fatura()
    {
        var servico = Servico(asyncRequestReply: true);
        _mensageria.FalhaNoProximoEnvio = new ServiceBusException("namespace indisponível", ServiceBusFailureReason.ServiceCommunicationProblem);

        await Assert.ThrowsAsync<ServiceBusException>(() =>
            servico.ReceberAsync(Pedido(), "cliente-a", null, CancellationToken.None));

        Assert.Equal(0, _faturas.Quantidade);
        Assert.Empty(_mensageria.Enviadas);
    }

    [Fact]
    public async Task A_chave_e_por_cliente()
    {
        var servico = Servico(asyncRequestReply: true);

        var a = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);
        var b = await servico.ReceberAsync(Pedido(), "cliente-b", "chave-0001", CancellationToken.None);

        Assert.IsType<ResultadoDoRecebimento.AceitaAssincrona>(b);
        Assert.NotEqual(a.FaturaId, b.FaturaId);
        Assert.Equal(2, _mensageria.Na(Filas.FaturasRecebidas).Count());
    }

    [Fact]
    public async Task Sem_chave_o_reenvio_vira_outra_fatura()
    {
        // É por isso que o ERP manda Idempotency-Key: sem ela, o reenvio refaz o trabalho.
        var servico = Servico(asyncRequestReply: true);

        var primeiro = await servico.ReceberAsync(Pedido(), "cliente-a", null, CancellationToken.None);
        var reenvio = await servico.ReceberAsync(Pedido(), "cliente-a", null, CancellationToken.None);

        Assert.NotEqual(primeiro.FaturaId, reenvio.FaturaId);
        Assert.True(Guid.TryParse(primeiro.FaturaId, out _));
        Assert.Equal(2, _mensageria.Na(Filas.FaturasRecebidas).Count());
    }

    [Fact]
    public async Task Modo_assincrono_grava_recebida_e_enfileira_a_mensagem_do_contrato()
    {
        var servico = Servico(asyncRequestReply: true);

        var resultado = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);

        Assert.Equal(StatusFatura.Recebida, resultado.Status);
        Assert.Equal(StatusFatura.Recebida, _faturas.Ler(resultado.FaturaId)?.Status);
        Assert.Empty(_banco.Chamadas);

        var mensagem = Assert.Single(_mensageria.Na(Filas.FaturasRecebidas));
        Assert.Equal(resultado.FaturaId, mensagem.MessageId);
        Assert.Equal("application/json", mensagem.ContentType);

        using var corpo = JsonDocument.Parse(mensagem.Body.ToMemory());
        string[] campos = ["faturaId", "clienteId", "valor", "vencimento", "pagadorCnpj", "referenciaErp", "callbackUrl", "caos"];
        Assert.All(campos, campo => Assert.True(corpo.RootElement.TryGetProperty(campo, out _), $"faltou {campo}"));
        Assert.Equal("cliente-a", corpo.RootElement.GetProperty("clienteId").GetString());
        Assert.Equal(1250.00m, corpo.RootElement.GetProperty("valor").GetDecimal());

        var evento = _telemetria.Unico(EventosDeTelemetria.FaturaRecebida);
        Assert.Equal("async", evento["modo"]);
        Assert.Equal(resultado.FaturaId, evento["faturaId"]);
        Assert.Equal("cliente-a", evento["clienteId"]);
        Assert.Equal("ERP-0001", evento["referenciaErp"]);
    }

    [Fact]
    public async Task Baseline_sincrono_chama_o_banco_e_responde_com_a_cobranca()
    {
        var servico = Servico(asyncRequestReply: false);

        var resultado = await servico.ReceberAsync(Pedido(), "cliente-a", null, CancellationToken.None);

        var criada = Assert.IsType<ResultadoDoRecebimento.CriadaSincrona>(resultado);
        Assert.Equal(StatusFatura.CobrancaEmitida, criada.Status);
        Assert.StartsWith("COB-", criada.IdCobranca);
        Assert.Empty(_mensageria.Enviadas);

        var chamada = Assert.Single(_banco.Chamadas);
        Assert.Equal(criada.FaturaId, chamada.FaturaId);
        Assert.Equal("https://apim-saldaro-teste.azure-api.net/saldaro/webhooks/banco", chamada.WebhookUrl);

        Assert.Equal(criada.IdCobranca, _faturas.Ler(criada.FaturaId)?.IdCobranca);
        Assert.Equal("sync", _telemetria.Unico(EventosDeTelemetria.FaturaRecebida)["modo"]);
        var emitida = _telemetria.Unico(EventosDeTelemetria.CobrancaEmitida);
        Assert.Equal(criada.IdCobranca, emitida["idCobranca"]);
        Assert.Equal(criada.FaturaId, emitida["faturaId"]);
        Assert.Equal("ERP-0001", emitida["referenciaErp"]);
    }

    [Fact]
    public async Task Baseline_ignora_a_chave_e_cada_reenvio_vira_outra_fatura_e_outra_cobranca()
    {
        // Incidente 1: o APIM cortou aos 30 s, o ERP reenviou a mesma fatura, e o banco já tinha registrado.
        // A prova é a mesma referenciaErp com N faturas e N cobranças.
        var servico = Servico(asyncRequestReply: false);

        var primeiro = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);
        var reenvio = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);

        Assert.NotEqual(primeiro.FaturaId, reenvio.FaturaId);
        Assert.Equal(2, _faturas.Quantidade);
        Assert.Equal(2, _banco.Chamadas.Count);
        Assert.Equal(2, _telemetria.Eventos.Count(e => e.Nome == EventosDeTelemetria.FaturaRecebida && e.Propriedades["referenciaErp"] == "ERP-0001"));
        Assert.Equal(2, _telemetria.Eventos.Count(e => e.Nome == EventosDeTelemetria.CobrancaEmitida && e.Propriedades["referenciaErp"] == "ERP-0001"));
    }

    [Fact]
    public async Task Consertado_com_a_mesma_chave_a_referencia_vira_uma_fatura_e_uma_cobranca()
    {
        var servico = Servico(asyncRequestReply: true);

        var primeiro = await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);
        await servico.ReceberAsync(Pedido(), "cliente-a", "chave-0001", CancellationToken.None);
        await servico.EmitirCobrancaAsync(FaturaRecebidaMensagem.De(_faturas.Ler(primeiro.FaturaId)!), CancellationToken.None);

        Assert.Equal(1, _faturas.Quantidade);
        Assert.Single(_banco.Chamadas);
        Assert.Equal(1, _telemetria.Eventos.Count(e => e.Nome == EventosDeTelemetria.CobrancaEmitida && e.Propriedades["referenciaErp"] == "ERP-0001"));
    }

    [Fact]
    public async Task Baseline_grava_a_cobranca_mesmo_quando_o_APIM_desiste_no_meio()
    {
        // Incidente 1: aos 30 s o APIM corta e fecha a conexão, e o host pode cancelar a invocação. O baseline
        // ingênuo não desiste: espera o banco e grava a cobrança, para a duplicata aparecer inteira nos dados do hub.
        using var requisicao = new CancellationTokenSource();
        _banco.RespeitaCancelamento = true;
        _banco.AoReceber = requisicao.Cancel;
        var servico = Servico(asyncRequestReply: false);

        var resultado = await servico.ReceberAsync(Pedido(), "cliente-a", null, requisicao.Token);

        Assert.True(requisicao.IsCancellationRequested);
        var criada = Assert.IsType<ResultadoDoRecebimento.CriadaSincrona>(resultado);
        Assert.Equal(criada.IdCobranca, _faturas.Ler(resultado.FaturaId)?.IdCobranca);
        Assert.Equal(StatusFatura.CobrancaEmitida, _faturas.Ler(resultado.FaturaId)?.Status);
        Assert.Equal(1, _telemetria.Contar(EventosDeTelemetria.CobrancaEmitida));
    }

    [Fact]
    public async Task Baseline_com_timeout_do_cliente_http_devolve_falha_no_banco()
    {
        // Sem o token da requisição, o TaskCanceledException só vem do timeout de 120 s do HttpClient: vira 502.
        _banco.Falha = new TaskCanceledException("timeout do HttpClient");
        var servico = Servico(asyncRequestReply: false);

        var resultado = await servico.ReceberAsync(Pedido(), "cliente-a", null, CancellationToken.None);

        Assert.IsType<ResultadoDoRecebimento.FalhaNoBanco>(resultado);
        Assert.Equal(StatusFatura.Recebida, _faturas.Ler(resultado.FaturaId)?.Status);
    }

    [Fact]
    public async Task Baseline_com_banco_fora_devolve_falha_no_banco()
    {
        _banco.Falha = new BancoIndisponivelException("503");
        var servico = Servico(asyncRequestReply: false);

        var resultado = await servico.ReceberAsync(Pedido(), "cliente-a", null, CancellationToken.None);

        Assert.IsType<ResultadoDoRecebimento.FalhaNoBanco>(resultado);
        Assert.Equal(StatusFatura.Recebida, _faturas.Ler(resultado.FaturaId)?.Status);
    }

    [Fact]
    public async Task Emissao_grava_a_cobranca_e_o_status()
    {
        var servico = Servico(asyncRequestReply: true);
        var recebida = await servico.ReceberAsync(Pedido(), "cliente-a", null, CancellationToken.None);
        var mensagem = FaturaRecebidaMensagem.De(_faturas.Ler(recebida.FaturaId)!);

        var chamou = await servico.EmitirCobrancaAsync(mensagem, CancellationToken.None);

        Assert.True(chamou);
        var fatura = _faturas.Ler(recebida.FaturaId)!;
        Assert.Equal(StatusFatura.CobrancaEmitida, fatura.Status);
        Assert.False(string.IsNullOrEmpty(fatura.IdCobranca));
        Assert.Equal(1, _telemetria.Contar(EventosDeTelemetria.CobrancaEmitida));
    }

    [Fact]
    public async Task Emissao_com_banco_fora_lanca_excecao_para_o_Service_Bus_reentregar()
    {
        var servico = Servico(asyncRequestReply: true);
        var recebida = await servico.ReceberAsync(Pedido(), "cliente-a", null, CancellationToken.None);
        var mensagem = FaturaRecebidaMensagem.De(_faturas.Ler(recebida.FaturaId)!);
        _banco.Falha = new BancoIndisponivelException("503");

        await Assert.ThrowsAsync<BancoIndisponivelException>(() => servico.EmitirCobrancaAsync(mensagem, CancellationToken.None));
        Assert.Equal(StatusFatura.Recebida, _faturas.Ler(recebida.FaturaId)?.Status);
    }

    [Fact]
    public async Task Reentrega_depois_da_emissao_nao_chama_o_banco_de_novo()
    {
        var servico = Servico(asyncRequestReply: true);
        var recebida = await servico.ReceberAsync(Pedido(), "cliente-a", null, CancellationToken.None);
        var mensagem = FaturaRecebidaMensagem.De(_faturas.Ler(recebida.FaturaId)!);

        await servico.EmitirCobrancaAsync(mensagem, CancellationToken.None);
        var chamouDeNovo = await servico.EmitirCobrancaAsync(mensagem, CancellationToken.None);

        Assert.False(chamouDeNovo);
        Assert.Equal(1, _banco.QuantidadePara(recebida.FaturaId));
    }

    [Fact]
    public async Task Emissao_atrasada_nao_rebaixa_o_status_de_uma_fatura_ja_baixada()
    {
        // Banco lento: a confirmação de pagamento chegou antes da resposta da emissão.
        var servico = Servico(asyncRequestReply: true);
        var recebida = await servico.ReceberAsync(Pedido(), "cliente-a", null, CancellationToken.None);
        _faturas.GravarPorFora(f => f.Status = StatusFatura.Baixada, recebida.FaturaId);
        var mensagem = FaturaRecebidaMensagem.De(_faturas.Ler(recebida.FaturaId)!);

        await servico.EmitirCobrancaAsync(mensagem, CancellationToken.None);

        var fatura = _faturas.Ler(recebida.FaturaId)!;
        Assert.Equal(StatusFatura.Baixada, fatura.Status);
        Assert.False(string.IsNullOrEmpty(fatura.IdCobranca));
    }

    /// <summary>A mensagem enviada como o trigger a recebe da fila.</summary>
    private static ServiceBusReceivedMessage Recebida(ServiceBusMessage enviada) =>
        ServiceBusModelFactory.ServiceBusReceivedMessage(body: enviada.Body, messageId: enviada.MessageId, deliveryCount: 1);
}
