namespace Saldaro.Ferramentas;

/// <summary>Códigos de saída do processo (úteis para scripts que chamam a ferramenta).</summary>
internal static class CodigosDeSaida
{
    public const int Sucesso = 0;

    /// <summary>Falha de execução: Azure, rede, fila inexistente, Ctrl+C.</summary>
    public const int Falha = 1;

    /// <summary>Argumentos inválidos: subcomando ou opção desconhecida, valor fora da faixa.</summary>
    public const int UsoInvalido = 2;

    /// <summary>401/403: a identidade não tem a role necessária no namespace.</summary>
    public const int AcessoNegado = 3;
}
