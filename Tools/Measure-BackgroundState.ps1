param(
    [string]$ExePath = "D:\Codex\src-winui\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\Tenlux.exe",
    [int]$WaitSeconds = 4,
    [switch]$RestartTargetInstance,
    [string]$InstanceSuffix = ""
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ExePath)) {
    throw "Executable not found: $ExePath"
}

$proc = $null
$launchedHere = $false
$resolvedExePath = (Resolve-Path -LiteralPath $ExePath).Path
$launchedPid = $null

function Start-TenluxInstance([string]$targetExe, [string]$suffix) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $targetExe
    $psi.UseShellExecute = $false
    if (-not [string]::IsNullOrWhiteSpace($suffix)) {
        $psi.EnvironmentVariables["TENLUX_INSTANCE_SUFFIX"] = $suffix
    }
    return [System.Diagnostics.Process]::Start($psi)
}

function Get-TenluxProcesses {
    Get-Process Tenlux -ErrorAction SilentlyContinue | ForEach-Object {
        [pscustomobject]@{
            Process = $_
            Path = try { $_.MainModule.FileName } catch { $null }
        }
    }
}

try {
    if ([string]::IsNullOrWhiteSpace($InstanceSuffix)) {
        $existing = @(Get-TenluxProcesses)
        $matching = @($existing | Where-Object { $_.Path -eq $resolvedExePath })
        $blocking = @($existing | Where-Object { $_.Path -and $_.Path -ne $resolvedExePath })

        if ($blocking.Count -gt 0 -and $matching.Count -eq 0) {
            [pscustomobject]@{
                Status = "BlockedByOtherInstance"
                Message = "Another Tenlux instance is already running from a different path."
                BlockingPath = $blocking[0].Path
                BlockingPid = $blocking[0].Process.Id
            } | Format-List
            return
        }

        if ($matching.Count -gt 0) {
            if ($RestartTargetInstance) {
                foreach ($item in $matching) {
                    Stop-Process -Id $item.Process.Id -Force
                }
                Start-Sleep -Seconds 1
            } else {
                $proc = $matching[0].Process
            }
        }
    }

    if ($null -eq $proc) {
        if (-not [string]::IsNullOrWhiteSpace($InstanceSuffix)) {
            $proc = Start-TenluxInstance -targetExe $resolvedExePath -suffix $InstanceSuffix
            $launchedHere = $true
            $launchedPid = $proc.Id
            Start-Sleep -Seconds $WaitSeconds
        } else {
            $proc = Start-Process -FilePath $resolvedExePath -PassThru
            $launchedHere = $true
            $launchedPid = $proc.Id
            Start-Sleep -Seconds $WaitSeconds
        }
    }

    $running = Get-Process -Id $launchedPid -ErrorAction SilentlyContinue
    if ($null -eq $running) {
        throw "Tenlux exited before measurement completed."
    }

    [pscustomobject]@{
        Status         = if ($launchedHere) { "LaunchedAndMeasured" } else { "MeasuredExistingInstance" }
        Timestamp      = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Id             = $running.Id
        WorkingSetMB   = [math]::Round($running.WorkingSet64 / 1MB, 2)
        PrivateMB      = [math]::Round($running.PrivateMemorySize64 / 1MB, 2)
        Handles        = $running.Handles
        Threads        = $running.Threads.Count
        Path           = $resolvedExePath
        WaitSeconds    = $WaitSeconds
        InstanceSuffix = $InstanceSuffix
    } | Format-List
}
finally {
    if ($launchedHere -and $launchedPid) {
        $cleanupProc = Get-Process -Id $launchedPid -ErrorAction SilentlyContinue
        if ($cleanupProc -ne $null) {
            Stop-Process -Id $cleanupProc.Id -Force
        }
    }
}
