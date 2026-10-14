# Saldaro Hub

Glossário da aula hands-on de Integração e Mensageria no Azure, que se passa na Saldaro Pagamentos S.A., um hub de cobrança B2B fictício. Os nomes técnicos citados aqui seguem o contrato do kit (`docs/research/2026-10-13-contrato-do-kit.md`), e as decisões estão em `docs/adr/`.

**Version-anchor:** Q4-2026 · Empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência.

## Linguagem

### Negócio da Saldaro

**Saldaro Pagamentos S.A.**:
Empresa fictícia da aula: um hub de cobrança B2B que liga os ERPs das empresas clientes aos bancos parceiros.
_Evitar_: "o cliente" para falar da Saldaro (cliente, aqui, é a empresa cliente); o nome de qualquer empresa real

**Saldaro Hub** (o hub):
Plataforma da Saldaro que recebe faturas, registra cobranças, processa confirmações, faz a baixa e notifica o parceiro; no kit, a Function App `func-saldaro-hub-<sufixo>`.
_Evitar_: "API" (a API é só a porta de entrada), "middleware", "barramento"

**Saldaro 1.0**:
Geração legada do hub, que só existe na história: monólito com banco relacional usado como fila e um worker único de polling.
_Evitar_: "baseline" (o baseline ingênuo já é a Saldaro 2.0 no primeiro dia)

**Saldaro 2.0**:
Geração do hub que a aula constrói no Azure, consertando uma herança da Saldaro 1.0 por cenário.
_Evitar_: "versão consertada" (a Saldaro 2.0 nasce ingênua, no baseline)

**Empresa cliente** (emissora):
Empresa que contrata a Saldaro e emite faturas para os seus pagadores; no kit, identificada por `clienteId` (header `x-cliente-id`).
_Evitar_: "tenant" (reservado ao contexto técnico), "usuário", "conta"

**ERP**:
Sistema de gestão da empresa cliente, que envia as faturas ao hub e recebe a notificação ao parceiro.
_Evitar_: "cliente" sozinho (a empresa cliente é a dona do ERP)

**Pagador**:
Pessoa jurídica que deve pagar a fatura; não é cliente da Saldaro.
_Evitar_: "sacado" (jargão de boleto), "cliente final"

**Parceiro**:
Banco parceiro ou ERP integrado ao hub por API ou webhook.
_Evitar_: "fornecedor", "integração" (é o canal, não a entidade)

**Banco parceiro**:
Banco que registra as cobranças da Saldaro e confirma os pagamentos por webhook; na aula, é o banco simulado.
_Evitar_: "banco" sozinho onde puder ser confundido com banco de dados

**Jornada da cobrança**:
Fio condutor da aula: ERP → hub → banco parceiro → webhook de confirmação → baixa → notificação ao parceiro.
_Evitar_: "fluxo de pagamento" (a jornada começa na fatura, antes de existir pagamento)

**Fatura**:
Documento de cobrança que a empresa cliente emite para um pagador (valor, vencimento, referência do ERP); é a entidade raiz do hub.
_Evitar_: "invoice", "título" (ambíguo), "nota fiscal" (outro documento)

**Status da fatura**:
Etapa da fatura no hub, que só avança: `recebida` → `cobranca-emitida` → `registrada` → `baixada` → `notificada` (etapas podem ser puladas: a `registrada`, e até a `cobranca-emitida` quando o banco confirma antes de responder). Quem garante o "só avança" é a máquina de estados da inbox; no baseline, um `cobranca.registrada` atrasado devolve a fatura baixada para `registrada`, sintoma do Incidente 2.
_Evitar_: "paga" ou "liquidada" como status (o status é `baixada`), "emitida" sem o prefixo `cobranca-`

**Cobrança**:
Instrumento de recebimento que o banco parceiro registra para uma fatura (boleto registrado ou Pix com QR dinâmico); uma fatura pode ter várias ao longo da vida.
_Evitar_: "boleto" como termo geral, "pedido"

**Cobrança duplicada**:
Segunda cobrança registrada para a mesma fatura do ERP (a mesma `referenciaErp`) por causa de um reenvio, e não de uma reemissão; é o sintoma do Incidente 1. No baseline, cada reenvio vira outro `faturaId` no hub e outra cobrança no banco parceiro, por isso a prova agrupa por `referenciaErp`.
_Evitar_: "reemissão" (a reemissão é legítima, por vencimento ou troca de banco)

