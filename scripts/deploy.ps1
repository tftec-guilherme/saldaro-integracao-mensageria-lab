<#
.SYNOPSIS
  Cria ou atualiza o ambiente do lab Saldaro Hub (rg-saldaro-lab) no estado de um cenário.

.DESCRIPTION
  Leva o rg-saldaro-lab ao estado inicial (padrão) ou ao estado pronto (-Pronto)
  do cenário pedido, usando infra/main.bicep e um arquivo de infra/parameters/.

  Passos:
    1. Confere as ferramentas (Azure CLI com Bicep; .NET 10 SDK para publicar)
       e o login (az account show).
    2. Escolhe o arquivo de parâmetros. Sem -Pronto: cenários 0, 1 e 2 começam
       no cenario-0; 3 no cenario-2-pronto; 4 no cenario-3-pronto; 5 e 6 no
       cenario-4-pronto; 7 no cenario-5-pronto. Com -Pronto: cenario-N-pronto.
    3. Reaproveita sufixo, região e plano do ambiente que já existe (ou do
       scripts/.env.local); num ambiente novo, gera o sufixo e confere se os
       nomes globais estão livres. Um Key Vault ou um APIM em soft-delete com
       o mesmo nome (sobra de um lab anterior) faz o deploy parar antes de
       criar qualquer recurso; o purge só acontece com -PurgarExcluidos.
    4. Confere se o Flex Consumption existe na região (dotnet-isolated 10);
       se não existir, para com a instrução de usar -Plano Consumption.
    5. Gera os segredos WebhookSecret e BancoApiKey (32 bytes aleatórios em
       base64) uma única vez e os reaproveita nos redeploys (lidos do app
       setting existente ou do Key Vault). Eles vão para o Bicep por variável
       de ambiente do próprio processo: nunca para arquivo nem linha de comando.
    6. Lê o object ID do aluno (az ad signed-in-user show) para o Bicep criar as
       roles do aluno (Service Bus Data Owner, Storage Table Data Contributor,
       Key Vault Secrets Officer).
    7. az group create; se a mudança pedida for imutável na fila
       sbq-confirmacoes-banco (sessions ou duplicate detection), recria a fila
       SÓ com -Pronto (sem -Pronto, para sem mudar nada e mostra os dois
       caminhos: apagar a fila à mão, que no 3.3 é o caminho didático, ou
       repetir o comando com -Pronto).
    8. az deployment group what-if e, em seguida, az deployment group create.
    9. Remove alertas que sobraram de um cenário anterior quando o estado pedido
       tem alertas = false (o deploy é incremental e não apagaria sozinho).
   10. dotnet publish -c Release dos dois Function Apps (03_Aplicações) e
       az functionapp deployment source config-zip.
   11. Com identidadeGerenciada, espera as Key Vault references do hub
       resolverem (propagação de RBAC) e reinicia o hub, para os triggers do
       Service Bus subirem já com as roles da identidade.
   12. Grava scripts/.env.local (sem segredos), imprime a tabela de recursos e,
       no cenário 0, roda scripts/smoke-test.ps1.

  O que o script cria custa dinheiro enquanto existir (o Service Bus Standard
  cobra por hora). Ao terminar o lab: pwsh ./scripts/limpar.ps1

.PARAMETER Cenario
  Número do cenário (0 a 7). 0 = Lab 0 (baseline ingênuo); 1 a 7 = cenários 3.1 a 3.7.
  O 3.8 é exercício de decisão e não tem deploy próprio.

.PARAMETER Pronto
  Leva ao estado pronto (consertado) do cenário, em vez do estado inicial.
  É o plano B para quem travar ao vivo. Também é a única forma de o script
  recriar sozinho a fila sbq-confirmacoes-banco quando a mudança é imutável.
  No cenário 0, -Pronto é aceito e aplica o mesmo cenario-0 (o baseline);
  a única diferença é permitir a recriação da fila, se ela vier de outro estado.

.PARAMETER Sufixo
  4 a 6 letras minúsculas ou dígitos que deixam únicos os nomes globais
  (ex.: abcd). Se omitido: reaproveita o do ambiente existente ou do
  scripts/.env.local; num ambiente novo, gera um aleatório.

.PARAMETER Location
  Região dos recursos (padrão brazilsouth). Se omitido num ambiente que já
  existe, reaproveita a região dele.

.PARAMETER Plano
  Flex (padrão; Flex Consumption, recomendado) ou Consumption (plano B quando o
  Flex não está disponível: Windows Consumption Y1, legado mas GA; é Windows
  porque o .NET 10 não roda no Consumption em Linux).
  Se omitido num ambiente que já existe, reaproveita o plano dele.

.PARAMETER EmailAlerta
  E-mail que recebe os alertas (só nos estados com alertas = true, o
  cenario-6-pronto). Se omitido, usa o e-mail da conta logada na Azure CLI.

.PARAMETER SemPublicar
  Só a infraestrutura: não roda dotnet publish nem config-zip (nem o smoke test).

.PARAMETER SomenteValidar
  Modo seguro: compila o Bicep e roda o what-if, sem criar, alterar, apagar,
  purgar ou publicar nada (nem o resource group, nem o .env.local). Sem o
  resource group, o what-if não tem onde rodar e é pulado.

.PARAMETER PurgarExcluidos
  Autoriza o deploy a purgar o Key Vault kv-saldaro-<sufixo> e o APIM
  apim-saldaro-<sufixo> que estiverem em soft-delete (sobras de um lab
  anterior, como um resource group apagado pelo Portal). O purge apaga de vez
  e não tem volta. Sem este switch, o deploy para antes de criar qualquer
  recurso e oferece duas saídas: pwsh ./scripts/limpar.ps1 -Sufixo <sufixo>
  ou repetir o deploy com -PurgarExcluidos. Com -SomenteValidar, nada é
  purgado: o script só avisa.

.EXAMPLE
  pwsh ./scripts/deploy.ps1 -Cenario 0 -Sufixo abcd

  Lab 0: cria o ambiente inteiro no baseline ingênuo, publica os dois apps e
  roda o smoke test.

.EXAMPLE
  pwsh ./scripts/deploy.ps1 -Cenario 3 -Pronto

  Plano B do Incidente 2: leva o ambiente ao estado pronto do 3.3 (consertado),
  recriando a fila de confirmações (sessions + duplicate detection) se preciso.

.EXAMPLE
  pwsh ./scripts/deploy.ps1 -Cenario 0 -Plano Consumption

  Lab 0 no plano B (Windows Consumption), para regiões ou assinaturas sem Flex.

.EXAMPLE
  pwsh ./scripts/deploy.ps1 -Cenario 7 -Pronto -SomenteValidar

  Mostra o what-if do estado pronto do 3.7 sem mudar nada.

.EXAMPLE
  pwsh ./scripts/deploy.ps1 -Cenario 0 -Sufixo abcd -PurgarExcluidos

  Recria o lab com o sufixo de um lab cujo resource group foi apagado pelo
  Portal: purga o kv-saldaro-abcd e o apim-saldaro-abcd que ficaram em
  soft-delete e segue o deploy.

.NOTES
  Version-anchor: Q4-2026. Requer PowerShell 7, Azure CLI com Bicep e, para
  publicar, o .NET 10 SDK. Azure Functions Core Tools não é necessário.
  Contrato dos nomes: docs/research/2026-10-13-contrato-do-kit.md.
#>
#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateRange(0, 7)]
    [int]$Cenario,

    [switch]$Pronto,

    [string]$Sufixo,

    [string]$Location = 'brazilsouth',

    [ValidateSet('Flex', 'Consumption')]
    [string]$Plano = 'Flex',

    [string]$EmailAlerta,

    [switch]$SemPublicar,

    [switch]$SomenteValidar,

    [switch]$PurgarExcluidos
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

