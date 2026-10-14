// =============================================================================
// Hub (func-saldaro-hub-<sufixo>): app settings do contrato (seção 5.2)
// =============================================================================
// Duas formas de conexão, escolhidas por identidadeGerenciada:
//
// - Baseline (false): connection strings e chaves em texto nos app settings,
//   inclusive a RootManageSharedAccessKey do Service Bus (ingênuo de propósito).
// - 3.5 (true): o hub usa a própria identidade (DefaultAzureCredential):
//   ServiceBusConnection__fullyQualifiedNamespace, TablesConnection__serviceUri,
//   sem EventGridTopicKey, e os segredos viram Key Vault references.
//
// Os toggles do cenário viram Cenario__* (lidos pelo padrão Options como
// Cenario:*) e os dois AzureWebJobs.<Function>.Disabled escolhem qual trigger
// de confirmação fica ativo (um receptor sem sessão numa fila com sessão falha).
// =============================================================================

@description('Região dos recursos.')
param location string

@description('Tags aplicadas a todos os recursos.')
param tags object

@description('Flex ou Consumption.')
param plano string

@description('Nome do plano do hub.')
param nomePlano string

@description('Nome do Function App do hub.')
param nomeApp string

@description('Container de deploy do hub (Flex).')
param containerDeploy string

@description('Storage do lab.')
param storageAccountName string

@description('https://stsaldaro<sufixo>.table.core.windows.net (forma por identidade).')
param tableServiceUri string

@description('Namespace do Service Bus.')
param serviceBusNamespaceName string

@description('FQDN do namespace (forma por identidade).')
param serviceBusFqdn string

@description('Tópico do Event Grid.')
param eventGridTopicName string

@description('Key Vault do lab.')
param keyVaultName string

@description('Application Insights do lab.')
param appInsightsName string

@description('https://<defaultHostName do banco>/api')
param bancoBaseUrl string

@description('https://apim-saldaro-<sufixo>.azure-api.net/saldaro')
param apimBaseUrl string

@description('3.5: conexões por identidade + Key Vault references.')
param identidadeGerenciada bool

@description('Segredo HMAC do webhook.')
@secure()
param webhookSecret string

@description('Chave da API do banco simulado.')
@secure()
param bancoApiKey string

param asyncRequestReply bool
param webhookHmac bool
param inbox bool
param workerComSessao bool
param dlqComMotivo bool
param retryAgendado bool
param heartbeat bool

func texto(valor bool) string => valor ? 'true' : 'false'

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' existing = {
  name: storageAccountName
}

resource namespace 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: serviceBusNamespaceName
}

resource rootManageSharedAccessKey 'Microsoft.ServiceBus/namespaces/authorizationRules@2024-01-01' existing = {
  parent: namespace
  name: 'RootManageSharedAccessKey'
}

resource topic 'Microsoft.EventGrid/topics@2025-02-15' existing = {
  name: eventGridTopicName
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'

// Baseline ingênuo: tudo por connection string e chave em texto.
var settingsBaseline = {
  ServiceBusConnection: rootManageSharedAccessKey.listKeys().primaryConnectionString
  TablesConnection: storageConnectionString
  EventGridTopicKey: topic.listKeys().key1
  BancoApiKey: bancoApiKey
  WebhookSecret: webhookSecret
}

// 3.5: nenhuma connection string de dados nem chave nos app settings.
var settingsIdentidade = {
  ServiceBusConnection__fullyQualifiedNamespace: serviceBusFqdn
  TablesConnection__serviceUri: tableServiceUri
  BancoApiKey: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=BancoApiKey)'
  WebhookSecret: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=WebhookSecret)'
}

var settingsComuns = {
  EventGridTopicEndpoint: topic.properties.endpoint
  BancoBaseUrl: bancoBaseUrl
  UrlBasePublica: apimBaseUrl
  WebhookUrlPublica: '${apimBaseUrl}/webhooks/banco'
  Cenario__AsyncRequestReply: texto(asyncRequestReply)
  Cenario__WebhookHmac: texto(webhookHmac)
  Cenario__Inbox: texto(inbox)
  Cenario__DlqComMotivo: texto(dlqComMotivo)
  Cenario__RetryAgendado: texto(retryAgendado)
  Cenario__Heartbeat: texto(heartbeat)
  'AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled': texto(!workerComSessao)
  'AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled': texto(workerComSessao)
}

module app 'function-app.bicep' = {
  name: 'app-hub'
  params: {
    location: location
    tags: tags
    plano: plano
    nomePlano: nomePlano
    nomeApp: nomeApp
    // O hub sempre tem identidade system-assigned (as roles só entram no 3.5).
    identidadeSistema: true
    storageAccountName: storageAccountName
    containerDeploy: containerDeploy
    appInsightsName: appInsightsName
    // Até 10 instâncias por grupo de escala (veja a conta da cota em function-app.bicep).
    maximumInstanceCount: 10
    appSettings: union(settingsComuns, identidadeGerenciada ? settingsIdentidade : settingsBaseline)
  }
}

output nomeApp string = app.outputs.nomeApp
output appId string = app.outputs.appId
output defaultHostName string = app.outputs.defaultHostName
output principalId string = app.outputs.principalId
