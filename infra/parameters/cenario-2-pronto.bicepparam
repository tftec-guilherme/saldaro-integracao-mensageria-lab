// Cenário 2 pronto (3.2, timeout): baseline + asyncRequestReply (202 + fila).
// Também é o estado inicial do cenário 3.

using '../main.bicep'

// Valores do aluno. O deploy.ps1 exporta estas variáveis de ambiente só para o
// próprio processo (nada vai para arquivo; os segredos também não aparecem na
// linha de comando). Para um what-if manual, defina-as antes ou use
// "pwsh ./scripts/deploy.ps1 -Cenario N [-Pronto] -SomenteValidar".
// O sufixo 'troque' é só um marcador para o arquivo compilar sozinho.
param sufixo = readEnvironmentVariable('SALDARO_SUFIXO', 'troque')
param location = readEnvironmentVariable('SALDARO_LOCATION', 'brazilsouth')
param plano = readEnvironmentVariable('SALDARO_PLANO', 'Flex')
param principalIdAluno = readEnvironmentVariable('SALDARO_PRINCIPAL_ID_ALUNO', '')
param emailAlerta = readEnvironmentVariable('SALDARO_EMAIL_ALERTA', '')
param webhookSecret = readEnvironmentVariable('SALDARO_WEBHOOK_SECRET', '')
param bancoApiKey = readEnvironmentVariable('SALDARO_BANCO_API_KEY', '')

// Toggles deste estado (seção 4 do contrato do kit)
param asyncRequestReply = true          // 3.2: 202 + fila
param webhookHmac = false               // 3.3: assinatura HMAC
param inbox = false                     // 3.3: baixa idempotente
param filaComSessions = false           // 3.3: imutável
param filaDupDetection = false          // 3.3: imutável
param workerComSessao = false           // 3.3: trigger com sessão
param dlqComMotivo = false              // 3.4: DLQ com motivo
param retryAgendado = false             // 3.4: retry agendado
param maxDeliveryCount = 10             // 3.4: baseline 10, consertado 3
param rateLimit = false                 // 3.1: 60/min em POST /faturas
param identidadeGerenciada = false      // 3.5: MI + Key Vault
param heartbeat = false                 // 3.6: heartbeat ao concluir
param alertas = false                   // 3.6: action group + alertas
