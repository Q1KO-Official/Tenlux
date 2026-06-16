param(
    [int]$Port = 4173
)

$ErrorActionPreference = "Stop"

$siteRoot = Join-Path $PSScriptRoot "..\promo-site"
$siteRoot = (Resolve-Path -LiteralPath $siteRoot).Path

Write-Host "Serving promo site from: $siteRoot"
Write-Host "Open: http://127.0.0.1:$Port/index.html"

python -m http.server $Port --bind 127.0.0.1 --directory $siteRoot
