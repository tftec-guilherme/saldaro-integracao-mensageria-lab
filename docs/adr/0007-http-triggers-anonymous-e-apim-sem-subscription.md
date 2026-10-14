# ADR-0007: HTTP triggers anônimos e APIM sem subscription como simplificação de laboratório

**Status:** Aceita; a pendência do cenário 3.1 foi resolvida em 2026-10-14 (ver "Rate limit do 3.1") · **Data:** 2026-10-13, atualizada em 2026-10-14 · **Decisão:** professor da aula · **Origem:** experiência em projetos de consultoria da TFTEC · **Version-anchor:** Q4-2026

## Contexto

O ERP chega ao hub pelo APIM (`apim-saldaro-<sufixo>`, API `saldaro`), o banco parceiro manda os webhooks também pelo APIM (API `saldaro-webhooks`, URL `{apimBaseUrl}/webhooks/banco`), e o hub chama o banco direto (`POST {BancoBaseUrl}/cobrancas`, com `x-api-key`). No lab, os scripts (`erp-simulado.ps1`, `smoke-test.ps1`) precisam rodar sem gestão de chaves, e o `erp-simulado.ps1 -Direto` chama o hub sem passar pelo APIM.

A segurança que a aula ensina é outra: assinatura do webhook (3.3), identidade gerenciada e Key Vault (3.5) e rate limit (3.1). Autenticar quem chama a API fica fora do escopo. O Learn é claro sobre o padrão de mercado: chaves de acesso do Functions não bastam para proteger endpoints HTTP em produção, e uma API sem subscription no APIM pode ficar permissiva demais.

## Decisão

- Os HTTP triggers do hub usam `AuthorizationLevel.Anonymous`, com comentário no código marcando a simplificação de laboratório.
- As APIs do APIM não exigem subscription (`subscriptionRequired: false`), com uma exceção: no estado pronto do 3.1 (`cenario-1-pronto`), a API `saldaro` passa a exigir a subscription da empresa cliente (ver "Rate limit do 3.1"). A API `saldaro-webhooks` nunca exige: quem autentica o banco é a assinatura HMAC (3.3).
- O header `x-cliente-id` identifica a empresa cliente para o hub (`clienteId`) e para o KQL; ele não autentica ninguém. No 3.1, quem identifica a empresa cliente para o rate limit é a subscription.
- O banco simulado exige `x-api-key` com a `BancoApiKey` no `POST /api/cobrancas`, como faria um parceiro.
- Todo guia e slide que mostra esses endpoints diz que é simplificação de laboratório e aponta o que mudaria em produção.

## O que mudaria em produção

1. **Autenticar quem chama no APIM**: uma subscription por empresa cliente (chave própria, com rotação) e, de preferência, OAuth 2.0 com `validate-jwt` ou `validate-azure-ad-token`; mTLS com `validate-client-certificate` para os parceiros que exigirem. As três políticas existem no tier Consumption.
2. **Tirar a identidade da empresa cliente da credencial** (a subscription ou um claim do token), nunca de um header que o próprio cliente escolhe: com `x-cliente-id` livre, a metralhadora troca o header e foge do rate limit.
3. **Fechar o backend para quem não for o APIM**: com o APIM Consumption, o caminho é autenticar o APIM no backend com a identidade gerenciada dele (política `authentication-managed-identity`, que existe no Consumption) e exigir App Service Authentication na Function App; ou, no mínimo, a chave da função guardada como named value no APIM, vinda do Key Vault. Restrição de acesso de entrada e Private Endpoint de entrada existem no Flex Consumption, mas não isolam o backend atrás de um APIM Consumption, que não tem IP determinístico (o Learn manda liberar a faixa do datacenter) nem alcança VNet; eles passam a valer com um tier de APIM que tenha IP estável ou VNet ([IP addresses of API Management](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-ip-addresses)).
4. **No webhook do banco**, manter a assinatura e o timestamp do 3.3 e somar uma lista de IPs do banco (`ip-filter`) ou mTLS, conforme o contrato com o parceiro.
5. **Na chamada do hub ao banco**, usar o mecanismo do contrato com o banco (OAuth client credentials ou mTLS, por exemplo); a `x-api-key` no Key Vault, do 3.5, é a aproximação do lab.

## Alternativas consideradas

| Alternativa | Por que não, no lab |
|---|---|
| `AuthorizationLevel.Function` atrás do APIM, com a chave num named value | Mais passos de deploy e de script para uma proteção que o próprio Learn diz não bastar em produção |
| Uma subscription do APIM por empresa cliente (`cliente-a`, `cliente-b`) em todos os cenários | Mais realista, mas acrescenta criar subscriptions e distribuir chaves para os scripts em todos os cenários; fica só no estado pronto do 3.1, onde ela é a lição |
| OAuth 2.0 com Microsoft Entra ID | Exige app registrations no tenant do aluno e tokens nos scripts; fora do escopo |
| Private Endpoint ou VNet | Acrescenta rede, DNS e custo, e os scripts rodam da máquina do aluno, pela internet |

## Consequências

