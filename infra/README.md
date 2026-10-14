# Infraestrutura do lab Saldaro Hub (Bicep + PowerShell)

**Version-anchor:** Q4-2026 · Só serviços e versões de API em GA · Empresa, pessoas, números e incidentes são fictícios.

Esta pasta descreve, em Bicep, todo o ambiente do lab de Integração e Mensageria no Azure: o hub de cobrança da Saldaro Pagamentos S.A., o banco parceiro simulado e o que liga os dois. Você não precisa clicar para criar nada: o `scripts/deploy.ps1` cria o resource group, faz o deploy deste Bicep no estado de um cenário, publica os dois Function Apps e grava `scripts/.env.local` para os outros scripts.

> **CUSTO E FREE TRIAL**
>
> | Item | Valor |
> |---|---|
> | Custo mensal se ficar ligado | ~R$ 60 (quase tudo é o Service Bus Standard); ~R$ 70 com os alertas do 3.6 |
> | Custo real do lab (encontro de 4 h, RG apagado no fim) | ~R$ 1 |
> | Compatível com Free Trial (US$ 200)? | Sim. Nada aqui usa SKU com custo fixo alto (sem APIM Developer, Service Bus Premium ou Functions Premium) |
>
> **Regra de ouro:** ao terminar, rode `pwsh ./scripts/limpar.ps1`. Ele apaga o `rg-saldaro-lab` e purga o Key Vault e o APIM, que ficam em soft-delete com o nome reservado.

## Conteúdo

| Caminho | O que é |
|---|---|
| `main.bicep` | Orquestra os módulos, define os nomes e expõe os outputs |
| `modules/observabilidade.bicep` | Log Analytics + Application Insights (workspace-based) |
| `modules/storage.bicep` | Storage do lab: tabelas, fila `banco-pagamentos`, containers de deploy do Flex |
| `modules/service-bus.bicep` | Namespace Standard e as três filas (lista única, reaproveitada pelo `check-drift.ps1`) |
| `modules/event-grid.bicep` | Tópico CloudEvents, role do tópico na fila e event subscription com identidade |
| `modules/key-vault.bicep` | Key Vault com RBAC; segredos só com `identidadeGerenciada` |
| `modules/function-app.bicep` | Plano + Function App .NET 10 isolated (Flex Consumption ou Windows Consumption) |
| `modules/hub.bicep`, `modules/banco.bicep` | App settings do hub (seção 5.2 do contrato) e do banco (seção 6) |
| `modules/apim.bicep` | APIM Consumption, logger e diagnóstico do App Insights |
| `modules/apim-api.bicep` | APIs `saldaro` e `saldaro-webhooks`, políticas e subscriptions |
| `modules/roles-hub.bicep`, `modules/roles-aluno.bicep` | Role assignments com nomes `guid(...)` determinísticos |
| `modules/alertas.bicep` | Action group e alertas (heartbeat ausente e DLQ) |
| `parameters/*.bicepparam` | Um arquivo por estado de cenário (seção 4 do contrato) |

## Pré-requisitos

- Assinatura Azure própria (Free Trial serve) em que você seja **Owner**: o deploy cria role assignments. Sem `Microsoft.Authorization/roleAssignments/write` (comum em assinaturas corporativas ou de parceiro), o what-if já falha com `Authorization failed ... roleAssignments/write`.
- PowerShell 7 (`pwsh`), Azure CLI com Bicep (`az bicep install`) e, para publicar o código, o .NET 10 SDK. Azure Functions Core Tools **não** é necessário.
- `az login` feito e a assinatura certa ativa (`az account show`; para trocar, `az account set --subscription <nome ou id>`). O `deploy.ps1` mostra a assinatura ativa logo no início.
- Região padrão `brazilsouth`. O script confere se o Flex Consumption existe lá com `dotnet-isolated` 10; se não existir, ele para e indica o plano B (`-Plano Consumption`).

## Como usar

