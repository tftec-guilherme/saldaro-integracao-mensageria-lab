# ADR-0003: Service Bus Standard com identidade gerenciada e `disableLocalAuth`, e gatilhos para o Premium

**Status:** Aceita · **Data:** 2026-10-13 · **Decisão:** professor da aula · **Origem:** experiência em projetos de consultoria da TFTEC · **Version-anchor:** Q4-2026

## Contexto

O hub usa um namespace, `sb-saldaro-<sufixo>`, com três filas: `sbq-faturas-recebidas`, `sbq-confirmacoes-banco` (sessions, detecção de duplicatas, DLQ e mensagens agendadas) e `sbq-notificacoes-parceiro`. Tudo isso existe no tier Standard. O Premium traz recursos dedicados (messaging units), latência previsível, mensagens de até 100 MB e os recursos de rede (Private Endpoint, service endpoints e "trusted services"), mas cobra preço fixo por messaging unit por hora, o que não cabe no Free Trial do aluno.

O incidente do cenário 3.5 ("O segredo que expirou às 3h") nasce de segredos com dono e validade: connection strings com `RootManageSharedAccessKey` e chaves coladas em app setting. O baseline ingênuo usa exatamente isso, de propósito.

O cenário 3.8 ("Premium ou Standard?") reproduz uma discussão comum, segundo a experiência em projetos de consultoria da TFTEC: segurança pede Private Endpoint, que só existe no Premium, a diretoria recusa o custo e ninguém tem números na mesa. A aula quer o contrário: uma postura segura possível no Standard e critérios objetivos para mudar de tier.

## Decisão

- O namespace é **Standard** em todos os cenários. A aula nunca provisiona Premium: o 3.8 é um exercício de decisão, sem deploy.
- No baseline, o hub usa a connection string da `RootManageSharedAccessKey` (`ServiceBusConnection`), ingênua de propósito.
- A partir do cenário 3.5 (`identidadeGerenciada = true`):
  - o hub se autentica com a **identidade gerenciada** system-assigned (`ServiceBusConnection__fullyQualifiedNamespace`) e recebe `Azure Service Bus Data Sender` e `Azure Service Bus Data Receiver` no namespace, sem `Azure Service Bus Data Owner`;
  - o namespace recebe `disableLocalAuth = true`: as políticas SAS continuam lá, mas deixam de gerar tokens válidos, e só o Microsoft Entra ID autentica.
- O aluno recebe `Azure Service Bus Data Owner` no namespace, para usar o Service Bus Explorer, a triagem da DLQ e os exercícios do 3.7 mesmo com `disableLocalAuth`.
- O Event Grid entrega na fila `sbq-notificacoes-parceiro` com a identidade do tópico desde o baseline ([ADR-0005](0005-event-grid-entrega-em-fila-do-service-bus.md)), então nada quebra quando o `disableLocalAuth` liga.
- Só os gatilhos abaixo reabrem a discussão do Premium.

### Gatilhos objetivos para revisar o Premium

1. Uma política de segurança exigir isolamento de rede no namespace: Private Endpoint, service endpoint ou "trusted services", que só existem no Premium.
2. Throttling recorrente no pico mesmo depois de ajustar os clientes (`ThrottledRequests` acima de zero, erro 50009), ou a projeção do pico, somando mensagens, avaliações de filtro e operações de gerência, chegar perto de 1.000 créditos por segundo no namespace.
3. Mensagens acima de 256 KB que não possam virar claim check (guardar o corpo no Storage e mandar só a referência); o Premium aceita até 100 MB.
4. Um contrato exigir latência previsível ou isolamento por empresa cliente (bulkhead) que namespaces Standard separados não resolvam.
5. Um requisito de conformidade pedir chave gerenciada pelo cliente (CMK) ou Geo-Replication, que são do Premium.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Premium desde o Lab 0 | Preço fixo por messaging unit por hora, fora do orçamento do Free Trial, para recursos de rede que o kit nem usa |
| Basic | Não tem sessions nem detecção de duplicatas, que são o centro do Incidente 2 |
| Manter SAS e só guardar a connection string no Key Vault | Tira o segredo do app setting, mas o segredo continua existindo, com dono e validade: o incidente do 3.5 se repete |
| Firewall de IP no Standard | Existe (por Bicep, CLI, PowerShell ou REST, não pelo Portal), mas sem "trusted services": bloquearia a entrega do Event Grid e exigiria IPs de saída conhecidos para as Functions e para a máquina do aluno |
| Network Security Perimeter (NSP) no Standard | Está em GA para o Service Bus, e o Architecture Center o recomenda para namespace Standard sem private endpoint: restringe o endpoint público a regras de entrada explícitas, sem Premium. Mas não dá IP privado, o suporte do Event Grid ao NSP ainda está em preview, e a entrega do tópico na `sbq-notificacoes-parceiro` com o perímetro em modo Enforced não foi testada no kit. Fica para a discussão do 3.8, com esse teste antes de qualquer recomendação |
| Dar `Azure Service Bus Data Owner` ao hub | É o que a dica de produção da documentação do trigger sugere para a escala, mas resolve qualquer permissão, inclusive criar filas, que é justamente o problema da fila fantasma do 3.7 |

