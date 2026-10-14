using Azure.Data.Tables;
using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Functions;
using Saldaro.Hub.Infraestrutura;

namespace Saldaro.Hub.Tests;

/// <summary>
/// Monta o contêiner de DI como o Program.cs monta e cria cada Function, nas duas formas de conexão da seção 5.2.
/// Nada se conecta ao Azure: os clientes do SDK só abrem conexão no primeiro uso.
/// </summary>
public class ComposicaoTests
{
    private static readonly Dictionary<string, string?> ComConnectionString = new()
    {
        ["ServiceBusConnection"] = "Endpoint=sb://sb-saldaro-ficticio.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=ZmljdGljaWE=",
        ["TablesConnection"] = "UseDevelopmentStorage=true",
        ["EventGridTopicEndpoint"] = "https://evgt-saldaro-faturas-ficticio.brazilsouth-1.eventgrid.azure.net/api/events",
        ["EventGridTopicKey"] = "ZmljdGljaWE=",
        ["BancoBaseUrl"] = "http://localhost:7072/api",
        ["Cenario:AsyncRequestReply"] = "true",
    };

    private static readonly Dictionary<string, string?> ComIdentidade = new()
    {
        ["ServiceBusConnection:fullyQualifiedNamespace"] = "sb-saldaro-ficticio.servicebus.windows.net",
        ["TablesConnection:serviceUri"] = "https://stsaldaroficticio.table.core.windows.net",
        ["EventGridTopicEndpoint"] = "https://evgt-saldaro-faturas-ficticio.brazilsouth-1.eventgrid.azure.net/api/events",
        ["BancoBaseUrl"] = "https://func-saldaro-banco-ficticio.azurewebsites.net/api",
    };

    private static ServiceProvider Montar(Dictionary<string, string?> settings)
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuracao);
        services.AdicionarSaldaroHub(configuracao, appInsightsConfigurado: false);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public static TheoryData<string> FormasDeConexao => [nameof(ComConnectionString), nameof(ComIdentidade)];

    [Theory]
    [MemberData(nameof(FormasDeConexao))]
    public async Task Todas_as_Functions_sao_criadas_pelo_conteiner(string forma)
    {
        // ServiceBusClient só implementa IAsyncDisposable: o contêiner precisa de DisposeAsync.
        await using var provedor = Montar(forma == nameof(ComIdentidade) ? ComIdentidade : ComConnectionString);
        await using var escopo = provedor.CreateAsyncScope();

        Type[] functions =
        [
            typeof(PostFatura), typeof(GetFatura), typeof(GetFaturaStatus), typeof(EmitirCobranca), typeof(WebhookBanco),
            typeof(ProcessarConfirmacaoSemSessao), typeof(ProcessarConfirmacaoComSessao), typeof(NotificarParceiro),
            typeof(Conciliacao), typeof(Saude),
        ];
        Assert.All(functions, tipo => Assert.NotNull(ActivatorUtilities.CreateInstance(escopo.ServiceProvider, tipo)));

        Assert.NotNull(escopo.ServiceProvider.GetRequiredService<ServiceBusClient>());
        Assert.NotNull(escopo.ServiceProvider.GetRequiredService<TableServiceClient>());
        Assert.NotNull(escopo.ServiceProvider.GetRequiredService<EventGridPublisherClient>());
        Assert.IsType<TelemetriaNoLog>(escopo.ServiceProvider.GetRequiredService<ITelemetria>());
    }

    [Fact]
    public async Task Cliente_do_banco_tem_timeout_de_120_segundos_e_a_url_base()
    {
        await using var provedor = Montar(ComConnectionString);
        var fabrica = provedor.GetRequiredService<IHttpClientFactory>();

        var http = fabrica.CreateClient(typeof(IBancoClient).Name);

        Assert.Equal(TimeSpan.FromSeconds(120), http.Timeout);
        Assert.Equal(new Uri("http://localhost:7072/api/"), http.BaseAddress);
    }

    [Fact]
    public async Task Toggles_sao_lidos_da_secao_Cenario()
    {
        await using var provedor = Montar(ComConnectionString);

        var cenario = provedor.GetRequiredService<Microsoft.Extensions.Options.IOptions<CenarioOptions>>().Value;

        Assert.True(cenario.AsyncRequestReply);
        Assert.False(cenario.Inbox);
    }

    [Theory]
    [InlineData("ServiceBusConnection", "ServiceBusConnection__fullyQualifiedNamespace")]
    [InlineData("TablesConnection", "TablesConnection__serviceUri")]
    [InlineData("EventGridTopicEndpoint", "EventGridTopicEndpoint")]
    public void Sem_nenhuma_das_formas_de_conexao_a_inicializacao_falha_com_mensagem_clara(string setting, string trecho)
    {
        var settings = new Dictionary<string, string?>(ComConnectionString);
        settings.Remove(setting);

        var erro = Assert.Throws<InvalidOperationException>(() => Montar(settings));

        Assert.Contains(trecho, erro.Message);
    }
}