- Quem souber a URL chama o hub, direto ou pelo APIM. O sufixo deixa a URL difícil de adivinhar, mas isso não é segurança; o que limita o risco no lab é o Resource Group ser apagado no fim do encontro e os dados serem fictícios.
- A confirmação forjada passar no baseline faz parte da lição do Incidente 2: no lab, a assinatura é a única defesa do webhook.
- No 3.1, o limite conta por subscription: trocar o `x-cliente-id` não escapa dele. Mas o hub continua gravando como `clienteId` o header que o próprio cliente escolhe; em produção, o gateway derivaria o cliente da credencial (item 2 acima), e o guia 3.1 mostra como.
- Nenhuma chave de acesso do Functions aparece nos scripts nem no `scripts/.env.local`. As chaves das subscriptions do 3.1 também não: o `erp-simulado.ps1` e o `smoke-test.ps1` buscam a chave na hora (`az rest` no `listSecrets`) e a mantêm só em memória.

## Rate limit do 3.1 (pendência de 13/10/2026, resolvida em 14/10/2026)

A referência de políticas do APIM no Learn marca `rate-limit-by-key` como indisponível no tier Consumption, e o `rate-limit` só é aplicado quando a chamada traz subscription key; com `subscriptionRequired: false`, o APIM ignora a chave que vier na requisição. A verificação de 13/10 deixou três caminhos para o cenário 3.1 (`rateLimit = true`):

- **(a)** no `cenario-1-pronto`, a API passa a exigir subscription, com uma subscription por empresa cliente, e o `erp-simulado.ps1` manda a chave;
- **(b)** usar no 3.1 um tier do APIM que tenha `rate-limit-by-key`, com custo fixo, que o briefing manda evitar no Free Trial;
- **(c)** manter o 3.1 só como leitura, explicando a limitação, sem a prova de 429 por cliente.

**Decisão (Atualização 1 do contrato, 14/10/2026): caminho (a)**, por ser o item 2 de "O que mudaria em produção" virando lição, sem custo fixo; (b) contraria o Free Trial e (c) tiraria a prova do 3.1.

- No `cenario-1-pronto`, a API `saldaro` exige subscription, com duas subscriptions de escopo da API, `cliente-a` e `cliente-b` (o mesmo nome no `displayName`), e `<rate-limit calls="60" renewal-period="60" />` no inbound de `post-fatura` (429 com `Retry-After`). Nos outros estados, sem subscription e sem rate limit.
- Os webhooks do banco ficam numa API separada, `saldaro-webhooks` (path `saldaro/webhooks`, operação POST `/banco`), que nunca exige subscription; a URL pública continua `{apimBaseUrl}/webhooks/banco`.
- O `erp-simulado.ps1` detecta o 401 do APIM com uma consulta sem efeito (ou recebe `-ComChave`), busca a chave do `-Cliente` com `az rest` no `listSecrets` da subscription e envia `Ocp-Apim-Subscription-Key`; a chave nunca vai para arquivo nem output, e o `x-cliente-id` continua indo.
- O `kql/429-por-cliente.kql` agrupa por subscription (o que o APIM conta) e pelo header `x-cliente-id` (o que o hub grava).
- Custo da decisão: um estado a mais do APIM no Bicep e a busca da chave nos scripts, só no 3.1.

## Gatilhos de revisão

- O kit ser usado num ambiente que fique no ar além do encontro (uma demonstração permanente, uma prova de conceito): aplicar pelo menos os itens 1 e 3 de "O que mudaria em produção".
- O repositório ficar público com instruções de deploy: reforçar o aviso de simplificação nos guias.
- O tier Consumption passar a ter `rate-limit-by-key`, ou a aula mudar de tier.

## Fontes

Consultadas em 2026-10-13.

- [Azure Functions HTTP trigger](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger#http-auth): níveis `anonymous`, `function` e `admin`.
- [Work with access keys in Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/function-keys-how-to): chaves de acesso não bastam para proteger endpoints em produção.
- [Securing Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/security-concepts): camadas de proteção de endpoints HTTP, APIM autenticando as requisições e restrição de acesso.
- [Subscriptions in Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/api-management-subscriptions): subscription exigida por padrão, cuidado ao desligar e chave ignorada quando a API não exige subscription.
- [Azure API Management policy reference](https://learn.microsoft.com/en-us/azure/api-management/api-management-policies): disponibilidade por tier (`rate-limit-by-key` indisponível no Consumption; `rate-limit`, `validate-jwt`, `validate-azure-ad-token`, `validate-client-certificate` e `ip-filter` disponíveis).
- [rate-limit policy](https://learn.microsoft.com/en-us/azure/api-management/rate-limit-policy): só é aplicada com subscription key.
- [rate-limit-by-key policy](https://learn.microsoft.com/en-us/azure/api-management/rate-limit-by-key-policy): tiers em que a política existe.
- [Use named values in Azure API Management policies](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-properties): named values vindos do Key Vault, com a identidade gerenciada do APIM.
- [Azure Functions scale and hosting](https://learn.microsoft.com/en-us/azure/azure-functions/functions-scale#networking-features): restrição de acesso de entrada e Private Endpoint de entrada no Flex Consumption.
- Internas: contrato do kit, seções 0, 3 (com a Atualização 1), 5.1, 6 e 9; `infra/modules/apim-api.bicep`; guia `01_Aulas/Leitura_3.1_RateLimit_Guia.md`; briefing da aula ([seções 3.1 e 4.4](../research/2026-10-13-briefing-da-aula.md)).
