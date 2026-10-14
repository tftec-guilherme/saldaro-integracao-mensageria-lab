using System.Globalization;

namespace Saldaro.Ferramentas;

internal enum TipoDeOpcao
{
    /// <summary>Recebe um texto: <c>--fila sbq-confirmacoes-banco</c>.</summary>
    Texto,

    /// <summary>Recebe um inteiro dentro de uma faixa: <c>--max 500</c>.</summary>
    Inteiro,

    /// <summary>Liga um comportamento, sem valor: <c>--reprocessar</c>.</summary>
    Interruptor,
}

internal sealed record DefinicaoDeOpcao(
    string Nome,
    TipoDeOpcao Tipo,
    string Descricao,
    bool Obrigatoria = false,
    string? Padrao = null,
    int Minimo = 1,
    int Maximo = int.MaxValue,
    string? Marcador = null);

internal sealed record DefinicaoDeSubcomando(
    string Nome,
    string Resumo,
    IReadOnlyList<DefinicaoDeOpcao> Opcoes,
    IReadOnlyList<string> Exemplos);

/// <summary>Argumentos já validados de um subcomando, com os padrões aplicados.</summary>
internal sealed class ArgumentosAnalisados(
    string subcomando,
    IReadOnlyDictionary<string, string> valores,
    IReadOnlySet<string> ligadas)
{
    public string Subcomando { get; } = subcomando;

    public string Texto(string opcao) =>
        valores.TryGetValue(opcao, out var valor) ? valor : throw new KeyNotFoundException($"opção sem valor: {opcao}");

    public string? TextoOpcional(string opcao) => valores.GetValueOrDefault(opcao);

    public int Inteiro(string opcao) => int.Parse(Texto(opcao), NumberStyles.None, CultureInfo.InvariantCulture);

    public bool Ligada(string opcao) => ligadas.Contains(opcao);
}

internal sealed record ResultadoDaAnalise(
    ArgumentosAnalisados? Argumentos,
    string? Erro,
    bool PedeAjuda,
    string? SubcomandoDaAjuda)
{
    public static ResultadoDaAnalise Ajuda(string? subcomando = null) => new(null, null, true, subcomando);

    public static ResultadoDaAnalise Falha(string erro, string? subcomando = null) => new(null, erro, false, subcomando);

    public static ResultadoDaAnalise Ok(ArgumentosAnalisados argumentos) => new(argumentos, null, false, argumentos.Subcomando);
}

/// <summary>
/// Parser de argumentos propositalmente simples (sem biblioteca externa): subcomando na primeira
/// posição e opções no formato <c>--nome valor</c>, <c>--nome=valor</c> ou só <c>--nome</c> (interruptor, sem valor).
/// </summary>
internal static class Argumentos
{
    public const string Triagem = "triagem";
    public const string LegadoAutoCreate = "legado-auto-create";
    public const string Rajada = "rajada";

    private const string ExemploDeNamespace = "sb-saldaro-ab12c.servicebus.windows.net";

    private static readonly string[] PedidosDeAjuda = ["--ajuda", "--help", "-h", "-?", "/?", "ajuda", "help"];

    private static DefinicaoDeOpcao OpcaoNamespace() => new(
        "--namespace",
        TipoDeOpcao.Texto,
        $"FQDN do namespace do Service Bus (ex.: {ExemploDeNamespace}); está em SALDARO_SB_FQDN no scripts/.env.local",
        Obrigatoria: true,
        Marcador: "<fqdn>");

    public static IReadOnlyList<DefinicaoDeSubcomando> Subcomandos { get; } =
    [
        new(
            Triagem,
            "Lê a DLQ de uma fila sem alterar nada (peek), agrupa por DeadLetterReason e só age com --reprocessar ou --descartar-motivo.",
            [
                OpcaoNamespace(),
                new("--fila", TipoDeOpcao.Texto, "fila cuja DLQ será lida (ex.: sbq-confirmacoes-banco)", Obrigatoria: true, Marcador: "<nome>"),
                new("--max", TipoDeOpcao.Inteiro, "máximo de mensagens lidas da DLQ", Padrao: "500", Maximo: 5000, Marcador: "<n>"),
                new("--reprocessar", TipoDeOpcao.Interruptor,"corrige o valor em texto pt-BR (\"1.250,00\" -> 1250.00), reprocessa na fila principal com MessageId {original}-r{n} e tira a mensagem da DLQ"),
                new("--descartar-motivo", TipoDeOpcao.Texto, "descarta (completa) as mensagens da DLQ com este DeadLetterReason; pode combinar com --reprocessar", Marcador: "<motivo>"),
            ],
            [
                $"triagem --namespace {ExemploDeNamespace} --fila sbq-confirmacoes-banco",
                $"triagem --namespace {ExemploDeNamespace} --fila sbq-confirmacoes-banco --reprocessar",
                $"triagem --namespace {ExemploDeNamespace} --fila sbq-confirmacoes-banco --descartar-motivo TentativasEsgotadas",
            ]),
        new(
            LegadoAutoCreate,
            $"\"Worker legado\" do 3.7: cria a fila {ComandoLegadoAutoCreate.FilaDoLegado} (configuração padrão) se ela não existir e envia mensagens para ela. Falha com 401 se a identidade só tiver Data Sender/Receiver.",
            [
                OpcaoNamespace(),
                new("--mensagens", TipoDeOpcao.Inteiro, "quantas mensagens o legado envia", Padrao: "20", Maximo: 1000, Marcador: "<n>"),
            ],
            [
                $"legado-auto-create --namespace {ExemploDeNamespace}",
                $"legado-auto-create --namespace {ExemploDeNamespace} --mensagens 50",
            ]),
        new(
            Rajada,
            "Envio concorrente para o exercício de throttling do 3.8. Use uma fila descartável (o guia cria e apaga a sbq-rajada).",
            [
                OpcaoNamespace(),
                new("--fila", TipoDeOpcao.Texto, "fila descartável que recebe a rajada (ex.: sbq-rajada); as filas do hub são recusadas", Obrigatoria: true, Marcador: "<nome>"),
                new("--remetentes", TipoDeOpcao.Inteiro, "remetentes concorrentes", Padrao: "50", Maximo: 200, Marcador: "<n>"),
                new("--mensagens", TipoDeOpcao.Inteiro, "mensagens por remetente", Padrao: "100", Maximo: 1000, Marcador: "<n>"),
            ],
            [
                $"rajada --namespace {ExemploDeNamespace} --fila sbq-rajada",
                $"rajada --namespace {ExemploDeNamespace} --fila sbq-rajada --remetentes 50 --mensagens 100",
            ]),
    ];

