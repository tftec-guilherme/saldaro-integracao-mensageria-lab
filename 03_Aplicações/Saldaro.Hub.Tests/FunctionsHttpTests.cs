using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;
using Saldaro.Hub.Functions;
using Saldaro.Testes;

namespace Saldaro.Hub.Tests;

/// <summary>Status codes e headers das Functions HTTP (seção 5.1).</summary>
public class FunctionsHttpTests
{
    private const string UrlBase = "https://apim-saldaro-teste.azure-api.net/saldaro";
    private static readonly string CorpoValido =
        "{\"valor\":1250.00,\"vencimento\":\"2026-11-10\",\"pagadorCnpj\":\"" + CnpjDeTeste.Gerar() +
        "\",\"referenciaErp\":\"ERP-0001\",\"callbackUrl\":null,\"caos\":\"latencia=45&duplicar=3\"}";

    private readonly RepositorioFaturasEmMemoria _faturas = new();
    private readonly BancoEmMemoria _banco = new();
    private readonly MensageriaEmMemoria _mensageria = new();
    private readonly RelogioControlado _relogio = new(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));

    private PostFatura PostFatura(bool asyncRequestReply)
    {
        var servico = new ServicoDeFaturas(
            _faturas, new RepositorioIdempotenciaEmMemoria(), _mensageria, _banco, new TelemetriaEmMemoria(),
            Configuracoes.Cenario(asyncRequestReply: asyncRequestReply), Configuracoes.Hub(), _relogio,
            NullLogger<ServicoDeFaturas>.Instance);
        return new PostFatura(servico, Configuracoes.Hub());
    }

    private static HttpRequest Requisicao(string metodo, string? corpo = null, string? clienteId = "cliente-a", string? chave = null)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Method = metodo;
        contexto.Request.Scheme = "https";
        contexto.Request.Host = new HostString("func-saldaro-hub-teste.azurewebsites.net");
        contexto.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(corpo ?? string.Empty));
        if (clienteId is not null)
        {
            contexto.Request.Headers[Cabecalhos.ClienteId] = clienteId;
        }

        if (chave is not null)
        {
            contexto.Request.Headers[Cabecalhos.IdempotencyKey] = chave;
        }

        return contexto.Request;
    }

    private static JsonElement ComoJson(object? valor) =>
        JsonSerializer.SerializeToElement(valor, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static (string Erro, string[] Detalhes) Erro400(IActionResult resultado)
    {
        var corpo = ComoJson(Assert.IsType<BadRequestObjectResult>(resultado).Value);
        return (corpo.GetProperty("erro").GetString()!, corpo.GetProperty("detalhes").EnumerateArray().Select(d => d.GetString()!).ToArray());
    }

    [Fact]
    public async Task PostFatura_sem_x_cliente_id_responde_400()
    {
        var resultado = await PostFatura(asyncRequestReply: true).Run(Requisicao("POST", CorpoValido, clienteId: null), CancellationToken.None);

        var (erro, detalhes) = Erro400(resultado);
        Assert.Contains("x-cliente-id", erro);
        Assert.NotEmpty(detalhes);
        Assert.Empty(_mensageria.Enviadas);
    }

    [Fact]
    public async Task PostFatura_com_corpo_vazio_responde_400_com_erro_e_detalhes()
    {
        // Passo opcional do guia 3.1: POST com "{}".
        var resultado = await PostFatura(asyncRequestReply: true).Run(Requisicao("POST", "{}"), CancellationToken.None);

        var (erro, detalhes) = Erro400(resultado);
        Assert.Equal("Fatura inválida.", erro);
        Assert.Equal(2, detalhes.Length);
        Assert.Contains(detalhes, d => d.Contains("'valor'", StringComparison.Ordinal));
        Assert.Contains(detalhes, d => d.Contains("'vencimento'", StringComparison.Ordinal));
        Assert.Equal(0, _faturas.Quantidade);
    }

    [Theory]
    [InlineData("{\"vencimento\":\"2026-11-10\"}", "valor")]
    [InlineData("{\"valor\":\"1.250,00\",\"vencimento\":\"2026-11-10\"}", "$.valor")]
    [InlineData("{\"valor\":1250.00}", "vencimento")]
    [InlineData("{\"valor\":1250.00,\"vencimento\":\"2026-11-10\",\"callbackUrl\":\"ftp://x\"}", "callbackUrl")]
    [InlineData("não é json", "Corpo inválido")]
    [InlineData("", "Corpo inválido")]
    public async Task PostFatura_com_corpo_invalido_responde_400(string corpo, string trecho)
    {
        var resultado = await PostFatura(asyncRequestReply: true).Run(Requisicao("POST", corpo), CancellationToken.None);

        var (erro, detalhes) = Erro400(resultado);
        Assert.Contains(trecho, erro + " " + string.Join(" ", detalhes));
    }

    [Fact]
    public async Task PostFatura_assincrono_responde_202_com_Location_e_Retry_After()
    {
        var requisicao = Requisicao("POST", CorpoValido, chave: "chave-0001");

        var resultado = await PostFatura(asyncRequestReply: true).Run(requisicao, CancellationToken.None);

        var aceito = Assert.IsType<AcceptedResult>(resultado);
        var corpo = ComoJson(aceito.Value);
        var faturaId = corpo.GetProperty("faturaId").GetString();
        Assert.Equal(StatusFatura.Recebida, corpo.GetProperty("status").GetString());
        Assert.Equal($"{UrlBase}/faturas/{faturaId}/status", aceito.Location);
        Assert.Equal("5", requisicao.HttpContext.Response.Headers.RetryAfter.ToString());
        Assert.Empty(_banco.Chamadas);
        Assert.Single(_mensageria.Na(Filas.FaturasRecebidas));
    }

    [Fact]
    public async Task PostFatura_assincrono_com_a_mesma_chave_devolve_o_mesmo_faturaId()
    {
        var funcao = PostFatura(asyncRequestReply: true);

        var primeiro = Assert.IsType<AcceptedResult>(await funcao.Run(Requisicao("POST", CorpoValido, chave: "chave-0001"), CancellationToken.None));
        var reenvio = Assert.IsType<AcceptedResult>(await funcao.Run(Requisicao("POST", CorpoValido, chave: "chave-0001"), CancellationToken.None));

        Assert.Equal(ComoJson(primeiro.Value).GetProperty("faturaId").GetString(), ComoJson(reenvio.Value).GetProperty("faturaId").GetString());
        Assert.Single(_mensageria.Na(Filas.FaturasRecebidas));
    }

    [Fact]
    public async Task PostFatura_baseline_responde_201_com_a_cobranca()
    {
        var resultado = await PostFatura(asyncRequestReply: false).Run(Requisicao("POST", CorpoValido), CancellationToken.None);

        var criado = Assert.IsType<CreatedResult>(resultado);
        var corpo = ComoJson(criado.Value);
        var faturaId = corpo.GetProperty("faturaId").GetString();
        Assert.Equal(StatusFatura.CobrancaEmitida, corpo.GetProperty("status").GetString());
        Assert.StartsWith("COB-", corpo.GetProperty("idCobranca").GetString());
        Assert.Equal($"{UrlBase}/faturas/{faturaId}", criado.Location);
        Assert.Single(_banco.Chamadas);
    }

    [Fact]
    public async Task PostFatura_baseline_com_banco_fora_responde_502()
    {
        _banco.Falha = new BancoIndisponivelException("503");

        var resultado = await PostFatura(asyncRequestReply: false).Run(Requisicao("POST", CorpoValido), CancellationToken.None);

        Assert.Equal(StatusCodes.Status502BadGateway, Assert.IsType<ObjectResult>(resultado).StatusCode);
    }

    [Theory]
    [InlineData(StatusFatura.Recebida)]
    [InlineData(StatusFatura.CobrancaEmitida)]
    [InlineData(StatusFatura.Registrada)]
    public async Task GetFaturaStatus_responde_200_enquanto_nao_baixou(string status)
    {
        await SemearAsync("fatura-1", status);
        var funcao = new GetFaturaStatus(_faturas, Configuracoes.Hub());

        var resultado = await funcao.Run(Requisicao("GET"), "fatura-1", CancellationToken.None);

        var corpo = ComoJson(Assert.IsType<OkObjectResult>(resultado).Value);
        Assert.Equal("fatura-1", corpo.GetProperty("faturaId").GetString());
        Assert.Equal(status, corpo.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData(StatusFatura.Baixada)]
    [InlineData(StatusFatura.Notificada)]
    public async Task GetFaturaStatus_responde_303_para_a_fatura_quando_baixou(string status)
    {
        await SemearAsync("fatura-1", status);
        var requisicao = Requisicao("GET");

        var resultado = await new GetFaturaStatus(_faturas, Configuracoes.Hub()).Run(requisicao, "fatura-1", CancellationToken.None);

        Assert.Equal(StatusCodes.Status303SeeOther, Assert.IsType<StatusCodeResult>(resultado).StatusCode);
        Assert.Equal($"{UrlBase}/faturas/fatura-1", requisicao.HttpContext.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Sonda_do_ERP_e_do_smoke_test_com_GUID_zero_responde_404()
    {
        var resultado = await new GetFaturaStatus(_faturas, Configuracoes.Hub())
            .Run(Requisicao("GET"), "00000000-0000-0000-0000-000000000000", CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task GetFaturaStatus_e_GetFatura_respondem_404_para_fatura_inexistente()
    {
        Assert.IsType<NotFoundResult>(await new GetFaturaStatus(_faturas, Configuracoes.Hub()).Run(Requisicao("GET"), "nao-existe", CancellationToken.None));
        Assert.IsType<NotFoundResult>(await new GetFatura(_faturas).Run(Requisicao("GET"), "nao-existe", CancellationToken.None));
    }

    [Fact]
    public async Task GetFatura_devolve_todas_as_colunas_da_fatura()
    {
        await SemearAsync("fatura-1", StatusFatura.Baixada);

        var resultado = await new GetFatura(_faturas).Run(Requisicao("GET"), "fatura-1", CancellationToken.None);

        var corpo = ComoJson(Assert.IsType<OkObjectResult>(resultado).Value);
        string[] campos =
        [
            "faturaId", "clienteId", "valor", "vencimento", "pagadorCnpj", "referenciaErp", "status", "idCobranca",
            "quantidadeBaixas", "baixadaEm", "callbackUrl", "caos", "criadaEm", "atualizadaEm",
        ];
        Assert.All(campos, campo => Assert.True(corpo.TryGetProperty(campo, out _), $"faltou {campo}"));
        Assert.Equal(1, corpo.GetProperty("quantidadeBaixas").GetInt32());
    }

    [Fact]
    public void Saude_mostra_versao_e_toggles()
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServiceBusConnection:fullyQualifiedNamespace"] = "sb-saldaro-teste.servicebus.windows.net",
            ["AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled"] = "false",
            ["AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled"] = "true",
        }).Build();

        var resultado = new Saude(Configuracoes.Cenario(asyncRequestReply: true, inbox: true), configuracao).Run(Requisicao("GET"));

        // Serializado como o ASP.NET Core serializa (camelCase nas propriedades): as chaves do dicionário ficam em PascalCase.
        var corpo = ComoJson(Assert.IsType<OkObjectResult>(resultado).Value);
        Assert.Equal("1.0.0", corpo.GetProperty("versao").GetString());
        var toggles = corpo.GetProperty("toggles");
        string[] chaves = ["AsyncRequestReply", "WebhookHmac", "Inbox", "DlqComMotivo", "RetryAgendado", "Heartbeat", "WorkerComSessao", "IdentidadeGerenciada"];
        Assert.Equal(chaves, toggles.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.True(toggles.GetProperty("AsyncRequestReply").GetBoolean());
        Assert.True(toggles.GetProperty("Inbox").GetBoolean());
        Assert.False(toggles.GetProperty("WebhookHmac").GetBoolean());
        Assert.True(toggles.GetProperty("WorkerComSessao").GetBoolean());
        Assert.True(toggles.GetProperty("IdentidadeGerenciada").GetBoolean());
    }

    [Fact]
    public void Saude_no_baseline_mostra_tudo_desligado()
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServiceBusConnection"] = "Endpoint=sb://sb-saldaro-teste.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=ficticia",
            ["AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled"] = "true",
            ["AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled"] = "false",
        }).Build();

        var resultado = new Saude(Configuracoes.Cenario(), configuracao).Run(Requisicao("GET"));

        // O smoke test lê "toggles" como objeto de booleanos.
        var toggles = ComoJson(Assert.IsType<OkObjectResult>(resultado).Value).GetProperty("toggles");
        Assert.Equal(JsonValueKind.Object, toggles.ValueKind);
        Assert.Equal(8, toggles.EnumerateObject().Count());
        Assert.All(toggles.EnumerateObject(), toggle => Assert.Equal(JsonValueKind.False, toggle.Value.ValueKind));
    }

    private Task SemearAsync(string faturaId, string status) =>
        _faturas.SalvarAsync(new Fatura
        {
            FaturaId = faturaId,
            ClienteId = "cliente-a",
            Valor = 1250.00m,
            Vencimento = "2026-11-10",
            Status = status,
            IdCobranca = "COB-0a1b2c3d",
            QuantidadeBaixas = StatusFatura.JaBaixou(status) ? 1 : 0,
            CriadaEm = _relogio.Agora,
            AtualizadaEm = _relogio.Agora,
        }, CancellationToken.None);
}
