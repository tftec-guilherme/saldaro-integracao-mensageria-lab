# Checklist do primeiro deploy real do kit

| Campo | Valor |
|---|---|
| Version-anchor | Q4-2026 |
| Data | 2026-10-14 |
| Para quem | O professor, antes de liberar o kit para a turma |
| Onde | Uma assinatura Free Trial ou sandbox em que você seja **Owner**. O primeiro deploy real rodou numa assinatura Sponsorship da TFTEC; a cota do Flex da Free Trial fica para conferir numa Free Trial de verdade (seção 0) |
| Tempo | ~4 h para tudo, boa parte esperando deploy e alerta (Lab 0 ~30 min; Incidentes 1 a 3 ~1h30; 3.1, 3.5, 3.6, 3.7 e 3.8 ~2h) |
| Custo | ~R$ 2 a 3 se o `rg-saldaro-lab` for apagado no fim (preços de lista, ~R$ 5,50/US$) |

Empresa, pessoas, números e incidentes são fictícios.

**Resultado do primeiro deploy real (2026-10-14).** O roteiro rodou numa assinatura Sponsorship da TFTEC em que a conta era Owner, na região `brazilsouth`, com os dois Function Apps no Flex Consumption. As seções 0 a 12 foram executadas, e o ambiente foi apagado e purgado no fim. Cada caixa marcada traz o resultado real na mesma linha e, quando ele contrariou o esperado, o que aconteceu e onde o material foi corrigido. Ficam em aberto:

- o custo real no Cost Management, que só sai no dia seguinte (seção 12);
- a cota do Flex numa Free Trial de verdade;
- os pontos da seção 13 ainda marcados "a conferir".

A tabela da seção 13 ganhou a coluna "Primeiro deploy real".

Até esse deploy, nenhum agente que produziu o kit tinha feito deploy no Azure. O que foi verificado localmente: `dotnet build` (0 avisos) e `dotnet test` da `03_Aplicações/Saldaro.sln` (161 testes no hub, 152 no banco e 111 nas ferramentas), `az bicep build`, `az bicep lint` e `az bicep build-params` dos oito `.bicepparam` sem aviso, os scripts sem erro de parse e com `Get-Help` legível, e os nomes de guias, slides e notes conferidos contra o código, o Bicep e os scripts. Este roteiro cobre o resto: a ordem exata dos comandos, o que observar em cada cenário e os pontos que só o Azure responde. Marque cada caixa e anote os tempos reais, que voltam para os guias e as notes.

## 0. Antes de começar

> **Aviso sobre a assinatura.** A conta usada nesta máquina para produzir o kit **não tem `Microsoft.Authorization/roleAssignments/write` na assinatura padrão dela**: ali o `deploy.ps1` falha já no what-if (`Authorization failed ... roleAssignments/write`), porque o Bicep cria role assignments. Antes de qualquer comando, troque para uma assinatura em que você seja Owner e confira.

```powershell
az login
az account list -o table
az account set --subscription "<nome ou id da assinatura de teste>"
az account show --query "{assinatura:name, usuario:user.name}" -o table
$eu = az ad signed-in-user show --query id -o tsv
az role assignment list --assignee $eu --scope "/subscriptions/$(az account show --query id -o tsv)" --query "[].roleDefinitionName" -o tsv
```

- [x] A lista de roles mostra **Owner** (ou **User Access Administrator** junto com **Contributor**). **Resultado:** Owner na assinatura Sponsorship.
- [x] `az group exists --name rg-saldaro-lab` responde `false` (nenhuma sobra de teste nessa assinatura). **Resultado:** o resource group não existia (o `-SomenteValidar` da seção 1 pulou o what-if por isso).
- [x] Ferramentas: `pwsh --version` (7.x), `dotnet --list-sdks` (10.0.x), `az version` e `az bicep version`. O Azure Functions Core Tools não é necessário. **Resultado:** pwsh 7.6.6, .NET SDK 10.0.401 e Bicep 0.48.1.
- [ ] Verificação local, sem Azure, a partir da raiz do repositório, com os comandos abaixo. **Não conferido no primeiro deploy real:** o registro da execução não traz essa rodada; vale a verificação local do parágrafo do topo, e o `dotnet publish` do `deploy.ps1` compilou os dois apps.

```powershell
dotnet build 03_Aplicações/Saldaro.sln
dotnet test 03_Aplicações/Saldaro.sln
az bicep build --file infra/main.bicep --stdout > $null
az bicep lint --file infra/main.bicep
```

- [x] O Flex Consumption existe na região com .NET 10: `az functionapp list-flexconsumption-runtimes --location brazilsouth --runtime dotnet-isolated -o table` lista a versão `10.0` (o `deploy.ps1` confere de novo). **Resultado:** a lista traz a versão 10 (fim do suporte em 2028-11-10), além da 9 e da 8.
- [ ] Cota do Flex: no Portal, **Subscriptions → Usage + quotas**, filtre por Flex Consumption na região (rótulo a confirmar) e anote a cota da assinatura Sponsorship; o kit usa até ~13 núcleos no pior caso. A cota da Free Trial, de 15 núcleos por região, não aparece numa Sponsorship: fica como ponto a conferir numa Free Trial de verdade (ponto 7 da seção 13). **Não conferido no primeiro deploy real:** a tela **Usage + quotas** não foi aberta; na rajada de 1.000 faturas do Incidente 1 não houve sinal de cota (seção 4).
- [x] Escolha um sufixo (4 a 6 letras minúsculas ou dígitos) e uma caixa de e-mail real para os alertas do 3.6. Anote os dois. **Resultado:** sufixo de seis caracteres escolhido; o e-mail dos alertas é o da seção 9.

## 1. Validação sem criar nada

```powershell
pwsh ./scripts/deploy.ps1 -Cenario 0 -Sufixo <sufixo> -SomenteValidar
```

- [x] O script mostra a assinatura ativa, confere os nomes globais (Storage, Service Bus, APIM, Key Vault, Function Apps) e o Flex na região, avisa que o resource group não existe e que o what-if foi pulado, e termina com "Validação concluída: nada foi criado, alterado ou publicado." **Resultado:** 35 s, código de saída 0, what-if pulado porque o resource group não existia. O `-SomenteValidar` não pega erro de avaliação de saída do Bicep, como o da seção 3: esse só aparece no deploy.
- [x] Logo depois, `az group exists --name rg-saldaro-lab` continua `false`. **Resultado:** nada foi criado.

## 2. Ordem dos comandos

A ordem segue a aula: o Lab 0, os três incidentes ao vivo e, depois, os guias de casa. A tabela mostra o estado que cada comando aplica; as seções seguintes dizem o que observar.

