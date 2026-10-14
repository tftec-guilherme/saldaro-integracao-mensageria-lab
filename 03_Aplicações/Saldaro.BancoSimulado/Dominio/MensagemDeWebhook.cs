using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Saldaro.BancoSimulado.Dominio;

/// <summary>
/// Mensagem da fila <c>banco-pagamentos</c> (contrato, seção 6):
/// <c>{ "evento": { ... }, "webhookUrl": "...", "assinaturaInvalida": false }</c>.
/// <see cref="Corpo"/> é o JSON do evento exatamente como será assinado e enviado.
/// </summary>
public sealed record MensagemDeWebhook(string Corpo, string WebhookUrl, bool AssinaturaInvalida)
{
    public string ParaTexto()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WritePropertyName("evento");
            // CONCEITO: o evento entra "cru", sem reserializar. O EnviarWebhook assina e envia
            // exatamente estes bytes; assim 1250.00 continua 1250.00 e a assinatura bate no hub.
            json.WriteRawValue(Corpo);
            json.WriteString("webhookUrl", WebhookUrl);
            json.WriteBoolean("assinaturaInvalida", AssinaturaInvalida);
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>Lê o texto da fila. Mensagem fora do formato lança <see cref="FormatException"/>.</summary>
    public static MensagemDeWebhook Ler(string texto)
    {
        JsonDocument documento;
        try
        {
            documento = JsonDocument.Parse(texto);
        }
        catch (JsonException ex)
        {
            throw new FormatException("a mensagem da fila não é um JSON válido", ex);
        }

        using (documento)
        {
            var raiz = documento.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object
                || !raiz.TryGetProperty("evento", out var evento)
                || evento.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("a mensagem da fila não tem o objeto 'evento'");
            }

            if (!raiz.TryGetProperty("webhookUrl", out var url)
                || url.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(url.GetString()))
            {
                throw new FormatException("a mensagem da fila não tem 'webhookUrl'");
            }

            var assinaturaInvalida = raiz.TryGetProperty("assinaturaInvalida", out var flag) && flag.ValueKind == JsonValueKind.True;

            // GetRawText devolve o trecho original do evento, sem reformatar nada.
            return new MensagemDeWebhook(evento.GetRawText(), url.GetString()!, assinaturaInvalida);
        }
    }

    /// <summary>Campos do evento usados só em log (<c>idEvento</c>, <c>tipo</c>, <c>faturaId</c>).</summary>
    public IdentificacaoDoEvento Identificar()
    {
        using var documento = JsonDocument.Parse(Corpo);
        var raiz = documento.RootElement;
        return new IdentificacaoDoEvento(LerTexto(raiz, "idEvento"), LerTexto(raiz, "tipo"), LerTexto(raiz, "faturaId"));
    }

    private static string LerTexto(JsonElement objeto, string propriedade) =>
        objeto.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString() ?? "?"
            : "?";
}

public sealed record IdentificacaoDoEvento(string IdEvento, string Tipo, string FaturaId);