# Saída em UTF-8 (acentos corretos no terminal e na leitura da saída da az).
$utf8 = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $utf8
$OutputEncoding = $utf8

# -----------------------------------------------------------------------------
# Constantes do contrato
# -----------------------------------------------------------------------------

$ResourceGroup = 'rg-saldaro-lab'
$Raiz = Split-Path -Parent $PSScriptRoot
$ArquivoMain = Join-Path $Raiz 'infra' 'main.bicep'
$PastaParametros = Join-Path $Raiz 'infra' 'parameters'
$ArquivoEnvLocal = Join-Path $PSScriptRoot '.env.local'
$ProjetoHub = Join-Path $Raiz '03_Aplicações' 'Saldaro.Hub' 'Saldaro.Hub.csproj'
$ProjetoBanco = Join-Path $Raiz '03_Aplicações' 'Saldaro.BancoSimulado' 'Saldaro.BancoSimulado.csproj'
$FilaImutavel = 'sbq-confirmacoes-banco'
$NomesAlertas = @('alrt-heartbeat-ausente', 'alrt-dlq-confirmacoes', 'ag-saldaro')
$NomesToggles = @(
    'asyncRequestReply', 'webhookHmac', 'inbox', 'filaComSessions', 'filaDupDetection',
    'workerComSessao', 'dlqComMotivo', 'retryAgendado', 'maxDeliveryCount', 'rateLimit',
    'identidadeGerenciada', 'heartbeat', 'alertas'
)
# Variáveis de ambiente lidas pelos .bicepparam (só neste processo).
$VariaveisBicep = @(
    'SALDARO_SUFIXO', 'SALDARO_LOCATION', 'SALDARO_PLANO', 'SALDARO_PRINCIPAL_ID_ALUNO',
    'SALDARO_EMAIL_ALERTA', 'SALDARO_WEBHOOK_SECRET', 'SALDARO_BANCO_API_KEY'
)

# -----------------------------------------------------------------------------
# Funções de apoio
# -----------------------------------------------------------------------------

$script:Passo = 0

function Write-Titulo([string]$Texto) {
    Write-Host ''
    Write-Host ('=' * 78) -ForegroundColor DarkCyan
    Write-Host " $Texto" -ForegroundColor Cyan
    Write-Host ('=' * 78) -ForegroundColor DarkCyan
}

function Write-Passo([string]$Texto) {
    $script:Passo++
    Write-Host ''
    Write-Host (">>> {0}. {1}" -f $script:Passo, $Texto) -ForegroundColor Cyan
}

function Write-Info([string]$Texto) { Write-Host "    $Texto" }
function Write-Ok([string]$Texto) { Write-Host "    OK: $Texto" -ForegroundColor Green }
function Write-Aviso([string]$Texto) { Write-Host "    ATENÇÃO: $Texto" -ForegroundColor Yellow }

function Stop-Deploy([string]$Texto) {
    # Interrompe o script com uma mensagem clara (tratada no catch principal).
    throw [System.InvalidOperationException]::new($Texto)
}

function Invoke-Az {
    <# Roda a az, captura a saída e devolve o texto (ou o objeto, com -Json). #>
    param(
        [Parameter(Mandatory)][string[]]$Argumentos,
        [switch]$Json,
        [switch]$PermitirFalha
    )
    $saida = & az @Argumentos --only-show-errors 2>&1
    $codigo = $LASTEXITCODE
    $texto = (@($saida | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } |
                ForEach-Object { "$_" }) -join "`n").Trim()
    if ($codigo -ne 0) {
        if ($PermitirFalha) { return $null }
        $erro = (@($saida | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } |
                    ForEach-Object { $_.ToString() }) -join "`n").Trim()
        Stop-Deploy ("az {0} falhou (código {1}).`n{2}" -f ($Argumentos[0..1] -join ' '), $codigo, $erro)
    }
    if ($Json) {
        if ([string]::IsNullOrWhiteSpace($texto)) { return $null }
        return ($texto | ConvertFrom-Json -Depth 50)
    }
    return $texto
}

# Cópia intencional: cada script do aluno é autossuficiente. Mantenha igual em limpar.ps1,
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

function Get-NomesDoLab([string]$S) {
    return @{
        serviceBus = "sb-saldaro-$S"
        storage    = "stsaldaro$S"
        planoHub   = "asp-saldaro-hub-$S"
        planoBanco = "asp-saldaro-banco-$S"
        hub        = "func-saldaro-hub-$S"
        banco      = "func-saldaro-banco-$S"
        apim       = "apim-saldaro-$S"
        keyVault   = "kv-saldaro-$S"
        topico     = "evgt-saldaro-faturas-$S"
        log        = "log-saldaro-$S"
        appi       = "appi-saldaro-$S"
    }
}

function New-Sufixo {
    <# 5 caracteres: uma letra seguida de letras minúsculas ou dígitos. #>
    $letras = 'abcdefghijklmnopqrstuvwxyz'
    $todos = $letras + '0123456789'
    $s = [string]$letras[[System.Security.Cryptography.RandomNumberGenerator]::GetInt32(0, $letras.Length)]
    for ($i = 1; $i -lt 5; $i++) {
        $s += $todos[[System.Security.Cryptography.RandomNumberGenerator]::GetInt32(0, $todos.Length)]
    }
    return [string]$s
}

