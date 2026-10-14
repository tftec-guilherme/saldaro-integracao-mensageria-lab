# Saldaro Pagamentos: Integração e Mensageria no Azure

Laboratório da aula hands-on de Integração e Mensageria no Azure da pós-graduação da TFTEC. Você monta, quebra e conserta o hub de cobrança B2B de uma empresa fictícia, a **Saldaro Pagamentos S.A.**, usando API Management, Azure Functions, Service Bus, Event Grid, Storage, Key Vault e Application Insights. A aula deriva da [Disciplina 04 do repositório `azure-retail`](https://github.com/tftec-guilherme/azure-retail/tree/main/Disciplina_04_Integracao_Mensageria_Azure).

`Version-anchor: Q4-2026`. Empresa, pessoas, números e incidentes são fictícios; qualquer semelhança é coincidência.

## Por onde começar

1. **Monte o ambiente no Portal, entendendo o porquê de cada peça:** o [Lab 0 montado no Portal](01_Aulas/Lab0_Montagem_no_Portal_Guia.md) cria, clique a clique, cada recurso da Saldaro e explica, para cada um, o objetivo, o papel no fluxo, por que ele e não as alternativas, a configuração escolhida e o custo.
2. **Ou suba tudo com um comando:** o [Lab 0](01_Aulas/Lab0_Fundacao_Guia.md) faz o mesmo ambiente pelo `deploy.ps1` (Bicep), em uns 10 minutos. É também o plano B de quem travar.
3. **Viva os incidentes:** [Incidente 1](01_Aulas/Incidente1_Timeout_Guia.md) (timeout e cobrança duplicada), [Incidente 2](01_Aulas/Incidente2_Webhook_Guia.md) (webhook duplicado, fora de ordem e forjado) e [Incidente 3](01_Aulas/Incidente3_DLQ_Guia.md) (mensagem envenenada e DLQ), cada um consertado no Portal.
4. **Para casa:** o desafio 3.6 (heartbeat e alertas) e os guias de leitura 3.1 (rate limit), 3.5 (identidade gerenciada e Key Vault), 3.7 (Bicep e drift) e 3.8 (Standard ou Premium).
5. **Dúvidas "por que X e não Y"** (por que APIM e não WAF, por que Service Bus e não Storage Queue, e outras 21): a [FAQ das tecnologias](docs/research/2026-10-14-duvidas-das-tecnologias.md), com fontes do Microsoft Learn.

## Mapa

| Caminho | O que é |
|---|---|
| `01_Aulas/` | Guias do lab: Lab 0 (no Portal ou por script), Incidentes 1, 2 e 3, desafio 3.6 e guias de leitura 3.1, 3.5, 3.7 e 3.8 |
| `02_Apresentação/` | Índice de slides, speaker notes, QA das notes e a fonte do deck da aula (`deck/project/`) |
| `03_Aplicações/` | Código do kit em .NET 10 (`Saldaro.sln`): o hub, o banco parceiro simulado, o console de ferramentas e os testes de cada um |
| `infra/` | Bicep do ambiente inteiro, com um arquivo de parâmetros por estado de cenário ([infra/README.md](infra/README.md)) |
| `scripts/` | PowerShell 7: deploy, limpeza, drift, ERP simulado, smoke test e triagem da DLQ (`Get-Help ./scripts/<script>.ps1 -Full`) |
| `kql/` | Consultas KQL prontas para as provas de cada cenário |
| `templates/` | Post-mortem de cinco linhas e ADR de uma página |
| `CONTEXT.md`, `docs/adr/` | Glossário da Saldaro e as decisões de arquitetura do kit |
| `docs/research/` | FAQ das tecnologias, contrato do kit (nomes e comportamentos), briefing da aula e checklist do primeiro deploy real |

## Como rodar o kit

O kit sobe a "Saldaro 2.0" inteira numa assinatura sua, com um comando: APIM, dois Function Apps em .NET 10, Service Bus, Event Grid, Storage, Key Vault e Application Insights, tudo descrito em Bicep. O passo a passo completo, com troubleshooting, é o [Lab 0](01_Aulas/Lab0_Fundacao_Guia.md), feito em casa antes da aula.

Prefere montar o ambiente à mão, clique a clique no Portal, entendendo por que cada tecnologia está ali? Siga o [Lab 0 montado no Portal](01_Aulas/Lab0_Montagem_no_Portal_Guia.md) (~3 h): os mesmos nomes e configurações do Bicep, a mesma prova (o smoke test), e o `deploy.ps1` como plano B.

**Pré-requisitos:** assinatura Azure (o Free Trial basta), VS Code, .NET 10 SDK, Azure CLI com Bicep (`az bicep version`), PowerShell 7 (`pwsh`) e Git. O Azure Functions Core Tools é opcional: o deploy publica os apps sem ele.

**Três comandos**, no PowerShell 7 e na raiz do repositório (troque `abcd` pelo seu sufixo: 4 a 6 letras minúsculas e dígitos):

```powershell
az login                                            # entre na sua assinatura e confira com: az account show
pwsh ./scripts/deploy.ps1 -Cenario 0 -Sufixo abcd   # cria o rg-saldaro-lab, publica os apps e roda o smoke test
pwsh ./scripts/limpar.ps1 -Sufixo abcd              # no fim: apaga o rg-saldaro-lab e purga o Key Vault e o APIM
```

**Custo:** ~R$ 0,07 por hora enquanto o `rg-saldaro-lab` existir, quase tudo do Service Bus Standard; o resto cabe na franquia gratuita. Da véspera ao fim da aula, ~R$ 2 a 3; esquecido ligado, ~R$ 55 a 60 por mês. Cabe com folga nos US$ 200 do Free Trial. Regra de ouro: terminou, rode o `limpar.ps1`.

Sem Flex Consumption disponível na sua assinatura, use o plano B: `-Plano Consumption` (Consumption em Windows; o .NET 10 não roda no Consumption em Linux). Cada cenário tem um estado pronto (`deploy.ps1 -Cenario N -Pronto`) para quem travar durante a aula.

Para conferir o kit sem criar nada no Azure: `dotnet build 03_Aplicações/Saldaro.sln` e `dotnet test 03_Aplicações/Saldaro.sln` (os testes não usam Azure), `az bicep build --file infra/main.bicep --stdout` e, já com `az login`, `pwsh ./scripts/deploy.ps1 -Cenario 0 -SomenteValidar` (compila o Bicep e roda o what-if, sem criar nem mudar nada).
