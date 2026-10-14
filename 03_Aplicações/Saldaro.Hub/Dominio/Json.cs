using System.Text.Json;
using System.Text.Json.Serialization;

namespace Saldaro.Hub.Dominio;

/// <summary>Opções de JSON compartilhadas pelo hub.</summary>
public static class Json
{
    /// <summary>
    /// camelCase na escrita; nomes sem diferenciar maiúsculas na leitura (leitor tolerante nos nomes)
    /// e números estritos: <c>"valor": "1.250,00"</c> é erro de schema, não um número.
    /// </summary>
    public static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.Strict,
    };
}