| # | Comando | Estado aplicado | Seção |
|---|---|---|---|
| 1 | `deploy.ps1 -Cenario 0 -Sufixo <sufixo>` | `cenario-0` (baseline) + smoke test | 3 |
| 2 | `deploy.ps1 -Cenario 2`, conserto pelo Portal e, no fim, `deploy.ps1 -Cenario 2 -Pronto` | `cenario-0` → `cenario-2-pronto` | 4 |
| 3 | `deploy.ps1 -Cenario 3`, edição do `.bicepparam`, `deploy.ps1 -Cenario 3 -SemPublicar` (precisa parar), fila apagada no Portal e de novo; plano B `-Cenario 3 -Pronto` | `cenario-2-pronto` → `cenario-3-pronto` | 5 |
| 4 | `deploy.ps1 -Cenario 4`, toggles no Portal e `deploy.ps1 -Cenario 4 -Pronto -SemPublicar` | `cenario-3-pronto` → `cenario-4-pronto` | 6 |
| 5 | `deploy.ps1 -Cenario 1` (precisa parar: mudança imutável), `deploy.ps1 -Cenario 1 -Pronto` | `cenario-1-pronto` | 7 |
| 6 | `deploy.ps1 -Cenario 4 -Pronto` (recria a fila com sessions), `deploy.ps1 -Cenario 5`, `deploy.ps1 -Cenario 5 -Pronto` | `cenario-4-pronto` → `cenario-5-pronto` | 8 |
| 7 | `deploy.ps1 -Cenario 6`, `deploy.ps1 -Cenario 6 -Pronto -EmailAlerta <e-mail>` | `cenario-4-pronto` → `cenario-6-pronto` | 9 |
| 8 | `deploy.ps1 -Cenario 7`, `deploy.ps1 -Cenario 7 -Pronto -SomenteValidar`, `deploy.ps1 -Cenario 7 -Pronto` | `cenario-5-pronto` → `cenario-7-pronto` | 10 |
| 9 | `rajada` numa fila descartável `sbq-rajada` | sem deploy | 11 |
| 10 | `limpar.ps1` | nada | 12 |

Todos os comandos rodam na raiz do repositório, com `pwsh`. Depois do primeiro deploy, o `-Sufixo` é opcional: o script reaproveita o do ambiente (e do `scripts/.env.local`).

## 3. Lab 0 (cenário 0)

```powershell
pwsh ./scripts/deploy.ps1 -Cenario 0 -Sufixo <sufixo>
```

- [x] Tempo total do primeiro deploy (anote; o guia diz de 15 a 20 min, com 5 a 15 min só do `az deployment group create`). **Resultado:** ~7 min (7,1) na segunda tentativa; a primeira falhou aos 6,3 min, num bug da saída `principalId` corrigido no Bicep (caixa do deployment, logo abaixo). Um deploy limpo deve levar de 8 a 10 min, e é o que o Lab 0 e o Incidente 1 dizem agora.
- [x] O what-if aparece em `ResourceIdOnly` (só a lista de recursos e o tipo de mudança) e o script segue sem pedir confirmação. **Resultado:** 41 recursos a criar na primeira tentativa; 11 a criar e 30 a reaplicar na segunda.
- [x] O deployment passa de primeira. Se falhar, anote o recurso: os candidatos são a event subscription `evgs-notificar-parceiro` (entrega com a identidade do tópico logo depois do role assignment, que pode não ter propagado), o pacote do Flex (`functionAppConfig.deployment.storage` com `StorageAccountConnectionString` e `storageAccountConnectionStringName = AzureWebJobsStorage`), o APIM ainda ativando e o logger do APIM. Rodar de novo é seguro (idempotente). **Resultado:** não passou, por um motivo fora da lista: o deployment `app-banco` falhou com `DeploymentOutputEvaluationFailed` ("The language expression property 'identity' doesn't exist"), porque o banco simulado sobe sem identidade e a saída `principalId` lia `identity` direto. Corrigido em `infra/modules/function-app.bicep` (`app.?identity.?principalId ?? ''`); nem o `-SomenteValidar` nem o `az bicep lint` pegam esse erro, só o deploy. Os resource providers que faltavam na assinatura (`Microsoft.ApiManagement` e `Microsoft.EventGrid`) o próprio `az deployment group create` registrou; a linha do troubleshooting do Lab 0 ficou para quem cria recursos por outro caminho.
- [x] O `config-zip` publica os dois apps (o script tenta de novo depois de 30 s se o app recém-criado ainda não aceitar). No Portal, o hub lista dez Functions e o banco, quatro. **Resultado:** dez Functions no hub e quatro no banco.
- [x] O smoke test termina com a fatura em `baixada` ou `notificada` em até 120 s (no cold start, `pwsh ./scripts/smoke-test.ps1 -TimeoutSegundos 300`) e imprime `faturaId`, `referenciaErp`, tempo total e o `operation_Id` do POST. **Resultado:** terminou em 29,8 s.
- [x] `scripts/.env.local` tem as doze chaves `SALDARO_*` e nenhum segredo. **Resultado:** doze chaves, nenhum segredo.
- [x] O RG tem os recursos da tabela do Lab 0, com as tags `projeto = saldaro-hub` e `ambiente = lab`; o Key Vault está sem purge protection. **Resultado:** as duas tags em todos os recursos; Key Vault sem purge protection.
- [x] APIM: duas APIs, **Saldaro Hub - faturas** e **Saldaro Hub - webhooks**, com as operações pelo nome do contrato. O roteamento `saldaro` × `saldaro/webhooks` funciona: o webhook do banco chega ao hub pelo APIM (o smoke test só passa se chegar) e o `requests` do APIM mostra a operação `post-webhook-banco`. **Resultado:** **Saldaro Hub - faturas** (path `saldaro`: `get-fatura`, `get-fatura-status` e `post-fatura`) e **Saldaro Hub - webhooks** (path `saldaro/webhooks`: `post-webhook-banco`); o roteamento funciona.
- [x] Application Insights: **Investigate → Search** (antigo Transaction search) e a consulta por `faturaId` mostram a jornada. **Resultado:** a barra do Search traz **View in Logs** e **Leave Preview**, e os resultados, **View as: Traces**; o `faturaId` do smoke test trouxe 113 itens (13 traces), e o **End-to-end transaction details** mostra APIM → hub → tabela → banco. O Lab 0 (Passos 9.1 a 9.3) foi atualizado. Confira as chaves que o diagnóstico do APIM grava, usadas pelo `kql/429-por-cliente.kql`:

```kusto
requests
| where isnotempty(tostring(customDimensions["Service ID"]))
| take 5
| project name, resultCode, customDimensions
```

- [x] Aparecem `Service ID`, `Subscription Name` e `Request-x-cliente-id` (se os nomes forem outros, ajuste o `kql/429-por-cliente.kql` e o guia 3.1). **Resultado:** conferido na seção 7: as três chaves e também a `Response-Retry-After`. A consulta não mudou; só o comentário do `kql/429-por-cliente.kql` foi atualizado.
- [x] `az storage entity query --account-name stsaldaro<sufixo> --table-name faturas --auth-mode login` funciona logo depois do deploy (propagação da role do aluno). **Resultado:** funcionou logo depois do deploy.

## 4. Incidente 1, timeout (cenário 2)

```powershell
pwsh ./scripts/deploy.ps1 -Cenario 2
./scripts/erp-simulado.ps1 -Faturas 1 -MaxTentativas 2 -Caos "latencia=45"
```

