using Azure.Storage.Queues;
using Saldaro.BancoSimulado.Servicos;

namespace Saldaro.BancoSimulado.Infraestrutura;

/// <summary>Fila de storage <c>banco-pagamentos</c>, consumida pela Function <c>EnviarWebhook</c>.</summary>
public sealed class FilaDePagamentos(QueueClient cliente) : IFilaDePagamentos
{
    public const string NomeDaFila = "banco-pagamentos";

    /// <summary>
    /// Setting da conexão: o mesmo do trigger <c>EnviarWebhook</c>, para quem envia e quem consome
    /// apontarem para a mesma conta de storage.
    /// </summary>
    public const string SettingDaConexao = "AzureWebJobsStorage";

    // Em Azure o Bicep já cria a fila; localmente (Azurite) ela pode não existir.
    private readonly CriadoNoPrimeiroUso<QueueClient> _fila =
        new(cliente, (fila, ct) => fila.CreateIfNotExistsAsync(cancellationToken: ct));

    public static QueueClient CriarCliente(string connectionString) => new(connectionString, NomeDaFila, CriarOpcoes());

    public static QueueClientOptions CriarOpcoes() => new()
    {
        // CONCEITO: os dois lados da fila precisam combinar a codificação. O QueueClient v12 envia
        // texto puro por padrão, mas o trigger de fila do Functions (extensão 5.x) espera Base64
        // (messageEncoding = "base64" no host.json). Descombinados, toda mensagem vira poison.
        MessageEncoding = QueueMessageEncoding.Base64,
    };

    public async Task EnfileirarAsync(string mensagem, TimeSpan atraso, CancellationToken cancellationToken)
    {
        var fila = await _fila.ObterAsync(cancellationToken);

        // CONCEITO: entrega atrasada com visibilityTimeout. A mensagem entra já invisível e só
        // aparece para o trigger quando o atraso vence (máximo da plataforma: 7 dias).
        await fila.SendMessageAsync(mensagem, visibilityTimeout: atraso, timeToLive: null, cancellationToken: cancellationToken);
    }
}