**Baixa**:
Registro de que a fatura foi paga, feito a partir de uma confirmação do banco parceiro; cada fatura tem no máximo uma baixa válida, e ela é sempre total.
_Evitar_: "liquidação", "settlement" (no Service Bus, settlement é completar, abandonar ou mandar a mensagem para a DLQ), "fechar fatura", "baixa parcial" (o pagamento parcial fica fora da aula)

**Baixa duplicada**:
Mais de uma baixa para a mesma fatura, sintoma do Incidente 2; aparece como `quantidadeBaixas` maior que 1.
_Evitar_: "pagamento duplicado" (o pagador pagou uma vez; quem repetiu foi o hub)

**Conciliação**:
Verificação periódica que compara o que o banco parceiro confirmou com o que o hub baixou, para achar divergências.
_Evitar_: "reconciliação", "batimento"

**Divergência**:
Fatura cuja situação no hub não bate com a do banco parceiro; no kit, cobrança emitida há mais de 10 minutos sem confirmação.
_Evitar_: "erro" (divergência pede investigação; nem sempre é falha)

**Notificação ao parceiro**:
Aviso que o hub envia ao ERP da empresa cliente depois da baixa; no kit, um callback HTTP para o `callbackUrl` da fatura.
_Evitar_: "webhook" (reservado à chamada do banco ao hub), "confirmação"

### Integração e mensageria

**Request-reply assíncrono**:
Padrão em que a API responde 202 Accepted com o endereço de status e o processamento segue por fila.
_Evitar_: "fire and forget" (não há status), "assíncrono" sem qualificador

**Idempotência**:
Propriedade de uma operação produzir o mesmo resultado quando repetida com a mesma chave (`Idempotency-Key`, `MessageId`, `idEvento`).
_Evitar_: "deduplicação" (é um mecanismo), "retry seguro"

**Chave de idempotência**:
Valor do header `Idempotency-Key` que o ERP repete ao reenviar uma fatura, para receber o mesmo `faturaId` em vez de criar outra fatura.
_Evitar_: `MessageId` (é a chave do broker, não da API)

**Reenvio**:
Nova tentativa do ERP de mandar a mesma requisição depois de timeout ou erro; o certo é backoff com jitter e teto, com a mesma chave de idempotência. Só o ERP reenvia.
_Evitar_: "reentrega" (é do Service Bus), "retry agendado" (é do hub), "confirmação repetida" (é do banco parceiro), "reprocessamento" (é da DLQ)

**Metralhadora**:
Reenvio imediato e sem teto a cada erro (`erp-simulado.ps1 -SemBackoff`), que transforma uma falha em inundação.
_Evitar_: "retry" (sem espera nem teto, não é estratégia de retry)

**Rate limit por cliente**:
Limite de chamadas por minuto que o APIM aplica a cada empresa cliente, respondendo 429 com `Retry-After` a quem passa dele, sem afetar as outras.
_Evitar_: "throttling" (no Service Bus, throttling é o limite de créditos do namespace)

**Webhook de confirmação**:
Chamada HTTP que o banco parceiro faz ao hub para informar um evento da cobrança, assinada com HMAC do corpo.
_Evitar_: "callback" (reservado à notificação ao parceiro), "notificação" (é o que o hub envia)

**Assinatura do webhook**:
Código HMAC-SHA256 de `{timestamp}.{corpo}`, calculado com o segredo compartilhado `WebhookSecret` e enviado no header `X-Saldaro-Signature`, junto com `X-Saldaro-Timestamp`.
_Evitar_: "token", "segredo no header" (segredo copiado num header não é assinatura)

**Confirmação**:
Evento do banco parceiro sobre uma cobrança (`cobranca.registrada` ou `cobranca.paga`), identificado por `idEvento`, que chega pelo webhook de confirmação e vira mensagem em `sbq-confirmacoes-banco`.
_Evitar_: "pagamento" (só `cobranca.paga` informa pagamento), "notificação"

