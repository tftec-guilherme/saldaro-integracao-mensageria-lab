using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Aplicacao;

/// <summary>
/// Como terminou o <c>POST /faturas</c>, que a Function traduz em código HTTP. Hierarquia fechada, como a
/// <see cref="Classificacao"/>: todo desfecho tem o <see cref="FaturaId"/> e o <see cref="Status"/>, e só os do
/// baseline trazem a cobrança ou o erro do banco.
/// </summary>
public abstract record ResultadoDoRecebimento
{
    private ResultadoDoRecebimento(string faturaId, string status)
    {
        FaturaId = faturaId;
        Status = status;
    }

    public string FaturaId { get; }

    public string Status { get; }

    /// <summary>Baseline: o banco respondeu dentro da requisição (201).</summary>
    public sealed record CriadaSincrona(string FaturaId, string Status, string IdCobranca) : ResultadoDoRecebimento(FaturaId, Status);

    /// <summary>Baseline: o banco falhou dentro da requisição (502).</summary>
    public sealed record FalhaNoBanco(string FaturaId, string Status, string Erro) : ResultadoDoRecebimento(FaturaId, Status);

    /// <summary>Consertado: fatura aceita e enfileirada (202).</summary>
    public sealed record AceitaAssincrona(string FaturaId, string Status) : ResultadoDoRecebimento(FaturaId, Status);

    /// <summary>Consertado: <c>Idempotency-Key</c> repetida; devolve a fatura original sem reenfileirar (202).</summary>
    public sealed record ChaveRepetida(string FaturaId, string Status) : ResultadoDoRecebimento(FaturaId, Status);
}

