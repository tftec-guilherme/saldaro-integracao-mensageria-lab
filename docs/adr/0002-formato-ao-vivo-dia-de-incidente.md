# ADR-0002: Formato ao vivo de 4h, "Dia de incidente", com Lab 0 em casa e estados prontos

**Status:** Aceita · **Data:** 2026-10-13, atualizada em 2026-10-14 · **Decisão:** professor da aula · **Origem:** experiência em projetos de consultoria da TFTEC · **Version-anchor:** Q4-2026

## Contexto

A aula é um único encontro de cerca de 4h, 100% ao vivo, para alunos que já cursaram a Disciplina 04. O catálogo tem o Lab 0 e oito cenários, que somam de 6h40 a 7h20 só de laboratório (soma das durações do briefing, seção 3.0). Na D04, 11 guias em 40h deram cerca de 3,5h por guia, já contando a teoria.

Ao vivo, o tempo some em provisionamento (o APIM Consumption leva alguns minutos para ativar no primeiro deploy), em alunos travados e em máquinas diferentes. E cada mudança de código em .NET exige build e publicação ([ADR-0001](0001-stack-dotnet-10-isolated-bicep-e-powershell-7.md)).

## Decisão

Um encontro de 4h no formato "Dia de incidente", com o trio central da mensageria contado na ordem da jornada da cobrança:

| Bloco | Tempo | Conteúdo |
|---|---|---|
| Abertura | 15 min | A história da Saldaro, as regras do dia e uma revisão relâmpago da D04 |
| Lab 0 | 15 min | Só o smoke test: o `deploy.ps1 -Cenario 0` foi feito em casa |
| Incidente 1 | 50 min | Cenário 3.2: timeout → request-reply assíncrono com fila |
| Pausa | 10 min | |
| Incidente 2 | 75 min | Cenário 3.3: webhook duplicado e fora de ordem → assinatura, detecção de duplicatas, sessões e inbox |
| Incidente 3 | 50 min | Cenário 3.4: mensagem envenenada → DLQ com motivo, retry agendado, triagem e reprocessamento |
| Fechamento | 20 min | Post-mortem coletivo, desafio para casa (3.6) e guias de leitura (3.1, 3.5, 3.7 e 3.8) |
| Limpeza | 5 min | Apagar o Resource Group ao vivo, todos juntos |

- Cada cenário tem **estado inicial** (`deploy.ps1 -Cenario N`) e **estado pronto** (`deploy.ps1 -Cenario N -Pronto`): quem travar pula para o incidente seguinte sem ficar para trás.
- Cada guia segue Incidente → Sintomas → Hipóteses → Conserto → Prova e fecha com o post-mortem de cinco linhas do aluno. A exceção é o Lab 0, que é fundação, não incidente: ele monta o ambiente, segue história → Prova (o smoke test) e fecha com um pré-mortem de cinco linhas, em que o aluno aposta no que vai quebrar primeiro.
- O plano B do professor são prints de referência de cada prova e a demonstração na própria assinatura, que a turma acompanha quando o Azure não colabora.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Série de 3 encontros de 4h, "Uma semana na Saldaro" | Dá a cada cenário o tempo que ele merece, mas exige três agendas e o kit pronto antes do primeiro encontro. Fica registrada como alternativa (briefing, seção 4.2) |
| Os oito cenários num só encontro | Não cabe: só os laboratórios passam de 6h |
| Lab 0 ao vivo | Gastaria meia hora do encontro em provisionamento e concentraria o risco logo no começo |
| Sem estados prontos | Como cada cenário parte de outro, um aluno travado no Incidente 1 ficaria de fora do resto do dia |
| Aulas gravadas com guias, como na D04 | Esta aula é para encontro ao vivo, com diagnóstico em tempo real e interação com a turma |

## Consequências

- O encontro fica apertado. Os cenários 3.1, 3.5, 3.7 e 3.8 viram guias de leitura e o 3.6 vira desafio para casa, então esses guias precisam ser completos e autossuficientes.
- O kit precisa de estado pronto em todos os cenários, inclusive recriando a fila de confirmações quando a mudança é imutável ([ADR-0006](0006-consertos-por-toggle-e-sessions-em-duas-functions.md)).
- O Lab 0, com guia e prova próprios, é feito em casa, antes do encontro. Quem chegar sem ele roda `deploy.ps1 -Cenario 0` no começo e acompanha a demonstração do professor enquanto o APIM ativa.
- Os guias trazem marcação de tempo por etapa, e as speaker notes são roteiro de aula ao vivo, não narração de gravação.
- Segurança, infraestrutura como código, observabilidade, rate limit e escolha de tier ficam fora do tempo ao vivo; o fechamento precisa apontar esses guias com clareza.
- O custo por aluno fica em ~R$ 2 a 3 no Free Trial, da véspera ao fim do encontro, desde que o Resource Group seja apagado no fim (estimativa do kit, em `README.md` e `infra/README.md`).

## Gatilhos de revisão

- O ensaio do professor passar de 4h, ou um incidente estourar o próprio bloco em mais de 15 minutos.
- Mais de um terço da turma precisar do estado pronto no mesmo incidente.
- Mais de um quinto da turma chegar sem o Lab 0, o que pede tempo de Lab 0 dentro do encontro.
- A coordenação oferecer mais encontros: a série de 3 encontros volta à mesa.
- A premissa do Free Trial próprio mudar (assinatura ou sandbox da TFTEC).

## Fontes

- Briefing da aula ([seções 3.0, 4.1, 4.2 e 4.4](../research/2026-10-13-briefing-da-aula.md)): catálogo com durações, agenda do encontro, alternativa de três encontros e encaixe no Free Trial.
- Contrato do kit, seções 4 (estados inicial e pronto por cenário), 9 (`deploy.ps1`) e 11 (narrativa dos incidentes).
- `CLAUDE.md`: formato da aula e anatomia dos guias.
- `01_Aulas/Lab0_Fundacao_Guia.md`: o roteiro do Lab 0 (história, prova e pré-mortem).
- `02_Apresentação/Conteudo_Slides_Aula.md`: estrutura geral e tempos por bloco.
