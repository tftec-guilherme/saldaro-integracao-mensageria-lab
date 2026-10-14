namespace Saldaro.Hub.Dominio;

/// <summary>Motivos próprios de dead-letter (seção 7.4).</summary>
public static class MotivosDeadLetter
{
    public const string SchemaInvalida = "SchemaInvalida";
    public const string TentativasEsgotadas = "TentativasEsgotadas";

    /// <summary>Limite do Service Bus para <c>DeadLetterErrorDescription</c>.</summary>
    public const int TamanhoMaximoDaDescricao = 1024;

    public static string Truncar(string descricao) =>
        descricao.Length <= TamanhoMaximoDaDescricao ? descricao : descricao[..TamanhoMaximoDaDescricao];
}
