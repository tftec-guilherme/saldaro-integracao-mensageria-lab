using System.Diagnostics.CodeAnalysis;

namespace Saldaro.Hub.Dominio;

/// <summary>
/// Regras de chave do Table Storage e do Service Bus para os ids que viram PartitionKey/RowKey,
/// MessageId e SessionId: não vazio, até 128 caracteres, sem <c>/ \ # ?</c> nem caracteres de controle.
/// O banco simulado aplica a mesma regra ao <c>faturaId</c> que recebe (contrato, A2.5).
/// </summary>
public static class ChaveDeTabela
{
    /// <summary>Limite do Service Bus para <c>MessageId</c> e <c>SessionId</c>.</summary>
    public const int TamanhoMaximo = 128;

    public static bool EhValida([NotNullWhen(true)] string? valor) =>
        !string.IsNullOrWhiteSpace(valor)
        && valor.Length <= TamanhoMaximo
        && !valor.Any(c => c is '/' or '\\' or '#' or '?' || char.IsControl(c));
}
