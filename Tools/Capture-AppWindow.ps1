param(
    [string]$ExePath = "D:\Codex\src-winui\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\Tenlux.exe",
    [string]$InstanceSuffix = "",
    [string]$OutputPath = "D:\Codex\src-winui\dist\Tenlux-WindowCapture.png",
    [string]$OpenTag = "Dashboard",
    [int]$StartupWaitSeconds = 2,
    [int]$ShowWaitSeconds = 3
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ExePath)) {
    throw "Executable not found: $ExePath"
}

$resolvedOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
}

if ([string]::IsNullOrWhiteSpace($InstanceSuffix)) {
    $InstanceSuffix = "codexui-" + (Get-Date -Format "HHmmss")
}

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class WindowCaptureNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
}
"@

function Get-TenluxProcesses {
    Get-Process Tenlux -ErrorAction SilentlyContinue | Sort-Object StartTime
}

function Start-IsolatedInstance([string]$targetExe, [string]$suffix) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $targetExe
    $psi.UseShellExecute = $false
    $psi.EnvironmentVariables["TENLUX_INSTANCE_SUFFIX"] = $suffix
    if (-not [string]::IsNullOrWhiteSpace($OpenTag)) {
        $psi.Arguments = "--open=$OpenTag"
    }
    return [System.Diagnostics.Process]::Start($psi)
}

function Stop-ProcessTree([int]$rootPid) {
    try {
        Get-CimInstance Win32_Process | Where-Object { $_.ParentProcessId -eq $rootPid } | ForEach-Object {
            Stop-ProcessTree -rootPid $_.ProcessId
        }
    }
    catch {
        # Best effort only.
    }

    try {
        $proc = Get-Process -Id $rootPid -ErrorAction SilentlyContinue
        if ($proc -ne $null) {
            Stop-Process -Id $rootPid -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        # Best effort only.
    }
}

function Get-NewTenluxProcess([int[]]$existingIds, [datetime]$after) {
    Get-TenluxProcesses |
        Where-Object { $_.StartTime -ge $after -and $_.Id -notin $existingIds } |
        Sort-Object StartTime -Descending |
        Select-Object -First 1
}

function Wait-ForMainWindow([int]$processId, [int]$seconds) {
    $deadline = (Get-Date).AddSeconds($seconds)
    do {
        $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($proc -ne $null) {
            $proc.Refresh()
            if ($proc.MainWindowHandle -ne 0) {
                return $proc
            }
        }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    return $null
}

$launched = $null
$before = Get-Date

try {
    $launched = Start-IsolatedInstance -targetExe $ExePath -suffix $InstanceSuffix
    Start-Sleep -Seconds $StartupWaitSeconds

    if ($launched -eq $null) {
        throw "Isolated Tenlux instance did not launch."
    }

    # Launch the same isolated instance again to trigger "show settings" on the first instance.
    Start-IsolatedInstance -targetExe $ExePath -suffix $InstanceSuffix
    Start-Sleep -Seconds $ShowWaitSeconds

    $windowProc = Wait-ForMainWindow -processId $launched.Id -seconds 8
    if ($windowProc -eq $null) {
        throw "Main window handle was not available in time."
    }

    $rect = New-Object WindowCaptureNative+RECT
    if (-not [WindowCaptureNative]::GetWindowRect($windowProc.MainWindowHandle, [ref]$rect)) {
        throw "GetWindowRect failed."
    }

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -le 0 -or $height -le 0) {
        throw "Window bounds were invalid: ${width}x${height}"
    }

    $dir = Split-Path -Parent $resolvedOutputPath
    if ($dir) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $bitmap.Save($resolvedOutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()

    [pscustomobject]@{
        OutputPath = $resolvedOutputPath
        WindowWidth = $width
        WindowHeight = $height
        ProcessId = $windowProc.Id
        InstanceSuffix = $InstanceSuffix
        OpenTag = $OpenTag
    } | Format-List
}
finally {
    if ($launched -ne $null) {
        Stop-ProcessTree -rootPid $launched.Id
    }
}
