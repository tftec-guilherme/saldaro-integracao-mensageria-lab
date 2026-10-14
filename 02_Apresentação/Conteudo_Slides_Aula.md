# Conteúdo dos Slides — Aula hands-on: Integração e Mensageria no Azure

> **Pós-Graduação em Arquitetura Cloud Azure | TFTEC + Anhanguera** · **Prof. Guilherme Campos** · **Version-anchor:** Q4-2026
>
> **Modalidade:** 100% ao vivo, 1 encontro de ~4h · **Total:** 22 slides de aula + 10 slides de bolso (apêndice de FAQ, fora das 4h) · **Cenário:** Saldaro Pagamentos S.A. (fictícia), ver `docs/research/2026-10-13-briefing-da-aula.md`
>
> Slides são intencionalmente enxutos: toda a profundidade está nos guias de lab e nas `Speaker_Notes_Aula.md`. Formato da pós: bullets concisos com diagramas, não slides cheios de texto. Padrão "explica → demonstra → labifica" da Disciplina 04.
>
> **Status:** v1.4 (2026-10-14). Os 22 slides estão fechados, e o apêndice "Perguntas que a turma vai fazer" (FAQ-1 a FAQ-10) vem depois do slide 22, fora da soma das 4h. Nomes e comportamentos seguem o código, o Bicep e os scripts do kit; a sequência e os comandos dos três incidentes seguem os guias de `01_Aulas/` (conferidos na integração, ticket #13); os tempos, as contagens e os rótulos do Portal seguem o primeiro deploy real do kit no Azure, em 2026-10-14 (roteiro em `docs/research/2026-10-14-checklist-primeiro-deploy.md`). O contrato (`docs/research/2026-10-13-contrato-do-kit.md`) é a referência de nomes. Desde a v1.4, o conserto ao vivo de cada incidente é feito no Portal, e o `deploy.ps1` é o plano B (ADR 0008 e o parágrafo "Portal primeiro" dos guias dos incidentes).

---

## Estrutura geral

| Bloco | Slides | Quantidade | Janela no relógio | Soma dos "Tempo previsto" |
|---|---|---|---|---|
| Abertura (capa, regras do dia, a história, revisão relâmpago) | 1–4 | 4 | 0:00–0:15 (15 min) | 15 min |
| Lab 0 — Fundação | 5–6 | 2 | 0:15–0:30 (15 min) | 13 min |
| Incidente 1 — "O timeout de sexta às 18h" | 7–10 | 4 | 0:30–1:20 (50 min) | 50 min |
| Pausa | — | — | 1:20–1:30 (10 min) | 10 min |
| Incidente 2 — "O webhook que chegou duas vezes" | 11–15 | 5 | 1:30–2:45 (75 min) | 75 min |
| Incidente 3 — "A mensagem que ninguém conseguia processar" | 16–19 | 4 | 2:45–3:35 (50 min) | 50 min |
| Fechamento (post-mortem, desafio, limpeza ao vivo, encerramento) | 20–22 | 3 | 3:35–4:00 (25 min) | 20 min + 5 min de limpeza |
| **Total** | | **22 slides** | **4h (240 min)** | **238 min** (223 de slides + 10 de pausa + 5 de limpeza; 2 min de folga) |
| Apêndice: perguntas que a turma vai fazer (slides de bolso) | FAQ-1 a FAQ-10 | 10 | sob demanda, quando a pergunta surge | fora da soma (1 a 2 min cada) |

Os tempos dos incidentes seguem o roteiro de cada guia (`01_Aulas/Incidente1_Timeout_Guia.md`, `Incidente2_Webhook_Guia.md` e `Incidente3_DLQ_Guia.md`), que ocupa a janela inteira; cada guia diz o que encurtar se o relógio apertar. Os slides de bolso do apêndice não entram nas 4h: o professor abre um deles quando a pergunta surge e volta ao slide de onde ela veio.

---

## Slide 1 — Capa

**Visual:**
- Logo TFTEC + Anhanguera (rodapé padrão da pós)
- Título: **Integração e Mensageria no Azure: um dia de incidente na Saldaro**
- Subtítulo: Aula hands-on ao vivo · Pós-Graduação em Arquitetura Cloud Azure
- Crédito: Prof. Guilherme Campos
- Aviso de ficção (ADR 0004), em letra pequena acima do rodapé: "Empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência."
- Imagem de fundo: arquitetura Azure conceitual com filas e eventos

**Bullets:** nenhum (capa limpa)

---

## Slide 2 — Regras do dia

**Título:** Como vai funcionar nas próximas 4 horas

**Bullets:**
- Três incidentes inspirados em experiência de campo e reescritos como ficção, numa empresa fictícia: **Saldaro Pagamentos**
- Você já fez o **Lab 0** em casa; hoje só rodamos o smoke test
- Cada incidente: **Sintomas → Hipóteses → Conserto → Prova**
- Conserto ao vivo **no Portal**; travou? Plano B: `deploy.ps1 -Cenario N -Pronto` (N = 2, 3 ou 4), e ninguém fica para trás
- **Aqueça** antes de cada rajada, e sempre depois de um `deploy.ps1`: o smoke test ou uma sonda que responde 404
- Custo no Free Trial: ~R$ 2 a 3, da véspera ao fim da aula; **apagamos o Resource Group juntos às 3h55**

**Visual:** linha do tempo horizontal com os 7 blocos (0:00 → 4:00) e os ícones dos três incidentes

---

## Slide 3 — A Saldaro Pagamentos

**Título:** Um hub de cobrança B2B (fictício) com um legado que dói

**Bullets:**
- 300 empresas clientes cobram seus pagadores PJ via Pix e boleto
- ~400 mil cobranças/mês; picos nos dias 5–10 e no fim do mês
- Jornada: **ERP → Hub → Banco → Webhook → Baixa → Notificação**
- "Saldaro 1.0": monólito, banco como fila, um worker de polling
- Hoje construímos a "Saldaro 2.0" consertando uma herança por vez

**Visual:** diagrama Mermaid do fio condutor (seção 2.1 do briefing) com os serviços Azure em cada ponto; destaque colorido nos três pontos que vão quebrar hoje

---

## Slide 4 — O que a Disciplina 04 já nos deu

**Título:** Revisão relâmpago em 5 minutos

**Bullets:**
- Service Bus: queue, topic, subscriptions, filtros SQL, **DLQ**, `maxDeliveryCount`
- Functions e Durable: 202 + `statusQueryGetUri` "de graça"
- Event Grid: push, CloudEvents; Event Hubs: streaming
- O que **não** praticamos lá: 202 à mão, idempotência, sessions, DLQ com motivo, Managed Identity, Bicep
- Hoje é exatamente sobre essa lista

**Visual:** tabela de duas colunas "Já vimos na D04" × "Vamos praticar hoje", com ícones dos serviços

---

## Slide 5 — Separador: Lab 0

**Título:** Lab 0 — Saldaro 2.0 em 10 minutos

**Bullets:** nenhum (separador de bloco no estilo da D04, com número grande)

**Visual:** separador com fundo distinto e o diagrama miniatura do fio condutor

---

## Slide 5a — O que vamos montar

**Título:** O que vamos montar: a Saldaro 2.0 no Azure

**Bullets:**
- Porta única: APIM, para o ERP e o banco
- Processamento: hub e banco no Functions Flex
- Mensageria: Service Bus (3 filas) e Event Grid
- Estado e segredos: Storage (tabelas) e Key Vault
- Observação: Application Insights sobre Log Analytics

**Visual:** o diagrama da arquitetura do guia de montagem, com os recursos agrupados por papel e a jornada da cobrança atravessando todos eles (slide sem número de página, para não renumerar os seguintes)

---

## Slide 5b — A ordem da montagem e o porquê de cada peça

**Título:** Treze etapas: cada peça resolve um problema

**Bullets:**
- Resource Group: tudo nasce e morre junto
- Log Analytics e App Insights: enxergar antes de quebrar
- Storage: estado barato, com concorrência otimista
- Service Bus: absorver a rajada e garantir entrega
- Key Vault: segredo fora do app setting
- Event Grid: avisar o parceiro sem acoplar
- Functions no Flex: escala por evento, paga por uso
- APIM: porta única e corte de 30 s, não WAF

**Visual:** trilha numerada com as 13 etapas do guia e o tempo de cada uma; destaque para "crie o APIM cedo" (slide sem número de página)

---

## Slide 6 — Smoke test: o dia de sol

**Título:** Uma fatura, ponta a ponta, em menos de 2 minutos

**Bullets:**
- `POST /faturas` → cobrança no banco simulado → webhook → baixa → notificação
- Prova: `smoke-test.ps1` passa; a fatura chega a `baixada`/`notificada` em < 2 min; no Application Insights, o `faturaId` costura a jornada inteira (o `operation_Id` do POST junta só a parte síncrona, hub → banco)
- O baseline é **ingênuo de propósito**: endpoint síncrono, SAS key, fila sem sessions, sem alertas
- **Simplificação de laboratório** (ADR 0007): HTTP triggers anônimos e APIM sem subscription; em produção, credencial por empresa cliente e backend só via APIM
- Tabela de recursos: `rg-saldaro-lab`, `sb-saldaro-<sufixo>`, `func-saldaro-hub-<sufixo>`, `func-saldaro-banco-<sufixo>`, `stsaldaro<sufixo>`, `apim-saldaro-<sufixo>`

**Visual:** print do Application Insights: Search (antigo Transaction search) pelo `faturaId`, com os eventos da jornada, e o End-to-end transaction details do POST (hub → banco) + tabela de recursos enxuta

---

## Slide 7 — Separador: Incidente 1

**Título:** Incidente 1 — "O timeout de sexta às 18h"

**Bullets:** nenhum

**Visual:** separador; relógio marcando 18:02 e um ERP reenviando a mesma fatura

---

## Slide 8 — Sintomas (Incidente 1)

**Título:** O banco demorou 45 s e a Saldaro duplicou cobranças

**Bullets:**
- `POST /faturas` síncrono esperava o banco antes de responder
- O APIM cortou em **30 s** (504) e o ERP **reenviou**: cada reenvio virou **outra fatura** no hub e **outra cobrança** no banco, que grava antes de responder
- `faturas-por-referencia-erp.kql`: a mesma `referenciaErp` com **2 faturas e 2 cobranças** (no banco, uma por `faturaId`)
- O worker de polling do legado não drenava a fila que se formou
- Teto da plataforma: resposta HTTP do Functions morre aos **230 s** (aqui, o APIM corta antes)

**Visual:** sequência (ERP → APIM → hub → banco) com o corte aos 30 s na seta de resposta (504 `{"erro":"o gateway esperou 30 s e desistiu"}`), o hub ainda esperando até ~45 s (`PostFatura` com 499 no Application Insights: ninguém mais ouvia) e a segunda cobrança em vermelho

---

## Slide 9 — O padrão: request-reply assíncrono

**Título:** 202 Accepted + endereço de status + fila

**Bullets:**
- API responde **202** com `Location: …/faturas/{id}/status` (URL pública do APIM) e `Retry-After: 5`
- Status: 200 `{ faturaId, status }` enquanto processa; **303** para `/faturas/{id}` quando `baixada` ou `notificada`
- `Idempotency-Key` no POST: reenvio devolve o **mesmo `faturaId`** e não enfileira de novo
- A fila `sbq-faturas-recebidas` absorve a rajada (**queue-based load leveling**); o `EmitirCobranca` chama o banco no ritmo dele

**Visual:** diagrama do padrão Asynchronous Request-Reply (Cloud Design Patterns) com os três endpoints

---

## Slide 10 — Prova (Incidente 1)

**Título:** Como saber que consertamos

**Bullets:**
- POST responde **202 em < 500 ms** com o app aquecido, mesmo com o banco a 60 s (`latencia=60`)
- Status: **200** enquanto processa e **303** para a fatura em 75 a 90 s
- 1.000 faturas com `-Paralelismo 20 -IdempotencyKey`: **1.000 × 202 e zero 5xx** no APIM em ~47 s; a fila drena em ~2 min
- `faturas-por-referencia-erp.kql` **vazio** na janela da rajada: 1 fatura e 1 cobrança por `referenciaErp` (zero cobrança duplicada); no banco, `quantidade = 1` por `faturaId`
- Mesma `Idempotency-Key` → **mesmo `faturaId`**
- Comparar com o Durable da D04: `statusQueryGetUri` é este padrão pronto

**Visual:** checklist de 5 itens com caixas (as cinco checagens do guia, Passos 5.1 a 5.5) + nota de rodapé com a comparação com o Durable; print do `erp-simulado.ps1` com 1.000 faturas e 0 erros

---

## Slide 11 — Separador: Incidente 2

**Título:** Incidente 2 — "O webhook que chegou duas vezes (e antes da hora)"

**Bullets:** nenhum

**Visual:** separador; dois envelopes idênticos chegando e um terceiro com o lacre violado

---

## Slide 12 — Sintomas (Incidente 2)

**Título:** Dia 10, 9h30: 1.200 confirmações repetidas pelo banco

**Bullets:**
- Duplicados: a mesma fatura **baixada e notificada várias vezes**
- Fora de ordem: "paga" chegou antes de "cobrança registrada" e o evento atrasado **desbaixou** a fatura (no lab, 2 de 20 terminam `registrada`)
- Uma confirmação **forjada**, sem assinatura, foi aceita: o webhook confiava em qualquer POST
- O worker processa 4 mensagens em paralelo: cópias da mesma confirmação **disputavam a mesma fatura**

**Visual:** linha do tempo de uma fatura com os eventos chegando embaralhados e duplicados

---

## Slide 13 — Assinatura que vale: HMAC do corpo

**Título:** Segredo no header não é assinatura

**Bullets:**
- `X-Saldaro-Signature: sha256=<HMAC de "{timestamp}.{corpo bruto}">` + `X-Saldaro-Timestamp` (±300 s)
- Validar **antes** de desserializar; comparar com `CryptographicOperations.FixedTimeEquals`
- Assinatura inválida, ausente ou fora da janela → **401** e nada entra na fila
- Hoje o `WebhookSecret` é app setting; no guia 3.5 vira referência ao Key Vault (rotação sem redeploy)
- **Simplificação de laboratório** (ADR 0007): webhook `Anonymous` e sem subscription no APIM, então aqui a assinatura é a única defesa. Em produção, somar IPs do banco (`ip-filter`) ou mTLS, e o backend só aceita tráfego do APIM

**Visual:** diagrama "corpo + segredo → HMAC → header" com o caminho do atacante bloqueado

---

## Slide 14 — Uma vez só e na ordem certa

**Título:** Dup detection + sessions + inbox

**Bullets:**
- `MessageId = idEvento` + duplicate detection (janela de 10 min) → o broker **descarta a confirmação repetida**
- `SessionId = faturaId` → **um consumidor por fatura** de cada vez, na ordem de chegada
- Inbox (`faturaId`, `idEvento`) + máquina de estados com ETag → baixa **uma vez só**; "registrada" depois de "baixada" é ignorada
- "Enable sessions" e "Enable duplicate detection" são **imutáveis**: drenar, apagar e recriar a fila no Portal (**+ Queue**); o `.bicepparam` registra

**Visual:** três camadas de defesa (broker, sessão, inbox) como funis sucessivos; print do Overview da fila no Portal (bloco Essentials, com Sessions e Duplicate detection em "Disabled"), o erro do CLI (`SubCode=40000`) e o painel **+ Queue** com as duas caixas marcadas (print a tirar no ensaio)

---

## Slide 15 — Prova (Incidente 2)

**Título:** Como saber que consertamos

**Bullets:**
- P1: POST forjado no meio da tempestade → **401**; nada entra na fila
- P2: `baixa-duplicada.kql` **vazio**
- P3: as 20 faturas com `quantidadeBaixas = 1` e `notificada`: a "registrada" atrasada não desbaixa
- P4: log com `prop__SessionId = faturaId` e `prop__SequenceNumber` crescente: **uma fatura por vez**, mesmo com concorrência 4
- P5: **1 notificação por fatura**; 120 webhooks viram 40 processamentos (o broker descartou 80 cópias)
- P6: o Bicep descreve o consertado: `requiresSession`, `requiresDuplicateDetection` e os cinco toggles do `cenario-3-pronto`

**Visual:** checklist de 6 itens (P1 a P6 do guia); trecho de KQL com resultado vazio

---

## Slide 16 — Separador: Incidente 3

**Título:** Incidente 3 — "A mensagem que ninguém conseguia processar"

**Bullets:** nenhum

**Visual:** separador; uma fila com 3.800 mensagens vermelhas e uma lupa

---

## Slide 17 — Sintomas (Incidente 3)

**Título:** "1.250,00" derrubou o worker 3.800 vezes

**Bullets:**
- O **banco parceiro** atualizou a API e passou a mandar `valor` como texto ("1.250,00")
- Exceção → reentrega → DLQ após 10 tentativas, **sem motivo útil** (`MaxDeliveryCountExceeded`)
- Uma falha transiente de 45 s mandou mensagens **boas** para a mesma DLQ, em segundos
- Reprocessar "tudo" baixaria faturas erradas; descartar "tudo" perderia pagamentos: ninguém sabia o que era o quê

**Visual:** Service Bus Explorer na aba Dead-letter, com `MaxDeliveryCountExceeded` em todas as linhas, Delivery Count 10 e a descrição genérica da plataforma ("Message could not be consumed after 10 delivery attempts.")

---

## Slide 18 — Classificar, agendar, triar

**Título:** Permanente vai para a DLQ com motivo; transiente espera e tenta de novo

**Bullets:**
- Schema inválido → `DeadLetterMessageAsync(message, deadLetterReason: "SchemaInvalida", deadLetterErrorDescription: …)` na **primeira** entrega
- Transiente → cópia agendada (`ScheduledEnqueueTime = agora + 5 s × 2^n`, `MessageId = {idEvento}-t{n}`, `tentativa = n`); teto de 3 → `TentativasEsgotadas`
- `triagem-dlq.ps1`: peek na DLQ agrupado por `DeadLetterReason`
- `-Reprocessar` corrige o `valor` e reprocessa com `MessageId = {original}-r{n}` + `reprocessadoDe`

**Visual:** fluxograma de decisão do worker (válido? transiente? tentativas?) com os três destinos

---

## Slide 19 — Prova (Incidente 3)

**Título:** Como saber que consertamos

**Bullets:**
- Valor inválido → DLQ na **primeira** entrega, motivo `SchemaInvalida` e a descrição do erro
- Transientes resolvidas pelo retry agendado (`mensagem.retry.agendado`: tentativas 1 e 2, com 10 e 20 s; a 3ª, de 40 s, é folga); **nunca** chegam à DLQ
- Triagem lista a DLQ por motivo; `-Reprocessar` devolve 52 à fila (40 corrigidas + 12 boas; as boas variam com o sorteio), faturas baixadas e **DLQ vazia**
- `dlq-por-motivo.kql`: os dead-letters do hub agrupados por motivo, com as faturas e exemplos de `idEvento`
- `maxDeliveryCount = 3` (Change no Portal, sem recriar a fila) e lock de 1 min, declarados no `cenario-4-pronto`

**Visual:** checklist de 5 itens; a DLQ do lab antes e depois do reprocessamento, de ~50 a 0 (52 → 0 no primeiro deploy)

---

## Slide 20 — Post-mortem coletivo

**Título:** Cinco linhas, sem culpados

**Bullets:**
- O que aconteceu · Impacto · Causa raiz · O que consertamos · O que impede de repetir
- Um post-mortem por incidente, escrito pela turma em 3 minutos cada
- Padrões praticados hoje: Asynchronous Request-Reply, Idempotent Receiver, Resequencer, Dead Letter Channel, Retry com backoff
- O que o legado "Saldaro 1.0" ensinou: banco não é fila; segredo não é assinatura; DLQ sem motivo é lixo

**Visual:** template do post-mortem de cinco linhas em uma caixa grande; três cards menores com os nomes dos incidentes

---

## Slide 21 — Desafio para casa e guias de leitura

**Título:** O dia de incidente continua

**Bullets:**
- **Desafio (3.6)**: "O agendador que morreu em silêncio": heartbeat ao concluir, KQL, alerta por ausência de sinal (workbook opcional)
- Guias de leitura: 3.1 rate limit e 429 · 3.5 Managed Identity + Key Vault · 3.7 Bicep e drift · 3.8 Premium ou Standard (ADR)
- Entrega: print do alerta disparando + post-mortem do 3.6
- Pré-requisito de tudo: **apague o `rg-saldaro-lab` agora** (R$ 0,07/h do Service Bus não dorme)

**Visual:** grade 2×3 com os cards dos cenários restantes, o 3.6 em destaque; botão "Delete resource group" em evidência

---

## Slide 22 — Encerramento e referências

**Título:** Obrigado! Referências para continuar

**Bullets:**
- Cloud Design Patterns: Asynchronous Request-Reply, Queue-Based Load Leveling, Retry, Throttling
- Enterprise Integration Patterns: Idempotent Receiver, Resequencer, Dead Letter Channel, Competing Consumers
- Microsoft Learn: Service Bus (sessions, dup detection, DLQ), Azure Functions .NET isolated, Key Vault + Managed Identity, Bicep
- Repositório da aula e canal de dúvidas

**Visual:** logos TFTEC + Anhanguera, QR code do repositório, lista de links

---

## Apêndice — Perguntas que a turma vai fazer

> **Slides de bolso, fora da soma das 4h.** Os dez slides deste apêndice ficam depois do slide 22 e só aparecem quando a pergunta surge: o professor salta para o FAQ, responde em 60 a 90 segundos e volta ao slide de onde a pergunta veio. O tempo deles **não entra** nos 238 minutos da tabela "Estrutura geral". Cada FAQ tem gancho nas notes do slide principal em que a pergunta tende a aparecer: uma linha "Se perguntarem: ... → FAQ-N".
>
> **Fonte:** `docs/research/2026-10-14-duvidas-das-tecnologias.md`, as dez perguntas marcadas **[Slide]**, na ordem da pesquisa; a primeira, "Por que APIM e não WAF?", é pedido do professor. Os bullets vêm da "Resposta curta" e do "Quando a alternativa vence"; a resposta completa, o "No kit da Saldaro" e as URLs do Microsoft Learn ficam nas notes. As linhas **Pesquisa** e **Ganchos** de cada FAQ servem ao QA e ao deck e não vão para o slide.

---

## FAQ-1 — APIM × WAF

**Título:** Por que APIM e não WAF?

**Bullets:**
- WAF barra ataque web: OWASP, bots, geofiltro
- APIM governa a API: identidade, cota, timeout, telemetria
- Produção: WAF na borda, APIM atrás; lab: só APIM

**Visual:** diagrama em camadas, da esquerda para a direita: Internet → **WAF** (Front Door ou Application Gateway: barra ataque web e bots) → **APIM** (governa a API: quem chama, quanto, por quanto tempo, com que telemetria) → Functions do hub. A caixa do WAF tracejada, com a etiqueta "fora do lab: custo fixo; os incidentes de hoje são de integração"

**Pesquisa:** pergunta 1 · **Ganchos:** slides 3 e 6

---

## FAQ-2 — Service Bus × outras filas e eventos

**Título:** Por que Service Bus, e não Storage Queue, Event Hubs ou Event Grid?

**Bullets:**
- Cobrança é comando: peek-lock, DLQ, sessões, duplicatas
- Storage Queue: backlog simples e barato, sem ordem
- Event Hubs: telemetria; Event Grid: avisar que aconteceu

**Visual:** comparação lado a lado em quatro colunas (Service Bus, Storage Queue, Event Hubs, Event Grid), com as linhas papel, ordem, detecção de duplicatas e dead-letter; a coluna do Service Bus em destaque e, no rodapé, o contraste do kit: filas `sbq-*` no hub × fila `banco-pagamentos` (Storage Queue, com a `-poison`) no banco simulado

**Pesquisa:** pergunta 4 · **Ganchos:** slide 3

---

## FAQ-3 — Service Bus Standard × Premium

**Título:** Por que Service Bus Standard, e não Premium?

**Bullets:**
- Standard já tem sessões, duplicatas, DLQ e transações
- Premium compra isolamento, rede privada, mensagens grandes
- Muda quando um gatilho do ADR 0003 aparece

**Visual:** árvore de decisão com os cinco gatilhos do ADR 0003 (rede privada exigida; throttling 50009 recorrente; mensagem acima de 256 KB sem claim check; latência previsível ou isolamento por empresa cliente; CMK ou Geo-Replication): algum "sim" → Premium; todos "não" → Standard

**Pesquisa:** pergunta 6 (e a 23, sobre o Network Security Perimeter) · **Ganchos:** slides 2 e 21

---

## FAQ-4 — FIFO, sessões e partições

**Título:** Se a fila é FIFO, por que a fatura desbaixou?

**Bullets:**
- FIFO é ordem de chegada, não de negócio
- Sessão: uma fatura, um consumidor por vez
- Partição é throughput; a regra mora no consumidor

**Visual:** linha do tempo de uma fatura em três faixas: o banco envia "paga" e depois "registrada"; a fila com sessão entrega na mesma ordem, uma mensagem por vez; a máquina de estados ignora "registrada" depois de "baixada". Ao lado, em miniatura, uma fila particionada espalhando mensagens sem chave entre as partições

**Pesquisa:** pergunta 7 · **Ganchos:** slides 12 e 14

---

## FAQ-5 — Duplicate detection × idempotência

**Título:** Se a fila tem duplicate detection, para que inbox e `Idempotency-Key`?

**Bullets:**
- Duplicate detection: mesmo `MessageId`, só dentro da janela
- Reentrega processada duas vezes? Só o consumidor impede
- Uma camada por repetição: ERP, banco, reentrega

**Visual:** três funis em sequência, um por tipo de repetição: `Idempotency-Key` (reenvio do ERP) → duplicate detection com `MessageId = idEvento` e janela de 10 min (confirmação repetida pelo banco) → inbox + ETag (reentrega do Service Bus e o que escapa da janela); embaixo de cada funil, o que ele deixa passar

**Pesquisa:** pergunta 8 · **Ganchos:** slides 9 e 14

---

## FAQ-6 — DLQ, maxDeliveryCount e retry

**Título:** Por que retry agendado à mão? O Functions não tem retry policy?

**Bullets:**
- Trigger de Service Bus não tem retry policy
- Reentrega é imediata e gasta o `maxDeliveryCount`
- Permanente: DLQ com motivo; transiente: cópia agendada

**Visual:** comparação lado a lado no eixo do tempo: à esquerda, a reentrega imediata (10 tentativas em segundos contra uma falha de 45 s e tudo na DLQ com `MaxDeliveryCountExceeded`); à direita, a cópia agendada em 10, 20 e 40 s (a falha passa) e o erro permanente direto na DLQ com `SchemaInvalida`

**Pesquisa:** pergunta 9 · **Ganchos:** slides 17 e 18

---

## FAQ-7 — Functions × Logic Apps × contêineres

**Título:** Por que Functions? Integração não é coisa de Logic Apps?

**Bullets:**
- Hub é código: alto volume e regra de negócio
- Logic Apps: conectores, B2B, aprovação humana, baixo volume
- Contêiner ou cluster próprio: Container Apps ou AKS

**Visual:** árvore de decisão: "fluxo desenhado com conectores ou gente no meio?" → Logic Apps; "precisa controlar a imagem ou o cluster?" → Container Apps ou AKS; "API web contínua num plano dedicado?" → App Service; senão, código orientado a eventos → Functions, com o hub da Saldaro nessa folha

**Pesquisa:** pergunta 10 · **Ganchos:** slide 3

---

## FAQ-8 — Bicep × Terraform × Portal

**Título:** Por que Bicep e não Terraform? E por que consertar no Portal?

**Bullets:**
- Bicep: estado no próprio Azure, what-if antes
- Terraform: multicloud ou time que já usa
- Portal ensina; Bicep recria e fecha o drift

**Visual:** comparação lado a lado em três colunas (Portal, Bicep, Terraform), com as linhas estado, multicloud, recriar em minutos e aprender; embaixo, a seta do dia: conserto no Portal → `.bicepparam` diz o mesmo → o próximo deploy confirma, em vez de desfazer

**Pesquisa:** pergunta 17 · **Ganchos:** slides 2 e 9

---

## FAQ-9 — Durable × request-reply feito à mão

**Título:** Se o Durable dá o 202 de graça, por que fazer à mão?

**Bullets:**
- Mesmo padrão; à mão, cada peça aparece
- Uma etapa só: fila e status bastam
- Durable vence: várias etapas, timers, compensação

**Visual:** comparação lado a lado: Durable (202 enquanto roda, 200 no fim, URL de status com a chave `code=`, estado no Task Hub) × Saldaro (200 enquanto processa, 303 no fim, URL só leitura, estado na tabela `faturas`), e a linha "quando escolher" atravessando as duas colunas

**Pesquisa:** pergunta 18 · **Ganchos:** slides 4 e 10

---

## FAQ-10 — HMAC × mTLS × IP × OAuth

**Título:** Por que HMAC no webhook, e não mTLS, OAuth ou lista de IPs?

**Bullets:**
- HMAC prova o corpo; mTLS, quem conectou
- OAuth exige token; IP só filtra quem chega
- Produção: HMAC mais IPs ou mTLS (ADR 0007)

**Visual:** comparação em quatro colunas, cada uma com a pergunta que responde: HMAC (o corpo veio de quem tem o segredo?), mTLS (quem abriu a conexão?), lista de IPs (quem consegue chegar?), OAuth (quem chama, com um token que expira?); HMAC em destaque e uma chave somando HMAC + IPs ou mTLS com a etiqueta "produção"

**Pesquisa:** pergunta 19 · **Ganchos:** slide 13

---

*v1.0 em 2026-10-13: slides dos três incidentes fechados contra o contrato do kit e validados no `QA_Validacao_Speaker_Notes.md`. v1.1 em 2026-10-14: alinhados ao código e aos guias na integração (prova do Incidente 1 por `referenciaErp`, P1 a P6 do Incidente 2, tempos por slide do roteiro dos guias). v1.2 em 2026-10-14: números do primeiro deploy real (Lab 0 em ~10 min, 202 aquecido, 303 em 75 a 90 s, rajada de 1.000, 2 de 20 faturas desbaixadas, `prop__SessionId`, 80 cópias descartadas, DLQ de 52 e reprocesso 40 + 12, retries de 10 e 20 s), o Overview → Essentials da fila, o aquecimento depois de cada `deploy.ps1` e o post-mortem de 3 min. v1.3 em 2026-10-14: revisão de código: aviso de ficção na capa e incidentes "inspirados em experiência de campo" no slide 2 (ADR 0004), simplificação de laboratório no smoke test (ADR 0007), confirmação repetida e reprocessamento no lugar de "reenvio" (glossário do `CONTEXT.md`), as cinco checagens da prova do Incidente 1 e a DLQ do lab de ~50 a 0 no visual da prova do Incidente 3. v1.4 em 2026-10-14: apêndice "Perguntas que a turma vai fazer", com os slides de bolso FAQ-1 a FAQ-10 (fora das 4h; fonte `docs/research/2026-10-14-duvidas-das-tecnologias.md`), e "Portal primeiro" (ADR 0008): o conserto ao vivo é no Portal e o `deploy.ps1` é o plano B (slide 2), a fila do Incidente 2 é recriada no Portal com **+ Queue** (slide 14) e o `maxDeliveryCount` do Incidente 3 muda no **Change** da Overview da fila (slide 19). Fonte de verdade do layout: `Disciplina 04 - Integração e Mensageria.pptx` no Blob da pós.*
