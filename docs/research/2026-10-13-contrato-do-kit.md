# Contrato do kit da aula (fonte de verdade técnica)

**Version-anchor:** Q4-2026 · **Data:** 2026-10-13 · **Status:** v1, usado para produzir o material em paralelo, com a Atualização 1 (seção 3, 2026-10-14) e a Atualização 2 (no fim do documento, 2026-10-14), que consolida o que a execução refinou e prevalece sobre o texto original onde os dois divergirem

Este documento fixa nomes, interfaces e comportamentos que o código, o Bicep, os scripts, os guias, os slides e as speaker notes precisam usar **exatamente igual**. Ele complementa `docs/research/2026-10-13-briefing-da-aula.md` (o "porquê") com o "como". Onde os dois divergirem, vale este contrato, e o briefing é ajustado na integração.

## 0. Premissas assumidas (o professor pode mudar depois)

- Alunos com **Free Trial** próprio (US$ 200). Tudo precisa caber nele.
- Região padrão `brazilsouth`: Flex Consumption existe lá com runtime `dotnet-isolated` 10 (verificado em 2026-10-13 com `az functionapp list-flexconsumption-runtimes`). Plano B: `-Plano Consumption` (Windows Consumption, legado mas GA).
- Máquina do aluno: VS Code, .NET 10 SDK, Azure CLI (com Bicep), PowerShell 7. **Azure Functions Core Tools não é obrigatório**: o deploy publica com `dotnet publish` + `az functionapp deployment source config-zip`.
- Nome fictício **Saldaro Pagamentos S.A.** ("Saldaro Hub"), confirmado pelo professor em 2026-10-14.
- Nenhum dado, nome ou número real (ver `CLAUDE.md`, Confidencialidade). CNPJs de teste gerados com dígitos verificadores válidos por algoritmo, nunca CNPJs conhecidos.
- Só serviços e recursos em GA. Simplificações de laboratório são explícitas e comentadas (ex.: HTTP triggers anônimos).

## 1. Estrutura de pastas

```
01_Aulas/
  Lab0_Fundacao_Guia.md
  Lab0_Montagem_no_Portal_Guia.md            (o Lab 0 montado à mão no Portal; ADR 0008)
  Incidente1_Timeout_Guia.md                 (cenário 3.2, ao vivo)
  Incidente2_Webhook_Guia.md                 (cenário 3.3, ao vivo)
  Incidente3_DLQ_Guia.md                     (cenário 3.4, ao vivo)
  Leitura_3.1_RateLimit_Guia.md
  Leitura_3.5_ManagedIdentity_Guia.md
  Desafio_3.6_Heartbeat_Guia.md              (desafio para casa)
  Leitura_3.7_Bicep_Drift_Guia.md
  Leitura_3.8_Premium_vs_Standard_Guia.md
02_Apresentação/                             (já existe)
03_Aplicações/
  Saldaro.Hub/                 Azure Functions .NET 10 isolated (o hub)
  Saldaro.Hub.Tests/           xUnit
  Saldaro.BancoSimulado/       Azure Functions .NET 10 isolated (banco parceiro simulado)
  Saldaro.BancoSimulado.Tests/ xUnit
  Saldaro.Ferramentas/         console .NET 10 (triagem da DLQ, worker legado, rajada)
  Saldaro.Ferramentas.Tests/   xUnit
  Saldaro.sln                  (criada na integração, com os 6 projetos)
infra/
  main.bicep
  modules/*.bicep
  parameters/cenario-0.bicepparam, cenario-1-pronto.bicepparam … cenario-7-pronto.bicepparam
  README.md
scripts/
  deploy.ps1, limpar.ps1, check-drift.ps1          (ticket de infra)
  erp-simulado.ps1, smoke-test.ps1, triagem-dlq.ps1 (ticket de scripts)
  varredura-termos.ps1                              (já existe)
kql/*.kql
templates/post-mortem.md, templates/adr.md
docs/adr/NNNN-*.md
CONTEXT.md
```

## 2. Recursos Azure (Bicep, escopo resource group; o RG é criado pelo `deploy.ps1`)

> Refinado na Atualização 2, A2.2 (no fim do documento).

`<sufixo>` = 4 a 6 caracteres minúsculos (letras e dígitos), escolhido pelo aluno ou gerado.

| Recurso | Nome | Observações |
|---|---|---|
| Resource Group | `rg-saldaro-lab` | criado pelo `deploy.ps1` (`az group create`) |
| Service Bus namespace | `sb-saldaro-<sufixo>` | Standard; `disableLocalAuth = identidadeGerenciada` |
| Fila | `sbq-faturas-recebidas` | lock 1 min, `maxDeliveryCount` 10, sem sessions |
| Fila | `sbq-confirmacoes-banco` | lock 1 min; `requiresSession = filaComSessions`; `requiresDuplicateDetection = filaDupDetection` (janela 10 min); `maxDeliveryCount = maxDeliveryCount`; dead-lettering on expiration |
| Fila | `sbq-notificacoes-parceiro` | destino do Event Grid; `maxDeliveryCount` 10 |
| Storage | `stsaldaro<sufixo>` | Standard LRS; tabelas `faturas`, `inbox`, `idempotencia`, `conciliacao` (hub) e `cobrancas` (banco); fila de storage `banco-pagamentos` (banco); containers de deploy do Flex |
| Planos | `asp-saldaro-hub-<sufixo>`, `asp-saldaro-banco-<sufixo>` | Flex Consumption (FC1) ou Consumption (Y1), um por app |
| Function App hub | `func-saldaro-hub-<sufixo>` | .NET 10 isolated, identidade system-assigned |
| Function App banco | `func-saldaro-banco-<sufixo>` | .NET 10 isolated |
| APIM | `apim-saldaro-<sufixo>` | Consumption; ver seção 3 |
| Key Vault | `kv-saldaro-<sufixo>` | autorização RBAC; criado no baseline (vazio); segredos `WebhookSecret` e `BancoApiKey` só com `identidadeGerenciada = true` |
| Event Grid topic | `evgt-saldaro-faturas-<sufixo>` | esquema CloudEvents 1.0; identidade system-assigned; event subscription `evgs-notificar-parceiro` → fila `sbq-notificacoes-parceiro` com `deliveryWithResourceIdentity` (role `Azure Service Bus Data Sender` na fila; funciona com `disableLocalAuth`) |
| Log Analytics / App Insights | `log-saldaro-<sufixo>` / `appi-saldaro-<sufixo>` | workspace-based; os dois apps e o APIM enviam para o mesmo App Insights |
| Alertas (só com `alertas = true`) | Action Group `ag-saldaro`; `alrt-heartbeat-ausente` (scheduled query, a cada 5 min, janela 15 min, dispara com 0 heartbeats); `alrt-dlq-confirmacoes` (métrica `DeadletteredMessages` > 0 na entidade `sbq-confirmacoes-banco`) | e-mail = `emailAlerta` |
| Role assignments do aluno (se `principalIdAluno` não vazio) | `Azure Service Bus Data Owner` no namespace; `Storage Table Data Contributor` no storage; `Key Vault Secrets Officer` no Key Vault | para Service Bus Explorer com `disableLocalAuth`, triagem por identidade e passos do 3.5 |
| Role assignments do hub (só com `identidadeGerenciada = true`) | `Azure Service Bus Data Sender` + `Azure Service Bus Data Receiver` + `Reader` no namespace; `Storage Table Data Contributor` no storage; `EventGrid Data Sender` no tópico; `Key Vault Secrets User` no Key Vault | nomes `guid(...)` determinísticos (idempotentes). O `Reader` entrou em 2026-10-14, para a escala do trigger ler a contagem exata da fila (recomendação do Learn). O `deploy.ps1` também reinicia o hub depois que as Key Vault references resolvem: no primeiro deploy real, os triggers subiram antes de as roles valerem e o worker com sessão ficou parado até o restart |

