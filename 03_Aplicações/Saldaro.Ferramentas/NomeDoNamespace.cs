namespace Saldaro.Ferramentas;

internal static class NomeDoNamespace
{
    public const string SufixoPublico = ".servicebus.windows.net";

    /// <summary>
    /// Aceita o FQDN (<c>sb-saldaro-ab12c.servicebus.windows.net</c>), o FQDN com esquema
    /// (<c>sb://.../</c>) ou só o nome curto (<c>sb-saldaro-ab12c</c>) e devolve o FQDN.
    /// </summary>
    public static string Normalizar(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var nome = valor.Trim();

        // CONCEITO: a ferramenta autentica por identidade (az login + RBAC). Uma connection string
        // carrega uma chave SAS; recusá-la aqui é a mesma lição do 3.5, em miniatura.
        if (nome.Contains("SharedAccessKey", StringComparison.OrdinalIgnoreCase)
            || nome.Contains("Endpoint=", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "passe o FQDN do namespace, não uma connection string: a ferramenta autentica com o seu az login (DefaultAzureCredential).");
        }

        var esquema = nome.IndexOf("://", StringComparison.Ordinal);
        if (esquema >= 0)
        {
            nome = nome[(esquema + 3)..];
        }

        nome = nome.TrimEnd('/');
        if (nome.Length == 0 || nome.Contains('/') || nome.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"namespace inválido: '{valor}'. Exemplo: sb-saldaro-ab12c{SufixoPublico}");
        }

        if (!nome.Contains('.'))
        {
            nome += SufixoPublico;
        }

        return nome.ToLowerInvariant();
    }
}
