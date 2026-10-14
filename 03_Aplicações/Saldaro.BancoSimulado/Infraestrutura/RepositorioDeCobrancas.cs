using Azure.Data.Tables;
using Saldaro.BancoSimulado.Dominio;
using Saldaro.BancoSimulado.Servicos;

namespace Saldaro.BancoSimulado.Infraestrutura;

/// <summary>Tabela <c>cobrancas</c> no Table Storage: PartitionKey = <c>faturaId</c>, RowKey = <c>idCobranca</c>.</summary>
public sealed class RepositorioDeCobrancas(TableServiceClient servico) : IRepositorioDeCobrancas
{
    public const string NomeDaTabela = "cobrancas";

    // Em Azure o Bicep já cria a tabela; localmente (Azurite) ela pode não existir.
    private readonly CriadoNoPrimeiroUso<TableClient> _tabela =
        new(servico.GetTableClient(NomeDaTabela), (tabela, ct) => tabela.CreateIfNotExistsAsync(ct));

    public async Task GravarAsync(Cobranca cobranca, CancellationToken cancellationToken)
    {
        var tabela = await _tabela.ObterAsync(cancellationToken);

        // CONCEITO: todas as cobranças de uma fatura ficam na mesma partição. Contar as cobranças
        // de uma fatura (a prova do Incidente 1) vira uma consulta barata por PartitionKey.
        var entidade = new TableEntity(cobranca.FaturaId, cobranca.IdCobranca)
        {
            ["clienteId"] = cobranca.ClienteId,
            ["valor"] = (double)cobranca.Valor,
            ["vencimento"] = cobranca.Vencimento,
            ["pagadorCnpj"] = cobranca.PagadorCnpj,
            ["webhookUrl"] = cobranca.WebhookUrl,
            ["linhaDigitavel"] = cobranca.LinhaDigitavel,
            ["qrCode"] = cobranca.QrCode,
            ["caos"] = cobranca.Caos,
            ["criadaEm"] = cobranca.CriadaEm,
        };

        await tabela.AddEntityAsync(entidade, cancellationToken);
    }

    public async Task<IReadOnlyList<Cobranca>> ListarPorFaturaAsync(string faturaId, CancellationToken cancellationToken)
    {
        var tabela = await _tabela.ObterAsync(cancellationToken);

        // CreateQueryFilter escapa o valor: um faturaId com aspas não vira injeção de filtro OData.
        var filtro = TableClient.CreateQueryFilter($"PartitionKey eq {faturaId}");
        var cobrancas = new List<Cobranca>();
        await foreach (var entidade in tabela.QueryAsync<TableEntity>(filtro, cancellationToken: cancellationToken))
        {
            cobrancas.Add(ParaCobranca(entidade));
        }

        return cobrancas.OrderBy(c => c.CriadaEm).ToList();
    }

    private static Cobranca ParaCobranca(TableEntity entidade) => new(
        FaturaId: entidade.PartitionKey,
        IdCobranca: entidade.RowKey,
        ClienteId: entidade.GetString("clienteId") ?? string.Empty,
        Valor: (decimal)(entidade.GetDouble("valor") ?? 0d),
        Vencimento: entidade.GetString("vencimento") ?? string.Empty,
        PagadorCnpj: entidade.GetString("pagadorCnpj") ?? string.Empty,
        WebhookUrl: entidade.GetString("webhookUrl") ?? string.Empty,
        LinhaDigitavel: entidade.GetString("linhaDigitavel") ?? string.Empty,
        QrCode: entidade.GetString("qrCode") ?? string.Empty,
        Caos: entidade.GetString("caos") ?? string.Empty,
        CriadaEm: (entidade.GetDateTimeOffset("criadaEm") ?? entidade.Timestamp ?? DateTimeOffset.MinValue).ToUniversalTime());
}