```powershell
# Lab 0: ambiente inteiro no baseline ingênuo + smoke test
pwsh ./scripts/deploy.ps1 -Cenario 0 -Sufixo abcd

# Estado inicial de um cenário (o que o guia pede para você consertar)
pwsh ./scripts/deploy.ps1 -Cenario 3

# Plano B ao vivo: estado consertado do cenário
pwsh ./scripts/deploy.ps1 -Cenario 3 -Pronto

# Só infraestrutura, sem publicar código
pwsh ./scripts/deploy.ps1 -Cenario 3 -SemPublicar

# Modo seguro: compila e roda o what-if, sem criar nem mudar nada
pwsh ./scripts/deploy.ps1 -Cenario 7 -Pronto -SomenteValidar

# Plano B de hospedagem (Windows Consumption)
pwsh ./scripts/deploy.ps1 -Cenario 0 -Plano Consumption

Get-Help ./scripts/deploy.ps1 -Full
```

Sem `-Sufixo`, `-Location` e `-Plano`, o script reaproveita os do ambiente que já existe no `rg-saldaro-lab` (ou `SALDARO_SUFIXO` e `SALDARO_LOCATION` do `scripts/.env.local`); num ambiente novo ele gera um sufixo e confere se os nomes globais estão livres. `-Cenario 0 -Pronto` é aceito e equivale a `-Cenario 0`.

### Estado aplicado por cenário

| Comando | Arquivo de parâmetros usado |
|---|---|
| `-Cenario 0`, `1` ou `2` | `cenario-0.bicepparam` |
| `-Cenario 3` | `cenario-2-pronto.bicepparam` |
| `-Cenario 4` | `cenario-3-pronto.bicepparam` |
| `-Cenario 5` ou `6` | `cenario-4-pronto.bicepparam` |
| `-Cenario 7` | `cenario-5-pronto.bicepparam` |
| `-Cenario N -Pronto` (N ≥ 1) | `cenario-N-pronto.bicepparam` |

Sem `-Pronto`, o script usa o arquivo **como está no disco**. É assim que os guias funcionam: você edita uma linha (por exemplo, `param filaComSessions = true` no `cenario-2-pronto.bicepparam`) e redeploya com `-Cenario 3 -SemPublicar`.

### Toggles

Cada arquivo de `parameters/` é autocontido (`using '../main.bicep'`, sem `extends`) e traz uma linha por toggle, sempre nesta ordem:

| Toggle | 0 | 1p | 2p | 3p | 4p | 5p | 6p | 7p | Efeito |
|---|---|---|---|---|---|---|---|---|---|
| `asyncRequestReply` | | | x | x | x | x | x | x | POST `/faturas` responde 202 e enfileira |
| `webhookHmac` | | | | x | x | x | x | x | webhook exige HMAC válido (401) |
| `inbox` | | | | x | x | x | x | x | baixa idempotente (tabela `inbox` + ETag) |
| `filaComSessions` | | | | x | x | x | x | x | `requiresSession` (imutável) |
| `filaDupDetection` | | | | x | x | x | x | x | `requiresDuplicateDetection` (imutável) |
| `workerComSessao` | | | | x | x | x | x | x | liga `ProcessarConfirmacaoComSessao` |
| `dlqComMotivo` | | | | | x | x | x | x | DLQ imediata com `SchemaInvalida` |
| `retryAgendado` | | | | | x | x | x | x | retry com `ScheduledEnqueueTime` |
| `maxDeliveryCount` | 10 | 10 | 10 | 10 | 3 | 3 | 3 | 3 | da `sbq-confirmacoes-banco` |
| `rateLimit` | | x | | | | | | | subscription + 60 chamadas/min em POST `/faturas` |
| `identidadeGerenciada` | | | | | | x | | x | Managed Identity, Key Vault references, `disableLocalAuth` |
| `heartbeat` | | | | | | | x | | `conciliacao.heartbeat` ao concluir |
| `alertas` | | | | | | | x | | action group + 2 alertas |

