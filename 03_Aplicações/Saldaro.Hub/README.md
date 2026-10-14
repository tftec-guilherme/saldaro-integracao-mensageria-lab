# Saldaro.Hub

**Version-anchor:** Q4-2026 · Azure Functions, .NET 10 isolated worker

Azure Functions em .NET 10, modelo isolated worker: o hub de cobrança da Saldaro Pagamentos S.A. (empresa fictícia). O código nasce **ingênuo de propósito** (o baseline dos incidentes) e cada conserto é ligado por app setting, sem republicar: o aluno reproduz o incidente, muda o setting e prova o conserto lendo no código o caminho que o toggle liga. Procure por `TOGGLE Cenario:` e `CONCEITO:` nos arquivos.

Fonte de verdade de nomes e comportamentos: `docs/research/2026-10-13-contrato-do-kit.md` (seções 5, 7 e 10).

## Functions

| Function | Trigger / rota | Arquivo |
|---|---|---|
| `PostFatura` | HTTP POST `/api/faturas` | `Functions/PostFatura.cs` (lógica em `Aplicacao/ServicoDeFaturas.cs`) |
| `GetFatura` | HTTP GET `/api/faturas/{id}` | `Functions/GetFatura.cs` |
| `GetFaturaStatus` | HTTP GET `/api/faturas/{id}/status` | `Functions/GetFaturaStatus.cs` |
| `EmitirCobranca` | Service Bus `sbq-faturas-recebidas` | `Functions/EmitirCobranca.cs` |
| `WebhookBanco` | HTTP POST `/api/webhooks/banco` | `Functions/WebhookBanco.cs` |
| `ProcessarConfirmacaoSemSessao` | Service Bus `sbq-confirmacoes-banco`, sem sessions | `Functions/ProcessarConfirmacaoSemSessao.cs` |
| `ProcessarConfirmacaoComSessao` | Service Bus `sbq-confirmacoes-banco`, `IsSessionsEnabled = true` | `Functions/ProcessarConfirmacaoComSessao.cs` |
| `NotificarParceiro` | Service Bus `sbq-notificacoes-parceiro` | `Functions/NotificarParceiro.cs` |
| `Conciliacao` | Timer `0 */5 * * * *` (UTC) | `Functions/Conciliacao.cs` (lógica em `Aplicacao/ServicoDeConciliacao.cs`) |
| `Saude` | HTTP GET `/api/saude` | `Functions/Saude.cs` |

Os dois workers de confirmação compartilham a lógica de baixa da seção 5.3 em `Aplicacao/ProcessadorDeBaixa.cs`. Todos os triggers de Service Bus usam `AutoCompleteMessages = false` e `ServiceBusMessageActions`. Os HTTP triggers são anônimos (simplificação de laboratório: a porta pública é o APIM).

## Toggle → o que muda → cenário

