# Saldaro.BancoSimulado

**Version-anchor:** Q4-2026 · Azure Functions, .NET 10 isolated worker

O banco parceiro simulado da aula. Recebe do hub o pedido de cobrança, devolve uma linha digitável e um "QR code" fictícios e, depois, manda ao hub os webhooks `cobranca.registrada` e `cobranca.paga` assinados com HMAC. Os **botões de caos** (query string) fazem o banco se comportar como um parceiro real num dia ruim: demorar, repetir, embaralhar, assinar errado, mudar o formato do valor e falhar de forma transiente.

> Empresa, banco, pessoas, números e incidentes são fictícios. O banco simulado não reproduz a API nem o formato válido de nenhum banco real: a linha digitável usa o código de banco `000` e dígitos verificadores zerados, e o "QR code" é um texto marcado como `PIX-FICTICIO`.

## Functions

| Function | Trigger / rota | O que faz |
|---|---|---|
| `PostCobranca` | HTTP POST `/api/cobrancas?{botões}` | Exige `x-api-key` igual ao setting `BancoApiKey` (401 sem). Valida o corpo (400). Sorteia o `falhaTransiente` (503 sem nenhum efeito). Grava a cobrança na tabela `cobrancas`, enfileira os webhooks em `banco-pagamentos` e **só então** espera `latencia` segundos; responde 201 `{ idCobranca, linhaDigitavel, qrCode }` |
| `EnviarWebhook` | Storage Queue `banco-pagamentos` | Lê `{ evento, webhookUrl, assinaturaInvalida }`, assina o evento no momento do envio e faz o POST no `webhookUrl`. 2xx: completa. 400, 401 e demais 4xx (exceto 408 e 429): rejeitado, log de aviso e completa sem nova tentativa. 5xx, 408, 429 ou falha de rede: a Function falha e o Storage Queue reentrega (5 tentativas no total; depois, fila `banco-pagamentos-poison`) |
| `GetCobrancasPorFatura` | HTTP GET `/api/cobrancas/{faturaId}` | `{ faturaId, quantidade, cobrancas: [...] }`: quantas cobranças o banco registrou para a fatura (a prova do Incidente 1). Fatura sem cobrança devolve 200 com `quantidade = 0`, nunca 404 |
| `Saude` | HTTP GET `/api/saude` | 200 `{ servico, status, versao, settingsPreenchidos }` (diz só se cada setting está preenchido, nunca o valor) |

Os triggers HTTP são anônimos, sem function key (simplificação de lab). Só o `POST /api/cobrancas` exige `x-api-key`; `GET /api/cobrancas/{faturaId}` e `GET /api/saude` ficam sem chave de propósito, porque os guias chamam os dois direto como prova.

## Botões de caos

Vão na query string de `POST /api/cobrancas`. O hub repassa o campo `caos` da fatura, que vem do `-Caos` do `erp-simulado.ps1` (por exemplo, `-Caos "latencia=45;duplicar=3"`). O banco aceita `&` ou `;` como separador e não diferencia maiúsculas no nome. Valor inválido volta ao padrão e valor fora da faixa é limitado; nos dois casos o banco registra um aviso no log em vez de recusar a chamada.

