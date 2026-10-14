// =============================================================================
// Roles do aluno logado (se principalIdAluno não for vazio)
// =============================================================================
// O deploy.ps1 lê o object ID com "az ad signed-in-user show" e passa aqui.
// - Azure Service Bus Data Owner no namespace: Service Bus Explorer do Portal e
//   ferramentas de triagem continuam funcionando com disableLocalAuth (3.5).
// - Storage Table Data Contributor no storage: ver/editar as tabelas por
//   identidade (Storage browser com Entra ID).
// - Key Vault Secrets Officer no Key Vault: criar e rotacionar segredos no 3.5.
//
// Nomes guid(escopo, aluno, role) determinísticos: redeploy idempotente.
// =============================================================================

@description('Object ID do aluno (usuário do Entra ID).')
param principalIdAluno string

@description('Namespace do Service Bus.')
param serviceBusNamespaceName string

@description('Storage do lab.')
param storageAccountName string

@description('Key Vault do lab.')
param keyVaultName string

var roles = {
  serviceBusDataOwner: '090c5cfd-751d-490a-894a-3ce6f1109419'
  storageTableDataContributor: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
  keyVaultSecretsOfficer: 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
}

resource namespace 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: serviceBusNamespaceName
}

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' existing = {
  name: storageAccountName
}

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: keyVaultName
}

resource serviceBusOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(namespace.id, principalIdAluno, roles.serviceBusDataOwner)
  scope: namespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.serviceBusDataOwner)
    principalId: principalIdAluno
    principalType: 'User'
  }
}

resource tabelas 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, principalIdAluno, roles.storageTableDataContributor)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.storageTableDataContributor)
    principalId: principalIdAluno
    principalType: 'User'
  }
}

resource segredos 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, principalIdAluno, roles.keyVaultSecretsOfficer)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.keyVaultSecretsOfficer)
    principalId: principalIdAluno
    principalType: 'User'
  }
}
