namespace Saldaro.Hub.Dominio;

/// <summary>Próximo passo do retry agendado.</summary>
public sealed record DecisaoDeRetry(bool Esgotou, int Tentativa, TimeSpan Atraso);

/// <summary>
/// Retry agendado com backoff exponencial (seção 5.3, passo 2): atraso = 5 s × 2^n, n = 1..3.
/// </summary>
/// <remarks>
/// CONCEITO: retry agendado. Em vez de devolver a mensagem para a fila (que a entrega de novo na hora,
/// dez vezes em poucos segundos), o hub agenda uma cópia para daqui a 10 s, 20 s e 40 s. São 70 s ao
/// todo, mais que os 45 s da falha transiente simulada: a mensagem boa sobrevive e não vai para a DLQ.
/// </remarks>
public static class PoliticaDeRetry
{
    public const int MaximoDeTentativas = 3;
    public static readonly TimeSpan AtrasoBase = TimeSpan.FromSeconds(5);

    /// <summary>Atraso da tentativa <paramref name="tentativa"/> (1 → 10 s, 2 → 20 s, 3 → 40 s).</summary>
    public static TimeSpan Atraso(int tentativa)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tentativa, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tentativa, MaximoDeTentativas);
        return AtrasoBase * (1 << tentativa);
    }

    /// <summary>
    /// Decide a próxima tentativa a partir da atual (0 = mensagem original, sem a propriedade <c>tentativa</c>).
    /// Se a próxima passaria de 3, esgotou: dead-letter com motivo <c>TentativasEsgotadas</c>.
    /// </summary>
    public static DecisaoDeRetry Proxima(int tentativaAtual)
    {
        var proxima = Math.Max(tentativaAtual, 0) + 1;
        return proxima > MaximoDeTentativas
            ? new DecisaoDeRetry(true, tentativaAtual, TimeSpan.Zero)
            : new DecisaoDeRetry(false, proxima, Atraso(proxima));
    }
}
