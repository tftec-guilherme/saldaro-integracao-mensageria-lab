namespace Saldaro.Ferramentas;

internal static class Estatistica
{
    /// <summary>
    /// Percentil pelo método do posto mais próximo (nearest-rank), sobre valores já ordenados.
    /// Ex.: p95 de 100 latências é a 95ª menor.
    /// </summary>
    public static double Percentil(IReadOnlyList<double> ordenados, double percentil)
    {
        ArgumentNullException.ThrowIfNull(ordenados);
        ArgumentOutOfRangeException.ThrowIfLessThan(percentil, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentil, 100);
        if (ordenados.Count == 0)
        {
            return 0;
        }

        var posto = (int)Math.Ceiling(percentil / 100.0 * ordenados.Count);
        return ordenados[Math.Clamp(posto - 1, 0, ordenados.Count - 1)];
    }
}
