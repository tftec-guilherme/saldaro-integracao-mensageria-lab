// =============================================================================
// Alertas (só com alertas = true; cenário 3.6)
// =============================================================================
// - alrt-heartbeat-ausente: alerta por AUSÊNCIA de sinal (dead man's switch).
//   A cada 5 min conta os conciliacao.heartbeat dos últimos 15 min; dispara
//   quando a contagem é 0. O heartbeat só é emitido quando a conciliação
//   CONCLUI com sucesso, então "rodou e falhou" também dispara.
// - alrt-dlq-confirmacoes: métrica DeadletteredMessages > 0 na entidade
//   sbq-confirmacoes-banco (reaproveita o Incidente 3).
//
// Custo: o alerta de log com frequência de 5 min é o item mais caro do lab
// (centavos por dia); apague o RG ao terminar.
// =============================================================================

@description('Região dos recursos (o alerta de log é regional; os demais são globais).')
param location string

@description('Tags aplicadas a todos os recursos.')
param tags object

@description('Nome do action group (ag-saldaro).')
param actionGroupName string

@description('Nome do alerta de heartbeat (alrt-heartbeat-ausente).')
param alertaHeartbeatName string

@description('Nome do alerta de DLQ (alrt-dlq-confirmacoes).')
param alertaDlqName string

@description('E-mail que recebe os alertas. Vazio = action group sem destinatário (o alerta dispara, mas ninguém recebe).')
param emailAlerta string

@description('Application Insights consultado pelo alerta de heartbeat.')
param appInsightsName string

@description('Namespace do Service Bus monitorado pelo alerta de DLQ.')
param serviceBusNamespaceName string

@description('Fila monitorada (sbq-confirmacoes-banco).')
param filaConfirmacoesBanco string

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource namespace 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: serviceBusNamespaceName
}

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: actionGroupName
  location: 'Global'
  tags: tags
  properties: {
    groupShortName: 'saldaro'
    enabled: true
    emailReceivers: empty(emailAlerta)
      ? []
      : [
          {
            name: 'email-aluno'
            emailAddress: emailAlerta
            useCommonAlertSchema: true
          }
        ]
  }
}

resource alertaHeartbeat 'Microsoft.Insights/scheduledQueryRules@2023-12-01' = {
  name: alertaHeartbeatName
  location: location
  tags: tags
  kind: 'LogAlert'
  properties: {
    displayName: alertaHeartbeatName
    description: 'Nenhum conciliacao.heartbeat nos últimos 15 min: a conciliação parou (ou falhou) em silêncio.'
    severity: 1
    enabled: true
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      allOf: [
        {
          // Escopo do App Insights: tabela customEvents (no Log Analytics, AppEvents).
          query: 'customEvents | where name == "conciliacao.heartbeat"'
          timeAggregation: 'Count'
          // CONCEITO: dispara com ZERO linhas na janela (ausência de sinal).
          operator: 'LessThan'
          threshold: 1
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    // Resolve sozinho quando o heartbeat volta.
    autoMitigate: true
    // Um App Insights recém-criado ainda não tem eventos; não validar a consulta.
    skipQueryValidation: true
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

resource alertaDlq 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: alertaDlqName
  location: 'global'
  tags: tags
  properties: {
    description: 'Mensagens na DLQ da sbq-confirmacoes-banco: rode a triagem (triagem-dlq.ps1).'
    severity: 2
    enabled: true
    scopes: [
      namespace.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          criterionType: 'StaticThresholdCriterion'
          name: 'DeadletteredMaiorQueZero'
          metricNamespace: 'Microsoft.ServiceBus/namespaces'
          metricName: 'DeadletteredMessages'
          dimensions: [
            {
              name: 'EntityName'
              operator: 'Include'
              values: [
                filaConfirmacoesBanco
              ]
            }
          ]
          operator: 'GreaterThan'
          threshold: 0
          timeAggregation: 'Maximum'
        }
      ]
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

output actionGroupName string = actionGroup.name
output alertaHeartbeatName string = alertaHeartbeat.name
output alertaDlqName string = alertaDlq.name
