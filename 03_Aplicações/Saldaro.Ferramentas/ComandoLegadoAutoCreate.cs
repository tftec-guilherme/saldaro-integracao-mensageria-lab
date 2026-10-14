using System.Globalization;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Saldaro.Ferramentas;

internal sealed record OpcoesDoLegado(string Namespace, int Mensagens)
{
    public static OpcoesDoLegado De(ArgumentosAnalisados argumentos) => new(
        NomeDoNamespace.Normalizar(argumentos.Texto("--namespace")),
        argumentos.Inteiro("--mensagens"));
}

/// <summary>
/// Subcomando <c>legado-auto-create</c>: o "worker legado" do cenário 3.7, que ao subir cria a fila
/// se ela não existir. É o que gera a fila fantasma que o <c>check-drift.ps1</c> precisa achar.
/// </summary>
internal static class ComandoLegadoAutoCreate
{
    // CONCEITO: o nome "quase igual" é o que esconde a fila fantasma. A fila do Bicep é
    // sbq-confirmacoes-banco; a que o legado inventa é confirmacoesbanco, sem hífens.
    public const string FilaDoLegado = "confirmacoesbanco";

    public static async Task<int> ExecutarAsync(
        OpcoesDoLegado opcoes, TokenCredential credencial, TextWriter saida, CancellationToken ct)
    {
        saida.WriteLine($"Worker legado (3.7) subindo em {opcoes.Namespace}");
        saida.WriteLine($"Como no legado, ele \"cria a fila se ela não existir\": {FilaDoLegado}");

        try
        {
            // CONCEITO: criar entidade exige a permissão Manage (role Azure Service Bus Data Owner).
            // Com só Data Sender/Receiver a chamada abaixo falha com 401, e é isso que queremos depois
            // do conserto: um auto-create escondido passa a falhar alto, em vez de criar fila sozinho.
            var administracao = new ServiceBusAdministrationClient(opcoes.Namespace, credencial);
            if ((await administracao.QueueExistsAsync(FilaDoLegado, ct)).Value)
            {
                saida.WriteLine("A fila já existia; o legado segue usando-a.");
            }
            else
            {
                // Sem opções: maxDeliveryCount 10, sem duplicate detection, sem sessions, lock de 1 min.
                await administracao.CreateQueueAsync(FilaDoLegado, ct);
                saida.WriteLine("Fila criada com a configuração padrão do Service Bus: maxDeliveryCount 10,");
                saida.WriteLine("sem duplicate detection e sem sessions. Nada disso está no Bicep.");
            }
        }
        catch (Exception ex) when (Falhas.EhAcessoNegado(ex))
        {
            saida.WriteLine();
            saida.WriteLine(MensagemSemManage(opcoes.Namespace, ex));
            return CodigosDeSaida.AcessoNegado;
        }

        await using var cliente = new ServiceBusClient(opcoes.Namespace, credencial);
        await using var remetente = cliente.CreateSender(FilaDoLegado);
        for (var i = 1; i <= opcoes.Mensagens; i++)
        {
            await remetente.SendMessageAsync(CriarConfirmacao(), ct);
        }

        saida.WriteLine($"{Formatos.Inteiro(opcoes.Mensagens)} confirmações enviadas para {FilaDoLegado}. Nenhuma Function do hub lê essa fila.");
        saida.WriteLine("Próximos passos: veja a fila no Service Bus Explorer do Portal e rode ./scripts/check-drift.ps1.");
        return CodigosDeSaida.Sucesso;
    }

    /// <summary>A mensagem do 401 no 3.7: é a prova de que o privilégio mínimo funcionou.</summary>
    internal static string MensagemSemManage(string fqdn, Exception excecao) =>
        $"ERRO 401 (Unauthorized): a identidade não tem o direito Manage no namespace {fqdn}.\n"
        + "  Criar ou consultar a definição de uma fila exige Manage (role Azure Service Bus Data Owner).\n"
        + "  Com só Azure Service Bus Data Sender/Receiver, o ServiceBusAdministrationClient recebe 401.\n"
        + "\n"
        + "  No 3.7 este é o efeito esperado do privilégio mínimo: o auto-create escondido do legado\n"
        + "  passa a falhar alto e nenhuma fila fantasma nasce fora do Bicep.\n"
        + $"  Detalhe: {Falhas.Detalhe(excecao)}";

    /// <summary>Confirmação no formato do webhook do banco (seção 7.1 do contrato), com dados fictícios.</summary>
    internal static ServiceBusMessage CriarConfirmacao()
    {
        var idEvento = Guid.NewGuid().ToString();
        var faturaId = Guid.NewGuid().ToString();
        var centavos = Random.Shared.Next(10_000, 2_500_001);
        var corpo = new
        {
            idEvento,
            tipo = "cobranca.paga",
            faturaId,
            idCobranca = $"COB-{Guid.NewGuid():N}"[..12],
            valor = decimal.Round(centavos / 100m, 2),
            ocorridoEm = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            simulaTransiente = false,
            transienteAte = (string?)null,
        };

        var mensagem = new ServiceBusMessage(BinaryData.FromObjectAsJson(corpo))
        {
            MessageId = idEvento,
            SessionId = faturaId,
            ContentType = "application/json",
        };
        mensagem.ApplicationProperties["tipo"] = corpo.tipo;
        return mensagem;
    }
}
