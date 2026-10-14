# ADR-0004: Confidencialidade com empresa fictícia, pasta `.local/` e varredura de termos

**Status:** Aceita · **Data:** 2026-10-13, atualizada em 2026-10-14 · **Decisão:** professor da aula · **Origem:** experiência em projetos de consultoria da TFTEC · **Version-anchor:** Q4-2026

## Contexto

Os cenários se inspiram em situações reais vividas em projetos de consultoria da TFTEC, e os clientes desses projetos são confidenciais. O repositório é privado enquanto o material é inédito, mas pode ficar público para os alunos, então tudo que entra nele precisa estar pronto para um aluno ler. Além disso, o material é produzido em paralelo por vários agentes: a regra precisa ser mecânica e verificável, não depender da memória de quem escreve.

## Decisão

1. **Empresa fictícia.** A aula se passa na **Saldaro Pagamentos S.A.** ("Saldaro Hub"), um hub de cobrança B2B inventado. "Saldaro" vem de "saldar" (quitar a dívida) e é uma palavra inventada. O nome inicial, uma palavra comum, foi trocado em 2026-10-14: a checagem de marca achou marcas em vigor com o mesmo radical no ramo de cobrança e de pagamentos, e a história mostra a empresa cobrando em dobro e aceitando confirmação forjada. Todo material traz o aviso "empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência".
2. **Incidentes reescritos como ficção.** Datas, horários, setor da empresa cliente e nomes de sistemas são trocados; os números são plausíveis e arredondados. Os únicos números reais são os públicos da Microsoft: preços de lista e limites documentados no Learn.
3. **Nada vindo de ambiente real.** Código, logs, prints, Bicep e políticas são escritos do zero para a Saldaro. Dados de teste são gerados (CNPJs com dígitos verificadores válidos por algoritmo, nunca CNPJs conhecidos). Prints só do ambiente de lab, com subscription, tenant e e-mails mascarados.
4. **O que nunca entra** em arquivo versionado, issue, comentário, título, nome de branch ou mensagem de commit é a lista da seção Confidencialidade do `CLAUDE.md`: nome de cliente ou de suas marcas, nomes de pessoas, e-mails e telefones, IDs de tenant, subscription ou recurso de produção, URLs internas e números de negócio.
5. **A ponte entre o real e o fictício fica em `.local/`**, ignorada pelo git: `.local/briefing-privado.md` e `.local/termos-proibidos.txt`. Nada de `.local/` é copiado para arquivo versionado, issue ou commit, e `git add -f` não é usado.
6. **Varredura antes de cada commit.** `pwsh ./scripts/varredura-termos.ps1` precisa terminar com "Varredura limpa" (código de saída 0). O script imprime só contagens por arquivo, nunca o termo nem a linha, e por isso a saída pode ser colada em qualquer lugar. Numa worktree, que não tem `.local/`, o caminho da lista vai no parâmetro `-Lista`.
7. **Atribuição.** Documentos e ADRs citam a origem das decisões como "experiência em projetos de consultoria da TFTEC", sem nomes, números ou detalhes de clientes. Projetos da pós e da TFTEC podem ser citados pelo nome.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Usar os casos reais trocando só o nome do cliente | Setor, números e linha do tempo bastam para reidentificar o cliente |
| Reaproveitar o cenário da D04 (MegaRetail, varejo) | Não tem o fluxo de pagamentos B2B (cobrança, webhook do banco, baixa, conciliação) em que os incidentes acontecem |
| Exemplos genéricos de livro (pedido e estoque) | Perdem o realismo que faz o incidente ensinar |
| Versionar a lista de termos proibidos, ou colocá-la numa issue | A própria lista seria o vazamento |
| `git grep -f` direto na lista | A lista tem linhas de comentário, e o grep imprime as linhas encontradas, ou seja, o próprio termo |
| Hook de pre-commit | Não adotado por ora: a lista não vem no clone nem nas worktrees, e rodar o script já é condição de commit de todo ticket |

## Consequências

- Todo número da história é inventado e precisa parecer plausível; o aviso de ficção acompanha cada material.
- O nome "Saldaro" passou pela checagem de marca em 2026-10-14 sem nenhum achado no ramo: INPI nas classes 36, 42 e 9, consultas públicas de CNPJ, listas de participantes do Pix e do STR do Banco Central e busca na web. Trocar de novo custa caro: o nome está nos recursos (`rg-saldaro-lab`, `stsaldaro<sufixo>`), nos projetos (`Saldaro.Hub`), nos headers (`X-Saldaro-Signature`) e no tipo do CloudEvent (`Saldaro.Faturas.FaturaBaixada`).
- A varredura só pega o que está na lista. Ela não substitui a revisão humana: um número real arredondado ou uma paráfrase passam por ela.
- Agentes não leem `.local/`; quem mantém a lista é o professor.
- Toda entrega de ticket inclui a varredura limpa, e a saída dela pode ir para o PR.

## Gatilhos de revisão

- Antes de publicar para os alunos: revisão humana completa, varredura e decisão sobre manter ou não a menção a "projetos de consultoria da TFTEC" (pergunta em aberto no briefing, seção 7). Decisão de 2026-10-14: este repositório não vira público. Na hora da aula, o material validado vai para um repositório novo, com um único commit na `main` e sem o histórico deste, que guarda o nome anterior da empresa e nomes de produção da pós. Vão para lá só os caminhos marcados "Vai a público" no mapa do `CLAUDE.md`, sem o que citar o Blob de produção da pós.
- Surgir empresa ou marca do ramo financeiro, de pagamentos ou de cobrança com o nome "Saldaro": refazer a checagem antes de publicar.
- Um termo proibido chegar a um commit: rever o processo (hook obrigatório, revisão em par).
- A entrada de novos autores além do professor e dos agentes, como monitores ou outros professores.

## Fontes

- `CLAUDE.md`, seção Confidencialidade.
- Briefing da aula ([seções 1, 7 e 8](../research/2026-10-13-briefing-da-aula.md)): candidatos de nome, perguntas em aberto e regras para tudo que for versionado.
- Contrato do kit, seção 0 (premissas sobre o nome fictício e os dados de teste).
- `scripts/varredura-termos.ps1`.
