namespace Saldaro.Ferramentas.Tests;

public class ResumoDaDlqTests
{
    private const string Texto = """{"valor":"1.250,00"}""";
    private const string Numero = """{"valor":1250.0}""";
    private const string Invalido = """{"valor":"abc"}""";

    [Fact]
    public void Agrupa_por_motivo_e_separa_corrigiveis_das_demais()
    {
        MensagemDaDlq[] mensagens =
        [
            DecisaoDeTriagemTests.Mensagem(Texto, "MaxDeliveryCountExceeded", "a", sequencia: 1, enfileiradaEm: "2026-11-10T09:00:00Z"),
            DecisaoDeTriagemTests.Mensagem(Numero, "MaxDeliveryCountExceeded", "b", sequencia: 2, enfileiradaEm: "2026-11-10T09:05:00Z"),
            DecisaoDeTriagemTests.Mensagem(Texto, "MaxDeliveryCountExceeded", "c", sequencia: 3, enfileiradaEm: "2026-11-10T09:10:00Z"),
            DecisaoDeTriagemTests.Mensagem(Texto, "SchemaInvalida", "d", sequencia: 4),
            DecisaoDeTriagemTests.Mensagem(Texto, "SchemaInvalida", "e", sequencia: 5),
            DecisaoDeTriagemTests.Mensagem(Invalido, null, "f", sequencia: 6),
        ];

        var grupos = ResumoDaDlq.Agrupar(mensagens, exemplosPorMotivo: 2);

        Assert.Collection(
            grupos,
            g =>
            {
                Assert.Equal("MaxDeliveryCountExceeded", g.Motivo);
                Assert.Equal(3, g.Quantidade);
                Assert.Equal(2, g.Corrigiveis);
                Assert.Equal(1, g.SemCorrecao);
                Assert.Equal(0, g.NaoCorrigiveis);
                Assert.Equal(DateTimeOffset.Parse("2026-11-10T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture), g.MaisAntiga);
                Assert.Equal(DateTimeOffset.Parse("2026-11-10T09:10:00Z", System.Globalization.CultureInfo.InvariantCulture), g.MaisRecente);
                Assert.Equal(new[] { "a", "b" }, g.Exemplos.Select(e => e.MessageId));
            },
            g =>
            {
                Assert.Equal("SchemaInvalida", g.Motivo);
                Assert.Equal(2, g.Quantidade);
                Assert.Equal(2, g.Corrigiveis);
            },
            g =>
            {
                Assert.Equal(ResumoDaDlq.SemMotivo, g.Motivo);
                Assert.Equal(1, g.Quantidade);
                Assert.Equal(1, g.NaoCorrigiveis);
            });
    }

    [Fact]
    public void DLQ_vazia_nao_tem_grupos()
    {
        Assert.Empty(ResumoDaDlq.Agrupar([]));
    }
}
