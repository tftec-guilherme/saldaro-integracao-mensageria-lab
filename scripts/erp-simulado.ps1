#Requires -Version 7.0

<#
.SYNOPSIS
  ERP simulado: envia faturas para o Saldaro Hub (pelo APIM ou direto) e mede o que volta.

.DESCRIPTION
  Faz o papel do ERP de uma empresa cliente mandando faturas para POST /faturas, com o header
  x-cliente-id. As URLs vêm de scripts/.env.local, que o deploy.ps1 grava (sem segredos).

  CONCEITOS PARA A AULA
  - Retry com backoff exponencial e jitter: depois de uma falha retentável (429, 5xx ou timeout),
    o ERP espera um tempo que dobra a cada tentativa (cerca de 1, 2, 4 e 8 s), com uma parte
    aleatória (jitter) para muitos clientes não voltarem todos no mesmo instante.
  - Teto: no máximo 5 tentativas por fatura (ajustável com -MaxTentativas) e no máximo 30 s de
    espera entre duas tentativas.
  - Retry-After: quando o APIM ou o hub dizem quanto esperar, o ERP respeita, limitado ao teto.
  - Metralhadora (-SemBackoff): reenvia na hora, sem esperar e ignorando o Retry-After. É o
    comportamento do parceiro que derrubou o hub no incidente do 3.1.
  - Idempotency-Key (-IdempotencyKey): uma chave por fatura, repetida em todos os reenvios dela,
    para o hub reconhecer o reenvio e devolver a mesma fatura (3.2).
  - Timeout do cliente de 35 s, logo acima dos 30 s do APIM: o ERP enxerga o 504 do gateway.

  Cada fatura gerada tem uma referenciaErp única, ERP-<yyyyMMddHHmmss>-<n> (o número da fatura
  no ERP). Os reenvios mandam exatamente a mesma fatura (mesma referenciaErp, mesmo valor, mesmo
  pagador): para o ERP, é a mesma fatura. Se o hub criar duas faturas para a mesma referenciaErp,
  é duplicata, e o kql/faturas-por-referencia-erp.kql mostra (é a prova do Incidente 1).
  Os CNPJs de pagador são de teste: gerados por algoritmo, com dígitos verificadores válidos.

  SUBSCRIPTION KEY (só no cenário 1 pronto)
  No 3.1 consertado, a API saldaro do APIM exige subscription key e o rate limit é de 60 chamadas
  por minuto por subscription (uma por empresa cliente: cliente-a e cliente-b). Antes de enviar, o
  script faz uma consulta sem efeito (GET de status de uma fatura inexistente): se o APIM responder
  401 pedindo subscription key, ele busca a chave da subscription com o nome do -Cliente via
  `az rest` (listSecrets) e passa a enviar o header Ocp-Apim-Subscription-Key. A chave fica só em
  memória: nunca vai para arquivo nem aparece inteira na tela. O header x-cliente-id continua indo,
  porque é com ele que o hub identifica o cliente. Fora do cenário 1 pronto a API não pede chave e
  nada disso acontece; se a busca falhar, o script avisa e segue sem chave (nunca para por isso).

  No fim, imprime um relatório (resultado final por fatura, todas as respostas por código HTTP,
  média de tentativas, latência, tempo total e uma linha por fatura com referenciaErp, faturaId
  devolvido, tentativas e o código de cada tentativa) e grava um CSV com uma linha por fatura em
  scripts/saida/erp-<yyyyMMdd-HHmmss>.csv (UTF-8, separador vírgula; leia com Import-Csv), com as
  colunas referenciaErp,faturaId,cliente,tentativas,statusFinal,codigos,duracaoMs,idempotencyKey.
  Em codigos, os códigos HTTP de cada tentativa separados por "|" (ex.: 504|504); tentativa sem
  resposta HTTP aparece como timeout ou erro-rede. faturaId fica vazio quando nenhuma tentativa
  devolveu id.

.PARAMETER Faturas
  Quantas faturas enviar (1 a 100000).

.PARAMETER Cliente
  Empresa cliente, enviada no header x-cliente-id: cliente-a (padrão) ou cliente-b. O rate limit do
  APIM (3.1) conta separado por cliente.

.PARAMETER ReqPorMin
  Ritmo máximo de faturas novas por minuto; os reenvios não entram na conta. 0 (padrão) = sem
  limite: o ritmo fica limitado só pelo -Paralelismo.

.PARAMETER Paralelismo
  Quantas faturas ficam em andamento ao mesmo tempo (1 a 200; padrão 10).

