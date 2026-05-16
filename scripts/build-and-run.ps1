$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$csFile = Join-Path $projectRoot "src\ToggleDarkMode.cs"
$exeFile = Join-Path $projectRoot "build\ToggleDarkMode.exe"
$iconFile = Join-Path $projectRoot "assets\favicon.ico"

if (-not (Test-Path (Split-Path -Parent $exeFile))) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $exeFile) -Force | Out-Null
}

$cscPaths = @(
    "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:SystemRoot\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $null
foreach ($p in $cscPaths) {
    if (Test-Path $p) { $csc = $p; break }
}

if ($csc) {
    $cmd = "& `"$csc`" /nologo /target:winexe /out:`"$exeFile`" `"$csFile`""
    if (Test-Path $iconFile) {
        $cmd = "& `"$csc`" /nologo /target:winexe /win32icon:`"$iconFile`" /out:`"$exeFile`" `"$csFile`""
    }
    Invoke-Expression $cmd
    if ($LASTEXITCODE -ne 0) { exit 1 }
} else {
    $csSource = Get-Content $csFile -Raw -Encoding UTF8
    Add-Type -AssemblyName System
    $provider = New-Object Microsoft.CSharp.CSharpCodeProvider
    $params = New-Object System.CodeDom.Compiler.CompilerParameters
    $params.GenerateExecutable = $true
    $params.OutputAssembly = $exeFile
    $params.CompilerOptions = "/target:winexe"
    $result = $provider.CompileAssemblyFromSource($params, $csSource)
    if ($result.Errors.Count -gt 0) {
        foreach ($err in $result.Errors) { Write-Host $err.ToString() -ForegroundColor Red }
        exit 1
    }
}

$proc = Get-Process -Name "ToggleDarkMode" -ErrorAction SilentlyContinue
if (-not $proc) {
    Start-Process -FilePath $exeFile
}
