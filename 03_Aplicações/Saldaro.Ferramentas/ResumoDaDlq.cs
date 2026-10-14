namespace Saldaro.Ferramentas;

/// <summary>Uma linha da tabela da triagem: as mensagens de um mesmo DeadLetterReason.</summary>
internal sealed record GrupoDaDlq(
    string Motivo,
    int Quantidade,
    int Corrigiveis,
    int SemCorrecao,
    int NaoCorrigiveis,
    DateTimeOffset MaisAntiga,
    DateTimeOffset MaisRecente,
    IReadOnlyList<MensagemDaDlq> Exemplos);

internal static class ResumoDaDlq
{
    public const string SemMotivo = "(sem motivo)";

    // CONCEITO: agrupar por DeadLetterReason é o primeiro passo da triagem. Um motivo genérico
    // (MaxDeliveryCountExceeded) costuma misturar mensagens envenenadas com mensagens boas que só
    // pegaram uma falha transiente; as colunas mostram quantas de cada tipo há em cada motivo.
    public static IReadOnlyList<GrupoDaDlq> Agrupar(IEnumerable<MensagemDaDlq> mensagens, int exemplosPorMotivo = 3)
    {
        ArgumentNullException.ThrowIfNull(mensagens);
        ArgumentOutOfRangeException.ThrowIfNegative(exemplosPorMotivo);

        return mensagens
            .GroupBy(m => string.IsNullOrWhiteSpace(m.Motivo) ? SemMotivo : m.Motivo, StringComparer.Ordinal)
            .Select(grupo =>
            {
                var situacoes = grupo.Select(m => CorrecaoDeValor.Corrigir(m.Corpo)).ToList();
                return new GrupoDaDlq(
                    grupo.Key,
                    situacoes.Count,
                    situacoes.Count(s => s.Corrigiu),
                    situacoes.Count(s => s.PodeReprocessar && !s.Corrigiu),
                    situacoes.Count(s => !s.PodeReprocessar),
                    grupo.Min(m => m.EnfileiradaEm),
                    grupo.Max(m => m.EnfileiradaEm),
                    grupo.OrderBy(m => m.NumeroDeSequencia).Take(exemplosPorMotivo).ToList());
            })
            .OrderByDescending(g => g.Quantidade)
            .ThenBy(g => g.Motivo, StringComparer.Ordinal)
            .ToList();
    }
}