| Parâmetro | Padrão | Faixa | Efeito |
|---|---|---|---|
| `latencia` | `0` | 0 a 200 s | Segundos que o banco demora para **responder**, já tendo gravado a cobrança e enfileirado os webhooks |
| `atrasoWebhook` | `5` | 2 a 3600 s | Segundos até o `cobranca.paga`; o `cobranca.registrada` vai em 1 s (contados a partir da resposta, ver a linha do tempo) |
| `duplicar` | `1` | 1 a 5 | Quantas vezes cada evento é enviado, sempre com o **mesmo** `idEvento` e o mesmo corpo |
| `embaralhar` | `false` | `true`/`false` | Inverte a ordem: `cobranca.paga` em 1 s e `cobranca.registrada` em `atrasoWebhook + 3` s (contados a partir da resposta) |
| `assinatura` | `valida` | `valida`/`invalida` | `invalida` troca cada dígito hex da assinatura pelo complemento (0↔f, 1↔e...): o formato continua `sha256=` + 64 hex, mas nada confere |
| `valorComVirgula` | `false` | `true`/`false` | `valor` sai como texto pt-BR (`"1.250,00"`) em vez de número (`1250.00`) nos dois eventos: a "atualização da API" do banco vale para todos |
| `falhaTransiente` | `0` | 0 a 100 | N% dos `POST /cobrancas` respondem 503 (com `Retry-After: 1`) antes de qualquer efeito; N% dos `cobranca.paga` saem com `simulaTransiente = true` e `transienteAte = envio + 45 s`. São dois sorteios independentes |

Booleanos aceitam `true`, `false`, `1`, `0` e a forma curta (`?embaralhar` vale `true`).

Onde cada botão aparece na aula:

| Cenário | Botões |
|---|---|
| Incidente 1 (3.2, timeout) | `latencia=45` (e `latencia=60` na prova do 202) |
| Incidente 2 (3.3, confirmação repetida e fora de ordem) | `duplicar=3;embaralhar=true` (a confirmação forjada é um POST sem assinatura, direto no APIM); `assinatura=invalida` para ver o 401 |
| Incidente 3 (3.4, mensagem envenenada) | duas rajadas separadas: `valorComVirgula=true` (10 faturas) e `falhaTransiente=50` (20 faturas) |

### Linha do tempo dos webhooks

`R` é o instante em que o banco responde ao hub: o instante do `POST /cobrancas` mais a `latencia` (com `latencia=0`, os dois coincidem). Os webhooks já estão na fila antes da espera, mas só ficam visíveis depois de `R`: o banco nunca confirma antes de responder, então o hub já gravou o `idCobranca` (fatura em `cobranca-emitida`) quando o primeiro webhook chega, mesmo com o banco lento.

| Botões | Primeiro envio | Segundo envio |
|---|---|---|
| padrão | `cobranca.registrada` em R + 1 s | `cobranca.paga` em R + `atrasoWebhook` (5 s) |
| `embaralhar=true` | `cobranca.paga` em R + 1 s | `cobranca.registrada` em R + `atrasoWebhook` + 3 (8 s) |
| `duplicar=3` | cada evento sai 3 vezes no mesmo instante, com o mesmo `idEvento` | |
| `latencia=45` | `cobranca.registrada` 46 s depois do POST | `cobranca.paga` 50 s depois do POST |

O `ocorridoEm` de cada evento é o instante planejado de envio. Os tempos valem com o Function App aquecido: num cold start (a primeira chamada depois de um tempo parado), os eventos podem atrasar alguns segundos, sair juntos e em qualquer ordem. É a mesma lição do Incidente 2: a ordem de chegada nunca é garantida e quem recebe precisa tolerar.

## Contrato com o hub

### Corpo do webhook (contrato, seção 7.1)

JSON compacto, sem espaços, com os campos nesta ordem:

```json
{"idEvento":"11111111-1111-1111-1111-111111111111","tipo":"cobranca.paga","faturaId":"22222222-2222-2222-2222-222222222222","idCobranca":"COB-0a1b2c3d","valor":1250.00,"ocorridoEm":"2026-11-10T09:30:00Z","simulaTransiente":false,"transienteAte":null}
```

- `tipo`: `cobranca.registrada` ou `cobranca.paga`; cada tipo tem o seu `idEvento` (GUID), repetido nas duplicatas.
- `valor`: número com duas casas e ponto (`1250.00`); com `valorComVirgula`, texto (`"1.250,00"`).
- `ocorridoEm` e `transienteAte`: UTC no formato `yyyy-MM-ddTHH:mm:ssZ`; `transienteAte` é `null` quando `simulaTransiente` é `false`.

