# QA — Validação das Speaker Notes
## Aula hands-on: Integração e Mensageria no Azure (22 slides, ao vivo, e 10 slides de bolso de FAQ)

*Formato herdado do `QA_Validacao_Speaker_Notes.md` da Disciplina 04. Preencher por slide antes de montar o deck (`deck/project/`) e de novo antes de exportar o PPTX e o PDF.*

> **Version-anchor:** Q4-2026 · **Status:** v1.4 (2026-10-14). Na v1.2, o deck deixou de ser gerado no Genspark: é um Artifact do tipo Slides no claude.ai, com a fonte em `deck/project/`, e o checklist pós-geração do PPTX deu lugar ao checklist do deck. Os 22 slides têm QA. Na integração (ticket #13), slides e notes foram conferidos contra o código do kit (hub, banco simulado, ferramentas), o Bicep, a ajuda dos scripts (`Get-Help`), os KQL e os guias de `01_Aulas/`; os fatos do Microsoft Learn foram validados em 2026-10-13 e os links reabertos em 2026-10-14. O que só um deploy real ou o ensaio confirmam está marcado como **Conferir no ensaio** em cada slide e resumido no fim do arquivo. Na v1.3, a revisão de código alinhou slides e notes ao glossário do `CONTEXT.md` e às ADRs 0004 (aviso de ficção) e 0007 (simplificação de laboratório), a prova do Incidente 1 passou a ter as cinco checagens do guia e a D3 passou a cobrar os blocos "Comandos" no deck. Na v1.4 (2026-10-14), entraram o apêndice de FAQ (FAQ-1 a FAQ-10, slides de bolso fora das 4h, com fonte na pesquisa `docs/research/2026-10-14-duvidas-das-tecnologias.md` e ganchos nas notes dos slides principais; G12 a G16, D9 e a seção "Apêndice" abaixo) e o "Portal primeiro" da ADR 0008: o conserto ao vivo dos incidentes é feito no Portal, e o `deploy.ps1` é o plano B (G17 e G18, D10 e os slides 2, 4, 5, 9, 12, 13, 14, 15, 18 e 19).

---

## Checklist geral (antes de montar o deck)

| # | Critério | Como verificar | OK? |
|---|---|---|---|
| G1 | `Conteudo_Slides_Aula.md` e `Speaker_Notes_Aula.md` têm os mesmos 22 títulos, na mesma ordem | `Select-String '^## Slide \d+'` nos dois arquivos e comparar | **OK** (2026-10-14): 22 × 22, idênticos e na mesma ordem nos dois arquivos e neste QA (os slides 8, 10, 12, 15, 17 e 19 levam "(Incidente N)") |
| G2 | Nenhuma seção de notes contém o marcador de pendência, e nenhum slide contém a nota de "a fechar" | `Select-String '\[A\sESCREVER\]'` nas notes e `Select-String '\(a\sfechar'` nos slides, ambos vazios | **OK** (2026-10-14): zero ocorrências nos três arquivos |
| G3 | Soma dos "Tempo previsto" ≤ 4h (240 min), incluindo pausa de 10 min e limpeza de 5 min | somar manualmente ou por script | **OK** (2026-10-14): 223 min de slides + 10 de pausa + 5 de limpeza = **238 min**; folga de 2 min. Os tempos dos incidentes seguem o roteiro de cada guia, que ocupa a janela inteira (tabela G3 abaixo). Os 10 slides de bolso do apêndice ficam fora da soma (G14), e o conserto pelo Portal não aumenta o tempo de nenhum bloco (ADR 0008, Consequências) |
| G4 | Todo slide de conteúdo tem "O que falar" com ≥ 150 palavras em prosa (separadores podem ter menos) | contagem por seção | **OK** (2026-10-14): menor slide de conteúdo, o 20 (158 palavras); slides de incidente entre 295 (slide 15) e 618 (slide 14); separadores 5, 11 e 16 curtos por desenho, e o 7 com a história e o preparo do terminal (158). Números da contagem da v1.2; a v1.3 só acrescentou palavras (slides 4, 6, 9, 10, 12, 13, 14, 17 e 18), e a v1.4 também (ganchos dos FAQ e "Portal primeiro"), então os mínimos não mudam. O slide 14 cresceu com o caminho pelo Portal; o deck condensa no `<aside>` (D2). Os FAQ têm a sua contagem na G16 |
| G5 | Todo slide de incidente (8–10, 12–15, 17–19) indica "Mostrar:" com o artefato exato (Portal/VS Code/terminal) | leitura | **OK** (2026-10-14): os dez têm "Mostrar:" com o caminho no Portal ou no VS Code e os comandos dos guias; 7, 8, 10, 12, 14, 17, 18 e 19 têm bloco de comandos pronto para colar |
| G6 | Pelo menos uma "Pergunta para a turma" por bloco (abertura, cada incidente, fechamento) | leitura | **OK** (2026-10-14): abertura (2, 4), Lab 0 (6), Incidente 1 (8, 9, 10), Incidente 2 (12, 13, 14, 15), Incidente 3 (17, 18, 19), fechamento (20) |
| G7 | Nenhum termo proibido nos dois arquivos | `pwsh ./scripts/varredura-termos.ps1` termina com "Varredura limpa" | **Professor roda a varredura de termos** antes de publicar o deck (a lista fica só na máquina dele); a do deck é a D5 |
| G8 | Nenhum dado real: nomes de pessoas, e-mails, IDs, números de negócio reais | leitura | **OK** (2026-10-14): busca por padrão (e-mail, GUID, CNPJ, telefone) vazia nos três `.md`, mais leitura; só a Saldaro (fictícia) e os números fictícios do briefing e dos guias (300 clientes, 400 mil/mês, 800 faturas, 1.200 webhooks, 3.800 mensagens); o `idCobranca` do POST forjado é `COB-00000000` |
| G9 | Todas as "Referências" apontam para Learn/EIP/Cloud Design Patterns e abrem | abrir links | **OK** (2026-10-14): 43 links únicos nos três `.md`, todos com HTTP 200 (os do Learn redirecionam para `en-us`). Fora do Learn/EIP, só duas fontes oficiais conscientes: a página da conta gratuita (slide 2) e o Google SRE Book (slide 20). **A reabrir antes de publicar o deck:** os links novos da v1.4, as 62 referências dos FAQ (da pesquisa, consultadas em 2026-10-14) e o "Criar uma fila pelo Portal" do slide 14 (do guia do Incidente 2), que ainda não estão na contagem acima |
| G10 | Acentuação correta em todo o texto (pt-BR); termos de produto em inglês | leitura | **OK** (2026-10-14): leitura e busca por palavras comuns sem acento fora de código (só o verbo "valida", correto); termos do Portal como aparecem na tela (Service Bus Explorer, Peek Mode, DeadLetter, Environment variables, Storage browser, Enable sessions, Enable duplicate detection, Search, End-to-end transaction details); o antigo "Transaction search" aparece sempre como "Search (antigo Transaction search)". Na v1.4, a mesma busca no texto novo (apêndice e "Portal primeiro") só achou nomes de código (`latencia=45`, `faturas-por-referencia-erp.kql`); os rótulos novos do Portal vão como na tela: + Queue, Change, Max delivery count, Lock duration, Enable partitioning, Enable dead lettering on message expiration, Request client certificate, Custom domains, Deployments |
| G11 | `Version-anchor: Q4-2026` no cabeçalho dos dois arquivos; serviços citados em GA | leitura | **OK** (2026-10-14): `Version-anchor: Q4-2026` no cabeçalho dos três `.md` de `02_Apresentação/`; serviços citados em GA (Functions Flex Consumption, Service Bus Standard, APIM Consumption, Event Grid, Table Storage, Key Vault, Application Insights). Nada em preview é usado. Nos FAQ, o que está em preview ou sem GA confirmado aparece só como ressalva, nunca como recomendação: C# no Logic Apps Standard (FAQ-7), o Event Grid com Network Security Perimeter (FAQ-3) e o Durable Task Scheduler (FAQ-9) |
| G12 | O apêndice tem os 10 slides de bolso, FAQ-1 a FAQ-10, depois do slide 22, com os mesmos títulos e na mesma ordem nos dois arquivos; o FAQ-1 é "Por que APIM e não WAF?" (pedido do professor) e o título de cada FAQ é a pergunta, como o aluno faria | `Select-String '^## FAQ-\d+'` nos dois arquivos e comparar; ler os `**Título:**` do `Conteudo_Slides_Aula.md` | **OK** (2026-10-14): 10 × 10, idênticos e na mesma ordem, a da pesquisa (perguntas 1, 4, 6, 7, 8, 9, 10, 17, 18 e 19); a G1 continua contando só os 22 `## Slide` |
| G13 | Cada FAQ cita a fonte: a pergunta da pesquisa (`**Pesquisa:**` no slide; "Referências" nas notes) e, nas "Referências", as URLs do Microsoft Learn que a pesquisa cita para aquela pergunta | leitura lado a lado com a seção "Fontes" de cada pergunta da pesquisa; tabela do apêndice, abaixo | **OK** (2026-10-14): 10 de 10 com a pergunta citada e as URLs do Learn da pesquisa (62 referências, contando a página do Service Bus sobre o Network Security Perimeter, da pergunta 23, no FAQ-3; as URLs vão sem o segmento `en-us`, como no resto das notes). Fontes fora do Learn, como a página de preços, ficaram de fora |
| G14 | O tempo dos FAQ fica fora das 4h: o início do apêndice diz isso nos dois arquivos, e o "Tempo previsto" de cada FAQ diz "fora da soma das 4h"; a G3 não muda | leitura; somar só os slides 1 a 22 | **OK** (2026-10-14): o aviso abre o apêndice no `Conteudo_Slides_Aula.md` (e a linha nova da tabela "Estrutura geral") e nas notes; os 10 FAQ dizem "1 a 2 min, fora da soma das 4h"; a G3 continua em 238 min |
| G15 | Cada FAQ tem gancho na nota de pelo menos um slide principal: a linha "**Se perguntarem:** "..." → FAQ-N", e a "Transição" do FAQ volta a esse slide | `Select-String 'Se perguntarem:.*FAQ-\d+'` no `Speaker_Notes_Aula.md` e conferir com a tabela de ganchos do apêndice, abaixo | **OK** (2026-10-14): 17 ganchos em 12 slides (2, 3, 4, 6, 9, 10, 12, 13, 14, 17, 18 e 21); todo FAQ tem pelo menos um, e cada "Transição" nomeia o slide do gancho |
| G16 | Slides FAQ enxutos e notes no ritmo da aula ao vivo: no máximo 3 bullets por slide, curtos (média de até ~8 palavras), tirados da "Resposta curta" e do "Quando a alternativa vence"; "O que falar" cabe em 60 a 90 segundos | contagem por FAQ (tabela do apêndice) | **OK** (2026-10-14): 3 bullets em cada FAQ, de 6 a 9 palavras (média de ~7,6 por bullet); "O que falar" entre ~185 e ~225 palavras, uns 75 a 90 segundos em fala de aula |
| G17 | "Portal primeiro" (ADR 0008): nenhum slide nem nota trata o `deploy.ps1` como caminho ao vivo de conserto. Nos slides 9, 13, 14 e 18, o conserto é no Portal (app setting, fila recriada com **+ Queue**, **Change** do **Max delivery count**), e o `deploy.ps1` aparece só como plano B, como o caminho de quem chegou sem ambiente ou como o Lab 0 pelo atalho do script | `Select-String 'deploy\.ps1'` nos dois arquivos e ler cada ocorrência; comparar com o parágrafo "Portal primeiro" dos guias dos três incidentes | **OK** (2026-10-14): slide 2 (bullet "Conserto ao vivo no Portal; travou? Plano B"), slide 4 (o Portal para consertar e observar), slide 14 (fila recriada no Portal; `-SemPublicar` e `-Pronto` como plano B), slide 18 (toggles e **Change** no Portal; `-Cenario 4 -Pronto -SemPublicar` como plano B) e slide 19 (bullet e "Cinco" com o **Change** do Portal). O que sobra de `deploy.ps1` nas notes é Lab 0, quem chegou sem ambiente, plano B, aquecimento depois de um deploy e a fala "ninguém fica para trás" |
| G18 | Drift explicado onde o conserto é clique: depois de cada mudança no Portal, a nota diz qual `.bicepparam` declara o mesmo conserto e por que o arquivo precisa concordar antes do próximo deploy | leitura dos slides 9, 13, 14, 15, 18 e 19 | **OK** (2026-10-14): 9 (`cenario-2-pronto` como versão declarada), 13 e 14 (`webhookHmac`, `filaComSessions` e `filaDupDetection` no `cenario-2-pronto.bicepparam`, depois `workerComSessao` e `inbox`), 15 (P6: arquivo e Azure dizem o mesmo), 18 e 19 (`cenario-4-pronto` declara os toggles e o `maxDeliveryCount = 3`) |

### G3 — Tempos por bloco

| Bloco | Slides (Tempo previsto) | Soma | Janela no relógio | Folga |
|---|---|---|---|---|
| Abertura | 1 (2), 2 (4), 3 (5), 4 (4) | 15 min | 0:00–0:15 | 0 |
| Lab 0 | 5 (1), 6 (12) | 13 min | 0:15–0:30 | 2 min |
| Incidente 1 | 7 (5), 8 (15), 9 (10), 10 (20) | 50 min | 0:30–1:20 | 0 |
| Pausa | — | 10 min | 1:20–1:30 | — |
| Incidente 2 | 11 (1), 12 (18), 13 (12), 14 (30), 15 (14) | 75 min | 1:30–2:45 | 0 |
| Incidente 3 | 16 (1), 17 (19), 18 (12), 19 (18) | 50 min | 2:45–3:35 | 0 |
| Fechamento | 20 (10), 21 (7), 22 (3) | 20 min | 3:35–4:00, com a limpeza | 0 |
| Limpeza | no fim do slide 21, antes do 22 | 5 min | dentro do fechamento | — |
| **Total** | **223 min de slides** | **238 min** | **4h** | **2 min** |

Na v1.1, os tempos dos incidentes seguem o roteiro de cada guia, que ocupa a janela inteira: Incidente 1 com a Parte 1 no slide 7, as Partes 2 e 3 no 8, a 4 no 9 e as Partes 5 a 7 no 10; Incidente 2 com as Partes 1 a 3 no 12, a 4 no 13, as 5 e 6 no 14 e as 7 e 8 no 15; Incidente 3 com as Partes 1 a 3 no 17, a 4 no 18 e as 5 e 6, mais as lições, no 19. A v1.0 tinha 25 min no slide 9 (conserto por redeploy) e 30 min no 18; nos guias, os dois consertos são app settings no Portal e a prova é que ocupa o tempo. A folga do dia (2 min) fica no Lab 0, e cada guia diz o que encurtar se o relógio apertar. Os slides 20 e 21 continuam com 10 e 7 min, para o fechamento caber em 3:35–4:00 junto com a limpeza.

## Checklist do deck (`deck/project/`)

*Preencher depois de montar o deck e de novo antes de exportar o PPTX e o PDF. O deck é um Artifact do tipo Slides no claude.ai, privado e compartilhado pelo menu Share; a fonte versionada é o `deck.json` mais um `slides/<id>.html` por slide, com as notes do apresentador no `<aside>`.*

| # | Critério | Como verificar | OK? |
|---|---|---|---|
| D1 | O `order` do `deck.json` tem os 22 ids da aula (mais os 10 do apêndice, D9), cada um com o seu `slides/<id>.html`, na ordem e com os títulos do `Conteudo_Slides_Aula.md`; separadores em 5, 7, 11 e 16 | script abaixo, mais leitura lado a lado com o `Conteudo_Slides_Aula.md` | |
| D2 | Cada `<aside>` tem até 4.000 caracteres | script abaixo (mede o conteúdo bruto do `<aside>`, com as tags, o que é conservador) | |
| D3 | Cada `<aside>` traz o essencial do "O que falar" e da "Transição" do slide no `Speaker_Notes_Aula.md`, como roteiro de aula ao vivo, e o bloco "Comandos" inteiro onde as notes têm um (slides 7, 8, 10, 12, 14, 17, 18 e 19), pronto para colar | leitura lado a lado com o `Speaker_Notes_Aula.md`; os "Comandos", linha a linha (no HTML, os marcadores `<...>` das notes perdem os sinais de menor e maior) | |
| D4 | O tempo de cada slide aparece no `<aside>`, e a soma dos 22 bate com a G3: 223 min de slides, 238 com a pausa e a limpeza | somar os tempos dos `<aside>` e comparar com a tabela G3 | |
| D5 | Nenhum termo proibido na fonte do deck, e o deck publicado é igual à fonte (nenhuma edição feita só no Artifact) | `pwsh ./scripts/varredura-termos.ps1` termina com "Varredura limpa" (o script varre os arquivos rastreados e os não rastreados, inclusive `deck.json` e `slides/*.html`); antes de republicar, comparar os arquivos publicados com a fonte | |
| D6 | Identidade visual do PPTX canônico da Disciplina 04: paleta, tipografia, rodapé TFTEC + Anhanguera, capa, separadores de bloco e 16:9 | comparar com o PPTX canônico, baixado do Blob fora do repositório (comando em `docs/research/2026-10-13-fontes-da-disciplina-04.md`) | |
| D7 | Diagrama do fio condutor (slide 3) legível a 75% de zoom no projetor; Sintomas, Padrão e Prova de cada incidente visualmente diferentes entre si | modo de apresentação do Artifact | |
| D8 | PPTX e PDF exportados do deck e publicados no Blob em `TFTEC - Pós Graduação/01_Disciplinas/Disciplina 04 - Integração e Mensageria/10_Aula Hands-on/`; nenhum dos dois no git | `az storage blob list` com o prefixo da pasta (comando em `docs/research/2026-10-13-fontes-da-disciplina-04.md`) mostra os dois; `git ls-files '*.pptx' '*.pdf'` volta vazio | |
| D9 | O apêndice entra no deck como seção própria, depois do `encerramento`, com 10 slides `faq-*` na ordem FAQ-1 a FAQ-10, os títulos (a pergunta), os bullets e o visual do `Conteudo_Slides_Aula.md`; cada `<aside>` traz "fora da soma das 4h", os ganchos, o essencial do "O que falar" e a "Transição", e o tempo desses slides não entra na soma da D4 | leitura lado a lado com o apêndice dos dois `.md`; no script abaixo, somar a D4 só nos 22 primeiros ids | |
| D10 | Os textos "Portal primeiro" da v1.4 estão no deck, sem `deploy.ps1` como caminho ao vivo: `regras-do-dia` (os cards do plano B e do aquecimento), `revisao-d04` (o card que dizia "O Portal fica aberto para observar" e o `<aside>`), `sep-lab0` (`<aside>`), `inc2-sintomas` ou `sep-incidente-2` (o `<aside>` do "quem chegou agora", que ganha o Passo 0.3 pelo Portal), `inc2-hmac` (`<aside>`: sai o "atalho de sala"), `inc2-camadas` (caixa "Imutáveis", `<aside>` e o bloco de comandos), `inc2-prova` (`<aside>`, P6), `inc3-classificar` (`<aside>` e o bloco de comandos) e `inc3-prova` (card 5 e `<aside>`) | `Select-String 'deploy\.ps1'` em `deck/project/slides/*.html` e ler cada ocorrência contra a G17 | |

Script da D1 e da D2, no PowerShell 7 e na raiz do repositório (com o apêndice no deck, o `order` passa a ter 32 ids; os 10 últimos são os `faq-*`):

```powershell
$deck = '02_Apresentação/deck/project'
$order = (Get-Content "$deck/deck.json" -Raw | ConvertFrom-Json).order
"Slides no order: $($order.Count)"
foreach ($id in $order) {
  $arquivo = "$deck/slides/$id.html"
  if (-not (Test-Path $arquivo)) { "${id}: arquivo ausente"; continue }
  $aside = [regex]::Match((Get-Content $arquivo -Raw), '(?s)<aside[^>]*>(.*?)</aside>')
  '{0,-28} {1,5}' -f $id, ($aside.Success ? $aside.Groups[1].Value.Length : 'sem <aside>')
}
```

---

## Slide 1 — Capa

### Verificação técnica
Sem conteúdo técnico. Confirmar título, subtítulo, logos e o aviso de ficção da ADR 0004 visível na capa ("Empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência."). Confirmar que o Version-anchor Q4-2026 aparece no cabeçalho do `Conteudo_Slides_Aula.md`.

### Validação cruzada com Speaker Notes
Notes cobrem: postura de "plantão", diferença para a D04, aviso de ficção. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Preciso ter feito a Disciplina 04? **A:** Sim; a aula assume o vocabulário de filas, tópicos, DLQ e Durable. Quem não fez consegue acompanhar com o slide 4 e os guias.
2. **Q:** A empresa é real? **A:** Não. Saldaro Pagamentos é fictícia; os incidentes são inspirados em experiência de campo, mas reescritos com nomes, datas e números inventados.

### Dica de demonstração
Abrir, ao lado do slide, o repositório da aula já clonado e o Portal com o `rg-saldaro-lab` do professor.

---

## Slide 2 — Regras do dia

### Verificação técnica
Confirmar que `deploy.ps1 -Cenario N -Pronto` funciona para N = 2, 3 e 4 e que `deploy.ps1 -Cenario 0` termina rodando o `smoke-test.ps1` (contrato, seções 4 e 9; não existe `cenario-0-pronto`, e `-Cenario 0 -Pronto` é aceito como sinônimo de `-Cenario 0`). Confirmar na calculadora, na data, o custo estimado do README e do Lab 0: ~R$ 0,07/h, ~R$ 2 a 3 da véspera ao fim da aula. **Portal primeiro** (ADR 0008): o bullet "Conserto ao vivo no Portal; travou? Plano B: `deploy.ps1 -Cenario N -Pronto`" segue o parágrafo "Portal primeiro" dos guias dos três incidentes; o aquecimento vale antes de cada rajada (os guias chamam `Aquecer-Ambiente` antes de cada uma, "como garantia barata" pelo Portal) e é obrigatório depois de um `deploy.ps1`.

### Validação cruzada com Speaker Notes
Notes cobrem: Lab 0 em casa (montado no Portal ou pelo atalho do script), o conserto ao vivo no Portal e o drift que ele cria, os estados prontos como plano B, o aquecimento antes de cada rajada, ritual sintomas → hipóteses → conserto → prova, custo e limpeza, e os ganchos do FAQ-8 e do FAQ-3. **OK — alinhado** com a ADR 0008.

### Perguntas esperadas dos alunos
1. **Q:** E se eu não tiver Free Trial? **A:** A premissa do kit é Free Trial próprio (US$ 200; contrato, seção 0). Outra assinatura serve, desde que o aluno possa criar Resource Groups e role assignments e tenha cota de Flex Consumption na região.
2. **Q:** Posso usar Cloud Shell em vez de instalar tudo? **A:** Para `az`, Bicep e PowerShell, sim. O deploy publica com `dotnet publish` + `az functionapp deployment source config-zip`, então precisa do .NET 10 SDK; o Azure Functions Core Tools não é obrigatório (contrato, seção 0).

### Dica de demonstração
Mostrar o `deploy.ps1 -?` no terminal para a turma ver os parâmetros.

---

## Slide 3 — A Saldaro Pagamentos

### Verificação técnica
Diagrama precisa bater com a seção 2.1 do briefing (serviços e setas). Números (300 clientes, 400 mil/mês, 30/s) são fictícios e arredondados; manter assim.

### Validação cruzada com Speaker Notes
Notes cobrem: negócio, jornada, legado 1.0, os três pontos que quebram. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Por que Table Storage e não SQL/Cosmos? **A:** Custo zero no Free Trial e point reads baratos para status e inbox; em produção, a escolha é discutida no cenário 3.8/ADR.
2. **Q:** Pix de verdade usa webhook assim? **A:** O fluxo é genérico (cobrança com QR dinâmico + confirmação assinada); não reproduz a API de nenhum banco real.

### Dica de demonstração
Apontar no diagrama, com o cursor, os três pontos de quebra e dizer em que horário cada um acontece hoje.

---

## Slide 4 — O que a Disciplina 04 já nos deu

### Verificação técnica
Conferir que a lista "já vimos" corresponde ao que a D04 realmente praticou (queue, topic, subscriptions, DLQ gravada em tabela, Durable com `statusQueryGetUri`, Event Grid custom topic, Event Hubs) e que a lista "vamos praticar" corresponde às lacunas documentadas no briefing. A troca de Node.js (D04) por C# .NET 10 isolated está no briefing, seção 5.

### Validação cruzada com Speaker Notes
Notes cobrem: vocabulário da D04, o "lado sombrio" que vamos praticar, a troca de ferramenta (C# pronto, o ambiente montado no Portal e declarado no Bicep, o Portal para consertar e para observar, o `deploy.ps1` como plano B) e o gancho do FAQ-9. **OK — alinhado.** Na v1.4, saiu o "Portal para observar", que contradizia o "Portal primeiro" da ADR 0008.

### Perguntas esperadas dos alunos
1. **Q:** Então por que fazer 202 à mão se o Durable já entrega? **A:** Para entender o padrão por dentro; em produção, Durable é uma implementação válida do mesmo padrão. A resposta completa é o FAQ-9.

### Pegadinhas de certificação
- AZ-204 e AZ-305 cobram o padrão Asynchronous Request-Reply e quando usar fila versus chamada síncrona.

### Dica de demonstração
Nenhuma; manter o ritmo e ir para o Lab 0.

---

## Slide 5 — Separador: Lab 0

### Verificação técnica
Nenhuma além do comando: `./scripts/deploy.ps1 -Cenario 0 -Sufixo <sufixo>`, que levou ~8 a 10 min no primeiro deploy real (2026-10-14; o APIM Consumption é o mais lento) e termina com o smoke test. Quem montou o Lab 0 no Portal (`01_Aulas/Lab0_Montagem_no_Portal_Guia.md`, ~3 h em casa) chega no mesmo estado (ADR 0008); o script é o atalho e o caminho de quem chegou sem ambiente.

### Validação cruzada com Speaker Notes
Notes curtas, como separador. **OK.**

---

## Slide 6 — Smoke test: o dia de sol

### Verificação técnica
Confirmar na véspera: Flex Consumption disponível na região (`az functionapp list-flexconsumption-locations`); APIM Consumption ativo; `smoke-test.ps1` termina em < 2 min no ambiente do professor e imprime `faturaId` e `operation_Id` (contrato, seção 9). Nomes da tabela de recursos conforme o Bicep (contrato, seção 2): `func-saldaro-hub-<sufixo>` e `func-saldaro-banco-<sufixo>`. No baseline o `POST /faturas` é síncrono e responde 201 (contrato 5.1); a fatura termina em `baixada` ou `notificada`. Prova do Lab 0 (guia, critério 3): no Search, o `faturaId` traz `fatura.recebida`, `cobranca.emitida`, `confirmacao.recebida` e `fatura.baixada`; o End-to-end transaction details do POST mostra o `PostFatura` chamando o `PostCobranca` do banco. Slide e notes **não** prometem o mesmo `operation_Id` na jornada: o README do hub avisa que ele não atravessa as fronteiras assíncronas (fila de storage do banco, Service Bus, Event Grid) e não deve ser usado como prova; a prova é o `faturaId`, que vai em todos os eventos do hub. O `smoke-test.ps1` imprime o `faturaId`, a `referenciaErp`, o tempo total e o `operation_Id` do POST (o trace-id do `traceparent` que ele mesmo gera). No Portal atual, o antigo "Transaction search" aparece como **Search** (em Investigate), e o detalhe de uma transação como **End-to-end transaction details** ([Learn](https://learn.microsoft.com/azure/azure-monitor/app/failures-performance-transactions)). **Conferir no ensaio:** que o End-to-end do POST mostra o `PostCobranca` do banco no mesmo `operation_Id` (propagação W3C pelo `HttpClient` do hub; não verificado no Azure por este projeto). Se não mostrar, a fala continua valendo: a prova é o `faturaId`.

### Validação cruzada com Speaker Notes
Notes cobrem: fluxo ponta a ponta com o POST síncrono, smoke test com `faturaId` e `operation_Id`, Search (antigo Transaction search) pelo `faturaId` entre aspas, o End-to-end do POST (hub → banco), a correlação técnica que pode se perder nas fronteiras assíncronas e o `faturaId` como prova, o baseline ingênuo item a item, a simplificação de laboratório da ADR 0007 (triggers anônimos e APIM sem subscription, com o que muda em produção) e a notificação ao parceiro, que vai sempre ao ERP. **OK — alinhado** com o Lab 0 e com o README do hub.

### Perguntas esperadas dos alunos
1. **Q:** Por que o baseline usa `RootManageSharedAccessKey` se é errado? **A:** Para que o cenário 3.5 (guia de leitura) tenha o que consertar; é a herança mais comum em ambientes novos.
2. **Q:** O `operation_Id` atravessa a jornada inteira? **A:** Não. Do hub ao banco, a chamada HTTP síncrona leva o contexto de trace (W3C), e o End-to-end do POST mostra os dois. Na parte assíncrona (fila de Storage do banco, webhook, Service Bus, Event Grid), o `operation_Id` pode mudar, e o README do hub avisa que ele não deve ser usado como prova. A correlação de negócio é o `faturaId`, presente em todos os eventos do hub. Se faltar telemetria, verificar o sampling (desligado no kit).
3. **Q:** A notificação ao parceiro vai para o banco? **A:** Não. "Parceiro" é o termo guarda-chuva para banco parceiro e ERP; a notificação ao parceiro sai da fila `sbq-notificacoes-parceiro` (Function `NotificarParceiro`) e vai sempre ao ERP da empresa cliente, pelo `callbackUrl` da fatura. No lab, as faturas dos scripts não têm `callbackUrl`, e a Function só marca a fatura como `notificada`.

### Dica de demonstração
Guardar o print do End-to-end transaction details para comparar nos incidentes.

---

## Slide 7 — Separador: Incidente 1

### Verificação técnica
Nenhuma. **OK.**

---

## Slide 8 — Sintomas (Incidente 1)

### Verificação técnica
- **230 s**: confirmado no Learn ([Functions scale and hosting](https://learn.microsoft.com/azure/azure-functions/functions-scale#function-app-timeout-duration)): "Regardless of the function app timeout setting, 230 seconds is the maximum amount of time that an HTTP triggered function can take to respond to a request", por causa do idle timeout padrão do Azure Load Balancer. É o teto da plataforma, citado como contexto (contrato, seção 11), não a demonstração.
- **Corte do APIM**: `forward-request` tem `timeout` padrão de 300 s; valores acima de 240 s podem não ser honrados; ao estourar, o erro predefinido é `Timeout`, com origem `forward-request` ([forward-request](https://learn.microsoft.com/azure/api-management/forward-request-policy), [error handling](https://learn.microsoft.com/azure/api-management/api-management-error-handling-policies)). O kit usa `<forward-request timeout="30" />` (contrato, seção 3). O código devolvido ao cliente, 504, não está escrito no Learn; o blog de suporte da Microsoft (Azure PaaS, "Troubleshooting 4xx and 5xx Errors with Azure APIM services", parte 2) lista o timeout baixo do `forward-request` entre as causas de 504.
- **Relógios**: o `HttpClient` do hub para o banco tem 120 s (contrato 5.2) e o `erp-simulado.ps1` tem timeout de 35 s (contrato, seção 9): o APIM é quem corta primeiro, aos 30 s, e o `on-error` da API devolve 504 quando `context.LastError.Source == "forward-request"` e `Reason == "Timeout"` (`infra/modules/apim-api.bicep`). O banco persiste a cobrança antes de esperar `latencia` (contrato, seção 6), daí a duplicata.
- **Duplicata de negócio** (resolvido na integração): o hub gera um `faturaId` novo a cada POST (README do hub), então o reenvio vira outra fatura e outra cobrança para a mesma `referenciaErp`, e cada `faturaId` tem `quantidade = 1` no banco: a prova é por `referenciaErp`, nunca `quantidade = 2` num `faturaId`. O baseline síncrono não passa o token da requisição para a chamada ao banco (`CONCEITO:` em `Aplicacao/ServicoDeFaturas.cs`): o hub segue esperando até o banco responder e grava `idCobranca` e `cobranca.emitida` mesmo depois do 504, então o `kql/faturas-por-referencia-erp.kql` mostra 2 faturas e 2 cobranças de forma determinística. O `-MaxTentativas 2` do guia fixa duas tentativas (o padrão do script é 5); o relatório do ERP mostra `504|504` e nenhum `faturaId`. Os webhooks do banco só ficam visíveis depois da resposta lenta, e o hub só grava `cobranca-emitida` se a fatura ainda estiver `recebida`: a fatura não "volta" de `baixada`.
- **Conferir no ensaio:** (a) o 504 no terminal (a política em `rawxml` com `&&` no `on-error` só é validada no primeiro deploy real); (b) a linha do `PostFatura` com ~45 s nos `requests`.

### Validação cruzada com Speaker Notes
Notes cobrem: a história no slide 7 (com o preparo do terminal), a reprodução do guia (`-MaxTentativas 2 -Caos "latencia=45"`), o corte do APIM aos 30 s (504 do `on-error`), o hub que não desiste e grava a cobrança aos 45 s, o reenvio que vira outra fatura e outra cobrança, a prova por `referenciaErp` (2 faturas e 2 cobranças; no banco, `quantidade = 1` por `faturaId`), a política do APIM e o "e se aumentasse o timeout", o worker de polling do legado e o 230 s como teto da plataforma. **OK — alinhado** com o guia do Incidente 1 (Partes 1 a 3) e com o código. Título do slide refinado de "60 s" para "45 s", o valor que a turma vê.

### Perguntas esperadas dos alunos
1. **Q:** Por que não aumentar o timeout do APIM para 240 s? **A:** Empurra o problema: o banco chegou a 90 s na história, o ERP desiste aos 35 s, e acima de 240 s o Learn avisa que o valor pode não ser honrado. A operação continua não idempotente: qualquer corte reabre a duplicata.
2. **Q:** Se o APIM devolveu 504, por que a Function não parou? **A:** O APIM fecha a conexão com o backend, mas o baseline do kit não passa o token da requisição para a chamada ao banco: o hub segue esperando até o banco responder (ou até os 120 s do `HttpClient`) e grava a cobrança. É ingênuo de propósito, como o POST síncrono da Saldaro 1.0; e cancelar no meio não desfaria nada, porque o banco já gravou.
3. **Q:** O limite de 230 s vale para Flex Consumption também? **A:** Sim; é limite de resposta HTTP, independente do plano e do `functionTimeout`.
4. **Q:** O banco não deveria ser idempotente? **A:** Deveria aceitar uma chave de idempotência, e muitas APIs aceitam; mas o hub não pode depender disso. A correção começa do lado de quem chama.

### Pegadinhas de certificação
- AZ-204: "função HTTP que precisa de mais de 230 s" → padrão assíncrono (Durable ou fila + endpoint de status), nunca aumentar `functionTimeout`.
- AZ-204: `functionTimeout` padrão e máximo variam por plano (Consumption 5 e 10 min; Flex Consumption e Premium 30 min e sem limite), mas não mudam o limite HTTP de 230 s.
- AZ-204: `forward-request` tem `timeout` padrão de 300 s; no `on-error`, `context.LastError.Reason == "Timeout"` permite devolver uma resposta própria.
- AZ-305: dependência externa com latência variável pede desacoplamento temporal (fila), não um timeout maior.

### Dica de demonstração
Deixe o terminal e o Application Insights lado a lado antes de rodar e conte os 30 s em voz alta. Tenha um print de referência do par 504/201, porque a ingestão de telemetria pode levar de 2 a 5 minutos.

---

## Slide 9 — O padrão: request-reply assíncrono

### Verificação técnica
- **Padrão** no Learn ([Asynchronous Request-Reply](https://learn.microsoft.com/azure/architecture/patterns/asynchronous-request-reply)): 202 Accepted com `Location` e `Retry-After`; o status responde 200 enquanto a operação está pendente e **303 See Other** para o recurso quando conclui. O Learn recomenda 303 em vez de 302 (com 302, alguns clientes repetem o método original, "such as duplicate POST requests") e cita a `Idempotency-Key`, porque "the client can't distinguish between a lost response and a request that was never received".
- **Contrato 5.1**: `PostFatura` consertado responde 202 `{ faturaId, status: "recebida" }` com `Location: {UrlBasePublica}/faturas/{id}/status` e `Retry-After: 5`; `GetFaturaStatus` responde 200 `{ faturaId, status }`, 303 com `Location: {UrlBasePublica}/faturas/{id}` quando `baixada` ou `notificada`, 404 se não existe; tabela `idempotencia` com PK = `clienteId`, RK = chave (insert condicional); `EmitirCobranca` lança exceção no 503 do banco (reentrega pelo Service Bus).
- **APIM e o 303**: `follow-redirects` do `forward-request` é `false` por padrão, então o redirecionamento volta ao cliente ([forward-request](https://learn.microsoft.com/azure/api-management/forward-request-policy)); o `Location` já sai com a URL pública do APIM (`UrlBasePublica`).
- **Conserto** (guia, Parte 4): a turma liga `Cenario__AsyncRequestReply = true` no Portal (Environment variables, dois Apply e um Confirm; o app reinicia) e confere `toggles.AsyncRequestReply = True` no `/saude`; nada é publicado. É drift até o Bicep dizer o mesmo: a versão declarada é o `cenario-2-pronto.bicepparam`, e o plano B é `deploy.ps1 -Cenario 2 -Pronto` (com `-SemPublicar` se o código já estiver publicado). Até a v1.0, as notes faziam o conserto por redeploy.
- **Conferir no ensaio:** o tempo de reinício do hub no Flex Consumption depois de mudar a setting (o laço do Passo 4.3 espera até 2 min) e o tempo do plano B.

### Validação cruzada com Speaker Notes
Notes cobrem: 202 + `Location` + `Retry-After`, status 200/303 e por que 303, `Idempotency-Key` com insert condicional, a fila como amortecedor, o `EmitirCobranca` com reentrega no 503, a resposta ao worker de polling do legado, leitura do código e o conserto pela app setting no Portal, com o plano B e o tempo de cada caminho (uns 2 min pelo Portal; 6 a 7 min pelo `deploy.ps1 -Cenario 2 -Pronto`, que republica os apps; guia, Passo 4.2), e os ganchos do FAQ-5 e do FAQ-8. **OK — alinhado** com o guia (Parte 4 e "Portal primeiro").

### Perguntas esperadas dos alunos
1. **Q:** Polling não é desperdício? Por que não callback? **A:** O callback existe (`callbackUrl` e `NotificarParceiro`) e é melhor quando o ERP aceita receber chamadas; o polling é o mínimo que funciona com qualquer cliente, inclusive atrás de firewall. Os dois convivem.
2. **Q:** Quem garante o `Retry-After`? **A:** Só o cliente. O servidor sugere; quem se defende do cliente apressado é o rate limit do APIM (guia de leitura 3.1: limite de 60 chamadas/min por chave de cada cliente, com 429 e `Retry-After`).
3. **Q:** E se o ERP mandar chaves diferentes para a mesma fatura? **A:** Vira duas faturas: a chave é contrato do cliente. Por isso o contrato da API documenta que a chave identifica a intenção, não a requisição.
4. **Q:** Por que tabela, e não o duplicate detection do Service Bus, para a idempotência do POST? **A:** O cliente precisa receber de volta o mesmo `faturaId`, e o broker não devolve nada; além disso, a janela do duplicate detection vai no máximo a 7 dias e não guarda resultado.

### Pegadinhas de certificação
- AZ-204/AZ-305: Asynchronous Request-Reply = 202 + `Location` + `Retry-After` + endpoint de status; Durable Functions entrega o padrão pronto (`CreateCheckStatusResponse`).
- AZ-305: Queue-Based Load Leveling (amortecer picos) não é Competing Consumers (escalar o consumo) nem Throttling (limitar o produtor); a prova costuma descrever um e oferecer os três.
- 303 × 302: o 303 força GET no redirecionamento; o 302 não garante a troca de método.

### Dica de demonstração
Mostre lado a lado a app setting no Portal e a linha `param asyncRequestReply = true` do `cenario-2-pronto.bicepparam`: o conserto é configuração, não código novo, e só fica versionado quando chega ao Bicep (tema do guia 3.7).

---

## Slide 10 — Prova (Incidente 1)

### Verificação técnica
- **Critérios** (guia do Incidente 1, Parte 5, que refina a seção 11 do contrato): 202 em < 500 ms com `latencia=60`; status 200 → 303; 1.000 faturas com `-Paralelismo 20 -IdempotencyKey` sem 5xx; `kql/faturas-por-referencia-erp.kql` vazio na janela da rajada (1 fatura e 1 cobrança por `referenciaErp`), com `quantidade = 1` no banco para cada `faturaId` como checagem complementar; mesma `Idempotency-Key` → mesmo `faturaId`. O `rateLimit` fica desligado no `cenario-2-pronto` (contrato, seção 4), então a rajada não bate em 429.
- **Durable** no Learn ([HTTP features](https://learn.microsoft.com/azure/durable-task/durable-functions/durable-functions-http-features)): o 202 traz `Location` com o `statusQueryGetUri`; o endpoint devolve 202 enquanto a instância roda e 200 quando termina. É o dialeto que as notes comparam com o 200/303 do kit.
- **Polling**: o guia usa `curl.exe -s -w " | HTTP %{http_code} %{redirect_url}"`, que não segue redirect sem `-L` e mostra o 303 cru. Alternativa testada no PowerShell 7.6 em 2026-10-13: `Invoke-WebRequest -MaximumRedirection 0 -SkipHttpErrorCheck -ErrorAction SilentlyContinue` devolve a resposta 3xx com o `Location` (com `-ErrorAction Stop`, o cmdlet falha por "contagem máxima de redirecionamentos").
- **Resolvido na integração**: o reenvio com a mesma `Idempotency-Key` é o Passo 5.5 do guia (o mesmo `$corpo` e os mesmos `$cabecalhos` do POST do Passo 5.1); as colunas do CSV do ERP são `referenciaErp,faturaId,cliente,tentativas,statusFinal,codigos,duracaoMs,idempotencyKey` (`Get-Help ./scripts/erp-simulado.ps1`); a corrida "303 e depois 200" não acontece mais: o banco só libera os webhooks depois da resposta, e o hub só grava `cobranca-emitida` se a fatura ainda estiver `recebida`.
- **Conferir no ensaio:** a escala do Flex Consumption durante a rajada (cota regional de 15 núcleos no Free Trial; o kit usa 512 MB e teto de 10 instâncias no hub e 5 no banco), que pode deixar a drenagem mais lenta sem mudar o 202.

### Validação cruzada com Speaker Notes
Notes cobrem as cinco checagens do guia (Passos 5.1 a 5.5), com os comandos (POST cronometrado, `curl.exe`, rajada com `-IdempotencyKey`, KQL por `referenciaErp`, mesma chave), o Overview da fila no Portal, a comparação com o Durable (202/200 × 200/303) e o post-mortem de 3 min com as lições. **OK — alinhado** com o guia (Partes 5 a 7).

### Perguntas esperadas dos alunos
1. **Q:** Zero 5xx no APIM prova que o banco aguentou? **A:** Não; prova que o ERP não sente o banco. A prova do backend é a fila voltando a zero e uma fatura por `referenciaErp` (com `quantidade = 1` no banco para cada `faturaId`).
2. **Q:** E se o worker cair depois do 201 do banco e antes de completar a mensagem? **A:** O Service Bus reentrega e o banco registra de novo: o trecho worker → banco ainda é at-least-once. Em produção, o hub manda o `faturaId` como chave de idempotência também para o parceiro.
3. **Q:** O 202 rápido não esconde erro de validação? **A:** A validação continua síncrona: sem `x-cliente-id`, o `PostFatura` devolve 400 antes de aceitar qualquer coisa. O 202 vale só para o que é assíncrono de verdade.

### Pegadinhas de certificação
- AZ-204: no isolated worker, o `CreateCheckStatusResponse` do Durable trabalha com `HttpRequestData`/`HttpResponseData`, não com os tipos do ASP.NET.
- AZ-204: as ações HTTP do Logic Apps seguem o padrão 202 + `Location` nativamente; um ERP em Logic Apps faria o polling sozinho.
- AZ-305: API que aceita retry precisa de idempotência; at-least-once sem chave é duplicata garantida em algum momento.

### Dica de demonstração
Rode a rajada de 1.000 com o Overview da `sbq-faturas-recebidas` aberto (gráfico de mensagens ativas): o "morro" que sobe e desce é a melhor imagem do load leveling.

---

## Slide 11 — Separador: Incidente 2

### Verificação técnica
Nenhuma. **OK.**

---

## Slide 12 — Sintomas (Incidente 2)

### Verificação técnica
- **Estado inicial** do cenário 3 = `cenario-2-pronto` (contrato, seção 4): webhook sem HMAC, fila sem sessions nem duplicate detection, worker sem sessão, sem inbox. Quem fez o Incidente 1 já está nele; quem tem uma diferença pequena corrige no Portal (Passo 0.3 do guia, "Pelo Portal"); quem chegou sem ambiente roda `deploy.ps1 -Cenario 3`.
- **Botões de caos** (contrato, seção 6): `duplicar=3` envia cada evento três vezes com o mesmo `idEvento`; `embaralhar=true` manda o `cobranca.paga` em 1 s e o `cobranca.registrada` em `atrasoWebhook + 3` = 8 s (padrão `atrasoWebhook = 5`). O guia usa 20 faturas: 20 × 2 eventos × 3 cópias = 120 webhooks (os 1.200 da história).
- **Concorrência**: `host.json` com `maxConcurrentCalls = 4` (contrato 5.5). No Learn, o valor vale por instância, é multiplicado pelo número de núcleos e só se aplica ao trigger com `isSessionsEnabled = false` ([Service Bus no host.json](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus)). Substitui as "duas instâncias" do briefing (contrato, seção 11).
- **Baseline** (contrato 5.3, passo 4): sem guarda de estado nem ETag, cada `cobranca.paga` grava `baixada` e publica de novo, e o `cobranca.registrada` atrasado "desbaixa" a fatura. O `WebhookBanco` aceita qualquer POST (contrato 5.1).
- **POST forjado**: corpo no formato do evento 7.1 do contrato, `idCobranca` fictício `COB-00000000` e `valor = 1.00`, contra a fatura de estudo tirada do `baixa-duplicada.kql` (Passo 2.5 do guia), enviado a `{SALDARO_APIM_URL}/webhooks/banco` (`/saldaro/webhooks/banco` no APIM; a API `saldaro-webhooks` nunca exige subscription key).
- **Conferir no ensaio:** (a) o valor final de `quantidadeBaixas` (1, 2 ou 3, conforme a corrida; o `baixa-duplicada.kql` é o sintoma confiável); (b) que o POST forjado contra a fatura de estudo volta 202 e gera mais um `fatura.baixada`; (c) a ingestão do Application Insights antes de rodar o KQL (o guia espera 1 minuto).

### Validação cruzada com Speaker Notes
Notes cobrem: a reprodução do guia com 20 faturas, duplicatas, desordem que desbaixa a fatura, lost update, concorrência 4 no lugar de "duas instâncias", confirmação forjada aceita (202), com a simplificação de laboratório da ADR 0007 (webhook anônimo e API `saldaro-webhooks` sem subscription) e o que muda em produção, e as hipóteses H1 a H5. **OK — alinhado** com o guia do Incidente 2 (Partes 1 a 3) e com a seção 11 do contrato. Bullets do slide ajustados: a forjada passou por falta de validação (o baseline aceita qualquer POST) e a disputa vem da concorrência 4.

### Perguntas esperadas dos alunos
1. **Q:** Por que o banco repete a confirmação? **A:** Webhook é at-least-once: sem resposta no prazo dele, o parceiro tenta de novo, e ele não sabe se você processou.
2. **Q:** Concorrência 4 numa instância é igual a duas instâncias? **A:** Para a disputa, sim: são execuções paralelas lendo e gravando a mesma fatura. Mais instâncias só aumentam a chance.
3. **Q:** O Event Grid não protege contra isso? **A:** No fio condutor ele vem depois da baixa (notificação) e também entrega at-least-once; o `NotificarParceiro` notificou três vezes porque recebeu três `FaturaBaixada`.

### Pegadinhas de certificação
- AZ-204: PeekLock = at-least-once (duplicatas possíveis); ReceiveAndDelete = at-most-once (perda possível).
- AZ-305: ordem por entidade → sessions do Service Bus; Event Grid não garante ordem; Event Hubs garante ordem por partição.
- AZ-204: `maxConcurrentCalls` padrão 16 por núcleo e por instância, só com `IsSessionsEnabled = false`.

### Dica de demonstração
Deixe o snippet do POST forjado (bloco "Comandos" das notes) num rascunho fora do repositório; na hora, só preencha o `$faturaId` com a fatura de estudo do `baixa-duplicada.kql` (o guia tem a mesma chamada na função `Send-ConfirmacaoForjada`).

---

## Slide 13 — Assinatura que vale: HMAC do corpo

### Verificação técnica
- **Contrato 7.2**: headers `X-Saldaro-Timestamp` (segundos Unix) e `X-Saldaro-Signature: sha256=<hex minúsculo>`; conteúdo assinado `"{timestamp}.{corpo}"` com o corpo bruto em UTF-8; chave = `UTF8(WebhookSecret)`; validar antes de desserializar; |agora − timestamp| ≤ 300 s; inválida → 401 e nada na fila (contrato 5.1).
- **.NET**: `CryptographicOperations.FixedTimeEquals` leva um tempo que depende do tamanho das sequências, não do conteúdo, e só retorna `false` direto se os tamanhos diferem ([Learn](https://learn.microsoft.com/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals)); `HMACSHA256.HashData(key, source)` existe desde o .NET 6 ([Learn](https://learn.microsoft.com/dotnet/api/system.security.cryptography.hmacsha256.hashdata)).
- **App setting**: `Cenario__WebhookHmac` (contrato 5.2). Mudar a setting no Portal reinicia o app; é o caminho ao vivo ("Portal primeiro"), e não um atalho de sala. O deploy reaplica a lista inteira de app settings, então o guia leva `param webhookHmac = true` para o `cenario-2-pronto.bicepparam` no Passo 5.4, nos dois caminhos do slide 14: pelo Portal, para o arquivo dizer o que está no ar; pelo script (plano B), para o redeploy não desligar o HMAC em silêncio. A prova de que o banco continua passando é o `smoke-test.ps1` (Passo 4.4).
- **Key Vault reference** ([Learn](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)): `@Microsoft.KeyVault(VaultName=...;SecretName=...)` ou `SecretUri=...`; sem versão, o app passa a usar a versão nova em até 24 h, ou na hora, se a configuração do app mudar; a identidade do app precisa de `Key Vault Secrets User` (RBAC). No kit, o segredo só vai para o Key Vault com `identidadeGerenciada = true` (guia 3.5; contrato, seções 2 e 5.2): por isso o bullet foi ajustado.
- **Conferir no ensaio:** o tempo de reinício do hub no Flex Consumption depois de mudar a setting (a fala assume menos de 1 min).

### Validação cruzada com Speaker Notes
Notes cobrem: por que segredo no header não é assinatura, conteúdo assinado com timestamp, validação antes de desserializar, tempo constante, 401 sem fila, a demonstração com a app setting no Portal (o caminho da aula, e drift até o `.bicepparam` dizer o mesmo), o segredo ainda em app setting até o 3.5, quem pode chamar o webhook (simplificação de laboratório da ADR 0007; em produção, `ip-filter` ou mTLS e o backend só via APIM) e o gancho do FAQ-10. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Por que não validar a assinatura no APIM? **A:** Dá para fazer com política e expressão, mas o kit valida no hub para a lógica ficar testável (xUnit) e versionada com o código. Validar na borda e no hub não é redundância: é defesa em profundidade.
2. **Q:** O banco e a Saldaro precisam do mesmo segredo? **A:** Sim: HMAC é simétrico. Assinatura assimétrica (chave pública) evita compartilhar segredo, ao custo de gerir certificados.
3. **Q:** 300 s não é janela demais? **A:** É o compromisso com relógios dessincronizados; dentro da janela, o duplicate detection (10 min) e a inbox cobrem o replay.
4. **Q:** HTTPS não basta? **A:** HTTPS protege o transporte; não prova quem mandou nem impede replay de quem já tem acesso ao endpoint.

### Pegadinhas de certificação
- AZ-204: sintaxe de Key Vault reference (`SecretUri=` ou `VaultName=;SecretName=`) e a role `Key Vault Secrets User` na identidade do app.
- AZ-204: o handshake de validação de webhook do Event Grid (com CloudEvents, `OPTIONS` e `WebHook-Request-Origin`) prova que o endpoint aceita entregas; o Learn diz que ele "doesn't aim to establish an authentication or authorization context" ([Learn](https://learn.microsoft.com/azure/event-grid/end-point-validation-cloud-events-schema)).
- AZ-305: segredo compartilhado → Key Vault + rotação com janela dupla; nunca em app setting em texto em produção.

### Dica de demonstração
Opcional, se sobrar tempo: assine você mesmo uma confirmação com o `WebhookSecret` (neste cenário ele é app setting em texto, ambiente de lab) e mostre o 202; depois troque um caractere do corpo e mande de novo com a mesma assinatura: 401. É a prova de que a assinatura amarra o conteúdo, e de que quem tem o segredo pode tudo, por isso ele vai para o Key Vault.

```powershell
$segredo = '<valor da app setting WebhookSecret do hub>'
$ts = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$assinatura = 'sha256=' + [Convert]::ToHexString([System.Security.Cryptography.HMACSHA256]::HashData([Text.Encoding]::UTF8.GetBytes($segredo), [Text.Encoding]::UTF8.GetBytes("$ts.$forjado"))).ToLowerInvariant()
Invoke-WebRequest -Method Post -Uri "$($cfg.SALDARO_APIM_URL)/webhooks/banco" -ContentType 'application/json' -Body $forjado -Headers @{ 'X-Saldaro-Timestamp' = "$ts"; 'X-Saldaro-Signature' = $assinatura } -SkipHttpErrorCheck | Select-Object StatusCode
```

---

## Slide 14 — Uma vez só e na ordem certa

### Verificação técnica
- **Imutabilidade** no Learn: "You can't enable or disable message sessions after the queue or subscription is created" e o mesmo para duplicate detection ([enable-message-sessions](https://learn.microsoft.com/azure/service-bus-messaging/enable-message-sessions), [enable-duplicate-detection](https://learn.microsoft.com/azure/service-bus-messaging/enable-duplicate-detection)). Rótulos no Portal: **Enable sessions** e **Enable duplicate detection**; propriedades ARM/Bicep `requiresSession`, `requiresDuplicateDetection` e `duplicateDetectionHistoryTimeWindow`. A janela pode mudar depois; ligar ou desligar, não. Bullet do slide ajustado para os rótulos do Portal.
- **Duplicate detection** ([Learn](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection)): baseada no `MessageId` (com particionamento, `MessageId + PartitionKey`); janela padrão de 10 min, mínimo 20 s, máximo 7 dias; o envio repetido "is accepted (the send operation succeeds), but the newly sent message is instantly ignored and dropped"; mensagens agendadas entram na detecção. Basic não suporta sessions nem duplicate detection.
- **Sessions** ([Learn](https://learn.microsoft.com/azure/service-bus-messaging/message-sessions)): com sessions ligadas, o cliente só envia e recebe dentro de sessão; só um receptor tem o lock de uma sessão; as mensagens da sessão chegam em ordem; abandonar faz a mesma mensagem voltar na próxima recepção. Sequence number garante a ordem de enfileiramento e de extração, não a de processamento ([sequencing](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing)).
- **host.json** ([Learn](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus)): `maxConcurrentSessions` padrão 8, só com `isSessionsEnabled = true`; `maxConcurrentCalls` só sem sessões. No hub, `sessionIdleTimeout = 00:00:10` (o padrão seria o `tryTimeout`, 1 min), para as sessões ociosas liberarem o worker e a fila drenar ao vivo.
- **Caminho do guia** (Partes 5 e 6, "Portal primeiro"): tentar no Portal e no CLI (`az servicebus queue update --enable-session true` responde `Sub code=40000`), drenar (`countDetails` em 0), apagar a fila no Portal, editar o `cenario-2-pronto.bicepparam` (`webhookHmac`, `filaComSessions` e `filaDupDetection` = `true`, Passo 5.4, para o arquivo registrar o que vai ser criado à mão), recriar no Portal com **+ Queue** (Passo 5.5: nome `sbq-confirmacoes-banco`, **Max delivery count** `10`, **Lock duration** 1 minuto, **Enable duplicate detection** com janela de 10 min, **Enable dead lettering on message expiration** e **Enable sessions** marcados, **Enable partitioning** desmarcado, os valores da `var filas` do `service-bus.bicep`), conferir com o `az servicebus queue show` do Passo 5.6, trocar o consumidor pelos dois app settings `AzureWebJobs.ProcessarConfirmacao*.Disabled` no Portal e ligar `Cenario__Inbox`; no fim, `workerComSessao` e `inbox` vão para o mesmo `.bicepparam`, que fica igual ao `cenario-3-pronto` (Passo 6.4). Planos B: `deploy.ps1 -Cenario 3 -Sufixo <sufixo> -SemPublicar` (o Bicep recria a fila com o arquivo editado; sem `-Pronto`, o script usa o arquivo como está no disco, e o aviso amarelo de `filaComSessions = true` com `workerComSessao = false` é esperado) e `deploy.ps1 -Cenario 3 -Pronto`, que detecta a divergência e recria a fila. Até a v1.0, as notes iam direto ao `-Pronto`; da v1.1 à v1.3, o caminho ao vivo era o redeploy com `-SemPublicar`, que na v1.4 virou plano B.
- **Contrato**: `cenario-3-pronto` liga `webhookHmac`, `inbox`, `filaComSessions`, `filaDupDetection` e `workerComSessao` (seção 4); os triggers alternam por `AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled` e `AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled` (5.1, 5.2); `deploy.ps1 -Cenario 3 -Pronto` recria a fila quando a mudança é imutável (seções 4 e 9); inbox PK = `faturaId`, RK = `idEvento` (5.4); ETag com concorrência otimista (5.3). No SDK, `UpdateEntityAsync` com `ifMatch` falha com 412 (Precondition Failed) se o ETag não confere ([Learn](https://learn.microsoft.com/dotnet/api/azure.data.tables.tableclient.updateentityasync)).
- **Conferido no primeiro deploy real (2026-10-14):** (a) Sessions e Duplicate detection aparecem em **Overview → Essentials** como "Disabled" (print no slide 14); Settings → Properties virou uma página genérica de metadados do ARM; (b) nos planos B, o `deploy.ps1 -Cenario 3 -SemPublicar` para no passo 11 em ~50 s se a fila não foi apagada e, com a fila apagada, recria em ~3,3 min; o `-Pronto` recria em ~6 a 7 min; (c) no `-Pronto`, o script avisa que a fila será apagada e recriada pelo Bicep e mostra as mensagens perdidas. **Ainda a conferir:** (d) o erro do consumidor sem sessão nos `traces` (Passo 5.7); (e) o painel **+ Queue** com os campos do Passo 5.5 e o **Change** de **Lock duration** e de **Max delivery count** em Essentials, que o guia marca com `<!-- conferir no Portal -->`; (f) o print do painel com as duas caixas marcadas, para o visual do slide 14.

### Validação cruzada com Speaker Notes
Notes cobrem: as três camadas e o que cada uma pega, janela de 10 min × tolerância de 5 min, sessão = um consumidor por fatura na ordem de chegada (e o limite disso), inbox e máquina de estados com ETag, imutabilidade com o caminho do guia pelo Portal, na ordem dos Passos 5.2 a 5.6 (drenar, apagar, editar o `cenario-2-pronto.bicepparam` para o arquivo registrar o conserto, recriar com **+ Queue**, conferir pela API; depois, trocar o consumidor e ligar a inbox), os dois planos B (`-SemPublicar` e `-Pronto`), `host.json` (com o `sessionIdleTimeout` de 10 s), os dois triggers e os ganchos do FAQ-4 e do FAQ-5. O bloco "Comandos" trocou o `deploy.ps1` do caminho ao vivo pelos comandos dos Passos 5.1, 5.2 e 5.6, com o `-SemPublicar` no fim, como plano B. **OK — alinhado** com o guia do Incidente 2 (Partes 5 e 6 e "Portal primeiro").

### Perguntas esperadas dos alunos
1. **Q:** Se o duplicate detection já descarta, para que a inbox? **A:** Ele só olha o `MessageId`, só dentro da janela e só no envio. A inbox pega a confirmação repetida depois da janela e a reentrega da mesma mensagem (a Function caiu depois de gravar e antes de completar), que o broker não trata como duplicata.
2. **Q:** Sessions não matam o paralelismo? **A:** Dentro da fatura, sim, de propósito; entre faturas, não: até 8 sessões por instância, e a escala adiciona instâncias.
3. **Q:** Por que não `DeferMessageAsync` para o "paga" que chega antes? **A:** Funciona (é o desafio avançado do briefing), mas exige guardar o `SequenceNumber` e buscar depois; a máquina de estados tolerante resolve o caso do kit com menos peças.
4. **Q:** Recriar a fila em produção perde mensagens? **A:** Perde tudo o que estiver nela e na DLQ dela. Por isso: drenar antes, ou criar uma fila com outro nome e migrar produtor e consumidor ("versionar o canal").

### Pegadinhas de certificação
- AZ-204: duplicate detection e sessions **só na criação**; a janela do duplicate detection pode mudar depois.
- AZ-204: numa fila com sessions, todo receptor precisa aceitar sessão; no Functions, `IsSessionsEnabled = true` no trigger.
- AZ-305: "processar em ordem por pedido ou fatura" → sessions com o id da entidade; "só receber em ordem" → basta o sequence number.
- AZ-204: com particionamento, a unicidade do duplicate detection é `MessageId + PartitionKey`, e com sessions a partition key precisa ser igual ao `SessionId`.

### Dica de demonstração
Antes de apagar a fila, mostre o Overview com zero mensagens ativas e zero na DLQ: é a drenagem que torna seguro apagar. No painel **+ Queue**, pare em cada caixa marcada e diga qual camada ela liga. Depois de criar a fila, volte ao mesmo Overview e mostre o "Duplicate detection window" de 10 min, que agora aparece com um **Change** ao lado: a janela muda depois; ligar ou desligar, não.

---

## Slide 15 — Prova (Incidente 2)

### Verificação técnica
- **Critérios**: os P1 a P6 do guia (Parte 7): POST forjado com 401 e a fatura de estudo intacta; `baixa-duplicada.kql` vazio; as 20 faturas com `quantidadeBaixas = 1` e `notificada`; `traces` com `SessionId = faturaId` e `SequenceNumber` crescente (o log estruturado de `Aplicacao/LogDeConfirmacao.cs`); 1 notificação por fatura e ~120 `WebhookBanco` × 40 `ProcessarConfirmacaoComSessao`; o `.bicepparam` com os cinco toggles do `cenario-3-pronto`. Cobrem o contrato (seção 11: `baixa-duplicada.kql` vazio e `quantidadeBaixas = 1`) e o briefing (3.3). A concorrência 4 do `host.json` substitui as "duas instâncias" (contrato, seção 11). Slide e notes reorganizados em P1 a P6.
- **"Exactly once"**: o Learn diz que ligar duplicate detection "guarantees exactly once delivery over a user-defined span of time" ([enable-duplicate-detection](https://learn.microsoft.com/azure/service-bus-messaging/enable-duplicate-detection)): é entrega na janela, não processamento.
- **Sampling** desligado no `host.json` e no worker (contrato 5.5): sem isso, o KQL de baixa duplicada pode subcontar.
- **Conferir no ensaio:** o tempo entre a rajada e o KQL (o guia espera 1 minuto de ingestão depois de a fila zerar; no troubleshooting, 1 a 2); se o KQL demorar, a prova pela tabela `faturas` (`quantidadeBaixas = 1`) e pela `inbox` vem primeiro; e o formato real da linha de log com `SessionId` e `SequenceNumber` nos `traces` (P4).

### Validação cruzada com Speaker Notes
Notes cobrem os P1 a P6 do guia na ordem das camadas, as tabelas `faturas` e `inbox`, a concorrência que não mudou (agora por sessão), a contagem de envios × processamentos, o KQL que passou de 20 faturas para zero, o P6 com a fila criada no Portal e o `.bicepparam` dizendo o mesmo (o clique consertou; o arquivo garante que o conserto fica) e o post-mortem de 3 min com duas decisões na causa raiz. **OK — alinhado** com o guia (Partes 7 e 8).

### Perguntas esperadas dos alunos
1. **Q:** Como provar a ordem sem duas instâncias? **A:** A disputa acontecia dentro de uma instância (concorrência 4); com sessões, o mesmo `host.json` não processa a mesma fatura em paralelo, e o evento atrasado não desfaz a baixa.
2. **Q:** O KQL vazio prova o quê, exatamente? **A:** Que nenhuma fatura teve mais de um `fatura.baixada` na janela consultada, com sampling desligado.
3. **Q:** E se o banco repetir uma confirmação daqui a uma hora? **A:** Passa pelo broker (fora da janela) e para na inbox, com o evento `confirmacao.duplicada.ignorada`.

### Pegadinhas de certificação
- AZ-204: o "exactly once" do duplicate detection vale na janela e no envio; o processamento continua at-least-once e exige consumidor idempotente.
- AZ-305: telemetria usada como prova de correção exige sampling desligado (ou eventos de negócio fora do sampling).

### Dica de demonstração
Deixe duas abas do Application Insights com o `baixa-duplicada.kql`: uma rodada antes do conserto (as 20 faturas) e outra depois (vazia). O contraste vale mais que qualquer slide.

---

## Slide 16 — Separador: Incidente 3

### Verificação técnica
Nenhuma. **OK.**

---

## Slide 17 — Sintomas (Incidente 3)

### Verificação técnica
- **Estado inicial** do cenário 4 = `cenario-3-pronto` (contrato, seção 4): sessions, duplicate detection, inbox e HMAC ligados; `dlqComMotivo` e `retryAgendado` desligados; `maxDeliveryCount = 10`.
- **Botões de caos** (contrato, seção 6): `valorComVirgula=true` → `"valor": "1.250,00"` nos dois eventos (README do banco); `falhaTransiente=50` → 50% das chamadas `POST /cobrancas` com 503 e 50% dos `cobranca.paga` com `simulaTransiente = true` e `transienteAte = envio + 45 s`. Quem mudou o formato foi o banco parceiro (contrato, seção 11); bullet do slide e transição do slide 15 ajustados.
- **Duas rajadas** (guia, Passo 2.1): `-Faturas 10 -Caos "valorComVirgula=true"` (20 mensagens envenenadas: as duas confirmações de cada fatura) e `-Faturas 20 -Caos "falhaTransiente=50"` (uns 10 pagamentos bons pegos pela oscilação). A lógica de baixa desserializa antes de olhar `simulaTransiente` (contrato 5.3, passos 1 e 2): com os dois botões na mesma rajada, nenhuma mensagem boa cairia na DLQ. No primeiro deploy real, a DLQ chegou a 32 (20 envenenadas + 12 transientes; o número de transientes varia), todas `MaxDeliveryCountExceeded`, com 204 abandonos em ~2 min. Até a v1.0, as notes usavam `valorComVirgula=true;falhaTransiente=30` e `falhaTransiente=30` com 20 faturas cada.
- **DLQ** no Learn ([dead-letter queues](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)): `MaxDeliveryCount` padrão 10 → `MaxDeliveryCountExceeded`; sem limpeza automática; TTL não vale na DLQ; aplicações podem definir os próprios motivos. No Functions, "Poison message handling can't be controlled or configured": quem trata é o Service Bus ([trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus-trigger)).
- **Service Bus Explorer** ([Learn](https://learn.microsoft.com/azure/service-bus-messaging/explorer)): Peek Mode → DeadLetter → Peek from start (até 100 mensagens), com corpo e Message Properties; o Learn avisa que o Explorer "doesn't support management operations and sessions".
- **Resolvido na integração**: o hub faz `AbandonMessageAsync` explícito e relança a exceção (`Aplicacao/AbandonExplicito.cs`), então a reentrega é imediata e as 10 entregas acontecem em segundos; o `valorComVirgula` vale para os dois eventos, e as 10 faturas geram 20 envenenadas.
- **Conferido no primeiro deploy real (2026-10-14):** (a) 204 abandonos em ~2 min no baseline; (b) o Peek da DLQ de uma fila com sessions **funciona** no Service Bus Explorer (aba "Dead-letter (N)", Peek Mode, "Peek from start"; print no slide 17), apesar do aviso do Learn sobre sessions; o `triagem-dlq.ps1` continua como o caminho que agrupa por motivo.

### Validação cruzada com Speaker Notes
Notes cobrem: o banco parceiro mudando o formato numa atualização da API, a preparação (triagem de aquecimento), as duas rajadas do guia e por que são duas, a reentrega imediata até a DLQ, as mensagens boas misturadas, o Explorer, as hipóteses (exceções, ritmo das reentregas, triagem só leitura com um grupo só) e o dilema reprocessar × descartar (com o risco de ler R$ 1,25). **OK — alinhado** com o guia do Incidente 3 (Partes 1 a 3).

### Perguntas esperadas dos alunos
1. **Q:** Por que não aceitar "1.250,00" e converter no worker? **A:** Aceitar formato ambíguo no consumidor esconde a mudança do parceiro. O kit rejeita com motivo e corrige no reprocesso, com rastro; mudar o contrato é uma decisão a negociar com o banco.
2. **Q:** O `DeliveryCount` sobe a cada exceção? **A:** Sobe a cada entrega em peek-lock que é abandonada ou cujo lock expira; quando passa do `maxDeliveryCount`, a mensagem vai para a DLQ.
3. **Q:** A DLQ expira? **A:** Não: TTL não vale na DLQ e não há limpeza automática; as mensagens ficam lá até alguém recebê-las e completá-las.
4. **Q:** Por que aparecem exceções no `EmitirCobranca` durante a rajada? **A:** O `falhaTransiente=50` também faz metade das chamadas `POST /cobrancas` responderem 503; o `EmitirCobranca` lança exceção e o Service Bus reentrega a mensagem da `sbq-faturas-recebidas` (`maxDeliveryCount` 10 nessa fila). Com 50% de falha por tentativa, dez falhas seguidas acontecem em ~0,1% das faturas (~2% de chance por rajada de 20); se acontecer, a fatura fica `recebida` e a mensagem vai para a DLQ da `sbq-faturas-recebidas`, de onde `./scripts/triagem-dlq.ps1 -Fila sbq-faturas-recebidas -Reprocessar` a devolve (troubleshooting do guia).

### Pegadinhas de certificação
- AZ-204: `MaxDeliveryCount` padrão 10; motivos do sistema: `MaxDeliveryCountExceeded`, `TTLExpiredException`, `HeaderSizeExceeded`, `Session ID is null`, `MaxTransferHopCountExceeded`.
- AZ-204: caminho da DLQ `<fila>/$deadletterqueue`; não dá para fazer dead-letter de uma mensagem que já está na DLQ.
- AZ-305: monitorar DLQ com alerta de métrica (`DeadletteredMessages`), não com consulta manual.

### Dica de demonstração
Abra duas mensagens da DLQ lado a lado (uma envenenada, uma boa) e leia em voz alta o `DeadLetterReason` das duas: a igualdade é o argumento do slide.

---

## Slide 18 — Classificar, agendar, triar

### Verificação técnica
- **Assinatura** conferida no código-fonte da extensão `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus` em 2026-10-13: `DeadLetterMessageAsync(ServiceBusReceivedMessage message, Dictionary<string, object>? propertiesToModify = default, string? deadLetterReason = default, string? deadLetterErrorDescription = default, CancellationToken cancellationToken = default)`. Por isso o slide usa os parâmetros nomeados `deadLetterReason:` e `deadLetterErrorDescription:` (o esqueleto tinha `reason:` e `description:`, que não compilam). `ServiceBusMessageActions` exige `AutoCompleteMessages = false` e a extensão 5.14.1 ou superior ([trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus-trigger)).
- **Contrato 5.3**: `SchemaInvalida` com `DeadLetterErrorDescription` = mensagem do erro (até 1.024 caracteres, limite escolhido pelo kit); retry com cópia de `MessageId = {idEvento}-t{n}`, mesmo `SessionId`, `tentativa = n`, `ScheduledEnqueueTime = agora + 5 s × 2^n` (n = 1..3: 10, 20 e 40 s) e a original completada; acima de 3 → `TentativasEsgotadas`; eventos `mensagem.deadletter` e `mensagem.retry.agendado` (seção 10). Bullet do slide corrigido de `2^n s` para `5 s × 2^n`. Com `transienteAte = envio + 45 s`, as tentativas caem em ~10, ~30 e ~70 s: a terceira passa.
- **Scheduled messages** ([Learn](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing#scheduled-messages)): não aparecem na fila antes do horário e, ao ativar, ganham novo sequence number; no SDK, `ServiceBusMessage.ScheduledEnqueueTime` (`DateTimeOffset`) ([Learn](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusmessage.scheduledenqueuetime)); mensagens agendadas entram no duplicate detection ([Learn](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection)), daí o `-t{n}`.
- **Conserto do guia** (Parte 4, "Portal primeiro"): os dois toggles no Portal (`Cenario__DlqComMotivo` e `Cenario__RetryAgendado`, Passo 4.2) e o **Max delivery count** de `10` para `3` no **Change** da Overview da fila, em Essentials (Passo 4.3), conferido com o `az servicebus queue show` do Passo 4.4; a comparação `cenario-3-pronto` × `cenario-4-pronto` mostra a versão declarada do mesmo conserto, e o drift fica de propósito até um deploy do `cenario-4-pronto`. O `maxDeliveryCount` muda numa fila existente (o que é imutável são sessions e duplicate detection): nada é recriado, e a DLQ do incidente fica para a triagem. Planos B: `deploy.ps1 -Cenario 4 -Pronto -SemPublicar`, com a fila parada (no what-if, a fila aparece como **Deploy**, `!`, sem valores, e não como criação; a v1.3 dizia "`~`, entre os modificados", e o guia corrigiu), e `-Cenario 4 -Pronto` sozinho, com publicação. Sem o **Change** na Overview, o guia manda `az servicebus queue update @fila --max-delivery-count 3`. A execução das rajadas de novo e da triagem passou para o slide 19, como na Parte 5 do guia.
- **Triagem** (contrato, seções 8 e 9): peek com `SubQueue.DeadLetter` agrupado por `DeadLetterReason`; `-Reprocessar` corrige o `valor` pt-BR para número, reprocessa com `MessageId = {original}-r{n}`, mesmo `SessionId` e `reprocessadoDe`, e completa a mensagem da DLQ; `-DescartarMotivo` completa as do motivo informado.
- **Ordem no reprocesso** (Learn, sessions): "If a dead-lettered message is later moved back to the original queue for reprocessing, the original ordering relative to other session messages is lost".
- **Conferir no ensaio:** o **Change** ao lado de **Max delivery count** na Overview da fila (o guia marca com `<!-- conferir no Portal -->`), o tempo do plano B `deploy.ps1 -Cenario 4 -Pronto -SemPublicar` e a saída exata do `triagem-dlq.ps1` (colunas e exemplos; o guia mostra uma saída ilustrativa).

### Validação cruzada com Speaker Notes
Notes cobrem: permanente × transiente, `DeadLetterMessageAsync` com motivo e descrição, `AutoCompleteMessages = false`, retry agendado com backoff e teto, o `-t{n}` por causa do duplicate detection, os toggles e o `maxDeliveryCount = 3` no Portal sem recriar a fila, o `cenario-4-pronto` como versão declarada e o drift até o próximo deploy, o plano B com `-Pronto -SemPublicar`, a triagem explicada para a prova (reprocesso com `-r{n}` e `reprocessadoDe`, inbox pelo `idEvento`, `-DescartarMotivo`) e o gancho do FAQ-6. O bloco "Comandos" ganhou o `az servicebus queue update` de reserva e deixou o `deploy.ps1` no fim, como plano B. **OK — alinhado** com o guia (Parte 4 e "Portal primeiro").

### Perguntas esperadas dos alunos
1. **Q:** Por que não `AbandonMessageAsync` com atraso? **A:** O abandon devolve a mensagem na hora; não existe abandon com atraso. O atraso de verdade é a mensagem agendada (ou o defer com busca por sequence number).
2. **Q:** E o retry policy do Functions? **A:** Para Service Bus, quem reentrega é o broker (`maxDeliveryCount`); o kit usa mensagem agendada porque quer backoff de segundos sem segurar lock nem instância.
3. **Q:** Um 429 do parceiro é transiente? **A:** É transiente com instrução: respeite o `Retry-After` dele no agendamento.
4. **Q:** Quem pode rodar `-Reprocessar`? **A:** No lab, o aluno, que tem `Azure Service Bus Data Owner` no namespace (contrato, seção 2); em produção, um papel operacional com trilha de auditoria.

### Pegadinhas de certificação
- AZ-204: no isolated worker, settlement manual = `AutoCompleteMessages = false` + parâmetro `ServiceBusMessageActions`.
- AZ-204: `ScheduleMessageAsync` devolve o sequence number, que permite cancelar a mensagem agendada; com o envio comum e `ScheduledEnqueueTime`, o retorno não traz esse número.
- AZ-204: Defer tira a mensagem do fluxo e ela só volta pelo sequence number; Schedule cria uma mensagem futura; Abandon devolve na hora e incrementa o `DeliveryCount`.
- AZ-305: Retry com backoff exponencial e teto, mais classificação de erro; circuit breaker compartilhado entre clientes é antipadrão.

### Dica de demonstração
Rode a triagem antes do conserto (só `MaxDeliveryCountExceeded`) e de novo depois das novas rajadas (dois grupos). O antes e depois da tabela de motivos é a lição do slide. No Portal, deixe a Overview da fila aberta e mostre o **Max delivery count** em `3` logo depois do **OK**: é a mesma fila, com a mesma DLQ.

---

## Slide 19 — Prova (Incidente 3)

### Verificação técnica
- **Critérios** (briefing 3.4; contrato, seção 11, "DLQ vazia após a triagem"): `SchemaInvalida` na primeira entrega; transientes resolvidos com tentativas 1..3 e nunca na DLQ; triagem por motivo; DLQ vazia após o reprocesso; `maxDeliveryCount = 3` e lock de 1 min no Bicep (contrato, seções 2 e 4). Números do guia (Parte 5), com as mesmas duas rajadas: +20 `SchemaInvalida` com `DeliveryCount` 1; `mensagem.retry.agendado` com 1/10 s, 2/20 s e 3/40 s e nenhuma `TentativasEsgotadas`; triagem com ~30 `MaxDeliveryCountExceeded` + 20 `SchemaInvalida`; o `-Reprocessar` reprocessa ~50 e a DLQ chega a 0; a consulta final dá 60 faturas, 60 baixadas e nenhuma baixa dupla.
- **KQL** `kql/dlq-por-motivo.kql` sobre `mensagem.deadletter` (faturaId, idEvento, motivo) (contrato, seção 10).
- **Alerta** `alrt-dlq-confirmacoes` (métrica `DeadletteredMessages` > 0 na `sbq-confirmacoes-banco`) só existe com `alertas = true`, no cenário 6, que é o desafio 3.6 (contrato, seções 2 e 4).
- **Conferir no ensaio:** que a DLQ chega a zero depois do `-Reprocessar` também para o lote antigo (`MaxDeliveryCountExceeded`) e que as faturas reprocessadas aparecem `baixada` ou `notificada`.

### Validação cruzada com Speaker Notes
Notes cobrem os cinco critérios com os números do guia (20 `SchemaInvalida`, retries de 10/20/40 s, dois grupos na triagem, 60 de 60 baixadas), o aquecimento depois do restart (obrigatório depois do plano B), a política da fila no Azure pelo **Change** do Portal e declarada no `cenario-4-pronto` (bullet do slide ajustado na v1.4), por que três entregas bastam e o post-mortem de 3 min com o alerta de DLQ ligando ao desafio. **OK — alinhado** com o guia (Partes 5 e 6).

### Perguntas esperadas dos alunos
1. **Q:** Reprocessar pode baixar duas vezes? **A:** Não: a inbox olha o `idEvento` do corpo, que não muda no reprocessamento; só o `MessageId` muda (`-r{n}`).
2. **Q:** E a ordem dentro da sessão no reprocesso? **A:** A ordem original se perde (novo sequence number), e a máquina de estados tolera: "registrada" depois de "baixada" é ignorada.
3. **Q:** O que fazer com `TentativasEsgotadas`? **A:** Investigar a dependência e, quando ela voltar, reprocessar. É o motivo que diz "a mensagem estava certa; o mundo é que estava fora do ar".

### Pegadinhas de certificação
- AZ-204: ler a DLQ no SDK = `ServiceBusReceiverOptions.SubQueue = SubQueue.DeadLetter`; no Portal, o Resend do Service Bus Explorer envia uma cópia e não apaga a original.
- AZ-305: DLQ é parte do desenho operacional: alerta, runbook e dono definidos antes do primeiro incidente.

### Dica de demonstração
Termine o incidente com a métrica `DeadletteredMessages` da fila no Portal caindo para zero: é o "~50 → 0" do visual (52 → 0 no primeiro deploy), ao vivo.

---

## Slide 20 — Post-mortem coletivo

### Verificação técnica
Nomes dos padrões corretos (Asynchronous Request-Reply, Idempotent Receiver, Resequencer, Dead Letter Channel, Retry). Template de cinco linhas igual ao do kit (`templates/post-mortem.md`). Tempo previsto passou de 12 para 10 min (G3).

### Validação cruzada com Speaker Notes
Notes cobrem: leitura de post-mortems, causa raiz como decisão de arquitetura, as três frases do legado. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Post-mortem precisa de ferramenta? **A:** Não; cinco linhas num markdown no repositório bastam. O hábito importa mais que o formato.

### Pegadinhas de certificação
- AZ-305 cobra nomear o padrão a partir de um cenário descrito em prosa; treinar a tradução "sintoma → padrão".

---

## Slide 21 — Desafio para casa e guias de leitura

### Verificação técnica
Confirmar que os guias 3.1, 3.5, 3.6, 3.7 e 3.8 existem no repositório antes da aula e que o 3.6 tem critério de entrega claro (print do alerta + post-mortem). Tempo previsto passou de 8 para 7 min; a limpeza de 5 min acontece no fim deste slide (G3).

Desafio 3.6: o workbook é opcional; o alerta de heartbeat dispara entre 15 e 25 min depois do último heartbeat; o timer é desligado com `AzureWebJobs.Conciliacao.Disabled` (C maiúsculo, o nome da Function), e o deploy reaplica a lista inteira de app settings (um setting criado à mão some no próximo `deploy.ps1`).

Rate limit do guia 3.1: no APIM Consumption, a política `rate-limit` conta por subscription key ("on a per subscription basis") e "is only applied when an API is accessed using a subscription key"; a `rate-limit-by-key` não se aplica ao tier Consumption ([rate-limit](https://learn.microsoft.com/azure/api-management/rate-limit-policy), [rate-limit-by-key](https://learn.microsoft.com/azure/api-management/rate-limit-by-key-policy)). Cada empresa cliente tem a própria chave; nas notes, o limite aparece como "limite de 60 chamadas/min por chave de cada cliente", nunca por header.

### Validação cruzada com Speaker Notes
Notes cobrem: desafio 3.6, guias de leitura, limpeza ao vivo. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** O alerta por ausência de sinal custa? **A:** Alerta de pesquisa de log custa centavos por mês; no Free Trial cabe.

### Dica de demonstração
Apagar o `rg-saldaro-lab` do professor ao vivo, com a turma, e mostrar a tela de confirmação.

---

## Slide 22 — Encerramento e referências

### Verificação técnica
Todos os links abrem; QR code do repositório aponta para a URL correta (o repositório é privado por ora, decisão de 2026-10-14). O `limpar.ps1` citado nas notes apaga o `rg-saldaro-lab`, purga o Key Vault e o APIM (os dois ficam em soft-delete com o nome reservado) e apaga o `scripts/.env.local` (`Get-Help ./scripts/limpar.ps1`).

### Validação cruzada com Speaker Notes
Notes cobrem: onde estão os links e o kit, conferência da limpeza, refazer um incidente durante a semana, as duas coisas para levar. **OK — alinhado.**

---

## Apêndice — Perguntas que a turma vai fazer (FAQ-1 a FAQ-10)

### Verificação técnica
- **Fonte única:** `docs/research/2026-10-14-duvidas-das-tecnologias.md`, as dez perguntas marcadas [Slide] (1, 4, 6, 7, 8, 9, 10, 17, 18 e 19), na ordem da pesquisa; o FAQ-1 abre com "Por que APIM e não WAF?", pedido do professor. Bullets da "Resposta curta" e do "Quando a alternativa vence"; "O que falar" da "Resposta completa" e do "No kit da Saldaro"; "Insights do mundo real" do "Quando a alternativa vence", com o que mudaria em produção; "Referências" com as URLs do Learn da seção "Fontes" de cada pergunta. Nenhum fato entrou de fora da pesquisa, das ADRs e dos guias.
- **Ressalvas da pesquisa respeitadas** ("Conflitos com o kit" e "Lacunas"): "exactly once" só com a ressalva das páginas que divergem (FAQ-4; lacuna 5); o Network Security Perimeter só como alternativa a testar, porque a entrega do Event Grid num namespace com perímetro não está documentada (FAQ-3; conflito 3 e lacunas 3 e 9); o Durable Task Scheduler sem recomendação, porque o GA por SKU não foi confirmado (FAQ-9; lacuna 8); nenhum valor em moeda, e a comparação de custo remetida à calculadora (FAQ-1 e FAQ-3; lacuna 6); C# no Logic Apps Standard, em preview, só como ressalva (FAQ-7); a integração APIM → Service Bus, em preview, fora dos FAQ.
- **HMAC de webhook de entrada** (FAQ-10): o Learn não tem guia genérico (lacuna 2); a fala cita o exemplo mais próximo, os outgoing webhooks do Teams, e não promete política HMAC pronta no APIM.
- **Vocabulário do `CONTEXT.md`:** reenvio (do ERP), confirmação repetida (do banco), reentrega (do Service Bus), retry agendado (do hub) e reprocessamento (da DLQ), cada um no seu lugar; partição aparece só como recurso de fila do Service Bus ou do Event Hubs, nunca como sinônimo de sessão.
- **Conferir antes de publicar o deck:** reabrir as 62 URLs dos FAQ (G9).

### Validação cruzada com Speaker Notes
Cada FAQ tem, nas notes, "Tempo previsto" fora da soma das 4h, os ganchos, "Mostrar" (o visual do slide e, quando ajuda, uma tela do Portal ou do VS Code), "O que falar" de 60 a 90 segundos, "Insights do mundo real" com quando a alternativa vence e o que mudaria em produção, "Referências" e "Transição" de volta ao slide do gancho. O início do apêndice, nos dois arquivos, diz que o tempo dos FAQ fica fora das 4h. **OK — alinhado.**

### Tabela dos FAQ

| FAQ | Título (a pergunta) | Pesquisa | Ganchos (slides) | Referências do Learn | Palavras por bullet | "O que falar" |
|---|---|---|---|---|---|---|
| FAQ-1 | Por que APIM e não WAF? | 1 | 3 e 6 | 7 | 7, 8, 9 | ~220 palavras |
| FAQ-2 | Por que Service Bus, e não Storage Queue, Event Hubs ou Event Grid? | 4 | 3 | 4 | 7, 8, 8 | ~225 |
| FAQ-3 | Por que Service Bus Standard, e não Premium? | 6 e 23 | 2 e 21 | 9 | 8, 7, 8 | ~210 |
| FAQ-4 | Se a fila é FIFO, por que a fatura desbaixou? | 7 | 12 e 14 | 7 | 8, 7, 8 | ~205 |
| FAQ-5 | Se a fila tem duplicate detection, para que inbox e `Idempotency-Key`? | 8 | 9 e 14 | 6 | 8, 8, 7 | ~195 |
| FAQ-6 | Por que retry agendado à mão? O Functions não tem retry policy? | 9 | 17 e 18 | 8 | 8, 7, 7 | ~185 |
| FAQ-7 | Por que Functions? Integração não é coisa de Logic Apps? | 10 | 3 | 4 | 9, 8, 8 | ~200 |
| FAQ-8 | Por que Bicep e não Terraform? E por que consertar no Portal? | 17 | 2 e 9 | 3 | 7, 7, 8 | ~205 |
| FAQ-9 | Se o Durable dá o 202 de graça, por que fazer à mão? | 18 | 4 e 10 | 5 | 7, 7, 6 | ~200 |
| FAQ-10 | Por que HMAC no webhook, e não mTLS, OAuth ou lista de IPs? | 19 | 13 | 9 | 7, 8, 8 | ~210 |

### Ganchos nas notes dos slides principais

| Slide principal | Linha "Se perguntarem:" | Abre |
|---|---|---|
| 2 — Regras do dia | "Por que Bicep e não Terraform? E por que consertar no Portal, se o kit tem Bicep?" | FAQ-8 |
| 2 — Regras do dia | "Por que Service Bus Standard? Um hub de pagamentos não usaria Premium?" | FAQ-3 |
| 3 — A Saldaro Pagamentos | "Por que APIM e não WAF?" | FAQ-1 |
| 3 — A Saldaro Pagamentos | "Por que Service Bus, e não Storage Queue, Event Hubs ou Event Grid?" | FAQ-2 |
| 3 — A Saldaro Pagamentos | "Por que Functions? Integração não é coisa de Logic Apps?" | FAQ-7 |
| 4 — O que a Disciplina 04 já nos deu | "Se o Durable dá o 202 de graça, por que fazer à mão?" | FAQ-9 |
| 6 — Smoke test (simplificação de laboratório) | "Por que APIM e não WAF?" | FAQ-1 |
| 9 — O padrão: request-reply assíncrono | "Se a fila tem duplicate detection, para que a `Idempotency-Key`?" | FAQ-5 |
| 9 — O padrão: request-reply assíncrono | "Por que mudar no Portal, se o kit tem Bicep?" | FAQ-8 |
| 10 — Prova (Incidente 1) | "Se o Durable dá o 202 de graça, por que fazer à mão?" | FAQ-9 |
| 12 — Sintomas (Incidente 2) | "Se a fila é FIFO, por que a fatura desbaixou?" | FAQ-4 |
| 13 — Assinatura que vale: HMAC do corpo | "Por que HMAC no webhook, e não mTLS, OAuth ou lista de IPs?" | FAQ-10 |
| 14 — Uma vez só e na ordem certa | "Se a fila é FIFO, por que precisa de sessão? E partição não resolve a ordem?" | FAQ-4 |
| 14 — Uma vez só e na ordem certa | "Se a fila tem duplicate detection, para que inbox e ETag?" | FAQ-5 |
| 17 — Sintomas (Incidente 3) | "Por que não deixar o Service Bus reentregar até a DLQ? O Functions não tem retry policy?" | FAQ-6 |
| 18 — Classificar, agendar, triar | "Por que retry agendado à mão? O Functions não tem retry policy?" | FAQ-6 |
| 21 — Desafio para casa e guias de leitura | "Por que Service Bus Standard? Um hub de pagamentos não usaria Premium?" | FAQ-3 |

### Perguntas esperadas dos alunos
1. **Q:** Dá para pôr um WAF na frente do APIM Consumption? **A:** Dá, com o Front Door: o passo a passo do Learn vale para todos os tiers, inclusive o Consumption, com `ip-filter` nas faixas `AzureFrontDoor.Backend` e `check-header` no `X-Azure-FDID`. O APIM em modo privado atrás de um Application Gateway exige um tier com rede (FAQ-1).
2. **Q:** O Network Security Perimeter não dispensa o Premium? **A:** Em parte: ele nega o acesso público ao namespace Standard e libera só regras explícitas, mas não é Private Endpoint, e a entrega do Event Grid num namespace com perímetro não está documentada; teste antes de recomendar (FAQ-3; pesquisa, pergunta 23).
3. **Q:** E uma pergunta que não está nos FAQ? **A:** A pesquisa tem 23 perguntas; as outras 13, marcadas [Guia], também têm resposta curta pronta (Flex Consumption, .NET isolated, Table Storage, Key Vault, identidade gerenciada, observabilidade, CloudEvents, rede privada, entre outras), e várias já viraram quadros "Por que" no guia de montagem no Portal. Responda com a resposta curta da pesquisa e aponte o quadro, quando houver.

### Dica de demonstração
Ensaie o salto de ida e volta entre o slide do gancho e o FAQ no modo de apresentação do deck. Se a navegação por número não for rápida, deixe o apêndice aberto numa segunda aba e volte pela "Transição" da nota.

---

# Resumo da validação

## Fatos confirmados no Microsoft Learn (2026-10-13; links reabertos em 2026-10-14)

| # | Fato | Slides | Fonte |
|---|---|---|---|
| 1 | Resposta HTTP de Function limitada a 230 s em qualquer plano, independente do `functionTimeout` | 8 | [Functions scale and hosting](https://learn.microsoft.com/azure/azure-functions/functions-scale#function-app-timeout-duration) |
| 2 | `forward-request`: `timeout` padrão 300 s, acima de 240 s pode não ser honrado, erro `Timeout`; `follow-redirects` padrão `false` | 8, 9 | [forward-request](https://learn.microsoft.com/azure/api-management/forward-request-policy), [error handling](https://learn.microsoft.com/azure/api-management/api-management-error-handling-policies) |
| 3 | Async Request-Reply: 202 + `Location` + `Retry-After`; 200 pendente; 303 See Other; `Idempotency-Key` | 9, 10 | [Asynchronous Request-Reply](https://learn.microsoft.com/azure/architecture/patterns/asynchronous-request-reply) |
| 4 | Durable: 202 com `statusQueryGetUri`; 202 enquanto roda, 200 no fim | 10 | [Durable HTTP features](https://learn.microsoft.com/azure/durable-task/durable-functions/durable-functions-http-features) |
| 5 | Sessions e duplicate detection só na criação; rótulos "Enable sessions" e "Enable duplicate detection" | 14 | [enable-message-sessions](https://learn.microsoft.com/azure/service-bus-messaging/enable-message-sessions), [enable-duplicate-detection](https://learn.microsoft.com/azure/service-bus-messaging/enable-duplicate-detection) |
| 6 | Duplicate detection: `MessageId`, janela 10 min (20 s a 7 dias), aceita e descarta, inclui agendadas | 14, 15, 18 | [duplicate-detection](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection) |
| 7 | Sessions: lock exclusivo, ordem na sessão; sequence number não ordena o processamento; reprocesso perde a ordem | 14, 18 | [message-sessions](https://learn.microsoft.com/azure/service-bus-messaging/message-sessions), [message-sequencing](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing) |
| 8 | DLQ: `MaxDeliveryCount` 10, motivos do sistema, sem TTL nem limpeza, `SubQueue.DeadLetter` | 17, 19 | [dead-letter queues](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues) |
| 9 | `ServiceBusMessageActions` + `AutoCompleteMessages = false` (extensão 5.14.1+); poison não configurável no Functions | 17, 18 | [Service Bus trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus-trigger) |
| 10 | `host.json`: `maxConcurrentCalls` 16 por núcleo (sem sessões), `maxConcurrentSessions` 8 (com sessões) | 12, 14 | [Service Bus bindings](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus) |
| 11 | Scheduled messages e `ServiceBusMessage.ScheduledEnqueueTime` | 18 | [sequencing](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing#scheduled-messages), [SDK](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusmessage.scheduledenqueuetime) |
| 12 | `FixedTimeEquals` em tempo constante; `HMACSHA256.HashData` desde o .NET 6 | 13 | [FixedTimeEquals](https://learn.microsoft.com/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals), [HashData](https://learn.microsoft.com/dotnet/api/system.security.cryptography.hmacsha256.hashdata) |
| 13 | Key Vault references: sintaxe, versão nova em até 24 h, `Key Vault Secrets User` | 13 | [Key Vault references](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references) |
| 14 | `UpdateEntityAsync` com ETag falha com 412 | 14 | [Azure.Data.Tables](https://learn.microsoft.com/dotnet/api/azure.data.tables.tableclient.updateentityasync) |
| 15 | Service Bus Explorer: peek da DLQ, Resend copia, não suporta sessions | 17, 19 | [explorer](https://learn.microsoft.com/azure/service-bus-messaging/explorer) |
| 16 | Handshake do Event Grid não autentica a entrega | 13 | [CloudEvents endpoint validation](https://learn.microsoft.com/azure/event-grid/end-point-validation-cloud-events-schema) |
| 17 | Application Insights: Search (antigo Transaction search) e End-to-end transaction details | 6 | [failures, performance, transactions](https://learn.microsoft.com/azure/azure-monitor/app/failures-performance-transactions) |
| 18 | APIM Consumption: `rate-limit` por subscription key (só com chave); `rate-limit-by-key` fora do Consumption | 9, 21 | [rate-limit](https://learn.microsoft.com/azure/api-management/rate-limit-policy), [rate-limit-by-key](https://learn.microsoft.com/azure/api-management/rate-limit-by-key-policy) |
| 19 | Os fatos dos FAQ (WAF × APIM, comparação de mensageria, Standard × Premium, sessões e partições, detecção de duplicatas × Idempotent Consumer, DLQ e retry no Functions, Functions × Logic Apps, Bicep × Terraform, Durable × request-reply, segurança de webhook) | FAQ-1 a FAQ-10 | as fontes do Learn de cada pergunta em `docs/research/2026-10-14-duvidas-das-tecnologias.md` (consultadas em 2026-10-14; reabrir antes de publicar o deck, G9) |

## Itens do ensaio, conferidos no primeiro deploy real (2026-10-14)

Numa assinatura Sponsorship, em `brazilsouth` com Flex Consumption. O registro completo fica no checklist (`docs/research/2026-10-14-checklist-primeiro-deploy.md`).

1. **Slide 6**: conferido. O End-to-end transaction details do POST mostra APIM → hub → tabela → `PostCobranca` do banco no mesmo `operation_Id` (print no slide 6).
2. **Slide 8**: conferido. O 504 vem aos ~30 s com `{"erro":"o gateway esperou 30 s e desistiu"}`; o roteamento `saldaro` × `saldaro/webhooks` funciona; o `PostFatura` dura ~45 s e aparece com 499.
3. **Slides 9 e 13**: conferido. Um toggle novo vale em ~20 a 35 s (16 a 18 s para gravar).
4. **Slide 10**: em parte. A rajada de 1.000 faturas deu 202 em todas, sem 5xx, e drenou em ~2 min, mas numa Sponsorship. **Ainda a conferir:** a cota de 15 núcleos do Free Trial.
5. **Slide 12**: conferido. No baseline, `quantidadeBaixas = 3` por fatura; 18 de 20 terminam `notificada` e 2 regridem para `registrada`.
6. **Slide 14**: conferido. O print é do Overview → Essentials (Settings → Properties mudou); o `-SemPublicar` para em ~50 s; a mensagem do plano B avisa que a fila será recriada.
7. **Slide 15**: conferido. As chaves vêm com prefixo: `customDimensions["prop__SessionId"]`, `prop__SequenceNumber` e `prop__DeliveryCount`; a mensagem "Trigger Details" do host também traz o SessionId.
8. **Slide 17**: conferido. 204 abandonos em ~2 min; o Peek da DLQ com sessions funciona.
9. **Slides 18 e 19**: conferido. O `deploy.ps1 -Cenario 4 -Pronto -SemPublicar` leva ~3,6 min; a triagem mostra 52 na DLQ (32 antigas + 20 `SchemaInvalida`) e o `-Reprocessar` devolve 52 (40 corrigidas + 12 sem correção), zerando também o lote antigo.

**Novos na v1.4, a conferir no ensaio** (o primeiro deploy real usou o caminho por script; o "Portal primeiro" ainda não passou por uma turma):

10. **Slide 14**: o painel **+ Queue** com os campos do Passo 5.5 e o **Change** de **Lock duration** e de **Max delivery count** em Essentials, que o guia do Incidente 2 marca com `<!-- conferir no Portal -->`; e o print do painel com as duas caixas marcadas, para o visual do slide.
11. **Slide 18**: o **Change** ao lado de **Max delivery count** na Overview da fila (marcado `<!-- conferir no Portal -->` no guia do Incidente 3); sem ele, `az servicebus queue update @fila --max-delivery-count 3`.
12. **Apêndice**: o salto de ida e volta entre o slide do gancho e o FAQ no modo de apresentação do deck.

**Resolvidos na integração (2026-10-14):** a prova do Incidente 1 é por `referenciaErp` (o hub gera um `faturaId` por POST; nunca `quantidade = 2` num `faturaId`), com duas tentativas fixadas por `-MaxTentativas 2` e 2 faturas e 2 cobranças determinísticas (o baseline não desiste quando o ERP desiste); a corrida `cobranca-emitida` × `baixada` não acontece (webhooks só depois da resposta do banco e status monotônico na emissão); o reenvio com a mesma chave é o Passo 5.5 do guia; as colunas do CSV estão no `Get-Help` do `erp-simulado.ps1`; as 10 entregas em segundos vêm do abandon explícito; o `valor` em texto vale para os dois eventos; e a subscription key só existe no `cenario-1-pronto` (Atualização 1 do contrato), com a API `saldaro-webhooks` sem chave em todos os estados, então os comandos manuais dos cenários 2 a 4 não precisam do header `Ocp-Apim-Subscription-Key`.

## Estatísticas

- Slides com QA: 22/22 da aula e 10/10 do apêndice de FAQ (fonte, tempo fora das 4h, ganchos, bullets e "O que falar" conferidos nas G12 a G16).
- Ganchos "Se perguntarem: ... → FAQ-N" nas notes: 17, em 12 slides; todo FAQ tem pelo menos um.
- "Portal primeiro" (G17 e G18): notes de 10 slides ajustadas (2, 4, 5, 9, 12, 13, 14, 15, 18 e 19) e 3 bullets dos slides (2, 14 e 19); o deck repete na D10.
- Slides de incidente com QA completo (verificação técnica, validação cruzada, perguntas, pegadinhas, dica): 10/10.
- Perguntas antecipadas nos slides de incidente: 36.
- Pegadinhas de certificação nos slides de incidente: 31.
- Itens do ensaio: 9, dos quais 8 conferidos no primeiro deploy real (2026-10-14), mais 3 novos da v1.4 (os rótulos do Portal dos slides 14 e 18 e o salto do apêndice), ainda a conferir. Seguem abertos a cota do Flex numa Free Trial (slide 10) e o erro do consumidor sem sessão nos `traces` (Passo 5.7 do Incidente 2). Antes: Dos 11 da v1.0, quatro foram resolvidos na integração (`quantidade = 2`, corrida de status, reenvio com a mesma chave e colunas do CSV, subscription key) e dois em parte (reentrega imediata e `valor` nos dois eventos, que saíram do item do slide 17; os tempos de deploy, que viraram itens por slide).
