using System.Diagnostics.CodeAnalysis;

namespace Saldaro.BancoSimulado.Dominio;

/// <summary>
/// Regras do <c>faturaId</c>, que vira PartitionKey na tabela <c>cobrancas</c>: não vazio, até 128 caracteres,
/// sem <c>/ \ # ?</c> nem caracteres de controle. É a mesma regra do hub, que gera o id (contrato, A2.5).
/// </summary>
public static class ChaveDeTabela
{
    /// <summary>O limite do hub, que usa o <c>faturaId</c> também como <c>SessionId</c> no Service Bus.</summary>
    public const int TamanhoMaximo = 128;

    public static bool EhValida([NotNullWhen(true)] string? valor) =>
        !string.IsNullOrWhiteSpace(valor)
        && valor.Length <= TamanhoMaximo
        && !valor.Any(c => c is '/' or '\\' or '#' or '?' || char.IsControl(c));
}
