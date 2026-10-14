# Dúvidas da turma sobre as tecnologias do kit: FAQ e "Por que esta tecnologia (e não X)"

**Version-anchor:** Q4-2026 · **Data:** 2026-10-14 · **Status:** pesquisa, fonte dos slides de FAQ e dos boxes do guia de montagem · **Fontes:** Microsoft Learn (documentação de produto, Azure Architecture Center, páginas de comparação, limites e cotas) e a página de preços da Microsoft, todas consultadas em 2026-10-14

Empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência.

## Objetivo

Reunir, com fonte primária, as respostas às dúvidas que a turma (arquitetos e engenheiros de cloud que já cursaram a Disciplina 04) tende a levantar sobre as tecnologias do kit da Saldaro Pagamentos S.A. Este arquivo alimenta dois materiais:

1. **Slides de FAQ** ("perguntas que a turma vai fazer"), abrindo com "por que APIM e não WAF?". No slide vai a **Resposta curta**; nas speaker notes, a **Resposta completa** e o **No kit da Saldaro**.
2. **Boxes "Por que esta tecnologia (e não X)"** no guia novo de montagem do ambiente no Portal. Cada box usa a **Resposta curta**, o **No kit da Saldaro** e o **Quando a alternativa vence**, no passo do recurso indicado no índice.

Regras seguidas:

- Cada afirmação foi seguida até a página que a sustenta, citada em **Fontes**. Nenhum blog de terceiros foi usado como fonte de fato nem como indicador.
- Só recursos GA entram nas recomendações. O que está em preview aparece marcado como tal e fica fora delas.
- As respostas seguem as ADRs 0001 a 0007, o `CONTEXT.md` e o contrato do kit (seções 0 a 3, mais as Atualizações 1 e 2, que prevalecem sobre o texto original). Onde uma fonte contradiz o kit, o ponto está em "Conflitos com o kit".
- As perguntas 1 a 20 vieram do professor. As 21 a 23 saíram de limites, notas e FAQs das próprias páginas do Learn consultadas: a Microsoft documenta a resposta porque a pergunta aparece.

Cada pergunta traz: **Pergunta** (como o aluno faria), **Resposta curta** (até duas frases, cabe num slide), **Resposta completa** (para as notes ou o box), **No kit da Saldaro**, **Quando a alternativa vence** e **Fontes**.

## Índice

**[Slide]** marca as dez perguntas mais prováveis na turma, que viram slides de FAQ. **[Guia]** marca as que viram box no guia de montagem. A última coluna diz em que recurso do guia o box entra; as perguntas de slide também servem de box nesse recurso.

