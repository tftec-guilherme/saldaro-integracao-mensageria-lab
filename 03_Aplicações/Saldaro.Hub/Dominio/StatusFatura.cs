namespace Saldaro.Hub.Dominio;

/// <summary>
/// Estados da fatura, na ordem do caminho feliz:
/// <c>recebida → cobranca-emitida → registrada → baixada → notificada</c>.
/// </summary>
public static class StatusFatura
{
    public const string Recebida = "recebida";
    public const string CobrancaEmitida = "cobranca-emitida";
    public const string Registrada = "registrada";
    public const string Baixada = "baixada";
    public const string Notificada = "notificada";

    /// <summary>Todos os estados, na ordem do caminho feliz.</summary>
    public static readonly IReadOnlyList<string> Todos =
        [Recebida, CobrancaEmitida, Registrada, Baixada, Notificada];

    /// <summary>A baixa já aconteceu (o <c>GET .../status</c> responde 303).</summary>
    public static bool JaBaixou(string status) => status is Baixada or Notificada;
}
