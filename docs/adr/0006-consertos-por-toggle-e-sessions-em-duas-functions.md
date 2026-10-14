# ADR-0006: Consertos por toggle de app setting e duas Functions para a mudança de sessions

**Status:** Aceita · **Data:** 2026-10-13 · **Decisão:** professor da aula · **Origem:** experiência em projetos de consultoria da TFTEC · **Version-anchor:** Q4-2026

## Contexto

Ao vivo, em 4h ([ADR-0002](0002-formato-ao-vivo-dia-de-incidente.md)), não há tempo para escrever C#, compilar e publicar a cada conserto ([ADR-0001](0001-stack-dotnet-10-isolated-bicep-e-powershell-7.md)), e cada incidente precisa primeiro ser reproduzido no comportamento ingênuo para depois ser consertado e provado. Alguns consertos são de código, outros de infraestrutura, e dois são imutáveis: `requiresSession` e `requiresDuplicateDetection` só podem ser definidos na criação da fila.

No código, `IsSessionsEnabled` é uma propriedade `bool` do atributo `ServiceBusTrigger`, e numa fila com sessions só receptores de sessão recebem mensagens.

## Decisão

1. **O código do hub tem os dois caminhos** de cada conserto, o ingênuo e o consertado, e app settings escolhem qual roda: `Cenario__AsyncRequestReply`, `Cenario__WebhookHmac`, `Cenario__Inbox`, `Cenario__DlqComMotivo`, `Cenario__RetryAgendado` e `Cenario__Heartbeat`, lidos como `Cenario:*` pelo padrão Options. A troca para identidade gerenciada segue a mesma ideia: o código usa a connection string quando o setting simples existe, e endpoint com `DefaultAzureCredential` quando só existem `ServiceBusConnection__fullyQualifiedNamespace` e `TablesConnection__serviceUri`, ou quando falta o `EventGridTopicKey`.
2. **O Bicep recebe os toggles como parâmetros** (um `.bicepparam` por cenário) e os traduz em app settings do hub e em propriedades de recurso: filas, APIM, alertas, roles e Key Vault.
3. **Ao vivo**, o aluno liga o app setting no Portal (Environment variables) e espera o restart. O `deploy.ps1 -Cenario N -Pronto` aplica o mesmo estado pelo Bicep, como plano B.
4. **Sessions em duas Functions.** `ProcessarConfirmacaoSemSessao` e `ProcessarConfirmacaoComSessao` (`IsSessionsEnabled = true`) leem `sbq-confirmacoes-banco` e compartilham a mesma lógica de baixa. Só uma fica ativa, por `AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled = !workerComSessao` e `AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled = workerComSessao`.
5. **Mudança imutável na fila.** O caminho didático do 3.3 é drenar, apagar a fila no Portal e redeployar com `filaComSessions` e `filaDupDetection` ligados. O `deploy.ps1 -Cenario 3 -Pronto` detecta a divergência e recria a fila sozinho, avisando no terminal.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Live coding: o aluno edita o C#, compila e publica | Não cabe em 4h, quebra com erros de compilação em máquinas diferentes, e a turma veio de Node.js na D04 |
| Uma branch ou tag por cenário, com o código consertado | Cada troca vira build e publicação de minutos, e o ingênuo e o consertado deixam de estar lado a lado no mesmo arquivo |
| Feature flags no Azure App Configuration | Mais um serviço, custo e conceito fora do escopo; o app setting já aparece no Portal e já reinicia a app |
| Uma Function só, com sessions ligado por app setting | Não existe: em C#, argumentos de atributo são constantes de compilação, e as expressões `%AppSetting%` valem para propriedades de texto, como o nome da fila |
| Duas filas, uma com e outra sem sessions, trocando o produtor | Muda os nomes do contrato e tira do Incidente 2 a lição do **Enable sessions**, que o Portal só oferece na criação da fila |
| A fila já nascer com sessions no Lab 0 | Encurta o lab, mas apaga a lição de imutabilidade (opção A do briefing, preterida) |

## Consequências

- O hub carrega código ingênuo de propósito. Cada caminho tem comentário `// CONCEITO:` e testes, e o guia manda ler o trecho que o toggle liga.
- Cada troca de app setting reinicia a app, de segundos a um minuto, e os guias incluem essa espera. O Flex Consumption tem rolling updates para trocar settings sem indisponibilidade, mas a página do plano ainda os marca como preview (a de estratégias de atualização fala em GA só em parte das regiões), então o kit não os usa nem os recomenda; conferir o status em `brazilsouth` a cada `Version-anchor`.
- Os nomes dos toggles viram contrato público entre Bicep, código, guias e slides: renomear um quebra todos.
- Ligar um toggle no Portal é drift proposital: o próximo `deploy.ps1` volta ao que o `.bicepparam` declara. O cenário 3.7 aproveita isso.
- No Linux (Flex Consumption), Functions com hífen no nome não podem ser desligadas por app setting; os nomes do contrato não têm hífen.
- Só uma das duas Functions de confirmação pode ficar ativa, porque um receptor sem sessão numa fila com sessions falha. O `deploy.ps1` grava os dois settings juntos, e o guia manda trocar os dois.
- Em produção, o equivalente de um toggle é publicar uma versão nova, e a alternativa a drenar e apagar uma fila imutável costuma ser criar outra fila, com outro nome ("versionar o canal").

## Gatilhos de revisão

- O número de caminhos por toggle crescer a ponto de atrapalhar a leitura, por exemplo mais de dois toggles na mesma Function.
- A aula virar a série de 3 encontros ([ADR-0002](0002-formato-ao-vivo-dia-de-incidente.md)), com tempo para alterar código em pelo menos um cenário.
- O retorno da turma mostrar que o toggle escondeu o aprendizado ("só liguei uma chave").
- O Functions passar a aceitar a configuração de sessions fora do atributo.

## Fontes

Consultadas em 2026-10-13.

- [Enable Azure Service Bus message sessions](https://learn.microsoft.com/en-us/azure/service-bus-messaging/enable-message-sessions): sessions só na criação da fila.
- [Enable duplicate message detection](https://learn.microsoft.com/en-us/azure/service-bus-messaging/enable-duplicate-detection): detecção de duplicatas só na criação da fila.
- [Azure Service Bus sessions](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-sessions): numa fila com sessions, o cliente só envia e recebe dentro de sessões.
- [Azure Service Bus trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus-trigger): propriedades `IsSessionsEnabled` e `AutoCompleteMessages` do atributo.
- [Azure Functions binding expressions and patterns](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-expressions-patterns): expressões `%AppSetting%`.
- [How to disable functions in Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/disable-function): `AzureWebJobs.<FUNCTION_NAME>.Disabled`, restart ao mudar app setting, rolling updates no Flex Consumption e a limitação de hífen no Linux.
- Internas: contrato do kit, seções 4, 5.1, 5.2 e 9; briefing da aula ([seção 3.3](../research/2026-10-13-briefing-da-aula.md)).
