namespace Saldaro.BancoSimulado.Dominio;

/// <summary>Decide se um botão percentual (como <c>falhaTransiente=30</c>) dispara nesta chamada.</summary>
public interface ISorteio
{
    /// <summary>Devolve <c>true</c> em cerca de <paramref name="percentual"/>% das chamadas.</summary>
    bool Dispara(int percentual);
}

/// <summary>Sorteio de verdade, usado pelas Functions. Os testes usam um sorteio fixo.</summary>
public sealed class SorteioAleatorio : ISorteio
{
    public bool Dispara(int percentual) => Sorteio.Dispara(percentual, Random.Shared.Next(100));
}

public static class Sorteio
{
    /// <summary>
    /// Regra pura do sorteio: com <paramref name="valorSorteado"/> uniforme entre 0 e 99,
    /// dispara em exatamente <paramref name="percentual"/> dos 100 valores possíveis.
    /// </summary>
    public static bool Dispara(int percentual, int valorSorteado) => valorSorteado < Math.Clamp(percentual, 0, 100);
}
