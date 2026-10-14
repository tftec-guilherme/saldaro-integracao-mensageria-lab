// =============================================================================
// Event Grid: tópico FaturaBaixada -> fila sbq-notificacoes-parceiro
// =============================================================================
// O hub publica o CloudEvent Saldaro.Faturas.FaturaBaixada neste tópico; a
// event subscription entrega na fila do Service Bus usando a IDENTIDADE do
// tópico (deliveryWithResourceIdentity). Por isso a entrega continua
// funcionando quando o namespace liga disableLocalAuth (3.5).
// =============================================================================

@description('Região dos recursos.')
param location string

@description('Tags aplicadas a todos os recursos.')
param tags object

@description('Nome do tópico (evgt-saldaro-faturas-<sufixo>).')
param topicName string

@description('Nome da event subscription (evgs-notificar-parceiro).')
param eventSubscriptionName string

@description('Namespace do Service Bus que contém a fila de destino.')
param serviceBusNamespaceName string

@description('Fila de destino (sbq-notificacoes-parceiro).')
param filaDestino string

// Azure Service Bus Data Sender (role built-in)
var roleServiceBusDataSender = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'

resource topic 'Microsoft.EventGrid/topics@2025-02-15' = {
  name: topicName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    inputSchema: 'CloudEventSchemaV1_0'
    publicNetworkAccess: 'Enabled'
    // O baseline publica com a chave do tópico (EventGridTopicKey); no 3.5 o
    // hub passa a publicar com DefaultAzureCredential.
    disableLocalAuth: false
  }
}

resource namespace 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: serviceBusNamespaceName
}

resource fila 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' existing = {
  parent: namespace
  name: filaDestino
}

// CONCEITO: privilégio mínimo. O tópico só pode ENVIAR, e só para esta fila.
resource roleTopicoNaFila 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(fila.id, topic.id, roleServiceBusDataSender)
  scope: fila
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleServiceBusDataSender)
    principalId: topic.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource eventSubscription 'Microsoft.EventGrid/topics/eventSubscriptions@2025-02-15' = {
  parent: topic
  name: eventSubscriptionName
  properties: {
    eventDeliverySchema: 'CloudEventSchemaV1_0'
    deliveryWithResourceIdentity: {
      identity: {
        type: 'SystemAssigned'
      }
      destination: {
        endpointType: 'ServiceBusQueue'
        properties: {
          resourceId: fila.id
        }
      }
    }
  }
  dependsOn: [
    roleTopicoNaFila
  ]
}

output topicName string = topic.name
output topicEndpoint string = topic.properties.endpoint
output eventSubscriptionName string = eventSubscription.name