/// <summary>
/// Recebimento da fatura (<c>PostFatura</c>) e emissão da cobrança no banco (<c>PostFatura</c> no baseline,
/// <c>EmitirCobranca</c> no modo assíncrono). O toggle <c>Cenario:AsyncRequestReply</c> escolhe o caminho.
/// </summary>
public sealed class ServicoDeFaturas(
    IRepositorioFaturas faturas,
    IRepositorioIdempotencia idempotencia,
    IMensageria mensageria,
    IBancoClient banco,
    ITelemetria telemetria,
    IOptions<CenarioOptions> cenario,
    IOptions<HubOptions> hub,
    TimeProvider relogio,
    ILogger<ServicoDeFaturas> logger)
{
    public const string ModoSincrono = "sync";
    public const string ModoAssincrono = "async";

    public Task<ResultadoDoRecebimento> ReceberAsync(
        NovaFatura pedido, string clienteId, string? idempotencyKey, CancellationToken cancellationToken)
    {
        // TOGGLE Cenario:AsyncRequestReply (cenário 3.2).
        return cenario.Value.AsyncRequestReply
            ? ReceberAssincronoAsync(pedido, clienteId, idempotencyKey, cancellationToken)
            : ReceberSincronoAsync(pedido, clienteId, cancellationToken);
    }

    /// <summary>
    /// Baseline ingênuo: grava a fatura, chama o banco e só responde quando o banco responder.
    /// </summary>
    /// <remarks>
    /// Com o banco lento (<c>latencia=45</c>), o APIM corta a requisição aos 30 s (504), mas o banco já
    /// registrou a cobrança. O ERP reenvia, o hub cria outra fatura (<c>faturaId</c> novo) para a mesma
    /// <c>referenciaErp</c> e o banco registra outra cobrança: é o Incidente 1, visível nos eventos
    /// <c>fatura.recebida</c> e <c>cobranca.emitida</c> agrupados por <c>referenciaErp</c>. O <c>Idempotency-Key</c>
    /// é ignorado aqui.
    /// </remarks>
    private async Task<ResultadoDoRecebimento> ReceberSincronoAsync(NovaFatura pedido, string clienteId, CancellationToken cancellationToken)
    {
        var fatura = CriarFatura(pedido, clienteId, NovoFaturaId());
        await faturas.SalvarAsync(fatura, cancellationToken);
        telemetria.FaturaRecebida(fatura.FaturaId, clienteId, ModoSincrono, fatura.ReferenciaErp);

        // CONCEITO: ingênuo de propósito, como o POST síncrono da Saldaro 1.0. Quem desiste aos 30 s é o APIM (e o
        // ERP); o hub não desiste: segue esperando o banco (até o timeout de 120 s do HttpClient) e grava a cobrança
        // mesmo depois do 504. Por isso o token da requisição não vai para o banco nem para a gravação: com ele, o
        // host poderia cancelar a chamada quando o APIM fecha a conexão, e a cobrança que o banco já registrou
        // sumiria dos dados do hub. Assim a duplicata do Incidente 1 aparece inteira: N faturas e N cobranças.
        var semDesistir = CancellationToken.None;

        CobrancaEmitida cobranca;
        try
        {
            cobranca = await banco.EmitirCobrancaAsync(Solicitacao(fatura), fatura.Caos, semDesistir);
        }
        catch (Exception ex) when (ex is BancoIndisponivelException or BancoRespostaInesperadaException or HttpRequestException
                                       or TaskCanceledException)
        {
            // Sem o token da requisição, um TaskCanceledException aqui só pode ser o timeout do HttpClient (120 s).
            logger.LogWarning(ex, "Banco falhou durante o POST síncrono da fatura {FaturaId}.", fatura.FaturaId);
            return new ResultadoDoRecebimento.FalhaNoBanco(fatura.FaturaId, fatura.Status, ex.Message);
        }

        var atualizada = await RegistrarEmissaoAsync(fatura.FaturaId, cobranca.IdCobranca, semDesistir);
        return new ResultadoDoRecebimento.CriadaSincrona(
            fatura.FaturaId,
            atualizada?.Status ?? StatusFatura.CobrancaEmitida,
            cobranca.IdCobranca);
    }

    /// <summary>Consertado: grava, enfileira e responde 202 na hora; o banco é chamado pelo <c>EmitirCobranca</c>.</summary>
    private async Task<ResultadoDoRecebimento> ReceberAssincronoAsync(
        NovaFatura pedido, string clienteId, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var faturaId = NovoFaturaId();
        var chave = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey;

        if (chave is not null)
        {
            // CONCEITO: Idempotency-Key, em duas metades.
            // 1) Insert condicional. O ERP manda a mesma chave em todos os reenvios da mesma fatura. O primeiro
            //    pedido "reserva" a chave (só grava se a linha não existe); os reenvios batem no conflito 409,
            //    releem a linha e recebem o mesmo faturaId, sem gravar nem enfileirar de novo.
            // 2) Compensação. A reserva vem antes de gravar a fatura e de enfileirar. Se um dos dois falhar, o hub
            //    desfaz o que fez (apaga a fatura e libera a chave) e devolve o erro; sem isso, a chave apontaria
            //    para uma fatura que não existe ou que nunca entrou na fila, e todo reenvio receberia 202 por nada.
            //    Com a chave livre, o reenvio do ERP recomeça do zero, com outro faturaId.
            // Juntas, as duas metades fazem o reenvio deixar de ser perigoso. Resíduo: se o processo cair entre a
            // reserva e a compensação, a chave fica órfã, e os reenvios com ela recebem 202 de uma fatura que nunca
            // vai existir.
            var (chaveNova, faturaIdDaChave) = await idempotencia.RegistrarAsync(
                clienteId, chave, faturaId, relogio.GetUtcNow(), cancellationToken);
            if (!chaveNova)
            {
                var existente = await faturas.ObterAsync(faturaIdDaChave, cancellationToken);
                logger.LogInformation(
                    "Idempotency-Key {Chave} repetida para o cliente {ClienteId}: devolvendo a fatura {FaturaId} sem reenfileirar.",
                    chave, clienteId, faturaIdDaChave);
                return new ResultadoDoRecebimento.ChaveRepetida(faturaIdDaChave, existente?.Status ?? StatusFatura.Recebida);
            }
        }

        var fatura = CriarFatura(pedido, clienteId, faturaId);
        try
        {
            await faturas.SalvarAsync(fatura, cancellationToken);

            // CONCEITO: queue-based load leveling. A fila absorve a rajada; o EmitirCobranca drena no ritmo
            // que o banco aguenta, e a API responde em milissegundos, por mais lento que o banco esteja.
            var mensagem = new ServiceBusMessage(BinaryData.FromObjectAsJson(FaturaRecebidaMensagem.De(fatura), Json.Opcoes))
            {
                MessageId = fatura.FaturaId,
                ContentType = "application/json",
            };
            await mensageria.EnviarAsync(Filas.FaturasRecebidas, mensagem, cancellationToken);
        }
        catch (Exception falha)
        {
            // A compensação do CONCEITO acima: desfaz e devolve o erro original (o ERP recebe 500 e reenvia).
            await DesfazerRecebimentoAsync(clienteId, chave, faturaId, falha);
            throw;
        }

        telemetria.FaturaRecebida(fatura.FaturaId, clienteId, ModoAssincrono, fatura.ReferenciaErp);
        return new ResultadoDoRecebimento.AceitaAssincrona(fatura.FaturaId, fatura.Status);
    }

    /// <summary>
    /// Compensação do recebimento assíncrono, na ordem inversa da ida: apaga a fatura (se o <c>SalvarAsync</c> não
    /// chegou a gravar, não há o que apagar) e depois libera a reserva da <c>Idempotency-Key</c>. Melhor esforço: um
    /// passo que falha vai para o log e não troca a falha original, que o recebimento relança em seguida.
    /// </summary>
    /// <remarks>
    /// Nada é reenfileirado: num timeout, a mensagem pode já estar na fila, que não tem detecção de duplicatas.
    /// Se estiver, o <c>EmitirCobranca</c> acha a fatura inexistente e completa a mensagem sem chamar o banco.
    /// </remarks>
    private async Task DesfazerRecebimentoAsync(string clienteId, string? chave, string faturaId, Exception falha)
    {
        // Token próprio: se a falha foi o cancelamento da requisição, desfazer com o token dela falharia na hora e
        // deixaria a chave presa.
        var semDesistir = CancellationToken.None;
        logger.LogWarning(falha,
            "Falha ao gravar ou enfileirar a fatura {FaturaId}: desfazendo o recebimento (Idempotency-Key {Chave}).",
            faturaId, chave ?? "-");

        try
        {
            await faturas.RemoverAsync(faturaId, semDesistir);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Compensação incompleta: a fatura {FaturaId} não foi apagada e pode ficar recebida fora da fila.", faturaId);
        }

        if (chave is null)
        {
            return;
        }

        try
        {
            await idempotencia.RemoverAsync(clienteId, chave, faturaId, semDesistir);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Compensação incompleta: a Idempotency-Key {Chave} do cliente {ClienteId} continua reservada para a fatura {FaturaId} (chave órfã).",
                chave, clienteId, faturaId);
        }
    }

    /// <summary>O <c>faturaId</c> é sempre gerado pelo hub: um GUID novo por fatura recebida.</summary>
    private static string NovoFaturaId() => Guid.NewGuid().ToString();

    /// <summary>
    /// Trabalho do <c>EmitirCobranca</c>: chama o banco e grava o <c>idCobranca</c>. Exceções (ex.: 503 do banco)
    /// sobem de propósito: abandon e reentrega pelo Service Bus.
    /// </summary>
    /// <returns><c>true</c> se chamou o banco; <c>false</c> se não havia o que fazer.</returns>
    public async Task<bool> EmitirCobrancaAsync(FaturaRecebidaMensagem mensagem, CancellationToken cancellationToken)
    {
        var fatura = await faturas.ObterAsync(mensagem.FaturaId, cancellationToken);
        if (fatura is null)
        {
            logger.LogWarning("Fatura {FaturaId} não existe: mensagem concluída sem chamar o banco.", mensagem.FaturaId);
            return false;
        }

        // Receptor idempotente: numa reentrega depois de uma emissão bem-sucedida, não chama o banco de novo.
        if (!string.IsNullOrEmpty(fatura.IdCobranca))
        {
            logger.LogInformation(
                "Fatura {FaturaId} já tem a cobrança {IdCobranca}: reentrega concluída sem chamar o banco.",
                fatura.FaturaId, fatura.IdCobranca);
            return false;
        }

        var cobranca = await banco.EmitirCobrancaAsync(Solicitacao(mensagem), mensagem.Caos, cancellationToken);
        await RegistrarEmissaoAsync(mensagem.FaturaId, cobranca.IdCobranca, cancellationToken);
        return true;
    }

    /// <summary>
    /// Grava o <c>idCobranca</c> e avança para <c>cobranca-emitida</c> só se a fatura ainda está <c>recebida</c>.
    /// </summary>
    /// <remarks>
    /// Status monotônico: com o banco lento, o webhook (<c>registrada</c>/<c>baixada</c>) pode chegar antes da resposta
    /// da emissão. Isto não é a guarda do Incidente 2 (aquela trata confirmações e depende de <c>Cenario:Inbox</c>);
    /// vale sempre, para não haver corrida entre a resposta do banco e o webhook.
    /// </remarks>
    private async Task<Fatura?> RegistrarEmissaoAsync(string faturaId, string idCobranca, CancellationToken cancellationToken)
    {
        var atualizada = await faturas.AtualizarComConcorrenciaAsync(faturaId, fatura =>
        {
            fatura.IdCobranca = idCobranca;
            var decisao = MaquinaDeEstados.AvaliarEmissao(fatura.Status);
            if (decisao.Transiciona)
            {
                fatura.Status = decisao.StatusResultante;
            }

            fatura.AtualizadaEm = relogio.GetUtcNow();
            return true;
        }, cancellationToken);

        if (atualizada is null)
        {
            logger.LogWarning("Fatura {FaturaId} sumiu antes de gravar a cobrança {IdCobranca}.", faturaId, idCobranca);
            return null;
        }

        telemetria.CobrancaEmitida(faturaId, idCobranca, atualizada.ReferenciaErp);
        return atualizada;
    }

    private Fatura CriarFatura(NovaFatura pedido, string clienteId, string faturaId)
    {
        var agora = relogio.GetUtcNow();
        return new Fatura
        {
            FaturaId = faturaId,
            ClienteId = clienteId,
            Valor = pedido.Valor ?? 0m,
            Vencimento = pedido.Vencimento,
            PagadorCnpj = pedido.PagadorCnpj,
            ReferenciaErp = pedido.ReferenciaErp,
            Status = StatusFatura.Recebida,
            CallbackUrl = string.IsNullOrWhiteSpace(pedido.CallbackUrl) ? null : pedido.CallbackUrl,
            Caos = string.IsNullOrWhiteSpace(pedido.Caos) ? null : pedido.Caos,
            QuantidadeBaixas = 0,
            CriadaEm = agora,
            AtualizadaEm = agora,
        };
    }

    /// <summary>Corpo do <c>POST /cobrancas</c> a partir da fatura que acabou de ser gravada (caminho síncrono).</summary>
    private SolicitacaoDeCobranca Solicitacao(Fatura fatura) => new(
        fatura.FaturaId,
        fatura.ClienteId,
        fatura.Valor,
        fatura.Vencimento,
        fatura.PagadorCnpj,
        UrlsPublicas.Webhook(hub.Value));

    /// <summary>
    /// Corpo do <c>POST /cobrancas</c> a partir da mensagem da fila (caminho assíncrono). Os dados vêm da mensagem, e
    /// não da linha lida da tabela: a coluna <c>valor</c> é double, e <c>1250.00</c> chegaria ao banco como <c>1250</c>.
    /// </summary>
    private SolicitacaoDeCobranca Solicitacao(FaturaRecebidaMensagem mensagem) => new(
        mensagem.FaturaId,
        mensagem.ClienteId,
        mensagem.Valor,
        mensagem.Vencimento,
        mensagem.PagadorCnpj,
        UrlsPublicas.Webhook(hub.Value));
}
