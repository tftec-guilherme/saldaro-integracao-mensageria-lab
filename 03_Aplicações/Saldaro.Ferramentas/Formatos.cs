using System.Globalization;

namespace Saldaro.Ferramentas;

/// <summary>Formatos de número usados na conversão de valores e na saída da ferramenta.</summary>
internal static class Formatos
{
    // CONCEITO: no Brasil, "1.250,00" é mil duzentos e cinquenta reais: o ponto separa o milhar e a
    // vírgula separa os centavos. Os separadores ficam fixos aqui, em vez de CultureInfo("pt-BR"),
    // para o resultado ser o mesmo em Windows, macOS e Linux (não depende dos dados de cultura do
    // sistema operacional).
    public static NumberFormatInfo PtBr { get; } = CriarPtBr();

    private static NumberFormatInfo CriarPtBr()
    {
        var formato = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
        formato.NumberDecimalSeparator = ",";
        formato.NumberGroupSeparator = ".";
        formato.CurrencyDecimalSeparator = ",";
        formato.CurrencyGroupSeparator = ".";
        formato.CurrencySymbol = "R$";
        formato.PercentDecimalSeparator = ",";
        formato.PercentGroupSeparator = ".";
        return NumberFormatInfo.ReadOnly(formato);
    }

    /// <summary>Inteiro com separador de milhar pt-BR (ex.: 1.204).</summary>
    public static string Inteiro(long valor) => valor.ToString("N0", PtBr);

    /// <summary>Número com uma casa decimal pt-BR (ex.: 12,3).</summary>
    public static string UmaCasa(double valor) => valor.ToString("N1", PtBr);
}
