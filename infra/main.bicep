// =============================================================================
// Saldaro Hub: infraestrutura do laboratório (escopo: resource group)
// =============================================================================
// Version-anchor: Q4-2026. Só serviços e versões de API em GA.
//
// Como é usado: o scripts/deploy.ps1 cria o rg-saldaro-lab e faz o deploy
// deste arquivo com um dos arquivos de infra/parameters/ (um por estado de
// cenário). Os toggles ligam, um a um, os consertos dos incidentes; o cenário 0
// é o "baseline ingênuo": tudo desligado de propósito.
//
// Ordem dos módulos (o que depende de quê):
//   observabilidade, storage, service bus, key vault  (independentes)
//   event grid      -> fila sbq-notificacoes-parceiro (role na fila + subscription)
//   apim            -> App Insights (logger e diagnóstico)
//   banco           -> storage, App Insights
//   hub             -> tudo acima (URL do banco e URL pública do APIM)
//   apim-api        -> URL do hub (service URL da API)
//   roles e alertas -> por último, conforme os toggles
//
// O modo do deployment é incremental: desligar um toggle NÃO apaga recursos
// criados por ele. O deploy.ps1 cuida dos dois casos em que isso importa
// (alertas e recriação da fila imutável); veja infra/README.md.
// =============================================================================

targetScope = 'resourceGroup'

// -----------------------------------------------------------------------------
// Parâmetros do aluno
// -----------------------------------------------------------------------------

@description('Sufixo do aluno: 4 a 6 letras minúsculas ou dígitos. Deixa únicos os nomes globais (Storage, Service Bus, Key Vault, APIM, Function Apps).')
@minLength(4)
@maxLength(6)
param sufixo string

@description('Região de todos os recursos.')
param location string = 'brazilsouth'

@description('Plano das Function Apps: Flex (Flex Consumption FC1, Linux; recomendado) ou Consumption (Y1, Windows; plano B, legado mas GA).')
@allowed([
  'Flex'
  'Consumption'
])
param plano string = 'Flex'

@description('Object ID do aluno logado (az ad signed-in-user show). Vazio = não cria as roles do aluno.')
param principalIdAluno string = ''

@description('E-mail que recebe os alertas (só usado com alertas = true).')
param emailAlerta string = ''

@description('Segredo HMAC do webhook: 32 bytes aleatórios em base64, gerado uma vez pelo deploy.ps1 e nunca gravado em arquivo.')
@secure()
param webhookSecret string

@description('Chave da API do banco simulado (header x-api-key): gerada uma vez pelo deploy.ps1 e nunca gravada em arquivo.')
@secure()
param bancoApiKey string

// -----------------------------------------------------------------------------
// Toggles dos cenários (valores em infra/parameters/*.bicepparam)
// -----------------------------------------------------------------------------

@description('3.2: POST /faturas responde 202 e enfileira em sbq-faturas-recebidas (async request-reply).')
param asyncRequestReply bool

@description('3.3: o webhook do banco exige assinatura HMAC válida (401 se inválida).')
param webhookHmac bool

@description('3.3: baixa idempotente com a tabela inbox e concorrência otimista (ETag).')
param inbox bool

@description('3.3: sbq-confirmacoes-banco com sessions (SessionId = faturaId). Imutável numa fila existente.')
param filaComSessions bool

@description('3.3: sbq-confirmacoes-banco com duplicate detection (MessageId = idEvento, janela de 10 min). Imutável numa fila existente.')
param filaDupDetection bool

@description('3.3: ativa ProcessarConfirmacaoComSessao (e desativa ProcessarConfirmacaoSemSessao).')
param workerComSessao bool

@description('3.4: mensagem com schema inválido vai direto para a DLQ com motivo SchemaInvalida.')
param dlqComMotivo bool

@description('3.4: falha transiente vira retry agendado (ScheduledEnqueueTime com backoff) em vez de reentrega imediata.')
param retryAgendado bool

@description('3.4: maxDeliveryCount da sbq-confirmacoes-banco (baseline 10; consertado 3).')
@minValue(1)
param maxDeliveryCount int

@description('3.1: a API saldaro exige subscription (cliente-a, cliente-b) e POST /faturas aceita 60 chamadas por minuto por subscription (429 + Retry-After).')
param rateLimit bool

@description('3.5: Managed Identity + Key Vault no lugar de connection strings; disableLocalAuth no Service Bus.')
param identidadeGerenciada bool

@description('3.6: a conciliação emite conciliacao.heartbeat ao concluir com sucesso.')
param heartbeat bool

@description('3.6: cria o action group e os alertas (heartbeat ausente e DLQ).')
param alertas bool

// -----------------------------------------------------------------------------
// Nomes (seção 2 do contrato do kit)
// -----------------------------------------------------------------------------

