#Requires -Version 7.0

<#
.SYNOPSIS
  Smoke test ponta a ponta: 1 fatura, do POST /faturas até baixada (ou notificada).

.DESCRIPTION
  Envia uma fatura pelo APIM, como o ERP faria, e acompanha o status até a baixa, passando pelo fio
  condutor inteiro: hub, banco simulado, webhook de confirmação, fila e worker de baixa. Funciona
  em qualquer cenário (síncrono ou assíncrono, com ou sem HMAC no webhook). Falha (código de saída
  1, com o motivo na tela) se a fatura não chegar a baixada ou notificada dentro do prazo.

  Passos
  1. Aquece o hub e o banco (GET /api/saude direto nas Function Apps) e mostra a versão e os
     toggles do hub, para você confirmar em que estado o ambiente está.
  2. POST {SALDARO_APIM_URL}/faturas com x-cliente-id cliente-a, referenciaErp SMOKE-<data e hora>,
     Idempotency-Key e um traceparent W3C gerado aqui. Aceita 201 (baseline síncrono) ou 202
     (request-reply assíncrono).
  3. Consulta GET /faturas/{id}/status (o Location do 202, quando vier) até o hub responder
     303 See Other, o sinal de que a fatura foi baixada; segue o Location e confere a fatura.
  4. Imprime faturaId, referenciaErp, tempo total e o operation_Id do POST, com as consultas KQL.

  CONCEITO: o operation_Id do Application Insights é o trace-id do header traceparent (W3C Trace
  Context). Como o script escolhe o trace-id, ele já sabe o operation_Id antes de a telemetria
  chegar (se a resposta trouxer traceparent, traceresponse ou Request-Id, vale o da resposta).
  A parte assíncrona (webhook do banco, filas, baixa) pode aparecer com outro operation_Id; para
  ela, a consulta impressa filtra pelo faturaId.

  Se a API exigir subscription key (cenário 1 pronto), o script usa a chave da subscription
  cliente-a, buscada com az rest (listSecrets) e mantida só em memória.

.PARAMETER TimeoutSegundos
  Prazo total, em segundos, para a fatura chegar a baixada ou notificada (10 a 3600; padrão 120).

.EXAMPLE
  ./scripts/smoke-test.ps1

  Uma fatura ponta a ponta, com até 2 minutos de prazo.

.EXAMPLE
  ./scripts/smoke-test.ps1 -TimeoutSegundos 300

  Mais prazo, útil logo depois do primeiro deploy (cold start das Function Apps e ativação do APIM).

.NOTES
  Version-anchor: Q4-2026. Requer PowerShell 7 (Windows, macOS ou Linux). Lê SALDARO_APIM_URL,
  SALDARO_HUB_URL e SALDARO_BANCO_URL de scripts/.env.local (e SALDARO_SUBSCRIPTION, SALDARO_RG e
  SALDARO_SUFIXO só se a API exigir subscription key). Não grava nada.
  Código de saída: 0 se a fatura chegou a baixada/notificada no prazo; 1 caso contrário.
#>
[CmdletBinding()]
param(
    [ValidateRange(10, 3600)]
    [int]$TimeoutSegundos = 120
)

$ErrorActionPreference = 'Stop'

$Cliente = 'cliente-a'
$TimeoutHttpSegundos = 35
$IntervaloPadraoSegundos = 3
$EsperaPelaNotificacaoSegundos = 20

# Cópia intencional: cada script do aluno é autossuficiente. Mantenha igual em deploy.ps1,
# limpar.ps1, check-drift.ps1, erp-simulado.ps1 e triagem-dlq.ps1.
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

function Get-ValorObrigatorio {
    param([hashtable]$Valores, [string]$Chave, [string]$Caminho)
    $valor = $Valores[$Chave]
    if ([string]::IsNullOrWhiteSpace($valor)) {
        Stop-SmokeTest -Motivo "a chave $Chave não está em $Caminho." -Dicas @('Rode o deploy.ps1 de novo para regravar o arquivo.')
    }
    return $valor.TrimEnd('/')
}

function Get-CulturaPtBr {
    try { return [cultureinfo]::GetCultureInfo('pt-BR') } catch { return [cultureinfo]::InvariantCulture }
}

