using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Saldaro.Hub.Aplicacao;

namespace Saldaro.Hub.Infraestrutura;

/// <summary>Envio para as filas com o <see cref="ServiceBusClient"/> registrado por DI (um sender por fila).</summary>
public sealed class MensageriaServiceBus(ServiceBusClient cliente) : IMensageria
{
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public Task EnviarAsync(string fila, ServiceBusMessage mensagem, CancellationToken cancellationToken) =>
        _senders.GetOrAdd(fila, cliente.CreateSender).SendMessageAsync(mensagem, cancellationToken);
}
