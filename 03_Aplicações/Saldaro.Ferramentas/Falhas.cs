using Azure;

namespace Saldaro.Ferramentas;

internal static class Falhas
{
    /// <summary>Verdadeiro para 401/403 do Service Bus, em qualquer nível da cadeia de exceções.</summary>
    public static bool EhAcessoNegado(Exception? excecao)
    {
        for (var atual = excecao; atual is not null; atual = atual.InnerException)
        {
            if (atual is UnauthorizedAccessException || atual is RequestFailedException { Status: 401 or 403 })
            {
                return true;
            }
        }

        return false;
    }

    public static string MensagemDeAcessoNegado(Exception excecao) =>
        "ERRO 401 (acesso negado): a sua identidade não tem a permissão necessária no namespace.\n"
        + "  Ler e enviar mensagens exige Azure Service Bus Data Receiver/Sender; criar filas exige\n"
        + "  o direito Manage (Azure Service Bus Data Owner). Roles recém-atribuídas levam alguns\n"
        + "  minutos para valer.\n"
        + $"  Detalhe: {Detalhe(excecao)}";

    /// <summary>Mensagem da exceção de autorização mais interna (a que diz qual claim faltou).</summary>
    public static string Detalhe(Exception excecao)
    {
        for (var atual = excecao; atual is not null; atual = atual.InnerException)
        {
            if (atual is UnauthorizedAccessException || atual is RequestFailedException)
            {
                return atual.Message;
            }
        }

        return excecao.Message;
    }
}