# Gerador de CNPJ de teste (Get-DigitoCnpj e New-CnpjTeste).
# Cópia intencional: cada script do aluno é autossuficiente. Mantenha igual em erp-simulado.ps1.
function Get-DigitoCnpj {
    param([int[]]$Digitos, [int[]]$Pesos)
    $soma = 0
    for ($i = 0; $i -lt $Pesos.Count; $i++) { $soma += $Digitos[$i] * $Pesos[$i] }
    $resto = $soma % 11
    if ($resto -lt 2) { return 0 }
    return 11 - $resto
}

function New-CnpjTeste {
    # CONCEITO: CNPJ de teste gerado por algoritmo (módulo 11), com dígitos verificadores válidos.
    # Raiz aleatória de 8 dígitos + filial 0001 + 2 dígitos verificadores. Nunca use CNPJ real em teste.
    # Sem -Aleatorio, usa um gerador novo (o ERP passa o mesmo gerador para o lote inteiro).
    param([System.Random]$Aleatorio = [System.Random]::new())
    do {
        $raiz = @(1..8 | ForEach-Object { $Aleatorio.Next(0, 10) })
    } while (@($raiz | Select-Object -Unique).Count -eq 1) # evita 00000000, 11111111...
    $base = $raiz + @(0, 0, 0, 1)
    $dv1 = Get-DigitoCnpj -Digitos $base -Pesos @(5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2)
    $dv2 = Get-DigitoCnpj -Digitos ($base + $dv1) -Pesos @(6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2)
    return (($base + $dv1 + $dv2) -join '')
}

function Get-TraceIdDaResposta {
    # traceparent/traceresponse: 00-<trace-id>-<span-id>-<flags>; Request-Id (legado): |<raiz>.<n>.
    param([System.Net.Http.HttpResponseMessage]$Resposta)
    foreach ($nome in 'traceresponse', 'traceparent') {
        $valores = $null
        if ($Resposta.Headers.TryGetValues($nome, [ref]$valores)) {
            $partes = ([string]@($valores)[0]).Split('-')
            if ($partes.Count -ge 4 -and $partes[1] -match '^[0-9a-f]{32}$') { return $partes[1] }
        }
    }
    $valores = $null
    if ($Resposta.Headers.TryGetValues('Request-Id', [ref]$valores)) {
        $raiz = ([string]@($valores)[0]).TrimStart('|').Split('.')[0]
        if ($raiz) { return $raiz }
    }
    return ''
}

function Invoke-Saldaro {
    # Uma chamada HTTP que nunca lança exceção: devolve status, corpo, Location e o erro, se houver.
    param(
        [System.Net.Http.HttpClient]$Http,
        [string]$Metodo,
        [string]$Url,
        [hashtable]$Cabecalhos = @{},
        [string]$Corpo = ''
    )
    $pedido = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Metodo), $Url)
    foreach ($cabecalho in $Cabecalhos.GetEnumerator()) {
        [void]$pedido.Headers.TryAddWithoutValidation($cabecalho.Key, [string]$cabecalho.Value)
    }
    if ($Corpo) {
        $pedido.Content = [System.Net.Http.StringContent]::new($Corpo, [System.Text.Encoding]::UTF8, 'application/json')
    }
    $relogio = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $tarefa = $Http.SendAsync($pedido)
        while (-not $tarefa.Wait(200)) { } # fatias de 200 ms: o Ctrl+C consegue interromper
        $resposta = $tarefa.Result
        try {
            $retryAfter = $null
            $condicao = $resposta.Headers.RetryAfter
            if ($null -ne $condicao -and $null -ne $condicao.Delta) { $retryAfter = $condicao.Delta.TotalSeconds }
            return [pscustomobject]@{
                Status     = [int]$resposta.StatusCode
                Motivo     = $resposta.ReasonPhrase
                Corpo      = $resposta.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                Location   = $(if ($null -ne $resposta.Headers.Location) { $resposta.Headers.Location.OriginalString } else { '' })
                RetryAfter = $retryAfter
                TraceId    = (Get-TraceIdDaResposta -Resposta $resposta)
                LatenciaMs = [int]$relogio.Elapsed.TotalMilliseconds
                Erro       = ''
            }
        }
        finally { $resposta.Dispose() }
    }
    catch {
        $detalhe = $_.Exception.Message
        for ($e = $_.Exception; $null -ne $e; $e = $e.InnerException) {
            if ($e -is [System.TimeoutException] -or $e -is [System.Threading.Tasks.TaskCanceledException]) {
                $detalhe = "sem resposta em $TimeoutHttpSegundos s"
                break
            }
            $detalhe = $e.Message
        }
        return [pscustomobject]@{ Status = 0; Motivo = ''; Corpo = ''; Location = ''; RetryAfter = $null; TraceId = ''; LatenciaMs = [int]$relogio.Elapsed.TotalMilliseconds; Erro = $detalhe }
    }
    finally { $pedido.Dispose() }
}