var nomes = {
  serviceBus: 'sb-saldaro-${sufixo}'
  storage: 'stsaldaro${sufixo}'
  planoHub: 'asp-saldaro-hub-${sufixo}'
  planoBanco: 'asp-saldaro-banco-${sufixo}'
  hub: 'func-saldaro-hub-${sufixo}'
  banco: 'func-saldaro-banco-${sufixo}'
  apim: 'apim-saldaro-${sufixo}'
  keyVault: 'kv-saldaro-${sufixo}'
  eventGridTopic: 'evgt-saldaro-faturas-${sufixo}'
  eventSubscription: 'evgs-notificar-parceiro'
  logAnalytics: 'log-saldaro-${sufixo}'
  appInsights: 'appi-saldaro-${sufixo}'
  actionGroup: 'ag-saldaro'
  alertaHeartbeat: 'alrt-heartbeat-ausente'
  alertaDlq: 'alrt-dlq-confirmacoes'
}

// Containers do pacote de deploy do Flex Consumption (um por app).
var containerDeployHub = 'deploy-hub'
var containerDeployBanco = 'deploy-banco'

// Path público no APIM: https://apim-saldaro-<sufixo>.azure-api.net/saldaro
// (API "saldaro", faturas) e .../saldaro/webhooks (API "saldaro-webhooks").
var apiPath = 'saldaro'

var tags = {
  projeto: 'saldaro-hub'
  ambiente: 'lab'
}

// -----------------------------------------------------------------------------
// Módulos
// -----------------------------------------------------------------------------

module observabilidade 'modules/observabilidade.bicep' = {
  name: 'observabilidade'
  params: {
    location: location
    tags: tags
    logAnalyticsName: nomes.logAnalytics
    appInsightsName: nomes.appInsights
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    tags: tags
    storageAccountName: nomes.storage
    containersDeploy: plano == 'Flex' ? [containerDeployHub, containerDeployBanco] : []
  }
}

module serviceBus 'modules/service-bus.bicep' = {
  name: 'service-bus'
  params: {
    location: location
    tags: tags
    namespaceName: nomes.serviceBus
    disableLocalAuth: identidadeGerenciada
    filaComSessions: filaComSessions
    filaDupDetection: filaDupDetection
    maxDeliveryCount: maxDeliveryCount
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    location: location
    tags: tags
    keyVaultName: nomes.keyVault
    criarSegredos: identidadeGerenciada
    webhookSecret: webhookSecret
    bancoApiKey: bancoApiKey
  }
}

module eventGrid 'modules/event-grid.bicep' = {
  name: 'event-grid'
  params: {
    location: location
    tags: tags
    topicName: nomes.eventGridTopic
    eventSubscriptionName: nomes.eventSubscription
    serviceBusNamespaceName: serviceBus.outputs.namespaceName
    filaDestino: serviceBus.outputs.filaNotificacoesParceiro
  }
}

module apim 'modules/apim.bicep' = {
  name: 'apim'
  params: {
    location: location
    tags: tags
    apimName: nomes.apim
    appInsightsName: observabilidade.outputs.appInsightsName
  }
}

module banco 'modules/banco.bicep' = {
  name: 'banco'
  params: {
    location: location
    tags: tags
    plano: plano
    nomePlano: nomes.planoBanco
    nomeApp: nomes.banco
    containerDeploy: containerDeployBanco
    storageAccountName: storage.outputs.storageAccountName
    appInsightsName: observabilidade.outputs.appInsightsName
    webhookSecret: webhookSecret
    bancoApiKey: bancoApiKey
  }
}

// URLs de app sempre a partir de properties.defaultHostName (nunca montadas à
// mão: o Flex Consumption pode gerar hostnames com sufixo próprio).
var bancoBaseUrl = 'https://${banco.outputs.defaultHostName}/api'
var apimBaseUrl = '${apim.outputs.gatewayUrl}/${apiPath}'

module hub 'modules/hub.bicep' = {
  name: 'hub'
  params: {
    location: location
    tags: tags
    plano: plano
    nomePlano: nomes.planoHub
    nomeApp: nomes.hub
    containerDeploy: containerDeployHub
    storageAccountName: storage.outputs.storageAccountName
    tableServiceUri: storage.outputs.tableServiceUri
    serviceBusNamespaceName: serviceBus.outputs.namespaceName
    serviceBusFqdn: serviceBus.outputs.fqdn
    eventGridTopicName: eventGrid.outputs.topicName
    keyVaultName: keyVault.outputs.keyVaultName
    appInsightsName: observabilidade.outputs.appInsightsName
    bancoBaseUrl: bancoBaseUrl
    apimBaseUrl: apimBaseUrl
    identidadeGerenciada: identidadeGerenciada
    webhookSecret: webhookSecret
    bancoApiKey: bancoApiKey
    asyncRequestReply: asyncRequestReply
    webhookHmac: webhookHmac
    inbox: inbox
    workerComSessao: workerComSessao
    dlqComMotivo: dlqComMotivo
    retryAgendado: retryAgendado
    heartbeat: heartbeat
  }
}

var hubBaseUrl = 'https://${hub.outputs.defaultHostName}/api'

