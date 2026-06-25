param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$OutputRoot = "D:\Codex\src-winui\dist"
)

$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $workspaceRoot "ToggleDarkMode.WinUI.csproj"
$runtime = switch ($Platform.ToLowerInvariant()) {
    "x64" { "win-x64" }
    "x86" { "win-x86" }
    "arm64" { "win-arm64" }
    default { throw "Unsupported platform: $Platform" }
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$bundleRoot = Join-Path $OutputRoot "Tenlux-Release-$Platform-$timestamp"
$publishDir = Join-Path $workspaceRoot "bin\$Configuration\net10.0-windows10.0.26100.0\$runtime\publish"

dotnet publish $projectPath -c $Configuration -p:Platform=$Platform -r $runtime

if (-not (Test-Path -LiteralPath $publishDir)) {
    throw "Publish output not found: $publishDir"
}

New-Item -ItemType Directory -Path $bundleRoot -Force | Out-Null

$appDir = Join-Path $bundleRoot "app"
Copy-Item -LiteralPath $publishDir -Destination $appDir -Recurse -Force

foreach ($doc in @("README.md", "CHANGELOG.md", "RELEASE.md", "SUPPORT.md", "LICENSE.md")) {
    $path = Join-Path $workspaceRoot $doc
    if (Test-Path -LiteralPath $path) {
        Copy-Item -LiteralPath $path -Destination $bundleRoot -Force
    }
}

$versionFile = Join-Path $bundleRoot "VERSION.txt"
@(
    "Tenlux"
    "Version: 2.0.0"
    "Platform: $Platform"
    "Runtime: $runtime"
    "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
) | Set-Content -LiteralPath $versionFile -Encoding UTF8

$zipPath = "$bundleRoot.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -LiteralPath $bundleRoot -DestinationPath $zipPath -Force

[pscustomobject]@{
    Platform = $Platform
    Runtime = $runtime
    PublishDir = $publishDir
    BundleFolder = $bundleRoot
    ZipFile = $zipPath
} | Format-List