function Resolve-Location {
    param([string]$Base, [string]$Location)
    if ([string]::IsNullOrWhiteSpace($Location)) { return '' }
    $absoluta = $null
    if ([uri]::TryCreate($Location, [UriKind]::Absolute, [ref]$absoluta) -and $absoluta.Scheme -in 'http', 'https') {
        return $absoluta.AbsoluteUri
    }
    return ([uri]::new([uri]$Base, $Location)).AbsoluteUri
}

function Get-StatusDoCorpo {
    param([string]$Corpo)
    try { return [string](($Corpo | ConvertFrom-Json).status) } catch { return '' }
}

# Busca e máscara da subscription key (Test-ExigeChave, Get-ChaveDaSubscription e Format-ChaveMascarada).
# Cópia intencional: cada script do aluno é autossuficiente. Mantenha igual em erp-simulado.ps1.
function Test-ExigeChave {
    # Consulta sem efeito: status de uma fatura que não existe. Se a API exige subscription, o APIM
    # responde 401 "missing subscription key" antes de chegar ao hub; se não exige, o hub dá 404.
    param([System.Net.Http.HttpClient]$Http, [string]$Base)
    try {
        $tarefa = $Http.GetAsync("$Base/faturas/00000000-0000-0000-0000-000000000000/status")
        while (-not $tarefa.Wait(200)) { } # fatias de 200 ms: o Ctrl+C consegue interromper
        $resposta = $tarefa.Result
        try {
            if ([int]$resposta.StatusCode -ne 401) { return $false }
            $texto = $resposta.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            return ($texto -match 'subscription key')
        }
        finally { $resposta.Dispose() }
    }
    catch {
        return $false # sem resposta agora: as chamadas seguintes mostram o erro
    }
}

function Get-ChaveDaSubscription {
    # CONCEITO: a chave é buscada na hora (az rest no listSecrets do APIM) e fica só em memória.
    # Nada de chave em arquivo, em variável de ambiente persistente ou no histórico do terminal.
    # Se não der para buscar, avisa em amarelo e devolve vazio. Quem chama decide: o ERP segue
    # sem chave (o relatório mostra os 401) e o smoke test para.
    param([hashtable]$Ambiente, [string]$Nome)
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Host 'Aviso: não consegui buscar a subscription key: o Azure CLI (az) não foi encontrado.' -ForegroundColor Yellow
        Write-Host '  Instale o Azure CLI e rode az login.' -ForegroundColor Yellow
        return ''
    }
    $assinatura = "$($Ambiente['SALDARO_SUBSCRIPTION'])".Trim()
    $grupo = "$($Ambiente['SALDARO_RG'])".Trim()
    $sufixo = "$($Ambiente['SALDARO_SUFIXO'])".Trim()
    if (-not $assinatura -or -not $grupo -or -not $sufixo) {
        Write-Host 'Aviso: faltam SALDARO_SUBSCRIPTION, SALDARO_RG ou SALDARO_SUFIXO no .env.local para buscar a subscription key.' -ForegroundColor Yellow
        Write-Host '  Rode o deploy.ps1 de novo para regravar o arquivo.' -ForegroundColor Yellow
        return ''
    }
    $apim = "apim-saldaro-$sufixo"
    $uri = "https://management.azure.com/subscriptions/$assinatura/resourceGroups/$grupo/providers/Microsoft.ApiManagement/service/$apim/subscriptions/$Nome/listSecrets?api-version=2024-05-01"
    $saida = $null
    try {
        # Sem "| Select-Object -First 1": ele encerra o pipeline na primeira linha e o $LASTEXITCODE
        # não é atualizado (ficaria o do comando anterior, ou nulo num pwsh novo).
        $saida = & az rest --method post --url $uri --query primaryKey --output tsv --only-show-errors 2>$null
        $codigoDoAz = $LASTEXITCODE
    }
    catch {
        $codigoDoAz = 1
    }
    $chave = "$(@($saida)[0])".Trim()
    if ($codigoDoAz -ne 0 -or -not $chave) {
        Write-Host "Aviso: não consegui ler a chave da subscription '$Nome' do $apim (az rest no listSecrets)." -ForegroundColor Yellow
        Write-Host '  Confira o az login (az account show) e se o cenário 1 pronto está implantado: as subscriptions cliente-a e cliente-b só existem nele.' -ForegroundColor Yellow
        return ''
    }
    return $chave
}

