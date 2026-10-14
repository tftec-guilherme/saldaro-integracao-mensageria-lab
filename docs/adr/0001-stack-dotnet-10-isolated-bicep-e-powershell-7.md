# ADR-0001: Stack .NET 10 isolated, Bicep e PowerShell 7

**Status:** Aceita · **Data:** 2026-10-13 · **Decisão:** professor da aula · **Origem:** experiência em projetos de consultoria da TFTEC · **Version-anchor:** Q4-2026

## Contexto

O kit da aula tem três aplicações (o hub, o banco simulado e um console de ferramentas), a infraestrutura de todos os cenários e os scripts que o aluno roda. A stack precisa:

- ser a que os alunos vão encontrar no mercado: para o setor de pagamentos, a mais realista é .NET (C#), segundo a experiência em projetos de consultoria da TFTEC;
- ter o trigger de Service Bus mais completo para os incidentes: settlement com motivo via `ServiceBusMessageActions`, sessões com `IsSessionsEnabled` e `ServiceBusSessionMessageActions`;
- continuar suportada durante o `Version-anchor` Q4-2026 e depois dele, enquanto a turma usar o material;
- recriar o ambiente de qualquer cenário em minutos, na máquina do aluno (Windows, macOS ou Linux) e dentro do Free Trial.

A Disciplina 04 usou Node.js 20 e montou os recursos clique a clique no Portal, sem infraestrutura como código.

A escolha inicial do professor foi .NET 8 isolated. No Azure Functions, porém, .NET 8 e .NET 9 têm fim de suporte previsto para 10/11/2026, menos de um mês depois da data deste material; o modelo in-process termina na mesma data; o .NET 10 vai até 14/11/2028. Depois do fim de suporte, a app continua rodando, mas sem correções de segurança nem suporte, e a plataforma pode limitar a escala dela.

## Decisão

- **Aplicações**: Azure Functions em **.NET 10, isolated worker model**, em C# (`net10.0`): o hub (`Saldaro.Hub`), o banco simulado (`Saldaro.BancoSimulado`) e o console `Saldaro.Ferramentas`, cada um com testes xUnit.
- **Infraestrutura**: **Bicep** (`infra/main.bicep`, módulos e um `.bicepparam` por cenário), aplicado pela Azure CLI com `what-if` antes de cada `create`.
- **Comandos**: **PowerShell 7** (`pwsh`) nos scripts de `scripts/`, chamando a Azure CLI. A publicação usa `dotnet publish` + `az functionapp deployment source config-zip`, sem exigir o Azure Functions Core Tools.
- **Registro da troca**: o professor escolheu .NET 8; o material usa .NET 10 porque o .NET 8 sai de suporte no Functions em 10/11/2026. O modelo isolated e o código são os mesmos; muda o target.
- **Node.js descartado** para seguir a stack do setor, mesmo sendo a linguagem da Disciplina 04.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| .NET 8 isolated (escolha inicial) | Mesma API e mesmo modelo, mas fim de suporte no Functions em 10/11/2026: o material nasceria vencendo dentro do próprio trimestre |
| .NET 8 in-process | O modelo in-process também termina em 10/11/2026 e não recebe versões novas do .NET |
| Node.js (stack da D04) | Daria continuidade à D04, mas não é a stack do setor; além disso, o código da D04 está em Node.js 20, que já não aparece entre as versões suportadas do Functions (só 22 e 24) |
| ARM JSON | Mesmo motor do Bicep, com uma sintaxe bem mais verbosa para ler em aula |
| Terraform | Mais uma ferramenta na máquina do aluno e um arquivo de estado para gerenciar; no Bicep, o estado fica no próprio Azure e todo tipo de recurso é suportado assim que é lançado |
| Portal clique a clique (como na D04) | Não recria estados prontos em minutos e não deixa falar de drift (cenário 3.7) |
| Bash ou o módulo Azure PowerShell nos scripts | Bash exige WSL ou Git Bash no Windows; o Azure PowerShell não instala o Bicep sozinho, e a Azure CLI instala |

## Consequências

- O aluno recebe o código pronto: lê, ajusta valores e publica. O lab não pede C# do zero.
- Cada mudança de código exige build e publicação, e o Flex Consumption não tem edição no Portal. Por isso os consertos vêm prontos atrás de toggles de cenário ([ADR-0006](0006-consertos-por-toggle-e-sessions-em-duas-functions.md)) e cada cenário tem estado pronto ([ADR-0002](0002-formato-ao-vivo-dia-de-incidente.md)).
- O plano padrão é o Flex Consumption (Linux). O .NET 10 não roda no Consumption em Linux, então o plano B (`-Plano Consumption`) é o Consumption em Windows, legado mas GA.
- Pré-requisitos da máquina: VS Code, .NET 10 SDK, Azure CLI (que instala o Bicep na primeira vez que precisa) e PowerShell 7. O Core Tools é opcional.
- O código Node.js da D04 vira só referência de comportamento.

## Gatilhos de revisão

- A Microsoft mudar a data de fim de suporte do .NET 10 no Functions, ou a próxima LTS do .NET chegar a GA no Functions antes do próximo `Version-anchor`.
- O professor preferir voltar ao .NET 8 por fidelidade ao mercado: nesse caso, os guias registram a data de fim de suporte em destaque.
- O Flex Consumption ficar indisponível na região padrão ou na cota do Free Trial e o Consumption em Windows deixar de aceitar .NET 10.
- A publicação por `config-zip` deixar de funcionar no Flex sem o Core Tools.
- O retorno da turma mostrar que ler C# foi a principal barreira da aula.

## Fontes

Consultadas em 2026-10-13.

- [Supported languages in Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/supported-languages): fim de suporte de .NET 8 e .NET 9 (10/11/2026) e .NET 10 (14/11/2028) no isolated; Node.js suportado só nas versões 22 e 24; Flex Consumption sem edição no Portal.
- [Guide for running C# Azure Functions in the isolated worker model](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#supported-versions): .NET 10 não roda no Consumption em Linux.
- [Migrate C# apps from the in-process model to the isolated worker model](https://learn.microsoft.com/en-us/azure/azure-functions/migrate-dotnet-to-isolated-model): o modelo in-process termina em 10/11/2026; o alvo recomendado é .NET 10.
- [Azure Functions language stack support policy](https://learn.microsoft.com/en-us/azure/azure-functions/language-support-policy): o que acontece depois do fim de suporte.
- [Azure Functions scale and hosting](https://learn.microsoft.com/en-us/azure/azure-functions/functions-scale): Consumption é legado e o Flex Consumption é o plano recomendado; o Consumption em Linux se aposenta em 30/09/2028 e o em Windows não é afetado por enquanto.
- [Create and manage function apps in the Flex Consumption plan](https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-how-to): publicação de projeto C# com `az functionapp deployment source config-zip`.
- [What is Bicep?](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview): suporte a todos os tipos de recurso, `what-if` e nenhum arquivo de estado para gerenciar.
- [Install Bicep tools](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/install): a Azure CLI instala o Bicep automaticamente; o Azure PowerShell não.
- [What is PowerShell?](https://learn.microsoft.com/en-us/powershell/scripting/overview): roda em Windows, Linux e macOS.
- Internas: briefing da aula ([seção 5](../research/2026-10-13-briefing-da-aula.md)); contrato do kit, seções 0, 1 e 12; mapa de fontes da D04 (`docs/research/2026-10-13-fontes-da-disciplina-04.md`).
