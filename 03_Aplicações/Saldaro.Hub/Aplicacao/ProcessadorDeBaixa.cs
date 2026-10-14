using System.Diagnostics;
using System.Globalization;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Aplicacao;

/// <summary>
/// Lógica de baixa da seção 5.3, compartilhada por <c>ProcessarConfirmacaoSemSessao</c> e
/// <c>ProcessarConfirmacaoComSessao</c>. Passos: 1) schema, 2) transiente, 3) inbox, 4) máquina de estados,
/// 5) efeitos, 6) completar a mensagem.
/// </summary>
/// <remarks>
/// CONCEITO: peek-lock. A mensagem chega travada para este consumidor e continua na fila até ele dizer o que
/// fazer: completar (sai da fila), dead-letter (vai para a DLQ com motivo) ou abandon (volta para a fila e o
/// Service Bus reentrega, somando 1 no DeliveryCount). Por isso os triggers usam
/// <c>AutoCompleteMessages = false</c>: quem decide o destino é este código, não o runtime.
/// </remarks>
public sealed class ProcessadorDeBaixa(
    IRepositorioFaturas faturas,
    IRepositorioInbox inbox,
    IMensageria mensageria,
    IPublicadorDeEventos publicador,
    ITelemetria telemetria,
    IOptions<CenarioOptions> cenario,
    TimeProvider relogio,
    ILogger<ProcessadorDeBaixa> logger)
{
    /// <summary>
    /// Processa uma confirmação. Qualquer falha sem conserto (baseline, ou transiente sem retry agendado) termina
    /// em abandon explícito + exceção relançada: reentrega imediata (ver <see cref="AbandonExplicito"/>).
    /// </summary>
    public Task ProcessarAsync(ServiceBusReceivedMessage mensagem, ServiceBusMessageActions acoes, CancellationToken cancellationToken) =>
        AbandonExplicito.ProcessarAsync(mensagem, acoes, rastreadas => ProcessarSemAbandonAsync(mensagem, rastreadas, cancellationToken), logger);

    private async Task ProcessarSemAbandonAsync(
        ServiceBusReceivedMessage mensagem, ServiceBusMessageActions acoes, CancellationToken cancellationToken)
    {
        var opcoes = cenario.Value;
        var agora = relogio.GetUtcNow();
        var tentativaAtual = LerTentativa(mensagem);

        // Passos 1 e 2: classificar antes de qualquer efeito.
        var classificacao = ClassificadorDeConfirmacao.Classificar(mensagem.Body.ToMemory().Span, agora, tentativaAtual);
        switch (classificacao)
        {
            case Classificacao.SchemaInvalida schemaInvalida:
                await TratarSchemaInvalidaAsync(mensagem, acoes, schemaInvalida.Erro, opcoes, cancellationToken);
                return;

            case Classificacao.Transiente transiente:
                await TratarTransienteAsync(mensagem, acoes, transiente, opcoes, agora, cancellationToken);
                return;

            case Classificacao.TentativasEsgotadas esgotadas:
                await TratarTentativasEsgotadasAsync(mensagem, acoes, esgotadas, opcoes, cancellationToken);
                return;

            case Classificacao.Processavel processavel:
                // TOGGLE Cenario:Inbox (cenário 3.3): inbox + máquina de estados + ETag.
                if (opcoes.Inbox)
                {
                    await BaixarComGuardaAsync(processavel.Evento, agora, cancellationToken);
                }
                else
                {
                    await BaixarSemGuardaAsync(processavel.Evento, agora, cancellationToken);
                }

                // Passo 6.
                await acoes.CompleteMessageAsync(mensagem, cancellationToken);
                return;

            default:
                throw new UnreachableException($"Classificação não tratada: {classificacao.GetType().Name}");
        }
    }

    private async Task TratarSchemaInvalidaAsync(
        ServiceBusReceivedMessage mensagem, ServiceBusMessageActions acoes, string erro, CenarioOptions opcoes, CancellationToken cancellationToken)
    {
        // TOGGLE Cenario:DlqComMotivo (cenário 3.4).
        if (!opcoes.DlqComMotivo)
        {
            // Baseline: abandon + exceção, e o Service Bus reentrega na hora. Reentregar não conserta um schema
            // inválido: depois de maxDeliveryCount tentativas a mensagem cai na DLQ com o motivo genérico
            // MaxDeliveryCountExceeded, misturada com as falhas transientes.
            throw new ConfirmacaoInvalidaException(erro);
        }

        // CONCEITO: dead-letter com motivo. Erro permanente vai para a DLQ já na primeira entrega, com
        // DeadLetterReason e DeadLetterErrorDescription dizendo o porquê: a triagem agrupa por motivo e
        // sabe o que corrigir e reprocessar sem adivinhar.
        await acoes.DeadLetterMessageAsync(
            mensagem,
            propertiesToModify: null,
            deadLetterReason: MotivosDeadLetter.SchemaInvalida,
            deadLetterErrorDescription: MotivosDeadLetter.Truncar(erro),
            cancellationToken: cancellationToken);

        var (faturaId, idEvento) = Identificar(mensagem);
        telemetria.MensagemDeadLetter(faturaId, idEvento, MotivosDeadLetter.SchemaInvalida);
        logger.LogWarning("Mensagem {MessageId} enviada para a DLQ com motivo {Motivo}: {Erro}",
            mensagem.MessageId, MotivosDeadLetter.SchemaInvalida, erro);
    }

    private async Task TratarTransienteAsync(
        ServiceBusReceivedMessage mensagem,
        ServiceBusMessageActions acoes,
        Classificacao.Transiente transiente,
        CenarioOptions opcoes,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        // TOGGLE Cenario:RetryAgendado (cenário 3.4).
        if (!opcoes.RetryAgendado)
        {
            // Baseline: abandon + exceção. As reentregas são imediatas; as maxDeliveryCount tentativas acabam em
            // segundos, antes dos 45 s da falha, e uma mensagem boa termina na DLQ.
            throw new FalhaTransienteSimuladaException(transiente.Erro);
        }

        var evento = transiente.Evento;

        // CONCEITO: retry agendado. A original é completada e uma cópia idêntica volta para a fila só daqui a
        // 5 s × 2^n. O MessageId novo ({idEvento}-t{n}) evita que a detecção de duplicatas descarte a cópia;
        // o SessionId igual mantém a cópia na sessão (fatura) certa.
        var copia = new ServiceBusMessage(mensagem)
        {
            MessageId = $"{evento.IdEvento}-t{transiente.Tentativa}",
            SessionId = mensagem.SessionId,
            ScheduledEnqueueTime = agora + transiente.Atraso,
        };
        copia.ApplicationProperties[PropriedadesDaMensagem.Tentativa] = transiente.Tentativa;

        await mensageria.EnviarAsync(Filas.ConfirmacoesBanco, copia, cancellationToken);
        await acoes.CompleteMessageAsync(mensagem, cancellationToken);

        var atrasoSegundos = (int)transiente.Atraso.TotalSeconds;
        telemetria.MensagemRetryAgendado(evento.FaturaId, evento.IdEvento, transiente.Tentativa, atrasoSegundos);
        logger.LogInformation("Falha transiente na confirmação {IdEvento}: tentativa {Tentativa} agendada para daqui a {Atraso} s.",
            evento.IdEvento, transiente.Tentativa, atrasoSegundos);
    }

    private async Task TratarTentativasEsgotadasAsync(
        ServiceBusReceivedMessage mensagem,
        ServiceBusMessageActions acoes,
        Classificacao.TentativasEsgotadas esgotadas,
        CenarioOptions opcoes,
        CancellationToken cancellationToken)
    {
        // TOGGLE Cenario:RetryAgendado (cenário 3.4). Desligado, não existe "tentativas esgotadas": uma cópia agendada
        // antes de o toggle ser desligado ainda traz a propriedade tentativa, e vira a mesma falha transiente do baseline.
        if (!opcoes.RetryAgendado)
        {
            throw new FalhaTransienteSimuladaException(esgotadas.Erro);
        }

        var evento = esgotadas.Evento;
        var descricao = $"{esgotadas.Erro} O retry agendado esgotou as {PoliticaDeRetry.MaximoDeTentativas} tentativas.";
        await acoes.DeadLetterMessageAsync(
            mensagem,
            propertiesToModify: null,
            deadLetterReason: MotivosDeadLetter.TentativasEsgotadas,
            deadLetterErrorDescription: MotivosDeadLetter.Truncar(descricao),
            cancellationToken: cancellationToken);

        telemetria.MensagemDeadLetter(evento.FaturaId, evento.IdEvento, MotivosDeadLetter.TentativasEsgotadas);
        logger.LogWarning("Confirmação {IdEvento} enviada para a DLQ com motivo {Motivo}.", evento.IdEvento, MotivosDeadLetter.TentativasEsgotadas);
    }

    /// <summary>Baseline ingênuo: sem inbox, sem guarda de estado, sem ETag.</summary>
    private async Task BaixarSemGuardaAsync(EventoBanco evento, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        var fatura = await faturas.ObterAsync(evento.FaturaId, cancellationToken);
        if (fatura is null)
        {
            logger.LogWarning("Confirmação {IdEvento} para a fatura inexistente {FaturaId}: concluída sem efeito.", evento.IdEvento, evento.FaturaId);
            return;
        }

        var decisao = MaquinaDeEstados.AvaliarSemGuarda(evento.Tipo);
        Aplicar(fatura, decisao, agora);

        // Sem ETag: grava por cima de quem gravou antes. Duas baixas simultâneas podem até perder uma
        // soma de quantidadeBaixas (lost update). E cada cobranca.paga publica FaturaBaixada de novo.
        await faturas.AtualizarAsync(fatura, exigirMesmaVersao: false, cancellationToken);

        if (decisao.Efeito == EfeitoDaTransicao.Baixa)
        {
            await PublicarBaixaAsync(fatura, evento, cancellationToken);
        }
    }

    /// <summary>Consertado: inbox, máquina de estados tolerante a desordem e ETag.</summary>
    private async Task BaixarComGuardaAsync(EventoBanco evento, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        // Passo 3. CONCEITO: inbox (Idempotent Receiver). Cada evento processado deixa uma linha
        // (PK = faturaId, RK = idEvento). A confirmação que chega de novo (confirmação repetida pelo banco ou
        // reentrega do Service Bus) encontra a linha e é concluída sem efeito.
        if (await inbox.ExisteAsync(evento.FaturaId, evento.IdEvento, cancellationToken))
        {
            telemetria.ConfirmacaoDuplicadaIgnorada(evento.FaturaId, evento.IdEvento);
            logger.LogInformation("Confirmação {IdEvento} já processada (inbox): ignorada.", evento.IdEvento);
            return;
        }

        // Passo 4: máquina de estados + ETag. CONCEITO: ETag. A gravação só vale se a fatura ainda estiver na
        // versão lida; se outra execução gravou no meio (412), AtualizarComConcorrenciaAsync relê e a decisão é
        // refeita sobre o estado novo. É o que segura dois consumidores baixando a mesma fatura ao mesmo tempo.
        DecisaoDeEstado? decisao = null;
        var fatura = await faturas.AtualizarComConcorrenciaAsync(evento.FaturaId, atual =>
        {
            decisao = MaquinaDeEstados.AvaliarConfirmacao(atual.Status, evento.Tipo);
            if (!decisao.Transiciona)
            {
                return false;
            }

            Aplicar(atual, decisao, agora);
            return true;
        }, cancellationToken);

        if (fatura is null || decisao is null)
        {
            logger.LogWarning("Confirmação {IdEvento} para a fatura inexistente {FaturaId}: concluída sem efeito.", evento.IdEvento, evento.FaturaId);
            return;
        }

        // Passo 5: efeitos só na transição real.
        if (decisao.Transiciona && decisao.Efeito == EfeitoDaTransicao.Baixa)
        {
            await PublicarBaixaAsync(fatura, evento, cancellationToken);
        }
        else if (!decisao.Transiciona)
        {
            logger.LogInformation("Confirmação {IdEvento} ({Tipo}) sem transição na fatura {FaturaId}: {Motivo}.",
                evento.IdEvento, evento.Tipo, evento.FaturaId, decisao.Motivo);
        }

        await inbox.RegistrarAsync(evento.FaturaId, evento.IdEvento, evento.Tipo, agora, cancellationToken);
    }

    private static void Aplicar(Fatura fatura, DecisaoDeEstado decisao, DateTimeOffset agora)
    {
        fatura.Status = decisao.StatusResultante;
        fatura.AtualizadaEm = agora;
        if (decisao.Efeito == EfeitoDaTransicao.Baixa)
        {
            fatura.BaixadaEm = agora;
            fatura.QuantidadeBaixas++;
        }
    }

    private async Task PublicarBaixaAsync(Fatura fatura, EventoBanco evento, CancellationToken cancellationToken)
    {
        // Gravar "baixada" e publicar o evento são duas operações em dois serviços: se o processo cair entre
        // uma e outra, a publicação se perde. Em produção, o padrão Outbox fecha essa janela.
        await publicador.PublicarFaturaBaixadaAsync(
            new FaturaBaixadaDados(fatura.FaturaId, fatura.ClienteId, evento.Valor, fatura.BaixadaEm ?? relogio.GetUtcNow(), evento.IdEvento),
            cancellationToken);

        telemetria.FaturaBaixada(fatura.FaturaId, evento.IdEvento, fatura.ClienteId);
        logger.LogInformation("Fatura {FaturaId} baixada pela confirmação {IdEvento} (baixas: {QuantidadeBaixas}).",
            fatura.FaturaId, evento.IdEvento, fatura.QuantidadeBaixas);
    }

    private static int LerTentativa(ServiceBusReceivedMessage mensagem) =>
        mensagem.ApplicationProperties.TryGetValue(PropriedadesDaMensagem.Tentativa, out var valor)
        && int.TryParse(Convert.ToString(valor, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var tentativa)
            ? tentativa
            : 0;

    private static (string? FaturaId, string? IdEvento) Identificar(ServiceBusReceivedMessage mensagem)
    {
        var (faturaId, idEvento) = IdentificacaoDoEvento.MelhorEsforco(mensagem.Body.ToMemory());
        return (faturaId ?? mensagem.SessionId, idEvento ?? mensagem.MessageId);
    }
}
