using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Saldaro.BancoSimulado.Dominio;

public static class TiposDeEvento
{
    public const string CobrancaRegistrada = "cobranca.registrada";
    public const string CobrancaPaga = "cobranca.paga";
}

/// <summary>
/// Evento de webhook do banco para o hub (contrato, seção 7.1). O JSON gerado por
/// <see cref="ParaJson"/> é o corpo que o banco assina e envia, byte a byte.
/// </summary>
public sealed record EventoDeCobranca(
    Guid IdEvento,
    string Tipo,
    string FaturaId,
    string IdCobranca,
    decimal Valor,
    DateTimeOffset OcorridoEm,
    bool SimulaTransiente,
    DateTimeOffset? TransienteAte)
{
    // Separadores pt-BR fixos no código: não dependem dos dados de cultura (ICU) da máquina.
    private static readonly NumberFormatInfo FormatoPtBr = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = [3],
    };

    /// <summary>
    /// JSON compacto, com os campos na ordem do contrato. Com <paramref name="valorComVirgula"/>,
    /// <c>valor</c> sai como texto pt-BR (<c>"1.250,00"</c>); senão, como número com duas casas (<c>1250.00</c>).
    /// </summary>
    public string ParaJson(bool valorComVirgula = false)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("idEvento", IdEvento.ToString("D"));
            json.WriteString("tipo", Tipo);
            json.WriteString("faturaId", FaturaId);
            json.WriteString("idCobranca", IdCobranca);
            if (valorComVirgula)
            {
                // CONCEITO: mudança de contrato sem aviso. O banco "atualizou a API" e o valor virou
                // texto com vírgula. Quem desserializa como número quebra: é a mensagem envenenada do Incidente 3.
                json.WriteString("valor", FormatarValorPtBr(Valor));
            }
            else
            {
                json.WritePropertyName("valor");
                json.WriteRawValue(FormatarValorNumerico(Valor));
            }

            json.WriteString("ocorridoEm", FormatarInstante(OcorridoEm));
            json.WriteBoolean("simulaTransiente", SimulaTransiente);
            if (TransienteAte is { } ate)
            {
                json.WriteString("transienteAte", FormatarInstante(ate));
            }
            else
            {
                json.WriteNull("transienteAte");
            }

            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>Número JSON com duas casas decimais, sempre com ponto: <c>1250.00</c>.</summary>
    public static string FormatarValorNumerico(decimal valor) =>
        decimal.Round(valor, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Texto no formato brasileiro: <c>1.250,00</c>.</summary>
    public static string FormatarValorPtBr(decimal valor) =>
        decimal.Round(valor, 2, MidpointRounding.AwayFromZero).ToString("N2", FormatoPtBr);

    /// <summary>Instante em UTC, ISO 8601 sem frações de segundo: <c>2026-11-10T09:30:00Z</c>.</summary>
    public static string FormatarInstante(DateTimeOffset instante) =>
        instante.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
