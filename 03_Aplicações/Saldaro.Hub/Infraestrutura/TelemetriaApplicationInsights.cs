using Microsoft.ApplicationInsights;
using Saldaro.Hub.Aplicacao;

namespace Saldaro.Hub.Infraestrutura;

/// <summary>Eventos customizados via <see cref="TelemetryClient"/> do Application Insights do worker.</summary>
public sealed class TelemetriaApplicationInsights(TelemetryClient cliente) : ITelemetria
{
    public void Registrar(string evento, IReadOnlyDictionary<string, string?> propriedades) =>
        cliente.TrackEvent(evento, propriedades.ToDictionary(item => item.Key, item => item.Value ?? string.Empty));
}