Os demais parâmetros vêm de variáveis de ambiente que o `deploy.ps1` exporta só para o próprio processo e apaga ao terminar: `SALDARO_SUFIXO`, `SALDARO_LOCATION`, `SALDARO_PLANO`, `SALDARO_PRINCIPAL_ID_ALUNO`, `SALDARO_EMAIL_ALERTA`, `SALDARO_WEBHOOK_SECRET` e `SALDARO_BANCO_API_KEY`. Os dois segredos (32 bytes aleatórios em base64) são gerados uma vez e reaproveitados nos redeploys (lidos do app setting do banco, do hub ou do Key Vault); nunca vão para arquivo nem para a linha de comando. Sem as variáveis, cada `.bicepparam` compila sozinho com o sufixo marcador `troque`, que o `deploy.ps1` recusa.

## Recursos

| Recurso | Nome | Observações |
|---|---|---|
| Resource Group | `rg-saldaro-lab` | criado pelo `deploy.ps1` |
| Service Bus | `sb-saldaro-<sufixo>` | Standard; `disableLocalAuth = identidadeGerenciada` |
| Filas | `sbq-faturas-recebidas`, `sbq-confirmacoes-banco`, `sbq-notificacoes-parceiro` | lock 1 min; a de confirmações segue os toggles e tem dead-lettering na expiração |
| Storage | `stsaldaro<sufixo>` | Standard LRS; tabelas `faturas`, `inbox`, `idempotencia`, `conciliacao`, `cobrancas`; fila `banco-pagamentos`; containers `deploy-hub` e `deploy-banco` (só Flex) |
| Planos | `asp-saldaro-hub-<sufixo>`, `asp-saldaro-banco-<sufixo>` | FC1 (Flex) ou Y1 (Windows), um por app |
| Function Apps | `func-saldaro-hub-<sufixo>`, `func-saldaro-banco-<sufixo>` | .NET 10 isolated; o hub tem identidade system-assigned |
| APIM | `apim-saldaro-<sufixo>` | Consumption; APIs `saldaro` e `saldaro-webhooks` |
| Key Vault | `kv-saldaro-<sufixo>` | RBAC; soft-delete de 7 dias; purge protection desligada (veja Armadilhas) |
| Event Grid | `evgt-saldaro-faturas-<sufixo>` | CloudEvents 1.0; subscription `evgs-notificar-parceiro` → `sbq-notificacoes-parceiro` com `deliveryWithResourceIdentity` |
| Observabilidade | `log-saldaro-<sufixo>`, `appi-saldaro-<sufixo>` | os dois apps e o APIM no mesmo App Insights |
| Alertas (só `alertas = true`) | `ag-saldaro`, `alrt-heartbeat-ausente`, `alrt-dlq-confirmacoes` | e-mail = `-EmailAlerta` ou o e-mail da conta logada |

Role assignments: o aluno logado recebe `Azure Service Bus Data Owner`, `Storage Table Data Contributor` e `Key Vault Secrets Officer`; com `identidadeGerenciada`, o hub recebe `Azure Service Bus Data Sender` + `Receiver` e `Reader` no namespace (a leitura que a escala do trigger usa), `Storage Table Data Contributor`, `EventGrid Data Sender` e `Key Vault Secrets User`; o tópico do Event Grid recebe `Azure Service Bus Data Sender` só na fila de destino.

### Outputs do `main.bicep`

Nomes: `resourceGroupName`, `serviceBusNamespaceName`, `filaFaturasRecebidas`, `filaConfirmacoesBanco`, `filaNotificacoesParceiro`, `storageAccountName`, `tabelas`, `filaStorageBanco`, `hubPlanName`, `bancoPlanName`, `hubFunctionAppName`, `bancoFunctionAppName`, `apimName`, `apimApiName`, `apimApiWebhooksName`, `apimOperacoes`, `keyVaultName`, `eventGridTopicName`, `eventSubscriptionName`, `logAnalyticsName`, `appInsightsName`, `actionGroupName`, `alertaHeartbeatName`, `alertaDlqName` (os três últimos vazios sem `alertas`).

Endpoints: `hubBaseUrl`, `bancoBaseUrl` (sempre de `properties.defaultHostName`), `apimBaseUrl`, `serviceBusFqdn`, `tableServiceUri`, `eventGridTopicEndpoint`.

