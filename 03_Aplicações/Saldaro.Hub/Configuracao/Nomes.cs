namespace Saldaro.Hub.Configuracao;

// Nomes fixados pelo contrato do kit (seções 2, 5 e 7). Guias, Bicep, scripts e KQL usam
// exatamente estas strings: não renomeie aqui sem mudar o contrato.

/// <summary>Filas do namespace <c>sb-saldaro-&lt;sufixo&gt;</c>.</summary>
public static class Filas
{
    public const string FaturasRecebidas = "sbq-faturas-recebidas";
    public const string ConfirmacoesBanco = "sbq-confirmacoes-banco";
    public const string NotificacoesParceiro = "sbq-notificacoes-parceiro";
}

/// <summary>Tabelas do hub no storage <c>stsaldaro&lt;sufixo&gt;</c> (seção 5.4).</summary>
public static class Tabelas
{
    public const string Faturas = "faturas";
    public const string Inbox = "inbox";
    public const string Idempotencia = "idempotencia";
    public const string Conciliacao = "conciliacao";

    /// <summary>RowKey fixo das linhas da tabela <c>faturas</c> (PK = faturaId).</summary>
    public const string RowKeyFatura = "fatura";
}

/// <summary>
/// Nomes das conexões (app settings). Cada uma aceita duas formas: a connection string no
/// setting simples ou, com identidade gerenciada, só o endpoint no setting com sufixo
/// (<c>__fullyQualifiedNamespace</c> ou <c>__serviceUri</c>).
/// </summary>
public static class Conexoes
{
    public const string ServiceBus = "ServiceBusConnection";
    public const string Tabelas = "TablesConnection";
    public const string EventGridEndpoint = "EventGridTopicEndpoint";
    public const string EventGridChave = "EventGridTopicKey";
}

/// <summary>Cabeçalhos HTTP do contrato.</summary>
public static class Cabecalhos
{
    public const string ClienteId = "x-cliente-id";
    public const string IdempotencyKey = "Idempotency-Key";
    public const string ApiKeyBanco = "x-api-key";
    public const string Timestamp = "X-Saldaro-Timestamp";
    public const string Assinatura = "X-Saldaro-Signature";
}

/// <summary>Propriedades de aplicação das mensagens do Service Bus.</summary>
public static class PropriedadesDaMensagem
{
    public const string Tipo = "tipo";
    public const string Tentativa = "tentativa";
}
