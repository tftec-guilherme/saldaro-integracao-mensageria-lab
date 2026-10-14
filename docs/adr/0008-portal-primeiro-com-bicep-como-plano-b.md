# ADR-0008: Portal primeiro, com o Bicep e os scripts como plano B

**Status:** Aceita · **Data:** 2026-10-14 · **Decisão:** professor da aula · **Origem:** experiência em projetos de consultoria da TFTEC · **Version-anchor:** Q4-2026

## Contexto

O kit nasceu "tudo automático": o Lab 0 era um `deploy.ps1 -Cenario 0`, e os consertos dos incidentes eram uma linha no `.bicepparam` mais um redeploy ([ADR-0001](0001-stack-dotnet-10-isolated-bicep-e-powershell-7.md) e [ADR-0006](0006-consertos-por-toggle-e-sessions-em-duas-functions.md)). Na revisão do material, o professor apontou que assim o aluno não monta o Azure nem entende por que cada tecnologia está ali: o script esconde as decisões que a aula quer ensinar. O público já cursou a Disciplina 04, que montou tudo clique a clique no Portal, mas não discutiu as alternativas de cada serviço.

## Decisão

- **O aluno monta o ambiente no Portal.** O Lab 0 ganha o guia `01_Aulas/Lab0_Montagem_no_Portal_Guia.md`: clique a clique, em casa (~3 h), monta exatamente o que o Bicep cria no cenário 0, com os mesmos nomes, SKUs e configurações. Cada recurso tem um quadro "Por que <tecnologia> (e não <alternativa>)", ligado à FAQ `docs/research/2026-10-14-duvidas-das-tecnologias.md`.
- **Os incidentes são consertados no Portal ao vivo.** Os guias dos Incidentes 1 a 3 abrem com "Portal primeiro": o toggle, a política do APIM e a fila recriada com outra propriedade são mudados à mão, na frente da turma.
- **O Bicep e os scripts viram plano B e referência.** O `deploy.ps1` continua recriando qualquer estado (inicial ou pronto) em minutos, para quem travar ou chegar sem o Lab 0, e o Bicep continua sendo a declaração do ambiente. Mudar algo à mão e ver o próximo deploy desfazer é o gancho da leitura 3.7 (drift).
- **As speaker notes e os slides contam o Portal como caminho ao vivo**, e os slides de FAQ respondem às perguntas "por que X e não Y" quando elas surgem.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Manter tudo por script (o kit original) | Rápido e reprodutível, mas o aluno não vê as decisões; foi exatamente o que o professor rejeitou |
| Só o Portal, sem Bicep nem scripts | Volta ao problema da [ADR-0001](0001-stack-dotnet-10-isolated-bicep-e-powershell-7.md): sem estados prontos, um aluno travado fica de fora do resto do dia, e a leitura 3.7 perde o objeto |
| Montagem no Portal ao vivo, dentro do encontro | Cerca de 3 h de montagem não cabem nas 4 h ([ADR-0002](0002-formato-ao-vivo-dia-de-incidente.md)); fica em casa, com o encontro começando no smoke test |

## Consequências

- O Lab 0 em casa passa de ~30 min (script) para ~3 h (Portal), com o script como atalho para quem não puder. A chamada para o Lab 0 precisa sair com antecedência.
- O ambiente montado à mão precisa bater com o Bicep para os incidentes e o plano B funcionarem; o guia de montagem lista o que fica diferente, e a validação manual confere.
- Role assignments criadas à mão travam o Bicep (`RoleAssignmentExists`) num `deploy.ps1` posterior: o guia manda apagá-las antes, e o script deve passar a tratar isso.
- Rótulos do Portal mudam com mais frequência que o Bicep: os guias carregam marcadores "conferir no Portal" até a validação manual, e cada `Version-anchor` exige reconferir.
- Os consertos ao vivo pelo Portal não aumentam o tempo de nenhum bloco do encontro (estimativa dos guias dos incidentes).

## Gatilhos de revisão

- A validação manual do guia de montagem passar de 4 h, ou travar num passo que só o script resolve.
- Mais de um quinto da turma chegar sem o Lab 0 montado (mesmo gatilho da [ADR-0002](0002-formato-ao-vivo-dia-de-incidente.md)).
- O Portal mudar a ponto de exigir reescrita de mais de um guia por trimestre.

## Fontes

- Pedido do professor na revisão do material, em 2026-10-14.
- `01_Aulas/Lab0_Montagem_no_Portal_Guia.md` e o parágrafo "Portal primeiro" dos guias dos Incidentes 1 a 3.
- `docs/research/2026-10-14-duvidas-das-tecnologias.md`: as perguntas "por que X e não Y", com fontes do Microsoft Learn.
- [ADR-0001](0001-stack-dotnet-10-isolated-bicep-e-powershell-7.md), [ADR-0002](0002-formato-ao-vivo-dia-de-incidente.md) e [ADR-0006](0006-consertos-por-toggle-e-sessions-em-duas-functions.md).
