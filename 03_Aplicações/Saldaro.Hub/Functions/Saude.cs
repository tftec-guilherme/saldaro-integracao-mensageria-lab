using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Configuracao;

namespace Saldaro.Hub.Functions;

/// <summary>
/// <c>GET /api/saude</c>: versão e toggles em vigor. Serve para conferir, depois de um deploy, em que estado
/// de cenário o hub está (é o que o Bicep gravou nos app settings):
/// <c>{ "versao": "1.0.0", "toggles": { "AsyncRequestReply": false, ..., "WorkerComSessao": false, "IdentidadeGerenciada": false } }</c>.
/// </summary>
public sealed class Saude(IOptions<CenarioOptions> cenario, IConfiguration configuracao)
{
    public static readonly string Versao =
        typeof(Saude).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "0.0.0";

    // Simplificação de laboratório: HTTP trigger anônimo (AuthorizationLevel.Anonymous) e fora do APIM, para os
    // guias e o smoke-test.ps1 lerem os toggles direto; em produção, a rota ficaria fechada com o resto do backend.
    [Function(nameof(Saude))]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "saude")] HttpRequest req)
    {
        var c = cenario.Value;

        // Dicionário (e não objeto) para as chaves saírem em PascalCase, como os nomes dos settings Cenario__X.
        var toggles = new Dictionary<string, bool>
        {
            [nameof(CenarioOptions.AsyncRequestReply)] = c.AsyncRequestReply,
            [nameof(CenarioOptions.WebhookHmac)] = c.WebhookHmac,
            [nameof(CenarioOptions.Inbox)] = c.Inbox,
            [nameof(CenarioOptions.DlqComMotivo)] = c.DlqComMotivo,
            [nameof(CenarioOptions.RetryAgendado)] = c.RetryAgendado,
            [nameof(CenarioOptions.Heartbeat)] = c.Heartbeat,

            // Inferidos de outros settings (não são Cenario__X):
            ["WorkerComSessao"] = !FuncaoDesabilitada(nameof(ProcessarConfirmacaoComSessao)),
            ["IdentidadeGerenciada"] = string.IsNullOrWhiteSpace(configuracao[Conexoes.ServiceBus])
                && !string.IsNullOrWhiteSpace(configuracao[$"{Conexoes.ServiceBus}:fullyQualifiedNamespace"]),
        };

        return new OkObjectResult(new { versao = Versao, toggles });
    }

    // AzureWebJobs.<Function>.Disabled; em alguns ambientes Linux o ponto chega como sublinhado.
    private bool FuncaoDesabilitada(string funcao) =>
        bool.TryParse(configuracao[$"AzureWebJobs.{funcao}.Disabled"] ?? configuracao[$"AzureWebJobs_{funcao}_Disabled"], out var desabilitada)
        && desabilitada;
}
