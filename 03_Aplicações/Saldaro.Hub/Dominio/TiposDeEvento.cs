namespace Saldaro.Hub.Dominio;

/// <summary>Tipos de evento do webhook do banco (seção 7.1).</summary>
public static class TiposDeEvento
{
    public const string CobrancaRegistrada = "cobranca.registrada";
    public const string CobrancaPaga = "cobranca.paga";

    public static bool EhConhecido(string? tipo) => tipo is CobrancaRegistrada or CobrancaPaga;
}
