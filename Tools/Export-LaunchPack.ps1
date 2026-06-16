param(
    [string]$OutputRoot = "D:\Codex\src-winui\dist"
)

$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$packRoot = Join-Path $OutputRoot "Tenlux-LaunchPack-$timestamp"

$itemsToCopy = @(
    "Marketing",
    "promo-site",
    "README.md",
    "CHANGELOG.md",
    "RELEASE.md",
    "ROADMAP.md",
    "SUPPORT.md",
    "LICENSE.md",
    "PROJECT.md"
)

New-Item -ItemType Directory -Path $packRoot -Force | Out-Null

foreach ($item in $itemsToCopy) {
    $source = Join-Path $workspaceRoot $item
    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination $packRoot -Recurse -Force
    }
}

$zipPath = "$packRoot.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -LiteralPath $packRoot -DestinationPath $zipPath -Force

[pscustomobject]@{
    PackFolder = $packRoot
    ZipFile = $zipPath
} | Format-List
