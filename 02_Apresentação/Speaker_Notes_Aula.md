# Speaker Notes — Aula hands-on: Integração e Mensageria no Azure
## Pós-Graduação em Arquitetura Cloud Azure | Prof. Guilherme Campos | 1 encontro ao vivo de ~4h

> **Version-anchor:** Q4-2026 · **Fonte de verdade dos slides:** o deck v19, em `deck/project/`, e o índice `Conteudo_Slides_Aula.md` (29 slides). **Layout canônico:** `Disciplina 04 - Integração e Mensageria.pptx` (Blob da pós).
> **Modalidade:** 100% ao vivo. Cada slide traz **tempo previsto**, **o que mostrar** (Portal, VS Code, terminal) e **perguntas para a turma**, além de "O que falar", "Insights do mundo real", "Referências" e "Transição", no padrão da pós. Entre os slides 12 e 13, um bloco sem slide guia a montagem ao vivo no Portal.
> **Status:** v1.6 (2026-10-15). As notes seguem o deck v19, refeito em 2026-10-14 e já publicado: os 29 slides na ordem do `order` do `deck.json`, com o `<aside>` de cada slide como base da fala. A lógica da v19: primeiro o porquê de cada tecnologia, na ordem da montagem (slides 4 a 10); depois a conta, a ordem e a montagem ao vivo no Portal (11, 12 e o bloco sem slide); a Saldaro funcionando (13); e, no fim, os três incidentes (14 a 26). Os slides 2 a 13 são novos; os incidentes, o post-mortem, o desafio e o encerramento são as notes da v1.4, que já estavam corretas, renumeradas de 7 a 22 para 14 a 29. Saíram o apêndice de FAQ e os slides Regras do dia, A Saldaro Pagamentos, O que a Disciplina 04 já nos deu e o separador do Lab 0: o essencial das regras do dia foi para a capa e para o slide 14, a história da Saldaro 1.0 para o slide 2, e cada gancho "→ FAQ-N" virou uma resposta curta, com a pergunta da pesquisa `docs/research/2026-10-14-duvidas-das-tecnologias.md`. Onde as notes dizem "no primeiro deploy", o número é do primeiro deploy real do kit (2026-10-14, roteiro em `docs/research/2026-10-14-checklist-primeiro-deploy.md`); "no ensaio" é o ensaio dos incidentes pelo Portal; o da turma pode variar um pouco. Na v1.6, depois da primeira aula, os `<aside>` passaram a seguir estas notes (tempos, transições e o plano B depois do `limpar.ps1`), e o bloco da montagem ganhou os dois cuidados que vieram da primeira turma.
> **Tempo:** 165 min de slides + 60 de montagem ao vivo + 10 de pausa + 5 de limpeza = **240 min**, sem folga. Para caber, os incidentes ficaram com 0,6 do tempo do roteiro dos guias (30, 45 e 30 min, contra 50, 75 e 50): o professor conduz em ritmo de demonstração, quem chegou com o ambiente montado repete junto, e cada guia, completo, fica para a turma refazer em casa. Os `<aside>` do deck trazem os mesmos tempos.
> **Cuidados de telão, o dia todo:** em Environment variables, **Show value** só na linha, nunca o **Show values** da barra nem o **Advanced edit**; o Logs do Application Insights numa aba própria, em **KQL mode**, com o toggle **Agent** desligado; o **Warning** do Portal ao salvar a policy do APIM é só aviso: **Save**.
>
> Empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência.

---

## Slide 1 — Capa

**Tempo previsto:** 2 min · **Mostrar:** o slide, com a câmera aberta.

### O que falar
Bem-vindos. Hoje não é uma aula sobre serviços: é um plantão. Nas próximas quatro horas vocês vão trabalhar numa empresa fictícia chamada Saldaro Pagamentos, um hub de cobrança B2B que acabou de migrar para o Azure e carrega um legado cheio de decisões ruins. Três incidentes vão estourar na nossa mão, um de cada vez, e a missão é diagnosticar, consertar e provar que consertamos.

A diferença para a Disciplina 04 é de postura. Lá vocês aprenderam o que cada serviço faz e configuraram cada um no Portal. Aqui vocês já sabem o que é fila, tópico e DLQ; o que falta é a cicatriz: ver o sistema quebrar do jeito que quebra em produção e saber exatamente qual botão apertar.

Como vai ser o dia. Antes de quebrar, a gente monta: primeiro o porquê de cada peça, uma por slide, na ordem em que ela entra na montagem, com as alternativas, a configuração e o custo; depois eu monto a Saldaro ao vivo no Portal, e vocês, que montaram em casa, conferem no ambiente de vocês. Com a casa de pé, os três incidentes, sempre no mesmo ritual: sintomas, hipóteses, conserto e prova, e um post-mortem de cinco linhas, sem culpados. O conserto é no Portal; quem travar tem um plano B pronto, e ninguém fica para trás. E no fim todo mundo apaga o `rg-saldaro-lab`, junto comigo.

Tudo o que vocês vão ver é ficção inspirada em experiência de campo em projetos de consultoria da TFTEC. Empresa, pessoas, números e incidentes são inventados; qualquer semelhança é coincidência. O que não é inventado são os padrões: 202 assíncrono, idempotência, sessões, dead-letter com motivo. Esses aparecem toda semana em sistemas reais.

### Insights do mundo real
- A maioria dos incidentes de integração em produção não é bug de código: é timeout mal pensado, retry sem idempotência ou fila sem tratamento de mensagem envenenada.
- Quem sabe diagnosticar um sistema distribuído às 3h da manhã vale mais no mercado do que quem sabe criar o recurso no Portal.
- O ritual sintoma → hipótese → conserto → prova é o mesmo de um incidente real; treinar o ritual vale tanto quanto treinar a técnica.

