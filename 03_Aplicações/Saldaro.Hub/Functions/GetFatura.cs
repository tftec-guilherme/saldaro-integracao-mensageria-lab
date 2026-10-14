using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Functions;

/// <summary><c>GET /api/faturas/{id}</c>: a fatura inteira (200) ou 404.</summary>
public sealed class GetFatura(IRepositorioFaturas faturas)
{
    // Simplificação de laboratório: HTTP trigger anônimo (AuthorizationLevel.Anonymous). O ERP e os guias leem a
    // fatura pelo APIM; em produção o APIM autenticaria quem chama e o backend aceitaria só o APIM.
    [Function(nameof(GetFatura))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "faturas/{id}")] HttpRequest req,
        string id,
        CancellationToken cancellationToken)
    {
        if (!ChaveDeTabela.EhValida(id))
        {
            return new NotFoundResult();
        }

        var fatura = await faturas.ObterAsync(id, cancellationToken);
        return fatura is null ? new NotFoundResult() : new OkObjectResult(FaturaResposta.De(fatura));
    }
}
