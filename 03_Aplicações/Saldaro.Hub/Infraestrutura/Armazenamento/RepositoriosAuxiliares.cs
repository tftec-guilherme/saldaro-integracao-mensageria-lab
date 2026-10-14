using Azure;
using Azure.Data.Tables;
using Saldaro.Hub.Aplicacao;
using Saldaro.Hub.Configuracao;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Infraestrutura.Armazenamento;

/// <summary>Tabela <c>inbox</c>: PK = <c>faturaId</c>, RK = <c>idEvento</c>; colunas <c>tipo</c>, <c>processadoEm</c>.</summary>
public sealed class RepositorioInboxTabela(FabricaDeTabelas tabelas) : IRepositorioInbox
{
    public async Task<bool> ExisteAsync(string faturaId, string idEvento, CancellationToken cancellationToken)
    {
        var tabela = await tabelas.ObterAsync(Tabelas.Inbox, cancellationToken);
        var resposta = await tabela.GetEntityIfExistsAsync<TableEntity>(faturaId, idEvento, cancellationToken: cancellationToken);
        return resposta.HasValue;
    }

    public async Task RegistrarAsync(string faturaId, string idEvento, string tipo, DateTimeOffset processadoEm, CancellationToken cancellationToken)
    {
        var tabela = await tabelas.ObterAsync(Tabelas.Inbox, cancellationToken);
        var linha = new TableEntity(faturaId, idEvento)
        {
            ["tipo"] = tipo,
            ["processadoEm"] = processadoEm,
        };
        await tabela.UpsertEntityAsync(linha, TableUpdateMode.Replace, cancellationToken);
    }
}

/// <summary>Tabela <c>idempotencia</c>: PK = <c>clienteId</c>, RK = <c>Idempotency-Key</c>; colunas <c>faturaId</c>, <c>criadaEm</c>.</summary>
public sealed class RepositorioIdempotenciaTabela(FabricaDeTabelas tabelas) : IRepositorioIdempotencia
{
    public async Task<(bool ChaveNova, string FaturaId)> RegistrarAsync(
        string clienteId, string chave, string faturaId, DateTimeOffset criadaEm, CancellationToken cancellationToken)
    {
        var tabela = await tabelas.ObterAsync(Tabelas.Idempotencia, cancellationToken);
        var linha = new TableEntity(clienteId, chave)
        {
            ["faturaId"] = faturaId,
            ["criadaEm"] = criadaEm,
        };

        try
        {
            // Insert condicional: AddEntity falha com 409 se a linha (clienteId, chave) já existe.
            // Dois reenvios simultâneos não passam os dois: o Table Storage garante que só um insert vence.
            await tabela.AddEntityAsync(linha, cancellationToken);
            return (true, faturaId);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            var existente = await tabela.GetEntityAsync<TableEntity>(clienteId, chave, cancellationToken: cancellationToken);
            return (false, existente.Value.GetString("faturaId") ?? faturaId);
        }
    }

    public async Task RemoverAsync(string clienteId, string chave, string faturaId, CancellationToken cancellationToken)
    {
        var tabela = await tabelas.ObterAsync(Tabelas.Idempotencia, cancellationToken);
        var resposta = await tabela.GetEntityIfExistsAsync<TableEntity>(clienteId, chave, cancellationToken: cancellationToken);
        if (!resposta.HasValue || resposta.Value is not { } linha || linha.GetString("faturaId") != faturaId)
        {
            // Já não existe, ou a chave é de outro recebimento: não é a reserva que este pedido fez.
            return;
        }

        try
        {
            // If-Match na versão lida: só apaga se a linha não mudou depois da leitura.
            await tabela.DeleteEntityAsync(clienteId, chave, linha.ETag, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            // A linha mudou depois da leitura: já não é a reserva deste pedido.
        }
    }
}

/// <summary>
/// Tabela <c>conciliacao</c>: PK = <c>yyyyMMdd</c>, RK = id da execução; contagens por status
/// (<c>qtdRecebida</c>, <c>qtdCobrancaEmitida</c>, <c>qtdRegistrada</c>, <c>qtdBaixada</c>, <c>qtdNotificada</c>),
/// <c>faturasVerificadas</c>, <c>divergencias</c> e <c>duracaoMs</c>.
/// </summary>
public sealed class RepositorioConciliacaoTabela(FabricaDeTabelas tabelas) : IRepositorioConciliacao
{
    public async Task GravarAsync(ResumoConciliacao resumo, CancellationToken cancellationToken)
    {
        var tabela = await tabelas.ObterAsync(Tabelas.Conciliacao, cancellationToken);
        var linha = new TableEntity(resumo.ExecutadaEm.UtcDateTime.ToString("yyyyMMdd"), resumo.IdExecucao)
        {
            ["faturasVerificadas"] = resumo.FaturasVerificadas,
            ["divergencias"] = resumo.Divergencias,
            ["duracaoMs"] = resumo.DuracaoMs,
        };

        foreach (var (status, quantidade) in resumo.ContagemPorStatus)
        {
            linha[NomeDaColuna(status)] = quantidade;
        }

        await tabela.UpsertEntityAsync(linha, TableUpdateMode.Replace, cancellationToken);
    }

    /// <summary><c>cobranca-emitida</c> → <c>qtdCobrancaEmitida</c> (nome de coluna sem hífen).</summary>
    public static string NomeDaColuna(string status) =>
        "qtd" + string.Concat(status.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(parte => char.ToUpperInvariant(parte[0]) + parte[1..]));
}
