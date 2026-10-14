// =============================================================================
// Storage account do lab (stsaldaro<sufixo>)
// =============================================================================
// Um único storage atende:
// - o host dos dois Function Apps (AzureWebJobsStorage);
// - as tabelas do hub (faturas, inbox, idempotencia, conciliacao) e do banco
//   (cobrancas);
// - a fila de storage banco-pagamentos (webhooks que o banco simulado envia);
// - os containers de deploy do Flex Consumption (um por app).
// =============================================================================

@description('Região dos recursos.')
param location string

@description('Tags aplicadas a todos os recursos.')
param tags object

@description('Nome do storage account (stsaldaro<sufixo>: só letras minúsculas e dígitos, até 24 caracteres).')
@minLength(3)
@maxLength(24)
param storageAccountName string

@description('Containers de blob para o pacote de deploy do Flex Consumption (vazio no plano Consumption).')
param containersDeploy array

var tabelas = [
  'faturas'
  'inbox'
  'idempotencia'
  'conciliacao'
  'cobrancas'
]

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    // CONCEITO: o baseline usa connection string (chave da conta) de propósito.
    // No 3.5 só os dados da aplicação (tabelas) migram para identidade; o
    // AzureWebJobsStorage continua com chave (compromisso documentado).
    allowSharedKeyAccess: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2025-01-01' = {
  parent: storage
  name: 'default'
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-01-01' = [
  for container in containersDeploy: {
    parent: blobService
    name: container
    properties: {
      publicAccess: 'None'
    }
  }
]

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2025-01-01' = {
  parent: storage
  name: 'default'
}

resource tables 'Microsoft.Storage/storageAccounts/tableServices/tables@2025-01-01' = [
  for tabela in tabelas: {
    parent: tableService
    name: tabela
  }
]

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2025-01-01' = {
  parent: storage
  name: 'default'
}

resource filaBancoPagamentos 'Microsoft.Storage/storageAccounts/queueServices/queues@2025-01-01' = {
  parent: queueService
  name: 'banco-pagamentos'
}

// O endpoint vem com barra no fim; o contrato usa a forma sem barra.
var endpointTabela = storage.properties.primaryEndpoints.table

output storageAccountName string = storage.name
output blobEndpoint string = storage.properties.primaryEndpoints.blob
output tableServiceUri string = endsWith(endpointTabela, '/')
  ? take(endpointTabela, max(length(endpointTabela) - 1, 0))
  : endpointTabela
output filaBancoPagamentos string = filaBancoPagamentos.name
output tabelas array = [for (tabela, i) in tabelas: tables[i].name]
