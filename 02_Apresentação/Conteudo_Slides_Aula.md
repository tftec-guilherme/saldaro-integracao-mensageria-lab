# Conteúdo dos Slides — Aula hands-on: Integração e Mensageria no Azure

> **Pós-Graduação em Arquitetura Cloud Azure | TFTEC + Anhanguera** · **Prof. Guilherme Campos** · **Version-anchor:** Q4-2026
>
> **Modalidade:** 100% ao vivo, 1 encontro de ~4h · **Total:** 29 slides em cinco seções, com a montagem ao vivo no Portal entre os slides 12 e 13 · **Cenário:** Saldaro Pagamentos S.A. (fictícia), ver `docs/research/2026-10-13-briefing-da-aula.md`
>
> Empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência.
>
> Slides são intencionalmente enxutos: toda a profundidade está nos guias de lab e nas `Speaker_Notes_Aula.md`. Formato da pós: bullets concisos com diagramas, não slides cheios de texto.
>
> **Status:** v1.5 (2026-10-15). Este índice segue o deck v19 (`deck/project/`, refeito em 2026-10-14 e já publicado), que é a fonte de verdade: os 29 slides na ordem do `order` do `deck.json`, com o título, os bullets e o visual de cada `slides/<id>.html`. Nas tabelas "Por que este, e não as alternativas" dos slides 4 a 10, cada linha vira um bullet "**Opção:** critério → para a Saldaro". Saíram do deck o apêndice "Perguntas que a turma vai fazer" (FAQ-1 a FAQ-10) e os slides Regras do dia, A Saldaro Pagamentos, O que a Disciplina 04 já nos deu e o separador do Lab 0. Nomes e comportamentos seguem o código, o Bicep e os scripts do kit; a montagem segue `01_Aulas/Lab0_Montagem_no_Portal_Guia.md`; os incidentes seguem os guias de `01_Aulas/`, com o conserto ao vivo no Portal e o `deploy.ps1` como plano B (ADR 0008); os números medidos vêm do primeiro deploy real do kit, em 2026-10-14 (`docs/research/2026-10-14-checklist-primeiro-deploy.md`), e do ensaio dos incidentes pelo Portal. O contrato (`docs/research/2026-10-13-contrato-do-kit.md`) é a referência de nomes.

---

## Estrutura geral

O deck v19 tem 29 slides em cinco seções, as do `deck.json`. A lógica da aula: **primeiro o porquê de cada tecnologia**, uma peça por slide e na ordem em que ela entra na montagem, com as alternativas, a configuração e o custo; depois a conta, a ordem da montagem, a **montagem ao vivo no Portal** e a Saldaro funcionando; e só **no fim os três incidentes**, quebrando o que a turma acabou de ver montar. É o "explica → demonstra → labifica" da Disciplina 04 numa ordem nova: explica o porquê, monta, mostra funcionando e quebra.

| Seção do `deck.json` | Slides | Quantidade |
|---|---|---|
| 1. Abertura: o que a Saldaro precisa resolver e o que vamos montar | 1–3 | 3 |
| 2. Por que cada tecnologia: uma peça por slide, na ordem da montagem, com as alternativas, a configuração e o custo | 4–10 | 7 |
| 3. Quanto custa, a ordem da montagem e a Saldaro funcionando | 11–13 | 3 |
| 4. Quando dá errado: os três incidentes | 14–26 | 13 |
| 5. Fechamento: post-mortem coletivo, desafio para casa, limpeza e referências | 27–29 | 3 |
| **Total** | | **29 slides** |

### Tempo: 4h, com a montagem ao vivo no meio

| Bloco | Slides | Janela no relógio | Soma dos "Tempo previsto" |
|---|---|---|---|
| Abertura | 1–3 | 0:00–0:08 | 8 min |
| Por que cada tecnologia | 4–10 | 0:08–0:24 | 16 min |
| Quanto custa e a ordem da montagem | 11–12 | 0:24–0:30 | 6 min |
| Montagem ao vivo no Portal (sem slide) | entre o 12 e o 13 | 0:30–1:30 | 60 min |
| Ver a Saldaro funcionando | 13 | 1:30–1:40 | 10 min |
| Incidente 1 — "O timeout de sexta às 18h" | 14–17 | 1:40–2:10 | 30 min |
| Pausa | — | 2:10–2:20 | 10 min |
| Incidente 2 — "O webhook que chegou duas vezes (e antes da hora)" | 18–22 | 2:20–3:05 | 45 min |
| Incidente 3 — "A mensagem que ninguém conseguia processar" | 23–26 | 3:05–3:35 | 30 min |
| Fechamento (post-mortem, desafio com a limpeza ao vivo, encerramento) | 27–29 | 3:35–4:00 | 20 min + 5 de limpeza |
| **Total** | **29 slides** | **4h (240 min)** | **240 min**: 165 de slides + 60 de montagem + 10 de pausa + 5 de limpeza, sem folga |

- **A montagem não tem slide.** O slide 12 fecha com "vamos montar"; o professor monta no Portal pelo guia de montagem, das Etapas 2 a 12 (até o Passo 12.5), e o slide 13 é o Passo 12.7. Os 60 min supõem a Etapa 1 pronta antes da aula, o Passo 12.7 contado no slide 13 e a Etapa 13 para casa; na validação do kit, a montagem inteira, sem pausa, levou ~75 min. As metas no relógio e a ordem de corte estão nas notes, no bloco entre os slides 12 e 13.
- **Os incidentes ficaram com 0,6 do tempo do roteiro dos guias** (30, 45 e 30 min, contra 50, 75 e 50), para caber com a montagem: o professor conduz em ritmo de demonstração, quem chegou com o ambiente montado repete junto, e cada guia, completo, fica para a turma refazer em casa, com o estado inicial e o plano B do `deploy.ps1`. Os `<aside>` do deck ainda trazem os tempos dos guias.
- **A pausa fica onde o deck a põe**, depois do Incidente 1 (é a transição do slide 17). Se a turma pedir antes, ela vem logo depois do slide 13, e o total não muda.
- **Sem folga:** quem segura o relógio é a montagem, com metas e ordem de corte, e os incidentes, com o "se o tempo apertar" de cada guia.

