using System.Text.Encodings.Web;
using System.Text.Unicode;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Saldaro.BancoSimulado.Dominio;
using Saldaro.BancoSimulado.Infraestrutura;
using Saldaro.BancoSimulado.Servicos;

namespace Saldaro.BancoSimulado;

/// <summary>Registro por DI de tudo que o banco simulado usa.</summary>
public static class RegistroDeServicos
{
    /// <summary>Registra os serviços do banco simulado. Usado pelo Program.cs e pelo teste de DI.</summary>
    public static IServiceCollection AdicionarBancoSimulado(this IServiceCollection services, IConfiguration configuracao)
    {
        // Respostas JSON com acentos legíveis (sem á), mantendo o escape dos caracteres de HTML.
        services.Configure<JsonOptions>(opcoes =>
            opcoes.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All));

        // BancoApiKey e WebhookSecret: app settings de mesmo nome (padrão Options).
        services.AddOptions<ConfiguracaoDoBanco>().Bind(configuracao);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISorteio, SorteioAleatorio>();
        services.AddSingleton<IEspera, EsperaReal>();
        services.AddSingleton<IRepositorioDeCobrancas>(_ =>
            new RepositorioDeCobrancas(new TableServiceClient(SettingObrigatorio(configuracao, "TablesConnection"))));
        services.AddSingleton<IFilaDePagamentos>(_ =>
            new FilaDePagamentos(FilaDePagamentos.CriarCliente(SettingObrigatorio(configuracao, FilaDePagamentos.SettingDaConexao))));
        services.AddSingleton<RegistroDeCobranca>();
        services.AddHttpClient<RemetenteDeWebhook>(http => http.Timeout = TimeSpan.FromSeconds(30));

        return services;
    }

    private static string SettingObrigatorio(IConfiguration configuracao, string nome) =>
        configuracao[nome] is { Length: > 0 } valor
            ? valor
            : throw new InvalidOperationException($"O app setting {nome} não está configurado.");
}
