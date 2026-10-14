#Requires -Version 7.0

<#
.SYNOPSIS
  Varre os arquivos rastreados e não rastreados do repositório em busca de termos proibidos.

.DESCRIPTION
  Lê .local/termos-proibidos.txt (ignorado pelo git; um termo por linha; linhas vazias ou
  iniciadas por '#' são comentários), grava os termos válidos num arquivo temporário e roda
  `git grep -i -I -F --untracked`. Imprime SOMENTE o número de ocorrências por arquivo; nunca
  o termo nem a linha, para a saída poder ser colada em qualquer lugar sem vazar nada.

  Código de saída 0 = limpo; 1 = há ocorrências (não commitar); 2 = lista ausente.

.PARAMETER Lista
  Caminho da lista de termos. Padrão: .local/termos-proibidos.txt na raiz do repositório.
  Numa worktree (que não tem .local/), aponte para a lista do checkout principal.

.EXAMPLE
  pwsh ./scripts/varredura-termos.ps1

.EXAMPLE
  pwsh ./scripts/varredura-termos.ps1 -Lista C:\caminho\do\checkout-principal\.local\termos-proibidos.txt

.NOTES
  Version-anchor: Q4-2026. Requer PowerShell 7 e Git.
#>
[CmdletBinding()]
param(
    [string]$Lista = (Join-Path $PSScriptRoot '..' '.local' 'termos-proibidos.txt')
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

if (-not (Test-Path $Lista)) {
    Write-Host "Lista de termos não encontrada em $Lista (pasta .local/ é privada e não vem no clone)."
    exit 2
}

$termos = @(Get-Content -LiteralPath $Lista -Encoding utf8 |
    ForEach-Object { $_.Trim() } |
    Where-Object { $_.Length -gt 2 -and -not $_.StartsWith('#') })

if ($termos.Count -eq 0) {
    Write-Host 'A lista não tem termos válidos (só comentários ou linhas curtas).'
    exit 2
}

$tmp = New-TemporaryFile
try {
    # LF e UTF-8 sem BOM, para o git grep ler cada termo inteiro
    [System.IO.File]::WriteAllText($tmp.FullName, (($termos -join "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))

    $saida = git grep -i -I -F --untracked -c -f $tmp.FullName -- . 2>$null
    $total = 0
    if ($saida) {
        foreach ($linha in $saida) {
            # formato: caminho:contagem
            $pos = $linha.LastIndexOf(':')
            $arquivo = $linha.Substring(0, $pos)
            $n = [int]$linha.Substring($pos + 1)
            $total += $n
            Write-Host ("{0,5}  {1}" -f $n, $arquivo)
        }
    }

    if ($total -gt 0) {
        Write-Host ""
        Write-Host "Ocorrências de termos proibidos: $total (termos avaliados: $($termos.Count)). NÃO COMMITE. Corrija os arquivos acima."
        exit 1
    }

    Write-Host "Varredura limpa: 0 ocorrências ($($termos.Count) termos avaliados)."
    exit 0
}
finally {
    Remove-Item -LiteralPath $tmp.FullName -Force -ErrorAction SilentlyContinue
}