---

## Slide 1 — Capa

**Título:** Integração e Mensageria no Azure: um dia de incidente na Saldaro

**Bullets:** nenhum (capa limpa)

**Visual:**
- Selo "MICROSOFT AZURE" no canto superior direito
- Título em duas linhas: **Integração e Mensageria no Azure**, em azul, e "um dia de incidente na Saldaro"
- Subtítulo: Aula hands-on ao vivo · Pós-Graduação em Arquitetura Cloud Azure
- Caixa com quatro linhas e ícones: Pós-Graduação Cloud Azure · Prof. Guilherme Campos · 1 encontro ao vivo de 4 horas · TFTEC + Anhanguera; abaixo dela, "Version-anchor: Q4-2026"
- Aviso de ficção (ADR 0004), em letra pequena no rodapé: "Empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência."
- Imagem de fundo do design system da pós

---

## Slide 2 — O que a Saldaro precisa resolver

**Título:** O que a Saldaro precisa resolver

**Bullets:**
- **Um hub de cobrança B2B:** a jornada de uma fatura, do ERP da empresa cliente ao banco parceiro e de volta
- A jornada: 1. O ERP manda a fatura → 2. O hub registra a cobrança no banco → 3. O banco manda o webhook → 4. O hub dá baixa → 5. O hub avisa o ERP
- **Aguentar rajada:** Service Bus absorve o pico; Functions Flex Consumption escala até 10 instâncias no hub
- **Não perder fatura:** filas do Service Bus guardam a mensagem, com retry e Dead-letter queue
- **Não cobrar duas vezes:** Idempotency-Key com insert condicional no Table Storage; inbox para confirmações repetidas
- **Confiar no webhook do banco:** assinatura HMAC com timestamp, na API `saldaro-webhooks` do APIM
- **Enxergar o sistema:** Application Insights, a jornada inteira da fatura, seguida pelo `faturaId`
- **Custo de lab no Free Trial:** serverless e tiers de entrada, ~R$ 2 a 3 da véspera ao fim da aula

**Visual:** faixa horizontal com os cinco passos da jornada ligados por setas; embaixo, grade 3 × 2 de cards, um por requisito, com a peça que o atende; o card do custo em amarelo

---

## Slide 3 — O que vamos montar

**Título:** O que vamos montar: a Saldaro 2.0 no Azure

**Bullets:**
- Tudo no `rg-saldaro-lab`, à mão, no Portal: o mesmo ambiente que o Bicep do cenário 0 cria
- **Porta única:** APIM, para o ERP e o banco
- **Processamento:** hub e banco no Functions Flex
- **Mensageria:** Service Bus (3 filas) e Event Grid
- **Estado e segredos:** Storage (tabelas) e Key Vault
- **Observação:** Application Insights sobre Log Analytics

**Visual:** "A jornada da cobrança" em seis caixas ligadas por setas: ERP do cliente (`POST /faturas`) → APIM (porta única) → Hub (registra a cobrança) → Banco simulado (webhook de volta) → Service Bus (baixa da fatura) → Event Grid (avisa o parceiro); embaixo, cinco cards, um por papel

---

## Slide 4 — Por que Application Insights e Log Analytics

**Título:** Application Insights: a jornada da fatura num lugar só

**Bullets:**
- **Problema da Saldaro:** enxergar uma fatura que atravessa APIM, Functions, Service Bus e Event Grid, seguindo um só `faturaId`
- **Application Insights workspace-based:** APM, costura a transação distribuída pelo `operation_Id` → escolhido: a jornada inteira no Search, por `faturaId`
- **Log de cada serviço no seu lugar:** mostra um recurso, mas perde a junção pelo `faturaId` → vence para olhar um recurso isolado
- **Um Application Insights por app:** times e ciclos de vida diferentes → vence com times separados; aqui a jornada é uma só
- **Configuração que vamos escolher:** `log-saldaro-<sufixo>` Pay-as-you-go, com retenção de 30 dias; `appi-saldaro-<sufixo>` workspace-based, em Brazil South; hub, banco simulado e APIM no mesmo Application Insights; sampling desligado, porque a prova conta eventos por `faturaId`
- **Custo:** 5 GB/mês grátis por conta de cobrança, e o lab gera megabytes: R$ 0; acima disso, ~US$ 4,60/GB em Brazil South (~R$ 25/GB)

**Visual:** à esquerda, a tabela "Por que este, e não as alternativas" (Opção | Critério que decide | Para a Saldaro), com a linha escolhida em verde; à direita, o card "Configuração que vamos escolher" e o card amarelo "Custo". Os slides 4 a 10 usam o mesmo layout

---

## Slide 5 — Por que Table Storage

**Título:** Table Storage: estado barato, sem cobrar duas vezes

**Bullets:**
- **Problema da Saldaro:** guardar o estado de cada fatura por centavos e nunca cobrar duas vezes, com o insert condicional da Idempotency-Key e o ETag
- **Table Storage:** leitura por chave, insert condicional (409) e ETag (412), por centavos → escolhido: no storage que o Functions já exige
- **Cosmos DB for Table:** latência com SLA, índices secundários, distribuição global → vence com escala alta; o lab não precisa
- **Azure SQL:** transação entre fatura, inbox e outbox; joins e restrições → vence num razão contábil de verdade
- **Configuração que vamos escolher:** `stsaldaro<sufixo>` StorageV2, Standard LRS, Hot; 5 tabelas: `faturas`, `inbox`, `idempotencia`, `conciliacao` e `cobrancas`; sem esquema: o desenho é a PartitionKey e a RowKey
- **Custo:** ~R$ 1/mês a conta inteira, se ficar ligada; no lab, centavos; cobra por GB armazenado e por transação

