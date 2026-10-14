using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Saldaro.Hub.Dominio;

/// <summary>Resultado da validação da assinatura do webhook.</summary>
public enum ResultadoDaAssinatura
{
    Valida,
    SegredoNaoConfigurado,
    CabecalhosAusentes,
    TimestampInvalido,
    AssinaturaInvalida,
    ForaDaJanela,
}

/// <summary>
/// Assinatura do webhook do banco (seção 7.2). O banco assina, o hub valida.
/// </summary>
/// <remarks>
/// CONCEITO: assinatura é HMAC do corpo, não segredo no header. O segredo nunca viaja: o banco
/// calcula <c>HMAC-SHA256(UTF8(WebhookSecret), "{timestamp}.{corpo}")</c> e manda só o resultado.
/// Quem não tem o segredo não consegue forjar a confirmação, e o timestamp assinado barra o ataque de
/// repetição (replay): uma confirmação verdadeira, capturada e enviada de novo depois de 5 minutos.
/// </remarks>
public static class AssinaturaWebhook
{
    public const string Prefixo = "sha256=";
    public const int ToleranciaEmSegundos = 300;

    /// <summary>Calcula o valor do header <c>X-Saldaro-Signature</c> (<c>sha256=&lt;hex minúsculo&gt;</c>).</summary>
    public static string Calcular(string segredo, string timestamp, ReadOnlySpan<byte> corpo) =>
        Prefixo + Convert.ToHexStringLower(CalcularMac(segredo, timestamp, corpo));

    /// <summary>
    /// Valida a assinatura sobre o corpo <b>bruto</b>, antes de qualquer desserialização: reformatar o
    /// JSON (espaços, ordem dos campos, casas decimais) mudaria os bytes e invalidaria o HMAC.
    /// </summary>
    public static ResultadoDaAssinatura Validar(
        string? segredo,
        string? timestamp,
        string? assinatura,
        ReadOnlySpan<byte> corpo,
        DateTimeOffset agora,
        int toleranciaEmSegundos = ToleranciaEmSegundos)
    {
        if (string.IsNullOrEmpty(segredo))
        {
            return ResultadoDaAssinatura.SegredoNaoConfigurado;
        }

        if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(assinatura))
        {
            return ResultadoDaAssinatura.CabecalhosAusentes;
        }

        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var segundos))
        {
            return ResultadoDaAssinatura.TimestampInvalido;
        }

        if (!assinatura.StartsWith(Prefixo, StringComparison.Ordinal))
        {
            return ResultadoDaAssinatura.AssinaturaInvalida;
        }

        byte[] recebida;
        try
        {
            recebida = Convert.FromHexString(assinatura.AsSpan(Prefixo.Length));
        }
        catch (FormatException)
        {
            return ResultadoDaAssinatura.AssinaturaInvalida;
        }

        var esperada = CalcularMac(segredo, timestamp, corpo);

        // CONCEITO: comparação em tempo constante. Um "==" que para no primeiro byte diferente deixa
        // o atacante medir o tempo de resposta e descobrir a assinatura byte a byte.
        if (!CryptographicOperations.FixedTimeEquals(esperada, recebida))
        {
            return ResultadoDaAssinatura.AssinaturaInvalida;
        }

        if (Math.Abs(agora.ToUnixTimeSeconds() - segundos) > toleranciaEmSegundos)
        {
            return ResultadoDaAssinatura.ForaDaJanela;
        }

        return ResultadoDaAssinatura.Valida;
    }

    private static byte[] CalcularMac(string segredo, string timestamp, ReadOnlySpan<byte> corpo)
    {
        // Conteúdo assinado: os bytes UTF-8 de "{timestamp}." seguidos do corpo exatamente como chegou.
        var prefixo = Encoding.UTF8.GetBytes(timestamp + ".");
        var conteudo = new byte[prefixo.Length + corpo.Length];
        prefixo.CopyTo(conteudo, 0);
        corpo.CopyTo(conteudo.AsSpan(prefixo.Length));

        // A chave é a string do setting em UTF-8 (sem decodificar base64), igual no banco e no hub.
        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), conteudo);
    }
}
