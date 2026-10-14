using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Saldaro.Hub.Configuracao;

// Saldaro Hub: Azure Functions .NET 10, modelo isolated worker (o código roda num processo separado do host).
var builder = FunctionsApplication.CreateBuilder(args);

// Integração com ASP.NET Core: os HTTP triggers recebem HttpRequest e devolvem IActionResult.
builder.ConfigureFunctionsWebApplication();

// Application Insights direto do worker (logs, dependências e os eventos customizados via TelemetryClient), só
// quando o app setting existe: localmente e nos testes, sem ele, os eventos customizados vão para o log.
var appInsightsConfigurado = !string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
if (appInsightsConfigurado)
{
    builder.Services
        .AddApplicationInsightsTelemetryWorkerService(opcoes =>
        {
            // Sampling desligado no laboratório (também no host.json): com sampling, parte dos eventos some e
            // as contagens por KQL (baixas duplicadas, 429, heartbeats) deixam de bater.
            opcoes.EnableAdaptiveSampling = false;
        })
        .ConfigureFunctionsApplicationInsights();

    // O SDK do Application Insights registra uma regra que só envia Warning ou acima. Ela é removida para os logs
    // Information do worker também chegarem (o host.json controla só os logs do host, não os do worker).
    builder.Logging.Services.Configure<LoggerFilterOptions>(opcoes =>
    {
        var regraPadrao = opcoes.Rules.FirstOrDefault(regra =>
            regra.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
        if (regraPadrao is not null)
        {
            opcoes.Rules.Remove(regraPadrao);
        }
    });
}

builder.Services.AdicionarSaldaroHub(builder.Configuration, appInsightsConfigurado);

builder.Build().Run();
