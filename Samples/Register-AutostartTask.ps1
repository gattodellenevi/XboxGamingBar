# Script to register CouchGamingBarHelper in Task Scheduler for Autostart at logon.
# Run this script in PowerShell as Administrator or standard elevated user context.

param (
    [string]$ExePath = ""
)

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

# Locate installed AppX/MSIX package in WindowsApps
if ([string]::IsNullOrWhiteSpace($ExePath)) {
    try {
        $packages = Get-AppxPackage | Where-Object { $_.Name -like "*CouchGamingBar*" } |
            Sort-Object -Property @{ Expression = { if ($_.InstallLocation -like "*WindowsApps*") { 1 } else { 0 } }; Descending = $true },
                                  @{ Expression = { if (-not $_.IsDevelopmentMode) { 1 } else { 0 } }; Descending = $true },
                                  @{ Expression = { [Version]$_.Version }; Descending = $true }
        foreach ($pkg in $packages) {
            if ($pkg.InstallLocation) {
                $candidate1 = Join-Path $pkg.InstallLocation "CouchGamingBarHelper\CouchGamingBarHelper.exe"
                $candidate2 = Join-Path $pkg.InstallLocation "CouchGamingBarHelper.exe"

                if (Test-Path $candidate1) {
                    $ExePath = $candidate1
                    break
                } elseif (Test-Path $candidate2) {
                    $ExePath = $candidate2
                    break
                }
            }
        }
    } catch {
        Write-Warning "Could not locate helper in package: $($_.Exception.Message)"
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
    Start-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
} catch {
    Write-Error "Failed to register scheduled task: $_"
}
