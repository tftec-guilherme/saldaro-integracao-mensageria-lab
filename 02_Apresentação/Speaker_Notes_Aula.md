# Speaker Notes — Aula hands-on: Integração e Mensageria no Azure
## Pós-Graduação em Arquitetura Cloud Azure | Prof. Guilherme Campos | 1 encontro ao vivo de ~4h

> **Version-anchor:** Q4-2026 · **Fonte de verdade dos slides:** `Conteudo_Slides_Aula.md` (22 slides de aula e 10 slides de bolso, FAQ-1 a FAQ-10). **Layout canônico:** `Disciplina 04 - Integração e Mensageria.pptx` (Blob da pós).
> **Modalidade:** 100% ao vivo. Cada seção traz **tempo previsto**, **o que mostrar** (Portal, VS Code, terminal) e **perguntas para a turma**, além de "O que falar", "Insights do mundo real", "Referências" e "Transição", no padrão da pós.
> **Status:** v1.4 (2026-10-14). Os 22 slides têm roteiro completo, e o apêndice "Perguntas que a turma vai fazer" tem as notes dos 10 slides de bolso, que ficam fora da soma das 4h; os ganchos "Se perguntarem: ... → FAQ-N" estão nas notes dos slides principais. Na v1.4, o conserto ao vivo de cada incidente passou para o Portal, e o `deploy.ps1` virou plano B (ADR 0008 e o parágrafo "Portal primeiro" dos guias dos incidentes): mudaram as notes dos slides 2, 4, 5, 9, 12, 13, 14, 15, 18 e 19. Nomes de Functions, settings, filas, comandos e botões de caos seguem o código, o Bicep e os scripts do kit; a sequência e o tempo de cada parte dos incidentes seguem o roteiro dos guias de `01_Aulas/` (conferidos na integração, ticket #13); os tempos medidos, as contagens e os rótulos do Portal seguem o primeiro deploy real do kit no Azure, em 2026-10-14 (roteiro em `docs/research/2026-10-14-checklist-primeiro-deploy.md`). Onde as notes dizem "no primeiro deploy", o número é dessa medição, e o da turma pode variar um pouco. Soma dos tempos: 223 min de slides + 10 de pausa + 5 de limpeza = 238 min; os slides de bolso do apêndice ficam fora dela. Na v1.3, a revisão de código alinhou as notes ao glossário do `CONTEXT.md` (toggle de cenário, confirmação repetida, reprocessamento), à ADR 0007 (simplificação de laboratório nos slides 6 e 12) e às cinco checagens da prova do Incidente 1.

---

## Slide 1 — Capa

**Tempo previsto:** 2 min · **Mostrar:** o slide; câmera aberta.

### O que falar
Bem-vindos. Hoje não é uma aula sobre serviços: é um plantão. Nas próximas quatro horas vocês vão trabalhar numa empresa fictícia chamada Saldaro Pagamentos, um hub de cobrança B2B que acabou de migrar para o Azure e que carrega um legado cheio de decisões ruins. Três incidentes vão estourar na nossa mão, um de cada vez, e a nossa missão é diagnosticar, consertar e provar que consertamos.

A diferença para a Disciplina 04 é de postura. Lá vocês aprenderam o que cada serviço faz e configuraram cada um deles no Portal. Aqui vocês já sabem o que é uma fila, um tópico, uma DLQ; o que falta é a cicatriz: ver o sistema quebrar de um jeito que acontece em produção e saber exatamente qual botão apertar.

Tudo o que vocês vão ver é ficção inspirada em experiência de campo em projetos de consultoria da TFTEC. Empresa, pessoas, números e incidentes são inventados; qualquer semelhança é coincidência. O que não é inventado são os padrões: 202 assíncrono, idempotência, sessões, dead-letter com motivo. Esses aparecem toda semana em sistemas reais.

### Insights do mundo real
- A maioria dos incidentes de integração em produção não é bug de código: é timeout mal pensado, retry sem idempotência ou fila sem tratamento de mensagem envenenada.
- Quem sabe diagnosticar um sistema distribuído às 3h da manhã vale mais no mercado do que quem sabe criar o recurso no Portal.

### Referências
- [Cloud Design Patterns](https://learn.microsoft.com/azure/architecture/patterns/)
- [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/)

### Transição
Antes de começar, as regras do dia. São poucas, mas importam.

---

## Slide 2 — Regras do dia

**Tempo previsto:** 4 min · **Mostrar:** a linha do tempo no slide; o terminal com `deploy.ps1 -?`.

### O que falar
Primeiro: vocês já fizeram o Lab 0 em casa, montando a Saldaro no Portal, peça por peça, ou pelo atalho do script. Quem não fez, não entra em pânico: o kit tem `./scripts/deploy.ps1 -Cenario 0 -Sufixo <seu sufixo>`, que cria tudo sozinho em uns 8 a 10 minutos, quase todos esperando o Azure, enquanto eu falo.

Segundo: o conserto de cada incidente, ao vivo, é no Portal, clique a clique: o app setting, a fila, o consumidor. É de propósito: a aula é para vocês saberem onde cada peça mora. O script fica como plano B, e a regra "ninguém fica para trás" vale o dia inteiro: cada incidente tem um estado pronto (`./scripts/deploy.ps1 -Cenario 2`, `3` ou `4` com `-Pronto`), que sobe em uns 4 a 7 minutos num ambiente que já existe; se alguém travar num passo, pula para o próximo com o estado pronto e volta depois com calma. Cada clique no Portal deixa o Azure diferente do Bicep, e a gente vai levar cada conserto para o arquivo de parâmetros. Guardem a palavra: drift.

E uma regra que o primeiro deploy real do kit ensinou: aqueçam antes de cada rajada, e sempre depois de um `deploy.ps1`. O APIM Consumption e o hub acordam devagar: no primeiro deploy, a primeira chamada levou uns 2 segundos, e as seguintes, uns 130 ms. Uma sonda que responde 404, como a do Passo 5.1 do Incidente 1 (`GET {SALDARO_APIM_URL}/faturas/aquecimento/status`), ou o próprio smoke test resolve. Sem isso, a primeira rajada de 10 faturas logo depois do deploy levou 107 segundos em vez de uns 5, com 6 chamadas estourando o timeout do cliente sem nem chegar ao APIM.

Terceiro: todo incidente segue o mesmo ritual. Sintomas, o que a gente observa quebrado; hipóteses, onde a gente olha para confirmar; conserto, a mudança; prova, o critério objetivo de que funcionou. Vocês vão escrever um post-mortem de cinco linhas por incidente, e o post-mortem não tem culpado.

Quarto: custo. Tudo o que vamos criar cabe no Free Trial e custa uns dois ou três reais, da véspera ao fim da aula: quase tudo é o Service Bus Standard, uns sete centavos por hora. O único jeito de gastar mais é esquecer o Resource Group ligado. Por isso às 3h55 todo mundo apaga o `rg-saldaro-lab` junto comigo, ao vivo.

**Pergunta para a turma:** quem já viu uma fila "esquecida" gerar conta no fim do mês? Deixa alguém contar em 30 segundos.

**Se perguntarem:** "Por que Bicep e não Terraform? E por que consertar no Portal, se o kit tem Bicep?" → FAQ-8.

**Se perguntarem:** "Por que Service Bus Standard? Um hub de pagamentos não usaria Premium?" → FAQ-3.

### Insights do mundo real
- Portal para aprender, IaC para recriar: o clique mostra onde a peça mora, e o arquivo de parâmetros garante que o próximo deploy confirme o conserto em vez de desfazê-lo.
- Estados prontos em IaC são a diferença entre uma aula ao vivo que flui e uma em que metade da turma fica depurando digitação.
- O ritual sintoma → hipótese → conserto → prova é o mesmo de um incidente real; treinar o ritual vale tanto quanto treinar a técnica.
- Depois de um deploy, a primeira chamada paga o cold start do gateway e do backend. Aquecer antes de medir é método, não maquiagem: o critério de prova vale para o sistema aquecido.

### Referências
- [Azure free account](https://azure.microsoft.com/free/)
- `docs/adr/0008-portal-primeiro-com-bicep-como-plano-b.md`: o Portal como caminho ao vivo e o `deploy.ps1` como plano B

### Transição
Agora, a empresa onde vocês trabalham hoje.

---

## Slide 3 — A Saldaro Pagamentos

**Tempo previsto:** 5 min · **Mostrar:** o diagrama do fio condutor; apontar os três pontos que vão quebrar.

### O que falar
A Saldaro é um hub de cobrança B2B. Trezentas empresas clientes, de distribuidoras a SaaS, usam a Saldaro para cobrar seus próprios clientes pessoa jurídica. O ERP da empresa cliente manda a fatura; a Saldaro registra uma cobrança no banco parceiro, Pix ou boleto; quando o pagador paga, o banco avisa por webhook; a Saldaro faz a baixa, concilia no fim do dia e notifica o ERP. São uns 400 mil por mês, com pico nos dias 5 a 10 e nos últimos dias úteis, quando algum ERP manda lote e chega a 30 faturas por segundo.

A "Saldaro 1.0" é o legado: um monólito que usava o banco relacional como fila e um único worker que fazia polling a cada poucos segundos. Funcionou enquanto o volume era pequeno. A "Saldaro 2.0" é o que vocês provisionaram no Lab 0: APIM na frente, Functions, Service Bus, Table Storage, Event Grid, Key Vault, App Insights. Só que a 2.0 nasceu herdando os hábitos da 1.0, e é isso que vai quebrar hoje.

Olhem o diagrama. Três pontos marcados: o `POST /faturas` síncrono, o webhook do banco e o worker da baixa. Cada incidente ataca um deles.

**Se perguntarem:** "Por que APIM e não WAF?" → FAQ-1.

**Se perguntarem:** "Por que Service Bus, e não Storage Queue, Event Hubs ou Event Grid?" → FAQ-2.

**Se perguntarem:** "Por que Functions? Integração não é coisa de Logic Apps?" → FAQ-7.

### Insights do mundo real
- "Banco como fila" é muito mais comum do que parece, e quase sempre vem com um worker único e um polling que vira gargalo no primeiro pico.
- Hubs de cobrança vivem de idempotência: a mesma confirmação bancária chega duas vezes com mais frequência do que se imagina.

### Referências
- `docs/research/2026-10-13-briefing-da-aula.md`, seções 1 e 2

### Transição
Vamos lembrar o que a Disciplina 04 já nos deu, em cinco minutos, para alinhar o vocabulário.

---

## Slide 4 — O que a Disciplina 04 já nos deu

**Tempo previsto:** 4 min · **Mostrar:** só o slide.

### O que falar
Vocês já dominam o vocabulário: fila, tópico, subscription com filtro SQL, dead-letter queue, `maxDeliveryCount`, peek-lock. Viram Durable Functions entregar 202 com `statusQueryGetUri` sem esforço. Viram Event Grid empurrar eventos e Event Hubs ingerir telemetria.

O que a Disciplina 04 não fez, por desenho, foi praticar o lado sombrio: um 202 escrito à mão para entender o padrão, idempotência de verdade com tabela inbox, sessões para garantir ordem, dead-letter com motivo explícito, Managed Identity no lugar de connection string e infraestrutura em Bicep. A lista do lado direito do slide é literalmente o roteiro de hoje.

Muda também a ferramenta. Na Disciplina 04 o código era Node.js e quase tudo nascia no clique, sem um arquivo que dissesse como recriar; hoje o código é C# em .NET 10 isolated, já pronto, e o ambiente que vocês montaram, no Portal ou pelo script, é exatamente o que o Bicep do kit declara. Ninguém vai escrever C# do zero nem publicar código hoje: vocês vão ler o código e ligar os toggles de cenário certos, que são app settings. E o Portal fica aberto o dia todo, para as duas coisas: é nele que vocês consertam, clique a clique (o toggle, a fila, o consumidor), e é nele que observam (Service Bus Explorer, Application Insights, as propriedades das filas). O `deploy.ps1` fica como plano B, para quem travar.

**Pergunta para a turma:** alguém lembra o que acontece com uma mensagem depois do décimo `Abandon`? É isso que vamos ver no incidente 3.

**Se perguntarem:** "Se o Durable dá o 202 de graça, por que fazer à mão?" → FAQ-9.

### Insights do mundo real
- Saber o vocabulário não evita incidente; evita é ter praticado a falha antes de ela acontecer em produção.
- Montar e consertar no Portal ensina onde cada peça mora; o Bicep ensina a recriar tudo igual. Um arquiteto precisa das duas coisas, e da disciplina de levar o clique para o código.

### Referências
- Disciplina 04, `Bloco4_ServiceBus.md` (repositório `azure-retail`)

### Transição
Chega de slide. Lab 0: vamos ver a Saldaro 2.0 funcionando no dia de sol.

---

## Slide 5 — Separador: Lab 0

**Tempo previsto:** 1 min · **Mostrar:** terminal pronto.

### O que falar
Lab 0, Saldaro 2.0 em 10 minutos. Quem montou em casa, no Portal ou pelo script, abre o terminal na pasta do kit: os dois caminhos deixam o mesmo ambiente. Quem não fez roda `./scripts/deploy.ps1 -Cenario 0 -Sufixo <seu sufixo>` agora: são uns 8 a 10 minutos, e ele termina rodando o smoke test sozinho. Enquanto isso, acompanhe na minha tela.

### Transição
Smoke test.

---

## Slide 5a — O que vamos montar

**Tempo previsto:** 3 min · **Mostrar:** o diagrama deste slide e, ao lado, o Portal aberto na assinatura, ainda sem o `rg-saldaro-lab`.

### O que falar
Antes de clicar em qualquer coisa, a planta da casa. A Saldaro 2.0 tem cinco papéis. A porta única é o APIM: o ERP da empresa cliente e o banco parceiro só falam com ele. O processamento são dois Function Apps no Flex Consumption: o hub, que é a Saldaro, e o banco simulado, que faz o papel do parceiro. A mensageria é o Service Bus, com três filas, e o Event Grid, que avisa o parceiro quando a fatura é baixada. O estado mora no Storage, em tabelas, e os segredos no Key Vault. E os olhos são o Application Insights, gravando num workspace do Log Analytics. Cada peça que a gente criar hoje responde a uma pergunta: o que quebra se ela não existir?

### Insights do mundo real
Quem já montou isso em projeto sabe: o desenho cabe num slide, mas a ordem de criação e as permissões entre as peças é que derrubam a primeira tentativa. É por isso que a montagem tem ordem.

### Referências
- Guia `01_Aulas/Lab0_Montagem_no_Portal_Guia.md`: diagrama e Tabela de Recursos.
- FAQ das tecnologias, perguntas 1, 4 e 10.

### Transição
Em que ordem a gente monta, e por quê.

---

## Slide 5b — A ordem da montagem e o porquê de cada peça

**Tempo previsto:** 3 a 5 min · **Mostrar:** o guia de montagem aberto na Etapa 2 e um dos quadros "Por que este recurso" aberto, para a turma ver o formato (objetivo, descrição, papel, por que este e não alternativas, configuração, custos, produção e fontes). Nos app settings da Etapa 8, com a tela projetada: **Show value** só na linha que interessa; nunca o **Show values** da barra nem o **Advanced edit**, que mostram de uma vez todas as chaves e connection strings.

### O que falar
Treze etapas, e a ordem não é estética: cada peça depende da anterior. Primeiro o Resource Group, porque tudo o que nasce junto morre junto, e a limpeza no fim é um clique. Depois os olhos, Log Analytics e Application Insights, porque a gente quer enxergar o sistema antes de quebrá-lo. Depois o estado, no Storage; a mensageria, no Service Bus; o segredo, no Key Vault; e o aviso ao parceiro, no Event Grid. Só então os dois Function Apps, que já nascem apontando para tudo isso. Por último, o APIM, a porta única. Uma dica de quem já montou: comece a criar o APIM cedo, porque ele leva alguns minutos para ativar; na validação do kit, ele ativou em uns 2 minutos. Em cada etapa, o guia tem um quadro "Por que este recurso", com o objetivo, o papel na Saldaro, as alternativas, cada opção do formulário e o custo. Eu vou montando aqui, e explicando o porquê de cada peça.

### Insights do mundo real
Na validação do kit, a montagem inteira pelo Portal levou cerca de 75 minutos sem pausa; um aluno, com leitura e cuidado, leva umas 3 horas. Em produção, isso nasce de código (o Bicep do kit faz o mesmo em uns 10 minutos), e o Portal fica para explorar.

### Referências
- Guia `01_Aulas/Lab0_Montagem_no_Portal_Guia.md`, Etapas 2 a 13.
- Se perguntarem por que APIM e não WAF → FAQ-1. Por que Service Bus e não Storage Queue → FAQ-2. Por que Functions e não Logic Apps → FAQ-7.

### Transição
Vamos montar. E, no fim, o smoke test prova que a casa está de pé.

---

## Slide 6 — Smoke test: o dia de sol

**Tempo previsto:** 12 min · **Mostrar:** terminal com `./scripts/smoke-test.ps1` (ele imprime o `faturaId` e o `operation_Id` do POST); `GET {SALDARO_APIM_URL}/faturas/{id}`; `appi-saldaro-<sufixo>` → Investigate → Search (antigo Transaction search) pelo `faturaId` entre aspas, e o End-to-end transaction details do POST (`PostFatura` chamando o `PostCobranca` do banco); a tabela de recursos no Portal.

### O que falar
Uma fatura, ponta a ponta. Rodem o smoke test e acompanhem: o `POST /faturas` chama o banco simulado e só responde quando o banco devolve a cobrança; alguns segundos depois o banco "paga" e manda o webhook; a confirmação entra na fila, o worker faz a baixa, e o Event Grid dispara a notificação ao parceiro, pela fila `sbq-notificacoes-parceiro`. Em menos de dois minutos, `GET /faturas/{id}` tem que responder `baixada` ou `notificada`, e o script termina verde.

**Se perguntarem** "notificação a qual parceiro?": aqui, "parceiro" é o termo guarda-chuva para banco parceiro e ERP, e a notificação ao parceiro vai sempre ao ERP da empresa cliente, pelo `callbackUrl` da fatura, nunca ao banco.

Abram o Application Insights, Search, o antigo Transaction search, e procurem pelo `faturaId` que o script imprimiu, entre aspas: aparecem os eventos da jornada inteira, de `fatura.recebida` a `fatura.baixada`. Cliquem no `fatura.recebida`: o End-to-end transaction details mostra o `PostFatura` chamando o banco simulado, porque a chamada HTTP síncrona leva o contexto de trace. Daí em diante a correlação técnica pode se perder: a fila de storage do banco, o webhook, o Service Bus e o Event Grid são fronteiras assíncronas, e o webhook, a baixa e a notificação podem chegar com outros `operation_Id`. Não é erro. É por isso que o `faturaId` vai em todos os eventos do hub: ele é a correlação de negócio, e é ele, não o `operation_Id`, que prova a jornada. Esse é o "dia de sol", e eu quero que vocês guardem esse print, porque é com ele que vamos comparar quando tudo quebrar.

Reparem no que é ingênuo de propósito no baseline: o endpoint é síncrono, a connection string é a `RootManageSharedAccessKey`, a fila de confirmações não tem sessions nem dup detection, não existe alerta nenhum. Nenhum desses itens é um bug; é o jeito que muita gente sobe o primeiro ambiente. Hoje a gente paga a conta de cada um.

Uma coisa fica fora dessa conta e vale para o dia todo: os HTTP triggers do kit são anônimos e o APIM não pede subscription, uma simplificação de laboratório registrada na ADR 0007; em produção, cada empresa cliente chamaria com a sua credencial no APIM, e o hub só aceitaria tráfego vindo dele.

**Pergunta para a turma:** olhando a tabela de recursos, qual deles vocês acham que vai nos trair primeiro?

**Se perguntarem:** "Por que APIM e não WAF?" → FAQ-1.

### Insights do mundo real
- Um smoke test ponta a ponta com correlação técnica (`operation_Id`) e de negócio (`faturaId`) é a primeira coisa que um time deveria ter antes de qualquer alerta sofisticado.

### Referências
- [Application Insights: Search (antigo Transaction search) e End-to-end transaction details](https://learn.microsoft.com/azure/azure-monitor/app/failures-performance-transactions)
- `docs/adr/0007-http-triggers-anonymous-e-apim-sem-subscription.md`, seção "O que mudaria em produção"

### Transição
Sexta-feira, 18h. O banco entrou em manutenção.

---

## Slide 7 — Separador: Incidente 1

**Tempo previsto:** 5 min (a história e o terminal pronto: Parte 1 do guia) · **Mostrar:** o separador; o terminal da turma com os Passos 1.1 e 1.2 do guia (`.env.local` carregado e `AsyncRequestReply` = `False` no `/saude`).

**Comandos:**

```powershell
$cfg = Get-Content ./scripts/.env.local -Raw | ConvertFrom-StringData
(Invoke-RestMethod "$($cfg.SALDARO_HUB_URL)/saude").toggles | Format-List
```

### O que falar
Incidente 1: "O timeout de sexta às 18h". A partir de agora vocês estão de plantão.

Sexta-feira, 18h02. O banco parceiro avisou, numa caixa de e-mail que ninguém monitorava, que entraria em janela de manutenção, e passou a responder em 40, 60, às vezes 90 segundos. O `POST /faturas` da Saldaro era síncrono: só respondia ao ERP depois que o banco devolvia a cobrança. Às 19h10, quando o banco voltou ao normal, umas 800 faturas dos ERPs tinham virado duas ou três faturas no hub, cada uma com a sua cobrança no banco, e o suporte passou a noite atendendo pagadores com dois boletos.

Enquanto eu conto, preparem o terminal, na raiz do repositório: carreguem o `scripts/.env.local` numa variável `$cfg` e perguntem ao hub, no endpoint `/saude`, em que estado ele está. O toggle `AsyncRequestReply` tem que aparecer `False`: é o baseline do Lab 0. Se aparecer `True`, alguém já consertou; o guia diz como voltar.

### Transição
Sintomas.

---

## Slide 8 — Sintomas (Incidente 1)

**Tempo previsto:** 15 min (sintomas e hipóteses: Partes 2 e 3 do guia) · **Mostrar:** terminal com `./scripts/erp-simulado.ps1 -Faturas 1 -MaxTentativas 2 -Caos "latencia=45"` e o relatório (1 fatura, 2 tentativas, `504|504` e nenhum `faturaId` devolvido); `stsaldaro<sufixo>` → Storage browser → Tables → `faturas` (duas linhas com a mesma `referenciaErp` e `faturaId` diferentes); `appi-saldaro-<sufixo>` → Logs, numa aba própria do navegador (toggle **Agent** desligado → **Discard**; **KQL mode**), com `kql/faturas-por-referencia-erp.kql` e a consulta de `requests` e `dependencies` (APIM com 504 aos ~30 s, `PostFatura` com ~45 s e `resultCode` 499, um `PostCobranca` por tentativa); `apim-saldaro-<sufixo>` → APIs → `saldaro` → Design → Backend (`<forward-request timeout="30" />` e o `on-error` que devolve o 504).

**Comandos:**

```powershell
./scripts/erp-simulado.ps1 -Faturas 1 -MaxTentativas 2 -Caos "latencia=45"
Get-Content ./kql/faturas-por-referencia-erp.kql -Raw | Set-Clipboard   # cole no Logs do appi-saldaro-<sufixo>
```

```kusto
union requests, dependencies
| where timestamp > ago(30m)
| where name contains "fatura" or name contains "cobranca"
| project timestamp, itemType, quem = cloud_RoleName, name, resultCode,
          segundos = round(duration / 1000.0, 1)
| order by timestamp asc
```

### O que falar
Vamos reproduzir a sexta-feira: o ERP simulado manda uma fatura só, e o botão de caos `latencia=45` faz o banco demorar 45 segundos para responder. O `-MaxTentativas 2` deixa a reprodução previsível: uma tentativa e um reenvio, cada um cortado aos 30 segundos, e o relatório sai em pouco mais de um minuto. Rodem junto comigo e não cancelem: o mais importante acontece enquanto vocês esperam.

Aos 30 segundos o terminal mostra um 504. Quem cortou não foi o Functions nem o banco: foi o APIM. A política da API tem `forward-request timeout="30"`, e um `on-error` do kit transforma esse corte num 504 sempre igual, com o corpo `{"erro":"o gateway esperou 30 s e desistiu"}`. O ERP recebe um erro de gateway e faz o que todo cliente bem-comportado faz: espera um pouco e reenvia. Só que o trabalho do outro lado não parou junto: o banco já tinha gravado a cobrança antes de começar a esperar, e o hub, ingênuo de propósito, não desiste quando o ERP desiste. O `PostFatura` segue esperando o banco e, aos 45 segundos, recebe a resposta do banco, grava a cobrança na fatura e tenta responder 201, só que ninguém mais está ouvindo. No Application Insights, a consulta de `requests` e `dependencies` mostra as linhas do APIM com 504 em uns 30 segundos, o `PostFatura` com uns 45 e `resultCode` 499, que quer dizer "o cliente fechou a conexão antes da resposta" (o cliente, aqui, é o APIM), e um `PostCobranca` no banco por tentativa. O APIM desistiu; o hub terminou o trabalho. Se as linhas ainda não apareceram, é a ingestão da telemetria, que leva de 2 a 5 minutos; voltamos nela no fim do slide.

Agora o estrago. O reenvio não carrega nada que diga "esta fatura já foi enviada", então o hub cria outra fatura, com outro `faturaId`, e o banco, que faz o que muita API de cobrança faz, registra outra cobrança antes de responder. No Storage browser, a tabela `faturas` tem duas linhas com a mesma `referenciaErp`, o número da fatura no ERP. Abram o Logs do Application Insights numa aba só dele do navegador e deixem aberta até o fim do incidente: se ele abrir num chat, o Observability Agent, desliguem o toggle Agent, cliquem em Discard e troquem Simple mode por KQL mode. No ensaio, sair do Logs com consulta não salva travou a aba. Rodem o `kql/faturas-por-referencia-erp.kql`: a `referenciaErp` da reprodução aparece com duas faturas e duas cobranças. Cada `faturaId`, sozinho, tem uma cobrança só no banco (`GET /cobrancas/{faturaId}` devolve `quantidade = 1`); a duplicata é de negócio, e por isso a contagem é por `referenciaErp`. O pagador vai receber dois boletos da mesma fatura.

Na Saldaro 1.0, a mesma sexta teve um segundo sintoma: o worker único de polling lia a tabela de cobranças a cada poucos segundos e não dava conta da fila que se formou no banco relacional. Guardem os dois sintomas, porque o conserto ataca os dois de uma vez.

E se a gente só aumentasse o timeout? Mostro a política no Portal: `apim-saldaro-<sufixo>`, APIs, Saldaro Hub - faturas (a API `saldaro`), Design, Backend. Subir os 30 segundos não conserta nada: o ERP desiste aos 35 de qualquer jeito, e o reenvio continua criando fatura e cobrança novas. E os 230 segundos? É o teto da plataforma: nenhuma resposta HTTP de Function passa disso, qualquer que seja o `functionTimeout`, por causa do idle timeout do balanceador do Azure. Vale saber, mas não foi o que derrubou a Saldaro. Na vida real quase sempre existe alguém na frente que corta antes: um gateway, um proxy, o próprio cliente. Ficar abaixo de 230 segundos não salva ninguém se o APIM corta aos 30.

**Pergunta para a turma:** o que o ERP deveria fazer quando não recebe resposta? E como ele saberia se o primeiro POST foi processado ou não?

### Insights do mundo real
- Timeout em cascata: o cliente, o gateway e o backend têm cada um o seu relógio, e quem tem o menor decide. Mapeie os três antes de culpar o parceiro.
- Registrar antes de responder é o comportamento certo para quem recebe a chamada; o erro é chamar uma operação que não é idempotente, sem chave de idempotência e com retry automático.
- Um 504 não diz que a operação falhou; diz que ninguém sabe. É esse "não sei" que transforma retry em duplicata.
- O 499 é a outra metade do 504: o gateway desistiu, mas o backend seguiu e terminou o trabalho. Olhe os dois lados da mesma chamada antes de concluir que "deu erro".

### Referências
- [Azure Functions: limite de 230 s para resposta HTTP](https://learn.microsoft.com/azure/azure-functions/functions-scale#function-app-timeout-duration)
- [API Management: política `forward-request` (atributo `timeout`)](https://learn.microsoft.com/azure/api-management/forward-request-policy)
- [API Management: erros predefinidos (`Timeout` no `forward-request`)](https://learn.microsoft.com/azure/api-management/api-management-error-handling-policies)

### Transição
Hipóteses confirmadas: o cliente não sabe se deu certo, e o banco não perdoa reenvio. Vamos ao padrão que resolve as duas coisas.

---

## Slide 9 — O padrão: request-reply assíncrono

**Tempo previsto:** 10 min (o conserto: Parte 4 do guia) · **Mostrar:** VS Code em `03_Aplicações/Saldaro.Hub` (`Functions/PostFatura.cs` e `Aplicacao/ServicoDeFaturas.cs` com os dois caminhos de `Cenario:AsyncRequestReply`, `GetFaturaStatus`, `EmitirCobranca`; procure `CONCEITO:`); Portal → `func-saldaro-hub-<sufixo>` → Settings → Environment variables → App settings → `Cenario__AsyncRequestReply` = `true` → Apply → Apply → Confirm (aviso **Save changes**; para ver um valor, **Show value** só na linha, nunca o **Show values** da barra); terminal com o laço do Passo 4.3 perguntando ao `/saude` até `AsyncRequestReply` = `True`; `infra/parameters/cenario-2-pronto.bicepparam` como a versão declarada do mesmo conserto.

### O que falar
A saída é parar de prometer uma resposta que depende do banco. O `POST /faturas` passa a fazer só o que ele controla: valida, grava a fatura como `recebida`, coloca uma mensagem na `sbq-faturas-recebidas` e responde 202 Accepted em milissegundos. Junto com o 202 vão dois headers: `Location`, apontando para `/faturas/{id}/status` pela URL pública do APIM, e `Retry-After: 5`, que diz ao cliente quando vale a pena perguntar de novo.

O endpoint de status é barato de propósito: um point read na tabela `faturas`. Enquanto a fatura não baixou, ele responde 200 com `{ faturaId, status }`. Quando ela chega a `baixada` ou `notificada`, responde 303 See Other para `/faturas/{id}`. Por que 303 e não 302? Porque o 303 manda o cliente fazer GET no recurso, seja qual for o método original; com 302, há cliente que repete o POST no redirecionamento, e voltamos à duplicata.

Terceira peça: `Idempotency-Key`. O ERP gera uma chave por fatura e reusa a mesma em todo reenvio. O hub faz um insert condicional na tabela `idempotencia`, com o cliente na partition key e a chave na row key: se a chave já existe, devolve o mesmo `faturaId` e não enfileira de novo. É isso que responde à pergunta do slide anterior: o cliente não consegue distinguir uma resposta perdida de um pedido que nunca chegou, então o servidor precisa tornar o reenvio inofensivo.

Por último, a fila. A `sbq-faturas-recebidas` absorve a rajada do dia 10, e o `EmitirCobranca` chama o banco no ritmo que ele aguenta: é o queue-based load leveling. Se o banco devolver 503, a Function lança exceção e o Service Bus reentrega; ninguém fica segurando conexão HTTP aberta. É também a resposta ao worker de polling do legado: quem drena agora é um trigger que escala com a fila.

Vamos ao código. No VS Code, o `PostFatura` tem os dois caminhos lado a lado, escolhidos por `Cenario:AsyncRequestReply`, e não há nada para escrever nem para publicar: o conserto é ligar o toggle de cenário. Confiram que o ERP simulado da reprodução terminou, abram o `func-saldaro-hub-<sufixo>` no Portal, Settings, Environment variables, e troquem `Cenario__AsyncRequestReply` para `true`: o Value vem mascarado, apaguem e digitem. Para conferir um valor, usem o Show value da linha, nunca o Show values da barra: ele mostra todos os segredos do app de uma vez, e na minha tela isso seria segredo projetado. Dois Apply e um Confirm, no aviso Save changes: mudar uma app setting reinicia o app, e o toggle passa a valer de 5 a 35 segundos depois do Confirm (no ensaio pelo Portal, 19; no primeiro deploy, 34). Enquanto ele volta, a gente lê o `GetFaturaStatus` e o `EmitirCobranca` juntos, e o laço do guia pergunta ao `/saude` até o toggle aparecer `True`. Reparem que isso é drift: o Portal agora diverge do Bicep do Lab 0. A versão declarada do mesmo conserto é o `cenario-2-pronto.bicepparam`, e quem travar sobe direto esse estado com `./scripts/deploy.ps1 -Cenario 2 -Pronto`, o plano B: uns 6 a 7 minutos, porque ele também republica os apps, contra uns 2 pelo Portal.

**Pergunta para a turma:** se o ERP fizer polling a cada segundo em vez de respeitar o `Retry-After`, o que acontece com o APIM no dia 10? Que mecanismo vocês usariam para se defender?

**Se perguntarem:** "Se a fila tem duplicate detection, para que a `Idempotency-Key`?" → FAQ-5.

**Se perguntarem:** "Por que mudar no Portal, se o kit tem Bicep?" → FAQ-8.

### Insights do mundo real
- 202 sem `Idempotency-Key` só muda o lugar da duplicata: o reenvio do ERP vira duas mensagens na fila em vez de duas chamadas ao banco.
- O endpoint de status vira o mais chamado do sistema; ele precisa ser um point read, nunca uma consulta ao banco parceiro.
- A chave de idempotência tem prazo: guarde-a pelo menos pelo tempo máximo de retry do cliente e documente esse prazo no contrato da API.
- Tirar o ERP do caminho não tira o banco: o `EmitirCobranca` ainda chama o parceiro com entrega at-least-once. Em produção, mande o `faturaId` como chave de idempotência também para o banco.
- Toggle de cenário por app setting é configuração viva: mudar reinicia o app e encerra execuções em andamento, e o que mudou no Portal é drift até o Bicep dizer o mesmo.

### Referências
- [Asynchronous Request-Reply pattern](https://learn.microsoft.com/azure/architecture/patterns/asynchronous-request-reply)
- [Queue-Based Load Leveling pattern](https://learn.microsoft.com/azure/architecture/patterns/queue-based-load-leveling)
- [Request-Reply (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReply.html)
- [Correlation Identifier (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CorrelationIdentifier.html)

### Transição
Toggle ligado. Agora a prova: o mesmo banco lento, e o ERP sem perceber.

---

## Slide 10 — Prova (Incidente 1)

**Tempo previsto:** 20 min (a prova, o post-mortem de 3 min e as lições de 2 min: Partes 5 a 7 do guia) · **Mostrar:** terminal com o POST do Passo 5.1 do guia (banco a 60 s, cronômetro no cliente) e o polling do status com `curl.exe` (Passo 5.2); `./scripts/erp-simulado.ps1 -Faturas 1000 -Paralelismo 20 -IdempotencyKey` e o relatório; Portal com o Overview da `sbq-faturas-recebidas` enchendo e esvaziando (**Message Counts**; clique em **Refresh**, porque o Overview não se atualiza sozinho); `appi-saldaro-<sufixo>` → Logs (a aba do slide 8, em **KQL mode**) com `kql/faturas-por-referencia-erp.kql` depois que a fila drenar; o reenvio com a mesma `Idempotency-Key` (Passo 5.5).

**Comandos:**

```powershell
# Passo 5.1 do guia: POST manual com latencia=60 e Idempotency-Key, medido com Stopwatch (define $corpo, $cabecalhos, $statusUrl e $faturaId)
# Passo 5.2: o status sem seguir o redirect (o curl.exe não segue sem -L)
curl.exe -s -w " | HTTP %{http_code} %{redirect_url}" $statusUrl
./scripts/erp-simulado.ps1 -Faturas 1000 -Paralelismo 20 -IdempotencyKey
Get-Content ./kql/faturas-por-referencia-erp.kql -Raw | Set-Clipboard   # Passo 5.4, depois que a fila drenar
# Passo 5.5: o mesmo corpo com a mesma chave devolve o mesmo faturaId
(Invoke-RestMethod -Method Post -Uri "$($cfg.SALDARO_APIM_URL)/faturas" -ContentType 'application/json' -Body $corpo -Headers $cabecalhos).faturaId
```

### O que falar
Cinco checagens, os Passos 5.1 a 5.5 do guia, uma de cada vez, e cada um de vocês confere no próprio ambiente.

Primeira: o banco continua lento, agora com `latencia=60`, e o POST responde 202 em menos de meio segundo com o app aquecido, medido no cronômetro do próprio cliente, com `Location` e `Retry-After: 5`. A primeira chamada depois do reinício vem fria: no primeiro deploy, levou 2,2 segundos, com o hub escalando do zero e abrindo a conexão com o Service Bus. Rodem o POST de novo: aquecido, ficou entre uns 90 e 450 ms. O critério vale para o sistema aquecido. O banco vai demorar um minuto, mas isso virou problema do `EmitirCobranca`, não do ERP.

Segunda: consultem o status com o `curl.exe`, que não segue redirecionamento: 200 com o estado atual enquanto a fatura anda e, quando ela baixa, 303 com o `Location` do recurso. O 303 chega uns 75 a 90 segundos depois do POST, e não aos 60: depois de responder, o banco ainda espera uns 5 segundos para "pagar", o webhook atravessa a fila do banco e a do hub, e o polling só pergunta de 5 em 5 segundos. É a resposta à pergunta do guia.

Terceira: a rajada. Mil faturas com paralelismo 20 e `-IdempotencyKey`, uma chave por fatura, reusada se algum POST precisar de reenvio. O relatório tem que mostrar mil 202 e zero 5xx. No primeiro deploy, foram mil 202, zero 5xx e zero 429 em 47 segundos, com p50 de 220 ms, p95 de 639 ms e o pior POST em 8,4 segundos. Deixem o Overview da `sbq-faturas-recebidas` aberto no Portal enquanto ela enche e esvazia: esse morro no gráfico de mensagens ativas é o load leveling acontecendo na frente de vocês. As filas drenaram em uns 2 minutos desde o início da rajada, sem nada na DLQ e sem esbarrar na cota do Flex.

Quarta: nenhuma duplicata de negócio. Quando a fila drenar, rodem de novo o `kql/faturas-por-referencia-erp.kql`, com o período restrito aos minutos da rajada: vazio. A consulta só lista `referenciaErp` com mais de uma fatura ou cobrança; sem a linha `where faturas > 1 or cobrancas > 1`, cada referência da rajada aparece com uma e uma. A da reprodução, se entrar no período, continua com duas: é o histórico do incidente. A checagem complementar do guia vai direto ao banco, com os `faturaId` do CSV em `scripts/saida/`: `quantidade = 1` nos mil.

Quinta: reenviem o mesmo POST com a mesma `Idempotency-Key`, seguindo o passo do guia, e comparem. Volta o mesmo `faturaId`, nada novo entra na fila e o banco continua com uma cobrança para ele.

Para fechar, lembrem do Durable da Disciplina 04: o `statusQueryGetUri` é este mesmo padrão pronto, com uma diferença de dialeto. Ele devolve 202 enquanto a orquestração roda e 200 quando acaba, em vez do nosso 200 com status e 303. As duas formas são válidas; o que importa é o cliente saber qual contrato está seguindo.

Três minutos de post-mortem, cinco linhas: o que aconteceu, impacto, causa raiz, o que consertamos, o que impede de repetir. Dica para a causa raiz: não escrevam "o banco ficou lento". O banco vai ficar lento de novo; a pergunta é por que isso virou cobrança duplicada. Fecho com as lições do guia: não segurar a conexão esperando um parceiro lento, a chave de idempotência é do cliente, e idempotência é de ponta a ponta, porque o `EmitirCobranca` também entrega pelo menos uma vez.

**Pergunta para a turma:** qual das cinco checagens vocês colocariam num alerta de produção, e com que limite?

**Se perguntarem:** "Se o Durable dá o 202 de graça, por que fazer à mão?" → FAQ-9.

### Insights do mundo real
- Critério de prova bom é numérico e reproduzível: "202 em menos de 500 ms com o banco a 60 s" vale mais que "ficou mais rápido".
- Meça percentis, não a média nem a melhor chamada: na rajada do primeiro deploy, p50 de 220 ms, p95 de 639 ms e máximo de 8,4 s. A chamada isolada e o p95 da rajada contam histórias diferentes, e as duas importam.
- Rajada sem 5xx no APIM não prova que o backend aguentou; prova que o desacoplamento funcionou. A fila drenando até zero e uma fatura por `referenciaErp` são a outra metade da prova.
- Escolha um dialeto do padrão (o do Durable ou o 200/303) e documente no contrato da API: ERP de parceiro segue o que está escrito, não o que você imaginou.

### Referências
- [Durable Functions: async operation tracking (`statusQueryGetUri`)](https://learn.microsoft.com/azure/durable-task/durable-functions/durable-functions-http-features)
- [Asynchronous Request-Reply pattern](https://learn.microsoft.com/azure/architecture/patterns/asynchronous-request-reply)

### Transição
Pausa de 10 minutos. Na volta, dia 10, 9h30.

---

## Slide 11 — Separador: Incidente 2

**Tempo previsto:** 1 min.

### O que falar
Incidente 2: "O webhook que chegou duas vezes, e antes da hora". É o mais longo do dia e o mais importante.

### Transição
Sintomas.

---

## Slide 12 — Sintomas (Incidente 2)

**Tempo previsto:** 18 min (a história, os sintomas e as hipóteses: Partes 1 a 3 do guia) · **Mostrar:** terminal com `./scripts/erp-simulado.ps1 -Faturas 20 -Caos "duplicar=3;embaralhar=true"`; `appi-saldaro-<sufixo>` → Logs (aba própria, toggle **Agent** desligado, **KQL mode**) com `kql/baixa-duplicada.kql` filtrado pelo início da execução (e a fatura de estudo tirada dali); `GET {SALDARO_APIM_URL}/faturas/{id}` (status e `quantidadeBaixas`); `stsaldaro<sufixo>` → Storage browser → Tables → `faturas` filtrada por `status` = `registrada` (as desbaixadas: 2 das 20 no primeiro deploy); o POST forjado sem assinatura em `{SALDARO_APIM_URL}/webhooks/banco` respondendo 202; as consultas de hipóteses do guia (cópias por `idEvento`, linha do tempo da fatura, intervalo entre as baixas) e o `host.json` do hub.

**Comandos:**

```powershell
./scripts/erp-simulado.ps1 -Faturas 20 -Caos "duplicar=3;embaralhar=true"
$cfg = Get-Content ./scripts/.env.local -Raw | ConvertFrom-StringData
$faturaId = '<faturaId de estudo, tirado do baixa-duplicada.kql>'
Invoke-RestMethod "$($cfg.SALDARO_APIM_URL)/faturas/$faturaId" | Format-List
$forjado = [ordered]@{ idEvento = [guid]::NewGuid().ToString(); tipo = 'cobranca.paga'; faturaId = $faturaId; idCobranca = 'COB-00000000'; valor = 1.00; ocorridoEm = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'); simulaTransiente = $false; transienteAte = $null } | ConvertTo-Json
Invoke-WebRequest -Method Post -Uri "$($cfg.SALDARO_APIM_URL)/webhooks/banco" -ContentType 'application/json' -Body $forjado -SkipHttpErrorCheck | Select-Object StatusCode
```

### O que falar
Dia 10, 9h30, pico de vencimentos. O banco teve um timeout do lado dele e, quando voltou, repetiu de uma vez 1.200 confirmações: parte chegou em várias cópias, parte fora de ordem. E, no meio da confusão, alguém mandou uma confirmação que o banco nunca enviou.

O ambiente de vocês já está no ponto de partida: é o estado pronto do Incidente 1. Quem tem só uma diferença pequena, como o toggle do Incidente 1 desligado, corrige no Portal, pelo Passo 0.3 do guia. Quem chegou agora roda `./scripts/deploy.ps1 -Cenario 3 -Sufixo <seu sufixo>`, de preferência na pausa: são uns 6 minutos num ambiente que já existe e 8 a 10 num novo, e depois o aquecimento. Vinte faturas com `duplicar=3;embaralhar=true`: o banco manda cada evento três vezes, sempre com o mesmo `idEvento`, e inverte a ordem, com o "paga" em 1 segundo e o "registrada" uns 8 segundos depois. Os 1.200 webhooks da história viram 120 aqui: 20 faturas, 2 eventos, 3 cópias. O ERP fez tudo certo, mandou cada fatura uma vez e recebeu 202; o estrago acontece entre o banco e o hub.

Esperem um minuto pela telemetria e rodem o `baixa-duplicada.kql` com o filtro do início da execução: perto de 20 linhas, cada uma com três `fatura.baixada`. Peguem um `faturaId` dali; é a fatura de estudo. Consultem pela API: o mais provável é `notificada`, com `quantidadeBaixas` maior que 1, em geral 3. Um pagamento, três baixas e três avisos ao ERP. E a desordem? Ela não aparece em toda fatura, porque o status final depende de quem grava por último: o "registrada" atrasado ou a notificação, que no baseline marca `notificada` por cima de qualquer status. No primeiro deploy, 2 das 20 terminaram `registrada`, com `baixadaEm` preenchido: pagas, notificadas e depois "desbaixadas" pelo evento que chegou atrasado. Achem as de vocês no Storage browser, filtrando a tabela `faturas` por `status`. Olhem também o `quantidadeBaixas`. Se ele mostrar menos baixas do que o KQL mostrou, melhor ainda: vocês acabaram de ver um lost update, duas execuções lendo o mesmo valor e gravando uma por cima da outra. No primeiro deploy apareceu um: três `fatura.baixada` no KQL e `quantidadeBaixas = 2` na tabela.

Por que em paralelo? O `host.json` do hub tem `maxConcurrentCalls = 4`: uma instância processa até quatro mensagens ao mesmo tempo, e as três cópias do "paga" correm juntas. É o mesmo efeito de dois consumidores disputando a fila, sem precisar escalar nada.

Agora o pior. Mandem um POST forjado para a fatura de estudo, sem assinatura nenhuma, direto em `/saldaro/webhooks/banco`, dizendo que alguém pagou um real. Resposta: 202. Uns 25 segundos depois, o `quantidadeBaixas` subiu mais um, de 3 para 4 no primeiro deploy, e o ERP do cliente recebeu mais um aviso de pagamento. O hub aceitou um pagamento que o banco nunca confirmou. Na Saldaro 1.0, a "assinatura" era um segredo fixo num header; na 2.0, nem isso. E nada barrou o remetente antes do hub: no kit, o webhook é `Anonymous` e a API `saldaro-webhooks` do APIM não pede subscription, uma simplificação de laboratório da ADR 0007; em produção, o APIM só deixaria passar o banco, por IP ou por certificado, e no próximo slide eu volto nisso.

Antes de consertar, as hipóteses, com as consultas do guia: o ERP não duplicou nada, porque o banco tem uma cobrança por fatura; cada `idEvento` chegou três vezes; o "registrada" chegou por último e desfez a baixa (onde a notificação gravou antes dele, a fatura ficou `registrada`); as baixas da mesma fatura caíram a milissegundos uma da outra; e o webhook aceita qualquer remetente. Cada linha do `baixa-duplicada.kql` é uma notificação a mais no ERP de um cliente.

**Pergunta para a turma:** desses três sintomas (duplicata, desordem e forjada), qual vocês resolveriam primeiro? E qual dá para resolver só com configuração, sem código?

**Se perguntarem:** "Se a fila é FIFO, por que a fatura desbaixou?" → FAQ-4.

### Insights do mundo real
- Webhook de parceiro é at-least-once por definição: quando não recebe a resposta a tempo, o parceiro repete a confirmação, e ele nunca sabe se você processou.
- Ordem de chegada não é ordem de negócio. Um evento "registrada" atrasado é tão real quanto um duplicado, e o código precisa decidir o que fazer com ele.
- Lost update é silencioso: o contador fica certo na maioria das vezes, e o erro só aparece na conciliação, dias depois.
- Corrida que só aparece às vezes é a mais cara: 2 faturas desbaixadas em 20 parecem ruído até a conciliação provar que é defeito.

### Referências
- [Idempotent Receiver (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html)
- [Competing Consumers pattern](https://learn.microsoft.com/azure/architecture/patterns/competing-consumers)
- [Service Bus no Functions: `maxConcurrentCalls` e `maxConcurrentSessions` no `host.json`](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus)

### Transição
Primeira camada: a assinatura.

---

## Slide 13 — Assinatura que vale: HMAC do corpo

**Tempo previsto:** 12 min (a camada da assinatura: Parte 4 do guia) · **Mostrar:** VS Code com a validação de assinatura do `Saldaro.Hub` (`Dominio/AssinaturaWebhook.cs`, lógica pura, chamada pelo `Functions/WebhookBanco.cs` antes de qualquer `JsonDocument`); Portal → `func-saldaro-hub-<sufixo>` → Settings → Environment variables → `Cenario__WebhookHmac = true` (**Show value** só na linha, nunca o **Show values** da barra); terminal repetindo o POST forjado (agora 401) e `./scripts/smoke-test.ps1` (o webhook assinado do banco continua passando).

### O que falar
Segredo fixo num header não é assinatura. Ele prova só que quem mandou conhecia o segredo em algum momento: o segredo aparece em log, em proxy, em print de suporte, e não protege o corpo. Quem tem o header copia, troca o valor ou o `faturaId` e manda de novo quantas vezes quiser.

A assinatura de verdade amarra o segredo ao conteúdo e ao momento. O banco calcula um HMAC-SHA256 da string `"{timestamp}.{corpo}"`, com o corpo exatamente como foi enviado, e manda dois headers: `X-Saldaro-Timestamp`, em segundos Unix, e `X-Saldaro-Signature: sha256=` seguido do hex. O hub recalcula com o mesmo `WebhookSecret` e compara. Mudou um byte do corpo, a assinatura não bate; repetiu a mesma requisição meia hora depois, o timestamp está fora da janela de 300 segundos.

Três detalhes derrubam implementações reais. Um: validar antes de desserializar. Se o código faz parse do JSON e serializa de novo para assinar, a ordem dos campos ou um espaço muda e nenhuma assinatura confere; por isso o `WebhookBanco` lê o corpo bruto. Dois: comparar com `CryptographicOperations.FixedTimeEquals`, que leva o mesmo tempo qualquer que seja o conteúdo. Um `==` comum para no primeiro byte diferente e vaza, pelo tempo de resposta, quantos bytes o atacante já acertou. Três: assinatura inválida é 401 e nada entra na fila. Não existe "aceita e marca como suspeita".

Vamos ver funcionando. No Portal, abram o `func-saldaro-hub-<sufixo>`, Settings, Environment variables, e mudem `Cenario__WebhookHmac` para `true`. É o caminho da aula, e é drift: no próximo slide vocês levam a mesma linha para o arquivo de parâmetros do Bicep, para o arquivo dizer o que está no ar; sem ela, o próximo `deploy.ps1` deste estado desligaria o HMAC em silêncio. Esperem o app reiniciar, de 5 a 35 segundos (no ensaio, 13), confiram no `/saude` e mandem o POST forjado de novo: 401, e a fatura de estudo não muda. Agora rodem o smoke test: o webhook do banco, que sempre assinou, continua passando.

E onde mora o segredo? Hoje, numa app setting em texto. No guia de leitura 3.5 ele vai para o Key Vault como referência, e a rotação deixa de exigir redeploy.

E quem pode chamar o webhook? No kit, qualquer um: a Function é `Anonymous` e a API `saldaro-webhooks` do APIM não pede subscription. É simplificação de laboratório, registrada na ADR 0007, e por isso aqui a assinatura é a única defesa. Em produção, ela continua e ganha companhia: o APIM só aceita os IPs do banco (`ip-filter`) ou exige o certificado dele (mTLS), conforme o contrato com o parceiro, e o backend recusa quem não vier pelo APIM.

**Pergunta para a turma:** por que o timestamp entra no conteúdo assinado, e não só num header separado?

**Se perguntarem:** "Por que HMAC no webhook, e não mTLS, OAuth ou lista de IPs?" → FAQ-10.

### Insights do mundo real
- Rotação de segredo de webhook sem janela dupla derruba o parceiro: durante a troca, aceite o segredo atual e o anterior, e aposente o antigo com data marcada.
- Com Key Vault reference sem versão, o app passa a usar a versão nova em até 24 horas, ou na hora, se a configuração do app mudar. Planeje a rotação com isso em mente.
- Logar o corpo do webhook para "depurar a assinatura" costuma vazar dados do pagador. Logue `idEvento`, timestamp e o resultado da validação.

### Referências
- [`HMACSHA256` (.NET)](https://learn.microsoft.com/dotnet/api/system.security.cryptography.hmacsha256)
- [`CryptographicOperations.FixedTimeEquals`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals)
- [Key Vault references em App Service e Functions](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)
- [Securing Azure Functions: proteção de endpoints HTTP](https://learn.microsoft.com/azure/azure-functions/security-concepts)
- `docs/adr/0007-http-triggers-anonymous-e-apim-sem-subscription.md`, seção "O que mudaria em produção"

### Transição
Segunda e terceira camadas: o broker e o consumidor.

---

## Slide 14 — Uma vez só e na ordem certa

**Tempo previsto:** 30 min (as camadas do broker e do consumidor: Partes 5 e 6 do guia) · **Mostrar:** Portal → `sb-saldaro-<sufixo>` → Entities → Queues → `sbq-confirmacoes-banco` → Overview → bloco Essentials (Sessions e Duplicate detection em "Disabled", sem como ligar; o Settings → Properties virou uma página genérica de metadados do ARM) e o erro do `az servicebus queue update --enable-session true` (`SubCode=40000`); os contadores da fila em zero (Overview → **Message Counts**) e o Delete no Overview da fila (**Delete queue** → **Yes**); Queues → **+ Queue**, o painel de criação com o nome `sbq-confirmacoes-banco`, **Max delivery count** `10`, **Message time to live** no máximo (`10675199` dias, `2` h, `48` min, `5` s; o painel vem com 14 dias), **Lock duration** 1 minuto, **Enable dead lettering on message expiration** marcado, **Enable partitioning** desmarcado e as duas caixas do dia, **Enable duplicate detection** (janela de 10 min) e **Enable sessions**, marcadas (Passo 5.5 do guia); o Overview da fila nova, com Sessions e Duplicate detection fora de "Disabled"; VS Code com `infra/parameters/cenario-2-pronto.bicepparam` (as três linhas do Passo 5.4) ao lado do `cenario-3-pronto.bicepparam`, `infra/modules/service-bus.bicep` (`requiresSession`, `requiresDuplicateDetection`), os triggers `ProcessarConfirmacaoSemSessao` e `ProcessarConfirmacaoComSessao` (`IsSessionsEnabled = true`) e o `host.json`; terminal com o `az servicebus queue show` do Passo 5.6 do guia (`True`, `True`, `PT10M`, `10` e `PT1M`); o erro `non-sessionful` no Logs, depois de mandar uma fatura (com a fila vazia, a consulta volta vazia) e com a fila em **Active** 2; Portal → `func-saldaro-hub-<sufixo>` → Environment variables para trocar o consumidor (os dois app settings `AzureWebJobs.ProcessarConfirmacao*.Disabled`; a grade corta os nomes, confira o **Name** no painel) e ligar `Cenario__Inbox` (**Show value** só na linha, nunca o **Show values** da barra). Plano B, só para quem travar: `./scripts/deploy.ps1 -Cenario 3 -Sufixo <sufixo> -SemPublicar` (o Bicep recria a fila com o `.bicepparam` do Passo 5.4) ou `-Cenario 3 -Pronto`.

**Comandos:**

```powershell
# Passo 5.1 do guia ($rg e $sb vêm do Passo 0.1): a API recusa ligar sessions numa fila existente (SubCode=40000)
az servicebus queue update -g $rg --namespace-name $sb -n sbq-confirmacoes-banco --enable-session true
# Passo 5.2: drenar antes de apagar (activeMessageCount e deadLetterMessageCount em 0)
az servicebus queue show -g $rg --namespace-name $sb -n sbq-confirmacoes-banco --query countDetails -o jsonc
# Passo 5.6: a fila recriada no Portal (+ Queue), conferida pela API
az servicebus queue show -g $rg --namespace-name $sb -n sbq-confirmacoes-banco `
    --query "{sessions:requiresSession, dupDetection:requiresDuplicateDetection, janela:duplicateDetectionHistoryTimeWindow, maxDelivery:maxDeliveryCount, lock:lockDuration}" -o table
# Plano B, só para quem travar: o Bicep recria a fila com o .bicepparam do Passo 5.4 (um aviso amarelo de consumidor sem sessão é esperado)
./scripts/deploy.ps1 -Cenario 3 -Sufixo $cfg.SALDARO_SUFIXO -SemPublicar
```

### O que falar
Com a assinatura, sobrou a confirmação repetida: o banco mandando de novo um evento que ele mesmo assinou. Para isso são três camadas, e cada uma pega o que a anterior deixa passar.

Primeira, no broker. O `WebhookBanco` já envia a confirmação com `MessageId = idEvento`. Com duplicate detection ligada, o Service Bus guarda os `MessageId` dos últimos 10 minutos; um envio repetido é aceito, o produtor recebe sucesso, e a cópia é descartada na hora. Reparem que a janela de 10 minutos é maior que a tolerância de 5 minutos do timestamp: uma requisição capturada e repetida cai no 401, e uma confirmação repetida pelo próprio banco dentro da janela cai no broker.

Segunda, a sessão. Com `SessionId = faturaId`, quem pega uma sessão tem um lock exclusivo sobre todas as mensagens daquela fatura. O worker continua com concorrência alta, até oito sessões ao mesmo tempo, mas cada fatura é processada por um consumidor de cada vez, na ordem de chegada. Cuidado com o que isso promete: ordem de chegada não é ordem de negócio. O "paga" que chegou antes do "registrada" continua chegando antes.

Terceira, o consumidor. A tabela `inbox` guarda `faturaId` e `idEvento`: se o evento já foi processado, a mensagem é completada sem efeito. Ela pega o que o broker deixa passar, como uma confirmação repetida depois da janela ou a reentrega de uma mensagem cuja Function caiu entre gravar a baixa e completar. E a máquina de estados fecha a conta: `cobranca.registrada` depois de `baixada` é ignorada, e a gravação usa ETag, então quem perde a corrida relê e reavalia em vez de gravar por cima.

Agora tentem ligar sessions na fila que existe. No Portal, abram o Overview da `sbq-confirmacoes-banco`: no bloco Essentials, Sessions e Duplicate detection aparecem como "Disabled", e não há onde ligar. Não procurem em Settings → Properties: essa página virou uma lista genérica, gerada dos metadados do ARM, com Id, Name, Type, Tags e Location. Pelo CLI, a API responde com todas as letras: `The value for the RequiresSession property of an existing Queue cannot be changed.` Sessions e duplicate detection só se definem na criação da fila. O caminho do lab, todo no Portal: drenar, conferindo que as ativas e a DLQ estão em zero, na seção Message Counts do Overview, e apagar a fila, no Delete do Overview, com Yes no aviso Delete queue. Antes de criar de novo, abram o `cenario-2-pronto.bicepparam`, que é o arquivo do estado em que vocês estão, e deixem três linhas em `true`: `webhookHmac`, o que vocês ligaram no Portal no slide anterior, `filaComSessions` e `filaDupDetection`. É esse arquivo que registra o que vocês vão criar à mão: sem essas linhas, o próximo deploy pararia na propriedade imutável ou desligaria o HMAC em silêncio.

Agora a fila nova: Queues, + Queue. O nome é exatamente `sbq-confirmacoes-banco`, e os valores são os que o Bicep declara: Max delivery count 10, Message time to live no máximo, 10675199 dias, 2 horas, 48 minutos e 5 segundos, porque o painel sugere 14 dias e o Bicep deixa ilimitado, Lock duration 1 minuto, Enable dead lettering on message expiration marcado, Enable partitioning desmarcado, porque ele também só se escolhe na criação, e as duas caixas do dia, Enable duplicate detection, com a janela de 10 minutos, e Enable sessions. Enquanto a turma preenche, eu explico cada caixa: são justamente as duas que vocês não conseguiram mudar há dois minutos. Esqueceu uma? Não tem volta: apaga e cria de novo, uns dois minutos a mais. Confiram com o `az servicebus queue show` do Passo 5.6: `True`, `True`, `PT10M`, `10` e `PT1M`; se o lock ou o max delivery vierem com o padrão do painel, acertem no (change) da seção Settings, logo abaixo de Essentials. E reparem: nada reiniciou o hub. Vocês só criaram uma fila. Mesmo assim, o Azure e o último deployment agora discordam: é drift, e quem quiser ver roda o `check-drift.ps1`; o arquivo que vocês editaram garante que o próximo deploy confirme a fila, em vez de desfazer o conserto.

Quem travou tem dois planos B. O `./scripts/deploy.ps1 -Cenario 3 -Sufixo <seu sufixo> -SemPublicar` recria a fila pelo Bicep com esse mesmo arquivo, em uns 3 minutos e meio com a fila já apagada; no começo, um aviso amarelo de consumidor sem sessão é esperado, e quem esquecer de apagar a fila vê o script parar em menos de um minuto, com a mensagem de propriedade IMUTÁVEL. E o `./scripts/deploy.ps1 -Cenario 3 -Pronto` entrega as três camadas de uma vez e recria a fila sozinho, avisando no terminal.

Olhem o `host.json`: `maxConcurrentCalls = 4` vale para o trigger sem sessão, `maxConcurrentSessions = 8` para o trigger com sessão, e o `sessionIdleTimeout` de 10 segundos faz cada sessão ociosa liberar o worker logo, para a fila drenar ao vivo. É por isso que existem dois triggers e só um fica ligado: um receptor sem sessão numa fila com sessões simplesmente não funciona.

Fila nova, consumidor velho: o `ProcessarConfirmacaoSemSessao` continua ligado, e o host registra que uma entidade com sessions não aceita receptor sem sessão, mas só quando chega mensagem: com a fila vazia, ele nem tenta receber, e a consulta volta vazia. Por isso eu mando uma fatura: as duas confirmações ficam paradas na fila, Active 2 no Overview depois do Refresh, e uns 2 minutos depois o erro aparece no Logs, umas dez vezes por segundo. Não deixem o consumidor errado ligado muito tempo. Troquem o consumidor no Portal, com os dois app settings do toggle `workerComSessao`, sempre opostos: `AzureWebJobs.ProcessarConfirmacaoSemSessao.Disabled = true` e `AzureWebJobs.ProcessarConfirmacaoComSessao.Disabled = false`; a grade corta os nomes, então confiram o Name no painel antes de mudar o Value. O app reinicia, de 5 a 35 segundos (no ensaio, 7), e as duas mensagens paradas saem da fila. Aqueçam antes de rodar, e quem veio pelo plano B aquece de qualquer jeito, porque o deploy reaplicou o APIM. Rodem as 20 faturas de novo: a duplicata some, mas a desordem não. O "registrada" atrasado ainda desbaixa, e, como no baseline, as faturas em que ele grava por último terminam `registrada`. A sessão preservou a ordem de chegada, e o banco mandou fora de ordem; regra de negócio mora no consumidor. Liguem `Cenario__Inbox` no Portal e levem `workerComSessao` e `inbox` para o mesmo `.bicepparam`: agora ele tem os mesmos valores do `cenario-3-pronto`, que é de onde o Incidente 3 parte.

**Pergunta para a turma:** se o duplicate detection já descarta a confirmação repetida, para que a inbox? Deem um cenário em que só ela salva.

**Se perguntarem:** "Se a fila é FIFO, por que precisa de sessão? E partição não resolve a ordem?" → FAQ-4.

**Se perguntarem:** "Se a fila tem duplicate detection, para que inbox e ETag?" → FAQ-5.

### Insights do mundo real
- Propriedade imutável é decisão de desenho, não de operação: escolha sessions e duplicate detection ao criar a fila, e trate a mudança como uma versão nova do canal.
- Conserto feito no Portal durante um incidente é drift até o código dizer o mesmo: leve a mudança para o `.bicepparam` antes de encerrar o incidente, senão o próximo deploy para na propriedade imutável ou desfaz o conserto.
- O Portal muda de rótulo; a API, não. Mesmo criando a fila no Portal, confira a propriedade pelo `az servicebus queue show` ou pelo Bicep: o primeiro deploy do kit achou as propriedades da fila no Overview, e não em Settings → Properties, onde o roteiro esperava.
- Duplicate detection custa throughput: todo `MessageId` novo é comparado com a janela inteira. Janela pequena e consumidor idempotente rendem mais que janela de dias.
- Sessão por fatura dá paralelismo seguro: mil faturas em paralelo, cada uma em ordem. O risco é a sessão "quente", uma única chave com volume desproporcional, que vira gargalo.
- Separe identidade de transporte e identidade de negócio: o `MessageId` serve ao broker; o `idEvento` no corpo serve à inbox e sobrevive às cópias com `MessageId` novo, como as do retry agendado e do reprocessamento.

### Referências
- [Message sessions](https://learn.microsoft.com/azure/service-bus-messaging/message-sessions)
- [Duplicate detection](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection)
- [Enable message sessions (só na criação da fila)](https://learn.microsoft.com/azure/service-bus-messaging/enable-message-sessions)
- [Criar uma fila pelo Portal (+ Queue)](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-quickstart-portal)
- [Resequencer (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/Resequencer.html)
- [`TableClient.UpdateEntityAsync` com ETag (412 Precondition Failed)](https://learn.microsoft.com/dotnet/api/azure.data.tables.tableclient.updateentityasync)

### Transição
Prova.

---

## Slide 15 — Prova (Incidente 2)

**Tempo previsto:** 14 min (a prova e o post-mortem de 3 min: Partes 7 e 8 do guia) · **Mostrar:** terminal com `./scripts/erp-simulado.ps1 -Faturas 20 -Caos "duplicar=3;embaralhar=true"`, o POST forjado no meio da tempestade (401) e o laço do guia até a fila zerar; `appi-saldaro-<sufixo>` → Logs (**KQL mode**) com `kql/baixa-duplicada.kql` vazio, os `traces` da `ProcessarConfirmacaoComSessao` (em `customDimensions`, `prop__SessionId`, `prop__SequenceNumber` e `prop__DeliveryCount`) e a contagem de `requests` (`WebhookBanco` × `ProcessarConfirmacaoComSessao`); `az storage entity query` nas tabelas `faturas` e `inbox` (ou o Storage browser do `stsaldaro<sufixo>`); a fila recriada no Portal.

### O que falar
Seis critérios, os P1 a P6 do guia, na ordem das camadas.

P1: a mesma tempestade, com o POST forjado no meio dela. Ele volta 401, e a fatura de estudo continua com o mesmo `quantidadeBaixas`. Quem quiser ir além pode mandar o mesmo POST com um `X-Saldaro-Signature` inventado: 401 também.

P2 e P3: esperem a fila zerar, mais um minuto de telemetria, e rodem o `baixa-duplicada.kql` desde o início da prova: vazio. Na tabela `faturas`, as 20 faturas aparecem com `quantidadeBaixas = 1` e status `notificada`, mesmo com o "registrada" chegando 8 segundos depois do "paga": a máquina de estados ignorou o evento atrasado. No primeiro deploy, 19 das 20 já estavam `notificada` aos 51 segundos, e a última veio logo depois.

P4: a concorrência do worker não mudou, mas agora ela é por sessão. Nos `traces` da `ProcessarConfirmacaoComSessao`, as linhas de uma fatura trazem em `customDimensions` as chaves com o prefixo `prop__`: `prop__SessionId` igual ao `faturaId`, `prop__SequenceNumber` crescente e `prop__DeliveryCount`. Primeiro o "paga", depois o "registrada", um depois do outro, sem sobreposição. Na consulta, filtrem com `contains`, não com `has`: o `has` procura termos inteiros e não acha `SessionId` dentro de `prop__SessionId`. A linha "Trigger Details" do host também traz o SessionId, no texto da mensagem. Não precisamos de duas instâncias para provar isso: a disputa já acontecia dentro de uma.

P5: uma notificação por fatura. E o broker trabalhando: uns 120 `WebhookBanco` com sucesso, um com 401, o forjado, e só 40 execuções do consumidor, todas sem falha. O Service Bus aceitou os 120 envios e descartou 80 cópias em silêncio. Na `inbox`, duas linhas por fatura, uma por `idEvento`: a do "paga" e a do "registrada".

P6: o clique consertou; o código de infraestrutura garante que o conserto fica. O `.bicepparam` editado tem os cinco toggles do `cenario-3-pronto`, e a fila que vocês criaram no Portal mostra `requiresSession` e `requiresDuplicateDetection` ligados: o arquivo e o Azure dizem o mesmo. Se alguém apagar a fila amanhã, o próximo deploy a recria igual. E o `baixa-duplicada.kql`, que antes listava as 20 faturas, é o critério que eu deixaria como alerta permanente.

Três minutos de post-mortem. Para este, a causa raiz tem pelo menos duas decisões: confiar num webhook sem assinatura e ter um consumidor que não era idempotente. Escrevam as duas.

**Pergunta para a turma:** qual destas camadas vocês considerariam opcional num sistema de baixo volume? E qual nunca?

### Insights do mundo real
- "Exactly once" de broker vale dentro da janela e entre produtor e fila; o processamento continua at-least-once, e só o consumidor idempotente fecha a conta.
- KQL de "baixa duplicada" é um ótimo alerta permanente: zero é o único valor aceitável, e qualquer ocorrência é incidente.
- Num pipeline, mudança em propriedade imutável precisa parar o deploy e pedir uma decisão (drenar, recriar ou versionar o canal), nunca passar em silêncio.

### Referências
- [Idempotent Receiver (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html)
- [Enable duplicate detection (só na criação da fila)](https://learn.microsoft.com/azure/service-bus-messaging/enable-duplicate-detection)

### Transição
Quarta-feira, 14h. O banco parceiro publicou uma atualização da API e mudou o formato do valor.

---

## Slide 16 — Separador: Incidente 3

**Tempo previsto:** 1 min.

### O que falar
Incidente 3: "A mensagem que ninguém conseguia processar".

### Transição
Sintomas.

---

## Slide 17 — Sintomas (Incidente 3)

**Tempo previsto:** 19 min (o incidente, a preparação, os sintomas e as hipóteses: Partes 1 a 3 do guia) · **Mostrar:** terminal com o `Contar-Fila` e o `./scripts/triagem-dlq.ps1` de aquecimento (Parte 1; uns 2 minutos e meio); as duas rajadas (`-Faturas 10 -Caos "valorComVirgula=true"` e `-Faturas 20 -Caos "falhaTransiente=50"`) e a DLQ chegando a ~32; Portal → `sb-saldaro-<sufixo>` → Queues → `sbq-confirmacoes-banco` → Service Bus Explorer → aba Dead-letter → Peek Mode → Peek from start (o peek da DLQ funciona com a fila exigindo sessão; colunas Delivery Count, Session ID, Dead Letter Reason e Dead Letter Error Description; uma mensagem com `"valor": "1.250,00"` e uma `cobranca.paga` boa com `simulaTransiente = true`, as duas com `MaxDeliveryCountExceeded`); `appi-saldaro-<sufixo>` → Logs (aba própria, toggle **Agent** desligado, **KQL mode**) com as exceções do consumidor e o ritmo das reentregas (Passos 3.1 e 3.2); a triagem só leitura com um grupo só.

**Comandos:**

```powershell
./scripts/erp-simulado.ps1 -Faturas 10 -Caos "valorComVirgula=true"
./scripts/erp-simulado.ps1 -Faturas 20 -Caos "falhaTransiente=50"
./scripts/triagem-dlq.ps1
```

### O que falar
Quarta-feira, 14h. O banco parceiro publicou uma atualização da API e, sem avisar ninguém, passou a mandar o `valor` como texto no formato brasileiro: "1.250,00" em vez de 1250.00. O ERP não tem culpa nenhuma; o contrato mudou do lado de quem confirma o pagamento.

O ambiente de vocês está no ponto de partida certo, o estado pronto do Incidente 2; quem chegou agora roda `./scripts/deploy.ps1 -Cenario 4 -Sufixo <seu sufixo>`, uns 6 minutos, e aquece antes da primeira rajada. No primeiro deploy, sem aquecimento, a rajada de 10 levou 107 segundos, com 6 timeouts do cliente que nem chegaram ao APIM. Antes do incidente, rodem a triagem uma vez: ela compila a ferramenta e confirma que vocês conseguem ler a DLQ. Contem uns 2 minutos e meio, nesta e nas próximas: no primeiro deploy, a primeira levou 149 segundos, e as seguintes, de 134 a 145. Disparem e sigam a história enquanto ela roda.

Agora a primeira rajada, dez faturas com `valorComVirgula=true`: é a carteira que já recebe o formato novo do banco, e as duas confirmações de cada fatura, a "registrada" e a "paga", chegam com o valor em texto. Cada uma é veneno: o worker tenta desserializar, o `valor` não é número, exceção. O hub abandona a mensagem, o Service Bus reentrega na hora, a exceção se repete e, depois da décima entrega, a mensagem vai para a DLQ com o motivo genérico `MaxDeliveryCountExceeded` e uma descrição que não ajuda: "Message could not be consumed after 10 delivery attempts."

Agora a segunda rajada, vinte faturas só com `falhaTransiente=50`: a carteira que ainda recebe o formato antigo e pega a oscilação. Essas mensagens são boas: o valor é número e a assinatura confere. Só que metade dos "paga" chega com uma falha transiente simulada que dura 45 segundos, como um armazenamento instável por menos de um minuto. O worker falha, o Service Bus reentrega imediatamente, e as dez entregas acabam em poucos segundos, muito antes de a falha passar. Por que duas rajadas, e não uma com os dois botões? Porque o worker desserializa antes de olhar a falha transiente: com o valor em texto, nenhuma mensagem boa cairia na DLQ. Resultado: em um ou dois minutos, a DLQ fechou em 32 no primeiro deploy, 20 envenenadas e 12 boas, todas com o mesmo motivo. O número das boas varia de turma para turma, porque o botão sorteia metade dos "paga": contem com umas 10, para mais ou para menos.

Abram o Service Bus Explorer da fila, na aba Dead-letter, e façam o Peek from start. O peek da DLQ funciona mesmo com a fila exigindo sessão, e as colunas já contam metade da história: Delivery Count 10, o Session ID, que é o `faturaId`, e o Dead Letter Reason. Escolham duas mensagens. Uma tem `"valor": "1.250,00"` no corpo; a outra é um pagamento perfeito, com `simulaTransiente = true`. As duas dizem `MaxDeliveryCountExceeded`. Na história foram 3.800 assim; aqui, umas 32 já contam a mesma história.

Antes de mudar qualquer coisa, provem o diagnóstico. As exceções do consumidor mostram duas famílias, pelo tipo que o próprio hub lança: `ConfirmacaoInvalidaException`, a desserialização apontando para `valor`, e `FalhaTransienteSimuladaException`, a falha transiente simulada. Não leiam a contagem como uma exceção por falha: cada tipo aparece em duas linhas, uma sem `cloud_RoleName`, do host, bem mais numerosa, e outra com o nome do hub, do worker, e nenhuma das duas bate com a conta de mensagens vezes entregas. O que prova o diagnóstico são os tipos e as mensagens, não o total; para não somar as duas linhas, filtrem por `cloud_RoleName`. E o ritmo: no primeiro deploy, foram 204 abandonos em uns 2 minutos. A reentrega foi imediata porque o hub chama `AbandonMessageAsync` na falha, e o Service Bus não tem atraso configurável para reentrega. E a triagem só leitura mostra um grupo só, `MaxDeliveryCountExceeded`: agrupar por motivo não ajuda quando o motivo é sempre o mesmo. Quem sabia por que a mensagem falhou era o consumidor, e ele não disse.

E agora? Reprocessar tudo como está devolve o veneno para a DLQ, e uma cópia reprocessada com o mesmo `MessageId`, dentro da janela do duplicate detection, seria descartada em silêncio. Um conserto apressado que lesse o texto só até a vírgula registraria R$ 1,25 onde o banco disse R$ 1.250,00, e baixaria faturas com o valor errado. Descartar tudo perde pagamentos de verdade. Sem motivo, a DLQ não é uma ferramenta: é um depósito.

**Pergunta para a turma:** olhando só para estas duas mensagens, como vocês decidiriam qual reprocessar? O que faltou o worker registrar?

**Se perguntarem:** "Por que não deixar o Service Bus reentregar até a DLQ? O Functions não tem retry policy?" → FAQ-6.

### Insights do mundo real
- Mudança de contrato do parceiro é a causa mais comum de mensagem envenenada em integração B2B, e quase nunca vem com aviso. Validação de schema na entrada e versionamento do contrato são a primeira linha.
- Parse sensível a cultura é armadilha clássica: "1.250,00" vale mil duzentos e cinquenta em pt-BR e não é número na cultura invariante. Fixe a cultura no parser e rejeite o que não for número.
- `MaxDeliveryCountExceeded` em massa é sintoma, não diagnóstico: diz que o consumidor desistiu, nunca por quê.
- A DLQ de uma fila com sessions não exige sessão: o peek dela funcionou no Explorer do Portal e funciona no SDK, embora o Learn diga que o Explorer não suporta sessions. Teste o caminho do plantão antes do plantão.

### Referências
- [Dead-letter queues: motivos do sistema e `MaxDeliveryCount`](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)
- [Service Bus Explorer no Portal](https://learn.microsoft.com/azure/service-bus-messaging/explorer)
- [Invalid Message Channel (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/InvalidMessageChannel.html)

### Transição
Classificar antes de agir.

---

## Slide 18 — Classificar, agendar, triar

**Tempo previsto:** 12 min (o conserto: Parte 4 do guia) · **Mostrar:** VS Code com a lógica de baixa compartilhada pelos dois triggers (`Aplicacao/ProcessadorDeBaixa.cs`, `Dominio/ClassificadorDeConfirmacao.cs` e `Dominio/PoliticaDeRetry.cs`; procure `CONCEITO:`: classificação do erro, `DeadLetterMessageAsync` com motivo e descrição, cópia agendada com `ScheduledEnqueueTime`); Portal → `func-saldaro-hub-<sufixo>` → Settings → Environment variables → `Cenario__DlqComMotivo` e `Cenario__RetryAgendado` = `true` (Passo 4.2 do guia; **Show value** só na linha, nunca o **Show values** da barra); Portal → `sb-saldaro-<sufixo>` → Entities → Queues → `sbq-confirmacoes-banco` → Overview → seção **Settings**, abaixo de **Essentials** → **(change)** sob **Max delivery count** (de `10` para `3`) → **OK** → **Refresh** (Passo 4.3); a comparação entre `cenario-3-pronto.bicepparam` e `cenario-4-pronto.bicepparam` e a `var filas` de `infra/modules/service-bus.bicep` (a versão declarada do mesmo conserto); o Passo 4.4 do guia: `az servicebus queue show` com `maxDeliveryCount = 3` e lock `PT1M`, e o `Contar-Fila` com a DLQ ainda com as ~32 do incidente. Plano B, só para quem travar: `./scripts/deploy.ps1 -Cenario 4 -Pronto -SemPublicar` com a fila parada (no what-if, a fila aparece como **Deploy**, `!`, sem valores; nada é recriado).

**Comandos:**

```powershell
# Alternativa ao Portal para os dois toggles (Passo 4.2)
az functionapp config appsettings set -g $cfg.SALDARO_RG -n "func-saldaro-hub-$($cfg.SALDARO_SUFIXO)" --settings Cenario__DlqComMotivo=true Cenario__RetryAgendado=true -o none
# Passo 4.3: o mesmo conserto, declarado (três linhas de diferença)
git diff --no-index infra/parameters/cenario-3-pronto.bicepparam infra/parameters/cenario-4-pronto.bicepparam
# Sem o Change na Overview da fila? A mesma mudança pela CLI (@fila e Contar-Fila vêm do Passo 1.1)
az servicebus queue update @fila --max-delivery-count 3 -o none
# Passo 4.4: o Change do Portal, conferido pela API
az servicebus queue show @fila --query "{maxDeliveryCount:maxDeliveryCount, lockDuration:lockDuration}" -o table
Contar-Fila
# Plano B, só para quem travar: toggles e maxDeliveryCount de uma vez, com a fila parada
./scripts/deploy.ps1 -Cenario 4 -Pronto -SemPublicar
```

### O que falar
O conserto começa por uma pergunta que o worker não fazia: este erro passa sozinho ou não?

Erro permanente é o que se repete igual em toda tentativa: schema inválido, regra de negócio violada. Tentar dez vezes não muda nada, só atrasa e esconde. Com `Cenario:DlqComMotivo` ligado, o worker manda a mensagem para a DLQ na primeira entrega, com `DeadLetterMessageAsync(message, deadLetterReason: "SchemaInvalida", deadLetterErrorDescription: ...)`, e a descrição leva a mensagem do erro. Isso só é possível porque os triggers usam `AutoCompleteMessages = false` e recebem `ServiceBusMessageActions`: quem decide o destino da mensagem é o código, não o runtime.

Erro transiente é o que passa com o tempo. Reentregar na hora é inútil, como vimos: dez tentativas em segundos contra uma falha de 45. Com `Cenario:RetryAgendado`, o worker envia uma cópia agendada da mensagem para daqui a 5 segundos vezes 2 elevado a n, com a propriedade `tentativa`, e completa a original. Dez segundos, vinte, quarenta: setenta segundos de fôlego contra uma falha de 45. No primeiro deploy, a segunda tentativa já encontrou a falha resolvida, e a terceira nem foi usada: a primeira entrega chega alguns segundos depois do envio do banco, cada cópia agendada aparece no horário marcado ou um pouco depois, e esses atrasos contam a favor. Se nem a terceira resolver, aí sim vai para a DLQ, com o motivo `TentativasEsgotadas`. Um detalhe que pega muita gente: a cópia sai com `MessageId = {idEvento}-t{n}`. Se saísse com o mesmo `MessageId`, o duplicate detection, que agora está ligado, a descartaria em silêncio, porque mensagem agendada também passa pela detecção.

Vamos aplicar, tudo no Portal. Os toggles: no `func-saldaro-hub-<sufixo>`, Environment variables, `Cenario__DlqComMotivo` e `Cenario__RetryAgendado` em `true`, e o app reinicia, de 5 a 35 segundos (no ensaio, 7). A política da fila: no Overview da `sbq-confirmacoes-banco`, na seção Settings, (change) ao lado de Max delivery count, de 10 para 3, e OK; depois, Refresh, porque o Overview não se atualiza sozinho. Três cliques, menos de um minuto. Essa propriedade, ao contrário de sessions, muda numa fila existente: nada é recriado, e a DLQ do incidente continua lá, esperando a triagem. Confiram com o `az servicebus queue show` do Passo 4.4: 3 e `PT1M`. E o Bicep? Comparem o `cenario-3-pronto` com o `cenario-4-pronto`: são três linhas, os dois toggles e `maxDeliveryCount = 3`. O arquivo do estado pronto já declara o que vocês acabaram de clicar; o último deployment do ambiente, não. É drift de propósito: um `deploy.ps1 -Cenario 4`, o estado inicial, devolveria o 10 e desligaria os toggles sem perguntar, e quem fecha a conta é um deploy do `cenario-4-pronto`. Por que três? Porque agora o permanente sai na primeira entrega e o transiente é agendado; o `maxDeliveryCount` vira rede de segurança para o erro que ninguém previu. Quem travou tem o plano B: `./scripts/deploy.ps1 -Cenario 4 -Pronto -SemPublicar`, com a fila parada, uns 4 minutos no primeiro deploy, que liga os toggles e baixa o `maxDeliveryCount` de uma vez; no what-if, a fila aparece como todo recurso que já existe, com `!` (Deploy), sem dizer o que muda, porque o script usa o formato `ResourceIdOnly`: o 10 virando 3 se confere depois, com o `az servicebus queue show` do Passo 4.4. Para todos, a prova começa pelo aquecimento: o hub acabou de reiniciar, e o plano B ainda reaplicou o APIM.

E o que ficou de antes? A triagem, que vocês executam na prova. O `./scripts/triagem-dlq.ps1` faz peek na DLQ e agrupa por `DeadLetterReason`, com exemplos. Com `-Reprocessar`, a ferramenta recebe cada mensagem da DLQ, corrige o `valor` em texto para número, reprocessa a mensagem, mandando a cópia para a fila com `MessageId = {original}-r{n}`, o mesmo `SessionId` e a propriedade `reprocessadoDe`, e só então completa a mensagem da DLQ. As mensagens boas do lote antigo voltam como estão, porque a falha já passou. E a inbox garante que nada seja baixado duas vezes, porque ela olha o `idEvento` do corpo, não o `MessageId`. Para quando a decisão é jogar fora, existe o `-DescartarMotivo`, e essa decisão é de negócio, nunca do script.

**Pergunta para a turma:** que outros erros do hub vocês classificariam como permanentes? E um 429 do banco parceiro: permanente ou transiente?

**Se perguntarem:** "Por que retry agendado à mão? O Functions não tem retry policy?" → FAQ-6.

### Insights do mundo real
- Classificar o erro é a decisão mais barata e mais rentável de um consumidor: transforma a DLQ de depósito em fila de trabalho com instruções.
- Política de fila mudada no Portal durante o incidente é legítima, desde que o arquivo diga o mesmo antes do próximo deploy; o `check-drift.ps1` da leitura 3.7 acha o que ficou só no clique.
- Retry agendado não segura lock nem instância: a mensagem original é completada e a cópia espera no broker. Por isso o lock de 1 minuto sobra.
- Reprocessar perde a ordem original da sessão: a mensagem reprocessada ganha novo enqueue time e novo sequence number. É a máquina de estados tolerante que torna isso seguro.
- Ferramenta de reprocesso sempre deixa trilha: o `reprocessadoDe` na cópia reprocessada e o motivo na DLQ permitem responder, semanas depois, quem reprocessou o quê.

### Referências
- [Dead-letter queues: dead-letter pela aplicação e `SubQueue.DeadLetter`](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)
- [Scheduled messages](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing#scheduled-messages)
- [Service Bus trigger no isolated worker: `ServiceBusMessageActions` e `AutoCompleteMessages`](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus-trigger)
- [Retry pattern](https://learn.microsoft.com/azure/architecture/patterns/retry)
- [Dead Letter Channel (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/DeadLetterChannel.html)
- `01_Aulas/Incidente3_DLQ_Guia.md`, Passos 4.2 a 4.4 ("Portal primeiro") e a seção Plano B

### Transição
Prova.

---

## Slide 19 — Prova (Incidente 3)

**Tempo previsto:** 18 min (a prova, o post-mortem de 3 min e as lições de 2 min: Partes 5 e 6 do guia) · **Mostrar:** terminal com a sonda de aquecimento, as mesmas duas rajadas e o `Contar-Fila` (DLQ de ~32 para ~52); `./scripts/triagem-dlq.ps1` com o grupo novo `SchemaInvalida` (20) e o Service Bus Explorer com uma dessas mensagens (`DeliveryCount` 1); `appi-saldaro-<sufixo>` → Logs (**KQL mode**) com `mensagem.deadletter`, `mensagem.retry.agendado` (tentativas 1 e 2, com 10 e 20 s; a tentativa 3, de 40 s, só se a falha durar mais) e `kql/dlq-por-motivo.kql`; `./scripts/triagem-dlq.ps1 -Reprocessar` (52 reprocessadas no primeiro deploy) e a DLQ em zero (também na métrica `DeadletteredMessages` no Portal); a tabela `faturas` e a consulta final do guia (60 faturas, 60 baixadas, nenhuma baixa dupla); `GET {SALDARO_APIM_URL}/faturas/{id}` da fatura de estudo; `maxDeliveryCount` no Bicep e no Portal.

**Comandos:**

```powershell
# Aquecimento depois do restart do slide 18, obrigatório se o conserto veio pelo deploy.ps1 do plano B (404 é o esperado: a fatura não existe)
1..3 | ForEach-Object { (Invoke-WebRequest "$($cfg.SALDARO_APIM_URL)/faturas/aquecimento/status" -SkipHttpErrorCheck).StatusCode }
./scripts/erp-simulado.ps1 -Faturas 10 -Caos "valorComVirgula=true"
./scripts/erp-simulado.ps1 -Faturas 20 -Caos "falhaTransiente=50"
./scripts/triagem-dlq.ps1
./scripts/triagem-dlq.ps1 -Reprocessar
./scripts/triagem-dlq.ps1
```

### O que falar
Cinco critérios. A prova repete as mesmas duas rajadas: mesma entrada, outro desfecho. Antes, o aquecimento: o hub acabou de reiniciar com os toggles, e quem foi pelo plano B acabou de reaplicar o APIM. No primeiro deploy do kit, logo depois de um `deploy.ps1`, a primeira sonda levou 1,9 segundo e as seguintes, uns 130 ms, e as rajadas saíram limpas, 10 faturas em 5 segundos e 20 em menos de um. Um aviso de relógio: cada triagem leva uns 2 minutos e meio, e a prova tem três; disparem cada uma e sigam a conversa enquanto ela roda.

Um: o veneno sai na primeira entrega. A DLQ ganha 20 mensagens em uns 20 segundos, as duas confirmações de cada uma das 10 faturas (no primeiro deploy, de 32 para 52), e o consumidor executa uma vez por mensagem, sem falha. Na DLQ, cada mensagem nova tem `DeadLetterReason = SchemaInvalida`, `DeliveryCount` 1 e, na descrição, a mensagem do erro dizendo qual campo não converteu. Quem estiver de plantão amanhã sabe o que fazer sem abrir o código.

Dois: os transientes nunca chegam à DLQ. No `Contar-Fila`, as agendadas ficam acima de zero por uns 35 a 50 segundos (no primeiro deploy, pico de 11) e zeram uns 70 segundos depois da rajada. No Application Insights, `mensagem.retry.agendado` mostra as tentativas 1 e 2, com 10 e 20 segundos de atraso, umas 10 mensagens em cada (11 no primeiro deploy; o número varia com o sorteio); logo depois vem a `fatura.baixada` da mesma fatura. A tentativa 3, de 40 segundos, pode nem aparecer: no primeiro deploy, quando ela chegaria, a falha de 45 segundos já tinha passado. A DLQ não cresce, nenhuma `TentativasEsgotadas` aparece e as 20 faturas da rajada baixam.

Três: a triagem conta a história certa. O `triagem-dlq.ps1` mostra dois grupos: `MaxDeliveryCountExceeded`, com as 32 do incidente (ou perto disso), e `SchemaInvalida`, com as 20 da prova, e nenhuma `TentativasEsgotadas`. O `dlq-por-motivo.kql` mostra o mesmo pelo lado do hub, só com os dead-letters que o próprio código fez.

Quatro: depois do `-Reprocessar`, a DLQ está vazia e as faturas corrigidas estão baixadas. No primeiro deploy, o resumo mostrou 52 reprocessadas: 40 com o valor corrigido, as 20 envenenadas do incidente e as 20 da prova, e 12 sem correção, as boas do incidente; nenhuma descartada, nenhuma falha, e a DLQ zerou em cerca de um minuto. O 12 é o número de transientes do sorteio da Parte 2, então o total de cada um pode ser outro, perto de 50. Consultem uma fatura pela API: `baixada` ou `notificada`, com o valor numérico certo. A consulta final do guia fecha a conta: as 60 faturas do incidente e da prova, 60 baixadas, nenhuma com baixa dupla. Se ela ainda não mostrar as 60, é a ingestão: a `fatura.baixada` do reprocesso leva alguns minutos para chegar ao Application Insights, e a tabela `faturas` já mostra as 60 `notificada`, com `quantidadeBaixas = 1`.

Cinco: a política da fila está versionada. `maxDeliveryCount = 3` e o lock de 1 minuto, coerente com um processamento que leva milissegundos e com um retry que não segura lock: no Azure, pelo Change que vocês fizeram no Portal e conferiram com `az servicebus queue show`; no código, declarados no `cenario-4-pronto.bicepparam`. O clique resolveu o incidente; o arquivo garante que o próximo deploy confirme o conserto em vez de desfazê-lo. O que sobra para reentrega imediata é o imprevisto, e três tentativas bastam para distinguir um soluço de um defeito.

Três minutos de post-mortem. Causa raiz candidata: o worker tratava todo erro do mesmo jeito. E uma linha de "o que impede de repetir" que vale ouro: alerta em `DeadletteredMessages` maior que zero, que é exatamente o que o desafio para casa vai pedir.

**Pergunta para a turma:** quem deveria ser avisado quando aparece um `SchemaInvalida`: o time do hub, o banco parceiro ou os dois?

### Insights do mundo real
- DLQ vazia é estado de chegada, não de repouso: sem alerta, ela volta a encher em silêncio no próximo contrato que mudar.
- Avise o parceiro com evidência: um `DeadLetterErrorDescription` com o campo e o valor recusado encurta a conversa de dias para minutos.
- Escreva o runbook da triagem enquanto o incidente está fresco: quem roda `-Reprocessar` às 3h da manhã precisa saber quando não rodar.

### Referências
- [Dead-letter queues: receber da DLQ com `SubQueue.DeadLetter`](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)
- [Dead Letter Channel (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/DeadLetterChannel.html)

### Transição
Três incidentes, três post-mortems. Vamos olhar para eles juntos.

---

## Slide 20 — Post-mortem coletivo

**Tempo previsto:** 10 min · **Mostrar:** o template de cinco linhas; pedir que duas ou três pessoas leiam o seu.

### O que falar
Cinco linhas, sem culpados: o que aconteceu, impacto, causa raiz, o que consertamos, o que impede de repetir. Vocês escreveram um por incidente; agora vamos ler alguns em voz alta. Não estou procurando o "certo"; estou procurando a causa raiz escrita como decisão de arquitetura, não como erro de pessoa. "O ERP reenviou" não é causa raiz; "a API era síncrona e não tinha chave de idempotência" é. Quem ler o seu ganha perguntas da turma, não correções.

Olhem os padrões que praticamos: Asynchronous Request-Reply, Idempotent Receiver, Resequencer com sessões, Dead Letter Channel com motivo, Retry com backoff e teto. Cada um tem nome, e o nome importa: é o vocabulário que permite discutir arquitetura com precisão num comitê, numa entrevista, num ADR.

E as três heranças do legado que doeram: banco não é fila; segredo em header não é assinatura; DLQ sem motivo é lixo acumulado. Se vocês saírem daqui com essas três frases, o dia valeu.

**Pergunta para a turma:** qual dos três incidentes vocês acham que acontece primeiro num sistema novo? Por quê?

### Insights do mundo real
- Post-mortem sem culpa é cultura, e cultura se treina em sala antes de treinar em produção.
- Nomear o padrão transforma uma correção pontual em conhecimento reutilizável pelo time inteiro.

### Referências
- [Blameless postmortems (Google SRE Book)](https://sre.google/sre-book/postmortem-culture/)

### Transição
O plantão continua em casa.

---

## Slide 21 — Desafio para casa e guias de leitura

**Tempo previsto:** 7 min (a limpeza de 5 min vem logo depois, ainda neste slide) · **Mostrar:** o guia do cenário 3.6 no repositório; a lista dos outros guias; o Portal com o botão "Delete resource group".

### O que falar
O desafio para casa é o incidente que não coube hoje e que, na minha experiência, é o mais traiçoeiro: "O agendador que morreu em silêncio". Um timer para, nenhum erro aparece, e ninguém percebe por meses. A solução é um heartbeat emitido ao concluir o trabalho, não ao começar, e um alerta por ausência de sinal; o workbook é opcional. O guia está no repositório com o mesmo ritual: sintomas, hipóteses, conserto, prova. A entrega é um print do alerta disparando e o post-mortem de cinco linhas. Contem com a espera: o alerta dispara entre 15 e 25 minutos depois do último heartbeat. E um aviso que o primeiro deploy real do kit ensinou: durante o deploy do estado pronto, chega um e-mail do Azure pedindo para verificar o endereço do Action Group, com um código que vale 30 minutos. Sem essa verificação, o alerta dispara no Portal, mas nenhum e-mail chega.

Os outros quatro cenários ficam como guias de leitura: rate limit com 429, no limite de 60 chamadas/min por chave de cada cliente, e retry com teto; Managed Identity e Key Vault no lugar de connection string; Bicep, `what-if` e drift; e a decisão Premium ou Standard com um ADR. Façam no ritmo de vocês; cada um cabe numa noite.

**Se perguntarem:** "Por que Service Bus Standard? Um hub de pagamentos não usaria Premium?" → FAQ-3.

E agora o passo mais importante do dia: apaguem o `rg-saldaro-lab`. Todos juntos, agora, ao vivo. Service Bus Standard cobra por hora e não dorme.

### Insights do mundo real
- Alerta por ausência de sinal (dead man's switch) é o alerta mais barato e mais negligenciado de qualquer sistema com agendamento.

### Referências
- [Log search alerts](https://learn.microsoft.com/azure/azure-monitor/alerts/alerts-types#log-alerts)

### Transição
Encerramento.

---

## Slide 22 — Encerramento e referências

**Tempo previsto:** 3 min.

### O que falar
Obrigado pelo plantão. Os links do slide estão no repositório da aula, junto com os guias, o kit e os templates de post-mortem e de ADR. Dúvidas no canal da turma; eu respondo durante a semana.

Antes de fechar o notebook, confiram no Portal que o `rg-saldaro-lab` está sendo apagado. Quem preferir o terminal tem o `./scripts/limpar.ps1`, que apaga o Resource Group e ainda purga o Key Vault e o APIM, que ficam em soft-delete com o nome reservado, para o sufixo ficar livre na próxima vez. O kit continua no repositório: qualquer cenário sobe de novo com `deploy.ps1 -Cenario N` em poucos minutos, então refazer um incidente com calma, durante a semana, custa centavos. É o melhor exercício para fixar o dia: escolher um dos três, recriar o estado inicial e quebrar de novo, sem olhar o guia.

Duas coisas para levar: primeiro, o ritual, porque é o mesmo de um incidente real; segundo, os nomes dos padrões, porque são o vocabulário que faz vocês serem ouvidos como arquitetos. Até a próxima.

### Referências
- [Cloud Design Patterns](https://learn.microsoft.com/azure/architecture/patterns/)
- [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/)
- [Azure Service Bus documentation](https://learn.microsoft.com/azure/service-bus-messaging/)
- [Azure Functions .NET isolated worker](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [Bicep documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)

---

## Apêndice — Perguntas que a turma vai fazer

> **Slides de bolso, fora da soma das 4h.** Estes dez slides só abrem quando a pergunta surge: a nota do slide principal traz a linha "**Se perguntarem:** ... → FAQ-N", o professor salta para o FAQ, responde em 60 a 90 segundos e volta pela "Transição" ao slide de onde a pergunta veio. O tempo deles não entra nos 238 minutos do encontro. Se o relógio do bloco estiver apertado, responda com o slide (a resposta curta, uns 20 segundos) e deixe a resposta completa para a pausa, para o fim do bloco ou para o canal da turma.
>
> **Fonte:** `docs/research/2026-10-14-duvidas-das-tecnologias.md`, as dez perguntas marcadas [Slide]. "O que falar" vem da "Resposta completa" e do "No kit da Saldaro"; "Insights do mundo real", do "Quando a alternativa vence"; "Referências", das fontes do Microsoft Learn que a pesquisa cita para a pergunta (sem o segmento `en-us`, como no resto destas notes).

---

## FAQ-1 — APIM × WAF

**Tempo previsto:** 1 a 2 min, fora da soma das 4h (slide de bolso) · **Ganchos:** slides 3 e 6 · **Mostrar:** o diagrama em camadas do slide; se ajudar, `apim-saldaro-<sufixo>` → APIs → Saldaro Hub - faturas (`saldaro`) → Design, com o `forward-request timeout="30"` e o `on-error`: é o APIM governando a chamada, coisa que nenhum WAF faz.

### O que falar
Um não faz o papel do outro. O WAF, o Azure Web Application Firewall, protege a aplicação web contra ataques conhecidos: regras OWASP, SQL injection, cross-site scripting, bots, geofiltro. Ele roda no Application Gateway, no Application Gateway for Containers e no Front Door. O APIM governa a API: valida subscription key, JWT e certificado, aplica cota e rate limit, corta o backend no tempo combinado, transforma requisição e resposta e registra telemetria por operação. A página do Learn sobre o OWASP API Top 10 no APIM é explícita: mesmo com os controles do APIM, a proteção contra ameaças web tradicionais e bots vem do Front Door, do Application Gateway e do WAF, de preferência na frente dele. A arquitetura de referência do Architecture Center é exatamente essa: Application Gateway com WAF na borda e o APIM atrás. No lab ficou só o APIM, e ele trabalha o dia todo: corta aos 30 segundos no Incidente 1, registra o `x-cliente-id` no Application Insights e, no guia 3.1, limita cada empresa cliente. O WAF ficou de fora porque os incidentes de hoje são de integração, não de ataque web, e porque ele tem custo fixo, que não cabe num Free Trial de um dia. Uma dica: no Learn, "WAF" também é o Well-Architected Framework; confirme de qual dos dois a pessoa está falando.

### Insights do mundo real
- API pública exposta a ataque web e a bots: WAF na frente do APIM, nunca no lugar dele. Front Door quando o tráfego é global ou multirregião; Application Gateway quando é regional e os backends estão numa VNet.
- Sem API para governar (um site, um front-end): WAF sem APIM. Se o requisito é só TLS, roteamento ou balanceamento, Front Door ou Application Gateway resolvem; o Learn lembra que o APIM não é um balanceador de carga genérico.
- Em produção, a Saldaro poria o WAF na borda e faria o APIM aceitar só o tráfego dele. Com o Front Door, o Learn tem um passo a passo que vale para todos os tiers, inclusive o Consumption: `ip-filter` com as faixas `AzureFrontDoor.Backend` e `check-header` no `X-Azure-FDID`.
- APIM privado atrás do WAF exige rede: injeção em VNet (Developer ou Premium) ou as opções de rede do Standard v2 e do Premium v2. O Consumption do lab não tem VNet.
- mTLS atrás do Application Gateway tem pegadinha: o certificado do cliente não chega ao APIM, porque o gateway abre outra conexão TLS; o contorno documentado são as server variables de autenticação mútua.

### Referências
- [Introduction to Azure Web Application Firewall](https://learn.microsoft.com/azure/web-application-firewall/overview)
- [Mitigate OWASP API security top 10 in Azure API Management](https://learn.microsoft.com/azure/api-management/mitigate-owasp-api-threats)
- [Protect APIs by using Azure Application Gateway and Azure API Management](https://learn.microsoft.com/azure/architecture/web-apps/api-management/architectures/protect-apis)
- [Configure Azure Front Door in front of Azure API Management](https://learn.microsoft.com/azure/api-management/front-door-api-management)
- [API Gateway Overview](https://learn.microsoft.com/azure/api-management/api-management-gateways-overview)
- [Secure APIs using client certificate authentication in API Management](https://learn.microsoft.com/azure/api-management/api-management-howto-mutual-certificates-for-clients)
- [Load Balancing Options](https://learn.microsoft.com/azure/architecture/guide/technology-choices/load-balancing-overview)
- Pesquisa, pergunta 1; `docs/adr/0007-http-triggers-anonymous-e-apim-sem-subscription.md`, seção "O que mudaria em produção"

### Transição
De volta ao slide de onde a pergunta veio, em geral o 6 ou o 3: o APIM fica na frente o dia todo, e o primeiro incidente começa justamente com ele cortando aos 30 segundos.

---

## FAQ-2 — Service Bus × outras filas e eventos

**Tempo previsto:** 1 a 2 min, fora da soma das 4h (slide de bolso) · **Ganchos:** slide 3 · **Mostrar:** a comparação do slide; se ajudar, `stsaldaro<sufixo>` → Storage browser → Queues, com a `banco-pagamentos` do banco simulado, ao lado das filas `sbq-*` do hub em `sb-saldaro-<sufixo>` → Entities → Queues.

### O que falar
Porque cada serviço tem um papel, e o nosso é mensagem de alto valor. A página de comparação do Learn resume em três frases: Event Grid é roteamento reativo de eventos, Event Hubs é streaming e ingestão de big data, e Service Bus é mensageria transacional corporativa, com processamento de pedidos e transações financeiras como exemplos. Cobrança e baixa são comandos: alguém precisa tratar, e uma vez só; o guia de mensageria do Architecture Center lembra que comando processado duas vezes vira cobrança em dobro. Na tabela do Learn, só o Service Bus tem transações e detecção de duplicatas, e a ordem dele vem com as sessões; no Event Hubs, a ordem é por partição, e no Event Grid, não há garantia. A Storage Queue é mais barata e aguenta backlog muito maior, mas não garante ordem nem tem dead-letter automático: a mensagem envenenada aparece pelo `DequeueCount`. Event Hubs é telemetria aos milhões, com releitura do stream; Event Grid é avisar que algo aconteceu, em push, para vários assinantes. No kit, as três filas do hub são do Service Bus Standard, o `FaturaBaixada` sai pelo Event Grid e cai numa fila, e o banco simulado usa, de propósito, uma Storage Queue: o trigger tenta cinco vezes e joga a mensagem na fila `-poison`. Guardem esse contraste para a DLQ com motivo do Incidente 3.

### Insights do mundo real
- Storage Queue vence em trabalho simples e idempotente, backlog muito grande (acima de 80 GB), menor custo e nenhuma necessidade de ordem, sessão ou detecção de duplicatas.
- Event Hubs vence em telemetria e eventos em stream: milhões por segundo, releitura do stream, clientes Kafka.
- Event Grid vence quando é preciso reagir a eventos de recursos do Azure ou espalhar uma notificação para vários assinantes, com filtro e em push. Quando o evento pede workflow, o Architecture Center descreve o desenho do kit: o Event Grid entrega numa fila do Service Bus (ADR 0005).
- Em cobrança, escolha o broker pela semântica de entrega, não pelo preço por milhão de operações: comando processado duas vezes custa uma cobrança em dobro.

### Referências
- [Compare messaging services](https://learn.microsoft.com/azure/service-bus-messaging/compare-messaging-services)
- [Compare Azure Storage queues and Service Bus queues](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-azure-and-service-bus-queues-compared-contrasted)
- [Asynchronous messaging options](https://learn.microsoft.com/azure/architecture/guide/technology-choices/messaging)
- [Azure Queue storage trigger for Azure Functions](https://learn.microsoft.com/azure/azure-functions/functions-bindings-storage-queue-trigger)
- Pesquisa, pergunta 4

### Transição
De volta ao slide 3: os três consertos de hoje passam por uma fila do Service Bus, e é por isso que ela está no meio do diagrama.

---

## FAQ-3 — Service Bus Standard × Premium

**Tempo previsto:** 1 a 2 min, fora da soma das 4h (slide de bolso) · **Ganchos:** slides 2 e 21 · **Mostrar:** a árvore de decisão do slide; se ajudar, `docs/adr/0003-service-bus-standard-com-identidade-gerenciada.md` no VS Code, com os cinco gatilhos.

### O que falar
Porque o Standard tem tudo o que os incidentes usam: sessões, detecção de duplicatas, DLQ, transações e mensagens agendadas. O Premium compra outra coisa: isolamento de recursos, com messaging units dedicadas, latência previsível, mensagens de até 100 MB e os recursos de rede, como Private Endpoint, service endpoints e trusted services, a preço fixo por hora. O Standard é multitenant e cobra por operação, com throttling por crédito: mil créditos por segundo por namespace; quando eles acabam, a operação volta com o erro 50009, e os SDKs refazem com backoff, sem perder dados. O próprio Learn indica o Standard para desenvolvimento, QA e produção de baixo throughput. Então a pergunta certa não é "pagamento pede Premium?", e sim "que requisito pede Premium?". O ADR 0003 do kit fixa cinco gatilhos objetivos: exigência de isolamento de rede, throttling recorrente, mensagens acima de 256 KB que não possam virar claim check, latência previsível ou isolamento por empresa cliente, e chave gerenciada pelo cliente ou Geo-Replication. Apareceu um, a decisão reabre. E a migração existe, in-place e com a mesma connection string, mas sem volta: as role assignments não migram, e as mensagens não são copiadas, é preciso drenar o namespace antigo. A leitura 3.8 é exatamente esse exercício de decisão.

### Insights do mundo real
- Premium vence quando um gatilho do ADR 0003 aparece: política de rede que exige Private Endpoint, service endpoint ou trusted services; throttling recorrente (`ThrottledRequests` acima de zero, erro 50009) depois de ajustar os clientes; mensagens grandes sem claim check; SLA de latência ou isolamento por cliente; CMK, Geo-Replication ou JMS 2.0.
- Antes de migrar, planeje o que não vem junto: role assignments, mensagens e o que aponta para o ID de recurso, como a event subscription do Event Grid.
- Novidade na conversa de rede: o Network Security Perimeter está GA, suporta o Service Bus, e o Architecture Center o recomenda para namespace Standard sem private endpoint. Não é Private Endpoint (o tráfego continua no endpoint público), e o Learn não documenta a entrega do Event Grid num namespace com perímetro: teste antes de recomendar.
- Comparação de custo entre Standard e Premium só com a calculadora de preços, na data: as páginas de preço não mostram os valores fora dela.

### Referências
- [Service Bus Premium messaging](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-premium-messaging)
- [Azure Service Bus throttling](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-throttling)
- [Service Bus quotas and limits](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-quotas)
- [Network security for Azure Service Bus](https://learn.microsoft.com/azure/service-bus-messaging/network-security)
- [Configure IP firewall rules for Azure Service Bus](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-ip-filtering)
- [Migrate Service Bus namespaces from Standard to Premium](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-migrate-standard-premium)
- [What is a network security perimeter?](https://learn.microsoft.com/azure/private-link/network-security-perimeter-concepts)
- [Network Security Perimeter - Azure Service Bus](https://learn.microsoft.com/azure/service-bus-messaging/network-security-perimeter)
- [Use a message broker and events to integrate enterprise systems](https://learn.microsoft.com/azure/architecture/example-scenario/integration/queues-events)
- Pesquisa, perguntas 6 e 23; `docs/adr/0003-service-bus-standard-com-identidade-gerenciada.md`

### Transição
De volta ao slide de onde a pergunta veio: no 2, o custo do dia é quase todo esse Standard; no 21, a leitura 3.8 transforma esta conversa num ADR.

---

## FAQ-4 — FIFO, sessões e partições

**Tempo previsto:** 1 a 2 min, fora da soma das 4h (slide de bolso) · **Ganchos:** slides 12 e 14 · **Mostrar:** a linha do tempo do slide; se ajudar, depois do conserto, os `traces` da `ProcessarConfirmacaoComSessao` com `prop__SessionId` e `prop__SequenceNumber` (a consulta do Passo 7.4 do guia do Incidente 2).

### O que falar
O Service Bus entrega em ordem, mas a ordem é a de chegada à fila, não a do negócio. Sem sessões, o `SequenceNumber` garante a ordem de enfileiramento e de retirada, não a de processamento: com dois consumidores, ou com quatro chamadas em paralelo numa instância, como no nosso `host.json`, a mensagem 1 pode terminar depois da 3. Com sessões, quem aceita a sessão segura um lock exclusivo sobre todas as mensagens daquele `SessionId`: um consumidor por fatura, na ordem de chegada. Só que o banco mandou o "paga" antes do "registrada", e é nessa ordem que eles saem. Por isso a fatura desbaixou, e por isso o conserto do Incidente 2 tem máquina de estados: regra de negócio mora no consumidor. E partição? É outra coisa: serve para throughput e disponibilidade, espalhando a fila por vários brokers. Sem chave de partição, o envio é distribuído entre as partições e a ordem se perde; com sessão, o `SessionId` vira a chave. E o Learn desaconselha partição em baixo volume. Detalhe que pega: a mensagem que vai para a DLQ, a reprocessada e a agendada saem da vez na sessão. E, quando alguém disser "exactly once", desconfie: ponta a ponta, o que existe é at-least-once com consumidor idempotente.

### Insights do mundo real
- Ordem não importa e o processamento é idempotente: fila sem sessão, mais simples e com mais paralelismo.
- Ordem por chave em stream de altíssimo volume: partições do Event Hubs.
- Ordem de negócio que não depende da chegada, porque o parceiro pode mandar fora de ordem: máquina de estados com versão ou sequência no próprio dado, com ou sem sessão.
- Sessão e partição só se escolhem na criação da fila, e sessão não existe no Basic: decida no desenho. Em produção, escolha o `SessionId` pelo que precisa de ordem (por fatura, não por empresa cliente) e vigie a sessão "quente".
- As páginas do Learn divergem sobre "exactly once" (a comparação de serviços fala em "exactly once with sessions"; a comparação de filas diz que o termo "isn't exactly adequate"): não ponha a expressão em slide sem a ressalva.

### Referências
- [Message sessions](https://learn.microsoft.com/azure/service-bus-messaging/message-sessions)
- [Message sequencing and timestamps](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing)
- [Enable message sessions](https://learn.microsoft.com/azure/service-bus-messaging/enable-message-sessions)
- [Partitioned queues and topics](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-partitioning)
- [Compare messaging services](https://learn.microsoft.com/azure/service-bus-messaging/compare-messaging-services)
- [Compare Storage queues and Service Bus queues](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-azure-and-service-bus-queues-compared-contrasted)
- [Idempotent Consumer pattern](https://learn.microsoft.com/azure/architecture/patterns/idempotent-consumer)
- Pesquisa, pergunta 7

### Transição
Se a pergunta veio no slide 12, guardem: o 14 mostra a sessão funcionando e a desordem sobrevivendo a ela. Se veio no 14, sigam para a troca do consumidor.

---

## FAQ-5 — Duplicate detection × idempotência

**Tempo previsto:** 1 a 2 min, fora da soma das 4h (slide de bolso) · **Ganchos:** slides 9 e 14 · **Mostrar:** os três funis do slide; se ajudar, `stsaldaro<sufixo>` → Storage browser → Tables → `inbox` (uma linha por `idEvento`) e `idempotencia` (uma linha por `Idempotency-Key`).

### O que falar
Não é redundante, porque cada camada pega uma repetição diferente. A duplicate detection do Service Bus só descarta a cópia que o produtor enviar de novo com o mesmo `MessageId`, dentro da janela: 10 minutos por padrão, de 20 segundos a 7 dias. Nenhuma outra parte da mensagem é comparada, ela só se liga na criação da fila e pesa no throughput. E ela não impede o consumidor de processar duas vezes a mesma mensagem depois de uma reentrega. O padrão Idempotent Consumer do Architecture Center é direto: não troque a lógica idempotente do consumidor pela deduplicação do broker. No kit são três camadas, uma para cada repetição. A `Idempotency-Key` no POST, com insert condicional na tabela `idempotencia`, para o reenvio do ERP; o broker não serviria ali, porque o ERP precisa receber de volta o mesmo `faturaId`. O `MessageId = idEvento`, com janela de 10 minutos, para a confirmação repetida pelo banco. E a inbox, com a máquina de estados e o ETag, para a reentrega do Service Bus e para o que escapa da janela. Quem impede a baixa dupla, no fim, é a gravação condicional com ETag; a inbox é atalho e auditoria.

### Insights do mundo real
- Só duplicate detection: quando o efeito já é naturalmente idempotente (um upsert de estado absoluto) e o risco é só o produtor enviar de novo.
- Só idempotência no consumidor: quando a fila não pode ser recriada (a detecção é imutável) ou o throughput não comporta a janela.
- Nenhuma das duas: quando o efeito repetido é inofensivo e o custo do registro não se paga, como o próprio padrão admite.
- Em produção, grave a marca de deduplicação na mesma transação do efeito e guarde o registro pelo menos enquanto o broker puder reentregar, contando os reprocessamentos da DLQ. No kit, inbox e fatura ficam em tabelas diferentes, e a janela entre gravar a baixa e publicar o `FaturaBaixada` está comentada no código como caso para o padrão Outbox.
- Toda cópia legítima precisa de `MessageId` próprio: o retry agendado do Incidente 3 usa `{idEvento}-t{n}`, porque mensagem agendada também passa pela detecção.

### Referências
- [Duplicate detection](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection)
- [Enable duplicate message detection](https://learn.microsoft.com/azure/service-bus-messaging/enable-duplicate-detection)
- [Idempotent Consumer pattern](https://learn.microsoft.com/azure/architecture/patterns/idempotent-consumer)
- [Asynchronous Request-Reply pattern](https://learn.microsoft.com/azure/architecture/patterns/asynchronous-request-reply)
- [Designing Azure Functions for identical input](https://learn.microsoft.com/azure/azure-functions/functions-idempotent)
- [Message transfers, locks, and settlement](https://learn.microsoft.com/azure/service-bus-messaging/message-transfers-locks-settlement)
- Pesquisa, pergunta 8

### Transição
De volta ao slide de onde a pergunta veio: no 9, a `Idempotency-Key` é a primeira dessas camadas; no 14, é a pergunta para a turma ganhando a resposta.

---

## FAQ-6 — DLQ, maxDeliveryCount e retry

**Tempo previsto:** 1 a 2 min, fora da soma das 4h (slide de bolso) · **Ganchos:** slides 17 e 18 · **Mostrar:** a comparação do slide; se ajudar, o Service Bus Explorer da `sbq-confirmacoes-banco`, aba Dead-letter, com `MaxDeliveryCountExceeded` e Delivery Count 10.

### O que falar
Porque o Functions não oferece retry policy para o trigger de Service Bus. As retry policies existem para Timer, Event Hubs, Kafka e Cosmos DB; no Service Bus, o tratamento de mensagem envenenada não é configurável no Functions: quem decide é o broker. E o `clientRetryOptions` do `host.json` só refaz as chamadas ao Service Bus, não a execução da função. Já a reentrega do broker é imediata: abandonar a mensagem, ou deixar o lock expirar, devolve a mensagem para a frente da fila e soma uma entrega; passou do `maxDeliveryCount`, ela vai para a DLQ com `MaxDeliveryCountExceeded`, e isso não se desliga. Foi o que vimos no Incidente 3: dez tentativas em segundos contra uma falha de 45, e mensagem boa na DLQ junto com a envenenada. Então a espera é responsabilidade da aplicação: mandar uma cópia agendada, com `ScheduledEnqueueTime` e `MessageId` próprio, e completar a original. Para o erro permanente, o contrário: DLQ na primeira entrega, com motivo e descrição; o Learn recomenda registrar o tipo da exceção no motivo. O `maxDeliveryCount` baixo fica como rede de segurança para o erro que ninguém previu.

### Insights do mundo real
- Falha transiente curtíssima, de segundos, com efeito idempotente: deixe a reentrega imediata do Service Bus resolver, com um `maxDeliveryCount` adequado.
- Poucas mensagens para reprocessar, sem correção em lote: o Service Bus Explorer do Portal, que espia, edita e manda de volta para a fila.
- Fluxo de várias etapas, com espera longa e compensação: orquestração (Durable Functions ou Logic Apps), e não agendamento à mão.
- Em produção, some jitter ao atraso agendado (o kit usa atrasos fixos para a prova ser previsível) e trate a DLQ como fila de trabalho com dono: alerta em `DeadletteredMessages`, runbook e prazo.
- Contraste útil: o trigger de Storage Queue tem `visibilityTimeout`, que atrasa a nova tentativa, e manda a mensagem para a fila `-poison` depois de 5 tentativas.

### Referências
- [Service Bus dead-letter queues](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)
- [Message transfers, locks, and settlement](https://learn.microsoft.com/azure/service-bus-messaging/message-transfers-locks-settlement)
- [Message sequencing and timestamps](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing)
- [Azure Functions error handling and retry guidance](https://learn.microsoft.com/azure/azure-functions/functions-bindings-error-pages)
- [Azure Service Bus trigger for Azure Functions](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus-trigger)
- [Azure Service Bus bindings for Azure Functions](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus)
- [Duplicate detection](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection)
- [Azure Queue storage trigger for Azure Functions](https://learn.microsoft.com/azure/azure-functions/functions-bindings-storage-queue-trigger)
- Pesquisa, pergunta 9

### Transição
De volta ao slide 17 ou ao 18: no 17, é a hipótese que a gente prova a seguir; no 18, é o conserto que vocês ligam no Portal agora.

---

## FAQ-7 — Functions × Logic Apps × contêineres

**Tempo previsto:** 1 a 2 min, fora da soma das 4h (slide de bolso) · **Ganchos:** slide 3 · **Mostrar:** a árvore de decisão do slide; se ajudar, `func-saldaro-hub-<sufixo>` → Overview, com a lista de Functions (triggers de HTTP, Service Bus e Timer).

### O que falar
Porque o hub é código orientado a eventos, de alto volume e com regra de negócio: idempotência, máquina de estados, validação de assinatura. É o perfil do Functions, que o Learn chama de serviço de computação serverless: code-first, com triggers nativos de HTTP, Service Bus, Storage Queue e Timer, todos usados no kit. O Logic Apps é a plataforma serverless de integração por workflow: designer-first, com mais de 1.400 conectores, Enterprise Integration Pack para B2B e histórico de execução no Portal. Ele brilha quando o fluxo é desenhado com conectores ou tem gente no meio, como uma aprovação. E os dois se combinam: um workflow chama uma Function e vice-versa, e a arquitetura de referência de integração do Architecture Center usa APIM, Logic Apps, Service Bus e Event Grid juntos. Na árvore de decisão de computação, App Service é PaaS para web e APIs contínuas; Container Apps é serverless sobre Kubernetes, sem acesso à API do cluster; e AKS dá a API e o control plane, com a maior carga operacional. No kit, hub e banco simulado são Function Apps em .NET 10 isolated no Flex Consumption, e o Logic Apps aparece na leitura 3.8, como decisão de custo.

### Insights do mundo real
- Logic Apps: integração montada a partir de conectores SaaS ou B2B, aprovação humana, baixo volume, time que prefere designer.
- Container Apps: microsserviços em contêiner com imagem própria, dependências nativas ou GPU; e o próprio Functions roda no Container Apps quando o time quer o modelo de Functions com controle da imagem.
- App Service: API web síncrona e contínua, num plano dedicado. AKS: o time já opera Kubernetes e precisa da API do cluster.
- Em produção, não precisa ser um ou outro: Functions no processamento de alto volume e Logic Apps nos fluxos com conector e aprovação, os dois atrás do mesmo APIM.
- O desenvolvimento em C# no Logic Apps Standard ainda está em preview: fica fora das recomendações.

### Referências
- [Integration and automation platform options in Azure](https://learn.microsoft.com/azure/azure-functions/functions-compare-logic-apps-ms-flow-webjobs)
- [Choose an Azure compute service](https://learn.microsoft.com/azure/architecture/guide/technology-choices/compute-decision-tree)
- [Use a message broker and events to integrate enterprise systems](https://learn.microsoft.com/azure/architecture/example-scenario/integration/queues-events)
- [Azure Functions scale and hosting](https://learn.microsoft.com/azure/azure-functions/functions-scale)
- Pesquisa, pergunta 10

### Transição
De volta ao slide 3: o hub é código, e é por isso que hoje vocês leem C# e ligam toggles de cenário, em vez de desenhar workflow.

---

## FAQ-8 — Bicep × Terraform × Portal

**Tempo previsto:** 1 a 2 min, fora da soma das 4h (slide de bolso) · **Ganchos:** slides 2 e 9 · **Mostrar:** a comparação do slide; se ajudar, `rg-saldaro-lab` → Settings → Deployments (o que o Bicep aplicou) ao lado do `cenario-2-pronto.bicepparam` no VS Code.

### O que falar
Bicep porque o kit é só Azure. O Bicep é uma linguagem declarativa que vira template ARM no deploy, sem perder nada: todo tipo de recurso e versão de API do Azure está disponível desde o dia em que sai. O deploy é idempotente, tem módulos e what-if, e não há arquivo de estado para proteger: o estado mora no próprio Azure. A comparação oficial com o Terraform resume a diferença: o Terraform guarda o estado num `terraform.tfstate`, que precisa ser protegido e copiado, atende multicloud e on-premises por providers e processa o plano no cliente; o Bicep processa no Azure e lida melhor com mudança feita fora da ferramenta. Para o kit, o Terraform seria mais uma ferramenta e um arquivo de estado na máquina de cada aluno. E por que consertar no Portal, se o kit tem Bicep? Porque o Portal é ótimo para aprender onde cada peça mora e péssimo para recriar. Hoje vocês consertam clicando, e cada clique é drift: o próximo `deploy.ps1` volta ao que o `.bicepparam` declara. Por isso, depois de cada conserto, o arquivo passa a dizer o mesmo que o Azure, e o `deploy.ps1` fica como plano B. A leitura 3.7 fecha essa conta com o `check-drift.ps1`.

### Insights do mundo real
- Terraform vence em multicloud ou híbrido, em organização que já tem módulos, estado e processo em Terraform, e quando é preciso destruir uma configuração inteira como unidade.
- ARM JSON, só quando a ferramenta aceita apenas JSON: o Bicep gera o mesmo JSON.
- Portal vence para aprender, explorar e exportar um template como ponto de partida do IaC.
- Ambiente compartilhado em que mudança manual precisa ser bloqueada, e não só detectada: deployment stacks, com deny settings no plano de controle.
- Em produção, conserto de incidente feito no Portal volta ao código no mesmo dia; senão, o próximo deploy desfaz o conserto em silêncio.

### Referências
- [What is Bicep?](https://learn.microsoft.com/azure/azure-resource-manager/bicep/overview)
- [Comparing Terraform and Bicep](https://learn.microsoft.com/azure/developer/terraform/comparing-terraform-and-bicep)
- [Create and deploy Azure deployment stacks in Bicep](https://learn.microsoft.com/azure/azure-resource-manager/bicep/deployment-stacks)
- Pesquisa, pergunta 17; `docs/adr/0001-stack-dotnet-10-isolated-bicep-e-powershell-7.md` e `docs/adr/0008-portal-primeiro-com-bicep-como-plano-b.md`

### Transição
De volta ao slide de onde a pergunta veio: no 2, é a regra "Portal ao vivo, script como plano B"; no 9, é o drift que vocês acabaram de criar ao ligar o toggle.

---

## FAQ-9 — Durable × request-reply feito à mão

**Tempo previsto:** 1 a 2 min, fora da soma das 4h (slide de bolso) · **Ganchos:** slides 4 e 10 · **Mostrar:** a comparação do slide; se ajudar, a tabela do Passo 5.6 do guia do Incidente 1 (o Durable da D04 ao lado da Saldaro).

### O que falar
Porque o Durable entrega o mesmo padrão pronto, e aqui o objetivo é ver cada peça. O padrão Asynchronous Request-Reply do Architecture Center tem quatro: o 202 com `Location` e `Retry-After`; a fila; o status barato, que responde 200 enquanto processa e 303 para o recurso no fim; e a `Idempotency-Key`, que torna o reenvio inofensivo. No Durable, o `CreateCheckStatusResponse` devolve o 202 com o `statusQueryGetUri` no `Location`, e o status responde 202 enquanto a orquestração roda e 200 no fim. Funciona, mas tem dois detalhes: as URLs de gerenciamento da resposta carregam uma chave de acesso no parâmetro `code` e incluem terminate, raise event e purge; e o estado vive num Task Hub, com histórico e replay. Com uma etapa só, fila e status são mais simples e expõem menos superfície: a URL de status da Saldaro é só leitura. E o teto que empurra para esse padrão vale para os dois: 230 segundos para uma Function responder HTTP, em qualquer plano. O Durable vence quando o fluxo tem várias etapas com estado, como fan-out e fan-in, timers, aprovação humana e saga com compensação, ou quando dura horas ou dias e precisa de cancelamento e eventos externos prontos.

### Insights do mundo real
- Várias etapas com estado (fan-out/fan-in, timers, aprovação humana, saga com compensação): Durable.
- Fluxo longo, de horas ou dias, que precisa de status, cancelamento e eventos externos prontos: Durable.
- Consumidor que espera 202 enquanto processa, como a ação HTTP do Logic Apps ou o `CallHttpAsync` do Durable (no isolated, só com `asynchronousPatternEnabled` ligado): o status pendente precisa ser 202, e não o nosso 200.
- Em produção, escolha um dialeto do padrão e documente no contrato da API; com Durable, não entregue ao cliente externo as URLs de gerenciamento com a chave.

### Referências
- [Asynchronous Request-Reply pattern](https://learn.microsoft.com/azure/architecture/patterns/asynchronous-request-reply)
- [HTTP features in Durable Functions](https://learn.microsoft.com/azure/durable-task/durable-functions/durable-functions-http-features)
- [Azure Functions scale and hosting: limite de 230 s](https://learn.microsoft.com/azure/azure-functions/functions-scale#function-app-timeout-duration)
- [Azure Functions Flex Consumption plan hosting](https://learn.microsoft.com/azure/azure-functions/flex-consumption-plan)
- [Durable Task Scheduler](https://learn.microsoft.com/azure/durable-task/scheduler/durable-task-scheduler) (provedor de estado do Durable no Flex; a pesquisa não confirmou o GA por SKU, então não recomende sem conferir no Portal)
- Pesquisa, pergunta 18

### Transição
De volta ao slide de onde a pergunta veio: no 4, guardem a comparação para o fim do Incidente 1; no 10, ela fecha a prova.

---

## FAQ-10 — HMAC × mTLS × IP × OAuth

**Tempo previsto:** 1 a 2 min, fora da soma das 4h (slide de bolso) · **Ganchos:** slide 13 · **Mostrar:** a comparação do slide; se ajudar, `apim-saldaro-<sufixo>` → APIs → Saldaro Hub - webhooks (`saldaro-webhooks`, sem subscription), para mostrar onde um `ip-filter` ou um `validate-client-certificate` entrariam em produção.

### O que falar
Porque cada mecanismo responde uma pergunta diferente. HMAC prova que o corpo veio de quem tem o segredo e não foi alterado; mTLS prova quem abriu a conexão; lista de IPs reduz quem consegue chegar; OAuth prova quem chama, com um token que expira. A Microsoft usa HMAC em webhooks dela: os outgoing webhooks do Teams mandam um HMAC-SHA256 do corpo, e o receptor recalcula com o segredo e compara. O kit usa HMAC com o timestamp dentro do conteúdo assinado: só depende de um segredo compartilhado e cobre integridade e, com a janela de 300 segundos, repetição. As outras três defesas existem no APIM em todos os tiers, inclusive no Consumption: `ip-filter`, `validate-client-certificate` para mTLS e `validate-jwt` ou `validate-azure-ad-token` para OAuth. Mas cada uma cobra o seu preço. Lista de IPs sozinha é fraca: depende de o parceiro publicar IPs de saída estáveis. mTLS no Consumption exige ligar Request client certificate em Custom domains, não aceita certificados de CA para validar a cadeia e, atrás de um Application Gateway, o certificado nem chega ao APIM. OAuth exige que o banco consiga obter token. Por isso o ADR 0007 manda, em produção, somar à assinatura a lista de IPs ou o mTLS, conforme o contrato com o banco.

### Insights do mundo real
- mTLS: quando o parceiro ou a regulação exige; prefira um tier que aceite certificados de CA para validar a cadeia (o Consumption não aceita).
- Lista de IPs: quando o parceiro publica IPs de saída fixos; sempre como camada extra, nunca como única defesa, e como lista de permissão, não de bloqueio.
- OAuth (`validate-jwt`, `validate-azure-ad-token`): quando o parceiro consegue obter token, por exemplo com client credentials.
- Webhook enviado pelo Event Grid: endpoint protegido com Microsoft Entra ID.
- HMAC tem custos próprios: o segredo é compartilhado, e a rotação precisa ser combinada (aceite o atual e o anterior durante a troca). O Learn não tem um guia genérico de HMAC com timestamp para webhook de entrada, e o APIM não tem política pronta de validação HMAC: o exemplo mais próximo é o do Teams.

### Referências
- [Create an outgoing webhook (Teams)](https://learn.microsoft.com/microsoftteams/platform/webhooks-and-connectors/how-to/add-outgoing-webhook)
- [Authenticate event delivery in Azure Event Grid](https://learn.microsoft.com/azure/event-grid/security-authentication)
- [Mitigate OWASP API security top 10 in Azure API Management](https://learn.microsoft.com/azure/api-management/mitigate-owasp-api-threats)
- [ip-filter policy](https://learn.microsoft.com/azure/api-management/ip-filter-policy)
- [validate-client-certificate policy](https://learn.microsoft.com/azure/api-management/validate-client-certificate-policy)
- [validate-jwt policy](https://learn.microsoft.com/azure/api-management/validate-jwt-policy) e [validate-azure-ad-token policy](https://learn.microsoft.com/azure/api-management/validate-azure-ad-token-policy)
- [Secure APIs using client certificate authentication in API Management](https://learn.microsoft.com/azure/api-management/api-management-howto-mutual-certificates-for-clients)
- [Feature-based comparison of Azure API Management tiers](https://learn.microsoft.com/azure/api-management/api-management-features)
- Pesquisa, pergunta 19; `docs/adr/0007-http-triggers-anonymous-e-apim-sem-subscription.md`, item 4 de "O que mudaria em produção"

### Transição
De volta ao slide 13: a assinatura é a defesa que vocês ligam agora; as outras entram em produção, somadas a ela.
