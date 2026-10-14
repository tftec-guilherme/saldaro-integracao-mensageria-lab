using System.Text.Json;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Dominio;
using Saldaro.Hub.Infraestrutura;
using Saldaro.Hub.Infraestrutura.Armazenamento;
using Saldaro.Testes;

namespace Saldaro.Hub.Tests;

/// <summary>Rota do banco com os botões de caos, CloudEvent, mapeamento da tabela e nomes de coluna.</summary>
public class DetalhesTests
{
    [Theory]
    [InlineData(null, "cobrancas")]
    [InlineData("", "cobrancas")]
    [InlineData("latencia=45&duplicar=3", "cobrancas?latencia=45&duplicar=3")]
    [InlineData("latencia=45;duplicar=3", "cobrancas?latencia=45&duplicar=3")]
    [InlineData("?valorComVirgula=true", "cobrancas?valorComVirgula=true")]
    [InlineData("embaralhar", "cobrancas?embaralhar")]
    [InlineData(" latencia = 45 ;; duplicar=3 ", "cobrancas?latencia=45&duplicar=3")]
    [InlineData("assinatura=inválida", "cobrancas?assinatura=inv%C3%A1lida")]
    [InlineData("latencia=45 #/frag", "cobrancas?latencia=45%20%23%2Ffrag")]
    public void Rota_do_banco_repassa_os_botoes_de_caos_na_query_string(string? caos, string esperado)
    {
        Assert.Equal(esperado, BancoClientHttp.MontarRota(caos));
    }

    [Theory]
    [InlineData("latencia=1/../../outra-rota")]
    [InlineData("assinatura=x?code=abc#fragmento")]
    [InlineData("assinatura=inv%26latencia%3D200")]
    public void Rota_do_banco_nao_vira_outra_url_nem_ganha_parametro(string caos)
    {
        // O caos vem do corpo da fatura: um valor não muda o caminho, não abre fragmento nem vira outro botão.
        var url = new Uri(new Uri("https://banco.exemplo.test/api/"), BancoClientHttp.MontarRota(caos));

        Assert.Equal("/api/cobrancas", url.AbsolutePath);
        Assert.Empty(url.Fragment);
        Assert.Single(url.Query.TrimStart('?').Split('&'));
    }

    [Fact]
    public void FaturaBaixada_segue_o_CloudEvent_do_contrato()
    {
        var baixadaEm = new DateTimeOffset(2026, 11, 10, 9, 31, 0, TimeSpan.Zero);
        var dados = new FaturaBaixadaDados("fatura-1", "cliente-a", 1250.00m, baixadaEm, "evento-1");

        var evento = FaturaBaixadaEvento.Criar(dados);

        Assert.Equal("Saldaro.Faturas.FaturaBaixada", evento.Type);
        Assert.Equal("/saldaro/hub", evento.Source);
        Assert.Equal("faturas/fatura-1", evento.Subject);
        Assert.True(Guid.TryParse(evento.Id, out _));

        using var data = JsonDocument.Parse(evento.Data!.ToMemory());
        string[] campos = ["faturaId", "clienteId", "valor", "baixadaEm", "idEvento"];
        Assert.All(campos, campo => Assert.True(data.RootElement.TryGetProperty(campo, out _), $"faltou {campo}"));
        Assert.Equal(5, data.RootElement.EnumerateObject().Count());
    }

    [Theory]
    [InlineData(StatusFatura.Recebida, "qtdRecebida")]
    [InlineData(StatusFatura.CobrancaEmitida, "qtdCobrancaEmitida")]
    [InlineData(StatusFatura.Notificada, "qtdNotificada")]
    public void Coluna_de_contagem_da_conciliacao_nao_tem_hifen(string status, string coluna)
    {
        Assert.Equal(coluna, RepositorioConciliacaoTabela.NomeDaColuna(status));
    }

    [Fact]
    public void Fatura_vai_e_volta_da_tabela_com_as_colunas_do_contrato()
    {
        var fatura = new Fatura
        {
            FaturaId = "fatura-1",
            ClienteId = "cliente-a",
            Valor = 1250.50m,
            Vencimento = "2026-11-10",
            PagadorCnpj = CnpjDeTeste.Gerar(),
            ReferenciaErp = "ERP-0001",
            Status = StatusFatura.Baixada,
            IdCobranca = "COB-0a1b2c3d",
            QuantidadeBaixas = 1,
            BaixadaEm = new DateTimeOffset(2026, 11, 10, 9, 31, 0, TimeSpan.Zero),
            CriadaEm = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero),
            AtualizadaEm = new DateTimeOffset(2026, 11, 10, 9, 31, 0, TimeSpan.Zero),
        };

        var linha = RepositorioFaturasTabela.ParaEntidade(fatura);
        var lida = RepositorioFaturasTabela.ParaFatura(linha);

        Assert.Equal("fatura-1", linha.PartitionKey);
        Assert.Equal("fatura", linha.RowKey);
        Assert.IsType<double>(linha["valor"]);
        Assert.IsType<int>(linha["quantidadeBaixas"]);
        string[] colunas = ["clienteId", "valor", "vencimento", "pagadorCnpj", "referenciaErp", "status", "idCobranca", "quantidadeBaixas", "baixadaEm", "criadaEm", "atualizadaEm"];
        Assert.All(colunas, coluna => Assert.True(linha.ContainsKey(coluna), $"faltou {coluna}"));

        Assert.Equal(fatura.Valor, lida.Valor);
        Assert.Equal(fatura.Status, lida.Status);
        Assert.Equal(fatura.BaixadaEm, lida.BaixadaEm);
        Assert.Equal(fatura.QuantidadeBaixas, lida.QuantidadeBaixas);
    }
}
