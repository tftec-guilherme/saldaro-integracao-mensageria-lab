using System.Globalization;
using System.Security.Cryptography;

namespace Saldaro.BancoSimulado.Dominio;

/// <summary>
/// Identificadores e instrumentos de pagamento fictícios. Nada aqui reproduz a API ou o formato
/// válido de um banco real: a linha digitável começa com o código de banco 000 (inexistente) e tem
/// todos os dígitos verificadores zerados; o "QR code" é um texto marcado como fictício.
/// </summary>
public static class DadosFicticios
{
    public const string PrefixoDaCobranca = "COB-";

    /// <summary><c>COB-</c> seguido de 8 dígitos hexadecimais minúsculos, ex.: <c>COB-0a1b2c3d</c>.</summary>
    public static string NovoIdCobranca() => PrefixoDaCobranca + RandomNumberGenerator.GetHexString(8, lowercase: true);

    /// <summary>
    /// Linha digitável com o desenho de 47 dígitos de um boleto (5.5 5.6 5.6 1 14), mas inválida de
    /// propósito. O "campo livre" carrega o idCobranca em decimal e os 10 últimos dígitos, o valor em centavos.
    /// </summary>
    public static string LinhaDigitavel(string idCobranca, decimal valor)
    {
        var hex = idCobranca.StartsWith(PrefixoDaCobranca, StringComparison.Ordinal)
            ? idCobranca[PrefixoDaCobranca.Length..]
            : idCobranca;
        var numero = uint.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var convertido)
            ? convertido
            : 0u;
        var campoLivre = numero.ToString("D10", CultureInfo.InvariantCulture);
        var centavos = (long)decimal.Round(valor * 100m, 0, MidpointRounding.AwayFromZero);
        var valorEmCentavos = centavos.ToString("D10", CultureInfo.InvariantCulture);

        return $"00000.{campoLivre[..5]} {campoLivre[5..]}.000000 00000.000000 0 0000{valorEmCentavos}";
    }

    /// <summary>Conteúdo do "QR code": texto legível e claramente fictício, não um payload Pix.</summary>
    public static string QrCode(string idCobranca, decimal valor) =>
        $"PIX-FICTICIO|SALDARO-LAB|{idCobranca}|{EventoDeCobranca.FormatarValorNumerico(valor)}";
}