- [x] O what-if do `-Cenario 2` só mostra o ruído conhecido (é o mesmo `cenario-0`). **Resultado:** deploy em 6,7 min; o what-if listou os 41 recursos como *to deploy*, porque no `ResourceIdOnly` um recurso existente aparece como **Deploy** sem dizer se alguma propriedade muda. Nesse formato não dá para separar o ruído.
- [x] As duas tentativas voltam **504** em ~30 s, com o corpo `{"erro":"o gateway esperou 30 s e desistiu"}`. É a prova de que o `on-error` com `&&` no `rawxml` funciona. Se vier 500, o `when` não casou: veja no trace do APIM o `context.LastError` (Source e Reason). **Resultado:** 504 nas duas, ~30 s cada (61 s no total); direto no APIM, o corpo exato veio em 30,3 s. O `on-error` com `&&` funciona.
- [x] O relatório do ERP mostra `504|504` e `faturaId` `(nenhum)`; o CSV em `scripts/saida/` tem as colunas do contrato. **Resultado:** como esperado.
- [x] Depois de 2 a 5 min, o `kql/faturas-por-referencia-erp.kql` mostra a `referenciaErp` com **2 faturas e 2 cobranças**: o baseline não desiste quando o ERP desiste e grava a cobrança aos 45 s. O `request` `PostFatura` dura ~45 s. No banco, `GET {SALDARO_BANCO_URL}/cobrancas/{faturaId}` dá `quantidade = 1` em cada `faturaId`. **Resultado:** 2 faturas e 2 cobranças; o `PostFatura` levou 47,2 s e 45,3 s, com `resultCode` 499 (o APIM fechou a conexão, e o hub terminou o trabalho); `quantidade = 1` em cada `faturaId`. O 499 entrou no Incidente 1 (Passos 2.2 e 3.1).
- [x] Conserto ao vivo: Portal → `func-saldaro-hub-<sufixo>` → **Settings → Environment variables** → `Cenario__AsyncRequestReply = true` → **Apply** e confirmar. Anote quanto tempo o app leva para reiniciar e o `/api/saude` mostrar `"AsyncRequestReply": true`. **Resultado:** feito por `az`, o equivalente ao **Apply**: setting gravada em 16 s e `"AsyncRequestReply": true` no `/api/saude` aos 34 s. Os rótulos **Apply** e **Confirm** do Portal não foram conferidos.
- [x] Prova: 202 em < 500 ms com `-Caos "latencia=60"`, status 200 até virar 303 (em ~65 s); `./scripts/erp-simulado.ps1 -Faturas 1000 -Paralelismo 20 -IdempotencyKey` sem nenhum 5xx; a KQL vazia; `quantidade = 1` por fatura. Anote quanto a fila leva para drenar e se a escala do Flex esbarra na cota. **Resultado:** a primeira chamada depois do reinício levou 2,2 s (frio) e as seguintes, 449, 191 e 94 ms; o 303 veio entre 77 e 87 s, não em ~65 s (o Incidente 1 diz agora 75 a 90 s). Rajada: 1.000 × 202, nenhum 5xx nem 429, p50 de 220 ms, p95 de 639 ms, máximo de 8,4 s e 47 s no total; filas drenadas ~2 min depois do início, sem DLQ e sem sinal de cota do Flex; KQL de duplicatas vazia; `quantidade = 1` nas amostras.
- [x] Plano B: `pwsh ./scripts/deploy.ps1 -Cenario 2 -Pronto` chega ao mesmo estado (e um `-Cenario 2` sem `-Pronto` desfaria a mudança feita no Portal). **Resultado:** coberto pelo `-Cenario 3`, que aplica o mesmo `cenario-2-pronto.bicepparam` (`asyncRequestReply` ligado), em 6 min; o `-Cenario 2 -Pronto` em si não rodou.

## 5. Incidente 2, webhook duplicado e fora de ordem (cenário 3)

```powershell
pwsh ./scripts/deploy.ps1 -Cenario 3
./scripts/erp-simulado.ps1 -Faturas 20 -Caos "duplicar=3;embaralhar=true"
```

- [x] Baseline: 120 webhooks, `kql/baixa-duplicada.kql` com linhas (3 `fatura.baixada` por fatura) e faturas de volta a `registrada` com `baixadaEm` preenchido. O status final `registrada` depende de o Event Grid entregar em poucos segundos: anote o que aconteceu. **Resultado:** 121 webhooks no hub (os 120 do banco e o forjado), todos 202; o `kql/baixa-duplicada.kql` listou as 20 faturas; no fim, 18 `notificada` e só 2 `registrada`, uma delas a do POST forjado. O Incidente 2 fala agora em 2 de 20, não em "muitas".
- [x] O POST forjado sem assinatura em `{SALDARO_APIM_URL}/webhooks/banco` volta 202 no baseline. **Resultado:** 202; a `quantidadeBaixas` da fatura foi de 3 para 4 em 25 s, e ela voltou a `registrada`, e não a "baixada ou notificada", como o Passo 2.5 dizia (corrigido no Incidente 2).
- [x] Portal: na `sbq-confirmacoes-banco`, **Settings → Properties** mostra "Enable sessions" e "Enable duplicate detection" desabilitados (print para a aula). Anote o texto do erro de `az servicebus queue update ... --enable-session true`. **Resultado:** **Properties** virou uma página genérica de metadados do ARM ("This view has been generated from known ARM metadata", com Id, Name, Type, Tags e Location); **Sessions** e **Duplicate detection** aparecem em **Overview → Essentials**, como **Disabled**. O Lab 0 e o Incidente 2 apontam agora o Overview. Erro do `queue update`: `(MessagingGatewayBadRequest) SubCode=40000. The value for the RequiresSession property of an existing Queue cannot be changed.`
- [x] Edite `infra/parameters/cenario-2-pronto.bicepparam` (`filaComSessions = true` e `filaDupDetection = true`) e rode `pwsh ./scripts/deploy.ps1 -Cenario 3 -SemPublicar`: o script **para antes do what-if**, explicando que a mudança é imutável. Drene e apague a fila no Portal e rode de novo: a fila volta com as duas propriedades. **Resultado:** parou no passo 11, em 49 s, com a mensagem de propriedade IMUTÁVEL (0 ativas, 0 na DLQ e o plano B) e o aviso esperado de `workerComSessao = false`. Com a fila apagada (por `az`, não pelo Portal), o deploy seguinte a recriou com sessions e duplicate detection em 3,3 min (what-if: 1 to create, 40 to deploy).
- [x] Ligue `Cenario__WebhookHmac`, `Cenario__Inbox` e troque os dois `AzureWebJobs.ProcessarConfirmacao*.Disabled` no Portal (ou `-Cenario 3 -Pronto`). **Resultado:** feito por `az`, num só `appsettings set`: gravado em 10 s e refletido no `/api/saude` aos 18 s.
- [x] Prova: `kql/baixa-duplicada.kql` vazio, `quantidadeBaixas = 1`, as 20 faturas `notificada`, duas linhas de inbox por fatura e o POST forjado com 401. Os logs dos workers trazem `SessionId`, `SequenceNumber` e `DeliveryCount` em `customDimensions`. Anote quanto a fila leva para drenar com sessions (`sessionIdleTimeout` de 10 s). **Resultado:** POST forjado com 401; as 20 faturas `notificada` com `quantidadeBaixas = 1` (19 delas aos 51 s); KQL vazia; duas linhas de inbox por fatura (`cobranca.registrada` e `cobranca.paga`); 40 execuções do `ProcessarConfirmacaoComSessao`, sem falha (a duplicate detection descartou 80 das 120 cópias). As chaves vêm com o prefixo `prop__` (`prop__SessionId`, `prop__SequenceNumber` e `prop__DeliveryCount`), e o `has` não acha pedaço de nome camelCase: use `contains`. O Passo 7.4 do Incidente 2 foi corrigido.
- [ ] Service Bus Explorer numa fila com sessions: o Learn diz que não há suporte. Anote o que o Portal faz (peek, mensagem de erro) e se `SessionId` e a propriedade `tipo` aparecem em **Message Properties** antes da recriação. **Não conferido no primeiro deploy real:** o registro não traz o peek da fila ativa nem as **Message Properties** antes da recriação. Na seção 6, o peek da DLQ de uma fila com sessions funcionou.
- [x] Teste também o plano B num segundo momento: `-Cenario 3 -Pronto` recria a fila sozinho, com aviso. **Resultado:** o `-Cenario 3 -Pronto` em si não rodou, mas o mesmo caminho do `-Pronto` (apagar a fila e deixar o Bicep recriá-la) foi validado duas vezes: no `-Cenario 1 -Pronto` (seção 7) e no `-Cenario 4 -Pronto` (seção 8).
- [x] No fim: `git restore infra/parameters/cenario-2-pronto.bicepparam`. **Resultado:** feito.

