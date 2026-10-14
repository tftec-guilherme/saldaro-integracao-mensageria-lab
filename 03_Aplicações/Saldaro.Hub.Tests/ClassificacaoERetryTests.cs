using System.Text;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Tests;

/// <summary>Classificação de erro (schema × transiente × tentativas esgotadas) e cálculo do retry agendado.</summary>
public class ClassificacaoERetryTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 9, 30, 0, TimeSpan.Zero);

    internal static string Evento(
        string valor = "1250.00",
        string tipo = "cobranca.paga",
        bool simulaTransiente = false,
        DateTimeOffset? transienteAte = null,
        string idEvento = "11111111-1111-1111-1111-111111111111",
        string faturaId = "22222222-2222-2222-2222-222222222222")
    {
        var ate = transienteAte is null ? "null" : $"\"{transienteAte.Value:O}\"";
        return $"{{\"idEvento\":\"{idEvento}\",\"tipo\":\"{tipo}\",\"faturaId\":\"{faturaId}\",\"idCobranca\":\"COB-0a1b2c3d\",\"valor\":{valor},\"ocorridoEm\":\"2026-11-10T09:30:00Z\",\"simulaTransiente\":{(simulaTransiente ? "true" : "false")},\"transienteAte\":{ate}}}";
    }

    private static Classificacao Classificar(string json, int tentativa = 0) =>
        ClassificadorDeConfirmacao.Classificar(Encoding.UTF8.GetBytes(json), Agora, tentativa);

    [Fact]
    public void Evento_valido_e_processavel()
    {
        var resultado = Classificar(Evento());

        var processavel = Assert.IsType<Classificacao.Processavel>(resultado);
        Assert.Equal(1250.00m, processavel.Evento.Valor);
        Assert.Equal("22222222-2222-2222-2222-222222222222", processavel.Evento.FaturaId);
    }

    [Fact]
    public void Valor_como_texto_com_virgula_e_schema_invalida()
    {
        // valorComVirgula=true: a atualização da API do banco mandou "1.250,00".
        var resultado = Classificar(Evento(valor: "\"1.250,00\""));

        var schemaInvalida = Assert.IsType<Classificacao.SchemaInvalida>(resultado);
        Assert.Contains("$.valor", schemaInvalida.Erro);
    }

    [Theory]
    [InlineData("{\"tipo\":\"cobranca.paga\",\"faturaId\":\"f-1\",\"valor\":10}", "idEvento")]
    [InlineData("{\"idEvento\":\"e-1\",\"tipo\":\"cobranca.paga\",\"valor\":10}", "faturaId")]
    [InlineData("{\"idEvento\":\"e-1\",\"tipo\":\"cobranca.estornada\",\"faturaId\":\"f-1\",\"valor\":10}", "tipo")]
    [InlineData("{\"idEvento\":\"e-1\",\"tipo\":\"cobranca.paga\",\"faturaId\":\"f-1\"}", "valor")]
    [InlineData("{\"idEvento\":\"e/1\",\"tipo\":\"cobranca.paga\",\"faturaId\":\"f-1\",\"valor\":10}", "idEvento")]
    [InlineData("isto não é JSON", "schema")]
    [InlineData("null", "nulo")]
    public void Campos_obrigatorios_ausentes_ou_invalidos_sao_schema_invalida(string json, string trecho)
    {
        var resultado = Classificar(json);

        var schemaInvalida = Assert.IsType<Classificacao.SchemaInvalida>(resultado);
        Assert.Contains(trecho, schemaInvalida.Erro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Falha_transiente_dentro_do_prazo_agenda_a_primeira_tentativa_em_10_segundos()
    {
        var resultado = Classificar(Evento(simulaTransiente: true, transienteAte: Agora.AddSeconds(45)));

        var transiente = Assert.IsType<Classificacao.Transiente>(resultado);
        Assert.Equal(1, transiente.Tentativa);
        Assert.Equal(TimeSpan.FromSeconds(10), transiente.Atraso);
    }

    [Fact]
    public void Segunda_tentativa_ainda_transiente_agenda_a_terceira_em_40_segundos()
    {
        var resultado = Classificar(Evento(simulaTransiente: true, transienteAte: Agora.AddSeconds(15)), tentativa: 2);

        var transiente = Assert.IsType<Classificacao.Transiente>(resultado);
        Assert.Equal(3, transiente.Tentativa);
        Assert.Equal(TimeSpan.FromSeconds(40), transiente.Atraso);
    }

    [Fact]
    public void Terceira_tentativa_ainda_transiente_esgota_as_tentativas()
    {
        var resultado = Classificar(Evento(simulaTransiente: true, transienteAte: Agora.AddMinutes(5)), tentativa: 3);

        var esgotadas = Assert.IsType<Classificacao.TentativasEsgotadas>(resultado);
        Assert.Equal("22222222-2222-2222-2222-222222222222", esgotadas.Evento.FaturaId);

        // O contrato (A2.5) e o guia do Incidente 3 contam com este começo de texto.
        Assert.StartsWith("Falha transiente simulada até", esgotadas.Erro, StringComparison.Ordinal);
    }

    [Fact]
    public void Falha_transiente_vencida_e_processavel()
    {
        var resultado = Classificar(Evento(simulaTransiente: true, transienteAte: Agora.AddSeconds(-1)), tentativa: 2);

        Assert.IsType<Classificacao.Processavel>(resultado);
    }

    [Fact]
    public void SimulaTransiente_sem_prazo_e_processavel()
    {
        var resultado = Classificar(Evento(simulaTransiente: true, transienteAte: null));

        Assert.IsType<Classificacao.Processavel>(resultado);
    }

    [Fact]
    public void Schema_invalido_tem_precedencia_sobre_transiente()
    {
        var resultado = Classificar(Evento(valor: "\"1.250,00\"", simulaTransiente: true, transienteAte: Agora.AddSeconds(45)));

        Assert.IsType<Classificacao.SchemaInvalida>(resultado);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 40)]
    public void Atraso_do_retry_e_5_segundos_vezes_2_elevado_a_n(int tentativa, int segundos)
    {
        Assert.Equal(TimeSpan.FromSeconds(segundos), PoliticaDeRetry.Atraso(tentativa));
    }

    [Fact]
    public void As_tres_tentativas_somam_mais_que_os_45_segundos_da_falha_transiente()
    {
        var total = Enumerable.Range(1, PoliticaDeRetry.MaximoDeTentativas).Sum(n => PoliticaDeRetry.Atraso(n).TotalSeconds);

        Assert.Equal(70, total);
        Assert.True(total > 45);
    }

    [Theory]
    [InlineData(0, false, 1, 10)]
    [InlineData(1, false, 2, 20)]
    [InlineData(2, false, 3, 40)]
    [InlineData(3, true, 3, 0)]
    [InlineData(7, true, 7, 0)]
    public void Proxima_tentativa_a_partir_da_atual(int atual, bool esgotou, int tentativa, int segundos)
    {
        var decisao = PoliticaDeRetry.Proxima(atual);

        Assert.Equal(esgotou, decisao.Esgotou);
        Assert.Equal(tentativa, decisao.Tentativa);
        Assert.Equal(TimeSpan.FromSeconds(segundos), decisao.Atraso);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Atraso_fora_de_1_a_3_e_rejeitado(int tentativa)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PoliticaDeRetry.Atraso(tentativa));
    }

    [Fact]
    public void Descricao_do_dead_letter_e_truncada_em_1024_caracteres()
    {
        var longa = new string('x', 5000);

        Assert.Equal(MotivosDeadLetter.TamanhoMaximoDaDescricao, MotivosDeadLetter.Truncar(longa).Length);
        Assert.Equal("curta", MotivosDeadLetter.Truncar("curta"));
    }
}
