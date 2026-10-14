using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Messaging.ServiceBus;

namespace Saldaro.Ferramentas.Tests;

public class NomeDoNamespaceTests
{
    [Theory]
    [InlineData("sb-saldaro-ab12c.servicebus.windows.net", "sb-saldaro-ab12c.servicebus.windows.net")]
    [InlineData("sb-saldaro-ab12c", "sb-saldaro-ab12c.servicebus.windows.net")]
    [InlineData("SB-Saldaro-AB12C", "sb-saldaro-ab12c.servicebus.windows.net")]
    [InlineData("sb://sb-saldaro-ab12c.servicebus.windows.net/", "sb-saldaro-ab12c.servicebus.windows.net")]
    [InlineData("https://sb-saldaro-ab12c.servicebus.windows.net", "sb-saldaro-ab12c.servicebus.windows.net")]
    [InlineData("  sb-saldaro-ab12c.servicebus.windows.net  ", "sb-saldaro-ab12c.servicebus.windows.net")]
    public void Normaliza_para_o_FQDN(string entrada, string esperado)
    {
        Assert.Equal(esperado, NomeDoNamespace.Normalizar(entrada));
    }

    [Theory]
    [InlineData("Endpoint=sb://sb-saldaro-ab12c.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=nao-e-uma-chave")]
    [InlineData("sb-saldaro ab12c")]
    [InlineData("sb-saldaro-ab12c.servicebus.windows.net/sbq-confirmacoes-banco")]
    [InlineData("   ")]
    public void Recusa_connection_string_e_nomes_invalidos(string entrada)
    {
        Assert.ThrowsAny<ArgumentException>(() => NomeDoNamespace.Normalizar(entrada));
    }
}

public class EstatisticaTests
{
    [Theory]
    [InlineData(50, 50)]
    [InlineData(95, 95)]
    [InlineData(99, 99)]
    [InlineData(100, 100)]
    [InlineData(0, 1)]
    public void Percentil_pelo_posto_mais_proximo(double percentil, double esperado)
    {
        var valores = Enumerable.Range(1, 100).Select(v => (double)v).ToArray();

        Assert.Equal(esperado, Estatistica.Percentil(valores, percentil));
    }

    [Fact]
    public void Lista_vazia_da_zero()
    {
        Assert.Equal(0, Estatistica.Percentil([], 95));
    }
}

public class PoliticaDeRetryQueContaTests
{
    [Fact]
    public void Throttling_e_retentado_e_contado()
    {
        var politica = new PoliticaDeRetryQueConta();
        var ocupado = new ServiceBusException("ocupado", ServiceBusFailureReason.ServiceBusy);

        var espera = politica.CalculateRetryDelay(ocupado, attemptCount: 1);

        Assert.NotNull(espera);
        Assert.InRange(espera.Value.TotalMilliseconds, 700, 900);
        Assert.Equal(1, politica.Retentativas[nameof(ServiceBusFailureReason.ServiceBusy)]);
    }

    [Fact]
    public void Para_depois_do_maximo_de_retentativas()
    {
        var politica = new PoliticaDeRetryQueConta();
        var ocupado = new ServiceBusException("ocupado", ServiceBusFailureReason.ServiceBusy);

        Assert.NotNull(politica.CalculateRetryDelay(ocupado, attemptCount: 3));
        Assert.Null(politica.CalculateRetryDelay(ocupado, attemptCount: 4));
    }

    [Fact]
    public void Erro_permanente_nao_e_retentado()
    {
        var politica = new PoliticaDeRetryQueConta();
        var inexistente = new ServiceBusException("não existe", ServiceBusFailureReason.MessagingEntityNotFound);

        Assert.Null(politica.CalculateRetryDelay(inexistente, attemptCount: 1));
        Assert.Null(politica.CalculateRetryDelay(new UnauthorizedAccessException("401"), attemptCount: 1));
        Assert.Empty(politica.Retentativas);
    }

    [Fact]
    public void Espera_respeita_o_teto()
    {
        var politica = new PoliticaDeRetryQueConta { MaximoDeRetentativas = 20 };

        var espera = politica.CalculateRetryDelay(new TimeoutException(), attemptCount: 15);

        Assert.Equal(politica.EsperaMaxima, espera);
    }
}

