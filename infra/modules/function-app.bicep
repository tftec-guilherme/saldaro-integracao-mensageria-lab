// =============================================================================
// Function App genérico (.NET 10 isolated): plano + site
// =============================================================================
// Usado pelo hub e pelo banco simulado. Dois planos possíveis:
//
// - Flex (padrão): Flex Consumption (FC1, Linux). Runtime e escala ficam em
//   functionAppConfig; o pacote vai para um container de blob. Os settings
//   FUNCTIONS_WORKER_RUNTIME, FUNCTIONS_EXTENSION_VERSION, WEBSITE_CONTENT*,
//   WEBSITE_RUN_FROM_PACKAGE e netFrameworkVersion NÃO são permitidos aqui.
// - Consumption (plano B): Windows Consumption (Y1), legado mas GA. Precisa de
//   FUNCTIONS_WORKER_RUNTIME, netFrameworkVersion e do content share (Azure
//   Files) no storage.
//
// Armadilha: não dá para migrar um app entre Flex e Consumption no lugar; para
// trocar de plano, apague o RG (limpar.ps1) e faça o deploy de novo.
// =============================================================================

@description('Região dos recursos.')
param location string

@description('Tags aplicadas a todos os recursos.')
param tags object

@description('Flex (Flex Consumption, FC1) ou Consumption (Y1, Windows).')
@allowed([
  'Flex'
  'Consumption'
])
param plano string

@description('Nome do plano (asp-saldaro-<papel>-<sufixo>).')
param nomePlano string

@description('Nome do Function App (func-saldaro-<papel>-<sufixo>).')
param nomeApp string

@description('true cria a identidade system-assigned do app.')
param identidadeSistema bool

@description('Storage do host (AzureWebJobsStorage) e do pacote de deploy.')
param storageAccountName string

@description('Container de blob do pacote de deploy (só Flex).')
param containerDeploy string

@description('Application Insights que recebe a telemetria.')
param appInsightsName string

@description('Flex: máximo de instâncias POR GRUPO de escala (HTTP e cada trigger não HTTP escalam separados).')
@minValue(1)
@maxValue(1000)
param maximumInstanceCount int

@description('Settings específicos do app (os de plataforma são montados aqui).')
@secure()
param appSettings object

var flex = plano == 'Flex'

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' existing = {
  name: storageAccountName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'

var settingsComuns = {
  AzureWebJobsStorage: storageConnectionString
  APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
}

// Só no plano B (Windows Consumption). No Flex estes settings são proibidos.
var settingsConsumption = flex
  ? {}
  : {
      FUNCTIONS_EXTENSION_VERSION: '~4'
      FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
      WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: storageConnectionString
      // Nome fixo e determinístico: o deploy reaplica todos os settings a cada
      // execução, e um content share "trocado" deixaria o app sem código.
      WEBSITE_CONTENTSHARE: toLower(nomeApp)
      // O config-zip publica como pacote montado (run from package).
      WEBSITE_RUN_FROM_PACKAGE: '1'
    }

// CONCEITO: o deploy reaplica a lista INTEIRA de app settings. Um setting
// criado à mão no Portal some no próximo deploy.ps1.
var todosSettings = union(settingsComuns, settingsConsumption, appSettings)
var listaSettings = map(items(todosSettings), item => {
  name: item.key
  value: item.value
})

resource plan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: nomePlano
  location: location
  tags: tags
  kind: 'functionapp'
  sku: flex
    ? {
        name: 'FC1'
        tier: 'FlexConsumption'
      }
    : {
        name: 'Y1'
        tier: 'Dynamic'
      }
  properties: {
    // Flex Consumption é Linux (reserved = true); o Y1 do plano B é Windows.
    reserved: flex
  }
}

resource app 'Microsoft.Web/sites@2024-11-01' = {
  name: nomeApp
  location: location
  tags: tags
  kind: flex ? 'functionapp,linux' : 'functionapp'
  identity: {
    type: identidadeSistema ? 'SystemAssigned' : 'None'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    functionAppConfig: flex
      ? {
          deployment: {
            storage: {
              type: 'blobContainer'
              value: '${storage.properties.primaryEndpoints.blob}${containerDeploy}'
              authentication: {
                // Reaproveita a connection string do AzureWebJobsStorage: nenhum
                // setting extra além dos do contrato.
                type: 'StorageAccountConnectionString'
                storageAccountConnectionStringName: 'AzureWebJobsStorage'
              }
            }
          }
          scaleAndConcurrency: {
            // CONCEITO: no Free Trial a cota regional do Flex é de 15 núcleos
            // (30.720 MB) somando TODOS os apps Flex da assinatura na região, e o
            // máximo de instâncias vale por grupo de escala (HTTP e cada trigger
            // não HTTP escalam separados). Com 512 MB (0,25 núcleo) por instância,
            // hub (10 por grupo) + banco (5 por grupo) ficam em ~13 núcleos no
            // pior caso; com 2.048 MB (1 núcleo) a mesma escala estouraria a cota.
            instanceMemoryMB: 512
            maximumInstanceCount: maximumInstanceCount
          }
          runtime: {
            name: 'dotnet-isolated'
            version: '10.0'
          }
        }
      : null
    siteConfig: flex
      ? {
          minTlsVersion: '1.2'
          appSettings: listaSettings
        }
      : {
          minTlsVersion: '1.2'
          ftpsState: 'Disabled'
          netFrameworkVersion: 'v10.0'
          use32BitWorkerProcess: false
          // Mesmo teto de escala do Flex, para o comportamento do lab ser parecido.
          functionAppScaleLimit: maximumInstanceCount
          appSettings: listaSettings
        }
  }
}

output nomeApp string = app.name
output appId string = app.id
output defaultHostName string = app.properties.defaultHostName
// Sem identidade (identidadeSistema = false, caso do banco simulado), o ARM nem devolve a propriedade
// identity, e app.identity.?principalId falha na avaliação da saída (DeploymentOutputEvaluationFailed).
// Por isso o acesso seguro começa em identity.
output principalId string = app.?identity.?principalId ?? ''