**Visual:** o layout do slide 4, com três linhas na tabela

---

## Slide 6 — Por que Service Bus

**Título:** Service Bus: fatura e confirmação não se perdem

**Bullets:**
- **Problema da Saldaro:** aguentar a rajada de 1.000 faturas sem perder, nem aplicar duas vezes, uma confirmação de pagamento
- **Service Bus Standard:** comando financeiro, com peek-lock, DLQ com motivo, sessions e duplicate detection → escolhido: absorve a rajada e guarda cada confirmação
- **Storage Queue:** barata e simples; sem ordem e sem DLQ automática → vence em backlog simples e idempotente; fica no banco simulado
- **Event Hubs · Event Grid:** stream com releitura (Hubs); aviso em push, sem ordem (Grid) → vencem em telemetria e em aviso; o Grid leva o `FaturaBaixada`
- **Service Bus Premium:** messaging units dedicadas, Private Endpoint, mensagens de até 100 MB → vence num gatilho da ADR-0003: rede privada, erro 50009 recorrente
- **Configuração que vamos escolher:** namespace `sb-saldaro-<sufixo>`, tier Standard, Brazil South, TLS 1.2; 3 filas: `sbq-faturas-recebidas`, `sbq-confirmacoes-banco` e `sbq-notificacoes-parceiro`; lock de 1 min, `maxDeliveryCount` 10; sessions e duplicate detection desligadas no baseline: são o coração dos Incidentes 2 e 3
- **Custo:** ~R$ 55/mês (US$ 0,0134/h), por hora, com ou sem tráfego: o maior custo do lab (~R$ 60/mês no total); Premium, ~US$ 677/mês por messaging unit

**Visual:** o layout do slide 4, com quatro linhas na tabela

---

## Slide 7 — Por que Key Vault e identidade gerenciada

**Título:** Key Vault e identidade gerenciada: segredo com dono

**Bullets:**
- **Problema da Saldaro:** guardar o segredo do webhook e a chave do banco sem senha espalhada nem credencial expirando às 3h da manhã
- **Key Vault + identidade gerenciada:** segredo com dono, versão e auditoria; acesso por role, sem credencial para girar → escolhido: a Leitura 3.5 migra para cá
- **Segredo no app setting:** criptografado em repouso, mas sem gestão de segredo → vence no que não é segredo, como a connection string do App Insights
- **Connection string:** credencial que vaza, gira e expira → vence quando o cliente não obtém token do Entra ID
- **Configuração que vamos escolher:** Key Vault Standard, permissão por RBAC, soft-delete de 7 dias; baseline com o cofre vazio e connection string, de propósito; na Leitura 3.5, identidade gerenciada do hub e Key Vault references
- **Custo:** ~R$ 0 no lab: US$ 0,03 por 10 mil operações de segredo, sem taxa fixa; identidade gerenciada sem custo extra

**Visual:** o layout do slide 4, com três linhas na tabela

---

## Slide 8 — Por que Event Grid + fila

**Título:** Event Grid + fila: avisar sem acoplar

**Bullets:**
- **Problema da Saldaro:** avisar o ERP do parceiro de cada fatura baixada sem acoplar o hub a ele e sem perder o aviso quando o parceiro cai
- **Event Grid → fila do Service Bus:** push com filtro e vários assinantes; a fila dá peek-lock, reentrega e DLQ → escolhido: parceiro fora não apaga o aviso
- **Hub chama o parceiro direto:** acopla o hub a cada consumidor e à disponibilidade dele → descartado: um novo assinante (BI, conciliação) obrigaria a mudar o hub
- **Event Grid direto na Function:** trigger recomendado pelo Learn, mas aponta para a função já publicada e usa chave → vence em reação leve, sem reentrega pelo consumidor
- **Configuração que vamos escolher:** tópico `evgt-saldaro-faturas-<sufixo>`, Cloud Event Schema v1.0, identidade system-assigned; role Azure Service Bus Data Sender do tópico só na fila `sbq-notificacoes-parceiro`; event subscription `evgs-notificar-parceiro` → Service Bus Queue, com a identidade do tópico; retry padrão (30 tentativas ou 24 h); dead-letter desligado no lab
- **Custo:** R$ 0 no lab (grátis até 100 mil operações por mês); cobra por operação (publicação, entrega e filtro avançado), sem medidor por hora; cabe no Free Trial

**Visual:** o layout do slide 4, com três linhas na tabela

---

## Slide 9 — Por que Functions no Flex Consumption

**Título:** Azure Functions no Flex: escala na rajada, paga pelo uso

**Bullets:**
- **Problema da Saldaro:** aguentar a rajada de 1.000 faturas e, no resto do tempo, voltar a zero e pagar só pelo que executa
- **Functions no Flex Consumption:** código por evento; escala por função e volta a zero → escolhido: hub e banco simulado
- **Logic Apps:** designer, conectores e gente no meio → vence em baixo volume, B2B e aprovação humana
- **Container Apps ou App Service:** controle da imagem; API web contínua → vence com imagem própria, GPU ou plano dedicado
- **Consumption ou Premium:** o Consumption é legado; o Premium paga 1 instância sempre → o Premium vence em tráfego contínuo, sem cold start
- **Configuração que vamos escolher:** Flex Consumption (FC1), um plano por app; .NET 10 isolated, 512 MB por instância; teto por grupo de escala de 10 no hub e 5 no banco (~13 núcleos, dentro dos 15 do Free Trial)
- **Custo:** ~R$ 0 na franquia (100 mil GB-s e 250 mil execuções por mês, por assinatura); no Free Trial, a página de preços não deixa claro se ela vale