public class FalhasTests
{
    [Fact]
    public void Reconhece_401_em_qualquer_nivel()
    {
        Assert.True(Falhas.EhAcessoNegado(new UnauthorizedAccessException("401")));
        Assert.True(Falhas.EhAcessoNegado(new InvalidOperationException("fora", new UnauthorizedAccessException("401"))));
        Assert.True(Falhas.EhAcessoNegado(new Azure.RequestFailedException(403, "proibido")));
        Assert.False(Falhas.EhAcessoNegado(new Azure.RequestFailedException(404, "não existe")));
        Assert.False(Falhas.EhAcessoNegado(new TimeoutException()));
        Assert.False(Falhas.EhAcessoNegado(null));
    }
}

public class MensagensDeTesteTests
{
    [Fact]
    public void Legado_cria_a_fila_sem_hifens_do_contrato()
    {
        Assert.Equal("confirmacoesbanco", ComandoLegadoAutoCreate.FilaDoLegado);
    }

    [Fact]
    public void Legado_explica_o_401_pelo_direito_Manage_e_sai_com_codigo_de_erro()
    {
        var excecao = new InvalidOperationException(
            "falhou",
            new UnauthorizedAccessException("Unauthorized: Manage,EntityRead claims required for this operation."));

        var texto = ComandoLegadoAutoCreate.MensagemSemManage("sb-saldaro-ab12c.servicebus.windows.net", excecao);

        Assert.Contains("não tem o direito Manage no namespace sb-saldaro-ab12c.servicebus.windows.net", texto);
        Assert.Contains("privilégio mínimo", texto);
        Assert.Contains("Manage,EntityRead claims required", texto);
        Assert.True(Falhas.EhAcessoNegado(excecao));
        Assert.NotEqual(CodigosDeSaida.Sucesso, CodigosDeSaida.AcessoNegado);
    }

    [Fact]
    public void Confirmacao_do_legado_segue_o_formato_do_webhook()
    {
        var mensagem = ComandoLegadoAutoCreate.CriarConfirmacao();

        using var corpo = JsonDocument.Parse(mensagem.Body.ToString());
        var raiz = corpo.RootElement;
        Assert.Equal(raiz.GetProperty("idEvento").GetString(), mensagem.MessageId);
        Assert.Equal(raiz.GetProperty("faturaId").GetString(), mensagem.SessionId);
        Assert.Equal("cobranca.paga", raiz.GetProperty("tipo").GetString());
        Assert.Equal("cobranca.paga", mensagem.ApplicationProperties["tipo"]);
        Assert.Equal(JsonValueKind.Number, raiz.GetProperty("valor").ValueKind);
        Assert.Matches(new Regex("^COB-[0-9a-f]{8}$"), raiz.GetProperty("idCobranca").GetString());
        Assert.Equal("application/json", mensagem.ContentType);
    }

    [Fact]
    public void Rajada_recusa_as_filas_do_hub()
    {
        foreach (var fila in new[] { "sbq-faturas-recebidas", "sbq-confirmacoes-banco", "sbq-notificacoes-parceiro" })
        {
            var argumentos = Argumentos.Analisar(["rajada", "--namespace", "sb-saldaro-ab12c", "--fila", fila]).Argumentos!;
            Assert.Throws<ArgumentException>(() => OpcoesDaRajada.De(argumentos));
        }
    }

    [Fact]
    public void Rajada_aceita_fila_descartavel()
    {
        var argumentos = Argumentos.Analisar(["rajada", "--namespace", "sb-saldaro-ab12c", "--fila", "sbq-rajada", "--remetentes", "5", "--mensagens", "7"]).Argumentos!;

        var opcoes = OpcoesDaRajada.De(argumentos);

        Assert.Equal("sb-saldaro-ab12c.servicebus.windows.net", opcoes.Namespace);
        Assert.Equal(35, opcoes.Total);
    }

    [Fact]
    public void Mensagem_da_rajada_tem_id_unico_por_remetente_e_sequencia()
    {
        var mensagem = ComandoRajada.CriarMensagem("20261014090000", remetente: 7, sequencia: 42);

        Assert.Equal("rajada-20261014090000-7-42", mensagem.MessageId);
        Assert.Equal("remetente-7", mensagem.SessionId);
    }

    [Fact]
    public void Tabela_alinha_as_colunas()
    {
        var texto = Tabela.Montar(["Motivo", "Qtde"], [new[] { "SchemaInvalida", "12" }, new[] { "X", "3" }], [false, true]);

        var linhas = texto.Split(Environment.NewLine);
        Assert.Equal("Motivo          Qtde", linhas[0]);
        Assert.Equal("--------------  ----", linhas[1]);
        Assert.Equal("SchemaInvalida    12", linhas[2]);
        Assert.Equal("X                  3", linhas[3]);
    }
}
