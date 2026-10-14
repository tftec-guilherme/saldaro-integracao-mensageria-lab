using System.Globalization;

namespace Saldaro.BancoSimulado.Dominio;

/// <summary>
/// Os "botões de caos" do banco simulado, lidos da query string de <c>POST /api/cobrancas</c>
/// (contrato, seção 6). O hub repassa o campo <c>caos</c> da fatura sem mexer.
/// </summary>
// CONCEITO: o banco parceiro é a parte do sistema que a Saldaro não controla. Cada botão liga,
// sob comando, um comportamento que parceiros reais têm: demorar, repetir, embaralhar, mudar o formato.
public sealed record BotoesDeCaos
{
    public const int LatenciaMaxima = 200;
    public const int AtrasoWebhookMinimo = 2;
    public const int AtrasoWebhookMaximo = 3600;
    public const int DuplicarMaximo = 5;

    /// <summary>Segundos que o banco demora para responder, já tendo registrado a cobrança.</summary>
    public int Latencia { get; init; }

    /// <summary>
    /// Segundos até o <c>cobranca.paga</c> (o <c>cobranca.registrada</c> vai em 1 s). Os tempos dos
    /// webhooks contam a partir da resposta ao hub, ou seja, depois da <see cref="Latencia"/>.
    /// </summary>
    public int AtrasoWebhook { get; init; } = 5;

    /// <summary>Quantas vezes cada evento é enviado, sempre com o mesmo <c>idEvento</c>.</summary>
    public int Duplicar { get; init; } = 1;

    /// <summary>Inverte a ordem: <c>cobranca.paga</c> em 1 s e <c>cobranca.registrada</c> em <c>AtrasoWebhook + 3</c> s (contados da resposta).</summary>
    public bool Embaralhar { get; init; }

    /// <summary><c>assinatura=invalida</c>: o banco corrompe o hex da assinatura.</summary>
    public bool AssinaturaInvalida { get; init; }

    /// <summary><c>valor</c> sai como texto pt-BR (<c>"1.250,00"</c>) em vez de número.</summary>
    public bool ValorComVirgula { get; init; }

    /// <summary>Percentual (0 a 100) de POSTs que respondem 503 e de <c>cobranca.paga</c> marcados como transientes.</summary>
    public int FalhaTransiente { get; init; }

    public static BotoesDeCaos Padrao { get; } = new();

    /// <summary>Resumo dos valores efetivos, no mesmo formato da query string.</summary>
    public string Descrever() => string.Create(CultureInfo.InvariantCulture,
        $"latencia={Latencia}&atrasoWebhook={AtrasoWebhook}&duplicar={Duplicar}" +
        $"&embaralhar={(Embaralhar ? "true" : "false")}&assinatura={(AssinaturaInvalida ? "invalida" : "valida")}" +
        $"&valorComVirgula={(ValorComVirgula ? "true" : "false")}&falhaTransiente={FalhaTransiente}");

    /// <summary>
    /// Lê os botões de uma query string (<c>latencia=45&amp;duplicar=3</c>; aceita <c>;</c> como separador).
    /// Leitor tolerante: o nome não diferencia maiúsculas, valor inválido volta ao padrão e valor fora
    /// da faixa é limitado; cada ajuste vira um aviso para o log, em vez de derrubar a chamada.
    /// </summary>
    public static LeituraDosBotoes Interpretar(string? queryString)
    {
        var botoes = Padrao;
        var avisos = new List<string>();
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return new LeituraDosBotoes(botoes, avisos);
        }

        var pares = queryString.TrimStart('?').Split(['&', ';'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var par in pares)
        {
            var posicaoDoIgual = par.IndexOf('=');
            var nome = Decodificar(posicaoDoIgual < 0 ? par : par[..posicaoDoIgual]);
            var valor = posicaoDoIgual < 0 ? string.Empty : Decodificar(par[(posicaoDoIgual + 1)..]);

            switch (nome.ToLowerInvariant())
            {
                case "latencia":
                    botoes = botoes with { Latencia = LerInteiro(nome, valor, 0, LatenciaMaxima, Padrao.Latencia, avisos) };
                    break;
                case "atrasowebhook":
                    botoes = botoes with { AtrasoWebhook = LerInteiro(nome, valor, AtrasoWebhookMinimo, AtrasoWebhookMaximo, Padrao.AtrasoWebhook, avisos) };
                    break;
                case "duplicar":
                    botoes = botoes with { Duplicar = LerInteiro(nome, valor, 1, DuplicarMaximo, Padrao.Duplicar, avisos) };
                    break;
                case "embaralhar":
                    botoes = botoes with { Embaralhar = LerBooleano(nome, valor, Padrao.Embaralhar, avisos) };
                    break;
                case "assinatura":
                    botoes = botoes with { AssinaturaInvalida = LerAssinatura(nome, valor, avisos) };
                    break;
                case "valorcomvirgula":
                    botoes = botoes with { ValorComVirgula = LerBooleano(nome, valor, Padrao.ValorComVirgula, avisos) };
                    break;
                case "falhatransiente":
                    botoes = botoes with { FalhaTransiente = LerInteiro(nome, valor, 0, 100, Padrao.FalhaTransiente, avisos) };
                    break;
                case "code":
                    // Chave de função do Azure Functions, se alguém a passar na URL: não é botão de caos.
                    break;
                default:
                    avisos.Add($"parâmetro desconhecido ignorado: '{nome}'");
                    break;
            }
        }

        return new LeituraDosBotoes(botoes, avisos);
    }

    private static string Decodificar(string texto) => Uri.UnescapeDataString(texto.Replace('+', ' ')).Trim();

    private static int LerInteiro(string nome, string valor, int minimo, int maximo, int padrao, List<string> avisos)
    {
        if (!int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero))
        {
            avisos.Add($"{nome}='{valor}' não é um número inteiro; usando o padrão {padrao}");
            return padrao;
        }

        if (numero < minimo || numero > maximo)
        {
            var ajustado = Math.Clamp(numero, minimo, maximo);
            avisos.Add($"{nome}={numero} está fora da faixa de {minimo} a {maximo}; usando {ajustado}");
            return ajustado;
        }

        return numero;
    }

    private static bool LerBooleano(string nome, string valor, bool padrao, List<string> avisos)
    {
        switch (valor.ToLowerInvariant())
        {
            case "":
            case "true":
            case "1":
                return true;
            case "false":
            case "0":
                return false;
            default:
                avisos.Add($"{nome}='{valor}' não é true nem false; usando o padrão {(padrao ? "true" : "false")}");
                return padrao;
        }
    }

    private static bool LerAssinatura(string nome, string valor, List<string> avisos)
    {
        switch (valor.ToLowerInvariant())
        {
            case "valida":
            case "válida":
                return false;
            case "invalida":
            case "inválida":
                return true;
            default:
                avisos.Add($"{nome}='{valor}' não é valida nem invalida; usando o padrão valida");
                return false;
        }
    }
}

/// <summary>Resultado da leitura: os botões efetivos e os avisos de valores ajustados ou ignorados.</summary>
public sealed record LeituraDosBotoes(BotoesDeCaos Botoes, IReadOnlyList<string> Avisos);