## 6. Incidente 3, mensagem envenenada (cenário 4)

```powershell
pwsh ./scripts/deploy.ps1 -Cenario 4
./scripts/erp-simulado.ps1 -Faturas 10 -Caos "valorComVirgula=true"
./scripts/erp-simulado.ps1 -Faturas 20 -Caos "falhaTransiente=50"
```

- [x] Baseline: DLQ com ~30 mensagens, todas `MaxDeliveryCountExceeded` (`DeliveryCount` 10), ~300 abandonos em dezenas de segundos. **Resultado:** `-Cenario 4` em 5,9 min; DLQ com 32, todas `MaxDeliveryCountExceeded` com 10 entregas (20 de valor em texto e 12 transientes); 204 abandonos em menos de 2 min. A primeira rajada levou 106,9 s, com 6 timeouts de 35 s do ERP que nem chegaram ao APIM, ainda frio logo depois do deploy: os três incidentes ganharam o aquecimento (`Aquecer-Ambiente`) depois de cada deploy.
- [x] Em `exceptions`: os tipos do hub (`ConfirmacaoInvalidaException`, `FalhaTransienteSimuladaException`, `BancoIndisponivelException`) aparecem como tipo, ou só uma linha genérica do host? A contagem dobra? Ajuste o texto dos Passos 3.1 do guia e o `kql/erros-transientes-por-operacao.kql` se preciso. **Resultado:** aparecem como `type`, mas cada exceção vem em duas linhas: a do host, sem `cloud_RoleName` (a mais numerosa), e a do worker, com o nome do Function App (ex.: `ConfirmacaoInvalidaException`, 600 linhas do host e 134 do worker). A contagem não é uma por falha: o `kql/erros-transientes-por-operacao.kql` e o Incidente 3 filtram por `cloud_RoleName`.
- [x] `./scripts/triagem-dlq.ps1` mostra a tabela agrupada por motivo e a contagem que o namespace informa. **Resultado:** a tabela e a contagem do namespace batem (32); 149 s no primeiro uso (o `dotnet run` compila) e de 134 a 145 s nos seguintes. O Incidente 3 roda a triagem num terminal só dela.
- [x] Service Bus Explorer: o peek da DLQ de uma fila com sessions funciona? Se não, a triagem é o caminho (o guia já aponta). **Resultado:** funciona: aba **Dead-letter (32)**, **Peek Mode** e **Peek from start**, com as colunas Delivery Count, Session ID, Dead Letter Reason e Dead Letter Error Description ("Message could not be consumed after 10 delivery attempts.").
- [x] Conserto: `Cenario__DlqComMotivo` e `Cenario__RetryAgendado` no Portal e `pwsh ./scripts/deploy.ps1 -Cenario 4 -Pronto -SemPublicar`. O script diz que sessions e duplicate detection já estão como o estado pedido e **não recria a fila** (a DLQ continua lá). Confira: `az servicebus queue show -g rg-saldaro-lab --namespace-name sb-saldaro-<sufixo> -n sbq-confirmacoes-banco --query maxDeliveryCount` dá 3. **Resultado:** toggles por `az` em 8 s; `-Cenario 4 -Pronto -SemPublicar` em 3,6 min; `maxDeliveryCount` 3; a fila não foi recriada (a DLQ continuou com 32).
- [x] As mesmas duas rajadas: 20 `SchemaInvalida` na primeira entrega (`DeliveryCount` 1), `mensagem.retry.agendado` com tentativas 1 a 3 (10, 20 e 40 s), mensagens agendadas acima de zero por ~70 s e nenhuma `TentativasEsgotadas`. **Resultado:** depois do aquecimento (a primeira sonda 404 em 1,9 s, as outras em ~130 ms), as rajadas saíram limpas. A DLQ foi de 32 a 52 em ~20 s (20 `SchemaInvalida` na primeira entrega); `mensagem.retry.agendado` só nas tentativas 1 (10 s) e 2 (20 s), 11 de cada, sem tentativa 3; agendadas acima de zero de ~22 a ~55 s (pico de 11) e zeradas na leitura dos 71 s; 20 `mensagem.deadletter` e nenhuma `TentativasEsgotadas`. O Incidente 3 diz agora tentativa 2 e ~35 a 50 s.
- [x] `./scripts/triagem-dlq.ps1 -Reprocessar` zera a DLQ; no fim, 60 faturas e 60 baixadas. **Resultado:** 52 reprocessadas (40 com valor corrigido e 12 sem correção), nenhuma descartada ou com falha; DLQ zerada em 1 min; no fim, 60 faturas, todas `notificada` e com `quantidadeBaixas = 1`. A KQL de `fatura.baixada` atrasa alguns minutos depois do reprocessamento.
- [x] `./scripts/triagem-dlq.ps1 -Fila sbq-faturas-recebidas`: com `falhaTransiente=50`, ~2% das faturas por rajada podem ter esgotado as 10 entregas na fila de faturas. Anote se aconteceu. **Resultado:** não aconteceu: a DLQ da `sbq-faturas-recebidas` ficou com 0.

## 7. Leitura 3.1, rate limit (cenário 1)

```powershell
pwsh ./scripts/deploy.ps1 -Cenario 1
pwsh ./scripts/deploy.ps1 -Cenario 1 -Pronto
```

- [x] Vindo do `cenario-4-pronto`, o `-Cenario 1` **para**: a fila tem sessions e o 3.1 começa no baseline (é o caso do aluno que mantiver o ambiente da aula; o guia 3.1 tem a linha de troubleshooting). O `-Cenario 1 -Pronto` recria a fila sem sessions e liga as subscriptions e o `rate-limit`. **Resultado:** o `-Cenario 1` parou em 29 s (sessions de true para false), com 0 ativas e 0 na DLQ, mas a mensagem falava em "Caminho didático (3.3)": agora o `deploy.ps1` só fala em caminho didático no `-Cenario 3`. O `-Cenario 1 -Pronto` levou 6 min: apagou a fila vazia, o Bicep a recriou sem sessions (what-if: 3 to create) e o script avisou que a API `saldaro` passa a exigir subscription (`cliente-a` e `cliente-b`). Nesse estado o `asyncRequestReply` está desligado, e o POST volta a 201 síncrono.
- [x] `./scripts/erp-simulado.ps1 -Faturas 200 -Cliente cliente-a -ReqPorMin 300 -SemBackoff` e, em outro terminal, `./scripts/erp-simulado.ps1 -Faturas 20 -Cliente cliente-b -ReqPorMin 10`: o script detecta o 401 (anote o texto, que precisa conter "subscription key"), busca a chave com `az rest` no `listSecrets` e passa a enviar `Ocp-Apim-Subscription-Key`. **Resultado:** o 401 diz "Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API."; a chave veio pelo `az rest`, mascarada na tela.
- [x] O `cliente-a` leva 429 a partir da 61ª chamada do minuto (aproximado no Consumption), com `Retry-After` (anote o valor); o `cliente-b` não leva nenhum. **Resultado:** 56 × 201 antes do bloqueio e 723 × 429 em 40 s (144 faturas terminaram em 429); `Retry-After` de 48 s na amostra; o `cliente-b` fez 20 × 201 em 114 s, sem nenhum 429.
- [x] `kql/429-por-cliente.kql` separa por `Subscription Name` e por `Request-x-cliente-id`. O Portal lista também a "Built-in all-access subscription". **Resultado:** `cliente-a` com 779 chamadas e 723 × 429 (92,8%); `cliente-b` com 0 × 429. A lista de subscriptions do Portal não foi conferida.
- [x] Com backoff, 200 faturas terminam sem erro (anote o tempo; o guia estima 3 a 4 min). **Resultado:** não terminaram: em 3,1 min, 194 × 201 e 6 faturas em 429 (147 × 429 no total), porque o teto de 30 s de espera fica abaixo do `Retry-After` de ~48 s. A 3.1 manteve o teto e transformou o resultado na lição do Passo 4.5.

