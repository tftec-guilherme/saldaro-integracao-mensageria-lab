using Azure.Messaging.ServiceBus;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Aplicacao;

// Portas: o que a lógica do hub precisa do mundo de fora. As implementações reais (Table Storage,
// Service Bus, Event Grid, HTTP, Application Insights) ficam em Infraestrutura/; os testes usam
// versões em memória. É isso que permite testar a baixa, a idempotência e a conciliação sem Azure.

/// <summary>Tabela <c>faturas</c>.</summary>
public interface IRepositorioFaturas
{
    Task<Fatura?> ObterAsync(string faturaId, CancellationToken cancellationToken);

    /// <summary>Grava a fatura inteira (insert ou replace). Atualiza <see cref="Fatura.ETag"/>.</summary>
    Task SalvarAsync(Fatura fatura, CancellationToken cancellationToken);

    /// <summary>
    /// Grava as colunas da fatura (merge). Com <paramref name="exigirMesmaVersao"/>, só grava se a linha ainda
    /// estiver na versão <see cref="Fatura.ETag"/> lida (If-Match) e devolve <c>false</c> quando outra gravação
    /// venceu (HTTP 412). Sem exigir, grava por cima de qualquer versão: o modo ingênuo do baseline.
    /// </summary>
    Task<bool> AtualizarAsync(Fatura fatura, bool exigirMesmaVersao, CancellationToken cancellationToken);

    /// <summary>Apaga a fatura (compensação do recebimento). Fatura inexistente não é erro.</summary>
    Task RemoverAsync(string faturaId, CancellationToken cancellationToken);

    IAsyncEnumerable<ResumoFatura> ListarResumosAsync(CancellationToken cancellationToken);
}

/// <summary>Tabela <c>inbox</c> (PK = <c>faturaId</c>, RK = <c>idEvento</c>).</summary>
public interface IRepositorioInbox
{
    Task<bool> ExisteAsync(string faturaId, string idEvento, CancellationToken cancellationToken);

    Task RegistrarAsync(string faturaId, string idEvento, string tipo, DateTimeOffset processadoEm, CancellationToken cancellationToken);
}

/// <summary>Tabela <c>idempotencia</c> (PK = <c>clienteId</c>, RK = valor do <c>Idempotency-Key</c>).</summary>
public interface IRepositorioIdempotencia
{
    /// <summary>
    /// Insert condicional da chave. Chave nova: devolve <c>(true, faturaId)</c>. Chave repetida (conflito 409):
    /// relê a linha e devolve <c>(false, faturaId gravado na primeira vez)</c>.
    /// </summary>
    Task<(bool ChaveNova, string FaturaId)> RegistrarAsync(
        string clienteId, string chave, string faturaId, DateTimeOffset criadaEm, CancellationToken cancellationToken);

    /// <summary>
    /// Desfaz a reserva da chave (compensação do recebimento): apaga a linha só se ela ainda aponta para
    /// <paramref name="faturaId"/>. Linha inexistente, ou de outro recebimento, fica como está.
    /// </summary>
    Task RemoverAsync(string clienteId, string chave, string faturaId, CancellationToken cancellationToken);
}

/// <summary>Tabela <c>conciliacao</c> (PK = <c>yyyyMMdd</c>, RK = id da execução).</summary>
public interface IRepositorioConciliacao
{
    Task GravarAsync(ResumoConciliacao resumo, CancellationToken cancellationToken);
}

/// <summary>Envio de mensagens para as filas do Service Bus.</summary>
public interface IMensageria
{
    Task EnviarAsync(string fila, ServiceBusMessage mensagem, CancellationToken cancellationToken);
}

/// <summary>Publicação de eventos no tópico do Event Grid.</summary>
public interface IPublicadorDeEventos
{
    Task PublicarFaturaBaixadaAsync(FaturaBaixadaDados dados, CancellationToken cancellationToken);
}

/// <summary>API do banco parceiro (simulado).</summary>
public interface IBancoClient
{
    /// <summary>
    /// <c>POST {BancoBaseUrl}/cobrancas?{caos}</c> com <c>x-api-key</c>.
    /// 503 vira <see cref="BancoIndisponivelException"/>.
    /// </summary>
    Task<CobrancaEmitida> EmitirCobrancaAsync(SolicitacaoDeCobranca solicitacao, string? caos, CancellationToken cancellationToken);
}

/// <summary>Eventos customizados do Application Insights (tabela <c>customEvents</c>).</summary>
public interface ITelemetria
{
    void Registrar(string evento, IReadOnlyDictionary<string, string?> propriedades);
}

/// <summary>Corpo de <c>POST /cobrancas</c> no banco (seção 6).</summary>
public sealed record SolicitacaoDeCobranca(
    string FaturaId,
    string ClienteId,
    decimal Valor,
    string? Vencimento,
    string? PagadorCnpj,
    string WebhookUrl);

/// <summary>Resposta 201 do banco.</summary>
public sealed record CobrancaEmitida(string IdCobranca, string? LinhaDigitavel, string? QrCode);

/// <summary><c>data</c> do CloudEvent <c>FaturaBaixada</c> (seção 7.3).</summary>
public sealed record FaturaBaixadaDados(
    string FaturaId,
    string ClienteId,
    decimal Valor,
    DateTimeOffset BaixadaEm,
    string IdEvento);

/// <summary>Uma execução da conciliação (linha da tabela <c>conciliacao</c>).</summary>
public sealed record ResumoConciliacao(
    string IdExecucao,
    DateTimeOffset ExecutadaEm,
    IReadOnlyDictionary<string, int> ContagemPorStatus,
    int FaturasVerificadas,
    int Divergencias,
    long DuracaoMs);
