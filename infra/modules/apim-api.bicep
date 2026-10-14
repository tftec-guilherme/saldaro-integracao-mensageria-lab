// =============================================================================
// APIs do APIM: "saldaro" (faturas) e "saldaro-webhooks" (webhook do banco)
// =============================================================================
// Rate limit no tier Consumption (verificado no Microsoft Learn em 2026-10-13):
// a política rate-limit-by-key NÃO é suportada no Consumption (só nos tiers
// clássicos Developer/Basic/Standard/Premium e nos v2), e rate-limit só conta
// chamadas feitas COM chave de subscription. Por isso (decisão do professor da
// aula, registrada na ADR 0007 e na Atualização 1 do contrato, seção 3):
//
// - As operações ficam em duas APIs. A "saldaro" (path saldaro) tem as
//   operações de faturas; a "saldaro-webhooks" (path saldaro/webhooks) tem só o
//   POST /banco, nunca exige subscription e mantém a URL do contrato
//   {apimBaseUrl}/webhooks/banco.
// - Com rateLimit = true (só no cenario-1-pronto), a API "saldaro" passa a
//   exigir subscription, cada empresa cliente usa a sua (cliente-a, cliente-b;
//   chave no header Ocp-Apim-Subscription-Key) e POST /faturas ganha
//   <rate-limit calls="60" renewal-period="60" /> (429 + Retry-After). Nos
//   demais estados: sem subscription e sem rate limit.
//
// Num tier que suporta, a política por cliente seria (para comparar no 3.1):
//   <rate-limit-by-key calls="60" renewal-period="60"
//     counter-key="@(context.Request.Headers.GetValueOrDefault("x-cliente-id","anonimo"))" />
// =============================================================================

@description('Nome do APIM.')
param apimName string

@description('Path público da API de faturas (saldaro). A de webhooks fica em <apiPath>/webhooks.')
param apiPath string

@description('https://<defaultHostName do hub>/api')
param hubBaseUrl string

@description('3.1: exige subscription na API saldaro e limita POST /faturas a 60 chamadas por minuto por subscription.')
param rateLimit bool

var nomesSubscriptions = [
  'cliente-a'
  'cliente-b'
]

resource apim 'Microsoft.ApiManagement/service@2024-05-01' existing = {
  name: apimName
}

// CONCEITO: o gateway espera no máximo 30 s pela resposta do hub. É esse corte
// que o Incidente 1 (3.2) mostra. Sem on-error, o APIM devolveria 500 no
// timeout; aqui o timeout do forward-request (context.LastError com Source
// "forward-request" e Reason "Timeout", nomes do Microsoft Learn) vira
// 504 Gateway Timeout, sempre igual. Os demais erros (ex.: o 429 do rate
// limit) seguem o comportamento padrão (<base />).
var politicaApi = '''
<policies>
  <inbound>
    <base />
  </inbound>
  <backend>
    <forward-request timeout="30" />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
    <choose>
      <when condition="@(context.LastError.Source == "forward-request" && context.LastError.Reason == "Timeout")">
        <return-response>
          <set-status code="504" reason="Gateway Timeout" />
          <set-header name="Content-Type" exists-action="override">
            <value>application/json; charset=utf-8</value>
          </set-header>
          <set-body>{"erro":"o gateway esperou 30 s e desistiu"}</set-body>
        </return-response>
      </when>
    </choose>
  </on-error>
</policies>
'''

// 60 chamadas por 60 s por subscription; ao estourar, 429 com Retry-After.
var politicaPostFaturaComRateLimit = '''
<policies>
  <inbound>
    <base />
    <rate-limit calls="60" renewal-period="60" />
  </inbound>
  <backend>
    <base />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
  </on-error>
</policies>
'''

// Sem rate limit. A política da operação é SEMPRE declarada: com rateLimit =
// false ela sobrescreve a versão com limite (o deploy é incremental e não
// apagaria uma política que simplesmente deixasse de ser declarada).
var politicaPostFaturaSemRateLimit = '''
<policies>
  <inbound>
    <base />
  </inbound>
  <backend>
    <base />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
  </on-error>
</policies>
'''

var parametroId = [
  {
    name: 'id'
    type: 'string'
    required: true
    description: 'faturaId (GUID)'
  }
]

