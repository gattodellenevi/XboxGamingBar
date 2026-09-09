# Post-packaging script invoked by MSBuild target to create streamlined, user-friendly release packages and ZIP archives.
param(
    [string]$PublishDir = "",
    [string]$ScriptDir = ""
)

Write-Host "Create-ReleasePackage: PublishDir = '$PublishDir'"
Write-Host "Create-ReleasePackage: ScriptDir  = '$ScriptDir'"

if (-not $PublishDir -or -not (Test-Path $PublishDir)) {
    Write-Host "Publish directory '$PublishDir' not found."
    exit 0
}

$AutostartScript = Join-Path $ScriptDir "Register-AutostartTask.ps1"
if (-not (Test-Path $AutostartScript)) {
    Write-Warning "Autostart script '$AutostartScript' not found."
    exit 0
}

# Template for root Install.cmd (double-clickable, auto-elevates, bypasses execution policy)
$cmdContent = @"
@echo off
title CouchGamingBar Installer
cd /d "%~dp0"

set "SCRIPT=%~dp0Support\Install.ps1"
if not exist "%SCRIPT%" set "SCRIPT=%~dp0Install.ps1"

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& { if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { Start-Process powershell.exe -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"\"%SCRIPT%\"\"' -Verb RunAs } else { & \"%SCRIPT%\" } }"
"@

# Template for quick-start README.txt in the release root
$readmeContent = @"
=====================================================================
                    COUCH GAMING BAR INSTALLER
=====================================================================

QUICK START:
  1. Double-click "Install.cmd" to start installation.
  2. When prompted by Windows User Account Control (UAC), click "Yes".

WHAT THE INSTALLER DOES:
  - Automatically trusts the local package signing certificate.
  - Automatically installs all required Windows dependencies.
  - Installs CouchGamingBar package.
  - Stages CouchGamingBarHelper into %LOCALAPPDATA%\CouchGamingBarHelper.
  - Configures Windows Task Scheduler to autostart the helper at logon
    with full elevation and system tray icon support.

NOTE:
  All package assets and helper scripts are located in the "Support"
  folder. You do not need to open the Support folder or run anything
  inside it.
=====================================================================
"@

# Template for streamlined, zero-prompt Support\Install.ps1
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

# Define search directories for package assets (current folder, Support subfolder, or parent)
$searchDirs = @($PSScriptRoot)
if (Test-Path (Join-Path $PSScriptRoot "Support")) {
    $searchDirs += (Join-Path $PSScriptRoot "Support")
}
$parentDir = Split-Path -Parent $PSScriptRoot
if ($parentDir -and (Test-Path (Join-Path $parentDir "Support"))) {
    $searchDirs += (Join-Path $parentDir "Support")
}
$searchDirs = $searchDirs | Select-Object -Unique

# 2. Install Developer Certificate to Trusted Root and Trusted People
$cerFile = $null
foreach ($sDir in $searchDirs) {
    $cerFile = Get-ChildItem -Path $sDir -Filter "*.cer" | Select-Object -First 1
    if ($cerFile) { break }
}

if ($cerFile) {
    Write-Host "Trusting certificate: $($cerFile.Name)..." -ForegroundColor Yellow
    try {
        Import-Certificate -FilePath $cerFile.FullName -CertStoreLocation "Cert:\LocalMachine\Root" -ErrorAction Stop | Out-Null
        Import-Certificate -FilePath $cerFile.FullName -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" -ErrorAction Stop | Out-Null
        Write-Host "Certificate installed successfully." -ForegroundColor Green
    } catch {
        Write-Warning "Certificate import: $($_.Exception.Message)"
    }
}

# 3. Collect Dependencies
$depFiles = @()
$arch = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
foreach ($sDir in $searchDirs) {
    $depBase = Join-Path $sDir "Dependencies"
    if (Test-Path $depBase) {
        $depSearchDirs = @(
            (Join-Path $depBase $arch),
            $depBase
        )
        foreach ($dDir in $depSearchDirs) {
            if (Test-Path $dDir) {
                $depFiles += (Get-ChildItem -Path $dDir -Filter "*.appx" -Recurse -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
                $depFiles += (Get-ChildItem -Path $dDir -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
            }
        }
    }
}
$depFiles = $depFiles | Select-Object -Unique

# 4. Locate Main Package / Bundle
$package = $null
foreach ($sDir in $searchDirs) {
    $package = Get-ChildItem -Path $sDir -Filter "*.msixbundle" | Select-Object -First 1
    if (-not $package) { $package = Get-ChildItem -Path $sDir -Filter "*.appxbundle" | Select-Object -First 1 }
    if (-not $package) { $package = Get-ChildItem -Path $sDir -Filter "*.msix" | Select-Object -First 1 }
    if (-not $package) { $package = Get-ChildItem -Path $sDir -Filter "*.appx" | Select-Object -First 1 }
    if ($package) { break }
}

if (-not $package) {
    Write-Error "No .msixbundle, .appxbundle, .msix, or .appx package found in $($searchDirs -join ', ')!"
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
    $devScript = $null
    foreach ($sDir in $searchDirs) {
        $cand = Join-Path $sDir "Add-AppDevPackage.ps1"
        if (Test-Path $cand) { $devScript = $cand; break }
    }
    if ($devScript) {
        Write-Host "Attempting fallback via Add-AppDevPackage.ps1..." -ForegroundColor Yellow
        & $devScript -Force
    } else {
        Read-Host "Press Enter to exit"
        exit 1
    }
}

# 6. Register Task Scheduler Autostart
$autostartScript = $null
foreach ($sDir in $searchDirs) {
    $cand = Join-Path $sDir "Register-AutostartTask.ps1"
    if (Test-Path $cand) { $autostartScript = $cand; break }
}

if ($autostartScript) {
    Write-Host "Setting up Task Scheduler autostart for helper..." -ForegroundColor Yellow
    try {
        & $autostartScript
    } catch {
        Write-Warning "Autostart registration warning: $($_.Exception.Message)"
    }
}

Write-Host "`n=============================================" -ForegroundColor Green
Write-Host "  CouchGamingBar installed successfully!       " -ForegroundColor Green
Write-Host "=============================================`n" -ForegroundColor Green
Start-Sleep -Seconds 2
'@

# Find all package output directories
$packageDirs = Get-ChildItem -Path $PublishDir -Filter "*Add-AppDevPackage.ps1" -Recurse | ForEach-Object { $_.DirectoryName } | Select-Object -Unique

if (-not $packageDirs -or $packageDirs.Count -eq 0) {
    $packageDirs = Get-ChildItem -Path $PublishDir -Filter "*.msixbundle" -Recurse | ForEach-Object { $_.DirectoryName } | Select-Object -Unique
}

if (-not $packageDirs -or $packageDirs.Count -eq 0) {
    if (Get-ChildItem -Path $PublishDir -Filter "*.msixbundle") {
        $packageDirs = @($PublishDir)
    }
}

Write-Host "Found $($packageDirs.Count) package directories to configure."

foreach ($dir in $packageDirs) {
    Write-Host "`nConfiguring streamlined release package in: $dir" -ForegroundColor Cyan
    
    # 1. Ensure Support subfolder exists
    $supportDir = Join-Path $dir "Support"
    if (-not (Test-Path $supportDir)) {
        New-Item -ItemType Directory -Path $supportDir -Force | Out-Null
    }

    # 2. Move / copy dependencies folder into Support\
    $depSource = Join-Path $dir "Dependencies"
    $depTarget = Join-Path $supportDir "Dependencies"
    if ((Test-Path $depSource) -and ($depSource -ne $depTarget)) {
        if (Test-Path $depTarget) { Remove-Item -Path $depTarget -Recurse -Force }
        Move-Item -Path $depSource -Destination $depTarget -Force
    }

    # 3. Move / copy package bundles and certificates into Support\
    Get-ChildItem -Path $dir -Filter "*.msixbundle" | Where-Object { $_.DirectoryName -ne $supportDir } | ForEach-Object {
        Move-Item -Path $_.FullName -Destination (Join-Path $supportDir $_.Name) -Force
    }
    Get-ChildItem -Path $dir -Filter "*.appxbundle" | Where-Object { $_.DirectoryName -ne $supportDir } | ForEach-Object {
        Move-Item -Path $_.FullName -Destination (Join-Path $supportDir $_.Name) -Force
    }
    Get-ChildItem -Path $dir -Filter "*.msix" | Where-Object { $_.DirectoryName -ne $supportDir } | ForEach-Object {
        Move-Item -Path $_.FullName -Destination (Join-Path $supportDir $_.Name) -Force
    }
    Get-ChildItem -Path $dir -Filter "*.appx" | Where-Object { $_.DirectoryName -ne $supportDir } | ForEach-Object {
        Move-Item -Path $_.FullName -Destination (Join-Path $supportDir $_.Name) -Force
    }
    Get-ChildItem -Path $dir -Filter "*.cer" | Where-Object { $_.DirectoryName -ne $supportDir } | ForEach-Object {
        Move-Item -Path $_.FullName -Destination (Join-Path $supportDir $_.Name) -Force
    }

    # 4. Copy Register-AutostartTask.ps1 into Support\
    Copy-Item -Path $AutostartScript -Destination $supportDir -Force

    # 5. Write Support\Install.ps1
    $installPs1Path = Join-Path $supportDir "Install.ps1"
    [System.IO.File]::WriteAllText($installPs1Path, $installPs1Content)

    # 6. Write root Install.cmd and README.txt
    $installCmdPath = Join-Path $dir "Install.cmd"
    [System.IO.File]::WriteAllText($installCmdPath, $cmdContent)

    $readmePath = Join-Path $dir "README.txt"
    [System.IO.File]::WriteAllText($readmePath, $readmeContent)

    # 7. Clean up unwanted Visual Studio developer artifacts from root
    $unwantedFiles = @("Add-AppDevPackage.ps1", "Install.ps1", "Register-AutostartTask.ps1")
    foreach ($f in $unwantedFiles) {
        $p = Join-Path $dir $f
        if (Test-Path $p) { Remove-Item -Path $p -Force -ErrorAction SilentlyContinue }
    }
    $unwantedDirs = @("Add-AppDevPackage.resources")
    foreach ($d in $unwantedDirs) {
        $p = Join-Path $dir $d
        if (Test-Path $p) { Remove-Item -Path $p -Recurse -Force -ErrorAction SilentlyContinue }
    }
    Get-ChildItem -Path $dir -Filter "*.appxsym" | Remove-Item -Force -ErrorAction SilentlyContinue

    Write-Host "Successfully organized streamlined package structure in $($dir):" -ForegroundColor Green
    Write-Host "  - Root: Install.cmd, README.txt" -ForegroundColor Green
    Write-Host "  - Subfolder: Support/ (Dependencies, Package Bundle, Certificate, Scripts)" -ForegroundColor Green

    # 8. Generate ready-to-upload release ZIP archive
    try {
        $parentDir = Split-Path -Parent $dir
        $dirLeaf = Split-Path -Leaf $dir
        
        # Extract version string (e.g. CouchGamingBarPackage_1.4.2.0_Test -> 1.4.2)
        $shortVersion = if ($dirLeaf -match '(\d+\.\d+\.\d+)(\.\d+)?') {
            if ($matches[2] -and $matches[2] -ne ".0") { "$($matches[1])$($matches[2])" } else { $matches[1] }
        } else {
            "Release"
        }

        $zipName = "CouchGamingBarPackage_$shortVersion.zip"
        $zipPath = Join-Path $parentDir $zipName

        Write-Host "Compressing release archive: $zipName..." -ForegroundColor Yellow
        if (Test-Path $zipPath) { Remove-Item -Path $zipPath -Force }

        Compress-Archive -Path $dir -DestinationPath $zipPath -CompressionLevel Optimal
        Write-Host "Release archive successfully generated: $zipPath" -ForegroundColor Green
    } catch {
        Write-Warning "Could not automatically generate release ZIP: $($_.Exception.Message)"
    }
}
