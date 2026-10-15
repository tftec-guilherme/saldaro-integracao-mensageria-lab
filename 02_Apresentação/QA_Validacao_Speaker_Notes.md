# QA — Validação das Speaker Notes
## Aula hands-on: Integração e Mensageria no Azure (29 slides, ao vivo, com a montagem no Portal entre os slides 12 e 13)

*Formato herdado do `QA_Validacao_Speaker_Notes.md` da Disciplina 04. Preencher por slide antes de montar o deck (`deck/project/`) e de novo antes de exportar o PPTX e o PDF.*

> **Version-anchor:** Q4-2026 · **Status:** v1.6 (2026-10-15). Desde a v1.2, o deck é um Artifact do tipo Slides no claude.ai, com a fonte em `deck/project/`. Na integração (ticket #13), slides e notes foram conferidos contra o código do kit (hub, banco simulado, ferramentas), o Bicep, a ajuda dos scripts (`Get-Help`), os KQL e os guias de `01_Aulas/`; os fatos do Microsoft Learn foram validados em 2026-10-13 e os links reabertos em 2026-10-14. A v1.3 alinhou slides e notes ao glossário do `CONTEXT.md` e às ADRs 0004 (aviso de ficção) e 0007 (simplificação de laboratório); a v1.4 trouxe o "Portal primeiro" da ADR 0008, com o conserto ao vivo no Portal e o `deploy.ps1` como plano B. **Na v1.5**, o QA segue o deck v19, refeito em 2026-10-14 e já publicado: 29 slides, com o porquê de cada tecnologia na ordem da montagem (4 a 10), a conta, a ordem e a montagem ao vivo no Portal (11, 12 e o bloco sem slide), a Saldaro funcionando (13) e os incidentes no fim (14 a 26). Os slides 2 a 13 e o bloco da montagem ganharam QA novo, conferido contra o guia de montagem e os quadros "Por que este recurso"; os slides 14 a 29 são os QA da v1.4 renumerados (eram 7 a 22), com o tempo de reinício do app medido no ensaio pelo Portal (5 a 35 s) e os rótulos do Portal que o ensaio conferiu. Saíram o apêndice de FAQ (as antigas G12 a G16, a D9 e a seção do apêndice) e os QA dos slides Regras do dia, A Saldaro Pagamentos, O que a Disciplina 04 já nos deu e o separador do Lab 0; as antigas G17 e G18 viraram G12 e G13, e a D10 virou D9. O que só um deploy real ou o ensaio confirmam está marcado como **Conferir no ensaio** em cada slide e resumido no fim do arquivo. **Na v1.6**, depois da primeira aula (gravada em 2026-10-14), os `<aside>` do deck passaram a seguir as notes: os tempos, as transições, o reinício de 5 a 35 s, os ganchos de FAQ trocados por respostas curtas, o ambiente que vamos montar no lugar do `rg-saldaro-lab` e o aviso de que, num ambiente montado à mão, o plano B pelo script só roda depois do `limpar.ps1`. A D2, a D4, a D5 e a D9 foram refeitas, a D3 registra o que o `<aside>` resume para caber em 4.000 caracteres, e a G15 diz onde está cada cuidado de telão.
>
> Empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência.

---

## Checklist geral (antes de montar o deck)

| # | Critério | Como verificar | OK? |
|---|---|---|---|
| G1 | `Conteudo_Slides_Aula.md` e `Speaker_Notes_Aula.md` têm os mesmos 29 títulos, na mesma ordem, que é a do `order` do `deck.json` | `Select-String '^## Slide \d+'` nos dois arquivos e comparar entre si e com o `order` | **OK** (2026-10-15): 29 × 29, idênticos e na mesma ordem nos dois arquivos e neste QA, na ordem do `order` (os slides 15, 17, 19, 22, 24 e 26 levam "(Incidente N)"); o bloco da montagem, entre o 12 e o 13, não é slide e não começa com "Slide" |
| G2 | Nenhuma seção de notes contém o marcador de pendência, e nenhum slide contém a nota de "a fechar" | `Select-String '\[A\sESCREVER\]'` nas notes e `Select-String '\(a\sfechar'` nos slides, ambos vazios | **OK** (2026-10-15): zero ocorrências nos três arquivos |
| G3 | Soma dos "Tempo previsto" ≤ 4h (240 min), incluindo a montagem ao vivo, a pausa de 10 min e a limpeza de 5 min | somar manualmente ou por script | **OK** (2026-10-15): 165 min de slides + 60 de montagem + 10 de pausa + 5 de limpeza = **240 min**, sem folga (tabela G3 abaixo). Para caber, os incidentes ficaram com 0,6 do roteiro dos guias (30, 45 e 30 min, contra 50, 75 e 50), e a montagem tem metas no relógio e ordem de corte (G14) |
| G4 | Todo slide de conteúdo tem "O que falar" com ≥ 150 palavras em prosa (separadores podem ter menos) | contagem por seção | **OK** (2026-10-15, contagem aproximada): o menor slide de conteúdo continua sendo o 27 (158 palavras, contagem da v1.2); entre os novos, de ~180 (slide 3) a ~540 (slide 13), e ~390 no bloco da montagem; os incidentes têm o texto da v1.4, entre 295 (slide 22) e 618 (slide 21), com acréscimos nos slides 14, 15 e 16; separadores 18 e 23 curtos por desenho, e o 14 com a história, o preparo do terminal e as duas regras dos incidentes. O deck condensa no `<aside>` (D2) |
| G5 | Todo slide de incidente (15–17, 19–22, 24–26) indica "Mostrar:" com o artefato exato (Portal/VS Code/terminal) | leitura | **OK** (2026-10-15): os dez têm "Mostrar:" com o caminho no Portal ou no VS Code e os comandos dos guias; 14, 15, 17, 19, 21, 24, 25 e 26 têm bloco de comandos pronto para colar (e o 28, o do `limpar.ps1`). Os slides 4 a 13 dizem a tela do Portal e a etapa da montagem em que ela aparece |
| G6 | Pelo menos uma "Pergunta para a turma" por bloco (abertura, por que cada tecnologia, custo e montagem, cada incidente, fechamento) | leitura | **OK** (2026-10-15): abertura (2), por que cada tecnologia (4 a 10), custo (11), Ver a Saldaro funcionando (13), Incidente 1 (15, 16, 17), Incidente 2 (19, 20, 21, 22), Incidente 3 (24, 25, 26), fechamento (27) |
| G7 | Nenhum termo proibido nos três arquivos | `pwsh ./scripts/varredura-termos.ps1` termina com "Varredura limpa" | **Professor roda a varredura de termos** antes de publicar o deck (a lista fica só na máquina dele); a do deck é a D5 |
| G8 | Nenhum dado real: nomes de pessoas, e-mails, IDs, números de negócio reais | leitura | **OK** (2026-10-15): busca por padrão (e-mail, GUID, CNPJ, telefone) nos três `.md`, mais leitura; só a Saldaro (fictícia) e os números fictícios do briefing e dos guias (300 empresas clientes, 400 mil cobranças/mês, 800 faturas, 1.200 webhooks, 3.800 mensagens); o `idCobranca` do POST forjado é `COB-00000000`; os preços são de lista, públicos |
| G9 | Todas as "Referências" apontam para Learn/EIP/Cloud Design Patterns e abrem | abrir links | **Em parte** (2026-10-15): os 43 links da contagem da v1.4 abriram com HTTP 200 em 2026-10-14 (os do Learn redirecionam para `en-us`). Fora do Learn/EIP, só fontes oficiais conscientes: a calculadora de preços e a conta gratuita (slide 11) e o Google SRE Book (slide 27). **A reabrir antes de publicar o deck:** o "Criar uma fila pelo Portal" do slide 21 (da v1.4) e os links novos da v1.5, nos slides 1 a 13 e no bloco da montagem, tirados das fontes dos quadros "Por que este recurso" do guia de montagem (consultadas em 2026-10-14). Os links do apêndice de FAQ saíram |
| G10 | Acentuação correta em todo o texto (pt-BR); termos de produto em inglês | leitura | **OK** (2026-10-15): leitura do texto novo (slides 1 a 13 e o bloco da montagem); termos do Portal como aparecem na tela: Overview, Essentials, Logs workspace, Investigate, Search, End-to-end transaction details, Application map, Data storage, Tables, Entities, Queues, Pricing tier, All Available Pricing, Access control (IAM), Scale and concurrency, Instance memory, On demand maximum instance count, Diagnose and solve problems, Environment variables, Show value, Show values, Advanced edit, Test, Delete resource group; nos incidentes, os rótulos da v1.4 (Service Bus Explorer, Peek Mode, Enable sessions, Enable duplicate detection, + Queue, (change), Max delivery count, Lock duration). A referência do slide 13 lembra o antigo "Transaction search" |
| G11 | `Version-anchor: Q4-2026` no cabeçalho dos três arquivos; serviços citados em GA | leitura | **OK** (2026-10-15): `Version-anchor: Q4-2026` no cabeçalho dos três `.md` de `02_Apresentação/` e na capa do deck; serviços usados em GA (Functions Flex Consumption, Service Bus Standard, APIM Consumption, Event Grid, Table Storage, Key Vault, Application Insights). As alternativas dos slides 4 a 10 (Cosmos DB for Table, Azure SQL, Storage Queue, Event Hubs, Service Bus Premium, Logic Apps, Container Apps, App Service, AKS, App Configuration, Application Gateway, Front Door, APIM Standard v2) aparecem só como comparação. Nada em preview é usado nem recomendado |
| G12 | "Portal primeiro" (ADR 0008): nenhum slide nem nota trata o `deploy.ps1` como caminho ao vivo de conserto. A montagem é no Portal, ao vivo; nos slides 16, 20, 21 e 25, o conserto é no Portal (app setting, fila recriada com **+ Queue**, **(change)** do **Max delivery count**), e o `deploy.ps1` aparece só como plano B, como o caminho de quem chegou sem ambiente ou como o Lab 0 pelo atalho do script | `Select-String 'deploy\.ps1'` nos dois arquivos e ler cada ocorrência; comparar com o parágrafo "Portal primeiro" dos guias dos três incidentes | **OK** (2026-10-15): o bloco da montagem (Lab 0 pelo script para quem chegou sem ambiente; o plano B do guia), 14 (as duas regras dos incidentes), 16 (toggle no Portal; `-Cenario 2 -Pronto` como plano B), 19 e 24 (quem chegou agora), 21 (fila recriada no Portal; `-SemPublicar` e `-Pronto` como plano B), 25 (toggles e **(change)** no Portal; `-Cenario 4 -Pronto -SemPublicar` como plano B), 26 (critério 5 com o **(change)** do Portal) e 29 (refazer em casa). Novo na v1.5: num ambiente montado à mão, o plano B pelo script só roda depois do `limpar.ps1` (guia de montagem, "Antes do primeiro `deploy.ps1`"), e as notes avisam na montagem, no 14 e no 16 |
| G13 | Drift explicado onde o conserto é clique: depois de cada mudança no Portal, a nota diz qual `.bicepparam` declara o mesmo conserto e por que o arquivo precisa concordar antes do próximo deploy | leitura dos slides 16, 20, 21, 22, 25 e 26 | **OK** (2026-10-15): 16 (`cenario-2-pronto` como versão declarada), 20 e 21 (`webhookHmac`, `filaComSessions` e `filaDupDetection` no `cenario-2-pronto.bicepparam`, depois `workerComSessao` e `inbox`), 22 (P6: arquivo e Azure dizem o mesmo), 25 e 26 (`cenario-4-pronto` declara os toggles e o `maxDeliveryCount = 3`) |
| G14 | A montagem ao vivo tem roteiro nas notes: tempo, metas no relógio, ordem de corte e plano B | leitura do bloco entre os slides 12 e 13 contra o guia de montagem (tempos das etapas, Passo 10.1 cedo, blocos Equivalente `az`, "Antes do primeiro `deploy.ps1`") | **OK** (2026-10-15): 60 min (0:30–1:30); metas em 0:38, 0:58, 1:15, 1:25 e 1:30; a Etapa 1 antes da aula e a 13 para casa; a ordem de corte (Passo 12.6, Etapa 9 pelo `az`, repetições pelo `az`; nunca o 8.3 nem o 10.8); o plano B do guia (`limpar.ps1` e `deploy.ps1 -Cenario 0`, ~15 min). **Conferir no ensaio:** os 60 min, com a turma olhando |
| G15 | Cuidados de telão nas notes, onde a tela mostra segredo, o Logs ou a policy do APIM | `Select-String 'Show value\|KQL mode\|Warning'` nas notes e ler cada ocorrência | **OK** (2026-10-15): o cuidado completo, **Show value** só na linha e nunca o **Show values** nem o **Advanced edit**, está no cabeçalho, que vale o dia todo, e na montagem; os slides 7, 9, 16, 20, 21 e 25 lembram o **Show value** na linha e o **Show values** da barra, e o 13, o **Show value**; o Logs numa aba própria, em **KQL mode**, com o **Agent** desligado (cabeçalho, 4, 13, 15, 19, 24 e a montagem; 17, 22 e 26 voltam à mesma aba); o **Warning** da policy do APIM, que é só aviso: **Save** (cabeçalho, 10 e a montagem); as telas de chave sem revelar o valor e os segredos do Passo 8.6 só na memória (montagem) |

### G3 — Tempos por bloco

| Bloco | Slides (Tempo previsto) | Soma | Janela no relógio | Folga |
|---|---|---|---|---|
| Abertura | 1 (2), 2 (3), 3 (3) | 8 min | 0:00–0:08 | 0 |
| Por que cada tecnologia | 4 (2), 5 (2), 6 (2), 7 (2), 8 (2), 9 (2), 10 (4) | 16 min | 0:08–0:24 | 0 |
| Quanto custa e a ordem da montagem | 11 (3), 12 (3) | 6 min | 0:24–0:30 | 0 |
| Montagem ao vivo | bloco sem slide, entre o 12 e o 13 | 60 min | 0:30–1:30 | 0 |
| Ver a Saldaro funcionando | 13 (10) | 10 min | 1:30–1:40 | 0 |
| Incidente 1 | 14 (3), 15 (9), 16 (6), 17 (12) | 30 min | 1:40–2:10 | 0 |
| Pausa | — | 10 min | 2:10–2:20 | — |
| Incidente 2 | 18 (1), 19 (11), 20 (7), 21 (18), 22 (8) | 45 min | 2:20–3:05 | 0 |
| Incidente 3 | 23 (1), 24 (11), 25 (7), 26 (11) | 30 min | 3:05–3:35 | 0 |
| Fechamento | 27 (10), 28 (7), 29 (3) | 20 min | 3:35–4:00, com a limpeza | 0 |
| Limpeza | no fim do slide 28, antes do 29 | 5 min | dentro do fechamento | — |
| **Total** | **165 min de slides** | **240 min** | **4h** | **0** |

Na v1.5, o deck v19 pôs o porquê de cada tecnologia e a montagem ao vivo antes dos incidentes, e o tempo do roteiro dos guias deixou de caber: os `<aside>` somavam 235 a 237 min de slides, sem contar a montagem (na v1.6, eles seguem esta tabela; D4). Os incidentes ficaram com 0,6 do roteiro de cada guia, arredondado por slide (Incidente 1: 5, 15, 10 e 20 → 3, 9, 6 e 12; Incidente 2: 1, 18, 12, 30 e 14 → 1, 11, 7, 18 e 8; Incidente 3: 1, 19, 12 e 18 → 1, 11, 7 e 11), em ritmo de demonstração e com o que cada guia manda cortar se o tempo apertar (no Incidente 1, o Passo 3.1 só na tela do professor e o Passo 5.6 para casa); o roteiro completo de cada guia fica para a turma refazer em casa. A montagem tem 60 min: a validação levou ~75 min para a montagem inteira, sem pausa, e ao vivo a Etapa 1 vem pronta, o Passo 12.7 é o slide 13 e a Etapa 13 fica para casa. A pausa fica onde o deck a põe, depois do Incidente 1; se a turma pedir antes, ela vem logo depois do slide 13, sem mudar o total. Os slides 27 e 28 mantêm 10 e 7 min, para o fechamento caber em 3:35–4:00 junto com a limpeza.

## Checklist do deck (`deck/project/`)

*Preencher depois de montar o deck e de novo antes de exportar o PPTX e o PDF. O deck é um Artifact do tipo Slides no claude.ai, privado e compartilhado pelo menu Share; a fonte versionada é o `deck.json` mais um `slides/<id>.html` por slide, com as notes do apresentador no `<aside>`.*

| # | Critério | Como verificar | OK? |
|---|---|---|---|
| D1 | O `order` do `deck.json` tem os 29 ids, cada um com o seu `slides/<id>.html`, na ordem e com os títulos do `Conteudo_Slides_Aula.md`; separadores em 14, 18 e 23 | script abaixo, mais leitura lado a lado com o `Conteudo_Slides_Aula.md` | **OK** (2026-10-15, por leitura): 29 ids e 29 arquivos; o título visível de cada slide é o **Título** do índice |
| D2 | Cada `<aside>` tem até 4.000 caracteres | script abaixo (mede o conteúdo bruto do `<aside>`, com as tags, o que é conservador) | **OK** (2026-10-15, script abaixo): 29 `<aside>`, nenhum acima de 4.000; o maior é o do `inc2-sintomas` (3.976), e outros seis passam de 3.900 (`inc1-sintomas` 3.975, `inc3-prova` 3.971, `inc2-camadas` 3.969, `inc3-classificar` 3.945, `inc1-padrao` 3.939 e `inc3-sintomas` 3.935): para acrescentar algo neles, corte prosa, não comandos |
| D3 | Cada `<aside>` traz o essencial do "O que falar" e da "Transição" do slide no `Speaker_Notes_Aula.md`, como roteiro de aula ao vivo, e o bloco "Comandos" onde as notes têm um (slides 14, 15, 17, 19, 21, 24, 25, 26 e 28), pronto para colar | leitura lado a lado com o `Speaker_Notes_Aula.md`; os "Comandos", linha a linha (no HTML, os marcadores `<...>` das notes perdem os sinais de menor e maior) | **OK, com condensações aceitas** (2026-10-15): o deck segue as notes no reinício de 5 a 35 s, nas respostas curtas no lugar dos ganchos de FAQ, no separador do Incidente 2 (45 minutos, até a prova), nas transições, no `faturaId` do `precisa-resolver`, no ambiente que vamos montar (`precisa-resolver` e `custo`), no aviso do plano B depois do `limpar.ps1`, no "Mostrar" dos slides 4 a 10, nos dois cuidados da primeira turma e no **Advanced edit** da `montagem-ordem`. Para caber em 4.000 caracteres, o `<aside>` resume o que as notes trazem por inteiro: o `inc1-prova` traz os quatro comandos sem os comentários de passo; o `inc2-camadas` aponta para o `queue show` do Passo 5.6 em vez de repeti-lo; o `inc3-classificar` fica sem o `az functionapp config appsettings set`; a definição de drift no `inc2-camadas` e o deploy do `cenario-4-pronto` que fecha o drift no `inc3-classificar`; e o "sem chave nenhuma no caminho" do `tec-event-grid`, que vale para a entrega (slide 8, verificação técnica) |
| D4 | O tempo de cada slide aparece no `<aside>`, e a soma dos 29 bate com a G3: 165 min de slides, 240 com a montagem, a pausa e a limpeza | somar os tempos dos `<aside>` e comparar com a tabela G3 | **OK** (2026-10-15): os 29 `<aside>` trazem o tempo das notes, e a soma dá 165 min de slides, igual à G3 |
| D5 | Nenhum termo proibido na fonte do deck, e o deck publicado é igual à fonte (nenhuma edição feita só no Artifact) | `pwsh ./scripts/varredura-termos.ps1` termina com "Varredura limpa" (o script varre os arquivos rastreados e os não rastreados, inclusive `deck.json` e `slides/*.html`); antes de republicar, comparar os arquivos publicados com a fonte | **OK** (2026-10-15): varredura limpa; o `deck.json` e os 29 `slides/*.html` publicados no Artifact (Versão 22) têm o mesmo hash de blob da fonte; só o `ds/poscloudazure/tokens.json`, que vem do design system, existe apenas no Artifact |
| D6 | Identidade visual do PPTX canônico da Disciplina 04: paleta, tipografia, rodapé TFTEC + Anhanguera, capa, separadores de bloco e 16:9 | comparar com o PPTX canônico, baixado do Blob fora do repositório (comando em `docs/research/2026-10-13-fontes-da-disciplina-04.md`) | |
| D7 | A jornada da fatura (slides 2 e 3) legível a 75% de zoom no projetor; as tabelas dos slides 4 a 10 legíveis; Sintomas, Padrão e Prova de cada incidente visualmente diferentes entre si | modo de apresentação do Artifact | |
| D8 | PPTX e PDF exportados do deck e publicados no Blob em `TFTEC - Pós Graduação/01_Disciplinas/Disciplina 04 - Integração e Mensageria/10_Aula Hands-on/`; nenhum dos dois no git | `az storage blob list` com o prefixo da pasta (comando em `docs/research/2026-10-13-fontes-da-disciplina-04.md`) mostra os dois; `git ls-files '*.pptx' '*.pdf'` volta vazio | |
| D9 | Os textos "Portal primeiro" estão no deck, sem `deploy.ps1` como caminho ao vivo: `inc1-padrao` (`<aside>`), `sep-incidente-2` e `inc2-sintomas` (quem chegou agora), `inc2-hmac` (`<aside>`), `inc2-camadas` (caixa "Imutáveis" e `<aside>`), `sep-incidente-3` e `inc3-sintomas` (quem chegou agora), `inc3-classificar` (`<aside>` e o bloco de comandos), `inc3-prova` (aquecimento depois do plano B) e `encerramento` (refazer em casa) | `Select-String 'deploy\.ps1'` em `deck/project/slides/*.html` e ler cada ocorrência contra a G12 | **OK** (2026-10-15, por leitura): as 17 ocorrências, em 12 slides (agora também `montagem-arquitetura`, `montagem-ordem` e `sep-incidente-1`, e não mais `inc3-prova`), são plano B, quem chegou agora, drift, aquecimento depois de um deploy, o aviso sobre o ambiente montado à mão ou refazer em casa; nenhuma é caminho ao vivo. Todo plano B pelo script, no `<aside>`, avisa que, num ambiente montado à mão, ele só roda depois do `limpar.ps1`; a caixa "Imutáveis" do `inc2-camadas` cita o plano B sem o aviso, que fica no `<aside>` |

Script da D1 e da D2, no PowerShell 7 e na raiz do repositório (o `order` tem 29 ids):

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
Sem conteúdo técnico. Confirmar título, subtítulo, logos, o "Version-anchor: Q4-2026" da capa e o aviso de ficção da ADR 0004 visível no rodapé ("Empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência."). A transição do `<aside>` leva ao slide 2, como as notes.

### Validação cruzada com Speaker Notes
Notes cobrem: postura de "plantão", diferença para a D04, como vai ser o dia (o porquê de cada peça, a montagem ao vivo, os incidentes no ritual sintomas → hipóteses → conserto → prova, o conserto no Portal com plano B e a limpeza no fim, o essencial do slide Regras do dia, que saiu) e o aviso de ficção. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Preciso ter feito a Disciplina 04? **A:** Sim; a aula assume o vocabulário de filas, tópicos, DLQ e Durable. Quem não fez acompanha pelos slides 4 a 10, que dizem o porquê de cada peça, e pelos guias.
2. **Q:** A empresa é real? **A:** Não. Saldaro Pagamentos é fictícia; os incidentes são inspirados em experiência de campo, mas reescritos com nomes, datas e números inventados.

### Dica de demonstração
Abrir, ao lado do slide, o repositório da aula já clonado e o Portal na assinatura do professor, ainda sem o `rg-saldaro-lab`: ele nasce na montagem.

---

## Slide 2 — O que a Saldaro precisa resolver

### Verificação técnica
- **Os seis requisitos e as peças** batem com o kit: teto de 10 instâncias por grupo de escala no hub (guia de montagem, Passo 8.3); `Idempotency-Key` com insert condicional na tabela `idempotencia` (contrato 5.1 e 5.4) e inbox por `idEvento`; assinatura HMAC com timestamp (contrato 7.2), conferida pelo hub no `WebhookBanco`, com o webhook chegando pela API `saldaro-webhooks`, que não exige subscription em nenhum estado (ADR 0007); a jornada no Application Insights pelo `faturaId`; ~R$ 2 a 3 da véspera ao fim da aula (caixa "CUSTO E FREE TRIAL" do guia de montagem).
- O card "Confiar no webhook do banco" diz "na API saldaro-webhooks do APIM": é o caminho do webhook, não quem valida. As notes dizem que a assinatura é conferida no hub.
- **Correlação:** o slide, as notes e o `<aside>` seguem a jornada pelo `faturaId`, que é o certo: o `operation_Id` não atravessa as fronteiras assíncronas (README do hub; slide 13).
- Os números da história (300 empresas clientes, ~400 mil cobranças/mês, picos nos dias 5 a 10) são fictícios e arredondados, do briefing (seções 1 e 2); manter assim.
- **Mostrar:** o ambiente do professor só nasce na montagem (o `<aside>` do slide 3 diz "ainda sem o rg-saldaro-lab"); as notes e o `<aside>` mostram o de quem montou em casa, e o `<aside>` fala do "ambiente que vamos montar".

### Validação cruzada com Speaker Notes
Notes cobrem: a jornada em cinco passos, os seis requisitos e a peça de cada um, o `faturaId` como correlação, a herança da Saldaro 1.0 (que vinha do slide A Saldaro Pagamentos, que saiu) e a pergunta do pré-mortem. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Por que Table Storage e não SQL ou Cosmos DB? **A:** É o slide 5: leitura por chave, insert condicional e ETag por centavos, no storage que o Functions já exige; o SQL vence quando uma transação precisa cobrir várias entidades.
2. **Q:** Pix de verdade usa webhook assim? **A:** O fluxo é genérico (cobrança com QR dinâmico + confirmação assinada); não reproduz a API de nenhum banco real.

### Dica de demonstração
Apontar na faixa, com o cursor, onde cada incidente vai quebrar: a chamada do passo 2, síncrona (Incidente 1), o webhook do passo 3 (Incidente 2) e a baixa do passo 4 (Incidente 3).

---

## Slide 3 — O que vamos montar

### Verificação técnica
- O diagrama bate com o do guia de montagem (Tabela de Recursos e diagrama): APIM, dois Function Apps no Flex Consumption, Service Bus com três filas, Event Grid, Storage com tabelas, Key Vault e Application Insights sobre Log Analytics.
- "O mesmo ambiente que o Bicep do cenário 0 cria": nos nomes, SKUs e configurações, sim; o guia lista o que o Portal decide sozinho ("O que fica diferente do Bicep": nome dos planos Flex, endereço com hash, GUID das role assignments, TTL de 14 dias das filas, soft delete de blobs, logger do APIM, tags e histórico de deployments), sem mudar o comportamento do lab.
- A jornada do slide é simplificada: o banco simulado manda o webhook pelo APIM, e o Service Bus aparece como "baixa da fatura" (a fila de confirmações).

### Validação cruzada com Speaker Notes
Notes cobrem: os cinco papéis, o Resource Group único, a igualdade com o Bicep do cenário 0, a jornada atravessando as seis caixas e a pergunta "o que quebra se ela não existir?". **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** O ambiente montado à mão é igual ao do script? **A:** Funcionalmente, sim; mas o Portal decide alguns nomes sozinho, e por isso o `deploy.ps1` não roda por cima dele: só depois do `limpar.ps1` (guia de montagem, "Antes do primeiro `deploy.ps1`"; registrado como defeito do kit).

### Dica de demonstração
Deixe a assinatura vazia no Portal ao lado do slide: o contraste com o Application map do slide 13 é a prova de que a montagem aconteceu.

---

## Slide 4 — Por que Application Insights e Log Analytics

### Verificação técnica
- Fatos dos quadros "Por que este recurso" dos Passos 3.1 e 3.2 do guia de montagem (fontes do Learn consultadas em 2026-10-14): o Application Insights clássico foi aposentado, e hoje ele é sempre workspace-based, com ingestão e retenção cobradas no workspace; 5 GB/mês grátis por conta de cobrança; retenção sem custo de 31 dias nas tabelas Analytics e de 90 dias nos dados do Application Insights; ~US$ 4,60/GB em Brazil South acima da franquia; o Azure Monitor workspace é outro produto, para Prometheus e PromQL; o SDK clássico 2.x se aposenta em 31/03/2027.
- **Sampling** desligado no `host.json` e no worker e 100% no APIM (Passo 10.3 do guia; contrato 5.5): é a premissa das provas que contam eventos.
- **Mostrar:** o recurso do professor só existe depois da montagem; as notes e o `<aside>` mostram o de quem montou em casa e levam a mesma tela do professor para a Etapa 3 e para o slide 13. Os slides 5 a 10 seguem o mesmo padrão.

### Validação cruzada com Speaker Notes
Notes cobrem: Azure Monitor × Log Analytics × Application Insights, as três opções da tabela, a configuração (30 dias, Brazil South, sampling desligado e o contrário em produção), o custo, o Prometheus como ressalva e o cuidado de telão do Logs. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Por que desligar o sampling, se o Functions vem com ele ligado? **A:** Porque a prova conta eventos por `faturaId`: com amostragem, um `fatura.baixada` podia sumir, e o `baixa-duplicada.kql` subcontaria. Em produção, sampling calibrado, tirando requisições e exceções da amostragem.
2. **Q:** Log Analytics e Application Insights não são a mesma coisa? **A:** O Log Analytics é onde o dado mora e se consulta com KQL; o Application Insights é o APM que entende a transação distribuída e grava no workspace.

### Pegadinhas de certificação
- AZ-204: Application Insights workspace-based; sampling adaptativo ligado por padrão no Functions; correlação W3C (`operation_Id`) entre chamadas HTTP.
- AZ-305: comece com um workspace só; mais workspaces só por segurança, residência de dados, cobrança separada ou retenção diferente.

### Dica de demonstração
No Search, busque o `faturaId` entre aspas, como o guia manda (Passo 12.5), e abra o End-to-end do POST: a parte síncrona aparece inteira; a assíncrona, só pelo `faturaId`.

---

## Slide 5 — Por que Table Storage

### Verificação técnica
- Fatos do quadro "Por que este recurso: Table Storage" (Passo 4.2 do guia): o insert falha com 409 (`EntityAlreadyExists`); o update com `If-Match` falha com 412; a transação atômica só vale na mesma tabela e na mesma PartitionKey, até 100 entidades ([entity group transactions](https://learn.microsoft.com/rest/api/storageservices/performing-entity-group-transactions)); o Cosmos DB for Table usa a mesma API e o mesmo SDK; PartitionKey + RowKey são o único índice; ~R$ 1/mês a conta inteira.
- As cinco tabelas e as chaves batem com o contrato (seção 5.4): `faturas` (PK = `faturaId`), `inbox` (PK = `faturaId`, RK = `idEvento`), `idempotencia` (PK = `clienteId`, RK = a chave), `conciliacao` e `cobrancas` (banco simulado).

### Validação cruzada com Speaker Notes
Notes cobrem: as duas operações que sustentam os consertos (409 e 412), as três opções da tabela, o limite da transação no Table Storage e por que a baixa é protegida pelo ETag, a configuração (LRS, tabelas sem esquema) e o custo. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Por que não Cosmos DB, se a API é a mesma? **A:** Ele vence com latência com SLA, índices secundários e distribuição global; o lab não precisa e pagaria outro modelo de cobrança.
2. **Q:** Sem transação entre `faturas` e `inbox`, o que impede a baixa dupla? **A:** A gravação condicional com ETag na fatura e a máquina de estados; a inbox é atalho e auditoria (slide 21).

### Pegadinhas de certificação
- AZ-204: concorrência otimista com ETag (`If-Match`) e o 412 Precondition Failed; `UpdateEntityAsync` com `ifMatch` no SDK.
- AZ-204: PartitionKey + RowKey são a chave primária e o único índice da tabela.

### Dica de demonstração
Depois do slide 13, abra a tabela `faturas` no Storage browser e mostre a PartitionKey igual ao `faturaId` da fatura que acabou de passar.

---

## Slide 6 — Por que Service Bus

### Verificação técnica
- Fatos do quadro "Por que este recurso: Service Bus namespace" (Passo 5.1 do guia) e da [comparação de mensageria do Learn](https://learn.microsoft.com/azure/service-bus-messaging/compare-messaging-services): "enterprise transactional messaging", para "financial transactions"; só o Service Bus tem transações e duplicate detection, e FIFO com sessions; Storage Queue sem garantia de ordem e sem dead-letter automático; Premium com messaging units, mensagens de até 100 MB e rede privada, a US$ 0,9275 por messaging unit por hora (~US$ 677/mês); Standard com base de US$ 10/mês (US$ 0,0134/h) e 13 milhões de operações incluídas; o Basic sem sessions nem duplicate detection; throttling 50009 com 1.000 créditos por segundo por namespace no Standard; cinco gatilhos na ADR 0003.
- O bullet "sessions e duplicate detection desligadas no baseline: são o coração dos Incidentes 2 e 3" é aproximado: sessions e duplicate detection são o conserto do Incidente 2; o Incidente 3 usa a DLQ e o `maxDeliveryCount` da mesma fila, e o `-t{n}` do retry existe por causa da duplicate detection.
- A fila `-poison` do banco simulado (Storage Queue, cinco tentativas) é o contraste citado nas notes (guia, Passo 4.3; contrato, seção 6).

### Validação cruzada com Speaker Notes
Notes cobrem: comando × aviso, as quatro opções da tabela, Standard × Premium com a ADR 0003, a configuração do baseline e o custo por hora, com a pergunta para a turma. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Por que Standard e não Basic, que é mais barato? **A:** O Basic não tem sessions nem duplicate detection, que são o conserto do Incidente 2.
2. **Q:** Um hub de pagamentos não usaria Premium? **A:** Quando um gatilho da ADR 0003 aparecer: rede privada exigida, throttling 50009 recorrente, mensagem acima de 256 KB sem claim check, latência previsível ou isolamento por empresa cliente, CMK ou Geo-Replication. A Leitura 3.8 é esse exercício.

### Pegadinhas de certificação
- AZ-204: Basic × Standard × Premium (tópicos, sessions, transações e duplicate detection só a partir do Standard).
- AZ-305: Service Bus para comando, Event Grid para evento discreto, Event Hubs para stream.

### Dica de demonstração
No assistente do namespace, mostre que o **Recommended** é o Premium e que o Standard está em **All Available Pricing** (Passo 5.1): é o tipo de padrão de tela que encarece um lab.

---

## Slide 7 — Por que Key Vault e identidade gerenciada

### Verificação técnica
- Fatos do quadro "Por que este recurso: Key Vault" (Passo 6.1 do guia): o prazo do soft-delete só se escolhe na criação (7 dias é o mínimo); a purge protection, uma vez ligada, não desliga mais; RBAC recomendado, access policies como legado; ser Owner não dá leitura de segredo (role `Key Vault Secrets Officer`, Passo 11.3); o App Configuration complementa o Key Vault; US$ 0,03 por 10 mil operações de segredo no Standard; apagar o cofre apaga as role assignments e as event subscriptions ligadas a ele.
- Rótulos do Portal conferidos no guia: na Overview, em Essentials, Soft-delete = Enabled e Purge protection = Disabled; na aba Properties, Permission model = Azure role-based access control.
- A Leitura 3.5 dá `Key Vault Secrets User` ao hub e troca os app settings por Key Vault references; sem versão, o app passa a usar a versão nova em até 24 h, ou na hora, se a configuração do app mudar ([Learn](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)).

### Validação cruzada com Speaker Notes
Notes cobrem: os dois segredos e o baseline em texto, app setting × Key Vault × App Configuration, identidade gerenciada (system-assigned × user-assigned), as duas escolhas sem volta, o Owner sem acesso a segredo, o custo e o cuidado de telão do **Show value**. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Por que o cofre nasce vazio? **A:** O baseline é ingênuo de propósito (item 4); o cofre nasce agora porque o prazo do soft-delete e a purge protection se decidem na criação, e a Leitura 3.5 só liga o que já existe.
2. **Q:** System-assigned ou user-assigned? **A:** System-assigned é a mais simples e morre com o app; user-assigned vence quando a role precisa existir antes do app, ou quando vários apps compartilham a identidade.

### Pegadinhas de certificação
- AZ-204: sintaxe da Key Vault reference (`SecretUri=` ou `VaultName=;SecretName=`) e a role `Key Vault Secrets User` na identidade do app.
- AZ-305: RBAC × access policies no Key Vault; purge protection é irreversível.

### Dica de demonstração
Em Environment variables, mostre a linha do `WebhookSecret` com o valor oculto e diga o que o **Show values** da barra revelaria; se precisar abrir um valor, só com o **Show value** daquela linha.

---

## Slide 8 — Por que Event Grid + fila

### Verificação técnica
- Fatos do quadro "Por que este recurso: Event Grid topic" (Passo 7.1 do guia) e da [entrega e retry do Event Grid](https://learn.microsoft.com/azure/event-grid/delivery-and-retry): push com nova tentativa por até 24 h (30 tentativas no padrão); at least once, sem garantia de ordem; dead-letter só se configurado; 100 mil operações grátis por mês; o esquema de entrada (CloudEvents 1.0) não muda depois de criado o tópico.
- A role `Azure Service Bus Data Sender` vai na fila `sbq-notificacoes-parceiro`, não no namespace (Passo 7.2), e a event subscription entrega com a identidade do tópico (Passo 7.3; ADR 0005).
- "Sem chave nenhuma no caminho", no `<aside>`, vale para a entrega (tópico → fila); a publicação do hub no tópico usa a `EventGridTopicKey` no baseline (Passo 8.8), e a Leitura 3.5 troca para identidade. As notes fazem a distinção.

### Validação cruzada com Speaker Notes
Notes cobrem: hub chamando o parceiro × evento publicado uma vez, por que o Event Grid sozinho não basta, o crossover Event Grid → fila do Service Bus, por que não o trigger de Event Grid direto (ADR 0005), a configuração e o custo, com a pergunta "onde está o aviso?". **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Por que não um tópico do Service Bus? **A:** Serviria, mas cada avaliação de filtro consome crédito no Standard, e o lab tem um consumidor só (guia, Passo 7.1; ADR 0005).
2. **Q:** O Event Grid pode notificar duas vezes? **A:** Pode: a entrega é at least once. O consumidor precisa ser idempotente, e a ADR 0005 prevê ligar a duplicate detection na fila se a repetição aparecer.

### Pegadinhas de certificação
- AZ-204: Event Grid × Event Hubs × Service Bus; retry e dead-letter da event subscription; CloudEvents × Event Grid schema.
- AZ-305: evento que exige workflow vai do Event Grid para uma fila do Service Bus (crossover).

### Dica de demonstração
Mostre o **Access control (IAM)** da fila `sbq-notificacoes-parceiro`, e não o do namespace: o escopo da role é a lição de privilégio mínimo.

---

## Slide 9 — Por que Functions no Flex Consumption

### Verificação técnica
- Fatos dos quadros dos Passos 8.1 a 8.3 do guia (fontes do Learn): o Flex Consumption é "the recommended serverless hosting plan"; o Consumption em Linux é legado, sem versões novas, sem .NET 10 e com aposentadoria em 30/09/2028; o .NET 8 e o modelo in-process saem de suporte no Functions em 10/11/2026, e o .NET 10 vai até 14/11/2028; o isolated é o único modelo de C# no Flex; um app por plano, sem deployment slots e sem edição de C# no Portal; escala por grupo de funções até 1.000 instâncias.
- **Cota:** 15 núcleos por região no Free Trial; 512 MB = 0,25 núcleo; o pior caso com tetos de 10 e 5 é ~13 núcleos (4 grupos × 10 × 0,25 no hub, mais o timer; 2 × 5 × 0,25 no banco); com 2.048 MB, ~51.
- **Franquia** de 100 mil GB-s e 250 mil execuções por mês, por assinatura, com a ressalva da página de preços ("on paid, consumption subscriptions only").
- Rótulos do Portal (Passo 8.3): Settings → Scale and concurrency, Instance memory e On demand maximum instance count; a cota em Diagnose and solve problems → Flex Consumption Quota.

### Validação cruzada com Speaker Notes
Notes cobrem: Functions × Logic Apps × Container Apps × App Service × AKS, o plano (Flex × Consumption × Premium × Dedicated), a escala por grupo, a configuração pela cota do Free Trial, o .NET 10 isolated e as restrições do Flex, o custo e a ressalva da franquia. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Por que não o Premium, que não tem cold start? **A:** Ele cobra ao menos uma instância o tempo todo; no Flex, as instâncias always ready resolvem o cold start do grupo HTTP quando precisar, com custo de linha de base.
2. **Q:** Dá para editar o código no Portal? **A:** Não em C# no Flex: por isso os consertos vêm prontos atrás de toggles (ADR 0006).

### Pegadinhas de certificação
- AZ-204: planos do Functions (Flex Consumption, Consumption, Premium e Dedicated) e o `functionTimeout` de cada um; isolated worker × in-process.
- AZ-305: escolha de computação: Functions para código orientado a eventos; Logic Apps para fluxo com conectores; Container Apps ou AKS para controlar imagem e cluster.

### Dica de demonstração
Abra a ferramenta Flex Consumption Quota durante a rajada do Incidente 1: o uso de núcleos subindo e descendo é a cota virando número.

---

## Slide 10 — Por que API Management

### Verificação técnica
- Fatos do quadro "Por que este recurso: API Management" (Passo 10.1 do guia) e do [API Gateway overview](https://learn.microsoft.com/azure/api-management/api-management-gateways-overview): no Consumption, sem `rate-limit-by-key` nem `quota-by-key`, sem VNet nem IP estático, logs só no Application Insights e **30 s de duração total por requisição**; 1 milhão de chamadas grátis por mês e ~US$ 4,25 por milhão acima disso; SLA de 99,95%; o Standard v2 cobra por hora.
- O guia de segurança do Functions: "the only way to truly secure your function endpoints is by implementing positive authentication of clients"; o padrão Gateway Offloading; o `erp-simulado.ps1 -Direto` como gateway bypass (ADR 0007).
- WAF × APIM: o WAF compara a requisição com o Core Rule Set da OWASP, barra bots e filtra por geografia; em produção, WAF na frente e APIM atrás (Architecture Center); com o Front Door, `ip-filter` nas faixas `AzureFrontDoor.Backend` e `check-header` no `X-Azure-FDID`, até no Consumption.
- **Policy:** ao salvar a do Passo 10.8, o Portal mostra um **Warning** sobre o formato das expressões, por causa das chaves do JSON no `set-body`; é só aviso, e **Save** grava (troubleshooting do guia). No primeiro deploy real, o `forward-request` venceu o teto do tier: 504 com o corpo do `on-error` em 30,3 s.

### Validação cruzada com Speaker Notes
Notes cobrem: endereço estável e Gateway Offloading, chave de função × autenticação do cliente, APIM × WAF, Consumption × Standard v2 e o teto de 30 s, a pergunta para a turma e o cuidado de telão da policy. A transição do `<aside>` leva ao custo, como as notes. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Por que não aumentar o timeout para 240 s? **A:** No Consumption, não dá: a duração total da requisição tem teto de 30 s; nos outros tiers, acima de 240 s o valor pode não ser honrado; e a Function não responde HTTP depois de 230 s. Aumentar só empurra o problema (Incidente 1).
2. **Q:** Dá para pôr um WAF na frente do APIM Consumption? **A:** Dá, com o Front Door: `ip-filter` nas faixas `AzureFrontDoor.Backend` e `check-header` no `X-Azure-FDID`. O APIM privado atrás de um Application Gateway exige um tier com rede.

### Pegadinhas de certificação
- AZ-204: o `rate-limit` conta por subscription e só com chave; o `rate-limit-by-key` não existe no Consumption; as seções `inbound`, `backend`, `outbound` e `on-error`.
- AZ-305: WAF na borda e APIM atrás; o padrão Gateway Offloading.

### Dica de demonstração
Salve a policy do Passo 10.8 ao vivo, na montagem, e deixe o **Warning** aparecer: é o momento de dizer que é só aviso e clicar em **Save**.

---

## Slide 11 — Quanto custa

### Verificação técnica
Os valores batem com a caixa "CUSTO E FREE TRIAL" e a Tabela de Recursos do guia de montagem: ~R$ 60/mês se esquecido ligado, ~R$ 55 do Service Bus Standard (US$ 0,0134/h ≈ R$ 0,07/h com ~R$ 5,50/US$), ~R$ 0,20 a 1 montando e apagando no mesmo dia, ~R$ 2 a 3 da véspera ao fim da aula, ~R$ 1/mês do Storage, as franquias do APIM (1 milhão de chamadas), do Event Grid (100 mil operações) e do Log Analytics (5 GB), e a ressalva da franquia do Flex no Free Trial. O Key Vault e o APIM ficam em soft-delete com o nome reservado (o APIM por 48 h, e o Portal não o purga), e o `limpar.ps1` purga os dois. Conferir na calculadora, na data da aula.

### Validação cruzada com Speaker Notes
Notes cobrem: o único medidor por hora, as franquias, a ressalva do Flex e a cota de 15 núcleos, montar no dia × na véspera, a regra de ouro com o `limpar.ps1` e a purga, os SKUs a evitar e a pergunta da fila esquecida (que vinha do slide Regras do dia). O `<aside>` e as notes mostram o ambiente de quem montou em casa; o do professor nasce na montagem, e a regra de ouro fala do "ambiente que vamos montar". **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** E se eu não tiver Free Trial? **A:** A premissa do kit é Free Trial próprio (US$ 200; contrato, seção 0). Outra assinatura serve, desde que o aluno possa criar Resource Groups e role assignments e tenha cota de Flex Consumption na região.
2. **Q:** Quanto custa esquecer ligado por uma semana? **A:** Uns R$ 13 (~R$ 1,80 por dia), quase tudo Service Bus (guia, Limpeza de Recursos).

### Dica de demonstração
Mostrar o botão **Delete resource group** agora e dizer que ele volta, de verdade, no slide 28.

---

## Slide 12 — A ordem da montagem

### Verificação técnica
- As treze etapas e os tempos batem com os títulos das etapas do guia de montagem (soma de ~178 min, as "~3 h"); o `<aside>` diz "~5 se já fez o Lab 0", e o guia, "~5 min se já fez as Etapas 1 a 4 do Lab 0".
- "Crie o APIM cedo": o guia sugere fazer o Passo 10.1 logo depois da Etapa 3; no teste do guia, o Consumption ativou em uns 2 minutos.
- "Na validação do kit, ~75 min" e "o Bicep faz o mesmo em uns 10 minutos" (guia, "Por que montar à mão").
- APIM × WAF, Service Bus × Storage Queue e Functions × Logic Apps: o `<aside>` remete aos slides do API Management, do Service Bus e do Functions, e as notes, aos slides 10, 6 e 9.

### Validação cruzada com Speaker Notes
Notes cobrem: a ordem e a dependência entre as peças, o APIM cedo, os quadros "Por que este recurso", os tempos de cada etapa, o que vem pronto (Etapa 1) e o que fica para casa (Etapa 13). **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Posso usar Cloud Shell em vez de instalar tudo? **A:** Para `az`, Bicep e PowerShell, sim. A publicação usa `dotnet publish` + `az functionapp deployment source config-zip`, então precisa do .NET 10 SDK; o Azure Functions Core Tools não é obrigatório (contrato, seção 0).
2. **Q:** Depois de montar à mão, posso usar o `deploy.ps1`? **A:** Só depois do `limpar.ps1`: o Bicep não adota o plano Flex que o Portal batizou nem as role assignments criadas à mão (guia de montagem, "Antes do primeiro `deploy.ps1`").

### Dica de demonstração
Abra um quadro "Por que este recurso" de verdade, o do Service Bus, por exemplo, para a turma ver o formato antes de a montagem começar.

---

## Montagem ao vivo — entre os slides 12 e 13 (sem slide)

### Verificação técnica
- **Tempo:** 60 min, com a Etapa 1 pronta antes da aula, o Passo 12.7 contado no slide 13 e a Etapa 13 para casa; a validação levou ~75 min para a montagem inteira, sem pausa. As metas no relógio (0:38, 0:58, 1:15, 1:25 e 1:30) são planejamento, não medição.
- **Ordem de corte** apoiada no guia: o Passo 12.6 é opcional; o bloco Equivalente `az` da Etapa 9 é "o caminho validado" (troubleshooting do guia); os blocos Equivalente `az` das outras etapas fazem o mesmo que os assistentes; os Passos 8.3 e 10.8 sustentam o Incidente 1.
- **Plano B:** o do guia (`limpar.ps1` e `deploy.ps1 -Cenario 0`, uns 15 minutos); num ambiente montado à mão, o `deploy.ps1` só depois do `limpar.ps1` (Conflict 59602 no plano Flex e `RoleAssignmentExists`, registrados como defeito do kit).
- **Cuidados de telão** do guia: **Show value** só na linha, nunca o **Show values** nem o **Advanced edit** (Passos 8.7 e 8.8); os segredos do Passo 8.6 só na memória da sessão; o **Warning** da policy (Passo 10.8).
- **Os dois cuidados da primeira turma** estão no guia: tudo no `pwsh`, conferido com `$PSVersionTable.PSVersion` (Passo 1.1), e o `az account set` na assinatura da montagem antes de publicar (Etapa 9), com o `ResourceGroupNotFound` da assinatura errada no troubleshooting; o bloco `az` da Etapa 9 é o plano B do Deploy do VS Code.
- **Conferir no ensaio:** a montagem inteira ao vivo em 60 min, com a turma olhando e perguntando.

### Validação cruzada com Speaker Notes
O bloco tem tempo, roteiro com metas, o que narrar (as caixas que os incidentes vão mexer), os dois cuidados da primeira turma (`pwsh` e `az account set`), a ordem de corte, o plano B, o aviso do `deploy.ps1` sobre o ambiente montado à mão e os cuidados de telão. O deck não tem slide para este bloco: o essencial dele vai no `<aside>` do slide 12, cuja transição leva à montagem, e a transição do bloco leva ao slide 13. **OK.**

### Dica de demonstração
Deixe o guia aberto numa segunda tela e narre o porquê de cada campo, não o clique: a turma já montou em casa e quer ver a decisão, não o caminho do mouse.

---

## Slide 13 — Ver a Saldaro funcionando

### Verificação técnica
- **Passo 12.7 do guia de montagem:** aba **Test** do `POST /faturas`, com o header `x-cliente-id` = `cliente-a` (sem ele, o hub responde 400) e o corpo em Raw (`referenciaErp` `DEMO-001`); 201 Created com `faturaId`, `status` `cobranca-emitida` e `idCobranca` (no baseline, o POST é síncrono; contrato 5.1); o status anda `cobranca-emitida` → `registrada` → `baixada` → `notificada` em menos de um minuto; no Overview da fila, o **Active** volta a 0 depois do **Refresh**; a `sbq-faturas-recebidas` fica parada no baseline; a `cobrancas` tem uma cobrança só. O guia avisa que o `quantidadeBaixas` pode aparecer 1 ou 0, pela atualização perdida do baseline (Passo 12.3).
- **Search e End-to-end:** no Search (Investigate), o `faturaId` entre aspas traz `fatura.recebida`, `cobranca.emitida`, `confirmacao.recebida` e `fatura.baixada`; o End-to-end transaction details do POST mostra só a parte síncrona (APIM → hub → banco simulado); webhook, Service Bus e Event Grid iniciam operações com outro `operation_Id`, e a prova da jornada é o `faturaId` (README do hub). Conferido no primeiro deploy real (resumo, item 1). O Application map mostra APIM, hub, banco simulado, Service Bus e Storage.
- O `smoke-test.ps1` faz a mesma jornada por script e imprime o `faturaId`, a `referenciaErp`, o tempo total e o `operation_Id` do POST (contrato, seção 9).

### Validação cruzada com Speaker Notes
Notes cobrem: a jornada à mão pela aba Test, os quatro status e o que cada um significa, a notificação ao parceiro (sempre ao ERP), as filas e as tabelas, o Search, o End-to-end da parte síncrona e o `faturaId` como prova, o Application map, o baseline ingênuo item a item e a simplificação de laboratório da ADR 0007 (os dois últimos vinham do antigo slide do smoke test). **OK — alinhado** com o Passo 12.7 e com o README do hub.

### Perguntas esperadas dos alunos
1. **Q:** Por que o baseline usa `RootManageSharedAccessKey` se é errado? **A:** Para que a Leitura 3.5 tenha o que consertar; é a herança mais comum em ambientes novos.
2. **Q:** O `operation_Id` atravessa a jornada inteira? **A:** Não. Do hub ao banco, a chamada HTTP síncrona leva o contexto de trace (W3C), e o End-to-end do POST mostra os dois. Na parte assíncrona (fila de Storage do banco, webhook, Service Bus, Event Grid), o `operation_Id` muda, e o README do hub avisa que ele não deve ser usado como prova. A correlação de negócio é o `faturaId`, presente em todos os eventos do hub. Se faltar telemetria, verificar o sampling (desligado no kit).
3. **Q:** A notificação ao parceiro vai para o banco? **A:** Não. "Parceiro" é o termo guarda-chuva para banco parceiro e ERP; a notificação sai da fila `sbq-notificacoes-parceiro` (Function `NotificarParceiro`) e vai sempre ao ERP da empresa cliente, pelo `callbackUrl` da fatura. No lab, as faturas dos scripts e da aba Test não têm `callbackUrl`, e a Function só marca a fatura como `notificada`.

### Dica de demonstração
Guardar o print do Search e do End-to-end transaction details para comparar nos incidentes.

---

## Slide 14 — Separador: Incidente 1

### Verificação técnica
Os comandos são os dos Passos 1.1 e 1.2 do guia do Incidente 1. As duas regras das notes vêm do slide Regras do dia, que saiu: o aquecimento (a sonda do Passo 5.1 responde 404; no primeiro deploy, ~2 s na primeira chamada e ~130 ms nas seguintes) e o plano B, que num ambiente montado à mão só roda depois do `limpar.ps1` (guia de montagem, "Antes do primeiro `deploy.ps1`"). **OK.**

---

## Slide 15 — Sintomas (Incidente 1)

### Verificação técnica
- **230 s**: confirmado no Learn ([Functions scale and hosting](https://learn.microsoft.com/azure/azure-functions/functions-scale#function-app-timeout-duration)): "Regardless of the function app timeout setting, 230 seconds is the maximum amount of time that an HTTP triggered function can take to respond to a request", por causa do idle timeout padrão do Azure Load Balancer. É o teto da plataforma, citado como contexto (contrato, seção 11), não a demonstração.
- **Corte do APIM**: `forward-request` tem `timeout` padrão de 300 s; valores acima de 240 s podem não ser honrados; ao estourar, o erro predefinido é `Timeout`, com origem `forward-request` ([forward-request](https://learn.microsoft.com/azure/api-management/forward-request-policy), [error handling](https://learn.microsoft.com/azure/api-management/api-management-error-handling-policies)). No tier Consumption, a duração total da requisição já tem teto de 30 s pela plataforma ([API Gateway overview](https://learn.microsoft.com/azure/api-management/api-management-gateways-overview); slide 10). O kit usa `<forward-request timeout="30" />` (contrato, seção 3). O código devolvido ao cliente, 504, não está escrito no Learn; o blog de suporte da Microsoft (Azure PaaS, "Troubleshooting 4xx and 5xx Errors with Azure APIM services", parte 2) lista o timeout baixo do `forward-request` entre as causas de 504.
- **Relógios**: o `HttpClient` do hub para o banco tem 120 s (contrato 5.2) e o `erp-simulado.ps1` tem timeout de 35 s (contrato, seção 9): o APIM é quem corta primeiro, aos 30 s, e o `on-error` da API devolve 504 quando `context.LastError.Source == "forward-request"` e `Reason == "Timeout"` (`infra/modules/apim-api.bicep`). O banco persiste a cobrança antes de esperar `latencia` (contrato, seção 6), daí a duplicata.
- **Duplicata de negócio** (resolvido na integração): o hub gera um `faturaId` novo a cada POST (README do hub), então o reenvio vira outra fatura e outra cobrança para a mesma `referenciaErp`, e cada `faturaId` tem `quantidade = 1` no banco: a prova é por `referenciaErp`, nunca `quantidade = 2` num `faturaId`. O baseline síncrono não passa o token da requisição para a chamada ao banco (`CONCEITO:` em `Aplicacao/ServicoDeFaturas.cs`): o hub segue esperando até o banco responder e grava `idCobranca` e `cobranca.emitida` mesmo depois do 504, então o `kql/faturas-por-referencia-erp.kql` mostra 2 faturas e 2 cobranças de forma determinística. O `-MaxTentativas 2` do guia fixa duas tentativas (o padrão do script é 5); o relatório do ERP mostra `504|504` e nenhum `faturaId`. Os webhooks do banco só ficam visíveis depois da resposta lenta, e o hub só grava `cobranca-emitida` se a fatura ainda estiver `recebida`: a fatura não "volta" de `baixada`.
- **Conferido no primeiro deploy real (2026-10-14):** (a) o 504 no terminal aos ~30 s, com o corpo do `on-error`; (b) a linha do `PostFatura` com ~45 s e 499 nos `requests`.

### Validação cruzada com Speaker Notes
Notes cobrem: a história no slide 14 (com o preparo do terminal), a reprodução do guia (`-MaxTentativas 2 -Caos "latencia=45"`), o corte do APIM aos 30 s (504 do `on-error`), o hub que não desiste e grava a cobrança aos 45 s, o reenvio que vira outra fatura e outra cobrança, a prova por `referenciaErp` (2 faturas e 2 cobranças; no banco, `quantidade = 1` por `faturaId`), a política do APIM e o "e se aumentasse o timeout", com o teto de 30 s do Consumption (slide 10), o worker de polling do legado e o 230 s como teto da plataforma. **OK — alinhado** com o guia do Incidente 1 (Partes 1 a 3) e com o código. O tempo do slide caiu de 15 para 9 min (G3): o Passo 3.1 fica só na tela do professor, como o guia manda se o tempo apertar.

### Perguntas esperadas dos alunos
1. **Q:** Por que não aumentar o timeout do APIM para 240 s? **A:** No Consumption, nem dá: a duração total da requisição tem teto de 30 s pela plataforma. Nos outros tiers, empurra o problema: o banco chegou a 90 s na história, o ERP desiste aos 35 s, e acima de 240 s o Learn avisa que o valor pode não ser honrado. A operação continua não idempotente: qualquer corte reabre a duplicata.
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

## Slide 16 — O padrão: request-reply assíncrono

### Verificação técnica
- **Padrão** no Learn ([Asynchronous Request-Reply](https://learn.microsoft.com/azure/architecture/patterns/asynchronous-request-reply)): 202 Accepted com `Location` e `Retry-After`; o status responde 200 enquanto a operação está pendente e **303 See Other** para o recurso quando conclui. O Learn recomenda 303 em vez de 302 (com 302, alguns clientes repetem o método original, "such as duplicate POST requests") e cita a `Idempotency-Key`, porque "the client can't distinguish between a lost response and a request that was never received".
- **Contrato 5.1**: `PostFatura` consertado responde 202 `{ faturaId, status: "recebida" }` com `Location: {UrlBasePublica}/faturas/{id}/status` e `Retry-After: 5`; `GetFaturaStatus` responde 200 `{ faturaId, status }`, 303 com `Location: {UrlBasePublica}/faturas/{id}` quando `baixada` ou `notificada`, 404 se não existe; tabela `idempotencia` com PK = `clienteId`, RK = chave (insert condicional); `EmitirCobranca` lança exceção no 503 do banco (reentrega pelo Service Bus).
- **APIM e o 303**: `follow-redirects` do `forward-request` é `false` por padrão, então o redirecionamento volta ao cliente ([forward-request](https://learn.microsoft.com/azure/api-management/forward-request-policy)); o `Location` já sai com a URL pública do APIM (`UrlBasePublica`).
- **Conserto** (guia, Parte 4): a turma liga `Cenario__AsyncRequestReply = true` no Portal (Environment variables, dois Apply e um Confirm; o app reinicia) e confere `toggles.AsyncRequestReply = True` no `/saude`; nada é publicado. É drift até o Bicep dizer o mesmo: a versão declarada é o `cenario-2-pronto.bicepparam`, e o plano B é `deploy.ps1 -Cenario 2 -Pronto` (com `-SemPublicar` se o código já estiver publicado; num ambiente montado à mão, só depois do `limpar.ps1`). Até a v1.0, as notes faziam o conserto por redeploy.
- **Conferido:** o toggle vale de 5 a 35 s depois do Confirm (no ensaio pelo Portal, 19 s neste passo; no primeiro deploy, 34 s); o plano B leva uns 6 a 7 min, porque republica os apps (guia, Passo 4.2).

### Validação cruzada com Speaker Notes
Notes cobrem: 202 + `Location` + `Retry-After`, status 200/303 e por que 303, `Idempotency-Key` com insert condicional, a fila como amortecedor, o `EmitirCobranca` com reentrega no 503, a resposta ao worker de polling do legado, leitura do código e o conserto pela app setting no Portal, com o plano B e o tempo de cada caminho (uns 2 min pelo Portal; 6 a 7 min pelo `deploy.ps1 -Cenario 2 -Pronto`), e as respostas curtas a "Idempotency-Key × duplicate detection" e "Portal × Bicep" (pesquisa de dúvidas, perguntas 8 e 17). **OK — alinhado** com o guia (Parte 4 e "Portal primeiro").

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

## Slide 17 — Prova (Incidente 1)

### Verificação técnica
- **Critérios** (guia do Incidente 1, Parte 5, que refina a seção 11 do contrato): 202 em < 500 ms com `latencia=60`; status 200 → 303; 1.000 faturas com `-Paralelismo 20 -IdempotencyKey` sem 5xx; `kql/faturas-por-referencia-erp.kql` vazio na janela da rajada (1 fatura e 1 cobrança por `referenciaErp`), com `quantidade = 1` no banco para cada `faturaId` como checagem complementar; mesma `Idempotency-Key` → mesmo `faturaId`. O `rateLimit` fica desligado no `cenario-2-pronto` (contrato, seção 4), então a rajada não bate em 429.
- **Durable** no Learn ([HTTP features](https://learn.microsoft.com/azure/durable-task/durable-functions/durable-functions-http-features)): o 202 traz `Location` com o `statusQueryGetUri`; o endpoint devolve 202 enquanto a instância roda e 200 quando termina. É o dialeto que as notes comparam com o 200/303 do kit.
- **Polling**: o guia usa `curl.exe -s -w " | HTTP %{http_code} %{redirect_url}"`, que não segue redirect sem `-L` e mostra o 303 cru. Alternativa testada no PowerShell 7.6 em 2026-10-13: `Invoke-WebRequest -MaximumRedirection 0 -SkipHttpErrorCheck -ErrorAction SilentlyContinue` devolve a resposta 3xx com o `Location` (com `-ErrorAction Stop`, o cmdlet falha por "contagem máxima de redirecionamentos").
- **Resolvido na integração**: o reenvio com a mesma `Idempotency-Key` é o Passo 5.5 do guia (o mesmo `$corpo` e os mesmos `$cabecalhos` do POST do Passo 5.1); as colunas do CSV do ERP são `referenciaErp,faturaId,cliente,tentativas,statusFinal,codigos,duracaoMs,idempotencyKey` (`Get-Help ./scripts/erp-simulado.ps1`); a corrida "303 e depois 200" não acontece mais: o banco só libera os webhooks depois da resposta, e o hub só grava `cobranca-emitida` se a fatura ainda estiver `recebida`.
- **Conferir no ensaio:** a escala do Flex Consumption durante a rajada (cota regional de 15 núcleos no Free Trial; o kit usa 512 MB e teto de 10 instâncias no hub e 5 no banco), que pode deixar a drenagem mais lenta sem mudar o 202.

### Validação cruzada com Speaker Notes
Notes cobrem as cinco checagens do guia (Passos 5.1 a 5.5), com os comandos (POST cronometrado, `curl.exe`, rajada com `-IdempotencyKey`, KQL por `referenciaErp`, mesma chave), o Overview da fila no Portal (na aba de Logs do slide 15), a comparação com o Durable (202/200 × 200/303), o post-mortem de 3 min com as lições numa frase e a resposta curta a "Durable × request-reply à mão" (pesquisa de dúvidas, pergunta 18). **OK — alinhado** com o guia (Partes 5 a 7). O tempo do slide caiu de 20 para 12 min (G3): o Passo 5.6 vira leitura para casa, como o guia manda, e os Passos 5.3 e 5.4 ficam.

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

## Slide 18 — Separador: Incidente 2

### Verificação técnica
Nenhuma. O `<aside>` diz "45 minutos, deste separador até a prova", como as notes (do 18 ao 22; G3). **OK.**

---

## Slide 19 — Sintomas (Incidente 2)

### Verificação técnica
- **Estado inicial** do cenário 3 = `cenario-2-pronto` (contrato, seção 4): webhook sem HMAC, fila sem sessions nem duplicate detection, worker sem sessão, sem inbox. Quem fez o Incidente 1 já está nele; quem tem uma diferença pequena corrige no Portal (Passo 0.3 do guia, "Pelo Portal"); quem chegou sem ambiente roda `deploy.ps1 -Cenario 3`.
- **Botões de caos** (contrato, seção 6): `duplicar=3` envia cada evento três vezes com o mesmo `idEvento`; `embaralhar=true` manda o `cobranca.paga` em 1 s e o `cobranca.registrada` em `atrasoWebhook + 3` = 8 s (padrão `atrasoWebhook = 5`). O guia usa 20 faturas: 20 × 2 eventos × 3 cópias = 120 webhooks (os 1.200 da história).
- **Concorrência**: `host.json` com `maxConcurrentCalls = 4` (contrato 5.5). No Learn, o valor vale por instância, é multiplicado pelo número de núcleos e só se aplica ao trigger com `isSessionsEnabled = false` ([Service Bus no host.json](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus)). Substitui as "duas instâncias" do briefing (contrato, seção 11).
- **Baseline** (contrato 5.3, passo 4): sem guarda de estado nem ETag, cada `cobranca.paga` grava `baixada` e publica de novo, e o `cobranca.registrada` atrasado "desbaixa" a fatura. O `WebhookBanco` aceita qualquer POST (contrato 5.1).
- **POST forjado**: corpo no formato do evento 7.1 do contrato, `idCobranca` fictício `COB-00000000` e `valor = 1.00`, contra a fatura de estudo tirada do `baixa-duplicada.kql` (Passo 2.5 do guia), enviado a `{SALDARO_APIM_URL}/webhooks/banco` (`/saldaro/webhooks/banco` no APIM; a API `saldaro-webhooks` nunca exige subscription key).
- **Conferido no primeiro deploy real (2026-10-14):** (a) `quantidadeBaixas = 3` na maioria das faturas, com um lost update (três `fatura.baixada` no KQL e `quantidadeBaixas = 2`) e 2 de 20 terminando `registrada`; (b) o POST forjado contra a fatura de estudo volta 202 e sobe o `quantidadeBaixas` de 3 para 4, uns 25 s depois. **Conferir no ensaio:** (c) a ingestão do Application Insights antes de rodar o KQL (o guia espera 1 minuto).

### Validação cruzada com Speaker Notes
Notes cobrem: a reprodução do guia com 20 faturas, duplicatas, desordem que desbaixa a fatura, lost update, concorrência 4 no lugar de "duas instâncias", confirmação forjada aceita (202), com a simplificação de laboratório da ADR 0007 (webhook anônimo e API `saldaro-webhooks` sem subscription) e o que muda em produção, as hipóteses H1 a H5 e a resposta curta a "FIFO × ordem de negócio" (pesquisa de dúvidas, pergunta 7). **OK — alinhado** com o guia do Incidente 2 (Partes 1 a 3) e com a seção 11 do contrato. Bullets do slide ajustados: a forjada passou por falta de validação (o baseline aceita qualquer POST) e a disputa vem da concorrência 4.

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

## Slide 20 — Assinatura que vale: HMAC do corpo

### Verificação técnica
- **Contrato 7.2**: headers `X-Saldaro-Timestamp` (segundos Unix) e `X-Saldaro-Signature: sha256=<hex minúsculo>`; conteúdo assinado `"{timestamp}.{corpo}"` com o corpo bruto em UTF-8; chave = `UTF8(WebhookSecret)`; validar antes de desserializar; |agora − timestamp| ≤ 300 s; inválida → 401 e nada na fila (contrato 5.1).
- **.NET**: `CryptographicOperations.FixedTimeEquals` leva um tempo que depende do tamanho das sequências, não do conteúdo, e só retorna `false` direto se os tamanhos diferem ([Learn](https://learn.microsoft.com/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals)); `HMACSHA256.HashData(key, source)` existe desde o .NET 6 ([Learn](https://learn.microsoft.com/dotnet/api/system.security.cryptography.hmacsha256.hashdata)).
- **App setting**: `Cenario__WebhookHmac` (contrato 5.2). Mudar a setting no Portal reinicia o app; é o caminho ao vivo ("Portal primeiro"), e não um atalho de sala. O deploy reaplica a lista inteira de app settings, então o guia leva `param webhookHmac = true` para o `cenario-2-pronto.bicepparam` no Passo 5.4, nos dois caminhos do slide 21: pelo Portal, para o arquivo dizer o que está no ar; pelo script (plano B), para o redeploy não desligar o HMAC em silêncio. A prova de que o banco continua passando é o `smoke-test.ps1` (Passo 4.4).
- **Key Vault reference** ([Learn](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)): `@Microsoft.KeyVault(VaultName=...;SecretName=...)` ou `SecretUri=...`; sem versão, o app passa a usar a versão nova em até 24 h, ou na hora, se a configuração do app mudar; a identidade do app precisa de `Key Vault Secrets User` (RBAC). No kit, o segredo só vai para o Key Vault com `identidadeGerenciada = true` (guia 3.5; contrato, seções 2 e 5.2): por isso o bullet foi ajustado.
- **Conferido no ensaio pelo Portal:** o `/saude` mostrou o HMAC ligado 13 s depois do **Confirm** (de 5 a 35 s).

### Validação cruzada com Speaker Notes
Notes cobrem: por que segredo no header não é assinatura, conteúdo assinado com timestamp, validação antes de desserializar, tempo constante, 401 sem fila, a demonstração com a app setting no Portal (o caminho da aula, e drift até o `.bicepparam` dizer o mesmo), o segredo ainda em app setting até o 3.5, quem pode chamar o webhook (simplificação de laboratório da ADR 0007; em produção, `ip-filter` ou mTLS e o backend só via APIM) e a resposta curta a "HMAC × mTLS × IP × OAuth" (pesquisa de dúvidas, pergunta 19). **OK — alinhado.**

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

## Slide 21 — Uma vez só e na ordem certa

### Verificação técnica
- **Imutabilidade** no Learn: "You can't enable or disable message sessions after the queue or subscription is created" e o mesmo para duplicate detection ([enable-message-sessions](https://learn.microsoft.com/azure/service-bus-messaging/enable-message-sessions), [enable-duplicate-detection](https://learn.microsoft.com/azure/service-bus-messaging/enable-duplicate-detection)). Rótulos no Portal: **Enable sessions** e **Enable duplicate detection**; propriedades ARM/Bicep `requiresSession`, `requiresDuplicateDetection` e `duplicateDetectionHistoryTimeWindow`. A janela pode mudar depois; ligar ou desligar, não. Bullet do slide ajustado para os rótulos do Portal.
- **Duplicate detection** ([Learn](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection)): baseada no `MessageId` (com particionamento, `MessageId + PartitionKey`); janela padrão de 10 min, mínimo 20 s, máximo 7 dias; o envio repetido "is accepted (the send operation succeeds), but the newly sent message is instantly ignored and dropped"; mensagens agendadas entram na detecção. Basic não suporta sessions nem duplicate detection.
- **Sessions** ([Learn](https://learn.microsoft.com/azure/service-bus-messaging/message-sessions)): com sessions ligadas, o cliente só envia e recebe dentro de sessão; só um receptor tem o lock de uma sessão; as mensagens da sessão chegam em ordem; abandonar faz a mesma mensagem voltar na próxima recepção. Sequence number garante a ordem de enfileiramento e de extração, não a de processamento ([sequencing](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing)).
- **host.json** ([Learn](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus)): `maxConcurrentSessions` padrão 8, só com `isSessionsEnabled = true`; `maxConcurrentCalls` só sem sessões. No hub, `sessionIdleTimeout = 00:00:10` (o padrão seria o `tryTimeout`, 1 min), para as sessões ociosas liberarem o worker e a fila drenar ao vivo.
- **Caminho do guia** (Partes 5 e 6, "Portal primeiro"): tentar no Portal e no CLI (`az servicebus queue update --enable-session true` responde `Sub code=40000`), drenar (`countDetails` em 0), apagar a fila no Portal, editar o `cenario-2-pronto.bicepparam` (`webhookHmac`, `filaComSessions` e `filaDupDetection` = `true`, Passo 5.4, para o arquivo registrar o que vai ser criado à mão), recriar no Portal com **+ Queue** (Passo 5.5: nome `sbq-confirmacoes-banco`, **Max delivery count** `10`, **Lock duration** 1 minuto, **Enable duplicate detection** com janela de 10 min, **Enable dead lettering on message expiration** e **Enable sessions** marcados, **Enable partitioning** desmarcado, os valores da `var filas` do `service-bus.bicep`), conferir com o `az servicebus queue show` do Passo 5.6, trocar o consumidor pelos dois app settings `AzureWebJobs.ProcessarConfirmacao*.Disabled` no Portal e ligar `Cenario__Inbox`; no fim, `workerComSessao` e `inbox` vão para o mesmo `.bicepparam`, que fica igual ao `cenario-3-pronto` (Passo 6.4). Planos B: `deploy.ps1 -Cenario 3 -Sufixo <sufixo> -SemPublicar` (o Bicep recria a fila com o arquivo editado; sem `-Pronto`, o script usa o arquivo como está no disco, e o aviso amarelo de `filaComSessions = true` com `workerComSessao = false` é esperado) e `deploy.ps1 -Cenario 3 -Pronto`, que detecta a divergência e recria a fila. Até a v1.0, as notes iam direto ao `-Pronto`; da v1.1 à v1.3, o caminho ao vivo era o redeploy com `-SemPublicar`, que na v1.4 virou plano B.
- **Contrato**: `cenario-3-pronto` liga `webhookHmac`, `inbox`, `filaComSessions`, `filaDupDetection` e `workerComSessao` (seção 4); os triggers alternam por `AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled` e `AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled` (5.1, 5.2); `deploy.ps1 -Cenario 3 -Pronto` recria a fila quando a mudança é imutável (seções 4 e 9); inbox PK = `faturaId`, RK = `idEvento` (5.4); ETag com concorrência otimista (5.3). No SDK, `UpdateEntityAsync` com `ifMatch` falha com 412 (Precondition Failed) se o ETag não confere ([Learn](https://learn.microsoft.com/dotnet/api/azure.data.tables.tableclient.updateentityasync)).
- **Conferido no primeiro deploy real (2026-10-14):** (a) Sessions e Duplicate detection aparecem em **Overview → Essentials** como "Disabled" (print no slide 21); Settings → Properties virou uma página genérica de metadados do ARM; (b) nos planos B, o `deploy.ps1 -Cenario 3 -SemPublicar` para no passo 11 em ~50 s se a fila não foi apagada e, com a fila apagada, recria em ~3,3 min; o `-Pronto` recria em ~6 a 7 min; (c) no `-Pronto`, o script avisa que a fila será apagada e recriada pelo Bicep e mostra as mensagens perdidas. **Conferido no ensaio pelo Portal:** (d) o erro do consumidor sem sessão só aparece com mensagem na fila (3,5 min de fila vazia sem nenhum registro; com mensagem parada, ~978 registros em 100 s; guia, Passo 5.7); (e) o painel **+ Queue** do Passo 5.5 (com o **Message time to live** de 14 dias no painel) e o **(change)** de **Lock duration** e de **Max delivery count**, na seção **Settings** da Overview, logo abaixo de **Essentials** (o guia não tem mais os marcadores `conferir no Portal`); o app reinicia 7 s depois do **Confirm** na troca do consumidor. (f) O deck v19 mostra o print do Overview → Essentials e o erro do CLI; o print do painel **+ Queue** com as duas caixas marcadas não entrou.

### Validação cruzada com Speaker Notes
Notes cobrem: as três camadas e o que cada uma pega, janela de 10 min × tolerância de 5 min, sessão = um consumidor por fatura na ordem de chegada (e o limite disso), inbox e máquina de estados com ETag, imutabilidade com o caminho do guia pelo Portal, na ordem dos Passos 5.2 a 5.6 (drenar, apagar, editar o `cenario-2-pronto.bicepparam` para o arquivo registrar o conserto, recriar com **+ Queue**, conferir pela API; depois, trocar o consumidor e ligar a inbox), os dois planos B (`-SemPublicar` e `-Pronto`), `host.json` (com o `sessionIdleTimeout` de 10 s), os dois triggers e as respostas curtas a "FIFO × sessão × partição" e "duplicate detection × inbox e ETag" (pesquisa de dúvidas, perguntas 7 e 8). O bloco "Comandos" traz os comandos dos Passos 5.1, 5.2 e 5.6, com o `-SemPublicar` no fim, como plano B. **OK — alinhado** com o guia do Incidente 2 (Partes 5 e 6 e "Portal primeiro"). O tempo do slide caiu de 30 para 18 min (G3), em ritmo de demonstração.

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
Antes de apagar a fila, mostre o Overview com zero mensagens ativas e zero na DLQ: é a drenagem que torna seguro apagar. No painel **+ Queue**, pare em cada caixa marcada e diga qual camada ela liga. Depois de criar a fila, volte ao mesmo Overview e mostre o "Duplicate detection window" de 10 min, que agora aparece com um **(change)** ao lado: a janela muda depois; ligar ou desligar, não.

---

## Slide 22 — Prova (Incidente 2)

### Verificação técnica
- **Critérios**: os P1 a P6 do guia (Parte 7): POST forjado com 401 e a fatura de estudo intacta; `baixa-duplicada.kql` vazio; as 20 faturas com `quantidadeBaixas = 1` e `notificada`; `traces` com `SessionId = faturaId` e `SequenceNumber` crescente (o log estruturado de `Aplicacao/LogDeConfirmacao.cs`); 1 notificação por fatura e ~120 `WebhookBanco` × 40 `ProcessarConfirmacaoComSessao`; o `.bicepparam` com os cinco toggles do `cenario-3-pronto`. Cobrem o contrato (seção 11: `baixa-duplicada.kql` vazio e `quantidadeBaixas = 1`) e o briefing (3.3). A concorrência 4 do `host.json` substitui as "duas instâncias" (contrato, seção 11). Slide e notes organizados em P1 a P6.
- **"Exactly once"**: o Learn diz que ligar duplicate detection "guarantees exactly once delivery over a user-defined span of time" ([enable-duplicate-detection](https://learn.microsoft.com/azure/service-bus-messaging/enable-duplicate-detection)): é entrega na janela, não processamento.
- **Sampling** desligado no `host.json` e no worker (contrato 5.5): sem isso, o KQL de baixa duplicada pode subcontar.
- **Conferido no primeiro deploy real (2026-10-14):** as chaves vêm com o prefixo `prop__` em `customDimensions` (`prop__SessionId`, `prop__SequenceNumber` e `prop__DeliveryCount`), e a linha "Trigger Details" do host também traz o SessionId; 19 das 20 faturas estavam `notificada` aos 51 s. **Conferir no ensaio:** o tempo entre a rajada e o KQL (o guia espera 1 minuto de ingestão depois de a fila zerar; no troubleshooting, 1 a 2); se o KQL demorar, a prova pela tabela `faturas` (`quantidadeBaixas = 1`) e pela `inbox` vem primeiro.

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

## Slide 23 — Separador: Incidente 3

### Verificação técnica
Nenhuma. **OK.**

---

## Slide 24 — Sintomas (Incidente 3)

### Verificação técnica
- **Estado inicial** do cenário 4 = `cenario-3-pronto` (contrato, seção 4): sessions, duplicate detection, inbox e HMAC ligados; `dlqComMotivo` e `retryAgendado` desligados; `maxDeliveryCount = 10`.
- **Botões de caos** (contrato, seção 6): `valorComVirgula=true` → `"valor": "1.250,00"` nos dois eventos (README do banco); `falhaTransiente=50` → 50% das chamadas `POST /cobrancas` com 503 e 50% dos `cobranca.paga` com `simulaTransiente = true` e `transienteAte = envio + 45 s`. Quem mudou o formato foi o banco parceiro (contrato, seção 11); bullet do slide e transição do slide 22 ajustados.
- **Duas rajadas** (guia, Passo 2.1): `-Faturas 10 -Caos "valorComVirgula=true"` (20 mensagens envenenadas: as duas confirmações de cada fatura) e `-Faturas 20 -Caos "falhaTransiente=50"` (uns 10 pagamentos bons pegos pela oscilação). A lógica de baixa desserializa antes de olhar `simulaTransiente` (contrato 5.3, passos 1 e 2): com os dois botões na mesma rajada, nenhuma mensagem boa cairia na DLQ. No primeiro deploy real, a DLQ chegou a 32 (20 envenenadas + 12 transientes; o número de transientes varia), todas `MaxDeliveryCountExceeded`, com 204 abandonos em ~2 min. Até a v1.0, as notes usavam `valorComVirgula=true;falhaTransiente=30` e `falhaTransiente=30` com 20 faturas cada.
- **DLQ** no Learn ([dead-letter queues](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)): `MaxDeliveryCount` padrão 10 → `MaxDeliveryCountExceeded`; sem limpeza automática; TTL não vale na DLQ; aplicações podem definir os próprios motivos. No Functions, "Poison message handling can't be controlled or configured": quem trata é o Service Bus ([trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus-trigger)).
- **Service Bus Explorer** ([Learn](https://learn.microsoft.com/azure/service-bus-messaging/explorer)): Peek Mode → DeadLetter → Peek from start (até 100 mensagens), com corpo e Message Properties; o Learn avisa que o Explorer "doesn't support management operations and sessions".
- **Resolvido na integração**: o hub faz `AbandonMessageAsync` explícito e relança a exceção (`Aplicacao/AbandonExplicito.cs`), então a reentrega é imediata e as 10 entregas acontecem em segundos; o `valorComVirgula` vale para os dois eventos, e as 10 faturas geram 20 envenenadas.
- **Conferido no primeiro deploy real (2026-10-14):** (a) 204 abandonos em ~2 min no baseline; (b) o Peek da DLQ de uma fila com sessions **funciona** no Service Bus Explorer (aba "Dead-letter (N)", Peek Mode, "Peek from start"; print no slide 24), apesar do aviso do Learn sobre sessions; o `triagem-dlq.ps1` continua como o caminho que agrupa por motivo.

### Validação cruzada com Speaker Notes
Notes cobrem: o banco parceiro mudando o formato numa atualização da API, a preparação (triagem de aquecimento), as duas rajadas do guia e por que são duas, a reentrega imediata até a DLQ, as mensagens boas misturadas, o Explorer, as hipóteses (exceções, ritmo das reentregas, triagem só leitura com um grupo só), o dilema reprocessar × descartar (com o risco de ler R$ 1,25) e a resposta curta sobre o retry policy do Functions (pesquisa de dúvidas, pergunta 9). **OK — alinhado** com o guia do Incidente 3 (Partes 1 a 3). O tempo do slide caiu de 19 para 11 min (G3): a triagem de aquecimento pode ser disparada já no slide 23.

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

## Slide 25 — Classificar, agendar, triar

### Verificação técnica
- **Assinatura** conferida no código-fonte da extensão `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus` em 2026-10-13: `DeadLetterMessageAsync(ServiceBusReceivedMessage message, Dictionary<string, object>? propertiesToModify = default, string? deadLetterReason = default, string? deadLetterErrorDescription = default, CancellationToken cancellationToken = default)`. Por isso o slide usa os parâmetros nomeados `deadLetterReason:` e `deadLetterErrorDescription:` (o esqueleto tinha `reason:` e `description:`, que não compilam). `ServiceBusMessageActions` exige `AutoCompleteMessages = false` e a extensão 5.14.1 ou superior ([trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus-trigger)).
- **Contrato 5.3**: `SchemaInvalida` com `DeadLetterErrorDescription` = mensagem do erro (até 1.024 caracteres, limite escolhido pelo kit); retry com cópia de `MessageId = {idEvento}-t{n}`, mesmo `SessionId`, `tentativa = n`, `ScheduledEnqueueTime = agora + 5 s × 2^n` (n = 1..3: 10, 20 e 40 s) e a original completada; acima de 3 → `TentativasEsgotadas`; eventos `mensagem.deadletter` e `mensagem.retry.agendado` (seção 10). Bullet do slide corrigido de `2^n s` para `5 s × 2^n`. Com `transienteAte = envio + 45 s`, as tentativas caem em ~10, ~30 e ~70 s: a terceira passa.
- **Scheduled messages** ([Learn](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing#scheduled-messages)): não aparecem na fila antes do horário e, ao ativar, ganham novo sequence number; no SDK, `ServiceBusMessage.ScheduledEnqueueTime` (`DateTimeOffset`) ([Learn](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusmessage.scheduledenqueuetime)); mensagens agendadas entram no duplicate detection ([Learn](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection)), daí o `-t{n}`.
- **Conserto do guia** (Parte 4, "Portal primeiro"): os dois toggles no Portal (`Cenario__DlqComMotivo` e `Cenario__RetryAgendado`, Passo 4.2) e o **Max delivery count** de `10` para `3` no **(change)** da seção **Settings** da Overview da fila, abaixo de **Essentials** (Passo 4.3), conferido com o `az servicebus queue show` do Passo 4.4; a comparação `cenario-3-pronto` × `cenario-4-pronto` mostra a versão declarada do mesmo conserto, e o drift fica de propósito até um deploy do `cenario-4-pronto`. O `maxDeliveryCount` muda numa fila existente (o que é imutável são sessions e duplicate detection): nada é recriado, e a DLQ do incidente fica para a triagem. Planos B: `deploy.ps1 -Cenario 4 -Pronto -SemPublicar`, com a fila parada (no what-if, a fila aparece como **Deploy**, `!`, sem valores, e não como criação; a v1.3 dizia "`~`, entre os modificados", e o guia corrigiu), e `-Cenario 4 -Pronto` sozinho, com publicação. Sem o **(change)** na Overview, o guia manda `az servicebus queue update @fila --max-delivery-count 3`. A execução das rajadas de novo e da triagem fica no slide 26, como na Parte 5 do guia.
- **Triagem** (contrato, seções 8 e 9): peek com `SubQueue.DeadLetter` agrupado por `DeadLetterReason`; `-Reprocessar` corrige o `valor` pt-BR para número, reprocessa com `MessageId = {original}-r{n}`, mesmo `SessionId` e `reprocessadoDe`, e completa a mensagem da DLQ; `-DescartarMotivo` completa as do motivo informado.
- **Ordem no reprocesso** (Learn, sessions): "If a dead-lettered message is later moved back to the original queue for reprocessing, the original ordering relative to other session messages is lost".
- **Conferido:** o **(change)** sob **Max delivery count** fica na seção **Settings** da Overview, abaixo de **Essentials** (ensaio pelo Portal; o guia não tem mais o marcador); os toggles valeram 7 s depois do **Confirm** no ensaio (de 5 a 35 s); o plano B `deploy.ps1 -Cenario 4 -Pronto -SemPublicar` levou ~3,6 min no primeiro deploy; a saída do `triagem-dlq.ps1` no guia é a do primeiro deploy, no formato do `Saldaro.Ferramentas`.

### Validação cruzada com Speaker Notes
Notes cobrem: permanente × transiente, `DeadLetterMessageAsync` com motivo e descrição, `AutoCompleteMessages = false`, retry agendado com backoff e teto, o `-t{n}` por causa do duplicate detection, os toggles e o `maxDeliveryCount = 3` no Portal sem recriar a fila, o `cenario-4-pronto` como versão declarada e o drift até o próximo deploy, o plano B com `-Pronto -SemPublicar`, a triagem explicada para a prova (reprocesso com `-r{n}` e `reprocessadoDe`, inbox pelo `idEvento`, `-DescartarMotivo`) e a resposta curta sobre o retry policy do Functions (pesquisa de dúvidas, pergunta 9). O bloco "Comandos" tem o `az servicebus queue update` de reserva e o `deploy.ps1` no fim, como plano B. **OK — alinhado** com o guia (Parte 4 e "Portal primeiro").

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

## Slide 26 — Prova (Incidente 3)

### Verificação técnica
- **Critérios** (briefing 3.4; contrato, seção 11, "DLQ vazia após a triagem"): `SchemaInvalida` na primeira entrega; transientes resolvidos com tentativas 1..3 e nunca na DLQ; triagem por motivo; DLQ vazia após o reprocesso; `maxDeliveryCount = 3` e lock de 1 min no Bicep (contrato, seções 2 e 4). Números do guia (Parte 5), com as mesmas duas rajadas: +20 `SchemaInvalida` com `DeliveryCount` 1; `mensagem.retry.agendado` com 1/10 s, 2/20 s e 3/40 s e nenhuma `TentativasEsgotadas`; triagem com ~30 `MaxDeliveryCountExceeded` + 20 `SchemaInvalida`; o `-Reprocessar` reprocessa ~50 e a DLQ chega a 0; a consulta final dá 60 faturas, 60 baixadas e nenhuma baixa dupla.
- **KQL** `kql/dlq-por-motivo.kql` sobre `mensagem.deadletter` (faturaId, idEvento, motivo) (contrato, seção 10).
- **Alerta** `alrt-dlq-confirmacoes` (métrica `DeadletteredMessages` > 0 na `sbq-confirmacoes-banco`) só existe com `alertas = true`, no cenário 6, que é o desafio 3.6 (contrato, seções 2 e 4).
- **Conferido no primeiro deploy real (2026-10-14):** o `-Reprocessar` devolveu 52 (40 corrigidas + 12 sem correção), zerando também o lote antigo (`MaxDeliveryCountExceeded`), e as faturas reprocessadas aparecem `notificada`, com `quantidadeBaixas = 1`.

### Validação cruzada com Speaker Notes
Notes cobrem os cinco critérios com os números do guia (20 `SchemaInvalida`, retries de 10/20/40 s, dois grupos na triagem, 60 de 60 baixadas), o aquecimento depois do restart (obrigatório depois do plano B), a política da fila no Azure pelo **(change)** do Portal e declarada no `cenario-4-pronto`, por que três entregas bastam e o post-mortem de 3 min com o alerta de DLQ ligando ao desafio. **OK — alinhado** com o guia (Partes 5 e 6). O tempo do slide caiu de 18 para 11 min (G3): as três triagens rodam enquanto a conversa segue.

### Perguntas esperadas dos alunos
1. **Q:** Reprocessar pode baixar duas vezes? **A:** Não: a inbox olha o `idEvento` do corpo, que não muda no reprocessamento; só o `MessageId` muda (`-r{n}`).
2. **Q:** E a ordem dentro da sessão no reprocesso? **A:** A ordem original se perde (novo sequence number), e a máquina de estados tolera: "registrada" depois de "baixada" é ignorada.
3. **Q:** O que fazer com `TentativasEsgotadas`? **A:** Investigar a dependência e, quando ela voltar, reprocessar. É o motivo que diz "a mensagem estava certa; o mundo é que estava fora do ar".

### Pegadinhas de certificação
- AZ-204: ler a DLQ no SDK = `ServiceBusReceiverOptions.SubQueue = SubQueue.DeadLetter`; no Portal, o Resend do Service Bus Explorer envia uma cópia e não apaga a original.
- AZ-305: DLQ é parte do desenho operacional: alerta, runbook e dono definidos antes do primeiro incidente.

### Dica de demonstração
Termine o incidente com a métrica `DeadletteredMessages` da fila no Portal caindo para zero: é o "52 → 0" do visual, ao vivo.

---

## Slide 27 — Post-mortem coletivo

### Verificação técnica
Nomes dos padrões corretos (Asynchronous Request-Reply, Idempotent Receiver, Resequencer, Dead Letter Channel, Retry). Template de cinco linhas igual ao do kit (`templates/post-mortem.md`), com a descrição de cada linha que o slide mostra. Tempo previsto passou de 12 para 10 min na v1.1 (G3).

### Validação cruzada com Speaker Notes
Notes cobrem: leitura de post-mortems, causa raiz como decisão de arquitetura, os padrões e as três frases do legado, que o slide põe nos cards dos três incidentes. **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** Post-mortem precisa de ferramenta? **A:** Não; cinco linhas num markdown no repositório bastam. O hábito importa mais que o formato.

### Pegadinhas de certificação
- AZ-305 cobra nomear o padrão a partir de um cenário descrito em prosa; treinar a tradução "sintoma → padrão".

---

## Slide 28 — Desafio para casa e guias de leitura

### Verificação técnica
Confirmar que os guias 3.1, 3.5, 3.6, 3.7 e 3.8 existem no repositório antes da aula e que o 3.6 tem critério de entrega claro (print do alerta + post-mortem). O slide traz o tempo estimado de cada guia (desafio ~2h; leituras de ~1h30 a ~1h45). Tempo previsto passou de 8 para 7 min na v1.1; a limpeza de 5 min acontece no fim deste slide (G3). Os comandos do `limpar.ps1` são os do `<aside>`: com `-Sufixo`, ele purga o Key Vault e o APIM mesmo com o Resource Group já apagado pelo Portal (guia de montagem, Limpeza de Recursos).

Desafio 3.6: o workbook é opcional; o alerta de heartbeat dispara entre 15 e 25 min depois do último heartbeat; o timer é desligado com `AzureWebJobs.Conciliacao.Disabled` (C maiúsculo, o nome da Function), e o deploy reaplica a lista inteira de app settings (um setting criado à mão some no próximo `deploy.ps1`).

Rate limit do guia 3.1: no APIM Consumption, a política `rate-limit` conta por subscription key ("on a per subscription basis") e "is only applied when an API is accessed using a subscription key"; a `rate-limit-by-key` não se aplica ao tier Consumption ([rate-limit](https://learn.microsoft.com/azure/api-management/rate-limit-policy), [rate-limit-by-key](https://learn.microsoft.com/azure/api-management/rate-limit-by-key-policy)). Cada empresa cliente tem a própria chave; nas notes, o limite aparece como "limite de 60 chamadas/min por chave de cada cliente", nunca por header.

### Validação cruzada com Speaker Notes
Notes cobrem: desafio 3.6, guias de leitura, limpeza ao vivo, os comandos do `limpar.ps1`, o `DefaultResourceGroup-CQ` de quem montou à mão e a pergunta Standard × Premium, que volta ao slide 6 (pesquisa de dúvidas, pergunta 6). **OK — alinhado.**

### Perguntas esperadas dos alunos
1. **Q:** O alerta por ausência de sinal custa? **A:** Alerta de pesquisa de log custa centavos por mês; no Free Trial cabe.

### Dica de demonstração
Apagar o `rg-saldaro-lab` do professor ao vivo, com a turma, e mostrar a tela de confirmação.

---

## Slide 29 — Encerramento e referências

### Verificação técnica
Todos os links abrem. O card do repositório diz "Link no canal da turma": o deck v19 não tem QR code. O `limpar.ps1` citado nas notes apaga o `rg-saldaro-lab`, purga o Key Vault e o APIM (os dois ficam em soft-delete com o nome reservado) e apaga o `scripts/.env.local` (`Get-Help ./scripts/limpar.ps1`). O Lab 0 pelo script levou de 8 a 10 min no primeiro deploy real.

### Validação cruzada com Speaker Notes
Notes cobrem: onde estão os links e o kit, conferência da limpeza, refazer um incidente durante a semana (e o roteiro completo de cada guia, que na aula rodou em ritmo de demonstração), as duas coisas para levar. **OK — alinhado.**

---

# Resumo da validação

## Fatos confirmados no Microsoft Learn (2026-10-13; links reabertos em 2026-10-14)

| # | Fato | Slides | Fonte |
|---|---|---|---|
| 1 | Resposta HTTP de Function limitada a 230 s em qualquer plano, independente do `functionTimeout` | 15 | [Functions scale and hosting](https://learn.microsoft.com/azure/azure-functions/functions-scale#function-app-timeout-duration) |
| 2 | `forward-request`: `timeout` padrão 300 s, acima de 240 s pode não ser honrado, erro `Timeout`; `follow-redirects` padrão `false` | 10, 15, 16 | [forward-request](https://learn.microsoft.com/azure/api-management/forward-request-policy), [error handling](https://learn.microsoft.com/azure/api-management/api-management-error-handling-policies) |
| 3 | Async Request-Reply: 202 + `Location` + `Retry-After`; 200 pendente; 303 See Other; `Idempotency-Key` | 16, 17 | [Asynchronous Request-Reply](https://learn.microsoft.com/azure/architecture/patterns/asynchronous-request-reply) |
| 4 | Durable: 202 com `statusQueryGetUri`; 202 enquanto roda, 200 no fim | 17 | [Durable HTTP features](https://learn.microsoft.com/azure/durable-task/durable-functions/durable-functions-http-features) |
| 5 | Sessions e duplicate detection só na criação; rótulos "Enable sessions" e "Enable duplicate detection" | 21 | [enable-message-sessions](https://learn.microsoft.com/azure/service-bus-messaging/enable-message-sessions), [enable-duplicate-detection](https://learn.microsoft.com/azure/service-bus-messaging/enable-duplicate-detection) |
| 6 | Duplicate detection: `MessageId`, janela 10 min (20 s a 7 dias), aceita e descarta, inclui agendadas | 21, 22, 25 | [duplicate-detection](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection) |
| 7 | Sessions: lock exclusivo, ordem na sessão; sequence number não ordena o processamento; reprocesso perde a ordem | 21, 25 | [message-sessions](https://learn.microsoft.com/azure/service-bus-messaging/message-sessions), [message-sequencing](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing) |
| 8 | DLQ: `MaxDeliveryCount` 10, motivos do sistema, sem TTL nem limpeza, `SubQueue.DeadLetter` | 24, 26 | [dead-letter queues](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues) |
| 9 | `ServiceBusMessageActions` + `AutoCompleteMessages = false` (extensão 5.14.1+); poison não configurável no Functions | 24, 25 | [Service Bus trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus-trigger) |
| 10 | `host.json`: `maxConcurrentCalls` 16 por núcleo (sem sessões), `maxConcurrentSessions` 8 (com sessões) | 19, 21 | [Service Bus bindings](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus) |
| 11 | Scheduled messages e `ServiceBusMessage.ScheduledEnqueueTime` | 25 | [sequencing](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing#scheduled-messages), [SDK](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusmessage.scheduledenqueuetime) |
| 12 | `FixedTimeEquals` em tempo constante; `HMACSHA256.HashData` desde o .NET 6 | 20 | [FixedTimeEquals](https://learn.microsoft.com/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals), [HashData](https://learn.microsoft.com/dotnet/api/system.security.cryptography.hmacsha256.hashdata) |
| 13 | Key Vault references: sintaxe, versão nova em até 24 h, `Key Vault Secrets User` | 7, 20 | [Key Vault references](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references) |
| 14 | `UpdateEntityAsync` com ETag falha com 412 | 5, 21 | [Azure.Data.Tables](https://learn.microsoft.com/dotnet/api/azure.data.tables.tableclient.updateentityasync) |
| 15 | Service Bus Explorer: peek da DLQ, Resend copia, não suporta sessions | 24, 26 | [explorer](https://learn.microsoft.com/azure/service-bus-messaging/explorer) |
| 16 | Handshake do Event Grid não autentica a entrega | 20 | [CloudEvents endpoint validation](https://learn.microsoft.com/azure/event-grid/end-point-validation-cloud-events-schema) |
| 17 | Application Insights: Search (antigo Transaction search) e End-to-end transaction details | 4, 13 | [failures, performance, transactions](https://learn.microsoft.com/azure/azure-monitor/app/failures-performance-transactions) |
| 18 | APIM Consumption: `rate-limit` por subscription key (só com chave); `rate-limit-by-key` fora do Consumption | 10, 16, 28 | [rate-limit](https://learn.microsoft.com/azure/api-management/rate-limit-policy), [rate-limit-by-key](https://learn.microsoft.com/azure/api-management/rate-limit-by-key-policy) |
| 19 | Os fatos dos slides 4 a 11 (Application Insights e Log Analytics, Table Storage, Service Bus Standard × Premium, Key Vault e identidade gerenciada, Event Grid com entrega em fila, Functions no Flex Consumption, APIM Consumption e o teto de 30 s, preços e franquias) | 4 a 11 | os quadros "Por que este recurso" do guia de montagem, com as fontes do Learn de cada um (consultadas em 2026-10-14; reabrir antes de publicar o deck, G9) |

## Itens do ensaio, conferidos no primeiro deploy real (2026-10-14)

Numa assinatura Sponsorship, em `brazilsouth` com Flex Consumption. O registro completo fica no checklist (`docs/research/2026-10-14-checklist-primeiro-deploy.md`).

1. **Slide 13**: conferido. O End-to-end transaction details do POST mostra APIM → hub → tabela → `PostCobranca` do banco no mesmo `operation_Id`.
2. **Slide 15**: conferido. O 504 vem aos ~30 s com `{"erro":"o gateway esperou 30 s e desistiu"}`; o roteamento `saldaro` × `saldaro/webhooks` funciona; o `PostFatura` dura ~45 s e aparece com 499.
3. **Slides 16, 20, 21 e 25**: conferido. Um toggle novo vale de 5 a 35 s depois do **Confirm** (no ensaio pelo Portal, de 7 a 19 s; no primeiro deploy, 16 a 18 s para gravar e 34 s até o `/saude` mostrar o valor).
4. **Slide 17**: em parte. A rajada de 1.000 faturas deu 202 em todas, sem 5xx, e drenou em ~2 min, mas numa Sponsorship. **Ainda a conferir:** a cota de 15 núcleos do Free Trial.
5. **Slide 19**: conferido. No baseline, `quantidadeBaixas = 3` por fatura; 18 de 20 terminam `notificada` e 2 regridem para `registrada`.
6. **Slide 21**: conferido. O print é do Overview → Essentials (Settings → Properties mudou); o `-SemPublicar` para em ~50 s; a mensagem do plano B avisa que a fila será recriada.
7. **Slide 22**: conferido. As chaves vêm com prefixo: `customDimensions["prop__SessionId"]`, `prop__SequenceNumber` e `prop__DeliveryCount`; a mensagem "Trigger Details" do host também traz o SessionId.
8. **Slide 24**: conferido. 204 abandonos em ~2 min; o Peek da DLQ com sessions funciona.
9. **Slides 25 e 26**: conferido. O `deploy.ps1 -Cenario 4 -Pronto -SemPublicar` leva ~3,6 min; a triagem mostra 52 na DLQ (32 antigas + 20 `SchemaInvalida`) e o `-Reprocessar` devolve 52 (40 corrigidas + 12 sem correção), zerando também o lote antigo.

**Novos na v1.4, conferidos no ensaio dos incidentes pelo Portal** (o primeiro deploy real usou o caminho por script):

10. **Slide 21**: o painel **+ Queue** com os campos do Passo 5.5 e o **(change)** de **Lock duration** e de **Max delivery count**, na seção **Settings** da Overview, logo abaixo de **Essentials**: conferidos (o guia do Incidente 2 não tem mais os marcadores `conferir no Portal`). O print do painel com as duas caixas marcadas não entrou no deck v19.
11. **Slide 25**: o **(change)** sob **Max delivery count**, na seção **Settings** da Overview: conferido; sem ele, `az servicebus queue update @fila --max-delivery-count 3`.

**Novos na v1.5, a conferir no ensaio:**

12. **Montagem ao vivo** (entre os slides 12 e 13): os 60 min, com a turma olhando e perguntando (a validação levou ~75 min para a montagem inteira, sem pausa). A primeira aula, gravada em 2026-10-14, montou ao vivo; o tempo real sai da gravação.
13. **Slides 14 a 26**: os incidentes em 30, 45 e 30 min, em ritmo de demonstração (0,6 do roteiro dos guias).

**Resolvidos na integração (2026-10-14):** a prova do Incidente 1 é por `referenciaErp` (o hub gera um `faturaId` por POST; nunca `quantidade = 2` num `faturaId`), com duas tentativas fixadas por `-MaxTentativas 2` e 2 faturas e 2 cobranças determinísticas (o baseline não desiste quando o ERP desiste); a corrida `cobranca-emitida` × `baixada` não acontece (webhooks só depois da resposta do banco e status monotônico na emissão); o reenvio com a mesma chave é o Passo 5.5 do guia; as colunas do CSV estão no `Get-Help` do `erp-simulado.ps1`; as 10 entregas em segundos vêm do abandon explícito; o `valor` em texto vale para os dois eventos; e a subscription key só existe no `cenario-1-pronto` (Atualização 1 do contrato), com a API `saldaro-webhooks` sem chave em todos os estados, então os comandos manuais dos cenários 2 a 4 não precisam do header `Ocp-Apim-Subscription-Key`.

## Estatísticas

- Slides com QA: 29/29, mais o bloco da montagem ao vivo; o apêndice de FAQ saiu do deck e do QA.
- Slides novos da v19 com QA (2 a 13): 12, conferidos contra o guia de montagem e os quadros "Por que este recurso"; nos slides 4 a 10, verificação técnica, validação cruzada, perguntas, pegadinhas e dica em todos.
- Respostas curtas no lugar dos ganchos de FAQ: 10, nas notes dos slides 16, 17, 19, 20, 21, 24, 25 e 28, cada uma com a pergunta da pesquisa `docs/research/2026-10-14-duvidas-das-tecnologias.md`; o slide 12 remete aos slides 10, 6 e 9.
- "Portal primeiro" e drift (G12 e G13), montagem (G14) e cuidados de telão (G15) conferidos nas notes; o deck repete o "Portal primeiro" na D9, e o que o `<aside>` resume em relação às notes está na D3.
- Slides de incidente com QA completo (verificação técnica, validação cruzada, perguntas, pegadinhas, dica): 10/10.
- Perguntas antecipadas: 36 nos slides de incidente e 26 nos slides 1 a 13.
- Pegadinhas de certificação: 31 nos slides de incidente e 14 nos slides 4 a 10.
- Itens do ensaio: 9 do primeiro deploy real (8 conferidos e 1 em parte, a cota do Flex numa Free Trial), 2 da v1.4, conferidos no ensaio pelo Portal, e 2 novos da v1.5 (a montagem em 60 min e os incidentes em ritmo de demonstração), a conferir.
