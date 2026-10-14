using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saldaro.BancoSimulado.Functions;

namespace Saldaro.BancoSimulado.Tests;

/// <summary>Monta o mesmo contêiner do Program.cs e cria cada Function, sem Azure e sem host.</summary>
public class RegistroDeServicosTests
{
    private static ServiceProvider CriarProvedor(Dictionary<string, string?> settings)
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var servicos = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuracao)
            .AdicionarBancoSimulado(configuracao);
        return servicos.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static Dictionary<string, string?> SettingsCompletos() => new()
    {
        ["AzureWebJobsStorage"] = "UseDevelopmentStorage=true",
        ["TablesConnection"] = "UseDevelopmentStorage=true",
        ["BancoApiKey"] = "chave-de-teste",
        ["WebhookSecret"] = VetorCompartilhado.Segredo,
    };

    [Theory]
    [InlineData(typeof(PostCobranca))]
    [InlineData(typeof(EnviarWebhook))]
    [InlineData(typeof(GetCobrancasPorFatura))]
    [InlineData(typeof(Saude))]
    public void Cada_Function_e_criada_pelo_conteiner(Type tipoDaFunction)
    {
        using var provedor = CriarProvedor(SettingsCompletos());
        using var escopo = provedor.CreateScope();

        Assert.NotNull(ActivatorUtilities.CreateInstance(escopo.ServiceProvider, tipoDaFunction));
    }

    [Fact]
    public void ConfiguracaoDoBanco_le_os_settings_de_mesmo_nome()
    {
        using var provedor = CriarProvedor(SettingsCompletos());

        var segredos = provedor.GetRequiredService<IOptions<ConfiguracaoDoBanco>>().Value;

        Assert.Equal("chave-de-teste", segredos.BancoApiKey);
        Assert.Equal(VetorCompartilhado.Segredo, segredos.WebhookSecret);
    }

    [Fact]
    public void Sem_TablesConnection_falha_com_mensagem_clara()
    {
        var settings = SettingsCompletos();
        settings.Remove("TablesConnection");
        using var provedor = CriarProvedor(settings);
        using var escopo = provedor.CreateScope();

        var erro = Assert.Throws<InvalidOperationException>(() => ActivatorUtilities.CreateInstance<GetCobrancasPorFatura>(escopo.ServiceProvider));

        Assert.Contains("TablesConnection", erro.Message);
    }
}