function Format-ChaveMascarada {
    param([string]$Chave)
    if ($Chave.Length -le 4) { return '****' }
    return '****' + $Chave.Substring($Chave.Length - 4)
}

function Write-Passo {
    param([string]$Texto, [string]$Cor = 'Gray')
    Write-Host ('  [{0:mm\:ss}] {1}' -f $script:relogio.Elapsed, $Texto) -ForegroundColor $Cor
}

function Write-ConsultasKql {
    param([string]$OperationId, [string]$FaturaId)
    Write-Host ''
    Write-Host '  No Application Insights (Logs), a operação do POST:'
    Write-Host '    union requests, dependencies, customEvents, exceptions'
    Write-Host "    | where operation_Id == `"$OperationId`""
    Write-Host '    | project timestamp, itemType, name, resultCode, cloud_RoleName'
    Write-Host '    | order by timestamp asc'
    if ($FaturaId) {
        Write-Host '  A parte assíncrona (webhook do banco, filas, baixa, notificação) pode ter outro operation_Id;'
        Write-Host '  para a jornada inteira, filtre pelo faturaId:'
        Write-Host '    customEvents'
        Write-Host "    | where tostring(customDimensions.faturaId) == `"$FaturaId`""
        Write-Host '    | project timestamp, name, operation_Id, cloud_RoleName'
        Write-Host '    | order by timestamp asc'
    }
    Write-Host '  A telemetria leva de 2 a 5 minutos para aparecer.' -ForegroundColor DarkGray
}

function Stop-SmokeTest {
    param([string]$Motivo, [string[]]$Dicas = @())
    $faixa = '=' * 66
    Write-Host ''
    Write-Host $faixa -ForegroundColor Red
    Write-Host ' SMOKE TEST: FALHOU' -ForegroundColor Red
    Write-Host $faixa -ForegroundColor Red
    Write-Host "  Motivo: $Motivo" -ForegroundColor Red
    if ($script:referenciaErp) { Write-Host "  referenciaErp: $($script:referenciaErp)" }
    if ($script:faturaId) { Write-Host "  faturaId:      $($script:faturaId)" }
    if ($script:operationId) { Write-Host "  operation_Id:  $($script:operationId)" }
    Write-Host ('  Tempo total:   {0} s' -f $script:relogio.Elapsed.TotalSeconds.ToString('N1', (Get-CulturaPtBr)))
    foreach ($dica in $Dicas) { Write-Host "  - $dica" -ForegroundColor Yellow }
    if ($script:operationId) { Write-ConsultasKql -OperationId $script:operationId -FaturaId $script:faturaId }
    Write-Host $faixa -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------------------------------------
$codificacaoAnterior = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false) } catch { }
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$script:faturaId = ''
$script:referenciaErp = ''
$script:operationId = ''
$script:relogio = [System.Diagnostics.Stopwatch]::StartNew()
$manipulador = [System.Net.Http.SocketsHttpHandler]::new()
$manipulador.AllowAutoRedirect = $false # o 303 é parte da lição: o script segue o Location à mão
$http = [System.Net.Http.HttpClient]::new($manipulador)
$http.Timeout = [TimeSpan]::FromSeconds($TimeoutHttpSegundos)
$http.DefaultRequestHeaders.UserAgent.ParseAdd('saldaro-smoke-test/1.0')

