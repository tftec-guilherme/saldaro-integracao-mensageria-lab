// =============================================================================
// Service Bus: namespace Standard + as três filas do fio condutor
// =============================================================================
// As filas são declaradas numa lista (var filas) para que a MESMA declaração
// sirva de duas coisas: criar as filas e alimentar o check-drift.ps1, que
// compara o que existe no namespace com o output filasDeclaradas.
// =============================================================================

@description('Região dos recursos.')
param location string

@description('Tags aplicadas a todos os recursos.')
param tags object

@description('Nome do namespace (sb-saldaro-<sufixo>).')
param namespaceName string

@description('3.5: true desliga SAS/connection string no namespace (só Entra ID / identidade).')
param disableLocalAuth bool

@description('3.3: sbq-confirmacoes-banco com sessions (SessionId = faturaId). IMUTÁVEL numa fila existente.')
param filaComSessions bool

@description('3.3: sbq-confirmacoes-banco com duplicate detection (MessageId = idEvento). IMUTÁVEL numa fila existente.')
param filaDupDetection bool

@description('3.4: tentativas de entrega da sbq-confirmacoes-banco antes da DLQ (baseline 10; consertado 3).')
@minValue(1)
param maxDeliveryCount int

// CONCEITO: estas são as propriedades "declaradas" de cada fila. O
// check-drift.ps1 compara exatamente estas chaves com as filas reais.
var filas = [
  {
    name: 'sbq-faturas-recebidas'
    properties: {
      lockDuration: 'PT1M'
      maxDeliveryCount: 10
      requiresSession: false
      requiresDuplicateDetection: false
      deadLetteringOnMessageExpiration: false
    }
  }
  {
    name: 'sbq-confirmacoes-banco'
    properties: {
      lockDuration: 'PT1M'
      maxDeliveryCount: maxDeliveryCount
      // As duas linhas do conserto do Incidente 2 (3.3). O Portal mostra estas
      // caixas desabilitadas numa fila existente: para mudar, é preciso
      // drenar, apagar e recriar a fila (o deploy.ps1 -Pronto faz isso).
      requiresSession: filaComSessions
      requiresDuplicateDetection: filaDupDetection
      // Janela da duplicate detection (10 min, o padrão do Service Bus).
      duplicateDetectionHistoryTimeWindow: 'PT10M'
      deadLetteringOnMessageExpiration: true
    }
  }
  {
    // Destino do Event Grid (FaturaBaixada) e origem do NotificarParceiro.
    name: 'sbq-notificacoes-parceiro'
    properties: {
      lockDuration: 'PT1M'
      maxDeliveryCount: 10
      requiresSession: false
      requiresDuplicateDetection: false
      deadLetteringOnMessageExpiration: false
    }
  }
]

resource namespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    // CONCEITO: no baseline o hub usa a RootManageSharedAccessKey (ingênuo de
    // propósito). Com disableLocalAuth = true nenhuma chave SAS funciona mais.
    disableLocalAuth: disableLocalAuth
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource queues 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = [
  for fila in filas: {
    parent: namespace
    name: fila.name
    properties: fila.properties
  }
]

// serviceBusEndpoint vem como https://<namespace>.servicebus.windows.net:443/
var hostNamespace = replace(split(split(namespace.properties.serviceBusEndpoint, '//')[1], ':')[0], '/', '')

output namespaceName string = namespace.name
output namespaceId string = namespace.id
output fqdn string = hostNamespace
output filaFaturasRecebidas string = queues[0].name
output filaConfirmacoesBanco string = queues[1].name
output filaNotificacoesParceiro string = queues[2].name
output filasDeclaradas array = filas
output namespaceDeclarado object = {
  sku: 'Standard'
  disableLocalAuth: disableLocalAuth
}
