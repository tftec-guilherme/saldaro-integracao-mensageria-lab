namespace Saldaro.Hub.Dominio;

/// <summary>
/// A fatura, entidade raiz do hub. Uma linha da tabela <c>faturas</c>
/// (PK = <c>faturaId</c>, RK = <c>"fatura"</c>; colunas da seção 5.4).
/// </summary>
public sealed class Fatura
{
    public required string FaturaId { get; init; }
    public string ClienteId { get; set; } = string.Empty;

    /// <summary>
    /// Valor em reais. No código é <c>decimal</c>; o Table Storage não tem tipo decimal, então a
    /// coluna <c>valor</c> é gravada como double (simplificação de laboratório do contrato).
    /// </summary>
    public decimal Valor { get; set; }

    public string? Vencimento { get; set; }
    public string? PagadorCnpj { get; set; }
    public string? ReferenciaErp { get; set; }
    public string Status { get; set; } = StatusFatura.Recebida;
    public string? IdCobranca { get; set; }
    public int QuantidadeBaixas { get; set; }
    public DateTimeOffset? BaixadaEm { get; set; }
    public string? CallbackUrl { get; set; }

    /// <summary>Botões de caos repassados ao banco na query string (ex.: <c>latencia=45&amp;duplicar=3</c>).</summary>
    public string? Caos { get; set; }

    public DateTimeOffset CriadaEm { get; set; }
    public DateTimeOffset AtualizadaEm { get; set; }

    /// <summary>
    /// Versão da linha devolvida pelo Table Storage (ETag). Vazia em fatura nova.
    /// É ela que permite a concorrência otimista: "grave só se ninguém mudou desde que eu li".
    /// </summary>
    public string? ETag { get; set; }
}

/// <summary>Projeção leve usada pela conciliação (só o que ela precisa ler).</summary>
public sealed record ResumoFatura(string FaturaId, string Status, DateTimeOffset AtualizadaEm);
