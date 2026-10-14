using Azure;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Functions;
using Saldaro.Hub.Infraestrutura;
using Saldaro.Hub.Infraestrutura.Armazenamento;

namespace Saldaro.Hub.Configuracao;

/// <summary>Registro por DI de tudo que o hub usa.</summary>
public static class RegistroDeServicos
{
    /// <summary>Timeout do cliente HTTP do banco. Quem corta primeiro é o APIM, aos 30 s.</summary>
    public static readonly TimeSpan TimeoutDoBanco = TimeSpan.FromSeconds(120);

    /// <summary>Timeout do callback ao parceiro (<c>NotificarParceiro</c>).</summary>
    public static readonly TimeSpan TimeoutDoCallback = TimeSpan.FromSeconds(30);

    /// <param name="appInsightsConfigurado">
    /// <c>true</c> quando <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> existe e o <c>TelemetryClient</c> foi registrado;
    /// sem ele, os eventos customizados vão para o log (<see cref="TelemetriaNoLog"/>).
    /// </param>
    public static IServiceCollection AdicionarSaldaroHub(this IServiceCollection services, IConfiguration configuracao, bool appInsightsConfigurado)
    {
        // Padrão Options: Cenario__AsyncRequestReply (app setting) vira Cenario:AsyncRequestReply (configuração).
        services.AddOptions<CenarioOptions>().Bind(configuracao.GetSection(CenarioOptions.Secao));
        services.AddOptions<HubOptions>().Bind(configuracao);
        services.AddSingleton(TimeProvider.System);

        AdicionarClientesAzure(services, configuracao);

        services.AddSingleton<FabricaDeTabelas>();
        services.AddSingleton<IRepositorioFaturas, RepositorioFaturasTabela>();
        services.AddSingleton<IRepositorioInbox, RepositorioInboxTabela>();
        services.AddSingleton<IRepositorioIdempotencia, RepositorioIdempotenciaTabela>();
        services.AddSingleton<IRepositorioConciliacao, RepositorioConciliacaoTabela>();
        services.AddSingleton<IMensageria, MensageriaServiceBus>();
        services.AddSingleton<IPublicadorDeEventos, PublicadorEventGrid>();
        if (appInsightsConfigurado)
        {
            services.AddSingleton<ITelemetria, TelemetriaApplicationInsights>();
        }
        else
        {
            services.AddSingleton<ITelemetria, TelemetriaNoLog>();
        }

        services.AddHttpClient<IBancoClient, BancoClientHttp>((provedor, http) =>
        {
            var hub = provedor.GetRequiredService<IOptions<HubOptions>>().Value;
            if (Uri.TryCreate(hub.BancoBaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseDoBanco))
            {
                http.BaseAddress = baseDoBanco;
            }

            http.Timeout = TimeoutDoBanco;
        });
        services.AddHttpClient(NotificarParceiro.ClienteHttpDoCallback, http => http.Timeout = TimeoutDoCallback);

        services.AddTransient<ServicoDeFaturas>();
        services.AddTransient<ProcessadorDeBaixa>();
        services.AddTransient<ServicoDeConciliacao>();
        return services;
    }

    /// <summary>
    /// Registra <c>ServiceBusClient</c>, <c>TableServiceClient</c> e <c>EventGridPublisherClient</c>. Cada um aceita
    /// as duas formas da seção 5.2: connection string (ou chave) quando o setting simples existe, ou só o endpoint
    /// + <see cref="DefaultAzureCredential"/> quando existe apenas a forma com sufixo.
    /// </summary>
    /// <remarks>
    /// CONCEITO: identidade gerenciada. Com <c>identidadeGerenciada = true</c> o Bicep remove as connection strings
    /// e as chaves dos settings e dá à identidade da Function App os papéis de RBAC necessários. O código não muda:
    /// o <c>DefaultAzureCredential</c> encontra a identidade gerenciada no Azure (e o seu <c>az login</c> na
    /// máquina local). Não há segredo para expirar às 3h da manhã.
    /// </remarks>
    private static void AdicionarClientesAzure(IServiceCollection services, IConfiguration configuracao)
    {
        var serviceBusConexao = configuracao[Conexoes.ServiceBus];
        var serviceBusNamespace = configuracao[$"{Conexoes.ServiceBus}:fullyQualifiedNamespace"];
        var tabelasConexao = configuracao[Conexoes.Tabelas];
        var tabelasUri = configuracao[$"{Conexoes.Tabelas}:serviceUri"];
        var eventGridEndpoint = configuracao[Conexoes.EventGridEndpoint];
        var eventGridChave = configuracao[Conexoes.EventGridChave];

        services.AddAzureClients(clientes =>
        {
            clientes.UseCredential(new DefaultAzureCredential());

            if (!string.IsNullOrWhiteSpace(serviceBusConexao))
            {
                clientes.AddServiceBusClient(serviceBusConexao);
            }
            else if (!string.IsNullOrWhiteSpace(serviceBusNamespace))
            {
                clientes.AddServiceBusClientWithNamespace(serviceBusNamespace);
            }
            else
            {
                throw new InvalidOperationException(
                    "Configure ServiceBusConnection (connection string) ou ServiceBusConnection__fullyQualifiedNamespace (identidade gerenciada).");
            }

            if (!string.IsNullOrWhiteSpace(tabelasConexao))
            {
                clientes.AddTableServiceClient(tabelasConexao);
            }
            else if (Uri.TryCreate(tabelasUri, UriKind.Absolute, out var uriDasTabelas))
            {
                clientes.AddTableServiceClient(uriDasTabelas);
            }
            else
            {
                throw new InvalidOperationException(
                    "Configure TablesConnection (connection string) ou TablesConnection__serviceUri (identidade gerenciada).");
            }

            if (!Uri.TryCreate(eventGridEndpoint, UriKind.Absolute, out var uriDoTopico))
            {
                throw new InvalidOperationException("Configure EventGridTopicEndpoint com o endpoint do tópico do Event Grid.");
            }

            if (!string.IsNullOrWhiteSpace(eventGridChave))
            {
                clientes.AddEventGridPublisherClient(uriDoTopico, new AzureKeyCredential(eventGridChave));
            }
            else
            {
                clientes.AddEventGridPublisherClient(uriDoTopico);
            }
        });
    }
}
