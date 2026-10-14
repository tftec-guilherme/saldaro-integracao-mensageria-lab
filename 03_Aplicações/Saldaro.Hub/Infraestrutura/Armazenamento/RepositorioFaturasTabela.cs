using System.Globalization;
using System.Runtime.CompilerServices;
using Azure;
using Azure.Data.Tables;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Infraestrutura.Armazenamento;

/// <summary>Tabela <c>faturas</c>: PK = <c>faturaId</c>, RK = <c>"fatura"</c>, colunas da seção 5.4.</summary>
public sealed class RepositorioFaturasTabela(FabricaDeTabelas tabelas) : IRepositorioFaturas
{
    public async Task<Fatura?> ObterAsync(string faturaId, CancellationToken cancellationToken)
    {
        var tabela = await tabelas.ObterAsync(Tabelas.Faturas, cancellationToken);
        var resposta = await tabela.GetEntityIfExistsAsync<TableEntity>(faturaId, Tabelas.RowKeyFatura, cancellationToken: cancellationToken);
        return resposta.HasValue && resposta.Value is not null ? ParaFatura(resposta.Value) : null;
    }

    public async Task SalvarAsync(Fatura fatura, CancellationToken cancellationToken)
    {
        var tabela = await tabelas.ObterAsync(Tabelas.Faturas, cancellationToken);
        var resposta = await tabela.UpsertEntityAsync(ParaEntidade(fatura), TableUpdateMode.Replace, cancellationToken);
        fatura.ETag = resposta.Headers.ETag?.ToString();
    }

    public async Task<bool> AtualizarAsync(Fatura fatura, bool exigirMesmaVersao, CancellationToken cancellationToken)
    {
        var tabela = await tabelas.ObterAsync(Tabelas.Faturas, cancellationToken);

        // Com exigirMesmaVersao, If-Match: <ETag lido>. Sem, If-Match: * (grava por cima de qualquer versão).
        var versao = exigirMesmaVersao
            ? new ETag(fatura.ETag ?? throw new InvalidOperationException($"Fatura {fatura.FaturaId} sem ETag: leia antes de gravar."))
            : ETag.All;

        try
        {
            var resposta = await tabela.UpdateEntityAsync(ParaEntidade(fatura), versao, TableUpdateMode.Merge, cancellationToken);
            fatura.ETag = resposta.Headers.ETag?.ToString();
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            // 412 Precondition Failed: outra gravação mudou a linha depois da nossa leitura.
            return false;
        }
    }

    public async Task RemoverAsync(string faturaId, CancellationToken cancellationToken)
    {
        var tabela = await tabelas.ObterAsync(Tabelas.Faturas, cancellationToken);

        // If-Match: * (apaga qualquer versão). Linha inexistente não é erro: o SDK trata o 404 do delete como sucesso.
        await tabela.DeleteEntityAsync(faturaId, Tabelas.RowKeyFatura, ETag.All, cancellationToken);
    }

    public async IAsyncEnumerable<ResumoFatura> ListarResumosAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tabela = await tabelas.ObterAsync(Tabelas.Faturas, cancellationToken);
        var linhas = tabela.QueryAsync<TableEntity>(
            filter: (string?)null,
            select: ["status", "atualizadaEm"],
            cancellationToken: cancellationToken);

        await foreach (var linha in linhas)
        {
            yield return new ResumoFatura(
                linha.PartitionKey,
                linha.GetString("status") ?? string.Empty,
                linha.GetDateTimeOffset("atualizadaEm") ?? linha.Timestamp ?? DateTimeOffset.MinValue);
        }
    }

    internal static TableEntity ParaEntidade(Fatura fatura)
    {
        var entidade = new TableEntity(fatura.FaturaId, Tabelas.RowKeyFatura)
        {
            ["clienteId"] = fatura.ClienteId,
            ["valor"] = (double)fatura.Valor,
            ["status"] = fatura.Status,
            ["quantidadeBaixas"] = fatura.QuantidadeBaixas,
            ["criadaEm"] = fatura.CriadaEm,
            ["atualizadaEm"] = fatura.AtualizadaEm,
        };

        // Colunas opcionais só entram quando têm valor.
        Opcional(entidade, "vencimento", fatura.Vencimento);
        Opcional(entidade, "pagadorCnpj", fatura.PagadorCnpj);
        Opcional(entidade, "referenciaErp", fatura.ReferenciaErp);
        Opcional(entidade, "idCobranca", fatura.IdCobranca);
        Opcional(entidade, "callbackUrl", fatura.CallbackUrl);
        Opcional(entidade, "caos", fatura.Caos);
        if (fatura.BaixadaEm is { } baixadaEm)
        {
            entidade["baixadaEm"] = baixadaEm;
        }

        return entidade;
    }

    internal static Fatura ParaFatura(TableEntity linha) => new()
    {
        FaturaId = linha.PartitionKey,
        ClienteId = linha.GetString("clienteId") ?? string.Empty,
        Valor = LerDecimal(linha, "valor"),
        Vencimento = linha.GetString("vencimento"),
        PagadorCnpj = linha.GetString("pagadorCnpj"),
        ReferenciaErp = linha.GetString("referenciaErp"),
        Status = linha.GetString("status") ?? StatusFatura.Recebida,
        IdCobranca = linha.GetString("idCobranca"),
        QuantidadeBaixas = linha.GetInt32("quantidadeBaixas") ?? 0,
        BaixadaEm = linha.GetDateTimeOffset("baixadaEm"),
        CallbackUrl = linha.GetString("callbackUrl"),
        Caos = linha.GetString("caos"),
        CriadaEm = linha.GetDateTimeOffset("criadaEm") ?? default,
        AtualizadaEm = linha.GetDateTimeOffset("atualizadaEm") ?? default,
        ETag = linha.ETag.ToString(),
    };

    private static void Opcional(TableEntity entidade, string coluna, string? valor)
    {
        if (!string.IsNullOrEmpty(valor))
        {
            entidade[coluna] = valor;
        }
    }

    // A coluna é double; aceita também inteiro ou texto, caso alguém edite a linha à mão no Storage Browser.
    private static decimal LerDecimal(TableEntity linha, string coluna) =>
        linha.TryGetValue(coluna, out var valor) && valor is not null
            ? valor switch
            {
                double d => (decimal)d,
                int i => i,
                long l => l,
                string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var convertido) => convertido,
                _ => 0m,
            }
            : 0m;
}
