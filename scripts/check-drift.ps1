<#
.SYNOPSIS
  Compara as filas reais do Service Bus com o que o Bicep declarou e aponta o drift.

.DESCRIPTION
  Drift de IaC é a diferença entre o que o Bicep declara e o que existe na
  assinatura (cenário 3.7, "a fila fantasma").

  O "declarado" vem dos outputs filasDeclaradas e namespaceDeclarado do último
  deployment bem-sucedido do main.bicep (saldaro-cenario-...): são as mesmas
  propriedades que o service-bus.bicep usou para criar as filas, calculadas
  pelo próprio Bicep a partir dos parâmetros do cenário.

  O "real" vem de az servicebus queue list / namespace show / topic list
  (plano de controle: funciona mesmo com disableLocalAuth).

  Reporta:
    - extra: fila (ou tópico) que existe no namespace, mas não está no Bicep
      (ex.: a confirmacoesbanco criada por um worker legado);
    - faltante: fila declarada no Bicep que não existe;
    - divergente: propriedade com valor diferente do declarado (ex.: alguém
      mudou o maxDeliveryCount no Portal).

  Código de saída: 0 = sem drift; 1 = há drift; 2 = não foi possível comparar
  (sem login, sem resource group ou sem deployment do lab).

.PARAMETER Sufixo
  Opcional. Sem parâmetros, o script lê o resource group (SALDARO_RG) e o
  namespace (SALDARO_SB_NAMESPACE) do scripts/.env.local gravado pelo
  deploy.ps1; sem .env.local, usa rg-saldaro-lab e o namespace dos outputs.

.EXAMPLE
  pwsh ./scripts/check-drift.ps1

  Compara o namespace sb-saldaro-<sufixo> com o último deployment do lab.
  Saída 0 = sem drift; 1 = com drift.

.NOTES
  Version-anchor: Q4-2026. Requer PowerShell 7 e Azure CLI.
  Corrigir drift: rode de novo o deploy.ps1 do cenário (volta as propriedades
  declaradas) e apague à mão o que for extra (o deploy é incremental e não
  apaga o que não declara).
#>
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Sufixo
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

function Write-Info([string]$Texto) { Write-Host "    $Texto" }

function Stop-Comparacao([string]$Texto) {
    throw [System.InvalidOperationException]::new($Texto)
}

