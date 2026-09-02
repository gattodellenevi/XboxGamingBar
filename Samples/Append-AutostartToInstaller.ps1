# Helper script invoked by MSBuild post-packaging target to create streamlined, zero-prompt installer scripts.
param(
    [string]$PublishDir = "",
    [string]$ScriptDir = ""
)

Write-Host "Append-AutostartToInstaller: PublishDir = '$PublishDir'"
Write-Host "Append-AutostartToInstaller: ScriptDir = '$ScriptDir'"

if (-not $PublishDir -or -not (Test-Path $PublishDir)) {
    Write-Host "Publish directory '$PublishDir' not found."
    exit 0
}

$AutostartScript = Join-Path $ScriptDir "Register-AutostartTask.ps1"
if (-not (Test-Path $AutostartScript)) {
    Write-Warning "Autostart script '$AutostartScript' not found."
    exit 0
}

# Template for Install.cmd (double-clickable, auto-elevates, bypasses execution policy)
$cmdContent = @"
@echo off
title CouchGamingBar Installer
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& { if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { Start-Process powershell.exe -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"\"%~dp0Install.ps1\"\"' -Verb RunAs } else { & \"%~dp0Install.ps1\" } }"
"@

# Template for streamlined, zero-prompt Install.ps1
$installPs1Content = @'
# Streamlined unattended installer for CouchGamingBar
[CmdletBinding()]
param()

# 1. Self-elevate to Administrator if needed
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Requesting Administrator privileges..." -ForegroundColor Cyan
    $scriptPath = $PSCommandPath
    if (-not $scriptPath) { $scriptPath = Join-Path $PSScriptRoot "Install.ps1" }
    Start-Process powershell.exe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`"" -Verb RunAs
    exit
}

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  Installing CouchGamingBar                   " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# 2. Install Developer Certificate to Trusted Root and Trusted People
$cerFile = Get-ChildItem -Path $PSScriptRoot -Filter "*.cer" | Select-Object -First 1
if ($cerFile) {
    Write-Host "Trusting certificate: $($cerFile.Name)..." -ForegroundColor Yellow
    try {
        Import-Certificate -FilePath $cerFile.FullName -CertStoreLocation "Cert:\LocalMachine\Root" -ErrorAction Stop | Out-Null
        Import-Certificate -FilePath $cerFile.FullName -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" -ErrorAction Stop | Out-Null
        Write-Host "Certificate installed." -ForegroundColor Green
    } catch {
        Write-Warning "Certificate import: $($_.Exception.Message)"
    }
}

# 3. Collect Dependencies
$depFiles = @()
if (Test-Path (Join-Path $PSScriptRoot "Dependencies")) {
    $arch = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
    $searchDirs = @(
        (Join-Path $PSScriptRoot "Dependencies\$arch"),
        (Join-Path $PSScriptRoot "Dependencies")
    )
    foreach ($dir in $searchDirs) {
        if (Test-Path $dir) {
            $depFiles += (Get-ChildItem -Path $dir -Filter "*.appx" -Recurse -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
            $depFiles += (Get-ChildItem -Path $dir -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
        }
    }
    $depFiles = $depFiles | Select-Object -Unique
}

# 4. Locate Main Package / Bundle
$package = Get-ChildItem -Path $PSScriptRoot -Filter "*.msixbundle" | Select-Object -First 1
if (-not $package) { $package = Get-ChildItem -Path $PSScriptRoot -Filter "*.appxbundle" | Select-Object -First 1 }
if (-not $package) { $package = Get-ChildItem -Path $PSScriptRoot -Filter "*.msix" | Select-Object -First 1 }
if (-not $package) { $package = Get-ChildItem -Path $PSScriptRoot -Filter "*.appx" | Select-Object -First 1 }

if (-not $package) {
    Write-Error "No .msixbundle, .appxbundle, .msix, or .appx package found in $PSScriptRoot!"
    Read-Host "Press Enter to exit"
    exit 1
}

# 5. Install App Package
Write-Host "Installing package: $($package.Name)..." -ForegroundColor Yellow
try {
    if ($depFiles.Count -gt 0) {
        Add-AppxPackage -Path $package.FullName -DependencyPath $depFiles -ForceApplicationShutdown -ErrorAction Stop
    } else {
        Add-AppxPackage -Path $package.FullName -ForceApplicationShutdown -ErrorAction Stop
    }
    Write-Host "Package installed successfully!" -ForegroundColor Green
} catch {
    Write-Error "Package installation error: $($_.Exception.Message)"
    $devScript = Join-Path $PSScriptRoot "Add-AppDevPackage.ps1"
    if (Test-Path $devScript) {
        Write-Host "Attempting fallback via Add-AppDevPackage.ps1..." -ForegroundColor Yellow
        & $devScript -Force
    } else {
        Read-Host "Press Enter to exit"
        exit 1
    }
}

# 6. Register Task Scheduler Autostart
$autostartScript = Join-Path $PSScriptRoot "Register-AutostartTask.ps1"
if (Test-Path $autostartScript) {
    Write-Host "Setting up Task Scheduler autostart for helper..." -ForegroundColor Yellow
    try {
        & $autostartScript
    } catch {
        Write-Warning "Autostart registration warning: $($_.Exception.Message)"
    }
}

# 7. Start the helper task
try {
    Start-ScheduledTask -TaskName "CouchGamingBarHelper" -ErrorAction SilentlyContinue
} catch {}

Write-Host "`n=============================================" -ForegroundColor Green
Write-Host "  CouchGamingBar installed successfully!       " -ForegroundColor Green
Write-Host "=============================================`n" -ForegroundColor Green
Start-Sleep -Seconds 2
'@

# Find all package output directories (directories containing .msixbundle, .cer, or Add-AppDevPackage.ps1)
$packageDirs = Get-ChildItem -Path $PublishDir -Filter "*Add-AppDevPackage.ps1" -Recurse | ForEach-Object { $_.DirectoryName } | Select-Object -Unique

if (-not $packageDirs -or $packageDirs.Count -eq 0) {
    # If no Add-AppDevPackage.ps1 found, check if PublishDir itself is the package dir
    if (Get-ChildItem -Path $PublishDir -Filter "*.msixbundle") {
        $packageDirs = @($PublishDir)
    }
}

Write-Host "Found $($packageDirs.Count) package directories to configure."

foreach ($dir in $packageDirs) {
    Write-Host "Configuring streamlined installer in: $dir"
    
    # 1. Copy Register-AutostartTask.ps1
    Copy-Item -Path $AutostartScript -Destination $dir -Force
    
    # 2. Write Install.ps1 (streamlined, zero-prompt version)
    $installPs1Path = Join-Path $dir "Install.ps1"
    [System.IO.File]::WriteAllText($installPs1Path, $installPs1Content)
    
    # 3. Write Install.cmd (double-clickable batch launcher)
    $installCmdPath = Join-Path $dir "Install.cmd"
    [System.IO.File]::WriteAllText($installCmdPath, $cmdContent)
    
    Write-Host "Successfully generated Install.ps1 and Install.cmd in $dir" -ForegroundColor Green
}

