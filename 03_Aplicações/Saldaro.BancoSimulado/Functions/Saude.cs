using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Saldaro.BancoSimulado.Functions;

/// <summary><c>GET /api/saude</c>: 200 com a versão e quais settings estão preenchidos (nunca os valores).</summary>
public sealed class Saude(IOptions<ConfiguracaoDoBanco> segredos, IConfiguration configuracao)
{
    private static readonly string Versao =
        typeof(Saude).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "desconhecida";

    // Simplificação de lab: GET anônimo, sem x-api-key, porque os guias chamam direto como prova.
    // Só o POST /api/cobrancas exige x-api-key.
    [Function(nameof(Saude))]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "saude")] HttpRequest requisicao)
    {
        return new JsonResult(new
        {
            servico = "Saldaro.BancoSimulado",
            status = "ok",
            versao = Versao,
            settingsPreenchidos = new
            {
                bancoApiKey = !string.IsNullOrEmpty(segredos.Value.BancoApiKey),
                webhookSecret = !string.IsNullOrEmpty(segredos.Value.WebhookSecret),
                tablesConnection = !string.IsNullOrEmpty(configuracao["TablesConnection"]),
            },
        });
    }
}
