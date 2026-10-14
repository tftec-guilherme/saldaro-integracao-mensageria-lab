using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Functions;

/// <summary>
/// <c>POST /api/faturas</c>: o ERP do cliente envia uma fatura.
/// A decisão síncrono × assíncrono (toggle <c>Cenario:AsyncRequestReply</c>) está em <see cref="ServicoDeFaturas"/>.
/// </summary>
public sealed class PostFatura(ServicoDeFaturas servico, IOptions<HubOptions> hub)
{
    /// <summary>Intervalo sugerido ao cliente entre duas consultas de status.</summary>
    public const string RetryAfterSegundos = "5";

    // Simplificação de laboratório: HTTP triggers anônimos (AuthorizationLevel.Anonymous). A porta de entrada
    // pública é o APIM; em produção o backend aceitaria só o APIM (chave de função, Entra ID ou rede privada).
    [Function(nameof(PostFatura))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "faturas")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var clienteId = req.Headers[Cabecalhos.ClienteId].ToString().Trim();
        if (!ChaveDeTabela.EhValida(clienteId))
        {
            return Requisicao400("Header x-cliente-id é obrigatório.",
                "Envie o id da empresa cliente no header x-cliente-id (ex.: cliente-a); até 128 caracteres, sem / \\ # ?.");
        }

        var idempotencyKey = req.Headers[Cabecalhos.IdempotencyKey].ToString().Trim();
        if (idempotencyKey.Length > 0 && !ChaveDeTabela.EhValida(idempotencyKey))
        {
            return Requisicao400("Idempotency-Key inválida.", "Até 128 caracteres, sem / \\ # ?.");
        }

        NovaFatura? pedido;
        try
        {
            pedido = await JsonSerializer.DeserializeAsync<NovaFatura>(req.Body, Json.Opcoes, cancellationToken);
        }
        catch (JsonException ex)
        {
            return Requisicao400("Corpo inválido: não é o JSON de uma fatura.", ex.Message);
        }

        if (!NovaFatura.EhValida(pedido, out var problemas))
        {
            return Requisicao400("Fatura inválida.", [.. problemas]);
        }

        var resultado = await servico.ReceberAsync(pedido, clienteId, idempotencyKey.Length > 0 ? idempotencyKey : null, cancellationToken);
        var urlBase = UrlsPublicas.Base(hub.Value, req);

        switch (resultado)
        {
            case ResultadoDoRecebimento.CriadaSincrona criada:
                // Baseline: 201 só depois que o banco respondeu (o ERP esperou o banco inteiro).
                return new CreatedResult(
                    $"{urlBase}/faturas/{criada.FaturaId}",
                    new { faturaId = criada.FaturaId, status = criada.Status, idCobranca = criada.IdCobranca });

            case ResultadoDoRecebimento.FalhaNoBanco falha:
                return new ObjectResult(new { faturaId = falha.FaturaId, status = falha.Status, erro = falha.Erro })
                {
                    StatusCode = StatusCodes.Status502BadGateway,
                };

            case ResultadoDoRecebimento.AceitaAssincrona or ResultadoDoRecebimento.ChaveRepetida:
                // CONCEITO: 202 assíncrono (Asynchronous Request-Reply). "Recebi e vou processar": a resposta sai
                // em milissegundos com o endereço do status (Location) e de quanto em quanto tempo consultar
                // (Retry-After). O trabalho lento segue pela fila, longe do timeout do APIM.
                req.HttpContext.Response.Headers.RetryAfter = RetryAfterSegundos;
                return new AcceptedResult(
                    $"{urlBase}/faturas/{resultado.FaturaId}/status",
                    new { faturaId = resultado.FaturaId, status = resultado.Status });

            default:
                throw new UnreachableException($"Desfecho não tratado: {resultado.GetType().Name}");
        }
    }

    /// <summary>400 no formato <c>{ erro, detalhes }</c>: o resumo e a lista do que corrigir.</summary>
    private static BadRequestObjectResult Requisicao400(string erro, params string[] detalhes) =>
        new(new { erro, detalhes });
}