| App setting (parâmetro do Bicep) | O que muda | Onde ler no código | Cenário |
|---|---|---|---|
| `Cenario__AsyncRequestReply` (`asyncRequestReply`) | `false`: o `PostFatura` grava a fatura, chama o banco dentro da requisição e responde **201**. `true`: grava `recebida`, enfileira em `sbq-faturas-recebidas` e responde **202** com `Location: {UrlBasePublica}/faturas/{id}/status` e `Retry-After: 5`; honra `Idempotency-Key`; quem chama o banco é o `EmitirCobranca` | `Aplicacao/ServicoDeFaturas.cs`, `Functions/PostFatura.cs` | 3.2 |
| `Cenario__WebhookHmac` (`webhookHmac`) | `true`: o `WebhookBanco` valida o HMAC-SHA256 do corpo bruto e a janela de ±300 s; inválido → **401** e nada vai para a fila. `false`: aceita qualquer POST (a confirmação forjada passa) | `Functions/WebhookBanco.cs`, `Dominio/AssinaturaWebhook.cs` | 3.3 |
| `Cenario__Inbox` (`inbox`) | `true`: baixa com inbox (`inbox`), máquina de estados tolerante a desordem e ETag; efeitos só na transição real; `NotificarParceiro` ignora `FaturaBaixada` repetido. `false`: cada `cobranca.paga` baixa e publica de novo, e um `cobranca.registrada` atrasado "desbaixa" a fatura | `Aplicacao/ProcessadorDeBaixa.cs`, `Dominio/MaquinaDeEstados.cs`, `Aplicacao/ConcorrenciaOtimista.cs`, `Functions/NotificarParceiro.cs` | 3.3 |
| `AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled` e `AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled` (`workerComSessao`) | qual dos dois workers de confirmação escuta a fila (só um por vez) | `Functions/ProcessarConfirmacao*.cs` | 3.3 |
| `Cenario__DlqComMotivo` (`dlqComMotivo`) | `true`: schema inválido (ex.: `"valor": "1.250,00"`) vai para a DLQ na primeira entrega com `DeadLetterReason = SchemaInvalida`. `false`: abandon + exceção, reentrega até `maxDeliveryCount` e DLQ com `MaxDeliveryCountExceeded` | `Aplicacao/ProcessadorDeBaixa.cs`, `Dominio/ClassificadorDeConfirmacao.cs` | 3.4 |
| `Cenario__RetryAgendado` (`retryAgendado`) | `true`: falha transiente vira cópia agendada (`MessageId = {idEvento}-t{n}`, propriedade `tentativa`, atraso 5 s × 2^n = 10/20/40 s) e a original é completada; passou de 3, DLQ com `TentativasEsgotadas`. `false`: abandon + exceção (as 10 reentregas acabam antes dos 45 s da falha) | `Aplicacao/ProcessadorDeBaixa.cs`, `Dominio/PoliticaDeRetry.cs` | 3.4 |
| `Cenario__Heartbeat` (`heartbeat`) | `true`: a `Conciliacao` emite `conciliacao.heartbeat` como **último** passo, depois de gravar o resumo; se algo falhar antes, não há heartbeat | `Aplicacao/ServicoDeConciliacao.cs` | 3.6 |
| `ServiceBusConnection__fullyQualifiedNamespace`, `TablesConnection__serviceUri`, sem `EventGridTopicKey`; `BancoApiKey` e `WebhookSecret` como referências do Key Vault (`identidadeGerenciada`) | os clientes passam a autenticar com `DefaultAzureCredential` (identidade gerenciada). Nenhuma linha de código muda: o registro aceita as duas formas | `Configuracao/RegistroDeServicos.cs` | 3.5 |

Os parâmetros `filaComSessions`, `filaDupDetection` e `maxDeliveryCount` mudam só a fila no Bicep (sem toggle no código). O `MessageId = idEvento` e o `SessionId = faturaId` são gravados **sempre** pelo `WebhookBanco`, inclusive no baseline; é a fila que decide se usa.

Para o desafio 3.6, o timer é desligado com o app setting `AzureWebJobs.Conciliacao.Disabled = true`.

## App settings

| Setting | Uso |
|---|---|
| `AzureWebJobsStorage` | storage do host (timer, locks); continua connection string no 3.5 |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | liga o Application Insights do worker; sem ele (local), os eventos customizados vão para o log |
| `ServiceBusConnection` ou `ServiceBusConnection__fullyQualifiedNamespace` | triggers (host) e envio de mensagens (worker) |
| `TablesConnection` ou `TablesConnection__serviceUri` | tabelas `faturas`, `inbox`, `idempotencia`, `conciliacao` |
| `EventGridTopicEndpoint` e, sem identidade, `EventGridTopicKey` | publicação do `FaturaBaixada` |
| `BancoBaseUrl`, `BancoApiKey` | `POST {BancoBaseUrl}/cobrancas?{caos}` com `x-api-key` (timeout do cliente HTTP: 120 s) |
| `WebhookSecret` | chave do HMAC do webhook (a string do setting, em UTF-8) |
| `UrlBasePublica` | base dos headers `Location` (a URL do APIM) |
| `WebhookUrlPublica` | `webhookUrl` enviado ao banco (`{apimBaseUrl}/webhooks/banco`) |
| `Cenario__*`, `AzureWebJobs.<Function>.Disabled` | toggles da tabela acima |

`local.settings.json.example` tem os placeholders. Nada de segredo no código nem no repositório.

## Contratos HTTP

- `POST /api/faturas`, header `x-cliente-id` obrigatório (vira o `clienteId`) e `Idempotency-Key` opcional. Corpo: `{ "valor": 1250.00, "vencimento": "2026-11-10", "pagadorCnpj": "<cnpj fictício>", "referenciaErp": "ERP-0001", "callbackUrl": null, "caos": "latencia=45&duplicar=3" }`. `valor` e `vencimento` obrigatórios; `referenciaErp`, `callbackUrl` e `caos` opcionais. O `faturaId` é sempre um GUID novo gerado pelo hub.
  - 400 `{ erro, detalhes }` (ex.: corpo `{}` lista `valor` e `vencimento` em `detalhes`).
  - Baseline: 201 `{ faturaId, status, idCobranca }` com `Location: {UrlBasePublica}/faturas/{id}`; se o banco falhar dentro da requisição, 502 `{ faturaId, status, erro }`.
  - Consertado: 202 `{ faturaId, status }` com `Location` do status e `Retry-After: 5`. `Idempotency-Key` repetida (por `clienteId`) devolve o mesmo `faturaId`, com o status atual, sem gravar nem enfileirar. Se gravar a fatura ou enfileirar falhar, o hub apaga a fatura, libera a chave e responde 500: o reenvio com a mesma chave recomeça do zero, com outro `faturaId`.