**Confirmação forjada**:
Webhook de confirmação que não veio do banco parceiro; o baseline aceita, e o hub consertado responde 401 por falta de assinatura válida.
_Evitar_: "confirmação inválida" (a de schema inválido é mensagem envenenada)

**Confirmação repetida**:
A mesma confirmação, com o mesmo `idEvento`, entregue mais de uma vez pelo banco parceiro; é o que a inbox ignora no Incidente 2.
_Evitar_: "reenvio" (é do ERP), "reentrega" (é do Service Bus), "webhook duplicado"

**Detecção de duplicatas** (duplicate detection):
Recurso do Service Bus que descarta, dentro de uma janela (10 minutos no kit), mensagens com um `MessageId` já visto.
_Evitar_: "idempotência" (é só a camada do broker e vale apenas dentro da janela)

**Sessão** (Service Bus):
Agrupamento de mensagens por `SessionId` (aqui, o `faturaId`) que garante ordem e um consumidor por vez em cada grupo.
_Evitar_: "partição" (conceito do Event Hubs), "FIFO global"

**Inbox**:
Registro, por fatura e `idEvento`, das confirmações que o consumidor já aplicou; serve para ignorar repetições e para auditoria.
_Evitar_: "outbox" (é o padrão do lado de quem publica), "cache"

**Reentrega**:
Nova entrega da mesma mensagem pelo Service Bus depois de exceção, abandono ou lock expirado; conta até `maxDeliveryCount` e então manda a mensagem para a DLQ.
_Evitar_: "reenvio" (é do ERP), "retry agendado" (é do hub)

**Falha transiente**:
Falha que passa sozinha com o tempo (no kit, o banco respondendo 503 ou o evento com `simulaTransiente`) e merece nova tentativa, nunca descarte.
_Evitar_: "mensagem envenenada" (essa falha sempre), "erro intermitente" sem diagnóstico

**Retry agendado**:
Nova tentativa feita pelo hub: envia uma cópia da mensagem com entrega agendada (5 s × 2^n, até 3 vezes) e completa a original.
_Evitar_: "reentrega" (é imediata e conta no `maxDeliveryCount`), "retry" sem qualificador

**Mensagem envenenada**:
Mensagem que falha de forma permanente a cada tentativa (schema ou regra inválidos) e deve sair do fluxo com motivo registrado.
_Evitar_: "mensagem com erro" (inclui transientes), "poison" em texto pt-BR

**DLQ** (fila de mensagens mortas):
Subfila (`$DeadLetterQueue`) de cada fila do Service Bus que guarda as mensagens retiradas do fluxo, cada uma com um motivo de dead-letter.
_Evitar_: "fila de erro", "quarentena" (só para uma fila própria de triagem)

**Motivo de dead-letter**:
Valor de `DeadLetterReason` gravado na mensagem ao ir para a DLQ: os do kit (`SchemaInvalida`, `TentativasEsgotadas`) e os da plataforma, como `MaxDeliveryCountExceeded` e `TTLExpiredException` (o Microsoft Learn lista os demais).
_Evitar_: "código de erro" (é um texto, não um código numérico)

**Triagem** (da DLQ):
Leitura da DLQ sem remover nada (peek), agrupada por motivo, para decidir o que reprocessar e o que descartar.
_Evitar_: "limpeza da DLQ" (a triagem não altera nada), "reprocessar tudo"

**Reprocessamento**:
Envio controlado, para a fila principal, de cópias corrigidas de mensagens da DLQ, com `MessageId` novo (`{original}-r{n}`) e a propriedade `reprocessadoDe`.
_Evitar_: "replay", "reenvio" (é do ERP)

**FaturaBaixada**:
Evento de integração (CloudEvents 1.0, tipo `Saldaro.Faturas.FaturaBaixada`) que o hub publica no Event Grid na baixa e que chega à fila `sbq-notificacoes-parceiro`.
_Evitar_: `fatura.baixada` (é o evento de telemetria), "notificação"

**Evento de telemetria**:
Evento customizado que o hub grava no Application Insights, com nome em minúsculas separado por pontos (ex.: `fatura.baixada`, `conciliacao.heartbeat`).
_Evitar_: "log" (é estruturado e consultado por KQL), "evento de integração" (esse é o `FaturaBaixada`)

