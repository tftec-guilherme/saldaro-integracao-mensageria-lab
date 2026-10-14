using System.Globalization;

namespace Saldaro.Ferramentas;

internal sealed record IdReprocessado(string MessageId, string Original, int Numero);

/// <summary>Monta o MessageId da cópia reprocessada: <c>{original}-r{n}</c>.</summary>
internal static class IdDeReprocesso
{
    /// <summary>Propriedade de aplicação que guarda o MessageId original da mensagem.</summary>
    public const string Propriedade = "reprocessadoDe";

    /// <summary>Limite do Service Bus para o tamanho do MessageId.</summary>
    public const int TamanhoMaximo = 128;

    // CONCEITO: o reprocessamento precisa de MessageId NOVO. Com duplicate detection ligado, reprocessar com o
    // mesmo MessageId dentro da janela (10 min) faz o Service Bus descartar a cópia em silêncio.
    // E o id novo é determinístico ({original}-r1, -r2...): se o mesmo reprocessamento for repetido,
    // o duplicate detection descarta a repetição, em vez de baixar a fatura duas vezes.
    public static IdReprocessado Montar(string messageIdAtual, string? reprocessadoDe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageIdAtual);

        var jaReprocessada = !string.IsNullOrWhiteSpace(reprocessadoDe);
        var original = jaReprocessada ? reprocessadoDe! : messageIdAtual;
        var numero = 1;
        if (jaReprocessada)
        {
            // A mensagem já é uma cópia ({original}-r{n}) que voltou para a DLQ: a próxima é n+1.
            var prefixo = original + "-r";
            if (messageIdAtual.StartsWith(prefixo, StringComparison.Ordinal)
                && int.TryParse(messageIdAtual.AsSpan(prefixo.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var anterior))
            {
                numero = anterior + 1;
            }
        }

        var novo = $"{original}-r{numero.ToString(CultureInfo.InvariantCulture)}";
        if (novo.Length > TamanhoMaximo)
        {
            throw new ArgumentException(
                $"o MessageId reprocessado passaria de {TamanhoMaximo} caracteres: '{novo}'.", nameof(messageIdAtual));
        }

        return new IdReprocessado(novo, original, numero);
    }
}
