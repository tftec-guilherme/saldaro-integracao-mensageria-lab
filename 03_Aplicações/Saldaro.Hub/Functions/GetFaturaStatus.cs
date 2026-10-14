using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Functions;

/// <summary>
/// <c>GET /api/faturas/{id}/status</c>: o endereço que o 202 devolve no <c>Location</c>.
/// 200 <c>{ faturaId, status }</c> enquanto processa; 303 para a fatura quando já baixou; 404 se não existe.
/// </summary>
public sealed class GetFaturaStatus(IRepositorioFaturas faturas, IOptions<HubOptions> hub)
{
    // Simplificação de laboratório: HTTP trigger anônimo (AuthorizationLevel.Anonymous). É o Location do 202, que o
    // ERP consulta pelo APIM; em produção o APIM autenticaria quem chama e o backend aceitaria só o APIM.
    [Function(nameof(GetFaturaStatus))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "faturas/{id}/status")] HttpRequest req,
        string id,
        CancellationToken cancellationToken)
    {
        if (!ChaveDeTabela.EhValida(id))
        {
            return new NotFoundResult();
        }

        // Point read (PK + RK): consulta barata, porque o cliente vai chamar este endpoint várias vezes.
        var fatura = await faturas.ObterAsync(id, cancellationToken);
        if (fatura is null)
        {
            return new NotFoundResult();
        }

        if (StatusFatura.JaBaixou(fatura.Status))
        {
            // 303 See Other: "o trabalho terminou; o resultado está neste outro endereço".
            req.HttpContext.Response.Headers.Location = $"{UrlsPublicas.Base(hub.Value, req)}/faturas/{fatura.FaturaId}";
            return new StatusCodeResult(StatusCodes.Status303SeeOther);
        }

        return new OkObjectResult(new { faturaId = fatura.FaturaId, status = fatura.Status });
    }
}