    public static DefinicaoDeSubcomando? Encontrar(string? nome) =>
        Subcomandos.FirstOrDefault(s => string.Equals(s.Nome, nome, StringComparison.OrdinalIgnoreCase));

    public static ResultadoDaAnalise Analisar(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Count == 0 || EhPedidoDeAjuda(args[0]))
        {
            return ResultadoDaAnalise.Ajuda();
        }

        var definicao = Encontrar(args[0]);
        if (definicao is null)
        {
            var nomes = string.Join(", ", Subcomandos.Select(s => s.Nome));
            return ResultadoDaAnalise.Falha($"subcomando desconhecido: '{args[0]}'. Use um destes: {nomes}.");
        }

        var valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ligadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < args.Count; i++)
        {
            var token = args[i];
            if (EhPedidoDeAjuda(token))
            {
                return ResultadoDaAnalise.Ajuda(definicao.Nome);
            }

            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                return ResultadoDaAnalise.Falha($"argumento inesperado: '{token}'. As opções começam com --.", definicao.Nome);
            }

            var nome = token;
            string? valor = null;
            var igual = token.IndexOf('=', StringComparison.Ordinal);
            if (igual > 2)
            {
                nome = token[..igual];
                valor = token[(igual + 1)..];
            }

            var opcao = definicao.Opcoes.FirstOrDefault(o => string.Equals(o.Nome, nome, StringComparison.OrdinalIgnoreCase));
            if (opcao is null)
            {
                return ResultadoDaAnalise.Falha($"opção desconhecida para {definicao.Nome}: '{nome}'.", definicao.Nome);
            }

            if (valores.ContainsKey(opcao.Nome) || ligadas.Contains(opcao.Nome))
            {
                return ResultadoDaAnalise.Falha($"opção repetida: {opcao.Nome}.", definicao.Nome);
            }

            if (opcao.Tipo == TipoDeOpcao.Interruptor)
            {
                if (valor is not null)
                {
                    return ResultadoDaAnalise.Falha($"a opção {opcao.Nome} não recebe valor.", definicao.Nome);
                }

                ligadas.Add(opcao.Nome);
                continue;
            }

            if (valor is null)
            {
                if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    return ResultadoDaAnalise.Falha($"a opção {opcao.Nome} precisa de um valor.", definicao.Nome);
                }

                valor = args[++i];
            }

            valor = valor.Trim();
            if (valor.Length == 0)
            {
                return ResultadoDaAnalise.Falha($"a opção {opcao.Nome} precisa de um valor.", definicao.Nome);
            }

            if (opcao.Tipo == TipoDeOpcao.Inteiro
                && (!int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var numero)
                    || numero < opcao.Minimo
                    || numero > opcao.Maximo))
            {
                return ResultadoDaAnalise.Falha(
                    $"a opção {opcao.Nome} espera um inteiro entre {opcao.Minimo} e {opcao.Maximo}; recebeu '{valor}'.",
                    definicao.Nome);
            }

            valores[opcao.Nome] = valor;
        }

        foreach (var opcao in definicao.Opcoes.Where(o => o.Tipo != TipoDeOpcao.Interruptor && !valores.ContainsKey(o.Nome)))
        {
            if (opcao.Obrigatoria)
            {
                return ResultadoDaAnalise.Falha($"falta a opção obrigatória {opcao.Nome}.", definicao.Nome);
            }

            if (opcao.Padrao is not null)
            {
                valores[opcao.Nome] = opcao.Padrao;
            }
        }

        return ResultadoDaAnalise.Ok(new ArgumentosAnalisados(definicao.Nome, valores, ligadas));
    }

    private static bool EhPedidoDeAjuda(string token) =>
        PedidosDeAjuda.Contains(token, StringComparer.OrdinalIgnoreCase);
}
