using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace Saldaro.Ferramentas.Tests;

public class DecisaoDeTriagemTests
{
    private const string CorpoComTexto = """{"idEvento":"evt-1","tipo":"cobranca.paga","faturaId":"fatura-1","valor":"1.250,00"}""";
    private const string CorpoNumerico = """{"idEvento":"evt-1","tipo":"cobranca.paga","faturaId":"fatura-1","valor":1250.0}""";

    [Fact]
    public void Reprocessar_corrige_o_valor_e_monta_a_copia_com_id_novo_e_mesma_sessao()
    {
        var mensagem = Mensagem(CorpoComTexto, propriedades: new Dictionary<string, object>
        {
            ["tipo"] = "cobranca.paga",
            ["Diagnostic-Id"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            ["DeadLetterReason"] = "MaxDeliveryCountExceeded",
            ["DeadLetterErrorDescription"] = "Message could not be consumed after 10 delivery attempts.",
            ["tentativa"] = 3,
        });

        var acao = DecisaoDeTriagem.Decidir(mensagem, reprocessar: true, descartarMotivo: null);

        var nova = Assert.IsType<AcaoDeTriagem.Reprocessar>(acao).NovaMensagem;
        Assert.Equal("evt-1-r1", nova.MessageId);
        Assert.Equal("evt-1", nova.Original);
        Assert.Equal("fatura-1", nova.SessionId);
        Assert.Equal("application/json", nova.ContentType);
        Assert.True(nova.ValorCorrigido);

        Assert.Equal("evt-1", nova.Propriedades[IdDeReprocesso.Propriedade]);
        Assert.Equal("cobranca.paga", nova.Propriedades["tipo"]);
        Assert.True(nova.Propriedades.ContainsKey("Diagnostic-Id"));
        Assert.False(nova.Propriedades.ContainsKey("DeadLetterReason"));
        Assert.False(nova.Propriedades.ContainsKey("DeadLetterErrorDescription"));
        Assert.False(nova.Propriedades.ContainsKey("tentativa"));

        using var corpo = JsonDocument.Parse(nova.Corpo.ToString());
        Assert.Equal(1250.00m, corpo.RootElement.GetProperty("valor").GetDecimal());
    }

    [Fact]
    public void A_copia_vira_uma_ServiceBusMessage_com_os_mesmos_dados()
    {
        var mensagem = Mensagem(CorpoComTexto, propriedades: new Dictionary<string, object> { ["tipo"] = "cobranca.paga" });
        var nova = Assert.IsType<AcaoDeTriagem.Reprocessar>(DecisaoDeTriagem.Decidir(mensagem, reprocessar: true, descartarMotivo: null)).NovaMensagem;

        var servicebus = nova.ParaServiceBus();

        Assert.Equal("evt-1-r1", servicebus.MessageId);
        Assert.Equal("fatura-1", servicebus.SessionId);
        Assert.Equal("application/json", servicebus.ContentType);
        Assert.Equal("evt-1", servicebus.ApplicationProperties[IdDeReprocesso.Propriedade]);
        Assert.Equal("cobranca.paga", servicebus.ApplicationProperties["tipo"]);
        Assert.Equal(nova.Corpo.ToString(), servicebus.Body.ToString());
    }

    [Fact]
    public void Valor_ja_numerico_e_reprocessado_sem_alteracao()
    {
        var mensagem = Mensagem(CorpoNumerico);

        var acao = DecisaoDeTriagem.Decidir(mensagem, reprocessar: true, descartarMotivo: null);

        var nova = Assert.IsType<AcaoDeTriagem.Reprocessar>(acao).NovaMensagem;
        Assert.False(nova.ValorCorrigido);
        Assert.Equal(CorpoNumerico, nova.Corpo.ToString());
    }

    [Fact]
    public void Copia_reprocessada_que_voltou_para_a_DLQ_ganha_o_proximo_numero()
    {
        var mensagem = Mensagem(
            CorpoComTexto,
            messageId: "evt-1-r1",
            propriedades: new Dictionary<string, object> { [IdDeReprocesso.Propriedade] = "evt-1" });

        var nova = Assert.IsType<AcaoDeTriagem.Reprocessar>(DecisaoDeTriagem.Decidir(mensagem, reprocessar: true, descartarMotivo: null)).NovaMensagem;

        Assert.Equal("evt-1-r2", nova.MessageId);
        Assert.Equal("evt-1", nova.Propriedades[IdDeReprocesso.Propriedade]);
    }

    [Theory]
    [InlineData("TentativasEsgotadas", "TentativasEsgotadas")]
    [InlineData("TentativasEsgotadas", "tentativasesgotadas")]
    [InlineData("SchemaInvalida", " SchemaInvalida ")]
    public void Descarta_so_o_motivo_pedido(string motivo, string descartarMotivo)
    {
        var acao = DecisaoDeTriagem.Decidir(Mensagem(CorpoComTexto, motivo), reprocessar: true, descartarMotivo);

        Assert.IsType<AcaoDeTriagem.Descartar>(acao);
    }

    [Fact]
    public void Com_descartar_motivo_e_sem_reprocessar_as_demais_ficam_na_DLQ()
    {
        var acao = DecisaoDeTriagem.Decidir(Mensagem(CorpoComTexto, "SchemaInvalida"), reprocessar: false, "TentativasEsgotadas");

        Assert.IsType<AcaoDeTriagem.Manter>(acao);
    }

    [Fact]
    public void Com_descartar_motivo_e_reprocessar_as_demais_sao_reprocessadas()
    {
        var acao = DecisaoDeTriagem.Decidir(Mensagem(CorpoComTexto, "SchemaInvalida"), reprocessar: true, "TentativasEsgotadas");

        Assert.IsType<AcaoDeTriagem.Reprocessar>(acao);
    }

    [Fact]
    public void Sem_nenhuma_acao_pedida_nada_muda()
    {
        var acao = DecisaoDeTriagem.Decidir(Mensagem(CorpoComTexto), reprocessar: false, descartarMotivo: null);

        Assert.IsType<AcaoDeTriagem.Manter>(acao);
    }

    [Fact]
    public void Valor_nao_corrigivel_fica_na_DLQ()
    {
        var acao = DecisaoDeTriagem.Decidir(Mensagem("""{"idEvento":"evt-1","valor":"abc"}"""), reprocessar: true, descartarMotivo: null);

        var manter = Assert.IsType<AcaoDeTriagem.Manter>(acao);
        Assert.Contains("abc", manter.Explicacao);
    }

    [Fact]
    public void Le_o_motivo_e_a_descricao_da_mensagem_recebida_do_SDK()
    {
        var recebida = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(CorpoComTexto),
            messageId: "evt-1",
            sessionId: "fatura-1",
            contentType: "application/json",
            properties: new Dictionary<string, object>
            {
                ["DeadLetterReason"] = "SchemaInvalida",
                ["DeadLetterErrorDescription"] = "valor em texto: '1.250,00'",
                ["tipo"] = "cobranca.paga",
            },
            deliveryCount: 1,
            sequenceNumber: 42,
            enqueuedTime: DateTimeOffset.Parse("2026-11-10T09:30:00Z", System.Globalization.CultureInfo.InvariantCulture));

        var mensagem = MensagemDaDlq.De(recebida);

        Assert.Equal("SchemaInvalida", mensagem.Motivo);
        Assert.Equal("valor em texto: '1.250,00'", mensagem.Descricao);
        Assert.Equal(42, mensagem.NumeroDeSequencia);
        Assert.Equal("evt-1", mensagem.MessageId);
        Assert.Equal("fatura-1", mensagem.SessionId);
        Assert.Equal(1, mensagem.Entregas);
        Assert.Equal("cobranca.paga", mensagem.Propriedade("tipo"));
        Assert.Null(mensagem.Propriedade("inexistente"));
    }

    internal static MensagemDaDlq Mensagem(
        string corpo,
        string? motivo = "MaxDeliveryCountExceeded",
        string messageId = "evt-1",
        string? sessionId = "fatura-1",
        IDictionary<string, object>? propriedades = null,
        long sequencia = 1,
        string enfileiradaEm = "2026-11-10T09:30:00Z") =>
        new(
            sequencia,
            messageId,
            sessionId,
            motivo,
            "descrição de teste",
            DateTimeOffset.Parse(enfileiradaEm, System.Globalization.CultureInfo.InvariantCulture),
            10,
            "application/json",
            null,
            null,
            new Dictionary<string, object>(propriedades ?? new Dictionary<string, object>(), StringComparer.Ordinal),
            BinaryData.FromString(corpo));
}