- `GET /api/faturas/{id}`: 200 com todas as colunas da seção 5.4 em camelCase (`faturaId`, `clienteId`, `valor`, `vencimento`, `pagadorCnpj`, `referenciaErp`, `status`, `idCobranca`, `quantidadeBaixas`, `baixadaEm`, `callbackUrl`, `caos`, `criadaEm`, `atualizadaEm`) ou 404.
- `GET /api/faturas/{id}/status`: 200 `{ faturaId, status }` enquanto não baixou; 303 com `Location: {UrlBasePublica}/faturas/{id}` em `baixada` ou `notificada`; 404 se não existe (o ERP simulado e o smoke test sondam o hub com `GET /api/faturas/00000000-0000-0000-0000-000000000000/status` e esperam 404).
- `POST /api/webhooks/banco`: 202 sem corpo; 401 `{ erro }` com o HMAC ligado e assinatura recusada; 400 sem `idEvento`, `faturaId` ou `tipo`; 413 acima de 256 KB.
- `GET /api/saude`: `{ "versao": "1.0.0", "toggles": { "AsyncRequestReply", "WebhookHmac", "Inbox", "DlqComMotivo", "RetryAgendado", "Heartbeat", "WorkerComSessao", "IdentidadeGerenciada" } }`. `WorkerComSessao` vem de `AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled`; `IdentidadeGerenciada` é `true` quando só existe `ServiceBusConnection__fullyQualifiedNamespace` (sem a connection string).

## Telemetria

Eventos customizados (`customEvents`, propriedades em `customDimensions`), todos com `faturaId`, menos o heartbeat:

| Evento | Propriedades | Quem emite |
|---|---|---|
| `fatura.recebida` | `faturaId`, `clienteId`, `modo` (`sync`/`async`), `referenciaErp` | `PostFatura` |
| `cobranca.emitida` | `faturaId`, `idCobranca`, `referenciaErp` | `PostFatura` (síncrono) e `EmitirCobranca` (assíncrono) |
| `confirmacao.recebida` | `faturaId`, `idEvento`, `tipo` | `WebhookBanco`, logo depois de enfileirar (também no baseline) |
| `confirmacao.duplicada.ignorada` | `faturaId`, `idEvento` | baixa com inbox, evento já processado |
| `fatura.baixada` | `faturaId`, `idEvento`, `clienteId` | transição para `baixada` |
| `mensagem.deadletter` | `faturaId`, `idEvento`, `motivo` | DLQ com `SchemaInvalida` ou `TentativasEsgotadas` |
| `mensagem.retry.agendado` | `faturaId`, `idEvento`, `tentativa`, `atrasoSegundos` | retry agendado |
| `notificacao.enviada` | `faturaId` | `NotificarParceiro` |
| `notificacao.duplicada.ignorada` | `faturaId`, `idEvento` | `NotificarParceiro` com inbox, `FaturaBaixada` repetido |
| `conciliacao.heartbeat` | `faturasVerificadas`, `divergencias`, `duracaoMs` | `Conciliacao`, último passo |

`referenciaErp` nos dois primeiros e o evento `notificacao.duplicada.ignorada` entraram na seção 10 do contrato pela Atualização 2 (A2.9). A prova do Incidente 1 agrupa por `referenciaErp`: no baseline, a mesma referência vira N faturas e N cobranças (cada reenvio do ERP cria um `faturaId` novo); depois do conserto, uma.

Log estruturado dos workers de confirmação (`traces`), uma linha por mensagem: `Confirmação {Tipo} da fatura {FaturaId}: SessionId={SessionId} SequenceNumber={SequenceNumber} DeliveryCount={DeliveryCount} MessageId={MessageId}` (o worker com sessão acrescenta `SessionLockedUntil`). As chaves viram colunas em `customDimensions`.

Exceções lançadas de propósito (coluna `type` em `exceptions`): `ConfirmacaoInvalidaException` (schema inválido no baseline), `FalhaTransienteSimuladaException` (transiente sem retry agendado; a mensagem sempre começa por "Falha transiente simulada", que é o que o `kql/erros-transientes-por-operacao.kql` procura), `BancoIndisponivelException` (503 do banco), `BancoRespostaInesperadaException`, `ConflitoDeConcorrenciaException`. Nos workers de fila, a exceção sai depois do abandon explícito.

