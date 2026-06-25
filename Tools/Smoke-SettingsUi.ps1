param(
    [Parameter(Mandatory)]
    [int]$AppPid,
    [string]$OutputDir = "dist\qa"
)

$ErrorActionPreference = "Continue"
$pass = 0
$fail = 0
$results = @()

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

function Test-UI {
    param(
        [string]$Name,
        [scriptblock]$Script
    )

    try {
        $output = & $Script 2>&1
        if ($LASTEXITCODE -eq 0) {
            $script:pass++
            $script:results += [pscustomobject]@{ name = $Name; status = "PASS"; detail = "" }
        }
        else {
            $script:fail++
            $script:results += [pscustomobject]@{ name = $Name; status = "FAIL"; detail = "$output" }
        }
    }
    catch {
        $script:fail++
        $script:results += [pscustomobject]@{ name = $Name; status = "FAIL"; detail = "$_" }
    }
}

Test-UI "Window exists" {
    winapp ui list-windows -a $AppPid --json | Out-Null
}

Test-UI "Dashboard loads" {
    winapp ui invoke "SettingsNavDashboardItem" -a $AppPid | Out-Null
    winapp ui wait-for "DashboardToggleThemeButton" -a $AppPid -t 5000
    winapp ui wait-for "DashboardCard_current-preset" -a $AppPid -t 5000
}

Test-UI "Dashboard cards navigate to General" {
    winapp ui invoke "DashboardCard_startup" -a $AppPid | Out-Null
    winapp ui wait-for "GeneralStartupToggle" -a $AppPid -t 5000
}

Test-UI "General page controls exist" {
    winapp ui wait-for "GeneralLanguageComboBox" -a $AppPid -t 5000
    winapp ui wait-for "GeneralExportDropDownButton" -a $AppPid -t 5000
    winapp ui wait-for "GeneralImportDropDownButton" -a $AppPid -t 5000
    winapp ui wait-for "GeneralResetSettingsButton" -a $AppPid -t 5000
}

Test-UI "Hotkey page controls exist" {
    winapp ui invoke "SettingsNavHotkeyItem" -a $AppPid | Out-Null
    winapp ui wait-for "HotkeyTrayClickExpander" -a $AppPid -t 5000
    winapp ui wait-for "HotkeyGlobalExpander" -a $AppPid -t 5000
    winapp ui wait-for "HotkeyScheduleExpander" -a $AppPid -t 5000
    winapp ui wait-for "HotkeyToastExpander" -a $AppPid -t 5000
}

Test-UI "Wallpaper page controls exist" {
    winapp ui invoke "SettingsNavWallpaperItem" -a $AppPid | Out-Null
    winapp ui wait-for "WallpaperAutoSwitchToggle" -a $AppPid -t 5000
    $presetMatches = winapp ui search "WallpaperPresetCard" -a $AppPid --json | ConvertFrom-Json
    $addMatches = winapp ui search "WallpaperAddPresetCard" -a $AppPid --json | ConvertFrom-Json
    if (($presetMatches.matchCount + $addMatches.matchCount) -le 0) {
        throw "No wallpaper preset or add card was found."
    }
}

Test-UI "About page controls exist" {
    winapp ui invoke "SettingsNavAboutItem" -a $AppPid | Out-Null
    winapp ui wait-for "AboutGitHubLink" -a $AppPid -t 5000
    winapp ui wait-for "AboutTutorialButton" -a $AppPid -t 5000
    winapp ui wait-for "AboutLicenseExpander" -a $AppPid -t 5000
}

winapp ui invoke "SettingsNavDashboardItem" -a $AppPid 2>$null | Out-Null
winapp ui screenshot -a $AppPid -o (Join-Path $OutputDir "smoke-dashboard.png") 2>$null | Out-Null
$results | ConvertTo-Json -Depth 3 | Out-File (Join-Path $OutputDir "smoke-settings-ui-results.json") -Encoding utf8

Write-Host "Passed: $pass | Failed: $fail"
$results | Where-Object { $_.status -eq "FAIL" } | ForEach-Object {
    Write-Host "FAIL: $($_.name) - $($_.detail)"
}

if ($fail -gt 0) {
    exit 1
}