## 8. Leitura 3.5, identidade gerenciada e Key Vault (cenário 5)

```powershell
pwsh ./scripts/deploy.ps1 -Cenario 4 -Pronto
pwsh ./scripts/deploy.ps1 -Cenario 5
pwsh ./scripts/deploy.ps1 -Cenario 5 -Pronto
```

- [x] O `-Cenario 4 -Pronto` recria a fila com sessions (volta ao estado do fim da aula); o `-Cenario 5` não muda nada. **Resultado:** vindo do `cenario-1-pronto`, o `-Cenario 4 -Pronto` levou 7,2 min, apagou a fila sem sessions e o Bicep a recriou com sessions; o `-Cenario 5` levou 10,6 min e manteve o mesmo estado.
- [x] A quebra do guia (regenerar as chaves SAS "às 3h") derruba o baseline com 401. **Resultado:** derrubou, mas de fora não se vê 401: o smoke test falhou já no `POST /faturas`, com 500, e o ERP (3 faturas, 15 tentativas) levou 8 × 500 e 7 × 504. O Passo 2.3 da 3.5 diz isso agora; o texto das exceptions, onde o 401 aparece, não foi anotado.
- [x] O `-Cenario 5 -Pronto` espera as Key Vault references do hub: o endpoint `config/configreferences/appsettings` responde e o script diz "Key Vault references do hub resolvidas" (ou reinicia o app). No Portal, **Environment variables** mostra a coluna Source como Key vault. **Resultado:** 5,9 min, com as references resolvidas dentro do próprio deploy ("Key Vault references do hub resolvidas (BancoApiKey, WebhookSecret)"); a coluna Source do Portal não foi conferida. Depois da migração, porém, o worker com sessão ficou parado: o smoke test parou em `cobranca-emitida`, as confirmações ficaram na `sbq-confirmacoes-banco` e o log não mostrou erro nenhum. Os triggers do Service Bus subiram antes de as roles da identidade valerem e não se recuperaram sozinhos; um restart do hub bastou, mesmo sem o `Reader`. Corrigido no `deploy.ps1` (restart do hub depois que as references resolvem) e explicado na 3.5 (Passo 4.5 e Troubleshooting); o `Reader` entrou no `roles-hub.bicep` como recomendação de escala.
- [x] `az servicebus namespace show` dá `disableLocalAuth: true`; o Overview do namespace mostra Local Authentication desabilitado; `az functionapp config appsettings list` não tem connection string nem chave, exceto `AzureWebJobsStorage` (compromisso documentado). **Resultado:** `disableLocalAuth = true`; o inventário de settings antes e depois da migração bateu com a 3.5, com chave só no `AzureWebJobsStorage`. O rótulo do Overview do namespace não foi conferido.
- [x] Regenerar as chaves SAS de novo não quebra nada; a identidade do hub tem as seis roles (com `Reader` no namespace); o Service Bus Explorer funciona com **Settings → Microsoft Entra ID**. **Resultado:** com as chaves SAS e as do tópico regeneradas e o hub reiniciado, o smoke test chegou a `baixada` em 16,9 s, o ERP teve 3 × 202 e a consulta de exceptions voltou vazia desde o início da prova. A identidade tinha cinco roles, porque o `Reader` só entrou no Bicep depois, com o conserto do worker; a sexta role fica para a seção 10 (o `-Cenario 7` aplica o `cenario-5-pronto`). O Service Bus Explorer com Microsoft Entra ID não foi conferido.
- [x] Os eventos do Key Vault (`SecretNewVersionCreated`, `SecretNearExpiry`, `SecretExpired`) chegam pela assinatura que o guia cria; anote a latência e se a mensagem aparece no Storage browser como JSON ou Base64. **Resultado:** `SecretNewVersionCreated` em ~17 s e `SecretNearExpiry` em ~6,6 min; o `SecretExpired` chegou ~13 min depois do prazo da versão. Com o segredo expirado, o smoke test continuou passando (`baixada` em ~47 s): o hub seguiu com o valor que já tinha resolvido, e o evento é o aviso para agir. O Storage browser mostra a mensagem já decodificada, em JSON (com **Authentication method: Access key** no topo), e o `az storage message peek` a devolve em Base64. A fila, o system topic e a assinatura foram criados por `az`, em 50 s, e não pelo Portal: os rótulos do Passo 5.2.2 ficam a conferir. A 3.5 (Passos 5.2.4 e 5.2.5) traz os tempos.
- [x] Depois de renovar a chave do storage, `-Cenario 5 -Pronto -SemPublicar` recupera o ambiente no Flex. **Resultado:** recupera. O `az storage account keys renew --key primary` e o `--key secondary` funcionaram, mas a Azure CLI avisa que esses dois nomes estão obsoletos (o `--help` aceita `key1`, `key2`, `primary` e `secondary`); a Parte 6 da 3.5 usa agora `--key key1` e `--key key2`. Depois de renovar e reiniciar o hub, o smoke test falhou, com a fatura parada em `recebida`. O `-Cenario 5 -Pronto -SemPublicar` recuperou o ambiente em 3,2 min, com as Key Vault references resolvidas e o hub reiniciado, e o smoke test passou (`baixada` em 23 s). A Parte 6 da 3.5 traz o resultado.

## 9. Desafio 3.6, heartbeat e alerta por ausência (cenário 6)

```powershell
pwsh ./scripts/deploy.ps1 -Cenario 6
pwsh ./scripts/deploy.ps1 -Cenario 6 -Pronto -EmailAlerta <e-mail>
```