**Visual:** o layout do slide 4, com quatro linhas na tabela

---

## Slide 10 — Por que API Management

**Título:** API Management: uma porta única e governada para o hub

**Bullets:**
- **Problema da Saldaro:** o ERP e o banco precisam de um endereço estável, com corte de 30 s e, no 3.1, rate limit por empresa cliente
- **APIM Consumption:** corte, limite e log por chamada → escolhido: sem custo fixo
- **Function direto:** chave de função não autentica o cliente → vence com Function interna, de um só chamador
- **WAF (Application Gateway ou Front Door):** barra ataque web; não governa a API → soma: fica na frente do APIM em produção
- **APIM Standard v2:** cobra por hora; tem VNet e limite por chave → vence com backend em VNet ou chamada acima de 30 s
- **Configuração que vamos escolher:** tier Consumption, em Brazil South; APIs `saldaro` (ERP) e `saldaro-webhooks` (banco); corte do backend aos 30 s, com 504; sem subscription no baseline; rate limit só no 3.1
- **Custo:** 1 milhão de chamadas grátis por mês, por assinatura; acima disso, ~US$ 4,25 por milhão; no lab, R$ 0

**Visual:** o layout do slide 4, com quatro linhas na tabela

---

## Slide 11 — Quanto custa

**Título:** Quanto custa

**Bullets:**
- Preços de lista de Brazil South em outubro de 2026, com ~R$ 5,50/US$; confira na calculadora na data da aula
- **Resource Group** (N/A): gratuito, ligado ou no lab
- **Log Analytics workspace** (Pay-as-you-go, retenção de 30 dias): gratuito até 5 GB; no lab, na franquia
- **Application Insights** (workspace-based): incluído no Log Analytics; no lab, na franquia
- **Storage account** (StorageV2, Standard LRS, Hot): ~R$ 1/mês; no lab, centavos
- **Service Bus e 3 filas** (Standard): **~R$ 55/mês; no lab, ~R$ 0,07 por hora** (a linha em destaque)
- **Key Vault** (Standard, autorização RBAC): ~R$ 0, cobra por operação; no lab, centavos
- **Event Grid topic** (CloudEvents 1.0): gratuito até 100 mil operações; no lab, na franquia
- **Functions, banco e hub** (Flex Consumption FC1, 512 MB): ~R$ 0 na franquia; no lab, na franquia ou no crédito
- **API Management** (Consumption): gratuito até 1 milhão de chamadas; no lab, na franquia
- **Roles** (role assignments): gratuito
- **Total:** ~R$ 60/mês se esquecido ligado × ~R$ 0,20 a 1 no dia e ~R$ 2 a 3 da véspera
- **Regra de ouro: apague o Resource Group ao terminar:** `pwsh ./scripts/limpar.ps1 -Sufixo <seu-sufixo>`

**Visual:** tabela Recurso | SKU | Mensal se ficar ligado | No lab, com a linha do Service Bus em amarelo e a de total em azul; embaixo, a faixa amarela da regra de ouro com o comando do `limpar.ps1`

---

## Slide 12 — A ordem da montagem

**Título:** Treze etapas: cada peça resolve um problema

**Bullets:**
- Tempos do guia de montagem para a primeira vez (~3 h no total); na validação do kit, ~75 min
- As treze etapas: 1. Máquina, Portal e sufixo (~15 min) · 2. Resource Group (~3) · 3. Log Analytics e App Insights (~8) · 4. Storage: tabelas, fila e containers (~12) · 5. Service Bus: namespace e filas (~15) · 6. Key Vault (~5) · 7. Event Grid: tópico e assinatura (~12) · 8. Function Apps no Flex: banco e hub (~35) · 9. Publicar o código (~15) · 10. API Management, com a ativação (~30) · 11. Roles do seu usuário (~8) · 12. `.env.local` e a prova (~15) · 13. Post-mortem e pré-mortem (~5)
- O porquê de cada peça:
  - **Resource Group:** tudo nasce e morre junto
  - **Log Analytics e App Insights:** enxergar antes de quebrar
  - **Storage:** estado barato, com concorrência otimista
  - **Service Bus:** absorver a rajada e garantir entrega
  - **Key Vault:** segredo fora do app setting
  - **Event Grid:** avisar o parceiro sem acoplar
  - **Functions no Flex:** escala por evento, paga por uso
  - **APIM:** porta única e corte de 30 s, não WAF
- **Crie o APIM cedo:** ele leva alguns minutos para ativar; na validação do kit, uns 2 minutos

**Visual:** à esquerda, a trilha numerada das 13 etapas com o tempo de cada uma, a Etapa 10 em amarelo; à direita, "O porquê de cada peça" e o card amarelo "Crie o APIM cedo"

---

## Slide 13 — Ver a Saldaro funcionando

**Título:** Ver a Saldaro funcionando

**Bullets:**
- A jornada de uma fatura, à mão, no Portal: você faz o papel do ERP (~10 min)
- **1. Mande uma fatura pela aba Test do APIM:** API `saldaro` → `POST /faturas` → Test, com o header `x-cliente-id: cliente-a`; volta 201 Created com o `faturaId`
- **2. Acompanhe o status pelo `GET /faturas/{id}`:** `cobranca-emitida` → `registrada` → `baixada` → `notificada`, em menos de um minuto
- **3. Veja as filas no Service Bus:** `sbq-confirmacoes-banco` e `sbq-notificacoes-parceiro`, a mensagem entra e sai; `sbq-faturas-recebidas` fica parada
- **4. Veja as tabelas no Storage browser:** `faturas`, com status, `idCobranca` e `quantidadeBaixas`; `cobrancas`, com uma cobrança só para a fatura
- **5. Veja a jornada no Application Insights:** Investigate → Search pelo `faturaId`, entre aspas; o End-to-end mostra a parte síncrona; Investigate → Application map, a arquitetura viva, com chamadas e latência em cada seta
- **Pelo script:** `./scripts/smoke-test.ps1` faz a mesma jornada e imprime o `faturaId` e o `operation_Id`

