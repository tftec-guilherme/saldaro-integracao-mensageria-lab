using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Saldaro.Hub.Dominio;

/// <summary>
/// Evento do webhook do banco (seção 7.1). É também o corpo da mensagem em
/// <c>sbq-confirmacoes-banco</c>: o hub enfileira o corpo bruto, sem reescrever.
/// </summary>
public sealed class EventoBanco
{
    public required string IdEvento { get; init; }
    public required string Tipo { get; init; }
    public required string FaturaId { get; init; }
    public string? IdCobranca { get; init; }
    public decimal Valor { get; init; }
    public DateTimeOffset? OcorridoEm { get; init; }
    public bool SimulaTransiente { get; init; }
    public DateTimeOffset? TransienteAte { get; init; }
}

/// <summary>Desserializa e valida o evento do banco: aqui nasce o "erro de schema" do cenário 3.4.</summary>
public static class LeitorDeEventoBanco
{
    // Formato de leitura: tudo anulável, para a validação dizer exatamente o que faltou.
    private sealed class EventoBancoJson
    {
        public string? IdEvento { get; set; }
        public string? Tipo { get; set; }
        public string? FaturaId { get; set; }
        public string? IdCobranca { get; set; }
        public decimal? Valor { get; set; }
        public DateTimeOffset? OcorridoEm { get; set; }
        public bool SimulaTransiente { get; set; }
        public DateTimeOffset? TransienteAte { get; set; }
    }

    public static bool TentarLer(
        ReadOnlySpan<byte> corpo,
        [NotNullWhen(true)] out EventoBanco? evento,
        [NotNullWhen(false)] out string? erro)
    {
        evento = null;
        EventoBancoJson? lido;
        try
        {
            lido = JsonSerializer.Deserialize<EventoBancoJson>(corpo, Json.Opcoes);
        }
        catch (JsonException ex)
        {
            // Ex.: "The JSON value could not be converted to System.Nullable`1[System.Decimal]. Path: $.valor".
            erro = $"Corpo fora do schema do evento do banco: {ex.Message}";
            return false;
        }

        if (lido is null)
        {
            erro = "Corpo vazio ou nulo: o evento do banco é um objeto JSON.";
            return false;
        }

        if (!ChaveDeTabela.EhValida(lido.IdEvento))
        {
            erro = "Campo 'idEvento' ausente ou inválido.";
            return false;
        }

        if (!ChaveDeTabela.EhValida(lido.FaturaId))
        {
            erro = "Campo 'faturaId' ausente ou inválido.";
            return false;
        }

        if (!TiposDeEvento.EhConhecido(lido.Tipo))
        {
            erro = $"Campo 'tipo' ausente ou desconhecido: '{lido.Tipo}'.";
            return false;
        }

        if (lido.Valor is null)
        {
            erro = "Campo 'valor' ausente: o valor é um número JSON (ex.: 1250.00).";
            return false;
        }

        evento = new EventoBanco
        {
            IdEvento = lido.IdEvento!,
            Tipo = lido.Tipo!,
            FaturaId = lido.FaturaId!,
            IdCobranca = lido.IdCobranca,
            Valor = lido.Valor.Value,
            OcorridoEm = lido.OcorridoEm,
            SimulaTransiente = lido.SimulaTransiente,
            TransienteAte = lido.TransienteAte,
        };
        erro = null;
        return true;
    }
}

/// <summary>
/// Extrai só os campos de roteamento (<c>idEvento</c>, <c>faturaId</c>, <c>tipo</c>) com
/// <see cref="JsonDocument"/>, sem desserializar o resto: o webhook não lê <c>valor</c>, então um
/// valor fora do schema ainda chega à fila (e quem decide o que fazer é o worker).
/// </summary>
public static class IdentificacaoDoEvento
{
    public static bool TentarExtrair(
        ReadOnlyMemory<byte> corpo,
        [NotNullWhen(true)] out string? idEvento,
        [NotNullWhen(true)] out string? faturaId,
        [NotNullWhen(true)] out string? tipo,
        [NotNullWhen(false)] out string? erro)
    {
        idEvento = faturaId = tipo = null;
        try
        {
            using var documento = JsonDocument.Parse(corpo);
            if (documento.RootElement.ValueKind != JsonValueKind.Object)
            {
                erro = "O corpo precisa ser um objeto JSON.";
                return false;
            }

            idEvento = LerTexto(documento.RootElement, "idEvento");
            faturaId = LerTexto(documento.RootElement, "faturaId");
            tipo = LerTexto(documento.RootElement, "tipo");
        }
        catch (JsonException ex)
        {
            erro = $"JSON inválido: {ex.Message}";
            return false;
        }

        if (!ChaveDeTabela.EhValida(idEvento) || !ChaveDeTabela.EhValida(faturaId) || string.IsNullOrWhiteSpace(tipo))
        {
            erro = "Os campos 'idEvento', 'faturaId' e 'tipo' são obrigatórios (texto, até 128 caracteres, sem / \\ # ?).";
            return false;
        }

        erro = null;
        return true;
    }

    /// <summary>Melhor esforço, para telemetria de mensagens fora do schema: devolve o que der para ler.</summary>
    public static (string? FaturaId, string? IdEvento) MelhorEsforco(ReadOnlyMemory<byte> corpo)
    {
        try
        {
            using var documento = JsonDocument.Parse(corpo);
            if (documento.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (null, null);
            }

            return (LerTexto(documento.RootElement, "faturaId"), LerTexto(documento.RootElement, "idEvento"));
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static string? LerTexto(JsonElement objeto, string nome) =>
        objeto.TryGetProperty(nome, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;
}