Outputs do `main.bicep` (o `deploy.ps1` usa para a tabela de recursos e para `scripts/.env.local`): nomes de todos os recursos, `hubBaseUrl` (`https://<defaultHostName do hub>/api`), `bancoBaseUrl` (`https://<defaultHostName do banco>/api`), `apimBaseUrl` (`https://apim-saldaro-<sufixo>.azure-api.net/saldaro`), `serviceBusFqdn`, `tableServiceUri`, `eventGridTopicEndpoint`, `appInsightsName`. URLs de app vêm de `properties.defaultHostName`, nunca montadas à mão.

## 3. APIM (`apim-saldaro-<sufixo>`, Consumption)

> **Atualização 1 (2026-10-14):** `rate-limit-by-key` não é suportado no tier Consumption (referência de políticas do APIM) e `rate-limit` só conta por subscription key. O rate limit do 3.1 passa a ser por subscription, com uma chave por empresa cliente, e os webhooks ficam numa API separada que nunca exige chave.

- API `saldaro`, path `saldaro`, service URL = `hubBaseUrl`. Operações: `post-fatura` POST `/faturas`; `get-fatura` GET `/faturas/{id}`; `get-fatura-status` GET `/faturas/{id}/status`. `subscriptionRequired: false` em todos os estados, **exceto** `cenario-1-pronto`.
- API `saldaro-webhooks`, path `saldaro/webhooks`, service URL = `hubBaseUrl`, operação `post-webhook-banco` POST `/banco` (URL final `{apimBaseUrl}/webhooks/banco`, igual à de antes). `subscriptionRequired: false` sempre.
- Política das duas APIs: `<forward-request timeout="30" />` no backend (o gateway corta em 30 s: é isso que o Incidente 1 mostra). No tier Consumption, a duração total da requisição também tem teto de 30 s pela plataforma ("Total request duration", [API Gateway Overview](https://learn.microsoft.com/en-us/azure/api-management/api-management-gateways-overview)); o Learn não diz que erro o `on-error` enxerga quando esse teto vence antes do `forward-request`, então, se o deploy real devolver 500 no lugar do 504, baixar o `forward-request` para um pouco menos de 30 s.
- Com `rateLimit = true` (`cenario-1-pronto`): a API `saldaro` exige subscription; duas subscriptions de escopo da API, `cliente-a` e `cliente-b` (uma por empresa cliente); `<rate-limit calls="60" renewal-period="60" />` no inbound de `post-fatura` (resposta 429 com `Retry-After`). O `erp-simulado.ps1` busca a chave do `-Cliente` em tempo de execução (`az rest` no `listSecrets` da subscription) e envia `Ocp-Apim-Subscription-Key`; a chave nunca vai para arquivo nem output. O header `x-cliente-id` continua sendo enviado (o hub usa como `clienteId`).
- Diagnóstico do APIM para o App Insights registrando o header `x-cliente-id` (para o KQL de 429 por cliente, que também agrupa por subscription).
- Simplificação de laboratório fora do 3.1: sem subscription (ADR 0007).

Outros ajustes da Atualização 1: `deploy.ps1 -Cenario 0 -Pronto` é aceito como sinônimo de `-Cenario 0` (não existe `cenario-0-pronto`); o plano B `-Plano Consumption` é **Windows** Consumption (o .NET 10 não roda no Consumption em Linux); com `Cenario:Inbox = true`, a `NotificarParceiro` ignora `FaturaBaixada` repetido (a entrega do Event Grid é pelo menos uma vez); o firewall de IP do Standard não tem exceção de serviços confiáveis e bloquearia a entrega do Event Grid (vale para o guia 3.8).

## 4. Parâmetros do `main.bicep` e estados por cenário

> Refinado na Atualização 2, A2.4.

Parâmetros: `sufixo` (string), `location` (default `brazilsouth`), `plano` (`'Flex'` | `'Consumption'`, default `'Flex'`), `principalIdAluno` (string, default `''`), `emailAlerta` (string, default `''`), `@secure() webhookSecret`, `@secure() bancoApiKey`, e os toggles:

`asyncRequestReply`, `webhookHmac`, `inbox`, `filaComSessions`, `filaDupDetection`, `workerComSessao`, `dlqComMotivo`, `retryAgendado`, `maxDeliveryCount` (int), `rateLimit`, `identidadeGerenciada`, `heartbeat`, `alertas`.

| Arquivo de parâmetros | Valores |
|---|---|
| `cenario-0.bicepparam` (baseline ingênuo) | todos os booleanos `false`; `maxDeliveryCount = 10` |
| `cenario-1-pronto` (3.1) | baseline + `rateLimit = true` |
| `cenario-2-pronto` (3.2) | baseline + `asyncRequestReply = true` |
| `cenario-3-pronto` (3.3) | `cenario-2-pronto` + `webhookHmac`, `inbox`, `filaComSessions`, `filaDupDetection`, `workerComSessao` = `true` |
| `cenario-4-pronto` (3.4) | `cenario-3-pronto` + `dlqComMotivo`, `retryAgendado` = `true`; `maxDeliveryCount = 3` |
| `cenario-5-pronto` (3.5) | `cenario-4-pronto` + `identidadeGerenciada = true` |
| `cenario-6-pronto` (3.6) | `cenario-4-pronto` + `heartbeat`, `alertas` = `true` |
| `cenario-7-pronto` (3.7) | `cenario-5-pronto` (sem diferença de parâmetro; o "pronto" é drift zero) |

Estado **inicial** de cada cenário (`deploy.ps1 -Cenario N` sem `-Pronto`): 1 e 2 → `cenario-0`; 3 → `cenario-2-pronto`; 4 → `cenario-3-pronto`; 5 e 6 → `cenario-4-pronto`; 7 → `cenario-5-pronto`. O 3.8 é exercício de decisão, sem deploy próprio. `rateLimit` só fica ligado no `cenario-1-pronto` (a rajada de 1.000 faturas do 3.2 não pode bater em 429).

O Bicep traduz os toggles em app settings do hub (seção 5) e em propriedades de recurso (filas, APIM, alertas, roles, Key Vault). Os segredos `webhookSecret` e `bancoApiKey` são gerados uma vez pelo `deploy.ps1` (32 bytes aleatórios em base64), reaproveitados nos redeploys (lidos do app setting existente ou do Key Vault) e nunca gravados em arquivo.

`requiresSession` e `requiresDuplicateDetection` são **imutáveis** numa fila existente. O caminho didático do 3.3 é: drenar, apagar a fila no Portal, redeployar. O `deploy.ps1 -Cenario 3 -Pronto` (plano B) detecta a divergência e recria a fila sozinho, avisando no terminal.

## 5. Hub (`03_Aplicações/Saldaro.Hub`, `net10.0`, isolated worker)

> Refinado na Atualização 2, A2.5 (5.1, 5.3, 5.4 e 5.5).

### 5.1 Functions (nomes exatos; HTTP triggers `AuthorizationLevel.Anonymous`, simplificação de lab comentada no código)

| Function | Trigger / rota | Comportamento |
|---|---|---|
| `PostFatura` | HTTP POST `faturas` | Header `x-cliente-id` obrigatório (400 sem ele). **Baseline** (`Cenario:AsyncRequestReply = false`): grava a fatura `recebida`, chama o banco de forma síncrona (`POST {BancoBaseUrl}/cobrancas?{caos}` com `x-api-key`), grava `idCobranca`, status `cobranca-emitida`, responde **201** `{ faturaId, status, idCobranca }`. **Consertado** (`true`): honra `Idempotency-Key` (tabela `idempotencia`, insert condicional; chave repetida devolve o mesmo `faturaId` sem reenfileirar; se gravar ou enfileirar falhar depois da reserva, apaga a fatura e a reserva e devolve o erro, para o reenvio recomeçar do zero), grava `recebida`, envia para `sbq-faturas-recebidas`, responde **202** `{ faturaId, status: "recebida" }` com `Location: {UrlBasePublica}/faturas/{id}/status` e `Retry-After: 5` |
| `GetFatura` | HTTP GET `faturas/{id}` | 200 com a fatura (JSON) ou 404 |
| `GetFaturaStatus` | HTTP GET `faturas/{id}/status` | 200 `{ faturaId, status }` enquanto não baixou; **303** com `Location: {UrlBasePublica}/faturas/{id}` quando `baixada` ou `notificada`; 404 se não existe |
| `EmitirCobranca` | Service Bus `sbq-faturas-recebidas` | Chama o banco (mesmo request do baseline), grava `idCobranca`, status `cobranca-emitida`; 503 do banco → exceção (reentrega pelo Service Bus) |
| `WebhookBanco` | HTTP POST `webhooks/banco` | Lê o corpo bruto. Com `Cenario:WebhookHmac = true`: valida assinatura (seção 7.2); inválida ou timestamp fora de ±300 s → **401** e nada vai para a fila. **Baseline**: aceita qualquer POST (é assim que a confirmação forjada passa). Extrai `idEvento`, `faturaId`, `tipo` com `JsonDocument` (sem desserializar `valor`) e envia o **corpo bruto** para `sbq-confirmacoes-banco` com `MessageId = idEvento`, `SessionId = faturaId`, propriedade `tipo`, `ContentType = application/json`; responde 202 |
| `ProcessarConfirmacaoSemSessao` | Service Bus `sbq-confirmacoes-banco`, sem sessions | Ativa quando `workerComSessao = false` |
| `ProcessarConfirmacaoComSessao` | Service Bus `sbq-confirmacoes-banco`, `IsSessionsEnabled = true` | Ativa quando `workerComSessao = true`. Mesma lógica de baixa |
| `NotificarParceiro` | Service Bus `sbq-notificacoes-parceiro` | Corpo = CloudEvent `FaturaBaixada`; se a fatura tem `callbackUrl`, faz POST; status `notificada`; evento `notificacao.enviada` |
| `Conciliacao` | Timer `0 */5 * * * *` | Conta faturas por status e marca como divergência as `cobranca-emitida` há mais de 10 min sem confirmação; grava resumo em `conciliacao`; com `Cenario:Heartbeat = true` emite `conciliacao.heartbeat` **ao concluir com sucesso** (nunca no início) |
| `Saude` | HTTP GET `saude` | 200 `{ versao, toggles }` |

Os dois triggers de confirmação usam `AutoCompleteMessages = false` e `ServiceBusMessageActions` (e `ServiceBusSessionMessageActions` no de sessão) e compartilham a mesma lógica de baixa (seção 5.3). Só um deles fica ativo por vez, via `AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled` / `AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled` (um receptor sem sessão numa fila com sessão falharia).

### 5.2 App settings (nomes exatos)

| Setting | Baseline | Com `identidadeGerenciada = true` |
|---|---|---|
| `AzureWebJobsStorage` | connection string | connection string (compromisso documentado: só os dados da aplicação migram para identidade) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | sim | sim |
| `ServiceBusConnection` | connection string da `RootManageSharedAccessKey` (ingênuo de propósito) | removido |
| `ServiceBusConnection__fullyQualifiedNamespace` | — | `sb-saldaro-<sufixo>.servicebus.windows.net` |
| `TablesConnection` | connection string do storage | removido |
| `TablesConnection__serviceUri` | — | `https://stsaldaro<sufixo>.table.core.windows.net` |
| `EventGridTopicEndpoint` | endpoint do tópico | endpoint do tópico |
| `EventGridTopicKey` | chave do tópico | removido (publica com `DefaultAzureCredential`) |
| `BancoApiKey` | valor em texto | `@Microsoft.KeyVault(VaultName=kv-saldaro-<sufixo>;SecretName=BancoApiKey)` |
| `WebhookSecret` | valor em texto | `@Microsoft.KeyVault(VaultName=kv-saldaro-<sufixo>;SecretName=WebhookSecret)` |
| `BancoBaseUrl` | `bancoBaseUrl` | idem |
| `UrlBasePublica` | `apimBaseUrl` | idem |
| `WebhookUrlPublica` | `{apimBaseUrl}/webhooks/banco` | idem |
| `Cenario__AsyncRequestReply`, `Cenario__WebhookHmac`, `Cenario__Inbox`, `Cenario__DlqComMotivo`, `Cenario__RetryAgendado`, `Cenario__Heartbeat` | dos toggles | dos toggles |
| `AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled` | `!workerComSessao` | idem |
| `AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled` | `workerComSessao` | idem |

O código lê `Cenario:*` com o padrão Options. Os clientes (`ServiceBusClient`, `TableServiceClient`, `EventGridPublisherClient`) são registrados por DI e aceitam as duas formas: connection string quando o setting simples existe, ou endpoint + `DefaultAzureCredential` quando só existe a forma `__fullyQualifiedNamespace`/`__serviceUri`/sem chave. O `HttpClient` do banco tem `Timeout = 120 s` (quem corta primeiro é o APIM, aos 30 s).

### 5.3 Lógica de baixa (compartilhada pelos dois triggers)

1. **Desserializar** o evento (seção 7.1). Falha de schema (ex.: `valor` como texto "1.250,00"): com `Cenario:DlqComMotivo = true`, `DeadLetterMessageAsync` imediato com `DeadLetterReason = "SchemaInvalida"` e `DeadLetterErrorDescription` = mensagem do erro (até 1.024 caracteres) + evento `mensagem.deadletter`; senão, lançar exceção (reentrega até `maxDeliveryCount`, depois `MaxDeliveryCountExceeded`).
2. **Falha transiente simulada**: se `simulaTransiente = true` e agora < `transienteAte`, é falha transiente. Com `Cenario:RetryAgendado = true`: enviar cópia agendada (mesmo corpo, `MessageId = {idEvento}-t{n}`, mesmo `SessionId`, propriedade `tentativa = n`, `ScheduledEnqueueTime = agora + 5 s × 2^n`, n = 1..3) e completar a original; se n passaria de 3, dead-letter `TentativasEsgotadas` + evento `mensagem.deadletter`; evento `mensagem.retry.agendado`. Senão, lançar exceção (no baseline, 10 reentregas imediatas acabam em segundos, antes dos 45 s da falha: mensagens boas caem na DLQ, como no incidente).
3. **Inbox** (`Cenario:Inbox = true`): se existe `inbox` (PK = `faturaId`, RK = `idEvento`), completar sem efeito + evento `confirmacao.duplicada.ignorada`.
4. **Máquina de estados**: `recebida` → `cobranca-emitida` → `registrada` → `baixada` → `notificada`. `cobranca.paga` leva a `baixada` a partir de `cobranca-emitida` ou `registrada`; `cobranca.registrada` depois de `baixada` é ignorada. Com `Cenario:Inbox = true`, a atualização usa concorrência otimista (ETag): quem perde relê e reavalia. **Baseline**: sem guarda de estado nem ETag; cada `cobranca.paga` grava `baixada` e publica de novo, e um `cobranca.registrada` atrasado "desbaixa" a fatura (é o sintoma do Incidente 2).
5. **Efeitos**: na transição para `baixada`, gravar `baixadaEm`, incrementar `quantidadeBaixas`, publicar `FaturaBaixada` (seção 7.3) e emitir `fatura.baixada`. Com inbox ligada, gravar o registro na `inbox` e só publicar quando a transição de fato aconteceu.
6. Completar a mensagem.

### 5.4 Tabelas do hub

- `faturas`: PK = `faturaId`, RK = `"fatura"`. Colunas: `clienteId`, `valor` (double), `vencimento`, `pagadorCnpj`, `referenciaErp`, `status`, `idCobranca`, `quantidadeBaixas` (int), `baixadaEm`, `callbackUrl`, `caos`, `criadaEm`, `atualizadaEm`.
- `inbox`: PK = `faturaId`, RK = `idEvento`. Colunas: `tipo`, `processadoEm`.
- `idempotencia`: PK = `clienteId`, RK = valor do `Idempotency-Key`. Colunas: `faturaId`, `criadaEm`.
- `conciliacao`: PK = `yyyyMMdd`, RK = id da execução. Colunas: contagens por status, `divergencias`, `duracaoMs`.

### 5.5 `host.json`

`extensions.serviceBus`: `maxConcurrentCalls = 4` (simula dois workers disputando a mesma fatura sem precisar de duas instâncias), `maxConcurrentSessions = 8`, `prefetchCount = 0`, `autoCompleteMessages = false`. Sampling do Application Insights desligado (no `host.json` e no worker).

## 6. Banco simulado (`03_Aplicações/Saldaro.BancoSimulado`, `net10.0`, isolated worker)

> Refinado na Atualização 2, A2.6.

| Function | Trigger / rota | Comportamento |
|---|---|---|
| `PostCobranca` | HTTP POST `cobrancas` | Exige `x-api-key = BancoApiKey` (401 sem). Body `{ faturaId, clienteId, valor, vencimento, pagadorCnpj, webhookUrl }`. Com `falhaTransiente = N`, N% das chamadas respondem 503 antes de qualquer efeito. Senão: **persiste** a cobrança (`cobrancas`, PK = `faturaId`, RK = `idCobranca`), **enfileira** os webhooks em `banco-pagamentos`, **e só então** espera `latencia` segundos e responde 201 `{ idCobranca: "COB-<8 hex>", linhaDigitavel, qrCode }` (fictícios). É por persistir antes de responder que o reenvio do ERP vira cobrança duplicada |
| `EnviarWebhook` | Storage Queue `banco-pagamentos` | Mensagem `{ evento, webhookUrl, assinaturaInvalida }`; assina (seção 7.2) e faz POST em `webhookUrl`; com `assinaturaInvalida`, corrompe o hex |
| `GetCobrancasPorFatura` | HTTP GET `cobrancas/{faturaId}` | `{ faturaId, quantidade, cobrancas: [...] }` (a prova do Incidente 1 é `quantidade = 1`) |
| `Saude` | HTTP GET `saude` | 200 |

Botões de caos (query string de `POST /api/cobrancas`; o hub repassa o campo `caos` da fatura):

| Parâmetro | Default | Efeito |
|---|---|---|
| `latencia` | 0 (máx. 200) | segundos que o banco demora para **responder** (já tendo registrado a cobrança) |
| `atrasoWebhook` | 5 | segundos até o `cobranca.paga`; o `cobranca.registrada` vai em 1 s |
| `duplicar` | 1 (máx. 5) | quantas vezes cada evento é enviado, sempre com o **mesmo** `idEvento` |
| `embaralhar` | false | inverte a ordem: `cobranca.paga` em 1 s e `cobranca.registrada` em `atrasoWebhook + 3` s |
| `assinatura` | `valida` | `invalida` corrompe a assinatura |
| `valorComVirgula` | false | `valor` sai como texto pt-BR (`"1.250,00"`) |
| `falhaTransiente` | 0 (0–100) | N% das chamadas `POST /cobrancas` respondem 503; N% dos eventos `cobranca.paga` saem com `simulaTransiente = true` e `transienteAte = envio + 45 s` |

Settings do banco: `AzureWebJobsStorage`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `TablesConnection`, `WebhookSecret`, `BancoApiKey`. O banco é "o parceiro": não migra para identidade no 3.5.

## 7. Mensagens, assinatura e eventos

### 7.1 Evento de webhook (banco → hub; é também o corpo da mensagem em `sbq-confirmacoes-banco`)

```json
{
  "idEvento": "3f2b8c1e-9d4a-4c7e-8a1b-2f6d5e4c3b2a",
  "tipo": "cobranca.paga",
  "faturaId": "9a7e1c52-0b3d-4f8e-a6c1-7d2e4b9f0a13",
  "idCobranca": "COB-1a2b3c4d",
  "valor": 1250.00,
  "ocorridoEm": "2026-11-10T09:30:00Z",
  "simulaTransiente": false,
  "transienteAte": null
}
```

`tipo` ∈ { `cobranca.registrada`, `cobranca.paga` }. Com `valorComVirgula`, `"valor": "1.250,00"`.

Mensagem em `sbq-faturas-recebidas`: `{ faturaId, clienteId, valor, vencimento, pagadorCnpj, referenciaErp, callbackUrl, caos }`, `MessageId = faturaId`.

### 7.2 Assinatura do webhook (banco assina, hub valida)

- Headers: `X-Saldaro-Timestamp: <unix seconds>` e `X-Saldaro-Signature: sha256=<hex minúsculo>`.
- Conteúdo assinado: a string `"{timestamp}.{corpo}"`, onde `corpo` é o corpo bruto exatamente como enviado (UTF-8).
- Algoritmo: HMAC-SHA256 com chave = `UTF8(WebhookSecret)` (a string do setting, sem decodificar base64).
- Validação no hub: recalcular, comparar com `CryptographicOperations.FixedTimeEquals`, exigir |agora − timestamp| ≤ 300 s, validar **antes** de desserializar.

### 7.3 `FaturaBaixada` (hub → Event Grid → `sbq-notificacoes-parceiro`)

CloudEvents 1.0: `type = "Saldaro.Faturas.FaturaBaixada"`, `source = "/saldaro/hub"`, `subject = "faturas/{faturaId}"`, `id` = GUID novo, `data = { faturaId, clienteId, valor, baixadaEm, idEvento }`.

### 7.4 Retry agendado e dead-letter

> Refinado na Atualização 2, A2.7.

Ver 5.3. Motivos próprios de dead-letter: `SchemaInvalida`, `TentativasEsgotadas`. Da plataforma: `MaxDeliveryCountExceeded`, `TTLExpiredException`.

## 8. Ferramentas (`03_Aplicações/Saldaro.Ferramentas`, console `net10.0`)

> Refinado na Atualização 2, A2.8.

Autentica com `DefaultAzureCredential` (o `az login` do aluno; o `deploy.ps1` dá ao aluno `Azure Service Bus Data Owner`). Subcomandos:

- `triagem --namespace <fqdn> --fila <nome> [--max 500] [--reprocessar] [--descartar-motivo <motivo>]`: peek na DLQ (`SubQueue.DeadLetter`), tabela agrupada por `DeadLetterReason` com exemplos; com `--reprocessar`, recebe da DLQ, corrige `valor` em texto pt-BR para número, reprocessa a mensagem (manda a cópia para a fila principal com `MessageId = {original}-r{n}`, mesmo `SessionId`, propriedade `reprocessadoDe = {MessageId original}`) e completa a mensagem da DLQ; `--descartar-motivo` completa (descarta) as do motivo informado.
- `legado-auto-create --namespace <fqdn> [--mensagens 20]`: o "worker legado" do 3.7: cria a fila `confirmacoesbanco` (sem hífens, configuração padrão) com `ServiceBusAdministrationClient` se não existir e envia mensagens para ela. Precisa falhar com 401 quando o aluno só tem roles de Sender/Receiver.
- `rajada --namespace <fqdn> --fila <nome> [--remetentes 50] [--mensagens 100]`: envio concorrente para o exercício de throttling do 3.8 (o guia cria e apaga uma fila descartável `sbq-rajada`).

## 9. Scripts (PowerShell 7; todos com ajuda `Get-Help`, `-?`, saída UTF-8)

> Refinado na Atualização 2, A2.4.

- `deploy.ps1 -Cenario <0..7> [-Pronto] [-Sufixo <4-6>] [-Location brazilsouth] [-Plano Flex|Consumption] [-EmailAlerta <email>] [-SemPublicar]`: `az account show`; resolve o arquivo de parâmetros (seção 4); gera ou reaproveita os segredos; `az group create`; `az deployment group what-if` e `create`; `dotnet publish -c Release` dos dois apps + `az functionapp deployment source config-zip`; recria a fila de confirmações se a mudança for imutável (só com `-Pronto`); grava `scripts/.env.local`; imprime a tabela de recursos; roda `smoke-test.ps1` no fim do cenário 0. Atribui ao aluno logado as roles da seção 2 (`az ad signed-in-user show`).
- `limpar.ps1 [-Sufixo]`: apaga o `rg-saldaro-lab` e purga o Key Vault (soft-delete).
- `check-drift.ps1`: lista as filas e propriedades reais do namespace e compara com o declarado no Bicep; reporta extra, faltante e divergente; sai com código 1 se houver drift.
- `erp-simulado.ps1 -Faturas <n> [-Cliente cliente-a|cliente-b] [-ReqPorMin <n>] [-Paralelismo 10] [-SemBackoff] [-IdempotencyKey] [-Caos "latencia=45;duplicar=3"] [-Direto]`: envia para `{SALDARO_APIM_URL}/faturas` (ou direto ao hub com `-Direto`) com header `x-cliente-id`; `-Caos` vira o campo `caos` do corpo (`;` → `&`); backoff exponencial com jitter, teto de 5 tentativas e 30 s, honrando `Retry-After` limitado ao teto; `-SemBackoff` reenvia imediatamente (a "metralhadora"); `-IdempotencyKey` gera uma chave por fatura e a reusa nos reenvios; timeout HTTP do cliente 35 s; relatório final com contagens 201/202/429/5xx/timeout, média de tentativas e tempo total; CSV em `scripts/saida/`.
- `smoke-test.ps1 [-TimeoutSegundos 120]`: 1 fatura ponta a ponta; falha se não chegar a `baixada`/`notificada` no prazo; imprime o `faturaId` e o `operation_Id`.
- `triagem-dlq.ps1 [-Fila sbq-confirmacoes-banco] [-Reprocessar] [-DescartarMotivo <m>]`: atalho para `dotnet run --project 03_Aplicações/Saldaro.Ferramentas -- triagem ...` com o namespace lido do `.env.local`.

`scripts/.env.local` (gravado pelo `deploy.ps1`, ignorado pelo git, **sem segredos**), formato `CHAVE=valor`: `SALDARO_RG`, `SALDARO_SUFIXO`, `SALDARO_LOCATION`, `SALDARO_SUBSCRIPTION`, `SALDARO_APIM_URL`, `SALDARO_HUB_URL`, `SALDARO_BANCO_URL`, `SALDARO_SB_NAMESPACE`, `SALDARO_SB_FQDN`, `SALDARO_STORAGE`, `SALDARO_KV`, `SALDARO_APPI`.

## 10. Telemetria e KQL

> Refinado na Atualização 2, A2.9.

Eventos customizados do hub (`TelemetryClient.TrackEvent`, propriedades entre parênteses): `fatura.recebida` (faturaId, clienteId, modo = sync|async), `cobranca.emitida` (faturaId, idCobranca), `confirmacao.recebida` (faturaId, idEvento, tipo), `confirmacao.duplicada.ignorada` (faturaId, idEvento), `fatura.baixada` (faturaId, idEvento, clienteId), `mensagem.deadletter` (faturaId, idEvento, motivo), `mensagem.retry.agendado` (faturaId, idEvento, tentativa, atrasoSegundos), `notificacao.enviada` (faturaId), `conciliacao.heartbeat` (faturasVerificadas, divergencias, duracaoMs).

Consultas em `kql/` (escopo do recurso Application Insights: `customEvents`, `requests`, `exceptions`, `dependencies`; cada arquivo comenta o equivalente `App*` do Log Analytics): `heartbeat-ultimo.kql`, `heartbeat-ausente-alerta.kql`, `baixa-duplicada.kql` (`fatura.baixada` com mais de uma ocorrência por `faturaId`), `erros-transientes-por-operacao.kql`, `429-por-cliente.kql` (requests do APIM com `resultCode == "429"` por `x-cliente-id`), `dlq-por-motivo.kql`.

## 11. Narrativa e números dos incidentes (guias, slides e notes)

> **Substituído na Atualização 2, A2.11**: a prova do Incidente 1 é por `referenciaErp` (não `quantidade = 2` num `faturaId`) e o Incidente 3 usa duas rajadas separadas.

- **Incidente 1 (3.2)**: reproduz com `-Caos "latencia=45"`: o APIM corta em 30 s (504), o ERP reenvia, o banco já tinha registrado, e `GET /api/cobrancas/{faturaId}` no banco mostra `quantidade = 2`. O limite de 230 s do Functions é citado como teto da plataforma, não como a demonstração. Prova: 202 em < 500 ms com `latencia=60`; 1.000 faturas com `-Paralelismo 20` sem 5xx; `quantidade = 1` por fatura; mesma `Idempotency-Key` → mesmo `faturaId`.
- **Incidente 2 (3.3)**: `-Caos "duplicar=3;embaralhar=true"` e, separado, um POST forjado sem assinatura direto em `/saldaro/webhooks/banco` (no baseline é aceito). A concorrência de 4 chamadas no worker faz o papel de "dois consumidores disputando" (sem precisar escalar instâncias). Prova com KQL `baixa-duplicada.kql` vazio e `quantidadeBaixas = 1`.
- **Incidente 3 (3.4)**: quem mudou o formato do `valor` foi **o banco parceiro** numa atualização da API (não o ERP): `-Caos "valorComVirgula=true;falhaTransiente=30"`. Prova com a DLQ vazia após a triagem.
- Nomes de recurso nos slides e notes com `-<sufixo>` onde o contrato exige.

## 12. Convenções de código e verificação

- C#: `net10.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `Microsoft.Azure.Functions.Worker` 2.x, `Microsoft.Azure.Functions.Worker.Sdk` 2.x, extensões `Http.AspNetCore`, `ServiceBus` 5.x, `Storage.Queues`, `Timer`, `Microsoft.Azure.Functions.Worker.ApplicationInsights`, `Microsoft.ApplicationInsights.WorkerService`, `Azure.Data.Tables`, `Azure.Messaging.ServiceBus`, `Azure.Messaging.EventGrid`, `Azure.Identity`, `Microsoft.Extensions.Azure`. `Program.cs` com `FunctionsApplication.CreateBuilder(args)`, `ConfigureFunctionsWebApplication()`, Application Insights configurado.
- Lógica pura (assinatura, máquina de estados, classificação de erro, idempotência) separada das Functions para ser testável sem Azure; testes xUnit verdes.
- Comentários didáticos curtos em pt-BR (`// CONCEITO: ...`) nos pontos que os guias citam.
- Nada de segredo em código; `local.settings.json.example` com placeholders.
- Verificação mínima de cada entregável: `dotnet build` e `dotnet test` dos projetos; `az bicep build --file infra/main.bicep --stdout` sem erros (não commitar o JSON gerado); scripts sem erro de parse (`[System.Management.Automation.Language.Parser]::ParseFile`); varredura de termos limpa.

## Atualização 2 (2026-10-14): o que a execução refinou

Consolida as decisões tomadas durante a produção dos tickets #2 a #12 e conferidas na integração (#13). Onde divergir do texto acima, vale esta seção. O código, o Bicep e os scripts já seguem o que está aqui; os detalhes de cada aplicação estão nos READMEs de `03_Aplicações/` e em `infra/README.md`.

### A2.1 Estrutura e verificação (seções 1 e 12)

- `03_Aplicações/Saldaro.sln` (formato `.sln`) reúne os seis projetos; a verificação passa a ser `dotnet build` e `dotnet test` na solução (na integração: 0 avisos; 161 testes no hub, 152 no banco e 111 nas ferramentas).
- `kql/faturas-por-referencia-erp.kql` entra na lista de consultas (prova do Incidente 1).
- Roteiro do primeiro deploy real, com o que nenhum agente pôde verificar sem Azure: `docs/research/2026-10-14-checklist-primeiro-deploy.md`.
- Pacotes fixados: `Microsoft.Azure.Functions.Worker` 2.52.0, `Worker.Sdk` 2.1.0, `Extensions.Http.AspNetCore` 2.1.1, `Extensions.ServiceBus` 5.24.0, `Extensions.Storage.Queues` 5.5.5 (banco), `Extensions.Timer` 4.3.1, `Worker.ApplicationInsights` 2.51.0, `Microsoft.ApplicationInsights.WorkerService` 2.23.0 (a 3.x não é compatível com o `Worker.ApplicationInsights` 2.51.0), `Azure.Data.Tables` 12.13.0. O SDK clássico do Application Insights se aposenta em 31/03/2027; a migração para OpenTelemetry fica num ponto só (`ITelemetria`).

### A2.2 Recursos (seção 2)

- Storage: containers `deploy-hub` e `deploy-banco` (só Flex), com o pacote autenticado pela connection string do próprio `AzureWebJobsStorage` (`storageAccountConnectionStringName`), sem app setting extra.
- Flex Consumption dimensionado para a cota de 15 núcleos do Free Trial: `instanceMemoryMB = 512`, `maximumInstanceCount = 10` no hub e `5` no banco (por grupo de escala; ~13 núcleos no pior caso). No plano B (Windows Consumption, Y1), `functionAppScaleLimit` igual.
- Key Vault com soft-delete de 7 dias e purge protection desligada (no lab; em produção, ligada). O `limpar.ps1` purga o Key Vault e também o APIM, que fica 48 h em soft-delete.
- APIM com publisher fictício (`Saldaro Pagamentos S.A.`, `lab@saldaro.example`), logger com o nome do App Insights e o diagnóstico `applicationinsights` ("All APIs"): amostragem fixa de 100%, `httpCorrelationProtocol: 'W3C'`, `x-cliente-id` nos headers de requisição do frontend e do backend, `Retry-After` nos de resposta, sem IP do cliente.
- `alrt-heartbeat-ausente`: regra de log a cada 5 min, janela de 15 min, `timeAggregation: 'Count'` sem `metricMeasureColumn`, `LessThan 1`, `autoMitigate: true`. `alrt-dlq-confirmacoes`: `Maximum` de `DeadletteredMessages` > 0 com a dimensão `EntityName = sbq-confirmacoes-banco`, a cada 1 min em janela de 5 min. O e-mail é o `-EmailAlerta` ou o da conta logada; do disparo ao e-mail, contar de 15 a 25 min.
- No plano B (Windows Consumption), o `WEBSITE_CONTENTAZUREFILECONNECTIONSTRING` também carrega a chave do storage, o mesmo compromisso do `AzureWebJobsStorage`; no Flex, o pacote de deploy reaproveita o `AzureWebJobsStorage`.
- Tags em todos os recursos: `projeto = saldaro-hub`, `ambiente = lab`.
- Outputs a mais no `main.bicep`: `apimApiWebhooksName`, `apimOperacoes`, `apimSubscriptionsHabilitadas`, `apimSubscriptions` (nomes, nunca chaves), `filasDeclaradas` e `namespaceDeclarado` (lidos pelo `check-drift.ps1`), `toggles`, `tabelas`, `filaStorageBanco`, `sufixo`, `location`, `plano` e os nomes dos planos e dos alertas.

### A2.3 APIM (seção 3, além da Atualização 1)

- `displayName` das operações = os nomes do contrato (`post-fatura`, `get-fatura`, `get-fatura-status`, `post-webhook-banco`); APIs "Saldaro Hub - faturas" e "Saldaro Hub - webhooks"; subscriptions com `displayName` `cliente-a` e `cliente-b`.
- As duas APIs têm, além do `<forward-request timeout="30" />`, um `on-error` que transforma o timeout do backend (`context.LastError.Source == "forward-request"` e `context.LastError.Reason == "Timeout"`) em **504** com `{"erro":"o gateway esperou 30 s e desistiu"}`. Sem ele, o APIM devolveria 500. Os demais erros (como o 429) seguem o padrão.
- A política de `post-fatura` é sempre declarada (com ou sem `rate-limit`), para o limite sair de verdade num redeploy incremental. A API `saldaro-webhooks` aponta para `{hubBaseUrl}/webhooks`.

### A2.4 Parâmetros e scripts (seções 4 e 9)

- Cada `.bicepparam` é autocontido (`using '../main.bicep'`, sem `extends`), com uma linha por toggle, sempre na mesma ordem. Os outros parâmetros vêm de variáveis de ambiente que o `deploy.ps1` exporta só para o próprio processo e apaga no fim: `SALDARO_SUFIXO`, `SALDARO_LOCATION`, `SALDARO_PLANO`, `SALDARO_PRINCIPAL_ID_ALUNO`, `SALDARO_EMAIL_ALERTA`, `SALDARO_WEBHOOK_SECRET` e `SALDARO_BANCO_API_KEY`. Sem elas, o arquivo compila com o sufixo marcador `troque`, que o script recusa.
- `deploy.ps1 ... [-SomenteValidar]`: o novo `-SomenteValidar` compila o Bicep e roda o what-if sem criar, alterar, apagar ou publicar nada (sem o resource group, o what-if é pulado).
- Comportamento do `deploy.ps1`: mostra a assinatura ativa; reaproveita sufixo, região e plano do ambiente existente (ou do `.env.local`); num ambiente novo, gera o sufixo e confere os nomes globais; confere o Flex na região e, se não houver, para indicando `-Plano Consumption`; roda o what-if em `ResourceIdOnly` (sem valores, porque os app settings do baseline têm segredos) e segue sem pedir confirmação; sem `-Pronto`, usa o arquivo de parâmetros **como está no disco** (os guias editam uma linha e redeployam) e, numa mudança imutável na `sbq-confirmacoes-banco`, para e explica o caminho didático; só com `-Pronto` apaga e recria a fila; remove os alertas que sobraram quando o estado tem `alertas = false`; reaplica a lista inteira de app settings (um setting criado à mão no Portal some no próximo deploy); com `identidadeGerenciada`, espera as Key Vault references do hub (`config/configreferences/appsettings`) e reinicia o app se preciso; no cenário 0, roda o smoke test.
- **2026-10-14, sobras em soft-delete:** o `deploy.ps1` deixa de purgar sozinho o Key Vault e o APIM. Num ambiente novo, se o `kv-saldaro-<sufixo>` ou o `apim-saldaro-<sufixo>` estiver em soft-delete, ele para antes de criar qualquer recurso e manda rodar `pwsh ./scripts/limpar.ps1 -Sufixo <sufixo>` ou repetir o comando com o novo switch `-PurgarExcluidos`, que purga a sobra e segue. O `-SomenteValidar` nunca purga, nem com `-PurgarExcluidos`. Por padrão, só o `limpar.ps1` purga.
- `limpar.ps1 [-Sufixo] [-SemConfirmacao]`: pede "sim", apaga o `rg-saldaro-lab`, purga o Key Vault e o APIM ("não encontrado" conta como sucesso) e apaga o `.env.local`; com `-Sufixo` e o RG já apagado, só purga as sobras.
- `check-drift.ps1 [-Sufixo]`: roda sem parâmetros (lê o `.env.local`), compara com os outputs `filasDeclaradas` e `namespaceDeclarado` do último deployment bem-sucedido e sai com 0 (sem drift), 1 (drift) ou 2 (não conseguiu comparar).
- `erp-simulado.ps1`, além do que a seção 9 lista: `-MaxTentativas <1..10>` (padrão 5, contando a primeira) e `-ComChave`; sem `-ComChave`, detecta sozinho o 401 de subscription com uma consulta sem efeito (status de fatura inexistente) e só então busca a chave do `-Cliente`; cada fatura tem uma `referenciaErp` única (`ERP-<yyyyMMddHHmmss>-<n>`), repetida nos reenvios com o mesmo corpo; relatório final por fatura e CSV `scripts/saida/erp-<yyyyMMdd-HHmmss>.csv` com as colunas `referenciaErp,faturaId,cliente,tentativas,statusFinal,codigos,duracaoMs,idempotencyKey` (`codigos` separados por `|`, com `timeout` ou `erro-rede` quando não houve resposta HTTP; `faturaId` vazio quando nenhuma tentativa devolveu id); o backoff usa equal jitter sobre 1, 2, 4 e 8 s, respeita `max(espera, Retry-After + jitter)` e nunca passa de 30 s; a chave da subscription aparece só mascarada (`****` e os 4 últimos caracteres); sai com 0 só se todas as faturas foram aceitas (201 ou 202).
- `smoke-test.ps1 [-TimeoutSegundos 120]`: aquece os dois apps pelo `/api/saude` e mostra os toggles; aceita 201 ou 202; envia um `traceparent` W3C gerado por ele (por isso já sabe o `operation_Id` do POST); usa a chave da subscription `cliente-a` quando a API exige; segue o 303 do status; imprime `faturaId`, `referenciaErp`, tempo total, o `operation_Id` do POST e as consultas KQL por `faturaId`; sai com 1 se a fatura não chegar a `baixada`/`notificada` no prazo.
- `triagem-dlq.ps1` devolve o código de saída do console (A2.8). `varredura-termos.ps1 [-Lista <caminho>]` aceita a lista fora do checkout (worktrees).

### A2.5 Hub (seção 5)

- **Corpo do `POST /faturas`**: `{ valor, vencimento, pagadorCnpj, referenciaErp?, callbackUrl?, caos? }`. `valor` (número JSON > 0) e `vencimento` obrigatórios; o `faturaId` é sempre um GUID gerado pelo hub e o `clienteId` vem do `x-cliente-id`. 400 `{ erro, detalhes }`. Baseline: 201 com `Location: {UrlBasePublica}/faturas/{id}`; falha do banco dentro da requisição → 502 `{ faturaId, status, erro }`. A `Idempotency-Key` vale por `clienteId`; repetida, devolve o mesmo `faturaId` com o status atual, sem gravar nem enfileirar.
- `GET /api/faturas/{id}` devolve todas as colunas da seção 5.4 em camelCase. `GET .../status` devolve 404 para fatura inexistente; o ERP simulado e o smoke test sondam o hub com o id `00000000-0000-0000-0000-000000000000`.
- `WebhookBanco`: 202 sem corpo; 401 `{ erro }`; 400 sem `idEvento`, `faturaId` ou `tipo`; 413 acima de 256 KB.
- `Saude`: `{ "versao": "1.0.0", "toggles": { AsyncRequestReply, WebhookHmac, Inbox, DlqComMotivo, RetryAgendado, Heartbeat, WorkerComSessao, IdentidadeGerenciada } }` (oito booleanos; `WorkerComSessao` vem do setting `AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled`; `IdentidadeGerenciada` é `true` quando só existe a forma `__fullyQualifiedNamespace`).
- Quem emite cada evento: `fatura.recebida` no `PostFatura`; `cobranca.emitida` no `PostFatura` síncrono ou no `EmitirCobranca`; `confirmacao.recebida` no `WebhookBanco`, logo depois de enfileirar (também no baseline).
- Baseline síncrono: o `PostFatura` não repassa o token da requisição para a chamada ao banco nem para a gravação da cobrança. Depois do 504 do APIM, ele segue esperando o banco (até os 120 s do `HttpClient`) e grava a cobrança; um `TaskCanceledException` nesse caminho só pode ser o timeout do `HttpClient` e vira 502.
- O `EmitirCobranca` não chama o banco de novo se a fatura já tem `idCobranca`. Confirmação ou `FaturaBaixada` de fatura inexistente: completa sem efeito, com aviso no log.
- `NotificarParceiro` com `Cenario:Inbox = true`: completa sem callback o `FaturaBaixada` de fatura já `notificada` e emite `notificacao.duplicada.ignorada` (Atualização 1).
- Log estruturado dos workers de confirmação (`traces`), uma linha por mensagem: `Confirmação {Tipo} da fatura {FaturaId}: SessionId={SessionId} SequenceNumber={SequenceNumber} DeliveryCount={DeliveryCount} MessageId={MessageId}`, mais `SessionLockedUntil` no worker com sessão.
- **5.3, lógica de baixa**: em toda falha sem conserto, os triggers de Service Bus fazem `AbandonMessageAsync` explícito e relançam a exceção (reentrega imediata; a execução aparece como falha); se a mensagem já tem destino definido (complete, abandon, dead-letter ou defer), não há segundo abandon (`Aplicacao/AbandonExplicito.cs`). Ao gravar `cobranca-emitida`, o hub só avança se a fatura ainda está `recebida` (com ETag): o webhook pode chegar antes da resposta de um banco lento. Com a inbox, `cobranca.paga` baixa a partir de `recebida`, `cobranca-emitida` ou `registrada`, e `cobranca.registrada` vale a partir de `recebida` ou `cobranca-emitida`; a inbox registra o evento mesmo quando a máquina de estados o ignora (duas linhas por fatura: a `registrada` e a `paga`). Schema: `idEvento`, `faturaId`, `tipo` e `valor` (número JSON) obrigatórios; ids com até 128 caracteres, sem `/ \ # ?`.
- Exceções lançadas de propósito (coluna `type` em `exceptions`): `ConfirmacaoInvalidaException`, `FalhaTransienteSimuladaException` (a mensagem começa por "Falha transiente simulada", que é o que o `kql/erros-transientes-por-operacao.kql` procura), `BancoIndisponivelException`, `BancoRespostaInesperadaException` e `ConflitoDeConcorrenciaException`.
- 5.4: `valor` é `decimal` no código e `double` na coluna; a tabela `conciliacao` grava `qtdRecebida`, `qtdCobrancaEmitida`, `qtdRegistrada`, `qtdBaixada`, `qtdNotificada`, `faturasVerificadas`, `divergencias` e `duracaoMs`; o heartbeat sai depois de gravar o resumo. O hub cria as tabelas se não existirem (para o Azurite).
- 5.5: `host.json` com `sessionIdleTimeout = 00:00:10` (o padrão de 1 min prenderia cada sessão ociosa ao vivo).

### A2.6 Banco simulado (seção 6)

- `GET /api/cobrancas/{faturaId}` e `GET /api/saude` são anônimos; só o `POST /api/cobrancas` exige `x-api-key`. Fatura sem cobrança devolve 200 com `quantidade = 0`, nunca 404. `Saude`: `{ servico, status, versao, settingsPreenchidos }`.
- Os webhooks ficam visíveis só a partir do instante `R` em que o banco responde (POST + `latencia`): `cobranca.registrada` em R + 1 s e `cobranca.paga` em R + `atrasoWebhook` (faixa 2 a 3600 s); com `embaralhar`, `cobranca.paga` em R + 1 s e `cobranca.registrada` em R + `atrasoWebhook` + 3 s. O banco nunca confirma antes de responder.
- `valorComVirgula` vale para os dois eventos. O 503 do `falhaTransiente` vem com `Retry-After: 1`. Botões aceitam `&` ou `;`, sem diferenciar maiúsculas; valor inválido volta ao padrão e fora da faixa é limitado, sempre com aviso no log.
- `EnviarWebhook`: 2xx completa; 400, 401 e demais 4xx (exceto 408 e 429) registram aviso e completam sem reenviar; 5xx, 408, 429, timeout (30 s) ou falha de rede fazem a Function falhar para o Storage Queue reentregar (10 s entre tentativas, 5 no total, depois `banco-pagamentos-poison`). Cada nova tentativa assina de novo. Mensagens da fila em Base64 nos dois lados.
- A linha digitável usa o código de banco `000` e o "QR code" é um texto marcado `PIX-FICTICIO`.

### A2.7 Dead-letter (seção 7.4)

- Motivos da plataforma: o Microsoft Learn lista cinco, `MaxDeliveryCountExceeded`, `TTLExpiredException`, `HeaderSizeExceeded`, `Session ID is null` e `MaxTransferHopCountExceeded`; no kit só os dois primeiros aparecem. A DLQ não observa TTL nem se limpa sozinha.
- O `kql/dlq-por-motivo.kql` só enxerga os dead-letters feitos pelo hub (evento `mensagem.deadletter`); os da plataforma se veem na triagem, no Service Bus Explorer ou na métrica `DeadletteredMessages`.

### A2.8 Ferramentas (seção 8)

- Códigos de saída do console: 0 sucesso, 1 falha de execução, 2 uso inválido, 3 acesso negado (401/403, com a role que faltou).
- `triagem`: a tabela tem as colunas `Motivo (DeadLetterReason)`, `Qtde`, `Corrigíveis`, `Sem correção`, `Não corrigíveis`, `Mais antiga` e `Mais recente`, com exemplos; o `--reprocessar` termina com o resumo `Reprocessadas`, `Descartadas`, `Mantidas na DLQ` e `Falhas de envio`.
- `legado-auto-create`: `--mensagens` padrão 20; sem o direito Manage no namespace, falha com 401 e código 3.
- `rajada`: padrão 50 remetentes × 100 mensagens, uma mensagem de aquecimento e até 3 retentativas do SDK; basta `Azure Service Bus Data Sender`; recusa as três filas do hub com código 2 (os workers consumiriam as mensagens de teste): só fila descartável, como a `sbq-rajada` do guia 3.8.

### A2.9 Telemetria e KQL (seção 10)

- `referenciaErp` também em `fatura.recebida` e `cobranca.emitida`; evento novo `notificacao.duplicada.ignorada` (`faturaId`, `idEvento`).
- Correlação: o `faturaId` é a correlação de negócio (está em todos os eventos, nos logs e no `SessionId`). O `operation_Id` não atravessa a jornada inteira (a fila de storage do banco e o Event Grid iniciam operações novas) e não serve de prova; a tela do Application Insights se chama **Search** (antigo Transaction search).
- Todo KQL começa pela tabela e aceita um filtro `| where timestamp > datetime(...)` logo abaixo da primeira linha. O `429-por-cliente.kql` agrupa por subscription (`customDimensions["Subscription Name"]`) e pelo header (`customDimensions["Request-x-cliente-id"]`); essas chaves do diagnóstico do APIM vêm de exemplos publicados e precisam ser conferidas no primeiro deploy.

### A2.10 Rótulos do Portal que os guias usam (conferidos no Microsoft Learn; o primeiro deploy confirma)

- Application Insights: **Investigate → Search** (antigo Transaction search) e **End-to-end transaction details**.
- Function App: **Settings → Environment variables** (trocar um app setting reinicia o app); **Disable** na Overview da função.
- APIM: as APIs aparecem pelo `displayName` ("Saldaro Hub - faturas" e "Saldaro Hub - webhooks"); o diagnóstico fica em **APIs → All APIs → Settings → Diagnostics Logs**; todo APIM tem a "Built-in all-access subscription".
- Service Bus: **Enable sessions** e **Enable duplicate detection** só aparecem no painel de criação da fila (**+ Queue**); numa fila existente, **Overview → Essentials** mostra **Sessions** e **Duplicate detection** como "Disabled", sem nada para mudar (no primeiro deploy real, em 2026-10-14, **Settings → Properties** já era uma página genérica de metadados do ARM); no Service Bus Explorer, **Settings → Access Key** ou **Microsoft Entra ID** (com `disableLocalAuth`, só Entra ID), **Peek Mode → Peek from start**; o Learn diz que o Explorer não suporta sessions, mas no primeiro deploy o peek da DLQ de uma fila com sessions funcionou, e o `triagem-dlq.ps1` segue como o caminho que agrupa por motivo.

### A2.11 Narrativa e números (seção 11)

- **Lab 0**: a prova é o `smoke-test.ps1` (fatura em `baixada` ou `notificada` em até 2 min) e a consulta por `faturaId`; não se promete o mesmo `operation_Id` nas Functions.
- **Incidente 1 (3.2)**: `erp-simulado.ps1 -Faturas 1 -MaxTentativas 2 -Caos "latencia=45"`. O APIM corta em 30 s com 504 (o `on-error`), o ERP reenvia a mesma `referenciaErp`, o hub cria **outro `faturaId`** e o banco registra outra cobrança. Por isso a prova é por `referenciaErp` (`kql/faturas-por-referencia-erp.kql` e o relatório/CSV do ERP), e não `quantidade = 2` num `faturaId`: o `GET /api/cobrancas/{faturaId}` mostra `quantidade = 1` em cada uma. O baseline não desiste quando o ERP desiste: o caminho síncrono não repassa o token da requisição ao banco, então cada tentativa cortada ainda grava `idCobranca` e emite `cobranca.emitida` aos 45 s, e a KQL mostra `faturas = 2` e `cobrancas = 2`. Prova do conserto: 202 em < 500 ms com `latencia=60`; `-Faturas 1000 -Paralelismo 20 -IdempotencyKey` sem 5xx; `quantidade = 1` por fatura; mesma `Idempotency-Key` → mesmo `faturaId`; a KQL fica vazia. O limite de 230 s do Functions só é citado.
- **Incidente 2 (3.3)**: `erp-simulado.ps1 -Faturas 20 -Caos "duplicar=3;embaralhar=true"` e, separado, um POST forjado sem assinatura em `{apimBaseUrl}/webhooks/banco` (aceito no baseline, 401 depois do HMAC). São 120 webhooks (20 faturas × 2 eventos × 3 cópias); no baseline, cada fatura tem 3 `fatura.baixada` e volta para `registrada` com `baixadaEm` preenchido. Os dois consumidores disputando são as 4 chamadas concorrentes do worker (`maxConcurrentCalls = 4`), sem escalar instâncias. O segredo do HMAC é app setting em texto até o 3.5. Prova: `kql/baixa-duplicada.kql` vazio, `quantidadeBaixas = 1` e as 20 faturas `notificadas`, com duas linhas de inbox cada.
- **Incidente 3 (3.4)**: quem mudou o formato do `valor` foi o banco parceiro, numa atualização da API. Duas rajadas separadas: `erp-simulado.ps1 -Faturas 10 -Caos "valorComVirgula=true"` e `erp-simulado.ps1 -Faturas 20 -Caos "falhaTransiente=50"`. No baseline, a DLQ junta ~30 mensagens, todas `MaxDeliveryCountExceeded` (as 20 envenenadas e ~10 boas). Conserto: `Cenario__DlqComMotivo` e `Cenario__RetryAgendado` no Portal e `deploy.ps1 -Cenario 4 -Pronto -SemPublicar` (o `maxDeliveryCount` vai de 10 para 3 sem recriar a fila). Retry agendado com 5 s × 2^n (10, 20 e 40 s); com as mesmas rajadas, 20 `SchemaInvalida` na primeira entrega e nenhuma `TentativasEsgotadas`. Com `falhaTransiente=50`, uma fração pequena das faturas (~2% por rajada) pode esgotar as 10 entregas na `sbq-faturas-recebidas`; a recuperação é a triagem dessa fila. Prova: a DLQ vazia depois da triagem.
