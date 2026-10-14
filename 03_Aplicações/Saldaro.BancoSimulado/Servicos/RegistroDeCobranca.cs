using Microsoft.Extensions.Logging;
using Saldaro.BancoSimulado.Dominio;

namespace Saldaro.BancoSimulado.Servicos;

/// <summary>Desfecho de um <c>POST /api/cobrancas</c>, que a Function traduz em código HTTP.</summary>
public abstract record ResultadoDoRegistro
{
    private ResultadoDoRegistro()
    {
    }

    /// <summary>400: o corpo não passou na validação.</summary>
    public sealed record PedidoInvalido(IReadOnlyList<string> Erros) : ResultadoDoRegistro;

    /// <summary>503: o sorteio do <c>falhaTransiente</c> disparou; nada foi gravado nem enfileirado.</summary>
    public sealed record FalhaTransienteSimulada(int Percentual) : ResultadoDoRegistro;

    /// <summary>201: cobrança gravada e webhooks enfileirados.</summary>
    public sealed record Registrada(Cobranca Cobranca, IReadOnlyList<EntregaPlanejada> Entregas) : ResultadoDoRegistro;
}

/// <summary>
/// Regra do <c>POST /api/cobrancas</c>: valida, sorteia a falha transiente, grava a cobrança,
/// enfileira os webhooks e só então espera a <c>latencia</c>.
/// </summary>
public sealed class RegistroDeCobranca(
    IRepositorioDeCobrancas repositorio,
    IFilaDePagamentos fila,
    IEspera espera,
    ISorteio sorteio,
    TimeProvider relogio,
    ILogger<RegistroDeCobranca> logger)
{
    public async Task<ResultadoDoRegistro> RegistrarAsync(
        PedidoDeCobranca pedido,
        BotoesDeCaos botoes,
        CancellationToken cancellationToken)
    {
        var erros = pedido.Validar();
        if (erros.Count > 0)
        {
            return new ResultadoDoRegistro.PedidoInvalido(erros);
        }

        // CONCEITO: falha transiente ANTES de qualquer efeito. Nada foi gravado nem enfileirado,
        // então quem chamou pode repetir com segurança (é o 503 que o EmitirCobranca do hub reentrega).
        if (sorteio.Dispara(botoes.FalhaTransiente))
        {
            return new ResultadoDoRegistro.FalhaTransienteSimulada(botoes.FalhaTransiente);
        }

        var idCobranca = DadosFicticios.NovoIdCobranca();
        var valor = decimal.Round(pedido.Valor.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero);
        var cobranca = new Cobranca(
            FaturaId: pedido.FaturaId!,
            IdCobranca: idCobranca,
            ClienteId: pedido.ClienteId ?? string.Empty,
            Valor: valor,
            Vencimento: pedido.Vencimento ?? string.Empty,
            PagadorCnpj: pedido.PagadorCnpj ?? string.Empty,
            WebhookUrl: pedido.WebhookUrl!,
            LinhaDigitavel: DadosFicticios.LinhaDigitavel(idCobranca, valor),
            QrCode: DadosFicticios.QrCode(idCobranca, valor),
            Caos: botoes.Descrever(),
            CriadaEm: relogio.GetUtcNow());

        // 1) Persiste a cobrança.
        await repositorio.GravarAsync(cobranca, cancellationToken);

        // 2) Enfileira os webhooks, cada um com o seu atraso, na ordem de envio.
        var entregas = PlanejadorDeWebhooks.Planejar(
            cobranca, botoes, pagaComFalhaTransiente: sorteio.Dispara(botoes.FalhaTransiente));
        foreach (var entrega in entregas)
        {
            await fila.EnfileirarAsync(entrega.ParaMensagem().ParaTexto(), entrega.Atraso, cancellationToken);
        }

        logger.LogInformation(
            "Cobrança {IdCobranca} gravada para a fatura {FaturaId} e {Entregas} webhooks enfileirados ({Caos}). Respondendo em {Latencia} s.",
            cobranca.IdCobranca, cobranca.FaturaId, entregas.Count, cobranca.Caos, botoes.Latencia);

        // 3) Só agora o banco "demora".
        // CONCEITO: o efeito acontece antes da resposta. Se o ERP desistir no meio da espera (o APIM
        // corta em 30 s) e reenviar, a primeira cobrança já existe e o reenvio registra a segunda (Incidente 1).
        await espera.EsperarAsync(TimeSpan.FromSeconds(botoes.Latencia), cancellationToken);

        return new ResultadoDoRegistro.Registrada(cobranca, entregas);
    }
}
