// =============================================================================
// Key Vault (kv-saldaro-<sufixo>) com autorização RBAC
// =============================================================================
// Nasce vazio no baseline. Os segredos WebhookSecret e BancoApiKey só são
// gravados quando identidadeGerenciada = true (3.5): aí o hub troca o valor em
// texto do app setting por uma Key Vault reference.
//
// Armadilha: soft-delete. Apagar o RG não libera o nome do cofre por 7 dias;
// o limpar.ps1 faz o purge (e o deploy.ps1 purga sobras de um RG apagado pelo
// Portal).
// =============================================================================

@description('Região dos recursos.')
param location string

@description('Tags aplicadas a todos os recursos.')
param tags object

@description('Nome do cofre (kv-saldaro-<sufixo>).')
param keyVaultName string

@description('true grava os segredos WebhookSecret e BancoApiKey (só com identidadeGerenciada = true).')
param criarSegredos bool

@description('Segredo HMAC do webhook.')
@secure()
param webhookSecret string

@description('Chave da API do banco simulado.')
@secure()
param bancoApiKey string

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    // CONCEITO: RBAC (Key Vault Secrets User / Officer), não access policies.
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    // Purge protection DESLIGADA de propósito (enablePurgeProtection não é
    // definido): é o que permite ao limpar.ps1 purgar o cofre e reaproveitar o
    // nome. Em produção ela deve ficar LIGADA (e não pode mais ser desligada).
    publicNetworkAccess: 'Enabled'
  }
}

resource segredoWebhook 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = if (criarSegredos) {
  parent: keyVault
  name: 'WebhookSecret'
  properties: {
    value: webhookSecret
  }
}

resource segredoBanco 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = if (criarSegredos) {
  parent: keyVault
  name: 'BancoApiKey'
  properties: {
    value: bancoApiKey
  }
}

output keyVaultName string = keyVault.name
output keyVaultId string = keyVault.id