function New-Segredo {
    <# 32 bytes aleatórios em base64 (contrato, seção 4). #>
    return [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
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

function Invoke-CheckName([string]$Url, [hashtable]$Corpo) {
    <# POST checkNameAvailability via az rest (corpo em arquivo temporário: sem problemas de aspas). #>
    $tmp = New-TemporaryFile
    try {
        [System.IO.File]::WriteAllText($tmp.FullName, ($Corpo | ConvertTo-Json -Compress), $utf8)
        return Invoke-Az -Argumentos @('rest', '--method', 'post', '--url', $Url, '--body', "@$($tmp.FullName)") -Json -PermitirFalha
    }
    finally {
        Remove-Item -LiteralPath $tmp.FullName -Force -ErrorAction SilentlyContinue
    }
}

function Get-NomesOcupados([hashtable]$Nomes, [string]$ArmUrl, [string]$SubscriptionId) {
    <# Devolve a lista de nomes globais já em uso (por qualquer assinatura). #>
    $ocupados = [System.Collections.Generic.List[string]]::new()
    $r = Invoke-Az -Argumentos @('storage', 'account', 'check-name', '--name', $Nomes.storage) -Json -PermitirFalha
    if ($r -and -not $r.nameAvailable) { $ocupados.Add("Storage $($Nomes.storage)") }
    $r = Invoke-Az -Argumentos @('servicebus', 'namespace', 'exists', '--name', $Nomes.serviceBus) -Json -PermitirFalha
    if ($r -and -not $r.nameAvailable) { $ocupados.Add("Service Bus $($Nomes.serviceBus)") }
    $r = Invoke-Az -Argumentos @('apim', 'check-name', '--name', $Nomes.apim) -Json -PermitirFalha
    if ($r -and -not $r.nameAvailable) { $ocupados.Add("APIM $($Nomes.apim)") }
    $r = Invoke-CheckName "$ArmUrl/subscriptions/$SubscriptionId/providers/Microsoft.KeyVault/checkNameAvailability?api-version=2024-11-01" @{
        name = $Nomes.keyVault; type = 'Microsoft.KeyVault/vaults'
    }
    if ($r -and -not $r.nameAvailable) { $ocupados.Add("Key Vault $($Nomes.keyVault)") }
    foreach ($app in @($Nomes.hub, $Nomes.banco)) {
        $r = Invoke-CheckName "$ArmUrl/subscriptions/$SubscriptionId/providers/Microsoft.Web/checknameavailability?api-version=2024-11-01" @{
            name = $app; type = 'Microsoft.Web/sites'
        }
        if ($r -and -not $r.nameAvailable) { $ocupados.Add("Function App $app") }
    }
    return , $ocupados
}

function Get-SegredosExistentes([hashtable]$Nomes) {
    <# Lê os segredos do banco (sempre em texto), do hub (se não for Key Vault reference) ou do Key Vault. #>
    $achados = @{ WebhookSecret = $null; BancoApiKey = $null }
    $origem = @{ WebhookSecret = $null; BancoApiKey = $null }
    foreach ($app in @($Nomes.banco, $Nomes.hub)) {
        $lista = Invoke-Az -Argumentos @('functionapp', 'config', 'appsettings', 'list',
            '--resource-group', $ResourceGroup, '--name', $app, '-o', 'json') -Json -PermitirFalha
        if (-not $lista) { continue }
        foreach ($nome in @('WebhookSecret', 'BancoApiKey')) {
            if ($achados[$nome]) { continue }
            $valor = ($lista | Where-Object { $_.name -eq $nome } | Select-Object -First 1).value
            if ($valor -and -not $valor.StartsWith('@Microsoft.KeyVault')) {
                $achados[$nome] = $valor
                $origem[$nome] = "app setting de $app"
            }
        }
    }
    foreach ($nome in @('WebhookSecret', 'BancoApiKey')) {
        if ($achados[$nome]) { continue }
        $valor = Invoke-Az -Argumentos @('keyvault', 'secret', 'show', '--vault-name', $Nomes.keyVault,
            '--name', $nome, '--query', 'value', '-o', 'tsv') -PermitirFalha
        if ($valor) {
            $achados[$nome] = $valor.Trim()
            $origem[$nome] = "Key Vault $($Nomes.keyVault)"
        }
    }
    return @{ Valores = $achados; Origem = $origem }
}

function Resolve-NomeParametros([int]$N, [bool]$EstadoPronto) {
    if ($EstadoPronto) {
        if ($N -eq 0) { return 'cenario-0' }
        return "cenario-$N-pronto"
    }
    switch ($N) {
        { $_ -in 0, 1, 2 } { return 'cenario-0' }
        3 { return 'cenario-2-pronto' }
        4 { return 'cenario-3-pronto' }
        { $_ -in 5, 6 } { return 'cenario-4-pronto' }
        7 { return 'cenario-5-pronto' }
    }
}

function Resolve-AcaoSobras([int]$Quantidade, [bool]$SomenteValidar, [bool]$PurgarExcluidos) {
    <#
      O que fazer com sobras em soft-delete (Key Vault e APIM com o nome do kit): 'seguir'
      (não há sobras), 'avisar' (o -SomenteValidar nunca purga), 'purgar' (só com
      -PurgarExcluidos) ou 'parar' (antes de criar qualquer recurso). Purge é irreversível,
      por isso o deploy só purga quando o aluno pede. Não chama a az: dá para testar sem Azure.
    #>
    if ($Quantidade -le 0) { return 'seguir' }
    if ($SomenteValidar) { return 'avisar' }
    if ($PurgarExcluidos) { return 'purgar' }
    return 'parar'
}

function Format-ParadaSobras([string[]]$Sobras, [string]$Sufixo, [string]$ArgumentosDoDeploy) {
    <# Mensagem da parada por sobras em soft-delete, com as duas saídas (não chama a az). #>
    return (@(
            'Há sobras de um lab anterior em soft-delete com os nomes que este deploy precisa:'
            @($Sobras | ForEach-Object { "  - $_" })
            'Enquanto estiverem lá, esses nomes ficam reservados e o deploy falharia.'
            'Liberar os nomes exige purge, que apaga de vez e não tem volta: por isso o deploy só purga se você pedir.'
            'Nada foi criado nem alterado. Escolha uma saída:'
            '  1. Purgar com o script de limpeza (funciona mesmo sem o resource group) e rodar o deploy de novo:'
            "     pwsh ./scripts/limpar.ps1 -Sufixo $Sufixo"
            "     pwsh ./scripts/deploy.ps1 $ArgumentosDoDeploy"
            '  2. Repetir o deploy autorizando o purge:'
            "     pwsh ./scripts/deploy.ps1 $ArgumentosDoDeploy -PurgarExcluidos"
        ) -join "`n")
}

function Publish-FunctionApp([string]$Projeto, [string]$NomeApp) {
    <# dotnet publish -c Release + zip + az functionapp deployment source config-zip. #>
    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("saldaro-publish-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
    $pastaPublish = Join-Path $tmp 'publish'
    $zip = Join-Path $tmp "$NomeApp.zip"
    try {
        Write-Info "dotnet publish $([System.IO.Path]::GetFileName($Projeto)) -c Release"
        & dotnet publish $Projeto -c Release -o $pastaPublish --nologo -v minimal
        if ($LASTEXITCODE -ne 0) { Stop-Deploy "dotnet publish falhou para $Projeto (veja o erro acima)." }
        if (-not (Test-Path -LiteralPath (Join-Path $pastaPublish 'host.json'))) {
            Stop-Deploy "O publish de $Projeto não gerou host.json: confira se é um projeto Azure Functions."
        }
        [System.IO.Compression.ZipFile]::CreateFromDirectory($pastaPublish, $zip)
        Write-Info "az functionapp deployment source config-zip --name $NomeApp"
        for ($tentativa = 1; $tentativa -le 2; $tentativa++) {
            & az functionapp deployment source config-zip --resource-group $ResourceGroup --name $NomeApp --src $zip --timeout 600 -o none
            if ($LASTEXITCODE -eq 0) { Write-Ok "$NomeApp publicado"; return }
            if ($tentativa -eq 1) {
                Write-Aviso "o config-zip de $NomeApp falhou; tentando de novo em 30 s (app recém-criado às vezes ainda não aceita deploy)."
                Start-Sleep -Seconds 30
            }
        }
        Stop-Deploy "Não consegui publicar $NomeApp. Rode de novo o deploy.ps1 (ele é idempotente) ou use -SemPublicar e publique depois."
    }
    finally {
        Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Wait-ReferenciasKeyVault([string]$ArmUrl, [string]$SubscriptionId, [string]$NomeHub) {
    <# Espera as Key Vault references do hub resolverem; reinicia o app para forçar nova leitura. #>
    $url = "$ArmUrl/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Web/sites/$NomeHub/config/configreferences/appsettings?api-version=2024-11-01"
    for ($tentativa = 1; $tentativa -le 9; $tentativa++) {
        $refs = Invoke-Az -Argumentos @('rest', '--method', 'get', '--url', $url) -Json -PermitirFalha
        if (-not $refs) {
            Write-Aviso 'não consegui consultar as Key Vault references; confira no Portal (Environment variables, coluna Source).'
            return
        }
        $pendentes = @($refs.value | Where-Object { $_.properties.status -ne 'Resolved' })
        if ($pendentes.Count -eq 0) {
            Write-Ok 'Key Vault references do hub resolvidas (BancoApiKey, WebhookSecret).'
            # Os triggers do Service Bus sobem antes de as roles da identidade valerem e não se recuperam
            # sozinhos: no primeiro deploy real, o worker com sessão ficou parado até um restart, que
            # bastou mesmo sem a role Reader. Com as roles já valendo (as references resolveram), um
            # restart agora faz os triggers subirem com elas.
            Write-Info 'reiniciando o hub para os triggers do Service Bus subirem já com as roles da identidade.'
            & az functionapp restart --resource-group $ResourceGroup --name $NomeHub -o none --only-show-errors
            return
        }
        $situacao = ($pendentes | ForEach-Object { "$($_.name)=$($_.properties.status)" }) -join ', '
        Write-Info "ainda não resolvidas ($situacao); aguardando a propagação de RBAC ($tentativa/9)..."
        if ($tentativa % 3 -eq 0) {
            & az functionapp restart --resource-group $ResourceGroup --name $NomeHub -o none --only-show-errors
        }
        Start-Sleep -Seconds 20
    }
    Write-Aviso "as Key Vault references ainda não resolveram. Espere 1 ou 2 minutos e rode: az functionapp restart -g $ResourceGroup -n $NomeHub"
}

function Format-Booleano($Valor) { if ($Valor) { 'sim' } else { 'não' } }

# -----------------------------------------------------------------------------
# Fluxo principal
# -----------------------------------------------------------------------------

$ambienteAnterior = @{}
foreach ($v in $VariaveisBicep + @('PYTHONIOENCODING')) {
    $ambienteAnterior[$v] = [Environment]::GetEnvironmentVariable($v, 'Process')
}

try {
    $env:PYTHONIOENCODING = 'utf-8'
    $estado = if ($Pronto) { 'estado pronto' } else { 'estado inicial' }
    Write-Titulo "Saldaro Hub: deploy do cenário $Cenario ($estado)"
    if ($SomenteValidar) {
        Write-Aviso 'modo -SomenteValidar: só compila o Bicep e roda o what-if. Nada é criado, alterado, apagado ou publicado.'
    }
    $publicar = -not $SemPublicar -and -not $SomenteValidar

    # 1. Ferramentas ------------------------------------------------------------
    Write-Passo 'Conferindo as ferramentas'
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Stop-Deploy 'Azure CLI não encontrada. Instale: https://learn.microsoft.com/cli/azure/install-azure-cli'
    }
    $versaoBicep = Invoke-Az -Argumentos @('bicep', 'version') -PermitirFalha
    if (-not $versaoBicep) { Stop-Deploy 'Bicep não encontrado na Azure CLI. Rode: az bicep install' }
    Write-Ok ($versaoBicep -split "`n")[0]
    if ($publicar) {
        if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
            Stop-Deploy 'dotnet não encontrado. Instale o .NET 10 SDK (https://dotnet.microsoft.com/download) ou rode com -SemPublicar.'
        }
        $sdks = (& dotnet --list-sdks) -join "`n"
        if ($sdks -notmatch '(?m)^10\.') {
            Stop-Deploy 'O .NET 10 SDK não está instalado (dotnet --list-sdks). Instale-o ou rode com -SemPublicar.'
        }
        foreach ($projeto in @($ProjetoHub, $ProjetoBanco)) {
            if (-not (Test-Path -LiteralPath $projeto)) {
                $relativo = [System.IO.Path]::GetRelativePath($Raiz, $projeto)
                Stop-Deploy "Projeto não encontrado: $relativo. Atualize o repositório (git pull) ou rode com -SemPublicar para criar só a infraestrutura."
            }
        }
        Write-Ok '.NET 10 SDK e projetos de 03_Aplicações encontrados'
    }

    # 2. Login ------------------------------------------------------------------
    Write-Passo 'Conferindo a assinatura (az account show)'
    $conta = Invoke-Az -Argumentos @('account', 'show', '-o', 'json') -Json -PermitirFalha
    if (-not $conta) { Stop-Deploy 'Você não está logado na Azure CLI. Rode: az login' }
    $subscriptionId = $conta.id
    Write-Ok ("assinatura '{0}' (id {1}…)" -f $conta.name, $subscriptionId.Substring(0, 8))
    $armUrl = (Invoke-Az -Argumentos @('cloud', 'show', '--query', 'endpoints.resourceManager', '-o', 'tsv')).TrimEnd('/')

    # 3. Arquivo de parâmetros ----------------------------------------------------
    Write-Passo 'Escolhendo o arquivo de parâmetros'
    $nomeParametros = Resolve-NomeParametros $Cenario $Pronto.IsPresent
    $arquivoParametros = Join-Path $PastaParametros "$nomeParametros.bicepparam"
    if (-not (Test-Path -LiteralPath $arquivoParametros)) { Stop-Deploy "Arquivo não encontrado: $arquivoParametros" }
    $nomeDeployment = "saldaro-$nomeParametros"
    Write-Ok "infra/parameters/$nomeParametros.bicepparam (deployment $nomeDeployment)"
    if ($Cenario -eq 0 -and $Pronto) {
        Write-Info 'no cenário 0, -Pronto é sinônimo de -Cenario 0: o estado pronto do Lab 0 é o próprio baseline.'
    }
    elseif (-not $Pronto -and $Cenario -ge 1) {
        Write-Info "Estado inicial do cenário $Cenario. Para pular direto para o conserto: -Cenario $Cenario -Pronto"
    }

    # 4. Ambiente existente: sufixo, região e plano ------------------------------
    Write-Passo "Conferindo o ambiente existente ($ResourceGroup)"
    $rgExiste = (Invoke-Az -Argumentos @('group', 'exists', '--name', $ResourceGroup)) -eq 'true'
    $envLocal = Read-EnvLocal -Caminho $ArquivoEnvLocal
    $sufixoExistente = $null
    $locationExistente = $null
    $planoExistente = $null
    if ($rgExiste) {
        $locationExistente = Invoke-Az -Argumentos @('group', 'show', '--name', $ResourceGroup, '--query', 'location', '-o', 'tsv')
        $recursos = @(Invoke-Az -Argumentos @('resource', 'list', '--resource-group', $ResourceGroup, '-o', 'json') -Json)
        $hubExistente = $recursos | Where-Object { $_.name -like 'func-saldaro-hub-*' } | Select-Object -First 1
        $sbExistente = $recursos | Where-Object { $_.name -like 'sb-saldaro-*' } | Select-Object -First 1
        if ($hubExistente) { $sufixoExistente = $hubExistente.name.Substring('func-saldaro-hub-'.Length) }
        elseif ($sbExistente) { $sufixoExistente = $sbExistente.name.Substring('sb-saldaro-'.Length) }
        if ($sufixoExistente) {
            $planoHubExistente = $recursos | Where-Object { $_.name -eq "asp-saldaro-hub-$sufixoExistente" } | Select-Object -First 1
            if ($planoHubExistente) {
                $sku = Invoke-Az -Argumentos @('resource', 'show', '--ids', $planoHubExistente.id, '--query', 'sku.name', '-o', 'tsv') -PermitirFalha
                $planoExistente = switch ($sku) { 'FC1' { 'Flex' } 'Y1' { 'Consumption' } default { $null } }
            }
            Write-Info "ambiente encontrado: sufixo '$sufixoExistente', região $locationExistente$(if ($planoExistente) { ", plano $planoExistente" })"
        }
        else {
            Write-Info "o $ResourceGroup existe, mas ainda não tem os recursos do lab (região $locationExistente)"
        }
    }
    else {
        Write-Info "o $ResourceGroup ainda não existe: ambiente novo"
    }

    # Sufixo
    if ($PSBoundParameters.ContainsKey('Sufixo')) {
        if ($Sufixo -cnotmatch '^[a-z0-9]{4,6}$' -or $Sufixo -eq 'troque') {
            Stop-Deploy "Sufixo inválido: '$Sufixo'. Use de 4 a 6 letras minúsculas ou dígitos (ex.: abcd)."
        }
        if ($sufixoExistente -and $Sufixo -ne $sufixoExistente) {
            Stop-Deploy "O $ResourceGroup já tem o ambiente com o sufixo '$sufixoExistente'. Use -Sufixo $sufixoExistente, ou apague tudo antes com: pwsh ./scripts/limpar.ps1"
        }
    }
    elseif ($sufixoExistente) {
        $Sufixo = $sufixoExistente
    }
    elseif ($envLocal['SALDARO_SUFIXO'] -and $envLocal['SALDARO_SUFIXO'] -cmatch '^[a-z0-9]{4,6}$') {
        $Sufixo = $envLocal['SALDARO_SUFIXO']
        Write-Info "sufixo '$Sufixo' lido do scripts/.env.local"
    }
    else {
        $Sufixo = $null
    }

    # Região (comparada já normalizada: "Brazil South" = "brazilsouth")
    if (-not $PSBoundParameters.ContainsKey('Location')) {
        if ($locationExistente) { $Location = $locationExistente }
        elseif ($envLocal['SALDARO_LOCATION']) { $Location = $envLocal['SALDARO_LOCATION'] }
    }
    $Location = $Location.ToLowerInvariant() -replace '\s', ''
    if ($PSBoundParameters.ContainsKey('Location') -and $locationExistente -and $Location -ne $locationExistente) {
        Stop-Deploy "O $ResourceGroup está em $locationExistente. Para mudar de região, apague tudo antes (pwsh ./scripts/limpar.ps1)."
    }

    # Plano
    if ($PSBoundParameters.ContainsKey('Plano')) {
        if ($planoExistente -and $Plano -ne $planoExistente) {
            Stop-Deploy "O ambiente atual usa o plano $planoExistente e não dá para migrar um Function App entre planos no lugar. Para trocar, apague tudo (pwsh ./scripts/limpar.ps1) e rode de novo com -Plano $Plano."
        }
    }
    elseif ($planoExistente) {
        $Plano = $planoExistente
    }

    # 5. Flex Consumption na região ------------------------------------------------
    if ($Plano -eq 'Flex') {
        Write-Passo "Conferindo o Flex Consumption em $Location (dotnet-isolated 10)"
        $regioesFlex = Invoke-Az -Argumentos @('functionapp', 'list-flexconsumption-locations', '--query', '[].name', '-o', 'json') -Json -PermitirFalha
        $runtimesFlex = Invoke-Az -Argumentos @('functionapp', 'list-flexconsumption-runtimes', '--location', $Location, '--runtime', 'dotnet-isolated', '--query', '[].version', '-o', 'json') -Json -PermitirFalha
        $semFlex = ($null -ne $regioesFlex -and @($regioesFlex) -notcontains $Location) -or
                   ($null -ne $runtimesFlex -and @($runtimesFlex | ForEach-Object { "$_" }) -notcontains '10')
        if ($semFlex) {
            Stop-Deploy (@(
                    "O Flex Consumption não está disponível em '$Location' para dotnet-isolated 10 nesta assinatura."
                    'Opções:'
                    '  1. Rode de novo com -Plano Consumption (Windows Consumption, legado mas GA):'
                    "     pwsh ./scripts/deploy.ps1 -Cenario $Cenario$(if ($Pronto) { ' -Pronto' }) -Plano Consumption"
                    '  2. Escolha outra região com -Location (lista: az functionapp list-flexconsumption-locations -o table)'
                ) -join "`n")
        }
        if ($null -eq $regioesFlex -or $null -eq $runtimesFlex) {
            Write-Aviso 'não consegui listar regiões/runtimes do Flex (Azure CLI antiga?); seguindo assim mesmo.'
        }
        else {
            Write-Ok "Flex Consumption com dotnet-isolated 10 disponível em $Location"
        }
    }

    # 6. Nomes globais ---------------------------------------------------------------
    Write-Passo 'Conferindo os nomes globais'
    if (-not $Sufixo) {
        for ($tentativa = 1; $tentativa -le 5 -and -not $Sufixo; $tentativa++) {
            $candidato = New-Sufixo
            $ocupados = Get-NomesOcupados (Get-NomesDoLab $candidato) $armUrl $subscriptionId
            if ($ocupados.Count -eq 0) { $Sufixo = $candidato }
        }
        if (-not $Sufixo) { Stop-Deploy 'Não consegui gerar um sufixo com nomes livres. Escolha um com -Sufixo.' }
        Write-Ok "sufixo gerado: '$Sufixo' (guarde: ele está no scripts/.env.local depois do deploy)"
    }
    elseif (-not $sufixoExistente) {
        $nomesNovos = Get-NomesDoLab $Sufixo
        # Sobras em soft-delete (de um RG apagado pelo Portal) reservam o nome. Purge é irreversível
        # e o contrato o dá ao limpar.ps1: aqui, só com -PurgarExcluidos, e nunca no -SomenteValidar.
        $sobras = [System.Collections.Generic.List[string]]::new()
        $kvApagados = @(Invoke-Az -Argumentos @('keyvault', 'list-deleted', '--resource-type', 'vault', '-o', 'json') -Json -PermitirFalha)
        $kvSobra = $kvApagados | Where-Object { $_.name -eq $nomesNovos.keyVault } | Select-Object -First 1
        $localKv = $null
        if ($kvSobra) {
            $localKv = Get-Propriedade $kvSobra 'location'
            $sobras.Add("Key Vault $($nomesNovos.keyVault)$(if ($localKv) { " (região $localKv)" })")
        }
        # "show" direcionado (nome + região): falha = não há sobra com esse nome.
        $apimSobra = Invoke-Az -Argumentos @('apim', 'deletedservice', 'show', '--service-name', $nomesNovos.apim,
            '--location', $Location, '-o', 'json') -Json -PermitirFalha
        if ($apimSobra) { $sobras.Add("APIM $($nomesNovos.apim) (região $Location)") }

        $acaoSobras = Resolve-AcaoSobras $sobras.Count $SomenteValidar.IsPresent $PurgarExcluidos.IsPresent
        if ($acaoSobras -eq 'parar') {
            $argumentosDoDeploy = "-Cenario $Cenario$(if ($Pronto) { ' -Pronto' }) -Sufixo $Sufixo"
            if ($PSBoundParameters.ContainsKey('Location')) { $argumentosDoDeploy += " -Location $Location" }
            if ($PSBoundParameters.ContainsKey('Plano')) { $argumentosDoDeploy += " -Plano $Plano" }
            if ($SemPublicar) { $argumentosDoDeploy += ' -SemPublicar' }
            Stop-Deploy (Format-ParadaSobras $sobras.ToArray() $Sufixo $argumentosDoDeploy)
        }
        elseif ($acaoSobras -eq 'avisar') {
            foreach ($sobra in $sobras) { Write-Aviso "$sobra está em soft-delete (sobra de um lab anterior)." }
            if ($PurgarExcluidos) {
                Write-Aviso 'o -SomenteValidar não purga nada. Sem ele, o -PurgarExcluidos purga essas sobras antes de criar os recursos.'
            }
            else {
                Write-Aviso "o -SomenteValidar não purga nada. O deploy real vai parar antes de criar recursos e oferecer as saídas: pwsh ./scripts/limpar.ps1 -Sufixo $Sufixo, ou o mesmo deploy com -PurgarExcluidos."
            }
        }
        elseif ($acaoSobras -eq 'purgar') {
            if ($kvSobra) {
                Write-Aviso "o Key Vault $($nomesNovos.keyVault) está em soft-delete (sobra de um lab anterior); purgando para liberar o nome (-PurgarExcluidos)..."
                $argumentosPurge = @('keyvault', 'purge', '--name', $nomesNovos.keyVault)
                if ($localKv) { $argumentosPurge += @('--location', $localKv) }
                Invoke-Az -Argumentos $argumentosPurge | Out-Null
                Write-Ok 'Key Vault purgado'
            }
            if ($apimSobra) {
                Write-Aviso "o APIM $($nomesNovos.apim) está em soft-delete (sobra de um lab anterior); purgando para liberar o nome (-PurgarExcluidos)..."
                Invoke-Az -Argumentos @('apim', 'deletedservice', 'purge', '--service-name', $nomesNovos.apim, '--location', $Location) | Out-Null
                Write-Ok 'APIM purgado'
            }
        }
        $ocupados = Get-NomesOcupados $nomesNovos $armUrl $subscriptionId
        if ($acaoSobras -eq 'avisar') {
            # No -SomenteValidar as sobras continuam lá e ocupam o próprio nome: não contam como ocupado.
            $ocupados = @($ocupados | Where-Object { -not (($kvSobra -and $_ -like 'Key Vault*') -or ($apimSobra -and $_ -like 'APIM*')) })
        }
        if ($ocupados.Count -gt 0) {
            Stop-Deploy ("Nomes globais já em uso com o sufixo '$Sufixo': " + ($ocupados -join ', ') + ". Escolha outro com -Sufixo.")
        }
        Write-Ok "nomes com o sufixo '$Sufixo' estão livres"
    }
    else {
        Write-Ok "reaproveitando os recursos existentes com o sufixo '$Sufixo'"
    }
    $nomes = Get-NomesDoLab $Sufixo
    Write-Info "região $Location, plano $Plano"

    # 7. Toggles do estado pedido ---------------------------------------------------
    Write-Passo 'Lendo os toggles do estado pedido'
    $env:SALDARO_SUFIXO = $Sufixo
    $env:SALDARO_LOCATION = $Location
    $env:SALDARO_PLANO = $Plano
    Invoke-Az -Argumentos @('bicep', 'build', '--file', $ArquivoMain, '--stdout') | Out-Null
    $compilado = Invoke-Az -Argumentos @('bicep', 'build-params', '--file', $arquivoParametros, '--stdout') -Json
    $parametros = ($compilado.parametersJson | ConvertFrom-Json -Depth 20).parameters
    $toggles = [ordered]@{}
    foreach ($t in $NomesToggles) { $toggles[$t] = $parametros.$t.value }
    $ligados = @($toggles.Keys | Where-Object { $toggles[$_] -is [bool] -and $toggles[$_] })
    Write-Ok ("ligados: {0}; maxDeliveryCount = {1}" -f ($(if ($ligados) { $ligados -join ', ' } else { 'nenhum (baseline ingênuo)' })), $toggles.maxDeliveryCount)
    if ([bool]$toggles.filaComSessions -ne [bool]$toggles.workerComSessao) {
        # Edição do aluno no .bicepparam: avisa, mas respeita o arquivo como está.
        Write-Aviso ("filaComSessions = {0} e workerComSessao = {1}: o trigger de confirmação ativo não combina com a fila (um receptor sem sessão numa fila com sessão falha). Confira o arquivo de parâmetros." -f "$($toggles.filaComSessions)".ToLowerInvariant(), "$($toggles.workerComSessao)".ToLowerInvariant())
    }

    # 8. Segredos ------------------------------------------------------------------
    Write-Passo 'Segredos WebhookSecret e BancoApiKey'
    $segredos = @{ Valores = @{ WebhookSecret = $null; BancoApiKey = $null }; Origem = @{ WebhookSecret = $null; BancoApiKey = $null } }
    if ($rgExiste) { $segredos = Get-SegredosExistentes $nomes }
    foreach ($nome in @('WebhookSecret', 'BancoApiKey')) {
        if (-not $segredos.Valores[$nome]) {
            $segredos.Valores[$nome] = New-Segredo
            $segredos.Origem[$nome] = $(if ($SomenteValidar) { 'gerado só para o what-if (descartado)' } else { 'gerado agora (32 bytes aleatórios, base64)' })
        }
        Write-Info ("{0}: {1}" -f $nome, $segredos.Origem[$nome])
    }
    $env:SALDARO_WEBHOOK_SECRET = $segredos.Valores['WebhookSecret']
    $env:SALDARO_BANCO_API_KEY = $segredos.Valores['BancoApiKey']
    Write-Info 'os valores não são exibidos nem gravados em arquivo.'

    # 9. Identidade do aluno e e-mail dos alertas ------------------------------------
    Write-Passo 'Identidade do aluno (roles da seção 2)'
    $principalIdAluno = Invoke-Az -Argumentos @('ad', 'signed-in-user', 'show', '--query', 'id', '-o', 'tsv') -PermitirFalha
    if ($principalIdAluno) {
        $env:SALDARO_PRINCIPAL_ID_ALUNO = $principalIdAluno.Trim()
        Write-Ok 'o Bicep dá ao aluno: Azure Service Bus Data Owner, Storage Table Data Contributor e Key Vault Secrets Officer'
    }
    else {
        $env:SALDARO_PRINCIPAL_ID_ALUNO = ''
        Write-Aviso 'não consegui ler o usuário logado (login com service principal?); as roles do aluno não serão criadas.'
    }
    $env:SALDARO_EMAIL_ALERTA = ''
    if ($toggles.alertas) {
        if (-not $EmailAlerta -and $conta.user.type -eq 'user' -and $conta.user.name -like '*@*') {
            $EmailAlerta = $conta.user.name
        }
        if ($EmailAlerta) {
            $env:SALDARO_EMAIL_ALERTA = $EmailAlerta
            $pos = $EmailAlerta.IndexOf('@')
            Write-Info ("alertas vão para {0}***{1}" -f $EmailAlerta.Substring(0, [Math]::Min(2, [Math]::Max($pos, 0))), $EmailAlerta.Substring([Math]::Max($pos, 0)))
            # O Azure Monitor só manda alerta para um e-mail verificado (código de uso único, válido por 30 min).
            Write-Aviso 'verifique o e-mail "Action required: Verify your email for Azure Monitor action group" em até 30 min (confira o spam); sem isso, os alertas disparam, mas nenhum e-mail chega.'
        }
        else {
            Write-Aviso 'sem -EmailAlerta: os alertas disparam, mas ninguém recebe e-mail.'
        }
    }

    # 10. Resource group -------------------------------------------------------------
    Write-Passo "Resource group $ResourceGroup"
    if ($rgExiste) {
        Write-Ok "já existe ($locationExistente)"
    }
    elseif ($SomenteValidar) {
        Write-Aviso 'o resource group não existe: no modo -SomenteValidar ele não é criado e o what-if é pulado.'
    }
    else {
        Invoke-Az -Argumentos @('group', 'create', '--name', $ResourceGroup, '--location', $Location,
            '--tags', 'projeto=saldaro-hub', 'ambiente=lab', '-o', 'none') | Out-Null
        Write-Ok "criado em $Location"
    }

    # 11. Propriedades imutáveis da fila de confirmações -------------------------------
    if ($rgExiste) {
        Write-Passo "Conferindo propriedades imutáveis da $FilaImutavel"
        $fila = Invoke-Az -Argumentos @('servicebus', 'queue', 'show', '--resource-group', $ResourceGroup,
            '--namespace-name', $nomes.serviceBus, '--name', $FilaImutavel, '-o', 'json') -Json -PermitirFalha
        if (-not $fila) {
            Write-Info 'a fila ainda não existe: o Bicep cria com as propriedades certas.'
        }
        else {
            $mudancas = @()
            $sessaoReal = [bool](Get-Propriedade $fila 'requiresSession')
            $dupReal = [bool](Get-Propriedade $fila 'requiresDuplicateDetection')
            if ($sessaoReal -ne [bool]$toggles.filaComSessions) { $mudancas += ("requiresSession {0} -> {1}" -f "$sessaoReal".ToLowerInvariant(), "$($toggles.filaComSessions)".ToLowerInvariant()) }
            if ($dupReal -ne [bool]$toggles.filaDupDetection) { $mudancas += ("requiresDuplicateDetection {0} -> {1}" -f "$dupReal".ToLowerInvariant(), "$($toggles.filaDupDetection)".ToLowerInvariant()) }
            if ($mudancas.Count -eq 0) {
                Write-Ok 'sessions e duplicate detection já estão como o estado pedido.'
            }
            else {
                $contagem = Get-Propriedade $fila 'countDetails'
                $ativas = if ($contagem) { [int]$contagem.activeMessageCount } else { 0 }
                $mortas = if ($contagem) { [int]$contagem.deadLetterMessageCount } else { 0 }
                $descricao = $mudancas -join '; '
                if ($SomenteValidar) {
                    Write-Aviso "mudança IMUTÁVEL ($descricao). O deploy real recria a fila só com -Pronto."
                }
                elseif ($Pronto) {
                    Write-Aviso "mudança IMUTÁVEL na $FilaImutavel ($descricao)."
                    Write-Aviso "o Service Bus não altera essas propriedades numa fila existente: a fila será APAGADA e recriada pelo Bicep."
                    Write-Aviso "mensagens perdidas: $ativas ativas e $mortas na DLQ."
                    Invoke-Az -Argumentos @('servicebus', 'queue', 'delete', '--resource-group', $ResourceGroup,
                        '--namespace-name', $nomes.serviceBus, '--name', $FilaImutavel) | Out-Null
                    for ($espera = 0; $espera -lt 12; $espera++) {
                        $aindaExiste = Invoke-Az -Argumentos @('servicebus', 'queue', 'show', '--resource-group', $ResourceGroup,
                            '--namespace-name', $nomes.serviceBus, '--name', $FilaImutavel, '--query', 'name', '-o', 'tsv') -PermitirFalha
                        if (-not $aindaExiste) { break }
                        Start-Sleep -Seconds 5
                    }
                    Write-Ok "$FilaImutavel apagada; o deploy a seguir recria a fila"
                }
                else {
                    # O bloqueio vale para qualquer cenário (ex.: -Cenario 1 num ambiente que veio da aula,
                    # com sessions); só no 3.3 apagar a fila à mão é a lição do guia.
                    $apagarAMao = 'drene a fila, apague-a no Portal (Service Bus > Queues > sbq-confirmacoes-banco > Delete) e rode este comando de novo.'
                    $saidas = switch ($Cenario) {
                        3 {
                            "Caminho didático (3.3): $apagarAMao"
                            'Plano B: pwsh ./scripts/deploy.ps1 -Cenario 3 -Pronto (o script recria a fila sozinho).'
                        }
                        0 {
                            "Caminho manual: $apagarAMao"
                            'Atalho: pwsh ./scripts/deploy.ps1 -Cenario 0 -Pronto (aplica o mesmo baseline e recria a fila sozinho, perdendo as mensagens dela).'
                        }
                        default {
                            "Caminho manual: $apagarAMao"
                            "Atalho: pwsh ./scripts/deploy.ps1 -Cenario $Cenario -Pronto (leva ao estado pronto do cenário e recria a fila sozinho, perdendo as mensagens dela); depois, este mesmo comando volta ao estado inicial."
                        }
                    }
                    Stop-Deploy ((@(
                                "A $FilaImutavel existe e o estado pedido muda uma propriedade IMUTÁVEL ($descricao)."
                                "Mensagens na fila: $ativas ativas e $mortas na DLQ."
                            ) + $saidas) -join "`n")
                }
            }
        }
    }

    # 12. What-if -------------------------------------------------------------------
    $podeRodarWhatIf = $rgExiste -or -not $SomenteValidar
    if ($podeRodarWhatIf) {
        Write-Passo 'What-if (o que o deploy vai mudar)'
        Write-Info 'formato ResourceIdOnly: só os recursos, sem valores (os app settings do baseline têm segredos).'
        & az deployment group what-if --resource-group $ResourceGroup --name $nomeDeployment `
            --parameters $arquivoParametros --result-format ResourceIdOnly --exclude-change-types NoChange Ignore
        if ($LASTEXITCODE -ne 0) { Stop-Deploy 'O what-if falhou (veja o erro acima). Nada foi alterado pelo deploy.' }
    }

    if ($SomenteValidar) {
        if ($rgExiste -and -not $toggles.alertas) {
            $recursosRg = @(Invoke-Az -Argumentos @('resource', 'list', '--resource-group', $ResourceGroup, '-o', 'json') -Json)
            $sobras = @($recursosRg | Where-Object { $_.name -in $NomesAlertas } | ForEach-Object { $_.name })
            if ($sobras.Count -gt 0) {
                Write-Aviso ("o deploy real removeria os alertas que sobraram de outro cenário: {0}." -f ($sobras -join ', '))
            }
        }
        Write-Titulo 'Validação concluída: nada foi criado, alterado ou publicado.'
        exit 0
    }

    # 13. Deploy --------------------------------------------------------------------
    Write-Passo 'Deploy do Bicep (az deployment group create)'
    Write-Info 'na primeira vez leva de 5 a 15 minutos (o APIM Consumption demora para ativar).'
    & az deployment group create --resource-group $ResourceGroup --name $nomeDeployment `
        --parameters $arquivoParametros -o none
    if ($LASTEXITCODE -ne 0) {
        Stop-Deploy "O deploy falhou (veja o erro acima). Detalhes: Portal > $ResourceGroup > Deployments > $nomeDeployment. Rodar de novo é seguro (idempotente)."
    }
    $outputs = Invoke-Az -Argumentos @('deployment', 'group', 'show', '--resource-group', $ResourceGroup,
        '--name', $nomeDeployment, '--query', 'properties.outputs', '-o', 'json') -Json
    function Get-Saida([string]$Nome) { return $outputs.$Nome.value }
    Write-Ok "deployment $nomeDeployment concluído"

    # 14. Alertas que sobraram de um cenário anterior --------------------------------
    if (-not $toggles.alertas) {
        $recursosRg = @(Invoke-Az -Argumentos @('resource', 'list', '--resource-group', $ResourceGroup, '-o', 'json') -Json)
        $sobras = @($recursosRg | Where-Object { $_.name -in $NomesAlertas } |
                Sort-Object { if ($_.name -eq 'ag-saldaro') { 1 } else { 0 } })
        if ($sobras.Count -gt 0) {
            Write-Passo 'Removendo alertas de um cenário anterior (este estado tem alertas = false)'
            foreach ($r in $sobras) {
                Invoke-Az -Argumentos @('resource', 'delete', '--ids', $r.id) | Out-Null
                Write-Ok "$($r.name) removido"
            }
        }
    }

    # 15. Publicação ------------------------------------------------------------------
    if ($publicar) {
        Write-Passo 'Publicando os Function Apps (dotnet publish + config-zip)'
        Publish-FunctionApp $ProjetoHub (Get-Saida 'hubFunctionAppName')
        Publish-FunctionApp $ProjetoBanco (Get-Saida 'bancoFunctionAppName')
    }
    else {
        Write-Passo 'Publicação pulada (-SemPublicar)'
    }

    # 16. Key Vault references (3.5) -----------------------------------------------------
    if ($toggles.identidadeGerenciada) {
        Write-Passo 'Conferindo as Key Vault references do hub (propagação de RBAC)'
        Wait-ReferenciasKeyVault $armUrl $subscriptionId (Get-Saida 'hubFunctionAppName')
    }

    # 17. scripts/.env.local -------------------------------------------------------------
    Write-Passo 'Gravando scripts/.env.local (sem segredos)'
    $envValores = [ordered]@{
        SALDARO_RG           = $ResourceGroup
        SALDARO_SUFIXO       = $Sufixo
        SALDARO_LOCATION     = $Location
        SALDARO_SUBSCRIPTION = $subscriptionId
        SALDARO_APIM_URL     = Get-Saida 'apimBaseUrl'
        SALDARO_HUB_URL      = Get-Saida 'hubBaseUrl'
        SALDARO_BANCO_URL    = Get-Saida 'bancoBaseUrl'
        SALDARO_SB_NAMESPACE = Get-Saida 'serviceBusNamespaceName'
        SALDARO_SB_FQDN      = Get-Saida 'serviceBusFqdn'
        SALDARO_STORAGE      = Get-Saida 'storageAccountName'
        SALDARO_KV           = Get-Saida 'keyVaultName'
        SALDARO_APPI         = Get-Saida 'appInsightsName'
    }
    $conteudoEnv = (($envValores.Keys | ForEach-Object { "$_=$($envValores[$_])" }) -join "`n") + "`n"
    [System.IO.File]::WriteAllText($ArquivoEnvLocal, $conteudoEnv, $utf8)
    Write-Ok 'scripts/.env.local gravado (ignorado pelo git)'

    # 18. Tabela de recursos ---------------------------------------------------------------
    $sku = if ($Plano -eq 'Flex') { 'Flex Consumption (FC1)' } else { 'Consumption (Y1, Windows)' }
    $linhasTabela = @(
        [pscustomobject]@{ Recurso = 'Resource Group'; Nome = $ResourceGroup; Detalhe = $Location }
        [pscustomobject]@{ Recurso = 'Service Bus'; Nome = (Get-Saida 'serviceBusNamespaceName'); Detalhe = "Standard; disableLocalAuth = $(Format-Booleano $toggles.identidadeGerenciada)" }
        [pscustomobject]@{ Recurso = '  Fila'; Nome = (Get-Saida 'filaFaturasRecebidas'); Detalhe = 'lock 1 min; maxDeliveryCount 10' }
        [pscustomobject]@{ Recurso = '  Fila'; Nome = (Get-Saida 'filaConfirmacoesBanco'); Detalhe = "sessions $(Format-Booleano $toggles.filaComSessions); dup detection $(Format-Booleano $toggles.filaDupDetection); maxDeliveryCount $($toggles.maxDeliveryCount)" }
        [pscustomobject]@{ Recurso = '  Fila'; Nome = (Get-Saida 'filaNotificacoesParceiro'); Detalhe = 'destino do Event Grid; maxDeliveryCount 10' }
        [pscustomobject]@{ Recurso = 'Storage'; Nome = (Get-Saida 'storageAccountName'); Detalhe = 'Standard LRS; tabelas e fila banco-pagamentos' }
        [pscustomobject]@{ Recurso = 'Plano do hub'; Nome = (Get-Saida 'hubPlanName'); Detalhe = $sku }
        [pscustomobject]@{ Recurso = 'Plano do banco'; Nome = (Get-Saida 'bancoPlanName'); Detalhe = $sku }
        [pscustomobject]@{ Recurso = 'Function App hub'; Nome = (Get-Saida 'hubFunctionAppName'); Detalhe = '.NET 10 isolated; identidade system-assigned' }
        [pscustomobject]@{ Recurso = 'Function App banco'; Nome = (Get-Saida 'bancoFunctionAppName'); Detalhe = '.NET 10 isolated (banco parceiro simulado)' }
        [pscustomobject]@{ Recurso = 'APIM'; Nome = (Get-Saida 'apimName'); Detalhe = "Consumption; APIs $(Get-Saida 'apimApiName') e $(Get-Saida 'apimApiWebhooksName'); rate limit $(Format-Booleano $toggles.rateLimit)" }
        [pscustomobject]@{ Recurso = 'Key Vault'; Nome = (Get-Saida 'keyVaultName'); Detalhe = "RBAC; segredos gravados $(Format-Booleano $toggles.identidadeGerenciada)" }
        [pscustomobject]@{ Recurso = 'Event Grid topic'; Nome = (Get-Saida 'eventGridTopicName'); Detalhe = "CloudEvents 1.0 -> $(Get-Saida 'eventSubscriptionName')" }
        [pscustomobject]@{ Recurso = 'Log Analytics'; Nome = (Get-Saida 'logAnalyticsName'); Detalhe = 'PerGB2018' }
        [pscustomobject]@{ Recurso = 'App Insights'; Nome = (Get-Saida 'appInsightsName'); Detalhe = 'workspace-based (hub, banco e APIM)' }
    )
    if ($toggles.alertas) {
        $linhasTabela += [pscustomobject]@{ Recurso = 'Alertas'; Nome = 'ag-saldaro'; Detalhe = 'alrt-heartbeat-ausente, alrt-dlq-confirmacoes' }
    }
    Write-Titulo "Recursos do lab (cenário $Cenario, $estado)"
    Write-Host ($linhasTabela | Format-Table -AutoSize | Out-String -Width 220).TrimEnd()
    Write-Host ''
    Write-Host "  APIM (URL pública): $(Get-Saida 'apimBaseUrl')"
    Write-Host "  Hub (direto):       $(Get-Saida 'hubBaseUrl')"
    Write-Host "  Banco simulado:     $(Get-Saida 'bancoBaseUrl')"
    if (Get-Saida 'apimSubscriptionsHabilitadas') {
        Write-Host ''
        Write-Aviso ("a API saldaro agora EXIGE subscription ({0}): mande a chave no header Ocp-Apim-Subscription-Key." -f ((Get-Saida 'apimSubscriptions') -join ', '))
        Write-Info 'as chaves não vão para o .env.local; leia na hora (Portal > APIM > Subscriptions, ou az rest POST .../subscriptions/cliente-a/listSecrets).'
        Write-Info 'o webhook do banco (saldaro/webhooks) continua sem subscription.'
    }

    # 19. Smoke test (cenário 0) -----------------------------------------------------------
    if ($Cenario -eq 0 -and $publicar) {
        Write-Passo 'Smoke test ponta a ponta (scripts/smoke-test.ps1)'
        $smoke = Join-Path $PSScriptRoot 'smoke-test.ps1'
        if (-not (Test-Path -LiteralPath $smoke)) {
            Write-Aviso 'scripts/smoke-test.ps1 ainda não existe neste checkout; pulei o smoke test.'
        }
        else {
            $falhaSmoke = $null
            try {
                $global:LASTEXITCODE = 0
                & $smoke
                if ($LASTEXITCODE -ne 0) { $falhaSmoke = "código de saída $LASTEXITCODE" }
            }
            catch {
                $falhaSmoke = $_.Exception.Message
            }
            if ($falhaSmoke) {
                Stop-Deploy "O deploy terminou, mas o smoke test falhou ($falhaSmoke). Os apps podem estar no cold start: espere 1 ou 2 minutos e rode pwsh ./scripts/smoke-test.ps1"
            }
        }
    }

    Write-Titulo 'Pronto.'
    Write-Host '  Lembrete de custo: o Service Bus Standard cobra por hora enquanto existir.'
    Write-Host '  Ao terminar o lab: pwsh ./scripts/limpar.ps1'
    exit 0
}
catch {
    Write-Host ''
    Write-Host 'ERRO:' -ForegroundColor Red
    Write-Host ($_.Exception.Message) -ForegroundColor Red
    if ($_.Exception -isnot [System.InvalidOperationException]) {
        # Erro inesperado (não é uma das paradas previstas): mostra onde foi.
        Write-Host ($_.ScriptStackTrace) -ForegroundColor DarkGray
    }
    exit 1
}
finally {
    # Nada do que este script exportou fica na sessão (os segredos inclusive).
    foreach ($v in $ambienteAnterior.Keys) {
        [Environment]::SetEnvironmentVariable($v, $ambienteAnterior[$v], 'Process')
    }
}