Sampling desligado no `host.json` e no worker (`EnableAdaptiveSampling = false`).

### Correlação: use o `faturaId`

O `faturaId` é a correlação de negócio: está em todos os eventos customizados, nos logs e no `SessionId` das confirmações. O `operation_Id` do Application Insights não atravessa a jornada inteira e não deve ser usado como prova:

- o SDK do Service Bus grava o contexto de trace (`Diagnostic-Id`) na mensagem e o host costuma continuar o mesmo `operation_Id` no trigger da fila, mas isso depende da versão do host e da extensão no isolated worker (não verificado no Azure por este projeto);
- a jornada tem quebras certas: a fila de storage do banco (entre o `POST /cobrancas` e o webhook) e o Event Grid (entre a baixa e a `sbq-notificacoes-parceiro`) iniciam operações novas.

Se um guia quiser mostrar as Functions encadeadas, a consulta é por `customDimensions.faturaId` (eventos) ou pelo `faturaId` no texto dos `traces`, não por `operation_Id`.

## host.json

`extensions.serviceBus`: `maxConcurrentCalls = 4` (dois "consumidores" disputando a mesma fatura sem escalar instâncias), `maxConcurrentSessions = 8`, `prefetchCount = 0`, `autoCompleteMessages = false` e `sessionIdleTimeout = 00:00:10`. O padrão do `sessionIdleTimeout` é o `tryTimeout` (1 minuto): cada sessão ficaria presa 1 minuto ociosa antes de o worker passar para a próxima fatura; com 10 s as sessões drenam rápido ao vivo. Sampling do Application Insights desligado.

## Decisões onde o contrato não fixava detalhe

- **Abandon explícito**: em toda falha sem conserto, os triggers de Service Bus fazem `AbandonMessageAsync` e relançam a exceção (`Aplicacao/AbandonExplicito.cs`): reentrega imediata e a execução aparece como falha. Se a mensagem já tinha destino (complete, dead-letter), não há segundo abandon.
- **O baseline não desiste quando o ERP desiste**: o caminho síncrono não passa o token da requisição para a chamada ao banco nem para a gravação da cobrança. Depois do 504 do APIM (30 s), o `PostFatura` segue esperando o banco (até os 120 s do `HttpClient`) e grava `idCobranca`, `cobranca-emitida` e o evento `cobranca.emitida`. Assim a duplicata do Incidente 1 aparece inteira, N faturas e N cobranças por `referenciaErp`, qualquer que seja o comportamento do host com a conexão fechada. Um `TaskCanceledException` nesse caminho só pode ser o timeout do `HttpClient` e vira 502.
- **Status monotônico na emissão**: ao gravar `cobranca-emitida`, o hub só avança se a fatura ainda estiver `recebida` (com ETag). O webhook pode chegar antes da resposta de um banco lento; isso vale sempre e não é a guarda do Incidente 2.
- **Máquina de estados com inbox**: `cobranca.paga` baixa a partir de `recebida`, `cobranca-emitida` ou `registrada` (o banco lento pode confirmar antes de responder à emissão); `cobranca.registrada` vale a partir de `recebida` ou `cobranca-emitida`.
- `EmitirCobranca` não chama o banco de novo se a fatura já tem `idCobranca` (reentrega depois de uma emissão bem-sucedida).
- **Compensação da `Idempotency-Key`**: a chave é reservada antes de gravar a fatura e de enfileirar. Se um dos dois falhar, o `PostFatura` apaga a fatura e libera a reserva (melhor esforço, com um token próprio, para valer mesmo com a requisição cancelada) e relança o erro original. Nada é reenfileirado: num timeout com a mensagem já aceita, o `EmitirCobranca` acha a fatura inexistente e completa sem chamar o banco. Resíduo: se o processo cair entre a reserva e a compensação, a chave fica órfã.
- **Notificação com a inbox**: o `NotificarParceiro` testa "já `notificada`?" de novo dentro do laço com ETag. De duas entregas simultâneas do mesmo `FaturaBaixada`, só uma grava `notificada` e emite `notificacao.enviada`; a outra emite `notificacao.duplicada.ignorada`. Os dois callbacks podem sair (pelo menos uma vez): o ERP precisa ser idempotente.
- Confirmação ou `FaturaBaixada` de fatura inexistente: completa sem efeito, com aviso no log.
- Schema do evento: `idEvento`, `faturaId`, `tipo` (`cobranca.registrada` ou `cobranca.paga`) e `valor` (número JSON) obrigatórios; ids até 128 caracteres, sem `/ \ # ?`. A descrição do dead-letter é truncada em 1.024 caracteres.
- `valor` é `decimal` no código e `double` na coluna (o Table Storage não tem decimal). A tabela `conciliacao` grava as contagens como `qtdRecebida`, `qtdCobrancaEmitida`, `qtdRegistrada`, `qtdBaixada`, `qtdNotificada`, mais `faturasVerificadas`, `divergencias` e `duracaoMs`.
- O hub cria as tabelas se não existirem (`CreateIfNotExists`, uma vez por processo): o Bicep já cria no Azure; isso serve para o Azurite.
- Publicação do `FaturaBaixada`: gravar `baixada` e publicar no Event Grid são duas operações; se o processo cair entre elas, a publicação se perde. Em produção, o padrão Outbox fecha essa janela (comentado no código).