### Referências
- [Cloud Design Patterns](https://learn.microsoft.com/azure/architecture/patterns/)
- [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/)
- `docs/adr/0004-confidencialidade-e-varredura-de-termos.md`: o aviso de ficção

### Transição
Primeiro, o que a Saldaro precisa resolver.

---

## Slide 2 — O que a Saldaro precisa resolver

**Tempo previsto:** 3 min · **Mostrar:** o slide; quem montou o Lab 0 em casa abre o próprio `rg-saldaro-lab` e confere que cada peça do slide existe de verdade (o do professor ainda não existe: nasce na montagem, depois do slide 12).

### O que falar
A Saldaro é um hub de cobrança B2B, e a empresa é fictícia. Trezentas empresas clientes usam a Saldaro para cobrar os próprios clientes pessoa jurídica, uns 400 mil por mês, com pico nos dias 5 a 10 e no fim do mês. Leiam a faixa da esquerda para a direita: o ERP da empresa cliente manda a fatura, o hub registra a cobrança no banco parceiro, o banco manda o webhook de confirmação, o hub dá baixa na fatura e avisa o ERP. Cada seta é uma integração que pode falhar, e a aula de hoje é sobre essas falhas.

Embaixo estão os seis requisitos e a peça que atende cada um. Rajada: o Service Bus absorve o pico e o Functions Flex Consumption escala as instâncias; no lab, até 10 no hub, por causa da cota do Free Trial. Não perder fatura: a fila guarda a mensagem até alguém processar, com retry e Dead-letter queue. Não cobrar duas vezes: o reenvio do ERP traz a mesma Idempotency-Key, e o insert condicional na tabela `idempotencia` devolve a fatura que já existe; a inbox faz o mesmo com as confirmações repetidas do banco. Confiar no webhook: a assinatura HMAC com timestamp; o webhook chega pela API `saldaro-webhooks` do APIM, que nunca exige subscription, porque quem autentica o banco é a assinatura, conferida pelo hub. Enxergar: o Application Insights junta a jornada de uma fatura pelo `faturaId`, que vai em todos os eventos do hub; o `operation_Id` só costura a parte síncrona, como vocês vão ver no slide 13. Custo: tudo serverless ou no tier de entrada, uns R$ 2 a 3 da véspera ao fim da aula; o único medidor por hora é o do Service Bus Standard.

Importante: no baseline que vocês montaram, várias dessas peças ainda não estão ligadas, de propósito. É a herança da "Saldaro 1.0", um monólito que usava o banco relacional como fila e um único worker de polling; a 2.0 nasceu no Azure carregando os hábitos da 1.0. Os incidentes mostram o que falta.

**Pergunta para a turma:** qual desses seis requisitos vocês apostam que quebra primeiro? É o pré-mortem do Lab 0.

### Insights do mundo real
- "Banco como fila" é muito mais comum do que parece, e quase sempre vem com um worker único e um polling que vira gargalo no primeiro pico.
- Hubs de cobrança vivem de idempotência: a mesma confirmação bancária chega duas vezes com mais frequência do que se imagina.
- Requisito não funcional sem a peça que o atende é desejo, não arquitetura: escrevam o requisito e a peça lado a lado, como no slide.

### Referências
- `docs/research/2026-10-13-briefing-da-aula.md`, seções 1 e 2
- `docs/adr/0007-http-triggers-anonymous-e-apim-sem-subscription.md`: por que a API do webhook não pede subscription no lab

### Transição
Agora, como essas peças se encaixam no hub: o que vamos montar.

---

## Slide 3 — O que vamos montar

**Tempo previsto:** 3 min · **Mostrar:** o diagrama deste slide e, ao lado, o Portal aberto na assinatura do professor, ainda sem o `rg-saldaro-lab`.

### O que falar
Antes de clicar em qualquer coisa, a planta da casa. Tudo vai morar num Resource Group só, o `rg-saldaro-lab`, montado à mão, no Portal, e é o mesmo ambiente que o Bicep do cenário 0 cria: os mesmos nomes, os mesmos SKUs e as mesmas configurações. A Saldaro 2.0 tem cinco papéis. A porta única é o APIM: o ERP da empresa cliente e o banco parceiro só falam com ele. O processamento são dois Function Apps no Flex Consumption: o hub, que é a Saldaro, e o banco simulado, que faz o papel do parceiro. A mensageria é o Service Bus, com três filas, e o Event Grid, que avisa o parceiro quando a fatura é baixada. O estado mora no Storage, em tabelas, e os segredos no Key Vault. E os olhos são o Application Insights, gravando num workspace do Log Analytics. Sigam a faixa de cima: é a jornada de uma cobrança atravessando as seis caixas, do `POST /faturas` do ERP ao aviso de volta. Cada peça que a gente criar hoje responde a uma pergunta: o que quebra se ela não existir?

### Insights do mundo real
- Quem já montou isso em projeto sabe: o desenho cabe num slide, mas a ordem de criação e as permissões entre as peças é que derrubam a primeira tentativa. É por isso que a montagem tem ordem.
- O Portal decide algumas coisas sozinho (o nome do plano Flex, o endereço do app, o GUID das role assignments, o TTL das filas): o ambiente fica funcionalmente igual ao do Bicep, mas não idêntico, e é por isso que o `deploy.ps1` não roda por cima dele.

### Referências
- Guia `01_Aulas/Lab0_Montagem_no_Portal_Guia.md`: o diagrama, a Tabela de Recursos e "O que fica diferente do Bicep"
- `docs/research/2026-10-14-duvidas-das-tecnologias.md` (a FAQ das tecnologias), perguntas 1, 4 e 10

### Transição
Antes de montar, o porquê de cada peça, na ordem em que ela entra. Primeiro, os olhos.

---

## Slide 4 — Por que Application Insights e Log Analytics

**Tempo previsto:** 2 min · **Mostrar:** o slide; no Portal, o `appi-saldaro-<sufixo>` na Overview, em Essentials, com o Logs workspace apontando para o `log-saldaro-<sufixo>`; depois o Search com o `faturaId` do smoke test e o End-to-end transaction details do POST. Quem montou em casa abre o seu agora; o professor mostra as mesmas telas na Etapa 3 da montagem e no slide 13. **Cuidado de telão:** ao abrir o Logs, **KQL mode** e o toggle **Agent** desligado.

### O que falar
Três nomes que a turma confunde. O Azure Monitor é a plataforma. O Log Analytics é onde o dado mora e é consultado com KQL. O Application Insights é o APM, a camada que entende transação distribuída, e hoje é sempre workspace-based: o modo clássico, sem workspace, foi aposentado, e ingestão e retenção são cobradas no workspace.

Na Saldaro, a fatura entra pelo APIM, passa pelo hub nas Functions, pelas filas do Service Bus e pelo Event Grid. Com o log de cada serviço no seu lugar, dá para olhar um recurso, mas a jornada se perde nas fronteiras, e a prova pede a junção pelo `faturaId`. Um Application Insights por app vence quando times e ciclos de vida são diferentes; aqui a jornada é uma só, então hub, banco simulado e APIM mandam para o mesmo.

Configuração: workspace Pay-as-you-go com retenção de 30 dias, em Brazil South, e sampling desligado, porque a prova conta eventos por `faturaId`, e um evento amostrado sumiria. Em produção é o contrário: sampling ligado e calibrado, tirando requisições e exceções da amostragem. Custo: os primeiros 5 GB por mês por conta de cobrança são grátis e o lab gera megabytes, então sai R$ 0; acima disso, uns US$ 4,60 por GB em Brazil South.

Se alguém perguntar de Prometheus: o Azure Monitor workspace é outro produto, para métricas com PromQL, e vence quando a equipe já usa Prometheus.

**Pergunta para a turma:** quem já perdeu uma investigação porque o log estava espalhado em três lugares?

### Insights do mundo real
- O `operation_Id` costura a parte síncrona; nas fronteiras assíncronas (fila, webhook, Event Grid) ele se perde. Uma chave de negócio em todos os eventos, como o `faturaId`, é o que prova a jornada.
- Em observabilidade, o que encarece é volume: telemetria sem sampling, log em nível Information de tudo e retenção longa. Daily cap só como rede de segurança: quando ele bate, a coleta para e o time fica cego.
- O kit fixa o SDK clássico 2.x do Application Insights; em produção, planejem a migração para OpenTelemetry antes da aposentadoria do 2.x, em 31/03/2027.

### Referências
- [Create and configure Application Insights resources](https://learn.microsoft.com/azure/azure-monitor/app/create-workspace-resource)
- [Design a Log Analytics workspace architecture](https://learn.microsoft.com/azure/azure-monitor/logs/workspace-design)
- [Azure Monitor Logs cost calculations and options](https://learn.microsoft.com/azure/azure-monitor/logs/cost-logs)
- [Configure monitoring for Azure Functions](https://learn.microsoft.com/azure/azure-functions/configure-monitoring)
- Guia de montagem, Etapa 3 (quadros "Por que este recurso"); pesquisa de dúvidas, pergunta 16

### Transição
Agora enxergamos a fatura; mas onde ela guarda o estado? No Table Storage.

---

## Slide 5 — Por que Table Storage

**Tempo previsto:** 2 min · **Mostrar:** o slide; no Portal, o `stsaldaro<sufixo>` em Data storage → Tables, com as cinco tabelas: `faturas`, `inbox`, `idempotencia`, `conciliacao` e `cobrancas` (quem montou em casa abre o seu; o professor mostra na Etapa 4 da montagem).

### O que falar
O hub precisa de pouco. Guarda poucas entidades, sempre lidas pela chave, com a PartitionKey igual ao `faturaId`, e precisa de duas operações que sustentam os consertos.

A primeira é o insert condicional: o insert falha com 409, `EntityAlreadyExists`, se a chave já existe. É a Idempotency-Key do Incidente 1: o primeiro POST grava; o reenvio do ERP recebe 409, lê o `faturaId` gravado e devolve o mesmo, sem criar outra fatura nem outra cobrança. É atômico, diferente de consultar e depois gravar. A segunda é a concorrência otimista: o update com `If-Match` só grava se o ETag ainda for o lido; senão falha com 412, e quem perde a disputa relê e reavalia. É o que segura a baixa duplicada do Incidente 2.

O Table Storage faz isso por centavos, no mesmo storage account que o Functions já exige. O Cosmos DB for Table usa a mesma API e o mesmo SDK e vence quando é preciso latência com SLA, índices secundários e distribuição global. O Azure SQL vence quando uma transação precisa cobrir fatura, inbox e outbox juntas, com joins e restrições: o caso de um razão contábil de verdade. O limite que a turma leva para casa: no Table Storage, a transação atômica só vale na mesma tabela e na mesma PartitionKey, até 100 entidades. Como inbox e fatura ficam em tabelas diferentes, quem impede a baixa dupla é a máquina de estados com ETag.

Configuração: StorageV2, Standard LRS, a redundância mais barata, porque o dado do lab se reconstrói fácil; as cinco tabelas nascem só com o nome, sem esquema nem índice. O desenho está na PartitionKey e na RowKey, e mudar depois é difícil e caro. Custo: uns R$ 1 por mês a conta inteira; no lab, centavos.

**Pergunta para a turma:** no sistema de vocês, o que impede o mesmo pedido de virar duas cobranças?

### Insights do mundo real
- Consultar e depois gravar não é idempotência: entre a leitura e a escrita cabe outra requisição. Insert condicional e ETag põem a decisão no armazenamento, numa operação só.
- O desenho de PartitionKey e RowKey precisa sair certo de primeira: no Table Storage não existe "acrescentar um índice", como num banco relacional.
- Em produção: acesso só por identidade (`Storage Table Data Contributor`), as tabelas numa conta separada da do host do Functions e, se a regra de negócio exigir transação entre entidades, o estado num banco relacional.

### Referências
- [What is Azure Table storage?](https://learn.microsoft.com/azure/storage/tables/table-storage-overview)
- [Design Azure Table storage for queries](https://learn.microsoft.com/azure/storage/tables/table-storage-design-for-query)
- [Performing entity group transactions](https://learn.microsoft.com/rest/api/storageservices/performing-entity-group-transactions)
- [Azure Cosmos DB for Table overview](https://learn.microsoft.com/azure/cosmos-db/table/overview)
- [Storage considerations for Azure Functions](https://learn.microsoft.com/azure/azure-functions/storage-considerations)
- Guia de montagem, Etapa 4; pesquisa de dúvidas, perguntas 8 e 13

### Transição
O estado está guardado; agora, como a fatura anda entre as peças? Com o Service Bus.

---

## Slide 6 — Por que Service Bus

**Tempo previsto:** 2 min · **Mostrar:** o slide; no Portal, o namespace `sb-saldaro-<sufixo>` na Overview, com o Pricing tier Standard, e em Entities → Queues, as três filas (quem montou em casa abre o seu; o professor mostra na Etapa 5 da montagem).

### O que falar
A pergunta que sempre aparece é por que Service Bus, se a Storage Queue é mais barata. O critério é o que passa pela fila. Fatura a emitir e confirmação de pagamento são comandos financeiros: não podem se perder nem ser aplicados duas vezes. O Learn posiciona o Service Bus como enterprise transactional messaging, para financial transactions, e ele é o único dos quatro com transações e duplicate detection, além de FIFO com sessions e DLQ com motivo.

A Storage Queue não garante ordem e não tem dead-lettering automático; ela está no kit de propósito, no banco simulado (a fila `banco-pagamentos`), e vocês vão comparar a fila `-poison` dela com a DLQ do Service Bus. Event Hubs é stream de telemetria com releitura. Event Grid é aviso reativo, e é para isso que ele entra na Saldaro, daqui a pouco.

Standard contra Premium: o Premium compra messaging units dedicadas, mensagens de até 100 MB e rede privada, a preço fixo, uns 677 dólares por mês por messaging unit, o que não cabe no Free Trial. O Standard tem tudo o que os incidentes usam, e o Learn o indica para desenvolvimento, QA e produção de baixo throughput. Trocar de tier é decisão por gatilho, não por nome: a ADR-0003 fixa cinco, entre eles a exigência de Private Endpoint e o erro 50009 de throttling recorrente. A Leitura 3.8 é esse exercício.

Configuração: um namespace Standard e três filas com lock de 1 minuto e `maxDeliveryCount` 10, sem sessions e sem duplicate detection, porque o baseline é ingênuo de propósito. Custo: uns 55 reais por mês, cobrados por hora, com ou sem tráfego. É o único medidor do kit que corre por hora: esquecer o Resource Group ligado é, na prática, pagar Service Bus.

**Pergunta para a turma:** no projeto de vocês, o que passa pela fila é comando ou é aviso?

### Insights do mundo real
- Escolha o broker pela semântica de entrega, não pelo preço por milhão de operações: comando processado duas vezes vira cobrança em dobro.
- O Basic não tem sessions nem duplicate detection, que são o conserto do Incidente 2: o tier mais barato pode ser o que não resolve.
- A troca para Premium existe no lugar, mas sem volta: as role assignments não migram e as mensagens não são copiadas. Planeje antes de precisar.
- Em produção: só Microsoft Entra ID (`disableLocalAuth`, o que a Leitura 3.5 liga) e alertas de `ThrottledRequests` e de `DeadletteredMessages` desde o primeiro dia.

### Referências
- [Choose between Azure messaging services](https://learn.microsoft.com/azure/service-bus-messaging/compare-messaging-services)
- [Compare Storage queues and Service Bus queues](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-azure-and-service-bus-queues-compared-contrasted)
- [Service Bus Premium messaging](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-premium-messaging)
- [Azure Service Bus throttling](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-throttling)
- `docs/adr/0003-service-bus-standard-com-identidade-gerenciada.md`; guia de montagem, Etapa 5; pesquisa de dúvidas, perguntas 4 e 6

### Transição
O hub entra nesse namespace com uma chave, a `RootManageSharedAccessKey`. Chave pede cofre, e a próxima peça é o Key Vault, que no baseline nasce vazio.

---

## Slide 7 — Por que Key Vault e identidade gerenciada

**Tempo previsto:** 2 min · **Mostrar:** o slide; no Portal, o `kv-saldaro-<sufixo>` na Overview (Soft-delete = Enabled, Purge protection = Disabled) e na aba Properties (Permission model = Azure role-based access control); depois o `func-saldaro-hub-<sufixo>` → Settings → Environment variables, com `WebhookSecret` e `BancoApiKey` em texto (o professor mostra nas Etapas 6 e 8 da montagem). **Cuidado de telão:** em Environment variables, **Show value** só na linha; nunca o **Show values** da barra, que mostra todos os segredos no telão.

### O que falar
Dois segredos sustentam a conversa entre o hub e o banco. O `BancoApiKey` vai no header `x-api-key` quando o hub registra a cobrança; o `WebhookSecret` é a chave do HMAC com que o banco assina cada webhook. No baseline, os dois estão em texto nos app settings, e o hub fala com Service Bus e Storage por connection string. É ingênuo de propósito: é a herança que a Leitura 3.5 conserta.

**Pergunta para a turma**, antes de seguir: o app setting já não é criptografado? É, em repouso. Mas o Learn diz que, se você precisa gerir segredo, ele vai para um key vault: dono, versão, auditoria e rotação. E o App Configuration não concorre com o cofre: centraliza configuração e feature flags e complementa o Key Vault, sem substituí-lo.

A outra metade é a identidade gerenciada. Com ela, não há connection string para vazar, girar ou expirar, e o acesso vira role com escopo mínimo e auditável. O incidente da 3.5 é justamente um segredo que expirou às 3h. Usamos a system-assigned porque é a mais simples e boa para auditoria; a user-assigned vence quando a role precisa existir antes do app.

O cofre nasce agora, vazio e com RBAC, porque duas escolhas não têm volta: o prazo do soft-delete, que fica em 7 dias, e a purge protection, desligada no lab para o `limpar.ps1` conseguir purgar. Em produção, ligada. Reparem: ser Owner da assinatura não deixa ninguém ler segredo; para isso existe a role Key Vault Secrets Officer. Na 3.5, o hub ganha Key Vault Secrets User e os app settings viram Key Vault references: o código continua lendo o mesmo app setting, e a plataforma busca o valor no cofre com a identidade do app.

Custo: o cofre cobra por operação, US$ 0,03 por 10 mil operações de segredo, e a plataforma guarda as references em cache, então as leituras são poucas. A identidade gerenciada não tem custo extra.

### Insights do mundo real
- Segredo com data de validade precisa de dono e de calendário: o incidente da 3.5 é um segredo que expirou às 3h da manhã, sem ninguém de plantão para girar.
- Em produção: um cofre por aplicação, ambiente e região, para o raio de um vazamento ser pequeno; purge protection ligada; acesso público desligado, com Private Endpoint; rotação automática; e as roles de administração do cofre por PIM.
- Apagar um cofre apaga também as role assignments e as event subscriptions ligadas a ele, e recuperar o cofre não as traz de volta.

### Referências
- [Provide access to Key Vault keys, certificates, and secrets with Azure RBAC](https://learn.microsoft.com/azure/key-vault/general/rbac-guide)
- [Use Key Vault references as app settings](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)
- [Azure Key Vault soft-delete](https://learn.microsoft.com/azure/key-vault/general/soft-delete-overview)
- [What is Azure App Configuration?](https://learn.microsoft.com/azure/azure-app-configuration/overview)
- Guia de montagem, Etapa 6 e Passo 8.5; `01_Aulas/Leitura_3.5_ManagedIdentity_Guia.md`; pesquisa de dúvidas, perguntas 14 e 15

### Transição
Guardem a frase "segredo que expira às 3h": ela volta na Leitura 3.5, quando o cofre deixa de estar vazio. Próxima peça: como avisar o parceiro quando a fatura é baixada.

---

## Slide 8 — Por que Event Grid + fila

**Tempo previsto:** 2 min · **Mostrar:** o slide; no Portal, o tópico `evgt-saldaro-faturas-<sufixo>` e a event subscription `evgs-notificar-parceiro`, com Endpoint Type Service Bus Queue e a identidade System Assigned; depois, na fila `sbq-notificacoes-parceiro`, Access control (IAM), com a role Azure Service Bus Data Sender dada ao tópico (o professor mostra na Etapa 7 da montagem).

### O que falar
Quando a fatura é baixada, o ERP do parceiro precisa saber. O jeito ingênuo é o hub chamar o parceiro direto. Aí o hub passa a conhecer cada consumidor, e no dia em que entrar um BI ou uma conciliação externa é preciso mexer no hub. Com o Event Grid, o hub publica o `FaturaBaixada` uma vez, como um fato, sem saber quem consome; um novo interessado é só mais uma event subscription.

Só que o Event Grid sozinho não basta: ele entrega at least once, sem garantia de ordem e sem deduplicar, e, no push, se o assinante falha repetidamente, os eventos acabam descartados. Por isso a event subscription entrega numa fila do Service Bus, a `sbq-notificacoes-parceiro`, e a função `NotificarParceiro` consome dela com o mesmo peek-lock, reentrega e DLQ dos incidentes. O Architecture Center chama esse desenho de crossover: se o evento exige um workflow, o Event Grid o manda para uma fila do Service Bus.

E por que não o Event Grid trigger direto na Function, que é o que o Learn recomenda para Functions? Porque esse destino aponta para a função, que só existe depois da publicação do código, e o Event Grid a chamaria com uma chave de acesso. Essa decisão está na ADR-0005.

Configuração: tópico em CloudEvents 1.0, a especificação aberta que o Architecture Center manda preferir, com identidade system-assigned. Essa identidade tem Azure Service Bus Data Sender só nessa fila, e a event subscription entrega com ela, sem chave nenhuma na entrega; por isso nada quebra quando a Leitura 3.5 ligar o `disableLocalAuth`. Nova tentativa no padrão: até 30 tentativas ou 24 horas. O dead-letter do Event Grid fica desligado no lab; em produção, liguem. Custo: zero no lab. A cobrança é por operação, com 100 mil operações grátis por mês, e não há medidor por hora.

**Pergunta para a turma:** se o parceiro ficar fora do ar, onde está o aviso nesse meio tempo? Deixe responderem antes de mostrar: na fila, sendo reentregue, e no limite na DLQ.

### Insights do mundo real
- Evento de integração é fato publicado uma vez: o publicador não conhece os consumidores, e um novo interessado é configuração, não mudança de código.
- At least once vale para o Event Grid também: o consumidor da fila precisa ser idempotente. A ADR-0005 já diz o que fazer se aparecer notificação repetida: ligar a duplicate detection na fila, o que exige recriá-la.
- No baseline, a publicação do hub no tópico ainda usa a `EventGridTopicKey`; a entrega, do tópico para a fila, já é por identidade. A Leitura 3.5 troca a publicação para a identidade do hub.

### Referências
- [Event Grid delivery and retry](https://learn.microsoft.com/azure/event-grid/delivery-and-retry)
- [Event delivery with a managed identity](https://learn.microsoft.com/azure/event-grid/managed-service-identity)
- [Asynchronous messaging options](https://learn.microsoft.com/azure/architecture/guide/technology-choices/messaging)
- [Create a custom topic](https://learn.microsoft.com/azure/event-grid/create-custom-topic)
- `docs/adr/0005-event-grid-entrega-em-fila-do-service-bus.md`; guia de montagem, Etapa 7; pesquisa de dúvidas, perguntas 4, 5 e 22

### Transição
Quem consome as filas e publica no tópico é código. A próxima peça é onde ele roda: o Azure Functions, com o hub e o banco simulado.

---

## Slide 9 — Por que Functions no Flex Consumption

**Tempo previsto:** 2 min · **Mostrar:** o slide; no Portal, o `func-saldaro-hub-<sufixo>` → Settings → Scale and concurrency (Instance memory = 512 MB, On demand maximum instance count = 10); no `func-saldaro-banco-<sufixo>`, a mesma tela com teto 5; se der tempo, Diagnose and solve problems, buscando Flex Consumption Quota (o professor mostra no Passo 8.3 da montagem). **Cuidado de telão:** em Environment variables, **Show value** só na linha; o **Show values** mostra todos os segredos no telão.

### O que falar
O hub é código orientado a eventos com regra de negócio: idempotência, máquina de estados, HMAC. Tem triggers nativos de HTTP, Service Bus e Timer e passa boa parte do tempo parado. É o perfil do Azure Functions. O Logic Apps brilha quando a integração se monta com conectores e com gente no meio, em baixo volume; o Container Apps, quando o time quer controlar a imagem; o App Service, para API web síncrona e contínua num plano dedicado; o AKS, quando o time já opera Kubernetes.

Dentro do Functions, o plano. O Learn chama o Flex Consumption de plano serverless recomendado e marca o Consumption como legado: em Linux, ele não recebe versões novas, o .NET 10 nem roda nele, e se aposenta em 30/09/2028. O Premium elimina o cold start com instâncias sempre aquecidas, mas cobra pelo menos uma instância o tempo todo; o Dedicated cobra a máquina ligada.

O Flex escala por grupo de funções, as HTTP juntas e cada trigger de fila sozinho, até 1.000 instâncias, e volta a zero. É isso que o Incidente 1 exercita com a rajada de 1.000 faturas.

A configuração tem um porquê de Free Trial: lá, a cota do Flex é de 15 núcleos por região. Com 512 MB, cada instância vale 0,25 núcleo; com teto de 10 no hub e 5 no banco, por grupo, o pior caso fica em uns 13 núcleos. Com os 2.048 MB do padrão, a mesma escala pediria uns 51. E .NET 10 isolated porque, no Functions, o .NET 8 sai de suporte em 10/11/2026, e o isolated é o único modelo de C# que o Flex aceita. Restrições para lembrar: um app por plano, sem deployment slots e sem edição de C# no Portal; por isso os consertos da aula vêm prontos atrás de toggles.

Custo: o Flex cobra por GB-s e por execução. A franquia é de 100 mil GB-s e 250 mil execuções por mês, por assinatura, e o lab fica em ~R$ 0. A ressalva: a página de preços diz que ela vale em assinaturas pagas e não cita o Free Trial; se não valer, o pouco que os apps executam sai do crédito de US$ 200.

**Pergunta para a turma:** quem tem Function em produção no plano Consumption, ou em .NET 8? Qual é o plano de migração, e para quando?

### Insights do mundo real
- Data de fim de suporte é requisito: o .NET 8 e o modelo in-process saem de suporte no Functions em 10/11/2026, e o Consumption em Linux se aposenta em 30/09/2028. Quem descobre isso no dia do incidente perde a semana.
- Em produção: 2.048 MB como ponto de partida, tetos pela cota da assinatura paga (250 núcleos por padrão, ampliável por chamado) e always ready no grupo HTTP para cortar o cold start.
- Teto de escala também é proteção: sem ele, a rajada pede instâncias além da cota, e a plataforma atrasa ou recusa execuções; o sintoma parece bug do hub.

### Referências
- [Azure Functions Flex Consumption plan hosting](https://learn.microsoft.com/azure/azure-functions/flex-consumption-plan)
- [Azure Functions scale and hosting](https://learn.microsoft.com/azure/azure-functions/functions-scale)
- [Event-driven scaling: limit scale-out](https://learn.microsoft.com/azure/azure-functions/event-driven-scaling#limit-scale-out)
- [Guide for running C# Azure Functions in the isolated worker model](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- `docs/adr/0001-stack-dotnet-10-isolated-bicep-e-powershell-7.md`; guia de montagem, Etapa 8; pesquisa de dúvidas, perguntas 10, 11 e 12

### Transição
Guardem o teto de 10 instâncias: ele volta no Incidente 1, quando a rajada chegar. Última peça: a porta de entrada.

---

## Slide 10 — Por que API Management

**Tempo previsto:** 4 min · **Mostrar:** o slide; no Portal, o `apim-saldaro-<sufixo>` → APIs, as duas APIs (`saldaro` e `saldaro-webhooks`), e a policy do Passo 10.8, com o `forward-request` de 30 s (o professor mostra na Etapa 10 da montagem). **Cuidado de telão:** ao salvar a policy, o Portal mostra um **Warning** por causa das chaves do JSON no `set-body`: é só aviso; **Save** no aviso grava normalmente.

### O que falar
O ERP e o banco parceiro não falam com a Function, falam com um endereço estável, o `azure-api.net` do `apim-saldaro` com o seu sufixo. Autenticação, limite, timeout, transformação e log por operação são preocupações transversais, e o padrão Gateway Offloading tira isso do código e põe num lugar só. Chave de função não basta: o guia de segurança do Functions diz que o único jeito de proteger de verdade o endpoint é autenticar o cliente. O preço do padrão: um salto de rede a mais, um componente que precisa estar sempre de pé, nada de regra de negócio no gateway, e ele só protege se o backend aceitar tráfego apenas dele. No lab, o backend está aberto de propósito: o `erp-simulado.ps1 -Direto` é o gateway bypass (ADR-0007).

APIM contra WAF: não é um ou outro. O WAF, no Application Gateway ou no Front Door, compara a requisição com o Core Rule Set da OWASP, barra bots e filtra por geografia, mas não sabe quem é a empresa cliente nem quanto ela pode chamar. O APIM governa a API: quem chama, quanto, por quanto tempo, com que contrato e com que telemetria. Em produção, o WAF fica na frente e o APIM atrás. No lab só tem o APIM, porque os incidentes são de integração, não de ataque web, e o WAF tem parcela fixa por hora, fora do Free Trial.

Consumption contra v2: é o mesmo produto, cobrado por chamada, com o primeiro milhão incluído e SLA de 99,95%. Perde o `rate-limit-by-key`, a rede privada e o cache interno, e tem teto de 30 s por requisição. No 3.1, contornamos com uma subscription por empresa cliente e o `rate-limit`, que conta por subscription. O Standard v2 vence quando o backend está em VNet, quando é preciso limitar por chave ou passar de 30 s, mas cobra por hora. O corte de 30 s do Incidente 1 coincide com o teto do tier.

**Pergunta para a turma:** no projeto de vocês, quem fica na frente da API hoje: o WAF, o APIM ou os dois?

### Insights do mundo real
- WAF na frente do APIM, nunca no lugar dele: Front Door quando o tráfego é global ou multirregião; Application Gateway quando é regional e os backends estão numa VNet.
- Com o Front Door na frente, o APIM pode aceitar só o tráfego dele, até no Consumption: `ip-filter` nas faixas `AzureFrontDoor.Backend` e `check-header` no `X-Azure-FDID`. Atrás de um Application Gateway, cuidado: o certificado de cliente (mTLS) não chega ao APIM.
- Em produção, a identidade da empresa cliente sai da credencial (subscription ou, melhor, OAuth com `validate-jwt`), nunca de um header como o `x-cliente-id`, e o backend só aceita quem vier do APIM.
- No Learn, "WAF" também é a sigla do Well-Architected Framework: confirmem de qual dos dois a pessoa está falando.

### Referências
- [API Gateway overview](https://learn.microsoft.com/azure/api-management/api-management-gateways-overview)
- [Feature-based comparison of API Management tiers](https://learn.microsoft.com/azure/api-management/api-management-features)
- [Introduction to Azure Web Application Firewall](https://learn.microsoft.com/azure/web-application-firewall/overview)
- [Protect APIs by using Application Gateway and API Management](https://learn.microsoft.com/azure/architecture/web-apps/api-management/architectures/protect-apis)
- [Gateway Offloading pattern](https://learn.microsoft.com/azure/architecture/patterns/gateway-offloading)
- [Securing Azure Functions](https://learn.microsoft.com/azure/azure-functions/security-concepts)
- `docs/adr/0007-http-triggers-anonymous-e-apim-sem-subscription.md`; guia de montagem, Etapa 10; pesquisa de dúvidas, perguntas 1, 2, 3 e 21

### Transição
Sete peças escolhidas, cada uma com o seu porquê. Antes de montar, a conta.

---

## Slide 11 — Quanto custa

**Tempo previsto:** 3 min · **Mostrar:** a tabela do slide; no Portal, quem montou em casa abre o próprio `rg-saldaro-lab` → Overview, com a lista de recursos e o botão Delete resource group, que é por onde a aula termina (o do professor nasce na montagem e é apagado ao vivo no slide 28).

### O que falar
Antes de montar, a conta. Só um medidor corre por hora neste kit: o Service Bus Standard, uns R$ 0,07 por hora, com ou sem tráfego. É ele que responde por uns R$ 55 dos ~R$ 60 por mês que o ambiente custa se alguém esquecer o Resource Group ligado. Todo o resto cobra por uso, e o volume do lab cabe na franquia gratuita: o APIM Consumption até 1 milhão de chamadas por mês, o Event Grid até 100 mil operações, o Log Analytics até 5 GB, e o Application Insights entra junto com o Log Analytics. O Key Vault não tem franquia, mas no lab custa centavos; o Storage fica perto de R$ 1 por mês se ficar ligado.

Uma ressalva no Flex Consumption: a franquia é de 100 mil GB-s e 250 mil execuções por mês, por assinatura, e a página de preços diz que ela vale em assinaturas pagas, sem citar o Free Trial. Se não valer no seu, o pouco que os dois apps executam sai do crédito de US$ 200, em valores pequenos. No Free Trial, cuidado com a cota do Flex: 15 núcleos por região. Por isso o kit usa 512 MB por instância e no máximo 10 instâncias no hub e 5 no banco simulado.

Na prática: montar e apagar no mesmo dia sai uns R$ 0,20 a 1; montar na véspera e apagar no fim da aula, uns R$ 2 a 3. Montar à mão leva umas 3 horas contra uns 10 minutos do script, e a diferença na conta são uns R$ 0,20 de Service Bus. Os valores usam ~R$ 5,50 por dólar e preços de lista de Brazil South em outubro de 2026; confiram na calculadora na data da aula.

A regra de ouro: o ambiente que vamos montar só vive até o fim da aula. Rodem `pwsh ./scripts/limpar.ps1` com o seu sufixo ou, pelo Portal, apaguem o Resource Group e purguem o Key Vault e o APIM, que senão ficam presos no soft-delete com o nome reservado. E evitem APIM Developer, Service Bus Premium, Functions Premium e Defender for Storage: o kit não precisa de nenhum deles.

**Pergunta para a turma:** quem já viu uma fila "esquecida" gerar conta no fim do mês? Deixe alguém contar em 30 segundos.

### Insights do mundo real
- Num ambiente serverless, quem define a regra de ouro é o único recurso com medidor por hora: é ele que cobra quando todo mundo esqueceu.
- Budget com alerta avisa, mas não desliga nada: o único jeito de não pagar por um lab esquecido é apagar.
- Soft-delete reserva o nome: sem purgar o Key Vault e o APIM, o mesmo sufixo não volta, e a próxima montagem para no primeiro nome.

### Referências
- [Calculadora de preços do Azure](https://azure.microsoft.com/pricing/calculator/)
- [Azure free account](https://azure.microsoft.com/free/)
- [Create and manage budgets](https://learn.microsoft.com/azure/cost-management-billing/costs/tutorial-acm-create-budgets)
- Guia de montagem: a caixa "CUSTO E FREE TRIAL", a Tabela de Recursos e a Limpeza de Recursos

### Transição
Com a conta na cabeça, a ordem da montagem. Treze etapas, e cada peça resolve um problema.

---

## Slide 12 — A ordem da montagem

**Tempo previsto:** 3 min (o que passar disso sai da montagem) · **Mostrar:** o guia de montagem aberto na Etapa 2 e um dos quadros "Por que este recurso" aberto, para a turma ver o formato (objetivo, descrição, papel, por que este e não alternativas, configuração, custos, produção e fontes).

### O que falar
Treze etapas, e a ordem não é estética: cada peça depende da anterior. Primeiro o Resource Group, porque tudo o que nasce junto morre junto, e a limpeza no fim é um clique. Depois os olhos, Log Analytics e Application Insights, porque a gente quer enxergar o sistema antes de quebrá-lo. Depois o estado, no Storage; a mensageria, no Service Bus; o segredo, no Key Vault; e o aviso ao parceiro, no Event Grid. Só então os dois Function Apps, que já nascem apontando para tudo isso. Por último, o APIM, a porta única. Uma dica de quem já montou: comece a criar o APIM cedo, porque ele leva alguns minutos para ativar; na validação do kit, ele ativou em uns 2 minutos. Em cada etapa, o guia tem um quadro "Por que este recurso", com o objetivo, o papel na Saldaro, as alternativas, cada opção do formulário e o custo. Eu vou montando aqui, e explicando o porquê de cada peça.

Tempos de cada etapa no guia, para quem monta pela primeira vez: 1, máquina, Portal e sufixo, ~15 min (~5 se já fez as Etapas 1 a 4 do Lab 0); 2, Resource Group, ~3; 3, Log Analytics e App Insights, ~8; 4, Storage, ~12; 5, Service Bus, ~15; 6, Key Vault, ~5; 7, Event Grid, ~12; 8, Function Apps no Flex, ~35; 9, publicar o código, ~15; 10, API Management, ~30 com a espera da ativação; 11, roles do seu usuário, ~8; 12, `.env.local` e a prova, ~15; 13, post-mortem e pré-mortem, ~5. Ao vivo, eu chego com a Etapa 1 pronta e deixo a 13 para casa.

**Se perguntarem** por que APIM e não WAF, por que Service Bus e não Storage Queue, ou por que Functions e não Logic Apps: são os slides 10, 6 e 9; voltem a eles numa frase e sigam.

### Insights do mundo real
- Na validação do kit, a montagem inteira pelo Portal levou cerca de 75 minutos sem pausa; um aluno, com leitura e cuidado, leva umas 3 horas. Em produção, isso nasce de código (o Bicep do kit faz o mesmo em uns 10 minutos), e o Portal fica para explorar.
- Ordem de criação é dependência: o hub precisa do endereço do banco, do tópico e da fila antes de ganhar os app settings. Quem monta fora de ordem volta três telas para buscar um valor.

### Referências
- Guia `01_Aulas/Lab0_Montagem_no_Portal_Guia.md`, Etapas 1 a 13
- `docs/adr/0008-portal-primeiro-com-bicep-como-plano-b.md`: o Portal como caminho ao vivo

### Transição
Vamos montar. E, no fim, o smoke test prova que a casa está de pé.

---

## Montagem ao vivo — entre os slides 12 e 13 (sem slide)

**Tempo previsto:** 60 min (0:30–1:30) · **Mostrar:** o Portal em tela cheia, na assinatura do professor, e o guia `01_Aulas/Lab0_Montagem_no_Portal_Guia.md` numa segunda tela, das Etapas 2 a 12 (até o Passo 12.5); o slide 12 volta à tela quando a turma perguntar em que etapa estamos.

### Roteiro

| Etapas do guia | No guia (primeira vez) | Meta no relógio da aula |
|---|---|---|
| 1. Máquina, kit, Portal na assinatura certa, sufixo e o Flex conferido em Brazil South | ~15 min | pronta antes de 0:00 |
| 2 e 3. Resource Group, Log Analytics e Application Insights; logo depois, o Passo 10.1 (só o serviço do APIM), como o guia sugere | ~11 min | até 0:38 |
| 4 a 7. Storage, Service Bus, Key Vault e Event Grid | ~44 min | até 0:58 |
| 8 e 9. Os dois Function Apps (escala, deploy, identidade, segredos e app settings) e a publicação do código | ~50 min | até 1:15 |
| 10 e 11. APIs, operações, subscription, policy e as roles do seu usuário | ~38 min, com a ativação | até 1:25 |
| 12. `.env.local`, smoke test, a fatura e o Search (Passos 12.1 a 12.5) | ~15 min | até 1:30 |
| 13. Post-mortem e pré-mortem da montagem | ~5 min | para casa: o pré-mortem foi a pergunta do slide 2 |

Os 60 min supõem a Etapa 1 pronta antes da aula, o Passo 12.7 contado no slide 13 e a Etapa 13 para casa; na validação do kit, a montagem inteira, sem pausa, levou ~75 min.

### O que falar
Monte narrando a decisão de cada formulário, não o clique: o porquê já foi dito nos slides 4 a 10, e aqui ele vira campo preenchido. Abra o quadro "Por que este recurso" só quando a turma perguntar, e pare nas caixas que os incidentes vão mexer: o Pricing tier Standard do Service Bus, que no assistente aparece em All Available Pricing, porque o Recommended é o Premium (Passo 5.1); as caixas Enable sessions e Enable duplicate detection desmarcadas na `sbq-confirmacoes-banco` (Passo 5.3), que o Incidente 2 vai querer marcadas; os toggles `Cenario__*` em `false` no hub (Passo 8.8), que são os consertos dos três incidentes; o teto de instâncias (Passo 8.3), que segura a rajada do Incidente 1; e a policy de 30 s do APIM (Passo 10.8), que é o relógio do Incidente 1. Quem montou em casa acompanha no próprio ambiente e confere os mesmos campos; quem chegou sem ambiente sobe o Lab 0 pelo script agora, `./scripts/deploy.ps1 -Cenario 0 -Sufixo <seu sufixo>`, uns 8 a 10 minutos, e acompanha pela tela.

Dois cuidados da primeira turma: tudo roda no `pwsh`, e não no Windows PowerShell 5.1 (confiram com `$PSVersionTable.PSVersion`); e, antes de publicar na Etapa 9, `az account set` na assinatura da montagem, porque com o az noutra assinatura a publicação não acha o Resource Group. Se o Deploy do VS Code travar, o bloco `az` da Etapa 9 é o plano B.

Se o relógio apertar, corte nesta ordem: a Etapa 13 já é de casa; o Passo 12.6 (comparar com o Bicep) é opcional no guia; a publicação da Etapa 9 vai pelo bloco Equivalente `az` dela, o caminho validado do guia; e o que é repetição numa etapa (a segunda tabela em diante, as filas que não são a de confirmações) vai pelo bloco Equivalente `az` da mesma etapa. Não corte o Passo 8.3 nem o 10.8: são eles que fazem o Incidente 1 acontecer como no slide. Travou de vez? O plano B do guia apaga e sobe pelo script, uns 15 minutos: `pwsh ./scripts/limpar.ps1 -Sufixo <sufixo>` e depois `pwsh ./scripts/deploy.ps1 -Cenario 0 -Sufixo <sufixo>`.

Antes de fechar a montagem, um aviso que vale para o resto do dia: o `deploy.ps1` não roda por cima de um ambiente montado à mão. O Portal batiza sozinho o plano Flex de cada app e cria as role assignments com outro nome, e o Bicep para nos dois (guia de montagem, "Antes do primeiro `deploy.ps1`"). Para quem montou à mão, o plano B dos incidentes é o `limpar.ps1` e depois o `deploy.ps1 -Cenario N -Pronto`.

### Cuidados de telão
- Environment variables (Passos 8.7 e 8.8): **Show value** só na linha; nunca o **Show values** da barra nem o atalho **Advanced edit**, que mostram de uma vez todas as chaves e connection strings do app.
- Os dois segredos do Passo 8.6 ficam só na memória da sessão do PowerShell e vão para o Portal pelo clipboard: nada na tela, no histórico ou em arquivo. No fim da Etapa 8, feche a janela e copie qualquer outra coisa.
- Nas telas de chave (Access keys do storage, Passo 4.5; `RootManageSharedAccessKey`, Passo 5.5; Access keys do tópico, Passo 7.4), mostre onde a chave mora, sem revelar o valor.
- Policy do APIM (Passo 10.8): o **Warning** sobre o formato das expressões aparece por causa das chaves do JSON no `set-body`; é só aviso: **Save** no aviso grava a policy.
- Se abrir o Logs do Application Insights, que seja numa aba própria, em **KQL mode**, com o toggle **Agent** desligado.

### Insights do mundo real
- Montar à mão uma vez ensina onde cada peça mora; montar à mão duas vezes é desperdício. O que fica no ar nasce de código, e o Portal fica para explorar e para consertar no incidente.
- Algumas decisões só existem na criação (sessions e duplicate detection na fila, o prazo do soft-delete no Key Vault, o nome do APIM): um minuto a mais de atenção no formulário vale mais que uma hora de migração.

### Referências
- Guia `01_Aulas/Lab0_Montagem_no_Portal_Guia.md`: Etapas 2 a 12, "O que fica diferente do Bicep", "Antes do primeiro `deploy.ps1`" e o Troubleshooting
- `docs/adr/0008-portal-primeiro-com-bicep-como-plano-b.md`

### Transição
Casa de pé: o smoke test passou. Agora a mesma jornada, à mão, no Portal, comigo no papel do ERP.

---

## Slide 13 — Ver a Saldaro funcionando

**Tempo previsto:** 10 min · **Mostrar:** `apim-saldaro-<sufixo>` → APIs → `saldaro` → `POST /faturas` → Test (header `x-cliente-id` = `cliente-a`; Request body em Raw com a fatura `DEMO-001`); o `GET /faturas/{id}` na mesma aba Test; `sb-saldaro-<sufixo>` → Overview e Entities → Queues (Message Counts, com **Refresh**); `stsaldaro<sufixo>` → Storage browser → Tables (`faturas` e `cobrancas`); `appi-saldaro-<sufixo>` → Investigate → Search (o `faturaId` entre aspas), o End-to-end transaction details do POST e o Application map. **Cuidados de telão:** ao abrir o Logs, **KQL mode** e o toggle **Agent** desligado; em Environment variables, **Show value** só na linha, para não expor os outros valores na tela.

### O que falar
O smoke test prova a jornada por script. Agora a gente faz a mesma jornada à mão, sem script nenhum, e eu faço o papel do ERP. Na aba Test da operação `POST /faturas`, adiciono o header `x-cliente-id` com o valor `cliente-a`; sem ele, o hub responde 400. No Request body, em Raw, vai uma fatura com valor, vencimento e `referenciaErp` `DEMO-001`. Send: volta 201 Created com o `faturaId`, o status `cobranca-emitida` e o `idCobranca`. Reparem: no baseline, o POST é síncrono. O hub chamou o banco simulado dentro da requisição e só respondeu quando o banco devolveu a cobrança. É essa herança que o Incidente 1 conserta.

Agora o `GET /faturas/{id}`, repetido a cada poucos segundos. `cobranca-emitida`: o hub gravou a fatura e o banco registrou a cobrança. `registrada`: o banco mandou o webhook `cobranca.registrada`, assinado, pela API `saldaro-webhooks`, e o hub enfileirou na `sbq-confirmacoes-banco`. `baixada`: veio o `cobranca.paga`, o worker fez a baixa e publicou o `FaturaBaixada` no Event Grid. `notificada`: o Event Grid entregou na `sbq-notificacoes-parceiro` e o hub avisou o ERP. Tudo em menos de um minuto.

**Se perguntarem** "notificação a qual parceiro?": aqui, "parceiro" é o termo guarda-chuva para banco parceiro e ERP, e a notificação ao parceiro vai sempre ao ERP da empresa cliente, pelo `callbackUrl` da fatura, nunca ao banco.

No Service Bus, as duas confirmações entraram e saíram da `sbq-confirmacoes-banco`: o Active volta a 0, mas cliquem em Refresh, porque a página não se atualiza sozinha. A `sbq-faturas-recebidas` está parada; quem passa a usar essa fila é o conserto do Incidente 1. No Storage browser, a tabela `faturas` tem a linha com status, `idCobranca` e `quantidadeBaixas`, e a `cobrancas` tem uma cobrança só. A prova do Incidente 1 é exatamente essa contagem.

No Application Insights, esperem de 2 a 5 minutos de ingestão. Em Search, o `faturaId` entre aspas traz a jornada inteira, de `fatura.recebida` a `fatura.baixada`. Abram o POST: o End-to-end transaction details mostra só a parte síncrona, APIM, hub e banco simulado, porque o webhook sai de uma fila de Storage e o Event Grid inicia operações novas, com outro `operation_Id`. Não é erro: o `faturaId` é a correlação de negócio, e é ele que prova a jornada. Por fim, o Application map: é a arquitetura do slide "O que vamos montar", viva, com chamadas e latência em cada seta. Esse é o "dia de sol"; guardem o print, porque é com ele que vamos comparar quando tudo quebrar.

Reparem no que é ingênuo de propósito: o endpoint é síncrono, a connection string é a `RootManageSharedAccessKey`, a fila de confirmações não tem sessions nem duplicate detection, e não existe alerta nenhum. E uma simplificação que vale o dia todo: os HTTP triggers do kit são anônimos e o APIM não pede subscription, registrada na ADR 0007; em produção, cada empresa cliente chamaria com a sua credencial, e o hub só aceitaria tráfego vindo do APIM.

**Pergunta para a turma:** olhando essa jornada, qual peça vocês acham que vai nos trair primeiro?

### Insights do mundo real
- Um smoke test ponta a ponta com correlação técnica (`operation_Id`) e de negócio (`faturaId`) é a primeira coisa que um time deveria ter antes de qualquer alerta sofisticado.
- O Application map é o diagrama de arquitetura que não envelhece: se uma seta que deveria existir sumiu, alguém mudou o sistema sem avisar.

### Referências
- [Application Insights: Search (antigo Transaction search) e End-to-end transaction details](https://learn.microsoft.com/azure/azure-monitor/app/failures-performance-transactions)
- [Application Map](https://learn.microsoft.com/azure/azure-monitor/app/app-map)
- Guia de montagem, Passo 12.7; `docs/adr/0007-http-triggers-anonymous-e-apim-sem-subscription.md`, seção "O que mudaria em produção"

### Transição
Sexta-feira, 18h. O banco entrou em manutenção.

---

## Slide 14 — Separador: Incidente 1

**Tempo previsto:** 3 min (a história e o terminal pronto: Parte 1 do guia, que tem 5 no roteiro dele) · **Mostrar:** o separador; o terminal da turma com os Passos 1.1 e 1.2 do guia (`.env.local` carregado e `AsyncRequestReply` = `False` no `/saude`).

**Comandos:**

```powershell
$cfg = Get-Content ./scripts/.env.local -Raw | ConvertFrom-StringData
(Invoke-RestMethod "$($cfg.SALDARO_HUB_URL)/saude").toggles | Format-List
```

### O que falar
Incidente 1: "O timeout de sexta às 18h". A partir de agora vocês estão de plantão.

Sexta-feira, 18h02. O banco parceiro avisou, numa caixa de e-mail que ninguém monitorava, que entraria em janela de manutenção, e passou a responder em 40, 60, às vezes 90 segundos. O `POST /faturas` da Saldaro era síncrono: só respondia ao ERP depois que o banco devolvia a cobrança. Às 19h10, quando o banco voltou ao normal, umas 800 faturas dos ERPs tinham virado duas ou três faturas no hub, cada uma com a sua cobrança no banco, e o suporte passou a noite atendendo pagadores com dois boletos.

Enquanto eu conto, preparem o terminal, na raiz do repositório: carreguem o `scripts/.env.local` numa variável `$cfg` e perguntem ao hub, no endpoint `/saude`, em que estado ele está. O toggle `AsyncRequestReply` tem que aparecer `False`: é o baseline do Lab 0. Se aparecer `True`, alguém já consertou; o guia diz como voltar.

Duas regras para os três incidentes. Uma: aqueçam antes de cada rajada, e sempre depois de um `deploy.ps1`. O APIM Consumption e o hub acordam devagar: no primeiro deploy, a primeira chamada levou uns 2 segundos, e as seguintes, uns 130 ms. Uma sonda que responde 404, como a do Passo 5.1 (`GET {SALDARO_APIM_URL}/faturas/aquecimento/status`), ou o próprio smoke test resolve. Duas: o plano B de cada incidente é o `deploy.ps1` com o estado pronto, mas quem montou à mão, como eu acabei de fazer, só o usa depois do `limpar.ps1`: o script não roda por cima de um ambiente montado no Portal.

### Transição
Sintomas.

---

## Slide 15 — Sintomas (Incidente 1)

**Tempo previsto:** 9 min (sintomas e hipóteses: Partes 2 e 3 do guia, 15 no roteiro dele; para caber, o professor faz o Passo 3.1 sozinho na tela, como o guia manda se o tempo apertar) · **Mostrar:** terminal com `./scripts/erp-simulado.ps1 -Faturas 1 -MaxTentativas 2 -Caos "latencia=45"` e o relatório (1 fatura, 2 tentativas, `504|504` e nenhum `faturaId` devolvido); `stsaldaro<sufixo>` → Storage browser → Tables → `faturas` (duas linhas com a mesma `referenciaErp` e `faturaId` diferentes); `appi-saldaro-<sufixo>` → Logs, numa aba própria do navegador (toggle **Agent** desligado → **Discard**; **KQL mode**), com `kql/faturas-por-referencia-erp.kql` e a consulta de `requests` e `dependencies` (APIM com 504 aos ~30 s, `PostFatura` com ~45 s e `resultCode` 499, um `PostCobranca` por tentativa); `apim-saldaro-<sufixo>` → APIs → `saldaro` → Design → Backend (`<forward-request timeout="30" />` e o `on-error` que devolve o 504).

**Comandos:**

```powershell
./scripts/erp-simulado.ps1 -Faturas 1 -MaxTentativas 2 -Caos "latencia=45"
Get-Content ./kql/faturas-por-referencia-erp.kql -Raw | Set-Clipboard   # cole no Logs do appi-saldaro-<sufixo>
```

```kusto
union requests, dependencies
| where timestamp > ago(30m)
| where name contains "fatura" or name contains "cobranca"
| project timestamp, itemType, quem = cloud_RoleName, name, resultCode,
          segundos = round(duration / 1000.0, 1)
| order by timestamp asc
```

### O que falar
Vamos reproduzir a sexta-feira: o ERP simulado manda uma fatura só, e o botão de caos `latencia=45` faz o banco demorar 45 segundos para responder. O `-MaxTentativas 2` deixa a reprodução previsível: uma tentativa e um reenvio, cada um cortado aos 30 segundos, e o relatório sai em pouco mais de um minuto. Rodem junto comigo e não cancelem: o mais importante acontece enquanto vocês esperam.

Aos 30 segundos o terminal mostra um 504. Quem cortou não foi o Functions nem o banco: foi o APIM. A política da API tem `forward-request timeout="30"`, e um `on-error` do kit transforma esse corte num 504 sempre igual, com o corpo `{"erro":"o gateway esperou 30 s e desistiu"}`. O ERP recebe um erro de gateway e faz o que todo cliente bem-comportado faz: espera um pouco e reenvia. Só que o trabalho do outro lado não parou junto: o banco já tinha gravado a cobrança antes de começar a esperar, e o hub, ingênuo de propósito, não desiste quando o ERP desiste. O `PostFatura` segue esperando o banco e, aos 45 segundos, recebe a resposta do banco, grava a cobrança na fatura e tenta responder 201, só que ninguém mais está ouvindo. No Application Insights, a consulta de `requests` e `dependencies` mostra as linhas do APIM com 504 em uns 30 segundos, o `PostFatura` com uns 45 e `resultCode` 499, que quer dizer "o cliente fechou a conexão antes da resposta" (o cliente, aqui, é o APIM), e um `PostCobranca` no banco por tentativa. O APIM desistiu; o hub terminou o trabalho. Se as linhas ainda não apareceram, é a ingestão da telemetria, que leva de 2 a 5 minutos; voltamos nela no fim do slide.

Agora o estrago. O reenvio não carrega nada que diga "esta fatura já foi enviada", então o hub cria outra fatura, com outro `faturaId`, e o banco, que faz o que muita API de cobrança faz, registra outra cobrança antes de responder. No Storage browser, a tabela `faturas` tem duas linhas com a mesma `referenciaErp`, o número da fatura no ERP. Abram o Logs do Application Insights numa aba só dele do navegador e deixem aberta até o fim do incidente: se ele abrir num chat, o Observability Agent, desliguem o toggle Agent, cliquem em Discard e troquem Simple mode por KQL mode. No ensaio, sair do Logs com consulta não salva travou a aba. Rodem o `kql/faturas-por-referencia-erp.kql`: a `referenciaErp` da reprodução aparece com duas faturas e duas cobranças. Cada `faturaId`, sozinho, tem uma cobrança só no banco (`GET /cobrancas/{faturaId}` devolve `quantidade = 1`); a duplicata é de negócio, e por isso a contagem é por `referenciaErp`. O pagador vai receber dois boletos da mesma fatura.

Na Saldaro 1.0, a mesma sexta teve um segundo sintoma: o worker único de polling lia a tabela de cobranças a cada poucos segundos e não dava conta da fila que se formou no banco relacional. Guardem os dois sintomas, porque o conserto ataca os dois de uma vez.

E se a gente só aumentasse o timeout? Mostro a política no Portal: `apim-saldaro-<sufixo>`, APIs, Saldaro Hub - faturas (a API `saldaro`), Design, Backend. Subir os 30 segundos não conserta nada: o ERP desiste aos 35 de qualquer jeito, e o reenvio continua criando fatura e cobrança novas; e no Consumption nem dá para subir, porque o tier tem teto de 30 s por requisição, como vimos no slide 10. E os 230 segundos? É o teto da plataforma: nenhuma resposta HTTP de Function passa disso, qualquer que seja o `functionTimeout`, por causa do idle timeout do balanceador do Azure. Vale saber, mas não foi o que derrubou a Saldaro. Na vida real quase sempre existe alguém na frente que corta antes: um gateway, um proxy, o próprio cliente. Ficar abaixo de 230 segundos não salva ninguém se o APIM corta aos 30.

**Pergunta para a turma:** o que o ERP deveria fazer quando não recebe resposta? E como ele saberia se o primeiro POST foi processado ou não?

### Insights do mundo real
- Timeout em cascata: o cliente, o gateway e o backend têm cada um o seu relógio, e quem tem o menor decide. Mapeie os três antes de culpar o parceiro.
- Registrar antes de responder é o comportamento certo para quem recebe a chamada; o erro é chamar uma operação que não é idempotente, sem chave de idempotência e com retry automático.
- Um 504 não diz que a operação falhou; diz que ninguém sabe. É esse "não sei" que transforma retry em duplicata.
- O 499 é a outra metade do 504: o gateway desistiu, mas o backend seguiu e terminou o trabalho. Olhe os dois lados da mesma chamada antes de concluir que "deu erro".

### Referências
- [Azure Functions: limite de 230 s para resposta HTTP](https://learn.microsoft.com/azure/azure-functions/functions-scale#function-app-timeout-duration)
- [API Management: política `forward-request` (atributo `timeout`)](https://learn.microsoft.com/azure/api-management/forward-request-policy)
- [API Management: erros predefinidos (`Timeout` no `forward-request`)](https://learn.microsoft.com/azure/api-management/api-management-error-handling-policies)

### Transição
Hipóteses confirmadas: o cliente não sabe se deu certo, e o banco não perdoa reenvio. Vamos ao padrão que resolve as duas coisas.

---

## Slide 16 — O padrão: request-reply assíncrono

**Tempo previsto:** 6 min (o conserto: Parte 4 do guia, 10 no roteiro dele; o código se lê enquanto o app reinicia) · **Mostrar:** VS Code em `03_Aplicações/Saldaro.Hub` (`Functions/PostFatura.cs` e `Aplicacao/ServicoDeFaturas.cs` com os dois caminhos de `Cenario:AsyncRequestReply`, `GetFaturaStatus`, `EmitirCobranca`; procure `CONCEITO:`); Portal → `func-saldaro-hub-<sufixo>` → Settings → Environment variables → App settings → `Cenario__AsyncRequestReply` = `true` → Apply → Apply → Confirm (aviso **Save changes**; para ver um valor, **Show value** só na linha, nunca o **Show values** da barra); terminal com o laço do Passo 4.3 perguntando ao `/saude` até `AsyncRequestReply` = `True`; `infra/parameters/cenario-2-pronto.bicepparam` como a versão declarada do mesmo conserto.

### O que falar
A saída é parar de prometer uma resposta que depende do banco. O `POST /faturas` passa a fazer só o que ele controla: valida, grava a fatura como `recebida`, coloca uma mensagem na `sbq-faturas-recebidas` e responde 202 Accepted em milissegundos. Junto com o 202 vão dois headers: `Location`, apontando para `/faturas/{id}/status` pela URL pública do APIM, e `Retry-After: 5`, que diz ao cliente quando vale a pena perguntar de novo.

O endpoint de status é barato de propósito: um point read na tabela `faturas`. Enquanto a fatura não baixou, ele responde 200 com `{ faturaId, status }`. Quando ela chega a `baixada` ou `notificada`, responde 303 See Other para `/faturas/{id}`. Por que 303 e não 302? Porque o 303 manda o cliente fazer GET no recurso, seja qual for o método original; com 302, há cliente que repete o POST no redirecionamento, e voltamos à duplicata.

Terceira peça: `Idempotency-Key`. O ERP gera uma chave por fatura e reusa a mesma em todo reenvio. O hub faz um insert condicional na tabela `idempotencia`, com o cliente na partition key e a chave na row key: se a chave já existe, devolve o mesmo `faturaId` e não enfileira de novo. É isso que responde à pergunta do slide anterior: o cliente não consegue distinguir uma resposta perdida de um pedido que nunca chegou, então o servidor precisa tornar o reenvio inofensivo.

Por último, a fila. A `sbq-faturas-recebidas` absorve a rajada do dia 10, e o `EmitirCobranca` chama o banco no ritmo que ele aguenta: é o queue-based load leveling. Se o banco devolver 503, a Function lança exceção e o Service Bus reentrega; ninguém fica segurando conexão HTTP aberta. É também a resposta ao worker de polling do legado: quem drena agora é um trigger que escala com a fila.

Vamos ao código. No VS Code, o `PostFatura` tem os dois caminhos lado a lado, escolhidos por `Cenario:AsyncRequestReply`, e não há nada para escrever nem para publicar: o conserto é ligar o toggle de cenário. Confiram que o ERP simulado da reprodução terminou, abram o `func-saldaro-hub-<sufixo>` no Portal, Settings, Environment variables, e troquem `Cenario__AsyncRequestReply` para `true`: o Value vem mascarado, apaguem e digitem. Para conferir um valor, usem o Show value da linha, nunca o Show values da barra: ele mostra todos os segredos do app de uma vez, e na minha tela isso seria segredo projetado. Dois Apply e um Confirm, no aviso Save changes: mudar uma app setting reinicia o app, e o toggle passa a valer de 5 a 35 segundos depois do Confirm (no ensaio pelo Portal, 19; no primeiro deploy, 34). Enquanto ele volta, a gente lê o `GetFaturaStatus` e o `EmitirCobranca` juntos, e o laço do guia pergunta ao `/saude` até o toggle aparecer `True`. Reparem que isso é drift: o Portal agora diverge do Bicep do Lab 0. A versão declarada do mesmo conserto é o `cenario-2-pronto.bicepparam`, e quem travar sobe direto esse estado com `./scripts/deploy.ps1 -Cenario 2 -Pronto`, o plano B (quem montou à mão, depois do `limpar.ps1`): uns 6 a 7 minutos, porque ele também republica os apps, contra uns 2 pelo Portal.

**Pergunta para a turma:** se o ERP fizer polling a cada segundo em vez de respeitar o `Retry-After`, o que acontece com o APIM no dia 10? Que mecanismo vocês usariam para se defender?

**Se perguntarem:** "Se a fila tem duplicate detection, para que a `Idempotency-Key`?" A duplicate detection só descarta o mesmo `MessageId` dentro da janela e não devolve nada; o ERP precisa receber de volta o mesmo `faturaId`, e quem devolve é a tabela `idempotencia` (pesquisa de dúvidas, pergunta 8).

**Se perguntarem:** "Por que mudar no Portal, se o kit tem Bicep?" O Portal ensina onde cada peça mora; o Bicep recria tudo igual. Cada clique é drift até o `.bicepparam` dizer o mesmo, e o `deploy.ps1` fica como plano B (ADR 0008; pesquisa de dúvidas, pergunta 17).

### Insights do mundo real
- 202 sem `Idempotency-Key` só muda o lugar da duplicata: o reenvio do ERP vira duas mensagens na fila em vez de duas chamadas ao banco.
- O endpoint de status vira o mais chamado do sistema; ele precisa ser um point read, nunca uma consulta ao banco parceiro.
- A chave de idempotência tem prazo: guarde-a pelo menos pelo tempo máximo de retry do cliente e documente esse prazo no contrato da API.
- Tirar o ERP do caminho não tira o banco: o `EmitirCobranca` ainda chama o parceiro com entrega at-least-once. Em produção, mande o `faturaId` como chave de idempotência também para o banco.
- Toggle de cenário por app setting é configuração viva: mudar reinicia o app e encerra execuções em andamento, e o que mudou no Portal é drift até o Bicep dizer o mesmo.

### Referências
- [Asynchronous Request-Reply pattern](https://learn.microsoft.com/azure/architecture/patterns/asynchronous-request-reply)
- [Queue-Based Load Leveling pattern](https://learn.microsoft.com/azure/architecture/patterns/queue-based-load-leveling)
- [Request-Reply (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReply.html)
- [Correlation Identifier (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CorrelationIdentifier.html)

### Transição
Toggle ligado. Agora a prova: o mesmo banco lento, e o ERP sem perceber.

---

## Slide 17 — Prova (Incidente 1)

**Tempo previsto:** 12 min (a prova e o post-mortem de 3 min: Partes 5 e 6 do guia, que com as lições dão 20 no roteiro dele; para caber, o Passo 5.6 vira leitura para casa e as lições cabem numa frase; não pule os Passos 5.3 e 5.4) · **Mostrar:** terminal com o POST do Passo 5.1 do guia (banco a 60 s, cronômetro no cliente) e o polling do status com `curl.exe` (Passo 5.2); `./scripts/erp-simulado.ps1 -Faturas 1000 -Paralelismo 20 -IdempotencyKey` e o relatório; Portal com o Overview da `sbq-faturas-recebidas` enchendo e esvaziando (**Message Counts**; clique em **Refresh**, porque o Overview não se atualiza sozinho); `appi-saldaro-<sufixo>` → Logs (a aba do slide 15, em **KQL mode**) com `kql/faturas-por-referencia-erp.kql` depois que a fila drenar; o reenvio com a mesma `Idempotency-Key` (Passo 5.5).

**Comandos:**

```powershell
# Passo 5.1 do guia: POST manual com latencia=60 e Idempotency-Key, medido com Stopwatch (define $corpo, $cabecalhos, $statusUrl e $faturaId)
# Passo 5.2: o status sem seguir o redirect (o curl.exe não segue sem -L)
curl.exe -s -w " | HTTP %{http_code} %{redirect_url}" $statusUrl
./scripts/erp-simulado.ps1 -Faturas 1000 -Paralelismo 20 -IdempotencyKey
Get-Content ./kql/faturas-por-referencia-erp.kql -Raw | Set-Clipboard   # Passo 5.4, depois que a fila drenar
# Passo 5.5: o mesmo corpo com a mesma chave devolve o mesmo faturaId
(Invoke-RestMethod -Method Post -Uri "$($cfg.SALDARO_APIM_URL)/faturas" -ContentType 'application/json' -Body $corpo -Headers $cabecalhos).faturaId
```

### O que falar
Cinco checagens, os Passos 5.1 a 5.5 do guia, uma de cada vez, e cada um de vocês confere no próprio ambiente.

Primeira: o banco continua lento, agora com `latencia=60`, e o POST responde 202 em menos de meio segundo com o app aquecido, medido no cronômetro do próprio cliente, com `Location` e `Retry-After: 5`. A primeira chamada depois do reinício vem fria: no primeiro deploy, levou 2,2 segundos, com o hub escalando do zero e abrindo a conexão com o Service Bus. Rodem o POST de novo: aquecido, ficou entre uns 90 e 450 ms. O critério vale para o sistema aquecido. O banco vai demorar um minuto, mas isso virou problema do `EmitirCobranca`, não do ERP.

Segunda: consultem o status com o `curl.exe`, que não segue redirecionamento: 200 com o estado atual enquanto a fatura anda e, quando ela baixa, 303 com o `Location` do recurso. O 303 chega uns 75 a 90 segundos depois do POST, e não aos 60: depois de responder, o banco ainda espera uns 5 segundos para "pagar", o webhook atravessa a fila do banco e a do hub, e o polling só pergunta de 5 em 5 segundos. É a resposta à pergunta do guia.

Terceira: a rajada. Mil faturas com paralelismo 20 e `-IdempotencyKey`, uma chave por fatura, reusada se algum POST precisar de reenvio. O relatório tem que mostrar mil 202 e zero 5xx. No primeiro deploy, foram mil 202, zero 5xx e zero 429 em 47 segundos, com p50 de 220 ms, p95 de 639 ms e o pior POST em 8,4 segundos. Deixem o Overview da `sbq-faturas-recebidas` aberto no Portal enquanto ela enche e esvazia: esse morro no gráfico de mensagens ativas é o load leveling acontecendo na frente de vocês. As filas drenaram em uns 2 minutos desde o início da rajada, sem nada na DLQ e sem esbarrar na cota do Flex.

Quarta: nenhuma duplicata de negócio. Quando a fila drenar, rodem de novo o `kql/faturas-por-referencia-erp.kql`, com o período restrito aos minutos da rajada: vazio. A consulta só lista `referenciaErp` com mais de uma fatura ou cobrança; sem a linha `where faturas > 1 or cobrancas > 1`, cada referência da rajada aparece com uma e uma. A da reprodução, se entrar no período, continua com duas: é o histórico do incidente. A checagem complementar do guia vai direto ao banco, com os `faturaId` do CSV em `scripts/saida/`: `quantidade = 1` nos mil.

Quinta: reenviem o mesmo POST com a mesma `Idempotency-Key`, seguindo o passo do guia, e comparem. Volta o mesmo `faturaId`, nada novo entra na fila e o banco continua com uma cobrança para ele.

Para fechar, lembrem do Durable da Disciplina 04: o `statusQueryGetUri` é este mesmo padrão pronto, com uma diferença de dialeto. Ele devolve 202 enquanto a orquestração roda e 200 quando acaba, em vez do nosso 200 com status e 303. As duas formas são válidas; o que importa é o cliente saber qual contrato está seguindo.

Três minutos de post-mortem, cinco linhas: o que aconteceu, impacto, causa raiz, o que consertamos, o que impede de repetir. Dica para a causa raiz: não escrevam "o banco ficou lento". O banco vai ficar lento de novo; a pergunta é por que isso virou cobrança duplicada. Fecho com as lições do guia: não segurar a conexão esperando um parceiro lento, a chave de idempotência é do cliente, e idempotência é de ponta a ponta, porque o `EmitirCobranca` também entrega pelo menos uma vez.

**Pergunta para a turma:** qual das cinco checagens vocês colocariam num alerta de produção, e com que limite?

**Se perguntarem:** "Se o Durable dá o 202 de graça, por que fazer à mão?" É o mesmo padrão; à mão, cada peça aparece, e com uma etapa só a fila e o status bastam, com uma URL de status só de leitura. O Durable vence com várias etapas, timers, aprovação humana e compensação (pesquisa de dúvidas, pergunta 18).

### Insights do mundo real
- Critério de prova bom é numérico e reproduzível: "202 em menos de 500 ms com o banco a 60 s" vale mais que "ficou mais rápido".
- Meça percentis, não a média nem a melhor chamada: na rajada do primeiro deploy, p50 de 220 ms, p95 de 639 ms e máximo de 8,4 s. A chamada isolada e o p95 da rajada contam histórias diferentes, e as duas importam.
- Rajada sem 5xx no APIM não prova que o backend aguentou; prova que o desacoplamento funcionou. A fila drenando até zero e uma fatura por `referenciaErp` são a outra metade da prova.
- Escolha um dialeto do padrão (o do Durable ou o 200/303) e documente no contrato da API: ERP de parceiro segue o que está escrito, não o que você imaginou.

### Referências
- [Durable Functions: async operation tracking (`statusQueryGetUri`)](https://learn.microsoft.com/azure/durable-task/durable-functions/durable-functions-http-features)
- [Asynchronous Request-Reply pattern](https://learn.microsoft.com/azure/architecture/patterns/asynchronous-request-reply)

### Transição
Pausa de 10 minutos. Na volta, dia 10, 9h30.

---

## Slide 18 — Separador: Incidente 2

**Tempo previsto:** 1 min · **Mostrar:** o separador. Na volta da pausa, confiram que a turma está no ponto de partida, o estado pronto do Incidente 1; quem chegou agora roda `./scripts/deploy.ps1 -Cenario 3 -Sufixo <seu sufixo>`.

### O que falar
Incidente 2: "O webhook que chegou duas vezes, e antes da hora". É o mais longo do dia e o mais importante: 45 minutos, do slide 18 ao 22.

### Transição
Sintomas.

---

## Slide 19 — Sintomas (Incidente 2)

**Tempo previsto:** 11 min (a história, os sintomas e as hipóteses: Partes 1 a 3 do guia, 18 no roteiro dele; o professor reproduz e mostra, e quem tem o ambiente repete junto) · **Mostrar:** terminal com `./scripts/erp-simulado.ps1 -Faturas 20 -Caos "duplicar=3;embaralhar=true"`; `appi-saldaro-<sufixo>` → Logs (aba própria, toggle **Agent** desligado, **KQL mode**) com `kql/baixa-duplicada.kql` filtrado pelo início da execução (e a fatura de estudo tirada dali); `GET {SALDARO_APIM_URL}/faturas/{id}` (status e `quantidadeBaixas`); `stsaldaro<sufixo>` → Storage browser → Tables → `faturas` filtrada por `status` = `registrada` (as desbaixadas: 2 das 20 no primeiro deploy); o POST forjado sem assinatura em `{SALDARO_APIM_URL}/webhooks/banco` respondendo 202; as consultas de hipóteses do guia (cópias por `idEvento`, linha do tempo da fatura, intervalo entre as baixas) e o `host.json` do hub.

**Comandos:**

```powershell
./scripts/erp-simulado.ps1 -Faturas 20 -Caos "duplicar=3;embaralhar=true"
$cfg = Get-Content ./scripts/.env.local -Raw | ConvertFrom-StringData
$faturaId = '<faturaId de estudo, tirado do baixa-duplicada.kql>'
Invoke-RestMethod "$($cfg.SALDARO_APIM_URL)/faturas/$faturaId" | Format-List
$forjado = [ordered]@{ idEvento = [guid]::NewGuid().ToString(); tipo = 'cobranca.paga'; faturaId = $faturaId; idCobranca = 'COB-00000000'; valor = 1.00; ocorridoEm = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'); simulaTransiente = $false; transienteAte = $null } | ConvertTo-Json
Invoke-WebRequest -Method Post -Uri "$($cfg.SALDARO_APIM_URL)/webhooks/banco" -ContentType 'application/json' -Body $forjado -SkipHttpErrorCheck | Select-Object StatusCode
```

### O que falar
Dia 10, 9h30, pico de vencimentos. O banco teve um timeout do lado dele e, quando voltou, repetiu de uma vez 1.200 confirmações: parte chegou em várias cópias, parte fora de ordem. E, no meio da confusão, alguém mandou uma confirmação que o banco nunca enviou.

O ambiente de vocês já está no ponto de partida: é o estado pronto do Incidente 1. Quem tem só uma diferença pequena, como o toggle do Incidente 1 desligado, corrige no Portal, pelo Passo 0.3 do guia. Quem chegou agora roda `./scripts/deploy.ps1 -Cenario 3 -Sufixo <seu sufixo>`, de preferência na pausa: são uns 6 minutos num ambiente que já existe e 8 a 10 num novo, e depois o aquecimento. Vinte faturas com `duplicar=3;embaralhar=true`: o banco manda cada evento três vezes, sempre com o mesmo `idEvento`, e inverte a ordem, com o "paga" em 1 segundo e o "registrada" uns 8 segundos depois. Os 1.200 webhooks da história viram 120 aqui: 20 faturas, 2 eventos, 3 cópias. O ERP fez tudo certo, mandou cada fatura uma vez e recebeu 202; o estrago acontece entre o banco e o hub.

Esperem um minuto pela telemetria e rodem o `baixa-duplicada.kql` com o filtro do início da execução: perto de 20 linhas, cada uma com três `fatura.baixada`. Peguem um `faturaId` dali; é a fatura de estudo. Consultem pela API: o mais provável é `notificada`, com `quantidadeBaixas` maior que 1, em geral 3. Um pagamento, três baixas e três avisos ao ERP. E a desordem? Ela não aparece em toda fatura, porque o status final depende de quem grava por último: o "registrada" atrasado ou a notificação, que no baseline marca `notificada` por cima de qualquer status. No primeiro deploy, 2 das 20 terminaram `registrada`, com `baixadaEm` preenchido: pagas, notificadas e depois "desbaixadas" pelo evento que chegou atrasado. Achem as de vocês no Storage browser, filtrando a tabela `faturas` por `status`. Olhem também o `quantidadeBaixas`. Se ele mostrar menos baixas do que o KQL mostrou, melhor ainda: vocês acabaram de ver um lost update, duas execuções lendo o mesmo valor e gravando uma por cima da outra. No primeiro deploy apareceu um: três `fatura.baixada` no KQL e `quantidadeBaixas = 2` na tabela.

Por que em paralelo? O `host.json` do hub tem `maxConcurrentCalls = 4`: uma instância processa até quatro mensagens ao mesmo tempo, e as três cópias do "paga" correm juntas. É o mesmo efeito de dois consumidores disputando a fila, sem precisar escalar nada.

Agora o pior. Mandem um POST forjado para a fatura de estudo, sem assinatura nenhuma, direto em `/saldaro/webhooks/banco`, dizendo que alguém pagou um real. Resposta: 202. Uns 25 segundos depois, o `quantidadeBaixas` subiu mais um, de 3 para 4 no primeiro deploy, e o ERP do cliente recebeu mais um aviso de pagamento. O hub aceitou um pagamento que o banco nunca confirmou. Na Saldaro 1.0, a "assinatura" era um segredo fixo num header; na 2.0, nem isso. E nada barrou o remetente antes do hub: no kit, o webhook é `Anonymous` e a API `saldaro-webhooks` do APIM não pede subscription, uma simplificação de laboratório da ADR 0007; em produção, o APIM só deixaria passar o banco, por IP ou por certificado, e no próximo slide eu volto nisso.

Antes de consertar, as hipóteses, com as consultas do guia: o ERP não duplicou nada, porque o banco tem uma cobrança por fatura; cada `idEvento` chegou três vezes; o "registrada" chegou por último e desfez a baixa (onde a notificação gravou antes dele, a fatura ficou `registrada`); as baixas da mesma fatura caíram a milissegundos uma da outra; e o webhook aceita qualquer remetente. Cada linha do `baixa-duplicada.kql` é uma notificação a mais no ERP de um cliente.

**Pergunta para a turma:** desses três sintomas (duplicata, desordem e forjada), qual vocês resolveriam primeiro? E qual dá para resolver só com configuração, sem código?

**Se perguntarem:** "Se a fila é FIFO, por que a fatura desbaixou?" FIFO é ordem de chegada, não de negócio: o banco mandou o "paga" antes do "registrada", e é nessa ordem que eles saem. A sessão garante um consumidor por fatura; a ordem de negócio é a máquina de estados, no slide 21 (pesquisa de dúvidas, pergunta 7).

### Insights do mundo real
- Webhook de parceiro é at-least-once por definição: quando não recebe a resposta a tempo, o parceiro repete a confirmação, e ele nunca sabe se você processou.
- Ordem de chegada não é ordem de negócio. Um evento "registrada" atrasado é tão real quanto um duplicado, e o código precisa decidir o que fazer com ele.
- Lost update é silencioso: o contador fica certo na maioria das vezes, e o erro só aparece na conciliação, dias depois.
- Corrida que só aparece às vezes é a mais cara: 2 faturas desbaixadas em 20 parecem ruído até a conciliação provar que é defeito.

### Referências
- [Idempotent Receiver (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html)
- [Competing Consumers pattern](https://learn.microsoft.com/azure/architecture/patterns/competing-consumers)
- [Service Bus no Functions: `maxConcurrentCalls` e `maxConcurrentSessions` no `host.json`](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus)

### Transição
Primeira camada: a assinatura.

---

## Slide 20 — Assinatura que vale: HMAC do corpo

**Tempo previsto:** 7 min (a camada da assinatura: Parte 4 do guia, 12 no roteiro dele) · **Mostrar:** VS Code com a validação de assinatura do `Saldaro.Hub` (`Dominio/AssinaturaWebhook.cs`, lógica pura, chamada pelo `Functions/WebhookBanco.cs` antes de qualquer `JsonDocument`); Portal → `func-saldaro-hub-<sufixo>` → Settings → Environment variables → `Cenario__WebhookHmac = true` (**Show value** só na linha, nunca o **Show values** da barra); terminal repetindo o POST forjado (agora 401) e `./scripts/smoke-test.ps1` (o webhook assinado do banco continua passando).

### O que falar
Segredo fixo num header não é assinatura. Ele prova só que quem mandou conhecia o segredo em algum momento: o segredo aparece em log, em proxy, em print de suporte, e não protege o corpo. Quem tem o header copia, troca o valor ou o `faturaId` e manda de novo quantas vezes quiser.

A assinatura de verdade amarra o segredo ao conteúdo e ao momento. O banco calcula um HMAC-SHA256 da string `"{timestamp}.{corpo}"`, com o corpo exatamente como foi enviado, e manda dois headers: `X-Saldaro-Timestamp`, em segundos Unix, e `X-Saldaro-Signature: sha256=` seguido do hex. O hub recalcula com o mesmo `WebhookSecret` e compara. Mudou um byte do corpo, a assinatura não bate; repetiu a mesma requisição meia hora depois, o timestamp está fora da janela de 300 segundos.

Três detalhes derrubam implementações reais. Um: validar antes de desserializar. Se o código faz parse do JSON e serializa de novo para assinar, a ordem dos campos ou um espaço muda e nenhuma assinatura confere; por isso o `WebhookBanco` lê o corpo bruto. Dois: comparar com `CryptographicOperations.FixedTimeEquals`, que leva o mesmo tempo qualquer que seja o conteúdo. Um `==` comum para no primeiro byte diferente e vaza, pelo tempo de resposta, quantos bytes o atacante já acertou. Três: assinatura inválida é 401 e nada entra na fila. Não existe "aceita e marca como suspeita".

Vamos ver funcionando. No Portal, abram o `func-saldaro-hub-<sufixo>`, Settings, Environment variables, e mudem `Cenario__WebhookHmac` para `true`. É o caminho da aula, e é drift: no próximo slide vocês levam a mesma linha para o arquivo de parâmetros do Bicep, para o arquivo dizer o que está no ar; sem ela, o próximo `deploy.ps1` deste estado desligaria o HMAC em silêncio. Esperem o app reiniciar, de 5 a 35 segundos (no ensaio, 13), confiram no `/saude` e mandem o POST forjado de novo: 401, e a fatura de estudo não muda. Agora rodem o smoke test: o webhook do banco, que sempre assinou, continua passando.

E onde mora o segredo? Hoje, numa app setting em texto. No guia de leitura 3.5 ele vai para o Key Vault como referência, e a rotação deixa de exigir redeploy.

E quem pode chamar o webhook? No kit, qualquer um: a Function é `Anonymous` e a API `saldaro-webhooks` do APIM não pede subscription. É simplificação de laboratório, registrada na ADR 0007, e por isso aqui a assinatura é a única defesa. Em produção, ela continua e ganha companhia: o APIM só aceita os IPs do banco (`ip-filter`) ou exige o certificado dele (mTLS), conforme o contrato com o parceiro, e o backend recusa quem não vier pelo APIM.

**Pergunta para a turma:** por que o timestamp entra no conteúdo assinado, e não só num header separado?

**Se perguntarem:** "Por que HMAC no webhook, e não mTLS, OAuth ou lista de IPs?" Cada um responde uma pergunta: o HMAC prova que o corpo veio de quem tem o segredo; o mTLS, quem abriu a conexão; a lista de IPs, quem consegue chegar; o OAuth, quem chama, com um token que expira. Em produção, HMAC mais IPs ou mTLS, conforme o contrato com o banco (ADR 0007; pesquisa de dúvidas, pergunta 19).

### Insights do mundo real
- Rotação de segredo de webhook sem janela dupla derruba o parceiro: durante a troca, aceite o segredo atual e o anterior, e aposente o antigo com data marcada.
- Com Key Vault reference sem versão, o app passa a usar a versão nova em até 24 horas, ou na hora, se a configuração do app mudar. Planeje a rotação com isso em mente.
- Logar o corpo do webhook para "depurar a assinatura" costuma vazar dados do pagador. Logue `idEvento`, timestamp e o resultado da validação.

### Referências
- [`HMACSHA256` (.NET)](https://learn.microsoft.com/dotnet/api/system.security.cryptography.hmacsha256)
- [`CryptographicOperations.FixedTimeEquals`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals)
- [Key Vault references em App Service e Functions](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)
- [Securing Azure Functions: proteção de endpoints HTTP](https://learn.microsoft.com/azure/azure-functions/security-concepts)
- `docs/adr/0007-http-triggers-anonymous-e-apim-sem-subscription.md`, seção "O que mudaria em produção"

### Transição
Segunda e terceira camadas: o broker e o consumidor.

---

## Slide 21 — Uma vez só e na ordem certa

**Tempo previsto:** 18 min (as camadas do broker e do consumidor: Partes 5 e 6 do guia, 30 no roteiro dele; em ritmo de demonstração, e quem ficar para trás vai para o plano B) · **Mostrar:** Portal → `sb-saldaro-<sufixo>` → Entities → Queues → `sbq-confirmacoes-banco` → Overview → bloco Essentials (Sessions e Duplicate detection em "Disabled", sem como ligar; o Settings → Properties virou uma página genérica de metadados do ARM) e o erro do `az servicebus queue update --enable-session true` (`SubCode=40000`); os contadores da fila em zero (Overview → **Message Counts**) e o Delete no Overview da fila (**Delete queue** → **Yes**); Queues → **+ Queue**, o painel de criação com o nome `sbq-confirmacoes-banco`, **Max delivery count** `10`, **Message time to live** no máximo (`10675199` dias, `2` h, `48` min, `5` s; o painel vem com 14 dias), **Lock duration** 1 minuto, **Enable dead lettering on message expiration** marcado, **Enable partitioning** desmarcado e as duas caixas do dia, **Enable duplicate detection** (janela de 10 min) e **Enable sessions**, marcadas (Passo 5.5 do guia); o Overview da fila nova, com Sessions e Duplicate detection fora de "Disabled"; VS Code com `infra/parameters/cenario-2-pronto.bicepparam` (as três linhas do Passo 5.4) ao lado do `cenario-3-pronto.bicepparam`, `infra/modules/service-bus.bicep` (`requiresSession`, `requiresDuplicateDetection`), os triggers `ProcessarConfirmacaoSemSessao` e `ProcessarConfirmacaoComSessao` (`IsSessionsEnabled = true`) e o `host.json`; terminal com o `az servicebus queue show` do Passo 5.6 do guia (`True`, `True`, `PT10M`, `10` e `PT1M`); o erro `non-sessionful` no Logs, depois de mandar uma fatura (com a fila vazia, a consulta volta vazia) e com a fila em **Active** 2; Portal → `func-saldaro-hub-<sufixo>` → Environment variables para trocar o consumidor (os dois app settings `AzureWebJobs.ProcessarConfirmacao*.Disabled`; a grade corta os nomes, confira o **Name** no painel) e ligar `Cenario__Inbox` (**Show value** só na linha, nunca o **Show values** da barra). Plano B, só para quem travar: `./scripts/deploy.ps1 -Cenario 3 -Sufixo <sufixo> -SemPublicar` (o Bicep recria a fila com o `.bicepparam` do Passo 5.4) ou `-Cenario 3 -Pronto`.

**Comandos:**

```powershell
# Passo 5.1 do guia ($rg e $sb vêm do Passo 0.1): a API recusa ligar sessions numa fila existente (SubCode=40000)
az servicebus queue update -g $rg --namespace-name $sb -n sbq-confirmacoes-banco --enable-session true
# Passo 5.2: drenar antes de apagar (activeMessageCount e deadLetterMessageCount em 0)
az servicebus queue show -g $rg --namespace-name $sb -n sbq-confirmacoes-banco --query countDetails -o jsonc
# Passo 5.6: a fila recriada no Portal (+ Queue), conferida pela API
az servicebus queue show -g $rg --namespace-name $sb -n sbq-confirmacoes-banco `
    --query "{sessions:requiresSession, dupDetection:requiresDuplicateDetection, janela:duplicateDetectionHistoryTimeWindow, maxDelivery:maxDeliveryCount, lock:lockDuration}" -o table
# Plano B, só para quem travar: o Bicep recria a fila com o .bicepparam do Passo 5.4 (um aviso amarelo de consumidor sem sessão é esperado)
./scripts/deploy.ps1 -Cenario 3 -Sufixo $cfg.SALDARO_SUFIXO -SemPublicar
```

### O que falar
Com a assinatura, sobrou a confirmação repetida: o banco mandando de novo um evento que ele mesmo assinou. Para isso são três camadas, e cada uma pega o que a anterior deixa passar.

Primeira, no broker. O `WebhookBanco` já envia a confirmação com `MessageId = idEvento`. Com duplicate detection ligada, o Service Bus guarda os `MessageId` dos últimos 10 minutos; um envio repetido é aceito, o produtor recebe sucesso, e a cópia é descartada na hora. Reparem que a janela de 10 minutos é maior que a tolerância de 5 minutos do timestamp: uma requisição capturada e repetida cai no 401, e uma confirmação repetida pelo próprio banco dentro da janela cai no broker.

Segunda, a sessão. Com `SessionId = faturaId`, quem pega uma sessão tem um lock exclusivo sobre todas as mensagens daquela fatura. O worker continua com concorrência alta, até oito sessões ao mesmo tempo, mas cada fatura é processada por um consumidor de cada vez, na ordem de chegada. Cuidado com o que isso promete: ordem de chegada não é ordem de negócio. O "paga" que chegou antes do "registrada" continua chegando antes.

Terceira, o consumidor. A tabela `inbox` guarda `faturaId` e `idEvento`: se o evento já foi processado, a mensagem é completada sem efeito. Ela pega o que o broker deixa passar, como uma confirmação repetida depois da janela ou a reentrega de uma mensagem cuja Function caiu entre gravar a baixa e completar. E a máquina de estados fecha a conta: `cobranca.registrada` depois de `baixada` é ignorada, e a gravação usa ETag, então quem perde a corrida relê e reavalia em vez de gravar por cima.

Agora tentem ligar sessions na fila que existe. No Portal, abram o Overview da `sbq-confirmacoes-banco`: no bloco Essentials, Sessions e Duplicate detection aparecem como "Disabled", e não há onde ligar. Não procurem em Settings → Properties: essa página virou uma lista genérica, gerada dos metadados do ARM, com Id, Name, Type, Tags e Location. Pelo CLI, a API responde com todas as letras: `The value for the RequiresSession property of an existing Queue cannot be changed.` Sessions e duplicate detection só se definem na criação da fila. O caminho do lab, todo no Portal: drenar, conferindo que as ativas e a DLQ estão em zero, na seção Message Counts do Overview, e apagar a fila, no Delete do Overview, com Yes no aviso Delete queue. Antes de criar de novo, abram o `cenario-2-pronto.bicepparam`, que é o arquivo do estado em que vocês estão, e deixem três linhas em `true`: `webhookHmac`, o que vocês ligaram no Portal no slide anterior, `filaComSessions` e `filaDupDetection`. É esse arquivo que registra o que vocês vão criar à mão: sem essas linhas, o próximo deploy pararia na propriedade imutável ou desligaria o HMAC em silêncio.

Agora a fila nova: Queues, + Queue. O nome é exatamente `sbq-confirmacoes-banco`, e os valores são os que o Bicep declara: Max delivery count 10, Message time to live no máximo, 10675199 dias, 2 horas, 48 minutos e 5 segundos, porque o painel sugere 14 dias e o Bicep deixa ilimitado, Lock duration 1 minuto, Enable dead lettering on message expiration marcado, Enable partitioning desmarcado, porque ele também só se escolhe na criação, e as duas caixas do dia, Enable duplicate detection, com a janela de 10 minutos, e Enable sessions. Enquanto a turma preenche, eu explico cada caixa: são justamente as duas que vocês não conseguiram mudar há dois minutos. Esqueceu uma? Não tem volta: apaga e cria de novo, uns dois minutos a mais. Confiram com o `az servicebus queue show` do Passo 5.6: `True`, `True`, `PT10M`, `10` e `PT1M`; se o lock ou o max delivery vierem com o padrão do painel, acertem no (change) da seção Settings, logo abaixo de Essentials. E reparem: nada reiniciou o hub. Vocês só criaram uma fila. Mesmo assim, o Azure e o último deployment agora discordam: é drift, e quem quiser ver roda o `check-drift.ps1`; o arquivo que vocês editaram garante que o próximo deploy confirme a fila, em vez de desfazer o conserto.

Quem travou tem dois planos B. O `./scripts/deploy.ps1 -Cenario 3 -Sufixo <seu sufixo> -SemPublicar` recria a fila pelo Bicep com esse mesmo arquivo, em uns 3 minutos e meio com a fila já apagada; no começo, um aviso amarelo de consumidor sem sessão é esperado, e quem esquecer de apagar a fila vê o script parar em menos de um minuto, com a mensagem de propriedade IMUTÁVEL. E o `./scripts/deploy.ps1 -Cenario 3 -Pronto` entrega as três camadas de uma vez e recria a fila sozinho, avisando no terminal.

Olhem o `host.json`: `maxConcurrentCalls = 4` vale para o trigger sem sessão, `maxConcurrentSessions = 8` para o trigger com sessão, e o `sessionIdleTimeout` de 10 segundos faz cada sessão ociosa liberar o worker logo, para a fila drenar ao vivo. É por isso que existem dois triggers e só um fica ligado: um receptor sem sessão numa fila com sessões simplesmente não funciona.

Fila nova, consumidor velho: o `ProcessarConfirmacaoSemSessao` continua ligado, e o host registra que uma entidade com sessions não aceita receptor sem sessão, mas só quando chega mensagem: com a fila vazia, ele nem tenta receber, e a consulta volta vazia. Por isso eu mando uma fatura: as duas confirmações ficam paradas na fila, Active 2 no Overview depois do Refresh, e uns 2 minutos depois o erro aparece no Logs, umas dez vezes por segundo. Não deixem o consumidor errado ligado muito tempo. Troquem o consumidor no Portal, com os dois app settings do toggle `workerComSessao`, sempre opostos: `AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled = true` e `AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled = false`; a grade corta os nomes, então confiram o Name no painel antes de mudar o Value. O app reinicia, de 5 a 35 segundos (no ensaio, 7), e as duas mensagens paradas saem da fila. Aqueçam antes de rodar, e quem veio pelo plano B aquece de qualquer jeito, porque o deploy reaplicou o APIM. Rodem as 20 faturas de novo: a duplicata some, mas a desordem não. O "registrada" atrasado ainda desbaixa, e, como no baseline, as faturas em que ele grava por último terminam `registrada`. A sessão preservou a ordem de chegada, e o banco mandou fora de ordem; regra de negócio mora no consumidor. Liguem `Cenario__Inbox` no Portal e levem `workerComSessao` e `inbox` para o mesmo `.bicepparam`: agora ele tem os mesmos valores do `cenario-3-pronto`, que é de onde o Incidente 3 parte.

**Pergunta para a turma:** se o duplicate detection já descarta a confirmação repetida, para que a inbox? Deem um cenário em que só ela salva.

**Se perguntarem:** "Se a fila é FIFO, por que precisa de sessão? E partição não resolve a ordem?" Sem sessão, com quatro chamadas em paralelo, a mensagem 1 pode terminar depois da 3; a sessão dá a um consumidor o lock exclusivo do `faturaId`. Partição é throughput e disponibilidade: sem chave, ela espalha as mensagens, e a ordem se perde (pesquisa de dúvidas, pergunta 7).

**Se perguntarem:** "Se a fila tem duplicate detection, para que inbox e ETag?" A detecção só olha o `MessageId`, só dentro da janela e só no envio: a reentrega da mesma mensagem e a confirmação repetida depois da janela passam. Quem impede a baixa dupla é a gravação condicional com ETag; a inbox é atalho e auditoria (pesquisa de dúvidas, pergunta 8).

### Insights do mundo real
- Propriedade imutável é decisão de desenho, não de operação: escolha sessions e duplicate detection ao criar a fila, e trate a mudança como uma versão nova do canal.
- Conserto feito no Portal durante um incidente é drift até o código dizer o mesmo: leve a mudança para o `.bicepparam` antes de encerrar o incidente, senão o próximo deploy para na propriedade imutável ou desfaz o conserto.
- O Portal muda de rótulo; a API, não. Mesmo criando a fila no Portal, confira a propriedade pelo `az servicebus queue show` ou pelo Bicep: o primeiro deploy do kit achou as propriedades da fila no Overview, e não em Settings → Properties, onde o roteiro esperava.
- Duplicate detection custa throughput: todo `MessageId` novo é comparado com a janela inteira. Janela pequena e consumidor idempotente rendem mais que janela de dias.
- Sessão por fatura dá paralelismo seguro: mil faturas em paralelo, cada uma em ordem. O risco é a sessão "quente", uma única chave com volume desproporcional, que vira gargalo.
- Separe identidade de transporte e identidade de negócio: o `MessageId` serve ao broker; o `idEvento` no corpo serve à inbox e sobrevive às cópias com `MessageId` novo, como as do retry agendado e do reprocessamento.

### Referências
- [Message sessions](https://learn.microsoft.com/azure/service-bus-messaging/message-sessions)
- [Duplicate detection](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection)
- [Enable message sessions (só na criação da fila)](https://learn.microsoft.com/azure/service-bus-messaging/enable-message-sessions)
- [Criar uma fila pelo Portal (+ Queue)](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-quickstart-portal)
- [Resequencer (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/Resequencer.html)
- [`TableClient.UpdateEntityAsync` com ETag (412 Precondition Failed)](https://learn.microsoft.com/dotnet/api/azure.data.tables.tableclient.updateentityasync)

### Transição
Prova.

---

## Slide 22 — Prova (Incidente 2)

**Tempo previsto:** 8 min (a prova e o post-mortem de 3 min: Partes 7 e 8 do guia, 14 no roteiro dele) · **Mostrar:** terminal com `./scripts/erp-simulado.ps1 -Faturas 20 -Caos "duplicar=3;embaralhar=true"`, o POST forjado no meio da tempestade (401) e o laço do guia até a fila zerar; `appi-saldaro-<sufixo>` → Logs (**KQL mode**) com `kql/baixa-duplicada.kql` vazio, os `traces` da `ProcessarConfirmacaoComSessao` (em `customDimensions`, `prop__SessionId`, `prop__SequenceNumber` e `prop__DeliveryCount`) e a contagem de `requests` (`WebhookBanco` × `ProcessarConfirmacaoComSessao`); `az storage entity query` nas tabelas `faturas` e `inbox` (ou o Storage browser do `stsaldaro<sufixo>`); a fila recriada no Portal.

### O que falar
Seis critérios, os P1 a P6 do guia, na ordem das camadas.

P1: a mesma tempestade, com o POST forjado no meio dela. Ele volta 401, e a fatura de estudo continua com o mesmo `quantidadeBaixas`. Quem quiser ir além pode mandar o mesmo POST com um `X-Saldaro-Signature` inventado: 401 também.

P2 e P3: esperem a fila zerar, mais um minuto de telemetria, e rodem o `baixa-duplicada.kql` desde o início da prova: vazio. Na tabela `faturas`, as 20 faturas aparecem com `quantidadeBaixas = 1` e status `notificada`, mesmo com o "registrada" chegando 8 segundos depois do "paga": a máquina de estados ignorou o evento atrasado. No primeiro deploy, 19 das 20 já estavam `notificada` aos 51 segundos, e a última veio logo depois.

P4: a concorrência do worker não mudou, mas agora ela é por sessão. Nos `traces` da `ProcessarConfirmacaoComSessao`, as linhas de uma fatura trazem em `customDimensions` as chaves com o prefixo `prop__`: `prop__SessionId` igual ao `faturaId`, `prop__SequenceNumber` crescente e `prop__DeliveryCount`. Primeiro o "paga", depois o "registrada", um depois do outro, sem sobreposição. Na consulta, filtrem com `contains`, não com `has`: o `has` procura termos inteiros e não acha `SessionId` dentro de `prop__SessionId`. A linha "Trigger Details" do host também traz o SessionId, no texto da mensagem. Não precisamos de duas instâncias para provar isso: a disputa já acontecia dentro de uma.

P5: uma notificação por fatura. E o broker trabalhando: uns 120 `WebhookBanco` com sucesso, um com 401, o forjado, e só 40 execuções do consumidor, todas sem falha. O Service Bus aceitou os 120 envios e descartou 80 cópias em silêncio. Na `inbox`, duas linhas por fatura, uma por `idEvento`: a do "paga" e a do "registrada".

P6: o clique consertou; o código de infraestrutura garante que o conserto fica. O `.bicepparam` editado tem os cinco toggles do `cenario-3-pronto`, e a fila que vocês criaram no Portal mostra `requiresSession` e `requiresDuplicateDetection` ligados: o arquivo e o Azure dizem o mesmo. Se alguém apagar a fila amanhã, o próximo deploy a recria igual. E o `baixa-duplicada.kql`, que antes listava as 20 faturas, é o critério que eu deixaria como alerta permanente.

Três minutos de post-mortem. Para este, a causa raiz tem pelo menos duas decisões: confiar num webhook sem assinatura e ter um consumidor que não era idempotente. Escrevam as duas.

**Pergunta para a turma:** qual destas camadas vocês considerariam opcional num sistema de baixo volume? E qual nunca?

### Insights do mundo real
- "Exactly once" de broker vale dentro da janela e entre produtor e fila; o processamento continua at-least-once, e só o consumidor idempotente fecha a conta.
- KQL de "baixa duplicada" é um ótimo alerta permanente: zero é o único valor aceitável, e qualquer ocorrência é incidente.
- Num pipeline, mudança em propriedade imutável precisa parar o deploy e pedir uma decisão (drenar, recriar ou versionar o canal), nunca passar em silêncio.

### Referências
- [Idempotent Receiver (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html)
- [Enable duplicate detection (só na criação da fila)](https://learn.microsoft.com/azure/service-bus-messaging/enable-duplicate-detection)

### Transição
Quarta-feira, 14h. O banco parceiro publicou uma atualização da API e mudou o formato do valor.

---

## Slide 23 — Separador: Incidente 3

**Tempo previsto:** 1 min · **Mostrar:** o separador. Confiram que a turma está no ponto de partida, o estado pronto do Incidente 2; quem chegou agora roda `./scripts/deploy.ps1 -Cenario 4 -Sufixo <seu sufixo>`. Para ganhar tempo, a triagem de aquecimento da Parte 1 já pode ser disparada aqui.

### O que falar
Incidente 3: "A mensagem que ninguém conseguia processar".

### Transição
Sintomas.

---

## Slide 24 — Sintomas (Incidente 3)

**Tempo previsto:** 11 min (o incidente, a preparação, os sintomas e as hipóteses: Partes 1 a 3 do guia, 19 no roteiro dele; cada triagem leva uns 2 minutos e meio, então dispare e siga a história) · **Mostrar:** terminal com o `Contar-Fila` e o `./scripts/triagem-dlq.ps1` de aquecimento (Parte 1; uns 2 minutos e meio); as duas rajadas (`-Faturas 10 -Caos "valorComVirgula=true"` e `-Faturas 20 -Caos "falhaTransiente=50"`) e a DLQ chegando a ~32; Portal → `sb-saldaro-<sufixo>` → Queues → `sbq-confirmacoes-banco` → Service Bus Explorer → aba Dead-letter → Peek Mode → Peek from start (o peek da DLQ funciona com a fila exigindo sessão; colunas Delivery Count, Session ID, Dead Letter Reason e Dead Letter Error Description; uma mensagem com `"valor": "1.250,00"` e uma `cobranca.paga` boa com `simulaTransiente = true`, as duas com `MaxDeliveryCountExceeded`); `appi-saldaro-<sufixo>` → Logs (aba própria, toggle **Agent** desligado, **KQL mode**) com as exceções do consumidor e o ritmo das reentregas (Passos 3.1 e 3.2); a triagem só leitura com um grupo só.

**Comandos:**

```powershell
./scripts/erp-simulado.ps1 -Faturas 10 -Caos "valorComVirgula=true"
./scripts/erp-simulado.ps1 -Faturas 20 -Caos "falhaTransiente=50"
./scripts/triagem-dlq.ps1
```

### O que falar
Quarta-feira, 14h. O banco parceiro publicou uma atualização da API e, sem avisar ninguém, passou a mandar o `valor` como texto no formato brasileiro: "1.250,00" em vez de 1250.00. O ERP não tem culpa nenhuma; o contrato mudou do lado de quem confirma o pagamento.

O ambiente de vocês está no ponto de partida certo, o estado pronto do Incidente 2; quem chegou agora roda `./scripts/deploy.ps1 -Cenario 4 -Sufixo <seu sufixo>`, uns 6 minutos, e aquece antes da primeira rajada. No primeiro deploy, sem aquecimento, a rajada de 10 levou 107 segundos, com 6 timeouts do cliente que nem chegaram ao APIM. Antes do incidente, rodem a triagem uma vez: ela compila a ferramenta e confirma que vocês conseguem ler a DLQ. Contem uns 2 minutos e meio, nesta e nas próximas: no primeiro deploy, a primeira levou 149 segundos, e as seguintes, de 134 a 145. Disparem e sigam a história enquanto ela roda.

Agora a primeira rajada, dez faturas com `valorComVirgula=true`: é a carteira que já recebe o formato novo do banco, e as duas confirmações de cada fatura, a "registrada" e a "paga", chegam com o valor em texto. Cada uma é veneno: o worker tenta desserializar, o `valor` não é número, exceção. O hub abandona a mensagem, o Service Bus reentrega na hora, a exceção se repete e, depois da décima entrega, a mensagem vai para a DLQ com o motivo genérico `MaxDeliveryCountExceeded` e uma descrição que não ajuda: "Message could not be consumed after 10 delivery attempts."

Agora a segunda rajada, vinte faturas só com `falhaTransiente=50`: a carteira que ainda recebe o formato antigo e pega a oscilação. Essas mensagens são boas: o valor é número e a assinatura confere. Só que metade dos "paga" chega com uma falha transiente simulada que dura 45 segundos, como um armazenamento instável por menos de um minuto. O worker falha, o Service Bus reentrega imediatamente, e as dez entregas acabam em poucos segundos, muito antes de a falha passar. Por que duas rajadas, e não uma com os dois botões? Porque o worker desserializa antes de olhar a falha transiente: com o valor em texto, nenhuma mensagem boa cairia na DLQ. Resultado: em um ou dois minutos, a DLQ fechou em 32 no primeiro deploy, 20 envenenadas e 12 boas, todas com o mesmo motivo. O número das boas varia de turma para turma, porque o botão sorteia metade dos "paga": contem com umas 10, para mais ou para menos.

Abram o Service Bus Explorer da fila, na aba Dead-letter, e façam o Peek from start. O peek da DLQ funciona mesmo com a fila exigindo sessão, e as colunas já contam metade da história: Delivery Count 10, o Session ID, que é o `faturaId`, e o Dead Letter Reason. Escolham duas mensagens. Uma tem `"valor": "1.250,00"` no corpo; a outra é um pagamento perfeito, com `simulaTransiente = true`. As duas dizem `MaxDeliveryCountExceeded`. Na história foram 3.800 assim; aqui, umas 32 já contam a mesma história.

Antes de mudar qualquer coisa, provem o diagnóstico. As exceções do consumidor mostram duas famílias, pelo tipo que o próprio hub lança: `ConfirmacaoInvalidaException`, a desserialização apontando para `valor`, e `FalhaTransienteSimuladaException`, a falha transiente simulada. Não leiam a contagem como uma exceção por falha: cada tipo aparece em duas linhas, uma sem `cloud_RoleName`, do host, bem mais numerosa, e outra com o nome do hub, do worker, e nenhuma das duas bate com a conta de mensagens vezes entregas. O que prova o diagnóstico são os tipos e as mensagens, não o total; para não somar as duas linhas, filtrem por `cloud_RoleName`. E o ritmo: no primeiro deploy, foram 204 abandonos em uns 2 minutos. A reentrega foi imediata porque o hub chama `AbandonMessageAsync` na falha, e o Service Bus não tem atraso configurável para reentrega. E a triagem só leitura mostra um grupo só, `MaxDeliveryCountExceeded`: agrupar por motivo não ajuda quando o motivo é sempre o mesmo. Quem sabia por que a mensagem falhou era o consumidor, e ele não disse.

E agora? Reprocessar tudo como está devolve o veneno para a DLQ, e uma cópia reprocessada com o mesmo `MessageId`, dentro da janela do duplicate detection, seria descartada em silêncio. Um conserto apressado que lesse o texto só até a vírgula registraria R$ 1,25 onde o banco disse R$ 1.250,00, e baixaria faturas com o valor errado. Descartar tudo perde pagamentos de verdade. Sem motivo, a DLQ não é uma ferramenta: é um depósito.

**Pergunta para a turma:** olhando só para estas duas mensagens, como vocês decidiriam qual reprocessar? O que faltou o worker registrar?

**Se perguntarem:** "Por que não deixar o Service Bus reentregar até a DLQ? O Functions não tem retry policy?" Não para o trigger de Service Bus: quem reentrega é o broker, na hora, gastando o `maxDeliveryCount`, e o tratamento de mensagem envenenada não se configura no Functions. A espera tem de vir da aplicação, e é o conserto do slide 25 (pesquisa de dúvidas, pergunta 9).

### Insights do mundo real
- Mudança de contrato do parceiro é a causa mais comum de mensagem envenenada em integração B2B, e quase nunca vem com aviso. Validação de schema na entrada e versionamento do contrato são a primeira linha.
- Parse sensível a cultura é armadilha clássica: "1.250,00" vale mil duzentos e cinquenta em pt-BR e não é número na cultura invariante. Fixe a cultura no parser e rejeite o que não for número.
- `MaxDeliveryCountExceeded` em massa é sintoma, não diagnóstico: diz que o consumidor desistiu, nunca por quê.
- A DLQ de uma fila com sessions não exige sessão: o peek dela funcionou no Explorer do Portal e funciona no SDK, embora o Learn diga que o Explorer não suporta sessions. Teste o caminho do plantão antes do plantão.

### Referências
- [Dead-letter queues: motivos do sistema e `MaxDeliveryCount`](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)
- [Service Bus Explorer no Portal](https://learn.microsoft.com/azure/service-bus-messaging/explorer)
- [Invalid Message Channel (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/InvalidMessageChannel.html)

### Transição
Classificar antes de agir.

---

## Slide 25 — Classificar, agendar, triar

**Tempo previsto:** 7 min (o conserto: Parte 4 do guia, 12 no roteiro dele) · **Mostrar:** VS Code com a lógica de baixa compartilhada pelos dois triggers (`Aplicacao/ProcessadorDeBaixa.cs`, `Dominio/ClassificadorDeConfirmacao.cs` e `Dominio/PoliticaDeRetry.cs`; procure `CONCEITO:`: classificação do erro, `DeadLetterMessageAsync` com motivo e descrição, cópia agendada com `ScheduledEnqueueTime`); Portal → `func-saldaro-hub-<sufixo>` → Settings → Environment variables → `Cenario__DlqComMotivo` e `Cenario__RetryAgendado` = `true` (Passo 4.2 do guia; **Show value** só na linha, nunca o **Show values** da barra); Portal → `sb-saldaro-<sufixo>` → Entities → Queues → `sbq-confirmacoes-banco` → Overview → seção **Settings**, abaixo de **Essentials** → **(change)** sob **Max delivery count** (de `10` para `3`) → **OK** → **Refresh** (Passo 4.3); a comparação entre `cenario-3-pronto.bicepparam` e `cenario-4-pronto.bicepparam` e a `var filas` de `infra/modules/service-bus.bicep` (a versão declarada do mesmo conserto); o Passo 4.4 do guia: `az servicebus queue show` com `maxDeliveryCount = 3` e lock `PT1M`, e o `Contar-Fila` com a DLQ ainda com as ~32 do incidente. Plano B, só para quem travar: `./scripts/deploy.ps1 -Cenario 4 -Pronto -SemPublicar` com a fila parada (no what-if, a fila aparece como **Deploy**, `!`, sem valores; nada é recriado).

**Comandos:**

```powershell
# Alternativa ao Portal para os dois toggles (Passo 4.2)
az functionapp config appsettings set -g $cfg.SALDARO_RG -n "func-saldaro-hub-$($cfg.SALDARO_SUFIXO)" --settings Cenario__DlqComMotivo=true Cenario__RetryAgendado=true -o none
# Passo 4.3: o mesmo conserto, declarado (três linhas de diferença)
git diff --no-index infra/parameters/cenario-3-pronto.bicepparam infra/parameters/cenario-4-pronto.bicepparam
# Sem o Change na Overview da fila? A mesma mudança pela CLI (@fila e Contar-Fila vêm do Passo 1.1)
az servicebus queue update @fila --max-delivery-count 3 -o none
# Passo 4.4: o Change do Portal, conferido pela API
az servicebus queue show @fila --query "{maxDeliveryCount:maxDeliveryCount, lockDuration:lockDuration}" -o table
Contar-Fila
# Plano B, só para quem travar: toggles e maxDeliveryCount de uma vez, com a fila parada
./scripts/deploy.ps1 -Cenario 4 -Pronto -SemPublicar
```

### O que falar
O conserto começa por uma pergunta que o worker não fazia: este erro passa sozinho ou não?

Erro permanente é o que se repete igual em toda tentativa: schema inválido, regra de negócio violada. Tentar dez vezes não muda nada, só atrasa e esconde. Com `Cenario:DlqComMotivo` ligado, o worker manda a mensagem para a DLQ na primeira entrega, com `DeadLetterMessageAsync(message, deadLetterReason: "SchemaInvalida", deadLetterErrorDescription: ...)`, e a descrição leva a mensagem do erro. Isso só é possível porque os triggers usam `AutoCompleteMessages = false` e recebem `ServiceBusMessageActions`: quem decide o destino da mensagem é o código, não o runtime.

Erro transiente é o que passa com o tempo. Reentregar na hora é inútil, como vimos: dez tentativas em segundos contra uma falha de 45. Com `Cenario:RetryAgendado`, o worker envia uma cópia agendada da mensagem para daqui a 5 segundos vezes 2 elevado a n, com a propriedade `tentativa`, e completa a original. Dez segundos, vinte, quarenta: setenta segundos de fôlego contra uma falha de 45. No primeiro deploy, a segunda tentativa já encontrou a falha resolvida, e a terceira nem foi usada: a primeira entrega chega alguns segundos depois do envio do banco, cada cópia agendada aparece no horário marcado ou um pouco depois, e esses atrasos contam a favor. Se nem a terceira resolver, aí sim vai para a DLQ, com o motivo `TentativasEsgotadas`. Um detalhe que pega muita gente: a cópia sai com `MessageId = {idEvento}-t{n}`. Se saísse com o mesmo `MessageId`, o duplicate detection, que agora está ligado, a descartaria em silêncio, porque mensagem agendada também passa pela detecção.

Vamos aplicar, tudo no Portal. Os toggles: no `func-saldaro-hub-<sufixo>`, Environment variables, `Cenario__DlqComMotivo` e `Cenario__RetryAgendado` em `true`, e o app reinicia, de 5 a 35 segundos (no ensaio, 7). A política da fila: no Overview da `sbq-confirmacoes-banco`, na seção Settings, (change) ao lado de Max delivery count, de 10 para 3, e OK; depois, Refresh, porque o Overview não se atualiza sozinho. Três cliques, menos de um minuto. Essa propriedade, ao contrário de sessions, muda numa fila existente: nada é recriado, e a DLQ do incidente continua lá, esperando a triagem. Confiram com o `az servicebus queue show` do Passo 4.4: 3 e `PT1M`. E o Bicep? Comparem o `cenario-3-pronto` com o `cenario-4-pronto`: são três linhas, os dois toggles e `maxDeliveryCount = 3`. O arquivo do estado pronto já declara o que vocês acabaram de clicar; o último deployment do ambiente, não. É drift de propósito: um `deploy.ps1 -Cenario 4`, o estado inicial, devolveria o 10 e desligaria os toggles sem perguntar, e quem fecha a conta é um deploy do `cenario-4-pronto`. Por que três? Porque agora o permanente sai na primeira entrega e o transiente é agendado; o `maxDeliveryCount` vira rede de segurança para o erro que ninguém previu. Quem travou tem o plano B: `./scripts/deploy.ps1 -Cenario 4 -Pronto -SemPublicar`, com a fila parada, uns 4 minutos no primeiro deploy, que liga os toggles e baixa o `maxDeliveryCount` de uma vez; no what-if, a fila aparece como todo recurso que já existe, com `!` (Deploy), sem dizer o que muda, porque o script usa o formato `ResourceIdOnly`: o 10 virando 3 se confere depois, com o `az servicebus queue show` do Passo 4.4. Para todos, a prova começa pelo aquecimento: o hub acabou de reiniciar, e o plano B ainda reaplicou o APIM.

E o que ficou de antes? A triagem, que vocês executam na prova. O `./scripts/triagem-dlq.ps1` faz peek na DLQ e agrupa por `DeadLetterReason`, com exemplos. Com `-Reprocessar`, a ferramenta recebe cada mensagem da DLQ, corrige o `valor` em texto para número, reprocessa a mensagem, mandando a cópia para a fila com `MessageId = {original}-r{n}`, o mesmo `SessionId` e a propriedade `reprocessadoDe`, e só então completa a mensagem da DLQ. As mensagens boas do lote antigo voltam como estão, porque a falha já passou. E a inbox garante que nada seja baixado duas vezes, porque ela olha o `idEvento` do corpo, não o `MessageId`. Para quando a decisão é jogar fora, existe o `-DescartarMotivo`, e essa decisão é de negócio, nunca do script.

**Pergunta para a turma:** que outros erros do hub vocês classificariam como permanentes? E um 429 do banco parceiro: permanente ou transiente?

**Se perguntarem:** "Por que retry agendado à mão? O Functions não tem retry policy?" As retry policies do Functions valem para Timer, Event Hubs, Kafka e Cosmos DB, não para o Service Bus, e o `clientRetryOptions` do `host.json` só refaz as chamadas ao Service Bus, não a execução. A reentrega do broker é imediata; a espera é uma cópia agendada, com `MessageId` próprio (pesquisa de dúvidas, pergunta 9).

### Insights do mundo real
- Classificar o erro é a decisão mais barata e mais rentável de um consumidor: transforma a DLQ de depósito em fila de trabalho com instruções.
- Política de fila mudada no Portal durante o incidente é legítima, desde que o arquivo diga o mesmo antes do próximo deploy; o `check-drift.ps1` da leitura 3.7 acha o que ficou só no clique.
- Retry agendado não segura lock nem instância: a mensagem original é completada e a cópia espera no broker. Por isso o lock de 1 minuto sobra.
- Reprocessar perde a ordem original da sessão: a mensagem reprocessada ganha novo enqueue time e novo sequence number. É a máquina de estados tolerante que torna isso seguro.
- Ferramenta de reprocesso sempre deixa trilha: o `reprocessadoDe` na cópia reprocessada e o motivo na DLQ permitem responder, semanas depois, quem reprocessou o quê.

### Referências
- [Dead-letter queues: dead-letter pela aplicação e `SubQueue.DeadLetter`](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)
- [Scheduled messages](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing#scheduled-messages)
- [Service Bus trigger no isolated worker: `ServiceBusMessageActions` e `AutoCompleteMessages`](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus-trigger)
- [Retry pattern](https://learn.microsoft.com/azure/architecture/patterns/retry)
- [Dead Letter Channel (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/DeadLetterChannel.html)
- `01_Aulas/Incidente3_DLQ_Guia.md`, Passos 4.2 a 4.4 ("Portal primeiro") e a seção Plano B

### Transição
Prova.

---

## Slide 26 — Prova (Incidente 3)

**Tempo previsto:** 11 min (a prova e o post-mortem de 3 min: Partes 5 e 6 do guia, que com as lições dão 18 no roteiro dele; as três triagens rodam enquanto a conversa segue) · **Mostrar:** terminal com a sonda de aquecimento, as mesmas duas rajadas e o `Contar-Fila` (DLQ de ~32 para ~52); `./scripts/triagem-dlq.ps1` com o grupo novo `SchemaInvalida` (20) e o Service Bus Explorer com uma dessas mensagens (`DeliveryCount` 1); `appi-saldaro-<sufixo>` → Logs (**KQL mode**) com `mensagem.deadletter`, `mensagem.retry.agendado` (tentativas 1 e 2, com 10 e 20 s; a tentativa 3, de 40 s, só se a falha durar mais) e `kql/dlq-por-motivo.kql`; `./scripts/triagem-dlq.ps1 -Reprocessar` (52 reprocessadas no primeiro deploy) e a DLQ em zero (também na métrica `DeadletteredMessages` no Portal); a tabela `faturas` e a consulta final do guia (60 faturas, 60 baixadas, nenhuma baixa dupla); `GET {SALDARO_APIM_URL}/faturas/{id}` da fatura de estudo; `maxDeliveryCount` no Bicep e no Portal.

**Comandos:**

```powershell
# Aquecimento depois do restart do slide 25, obrigatório se o conserto veio pelo deploy.ps1 do plano B (404 é o esperado: a fatura não existe)
1..3 | ForEach-Object { (Invoke-WebRequest "$($cfg.SALDARO_APIM_URL)/faturas/aquecimento/status" -SkipHttpErrorCheck).StatusCode }
./scripts/erp-simulado.ps1 -Faturas 10 -Caos "valorComVirgula=true"
./scripts/erp-simulado.ps1 -Faturas 20 -Caos "falhaTransiente=50"
./scripts/triagem-dlq.ps1
./scripts/triagem-dlq.ps1 -Reprocessar
./scripts/triagem-dlq.ps1
```

### O que falar
Cinco critérios. A prova repete as mesmas duas rajadas: mesma entrada, outro desfecho. Antes, o aquecimento: o hub acabou de reiniciar com os toggles, e quem foi pelo plano B acabou de reaplicar o APIM. No primeiro deploy do kit, logo depois de um `deploy.ps1`, a primeira sonda levou 1,9 segundo e as seguintes, uns 130 ms, e as rajadas saíram limpas, 10 faturas em 5 segundos e 20 em menos de um. Um aviso de relógio: cada triagem leva uns 2 minutos e meio, e a prova tem três; disparem cada uma e sigam a conversa enquanto ela roda.

Um: o veneno sai na primeira entrega. A DLQ ganha 20 mensagens em uns 20 segundos, as duas confirmações de cada uma das 10 faturas (no primeiro deploy, de 32 para 52), e o consumidor executa uma vez por mensagem, sem falha. Na DLQ, cada mensagem nova tem `DeadLetterReason = SchemaInvalida`, `DeliveryCount` 1 e, na descrição, a mensagem do erro dizendo qual campo não converteu. Quem estiver de plantão amanhã sabe o que fazer sem abrir o código.

Dois: os transientes nunca chegam à DLQ. No `Contar-Fila`, as agendadas ficam acima de zero por uns 35 a 50 segundos (no primeiro deploy, pico de 11) e zeram uns 70 segundos depois da rajada. No Application Insights, `mensagem.retry.agendado` mostra as tentativas 1 e 2, com 10 e 20 segundos de atraso, umas 10 mensagens em cada (11 no primeiro deploy; o número varia com o sorteio); logo depois vem a `fatura.baixada` da mesma fatura. A tentativa 3, de 40 segundos, pode nem aparecer: no primeiro deploy, quando ela chegaria, a falha de 45 segundos já tinha passado. A DLQ não cresce, nenhuma `TentativasEsgotadas` aparece e as 20 faturas da rajada baixam.

Três: a triagem conta a história certa. O `triagem-dlq.ps1` mostra dois grupos: `MaxDeliveryCountExceeded`, com as 32 do incidente (ou perto disso), e `SchemaInvalida`, com as 20 da prova, e nenhuma `TentativasEsgotadas`. O `dlq-por-motivo.kql` mostra o mesmo pelo lado do hub, só com os dead-letters que o próprio código fez.

Quatro: depois do `-Reprocessar`, a DLQ está vazia e as faturas corrigidas estão baixadas. No primeiro deploy, o resumo mostrou 52 reprocessadas: 40 com o valor corrigido, as 20 envenenadas do incidente e as 20 da prova, e 12 sem correção, as boas do incidente; nenhuma descartada, nenhuma falha, e a DLQ zerou em cerca de um minuto. O 12 é o número de transientes do sorteio da Parte 2, então o total de cada um pode ser outro, perto de 50. Consultem uma fatura pela API: `baixada` ou `notificada`, com o valor numérico certo. A consulta final do guia fecha a conta: as 60 faturas do incidente e da prova, 60 baixadas, nenhuma com baixa dupla. Se ela ainda não mostrar as 60, é a ingestão: a `fatura.baixada` do reprocesso leva alguns minutos para chegar ao Application Insights, e a tabela `faturas` já mostra as 60 `notificada`, com `quantidadeBaixas = 1`.

Cinco: a política da fila está versionada. `maxDeliveryCount = 3` e o lock de 1 minuto, coerente com um processamento que leva milissegundos e com um retry que não segura lock: no Azure, pelo Change que vocês fizeram no Portal e conferiram com `az servicebus queue show`; no código, declarados no `cenario-4-pronto.bicepparam`. O clique resolveu o incidente; o arquivo garante que o próximo deploy confirme o conserto em vez de desfazê-lo. O que sobra para reentrega imediata é o imprevisto, e três tentativas bastam para distinguir um soluço de um defeito.

Três minutos de post-mortem. Causa raiz candidata: o worker tratava todo erro do mesmo jeito. E uma linha de "o que impede de repetir" que vale ouro: alerta em `DeadletteredMessages` maior que zero, que é exatamente o que o desafio para casa vai pedir.

**Pergunta para a turma:** quem deveria ser avisado quando aparece um `SchemaInvalida`: o time do hub, o banco parceiro ou os dois?

### Insights do mundo real
- DLQ vazia é estado de chegada, não de repouso: sem alerta, ela volta a encher em silêncio no próximo contrato que mudar.
- Avise o parceiro com evidência: um `DeadLetterErrorDescription` com o campo e o valor recusado encurta a conversa de dias para minutos.
- Escreva o runbook da triagem enquanto o incidente está fresco: quem roda `-Reprocessar` às 3h da manhã precisa saber quando não rodar.

### Referências
- [Dead-letter queues: receber da DLQ com `SubQueue.DeadLetter`](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)
- [Dead Letter Channel (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/DeadLetterChannel.html)

### Transição
Três incidentes, três post-mortems. Vamos olhar para eles juntos.

---

## Slide 27 — Post-mortem coletivo

**Tempo previsto:** 10 min · **Mostrar:** o template de cinco linhas (`templates/post-mortem.md`); pedir que duas ou três pessoas leiam o seu.

### O que falar
Cinco linhas, sem culpados: o que aconteceu, impacto, causa raiz, o que consertamos, o que impede de repetir. Vocês escreveram um por incidente; agora vamos ler alguns em voz alta. Não estou procurando o "certo"; estou procurando a causa raiz escrita como decisão de arquitetura, não como erro de pessoa. "O ERP reenviou" não é causa raiz; "a API era síncrona e não tinha chave de idempotência" é. Quem ler o seu ganha perguntas da turma, não correções.

Olhem os padrões que praticamos: Asynchronous Request-Reply, Idempotent Receiver, Resequencer com sessões, Dead Letter Channel com motivo, Retry com backoff e teto. Cada um tem nome, e o nome importa: é o vocabulário que permite discutir arquitetura com precisão num comitê, numa entrevista, num ADR.

E as três heranças do legado que doeram: banco não é fila; segredo em header não é assinatura; DLQ sem motivo é lixo acumulado. Se vocês saírem daqui com essas três frases, o dia valeu.

**Pergunta para a turma:** qual dos três incidentes vocês acham que acontece primeiro num sistema novo? Por quê?

### Insights do mundo real
- Post-mortem sem culpa é cultura, e cultura se treina em sala antes de treinar em produção.
- Nomear o padrão transforma uma correção pontual em conhecimento reutilizável pelo time inteiro.

### Referências
- [Blameless postmortems (Google SRE Book)](https://sre.google/sre-book/postmortem-culture/)

### Transição
O plantão continua em casa.

---

## Slide 28 — Desafio para casa e guias de leitura

**Tempo previsto:** 7 min (a limpeza de 5 min vem logo depois, ainda neste slide) · **Mostrar:** o guia do cenário 3.6 no repositório; a lista dos outros guias; o Portal com o botão "Delete resource group".

**Comandos (alternativa ao Portal):**

```powershell
./scripts/limpar.ps1
./scripts/limpar.ps1 -Sufixo <seu sufixo>   # se o RG já foi apagado pelo Portal: só purga o Key Vault e o APIM, que ficam em soft-delete
```

### O que falar
O desafio para casa é o incidente que não coube hoje e que, na minha experiência, é o mais traiçoeiro: "O agendador que morreu em silêncio". Um timer para, nenhum erro aparece, e ninguém percebe por meses. A solução é um heartbeat emitido ao concluir o trabalho, não ao começar, e um alerta por ausência de sinal; o workbook é opcional. O guia está no repositório com o mesmo ritual: sintomas, hipóteses, conserto, prova. A entrega é um print do alerta disparando e o post-mortem de cinco linhas. Contem com a espera: o alerta dispara entre 15 e 25 minutos depois do último heartbeat. E um aviso que o primeiro deploy real do kit ensinou: durante o deploy do estado pronto, chega um e-mail do Azure pedindo para verificar o endereço do Action Group, com um código que vale 30 minutos. Sem essa verificação, o alerta dispara no Portal, mas nenhum e-mail chega.

Os outros quatro cenários ficam como guias de leitura: rate limit com 429, no limite de 60 chamadas/min por chave de cada cliente, e retry com teto; Managed Identity e Key Vault no lugar de connection string; Bicep, `what-if` e drift; e a decisão Premium ou Standard com um ADR. Façam no ritmo de vocês; cada um cabe numa noite.

**Se perguntarem:** "Por que Service Bus Standard? Um hub de pagamentos não usaria Premium?" Voltem ao slide 6: o Standard tem tudo o que os incidentes usaram, e a ADR 0003 fixa os cinco gatilhos que pedem o Premium. A Leitura 3.8 transforma essa conversa num ADR (pesquisa de dúvidas, pergunta 6).

E agora o passo mais importante do dia: apaguem o `rg-saldaro-lab`. Todos juntos, agora, ao vivo. Service Bus Standard cobra por hora e não dorme: R$ 0,07 por hora. Quem montou à mão confere também se apareceu um `DefaultResourceGroup-CQ`, que o assistente do Function App pode criar fora do lab, e só o apaga se estiver vazio.

### Insights do mundo real
- Alerta por ausência de sinal (dead man's switch) é o alerta mais barato e mais negligenciado de qualquer sistema com agendamento.

### Referências
- [Log search alerts](https://learn.microsoft.com/azure/azure-monitor/alerts/alerts-types#log-alerts)
- Guia de montagem, Limpeza de Recursos

### Transição
Encerramento.

---

## Slide 29 — Encerramento e referências

**Tempo previsto:** 3 min · **Mostrar:** os links do slide no repositório da aula; o Portal com o `rg-saldaro-lab` sendo apagado.

**Comando (alternativa ao Portal):** `./scripts/limpar.ps1`

### O que falar
Obrigado pelo plantão. Os links do slide estão no repositório da aula, junto com os guias, o kit e os templates de post-mortem e de ADR. Dúvidas no canal da turma; eu respondo durante a semana.

Antes de fechar o notebook, confiram no Portal que o `rg-saldaro-lab` está sendo apagado. Quem preferir o terminal tem o `./scripts/limpar.ps1`, que apaga o Resource Group e ainda purga o Key Vault e o APIM, que ficam em soft-delete com o nome reservado, para o sufixo ficar livre na próxima vez. O kit continua no repositório: qualquer cenário sobe de novo com `deploy.ps1 -Cenario N` em poucos minutos (no primeiro deploy real, o Lab 0 levou de 8 a 10 minutos), então refazer um incidente com calma, durante a semana, custa centavos. É o melhor exercício para fixar o dia: escolher um dos três, recriar o estado inicial e quebrar de novo, sem olhar o guia; e, com tempo, o roteiro completo de cada guia, que hoje rodou em ritmo de demonstração.

Duas coisas para levar: primeiro, o ritual, porque é o mesmo de um incidente real; segundo, os nomes dos padrões, porque são o vocabulário que faz vocês serem ouvidos como arquitetos. Até a próxima.

### Referências
- [Cloud Design Patterns](https://learn.microsoft.com/azure/architecture/patterns/)
- [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/)
- [Azure Service Bus documentation](https://learn.microsoft.com/azure/service-bus-messaging/)
- [Azure Functions .NET isolated worker](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [Key Vault references em App Service e Functions](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)
- [Bicep documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