**Visual:** cinco passos numerados, um por linha, com os nomes de código em monoespaçado; embaixo, a faixa "Pelo script" com o comando

---

## Slide 14 — Separador: Incidente 1

**Título:** Incidente 1 — "O timeout de sexta às 18h"

**Bullets:**
- Sexta-feira, 18h02: o banco parceiro entra em manutenção
- Sintomas: 504 aos 30 s, o ERP reenvia e a cobrança duplica
- O padrão: 202 Accepted + endereço de status + fila
- Prova: 1.000 faturas em rajada, zero 5xx

**Visual:** separador no estilo da D04, com "01" grande em marca d'água, "INCIDENTE 1" e o nome do incidente em azul; quatro linhas com ícones: a história, os sintomas, o padrão e a prova

---

## Slide 15 — Sintomas (Incidente 1)

**Título:** O banco demorou 45 s e a Saldaro duplicou cobranças

**Bullets:**
- Incidente 1 · Sintomas: o `POST /faturas` síncrono esperava o banco antes de responder
- **504 aos 30 s:** o APIM corta com `{"erro":"o gateway esperou 30 s e desistiu"}`, e o ERP reenvia
- **2 faturas, 2 cobranças:** no `faturas-por-referencia-erp.kql`, a mesma `referenciaErp` com duas faturas e duas cobranças; no banco, uma por `faturaId`
- **O hub espera até ~45 s:** o `PostFatura` termina com 499, porque ninguém mais ouvia; o banco já gravou a cobrança
- Na Saldaro 1.0, o worker de polling não drenava a fila. Teto da plataforma: a resposta HTTP do Functions morre aos 230 s; aqui, o APIM corta antes

**Visual:** à esquerda, três cards vermelhos de sintoma e a nota sobre o legado e os 230 s; à direita, o diagrama de sequência ERP → APIM → Hub → Banco: `POST /faturas`, repassa, cobrança 1; o 504 aos 30 s de volta ao ERP, o reenvio, outra fatura e a cobrança 2 em vermelho; a resposta do banco aos ~45 s e o `PostFatura` com 499, em cinza

---

## Slide 16 — O padrão: request-reply assíncrono

**Título:** 202 Accepted + endereço de status + fila

**Bullets:**
- Incidente 1 · O padrão: Asynchronous Request-Reply com queue-based load leveling
- **202 Accepted:** `Location: …/faturas/{id}/status` (URL pública do APIM) e `Retry-After: 5`
- **Endereço de status:** 200 `{ faturaId, status }` enquanto processa; 303 para `/faturas/{id}` quando `baixada` ou `notificada`
- **Idempotency-Key no POST:** o reenvio devolve o mesmo `faturaId` e não enfileira de novo
- **Fila que absorve a rajada:** a `sbq-faturas-recebidas` nivela a carga; o `EmitirCobranca` chama o banco no ritmo dele

**Visual:** à esquerda, quatro cards (202, status, Idempotency-Key e fila); à direita, o diagrama: ERP → 1. `POST /faturas` (202 · Location · Retry-After: 5) → `sbq-faturas-recebidas` → `EmitirCobranca` → banco parceiro lento (`latencia=60`), "no ritmo do banco"; 2. `GET …/status` (200 enquanto processa; 303 ao baixar) → 303 See Other → 3. `GET /faturas/{id}` (a fatura, já baixada)

---

## Slide 17 — Prova (Incidente 1)

**Título:** Como saber que consertamos

**Bullets:**
- Incidente 1 · O banco continua lento, e o ERP nem percebe
- **1 · 202 em menos de 500 ms:** aquecido, mesmo com o banco a 60 s (`latencia=60`)
- **2 · 200, depois 303:** 200 enquanto processa; 303 para a fatura em 75 a 90 s
- **3 · Zero 5xx na rajada:** 1.000 faturas com `-Paralelismo 20 -IdempotencyKey`, 1.000 × 202 em ~47 s; a fila drena em ~2 min
- **4 · Nenhuma duplicata:** KQL vazio na janela da rajada, 1 fatura e 1 cobrança por `referenciaErp`; no banco, `quantidade = 1` por `faturaId`
- **5 · Mesma chave, mesmo id:** o reenvio com a mesma `Idempotency-Key` devolve o mesmo `faturaId`
- **Comparem com o Durable da D04:** o `statusQueryGetUri` é este padrão pronto, em outro dialeto: 202 → 200 lá; 200 → 303 aqui

**Visual:** grade 2 × 3 de cards, com as cinco checagens e o card amarelo do Durable; à direita, o terminal "PowerShell · a rajada do primeiro deploy": `erp-simulado.ps1 -Faturas 1000 -Paralelismo 20 -IdempotencyKey`, com Faturas 1.000, 202 Accepted 1.000, 5xx 0, 429 0, p50 220 ms, p95 639 ms, máx. 8,4 s e Duração 47 s

---

## Slide 18 — Separador: Incidente 2

**Título:** Incidente 2 — "O webhook que chegou duas vezes (e antes da hora)"

**Bullets:**
- Dia 10, 9h30: 1.200 confirmações repetidas, fora de ordem e uma forjada
- Assinatura que vale: HMAC do corpo, ou 401
- Dup detection + sessions + inbox: uma vez só, na ordem certa
- Prova: 120 webhooks viram 40 processamentos e 1 aviso por fatura

**Visual:** separador com "02" em marca d'água e quatro linhas com ícones

---

## Slide 19 — Sintomas (Incidente 2)

**Título:** Dia 10, 9h30: 1.200 confirmações repetidas pelo banco