**Heartbeat**:
Sinal que um processo emite ao concluir cada ciclo com sucesso; a ausência dele dispara alerta (dead man's switch).
_Evitar_: "health check" (é consultado, não emitido), "ping"

**Identidade gerenciada** (managed identity):
Identidade do Microsoft Entra ID atribuída a um recurso Azure (no kit, system-assigned no hub e no tópico do Event Grid), usada no lugar de connection strings e chaves.
_Evitar_: "conta de serviço" (tem senha que alguém precisa girar), "service principal com client secret"

**Throttling do Service Bus**:
Recusa temporária de operações (erro 50009) quando um namespace Standard passa de 1.000 créditos por segundo.
_Evitar_: "rate limit" (é o do APIM, por empresa cliente)

**Drift de IaC**:
Descompasso entre o que o Bicep declara e o que existe na assinatura: recurso extra, faltante ou com propriedade alterada.
_Evitar_: "desvio", "diferença de configuração"

**Fila fantasma**:
Fila que existe no namespace sem estar no Bicep, criada pelo auto-create de um worker legado (no kit, `confirmacoesbanco`).
_Evitar_: "fila duplicada" (não é cópia: tem outro nome e configuração padrão)

### Kit da aula

**Kit da aula**:
Código, Bicep, scripts, consultas KQL e templates que o aluno clona e publica na própria assinatura para viver os cenários.
_Evitar_: "lab" (lab é o exercício guiado), "projeto"

**Dia de incidente**:
Formato do encontro ao vivo de 4h: Lab 0 feito em casa, os Incidentes 1, 2 e 3 ao vivo e os outros cenários como guia de leitura ou desafio para casa.
_Evitar_: "série" (a série de três encontros é a alternativa registrada), "aula gravada"

**Cenário**:
Cada exercício do catálogo, de 3.1 a 3.8, mais o Lab 0 como cenário 0; em `deploy.ps1 -Cenario N`, N é 0 para o Lab 0 e o número depois de "3." para os demais, até 7 (o 3.8 não tem deploy).
_Evitar_: "módulo" (no kit, módulo é arquivo do Bicep)

**Lab 0**:
Fundação feita em casa antes do encontro: `deploy.ps1 -Cenario 0` cria o baseline ingênuo e o smoke test prova a jornada da cobrança.
_Evitar_: "setup" ou "pré-requisito" (o Lab 0 tem guia e prova próprios)

**Incidente**:
História fictícia de algo que quebrou na Saldaro, que abre cada guia, menos o do Lab 0 (fundação, não incidente); na aula ao vivo, os Incidentes 1, 2 e 3 são os cenários 3.2, 3.3 e 3.4.
_Evitar_: "caso real" (todos são fictícios), "bug"

**Guia de leitura**:
Guia completo de um cenário que fica fora do encontro (3.1, 3.5, 3.7 e 3.8), para o aluno fazer sozinho.
_Evitar_: "material complementar" (o guia tem passos e prova)

**Desafio para casa**:
Cenário 3.6, feito depois do encontro com o rigor de um incidente e com entrega definida.
_Evitar_: "guia de leitura" (o desafio tem entrega)

**Herança**:
Hábito ingênuo da Saldaro 1.0 que o baseline reproduz de propósito e que um cenário conserta (ex.: `POST /faturas` síncrono, `RootManageSharedAccessKey`).
_Evitar_: "bug" (é plantado de propósito), "dívida técnica"

**Baseline ingênuo**:
Estado do cenário 0 (`cenario-0.bicepparam`), com todos os toggles de cenário desligados e `maxDeliveryCount = 10`, ingênuo de propósito.
_Evitar_: "versão quebrada", "legado" (o legado é a Saldaro 1.0)

**Conserto**:
Mudança que elimina a causa de um incidente; no kit, ligar um toggle de cenário e, quando a propriedade é imutável, recriar o recurso.
_Evitar_: "workaround", "solução de contorno" (o conserto ataca a causa)

**Simplificação de laboratório**:
Escolha que deixa o kit mais simples do que um sistema real deveria ser, sempre explícita no código e no guia (ex.: HTTP triggers anônimos).
_Evitar_: "atalho", "gambiarra" (é deliberada e documentada)

**Toggle de cenário**:
Parâmetro do `main.bicep` (booleano, exceto `maxDeliveryCount`) que liga um conserto e vira app setting do hub (`Cenario__*`, `AzureWebJobs.*.Disabled`) ou propriedade de recurso.
_Evitar_: "feature flag" (não há serviço de flags), "botão de caos" (é do banco simulado), "chave" sozinho (confunde com segredo)

**Estado inicial** (de um cenário):
Ambiente em que um cenário começa, com os consertos de que ele depende e o defeito dele ainda presente; é o que `deploy.ps1 -Cenario N` aplica sem `-Pronto`.
_Evitar_: "baseline" (só os cenários 1 e 2 começam no baseline ingênuo)

**Estado pronto** (de um cenário):
Ambiente com o conserto do cenário já aplicado (`deploy.ps1 -Cenario N -Pronto`): plano B para quem travar e ponto de partida de cenários seguintes.
_Evitar_: "estado final", "gabarito", "checkpoint"

**Botão de caos**:
Parâmetro de query string do `POST /api/cobrancas` do banco simulado que injeta uma falha controlada (`latencia`, `atrasoWebhook`, `duplicar`, `embaralhar`, `assinatura`, `valorComVirgula`, `falhaTransiente`), vindo do campo `caos` da fatura.
_Evitar_: "toggle" (o toggle é do hub; o botão de caos vale por requisição), "fault injection" em texto pt-BR

**Banco simulado**:
Function App `func-saldaro-banco-<sufixo>` que faz o papel do banco parceiro: registra cobranças, envia webhooks de confirmação assinados e obedece aos botões de caos, sem imitar a API de nenhum banco real.
_Evitar_: "mock" (é um serviço publicado no Azure), "banco" sozinho (confunde com banco de dados)

**ERP simulado**:
Script `erp-simulado.ps1` que faz o papel do ERP de uma ou duas empresas clientes (`cliente-a`, `cliente-b`), com ou sem backoff.
_Evitar_: "teste de carga" (mesmo gerando rajada, o papel na história é o do ERP)

**Worker legado**:
Processo da Saldaro 1.0, simulado por `Saldaro.Ferramentas legado-auto-create`, que cria a fila se ela não existir e por isso gera a fila fantasma.
_Evitar_: "worker do hub" (o hub nunca cria filas)

**Smoke test**:
Uma fatura ponta a ponta (`smoke-test.ps1`) até `baixada` ou `notificada` dentro do prazo, para provar que o ambiente funciona no dia de sol.
_Evitar_: "prova" (a prova é o critério de feito de cada cenário), "teste unitário"

**Prova**:
Critério de feito verificável que encerra cada cenário (ex.: `quantidade = 1` por fatura no banco simulado, `quantidadeBaixas = 1`).
_Evitar_: "teste" (é verificação no ambiente, não teste automatizado), "validação"

**Post-mortem de cinco linhas**:
Registro sem culpados que o aluno escreve ao fim de cada incidente, a partir de `templates/post-mortem.md`.
_Evitar_: "RCA", "relatório de incidente" (é curto de propósito)

**`quantidadeBaixas`**:
Contador, na fatura, de quantas vezes ela foi baixada; a prova do Incidente 2 é `quantidadeBaixas = 1`.
_Evitar_: `totalBaixas`, `numeroBaixas`, "contador de pagamentos"

**Sufixo**:
De 4 a 6 letras minúsculas ou dígitos, escolhidos pelo aluno ou gerados, que tornam únicos os nomes globais de recurso (ex.: `sb-saldaro-<sufixo>`).
_Evitar_: "ambiente", "id do aluno"

## Relações

- A empresa cliente emite faturas pelo seu ERP; cada fatura tem um pagador, pode ter várias cobranças ao longo da vida e tem no máximo uma baixa válida.
- Cada confirmação se refere a uma cobrança e entra na sessão da sua fatura (`SessionId = faturaId`).
- A baixa publica `FaturaBaixada`, que vira a notificação ao parceiro.
- Os Incidentes 1, 2 e 3 são os cenários 3.2, 3.3 e 3.4; o estado inicial de um cenário é o baseline ingênuo ou o estado pronto de um cenário anterior.
- O toggle de cenário é do hub e da infraestrutura; o botão de caos é do banco simulado e vale por requisição.
