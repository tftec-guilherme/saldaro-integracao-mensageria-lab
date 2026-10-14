using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Saldaro.BancoSimulado;

var builder = FunctionsApplication.CreateBuilder(args);

// ASP.NET Core integration: as Functions HTTP recebem HttpRequest e devolvem IActionResult.
builder.ConfigureFunctionsWebApplication();

// Application Insights direto do worker, sem sampling (no lab, toda requisição precisa aparecer).
// Sem a connection string (rodando local), os logs seguem pelo host e aparecem no console.
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services
        .AddApplicationInsightsTelemetryWorkerService(opcoes => opcoes.EnableAdaptiveSampling = false)
        .ConfigureFunctionsApplicationInsights();

    // O SDK registra uma regra que só deixa passar Warning ou acima; sem ela, os logs Information também chegam.
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

builder.Services.AdicionarBancoSimulado(builder.Configuration);

builder.Build().Run();