- [x] O `-Cenario 6` (o `cenario-4-pronto`) desliga a identidade gerenciada: os app settings voltam às connection strings e o `disableLocalAuth` volta a `false`. **Resultado:** vindo do estado pronto da 3.5, 7,1 min; voltou às connection strings, com a identidade desligada e o `disableLocalAuth` em `false`. O Passo 1.1 do 3.6 avisa quem vem da 3.5.
- [x] O `-Cenario 6 -Pronto` cria `ag-saldaro`, `alrt-heartbeat-ausente` e `alrt-dlq-confirmacoes`; chega o e-mail do Azure avisando que você entrou no action group (confira o spam). O e-mail padrão sem `-EmailAlerta` é o da conta logada: numa conta pessoal do Free Trial, confira se é uma caixa real. **Resultado:** o `-Cenario 6 -Pronto -EmailAlerta` levou 6,4 min e criou os três. O e-mail que chegou não foi o aviso de entrada no action group, e sim um pedido de verificação, com o assunto "Action required: Verify your email for Azure Monitor action group", o botão **Verify my email** e um código de uso único que vale 30 min. Ninguém verificou, e nenhum e-mail de alerta chegou: os dois alertas dispararam só no Portal, em **Monitor → Alerts**. Pelo `az`, o receiver continua `Enabled`, sem nenhum sinal da pendência. O Microsoft Learn confirma a exigência (ponto 16 da seção 13). Na Sponsorship, o e-mail padrão saiu da conta logada; numa conta pessoal do Free Trial, fica a conferir. O 3.6 ganhou o passo de verificação (Passo 4.2), a causa no Troubleshooting e a lição de produção.
- [x] O `kql/heartbeat-ultimo.kql` mostra um heartbeat a cada 5 min. Anote se o alerta dispara logo depois do deploy (o App Insights ainda sem heartbeat na janela). **Resultado:** nenhum alerta disparou logo depois do deploy: o primeiro heartbeat veio 7 s depois do fim do `-Pronto`. O Passo 4.5 do 3.6 registra isso.
- [x] Desligue o timer: Portal → função `Conciliacao` → Overview → **Disable** (vira `AzureWebJobs.Conciliacao.Disabled = true`). **Não rode o `deploy.ps1` até o fim do teste**: ele reaplica a lista de app settings e apaga esse setting. Anote o tempo até o e-mail (o guia diz 15 a 25 min) e até o Resolved depois de religar (~15 min). **Resultado:** o timer foi desligado pela CLI (`az functionapp config appsettings set ... AzureWebJobs.Conciliacao.Disabled=true`), não pelo Portal: o rótulo **Disable** fica a conferir. O desligamento (T1) veio 54 s depois do último heartbeat (T0), e o **Fired** (T2), 19 min depois do último heartbeat (18 min depois de desligar o timer), dentro dos 15 a 25 min do guia, mas sem e-mail (caixa do `-Pronto`, acima). Ao religar o timer (T3), o app reiniciou, e o timer rodou na hora a execução atrasada, 16 s depois de T3 (`Timer fired` e `Succeeded` nos traces do host), mas essa execução não gravou heartbeat nenhum no App Insights (ponto 17 da seção 13). O primeiro heartbeat que chegou (T4) foi o do ciclo seguinte, 4 min 20 s depois de T3, com `divergencias = 1`: a fatura do `valorComVirgula` parada em `cobranca-emitida`, com as confirmações na DLQ, como a história do desafio prevê. O **Resolved** (T5) veio 15 min depois de T4 (19 min 22 s depois de T3), as três avaliações sem condição do guia. Nenhum `deploy.ps1` rodou entre T1 e T3. O Passo 5.1 do 3.6 ganhou a coluna de referência e o item 5 corrigido: a execução atrasada roda em segundos, e o heartbeat dela pode se perder.
- [x] `-Caos "falhaTransiente=50"`: o `kql/erros-transientes-por-operacao.kql` acha a primeira falha (`BancoIndisponivelException`, 503) com `operation_Id`; anote se `faturaIdDaPrimeira` vem preenchido e se o End-to-end transaction details mostra a dependência com 503. **Resultado:** com a KQL já corrigida (ponto 9 da seção 13), a primeira falha apareceu 7 s depois do início da rajada, com `operacao = EmitirCobranca`, a `BancoIndisponivelException` ("O banco respondeu 503 ao emitir a cobrança da fatura ..."), o `operation_Id` e o `faturaIdDaPrimeira` preenchido; foram 28 linhas do worker e 84 do host. O End-to-end da primeira falha mostra APIM 202 → `PostFatura` 202 → `EmitirCobranca` com a dependência `POST /api/cobrancas` 503 (success False) → 7 abandons seguidos, com reentrega imediata e sem backoff → a 8ª entrega com 201 e Complete, tudo em ~3 s. No banco, o request `PostCobranca` aparece com resultCode 503 e success True: a execução do banco terminou bem, e quem falhou foi a chamada do hub, a dependência com success False. Na mesma rajada, parte da telemetria do worker se perdeu: nenhum `mensagem.retry.agendado` no App Insights, mas os traces do host mostram as cópias com `MessageId` terminando em `-t1` (11, aos ~10 s) e `-t2` (11, aos ~40 s) (ponto 17 da seção 13). O Passo 5.3 do 3.6 traz os dois detalhes do End-to-end e a consulta pelos traces do host.
- [x] Uma mensagem envenenada (`-Caos "valorComVirgula=true"`) dispara o `alrt-dlq-confirmacoes`. **Resultado:** a rajada teve 20 faturas com `falhaTransiente=50` e 1 com `valorComVirgula`; a DLQ da `sbq-confirmacoes-banco` ficou com 2 mensagens `SchemaInvalida`, as duas da fatura com vírgula, e o `alrt-dlq-confirmacoes` disparou ~4,5 min depois da rajada, só no Portal. A triagem achou 2 mensagens corrigíveis, e o `-Reprocessar` corrigiu as duas (o `dotnet run` compila antes de rodar e levou ~2 min). O **Resolved** veio ~10 min depois de a DLQ esvaziar: a métrica `DeadletteredMessages` zerou no minuto seguinte ao fim do `-Reprocessar`, e o Resolved, uns 9 min depois, pela janela de 5 min mais as avaliações sem condição. O Passo 5.2 do 3.6 traz os tempos.

## 10. Leitura 3.7, Bicep e drift (cenário 7)

```powershell
pwsh ./scripts/deploy.ps1 -Cenario 7
pwsh ./scripts/deploy.ps1 -Cenario 7 -Pronto -SomenteValidar
```

- [x] O `-Cenario 7` (o `cenario-5-pronto`) liga a identidade gerenciada e remove os alertas do 3.6 (o estado tem `alertas = false`). **Resultado:**
  - Vindo do `cenario-6-pronto`, levou 6,8 min. Removeu `alrt-heartbeat-ausente`, `alrt-dlq-confirmacoes` e `ag-saldaro`, resolveu as Key Vault references e reiniciou o hub.
  - O smoke test logo depois chegou a `notificada` em 40,9 s. O worker com sessão consumiu depois da migração das chaves para a identidade, então o conserto da seção 8 vale no Azure.
  - A identidade do hub ficou com as seis roles, com o `Reader` no namespace.
- [x] O `-SomenteValidar` lista os recursos com `~` só no ruído conhecido (os dois Function Apps, os segredos do Key Vault, as políticas e o logger do APIM). O what-if completo, à mão (`infra/README.md`, "What-if avulso"), mostra `maxDeliveryCount: 10 => 3` quando a propriedade foi mudada no Portal. Não projete essa saída: ela pode ter valores de app settings. **Resultado: não é assim.**
  - No formato `ResourceIdOnly`, todo recurso que já existe aparece com `!` (Deploy), mude ou não. Deu `Resource changes: 49 to deploy` em 0,8 min, inclusive com o `maxDeliveryCount` alterado à mão.
  - O what-if completo mostrou `~ properties.maxDeliveryCount: 10 => 3` em 61 s. Veio com 25 recursos em `~` de ruído, entre eles as três filas (linhas `-` para propriedades de valor padrão que o Bicep não declara) e os role assignments. Os segredos do Key Vault não apareceram.
  - Foram corrigidos o 3.7 (Passos 4.1 e 6.1) e o `infra/README.md`.
- [x] `dotnet run --project 03_Aplicações/Saldaro.Ferramentas -- legado-auto-create --namespace <SALDARO_SB_FQDN>` com o seu Data Owner cria a `confirmacoesbanco`; `pwsh ./scripts/check-drift.ps1` sai com 1 (extra). Mude o `maxDeliveryCount` no Portal: sai com 1 (divergente). Depois do conserto do guia: sai com 0. **Resultado:**
  - A ferramenta criou a `confirmacoesbanco` (`maxDeliveryCount` 10, sem duplicate detection, sem sessions) e mandou 20 mensagens, em 36 s com o build.
  - Com o `maxDeliveryCount` mudado pela CLI (o Portal não foi usado): `DRIFT: 1 divergente, 1 extra.` e código 1.
  - Depois do `-Cenario 7 -Pronto` (6,0 min): `DRIFT: 1 extra.`
  - Depois de apagar a fila: `Sem drift` e código 0.
  - A dica do script cita o último deployment (`-Cenario 5 -Pronto`, logo depois do `-Cenario 7`), e o 3.7 agora explica isso.
