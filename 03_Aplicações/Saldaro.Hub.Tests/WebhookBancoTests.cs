using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Functions;

namespace Saldaro.Hub.Tests;

/// <summary>Function WebhookBanco: assinatura inválida → 401 e nada na fila; válida → corpo bruto na fila.</summary>
public class WebhookBancoTests
{
    private readonly MensageriaEmMemoria _mensageria = new();
    private readonly TelemetriaEmMemoria _telemetria = new();
    private readonly RelogioControlado _relogio = new(DateTimeOffset.FromUnixTimeSeconds(1760000000));

    private WebhookBanco Funcao(bool webhookHmac) => new(
        _mensageria, _telemetria, Configuracoes.Cenario(webhookHmac: webhookHmac),
        Configuracoes.Hub(AssinaturaWebhookTests.Segredo), _relogio, NullLogger<WebhookBanco>.Instance);

    private static HttpRequest Requisicao(string corpo, string? timestamp = null, string? assinatura = null)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Method = HttpMethods.Post;
        contexto.Request.ContentType = "application/json";
        contexto.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(corpo));
        if (timestamp is not null)
        {
            contexto.Request.Headers[Cabecalhos.Timestamp] = timestamp;
        }

        if (assinatura is not null)
        {
            contexto.Request.Headers[Cabecalhos.Assinatura] = assinatura;
        }

        return contexto.Request;
    }

    [Fact]
    public async Task Assinatura_valida_enfileira_o_corpo_bruto_com_MessageId_e_SessionId()
    {
        var resultado = await Funcao(webhookHmac: true).Run(
            Requisicao(AssinaturaWebhookTests.Corpo, AssinaturaWebhookTests.Timestamp, AssinaturaWebhookTests.AssinaturaEsperada),
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(resultado);
        var (fila, mensagem) = Assert.Single(_mensageria.Enviadas);
        Assert.Equal(Filas.ConfirmacoesBanco, fila);
        Assert.Equal("11111111-1111-1111-1111-111111111111", mensagem.MessageId);
        Assert.Equal("22222222-2222-2222-2222-222222222222", mensagem.SessionId);
        Assert.Equal("application/json", mensagem.ContentType);
        Assert.Equal("cobranca.paga", mensagem.ApplicationProperties[PropriedadesDaMensagem.Tipo]);
        Assert.Equal(Encoding.UTF8.GetBytes(AssinaturaWebhookTests.Corpo), mensagem.Body.ToArray());

        var evento = _telemetria.Unico(EventosDeTelemetria.ConfirmacaoRecebida);
        Assert.Equal("22222222-2222-2222-2222-222222222222", evento["faturaId"]);
        Assert.Equal("11111111-1111-1111-1111-111111111111", evento["idEvento"]);
        Assert.Equal("cobranca.paga", evento["tipo"]);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(AssinaturaWebhookTests.Timestamp, "sha256=0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData(AssinaturaWebhookTests.Timestamp, "segredo-de-teste-saldaro")]
    public async Task Assinatura_invalida_ou_ausente_responde_401_e_nada_vai_para_a_fila(string? timestamp, string? assinatura)
    {
        var resultado = await Funcao(webhookHmac: true).Run(Requisicao(AssinaturaWebhookTests.Corpo, timestamp, assinatura), CancellationToken.None);

        Assert.Equal(StatusCodes.Status401Unauthorized, Assert.IsType<UnauthorizedObjectResult>(resultado).StatusCode);
        Assert.Empty(_mensageria.Enviadas);
        Assert.Empty(_telemetria.Eventos);
    }

    [Fact]
    public async Task Timestamp_expirado_responde_401()
    {
        _relogio.Agora = _relogio.Agora.AddSeconds(301);

        var resultado = await Funcao(webhookHmac: true).Run(
            Requisicao(AssinaturaWebhookTests.Corpo, AssinaturaWebhookTests.Timestamp, AssinaturaWebhookTests.AssinaturaEsperada),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(resultado);
        Assert.Empty(_mensageria.Enviadas);
    }

    [Fact]
    public async Task Baseline_aceita_a_confirmacao_forjada_sem_assinatura()
    {
        // Incidente 2: no baseline, quem conhece a URL consegue "pagar" uma fatura.
        var resultado = await Funcao(webhookHmac: false).Run(Requisicao(AssinaturaWebhookTests.Corpo), CancellationToken.None);

        Assert.IsType<AcceptedResult>(resultado);
        var mensagem = Assert.Single(_mensageria.Enviadas).Mensagem;
        Assert.Equal("11111111-1111-1111-1111-111111111111", mensagem.MessageId);
        Assert.Equal("22222222-2222-2222-2222-222222222222", mensagem.SessionId); // SessionId sempre, já no baseline
        Assert.Equal(1, _telemetria.Contar(EventosDeTelemetria.ConfirmacaoRecebida));
    }

    [Fact]
    public async Task Valor_fora_do_schema_ainda_entra_na_fila_porque_o_webhook_nao_le_o_valor()
    {
        var corpo = AssinaturaWebhookTests.Corpo.Replace("1250.00", "\"1.250,00\"", StringComparison.Ordinal);

        var resultado = await Funcao(webhookHmac: false).Run(Requisicao(corpo), CancellationToken.None);

        Assert.IsType<AcceptedResult>(resultado);
        Assert.Single(_mensageria.Enviadas);
    }

    [Theory]
    [InlineData("{\"tipo\":\"cobranca.paga\",\"faturaId\":\"f-1\"}")]
    [InlineData("não é json")]
    [InlineData("[1,2,3]")]
    public async Task Corpo_sem_os_campos_de_roteamento_responde_400(string corpo)
    {
        var resultado = await Funcao(webhookHmac: false).Run(Requisicao(corpo), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(_mensageria.Enviadas);
    }
}