function Invoke-AzJson {
    <# Roda a az e devolve o JSON convertido (ou para com a mensagem de erro). #>
    param([Parameter(Mandatory)][string[]]$Argumentos, [switch]$PermitirFalha)
    $saida = & az @Argumentos --only-show-errors -o json 2>&1
    $codigo = $LASTEXITCODE
    $texto = (@($saida | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" }) -join "`n").Trim()
    if ($codigo -ne 0) {
        if ($PermitirFalha) { return $null }
        $erro = (@($saida | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } | ForEach-Object { $_.ToString() }) -join "`n").Trim()
        Stop-Comparacao ("az {0} falhou (código {1}).`n{2}" -f ($Argumentos[0..1] -join ' '), $codigo, $erro)
    }
    if ([string]::IsNullOrWhiteSpace($texto)) { return $null }
    return ($texto | ConvertFrom-Json -Depth 50)
}

# Cópia intencional: cada script do aluno é autossuficiente. Mantenha igual em deploy.ps1,
# limpar.ps1, erp-simulado.ps1, smoke-test.ps1 e triagem-dlq.ps1.
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

function Get-Propriedade($Objeto, [string]$Nome) {
    <# Lê a propriedade tanto da saída "achatada" da az quanto de .properties. #>
    if ($null -eq $Objeto) { return $null }
    if ($Objeto.PSObject.Properties.Name -contains $Nome) { return $Objeto.$Nome }
    if ($Objeto.PSObject.Properties.Name -contains 'properties' -and $null -ne $Objeto.properties -and
        $Objeto.properties.PSObject.Properties.Name -contains $Nome) {
        return $Objeto.properties.$Nome
    }
    return $null
}

function Test-ValorIgual($Declarado, $Real) {
    <# Compara respeitando o tipo: booleano, número e duração ISO 8601 (PT1M = PT60S). #>
    if ($null -eq $Real) { return $false }
    if ($Declarado -is [bool]) { return [bool]$Real -eq $Declarado }
    if ($Declarado -is [int] -or $Declarado -is [long] -or $Declarado -is [double]) {
        return [double]$Real -eq [double]$Declarado
    }
    $d = "$Declarado"
    $r = "$Real"
    if ($d -match '^-?P' -and $r -match '^-?P') {
        try {
            return [System.Xml.XmlConvert]::ToTimeSpan($d) -eq [System.Xml.XmlConvert]::ToTimeSpan($r)
        }
        catch {
            # Duração fora do intervalo do TimeSpan: compara como texto.
        }
    }
    return [string]::Equals($d, $r, [System.StringComparison]::OrdinalIgnoreCase)
}

function Format-Valor($Valor) {
    if ($null -eq $Valor) { return '(não informado)' }
    if ($Valor -is [bool]) { return $Valor.ToString().ToLowerInvariant() }
    return "$Valor"
}

try {
    Write-Titulo 'Saldaro Hub: check de drift do Service Bus'

    # Login e resource group --------------------------------------------------------
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Stop-Comparacao 'Azure CLI não encontrada. Instale: https://learn.microsoft.com/cli/azure/install-azure-cli'
    }
    $conta = Invoke-AzJson @('account', 'show') -PermitirFalha
    if (-not $conta) { Stop-Comparacao 'Você não está logado na Azure CLI. Rode: az login' }
    Write-Info ("assinatura '{0}' (id {1}…)" -f $conta.name, $conta.id.Substring(0, 8))
    # Sem parâmetros, o RG e o namespace vêm do scripts/.env.local gravado pelo deploy.ps1.
    $envLocal = Read-EnvLocal -Caminho $ArquivoEnvLocal
    if ($envLocal['SALDARO_RG']) { $ResourceGroup = $envLocal['SALDARO_RG'] }
    $existe = Invoke-AzJson @('group', 'exists', '--name', $ResourceGroup)
    if (-not $existe) { Stop-Comparacao "O $ResourceGroup não existe. Rode antes: pwsh ./scripts/deploy.ps1 -Cenario 0" }

    # Declarado: outputs do último deployment do lab ------------------------------------
    $deployments = @(Invoke-AzJson @('deployment', 'group', 'list', '--resource-group', $ResourceGroup,
            '--query', '[].{nome:name, estado:properties.provisioningState, quando:properties.timestamp}'))
    $ultimo = $deployments |
        Where-Object { $_ -and $_.nome -like 'saldaro-*' -and $_.estado -eq 'Succeeded' } |
        Sort-Object { [datetimeoffset]$_.quando } -Descending |
        Select-Object -First 1
    if (-not $ultimo) {
        Stop-Comparacao "Nenhum deployment bem-sucedido do lab (saldaro-*) no $ResourceGroup. Rode antes o deploy.ps1."
    }
    $outputs = Invoke-AzJson @('deployment', 'group', 'show', '--resource-group', $ResourceGroup,
        '--name', $ultimo.nome, '--query', 'properties.outputs')
    $filasDeclaradas = @($outputs.filasDeclaradas.value)
    $namespaceDeclarado = $outputs.namespaceDeclarado.value
    if ($filasDeclaradas.Count -eq 0 -or -not $namespaceDeclarado) {
        Stop-Comparacao "O deployment $($ultimo.nome) não tem os outputs filasDeclaradas/namespaceDeclarado (Bicep antigo?). Rode o deploy.ps1 de novo."
    }
    if ($PSBoundParameters.ContainsKey('Sufixo')) {
        $namespace = "sb-saldaro-$Sufixo"
    }
    elseif ($envLocal['SALDARO_SB_NAMESPACE']) {
        $namespace = $envLocal['SALDARO_SB_NAMESPACE']
    }
    else {
        $namespace = $outputs.serviceBusNamespaceName.value
    }
    $quando = ([datetimeoffset]$ultimo.quando).ToLocalTime().ToString('yyyy-MM-dd HH:mm')
    Write-Info "declarado: deployment $($ultimo.nome) ($quando)"
    Write-Info "real: namespace $namespace"

    # Real -------------------------------------------------------------------------------
    $ns = Invoke-AzJson @('servicebus', 'namespace', 'show', '--resource-group', $ResourceGroup, '--name', $namespace) -PermitirFalha
    if (-not $ns) { Stop-Comparacao "O namespace $namespace não existe no $ResourceGroup." }
    $filasReais = @(Invoke-AzJson @('servicebus', 'queue', 'list', '--resource-group', $ResourceGroup, '--namespace-name', $namespace)) |
        Where-Object { $_ }
    $topicosReais = @(Invoke-AzJson @('servicebus', 'topic', 'list', '--resource-group', $ResourceGroup, '--namespace-name', $namespace) -PermitirFalha) |
        Where-Object { $_ }

    # Comparação -------------------------------------------------------------------------
    $achados = [System.Collections.Generic.List[object]]::new()
    function Add-Achado([string]$Situacao, [string]$Entidade, [string]$Propriedade, $Declarado, $Real) {
        $achados.Add([pscustomobject]@{
                Situacao    = $Situacao
                Entidade    = $Entidade
                Propriedade = $Propriedade
                Declarado   = Format-Valor $Declarado
                Real        = Format-Valor $Real
            })
    }

    # Namespace
    $skuReal = if ($ns.sku) { $ns.sku.name } else { $null }
    if ($namespaceDeclarado.sku -and -not (Test-ValorIgual $namespaceDeclarado.sku $skuReal)) {
        Add-Achado 'divergente' $namespace 'sku' $namespaceDeclarado.sku $skuReal
    }
    $localAuthReal = Get-Propriedade $ns 'disableLocalAuth'
    if ($null -eq $localAuthReal) { $localAuthReal = $false }
    if (-not (Test-ValorIgual ([bool]$namespaceDeclarado.disableLocalAuth) $localAuthReal)) {
        Add-Achado 'divergente' $namespace 'disableLocalAuth' ([bool]$namespaceDeclarado.disableLocalAuth) $localAuthReal
    }

    # Filas declaradas: faltantes e divergentes
    $nomesDeclarados = @($filasDeclaradas | ForEach-Object { $_.name })
    foreach ($declarada in $filasDeclaradas) {
        $real = $filasReais | Where-Object { $_.name -eq $declarada.name } | Select-Object -First 1
        if (-not $real) {
            Add-Achado 'faltante' $declarada.name '(fila inteira)' 'existe' '(não existe)'
            continue
        }
        foreach ($p in $declarada.properties.PSObject.Properties) {
            $valorReal = Get-Propriedade $real $p.Name
            if (-not (Test-ValorIgual $p.Value $valorReal)) {
                Add-Achado 'divergente' $declarada.name $p.Name $p.Value $valorReal
            }
        }
    }

    # Filas e tópicos extras
    foreach ($real in $filasReais) {
        if ($nomesDeclarados -notcontains $real.name) {
            Add-Achado 'extra' $real.name '(fila inteira)' '(não declarada)' 'existe'
        }
    }
    foreach ($topico in $topicosReais) {
        Add-Achado 'extra' $topico.name '(tópico inteiro)' '(não declarado)' 'existe'
    }

    # Relatório -----------------------------------------------------------------------------
    Write-Host ''
    $resumoFilas = ($filasReais | ForEach-Object { $_.name }) -join ', '
    Write-Info "filas reais: $(if ($resumoFilas) { $resumoFilas } else { '(nenhuma)' })"
    if ($achados.Count -eq 0) {
        Write-Host ''
        Write-Host '    Sem drift: o namespace está exatamente como o Bicep declara.' -ForegroundColor Green
        exit 0
    }

    Write-Host ''
    Write-Host ($achados | Sort-Object Situacao, Entidade, Propriedade |
            Format-Table @{ n = 'Situação'; e = { $_.Situacao } }, Entidade, Propriedade,
            @{ n = 'Declarado (Bicep)'; e = { $_.Declarado } }, Real -AutoSize | Out-String -Width 200).TrimEnd()
    $contagem = $achados | Group-Object Situacao | ForEach-Object { "$($_.Count) $($_.Name)" }
    Write-Host ''
    Write-Host ("    DRIFT: {0}." -f ($contagem -join ', ')) -ForegroundColor Yellow
    $comando = 'pwsh ./scripts/deploy.ps1 -Cenario <N>'
    if ($ultimo.nome -match '^saldaro-cenario-(\d)(-pronto)?$') {
        $comando = "pwsh ./scripts/deploy.ps1 -Cenario $($Matches[1])$(if ($Matches[2]) { ' -Pronto' })"
    }
    Write-Info "divergente/faltante: rode de novo o deploy do mesmo estado ($comando)."
    Write-Info 'extra: o deploy é incremental e não apaga o que não declara; apague à mão, por exemplo:'
    Write-Info "  az servicebus queue delete -g $ResourceGroup --namespace-name $namespace -n <fila>"
    exit 1
}
catch {
    Write-Host ''
    Write-Host 'ERRO:' -ForegroundColor Red
    Write-Host ($_.Exception.Message) -ForegroundColor Red
    if ($_.Exception -isnot [System.InvalidOperationException]) {
        Write-Host ($_.ScriptStackTrace) -ForegroundColor DarkGray
    }
    exit 2
}
