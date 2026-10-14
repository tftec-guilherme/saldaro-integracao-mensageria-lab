using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Saldaro.Ferramentas;

/// <summary>
/// Política de retry do SDK (backoff exponencial com jitter, até 3 retentativas) que também conta
/// quantas retentativas houve e por qual motivo. Na rajada do 3.8, é assim que o throttling do
/// Standard aparece: o SDK recebe ServiceBusy (erro 50009), espera e tenta de novo, em silêncio.
/// </summary>
internal sealed class PoliticaDeRetryQueConta : ServiceBusRetryPolicy
{
    private readonly ConcurrentDictionary<string, int> _retentativas = new(StringComparer.Ordinal);

    public int MaximoDeRetentativas { get; init; } = 3;

    public TimeSpan EsperaBase { get; init; } = TimeSpan.FromSeconds(0.8);

    public TimeSpan EsperaMaxima { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan TempoPorTentativa { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>Retentativas feitas pelo SDK, por motivo (ex.: ServiceBusy).</summary>
    public IReadOnlyDictionary<string, int> Retentativas => _retentativas;

    public override TimeSpan CalculateTryTimeout(int attemptCount) => TempoPorTentativa;

    public override TimeSpan? CalculateRetryDelay(Exception lastException, int attemptCount)
    {
        // attemptCount conta as tentativas já feitas, incluindo a primeira.
        if (attemptCount > MaximoDeRetentativas || !EhTransiente(lastException))
        {
            return null;
        }

        _retentativas.AddOrUpdate(Motivo(lastException), 1, (_, n) => n + 1);

        // CONCEITO: backoff exponencial (0,8 s, 1,6 s, 3,2 s...) com jitter de ±10%, limitado ao teto.
        var exponencial = EsperaBase.TotalMilliseconds * Math.Pow(2, attemptCount - 1);
        var comJitter = exponencial * (0.9 + (Random.Shared.NextDouble() * 0.2));
        return TimeSpan.FromMilliseconds(Math.Min(EsperaMaxima.TotalMilliseconds, comJitter));
    }

    public static string Motivo(Exception excecao) =>
        excecao is ServiceBusException sb ? sb.Reason.ToString() : excecao.GetType().Name;

    private static bool EhTransiente(Exception excecao) => excecao switch
    {
        ServiceBusException sb => sb.IsTransient,
        TimeoutException => true,
        System.Net.Sockets.SocketException => true,
        IOException => true,
        _ => false,
    };
}
