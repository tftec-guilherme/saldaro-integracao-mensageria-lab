using System.Text;

namespace Saldaro.Ferramentas;

/// <summary>Texto de ajuda montado a partir das definições em <see cref="Argumentos"/>.</summary>
internal static class Ajuda
{
    private const string Prefixo = "dotnet run --project 03_Aplicações/Saldaro.Ferramentas --";

    public static string Texto(string? subcomando = null)
    {
        var definicao = Argumentos.Encontrar(subcomando);
        return definicao is null ? Geral() : DoSubcomando(definicao);
    }

    private static string Geral()
    {
        var texto = new StringBuilder();
        texto.AppendLine("Saldaro.Ferramentas: ferramentas de operação da aula (Saldaro Hub, empresa fictícia)");
        texto.AppendLine();
        texto.AppendLine("Uso:");
        texto.AppendLine($"  {Prefixo} <subcomando> [opções]");
        texto.AppendLine();
        texto.AppendLine("Subcomandos:");
        var largura = Argumentos.Subcomandos.Max(s => s.Nome.Length);
        foreach (var s in Argumentos.Subcomandos)
        {
            texto.AppendLine($"  {s.Nome.PadRight(largura)}  {s.Resumo}");
        }

        texto.AppendLine();
        texto.AppendLine("Autenticação: DefaultAzureCredential, ou seja, o seu `az login`. Nenhuma connection string.");
        texto.AppendLine("Códigos de saída: 0 sucesso, 1 falha, 2 uso inválido, 3 acesso negado (401/403).");
        texto.AppendLine("Opções de um subcomando: <subcomando> --ajuda");
        return texto.ToString().TrimEnd();
    }

    private static string DoSubcomando(DefinicaoDeSubcomando definicao)
    {
        var texto = new StringBuilder();
        texto.AppendLine($"{definicao.Nome}: {definicao.Resumo}");
        texto.AppendLine();
        texto.AppendLine("Uso:");
        var sintaxe = string.Join(' ', definicao.Opcoes.Select(Sintaxe));
        texto.AppendLine($"  {Prefixo} {definicao.Nome} {sintaxe}");
        texto.AppendLine();
        texto.AppendLine("Opções:");
        var rotulos = definicao.Opcoes.Select(o => o.Marcador is null ? o.Nome : $"{o.Nome} {o.Marcador}").ToList();
        var largura = rotulos.Max(r => r.Length);
        for (var i = 0; i < definicao.Opcoes.Count; i++)
        {
            var opcao = definicao.Opcoes[i];
            var extra = opcao.Obrigatoria ? " (obrigatória)" : opcao.Padrao is null ? string.Empty : $" (padrão: {opcao.Padrao})";
            texto.AppendLine($"  {rotulos[i].PadRight(largura)}  {opcao.Descricao}{extra}");
        }

        texto.AppendLine();
        texto.AppendLine("Exemplos:");
        foreach (var exemplo in definicao.Exemplos)
        {
            texto.AppendLine($"  {Prefixo} {exemplo}");
        }

        return texto.ToString().TrimEnd();
    }

    private static string Sintaxe(DefinicaoDeOpcao opcao)
    {
        var parte = opcao.Marcador is null ? opcao.Nome : $"{opcao.Nome} {opcao.Marcador}";
        return opcao.Obrigatoria ? parte : $"[{parte}]";
    }
}