- [x] Sem o direito Manage (só Sender/Receiver, como o guia demonstra), o `legado-auto-create` falha com 401 e código 3; anote se a mensagem do Service Bus cita `Manage` e quanto tempo a mudança de role leva para valer. Um redeploy recria o Data Owner do aluno. **Resultado: não falhou.**
  - A conta era Owner da assinatura e ficou só com Sender e Receiver no namespace, sem Data Owner em escopo nenhum, nem por grupo. Mesmo assim, o `legado-auto-create` criou a fila 2, 13 e 30 min depois da troca. Uma leitura de gerenciamento (`GET` da descrição de uma fila no endpoint do namespace) também seguiu permitida.
  - Conclusão: o Service Bus aceita as permissões do plano de controle no gerenciamento de entidades. A mensagem de 401 não foi vista.
  - O 3.7 foi reescrito no Passo 4.4, nas Lições e no Troubleshooting: o 401 vale para quem não é Owner nem Contributor. No Free Trial, o aluno vê a fila nascer e aprende por quê.
  - O redeploy `-SemPublicar` recriou o Data Owner em 3,1 min. Com um Data Owner recriado à mão antes, o mesmo deploy falhou com `RoleAssignmentExists` (ponto 19 da seção 13).
- [x] `pwsh ./scripts/deploy.ps1 -Cenario 7 -Pronto` deixa o drift em zero. **Resultado:** devolveu o `maxDeliveryCount` da fila declarada a 3. A fila extra só sai à mão, como o guia diz. Com ela apagada e as roles restauradas, o `check-drift.ps1` deu `Sem drift` e código 0, e o `-SomenteValidar` não mostrou nenhum `+` nem `-`.

## 11. Leitura 3.8, rajada no Standard

- [x] Crie a fila descartável do guia (`az servicebus queue create ... -n sbq-rajada`) e rode `dotnet run --project 03_Aplicações/Saldaro.Ferramentas -- rajada --namespace <SALDARO_SB_FQDN> --fila sbq-rajada --remetentes 50 --mensagens 100`. Anote se uma máquina só chega a `ThrottledRequests` (o guia trata como "se acontecer"), se **Incoming Messages** soma 5.001 e se a contagem da fila aparece como 0 durante o throttling. **Resultado:** uma máquina só não chegou a throttling: 5.000 mensagens em 13,1 s (~381 msg/s), com `ThrottledRequests` = 0 e `ServerErrors` = 0. A fila terminou com 5.051, e não 5.001: o SDK fez 50 retentativas por `ServiceCommunicationProblem` (erro de comunicação do lado do cliente) em envios que já tinham valido, e a `sbq-rajada`, sem detecção de duplicatas, guardou as cópias ("pelo menos uma vez"). A mensagem a mais além das 5.050 é a de aquecimento, que a ferramenta manda antes de medir (`ComandoRajada.cs`; o Passo 2.3 da 3.8 já a cita): 5.000 + 1 + 50 = 5.051. **Incoming Messages**, filtrada por `EntityName = sbq-rajada` com intervalo de 1 h, deu 5.051, igual à fila; com janela curta, logo depois da rajada, tinha dado só 53 (a métrica atrasa). A contagem 0 durante o throttling não apareceu, porque não houve throttling. A 3.8 (Passos 2.4 e 2.5) traz os números e a lição das duplicatas.
- [x] A ferramenta recusa as filas do hub com código 2. Apague a `sbq-rajada` no fim. **Resultado:** a ferramenta recusou a fila do hub, com código 2, e a `sbq-rajada` foi apagada no fim.

## 12. Limpeza

```powershell
pwsh ./scripts/limpar.ps1
```

- [x] O script lista os recursos, pede "sim", apaga o `rg-saldaro-lab`, purga o `kv-saldaro-<sufixo>` e o `apim-saldaro-<sufixo>` e apaga o `scripts/.env.local`. **Resultado:** com `-SemConfirmacao` (sem a pergunta), 4,2 min. Listou os 12 recursos, inclusive o system topic do Key Vault que a 3.5 cria. Apagou o RG, purgou o Key Vault e o APIM e apagou o `.env.local`.
- [x] `az group exists --name rg-saldaro-lab` dá `false`; `az keyvault list-deleted --query "[?name=='kv-saldaro-<sufixo>']"` volta vazio; `az apim deletedservice show --service-name apim-saldaro-<sufixo> --location brazilsouth` dá "não encontrado". **Resultado:** as três conferências deram o esperado, e nenhum recurso com o nome do lab sobrou na assinatura.
- [x] O Log Analytics apagado fica 14 dias em soft-delete (esperado; recriar na mesma região recupera o workspace). **Resultado:** o workspace aparece em `az monitor log-analytics workspace list-deleted-workspaces`.
- [ ] No dia seguinte, confira o custo real em **Cost Management** e atualize as caixas de custo dos guias se divergir.

## 13. Pontos que só o deploy real responde

