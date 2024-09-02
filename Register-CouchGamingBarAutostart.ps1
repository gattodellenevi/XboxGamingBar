# Script to register CouchGamingBarHelper in Task Scheduler for Autostart at logon.
# Locates the installed Microsoft Store / MSIX package, deploys the helper to LocalAppData
# (to prevent WindowsApps DLL loader restrictions), requests elevation, and configures the task.

param (
    [string]$ExePath = ""
)

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "   CouchGamingBar - Register Task Scheduler Autostart   " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Check for Administrator privileges; self-elevate if needed
$isElevated = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isElevated) {
    Write-Host "Requesting Administrator privileges..." -ForegroundColor Yellow
    try {
        $scriptPath = $MyInvocation.MyCommand.Definition
        if (-not $scriptPath) { $scriptPath = $PSCommandPath }
        if (-not $scriptPath) { $scriptPath = "$PSScriptRoot\Register-CouchGamingBarAutostart.ps1" }

        Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`" -ExePath `"$ExePath`"" -Wait -ErrorAction Stop
        Write-Host "Elevated registration completed." -ForegroundColor Green
        exit 0
    } catch {
        Write-Error "Elevation failed: $($_.Exception.Message)"
        Write-Host "Please re-run this script in an elevated PowerShell window (Run as Administrator)." -ForegroundColor Yellow
        exit 1
    }
}

# 2. Locate installed Store/MSIX package and deploy to LocalAppData
if ([string]::IsNullOrWhiteSpace($ExePath)) {
    try {
        $packages = Get-AppxPackage | Where-Object { $_.Name -like "*CouchGamingBar*" }
        foreach ($pkg in $packages) {
            if ($pkg.InstallLocation) {
                $candidate1 = Join-Path $pkg.InstallLocation "CouchGamingBarHelper"
                $candidate2 = $pkg.InstallLocation
                $sourceDir = $null

                if (Test-Path (Join-Path $candidate1 "CouchGamingBarHelper.exe")) {
                    $sourceDir = $candidate1
                } elseif (Test-Path (Join-Path $candidate2 "CouchGamingBarHelper.exe")) {
                    $sourceDir = $candidate2
                }

                if ($sourceDir) {
                    $targetDir = Join-Path $env:LOCALAPPDATA "CouchGamingBarHelper"
                    Write-Host "Deploying helper files from package to: $targetDir" -ForegroundColor Cyan
                    if (-not (Test-Path $targetDir)) {
                        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
                    }
                    Copy-Item -Path (Join-Path $sourceDir "*") -Destination $targetDir -Recurse -Force -ErrorAction Stop
                    $targetExe = Join-Path $targetDir "CouchGamingBarHelper.exe"
                    if (Test-Path $targetExe) {
                        $ExePath = $targetExe
                        break
                    }
                }
            }
        }
    } catch {
        Write-Warning "Could not extract helper from package: $($_.Exception.Message)"
    }
}

if (-not $ExePath -or -not (Test-Path $ExePath)) {
    Write-Error "CouchGamingBarHelper.exe could not be found. Please pass -ExePath 'C:\path\to\CouchGamingBarHelper.exe'"
    exit 1
}

$WorkingDir = Split-Path -Parent $ExePath
$TaskName = "CouchGamingBarHelper"

Write-Host "Target Executable: $ExePath" -ForegroundColor Yellow
Write-Host "Working Directory: $WorkingDir" -ForegroundColor Yellow
Write-Host ""

# 3. Create Scheduled Task (At Logon, Interactive Session, Highest Privileges)
$Action    = New-ScheduledTaskAction -Execute $ExePath -WorkingDirectory $WorkingDir
$Trigger   = New-ScheduledTaskTrigger -AtLogOn
$Principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Highest
$Settings  = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Days 0) -MultipleInstances IgnoreNew

try {
    Register-ScheduledTask -TaskName $TaskName -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings -Force -ErrorAction Stop | Out-Null
    Write-Host "Task '$TaskName' successfully registered in Task Scheduler!" -ForegroundColor Green
    Write-Host "  - Trigger:    At Log On ($env:USERNAME)" -ForegroundColor Green
    Write-Host "  - Privilege:  Highest (Elevated Administrator)" -ForegroundColor Green
    Write-Host "  - Session:    Interactive User (System Tray Icon enabled)" -ForegroundColor Green
    Write-Host ""

    # 4. Start the task immediately
    Write-Host "Starting '$TaskName' now..." -ForegroundColor Cyan
    Start-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    Write-Host "CouchGamingBarHelper is now running in the background." -ForegroundColor Green
} catch {
    Write-Error "Failed to register scheduled task: $_"
    exit 1
}
