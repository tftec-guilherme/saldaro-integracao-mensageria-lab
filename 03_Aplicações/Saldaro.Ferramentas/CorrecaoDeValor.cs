using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Saldaro.Ferramentas;

/// <summary>Como o campo <c>valor</c> aparece no corpo de uma mensagem da DLQ.</summary>
internal enum SituacaoDoValor
{
    /// <summary><c>"valor": 1250.00</c>. Nada a corrigir; reprocessa como está.</summary>
    Numerico,

    /// <summary><c>"valor": "1.250,00"</c>. Texto no formato brasileiro; dá para converter.</summary>
    TextoPtBr,

    /// <summary>Sem campo <c>valor</c> (ou nulo). Nada a corrigir; reprocessa como está.</summary>
    Ausente,

    /// <summary>Texto que não está no formato pt-BR (ex.: <c>"abc"</c>). Fica na DLQ.</summary>
    TextoInvalido,

    /// <summary>Booleano, objeto ou lista no lugar do número. Fica na DLQ.</summary>
    TipoInvalido,

    /// <summary>O corpo não é um objeto JSON. Fica na DLQ.</summary>
    CorpoInvalido,
}

internal sealed record ResultadoDaCorrecao(
    SituacaoDoValor Situacao,
    BinaryData Corpo,
    string? ValorOriginal,
    decimal? ValorCorrigido)
{
    /// <summary>Verdadeiro quando o reprocessamento faz sentido (com ou sem correção).</summary>
    public bool PodeReprocessar =>
        Situacao is SituacaoDoValor.Numerico or SituacaoDoValor.TextoPtBr or SituacaoDoValor.Ausente;

    public bool Corrigiu => Situacao == SituacaoDoValor.TextoPtBr;

    /// <summary>Valor como aparece no corpo, para mostrar na tela (texto entre aspas).</summary>
    public string ValorParaExibir => Situacao switch
    {
        SituacaoDoValor.TextoPtBr or SituacaoDoValor.TextoInvalido => $"\"{ValorOriginal}\"",
        SituacaoDoValor.Ausente => "(sem valor)",
        SituacaoDoValor.CorpoInvalido => "(corpo não é JSON)",
        _ => ValorOriginal ?? string.Empty,
    };

    public string Explicacao => Situacao switch
    {
        SituacaoDoValor.TextoPtBr => $"valor \"{ValorOriginal}\" -> {ValorCorrigido?.ToString(CultureInfo.InvariantCulture)}",
        SituacaoDoValor.Numerico => $"valor {ValorOriginal} já é número (reprocessada sem alteração)",
        SituacaoDoValor.Ausente => "sem campo valor (reprocessada sem alteração)",
        SituacaoDoValor.TextoInvalido => $"valor em texto fora do formato pt-BR: \"{ValorOriginal}\"",
        SituacaoDoValor.TipoInvalido => $"valor com tipo inesperado: {ValorOriginal}",
        _ => "corpo não é um objeto JSON",
    };
}

/// <summary>
/// Corrige o <c>valor</c> que o banco parceiro passou a mandar como texto pt-BR (Incidente 3).
/// Lógica pura: recebe o corpo e devolve o corpo corrigido, sem tocar no Service Bus.
/// </summary>
internal static partial class CorrecaoDeValor
{
    public const string Campo = "valor";

    private static readonly JsonSerializerOptions OpcoesDeEscrita = new()
    {
        // Mantém acentos e símbolos como estão no corpo original, sem escapar para \uXXXX.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // Parte inteira com milhar agrupado de 3 em 3 ("1.250", "12.345.678") ou sem agrupamento
    // ("1250"), seguida opcionalmente de vírgula e casas decimais (",00").
    [GeneratedRegex(@"^-?([0-9]{1,3}(\.[0-9]{3})+|[0-9]+)(,[0-9]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex NumeroPtBr();

    /// <summary>Converte "1.250,00" (ou "R$ 1.250,00", "1250,5", "1.250") para decimal.</summary>
    public static bool TentarConverterTextoPtBr(string? texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpo = texto.Trim();
        if (limpo.StartsWith(Formatos.PtBr.CurrencySymbol, StringComparison.Ordinal))
        {
            limpo = limpo[Formatos.PtBr.CurrencySymbol.Length..].TrimStart();
        }

        // CONCEITO: validar o formato antes de converter. Sem isso, "1250.00" (formato americano)
        // seria lido como 125000, porque no pt-BR o ponto é só separador de milhar.
        if (!NumeroPtBr().IsMatch(limpo))
        {
            return false;
        }

        return decimal.TryParse(
            limpo,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint,
            Formatos.PtBr,
            out valor);
    }

    /// <summary>Analisa o corpo JSON e, se o <c>valor</c> estiver em texto pt-BR, troca por número.</summary>
    public static ResultadoDaCorrecao Corrigir(BinaryData corpo)
    {
        ArgumentNullException.ThrowIfNull(corpo);

        JsonNode? raiz;
        try
        {
            raiz = JsonNode.Parse(SemBom(corpo.ToMemory().Span));
        }
        catch (JsonException)
        {
            return new(SituacaoDoValor.CorpoInvalido, corpo, null, null);
        }

        if (raiz is not JsonObject objeto)
        {
            return new(SituacaoDoValor.CorpoInvalido, corpo, null, null);
        }

        if (!objeto.TryGetPropertyValue(Campo, out var no) || no is null)
        {
            return new(SituacaoDoValor.Ausente, corpo, null, null);
        }

        switch (no.GetValueKind())
        {
            case JsonValueKind.Number:
                return new(SituacaoDoValor.Numerico, corpo, no.ToJsonString(), null);

            case JsonValueKind.String:
                var texto = no.GetValue<string>();
                if (!TentarConverterTextoPtBr(texto, out var valor))
                {
                    return new(SituacaoDoValor.TextoInvalido, corpo, texto, null);
                }

                // Só o campo valor muda; os demais campos seguem como vieram.
                objeto[Campo] = JsonValue.Create(valor);
                var corrigido = BinaryData.FromString(objeto.ToJsonString(OpcoesDeEscrita));
                return new(SituacaoDoValor.TextoPtBr, corrigido, texto, valor);

            default:
                return new(SituacaoDoValor.TipoInvalido, corpo, no.ToJsonString(), null);
        }
    }

    // "\uFEFF"u8 são os bytes EF BB BF, a marca de ordem de bytes (BOM) do UTF-8.
    private static ReadOnlySpan<byte> SemBom(ReadOnlySpan<byte> utf8) =>
        utf8.StartsWith("\uFEFF"u8) ? utf8[3..] : utf8;
}