var operacoesFaturas = [
  {
    name: 'post-fatura'
    displayName: 'post-fatura'
    method: 'POST'
    urlTemplate: '/faturas'
    templateParameters: []
  }
  {
    name: 'get-fatura'
    displayName: 'get-fatura'
    method: 'GET'
    urlTemplate: '/faturas/{id}'
    templateParameters: parametroId
  }
  {
    name: 'get-fatura-status'
    displayName: 'get-fatura-status'
    method: 'GET'
    urlTemplate: '/faturas/{id}/status'
    templateParameters: parametroId
  }
]

// ----------------------------------------------------------------------------
// API "saldaro": faturas
// ----------------------------------------------------------------------------

resource apiFaturas 'Microsoft.ApiManagement/service/apis@2024-05-01' = {
  parent: apim
  name: 'saldaro'
  properties: {
    displayName: 'Saldaro Hub - faturas'
    description: 'Faturas da Saldaro (lab). Com rateLimit, exige a subscription da empresa cliente.'
    path: apiPath
    protocols: [
      'https'
    ]
    serviceUrl: hubBaseUrl
    // Simplificação de lab: sem chave, exceto no estado pronto do 3.1, em que a
    // subscription identifica a empresa cliente para o rate limit.
    subscriptionRequired: rateLimit
  }
}

resource apiFaturasPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-05-01' = {
  parent: apiFaturas
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: politicaApi
  }
}

resource operacoes 'Microsoft.ApiManagement/service/apis/operations@2024-05-01' = [
  for operacao in operacoesFaturas: {
    parent: apiFaturas
    name: operacao.name
    properties: {
      displayName: operacao.displayName
      method: operacao.method
      urlTemplate: operacao.urlTemplate
      templateParameters: operacao.templateParameters
    }
  }
]

resource postFaturaPolicy 'Microsoft.ApiManagement/service/apis/operations/policies@2024-05-01' = {
  parent: operacoes[0]
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: rateLimit ? politicaPostFaturaComRateLimit : politicaPostFaturaSemRateLimit
  }
}

// Uma subscription por empresa cliente (os mesmos ids do header x-cliente-id),
// com escopo só na API de faturas. As chaves são geradas pelo APIM e NUNCA vão
// para output nem para o .env.local: quem precisa delas lê na hora (Portal ou
// az rest POST .../subscriptions/cliente-a/listSecrets).
resource subscriptionsClientes 'Microsoft.ApiManagement/service/subscriptions@2024-05-01' = [
  for cliente in nomesSubscriptions: if (rateLimit) {
    parent: apim
    name: cliente
    properties: {
      displayName: cliente
      scope: '/apis/${apiFaturas.name}'
      state: 'active'
      allowTracing: false
    }
  }
]

// ----------------------------------------------------------------------------
// API "saldaro-webhooks": o banco chama POST {apimBaseUrl}/webhooks/banco
// ----------------------------------------------------------------------------

resource apiWebhooks 'Microsoft.ApiManagement/service/apis@2024-05-01' = {
  parent: apim
  name: 'saldaro-webhooks'
  properties: {
    displayName: 'Saldaro Hub - webhooks'
    description: 'Webhook de confirmação do banco parceiro (lab). Nunca exige subscription: quem autentica é a assinatura HMAC.'
    path: '${apiPath}/webhooks'
    protocols: [
      'https'
    ]
    serviceUrl: '${hubBaseUrl}/webhooks'
    subscriptionRequired: false
  }
}

resource apiWebhooksPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-05-01' = {
  parent: apiWebhooks
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: politicaApi
  }
}

resource postWebhookBanco 'Microsoft.ApiManagement/service/apis/operations@2024-05-01' = {
  parent: apiWebhooks
  name: 'post-webhook-banco'
  properties: {
    displayName: 'post-webhook-banco'
    method: 'POST'
    urlTemplate: '/banco'
    templateParameters: []
  }
}

output apiFaturasName string = apiFaturas.name
output apiWebhooksName string = apiWebhooks.name
output operacoes array = concat(map(operacoesFaturas, operacao => operacao.name), [postWebhookBanco.name])
output subscriptionsHabilitadas bool = rateLimit
output subscriptions array = rateLimit ? nomesSubscriptions : []
