param(
    [string]$ProjectPath = "D:\Codex\src-winui\ToggleDarkMode.WinUI.csproj",
    [int]$PromoPort = 4173,
    [switch]$IncludeReleaseBundle,
    [string]$InstanceSuffix = "codex"
)

$ErrorActionPreference = "Stop"

function Write-Section([string]$title) {
    Write-Host ""
    Write-Host "== $title =="
}

Write-Section "Build"
dotnet clean $ProjectPath -p:Platform=x64
dotnet build $ProjectPath -p:Platform=x64

Write-Section "Background State"
powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Measure-BackgroundState.ps1") -InstanceSuffix $InstanceSuffix

Write-Section "Promo Site"
$siteRoot = Join-Path $PSScriptRoot "..\promo-site"
$resolvedSiteRoot = (Resolve-Path -LiteralPath $siteRoot).Path
$server = $null

try {
    $server = Start-Process -FilePath python -ArgumentList "-m","http.server",$PromoPort,"--bind","127.0.0.1","--directory",$resolvedSiteRoot -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 2
    $response = Invoke-WebRequest -Uri "http://127.0.0.1:$PromoPort/index.html" -UseBasicParsing
    $titleMatch = [regex]::Match($response.Content, "<title>(.*?)</title>")
    [pscustomobject]@{
        StatusCode    = $response.StatusCode
        ContentLength = $response.Content.Length
        HasTitle      = ($titleMatch.Success -and $titleMatch.Groups[1].Value -like "Tenlux*")
        HasGoToMarket = ($response.Content -match "Go To Market")
    } | Format-List
}
finally {
    if ($server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id -Force
    }
}

if ($IncludeReleaseBundle) {
    Write-Section "Release Bundle"
    powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Export-ReleaseBundle.ps1")
}