Outros: `apimSubscriptionsHabilitadas` (bool) e `apimSubscriptions` (nomes, nunca as chaves), `filasDeclaradas` e `namespaceDeclarado` (usados pelo `check-drift.ps1`), `toggles`, `sufixo`, `location`, `plano`.

O `scripts/.env.local` (ignorado pelo git, sem segredos) recebe `SALDARO_RG`, `SALDARO_SUFIXO`, `SALDARO_LOCATION`, `SALDARO_SUBSCRIPTION`, `SALDARO_APIM_URL`, `SALDARO_HUB_URL`, `SALDARO_BANCO_URL`, `SALDARO_SB_NAMESPACE`, `SALDARO_SB_FQDN`, `SALDARO_STORAGE`, `SALDARO_KV` e `SALDARO_APPI`.

## Decisões de implementação

- **Rate limit no APIM Consumption.** Verificado no Microsoft Learn (2026-10-13): `rate-limit-by-key` não é suportado no tier Consumption, e `rate-limit` só conta chamadas feitas com chave de subscription. Por isso as operações ficam em duas APIs: `saldaro` (path `saldaro`: `post-fatura`, `get-fatura`, `get-fatura-status`) e `saldaro-webhooks` (path `saldaro/webhooks`: `post-webhook-banco` em `/banco`, de modo que a URL continua `{apimBaseUrl}/webhooks/banco`). A de webhooks nunca exige subscription. Só no `cenario-1-pronto` a API `saldaro` passa a `subscriptionRequired: true`, com as subscriptions `cliente-a` e `cliente-b` (escopo da API) e `<rate-limit calls="60" renewal-period="60" />` no inbound de `post-fatura` (429 + `Retry-After`). Nos outros estados: sem subscription e sem rate limit. As chaves nunca saem em output nem no `.env.local`: quem precisa (o `erp-simulado.ps1`) lê na hora, por exemplo com `az rest --method post --url https://management.azure.com/subscriptions/<id>/resourceGroups/rg-saldaro-lab/providers/Microsoft.ApiManagement/service/apim-saldaro-<sufixo>/subscriptions/cliente-a/listSecrets?api-version=2024-05-01 --query primaryKey -o tsv`, e manda no header `Ocp-Apim-Subscription-Key`. O `displayName` das operações é o próprio nome do contrato, como aparece no Portal.
- **Timeout de 30 s e 504.** As duas APIs têm `<forward-request timeout="30" />` e um `on-error` que, quando `context.LastError.Source == "forward-request"` e `context.LastError.Reason == "Timeout"` (nomes da tabela de erros predefinidos do Learn), devolve `504 Gateway Timeout` com `{"erro":"o gateway esperou 30 s e desistiu"}`. Sem isso, o APIM responderia 500. Os demais erros, como o 429, seguem o padrão (`<base />`).
- **Diagnóstico do APIM.** Diagnóstico "All APIs" para o App Insights com amostragem de 100% e o header `x-cliente-id` registrado na requisição (frontend e backend), além do `Retry-After` da resposta.
- **Flex Consumption dimensionado para o Free Trial.** A cota regional do Flex no Free Trial é de 15 núcleos (30.720 MB) somando todos os apps Flex da assinatura na região, e o `maximumInstanceCount` vale por grupo de escala (o grupo HTTP e cada trigger não HTTP escalam separados). O kit usa `instanceMemoryMB = 512` (0,25 núcleo), `maximumInstanceCount = 10` no hub e `5` no banco: no pior caso, hub (4 grupos de fila/HTTP com até 10 instâncias + o timer) e banco (2 grupos com até 5) somam cerca de 13 núcleos. Com 2.048 MB (1 núcleo) a mesma escala estouraria a cota. Para .NET 10 isolated, 512 MB atende ao lab (cold start um pouco maior). Em assinatura paga (cota padrão de 250 núcleos), 2.048 MB é o tamanho recomendado.
- **Pacote de deploy do Flex.** Um container por app (`deploy-hub`, `deploy-banco`), autenticado com a connection string do próprio `AzureWebJobsStorage` (`StorageAccountConnectionString`), para não criar nenhum app setting além dos do contrato. Publicação: `dotnet publish -c Release` + zip + `az functionapp deployment source config-zip` (o comando que o Learn indica para C# no Flex).
- **Plano B: Windows Consumption (Y1).** É Windows porque o .NET 10 não roda no Consumption em Linux. Só nele entram `FUNCTIONS_EXTENSION_VERSION=~4`, `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`, `WEBSITE_CONTENTAZUREFILECONNECTIONSTRING`, `WEBSITE_CONTENTSHARE` (nome fixo, para os redeploys não trocarem o share), `WEBSITE_RUN_FROM_PACKAGE=1`, `netFrameworkVersion = v10.0` e processo de 64 bits. No Flex esses settings são proibidos (o runtime vem de `functionAppConfig`).
- **App settings reaplicados a cada deploy.** A lista inteira é declarada; um setting criado à mão no Portal (por exemplo, `AzureWebJobs.Conciliacao.Disabled` no 3.6) some no próximo `deploy.ps1`. O `host.json` dos apps não é sobrescrito por app setting (o banco depende de `messageEncoding: base64` e `maxPollingInterval` de 1 s).
- **Deploy incremental.** Desligar um toggle não apaga o que ele criou. O `deploy.ps1` trata os dois casos que importam: remove os alertas quando o estado pedido tem `alertas = false` e recria a `sbq-confirmacoes-banco` quando a mudança é imutável (só com `-Pronto`). Segredos no Key Vault e roles do hub ficam (inofensivos). A política de `post-fatura` é sempre declarada, para o rate limit sair de verdade.
- **Versões de API (todas GA):** Web `2024-11-01`, Service Bus `2024-01-01`, Storage `2025-01-01`, Event Grid `2025-02-15`, Key Vault `2024-11-01`, API Management `2024-05-01`, Log Analytics `2025-02-01`, App Insights `2020-02-02`, scheduled query rules `2023-12-01`, metric alerts `2018-03-01`, action groups `2023-01-01`, role assignments `2022-04-01`.

## Custo estimado por cenário

Preços de lista da Microsoft a ~R$ 5,50/US$; confirme na [calculadora](https://azure.microsoft.com/pricing/calculator/) na data da aula.

| Estado | O que muda no custo | Lab de ~4 h (RG apagado no fim) | Se esquecer ligado 30 dias |
|---|---|---|---|
| `cenario-0` (Lab 0) | Service Bus Standard (~R$ 0,07/h); APIM Consumption, Functions, Event Grid e Log Analytics dentro das franquias gratuitas; Storage e Key Vault em centavos | ~R$ 0,50 a 1 | ~R$ 60 |
| `cenario-1-pronto` | rate limit e subscriptions: sem custo | ~R$ 0,50 a 1 | ~R$ 60 |
| `cenario-2-pronto` | fila e rajada de 1.000 faturas: centavos de operações | ~R$ 0,50 a 1 | ~R$ 60 |
| `cenario-3-pronto` | sessions e duplicate detection: incluídos no Standard | ~R$ 0,50 a 1 | ~R$ 60 |
| `cenario-4-pronto` | DLQ e mensagens agendadas: incluídas no Standard | ~R$ 0,50 a 1 | ~R$ 60 |
| `cenario-5-pronto` | segredos e operações no Key Vault: centavos | ~R$ 0,50 a 1 | ~R$ 60 |
| `cenario-6-pronto` | alerta de log a cada 5 min + alerta de métrica (~R$ 9/mês somados) | ~R$ 1 | ~R$ 70 |
| `cenario-7-pronto` | igual ao 5 (deployments não custam) | ~R$ 0,50 a 1 | ~R$ 60 |

Franquias usadas: APIM Consumption com 1 milhão de chamadas/mês, Functions com a concessão gratuita mensal de execuções e GB-s, Event Grid com 100 mil operações/mês e Log Analytics com 5 GB/mês. O 3.8 é exercício de decisão: não cria Premium.

## What-if avulso

O `deploy.ps1` mostra o what-if antes de cada create no formato `ResourceIdOnly`: só a lista de recursos, sem valores (os app settings do baseline têm segredos). Nesse formato, o what-if não compara propriedades:

- recurso novo aparece com `+` (Create);
- recurso que vai sair aparece com `-` (Delete);
- **todo recurso que já existe aparece com `!` (Deploy)**, mude ou não.

No primeiro deploy real do kit, o estado pronto do 3.7 deu `Resource changes: 49 to deploy`, mesmo com o `maxDeliveryCount` de uma fila alterado à mão. Use essa lista para conferir que nada vai ser criado nem apagado. Para ver a lista sem mudar nada, rode `pwsh ./scripts/deploy.ps1 -Cenario 7 -Pronto -SomenteValidar`. Para ver a diferença propriedade a propriedade (útil no 3.7), rode o what-if à mão:

```powershell
# 1. Variáveis que o .bicepparam lê (valores do seu scripts/.env.local)
$env:SALDARO_SUFIXO   = '<seu sufixo>'
$env:SALDARO_LOCATION = 'brazilsouth'
$env:SALDARO_PLANO    = 'Flex'                 # ou Consumption, se foi o seu plano
$env:SALDARO_PRINCIPAL_ID_ALUNO = az ad signed-in-user show --query id -o tsv

# 2. Segredos atuais, lidos do banco simulado (não aparecem na tela)
$s = az functionapp config appsettings list -g rg-saldaro-lab -n "func-saldaro-banco-$env:SALDARO_SUFIXO" | ConvertFrom-Json
$env:SALDARO_WEBHOOK_SECRET = ($s | Where-Object name -eq 'WebhookSecret').value
$env:SALDARO_BANCO_API_KEY  = ($s | Where-Object name -eq 'BancoApiKey').value

# 3. What-if do estado desejado
az deployment group what-if --resource-group rg-saldaro-lab `
  --parameters infra/parameters/cenario-7-pronto.bicepparam `
  --exclude-change-types NoChange Ignore

# 4. Tire os segredos da sessão
Remove-Item Env:SALDARO_WEBHOOK_SECRET, Env:SALDARO_BANCO_API_KEY
```

No formato completo (o padrão), o what-if pode exibir valores de app settings, inclusive segredos do baseline: não projete essa saída. Para ver só a lista de recursos, acrescente `--result-format ResourceIdOnly`.

**Ruído esperado (não é drift):** mesmo logo depois de um deploy, o what-if completo marca muitos recursos como modificados (`~`). No primeiro deploy real do kit, foram 25 no estado pronto do 3.7 (`Resource changes: 25 to modify`):

- **os dois Function Apps:** o ARM não devolve app settings na leitura do site, e os valores vindos de `listKeys()` e de parâmetros `@secure()` não são comparáveis;
- **o APIM:** o serviço, as duas APIs, as políticas (o Bicep envia `rawxml`, e a leitura devolve `xml` escapado) e o logger, por causa das credenciais;
- **outros recursos:** o tópico do Event Grid, o Application Insights, os serviços e os containers do Storage e os role assignments;
- **as próprias filas:** o Service Bus preenche com o valor padrão propriedades que o Bicep não declara (`autoDeleteOnIdle`, `defaultMessageTimeToLive`, `enablePartitioning`, `maxMessageSizeInKilobytes`, `maxSizeInMegabytes`). Elas aparecem com `-`, como se fossem sair, mas o deploy não as apaga.

Drift de verdade aparece como `~` com o valor de agora e o declarado, por exemplo `~ properties.maxDeliveryCount: 10 => 3` no bloco da `sbq-confirmacoes-banco`. Para o inventário das filas, inclusive as que o Bicep não declara, use o `check-drift.ps1`.

## Drift das filas

```powershell
pwsh ./scripts/check-drift.ps1    # sem parâmetros: lê o scripts/.env.local
```

Compara as filas e o namespace reais com os outputs `filasDeclaradas` e `namespaceDeclarado` do último deployment bem-sucedido do lab e lista o que é **extra** (ex.: a `confirmacoesbanco` do worker legado), **faltante** ou **divergente** (ex.: `maxDeliveryCount` mudado no Portal). Sai com 0 sem drift, 1 com drift e 2 quando não consegue comparar (sem login, sem RG ou sem deployment).

## Limpeza

```powershell
pwsh ./scripts/limpar.ps1             # pergunta antes; -SemConfirmacao para automação
pwsh ./scripts/limpar.ps1 -Sufixo abcd   # RG já apagado pelo Portal: só purga as sobras
```

Apaga o `rg-saldaro-lab`, purga o `kv-saldaro-<sufixo>` e o `apim-saldaro-<sufixo>` ("não encontrado" conta como sucesso) e apaga o `scripts/.env.local`.

## Armadilhas

| Sintoma | Causa | O que fazer |
|---|---|---|
| `nome já em uso` / `AlreadyExists` no Storage, Service Bus, Key Vault, APIM ou Function App | nomes globais (únicos no Azure inteiro) | escolha outro `-Sufixo`; num ambiente novo o `deploy.ps1` confere antes e gera um sufixo livre |
| recriar com o mesmo sufixo falha no Key Vault | soft-delete de 7 dias guarda o nome | `limpar.ps1 -Sufixo <sufixo>` purga, mesmo com o RG já apagado pelo Portal. Num ambiente novo, o `deploy.ps1` detecta a sobra e para sem purgar; repita com `-PurgarExcluidos` para ele purgar (ou `az keyvault purge --name kv-saldaro-<sufixo> --location brazilsouth`) |
| recriar com o mesmo sufixo falha no APIM | soft-delete de 48 h guarda o nome | `limpar.ps1 -Sufixo <sufixo>` purga; o `deploy.ps1` só purga a sobra com `-PurgarExcluidos` (ou `az apim deletedservice purge --service-name apim-saldaro-<sufixo> --location brazilsouth`) |
| Key Vault com purge protection | não usamos de propósito: com ela ligada, o cofre não pode ser purgado nem desligado depois | no lab, deixe desligada; **em produção ela deve ficar ligada** |
| 401/403 logo depois do 3.5; Key Vault reference sem resolver; Service Bus Explorer sem acesso | propagação de RBAC (segundos a alguns minutos) | espere; o `deploy.ps1` aguarda as Key Vault references e reinicia o hub se precisar |
| primeiro deploy demorado | o APIM Consumption leva alguns minutos para ativar | normal; faça o Lab 0 antes do encontro |
| `deploy.ps1` para dizendo que a mudança é imutável | `requiresSession` e `requiresDuplicateDetection` não mudam numa fila existente (a caixa fica desabilitada no Portal) | caminho do 3.3: drene, apague a fila no Portal e rode de novo; plano B: `-Pronto` recria a fila (as mensagens dela se perdem) |
| `Flex Consumption não está disponível` | região ou assinatura sem Flex para `dotnet-isolated` 10 | `-Plano Consumption` ou outra `-Location` |
| trocar de Flex para Consumption (ou o contrário) falha | não existe migração de plano no lugar | `limpar.ps1` e deploy de novo com o outro `-Plano` |
| funções lentas ou com escala segurada no Free Trial | cota regional de 15 núcleos do Flex | o kit já usa 512 MB e teto de 10/5 instâncias; não suba sem conferir a cota |
| setting criado à mão no Portal sumiu | o deploy reaplica a lista inteira de app settings | refaça depois do deploy (ou declare no Bicep) |
| alertas não chegam | e-mail do Action Group não verificado (o Azure manda um código de uso único que vale 30 minutos); sem `-EmailAlerta` e sem e-mail na conta; ou caiu no spam | confirme o e-mail "Action required: Verify your email for Azure Monitor action group" (se o código expirou, abra o Action Group e selecione **Resend**); ou rode com `-EmailAlerta voce@exemplo.example` |
| `ProviderError` ao listar APIM apagados | instabilidade da listagem de soft-delete | os scripts consultam pelo nome (`az apim deletedservice show`), não pela lista |
| Log Analytics com nome em conflito ao recriar em outra região | soft-delete de 14 dias do workspace | recrie na mesma região (o deploy recupera o workspace) ou use outro sufixo |
