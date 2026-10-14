# ADR NNNN: _título curto, como uma afirmação da decisão_

**Version-anchor:** Q4-2026 · **Tipo:** template do kit da aula de Integração e Mensageria no Azure

| Campo | Valor |
|---|---|
| Status | Proposta · Aceita · Substituída pela ADR NNNN · Descontinuada |
| Data | _aaaa-mm-dd_ |
| Decisores | _papéis, não nomes (ex.: arquitetura de integração, segurança, finanças)_ |
| Cenário | _ex.: 3.8, "Premium ou Standard?"_ |

Uma página. Escreva para quem chegar daqui a um ano sem contexto: o que estava em jogo, o que foi decidido, o que custou e quando revisar. Salve como `docs/adr/NNNN-titulo-em-minusculas-com-hifens.md`.

## Contexto

_O problema e as forças em jogo, com números: volume, pico, custo mensal em R$, requisitos de segurança e de operação, prazos. O que acontece se nada for decidido._

## Decisão

_A escolha, em uma ou duas frases afirmativas: "Vamos usar X para Y, com Z."_

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não agora |
|---|---|---|---|
| _A_ | | | |
| _B_ | | | |

## Consequências

- **Positivas:** _o que fica melhor._
- **Negativas e riscos:** _o que fica pior, o custo aceito, a dívida assumida._
- **O que muda na operação:** _alertas, runbooks, Bicep, custo mensal._

## Gatilhos de revisão

_Condições objetivas e mensuráveis que reabrem esta decisão, cada uma com o sinal que a detecta (métrica, consulta KQL, auditoria, contrato). Evite gatilhos vagos como "se crescer muito"._

1. _Ex.: `ThrottledRequests` > 0 no namespace em três picos seguidos do dia 10 (métrica do Service Bus)._
2. _Ex.: uma exigência formal de Private Endpoint na auditoria de segurança._
3. _Ex.: o volume passar de N operações por mês, acima do que o Standard inclui no custo-base._
