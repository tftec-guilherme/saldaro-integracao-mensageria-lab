using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Saldaro.BancoSimulado.Servicos;

namespace Saldaro.BancoSimulado.Functions;

/// <summary>
/// <c>GET /api/cobrancas/{faturaId}</c>: quantas cobranças o banco registrou para a fatura.
/// A prova do Incidente 1 é <c>quantidade = 1</c> depois do conserto.
/// </summary>
public sealed class GetCobrancasPorFatura(IRepositorioDeCobrancas repositorio)
{
    // Simplificação de lab: GET anônimo, sem x-api-key, porque os guias chamam direto como prova
    // (até no navegador). Só o POST /api/cobrancas exige x-api-key.
    [Function(nameof(GetCobrancasPorFatura))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cobrancas/{faturaId}")] HttpRequest requisicao,
        string faturaId,
        CancellationToken cancellationToken)
    {
        var cobrancas = await repositorio.ListarPorFaturaAsync(faturaId, cancellationToken);

        return new JsonResult(new
        {
            faturaId,
            quantidade = cobrancas.Count,
            cobrancas = cobrancas.Select(c => new
            {
                c.IdCobranca,
                c.ClienteId,
                c.Valor,
                c.Vencimento,
                c.PagadorCnpj,
                c.LinhaDigitavel,
                c.QrCode,
                c.WebhookUrl,
                c.Caos,
                c.CriadaEm,
            }),
        });
    }
}
