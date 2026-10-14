using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saldaro.BancoSimulado.Dominio;
using Saldaro.BancoSimulado.Servicos;

namespace Saldaro.BancoSimulado.Functions;

/// <summary>Resposta 201 de <c>POST /api/cobrancas</c> (dados fictícios).</summary>
public sealed record RespostaDaCobranca(string IdCobranca, string LinhaDigitavel, string QrCode);

/// <summary>
/// <c>POST /api/cobrancas?{botões de caos}</c>: o hub pede o registro de uma cobrança para a fatura.
/// Grava, enfileira os webhooks e só então espera <c>latencia</c> segundos para responder.
/// </summary>
public sealed class PostCobranca(
    RegistroDeCobranca registro,
    IOptions<ConfiguracaoDoBanco> segredos,
    ILogger<PostCobranca> logger)
{
    private static readonly JsonSerializerOptions OpcoesDoCorpo = new(JsonSerializerDefaults.Web);

    // Simplificação de lab: trigger HTTP anônimo (sem function key). Quem autentica o hub é o x-api-key.
    [Function(nameof(PostCobranca))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cobrancas")] HttpRequest requisicao,
        CancellationToken cancellationToken)
    {
        if (!ChaveDeApi.Confere(requisicao.Headers[ChaveDeApi.Header], segredos.Value.BancoApiKey))
        {
            logger.LogWarning("POST /cobrancas recusado: header {Header} ausente ou diferente do setting BancoApiKey.", ChaveDeApi.Header);
            return Responder(StatusCodes.Status401Unauthorized, new { erro = "x-api-key ausente ou inválida" });
        }

        var leitura = BotoesDeCaos.Interpretar(requisicao.QueryString.Value);
        foreach (var aviso in leitura.Avisos)
        {
            logger.LogWarning("Botão de caos ajustado: {Aviso}", aviso);
        }

        PedidoDeCobranca? pedido;
        try
        {
            pedido = await JsonSerializer.DeserializeAsync<PedidoDeCobranca>(requisicao.Body, OpcoesDoCorpo, cancellationToken);
        }
        catch (JsonException ex)
        {
            return Responder(StatusCodes.Status400BadRequest, new { erro = "o corpo não é um JSON válido", detalhes = new[] { ex.Message } });
        }

        if (pedido is null)
        {
            return Responder(StatusCodes.Status400BadRequest, new { erro = "o corpo está vazio" });
        }

        var resultado = await registro.RegistrarAsync(pedido, leitura.Botoes, cancellationToken);
        switch (resultado)
        {
            case ResultadoDoRegistro.PedidoInvalido invalido:
                return Responder(StatusCodes.Status400BadRequest, new { erro = "pedido de cobrança inválido", detalhes = invalido.Erros });

            case ResultadoDoRegistro.FalhaTransienteSimulada falha:
                logger.LogWarning(
                    "Falha transiente simulada (falhaTransiente={Percentual}) para a fatura {FaturaId}: 503 sem gravar nem enfileirar nada.",
                    falha.Percentual, pedido.FaturaId);
                requisicao.HttpContext.Response.Headers.RetryAfter = "1";
                return Responder(StatusCodes.Status503ServiceUnavailable,
                    new { erro = $"falha transiente simulada (falhaTransiente={falha.Percentual}); tente de novo" });

            case ResultadoDoRegistro.Registrada registrada:
                var cobranca = registrada.Cobranca;
                return Responder(StatusCodes.Status201Created,
                    new RespostaDaCobranca(cobranca.IdCobranca, cobranca.LinhaDigitavel, cobranca.QrCode));

            default:
                throw new UnreachableException($"Resultado não tratado: {resultado.GetType().Name}");
        }
    }

    private static JsonResult Responder(int statusCode, object corpo) => new(corpo) { StatusCode = statusCode };
}