.PARAMETER MaxTentativas
  Teto de tentativas por fatura, contando a primeira (1 a 10; padrão 5). Com 1, não há reenvio.

.PARAMETER SemBackoff
  Metralhadora: reenvia imediatamente, sem espera e sem honrar o Retry-After (continua limitado ao
  -MaxTentativas).

.PARAMETER IdempotencyKey
  Envia o header Idempotency-Key: uma chave nova por fatura, repetida em todos os reenvios dela.

.PARAMETER Caos
  Botões de caos do banco simulado no formato "chave=valor;chave=valor" (ex.: "latencia=45;duplicar=3").
  Vai no campo caos do corpo da fatura, com ";" trocado por "&", e o hub repassa ao banco. Chaves
  aceitas: latencia, atrasoWebhook, duplicar, embaralhar, assinatura, valorComVirgula, falhaTransiente.

.PARAMETER Direto
  Envia direto ao hub (SALDARO_HUB_URL), sem passar pelo APIM: sem rate limit, sem subscription key e
  sem o corte de 30 s do gateway.

.PARAMETER ComChave
  Busca a subscription key do -Cliente logo de início (az rest no listSecrets do APIM), sem esperar a
  detecção automática do 401. Usa SALDARO_SUBSCRIPTION, SALDARO_RG e SALDARO_SUFIXO do .env.local.

.EXAMPLE
  ./scripts/erp-simulado.ps1 -Faturas 10

  Dez faturas do cliente-a pelo APIM, com backoff.

.EXAMPLE
  ./scripts/erp-simulado.ps1 -Faturas 300 -ReqPorMin 300 -SemBackoff

  3.1, a quebra: o cliente-a vira metralhadora. Em outro terminal, ao mesmo tempo:
  ./scripts/erp-simulado.ps1 -Faturas 10 -Cliente cliente-b -ReqPorMin 10
  Com o cenário 1 pronto, cada um usa a subscription key do seu cliente e só o cliente-a leva 429.

.EXAMPLE
  ./scripts/erp-simulado.ps1 -Faturas 1 -Caos "latencia=45" -MaxTentativas 2

  3.2, o incidente: o APIM corta em 30 s (504), o ERP reenvia a mesma referenciaErp e o hub registra
  outra fatura e outra cobrança. Duas tentativas bastam para a demonstração caber no tempo.

.EXAMPLE
  ./scripts/erp-simulado.ps1 -Faturas 1000 -Paralelismo 20 -IdempotencyKey

  3.2, a prova: 1.000 faturas em rajada, sem 5xx, com reenvio seguro.

.EXAMPLE
  ./scripts/erp-simulado.ps1 -Faturas 10 -Caos "valorComVirgula=true"

  3.4, primeira rajada: o banco parceiro passa a mandar o valor como texto ("1.250,00"). Logo em
  seguida, a segunda rajada, separada para a DLQ mostrar as duas causas:
  ./scripts/erp-simulado.ps1 -Faturas 20 -Caos "falhaTransiente=50"
  (metade das chamadas ao banco responde 503 e metade dos pagamentos chega com falha transiente).

.NOTES
  Version-anchor: Q4-2026. Requer PowerShell 7 (Windows, macOS ou Linux). Lê SALDARO_APIM_URL e
  SALDARO_HUB_URL de scripts/.env.local (e, só quando a API exige subscription key,
  SALDARO_SUBSCRIPTION, SALDARO_RG e SALDARO_SUFIXO, com o Azure CLI logado). Não grava segredos.
  Código de saída: 0 se todas as faturas foram aceitas (201 ou 202); 1 caso contrário.
  Ctrl+C interrompe e ainda imprime o relatório parcial e grava o CSV.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateRange(1, 100000)]
    [int]$Faturas,

    [ValidateSet('cliente-a', 'cliente-b')]
    [string]$Cliente = 'cliente-a',

    [ValidateRange(0, 100000)]
    [int]$ReqPorMin = 0,

    [ValidateRange(1, 200)]
    [int]$Paralelismo = 10,

    [ValidateRange(1, 10)]
    [int]$MaxTentativas = 5,

    [switch]$SemBackoff,

    [switch]$IdempotencyKey,

    [string]$Caos = '',

    [switch]$Direto,

    [switch]$ComChave
)

$ErrorActionPreference = 'Stop'

