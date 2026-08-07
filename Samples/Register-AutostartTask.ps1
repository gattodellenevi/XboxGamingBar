# Script to register CouchGamingBarHelper in Task Scheduler for Autostart at logon.
# Run this script in PowerShell as Administrator or standard elevated user context.

param (
    [string]$ExePath = ""
)

if ([string]::IsNullOrWhiteSpace($ExePath)) {
    # Default to bin directory relative to script
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $CandidatePaths = @(
        "$ScriptDir\XboxGamingBarHelper\bin\x64\Debug\net8.0-windows10.0.22000.0\CouchGamingBarHelper.exe",
        "$ScriptDir\XboxGamingBarHelper\bin\x64\Release\net8.0-windows10.0.22000.0\CouchGamingBarHelper.exe",
        "$ScriptDir\XboxGamingBarHelper\bin\Debug\CouchGamingBarHelper.exe",
        "$ScriptDir\XboxGamingBarHelper\bin\Release\CouchGamingBarHelper.exe"
    )

    foreach ($Path in $CandidatePaths) {
        if (Test-Path $Path) {
            $ExePath = (Get-Item $Path).FullName
            break
        }
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
    Register-ScheduledTask -TaskName $TaskName -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings -Force | Out-Null
    Write-Host "Successfully registered Task Scheduler task '$TaskName'!" -ForegroundColor Green
    Write-Host "  - Trigger: At Log On" -ForegroundColor Green
    Write-Host "  - Privilege: Highest (Elevated)" -ForegroundColor Green
    Write-Host "  - Session: Interactive User (Session 1+, System Tray enabled)" -ForegroundColor Green
} catch {
    Write-Error "Failed to register scheduled task: $_"
}
