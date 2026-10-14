# ADR-0005: Event Grid entrega o `FaturaBaixada` numa fila do Service Bus, com a identidade do tópico

**Status:** Aceita · **Data:** 2026-10-13 · **Decisão:** professor da aula · **Origem:** experiência em projetos de consultoria da TFTEC · **Version-anchor:** Q4-2026

## Contexto

Quando uma fatura é baixada, o hub publica o evento `FaturaBaixada` (CloudEvents 1.0) no tópico `evgt-saldaro-faturas-<sufixo>`, e a Function `NotificarParceiro` faz o callback para o ERP da empresa cliente. O ERP pode estar lento ou fora do ar, então a notificação precisa de nova tentativa, DLQ e observabilidade. O Event Grid empurra cada evento para um destino, como uma Function (Event Grid trigger), um webhook, uma fila do Service Bus, uma fila do Storage ou um Event Hub.

Duas restrições pesam na escolha:

- a partir do cenário 3.5, o namespace tem `disableLocalAuth` e só aceita Microsoft Entra ID ([ADR-0003](0003-service-bus-standard-com-identidade-gerenciada.md));
- o `deploy.ps1` aplica o Bicep antes de publicar o código (`dotnet publish` + `config-zip`).

## Decisão

- A event subscription `evgs-notificar-parceiro` do tópico entrega na fila `sbq-notificacoes-parceiro` com `deliveryWithResourceIdentity`, usando a identidade system-assigned do tópico, que recebe `Azure Service Bus Data Sender` só nessa fila.
- Isso vale desde o baseline e não é um toggle de cenário: a perna Event Grid → Service Bus nunca usa chave.
- `NotificarParceiro` é um trigger de Service Bus nessa fila, com o mesmo modelo dos outros consumidores: peek-lock, reentrega até `maxDeliveryCount` 10 e DLQ.
- A publicação no tópico é a outra perna e segue os toggles: no baseline o hub usa `EventGridTopicKey`; com `identidadeGerenciada = true`, publica com `DefaultAzureCredential` e o role `EventGrid Data Sender`.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Event Grid → Function com Event Grid trigger | É o caminho que o Learn recomenda para Functions (validação e ajuste de ritmo automáticos), mas o destino aponta para o recurso da função (`.../sites/<app>/functions/<nome>`), que só existe depois da publicação do código, e o Bicep roda antes. Além disso, o Event Grid chama a função com uma chave de acesso: mais um segredo no fluxo |
| Event Grid → webhook (HTTP trigger) | Exige implementar o handshake de validação e abre mais um endpoint público, anônimo no lab ([ADR-0007](0007-http-triggers-anonymous-e-apim-sem-subscription.md)) |
| O hub enviar direto para a fila, sem Event Grid | Mais simples, mas acopla o hub a cada consumidor. `FaturaBaixada` é um evento de integração: outros assinantes podem consumi-lo sem mudar o hub |
| Entrega sem identidade gerenciada | Com `disableLocalAuth`, o namespace só aceita Microsoft Entra ID, e a entrega com a identidade do tópico é o caminho documentado para isso |
| Tópico do Service Bus no lugar da fila | Útil para vários consumidores com filtros, mas cada avaliação de filtro consome crédito no Standard, e o lab tem um consumidor só |
| Fila do Storage | Mais barata, mas sem DLQ com motivo e fora do modelo que a aula ensina nos incidentes |

## Consequências

- Infraestrutura e código ficam desacoplados: a event subscription só depende da fila, criada pelo próprio Bicep, e o `deploy.ps1` funciona numa passada.
- Nenhum segredo na perna Event Grid → Service Bus, do primeiro dia ao 3.5; ligar o `disableLocalAuth` não quebra a notificação.
- A notificação ganha a semântica do Service Bus (peek-lock, reentrega, DLQ com motivo), a mesma dos incidentes.
- A entrega do Event Grid é "pelo menos uma vez". O `MessageId` da mensagem é um ID interno do Event Grid que se repete nas novas tentativas, mas a `sbq-notificacoes-parceiro` não tem detecção de duplicatas no contrato: no baseline, uma nova tentativa do Event Grid, rara, vira uma notificação repetida ao ERP. A partir do 3.3 (`Cenario:Inbox = true`, Atualização 1 do contrato), a `NotificarParceiro` completa sem callback o `FaturaBaixada` de uma fatura que já está `notificada` e emite `notificacao.duplicada.ignorada`. Duas entregas simultâneas da mesma baixa ainda podem passar pela guarda, então o ERP também deve deduplicar pelo `id` do CloudEvent.
- São dois saltos (Event Grid e Service Bus) até a notificação, o que é aceitável para um aviso.
- O Event Grid não entrega por Private Endpoint. Se um dia o namespace for Premium com rede isolada, a entrega vai depender de "trusted services" com a identidade do tópico (gatilho 1 da [ADR-0003](0003-service-bus-standard-com-identidade-gerenciada.md)).
- Numa migração para o Premium, a event subscription aponta para o namespace pelo ID de recurso e precisa ser atualizada; no kit, quem faz isso é o Bicep.

## Gatilhos de revisão

- Aparecer notificação repetida atribuível a nova tentativa do Event Grid: ligar a detecção de duplicatas na `sbq-notificacoes-parceiro`, com janela igual ao maior entre o TTL do evento e a duração máxima de nova tentativa da subscription. Isso exige recriar a fila (propriedade imutável).
- O deploy passar a publicar o código antes do Bicep (por exemplo, um pipeline em duas fases): a entrega direta para a Function volta a ser opção.
- Surgirem outros consumidores do `FaturaBaixada` com filtros diferentes: mais event subscriptions ou um tópico do Service Bus.
- Uma exigência de rede isolada no namespace.

## Fontes

Consultadas em 2026-10-13.

- [Enable managed identities for Event Grid topics](https://learn.microsoft.com/en-us/azure/event-grid/managed-service-identity): entrega com identidade gerenciada, role no destino e ausência de entrega por Private Endpoint.
- [Configure Service Bus as an Event Grid handler](https://learn.microsoft.com/en-us/azure/event-grid/handler-service-bus): `deliveryWithResourceIdentity`, `MessageId` interno que persiste nas novas tentativas e janela de detecção de duplicatas recomendada.
- [Event Grid event handler with Azure Functions](https://learn.microsoft.com/en-us/azure/event-grid/handler-functions): destino `AzureFunction` com o ID de recurso da função e a chave de acesso buscada pelo Event Grid.
- [Integrate Azure Service Bus with Azure Private Link](https://learn.microsoft.com/en-us/azure/service-bus-messaging/private-link-service): o Event Grid como trusted service, com identidade gerenciada.
- [Disable local authentication with Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/disable-local-authentication): com `disableLocalAuth`, só o Microsoft Entra ID autentica.
- [Migrate Azure Service Bus namespaces from Standard to Premium](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-migrate-standard-premium): IDs de recurso a atualizar depois da migração.
- [Azure Service Bus throttling](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-throttling): avaliação de filtro de tópico consome crédito.
- Internas: contrato do kit, seções 2, 5.1, 5.2, 7.3 e 9.
