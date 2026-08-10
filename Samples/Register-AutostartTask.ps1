# Script to register CouchGamingBarHelper in Task Scheduler for Autostart at logon.
# Run this script in PowerShell as Administrator or standard elevated user context.

param (
    [string]$ExePath = ""
)

if ([string]::IsNullOrWhiteSpace($ExePath)) {
    # 1. Search for installed AppX/MSIX package executable
    try {
        $packages = Get-AppxPackage | Where-Object { $_.Name -like "*CouchGameBar*" -or $_.Name -like "*XboxGamingBar*" -or $_.Name -like "*CouchGamingBar*" }
        foreach ($pkg in $packages) {
            if ($pkg.InstallLocation) {
                $c1 = Join-Path $pkg.InstallLocation "CouchGamingBarHelper\CouchGamingBarHelper.exe"
                $c2 = Join-Path $pkg.InstallLocation "CouchGamingBarHelper.exe"
                if (Test-Path $c1) { $ExePath = (Get-Item $c1).FullName; break }
                if (Test-Path $c2) { $ExePath = (Get-Item $c2).FullName; break }
            }
        }
    } catch {
        # Ignore AppX lookup failure and fallback to directory search
    }
}

if ([string]::IsNullOrWhiteSpace($ExePath)) {
    # 2. Search relative to script directory and parent directory levels
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    if (-not $ScriptDir) { $ScriptDir = $PSScriptRoot }

    $curr = $ScriptDir
    for ($i = 0; $i -lt 6; $i++) {
        if (-not $curr -or -not (Test-Path $curr)) { break }

        $CandidatePaths = @(
            (Join-Path $curr "CouchGamingBarHelper.exe"),
            (Join-Path $curr "CouchGamingBarHelper\CouchGamingBarHelper.exe"),
            (Join-Path $curr "XboxGamingBarHelper\bin\x64\Release\net8.0-windows10.0.22000.0\CouchGamingBarHelper.exe"),
            (Join-Path $curr "XboxGamingBarHelper\bin\x64\Debug\net8.0-windows10.0.22000.0\CouchGamingBarHelper.exe"),
            (Join-Path $curr "XboxGamingBarHelper\bin\Release\net8.0-windows10.0.22000.0\CouchGamingBarHelper.exe"),
            (Join-Path $curr "XboxGamingBarHelper\bin\Debug\net8.0-windows10.0.22000.0\CouchGamingBarHelper.exe"),
            (Join-Path $curr "XboxGamingBarHelper\bin\Release\CouchGamingBarHelper.exe"),
            (Join-Path $curr "XboxGamingBarHelper\bin\Debug\CouchGamingBarHelper.exe"),
            (Join-Path $curr "XboxGamingBarHelper\bin\x64\Release\CouchGamingBarHelper.exe"),
            (Join-Path $curr "XboxGamingBarHelper\bin\x64\Debug\CouchGamingBarHelper.exe")
        )

        foreach ($Path in $CandidatePaths) {
            if (Test-Path $Path) {
                $ExePath = (Get-Item $Path).FullName
                break
            }
        }

        if ($ExePath) { break }
        $curr = Split-Path -Parent $curr
    }
}

if (-not $ExePath -or -not (Test-Path $ExePath)) {
    Write-Error "CouchGamingBarHelper.exe not found! Please specify -ExePath 'C:\path\to\CouchGamingBarHelper.exe'"
    exit 1
}

$WorkingDir = Split-Path -Parent $ExePath
$TaskName = "CouchGamingBarHelper"

Write-Host "Registering Task Scheduler autostart task for:" -ForegroundColor Cyan
Write-Host "  Executable: $ExePath" -ForegroundColor Yellow
Write-Host "  Working Dir: $WorkingDir" -ForegroundColor Yellow

$isElevated = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isElevated) {
    Write-Host "Administrative privileges required to register Task Scheduler task with Highest RunLevel." -ForegroundColor Yellow
    Write-Host "Attempting to elevate..." -ForegroundColor Yellow
    try {
        $scriptPath = $MyInvocation.MyCommand.Definition
        if (-not $scriptPath) { $scriptPath = $PSCommandPath }
        if (-not $scriptPath) { $scriptPath = "$PSScriptRoot\Register-AutostartTask.ps1" }
        Start-Process powershell -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`" -ExePath `"$ExePath`"" -Wait -ErrorAction Stop
        Write-Host "Elevated registration process completed." -ForegroundColor Green
        exit 0
    } catch {
        Write-Warning "Could not automatically elevate process: $($_.Exception.Message)"
        Write-Host "Please run this script from an elevated PowerShell window (Run as Administrator):" -ForegroundColor Yellow
        Write-Host "  powershell -ExecutionPolicy Bypass -File `"$scriptPath`" -ExePath `"$ExePath`"" -ForegroundColor Cyan
        exit 1
    }
}

$Action = New-ScheduledTaskAction -Execute $ExePath -WorkingDirectory $WorkingDir
$Trigger = New-ScheduledTaskTrigger -AtLogOn
$Principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Highest
$Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Days 0) -MultipleInstances IgnoreNew

try {
    Register-ScheduledTask -TaskName $TaskName -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings -Force -ErrorAction Stop | Out-Null
    Write-Host "Successfully registered Task Scheduler task '$TaskName'!" -ForegroundColor Green
    Write-Host "  - Trigger: At Log On" -ForegroundColor Green
    Write-Host "  - Privilege: Highest (Elevated)" -ForegroundColor Green
    Write-Host "  - Session: Interactive User (Session 1+, System Tray enabled)" -ForegroundColor Green
} catch {
    Write-Error "Failed to register scheduled task: $_"
}