## Consequências

- O Service Bus custa pouco, mas cobra por hora enquanto o namespace existir: apagar o Resource Group no fim do encontro continua obrigatório.
- Não há isolamento de rede: o endpoint do namespace é público e a proteção é a autenticação (Microsoft Entra ID a partir do 3.5). A rede fica para o 3.8, como discussão.
- Com só `Data Sender` e `Data Receiver`, a extensão de Service Bus do Functions não lê as propriedades de runtime da fila e passa a estimar a profundidade por peek, o que pode atrasar decisões de escala. **Atualização de 2026-10-14:** no primeiro deploy real (Flex Consumption), depois da migração para identidade, o worker da fila com sessions parou de consumir, sem erro no log. A causa foi os triggers subirem antes de as roles valerem; um restart depois da propagação resolveu, com ou sem a role de leitura. O `deploy.ps1` passou a reiniciar o hub depois que as Key Vault references resolvem. O hub também ganhou `Reader` no namespace, para a escala ler a contagem exata (só leitura de gerenciamento, sem ler mensagens nem criar entidades). Em produção, um role customizado com só `Microsoft.ServiceBus/namespaces/*/read` é ainda mais estreito; `Data Owner` continua fora.
- Com `disableLocalAuth`, o Service Bus Explorer do Portal passa a depender do role do aluno no namespace; por isso o `Data Owner` do aluno.
- O `AzureWebJobsStorage` continua com connection string mesmo no 3.5: só os dados da aplicação migram para identidade. É um compromisso documentado (contrato, seção 5.2).
- Se um dia for preciso Premium, existe migração in-place do Standard para o Premium, e a connection string continua valendo, mas não existe volta: a ferramenta não faz downgrade. Os role assignments não migram, e o que aponta para o namespace pelo ID de recurso, como a event subscription do Event Grid, precisa ser atualizado. No kit, as duas coisas vivem no Bicep.
- A rajada do exercício do 3.8 pode ou não provocar throttling a partir de uma máquina só; o guia trata como "se acontecer".

## Gatilhos de revisão

- Qualquer um dos cinco gatilhos objetivos acima.
- A premissa do Free Trial mudar para uma assinatura com orçamento, em que uma hora de Premium para demonstração caiba.
- A Microsoft levar Private Endpoint ou "trusted services" ao Standard.

## Fontes

Consultadas em 2026-10-13.

- [Service Bus Premium messaging](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-premium-messaging): Premium × Standard (recursos dedicados, preço fixo, 100 MB × 256 KB), recursos de rede só no Premium, CMK e Geo-Replication.
- [Integrate Azure Service Bus with Azure Private Link](https://learn.microsoft.com/en-us/azure/service-bus-messaging/private-link-service): Private Endpoint só no Premium; o Event Grid como trusted service, com identidade gerenciada e `Azure Service Bus Data Sender`.
- [Configure IP firewall rules for Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-ip-filtering): o Standard tem firewall de IP, mas sem "trusted services"; Private Endpoint e service endpoints só no Premium.
- [What is a network security perimeter?](https://learn.microsoft.com/en-us/azure/private-link/network-security-perimeter-concepts) (conferida em 2026-10-14): NSP em GA; na lista de recursos, Azure Service Bus "Generally Available" e Event Grid "Public Preview"; modos Transition e Enforced; regras de entrada por assinatura não aceitam SAS.
- [Use a Message Broker and Events to Integrate Enterprise Systems](https://learn.microsoft.com/en-us/azure/architecture/example-scenario/integration/queues-events) (Azure Architecture Center, conferida em 2026-10-14): NSP para namespace Standard sem private endpoint, com regras de entrada explícitas.
- [Azure Service Bus throttling](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-throttling): 1.000 créditos por segundo por namespace no Standard, custo por operação e erro 50009.
- [Disable local authentication with Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/disable-local-authentication): `disableLocalAuth` no Standard, roles de dados e políticas SAS que deixam de gerar tokens.
- [Migrate Azure Service Bus namespaces from Standard to Premium](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-migrate-standard-premium): migração in-place, sem downgrade, sem migrar RBAC e com ID de recurso a atualizar.
- [Azure Service Bus trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus-trigger#scaling-permissions): permissões que a extensão usa para decidir a escala e a dica de produção com `Azure Service Bus Data Owner` (conferida em 2026-10-14).
- [Service Bus pricing](https://azure.microsoft.com/en-us/pricing/details/service-bus/): modelo de cobrança (valores só na calculadora).
- Internas: briefing da aula ([seções 3.5 e 3.8](../research/2026-10-13-briefing-da-aula.md)); contrato do kit, seções 2 e 5.2.
