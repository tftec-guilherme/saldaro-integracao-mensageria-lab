using System.Security.Cryptography;
using System.Text;

namespace Saldaro.BancoSimulado.Dominio;

/// <summary>Autenticação do hub no banco: header <c>x-api-key</c> igual ao setting <c>BancoApiKey</c>.</summary>
public static class ChaveDeApi
{
    public const string Header = "x-api-key";

    /// <summary>Sem chave configurada no banco, nenhuma chamada passa (falha fechada).</summary>
    public static bool Confere(string? recebida, string? esperada)
    {
        if (string.IsNullOrEmpty(recebida) || string.IsNullOrEmpty(esperada))
        {
            return false;
        }

        // CONCEITO: comparação em tempo constante. Com == comum, o tempo de resposta denunciaria
        // quantos caracteres iniciais da chave o atacante já acertou.
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recebida), Encoding.UTF8.GetBytes(esperada));
    }
}
