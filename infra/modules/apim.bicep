// =============================================================================
// API Management (apim-saldaro-<sufixo>), tier Consumption: serviço + logs
// =============================================================================
// Este módulo cria só o serviço, o logger do App Insights e o diagnóstico. A
// API saldaro fica em apim-api.bicep, porque depende da URL do hub, que por sua
// vez precisa da URL pública do APIM (assim não há dependência circular).
//
// Armadilhas: na primeira vez o Consumption leva alguns minutos para ativar; e
// um APIM apagado fica em soft-delete por 48 h com o nome reservado (o
// limpar.ps1 purga).
// =============================================================================

@description('Região dos recursos.')
param location string

@description('Tags aplicadas a todos os recursos.')
param tags object

@description('Nome do APIM (apim-saldaro-<sufixo>).')
param apimName string

@description('Application Insights que recebe os logs do gateway.')
param appInsightsName string

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource apim 'Microsoft.ApiManagement/service@2024-05-01' = {
  name: apimName
  location: location
  tags: tags
  sku: {
    name: 'Consumption'
    capacity: 0
  }
  properties: {
    // Empresa e e-mail fictícios (domínio .example é reservado para exemplos).
    publisherName: 'Saldaro Pagamentos S.A.'
    publisherEmail: 'lab@saldaro.example'
  }
}

resource logger 'Microsoft.ApiManagement/service/loggers@2024-05-01' = {
  parent: apim
  name: appInsightsName
  properties: {
    loggerType: 'applicationInsights'
    description: 'App Insights do lab (o mesmo dos dois Function Apps)'
    resourceId: appInsights.id
    credentials: {
      connectionString: appInsights.properties.ConnectionString
    }
  }
}

// CONCEITO: diagnóstico "All APIs" com 100% de amostragem (o 429 não pode
// sumir por sampling) e o header x-cliente-id registrado em cada request, para
// o KQL 429-por-cliente.kql agrupar por empresa cliente.
resource diagnostico 'Microsoft.ApiManagement/service/diagnostics@2024-05-01' = {
  parent: apim
  name: 'applicationinsights'
  properties: {
    loggerId: logger.id
    alwaysLog: 'allErrors'
    sampling: {
      samplingType: 'fixed'
      percentage: 100
    }
    httpCorrelationProtocol: 'W3C'
    verbosity: 'information'
    // O KQL agrupa por x-cliente-id; o IP de quem chama não é necessário.
    logClientIp: false
    frontend: {
      request: {
        headers: [
          'x-cliente-id'
        ]
        body: {
          bytes: 0
        }
      }
      response: {
        headers: [
          'Retry-After'
        ]
        body: {
          bytes: 0
        }
      }
    }
    backend: {
      request: {
        headers: [
          'x-cliente-id'
        ]
        body: {
          bytes: 0
        }
      }
      response: {
        headers: []
        body: {
          bytes: 0
        }
      }
    }
  }
}

output apimName string = apim.name
output gatewayUrl string = apim.properties.gatewayUrl
