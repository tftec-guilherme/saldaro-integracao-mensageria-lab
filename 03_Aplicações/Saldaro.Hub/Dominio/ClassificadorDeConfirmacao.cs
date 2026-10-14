namespace Saldaro.Hub.Dominio;

/// <summary>
/// Como uma confirmação do banco deve ser tratada, com o que cada caminho precisa. Hierarquia fechada (o construtor
/// é privado, como o <c>ResultadoDoRegistro</c> do banco simulado): só existem os quatro casos abaixo, e o evento
/// aparece só nos casos em que ele foi lido.
/// </summary>
public abstract record Classificacao
{
    private Classificacao()
    {
    }

    /// <summary>Evento válido e sem falha: segue para inbox e máquina de estados.</summary>
    public sealed record Processavel(EventoBanco Evento) : Classificacao;

    /// <summary>Falha permanente: reentregar não conserta (ex.: <c>valor</c> como texto "1.250,00").</summary>
    public sealed record SchemaInvalida(string Erro) : Classificacao;

    /// <summary>Falha passageira: vale tentar de novo daqui a pouco (a próxima tentativa e o atraso dela).</summary>
    public sealed record Transiente(EventoBanco Evento, string Erro, int Tentativa, TimeSpan Atraso) : Classificacao;

    /// <summary>Falha passageira que já usou as 3 tentativas agendadas.</summary>
    public sealed record TentativasEsgotadas(EventoBanco Evento, string Erro) : Classificacao;
}

/// <summary>
/// Classifica a confirmação antes de qualquer efeito (seção 5.3, passos 1 e 2).
/// </summary>
/// <remarks>
/// CONCEITO: transiente × permanente. Uma DLQ útil separa "nunca vai dar certo" (schema inválido:
/// dead-letter já, com motivo) de "ainda não deu certo" (transiente: esperar e tentar de novo).
/// Misturar os dois na mesma DLQ, com o mesmo motivo genérico, é o Incidente 3.
/// </remarks>
public static class ClassificadorDeConfirmacao
{
    /// <param name="corpo">Corpo bruto da mensagem.</param>
    /// <param name="agora">Relógio atual (injetado, para teste).</param>
    /// <param name="tentativaAtual">Propriedade <c>tentativa</c> da mensagem (0 na original).</param>
    public static Classificacao Classificar(ReadOnlySpan<byte> corpo, DateTimeOffset agora, int tentativaAtual)
    {
        if (!LeitorDeEventoBanco.TentarLer(corpo, out var evento, out var erro))
        {
            return new Classificacao.SchemaInvalida(erro);
        }

        // Falha transiente simulada pelo banco: "simulaTransiente = true" e ainda não passou de "transienteAte".
        if (evento.SimulaTransiente && evento.TransienteAte is { } transienteAte && agora < transienteAte)
        {
            var retry = PoliticaDeRetry.Proxima(tentativaAtual);

            // Não mude o começo do texto: sem o retry agendado, ele é a mensagem da FalhaTransienteSimuladaException,
            // que o contrato (A2.5), o guia do Incidente 3 e o kql/erros-transientes-por-operacao.kql citam.
            var descricao = $"Falha transiente simulada até {transienteAte:O} (tentativa {tentativaAtual}).";
            return retry.Esgotou
                ? new Classificacao.TentativasEsgotadas(evento, descricao)
                : new Classificacao.Transiente(evento, descricao, retry.Tentativa, retry.Atraso);
        }

        return new Classificacao.Processavel(evento);
    }
}