### Assinatura (contrato, seção 7.2)

- Headers `X-Saldaro-Timestamp: <unix seconds>` e `X-Saldaro-Signature: sha256=<hex minúsculo>`; `Content-Type: application/json; charset=utf-8`.
- Conteúdo assinado: a string `"{timestamp}.{corpo}"`, com o corpo exatamente como enviado (UTF-8).
- HMAC-SHA256 com chave = bytes UTF-8 do setting `WebhookSecret` (a string do setting, sem decodificar base64).
- O banco assina no momento do envio, inclusive nas novas tentativas, então o timestamp fica dentro da tolerância de ±300 s do hub mesmo com `atrasoWebhook` longo.
- Vetor de teste compartilhado com o hub: segredo `segredo-de-teste-saldaro`, timestamp `1760000000` e o corpo do exemplo acima resultam em `sha256=a34d32f6a9230ca8a5e9dfc0bd8241bc95ff7c3aabc38f6efc032dffa40816a9`.

### Pedido de cobrança

`POST /api/cobrancas` com o header `x-api-key` e o corpo `{ faturaId, clienteId, valor, vencimento, pagadorCnpj, webhookUrl }`. Obrigatórios: `faturaId` (até 128 caracteres, sem `/ \ # ?`, a mesma regra do hub), `valor` (de 0.01 a 99999999.99; número ou texto com ponto) e `webhookUrl` (URL absoluta http ou https). `clienteId`, `vencimento` e `pagadorCnpj` são só guardados.

### Respostas do hub e novas tentativas

| Resposta do `webhookUrl` | O que o `EnviarWebhook` faz |
|---|---|
| 2xx | Entregue: a mensagem é completada (sai da fila) |
| 400, 401 e os demais 4xx, exceto 408 e 429 | Rejeitado: log de aviso com o status e a mensagem é completada, sem nova tentativa. O banco não insiste num evento rejeitado (é o caso do `assinatura=invalida`) |
| 5xx, 408, 429, timeout (30 s) ou falha de rede | A Function falha de propósito e o Storage Queue reentrega a mensagem depois de 10 s (`visibilityTimeout`), até `maxDequeueCount` (5, o padrão do Functions, explícito no `host.json`); depois disso, a mensagem vai para a fila `banco-pagamentos-poison` |

Cada nova tentativa assina de novo, com timestamp novo, e mantém o mesmo corpo e o mesmo `idEvento`.

## Fila `banco-pagamentos`

Cada mensagem é uma entrega: `{"evento":{...corpo do webhook...},"webhookUrl":"...","assinaturaInvalida":false}`. O `evento` vai como JSON cru e o `EnviarWebhook` envia exatamente esses bytes, então `1250.00` continua `1250.00` e a assinatura bate no hub.

O atraso de cada entrega é o `visibilityTimeout` do envio para a fila: a mensagem entra invisível e só aparece para o trigger (e no Storage browser do Portal) quando o atraso vence.

**Codificação Base64.** O `QueueClient` do SDK v12 envia texto puro por padrão, mas o trigger de fila do Functions (extensão 5.x) espera Base64 (`messageEncoding`, padrão `base64`). A escolha foi Base64 nos dois lados, explícito no código (`QueueClientOptions.MessageEncoding = QueueMessageEncoding.Base64`, em `Infraestrutura/FilaDePagamentos.cs`) e no `host.json` (`extensions.queues.messageEncoding = "base64"`). Se um dos lados mudar sozinho, as mensagens falham na leitura e acabam em `banco-pagamentos-poison`. O `FilaDePagamentosTests` trava as duas pontas.

Outros valores de `extensions.queues` no `host.json`: `maxPollingInterval` de 1 s (sem isso, a fila ociosa é consultada no máximo a cada 1 min e os atrasos de 1 s e 5 s perdem o sentido), `visibilityTimeout` de 10 s entre tentativas e `maxDequeueCount` 5.

