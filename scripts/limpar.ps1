<#
.SYNOPSIS
  Apaga o ambiente do lab Saldaro Hub (rg-saldaro-lab) e purga o que fica em soft-delete.

.DESCRIPTION
  Regra de ouro do Free Trial: apague o resource group ao terminar o lab.

  Passos:
    1. Confere o login (az account show).
    2. Descobre o sufixo (parâmetro -Sufixo, scripts/.env.local ou os recursos
       do próprio rg-saldaro-lab) e a região.
    3. Lista os recursos, pede confirmação ("sim") e apaga o rg-saldaro-lab
       (az group delete), esperando terminar.
    4. Purga o Key Vault kv-saldaro-<sufixo> (soft-delete de 7 dias): sem o
       purge, recriar o lab com o mesmo sufixo falha.
    5. Purga o APIM apim-saldaro-<sufixo> (soft-delete de 48 h), pelo mesmo
       motivo. "Não encontrado" conta como sucesso nos dois purges.
    6. Apaga o scripts/.env.local, que passaria a apontar para recursos que
       não existem mais.

  Também serve quando o RG já foi apagado pelo Portal: nesse caso só purga as
  sobras (passe -Sufixo se o .env.local não existir mais).

.PARAMETER Sufixo
  Sufixo usado no deploy (4 a 6 letras minúsculas ou dígitos). Se omitido, vem
  do scripts/.env.local ou dos recursos do rg-saldaro-lab.

.PARAMETER SemConfirmacao
  Não pergunta antes de apagar (para uso em automação). Cuidado: é irreversível.

.EXAMPLE
  pwsh ./scripts/limpar.ps1

  Apaga o rg-saldaro-lab do lab atual (pergunta antes) e purga Key Vault e APIM.

.EXAMPLE
  pwsh ./scripts/limpar.ps1 -Sufixo abcd

  Depois de apagar o RG pelo Portal: purga o kv-saldaro-abcd e o apim-saldaro-abcd.

.NOTES
  Version-anchor: Q4-2026. Requer PowerShell 7 e Azure CLI.
  O Log Analytics apagado fica 14 dias em soft-delete com o nome reservado.
  Recriar o lab com o mesmo sufixo, no mesmo RG e na mesma região, recupera o
  workspace (o PUT do Bicep faz a recuperação); em outra região, o nome dá
  conflito até o fim desse prazo.
#>
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Sufixo,

    [switch]$SemConfirmacao
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$utf8 = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $utf8
$OutputEncoding = $utf8

$ResourceGroup = 'rg-saldaro-lab'
$ArquivoEnvLocal = Join-Path $PSScriptRoot '.env.local'

function Write-Titulo([string]$Texto) {
    Write-Host ''
    Write-Host ('=' * 78) -ForegroundColor DarkCyan
    Write-Host " $Texto" -ForegroundColor Cyan
    Write-Host ('=' * 78) -ForegroundColor DarkCyan
}

$script:Passo = 0
function Write-Passo([string]$Texto) {
    $script:Passo++
    Write-Host ''
    Write-Host (">>> {0}. {1}" -f $script:Passo, $Texto) -ForegroundColor Cyan
}

function Write-Info([string]$Texto) { Write-Host "    $Texto" }
function Write-Ok([string]$Texto) { Write-Host "    OK: $Texto" -ForegroundColor Green }
function Write-Aviso([string]$Texto) { Write-Host "    ATENÇÃO: $Texto" -ForegroundColor Yellow }

function Stop-Limpeza([string]$Texto) {
    throw [System.InvalidOperationException]::new($Texto)
}

