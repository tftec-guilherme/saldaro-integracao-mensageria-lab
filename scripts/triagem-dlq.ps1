#Requires -Version 7.0

<#
.SYNOPSIS
  Triagem da DLQ: atalho para o subcomando triagem do Saldaro.Ferramentas, com o namespace do .env.local.

.DESCRIPTION
  Roda o console da aula com o namespace lido de scripts/.env.local (SALDARO_SB_FQDN):

    dotnet run --project 03_Aplicações/Saldaro.Ferramentas -- triagem --namespace <fqdn> --fila <Fila> [...]

  Sem opções, só LÊ a DLQ (peek) e mostra uma tabela agrupada por DeadLetterReason, com exemplos.
  Nada é alterado. Para agir:
  - -Reprocessar: corrige o valor em texto pt-BR ("1.250,00" vira 1250.00) e reprocessa: manda a
    cópia para a fila principal com MessageId {original}-r{n}, o mesmo SessionId e a propriedade
    reprocessadoDe; só depois disso a mensagem sai da DLQ. O que não dá para corrigir fica na DLQ.
  - -DescartarMotivo <motivo>: descarta (completa) as mensagens daquele DeadLetterReason. Junto com
    -Reprocessar, descarta as do motivo e reprocessa as demais.

  CONCEITO: a triagem separa o que é envenenado (schema inválido) do que só pegou uma falha
  transiente, antes de qualquer reprocessamento. Reprocessar "tudo" às cegas baixaria faturas
  erradas; descartar às cegas perderia confirmações boas.

  Autentica com o seu az login (DefaultAzureCredential): o deploy.ps1 dá ao seu usuário a role
  Azure Service Bus Data Owner no namespace, o que permite ler a DLQ mesmo com disableLocalAuth.

.PARAMETER Fila
  Fila cuja DLQ será lida (padrão sbq-confirmacoes-banco).

.PARAMETER Reprocessar
  Corrige e reprocessa as mensagens da DLQ, com uma cópia de cada uma na fila principal.

.PARAMETER DescartarMotivo
  DeadLetterReason das mensagens a descartar (ex.: TentativasEsgotadas, SchemaInvalida,
  MaxDeliveryCountExceeded).

.EXAMPLE
  ./scripts/triagem-dlq.ps1

  Mostra a DLQ de sbq-confirmacoes-banco agrupada por motivo, sem alterar nada.

.EXAMPLE
  ./scripts/triagem-dlq.ps1 -Reprocessar

  3.4: corrige o valor "1.250,00" das confirmações e reprocessa as mensagens da DLQ; as que não
  dá para corrigir ficam lá.

.EXAMPLE
  ./scripts/triagem-dlq.ps1 -DescartarMotivo TentativasEsgotadas -Reprocessar

  Descarta as que esgotaram as tentativas e reprocessa as demais.

.EXAMPLE
  ./scripts/triagem-dlq.ps1 -Fila sbq-notificacoes-parceiro

  Olha a DLQ de outra fila do hub.

.NOTES
  Version-anchor: Q4-2026. Requer PowerShell 7 e o .NET 10 SDK. O primeiro uso compila a
  ferramenta (alguns segundos).
  Código de saída: o mesmo do Saldaro.Ferramentas (0 sucesso, 1 falha, 2 uso inválido,
  3 acesso negado).
#>
[CmdletBinding()]
param(
    [ArgumentCompletions('sbq-confirmacoes-banco', 'sbq-faturas-recebidas', 'sbq-notificacoes-parceiro')]
    [ValidateNotNullOrEmpty()]
    [string]$Fila = 'sbq-confirmacoes-banco',

    [switch]$Reprocessar,

    [ArgumentCompletions('SchemaInvalida', 'TentativasEsgotadas', 'MaxDeliveryCountExceeded', 'TTLExpiredException')]
    [string]$DescartarMotivo
)

$ErrorActionPreference = 'Stop'

function Stop-ComErro {
    param([string]$Mensagem)
    Write-Host "ERRO: $Mensagem" -ForegroundColor Red
    exit 1
}

# Cópia intencional: cada script do aluno é autossuficiente. Mantenha igual em deploy.ps1,
# limpar.ps1, check-drift.ps1, erp-simulado.ps1 e smoke-test.ps1.
function Read-EnvLocal {
    <#
      Lê o scripts/.env.local (CHAVE=valor, gravado pelo deploy.ps1) num dicionário.
      Ignora linhas vazias e comentários (#) e tira aspas em volta do valor. Sem o
      arquivo, devolve o dicionário vazio: quem chama decide se isso é erro.
    #>
    param([Parameter(Mandatory)][string]$Caminho)
    $valores = @{}
    if (-not (Test-Path -LiteralPath $Caminho)) { return $valores }
    foreach ($linha in (Get-Content -LiteralPath $Caminho -Encoding utf8)) {
        $texto = $linha.Trim()
        if ($texto.Length -eq 0 -or $texto.StartsWith('#')) { continue }
        $posicao = $texto.IndexOf('=')
        if ($posicao -lt 1) { continue }
        $valores[$texto.Substring(0, $posicao).Trim()] = $texto.Substring($posicao + 1).Trim().Trim('"', "'")
    }
    return $valores
}

$codificacaoAnterior = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false) } catch { }
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

try {
    $caminhoEnv = Join-Path $PSScriptRoot '.env.local'
    if (-not (Test-Path -LiteralPath $caminhoEnv)) {
        Stop-ComErro "não encontrei $caminhoEnv. Rode o deploy.ps1 primeiro: ele cria o ambiente e grava esse arquivo com as URLs e o namespace (sem segredos)."
    }
    $ambiente = Read-EnvLocal -Caminho $caminhoEnv
    $fqdn = $ambiente['SALDARO_SB_FQDN']
    if ([string]::IsNullOrWhiteSpace($fqdn) -and -not [string]::IsNullOrWhiteSpace($ambiente['SALDARO_SB_NAMESPACE'])) {
        $fqdn = '{0}.servicebus.windows.net' -f $ambiente['SALDARO_SB_NAMESPACE']
    }
    if ([string]::IsNullOrWhiteSpace($fqdn)) {
        Stop-ComErro "a chave SALDARO_SB_FQDN não está em $caminhoEnv. Rode o deploy.ps1 de novo para regravar o arquivo."
    }

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Stop-ComErro 'o comando dotnet não foi encontrado. Instale o .NET 10 SDK.'
    }

    $projeto = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..' '03_Aplicações' 'Saldaro.Ferramentas'))
    if (-not (Test-Path -LiteralPath (Join-Path $projeto 'Saldaro.Ferramentas.csproj'))) {
        Stop-ComErro "não encontrei o projeto em $projeto."
    }

    $argumentos = @('run', '--project', $projeto, '--', 'triagem', '--namespace', $fqdn, '--fila', $Fila)
    if ($Reprocessar) { $argumentos += '--reprocessar' }
    if ($DescartarMotivo) { $argumentos += @('--descartar-motivo', $DescartarMotivo) }

    Write-Host ('> dotnet {0}' -f ($argumentos -join ' ')) -ForegroundColor DarkGray
    & dotnet @argumentos
    $codigo = $LASTEXITCODE
}
finally {
    try { [Console]::OutputEncoding = $codificacaoAnterior } catch { }
}
exit $codigo