**Bullets:**
- Incidente 2 · Sintomas: o banco repetiu de uma vez a última hora de confirmações
- **Duplicados:** a mesma fatura baixada e notificada três vezes, uma por cópia do `idEvento`
- **Fora de ordem:** o "paga" chegou antes do "registrada", e o evento atrasado desbaixou a fatura: no lab, 2 de 20
- **Forjada e aceita:** uma confirmação sem assinatura recebeu 202; o webhook confiava em qualquer POST
- **Por que em paralelo:** `maxConcurrentCalls = 4`, e as cópias da mesma confirmação disputavam a mesma fatura

**Visual:** à esquerda, três cards vermelhos e um azul ("Por que em paralelo"); à direita, "Uma fatura de estudo, no tempo": linha vertical com 0 s (`POST /faturas` → 202 Accepted: uma fatura, uma cobrança no banco, o ERP fez tudo certo), ~1 s (`cobranca.paga` × 3, mesmo `idEvento`: 3 baixas e 3 avisos ao ERP), ~8 s (`cobranca.registrada` × 3, atrasada: onde grava por último, desbaixa) e "depois" (POST forjado, sem assinatura: 202 e, uns 25 s depois, mais uma baixa); no rodapé, "No lab: 20 faturas × 2 eventos × 3 cópias = 120 webhooks"

---

## Slide 20 — Assinatura que vale: HMAC do corpo

**Título:** Segredo no header não é assinatura

**Bullets:**
- Incidente 2 · Camada 1: só o banco confirma pagamento, e a assinatura amarra o segredo ao corpo e ao momento
- Os headers que o banco assina: `X-Saldaro-Timestamp` e `X-Saldaro-Signature: sha256=…`, o HMAC-SHA256 de `"{timestamp}.{corpo bruto}"`, com ±300 s
- **Corpo bruto, antes do JSON, em tempo constante:** compare com `CryptographicOperations.FixedTimeEquals`
- **401, e nada entra na fila:** assinatura inválida, ausente ou fora da janela
- **`WebhookSecret` no Key Vault (guia 3.5):** hoje é app setting; lá, a rotação dispensa redeploy
- **Simplificação de laboratório (ADR 0007):** webhook `Anonymous` e APIM sem subscription, então aqui a assinatura é a única defesa; em produção, some `ip-filter` ou mTLS, e o backend só aceita tráfego do APIM

**Visual:** à esquerda, o bloco de código com os dois headers e o comentário do HMAC, e três cards; à direita, o diagrama: banco parceiro (assina `{timestamp}.{corpo}`) e atacante (POST sem assinatura válida) → hub, `WebhookBanco`, pelo APIM (recalcula o HMAC com o mesmo `WebhookSecret` e compara) → "confere": 202 e enfileira na `sbq-confirmacoes-banco`; "não confere": 401, nada entra na fila; embaixo, a faixa amarela da simplificação de laboratório

---

## Slide 21 — Uma vez só e na ordem certa

**Título:** Dup detection + sessions + inbox

**Bullets:**
- Incidente 2 · Camadas 2 e 3: uma vez só e na ordem certa, no broker e no consumidor
- **Camada 2a · broker, Duplicate detection:** `MessageId = idEvento`, janela de 10 min: a confirmação repetida é descartada (120 envios → 40 mensagens)
- **Camada 2b · sessão, Sessions:** `SessionId = faturaId`, um consumidor por fatura, na ordem de chegada (até 8 sessões em paralelo)
- **Camada 3 · consumidor, Inbox + estados com ETag:** evento já visto completa sem efeito; "registrada" depois da baixa é ignorada (1 baixa e 1 aviso por fatura)
- **Overview → Essentials da fila:** Sessions e Duplicate detection em Disabled; pelo CLI, `az servicebus queue update --enable-session true` volta Bad Request, `SubCode=40000`
- **Imutáveis, Enable sessions e Enable duplicate detection:** só se definem na criação da fila; no lab, drenar (ativas e DLQ em 0), apagar no Overview e recriar no Portal com **+ Queue**, as duas caixas marcadas; o `.bicepparam` registra; plano B: `deploy.ps1 -Cenario 3 -SemPublicar` ou `-Pronto`

**Visual:** três cards em sequência, ligados por setas (broker → sessão → consumidor), cada um com o resultado em roxo; embaixo, o print do Overview → Essentials da `sbq-confirmacoes-banco` no primeiro deploy real (Sessions e Duplicate detection em Disabled), ao lado do terminal "Pelo CLI" com o `SubCode=40000`; a faixa amarela dos imutáveis

---

## Slide 22 — Prova (Incidente 2)

**Título:** Como saber que consertamos

**Bullets:**
- Incidente 2 · P1 a P6, na ordem das camadas: a mesma tempestade, outro desfecho
- **P1 · Forjado → 401:** no meio da tempestade, nada entra na fila e o `quantidadeBaixas` não muda
- **P2 · KQL vazio:** o `baixa-duplicada.kql`, desde o início da prova, volta sem nenhuma linha
- **P3 · 1 baixa, notificada:** as 20 faturas com `quantidadeBaixas = 1`; a "registrada" atrasada não desbaixa
- **P4 · Uma fatura por vez:** `prop__SessionId = faturaId` e `prop__SequenceNumber` crescente, mesmo com concorrência 4
- **P5 · 1 aviso por fatura:** 120 webhooks viram 40 processamentos; o broker descartou 80 cópias
- **P6 · O Bicep descreve o conserto:** `requiresSession`, `requiresDuplicateDetection` e os cinco toggles do `cenario-3-pronto`
- **Post-mortem, 3 min:** a causa raiz tem pelo menos duas decisões, confiar num webhook sem assinatura e ter um consumidor que não era idempotente