function Invoke-Az {
    <# Roda a az e devolve um objeto com Ok, Texto (stdout) e Erro (stderr). #>
    param([Parameter(Mandatory)][string[]]$Argumentos)
    $saida = & az @Argumentos --only-show-errors 2>&1
    $codigo = $LASTEXITCODE
    return [pscustomobject]@{
        Ok    = ($codigo -eq 0)
        Texto = (@($saida | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" }) -join "`n").Trim()
        Erro  = (@($saida | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } | ForEach-Object { $_.ToString() }) -join "`n").Trim()
    }
}

# Cópia intencional: cada script do aluno é autossuficiente. Mantenha igual em deploy.ps1,
# check-drift.ps1, erp-simulado.ps1, smoke-test.ps1 e triagem-dlq.ps1.
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

function Test-NaoEncontrado([string]$Erro) {
    return $Erro -match '(?i)not\s*found|NotFound|does not exist|ResourceNotFound|ServiceNotFound|VaultNotFound|DeletedVaultNotFound'
}

try {
    Write-Titulo 'Saldaro Hub: limpeza do lab'

    # 1. Login -------------------------------------------------------------------
    Write-Passo 'Conferindo a assinatura (az account show)'
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Stop-Limpeza 'Azure CLI não encontrada. Instale: https://learn.microsoft.com/cli/azure/install-azure-cli'
    }
    $conta = Invoke-Az @('account', 'show', '-o', 'json')
    if (-not $conta.Ok) { Stop-Limpeza 'Você não está logado na Azure CLI. Rode: az login' }
    $dadosConta = $conta.Texto | ConvertFrom-Json
    Write-Ok ("assinatura '{0}' (id {1}…)" -f $dadosConta.name, $dadosConta.id.Substring(0, 8))

    # 2. Sufixo e região -----------------------------------------------------------
    Write-Passo 'Descobrindo o sufixo e a região'
    $envLocal = Read-EnvLocal -Caminho $ArquivoEnvLocal
    $rgExiste = (Invoke-Az @('group', 'exists', '--name', $ResourceGroup)).Texto -eq 'true'
    $location = $envLocal['SALDARO_LOCATION']
    $recursos = @()
    if ($rgExiste) {
        $location = (Invoke-Az @('group', 'show', '--name', $ResourceGroup, '--query', 'location', '-o', 'tsv')).Texto
        $lista = Invoke-Az @('resource', 'list', '--resource-group', $ResourceGroup, '-o', 'json')
        if ($lista.Ok -and $lista.Texto) { $recursos = @($lista.Texto | ConvertFrom-Json) }
    }
    if ($PSBoundParameters.ContainsKey('Sufixo')) {
        if ($Sufixo -cnotmatch '^[a-z0-9]{4,6}$') {
            Stop-Limpeza "Sufixo inválido: '$Sufixo'. Use de 4 a 6 letras minúsculas ou dígitos."
        }
    }
    else {
        $kvNoRg = $recursos | Where-Object { $_.name -like 'kv-saldaro-*' } | Select-Object -First 1
        $sbNoRg = $recursos | Where-Object { $_.name -like 'sb-saldaro-*' } | Select-Object -First 1
        if ($kvNoRg) { $Sufixo = $kvNoRg.name.Substring('kv-saldaro-'.Length) }
        elseif ($sbNoRg) { $Sufixo = $sbNoRg.name.Substring('sb-saldaro-'.Length) }
        elseif ($envLocal['SALDARO_SUFIXO']) { $Sufixo = $envLocal['SALDARO_SUFIXO'] }
    }
    if (-not $location) { $location = 'brazilsouth' }
    $location = $location.ToLowerInvariant() -replace '\s', ''
    if ($Sufixo) {
        Write-Ok "sufixo '$Sufixo', região $location"
    }
    elseif ($rgExiste) {
        Write-Aviso 'não achei o sufixo (nem no .env.local nem no RG): apago o RG, mas não consigo purgar Key Vault e APIM.'
    }
    else {
        Stop-Limpeza "O $ResourceGroup não existe e não sei qual sufixo purgar. Rode: pwsh ./scripts/limpar.ps1 -Sufixo <sufixo>"
    }

    # 3. Resource group ---------------------------------------------------------------
    Write-Passo "Apagando o resource group $ResourceGroup"
    if (-not $rgExiste) {
        Write-Info "o $ResourceGroup não existe (já foi apagado): sigo para os purges."
    }
    else {
        if ($recursos.Count -gt 0) {
            Write-Host ($recursos | Sort-Object type, name | Select-Object @{ n = 'Nome'; e = { $_.name } }, @{ n = 'Tipo'; e = { $_.type } } |
                    Format-Table -AutoSize | Out-String -Width 200).TrimEnd()
        }
        if (-not $SemConfirmacao) {
            Write-Host ''
            Write-Host "    Isso apaga o $ResourceGroup e TUDO que está nele (filas, tabelas, logs). É irreversível." -ForegroundColor Yellow
            $resposta = Read-Host "    Digite 'sim' para confirmar"
            if ($resposta -ne 'sim') {
                Write-Info 'Cancelado: nada foi apagado.'
                exit 0
            }
        }
        Write-Info 'apagando (leva alguns minutos; o APIM costuma ser o último)...'
        $r = Invoke-Az @('group', 'delete', '--name', $ResourceGroup, '--yes')
        if (-not $r.Ok) { Stop-Limpeza "Falha ao apagar o $ResourceGroup.`n$($r.Erro)" }
        Write-Ok "$ResourceGroup apagado"
    }

    if ($Sufixo) {
        # 4. Key Vault ------------------------------------------------------------------
        $kv = "kv-saldaro-$Sufixo"
        Write-Passo "Purgando o Key Vault $kv (soft-delete)"
        $apagado = Invoke-Az @('keyvault', 'show-deleted', '--name', $kv, '-o', 'json')
        if (-not $apagado.Ok) {
            Write-Ok 'não está em soft-delete (nada a purgar)'
        }
        else {
            $localKv = ($apagado.Texto | ConvertFrom-Json).properties.location
            if (-not $localKv) { $localKv = $location }
            $r = Invoke-Az @('keyvault', 'purge', '--name', $kv, '--location', $localKv)
            if ($r.Ok -or (Test-NaoEncontrado $r.Erro)) { Write-Ok "$kv purgado (nome liberado)" }
            else { Write-Aviso "não consegui purgar o $kv. Tente depois: az keyvault purge --name $kv --location $localKv`n    $($r.Erro)" }
        }

        # 5. APIM -----------------------------------------------------------------------
        $apim = "apim-saldaro-$Sufixo"
        Write-Passo "Purgando o APIM $apim (soft-delete de 48 h)"
        $apagado = Invoke-Az @('apim', 'deletedservice', 'show', '--service-name', $apim, '--location', $location, '-o', 'json')
        if (-not $apagado.Ok) {
            Write-Ok 'não está em soft-delete (nada a purgar)'
        }
        else {
            $r = Invoke-Az @('apim', 'deletedservice', 'purge', '--service-name', $apim, '--location', $location)
            if ($r.Ok -or (Test-NaoEncontrado $r.Erro)) { Write-Ok "$apim purgado (nome liberado)" }
            else { Write-Aviso "não consegui purgar o $apim. Tente depois: az apim deletedservice purge --service-name $apim --location $location`n    $($r.Erro)" }
        }
    }

    # 6. .env.local ---------------------------------------------------------------------
    Write-Passo 'Apagando o scripts/.env.local'
    if (Test-Path -LiteralPath $ArquivoEnvLocal) {
        Remove-Item -LiteralPath $ArquivoEnvLocal -Force
        Write-Ok 'scripts/.env.local apagado (o próximo deploy.ps1 grava de novo)'
    }
    else {
        Write-Info 'não existia'
    }

    Write-Titulo 'Limpeza concluída: os recursos do lab foram apagados e os nomes liberados.'
    exit 0
}
catch {
    Write-Host ''
    Write-Host 'ERRO:' -ForegroundColor Red
    Write-Host ($_.Exception.Message) -ForegroundColor Red
    if ($_.Exception -isnot [System.InvalidOperationException]) {
        Write-Host ($_.ScriptStackTrace) -ForegroundColor DarkGray
    }
    exit 1
}
