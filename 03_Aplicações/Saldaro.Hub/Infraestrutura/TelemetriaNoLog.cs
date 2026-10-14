using Microsoft.Extensions.Logging;
using Saldaro.Hub.Aplicacao;

namespace Saldaro.Hub.Infraestrutura;

/// <summary>Sem <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> (execução local): os eventos customizados vão para o log.</summary>
public sealed class TelemetriaNoLog(ILogger<TelemetriaNoLog> logger) : ITelemetria
{
    public void Registrar(string evento, IReadOnlyDictionary<string, string?> propriedades) =>
        logger.LogInformation("Evento {Evento}: {Propriedades}", evento,
            string.Join(", ", propriedades.Select(item => $"{item.Key}={item.Value}")));
}