**Visual:** grade 2 × 3 com os seis critérios; à direita, o bloco "Logs · kql/baixa-duplicada.kql" (`customEvents`, `fatura.baixada`, contagem por `faturaId` e `clienteId`, `where baixas > 1`) com a faixa verde "Depois do conserto: nenhuma linha" e a nota "Antes: ~20 linhas, cada uma com baixas = 3"; embaixo, o card amarelo do post-mortem

---

## Slide 23 — Separador: Incidente 3

**Título:** Incidente 3 — "A mensagem que ninguém conseguia processar"

**Bullets:**
- Quarta-feira, 14h: o banco passa a mandar o valor como texto
- Classificar o erro: schema inválido vai para a DLQ, com motivo
- Transiente: cópia agendada em 10, 20 e 40 s
- Prova: triagem por motivo, reprocessamento e DLQ em zero

**Visual:** separador com "03" em marca d'água e quatro linhas com ícones

---

## Slide 24 — Sintomas (Incidente 3)

**Título:** "1.250,00" derrubou o worker 3.800 vezes

**Bullets:**
- Incidente 3 · Sintomas: o contrato do banco mudou, e a DLQ encheu sem dizer por quê
- **Valor como texto:** o banco atualizou a API e passou a mandar `valor` como texto, "1.250,00"
- **10 entregas, motivo inútil:** exceção e reentrega até a DLQ, com `MaxDeliveryCountExceeded` e "Message could not be consumed after 10 delivery attempts."
- **Boas na mesma DLQ:** uma falha transiente de 45 s esgotou as entregas em segundos
- **Ninguém sabia o que era o quê:** reprocessar tudo baixaria faturas erradas; descartar tudo perderia pagamentos
- Na história, 3.800; no primeiro deploy, 32: 20 corrigíveis e 12 boas (as boas variam com o sorteio)

**Visual:** à esquerda, três cards vermelhos e um azul; à direita, o print do Service Bus Explorer da `sbq-confirmacoes-banco` no primeiro deploy real, aba Dead-letter com 32 mensagens em Peek Mode (Delivery Count 10, Session ID e `MaxDeliveryCountExceeded` em todas); embaixo dele, dois blocos lado a lado: "Envenenada" (`"valor": "1.250,00"`) e "Boa, com falha transiente" (`"valor": 1250.00`, `"simulaTransiente": true`), as duas com `DeadLetterReason` `MaxDeliveryCountExceeded`

---

## Slide 25 — Classificar, agendar, triar

**Título:** Permanente vai para a DLQ com motivo; transiente espera e tenta de novo

**Bullets:**
- Incidente 3 · O conserto: classificar o erro, agendar o retry e triar a DLQ
- O fluxo do worker: a mensagem chega (peek-lock) → desserializa? Não → DLQ na 1ª entrega, `SchemaInvalida`. Sim → falha transiente? Não → inbox, estados, baixa e Complete. Sim → n ≤ 3 (n = tentativa + 1)? Sim → cópia agendada `{idEvento}-t{n}`, em +10, +20 e +40 s, que volta à fila; não → DLQ, `TentativasEsgotadas`
- `DeadLetterMessageAsync(message, deadLetterReason: "SchemaInvalida", …)` e `ScheduledEnqueueTime = agora + 5 s × 2^n`
- **`triagem-dlq.ps1`:** peek na DLQ, agrupado por `DeadLetterReason`, com exemplos
- **`-Reprocessar`:** corrige o `valor` e reprocessa com `MessageId = {original}-r{n}`, o mesmo `SessionId` e `reprocessadoDe`
- **`maxDeliveryCount`: 10 → 3:** o permanente sai na 1ª entrega e o transiente é agendado; as 3 entregas viram rede de segurança

**Visual:** o fluxograma de decisão do worker, com as três perguntas em amarelo, os três destinos (DLQ com `SchemaInvalida`; baixa e Complete; DLQ com `TentativasEsgotadas`) e a cópia agendada voltando à fila em tracejado; embaixo, o código das duas chamadas; à direita, os cards da triagem e do `-Reprocessar` e o card amarelo do `maxDeliveryCount`

---

## Slide 26 — Prova (Incidente 3)

**Título:** Como saber que consertamos

**Bullets:**
- Incidente 3 · Cinco critérios: as mesmas duas rajadas, outro desfecho
- **1 · Veneno na 1ª entrega:** DLQ com `SchemaInvalida`, `DeliveryCount` 1 e o erro de `valor` na descrição
- **2 · Transiente fora da DLQ:** `mensagem.retry.agendado` com as tentativas 1 e 2, com 10 e 20 s; a 3ª, de 40 s, é folga
- **3 · Triagem por motivo:** a triagem e o `dlq-por-motivo.kql` mostram o grupo do incidente e as 20 `SchemaInvalida`
- **4 · DLQ vazia, faturas baixadas:** o `-Reprocessar` devolve 52 (40 corrigidas + 12 boas); 60 de 60 baixadas, sem baixa dupla
- **5 · A política versionada:** `maxDeliveryCount = 3` ((change) no Portal, sem recriar a fila) e lock de 1 min, declarados no `cenario-4-pronto`
- **Post-mortem, 3 min:** o worker tratava todo erro igual; alerta em `DeadletteredMessages` > 0

**Visual:** grade 2 × 3 com os cinco critérios e o card amarelo do post-mortem; à direita, o painel "DLQ da sbq-confirmacoes-banco": 52 antes do reprocessamento (no primeiro deploy, 32 do incidente + 20 `SchemaInvalida` da prova), uma seta para baixo e 0 depois do `-Reprocessar` ("A DLQ está vazia.", em cerca de um minuto)

---

## Slide 27 — Post-mortem coletivo

**Título:** Cinco linhas, sem culpados