module apimApi 'modules/apim-api.bicep' = {
  name: 'apim-api'
  params: {
    apimName: apim.outputs.apimName
    apiPath: apiPath
    hubBaseUrl: hubBaseUrl
    rateLimit: rateLimit
  }
}

module rolesHub 'modules/roles-hub.bicep' = if (identidadeGerenciada) {
  name: 'roles-hub'
  params: {
    hubAppId: hub.outputs.appId
    hubPrincipalId: hub.outputs.principalId
    serviceBusNamespaceName: serviceBus.outputs.namespaceName
    storageAccountName: storage.outputs.storageAccountName
    eventGridTopicName: eventGrid.outputs.topicName
    keyVaultName: keyVault.outputs.keyVaultName
  }
}

module rolesAluno 'modules/roles-aluno.bicep' = if (!empty(principalIdAluno)) {
  name: 'roles-aluno'
  params: {
    principalIdAluno: principalIdAluno
    serviceBusNamespaceName: serviceBus.outputs.namespaceName
    storageAccountName: storage.outputs.storageAccountName
    keyVaultName: keyVault.outputs.keyVaultName
  }
}

module alertasMonitor 'modules/alertas.bicep' = if (alertas) {
  name: 'alertas'
  params: {
    location: location
    tags: tags
    actionGroupName: nomes.actionGroup
    alertaHeartbeatName: nomes.alertaHeartbeat
    alertaDlqName: nomes.alertaDlq
    emailAlerta: emailAlerta
    appInsightsName: observabilidade.outputs.appInsightsName
    serviceBusNamespaceName: serviceBus.outputs.namespaceName
    filaConfirmacoesBanco: serviceBus.outputs.filaConfirmacoesBanco
  }
}

// -----------------------------------------------------------------------------
// Outputs (o deploy.ps1 usa para a tabela de recursos e o scripts/.env.local)
// -----------------------------------------------------------------------------

output resourceGroupName string = resourceGroup().name
output sufixo string = sufixo
output location string = location
output plano string = plano

output serviceBusNamespaceName string = serviceBus.outputs.namespaceName
output filaFaturasRecebidas string = serviceBus.outputs.filaFaturasRecebidas
output filaConfirmacoesBanco string = serviceBus.outputs.filaConfirmacoesBanco
output filaNotificacoesParceiro string = serviceBus.outputs.filaNotificacoesParceiro
output storageAccountName string = storage.outputs.storageAccountName
output tabelas array = storage.outputs.tabelas
output filaStorageBanco string = storage.outputs.filaBancoPagamentos
output hubPlanName string = nomes.planoHub
output bancoPlanName string = nomes.planoBanco
output hubFunctionAppName string = hub.outputs.nomeApp
output bancoFunctionAppName string = banco.outputs.nomeApp
output apimName string = apim.outputs.apimName
output apimApiName string = apimApi.outputs.apiFaturasName
output apimApiWebhooksName string = apimApi.outputs.apiWebhooksName
output apimOperacoes array = apimApi.outputs.operacoes
// true só no cenario-1-pronto: a API saldaro exige a subscription da empresa
// cliente. As CHAVES nunca saem em output: leia com az rest .../listSecrets.
output apimSubscriptionsHabilitadas bool = apimApi.outputs.subscriptionsHabilitadas
output apimSubscriptions array = apimApi.outputs.subscriptions
output keyVaultName string = keyVault.outputs.keyVaultName
output eventGridTopicName string = eventGrid.outputs.topicName
output eventSubscriptionName string = eventGrid.outputs.eventSubscriptionName
output logAnalyticsName string = observabilidade.outputs.logAnalyticsName
output appInsightsName string = observabilidade.outputs.appInsightsName
output actionGroupName string = alertas ? nomes.actionGroup : ''
output alertaHeartbeatName string = alertas ? nomes.alertaHeartbeat : ''
output alertaDlqName string = alertas ? nomes.alertaDlq : ''

output hubBaseUrl string = hubBaseUrl
output bancoBaseUrl string = bancoBaseUrl
output apimBaseUrl string = apimBaseUrl
output serviceBusFqdn string = serviceBus.outputs.fqdn
output tableServiceUri string = storage.outputs.tableServiceUri
output eventGridTopicEndpoint string = eventGrid.outputs.topicEndpoint

// Declarado no Bicep para o check-drift.ps1 comparar com o que existe.
output filasDeclaradas array = serviceBus.outputs.filasDeclaradas
output namespaceDeclarado object = serviceBus.outputs.namespaceDeclarado

// Estado aplicado (para conferência e para a tabela do deploy.ps1).
output toggles object = {
  asyncRequestReply: asyncRequestReply
  webhookHmac: webhookHmac
  inbox: inbox
  filaComSessions: filaComSessions
  filaDupDetection: filaDupDetection
  workerComSessao: workerComSessao
  dlqComMotivo: dlqComMotivo
  retryAgendado: retryAgendado
  maxDeliveryCount: maxDeliveryCount
  rateLimit: rateLimit
  identidadeGerenciada: identidadeGerenciada
  heartbeat: heartbeat
  alertas: alertas
}
