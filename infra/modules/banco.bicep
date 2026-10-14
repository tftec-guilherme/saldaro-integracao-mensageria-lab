// =============================================================================
// Banco simulado (func-saldaro-banco-<sufixo>): settings da seção 6
// =============================================================================
// O banco é "o parceiro": não migra para identidade no 3.5. Ele guarda as
// cobranças na tabela cobrancas, enfileira os webhooks em banco-pagamentos e
// assina cada webhook com o WebhookSecret (HMAC-SHA256).
// =============================================================================

@description('Região dos recursos.')
param location string

@description('Tags aplicadas a todos os recursos.')
param tags object

@description('Flex ou Consumption.')
param plano string

@description('Nome do plano do banco.')
param nomePlano string

@description('Nome do Function App do banco.')
param nomeApp string

@description('Container de deploy do banco (Flex).')
param containerDeploy string

@description('Storage do lab.')
param storageAccountName string

@description('Application Insights do lab.')
param appInsightsName string

@description('Segredo HMAC do webhook (o banco assina, o hub valida).')
@secure()
param webhookSecret string

@description('Chave que o banco exige no header x-api-key.')
@secure()
param bancoApiKey string

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' existing = {
  name: storageAccountName
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'

module app 'function-app.bicep' = {
  name: 'app-banco'
  params: {
    location: location
    tags: tags
    plano: plano
    nomePlano: nomePlano
    nomeApp: nomeApp
    identidadeSistema: false
    storageAccountName: storageAccountName
    containerDeploy: containerDeploy
    appInsightsName: appInsightsName
    // Até 5 instâncias por grupo de escala (veja a conta da cota em function-app.bicep).
    maximumInstanceCount: 5
    appSettings: {
      TablesConnection: storageConnectionString
      WebhookSecret: webhookSecret
      BancoApiKey: bancoApiKey
    }
  }
}

output nomeApp string = app.outputs.nomeApp
output defaultHostName string = app.outputs.defaultHostName
