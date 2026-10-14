// =============================================================================
// Roles da identidade do hub (só com identidadeGerenciada = true; cenário 3.5)
// =============================================================================
// CONCEITO: privilégio mínimo. O hub envia e recebe (Sender + Receiver), mas
// NÃO é Data Owner: não consegue criar filas. É isso que faz um "auto-create"
// de fila falhar alto (3.7).
//
// Os nomes guid(escopo, app, role) são determinísticos: redeployar não cria
// role assignment duplicado (idempotente).
//
// Armadilha: a propagação de RBAC leva de segundos a alguns minutos. Até lá o
// hub recebe 401/403 e as Key Vault references ficam sem resolver.
//
// CONCEITO: escala do trigger de Service Bus. Para decidir quantas instâncias
// subir, a extensão lê a contagem da fila pela API de administração, que pede
// Data Owner ou uma role com Microsoft.ServiceBus/namespaces/*/read. Sem essa
// leitura ela cai, em silêncio, para estimar a fila por peek. Por isso o hub
// ganha Reader no namespace: só leitura de gerenciamento, sem ler mensagens e
// sem criar filas. (O worker com sessão parado no primeiro deploy real era
// outra coisa: os triggers sobem antes de as roles valerem; o deploy.ps1
// reinicia o hub depois da propagação.)
// =============================================================================

@description('Resource ID do Function App do hub (entra no guid do nome).')
param hubAppId string

@description('principalId da identidade system-assigned do hub.')
param hubPrincipalId string

@description('Namespace do Service Bus.')
param serviceBusNamespaceName string

@description('Storage do lab (tabelas).')
param storageAccountName string

@description('Tópico do Event Grid.')
param eventGridTopicName string

@description('Key Vault do lab.')
param keyVaultName string

var roles = {
  serviceBusDataSender: '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
  serviceBusDataReceiver: '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
  storageTableDataContributor: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
  eventGridDataSender: 'd5a91429-5739-47e2-a06b-3470a27159e7'
  keyVaultSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
  reader: 'acdd72a7-3385-48ef-bd42-f606fba81ae7'
}

resource namespace 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: serviceBusNamespaceName
}

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' existing = {
  name: storageAccountName
}

resource topic 'Microsoft.EventGrid/topics@2025-02-15' existing = {
  name: eventGridTopicName
}

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: keyVaultName
}

resource serviceBusSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(namespace.id, hubAppId, roles.serviceBusDataSender)
  scope: namespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.serviceBusDataSender)
    principalId: hubPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource serviceBusReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(namespace.id, hubAppId, roles.serviceBusDataReceiver)
  scope: namespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.serviceBusDataReceiver)
    principalId: hubPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Leitura de gerenciamento no namespace, para a escala do trigger (ver o CONCEITO no topo).
resource serviceBusLeituraEscala 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(namespace.id, hubAppId, roles.reader)
  scope: namespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.reader)
    principalId: hubPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource tabelas 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, hubAppId, roles.storageTableDataContributor)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.storageTableDataContributor)
    principalId: hubPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource eventGridSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(topic.id, hubAppId, roles.eventGridDataSender)
  scope: topic
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.eventGridDataSender)
    principalId: hubPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource segredos 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, hubAppId, roles.keyVaultSecretsUser)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.keyVaultSecretsUser)
    principalId: hubPrincipalId
    principalType: 'ServicePrincipal'
  }
}
