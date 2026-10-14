using Azure.Messaging.EventGrid;
using Saldaro.Hub.Aplicacao;

namespace Saldaro.Hub.Infraestrutura;

/// <summary>Publicação no tópico <c>evgt-saldaro-faturas-&lt;sufixo&gt;</c> (esquema CloudEvents 1.0).</summary>
public sealed class PublicadorEventGrid(EventGridPublisherClient cliente) : IPublicadorDeEventos
{
    public async Task PublicarFaturaBaixadaAsync(FaturaBaixadaDados dados, CancellationToken cancellationToken) =>
        await cliente.SendEventAsync(FaturaBaixadaEvento.Criar(dados), cancellationToken);
}
