// =============================================================================
// Observabilidade: Log Analytics + Application Insights (workspace-based)
// =============================================================================
// Os dois Function Apps (hub e banco) e o APIM enviam telemetria para o MESMO
// App Insights: é isso que permite seguir uma fatura de ponta a ponta pelo
// operation_Id (Lab 0) e montar os KQL de kql/.
// =============================================================================

@description('Região dos recursos.')
param location string

@description('Tags aplicadas a todos os recursos.')
param tags object

@description('Nome do workspace do Log Analytics (log-saldaro-<sufixo>).')
param logAnalyticsName string

@description('Nome do Application Insights (appi-saldaro-<sufixo>).')
param appInsightsName string

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    // Retenção curta: o lab vive algumas horas e o RG é apagado no fim.
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    // CONCEITO: workspace-based. As tabelas customEvents/requests (escopo do App
    // Insights) aparecem como AppEvents/AppRequests no Log Analytics.
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output logAnalyticsName string = logAnalytics.name
output appInsightsName string = appInsights.name
output appInsightsId string = appInsights.id