## Tabela `cobrancas`

PartitionKey = `faturaId`, RowKey = `idCobranca` (`COB-` + 8 hex). Colunas: `clienteId`, `valor` (double), `vencimento`, `pagadorCnpj`, `webhookUrl`, `linhaDigitavel`, `qrCode`, `caos` (os botões efetivos, no formato da query string) e `criadaEm`.

## Settings

| Setting | Uso |
|---|---|
| `AzureWebJobsStorage` | Runtime do Functions **e** fila `banco-pagamentos` (o trigger e quem enfileira usam a mesma conexão) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Telemetria do worker direto para o Application Insights, sem sampling. Opcional rodando local |
| `TablesConnection` | Connection string do storage com a tabela `cobrancas` |
| `BancoApiKey` | Valor que o hub precisa mandar no header `x-api-key` |
| `WebhookSecret` | Segredo do HMAC dos webhooks; precisa ser igual ao do hub |

O banco é "o parceiro": fica com connection strings e não migra para identidade no cenário 3.5. Nenhum segredo fica no código. Em Azure, o Bicep grava os valores; localmente, copie `local.settings.json.example` para `local.settings.json` (ignorado pelo git) e preencha.

## Compilar, testar e rodar

```powershell
dotnet build 03_Aplicações/Saldaro.BancoSimulado/Saldaro.BancoSimulado.csproj
dotnet test  03_Aplicações/Saldaro.BancoSimulado.Tests/Saldaro.BancoSimulado.Tests.csproj
```

Os CNPJs de pagador dos testes são gerados por algoritmo, nunca fixos: o projeto de testes compila o `CnpjDeTeste.cs` dos testes do hub (`Saldaro.Hub.Tests/`), em vez de manter uma cópia.

Para rodar local (opcional), use o Azurite e o Azure Functions Core Tools: `func start` na pasta do projeto sobe o banco na porta 7072, ao lado do hub na 7071. Chamada direta ao banco:

```powershell
$banco = 'http://localhost:7072/api'          # em Azure: SALDARO_BANCO_URL do scripts/.env.local
$chave = '<o BancoApiKey do local.settings.json>'
$faturaId = [guid]::NewGuid().ToString()
$corpo = @{
    faturaId    = $faturaId
    clienteId   = 'cliente-a'
    valor       = 1250.00
    vencimento  = '2026-11-20'
    pagadorCnpj = '<cnpj fictício>'            # o banco não valida CNPJ
    webhookUrl  = 'http://localhost:7071/api/webhooks/banco'
} | ConvertTo-Json

Invoke-RestMethod -Method Post "$banco/cobrancas?duplicar=3&embaralhar=true" `
    -Headers @{ 'x-api-key' = $chave } -ContentType 'application/json' -Body $corpo
Invoke-RestMethod "$banco/cobrancas/$faturaId"
```

## Troubleshooting

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| 401 em `POST /cobrancas` | `x-api-key` ausente ou diferente de `BancoApiKey` | Conferir o `BancoApiKey` do hub e do banco; `GET /api/saude` mostra se o do banco está preenchido |
| 503 "falha transiente simulada" | Botão `falhaTransiente` ligado | Esperado: quem chama deve repetir |
| Webhook rejeitado com 401 sem `assinatura=invalida` | `WebhookSecret` diferente no hub e no banco | Igualar os dois settings |
| Mensagens em `banco-pagamentos-poison` | Hub fora do ar por mais de uns 40 s (5 tentativas, 10 s entre elas), mensagem fora do formato ou codificação descombinada | Ver a exceção do `EnviarWebhook` no log; conferir `messageEncoding` no `host.json` |
| Webhooks em ordem diferente da tabela de tempos | Cold start do Function App | Repetir com o app aquecido; o hub precisa tolerar desordem de qualquer jeito |
| Mensagem não aparece na fila no Portal | Ainda invisível (atraso do `visibilityTimeout`) | Esperar o atraso da entrega |
