using System.Text;

namespace Saldaro.Ferramentas;

/// <summary>Tabela de texto simples, com colunas alinhadas, para a saída no terminal.</summary>
internal static class Tabela
{
    public static string Montar(
        IReadOnlyList<string> cabecalho,
        IEnumerable<IReadOnlyList<string>> linhas,
        IReadOnlyList<bool>? alinharADireita = null)
    {
        ArgumentNullException.ThrowIfNull(cabecalho);
        ArgumentNullException.ThrowIfNull(linhas);

        var corpo = linhas.ToList();
        if (corpo.Any(l => l.Count != cabecalho.Count))
        {
            throw new ArgumentException("todas as linhas precisam ter o mesmo número de colunas do cabeçalho.", nameof(linhas));
        }

        var larguras = cabecalho
            .Select((titulo, i) => corpo.Select(l => l[i].Length).Append(titulo.Length).Max())
            .ToArray();

        var texto = new StringBuilder();
        Escrever(cabecalho);
        Escrever(larguras.Select(l => new string('-', l)).ToArray());
        foreach (var linha in corpo)
        {
            Escrever(linha);
        }

        return texto.ToString().TrimEnd();

        void Escrever(IReadOnlyList<string> celulas)
        {
            var linha = new StringBuilder();
            for (var i = 0; i < celulas.Count; i++)
            {
                if (i > 0)
                {
                    linha.Append("  ");
                }

                var direita = alinharADireita is not null && i < alinharADireita.Count && alinharADireita[i];
                linha.Append(direita ? celulas[i].PadLeft(larguras[i]) : celulas[i].PadRight(larguras[i]));
            }

            texto.AppendLine(linha.ToString().TrimEnd());
        }
    }
}