**Bullets:**
- Post-mortem coletivo · um por incidente, escrito pela turma em 3 minutos cada
- O template de cinco linhas (`templates/post-mortem.md`): 1. **O que aconteceu**, o sintoma visto de fora, com hora e duração · 2. **Impacto**, quem foi afetado e quanto, em números, e o que não foi · 3. **Causa raiz**, uma decisão de desenho que faltava, nunca uma pessoa · 4. **O que consertamos**, a mudança e a prova: comando, KQL, antes e depois · 5. **O que impede de repetir**, um mecanismo que funciona sozinho: teste, alerta, Bicep
- **Incidente 1:** banco não é fila: 202 + status + fila, com Idempotency-Key
- **Incidente 2:** segredo não é assinatura: HMAC do corpo, dup detection, sessions e inbox
- **Incidente 3:** DLQ sem motivo é lixo: classificar o erro, agendar o retry, triar
- **Padrões praticados hoje:** Asynchronous Request-Reply, Idempotent Receiver, Resequencer, Dead Letter Channel, Retry com backoff

**Visual:** à esquerda, o template de cinco linhas numeradas numa caixa grande; à direita, três cards, um por incidente (amarelo, roxo e vermelho), cada um com a frase do legado; embaixo, os cinco padrões em etiquetas azuis

---

## Slide 28 — Desafio para casa e guias de leitura

**Título:** O dia de incidente continua

**Bullets:**
- Desafio para casa e guias de leitura, com o mesmo ritual: sintomas, hipóteses, conserto, prova
- **Desafio · ~2h, 3.6 · "O agendador que morreu em silêncio":** heartbeat ao concluir, KQL e alerta por ausência de sinal; entrega: print do alerta disparando + post-mortem
- **Leitura · ~1h30, 3.1 · "O parceiro que não dormia":** rate limit no APIM, 429 e retry com teto
- **Leitura · ~1h45, 3.5 · "O segredo que expirou às 3h":** Managed Identity e Key Vault no lugar de connection string
- **Leitura · ~1h30, 3.7 · "A fila fantasma":** Bicep, what-if e detecção de drift
- **Leitura · ~1h30, 3.8 · "Premium ou Standard?":** custo, throughput e isolamento, com a decisão num ADR
- **Antes de tudo: apague o `rg-saldaro-lab`:** Delete resource group; o Service Bus Standard cobra por hora e não dorme: R$ 0,07/h

**Visual:** grade 3 × 2 de cards: o 3.6 em destaque, com fundo azul e a etiqueta amarela "Desafio · ~2h"; os quatro guias de leitura com o tempo de cada um; e o card vermelho com o botão "Delete resource group"

---

## Slide 29 — Encerramento e referências

**Título:** Obrigado! Referências para continuar

**Bullets:**
- Pós-Graduação em Arquitetura Cloud Azure · TFTEC + Anhanguera
- **Cloud Design Patterns:** Asynchronous Request-Reply, Queue-Based Load Leveling, Retry e Throttling (Microsoft Learn)
- **Enterprise Integration Patterns:** Idempotent Receiver, Resequencer, Dead Letter Channel e Competing Consumers (Messaging Patterns)
- **Azure Service Bus:** sessions, duplicate detection e dead-letter queues (Microsoft Learn)
- **Functions .NET isolated:** o worker isolado e o trigger do Service Bus, com `AutoCompleteMessages` (Microsoft Learn)
- **Key Vault + Managed Identity:** Key Vault references no lugar de segredo em app setting (Microsoft Learn)
- **Bicep:** what-if, parâmetros e a infraestrutura como fonte da verdade (Microsoft Learn)
- **Repositório da aula:** guias, kit, scripts, KQL e os templates de post-mortem e de ADR (link no canal da turma)
- **Canal de dúvidas:** perguntas durante a semana, respondidas por lá (canal da turma)

**Visual:** grade 4 × 2 de cards, um por referência, cada um com ícone, uma linha de descrição e o link (Microsoft Learn ou Messaging Patterns); os dois últimos sem link, com "Link no canal da turma" e "Canal da turma"

---

*v1.0 em 2026-10-13: slides dos três incidentes fechados contra o contrato do kit e validados no `QA_Validacao_Speaker_Notes.md`. v1.1 em 2026-10-14: alinhados ao código e aos guias na integração (prova do Incidente 1 por `referenciaErp`, P1 a P6 do Incidente 2, tempos por slide do roteiro dos guias). v1.2 em 2026-10-14: números do primeiro deploy real (202 aquecido, 303 em 75 a 90 s, rajada de 1.000, 2 de 20 faturas desbaixadas, `prop__SessionId`, 80 cópias descartadas, DLQ de 52 e reprocesso 40 + 12, retries de 10 e 20 s), o Overview → Essentials da fila, o aquecimento depois de cada `deploy.ps1` e o post-mortem de 3 min. v1.3 em 2026-10-14: revisão de código: aviso de ficção na capa (ADR 0004), simplificação de laboratório (ADR 0007), confirmação repetida e reprocessamento no lugar de "reenvio" (glossário do `CONTEXT.md`), as cinco checagens da prova do Incidente 1 e a DLQ do lab de ~50 a 0. v1.4 em 2026-10-14: apêndice de FAQ e "Portal primeiro" (ADR 0008), com o conserto ao vivo no Portal e o `deploy.ps1` como plano B. v1.5 em 2026-10-15: o índice segue o deck v19, com 29 slides em cinco seções: o porquê de cada tecnologia na ordem da montagem (slides 4 a 10), a conta, a ordem e a montagem ao vivo no Portal (11, 12 e o bloco sem slide), a Saldaro funcionando (13) e os incidentes no fim (14 a 26), em 30, 45 e 30 min; saíram o apêndice de FAQ e os slides Regras do dia, A Saldaro Pagamentos, O que a Disciplina 04 já nos deu e o separador do Lab 0. Fonte de verdade: o deck v19, em `deck/project/`; referência canônica de layout: `Disciplina 04 - Integração e Mensageria.pptx`, no Blob da pós.*
