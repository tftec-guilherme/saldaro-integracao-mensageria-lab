using Saldaro.Hub.Dominio;

namespace Saldaro.Hub.Aplicacao;

/// <summary>Leitura → decisão → gravação condicional, repetindo enquanto outra gravação vencer.</summary>
public static class ConcorrenciaOtimista
{
    public const int MaximoDeTentativas = 5;

    /// <summary>
    /// Lê a fatura, aplica <paramref name="alterar"/> e grava com If-Match na versão lida.
    /// <paramref name="alterar"/> devolve <c>false</c> quando não há nada a gravar.
    /// Devolve a fatura como ficou (ou <c>null</c> se ela não existe).
    /// </summary>
    /// <remarks>
    /// CONCEITO: ETag (concorrência otimista). Ninguém trava a linha: cada um grava "se a versão ainda for a
    /// que eu li". Quem perde a corrida recebe 412, relê a fatura e reavalia a decisão com o estado novo.
    /// Sem isso, duas gravações simultâneas se sobrescrevem e uma delas se perde (lost update).
    /// </remarks>
    public static async Task<Fatura?> AtualizarComConcorrenciaAsync(
        this IRepositorioFaturas repositorio,
        string faturaId,
        Func<Fatura, bool> alterar,
        CancellationToken cancellationToken)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            var fatura = await repositorio.ObterAsync(faturaId, cancellationToken);
            if (fatura is null)
            {
                return null;
            }

            if (!alterar(fatura))
            {
                return fatura;
            }

            if (await repositorio.AtualizarAsync(fatura, exigirMesmaVersao: true, cancellationToken))
            {
                return fatura;
            }

            if (tentativa >= MaximoDeTentativas)
            {
                throw new ConflitoDeConcorrenciaException(
                    $"A fatura {faturaId} mudou entre a leitura e a gravação {MaximoDeTentativas} vezes seguidas.");
            }
        }
    }
}