## Compilar e testar

```powershell
dotnet build "03_Aplicações/Saldaro.Hub/Saldaro.Hub.csproj"
dotnet test "03_Aplicações/Saldaro.Hub.Tests/Saldaro.Hub.Tests.csproj"
```

Os testes (xUnit, sem Azure, com dublês em memória em `Dubles.cs`) cobrem assinatura (vetor compartilhado com o banco simulado, inválida, timestamp expirado), idempotência por `Idempotency-Key` (com a compensação quando gravar ou enfileirar falha), máquina de estados, classificação de erro, cálculo do retry agendado, a lógica de baixa com e sem os toggles, ETag, conciliação, notificação, os status HTTP e a composição de DI nas duas formas de conexão. Os CNPJs de pagador dos testes são gerados por algoritmo, com dígitos verificadores válidos e nunca fixos (`CnpjDeTeste.cs`, o mesmo algoritmo dos scripts; os testes do banco simulado compilam o mesmo arquivo).

## Rodar localmente (opcional)

Não é necessário para a aula: o `deploy.ps1` publica com `dotnet publish` + `az functionapp deployment source config-zip`. Para depurar na máquina, com Azure Functions Core Tools e Azurite:

1. Copie `local.settings.json.example` para `local.settings.json` (ignorado pelo git) e preencha.
2. Os triggers de Service Bus precisam de um namespace real. Sem ele, desligue-os com `AzureWebJobs.EmitirCobranca.Disabled`, `AzureWebJobs.NotificarParceiro.Disabled` e os dois `AzureWebJobs.ProcessarConfirmacao*.Disabled` = `true`; as rotas HTTP e a `Conciliacao` funcionam com o Azurite.
3. `func start` na pasta do projeto. A `Conciliacao` pode ser disparada com `POST http://localhost:7071/admin/functions/Conciliacao` e corpo `{"input":""}`.

## Pacotes

As versões ficam num lugar só, em `03_Aplicações/Directory.Packages.props` (Central Package Management); o `.csproj` lista os pacotes sem versão.

| Pacote | Versão |
|---|---|
| `Microsoft.Azure.Functions.Worker` | 2.52.0 |
| `Microsoft.Azure.Functions.Worker.Sdk` | 2.1.0 |
| `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` | 2.1.1 |
| `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus` | 5.24.0 |
| `Microsoft.Azure.Functions.Worker.Extensions.Timer` | 4.3.1 |
| `Microsoft.Azure.Functions.Worker.ApplicationInsights` | 2.51.0 |
| `Microsoft.ApplicationInsights.WorkerService` | 2.23.0 |
| `Azure.Data.Tables` | 12.13.0 |
| `Azure.Messaging.ServiceBus` | 7.21.0 |
| `Azure.Messaging.EventGrid` | 5.0.0 |
| `Azure.Identity` | 1.21.0 |
| `Microsoft.Extensions.Azure` | 1.14.1 |

`Microsoft.ApplicationInsights.WorkerService` fica na 2.23.0 de propósito: a 3.x (baseada em OpenTelemetry) não é compatível com `Microsoft.Azure.Functions.Worker.ApplicationInsights` 2.51.0. O SDK clássico 2.x do Application Insights está depreciado e se aposenta em 31/03/2027; o caminho de migração recomendado pelo Microsoft Learn é `Microsoft.Azure.Functions.Worker.OpenTelemetry` + `Azure.Monitor.OpenTelemetry.Exporter`, e o Learn também recomenda o MSBuild SDK `Azure.Functions.Sdk` no lugar do `Microsoft.Azure.Functions.Worker.Sdk`. Os eventos customizados passam por `ITelemetria` (`Infraestrutura/TelemetriaApplicationInsights.cs`), então a troca fica num ponto só.
