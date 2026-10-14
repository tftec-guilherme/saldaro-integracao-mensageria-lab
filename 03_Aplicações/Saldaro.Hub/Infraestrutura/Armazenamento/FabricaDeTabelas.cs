using System.Collections.Concurrent;
using Azure.Data.Tables;

namespace Saldaro.Hub.Infraestrutura.Armazenamento;

/// <summary>
/// Entrega o <see cref="TableClient"/> de cada tabela, garantindo uma vez por processo que ela existe.
/// O Bicep já cria as tabelas no Azure; o <c>CreateIfNotExists</c> serve para rodar contra o Azurite.
/// </summary>
public sealed class FabricaDeTabelas(TableServiceClient servico)
{
    private readonly ConcurrentDictionary<string, CriadoNoPrimeiroUso<TableClient>> _tabelas = new();

    public Task<TableClient> ObterAsync(string nome, CancellationToken cancellationToken) =>
        _tabelas.GetOrAdd(nome, Preparar).ObterAsync(cancellationToken);

    private CriadoNoPrimeiroUso<TableClient> Preparar(string nome) =>
        new(servico.GetTableClient(nome), (tabela, ct) => tabela.CreateIfNotExistsAsync(ct));
}
