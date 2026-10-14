using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Azure.Core.Pipeline;
using Azure.Storage.Queues;
using Saldaro.BancoSimulado.Infraestrutura;

namespace Saldaro.BancoSimulado.Tests;

/// <summary>
/// Os dois lados da fila banco-pagamentos precisam combinar a codificação: o QueueClient que
/// enfileira e o trigger EnviarWebhook (host.json). Estes testes travam as duas pontas.
/// </summary>
public class FilaDePagamentosTests
{
    [Fact]
    public void O_trigger_de_fila_do_host_json_espera_Base64()
    {
        using var hostJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "banco-host.json")));

        var encoding = hostJson.RootElement.GetProperty("extensions").GetProperty("queues").GetProperty("messageEncoding").GetString();

        Assert.Equal("base64", encoding);
    }

    [Fact]
    public void CriarOpcoes_envia_em_Base64()
    {
        Assert.Equal(QueueMessageEncoding.Base64, FilaDePagamentos.CriarOpcoes().MessageEncoding);
    }

    [Fact]
    public async Task Enfileirar_manda_o_texto_em_Base64_com_o_atraso_como_visibilityTimeout()
    {
        var handler = new HandlerDeCaptura(RespostaDoStorage);
        var opcoes = FilaDePagamentos.CriarOpcoes();
        opcoes.Transport = new HttpClientTransport(new HttpClient(handler));
        opcoes.Retry.MaxRetries = 0;
        var fila = new FilaDePagamentos(new QueueClient(new Uri("https://contaficticia.queue.core.windows.net/banco-pagamentos"), opcoes));
        const string mensagem = "{\"evento\":{\"valor\":1250.00},\"webhookUrl\":\"https://exemplo.invalid\",\"assinaturaInvalida\":false}";

        await fila.EnfileirarAsync(mensagem, TimeSpan.FromSeconds(8), CancellationToken.None);

        var envio = Assert.Single(handler.Requisicoes, r => r.Metodo == HttpMethod.Post);
        Assert.Contains("visibilitytimeout=8", envio.Uri.Query);
        // Lido dos bytes (e não de uma string) porque o SDK grava o XML com BOM.
        var textoNaFila = XDocument.Load(new MemoryStream(envio.Corpo)).Root!.Element("MessageText")!.Value;
        Assert.Equal(mensagem, Encoding.UTF8.GetString(Convert.FromBase64String(textoNaFila)));
    }

    private static HttpResponseMessage RespostaDoStorage(HttpRequestMessage requisicao)
    {
        if (requisicao.Method == HttpMethod.Put)
        {
            // Create Queue (CreateIfNotExists).
            return new HttpResponseMessage(HttpStatusCode.Created);
        }

        // Put Message: o SDK lê o recibo devolvido pelo serviço.
        const string recibo = """
            <?xml version="1.0" encoding="utf-8"?>
            <QueueMessagesList>
              <QueueMessage>
                <MessageId>00000000-0000-0000-0000-000000000001</MessageId>
                <InsertionTime>Tue, 10 Nov 2026 09:30:00 GMT</InsertionTime>
                <ExpirationTime>Tue, 17 Nov 2026 09:30:00 GMT</ExpirationTime>
                <PopReceipt>recibo-ficticio</PopReceipt>
                <TimeNextVisible>Tue, 10 Nov 2026 09:30:08 GMT</TimeNextVisible>
              </QueueMessage>
            </QueueMessagesList>
            """;
        return new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(recibo, Encoding.UTF8, "application/xml"),
        };
    }
}