# Teto do retry (contrato do kit, seção 9): 5 tentativas (-MaxTentativas), espera máxima de 30 s,
# timeout do cliente de 35 s.
$EsperaBaseSegundos = 1.0
$EsperaMaximaSegundos = 30.0
$TimeoutClienteSegundos = 35
$ChavesDeCaos = 'latencia', 'atrasoWebhook', 'duplicar', 'embaralhar', 'assinatura', 'valorComVirgula', 'falhaTransiente'
$Categorias = '201', '202', '429', '5xx', 'timeout', 'outros'

function Stop-ComErro {
    param([string]$Mensagem)
    Write-Host "ERRO: $Mensagem" -ForegroundColor Red
    if ($null -ne $script:codificacaoAnterior) {
        try { [Console]::OutputEncoding = $script:codificacaoAnterior } catch { }
    }
    exit 1
}

# Cópia intencional: cada script do aluno é autossuficiente. Mantenha igual em deploy.ps1,
# limpar.ps1, check-drift.ps1, smoke-test.ps1 e triagem-dlq.ps1.
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
        Stop-ComErro "a chave $Chave não está em $Caminho. Rode o deploy.ps1 de novo para regravar o arquivo."
    }
    return $valor.TrimEnd('/')
}

function Get-CulturaPtBr {
    try { return [cultureinfo]::GetCultureInfo('pt-BR') } catch { return [cultureinfo]::InvariantCulture }
}

# Gerador de CNPJ de teste (Get-DigitoCnpj e New-CnpjTeste).
# Cópia intencional: cada script do aluno é autossuficiente. Mantenha igual em smoke-test.ps1.
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

function ConvertTo-CaosQuery {
    # "latencia=45;duplicar=3" -> "latencia=45&duplicar=3" (o formato da query string do banco)
    param([string]$Texto)
    if ([string]::IsNullOrWhiteSpace($Texto)) { return '' }

    $pares = foreach ($parte in ($Texto -split '[;&]')) {
        $par = $parte.Trim()
        if (-not $par) { continue }
        $chave, $valor = $par -split '=', 2
        $chave = "$chave".Trim()
        $valor = "$valor".Trim()
        $oficial = $ChavesDeCaos | Where-Object { $_ -eq $chave } | Select-Object -First 1
        if (-not $oficial) {
            Stop-ComErro "chave de caos desconhecida: '$chave'. Use: $($ChavesDeCaos -join ', ')."
        }
        if (-not $valor) {
            Stop-ComErro "a chave de caos '$oficial' está sem valor (formato: chave=valor;chave=valor)."
        }
        '{0}={1}' -f $oficial, [uri]::EscapeDataString($valor)
    }
    return ($pares -join '&')
}

function Get-Percentil {
    param([double[]]$Ordenados, [double]$Percentil)
    if ($Ordenados.Count -eq 0) { return 0 }
    $posto = [math]::Ceiling($Percentil / 100 * $Ordenados.Count)
    return $Ordenados[[math]::Max(0, [math]::Min($Ordenados.Count - 1, $posto - 1))]
}

# Busca e máscara da subscription key (Test-ExigeChave, Get-ChaveDaSubscription e Format-ChaveMascarada).
# Cópia intencional: cada script do aluno é autossuficiente. Mantenha igual em smoke-test.ps1.
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

