using Azure.Messaging;
using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Aplicacao;

/// <summary>
/// CloudEvent <c>FaturaBaixada</c> (seção 7.3), publicado no tópico do Event Grid e entregue pela
/// event subscription na fila <c>sbq-notificacoes-parceiro</c>.
/// </summary>
public static class FaturaBaixadaEvento
{
    public const string Tipo = "Saldaro.Faturas.FaturaBaixada";
    public const string Origem = "/saldaro/hub";

    public static CloudEvent Criar(FaturaBaixadaDados dados) =>
        new(Origem, Tipo, BinaryData.FromObjectAsJson(dados, Json.Opcoes), "application/json", CloudEventDataFormat.Json)
        {
            Id = Guid.NewGuid().ToString(),
            Subject = $"faturas/{dados.FaturaId}",
            Time = dados.BaixadaEm,
        };
}