try {
    $caminhoEnv = Join-Path $PSScriptRoot '.env.local'
    if (-not (Test-Path -LiteralPath $caminhoEnv)) {
        Stop-SmokeTest -Motivo "não encontrei $caminhoEnv." -Dicas @('Rode o deploy.ps1 primeiro: ele cria o ambiente e grava esse arquivo com as URLs (sem segredos).')
    }
    $ambiente = Read-EnvLocal -Caminho $caminhoEnv
    $apim = Get-ValorObrigatorio -Valores $ambiente -Chave 'SALDARO_APIM_URL' -Caminho $caminhoEnv
    $hub = Get-ValorObrigatorio -Valores $ambiente -Chave 'SALDARO_HUB_URL' -Caminho $caminhoEnv
    $banco = Get-ValorObrigatorio -Valores $ambiente -Chave 'SALDARO_BANCO_URL' -Caminho $caminhoEnv
    $prazo = [DateTime]::UtcNow.AddSeconds($TimeoutSegundos)
    $ptBr = Get-CulturaPtBr

    $faixa = '=' * 66
    Write-Host $faixa -ForegroundColor Cyan
    Write-Host ' SMOKE TEST · Saldaro Hub (empresa fictícia)' -ForegroundColor Cyan
    Write-Host $faixa -ForegroundColor Cyan
    Write-Host "  APIM:   $apim"
    Write-Host "  Hub:    $hub"
    Write-Host "  Banco:  $banco"
    Write-Host "  Prazo:  $TimeoutSegundos s"
    Write-Host $faixa -ForegroundColor Cyan

    # 1. Aquecimento: as Function Apps podem estar frias (cold start).
    foreach ($app in @(@{ Nome = 'hub'; Url = "$hub/saude" }, @{ Nome = 'banco'; Url = "$banco/saude" })) {
        $avisado = $false
        while ($true) {
            $saude = Invoke-Saldaro -Http $http -Metodo 'GET' -Url $app.Url
            if ($saude.Status -eq 200) { break }
            if ([DateTime]::UtcNow -ge $prazo) {
                Stop-SmokeTest -Motivo "o $($app.Nome) não respondeu 200 em $($app.Url) (último: $($saude.Status) $($saude.Erro))." -Dicas @(
                    'Confira se o deploy publicou o código das Function Apps (deploy.ps1 sem -SemPublicar).',
                    'Veja os logs da Function App no Portal (Log stream) ou no Application Insights (exceptions).')
            }
            if (-not $avisado) {
                Write-Passo ("aguardando o {0} responder ({1})..." -f $app.Nome, $(if ($saude.Status) { $saude.Status } else { $saude.Erro })) 'DarkGray'
                $avisado = $true
            }
            Start-Sleep -Seconds 5
        }

        $resumo = 'ok'
        if ($app.Nome -eq 'hub') {
            try {
                $info = $saude.Corpo | ConvertFrom-Json
                $toggles = $info.toggles
                $ligados = @()
                if ($toggles -is [System.Array]) {
                    $ligados = @($toggles)
                }
                elseif ($toggles -is [System.Management.Automation.PSCustomObject]) {
                    $ligados = @($toggles.PSObject.Properties | Where-Object { $_.Value -eq $true } | ForEach-Object { $_.Name })
                }
                $resumo = 'versão {0} · toggles ligados: {1}' -f $info.versao, $(if ($ligados.Count) { $ligados -join ', ' } else { 'nenhum (baseline)' })
            }
            catch { $resumo = 'ok' }
        }
        Write-Passo ("{0}: {1} ({2} ms)" -f $app.Nome, $resumo, $saude.LatenciaMs) 'Green'
    }

    # 2. Subscription key, só se a API saldaro exigir (cenário 1 pronto).
    $cabecalhosDeConsulta = @{}
    if (Test-ExigeChave -Http $http -Base $apim) {
        $chave = Get-ChaveDaSubscription -Ambiente $ambiente -Nome $Cliente
        if (-not $chave) {
            # Sem a chave, o POST levaria 401: para já, com o motivo no aviso acima.
            Stop-SmokeTest -Motivo "a API exige subscription key e não consegui ler a da subscription $Cliente (veja o aviso acima)."
        }
        $cabecalhosDeConsulta['Ocp-Apim-Subscription-Key'] = $chave
        Write-Passo ("a API exige subscription key: usando a da subscription {0} ({1}), só em memória" -f $Cliente, (Format-ChaveMascarada $chave))
    }

    # 3. A fatura. O traceparent define o operation_Id (trace-id) de toda a operação do POST.
    $script:operationId = [guid]::NewGuid().ToString('N')
    $origemDoOperationId = 'trace-id do traceparent enviado'
    $traceparent = '00-{0}-{1}-01' -f $script:operationId, [guid]::NewGuid().ToString('N').Substring(0, 16)
    $script:referenciaErp = 'SMOKE-{0}' -f (Get-Date -Format 'yyyyMMddHHmmss')
    $fatura = [ordered]@{
        valor         = [decimal]::Parse('1250.00', [cultureinfo]::InvariantCulture)
        vencimento    = (Get-Date).Date.AddDays(10).ToString('yyyy-MM-dd')
        pagadorCnpj   = New-CnpjTeste
        referenciaErp = $script:referenciaErp
        caos          = ''
    }
    $cabecalhosDoPost = @{
        'x-cliente-id'    = $Cliente
        'Idempotency-Key' = [guid]::NewGuid().ToString()
        'traceparent'     = $traceparent
    } + $cabecalhosDeConsulta
    $post = Invoke-Saldaro -Http $http -Metodo 'POST' -Url "$apim/faturas" -Cabecalhos $cabecalhosDoPost -Corpo ($fatura | ConvertTo-Json -Compress)
    if ($post.Status -notin 201, 202) {
        $trecho = if ($post.Corpo.Length -gt 300) { $post.Corpo.Substring(0, 300) + '...' } else { $post.Corpo }
        $dicas = switch ($post.Status) {
            0 { @('Sem resposta do APIM: confira SALDARO_APIM_URL e a sua conexão.') }
            401 { @('O APIM recusou a chamada (401). Se a API exige subscription key, confira as subscriptions cliente-a/cliente-b.') }
            404 { @('O APIM Consumption leva alguns minutos para ativar a API depois do primeiro deploy: tente de novo em instantes.') }
            default { @('Veja as exceptions do hub no Application Insights com a consulta abaixo.') }
        }
        Stop-SmokeTest -Motivo ("POST /faturas respondeu {0} {1} {2}. Corpo: {3}" -f $post.Status, $post.Motivo, $post.Erro, $trecho) -Dicas $dicas
    }

    try { $script:faturaId = [string](($post.Corpo | ConvertFrom-Json).faturaId) } catch { $script:faturaId = '' }
    if (-not $script:faturaId) {
        Stop-SmokeTest -Motivo "POST /faturas respondeu $($post.Status), mas o corpo não trouxe faturaId: $($post.Corpo)"
    }
    if ($post.TraceId -and $post.TraceId -ne $script:operationId) {
        $script:operationId = $post.TraceId
        $origemDoOperationId = 'trace-id devolvido no header da resposta'
    }
    $modo = if ($post.Status -eq 202) { 'request-reply assíncrono' } else { 'baseline síncrono' }
    Write-Passo ("POST /faturas -> {0} {1} em {2} ms ({3}) · faturaId {4}" -f $post.Status, $post.Motivo, $post.LatenciaMs, $modo, $script:faturaId) 'Green'

    # 4. Acompanha o status até o 303 (baixada ou notificada).
    $urlStatus = if ($post.Status -eq 202 -and $post.Location) { Resolve-Location -Base "$apim/faturas" -Location $post.Location } else { "$apim/faturas/$($script:faturaId)/status" }
    $intervalo = if ($null -ne $post.RetryAfter) { [math]::Min([double]10, [math]::Max([double]1, [double]$post.RetryAfter)) } else { $IntervaloPadraoSegundos }
    $ultimo = Get-StatusDoCorpo -Corpo $post.Corpo
    if ($ultimo) { Write-Passo "status: $ultimo" }
    $ultimoProblema = ''
    $faturaFinal = $null
    $statusFinal = ''
    while ([DateTime]::UtcNow -lt $prazo) {
        Start-Sleep -Seconds $intervalo
        $consulta = Invoke-Saldaro -Http $http -Metodo 'GET' -Url $urlStatus -Cabecalhos $cabecalhosDeConsulta
        if ($consulta.Status -eq 303) {
            # CONCEITO: 303 See Other = "terminou; o resultado está neste outro endereço". Pelo
            # contrato, o hub só responde 303 quando a fatura está baixada ou notificada.
            $urlFatura = Resolve-Location -Base $urlStatus -Location $consulta.Location
            if (-not $urlFatura) { $urlFatura = "$apim/faturas/$($script:faturaId)" }
            Write-Passo "303 See Other -> $urlFatura" 'Green'
            $statusFinal = 'baixada'
            $resposta = Invoke-Saldaro -Http $http -Metodo 'GET' -Url $urlFatura -Cabecalhos $cabecalhosDeConsulta
            if ($resposta.Status -eq 200) {
                $faturaFinal = $resposta.Corpo | ConvertFrom-Json
                if ($faturaFinal.status) { $statusFinal = [string]$faturaFinal.status }
            }
            else {
                Write-Passo "aviso: GET da fatura respondeu $($resposta.Status) $($resposta.Erro); o 303 já indica a baixa" 'Yellow'
            }
            break
        }
        if ($consulta.Status -eq 200) {
            $status = Get-StatusDoCorpo -Corpo $consulta.Corpo
            if ($status -and $status -ne $ultimo) {
                Write-Passo "status: $status"
                $ultimo = $status
            }
            if ($status -in 'baixada', 'notificada') {
                $statusFinal = $status
                break
            }
            continue
        }
        $problema = if ($consulta.Status) { "GET status -> $($consulta.Status) $($consulta.Motivo)" } else { "GET status sem resposta: $($consulta.Erro)" }
        if ($problema -ne $ultimoProblema) {
            Write-Passo $problema 'Yellow'
            $ultimoProblema = $problema
        }
    }

    if ($statusFinal -notin 'baixada', 'notificada') {
        $ultimoVisto = if ($statusFinal) { $statusFinal } elseif ($ultimo) { $ultimo } else { 'desconhecido' }
        Stop-SmokeTest -Motivo "a fatura não chegou a baixada/notificada em $TimeoutSegundos s (último status: $ultimoVisto)." -Dicas @(
            'cobranca-emitida parado: o webhook do banco não chegou ou não foi processado (veja exceptions e a DLQ de sbq-confirmacoes-banco).',
            'recebida parado: o worker EmitirCobranca não consumiu sbq-faturas-recebidas ou o banco falhou.',
            'Rode de novo com mais prazo: ./scripts/smoke-test.ps1 -TimeoutSegundos 300')
    }
    Write-Passo "status: $statusFinal" 'Green'
    $baixadaEm = $script:relogio.Elapsed

    # 5. A notificação ao parceiro vem logo depois da baixa; espera um pouco por ela, sem exigir.
    if ($statusFinal -eq 'baixada') {
        $limite = [DateTime]::UtcNow.AddSeconds($EsperaPelaNotificacaoSegundos)
        if ($limite -gt $prazo) { $limite = $prazo }
        $urlFatura = "$apim/faturas/$($script:faturaId)"
        while ([DateTime]::UtcNow -lt $limite) {
            Start-Sleep -Seconds $IntervaloPadraoSegundos
            $consulta = Invoke-Saldaro -Http $http -Metodo 'GET' -Url $urlFatura -Cabecalhos $cabecalhosDeConsulta
            if ($consulta.Status -ne 200) { continue }
            try { $faturaFinal = $consulta.Corpo | ConvertFrom-Json } catch { continue }
            if ([string]$faturaFinal.status -eq 'notificada') {
                $statusFinal = 'notificada'
                Write-Passo 'status: notificada' 'Green'
                break
            }
        }
        if ($statusFinal -eq 'baixada') {
            Write-Passo "ainda sem notificação ao parceiro depois de $EsperaPelaNotificacaoSegundos s (não é obrigatório para o smoke test)" 'DarkGray'
        }
    }

    $faixa = '=' * 66
    Write-Host ''
    Write-Host $faixa -ForegroundColor Green
    Write-Host (' SMOKE TEST: OK (baixada em {0} s)' -f $baixadaEm.TotalSeconds.ToString('N1', $ptBr)) -ForegroundColor Green
    Write-Host $faixa -ForegroundColor Green
    Write-Host "  faturaId:          $($script:faturaId)"
    Write-Host "  referenciaErp:     $($script:referenciaErp)"
    Write-Host "  Status final:      $statusFinal"
    if ($null -ne $faturaFinal.idCobranca) { Write-Host "  idCobranca:        $($faturaFinal.idCobranca)" }
    if ($null -ne $faturaFinal.quantidadeBaixas) { Write-Host "  quantidadeBaixas:  $($faturaFinal.quantidadeBaixas)" }
    Write-Host ('  Tempo total:       {0} s' -f $script:relogio.Elapsed.TotalSeconds.ToString('N1', $ptBr))
    Write-Host "  operation_Id:      $($script:operationId) ($origemDoOperationId; vale para o POST)"
    Write-ConsultasKql -OperationId $script:operationId -FaturaId $script:faturaId
    Write-Host $faixa -ForegroundColor Green
    exit 0
}
finally {
    $http.Dispose()
    try { [Console]::OutputEncoding = $codificacaoAnterior } catch { }
}