function Write-Relatorio {
    param(
        [object[]]$Resultados,
        [timespan]$Duracao,
        [string]$Destino,
        [string]$Modo,
        [string]$CaminhoCsv,
        [bool]$Interrompido
    )

    $ptBr = Get-CulturaPtBr
    $faixa = '=' * 66
    Write-Host ''
    Write-Host $faixa -ForegroundColor Cyan
    Write-Host ' RELATÓRIO DO ERP SIMULADO' -ForegroundColor Cyan
    Write-Host $faixa -ForegroundColor Cyan
    Write-Host ("  Cliente:          {0}   Destino: {1}" -f $Cliente, $Destino)
    Write-Host ("  Reenvio:          {0}" -f $Modo)
    Write-Host ("  Faturas:          {0} de {1}{2}" -f $Resultados.Count, $Faturas, $(if ($Interrompido) { '  (INTERROMPIDO)' } else { '' }))
    Write-Host ("  Tempo total:      {0} s" -f $Duracao.TotalSeconds.ToString('N1', $ptBr))
    if ($Resultados.Count -eq 0) {
        Write-Host '  Nenhuma fatura concluída.'
        Write-Host $faixa -ForegroundColor Cyan
        return
    }

    $respostas = @($Resultados | ForEach-Object { $_.Respostas })
    $finais = @{}
    $todas = @{}
    foreach ($categoria in $Categorias) { $finais[$categoria] = 0; $todas[$categoria] = 0 }
    foreach ($r in $Resultados) {
        $categoria = if ($Categorias -contains $r.Resultado) { $r.Resultado } else { 'outros' }
        $finais[$categoria]++
    }
    foreach ($r in $respostas) {
        $categoria = if ($Categorias -contains $r.Categoria) { $r.Categoria } else { 'outros' }
        $todas[$categoria]++
    }

    $rotulos = [ordered]@{
        '201'     = '201 Created'
        '202'     = '202 Accepted'
        '429'     = '429 Too Many Requests'
        '5xx'     = '5xx (erro do servidor ou do gateway)'
        'timeout' = "timeout do cliente ($TimeoutClienteSegundos s)"
        'outros'  = 'outros (4xx, erro de rede)'
    }
    Write-Host ''
    Write-Host '  Resultado final por fatura'
    foreach ($categoria in $Categorias) {
        $cor = if ($categoria -in '201', '202') { 'Green' } elseif ($finais[$categoria] -gt 0) { 'Yellow' } else { 'Gray' }
        Write-Host ("    {0,-40} {1,6}" -f $rotulos[$categoria], $finais[$categoria]) -ForegroundColor $cor
    }

    Write-Host ''
    Write-Host ("  Todas as respostas ({0} chamadas HTTP)" -f $respostas.Count)
    Write-Host ('    ' + (($Categorias | ForEach-Object { '{0}: {1}' -f $_, $todas[$_] }) -join ' · '))
    $porCodigo = @($respostas | Group-Object -Property { if ($_.Status -gt 0) { [string]$_.Status } else { $_.Categoria } } | Sort-Object -Property Name)
    Write-Host ('    por código: ' + (($porCodigo | ForEach-Object { '{0} x{1}' -f $_.Name, $_.Count }) -join ' · '))

    $media = ($Resultados | Measure-Object -Property Tentativas -Average -Maximum)
    Write-Host ''
    Write-Host ("  Tentativas por fatura:  média {0} · máximo {1}" -f ([double]$media.Average).ToString('N2', $ptBr), $media.Maximum)

    $latencias = [double[]]@($respostas | Where-Object { $_.Status -in 201, 202 } | ForEach-Object { [double]$_.LatenciaMs } | Sort-Object)
    if ($latencias.Count -gt 0) {
        Write-Host ("  Latência das respostas 2xx:  p50 {0} ms · p95 {1} ms · máx. {2} ms" -f `
            (Get-Percentil $latencias 50).ToString('N0', $ptBr),
            (Get-Percentil $latencias 95).ToString('N0', $ptBr),
            $latencias[-1].ToString('N0', $ptBr))
    }

    $erros = @($respostas | Where-Object { $_.Erro } | Group-Object -Property Erro | Sort-Object -Property Count -Descending | Select-Object -First 5)
    if ($erros.Count -gt 0) {
        Write-Host ''
        Write-Host '  Principais erros'
        foreach ($erro in $erros) {
            Write-Host ("    {0,5}x  {1}" -f $erro.Count, $erro.Name) -ForegroundColor Yellow
        }
    }

    $limiteDaLista = 20
    $ordenadas = @($Resultados | Sort-Object -Property @{ Expression = { $_.Resultado -in '201', '202' } }, Indice)
    Write-Host ''
    if ($Resultados.Count -gt $limiteDaLista) {
        Write-Host ("  Por fatura ({0} de {1}, as com problema primeiro; todas estão no CSV)" -f $limiteDaLista, $Resultados.Count)
    }
    else {
        Write-Host '  Por fatura'
    }
    $larguraDaReferencia = [math]::Max(12, ($ordenadas | Select-Object -First $limiteDaLista | ForEach-Object { $_.ReferenciaErp.Length } | Measure-Object -Maximum).Maximum)
    Write-Host ("    {0}  {1,-36}  {2,10}  {3}" -f 'referenciaErp'.PadRight($larguraDaReferencia), 'faturaId devolvido', 'tentativas', 'códigos (por tentativa)')
    foreach ($r in ($ordenadas | Select-Object -First $limiteDaLista)) {
        $faturaIdExibido = if ($r.FaturaId) { $r.FaturaId } else { '(nenhum)' }
        $cor = if ($r.Resultado -in '201', '202') { 'Gray' } else { 'Yellow' }
        Write-Host ("    {0}  {1,-36}  {2,10}  {3}" -f $r.ReferenciaErp.PadRight($larguraDaReferencia), $faturaIdExibido, $r.Tentativas, $r.StatusPorTentativa) -ForegroundColor $cor
    }

    $reenviadas = @($Resultados | Where-Object { $_.Tentativas -gt 1 })
    if ($reenviadas.Count -gt 0 -and ($todas['5xx'] + $todas['timeout']) -gt 0 -and -not $IdempotencyKey) {
        Write-Host ''
        Write-Host '  Atenção: houve reenvio depois de 5xx/timeout sem Idempotency-Key. Se o hub já tinha registrado a' -ForegroundColor Yellow
        Write-Host '  primeira tentativa, a mesma referenciaErp virou mais de uma fatura (e mais de uma cobrança no banco).' -ForegroundColor Yellow
        Write-Host '  Confira no Application Insights com kql/faturas-por-referencia-erp.kql.' -ForegroundColor Yellow
    }

    if (@($respostas | Where-Object { $_.Status -eq 401 }).Count -gt 0) {
        Write-Host ''
        Write-Host '  Houve 401: se a API passou a exigir subscription key, rode de novo com -ComChave e confira' -ForegroundColor Yellow
        Write-Host '  no APIM as subscriptions cliente-a e cliente-b da API saldaro.' -ForegroundColor Yellow
    }

    Write-Host ''
    Write-Host "  CSV: $CaminhoCsv"
    Write-Host $faixa -ForegroundColor Cyan
}

# ---------------------------------------------------------------------------------------------
# Preparação
# ---------------------------------------------------------------------------------------------
$codificacaoAnterior = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false) } catch { }
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$caminhoEnv = Join-Path $PSScriptRoot '.env.local'
if (-not (Test-Path -LiteralPath $caminhoEnv)) {
    Stop-ComErro "não encontrei $caminhoEnv. Rode o deploy.ps1 primeiro: ele cria o ambiente e grava esse arquivo com as URLs (sem segredos)."
}
$ambiente = Read-EnvLocal -Caminho $caminhoEnv
if ($Direto) {
    $base = Get-ValorObrigatorio -Valores $ambiente -Chave 'SALDARO_HUB_URL' -Caminho $caminhoEnv
    $destino = 'hub direto (sem APIM)'
}
else {
    $base = Get-ValorObrigatorio -Valores $ambiente -Chave 'SALDARO_APIM_URL' -Caminho $caminhoEnv
    $destino = 'APIM'
}
$url = "$base/faturas"
$caosQuery = ConvertTo-CaosQuery -Texto $Caos
$modo = if ($SemBackoff) {
    "metralhadora: reenvio imediato, sem espera (até $MaxTentativas tentativas)"
}
else {
    "backoff exponencial com jitter (até $MaxTentativas tentativas, espera máx. $EsperaMaximaSegundos s, honra Retry-After)"
}

# Um HttpClient só, compartilhado pelas threads (é thread-safe e reaproveita conexões).
$manipulador = [System.Net.Http.SocketsHttpHandler]::new()
$manipulador.AllowAutoRedirect = $false
$manipulador.PooledConnectionLifetime = [TimeSpan]::FromMinutes(2)
$http = [System.Net.Http.HttpClient]::new($manipulador)
$http.Timeout = [TimeSpan]::FromSeconds($TimeoutClienteSegundos)
$http.DefaultRequestHeaders.UserAgent.ParseAdd('saldaro-erp-simulado/1.0')

# Subscription key: só pelo APIM, e só quando a API exige (cenário 1 pronto) ou com -ComChave.
$chaveDaSubscription = ''
$tentouChave = $false
if (-not $Direto -and ($ComChave -or (Test-ExigeChave -Http $http -Base $base))) {
    $tentouChave = $true
    $chaveDaSubscription = Get-ChaveDaSubscription -Ambiente $ambiente -Nome $Cliente
}
$descricaoDaChave = if ($chaveDaSubscription) {
    "subscription {0} ({1}), obtida com az rest e mantida só em memória" -f $Cliente, (Format-ChaveMascarada $chaveDaSubscription)
}
elseif ($Direto) { 'não se aplica (sem APIM)' }
elseif ($tentouChave) { 'não obtida (veja o aviso acima); enviando sem chave' }
else { 'não exigida pela API' }

$faixa = '=' * 66
Write-Host $faixa -ForegroundColor Cyan
Write-Host ' ERP SIMULADO · Saldaro Hub (empresa fictícia)' -ForegroundColor Cyan
Write-Host $faixa -ForegroundColor Cyan
Write-Host "  Destino:          $url ($destino)"
Write-Host "  Cliente:          $Cliente (header x-cliente-id)"
Write-Host "  Subscription key: $descricaoDaChave"
Write-Host ("  Faturas:          {0} · paralelismo {1} · ritmo {2}" -f $Faturas, $Paralelismo, $(if ($ReqPorMin -gt 0) { "$ReqPorMin/min" } else { 'sem limite' }))
Write-Host "  Reenvio:          $modo"
Write-Host ("  Idempotency-Key:  {0}" -f $(if ($IdempotencyKey) { 'uma por fatura, repetida nos reenvios' } else { 'não enviada' }))
Write-Host ("  Caos:             {0}" -f $(if ($caosQuery) { $caosQuery } else { 'nenhum' }))
Write-Host $faixa -ForegroundColor Cyan

# Faturas geradas antes do envio, para a geração não pesar na latência medida.
$aleatorio = [System.Random]::new()
$agora = Get-Date
$carimbo = $agora.ToString('yyyyMMdd-HHmmss')
$carimboDoErp = $agora.ToString('yyyyMMddHHmmss')
$intervaloMs = if ($ReqPorMin -gt 0) { 60000.0 / $ReqPorMin } else { 0.0 }
$hoje = $agora.Date
$itens = for ($i = 1; $i -le $Faturas; $i++) {
    $centavos = $aleatorio.Next(10000, 2500001)
    $fatura = [ordered]@{
        valor         = [decimal]::Parse(($centavos / 100).ToString('0.00', [cultureinfo]::InvariantCulture), [cultureinfo]::InvariantCulture)
        vencimento    = $hoje.AddDays($aleatorio.Next(5, 46)).ToString('yyyy-MM-dd')
        pagadorCnpj   = New-CnpjTeste -Aleatorio $aleatorio
        # CONCEITO: a referência da fatura no ERP. Os reenvios repetem a mesma: é a mesma fatura.
        referenciaErp = 'ERP-{0}-{1}' -f $carimboDoErp, $i
        caos          = $caosQuery
    }
    [pscustomobject]@{
        Indice         = $i
        ReferenciaErp  = $fatura.referenciaErp
        IdempotencyKey = $(if ($IdempotencyKey) { [guid]::NewGuid().ToString() } else { '' })
        Corpo          = ($fatura | ConvertTo-Json -Compress)
        AgendadoMs     = ($i - 1) * $intervaloMs
    }
}

$configuracao = @{
    Url             = $url
    Cliente         = $Cliente
    Chave           = $chaveDaSubscription
    SemBackoff      = [bool]$SemBackoff
    MaxTentativas   = $MaxTentativas
    EsperaBase      = $EsperaBaseSegundos
    EsperaMaxima    = $EsperaMaximaSegundos
    TimeoutSegundos = $TimeoutClienteSegundos
    Inicio          = [DateTime]::UtcNow
}
$resultados = [System.Collections.Concurrent.ConcurrentBag[object]]::new()

# ---------------------------------------------------------------------------------------------
# Envio de uma fatura, com retry. Roda em paralelo (uma thread por fatura em andamento).
# ---------------------------------------------------------------------------------------------
$enviarFatura = {
    $item = $_
    $http = $using:http
    $cfg = $using:configuracao
    $concluidas = $using:resultados

    # -ReqPorMin: espera o horário planejado desta fatura.
    $adiantamento = $item.AgendadoMs - ([DateTime]::UtcNow - $cfg.Inicio).TotalMilliseconds
    if ($adiantamento -gt 1) { Start-Sleep -Milliseconds ([int][math]::Ceiling($adiantamento)) }

    $relogioDaFatura = [System.Diagnostics.Stopwatch]::StartNew()
    $tentativas = [System.Collections.Generic.List[object]]::new()
    $faturaId = ''
    for ($n = 1; $n -le $cfg.MaxTentativas; $n++) {
        $status = 0
        $categoria = ''
        $erro = ''
        $retryAfter = $null
        $pedido = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, $cfg.Url)
        [void]$pedido.Headers.TryAddWithoutValidation('x-cliente-id', $cfg.Cliente)
        if ($cfg.Chave) {
            [void]$pedido.Headers.TryAddWithoutValidation('Ocp-Apim-Subscription-Key', $cfg.Chave)
        }
        if ($item.IdempotencyKey) {
            [void]$pedido.Headers.TryAddWithoutValidation('Idempotency-Key', $item.IdempotencyKey)
        }
        $pedido.Content = [System.Net.Http.StringContent]::new($item.Corpo, [System.Text.Encoding]::UTF8, 'application/json')
        $cancelamento = [System.Threading.CancellationTokenSource]::new()
        $relogio = [System.Diagnostics.Stopwatch]::StartNew()
        $tarefa = $null
        try {
            $tarefa = $http.SendAsync($pedido, $cancelamento.Token)
            # Espera em fatias de 200 ms para o Ctrl+C conseguir interromper.
            while (-not $tarefa.Wait(200)) { }
            $resposta = $tarefa.Result
            try {
                $status = [int]$resposta.StatusCode
                $texto = $resposta.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                $condicao = $resposta.Headers.RetryAfter
                if ($null -ne $condicao) {
                    if ($null -ne $condicao.Delta) { $retryAfter = $condicao.Delta.TotalSeconds }
                    elseif ($null -ne $condicao.Date) { $retryAfter = [math]::Max([double]0, [double]($condicao.Date - [DateTimeOffset]::UtcNow).TotalSeconds) }
                }
                if ($status -in 200, 201, 202 -and $texto) {
                    try {
                        $json = $texto | ConvertFrom-Json
                        if ($json.faturaId) { $faturaId = [string]$json.faturaId }
                    }
                    catch { }
                }
                if ($status -ge 400) { $erro = '{0} {1}' -f $status, $resposta.ReasonPhrase }
            }
            finally {
                $resposta.Dispose()
            }
        }
        catch {
            $timeout = $false
            $detalhe = ''
            for ($e = $_.Exception; $null -ne $e; $e = $e.InnerException) {
                if ($e -is [System.TimeoutException] -or $e -is [System.Threading.Tasks.TaskCanceledException]) { $timeout = $true; break }
                $detalhe = $e.Message
            }
            if ($timeout) {
                $categoria = 'timeout'
                $erro = "timeout do cliente (sem resposta em $($cfg.TimeoutSegundos) s)"
            }
            else {
                $categoria = 'erro-rede'
                $erro = "erro de rede: $detalhe"
            }
        }
        finally {
            $relogio.Stop()
            if ($null -ne $tarefa -and -not $tarefa.IsCompleted) { $cancelamento.Cancel() }
            $cancelamento.Dispose()
            $pedido.Dispose()
        }

        if ($status -gt 0) {
            $categoria = if ($status -eq 201) { '201' }
            elseif ($status -eq 202) { '202' }
            elseif ($status -eq 429) { '429' }
            elseif ($status -ge 500) { '5xx' }
            else { 'outros' }
        }
        $retentavel = ($categoria -in '429', '5xx', 'timeout', 'erro-rede') -or $status -eq 408

        $espera = 0.0
        if ($retentavel -and $n -lt $cfg.MaxTentativas -and -not $cfg.SemBackoff) {
            # CONCEITO: backoff exponencial com "equal jitter": metade da espera é fixa (1, 2, 4, 8 s
            # divididos por 2) e a outra metade é aleatória.
            # (Os [double] evitam que o PowerShell escolha Math.Min(int, int) e arredonde a espera.)
            $exponencial = [math]::Min([double]$cfg.EsperaMaxima, [double]$cfg.EsperaBase * [math]::Pow(2, $n - 1))
            $espera = ($exponencial / 2) + (Get-Random -Minimum 0.0 -Maximum ($exponencial / 2))
            if ($null -ne $retryAfter) {
                # CONCEITO: honra o Retry-After (o servidor sabe quando volta a aceitar), soma um
                # pouco de jitter e nunca passa do teto de 30 s.
                $espera = [math]::Max([double]$espera, [double]$retryAfter + (Get-Random -Minimum 0.0 -Maximum 1.0))
            }
            $espera = [math]::Min([double]$cfg.EsperaMaxima, [double]$espera)
        }

        $tentativas.Add([pscustomobject]@{
                Numero         = $n
                Status         = $status
                Categoria      = $categoria
                LatenciaMs     = [int]$relogio.Elapsed.TotalMilliseconds
                RetryAfter     = $retryAfter
                EsperaSegundos = [math]::Round($espera, 1)
                Erro           = $erro
            })

        if (-not $retentavel) { break }
        if ($n -lt $cfg.MaxTentativas -and $espera -gt 0) {
            Start-Sleep -Milliseconds ([int]($espera * 1000))
        }
    }
    $relogioDaFatura.Stop()

    $ultima = $tentativas[$tentativas.Count - 1]
    # Código de cada tentativa: o HTTP, ou timeout/erro-rede quando não houve resposta.
    $codigos = @($tentativas | ForEach-Object { if ($_.Status -gt 0) { [string]$_.Status } else { $_.Categoria } })
    $resultado = [pscustomobject]@{
        Indice             = $item.Indice
        ReferenciaErp      = $item.ReferenciaErp
        IdempotencyKey     = $item.IdempotencyKey
        FaturaId           = $faturaId
        Resultado          = $ultima.Categoria
        StatusFinal        = $codigos[-1]
        Tentativas         = $tentativas.Count
        StatusPorTentativa = ($codigos -join '|')
        DuracaoTotalMs     = [int]$relogioDaFatura.Elapsed.TotalMilliseconds
        Respostas          = $tentativas.ToArray()
    }
    $concluidas.Add($resultado)
    $resultado
}

# ---------------------------------------------------------------------------------------------
# Envio, progresso, relatório e CSV
# ---------------------------------------------------------------------------------------------
$pastaSaida = Join-Path $PSScriptRoot 'saida'
$caminhoCsv = Join-Path $pastaSaida ('erp-{0}.csv' -f $carimbo)
for ($sequencia = 2; Test-Path -LiteralPath $caminhoCsv; $sequencia++) {
    # Duas execuções no mesmo segundo (ex.: cliente-a e cliente-b juntos) não se sobrescrevem.
    $caminhoCsv = Join-Path $pastaSaida ('erp-{0}-{1}.csv' -f $carimbo, $sequencia)
}
$contagem = [ordered]@{}
foreach ($categoria in $Categorias) { $contagem[$categoria] = 0 }
$prontas = 0
$interrompido = $true
$relogioGeral = [System.Diagnostics.Stopwatch]::StartNew()
$configuracao.Inicio = [DateTime]::UtcNow
try {
    $itens | ForEach-Object -Parallel $enviarFatura -ThrottleLimit $Paralelismo | ForEach-Object {
        $prontas++
        $chave = if ($contagem.Contains($_.Resultado)) { $_.Resultado } else { 'outros' }
        $contagem[$chave]++
        $placar = ($contagem.GetEnumerator() | ForEach-Object { '{0}: {1}' -f $_.Key, $_.Value }) -join ' · '
        Write-Progress -Activity "ERP simulado ($Cliente) -> $destino" -Status "$prontas/$Faturas faturas · $placar" -PercentComplete ([int](100 * $prontas / $Faturas))
    }
    $interrompido = $false
}
finally {
    Write-Progress -Activity "ERP simulado ($Cliente) -> $destino" -Completed
    $relogioGeral.Stop()
    $http.Dispose()

    $finalizadas = @($resultados.ToArray() | Sort-Object -Property Indice)
    try {
        if (-not (Test-Path -LiteralPath $pastaSaida)) { New-Item -ItemType Directory -Path $pastaSaida | Out-Null }
        # Formato lido pelo guia do Incidente 1: UTF-8, vírgula, estas colunas nesta ordem.
        $finalizadas |
            Select-Object @{ n = 'referenciaErp'; e = { $_.ReferenciaErp } },
                @{ n = 'faturaId'; e = { $_.FaturaId } },
                @{ n = 'cliente'; e = { $Cliente } },
                @{ n = 'tentativas'; e = { $_.Tentativas } },
                @{ n = 'statusFinal'; e = { $_.StatusFinal } },
                @{ n = 'codigos'; e = { $_.StatusPorTentativa } },
                @{ n = 'duracaoMs'; e = { $_.DuracaoTotalMs } },
                @{ n = 'idempotencyKey'; e = { $_.IdempotencyKey } } |
            Export-Csv -LiteralPath $caminhoCsv -Encoding utf8NoBOM -UseQuotes AsNeeded -NoTypeInformation
    }
    catch {
        Write-Host "Aviso: não consegui gravar o CSV em ${caminhoCsv}: $($_.Exception.Message)" -ForegroundColor Yellow
    }

    Write-Relatorio -Resultados $finalizadas -Duracao $relogioGeral.Elapsed -Destino $destino -Modo $modo -CaminhoCsv $caminhoCsv -Interrompido $interrompido
    try { [Console]::OutputEncoding = $codificacaoAnterior } catch { }
}

$aceitas = @($finalizadas | Where-Object { $_.Resultado -in '201', '202' }).Count
if ($interrompido -or $aceitas -lt $Faturas) { exit 1 }
exit 0