| # | Pergunta | Marca | Recurso no guia de montagem e título do box |
|---|---|---|---|
| 1 | [Por que APIM e não WAF?](#1-por-que-apim-e-não-waf) | [Slide] | API Management: "Por que APIM (e não só um WAF)" |
| 2 | [APIM Consumption × Basic v2 × Standard v2 × Premium](#2-apim-consumption--basic-v2--standard-v2--premium) | [Guia] | API Management: "Por que o tier Consumption (e não v2 ou Premium)" |
| 3 | [Por que um gateway na frente das Functions?](#3-por-que-um-gateway-na-frente-das-functions) | [Guia] | API Management: "Por que o ERP não chama a Function direto" |
| 4 | [Service Bus × Storage Queues × Event Hubs × Event Grid](#4-service-bus--storage-queues--event-hubs--event-grid) | [Slide] | Service Bus: "Por que Service Bus (e não Storage Queue, Event Hubs ou Event Grid)" |
| 5 | [Fila × tópico e por que o Event Grid entrega numa fila](#5-fila--tópico-e-por-que-o-event-grid-entrega-numa-fila) | [Guia] | Event Grid: "Por que Event Grid entregando numa fila (e não tópico ou Function)" |
| 6 | [Service Bus Standard × Premium](#6-service-bus-standard--premium) | [Slide] | Service Bus: "Por que Standard (e não Premium)" |
| 7 | [Sessions × partições: o que FIFO garante](#7-sessions--partições-o-que-fifo-garante) | [Slide] | Service Bus, fila de confirmações: "Por que sessões (e não partições)" |
| 8 | [Duplicate detection × idempotência](#8-duplicate-detection--idempotência) | [Slide] | Service Bus, fila de confirmações: "Por que detecção de duplicatas e inbox" |
| 9 | [DLQ, maxDeliveryCount e retry](#9-dlq-maxdeliverycount-e-retry) | [Slide] | Service Bus, filas: "Por que DLQ com motivo e retry agendado" |
| 10 | [Functions × Logic Apps × Container Apps × App Service × AKS](#10-functions--logic-apps--container-apps--app-service--aks) | [Slide] | Function App: "Por que Azure Functions (e não Logic Apps ou contêineres)" |
| 11 | [Flex Consumption × Consumption × Premium × Dedicated](#11-flex-consumption--consumption--premium--dedicated) | [Guia] | Function App, plano: "Por que Flex Consumption" |
| 12 | [.NET isolated × in-process](#12-net-isolated--in-process) | [Guia] | Function App, runtime: "Por que .NET 10 isolated" |
| 13 | [Table Storage × Cosmos DB × Azure SQL](#13-table-storage--cosmos-db--azure-sql) | [Guia] | Storage account: "Por que Table Storage (e não Cosmos DB ou SQL)" |
| 14 | [Key Vault × app settings × App Configuration](#14-key-vault--app-settings--app-configuration) | [Guia] | Key Vault: "Por que Key Vault com RBAC e Key Vault references" |
| 15 | [Identidade gerenciada × connection string](#15-identidade-gerenciada--connection-string) | [Guia] | Identidades e roles: "Por que identidade gerenciada (e não connection string)" |
| 16 | [Application Insights × Log Analytics × Azure Monitor](#16-application-insights--log-analytics--azure-monitor) | [Guia] | Log Analytics, Application Insights e alertas: "Quem é quem na observabilidade" |
| 17 | [Bicep × Terraform × ARM × Portal](#17-bicep--terraform--arm--portal) | [Slide] | Abertura do guia: "Por que montar no Portal se o kit tem Bicep" |
| 18 | [Durable Functions × request-reply feito à mão](#18-durable-functions--request-reply-feito-à-mão) | [Slide] | Function App do hub: "Por que 202 + status feito à mão (e não Durable)" |
| 19 | [Segurança de webhook: HMAC × mTLS × IP × OAuth](#19-segurança-de-webhook-hmac--mtls--ip--oauth) | [Slide] | API Management, API de webhooks: "Por que HMAC com timestamp" |
| 20 | [Private Endpoint e VNet](#20-private-endpoint-e-vnet) | [Guia] | Fechamento do guia: "O que muda em produção: rede privada" |
| 21 | [Não dava para só aumentar o timeout?](#21-não-dava-para-só-aumentar-o-timeout) | [Guia] | API Management: "Por que o gateway corta em 30 s" |
| 22 | [Por que CloudEvents?](#22-por-que-cloudevents) | [Guia] | Event Grid: "Por que CloudEvents (e não o esquema do Event Grid)" |
| 23 | [Network Security Perimeter resolve a rede do Service Bus Standard?](#23-network-security-perimeter-resolve-a-rede-do-service-bus-standard) | [Guia] | Service Bus, rede (e leitura 3.8): "Rede no Standard sem Premium" |

---

## 1. Por que APIM e não WAF?

**Marca:** [Slide]

**Pergunta:** "Por que vocês puseram um API Management na frente do hub e não um WAF, como o Application Gateway WAF ou o Front Door WAF? Um não faz o papel do outro?"

**Resposta curta:** Não: o WAF inspeciona o tráfego web contra ataques conhecidos (regras OWASP, bots, geofiltro), e o APIM governa a API (quem chama, quanto, por quanto tempo, com que contrato e com que telemetria). Em produção eles se somam, com o WAF na borda e o APIM atrás; no lab ficou só o APIM, porque os incidentes são de integração, não de ataque web, e o WAF tem custo fixo fora do Free Trial.

**Resposta completa:**

O Azure Web Application Firewall protege aplicações web de forma centralizada contra exploits comuns, como SQL injection e cross-site scripting, e roda no Application Gateway, no Application Gateway for Containers e no Front Door (no Azure CDN, ainda em preview). O APIM é outra camada: o gateway valida subscription keys, JWT e certificados, aplica cotas e rate limits, transforma requisições e respostas e emite logs, métricas e traces por API. A página do Learn que mapeia o OWASP API Security Top 10 para o APIM é explícita: mesmo com os controles do APIM, "Azure Front Door, Azure Application Gateway, and Azure Web Application Firewall provide protection against traditional web application threats and bots", e a proteção de camada 7 contra bots melhora com um desses serviços na frente do APIM. Dica de vocabulário: no Learn, "WAF" é o Web Application Firewall, e o Well-Architected Framework aparece por extenso; vale confirmar de qual "WAF" o aluno fala.

A arquitetura de referência do Azure Architecture Center ("Protect APIs by using Azure Application Gateway and Azure API Management") põe o Application Gateway com WAF na borda, com regras OWASP CRS 3.2 ou mais nova, bot protection e geomatch, e o APIM em modo privado atrás dele para tirar das APIs as preocupações transversais. O modo privado exige injeção em VNet (Developer ou Premium) ou as opções de rede do Standard v2 e do Premium v2. O mesmo artigo cita o Front Door (DDoS embutido e balanceamento global) e o Azure Firewall (proteção de rede) como alternativas. Para o Front Door, o Learn tem um passo a passo que vale para todos os tiers do APIM, inclusive o Consumption, e mostra como fazer o APIM aceitar só o Front Door: `ip-filter` com as faixas `AzureFrontDoor.Backend` e `check-header` no `X-Azure-FDID`.

Dois cuidados de quem já montou isso. Com o Application Gateway na frente, o certificado do cliente não chega ao APIM, porque o gateway abre outra conexão TLS; o contorno documentado são as server variables de autenticação mútua. E o guia de balanceamento lembra que o APIM "isn't a traditional, general-purpose load balancer"; para hospedagem só de API com alcance global, a árvore de decisão sugere Front Door com API Management.

**No kit da Saldaro:** O `apim-saldaro-<sufixo>` (Consumption) é a porta do ERP (API `saldaro`) e do banco parceiro (API `saldaro-webhooks`). É ele que corta o backend aos 30 s e devolve 504 no Incidente 1, que registra o `x-cliente-id` no Application Insights e que, no estado pronto do 3.1, aplica o `rate-limit` por subscription de empresa cliente. Não há WAF: o encontro dura 4 h num Free Trial, e o ADR-0007 já lista o que muda em produção.

**Quando a alternativa vence:**

- API pública exposta a ataque web e a bots: WAF na frente do APIM, nunca no lugar dele. Front Door quando o tráfego é global ou multirregião; Application Gateway quando é regional e os backends estão numa VNet.
- Não há API para governar (site, front-end, app web): WAF sem APIM.
- O requisito é só TLS, roteamento ou balanceamento: Front Door ou Application Gateway resolvem, e o Learn desaconselha usar o APIM só para balancear.

**Fontes** (Consultado em 2026-10-14):

- [Introduction to Azure Web Application Firewall](https://learn.microsoft.com/en-us/azure/web-application-firewall/overview): o que o WAF protege e onde roda; WAF no CDN em preview.
- [Mitigate OWASP API security top 10 in Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/mitigate-owasp-api-threats): serviços complementares ao APIM e proteção contra bots na frente dele.
- [Protect APIs by using Azure Application Gateway and Azure API Management](https://learn.microsoft.com/en-us/azure/architecture/web-apps/api-management/architectures/protect-apis): arquitetura de referência, tiers exigidos e alternativas.
- [Configure Azure Front Door in front of Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/front-door-api-management): todos os tiers; `ip-filter` e `X-Azure-FDID`.
- [API Gateway Overview](https://learn.microsoft.com/en-us/azure/api-management/api-management-gateways-overview): o papel do gateway.
- [Secure APIs using client certificate authentication in API Management](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-mutual-certificates-for-clients): mTLS atrás do Application Gateway.
- [Load Balancing Options](https://learn.microsoft.com/en-us/azure/architecture/guide/technology-choices/load-balancing-overview): APIM não é balanceador genérico; Front Door com APIM para hospedagem só de API.

---

## 2. APIM Consumption × Basic v2 × Standard v2 × Premium

**Marca:** [Guia]

**Pergunta:** "O Consumption é um APIM 'de verdade'? O que eu perco em relação ao Basic v2, ao Standard v2 ou ao Premium?"

**Resposta curta:** É o mesmo produto, cobrado por execução e com escala automática, mas sem rede privada, sem cache interno, sem `rate-limit-by-key` e com no máximo 30 s por requisição. Serve no lab porque inclui o primeiro milhão de operações por assinatura e tem SLA de 99,95%.

**Resposta completa:**

Na comparação oficial de tiers, o Consumption não tem integração com o Microsoft Entra no developer portal, private endpoint de entrada, injeção em VNet, conexão com backends isolados em VNet, multirregião, zonas de disponibilidade, developer portal, cache interno (só o externo, compatível com Redis), autoscaling configurável (a escala é automática), API analytics, self-hosted gateway, workspaces, backup e restore, logs de requisição no Azure Monitor e no Log Analytics, IP estático nem Defender for APIs. Mantém as políticas, o credential manager, a autenticação por certificado de cliente, as configurações de TLS, as métricas do Azure Monitor e os logs de requisição no Application Insights. O gateway do Consumption não executa `rate-limit-by-key`, `quota-by-key` nem o limite de tokens de LLM; o `rate-limit`, que conta por subscription, existe. Nos limites de runtime, a duração total de uma requisição é de 30 s no Consumption (ilimitada nos tiers clássicos e v2), e o documento de política vai até 16 KiB.

Os tiers v2 são GA: Basic v2 (desenvolvimento e teste, com SLA), Standard v2 (produção, com VNet integration para alcançar backends isolados e com private endpoint de entrada) e Premium v2 (injeção em VNet, zonas de disponibilidade e até 30 unidades). Não existe migração automática de uma instância existente para um tier v2: o v2 é só para instância nova. O Consumption e os três v2 rodam em infraestrutura compartilhada e não têm IP de saída determinístico. Na página de preços, o Consumption é "billed per execution", com o primeiro milhão de operações por assinatura incluído; o SLA é de 99,95% no Consumption, no Basic, no Standard, no Basic v2 e no Standard v2, de 99,99% no Premium e no Premium v2 com unidades em duas ou mais zonas ou regiões, e o Developer não tem SLA. Os valores em moeda só aparecem na calculadora.

**No kit da Saldaro:** O Consumption é o único APIM que cabe num Free Trial que nasce e morre no mesmo dia. O 3.1 contorna a falta do `rate-limit-by-key` com uma subscription por empresa cliente (`cliente-a`, `cliente-b`) e `<rate-limit calls="60" renewal-period="60" />` (ADR-0007 e Atualização 1 do contrato). O diagnóstico vai para o Application Insights, porque no Consumption os logs de requisição não vão para o Log Analytics. O corte de 30 s do Incidente 1 coincide com o teto de duração do tier (pergunta 21).

**Quando a alternativa vence:**

- Rate limit ou cota por chave arbitrária (IP, claim do token, header): qualquer tier, menos o Consumption.
- Backend dentro de VNet: Standard v2, Premium v2, Developer ou Premium. APIM sem endpoint público: Premium v2 ou Premium injetados, ou private endpoint de entrada (todos os tiers, menos o Consumption e o Basic v2).
- Cache interno, logs no Log Analytics, Defender for APIs, requisições acima de 30 s, IP estático ou zonas de disponibilidade.
- Tráfego alto e constante, em que o preço por unidade passa a valer mais que o preço por chamada (só a calculadora responde).

**Fontes** (Consultado em 2026-10-14):

- [Feature-based comparison of Azure API Management tiers](https://learn.microsoft.com/en-us/azure/api-management/api-management-features): recursos por tier.
- [API Gateway Overview](https://learn.microsoft.com/en-us/azure/api-management/api-management-gateways-overview): políticas indisponíveis no Consumption, limites de runtime (30 s, 16 KiB), Defender for APIs.
- [Azure API Management - V2 Tiers](https://learn.microsoft.com/en-us/azure/api-management/v2-service-tiers-overview): status GA, rede e ausência de migração.
- [rate-limit-by-key policy](https://learn.microsoft.com/en-us/azure/api-management/rate-limit-by-key-policy): tiers em que a política existe.
- [Add caching to improve performance in Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-cache): cache interno indisponível no Consumption.
- [IP addresses of Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-ip-addresses): Consumption e v2 sem IP determinístico.
- [API Management pricing](https://azure.microsoft.com/en-us/pricing/details/api-management/): modelo de cobrança e SLA por tier (página de preços da Microsoft).

---

## 3. Por que um gateway na frente das Functions?

**Marca:** [Guia]

**Pergunta:** "Por que o ERP não chama a Function direto? E onde entram o Front Door e o Application Gateway nessa conversa?"

**Resposta curta:** Porque autenticação, limite, timeout, transformação e log por operação são preocupações transversais que o gateway centraliza (padrão Gateway Offloading), e chave de função não basta para proteger endpoint em produção. Front Door e Application Gateway são a borda HTTP (WAF, TLS, roteamento global ou regional); o APIM é o gateway de API atrás deles.

**Resposta completa:**

O guia de segurança do Azure Functions ordena as defesas de um endpoint HTTP público, do mais básico ao mais restritivo: HTTPS, chaves de acesso, App Service Authentication, APIM para autenticar as requisições, VNet e isolamento. E avisa que, embora as chaves dificultem o acesso, "the only way to truly secure your function endpoints is by implementing positive authentication of clients". O padrão Gateway Offloading do Architecture Center descreve o ganho (TLS, autenticação, throttling e logging num lugar só) e os custos: o gateway precisa ser altamente disponível e não pode virar gargalo, acrescenta um salto de rede, não pode receber regra de negócio ("Never offload business logic to the gateway") e só protege se o backend aceitar tráfego apenas pelo caminho do gateway ("Gateway bypass prevention").

Fechar o backend depende do tier do APIM. O guia do Functions sugere aceitar só o IP do APIM, mas o Consumption não tem IP determinístico: o Learn manda liberar a faixa do datacenter inteiro, o que não isola nada. No Consumption, o caminho que funciona é o APIM se autenticar no backend com a própria identidade gerenciada (a política `authentication-managed-identity` existe no gateway Consumption) e a Function exigir App Service Authentication. Uma pergunta que costuma vir junto é "por que o APIM não grava direto na fila?": a integração do APIM com o Service Bus (`send-service-bus-message`) está em preview, então fica fora do kit.

**No kit da Saldaro:** Os HTTP triggers do hub são `AuthorizationLevel.Anonymous` e as APIs do APIM não exigem subscription fora do 3.1: é simplificação de laboratório, comentada no código e explicada no ADR-0007. O `erp-simulado.ps1 -Direto` chama o hub sem passar pelo APIM, de propósito: é o "gateway bypass" que o padrão manda impedir. O ADR-0007 lista o que muda em produção (subscription ou OAuth no APIM, identidade da empresa cliente tirada da credencial, backend fechado para quem não for o APIM).

**Quando a alternativa vence:**

- Function interna, chamada só por outro serviço do mesmo time: App Service Authentication com Microsoft Entra ID, sem APIM.
- Só é preciso borda web (WAF, TLS, roteamento), sem governança de API: Front Door ou Application Gateway.
- Muitas APIs, vários consumidores, versões e cotas: APIM, com o backend fechado para quem não for o gateway.

**Fontes** (Consultado em 2026-10-14):

- [Securing Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/security-concepts): camadas de proteção de endpoints HTTP e limites das chaves de acesso.
- [Gateway Offloading pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/gateway-offloading): benefícios, cuidados e "gateway bypass prevention".
- [IP addresses of Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-ip-addresses): Consumption sem IP determinístico.
- [API Gateway Overview](https://learn.microsoft.com/en-us/azure/api-management/api-management-gateways-overview): `authentication-managed-identity` no gateway Consumption; integração com Service Bus marcada como preview.
- [How to send messages to Azure Service Bus](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-send-service-bus) e [send-service-bus-message policy](https://learn.microsoft.com/en-us/azure/api-management/send-service-bus-message-policy): "currently in preview".

---

## 4. Service Bus × Storage Queues × Event Hubs × Event Grid

**Marca:** [Slide]

**Pergunta:** "Por que Service Bus? Não dava para usar Storage Queue, que é mais barata, ou Event Hubs, ou só Event Grid?"

**Resposta curta:** Cada um tem um papel: Service Bus para mensagens de alto valor (comandos) com peek-lock, DLQ, sessões, transações e detecção de duplicatas; Storage Queue para backlog simples e barato; Event Hubs para streams de telemetria; Event Grid para avisar que algo aconteceu. Cobrança e baixa são transações financeiras, o caso de uso que a página oficial dá ao Service Bus.

**Resposta completa:**

A página de comparação do Learn resume: Event Grid é "reactive event routing", Event Hubs é "big data streaming and ingestion" e Service Bus é "enterprise transactional messaging", indicado para "order processing, financial transactions, workflows". Na tabela de recursos, só o Service Bus tem transações e detecção de duplicatas; a ordem é "no guarantee" no Event Grid, por partição no Event Hubs e FIFO com sessões no Service Bus; dead-lettering existe no Event Grid e no Service Bus, não no Event Hubs; replay (Capture) só no Event Hubs. A distinção de fundo é entre evento (notificação leve, sem expectativa do publicador sobre o tratamento) e mensagem (dado com contrato entre produtor e consumidor). O guia de mensageria do Architecture Center diz o mesmo de outro jeito: comandos têm requisitos estritos de entrega e "consumers shouldn't process a command more than one time", porque a duplicação causa "double billing".

Entre as duas filas, a comparação oficial manda considerar a Storage Queue quando a fila precisa passar de 80 GB, quando o worker quer gravar progresso no próprio item ou quando é preciso log de transações no servidor. O Service Bus vence quando há FIFO garantido (sessões), detecção de duplicatas automática, transações, mensagens acima de 64 KB (até 256 KB no Standard), RBAC com papéis separados de envio e recebimento, AMQP, ou quando o desenho vai evoluir para publish-subscribe. A Storage Queue não garante ordem e não tem dead-lettering automático: a mensagem envenenada é detectada pelo `DequeueCount`.

**No kit da Saldaro:** As três filas do hub são do Service Bus Standard: `sbq-faturas-recebidas`, `sbq-confirmacoes-banco` (sessões, detecção de duplicatas e DLQ, o coração dos Incidentes 2 e 3) e `sbq-notificacoes-parceiro`. O `FaturaBaixada` sai pelo Event Grid (pergunta 5). O banco simulado usa, de propósito, uma fila do Storage (`banco-pagamentos`): o trigger tenta 5 vezes, com intervalo configurável, e depois move a mensagem para `banco-pagamentos-poison`. É um bom contraste ao vivo com a DLQ com motivo do Service Bus.

**Quando a alternativa vence:**

- Storage Queue: trabalho simples e idempotente, backlog muito grande, menor custo, sem necessidade de ordem, sessão ou detecção de duplicatas.
- Event Hubs: telemetria e eventos em stream, milhões por segundo, releitura do stream, clientes Kafka.
- Event Grid: reagir a eventos de recursos do Azure ou espalhar notificações para vários assinantes, com filtro e em push.

**Fontes** (Consultado em 2026-10-14):

- [Compare messaging services](https://learn.microsoft.com/en-us/azure/service-bus-messaging/compare-messaging-services): propósito, quando usar e tabela de recursos.
- [Compare Azure Storage queues and Service Bus queues](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-azure-and-service-bus-queues-compared-contrasted): critérios de escolha e limites.
- [Asynchronous messaging options](https://learn.microsoft.com/en-us/azure/architecture/guide/technology-choices/messaging): comandos × eventos.
- [Azure Queue storage trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger): 5 tentativas, fila `-poison` e `visibilityTimeout`.

---

## 5. Fila × tópico e por que o Event Grid entrega numa fila

**Marca:** [Guia]

**Pergunta:** "Por que o `FaturaBaixada` passa pelo Event Grid e cai numa fila do Service Bus? Não seria mais simples um tópico do Service Bus, ou o Event Grid chamar a Function direto?"

**Resposta curta:** O Event Grid é bom para publicar e rotear eventos (push, filtro, vários assinantes), mas não garante ordem nem deduplica; a fila do Service Bus dá ao consumidor peek-lock, reentrega e DLQ com motivo. O kit usa os dois: o Event Grid distribui o evento de integração e a fila garante o tratamento da notificação.

**Resposta completa:**

Uma fila do Service Bus é ponto a ponto, com consumidores competindo pelas mensagens; um tópico com subscriptions é publish-subscribe, e cada subscription funciona como uma fila, com filtro opcional. O Event Grid também é publish-subscribe, mas para eventos discretos e em push: tenta entregar cada evento "at least once" por subscription, refaz a entrega com backoff por até 24 h (30 tentativas e TTL de 1.440 minutos por padrão), não garante ordem e só faz dead-letter num container de blob se isso for configurado. O guia de mensageria do Architecture Center avisa que, no push, se os assinantes falham repetidamente, o Event Grid descarta os eventos, e que, se o workload precisa de sessões, transações ou detecção de duplicatas, "use Service Bus instead". O mesmo guia descreve o desenho do kit como "crossover": "If an event requires a workflow, Event Grid sends it to a Service Bus queue."

O handler de Service Bus do Event Grid aceita entrega com a identidade gerenciada do tópico (`deliveryWithResourceIdentity`). O `MessageId` da mensagem é um ID interno do Event Grid que persiste nas novas tentativas, então a fila de destino pode ligar a detecção de duplicatas com janela igual ao maior entre o TTL do evento e a duração máxima de retry. Para Functions, o Learn recomenda o endpoint "Azure Function" (Event Grid trigger), com validação e ajuste de ritmo automáticos; mas esse destino aponta para o ID de recurso da função, e o Event Grid busca a chave de acesso dela.

**No kit da Saldaro:** ADR-0005: a event subscription `evgs-notificar-parceiro` entrega o CloudEvent na `sbq-notificacoes-parceiro` com a identidade do tópico `evgt-saldaro-faturas-<sufixo>`, que tem `Azure Service Bus Data Sender` só nessa fila; funciona desde o baseline e continua funcionando com o `disableLocalAuth` do 3.5. Não usa o Event Grid trigger porque o Bicep roda antes da publicação do código (a função ainda não existe) e porque a chamada usaria chave. Não usa tópico do Service Bus porque há um consumidor só, e cada avaliação de filtro de tópico custa crédito no Standard. A fila não tem detecção de duplicatas: a partir do 3.3, a `NotificarParceiro` ignora o `FaturaBaixada` de uma fatura já `notificada`.

**Quando a alternativa vence:**

- Vários consumidores do mesmo evento, cada um com filtro e precisando de semântica de mensagem: tópico do Service Bus.
- Reação leve a eventos, sem necessidade de reentrega controlada pelo consumidor: Event Grid direto para o handler (Event Grid trigger, no caso de Functions).
- Pipeline que publica o código antes da infraestrutura: o destino Azure Function volta a ser opção (gatilho de revisão do ADR-0005).

**Fontes** (Consultado em 2026-10-14):

- [Asynchronous messaging options](https://learn.microsoft.com/en-us/azure/architecture/guide/technology-choices/messaging): filas, tópicos, Event Grid em push e o crossover Event Grid → Service Bus.
- [Azure Event Grid delivery and retry](https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry): at least once, sem ordem, cronograma de retry e dead-letter.
- [Configure Service Bus as an Event Grid handler](https://learn.microsoft.com/en-us/azure/event-grid/handler-service-bus): entrega com identidade, `MessageId` interno e janela de detecção recomendada.
- [Event Grid event handler with Azure Functions](https://learn.microsoft.com/en-us/azure/event-grid/handler-functions): Event Grid trigger recomendado e chave de acesso buscada pelo Event Grid.
- [Azure Service Bus throttling](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-throttling): avaliação de filtro de tópico custa 1 crédito.

---

## 6. Service Bus Standard × Premium

**Marca:** [Slide]

**Pergunta:** "Por que Standard? Um hub de pagamentos de verdade não usaria Premium, com Private Endpoint?"

**Resposta curta:** O Premium compra isolamento de recursos (messaging units), latência previsível, mensagens de até 100 MB e os recursos de rede (Private Endpoint, service endpoints, trusted services), a preço fixo por hora. O Standard tem tudo o que os incidentes usam (sessões, detecção de duplicatas, DLQ, transações) e é a escolha que o próprio Learn indica para desenvolvimento, QA e produção de baixo throughput; mudar de tier é decisão por gatilho objetivo, não por nome.

**Resposta completa:**

O Standard é multitenant e cobrado por operação, com throttling por crédito: 1.000 créditos por segundo por namespace, 1 crédito por mensagem enviada, recebida ou espiada, 10 por operação de gerência e 1 por avaliação de filtro de tópico. Quando os créditos acabam, a operação volta com o erro 50009, e os SDKs refazem com backoff, sem perda de dados. O Premium aloca 1, 2, 4, 8 ou 16 messaging units por namespace, isola CPU e memória por cliente, e nele "throttling is purely driven by the limitations of the resources allocated". Mensagens: 256 KB no Standard; no Premium, 1 MB por padrão por entidade, até 100 MB via AMQP. Só o Premium tem private endpoints, service endpoints, chave gerenciada pelo cliente (CMK), Geo-Replication e JMS 2.0; o Standard tem firewall de IP, mas sem a exceção de trusted services.

Existe migração in-place do Standard para o Premium: a connection string continua valendo e a troca de DNS leva cerca de 5 minutos, mas não há volta, as role assignments não migram, as mensagens não são copiadas (é preciso drenar o namespace antigo) e o que aponta para o ID de recurso, como uma event subscription do Event Grid, precisa ser atualizado. Novidade que muda a conversa de rede: o Network Security Perimeter está GA, o Service Bus é um dos recursos suportados, e o Architecture Center recomenda usá-lo quando o namespace é Standard e não tem private endpoint (pergunta 23).

**No kit da Saldaro:** O `sb-saldaro-<sufixo>` é Standard em todos os cenários; a aula nunca provisiona Premium, e o 3.8 é um exercício de decisão sem deploy. O ADR-0003 fixa cinco gatilhos objetivos para reabrir o Premium: exigência de isolamento de rede, throttling recorrente (`ThrottledRequests` acima de zero, erro 50009), mensagens acima de 256 KB que não possam virar claim check, latência previsível ou isolamento por empresa cliente, e CMK ou Geo-Replication. A ferramenta `rajada` do 3.8 tenta provocar o 50009 numa fila descartável.

**Quando a alternativa vence:** Premium quando um dos gatilhos do ADR-0003 aparece: política de rede que exige Private Endpoint, service endpoint ou trusted services; throttling recorrente depois de ajustar os clientes; mensagens grandes sem claim check; SLA de latência ou isolamento por cliente; CMK, Geo-Replication ou JMS 2.0.

**Fontes** (Consultado em 2026-10-14):

- [Service Bus Premium messaging](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-premium-messaging): Premium × Standard, messaging units, mensagens grandes, rede, CMK e Geo-Replication.
- [Azure Service Bus throttling](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-throttling): créditos, erro 50009 e indicação do Standard para dev, QA e produção de baixo throughput.
- [Service Bus quotas and limits](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas): tamanhos e limites por tier.
- [Network security for Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/network-security): service endpoints e private endpoints só no Premium; NSP.
- [Configure IP firewall rules for Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-ip-filtering): o Standard filtra IP, sem trusted services.
- [Migrate Service Bus namespaces from Standard to Premium](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-migrate-standard-premium): migração in-place, sem volta, sem RBAC e sem mensagens.
- [What is a network security perimeter?](https://learn.microsoft.com/en-us/azure/private-link/network-security-perimeter-concepts): NSP GA e Service Bus GA.
- [Use a message broker and events to integrate enterprise systems](https://learn.microsoft.com/en-us/azure/architecture/example-scenario/integration/queues-events): NSP para namespaces Standard sem private endpoint.

---

## 7. Sessions × partições: o que FIFO garante

**Marca:** [Slide]

**Pergunta:** "O Service Bus garante FIFO? Então por que a fatura voltou de 'baixada' para 'registrada' no Incidente 2? E partição não resolve ordem?"

**Resposta curta:** Sem sessões, o Service Bus garante a ordem de enfileiramento e de retirada, não a de processamento; com sessões, garante processamento em ordem e um consumidor por vez dentro de cada `SessionId`, e só dentro dela. FIFO é a ordem em que as mensagens chegaram à fila: se o banco mandou o `cobranca.paga` antes do `cobranca.registrada`, é nessa ordem que elas saem, e quem protege a regra de negócio é a máquina de estados.

**Resposta completa:**

O `SequenceNumber` "guarantees the queuing order and the extraction order of messages, but not the processing order, which requires sessions": com dois consumidores, a mensagem 1 pode terminar depois da 3. Com sessões, quem aceita a sessão segura um lock exclusivo sobre todas as mensagens daquele `SessionId`, inclusive as que chegarem depois, e "abandoning a message causes the same message to be served again by the next receive operation". Sessão só se liga na criação da fila (não dá para ligar nem desligar depois), não existe no Basic, e uma fila com sessões só aceita envio e recebimento dentro de sessões. Há exceções que quebram a ordem: a mensagem que estoura o `MaxDeliveryCount` vai para a DLQ e a sessão segue com as próximas; a mensagem reprocessada a partir da DLQ entra no fim, com `SequenceNumber` novo; e uma mensagem agendada entra na fila, na hora marcada, como se tivesse sido enfileirada naquele instante. Se uma mensagem da sessão expira por TTL, todas as mensagens da sessão expiram (com listener ativo).

Partição é outra coisa: serve para throughput e disponibilidade, espalhando a entidade por vários brokers (16 partições no Standard, escolhidas na criação e imutáveis). Ordem só existe com chave de partição, e o `SessionId` vira essa chave; sem chave, o envio é distribuído entre as partições e "you don't achieve the guaranteed ordering". O Learn desaconselha partição em cenários de baixo volume. Sobre "exactly once": a página de comparação diz "exactly once with sessions", mas a comparação de filas avisa que o termo "isn't exactly adequate", porque falhas de transação causam reentrega, e o Architecture Center é direto: exactly-once ponta a ponta é impraticável; o que existe é at-least-once com consumidor idempotente.

**No kit da Saldaro:** `SessionId = faturaId` na `sbq-confirmacoes-banco`. A `ProcessarConfirmacaoComSessao` (`IsSessionsEnabled = true`, `maxConcurrentSessions = 8`, `sessionIdleTimeout = 00:00:10`) processa uma confirmação por vez por fatura. O botão de caos `embaralhar` mostra que a sessão não conserta a desordem de origem; por isso a máquina de estados da inbox é quem garante o "só avança". Como sessão é imutável, o Incidente 2 drena e recria a fila (ADR-0006).

**Quando a alternativa vence:**

- Ordem não importa e o processamento é idempotente: fila sem sessão, mais simples e com mais paralelismo.
- Ordem por chave em stream de altíssimo volume: partições do Event Hubs.
- Ordem de negócio que não depende da ordem de chegada, porque o parceiro pode mandar fora de ordem: máquina de estados com versão ou sequência no dado, com ou sem sessão.

**Fontes** (Consultado em 2026-10-14):

- [Message sessions](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-sessions): FIFO, lock de sessão, DLQ e TTL dentro da sessão, sequência × sessão.
- [Message sequencing and timestamps](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-sequencing): ordem de retirada × de processamento; mensagens agendadas ganham `SequenceNumber` novo.
- [Enable message sessions](https://learn.microsoft.com/en-us/azure/service-bus-messaging/enable-message-sessions): só na criação; não existe no Basic.
- [Partitioned queues and topics](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-partitioning): chaves de partição, 16 partições no Standard e baixo volume.
- [Compare messaging services](https://learn.microsoft.com/en-us/azure/service-bus-messaging/compare-messaging-services) e [Compare Storage queues and Service Bus queues](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-azure-and-service-bus-queues-compared-contrasted): as duas leituras de "exactly once".
- [Idempotent Consumer pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer): exactly-once ponta a ponta é impraticável.

---

## 8. Duplicate detection × idempotência

**Marca:** [Slide]

**Pergunta:** "Se a fila tem detecção de duplicatas, para que inbox, ETag e `Idempotency-Key`? Não é redundante?"

**Resposta curta:** A detecção de duplicatas só descarta as cópias que o produtor enviar de novo com o mesmo `MessageId` dentro de uma janela (10 minutos por padrão, de 20 segundos a 7 dias) e não impede o consumidor de processar duas vezes a mesma mensagem depois de uma reentrega. Idempotência é responsabilidade do consumidor e da API, e o Learn é explícito: uma não substitui a outra.

**Resposta completa:**

A detecção de duplicatas guarda os `MessageId` vistos durante a janela e descarta a cópia nova, aceitando o envio; "no other parts of the message other than the `MessageId` are considered", e o `MessageId` precisa ser controlado pela aplicação e ancorado no processo de negócio. Só se liga na criação da fila (a janela pode mudar depois), não existe no Basic e pesa no throughput: quanto maior a janela, mais IDs para comparar. Com partição, a unicidade é `MessageId` + `PartitionKey`, e mensagens agendadas também entram na conta. A página de habilitação chega a dizer que o recurso "guarantees exactly once delivery over a user-defined span of time", mas isso fala do que fica armazenado na fila, não do processamento.

O padrão Idempotent Consumer do Architecture Center fecha a questão: "Don't substitute broker deduplication for idempotent consumer logic", porque a detecção opera no envio, numa janela limitada, e "doesn't prevent a consumer from processing the same message twice after a redelivery". O consumidor guarda as chaves já processadas (a inbox), de preferência na mesma transação do efeito; contra disputa simultânea, usa restrição de unicidade ou escrita condicional atômica ("an insert that fails on conflict"); e mantém o registro pelo menos enquanto o broker ainda puder reentregar, contando os reprocessamentos feitos a partir da DLQ. O padrão Asynchronous Request-Reply recomenda o mesmo do lado da API: o cliente manda uma `Idempotency-Key`, e a chave repetida devolve o status existente em vez de enfileirar de novo.

**No kit da Saldaro:** Três camadas, uma para cada tipo de repetição: a `Idempotency-Key` por `clienteId` na tabela `idempotencia`, com insert condicional, para o reenvio do ERP (Incidente 1); `MessageId = idEvento` com janela de 10 minutos na `sbq-confirmacoes-banco`, para a confirmação repetida pelo banco dentro da janela; e a inbox (`PK = faturaId`, `RK = idEvento`) com ETag na fatura, para a reentrega do Service Bus e para o que escapa da janela (Incidente 2). A inbox fica numa tabela separada da fatura e é gravada depois do efeito; quem impede a baixa dupla é a máquina de estados com ETag, e a janela entre gravar a baixa e publicar o `FaturaBaixada` está comentada no código como caso para o padrão Outbox em produção. O retry agendado usa `MessageId = {idEvento}-t{n}` justamente porque mensagens agendadas também passam pela detecção.

**Quando a alternativa vence:**

- Só detecção de duplicatas: o efeito já é naturalmente idempotente (upsert de estado absoluto) e o risco é só o produtor enviar a mesma mensagem de novo.
- Só idempotência no consumidor: a fila não pode ser recriada (a detecção é imutável) ou o throughput não comporta a janela.
- Nenhuma das duas: o efeito repetido é inofensivo e o custo do registro não se paga, como o próprio padrão admite.

**Fontes** (Consultado em 2026-10-14):

- [Duplicate detection](https://learn.microsoft.com/en-us/azure/service-bus-messaging/duplicate-detection): `MessageId`, janela, partição e mensagens agendadas.
- [Enable duplicate message detection](https://learn.microsoft.com/en-us/azure/service-bus-messaging/enable-duplicate-detection): só na criação; janela padrão de 10 minutos e máxima de 7 dias.
- [Idempotent Consumer pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer): inbox, escrita condicional e "don't substitute broker deduplication".
- [Asynchronous Request-Reply pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/asynchronous-request-reply): `Idempotency-Key` no request-reply assíncrono.
- [Designing Azure Functions for identical input](https://learn.microsoft.com/en-us/azure/azure-functions/functions-idempotent): comportamentos de uma função idempotente.
- [Message transfers, locks, and settlement](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement): o `message-id` como mecanismo para reconhecer reentregas.

---

## 9. DLQ, maxDeliveryCount e retry

**Marca:** [Slide]

**Pergunta:** "Por que escrever retry agendado na mão? O Functions não tem retry policy? E por que não deixar o Service Bus reentregar até cair na DLQ?"

**Resposta curta:** Porque a reentrega do Service Bus é imediata e conta para o `maxDeliveryCount`, e o Functions não oferece retry policy para o trigger de Service Bus (só para Timer, Event Hubs, Kafka e Cosmos DB). Para esperar entre tentativas, a aplicação agenda uma cópia; para erro permanente, manda direto para a DLQ com motivo.

**Resposta completa:**

Em peek-lock, abandonar a mensagem ou deixar o lock expirar a devolve "to the front of the retrieval order for redelivery" e incrementa o delivery count; ao passar do `MaxDeliveryCount` (padrão 10), ela vai para a DLQ com o motivo `MaxDeliveryCountExceeded`, comportamento que "can't be disabled". A DLQ não precisa ser criada, não observa TTL, não se limpa sozinha e guarda também os motivos da plataforma (`TTLExpiredException`, `HeaderSizeExceeded`, `Session ID is null`, `MaxTransferHopCountExceeded`); a aplicação pode mandar uma mensagem para lá com motivo e descrição próprios, e o Learn recomenda registrar o tipo da exceção no motivo. No Functions, "poison message handling can't be controlled or configured" no trigger de Service Bus, as retry policies existem só para Cosmos DB, Event Hubs, Kafka e Timer, e o `clientRetryOptions` do `host.json` "only apply to interactions with the Service Bus service. They don't affect retries of function executions."

A espera entre tentativas, então, é responsabilidade da aplicação: enviar uma cópia com `ScheduledEnqueueTimeUtc` e completar a original. A cópia agendada só aparece na hora marcada, ganha `SequenceNumber` novo e entra na detecção de duplicatas, por isso precisa de `MessageId` próprio. Para reprocessar mensagens da DLQ, o Learn aponta o Service Bus Explorer do Portal, que permite espiar, editar e mandar as mensagens de volta para a fila. Contraste útil: o trigger de Storage Queue tem `visibilityTimeout`, que atrasa a nova tentativa, e manda para a fila `-poison` depois de 5 tentativas.

**No kit da Saldaro:** No baseline do Incidente 3, com `maxDeliveryCount = 10`, as 10 reentregas imediatas acabam antes dos 45 s da falha transiente: mensagens boas caem na DLQ junto com as envenenadas, todas `MaxDeliveryCountExceeded`. O conserto liga `Cenario__DlqComMotivo` (`SchemaInvalida` direto na DLQ) e `Cenario__RetryAgendado` (cópia em 5 s × 2^n, ou seja, 10, 20 e 40 s, com `MessageId = {idEvento}-t{n}`; depois da terceira, `TentativasEsgotadas`) e baixa o `maxDeliveryCount` para 3 sem recriar a fila. A triagem (`triagem-dlq.ps1`) agrupa por `DeadLetterReason` e reprocessa com `MessageId = {original}-r{n}`; o alerta `alrt-dlq-confirmacoes` vigia a métrica `DeadletteredMessages`.

**Quando a alternativa vence:**

- Falha transiente curtíssima (segundos) e efeito idempotente: deixar a reentrega imediata do Service Bus resolver, com um `maxDeliveryCount` adequado.
- Poucas mensagens para reprocessar, sem correção em lote: o Service Bus Explorer do Portal.
- Fluxo de várias etapas com espera longa e compensação: orquestração (Durable Functions ou Logic Apps), em vez de agendamento manual.

**Fontes** (Consultado em 2026-10-14):

- [Service Bus dead-letter queues](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dead-letter-queues): motivos da plataforma, `MaxDeliveryCount`, dead-letter pela aplicação e reprocessamento pelo Service Bus Explorer.
- [Message transfers, locks, and settlement](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement): abandono e lock expirado devolvem a mensagem para reentrega.
- [Message sequencing and timestamps](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-sequencing): mensagens agendadas.
- [Azure Functions error handling and retry guidance](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-error-pages): triggers com retry policy.
- [Azure Service Bus trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus-trigger): poison messages tratadas pelo Service Bus.
- [Azure Service Bus bindings for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus): `clientRetryOptions` não refaz execuções.
- [Duplicate detection](https://learn.microsoft.com/en-us/azure/service-bus-messaging/duplicate-detection): mensagens agendadas na detecção.
- [Azure Queue storage trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger): `visibilityTimeout` e fila `-poison`.

---

## 10. Functions × Logic Apps × Container Apps × App Service × AKS

**Marca:** [Slide]

**Pergunta:** "Por que Azure Functions? Integração não é coisa de Logic Apps? E por que não Container Apps, App Service ou AKS?"

**Resposta curta:** O hub é código orientado a eventos, de alto volume e com regra de negócio (idempotência, máquina de estados, assinatura): é o perfil do Functions, serverless e com triggers nativos de Service Bus e HTTP. Logic Apps brilha em fluxo desenhado com conectores e gente no meio; Container Apps e AKS entram quando o time quer controlar o contêiner ou o Kubernetes.

**Resposta completa:**

O Learn compara Functions e Logic Apps como "serverless compute service" e "serverless workflow integration platform". O primeiro é code-first, com uma dúzia de tipos de binding e orquestração por Durable Functions; o segundo é designer-first, com mais de 1.400 conectores, Enterprise Integration Pack para B2B e histórico de execução no Portal (o desenvolvimento em C# para Logic Apps Standard ainda está em preview). Os dois se combinam: um workflow chama uma Function e vice-versa, e a arquitetura de referência de integração corporativa do Architecture Center usa APIM, Logic Apps, Service Bus e Event Grid juntos.

Na árvore de decisão de computação, Functions é FaaS ("event-driven programming, serverless patterns", sobrecarga operacional muito baixa); App Service é PaaS para web e APIs, com WebJobs para tarefas de fundo; Container Apps é serverless sobre Kubernetes, sem acesso à API do Kubernetes; AKS dá acesso direto à API e ao control plane, com a maior carga operacional (gestão do cluster, atualizações, segurança). O próprio Functions pode rodar dentro do Container Apps quando o time quer controlar a imagem.

**No kit da Saldaro:** Hub e banco simulado são Function Apps .NET 10 isolated (`func-saldaro-hub-<sufixo>` e `func-saldaro-banco-<sufixo>`) no Flex Consumption, com triggers de HTTP, Service Bus, Storage Queue e Timer. Logic Apps aparece no 3.8 como decisão de custo (compensa em baixo volume e com aprovação humana) e como exemplo de cliente que espera 202 no request-reply assíncrono (pergunta 18).

**Quando a alternativa vence:**

- Logic Apps: integração montada a partir de conectores SaaS ou B2B, aprovação humana, baixo volume, time que prefere designer.
- Container Apps: microsserviços em contêiner com imagem própria, dependências nativas ou GPU; Functions no Container Apps quando se quer o modelo de Functions com controle da imagem.
- App Service: API web síncrona e contínua, num plano dedicado.
- AKS: o time já opera Kubernetes e precisa da API do cluster.

**Fontes** (Consultado em 2026-10-14):

- [Integration and automation platform options in Azure](https://learn.microsoft.com/en-us/azure/azure-functions/functions-compare-logic-apps-ms-flow-webjobs): Functions × Logic Apps × WebJobs; C# no Logic Apps Standard em preview.
- [Choose an Azure compute service](https://learn.microsoft.com/en-us/azure/architecture/guide/technology-choices/compute-decision-tree): modelos de hospedagem, habilidades e sobrecarga operacional.
- [Use a message broker and events to integrate enterprise systems](https://learn.microsoft.com/en-us/azure/architecture/example-scenario/integration/queues-events): APIM, Logic Apps, Service Bus e Event Grid juntos.
- [Azure Functions scale and hosting](https://learn.microsoft.com/en-us/azure/azure-functions/functions-scale): Container Apps como hospedagem do Functions.

---

## 11. Flex Consumption × Consumption × Premium × Dedicated

**Marca:** [Guia]

**Pergunta:** "Por que Flex Consumption? O Consumption de sempre não serve? E o Premium, que não tem cold start?"

**Resposta curta:** O Flex Consumption é o plano serverless recomendado: escala por função até 1.000 instâncias, tem VNet integration, always ready opcional e roda .NET 10; o Consumption virou legado e, em Linux, não recebe versões novas e se aposenta em 30/09/2028. O Premium elimina o cold start com instâncias sempre aquecidas, mas cobra pelo menos uma instância o tempo todo.

**Resposta completa:**

A página de hospedagem marca o Consumption como "legacy" e manda usar o Flex Consumption em apps serverless novos. Em Linux, o Consumption não recebe recursos nem versões de linguagem novas (.NET 9 é a última) e se aposenta em 30/09/2028; o de Windows não é afetado, por enquanto. O Flex Consumption é só Linux, escala por grupo de funções (os HTTP juntos, os de Blob via Event Grid juntos, os Durable juntos e cada outro trigger sozinho), oferece instâncias de 512 MB (0,25 núcleo), 2.048 MB (1 núcleo, o padrão recomendado) e 4.096 MB (2 núcleos), tem always ready opcional para reduzir o cold start, VNet integration de saída, private endpoint de entrada e timeout padrão de 30 minutos sem teto (mantido o limite de 230 s para responder HTTP). Restrições: um app por plano, sem deployment slots, sem edição no Portal, sem o modelo in-process do .NET e sem migração in-place de outro plano para o Flex, nem do Flex para outro.

No Free Trial, o detalhe que manda é a cota regional do Flex: 250 núcleos por padrão, mas 15 núcleos (30.720 MB) em assinaturas Free Trial e Azure for Students, e as instâncias always ready contam na cota. O Premium escala por demanda com workers pré-aquecidos, e nele "at least one instance per plan must always be kept warm"; o Dedicated (App Service plan) dá preço previsível e escala manual ou por autoscale.

**No kit da Saldaro:** Dois planos Flex (FC1), um por app, com `instanceMemoryMB = 512` e `maximumInstanceCount` 10 no hub e 5 no banco, por grupo de escala: cerca de 13 núcleos no pior caso, dentro dos 15 do Free Trial (Atualização 2, A2.2). O plano B `-Plano Consumption` é Windows Consumption, porque o .NET 10 não roda no Consumption em Linux (ADR-0001). Como o Flex não edita no Portal, os consertos vêm prontos atrás de toggles (ADR-0006).

**Quando a alternativa vence:**

- Premium: tráfego contínuo, latência sem cold start, deployment slots, Windows com VNet.
- Dedicated: já existe App Service plan ocioso ou o custo precisa ser fixo e previsível; App Service Environment para isolamento total.
- Consumption em Windows: dependência de Windows, como .NET Framework ou módulos de PowerShell específicos.
- Container Apps: controle da imagem ou GPU.

**Fontes** (Consultado em 2026-10-14):

- [Azure Functions scale and hosting](https://learn.microsoft.com/en-us/azure/azure-functions/functions-scale): planos, Consumption legado, aposentadoria do Linux Consumption, timeouts, rede e cobrança.
- [Azure Functions Flex Consumption plan hosting](https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-plan): tamanhos, always ready, escala por função, cota de 15 núcleos no Free Trial e restrições.
- [Supported languages in Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/supported-languages): .NET 9 é a última versão no Linux Consumption; sem edição no Portal no Flex.
- [Differences between in-process and isolated worker process .NET Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-in-process-differences): .NET 10 não roda no Consumption em Linux.

---

## 12. .NET isolated × in-process

**Marca:** [Guia]

**Pergunta:** "Por que isolated worker e .NET 10? O in-process não é mais rápido? E o .NET 8, que é LTS?"

**Resposta curta:** O modelo in-process termina em 10/11/2026 e não roda no Flex Consumption; o isolated é o único que recebe versões novas do .NET. E o .NET 8 (e o 9) também sai de suporte no Functions em 10/11/2026, enquanto o .NET 10 vai até 14/11/2028.

**Resposta completa:**

No isolated worker, o código roda num processo .NET separado do host; no in-process, roda no mesmo processo, só com versões LTS, e para no .NET 8. O Learn recomenda "highly" migrar para o isolated: ele suporta versões LTS, STS e .NET Framework, middleware, injeção de dependência alinhada ao ecossistema .NET, tipos dos SDKs do Azure nos bindings, integração com ASP.NET Core e o Flex Consumption, que não aceita in-process. No cold start, o in-process aparece como "optimized" e o isolated como "configurable optimizations": a diferença existe e se trata com configuração.

Datas de suporte no Functions: .NET 10 até 14/11/2028; .NET 9 e .NET 8 até 10/11/2026 (a data do .NET 9 foi estendida quando o suporte STS passou a 24 meses). Depois do fim do suporte, a app continua rodando, mas não recebe correções de segurança nem otimizações, e a plataforma pode limitar a escala, em alguns casos a uma instância.

**No kit da Saldaro:** ADR-0001: .NET 10 isolated em C# no hub, no banco simulado e no console de ferramentas. A escolha inicial do professor foi .NET 8, e o material subiu para .NET 10 porque o .NET 8 venceria dentro do próprio trimestre. Os triggers usam `ServiceBusMessageActions` e `ServiceBusSessionMessageActions` com `AutoCompleteMessages = false`, recursos do isolated com a extensão de Service Bus 5.x.

**Quando a alternativa vence:** O in-process, nunca, para código novo. O .NET Framework 4.8.1 no isolated, quando há dependência de biblioteca que só existe no .NET Framework (Windows).

**Fontes** (Consultado em 2026-10-14):

- [Differences between in-process and isolated worker process .NET Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-in-process-differences): comparação dos modelos e fim do in-process.
- [Supported languages in Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/supported-languages): datas de fim de suporte.
- [Azure Functions language stack support policy](https://learn.microsoft.com/en-us/azure/azure-functions/language-support-policy): o que acontece depois do fim do suporte.
- [Azure Service Bus bindings for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus): `ServiceBusMessageActions` no isolated, extensão 5.x.

---

## 13. Table Storage × Cosmos DB × Azure SQL

**Marca:** [Guia]

**Pergunta:** "Por que faturas, inbox e idempotência em Table Storage? Não seria melhor Cosmos DB, ou um Azure SQL com transação de verdade?"

**Resposta curta:** O Table Storage dá o que o hub precisa (leitura por chave, insert condicional, ETag e transação dentro da mesma partição) pelo menor custo e no mesmo storage account das Functions. O Cosmos DB for Table vence em latência, índices secundários e distribuição global; o Azure SQL vence quando a regra exige transação entre entidades, joins e restrições, o caso de um razão financeiro de verdade.

**Resposta completa:**

O Table Storage é um banco NoSQL de chave e atributos, sem esquema, "typically lower in cost than traditional SQL for similar volumes of data", indicado para dados que dispensam joins, chaves estrangeiras e stored procedures. Cada entidade tem até 1 MB e 252 propriedades, e `PartitionKey` + `RowKey` formam a chave primária, única na tabela. Duas operações sustentam a idempotência: o Insert Entity falha com `EntityAlreadyExists` (409) se a chave já existe, que é o insert condicional; e o Update Entity com `If-Match` só grava se o ETag ainda for o lido, senão falha com `UpdateConditionNotSatisfied` (412), que é a concorrência otimista. A transação (entity group transaction) vale só para entidades da mesma tabela e da mesma `PartitionKey`, até 100 entidades e 4 MiB, de forma atômica.

O Cosmos DB for Table usa a mesma API e o mesmo SDK, com "higher performance and availability, global distribution, and automatic secondary indexes", modo serverless e entidades de até 2 MB. O guia de modelos de dados do Architecture Center põe a fronteira com clareza: para "strict multi-entity transactions", relacional; e lista "financial ledger recording, billing" entre as cargas típicas de bancos relacionais. O padrão Idempotent Consumer recomenda gravar a marca de deduplicação e o efeito na mesma transação, o que no Table Storage só é possível na mesma tabela e na mesma partição.

**No kit da Saldaro:** Tabelas no `stsaldaro<sufixo>`: `faturas` (`PK = faturaId`), `inbox` (`PK = faturaId`, `RK = idEvento`), `idempotencia` (`PK = clienteId`, `RK` = valor da `Idempotency-Key`) e `conciliacao`, além de `cobrancas` no banco simulado. A `Idempotency-Key` usa o insert condicional; a baixa usa ETag na fatura, e quem perde a disputa relê e reavalia. Como inbox e fatura ficam em tabelas diferentes, não há transação entre elas: a proteção é a máquina de estados com ETag (pergunta 8).

**Quando a alternativa vence:**

- Cosmos DB for Table: latência com SLA, distribuição global, consultas por propriedades além da chave (índices secundários), escala alta.
- Cosmos DB for NoSQL: documento por fatura com inbox e outbox no mesmo item ou na mesma partição, com transactional batch.
- Azure SQL: uma transação precisa cobrir fatura, inbox e outbox; relatórios com joins; restrições de integridade; razão contábil.

**Fontes** (Consultado em 2026-10-14):

- [Introduction to Table storage](https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview): modelo, custo, limites e comparação com o Cosmos DB for Table.
- [Insert Entity](https://learn.microsoft.com/en-us/rest/api/storageservices/insert-entity) e [Table Storage error codes](https://learn.microsoft.com/en-us/rest/api/storageservices/table-service-error-codes): chave única, 409 `EntityAlreadyExists` e 412 `UpdateConditionNotSatisfied`.
- [Update Entity](https://learn.microsoft.com/en-us/rest/api/storageservices/update-entity2): `If-Match` e concorrência otimista.
- [Performing entity group transactions](https://learn.microsoft.com/en-us/rest/api/storageservices/performing-entity-group-transactions): mesma tabela, mesma partição, 100 entidades e 4 MiB.
- [Azure Cosmos DB for Table overview](https://learn.microsoft.com/en-us/azure/cosmos-db/table/overview): recursos do Cosmos DB for Table.
- [Understand data models](https://learn.microsoft.com/en-us/azure/architecture/data-guide/technology-choices/understand-data-store-models): relacional × chave-valor e transações entre entidades.
- [Idempotent Consumer pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer): marca e efeito na mesma transação.

---

## 14. Key Vault × app settings × App Configuration

**Marca:** [Guia]

**Pergunta:** "Por que Key Vault, se o app setting já é criptografado? E onde entra o App Configuration?"

**Resposta curta:** O Key Vault guarda segredo com dono, versão, auditoria e controle de acesso; o app setting é configuração (criptografada em repouso, mas sem gestão de segredo); o App Configuration centraliza configuração e feature flags e complementa o Key Vault, sem substituí-lo. A Key Vault reference junta os dois: o código lê o app setting de sempre e a plataforma busca o valor no cofre com a identidade da app.

**Resposta completa:**

A página de Key Vault references resume: "App settings are securely encrypted at rest, but if you need capabilities for managing secrets, they should go into a key vault." A referência tem a forma `@Microsoft.KeyVault(VaultName=...;SecretName=...)` (ou `SecretUri=...`), e a identidade da app precisa de `Key Vault Secrets User` (no modelo RBAC). Sem versão na referência, a app passa a usar a versão nova em até 24 h, porque a plataforma guarda os valores em cache; qualquer mudança de configuração reinicia a app e busca tudo de novo, e há um endpoint (`config/configreferences/appsettings/refresh`) para forçar a resolução. Se a referência não resolve, a app recebe o texto literal `@Microsoft.KeyVault(...)`. A connection string do Application Insights não é considerada segredo e pode ficar como app setting comum.

No Key Vault, o modelo de permissão recomendado é o Azure RBAC; o access policy é "a legacy authorization system", e nele quem tem `Contributor` ou `Key Vault Contributor` consegue se dar acesso aos segredos. A partir da API 2026-02-01, o RBAC é o padrão para cofres novos. O App Configuration centraliza configuração hierárquica por ambiente, permite mudar configuração "without the need to redeploy or restart an application" e controla feature flags; a própria documentação diz que ele "complements Azure Key Vault, which is used to store application secrets".

**No kit da Saldaro:** O `kv-saldaro-<sufixo>` usa RBAC e nasce vazio no baseline; no 3.5 recebe `WebhookSecret` e `BancoApiKey`, que viram Key Vault references nos app settings do hub. O hub tem `Key Vault Secrets User`; o aluno, `Key Vault Secrets Officer`. O `deploy.ps1` espera as referências resolverem e reinicia o hub se preciso. Os toggles de cenário são app settings e não feature flags do App Configuration (ADR-0006): o app setting já aparece no Portal e já reinicia a app. No lab, o cofre tem soft-delete de 7 dias e purge protection desligada; em produção, ligada.

**Quando a alternativa vence:**

- App Configuration: muitas apps compartilhando configuração, feature flags com segmentação, troca de configuração sem restart.
- App setting simples: configuração que não é segredo, como a connection string do Application Insights.
- Leitura direta do cofre pelo SDK: segredo que gira com frequência e não pode esperar até 24 h nem um restart.

**Fontes** (Consultado em 2026-10-14):

- [Use Key Vault references as app settings](https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references): sintaxe, identidade, rotação em 24 h, restart e refresh, falha de resolução e Application Insights.
- [Azure RBAC vs. access policies](https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-access-policy): RBAC recomendado, access policy legado e padrão da API 2026-02-01.
- [What is Azure App Configuration?](https://learn.microsoft.com/en-us/azure/azure-app-configuration/overview): configuração dinâmica, feature flags e complemento ao Key Vault.
- [Securing Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/security-concepts): app settings criptografados e Key Vault references.

---

## 15. Identidade gerenciada × connection string

**Marca:** [Guia]

**Pergunta:** "Por que trocar a connection string por identidade gerenciada? E por que system-assigned, se user-assigned é mais flexível? Aliás, por que o hub precisou de restart depois das roles?"

**Resposta curta:** Identidade gerenciada tira o segredo do caminho: não há connection string para vazar, girar ou expirar, e o acesso vira role com escopo mínimo e auditável. System-assigned nasce e morre com o recurso (simples e bom para auditoria); user-assigned existe antes do recurso e pode ter as roles prontas, o que evita a corrida de propagação que o kit viu no primeiro deploy.

**Resposta completa:**

Com identidade gerenciada, "the Azure platform manages the identity, so you don't need to provision or rotate any secrets", e as conexões por identidade dão "more fine-grained access control and auditing". No Service Bus, os papéis de dados são `Azure Service Bus Data Owner`, `Data Sender` e `Data Receiver`, atribuíveis do namespace até a fila, e o Learn manda usar "the narrowest possible scope". O guia de boas práticas compara os dois tipos: user-assigned para criação rápida de recursos, recursos replicados, processos de aprovação e acesso necessário antes do deploy; system-assigned para auditar qual recurso fez a ação e para que as permissões morram com o recurso. Como a system-assigned nasce junto com o recurso, as role assignments "can't be created in advance". A página de conexões do Functions vai além e chama a user-assigned de "more flexible and recommended", e a de Key Vault references manda usar user-assigned quando a app precisa de segredo já na criação.

Sobre o restart: o Learn documenta a propagação (até 10 minutos no Azure RBAC e nas conexões do Functions, com possíveis 403 nesse meio-tempo; até 5 minutos no Service Bus) e documenta que, sem a permissão de leitura de gerenciamento (`Azure Service Bus Data Owner` ou uma role customizada com `Microsoft.ServiceBus/namespaces/*/read`), o trigger de Service Bus não dá erro na partida e cai, em silêncio, para uma estimativa por peek, o que atrasa ou erra as decisões de escala. Documenta também que qualquer mudança de configuração reinicia a app e rebusca as Key Vault references, e que, ao remover uma role, a app pode seguir com o token em cache (validade padrão de 24 h) até ser reiniciada. O Learn não diz "reinicie depois de adicionar a role"; o achado do kit é coerente com esses pontos.

**No kit da Saldaro:** A partir do 3.5 (`identidadeGerenciada = true`), o hub usa a identidade system-assigned (`ServiceBusConnection__fullyQualifiedNamespace`, `TablesConnection__serviceUri`, `DefaultAzureCredential` no Event Grid) com `Azure Service Bus Data Sender`, `Data Receiver` e `Reader` no namespace, `Storage Table Data Contributor`, `EventGrid Data Sender` e `Key Vault Secrets User`; o namespace ganha `disableLocalAuth` (ADR-0003). No primeiro deploy real, o worker com sessão parou de consumir, sem erro no log, até o `Reader` entrar e o hub reiniciar; por isso o `deploy.ps1` reinicia o hub depois que as Key Vault references resolvem. O `AzureWebJobsStorage` continua com connection string, compromisso documentado no contrato (5.2).

**Quando a alternativa vence:**

- User-assigned: as roles precisam existir antes da app (IaC com ordem estrita, Key Vault reference na criação), várias apps com as mesmas permissões, criação e destruição frequentes.
- Connection string ou SAS: cliente que não consegue obter token do Microsoft Entra ID; a arquitetura de referência do Architecture Center admite SAS "if you can't use Entra ID".

**Fontes** (Consultado em 2026-10-14):

- [Securing Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/security-concepts): identidade gerenciada e conexões por identidade.
- [Best practice recommendations for managed system identities](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/managed-identity-best-practice-recommendations): system × user-assigned e cache de token por cerca de 24 h para grupos e roles do token.
- [Configure connections to remote services in Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/manage-connections): user-assigned recomendada, propagação de até 10 minutos e roles por binding.
- [Use managed identities to access Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-managed-service-identity): papéis, escopo, propagação de até 5 minutos e restart para expirar o token.
- [Azure Service Bus trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus-trigger#scaling-permissions): permissão de escala e fallback silencioso.
- [Troubleshoot Azure RBAC](https://learn.microsoft.com/en-us/azure/role-based-access-control/troubleshooting): até 10 minutos para a role valer.
- [Use Key Vault references as app settings](https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references): restart rebusca as referências; user-assigned para segredo na criação.
- [Use a message broker and events to integrate enterprise systems](https://learn.microsoft.com/en-us/azure/architecture/example-scenario/integration/queues-events): Entra ID com identidade gerenciada; SAS quando não der.

---

## 16. Application Insights × Log Analytics × Azure Monitor

**Marca:** [Guia]

**Pergunta:** "Qual a diferença entre Application Insights, Log Analytics e Azure Monitor? Por que um workspace? E por que o lab desliga o sampling?"

**Resposta curta:** O Azure Monitor é a plataforma; o Log Analytics é onde os logs moram e são consultados com KQL; o Application Insights é o APM, que hoje grava num workspace do Log Analytics. Sampling economiza dados e custo, mas some com eventos individuais: o lab desliga para provar cada fatura, e a produção calibra.

**Resposta completa:**

O Azure Monitor é o serviço unificado de observabilidade; seus dados ficam em dois tipos de workspace: o Log Analytics workspace (logs e traces, consultados com KQL) e o Azure Monitor workspace (métricas Prometheus e OpenTelemetry, com PromQL). O Application Insights é "an application performance monitoring (APM) feature of Azure Monitor"; os recursos clássicos foram aposentados, e o recurso workspace-based manda a telemetria para um Log Analytics workspace, onde ingestão e retenção são cobradas. Alertas são do Azure Monitor: alertas de métrica avaliam métricas em intervalos; alertas de log search rodam uma consulta do Log Analytics numa frequência; os dois disparam action groups (e-mail, webhook, Functions, Logic Apps).

Dois cuidados de volume. No APIM, "logging all events might have serious performance implications": em teste interno, ligar o log derrubou de 40% a 50% do throughput acima de 1.000 requisições por segundo, e o Application Insights não é "an audit system"; o remédio é o sampling. No Functions, o sampling vem ligado por padrão (20 itens por segundo por host) e pode excluir tipos, como requisições e exceções. E, no Consumption, o APIM não tem resource logs no Log Analytics: os logs de requisição vão para o Application Insights (ou para o Event Hubs, por política), e o Learn recomenda o logger do Application Insights com connection string e identidade gerenciada. O SDK clássico do Application Insights para .NET (2.x) se aposenta em 31/03/2027; o caminho é o SDK 3.x ou, de preferência, a distribuição OpenTelemetry.

**No kit da Saldaro:** `log-saldaro-<sufixo>` e `appi-saldaro-<sufixo>` workspace-based; o hub, o banco e o APIM mandam para o mesmo Application Insights. O sampling está desligado no `host.json` e no worker, e o diagnóstico do APIM registra 100% das chamadas, porque as provas contam eventos por `faturaId` e o volume do lab é pequeno. As consultas de `kql/` rodam no escopo do Application Insights, com o equivalente do Log Analytics comentado. Alertas: `alrt-dlq-confirmacoes` (métrica `DeadletteredMessages`) e `alrt-heartbeat-ausente` (log search, a cada 5 min, janela de 15 min, contagem menor que 1). O kit fixa o SDK clássico 2.x e concentra a futura migração para OpenTelemetry na `ITelemetria` (Atualização 2, A2.1).

**Quando a alternativa vence:**

- Produção com volume: sampling ligado, excluindo requisições e exceções, e níveis de log por categoria.
- Trilha de auditoria: não usar o Application Insights como auditoria.
- Métricas Prometheus ou OpenTelemetry com PromQL: Azure Monitor workspace.
- Resource logs do APIM no Log Analytics: qualquer tier, menos o Consumption.

**Fontes** (Consultado em 2026-10-14):

- [Azure Monitor overview](https://learn.microsoft.com/en-us/azure/azure-monitor/fundamentals/overview): plataforma, workspaces e Application Insights como recurso do Azure Monitor.
- [Application Insights OpenTelemetry observability overview](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview): Application Insights como APM.
- [Create and configure Application Insights resources](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-workspace-resource): recursos clássicos aposentados; telemetria e cobrança no Log Analytics workspace.
- [Overview of Azure Monitor alerts](https://learn.microsoft.com/en-us/azure/azure-monitor/alerts/alerts-overview): tipos de alerta, stateful × stateless e action groups.
- [Integrate Azure API Management with Application Insights](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-app-insights): impacto de logar tudo, sampling e logger com identidade.
- [API Gateway Overview](https://learn.microsoft.com/en-us/azure/api-management/api-management-gateways-overview): logs de requisição no Log Analytics indisponíveis no Consumption.
- [Configure monitoring for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/configure-monitoring): sampling padrão e cenários de alto volume.
- [Monitor .NET and Node.js applications with Application Insights (Classic API 2.x)](https://learn.microsoft.com/en-us/azure/azure-monitor/app/classic-api): aposentadoria do SDK 2.x para .NET em 31/03/2027.

---

## 17. Bicep × Terraform × ARM × Portal

**Marca:** [Slide]

**Pergunta:** "Por que Bicep e não Terraform, que é o padrão do mercado? E se o kit tem Bicep, por que um guia de montagem no Portal?"

**Resposta curta:** Bicep porque o kit é só Azure, o estado mora no próprio Azure (sem arquivo de estado para proteger), o what-if mostra a mudança antes e todo recurso novo do Azure já está disponível no dia em que sai. O Terraform vence em multicloud ou onde o time já opera Terraform; o Portal é ótimo para aprender e péssimo para recriar.

**Resposta completa:**

O Bicep é uma linguagem declarativa que vira template ARM JSON no deploy ("a transparent abstraction over a Resource Manager JSON template that doesn't lose the capabilities"), com sintaxe mais curta que o JSON, suporte imediato a todos os tipos de recurso e versões de API (inclusive preview), deploy idempotente, módulos, what-if e "no state or state files to manage: Azure stores all states". A comparação oficial com o Terraform resume as diferenças: o Terraform guarda estado num `terraform.tfstate` que precisa ser protegido e copiado, atende multicloud e on-premises por providers e processa o plano no cliente; o Bicep processa no Azure, com validação prévia contra o Azure Policy, e lida melhor com mudanças fora da ferramenta (out-of-band). O Terraform tem a vantagem de destruir com facilidade tudo o que uma configuração gerencia, e o provider AzAPI também dá acesso imediato a recursos novos.

O Portal serve para aprender e explorar, e ainda exporta template como ponto de partida, mas não recria um ambiente em minutos nem deixa discutir drift. Para quem pergunta "e para impedir mudança manual?", o Bicep tem os deployment stacks: gerenciam um conjunto de recursos como unidade, com deny settings que bloqueiam alteração e exclusão fora do stack (só no plano de controle) e flags para apagar o que sai do template.

**No kit da Saldaro:** ADR-0001: Bicep (`infra/main.bicep`, módulos e um `.bicepparam` por cenário), aplicado pela Azure CLI com what-if antes de cada `create`; o Terraform ficou de fora por ser mais uma ferramenta e um arquivo de estado na máquina do aluno. Ao vivo, o conserto liga um app setting no Portal, o que é drift proposital: o próximo `deploy.ps1` volta ao que o `.bicepparam` declara, e o 3.7 usa isso com o `check-drift.ps1`. O guia de montagem no Portal mostra, clique a clique, o que o Bicep faz, para o aluno saber o que está automatizando.

**Quando a alternativa vence:**

- Terraform: multicloud ou híbrido, organização que já tem módulos, estado e processo em Terraform, necessidade de destruir uma configuração inteira como unidade.
- ARM JSON: ferramenta que só aceita JSON (o Bicep gera o mesmo JSON).
- Portal: aprendizado, exploração pontual, exportar template para começar o IaC.
- Deployment stacks: ambiente compartilhado em que mudança manual precisa ser bloqueada, não só detectada.

**Fontes** (Consultado em 2026-10-14):

- [What is Bicep?](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview): benefícios, what-if, sem arquivo de estado e relação com o ARM JSON.
- [Comparing Terraform and Bicep](https://learn.microsoft.com/en-us/azure/developer/terraform/comparing-terraform-and-bicep): estado, multicloud, processamento, mudanças out-of-band e cobertura do Azure.
- [Create and deploy Azure deployment stacks in Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/deployment-stacks): gerenciamento como unidade e deny settings.

---

## 18. Durable Functions × request-reply feito à mão

**Marca:** [Slide]

**Pergunta:** "Na Disciplina 04 o Durable Functions devolveu 202 com a URL de status de graça. Por que o Incidente 1 faz tudo à mão?"

**Resposta curta:** O Durable entrega o mesmo padrão (202 + endereço de status) pronto e brilha em orquestrações de várias etapas; o kit faz à mão para o aluno ver cada peça: 202 com `Location` e `Retry-After`, fila, status barato e `Idempotency-Key`. Com uma etapa só, fila + status é mais simples e expõe menos superfície.

**Resposta completa:**

O padrão Asynchronous Request-Reply do Architecture Center: a API valida, responde 202 com `Location` (o endereço de status) e `Retry-After`, e passa o trabalho para uma fila; o status responde 200 com o estado enquanto processa e 303 para o recurso quando termina (303 porque obriga o cliente a fazer GET; 302 pode repetir o POST). O mesmo artigo recomenda que o cliente mande uma `Idempotency-Key`, para reenviar sem duplicar, e lembra que clientes como o Logic Apps tratam o 202 do jeito deles. O Functions tem um teto que empurra para esse padrão: 230 s para responder uma requisição HTTP em qualquer plano, e a recomendação do Learn é o padrão assíncrono do Durable ou adiar o trabalho e responder já.

No Durable Functions, o `CreateCheckStatusResponse` devolve 202 com `Location` igual ao `statusQueryGetUri`; o cliente recebe 202 enquanto a orquestração roda e 200 no fim. As URLs de gerenciamento que vêm na resposta carregam uma chave de acesso no parâmetro `code` e incluem terminate, raise event e purge. Do lado cliente, o `CallHttpAsync` de um orquestrador segue o 202 sozinho, mas só em .NET e, no isolated, só com `asynchronousPatternEnabled` ligado. No Flex Consumption, o Durable aceita como provedores de estado o Azure Storage e o Durable Task Scheduler, este o recomendado pelo Learn.

**No kit da Saldaro:** No Incidente 1 (3.2), o `PostFatura` consertado responde 202 com `Location` e `Retry-After: 5`, reserva a `Idempotency-Key` com insert condicional e enfileira em `sbq-faturas-recebidas`; o `GetFaturaStatus` responde 200 enquanto processa e 303 quando a fatura chega a `baixada` ou `notificada`. O Passo 5.6 do guia compara com o Hands-on 3 da Disciplina 04: o Durable usa 202 enquanto processa e 200 no fim; a Saldaro usa 200 e 303, a variante do Architecture Center, e a URL de status da Saldaro é só leitura.

**Quando a alternativa vence:**

- Várias etapas com estado: fan-out/fan-in, timers, aprovação humana, saga com compensação.
- Fluxo longo (horas ou dias) que precisa de status, cancelamento e eventos externos prontos.
- Consumidor que espera 202 enquanto processa, como a ação HTTP do Logic Apps ou o `CallHttpAsync` do Durable.

**Fontes** (Consultado em 2026-10-14):

- [Asynchronous Request-Reply pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/asynchronous-request-reply): 202, `Location`, `Retry-After`, 200/303 e `Idempotency-Key`.
- [HTTP features in Durable Functions](https://learn.microsoft.com/en-us/azure/durable-task/durable-functions/durable-functions-http-features): `statusQueryGetUri`, chave nas URLs de gerenciamento e polling automático do `CallHttpAsync`.
- [Azure Functions scale and hosting](https://learn.microsoft.com/en-us/azure/azure-functions/functions-scale#function-app-timeout-duration): limite de 230 s.
- [Azure Functions Flex Consumption plan hosting](https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-plan): provedores de estado do Durable no Flex.
- [Durable Task Scheduler](https://learn.microsoft.com/en-us/azure/durable-task/scheduler/durable-task-scheduler): provedor recomendado.

---

## 19. Segurança de webhook: HMAC × mTLS × IP × OAuth

**Marca:** [Slide]

**Pergunta:** "Por que assinatura HMAC no webhook do banco? Não seria mais seguro mTLS, OAuth ou uma lista de IPs?"

**Resposta curta:** Cada mecanismo responde uma pergunta: HMAC prova que o corpo veio de quem tem o segredo e não foi alterado; mTLS prova quem abriu a conexão; lista de IPs reduz quem consegue chegar; OAuth prova quem chama com um token que expira. O kit usa HMAC com timestamp, que só depende de um segredo compartilhado e cobre integridade e repetição, e em produção soma lista de IPs ou mTLS conforme o contrato com o banco (ADR-0007).

**Resposta completa:**

A Microsoft usa HMAC nos próprios webhooks: os outgoing webhooks do Teams mandam um HMAC-SHA256 do corpo no header de autorização, e o receptor recalcula com o segredo e compara, "to ensure that your service is receiving calls only from actual Teams clients". Na entrega de webhook do Event Grid, as opções são um token do Microsoft Entra ID ou um segredo na query string; para trocar esse segredo sem falha, o Learn manda aceitar o antigo e o novo por um tempo, e a entrega só vai para HTTPS. No APIM, as três outras defesas existem em todos os tiers, inclusive no Consumption: `ip-filter` (o Learn pede lista de permissão, não de bloqueio), `validate-client-certificate` para mTLS (por padrão, confere revogação, cadeia de confiança e validade) e `validate-jwt` ou `validate-azure-ad-token` para OAuth.

Os limites de cada um importam na escolha. No Consumption, o mTLS exige ligar **Request client certificate** em **Custom domains**, e o tier não aceita certificados de CA para validar a cadeia; atrás de um Application Gateway, o certificado do cliente nem chega ao APIM. Lista de IPs sozinha é fraca, porque depende de o parceiro publicar IPs de saída estáveis. OAuth exige que o parceiro consiga obter token de um provedor de identidade. HMAC não precisa de infraestrutura, mas o segredo é compartilhado, a rotação precisa ser combinada e, sem um timestamp dentro da assinatura, a mesma requisição pode ser repetida.

**No kit da Saldaro:** O `WebhookBanco` valida `X-Saldaro-Signature: sha256=<hex>` sobre `{timestamp}.{corpo}`, com o `WebhookSecret`, compara com `CryptographicOperations.FixedTimeEquals`, exige |agora − timestamp| ≤ 300 s e valida antes de desserializar; assinatura inválida devolve 401 e nada vai para a fila (Incidente 2). A API `saldaro-webhooks` nunca exige subscription, porque quem autentica o banco é a assinatura. O segredo fica em app setting até o 3.5 e vira Key Vault reference depois. O ADR-0007, item 4, manda somar `ip-filter` ou mTLS em produção.

**Quando a alternativa vence:**

- mTLS: o parceiro ou a regulação exige; prefira um tier que aceite certificados de CA para validar a cadeia (o Consumption não aceita).
- Lista de IPs: o parceiro publica IPs de saída fixos; sempre como camada extra, não como única defesa.
- OAuth (`validate-jwt`, `validate-azure-ad-token`): o parceiro consegue obter token (client credentials, por exemplo).
- Webhook enviado pelo Event Grid: endpoint protegido com Microsoft Entra ID.

**Fontes** (Consultado em 2026-10-14):

- [Create an outgoing webhook (Teams)](https://learn.microsoft.com/en-us/microsoftteams/platform/webhooks-and-connectors/how-to/add-outgoing-webhook): HMAC-SHA256 do corpo e verificação no receptor.
- [Authenticate event delivery in Azure Event Grid](https://learn.microsoft.com/en-us/azure/event-grid/security-authentication): Microsoft Entra ID, segredo na query string, rotação e HTTPS.
- [Mitigate OWASP API security top 10 in Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/mitigate-owasp-api-threats): `ip-filter` com lista de permissão, `validate-client-certificate` e `validate-jwt`.
- [ip-filter policy](https://learn.microsoft.com/en-us/azure/api-management/ip-filter-policy): disponível no gateway Consumption.
- [validate-client-certificate policy](https://learn.microsoft.com/en-us/azure/api-management/validate-client-certificate-policy): todos os tiers, gateway Consumption incluído; `validate-revocation`, `validate-trust`, `validate-not-before` e `validate-not-after` valem `true` por padrão.
- [validate-jwt policy](https://learn.microsoft.com/en-us/azure/api-management/validate-jwt-policy) e [validate-azure-ad-token policy](https://learn.microsoft.com/en-us/azure/api-management/validate-azure-ad-token-policy): todos os tiers, gateway Consumption incluído.
- [Secure APIs using client certificate authentication in API Management](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-mutual-certificates-for-clients): mTLS no Consumption e atrás do Application Gateway.
- [Feature-based comparison of Azure API Management tiers](https://learn.microsoft.com/en-us/azure/api-management/api-management-features): autenticação por certificado de cliente em todos os tiers.

---

## 20. Private Endpoint e VNet

**Marca:** [Guia]

**Pergunta:** "Por que o kit não usa VNet nem Private Endpoint? Em produção, o que mudaria?"

**Resposta curta:** Porque o kit roda inteiro da máquina do aluno, pela internet, no Free Trial, e rede privada exige tiers pagos (APIM Standard v2 ou Premium, Service Bus Premium), DNS privado e VNet. Em produção, a Saldaro fecharia os endpoints públicos: APIM com private endpoint ou injeção em VNet atrás de um WAF, Functions com VNet integration e private endpoint, Service Bus Premium com Private Endpoint (ou NSP no Standard), Key Vault e Storage com private endpoint.

**Resposta completa:**

O que existe em cada serviço: o APIM Consumption não tem VNet nem private endpoint; o private endpoint de entrada existe nos outros tiers, menos o Basic v2; a VNet integration de saída, no Standard v2 e no Premium v2; a injeção, no Developer, no Premium e no Premium v2. No Functions, o Flex Consumption tem restrição de acesso, private endpoint e service endpoint de entrada e VNet integration de saída; o Consumption só tem restrição de acesso. No Service Bus, private endpoints e service endpoints são só do Premium; o Standard tem firewall de IP sem trusted services, e App Service e Functions não são trusted services: chegam ao namespace Premium por VNet integration. O Network Security Perimeter, GA, também controla o acesso público ao Service Bus (pergunta 23).

Um desenho de produção seguiria o Architecture Center: WAF na borda (Front Door ou Application Gateway), APIM com private endpoint de entrada e VNet integration (Standard v2) ou injetado (Premium v2), Functions Flex com VNet integration e private endpoint, Service Bus Premium com `publicNetworkAccess` desligado, Key Vault e Storage com private endpoint. Detalhes que pesam no projeto: o Event Grid não entrega por private endpoint e depende de trusted services com a identidade do tópico (ADR-0005); para alcançar um Key Vault restrito, a app precisa de rota pela VNet (no Flex, a rota é automática); a VNet integration do Flex exige o resource provider `Microsoft.App` registrado; e a telemetria privada do Azure Monitor passa pelo Azure Monitor Private Link Scope (AMPLS).

**No kit da Saldaro:** Sem VNet nem private endpoint (ADR-0003 e ADR-0007): os endpoints são públicos, a proteção é a autenticação (Microsoft Entra ID a partir do 3.5), os dados são fictícios e o Resource Group é apagado no fim do encontro. A discussão de rede fica para a leitura 3.8, como exercício de decisão.

**Quando a alternativa vence:** Sempre que uma política de segurança ou de conformidade proibir endpoint público de PaaS, quando houver risco de exfiltração de dados, ou quando os parceiros chegarem por ExpressRoute ou VPN.

**Fontes** (Consultado em 2026-10-14):

- [Azure API Management with an Azure virtual network](https://learn.microsoft.com/en-us/azure/api-management/virtual-network-concepts): opções de rede por tier e WAF na frente do APIM interno.
- [Azure Functions scale and hosting](https://learn.microsoft.com/en-us/azure/azure-functions/functions-scale#networking-features): recursos de rede por plano.
- [Azure Functions Flex Consumption plan hosting](https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-plan): VNet integration e `Microsoft.App`.
- [Network security for Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/network-security): private endpoint e service endpoint só no Premium; NSP.
- [Configure IP firewall rules for Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-ip-filtering): Standard sem trusted services; App Service e Functions fora da lista.
- [Use Key Vault references as app settings](https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references): cofre com restrição de rede e rota automática no Flex.
- [Protect APIs by using Azure Application Gateway and Azure API Management](https://learn.microsoft.com/en-us/azure/architecture/web-apps/api-management/architectures/protect-apis): desenho de referência.
- [Integrate Azure API Management with Application Insights](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-app-insights): AMPLS na ingestão de telemetria.

---

## 21. Não dava para só aumentar o timeout?

**Marca:** [Guia]

**Pergunta:** "No Incidente 1, por que não subir o timeout do APIM e da Function em vez de mudar a arquitetura?"

**Resposta curta:** No APIM Consumption a duração total de uma requisição é limitada a 30 s pela plataforma, e a Function não segura uma resposta HTTP por mais de 230 s em plano nenhum. Aumentar timeout só empurra o problema: o cliente desiste, reenvia e duplica.

**Resposta completa:**

A tabela de limites de runtime do gateway traz "Total request duration": ilimitada nos tiers clássicos e v2, 30 segundos no Consumption. Fora do Consumption, o `timeout` do `forward-request` tem padrão de 300 s, e valores acima de 240 s "may not be honored, because the underlying network infrastructure can drop idle connections after this time". No Functions, "regardless of the function app timeout setting, 230 seconds is the maximum amount of time that an HTTP triggered function can take to respond to a request", por causa do idle timeout do Azure Load Balancer; a recomendação do Learn para trabalhos longos é o padrão assíncrono do Durable ou adiar o trabalho e responder já. O guia OWASP do APIM vai na mesma direção: usar no `forward-request` o menor timeout aceitável e limitar a concorrência com o backend, porque conexão presa reduz o que o gateway consegue atender.

**No kit da Saldaro:** O APIM usa `<forward-request timeout="30" />` e um `on-error` que transforma o timeout em 504; o ERP simulado desiste aos 35 s; o `HttpClient` do hub para o banco espera 120 s; o banco simulado pode demorar até 200 s. A lição do Incidente 1 é responder já (202) e terminar depois, com a `Idempotency-Key` para o reenvio não duplicar.

**Quando a alternativa vence:** Chamada síncrona quando o backend responde de forma confiável em poucos segundos, bem abaixo dos tetos; nunca como conserto para um parceiro lento.

**Fontes** (Consultado em 2026-10-14):

- [API Gateway Overview](https://learn.microsoft.com/en-us/azure/api-management/api-management-gateways-overview): "Total request duration" de 30 s no Consumption.
- [forward-request policy](https://learn.microsoft.com/en-us/azure/api-management/forward-request-policy): padrão de 300 s e limite prático de 240 s.
- [Azure Functions scale and hosting](https://learn.microsoft.com/en-us/azure/azure-functions/functions-scale#function-app-timeout-duration): limite de 230 s para HTTP.
- [Mitigate OWASP API security top 10 in Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/mitigate-owasp-api-threats): menor timeout aceitável e `limit-concurrency`.

---

## 22. Por que CloudEvents?

**Marca:** [Guia]

**Pergunta:** "Por que o tópico do Event Grid usa CloudEvents e não o esquema do próprio Event Grid?"

**Resposta curta:** Porque CloudEvents é uma especificação aberta e o formato que o Architecture Center manda preferir; o esquema do Event Grid é proprietário e não extensível. E o par `source` + `id` do CloudEvent identifica o evento de forma estável, o que ajuda o ERP a deduplicar.

**Resposta completa:**

O guia de mensageria do Architecture Center é direto: o CloudEvents "provides high interoperability between vendor systems" e deve ser o preferido; o esquema do Event Grid é "proprietary, nonextensible" e só deve ser usado "when you can't use the CloudEvents schema". Em entregas para webhook, o Event Grid passa a usar a proteção contra abuso do CloudEvents 1.0 (o handshake por `OPTIONS`) no lugar do evento de validação próprio. E o padrão Idempotent Consumer lembra que, em CloudEvents, "the `source` and `id` attribute combination uniquely identifies an event and stays stable across redeliveries". Um detalhe que confunde: quando o destino é uma fila do Service Bus, o `MessageId` da mensagem é, por padrão, um ID interno do Event Grid, e o Learn não garante que ele seja o `id` do CloudEvent; dá para sobrescrevê-lo por delivery property, mas não junto com o `SessionId`.

**No kit da Saldaro:** O `FaturaBaixada` é CloudEvents 1.0, com `type = "Saldaro.Faturas.FaturaBaixada"`, `source = "/saldaro/hub"`, `subject = "faturas/{faturaId}"` e `id` = GUID novo (contrato 7.3). O ADR-0005 orienta o ERP a deduplicar pelo `id` do CloudEvent, porque duas entregas simultâneas da mesma baixa ainda podem passar pela guarda da `NotificarParceiro`.

**Quando a alternativa vence:** Consumidores que já dependem do esquema do Event Grid e não podem mudar.

**Fontes** (Consultado em 2026-10-14):

- [Asynchronous messaging options](https://learn.microsoft.com/en-us/azure/architecture/guide/technology-choices/messaging): preferir CloudEvents; esquema do Event Grid só quando não der.
- [Authenticate event delivery in Azure Event Grid](https://learn.microsoft.com/en-us/azure/event-grid/security-authentication): proteção contra abuso do CloudEvents 1.0.
- [Idempotent Consumer pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer): `source` + `id` como chave estável.
- [Configure Service Bus as an Event Grid handler](https://learn.microsoft.com/en-us/azure/event-grid/handler-service-bus): `MessageId` interno e delivery properties.

---

## 23. Network Security Perimeter resolve a rede do Service Bus Standard?

**Marca:** [Guia]

**Pergunta:** "Se o problema do Standard é rede, o Network Security Perimeter não resolve sem pagar Premium?"

**Resposta curta:** Em parte: o Network Security Perimeter está GA e o Service Bus é recurso suportado, então dá para negar o acesso público ao namespace Standard e liberar só regras explícitas (por assinatura ou por IP), sem Premium. Não é Private Endpoint (o tráfego continua pelo endpoint público), e o Learn não documenta como fica a entrega do Event Grid nesse cenário: é preciso testar antes de recomendar.

**Resposta completa:**

O Network Security Perimeter cria uma fronteira lógica em volta de recursos PaaS que ficam fora de VNets: no modo enforced, nega o tráfego público, menos o que estiver dentro do perímetro e o que regras explícitas liberarem (entrada por assinatura ou por IP; saída por FQDN), e gera logs de acesso. Está GA nas regiões públicas, e o Service Bus aparece como "Generally available"; o Event Grid aparece em preview, e só para domains. Regras de entrada por assinatura e tráfego dentro do perímetro não aceitam SAS, só autenticação do Microsoft Entra ID. A arquitetura de referência de integração do Architecture Center recomenda: "If your Service Bus namespace runs on Standard tier and doesn't use private endpoints, use Azure Network Security Perimeter", com regras de entrada por assinatura para os serviços que publicam e consomem.

Pontos práticos da página do Service Bus: a associação é feita em **Networking**, aba **Public access**, seção **Network security perimeter**; quando ativa, o `publicNetworkAccess` mostra `SecuredByPerimeter`; algumas assinaturas precisam registrar feature flags (`AllowNetworkSecurityPerimeter`); e as regras do perímetro não governam tráfego de private endpoint. Entre as limitações gerais, o nome do recurso associado pelo Portal não pode passar de 44 caracteres.

**No kit da Saldaro:** O kit não usa. Depois do 3.5, o hub já autentica só com Microsoft Entra ID (`disableLocalAuth`), que é o requisito para regras por assinatura. O ponto em aberto é a entrega do Event Grid na `sbq-notificacoes-parceiro`: o Event Grid não é membro de perímetro em GA, e o Learn não diz se a entrega com a identidade do tópico passa por uma regra de assinatura. Ver "Conflitos com o kit", item 3.

**Quando a alternativa vence:**

- Private Endpoint (Premium): o requisito é IP privado e nenhum caminho público.
- Network Security Perimeter: o requisito é "nenhum acesso público, exceto das nossas assinaturas ou IPs", no Standard.
- Firewall de IP do Standard: clientes com IPs fixos e nenhum serviço do Azure entregando no namespace (no Standard não há trusted services).

**Fontes** (Consultado em 2026-10-14):

- [What is a network security perimeter?](https://learn.microsoft.com/en-us/azure/private-link/network-security-perimeter-concepts): GA, recursos suportados, regras, SAS e limitações.
- [Network Security Perimeter - Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/network-security-perimeter): associação, `SecuredByPerimeter` e feature flags.
- [Use a message broker and events to integrate enterprise systems](https://learn.microsoft.com/en-us/azure/architecture/example-scenario/integration/queues-events): NSP para namespaces Standard sem private endpoint.
- [Configure IP firewall rules for Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-ip-filtering): Standard sem trusted services.

---

## Conflitos com o kit

### Pedem ajuste no material

1. **Rolling updates no Flex Consumption.** O ADR-0006 (Consequências) diz que "em produção, o Flex Consumption oferece rolling updates para trocar settings sem indisponibilidade", com base na página de como desabilitar funções, que recomenda o recurso sem ressalva. A página do plano Flex Consumption diz que "zero-downtime deployments by using rolling updates are currently in public preview", e a página de estratégias de atualização fala em regiões onde já é GA e regiões em que ainda está chegando. Pela regra "só GA", o ADR deveria marcar o recurso como preview (ou como GA apenas em algumas regiões, a confirmar para `brazilsouth`) e não recomendá-lo sem a ressalva. Fontes: [Flex Consumption plan](https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-plan), [Site update strategies in Flex Consumption](https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-site-updates), [How to disable functions](https://learn.microsoft.com/en-us/azure/azure-functions/disable-function).

2. **O limite de 30 s do APIM Consumption não aparece no Incidente 1.** A tabela de relógios do guia do Incidente 1 lista o APIM com `forward-request timeout="30"` ("o padrão do produto é 300 s") e diz que "subir o APIM para 120 s não muda nada"; o contrato (seção 3) também trata os 30 s como escolha da política. No tier do kit, a duração total da requisição já é limitada a 30 s pela plataforma ("Total request duration", Consumption), então subir para 120 s nem é possível. A lição não muda, mas a tabela deveria citar o teto do tier. E, como os dois relógios valem 30 s, não está documentado qual erro o `on-error` enxerga quando o teto da plataforma vence primeiro: se o primeiro deploy devolver 500 no lugar do 504 (item 3 do checklist), vale testar um `forward-request` um pouco abaixo de 30 s. Fonte: [API Gateway Overview](https://learn.microsoft.com/en-us/azure/api-management/api-management-gateways-overview).

3. **O Network Security Perimeter não aparece no ADR-0003 nem no guia 3.8.** O ADR-0003 diz que no Standard "não há isolamento de rede" e só reabre a discussão se "a Microsoft levar Private Endpoint ou trusted services ao Standard". Isso continua não acontecendo, mas surgiu outro controle de rede GA para o Standard: o NSP, que o Architecture Center recomenda justamente para namespaces Standard sem private endpoint. Ele não resolve o requisito de IP privado, mas muda a conversa do 3.8 ("Premium só por causa de rede?"). Sugestão: incluir o NSP como alternativa no ADR-0003 e no 3.8, e testar a entrega do Event Grid com o perímetro em modo enforced antes de recomendá-lo (pergunta 23). Fontes: [Network security perimeter](https://learn.microsoft.com/en-us/azure/private-link/network-security-perimeter-concepts), [Queues and events integration](https://learn.microsoft.com/en-us/azure/architecture/example-scenario/integration/queues-events).

4. **Fechar o backend com o APIM Consumption.** O ADR-0007 ("O que mudaria em produção", item 3) lista restrição de acesso de entrada ou private endpoint de entrada no Functions como formas de aceitar só o APIM. Com o gateway Consumption, nenhuma das duas isola: ele não tem IP determinístico (o Learn manda liberar a faixa do datacenter) e não alcança VNet. No Consumption, o caminho viável é autenticar o APIM no backend com a identidade gerenciada (`authentication-managed-identity` existe no gateway Consumption) e exigir App Service Authentication; restrição por IP e private endpoint só fazem sentido com tiers que tenham IP estável ou VNet. Fontes: [IP addresses of API Management](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-ip-addresses), [API Gateway Overview](https://learn.microsoft.com/en-us/azure/api-management/api-management-gateways-overview).

### Tensões já documentadas ou de recomendação (manter e explicar em aula)

5. **`Reader` como permissão de escala do trigger de Service Bus.** O ADR-0003 (atualização de 2026-10-14) dá `Reader` ao hub e sugere, em produção, uma role customizada com `Microsoft.ServiceBus/namespaces/*/read`, mantendo o `Data Owner` de fora. O Learn pede `Azure Service Bus Data Owner` "or a custom role that includes `Microsoft.ServiceBus/namespaces/*/read`" e, na dica para produção, recomenda o `Data Owner`. O `Reader` inclui `*/read` e cobre a ação; a escolha do kit é de menor privilégio e está bem justificada (o `Data Owner` permitiria criar filas, o problema da fila fantasma do 3.7; e não só ele: no primeiro deploy real do kit, o Owner da assinatura, sem `Data Owner`, criou filas pelo cliente de administração do Service Bus, porque o gerenciamento de entidades aceita as permissões do plano de controle. O privilégio mínimo vale para a identidade da app; para o usuário Owner, o 401 do 3.7 não acontece). Vale citar a redação exata do Learn no guia 3.5. Fonte: [Service Bus trigger, scaling permissions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus-trigger#scaling-permissions).

6. **System-assigned no lugar de user-assigned.** O kit usa identidade system-assigned no hub e no tópico (ADR-0003 e ADR-0005). A página de conexões do Functions chama a user-assigned de "more flexible and recommended", e as boas práticas lembram que role assignments de uma system-assigned não podem ser criadas antes do recurso. O restart que o kit precisou depois da propagação é o tipo de corrida que a user-assigned evita. A escolha é válida para o lab; vale registrar a alternativa em "o que mudaria em produção" no guia 3.5. Fontes: [manage-connections](https://learn.microsoft.com/en-us/azure/azure-functions/manage-connections), [managed identity best practices](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/managed-identity-best-practice-recommendations).

7. **`AzureWebJobsStorage` com connection string no 3.5.** O contrato (5.2) e o ADR-0003 tratam como compromisso documentado. No Flex Consumption, o Learn marca a identidade gerenciada para o host storage como "Full support" e "Recommended for MI", sem a dependência de Azure Files que, nos planos Consumption e Premium, exige contorno. O compromisso continua razoável por causa do plano B (Windows Consumption, que usa `WEBSITE_CONTENTAZUREFILECONNECTIONSTRING`), mas o guia 3.5 deveria dizer que, no Flex, não há bloqueio técnico. Fonte: [manage-connections, host storage](https://learn.microsoft.com/en-us/azure/azure-functions/manage-connections?pivots=functions-auth-identity&tabs=host).

8. **APIs do APIM sem subscription.** O guia OWASP do APIM manda proteger todas as APIs com subscription key ou política de autorização e não publicar produtos abertos. O ADR-0007 já documenta a exceção como simplificação de laboratório, com a lista de produção; basta manter o aviso em todo slide e guia que mostra os endpoints. Fonte: [Mitigate OWASP API threats](https://learn.microsoft.com/en-us/azure/api-management/mitigate-owasp-api-threats).

## Lacunas

1. **Restart depois de adicionar role.** O Learn não diz que o trigger precisa de restart depois que a role propaga. Documenta a propagação (até 10 minutos no Azure RBAC e no Functions; até 5 no Service Bus), o fallback silencioso do trigger para estimativa por peek sem a permissão de leitura, o restart que rebusca as Key Vault references e o restart para cortar o token depois de remover uma role. O achado do primeiro deploy fica como evidência empírica do kit.
2. **Assinatura de webhook de entrada.** Não há no Learn um guia genérico de HMAC com timestamp e janela contra repetição para webhooks recebidos de parceiros. Os exemplos mais próximos são os outgoing webhooks do Teams (HMAC) e a entrega do Event Grid (Microsoft Entra ID ou segredo na query string). Entre as páginas consultadas, o APIM não tem política pronta de validação HMAC.
3. **Event Grid entregando num Service Bus Standard com NSP.** Não está documentado se a entrega com a identidade do tópico passa por um perímetro em modo enforced, nem que regra seria necessária; o suporte do Event Grid ao NSP está em preview e só para domains.
4. **Erro do gateway Consumption no teto de 30 s.** O Learn não diz que status nem que `context.LastError` aparecem quando a duração total estoura, comparado com o timeout do `forward-request`.
5. **Divergências entre páginas do Learn**, que não devem ir para slide sem a ressalva:
   - "Exactly once": a comparação de serviços ("exactly once with sessions") e a habilitação da detecção de duplicatas ("guarantees exactly once delivery over a user-defined span of time") contra a comparação de filas ("isn't exactly adequate") e o padrão Idempotent Consumer ("impractical to guarantee exactly-once").
   - "Securing Azure Functions" ainda diz que o WAF exige ASE ou "Private Endpoints (preview)", enquanto a mesma página e a tabela de hospedagem tratam o private endpoint como disponível no Flex, no Premium e no Dedicated.
   - Service tags no Service Bus: a página do Premium lista como recurso só do Premium; a de segurança de rede diz que hoje incluem todos os tiers.
   - Integração APIM → Service Bus (preview): a página da política diz "APPLIES TO: Developer, Basic, Standard, Premium" e gateway clássico; a visão geral de gateways marca a integração nos gateways clássico, v2 e Consumption.
   - Rolling updates no Flex: preview na página do plano; GA em parte das regiões na página de estratégias de atualização.
   - Firewall de IP do Service Bus no Portal: a página do Premium diz que o Portal só configura no Premium; a de firewall menciona o Portal para outros tiers e também diz que a aba Networking só aparece no Premium; a de NSP manda associar em Networking, sem dizer o tier.
6. **Preços.** As páginas de preço não mostram valores sem a calculadora (APIM, Service Bus Premium, Front Door e Application Gateway com WAF). Comparações de custo em slide precisam vir da calculadora, com data.
7. **Cold start do APIM Consumption.** As páginas consultadas não falam de cold start no Consumption; não afirmar.
8. **Durable Task Scheduler.** A página do serviço não traz selo de preview, mas também não declara GA por SKU (Dedicated e Consumption); confirmar no Portal antes de recomendar.
9. **NSP no Service Bus Standard na prática.** A página do Service Bus não diz se há restrição de tier para a associação e avisa que algumas assinaturas precisam de feature flags; confirmar na assinatura (e no Free Trial) antes de usar no 3.8.