| # | Ponto | Onde aparece | Se falhar | Primeiro deploy real |
|---|---|---|---|---|
| 1 | Pacote do Flex com `StorageAccountConnectionString` e `storageAccountConnectionStringName = AzureWebJobsStorage` | Lab 0 (`infra/modules/function-app.bicep`) | Trocar a autenticação do container de deploy pela identidade do app ou por um setting dedicado | **Respondido:** funcionou; os dois apps publicaram (dez Functions no hub, quatro no banco) |
| 2 | Roteamento entre as APIs `saldaro` e `saldaro/webhooks` (paths aninhados) | Lab 0 e Incidente 2 | Dar à API de webhooks um path que não comece por `saldaro` e ajustar o `WebhookUrlPublica` | **Respondido:** funciona; o webhook do banco chega ao hub pelo APIM |
| 3 | `on-error` com `&&` no `rawxml` devolvendo 504 | Incidente 1 | Conferir `context.LastError` no trace; se a política nem subir, escapar como `&amp;&amp;` | **Respondido:** funciona; 504 com o corpo do `on-error` em 30,3 s |
| 4 | Endpoint `config/configreferences/appsettings` e o tempo de resolução das Key Vault references no Flex | 3.5 | Conferir no Portal (Environment variables, coluna Source) e reiniciar o hub | **Respondido:** o endpoint responde, e as references resolveram dentro do próprio `-Cenario 5 -Pronto` (5,9 min no total) |
| 5 | Chaves do diagnóstico do APIM em `customDimensions` (`Service ID`, `Subscription Name`, `Request-x-cliente-id`) e o valor do `Retry-After` do 429 | 3.1 | Ajustar o `kql/429-por-cliente.kql` e o guia | **Respondido:** as três chaves, mais a `Response-Retry-After`; `Retry-After` de ~48 s |
| 6 | Event subscription com `deliveryWithResourceIdentity` criada logo depois do role assignment | Lab 0 | Rodar o deploy de novo (idempotente) | A conferir: a primeira tentativa do Lab 0 falhou por outro motivo (a saída `principalId`), e um deploy limpo, de primeira, ainda não passou por esse ponto |
| 7 | Cota do Flex no Free Trial com a rajada de 1.000 faturas | Incidente 1 | Baixar o `maximumInstanceCount` | A conferir numa Free Trial de verdade; na Sponsorship, a rajada drenou em ~2 min sem sinal de cota |
| 8 | Peek da DLQ de uma fila com sessions no Service Bus Explorer | Incidentes 2 e 3 | Usar o `triagem-dlq.ps1` (os guias já apontam) | **Respondido:** funciona (aba **Dead-letter**, **Peek Mode** e **Peek from start**) |
| 9 | Tipo das exceções do worker isolated em `exceptions` (tipo do hub ou linha genérica do host; contagem duplicada) | Incidente 3 e 3.6 | Ajustar o texto e o `kql/erros-transientes-por-operacao.kql` | **Respondido:** aparecem como `type`, em duas linhas por exceção (host sem `cloud_RoleName`, mais numerosa, e worker); a KQL e o Incidente 3 filtram por `cloud_RoleName`. Em `exceptions`, o `operation_Name` vem vazio no host e no worker: o nome da Function está em `customDimensions.AzureFunctions_FunctionName`, só nas linhas do worker. O Passo 3.1 do Incidente 3 devolvia 0 linhas e foi corrigido, junto com a KQL de transientes, o Lab 0 e a 3.5. Em `requests` e `traces`, o `operation_Name` vem preenchido |
| 10 | Rótulos do Portal: Search, Environment variables (Apply/Confirm), Settings → Properties da fila, Service Bus Explorer (Settings, Peek Mode), Disable na Overview da função no Flex, Headers to log do APIM, Usage + quotas | todos | Corrigir os guias e as notes | **Respondido em parte.** Conferidos: **Investigate → Search** (**View in Logs**, **Leave Preview**, **View as: Traces**) e o **End-to-end transaction details**; **Settings → Properties** da fila só lista metadados do ARM, e Sessions e Duplicate detection estão em **Overview → Essentials** (Lab 0 e Incidente 2 corrigidos); Service Bus Explorer com **Peek Mode**, **Peek from start** e a aba **Dead-letter**; Storage browser com **Authentication method: Access key**. Os demais ficam a conferir |
| 11 | Tempos reais: primeiro deploy, ativação do APIM, cold start, reinício após trocar um app setting, ingestão no App Insights, e-mail do alerta, drenagem com sessions | todos | Atualizar guias e notes | **Respondido em parte:** primeiro deploy em ~7 min (8 a 10 min estimados do zero); redeploys de 3,2 a 3,6 min com `-SemPublicar` e de 5,9 a 7,2 min publicando (o `-Cenario 5`, 10,6 min); app setting gravado em 8 a 16 s e valendo no `/api/saude` em 18 a 34 s; primeira chamada depois do reinício em 2,2 s; APIM frio logo depois de um deploy (6 timeouts de 35 s na primeira rajada; com o aquecimento, a primeira sonda em 1,9 s); drenagem com sessions: 19 de 20 faturas notificadas aos 51 s; alertas do 3.6: o **Fired** do heartbeat 19 min depois do último heartbeat, e o de DLQ ~4,5 min depois da rajada; o **Resolved** do heartbeat 15 min depois do primeiro heartbeat de volta, e o de DLQ ~10 min depois de a DLQ esvaziar. O e-mail do alerta não chegou, porque o endereço não foi verificado (ponto 16). A conferir: a ativação do APIM à parte e a ingestão no App Insights (não cronometrada) |
| 12 | `az ad signed-in-user show` e o e-mail padrão dos alertas numa conta pessoal do Free Trial | Lab 0 e 3.6 | Usar `-EmailAlerta` | **Respondido em parte:** na Sponsorship, o e-mail padrão saiu da conta logada; numa conta pessoal do Free Trial, continua a conferir. E qualquer e-mail novo precisa da verificação por OTP (ponto 16) |
| 13 | O deploy reaplicando a lista de app settings (some o setting criado à mão) | 3.6 | Ajustar o aviso do guia | **Respondido:** o deploy reaplica a lista inteira, e o que sai dela some (no `-Cenario 5 -Pronto`, `ServiceBusConnection`, `TablesConnection` e `EventGridTopicKey`); o aviso do 3.6 se sustenta. O teste com o setting criado à mão não foi feito, porque nenhum `deploy.ps1` rodou entre T1 e T3 na seção 9: essa parte fica a conferir |
| 14 | Mensagem de 401 do `legado-auto-create` citando `Manage` e o tempo de propagação de RBAC | 3.7 | Ajustar o texto do guia | **Respondido, ao contrário do esperado:** com a conta Owner da assinatura, não houve 401. Sem Data Owner, o worker legado criou a fila 2, 13 e 30 min depois da troca de roles, porque o Service Bus aceita o plano de controle no gerenciamento de entidades. O 3.7 foi reescrito (seção 10); a mensagem de 401 em si fica a conferir com uma identidade sem Owner |
| 15 | Os diagramas Mermaid renderizando no GitHub | todos os guias | Corrigir a sintaxe do bloco | A conferir |
| 16 | Verificação por OTP do e-mail do Action Group | 3.6 | Verificar o endereço pelo e-mail do Azure em até 30 min, ou selecionar **Resend** no Action Group se o código expirou; o estado **Fired** em Monitor → Alerts vale como print. Em produção, um receptor que não dependa de caixa pessoal | **Respondido:** durante o `-Pronto` chegou o pedido de verificação ("Action required: Verify your email for Azure Monitor action group", com **Verify my email** e um código que vale 30 min). Sem a verificação, nenhum e-mail de alerta chegou, e os dois alertas dispararam só no Portal; pelo `az`, o receiver seguiu `Enabled`. O Microsoft Learn ([Create and manage action groups](https://learn.microsoft.com/en-us/azure/azure-monitor/alerts/action-groups), tabela "Notification types") exige o OTP em até 30 min, com a verificação valendo para o tenant, e dispensa o "Email Azure Resource Manager role". O 3.6 ganhou o passo (4.2), a causa no Troubleshooting e a lição de produção |
| 17 | Telemetria do worker perdida em instâncias curtas do Flex | 3.6, Incidentes 2 e 3 | Confirmar pela fila, pelas tabelas e pelos traces do host; para sinal crítico, flush depois de emitir ou no encerramento do host (decisão do professor; o código não mudou) | **Comportamento observado, sem prometer quando acontece:** a rajada do 3.6 escalou o hub para ~30 instâncias do Flex, e várias viveram ~20 s gravando só linhas do host. No App Insights, `fatura.baixada` deu 11 de 20, `notificacao.enviada` 13 de 20, e `mensagem.retry.agendado` e `mensagem.deadletter`, zero; as filas, porém, terminaram com ativas 0, agendadas 0 e DLQ 2, e os traces do host mostraram as cópias `-t1` (11) e `-t2` (11). Não é sampling: `itemCount = 1` em tudo, e o sampling do worker está desligado no `Program.cs`. Mecanismo provável: o SDK do Application Insights no worker guarda a telemetria em buffer e envia em lotes, e a instância é desligada antes do envio (escala para baixo depois da rajada, ou restart por app setting: o heartbeat da execução atrasada do T3 também não chegou); as linhas do host chegam. O Learn pede `Flush()` seguido de espera nos console apps do Worker Service. Na seção 6 (Incidente 3), as contagens bateram. O 3.6 avisa no Passo 5.3, no Troubleshooting e nas Lições |
| 18 | Tipo de mudança no what-if `ResourceIdOnly` do `deploy.ps1` | 3.7 e `infra/README.md` | Ler o what-if completo para ver propriedades | **Respondido:** todo recurso que já existe aparece com `!` (Deploy), mude ou não. Foram 49 no estado pronto do 3.7, mesmo com drift real, porque o formato não compara propriedades. O what-if completo mostra a propriedade (`maxDeliveryCount: 10 => 3`), com 25 recursos de ruído. O 3.7 e o README foram corrigidos |
| 19 | `RoleAssignmentExists` quando uma role que o Bicep declara já foi criada à mão | 3.7 e qualquer role criada à mão pelo Portal | Apagar a atribuição manual e rodar o deploy de novo | **Respondido:** com o Data Owner do aluno recriado à mão, o `-SemPublicar` falhou em 3,1 min no módulo `roles-aluno` ("The role assignment already exists"). O Bicep usa nome `guid(escopo, principal, role)`, e a atribuição manual tem outro. Apagada a manual, o deploy passou. O 3.7 ganhou o aviso no Passo 4.4 e no Troubleshooting |
