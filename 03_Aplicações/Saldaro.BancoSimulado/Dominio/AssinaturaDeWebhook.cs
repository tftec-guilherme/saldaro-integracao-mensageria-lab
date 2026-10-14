using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Saldaro.BancoSimulado.Dominio;

/// <summary>
/// Assinatura do webhook (contrato, seção 7.2): o banco assina e o hub valida com a mesma regra.
/// </summary>
public static class AssinaturaDeWebhook
{
    public const string HeaderTimestamp = "X-Saldaro-Timestamp";
    public const string HeaderAssinatura = "X-Saldaro-Signature";
    public const string Prefixo = "sha256=";

    private const string DigitosHex = "0123456789abcdef";

    /// <summary>
    /// <c>sha256=</c> + HMAC-SHA256 em hex minúsculo da string <c>"{timestamp}.{corpo}"</c>,
    /// com chave = bytes UTF-8 do segredo (a string do setting, sem decodificar base64).
    /// </summary>
    // CONCEITO: o HMAC cobre o corpo bruto. O hub recalcula sobre os bytes que recebeu, antes de
    // desserializar: se um único caractere mudar no caminho (até um espaço), a assinatura não bate.
    // O timestamp entra no conteúdo assinado para o hub recusar, fora de ±300 s, o ataque de repetição (replay).
    public static string Calcular(string segredo, long timestamp, string corpo)
    {
        var conteudo = string.Create(CultureInfo.InvariantCulture, $"{timestamp}.{corpo}");
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), Encoding.UTF8.GetBytes(conteudo));
        return Prefixo + Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Botão <c>assinatura=invalida</c>: troca cada dígito hex pelo seu complemento (0↔f, 1↔e, ...).
    /// O formato continua plausível (<c>sha256=</c> + 64 hex), mas nenhum dígito confere.
    /// </summary>
    public static string Corromper(string assinatura)
    {
        var hex = assinatura.StartsWith(Prefixo, StringComparison.Ordinal) ? assinatura[Prefixo.Length..] : assinatura;
        var corrompido = string.Create(hex.Length, hex, static (destino, origem) =>
        {
            for (var i = 0; i < origem.Length; i++)
            {
                var posicao = DigitosHex.IndexOf(char.ToLowerInvariant(origem[i]));
                destino[i] = posicao < 0 ? origem[i] : DigitosHex[15 - posicao];
            }
        });
        return Prefixo + corrompido;
    }
}
