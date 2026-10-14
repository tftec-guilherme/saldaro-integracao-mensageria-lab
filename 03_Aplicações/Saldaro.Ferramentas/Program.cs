// Saldaro.Ferramentas: console da aula com três subcomandos (seção 8 do contrato do kit).
//
//   triagem             DLQ agrupada por DeadLetterReason; só reprocessa ou descarta quando pedido (3.4)
//   legado-auto-create  o "worker legado" que cria a fila se ela não existir (3.7)
//   rajada              envio concorrente para o exercício de throttling (3.8)
//
// Uso: dotnet run --project 03_Aplicações/Saldaro.Ferramentas -- <subcomando> --ajuda
//
// CONCEITO: nenhuma connection string. A ferramenta autentica com DefaultAzureCredential, que no
// computador do aluno usa o `az login`. O que ela consegue fazer depende só das roles RBAC dessa
// identidade no namespace (o deploy.ps1 dá Azure Service Bus Data Owner ao aluno).

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Saldaro.Ferramentas;

var analise = Argumentos.Analisar(args);
if (analise.PedeAjuda)
{
    Console.WriteLine(Ajuda.Texto(analise.SubcomandoDaAjuda));
    return CodigosDeSaida.Sucesso;
}

if (analise.Argumentos is not { } argumentos)
{
    Console.Error.WriteLine($"Erro: {analise.Erro}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(Ajuda.Texto(analise.SubcomandoDaAjuda));
    return CodigosDeSaida.UsoInvalido;
}

using var cancelamento = new CancellationTokenSource();
Console.CancelKeyPress += (_, evento) =>
{
    // Primeiro Ctrl+C: encerra com calma (os locks da DLQ são devolvidos). Segundo: mata o processo.
    if (!cancelamento.IsCancellationRequested)
    {
        evento.Cancel = true;
        cancelamento.Cancel();
    }
};

try
{
    var credencial = new DefaultAzureCredential();
    var saida = Console.Out;
    return argumentos.Subcomando switch
    {
        Argumentos.Triagem => await ComandoTriagem.ExecutarAsync(
            OpcoesDaTriagem.De(argumentos), credencial, saida, cancelamento.Token),
        Argumentos.LegadoAutoCreate => await ComandoLegadoAutoCreate.ExecutarAsync(
            OpcoesDoLegado.De(argumentos), credencial, saida, cancelamento.Token),
        Argumentos.Rajada => await ComandoRajada.ExecutarAsync(
            OpcoesDaRajada.De(argumentos), credencial, saida, cancelamento.Token),
        _ => CodigosDeSaida.UsoInvalido,
    };
}
catch (OperationCanceledException) when (cancelamento.IsCancellationRequested)
{
    Console.Error.WriteLine("Interrompido (Ctrl+C).");
    return CodigosDeSaida.Falha;
}
catch (Exception ex) when (Falhas.EhAcessoNegado(ex))
{
    Console.Error.WriteLine(Falhas.MensagemDeAcessoNegado(ex));
    return CodigosDeSaida.AcessoNegado;
}
catch (Exception ex) when (ex is CredentialUnavailableException or AuthenticationFailedException)
{
    Console.Error.WriteLine("Erro: não foi possível obter um token com DefaultAzureCredential.");
    Console.Error.WriteLine("  Rode `az login` (e `az account set --subscription <id>` se tiver mais de uma assinatura) e tente de novo.");
    Console.Error.WriteLine($"  Detalhe: {ex.Message}");
    return CodigosDeSaida.Falha;
}
catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
{
    Console.Error.WriteLine("Erro: a fila não existe neste namespace (confira o nome e o sufixo).");
    Console.Error.WriteLine($"  Detalhe: {ex.Message}");
    return CodigosDeSaida.Falha;
}
catch (ServiceBusException ex)
{
    Console.Error.WriteLine($"Erro do Service Bus ({ex.Reason}): {ex.Message}");
    if (ex.Reason == ServiceBusFailureReason.ServiceCommunicationProblem)
    {
        Console.Error.WriteLine("  Confira o FQDN do namespace (SALDARO_SB_FQDN no scripts/.env.local) e a sua conexão.");
    }

    return CodigosDeSaida.Falha;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Erro: {ex.Message}");
    return CodigosDeSaida.UsoInvalido;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Falha inesperada ({ex.GetType().Name}): {ex.Message}");
    return CodigosDeSaida.Falha;
}
