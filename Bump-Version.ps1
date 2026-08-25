<#
.SYNOPSIS
    Bumps version numbers across all CouchGamingBar / XboxGamingBar manifests, C# project files, and assembly metadata.

.DESCRIPTION
    This script updates the version in:
    - Package.appxmanifest (XboxGamingBarPackage)
    - Package.Store.appxmanifest (XboxGamingBarPackage)
    - Package.appxmanifest (XboxGamingBar)
    - CouchGamingBarHelper.csproj (<ApplicationVersion>)
    - AssemblyInfo.cs (XboxGamingBar)
    - app.manifest / app.Store.manifest files (Application identity)

.PARAMETER NewVersion
    The new version number (e.g. "1.1.0" or "1.1.0.0").

.EXAMPLE
    .\Bump-Version.ps1 -NewVersion "1.1.0.0"
    .\Bump-Version.ps1 "1.2.0"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0, HelpMessage = "New version string, e.g. 1.1.0 or 1.1.0.0")]
    [string]$NewVersion
)

$ErrorActionPreference = "Stop"

# Parse and validate version input
try {
    $parsedVersion = [System.Version]$NewVersion
} catch {
    Write-Error "Invalid version string '$NewVersion'. Please use format X.Y.Z or X.Y.Z.W (e.g., 1.1.0 or 1.1.0.0)."
    exit 1
}

$build = if ($parsedVersion.Build -ge 0) { $parsedVersion.Build } else { 0 }
$revision = if ($parsedVersion.Revision -ge 0) { $parsedVersion.Revision } else { 0 }

$v4 = "$($parsedVersion.Major).$($parsedVersion.Minor).$build.$revision"
$v3 = "$($parsedVersion.Major).$($parsedVersion.Minor).$build"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  CouchGamingBar Version Bump Tool" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " Target 4-part version (Manifests/AssemblyInfo) : $v4" -ForegroundColor Green
Write-Host " Target 3-part version (Csproj ApplicationVersion): $v3" -ForegroundColor Green
Write-Host "--------------------------------------------------"

$ScriptRoot = $PSScriptRoot
if (-not $ScriptRoot) { $ScriptRoot = Get-Location }

# Define target files relative to repository root
$targets = @(
    @{
        Path = Join-Path $ScriptRoot "Samples\XboxGamingBarPackage\Package.appxmanifest"
        Type = "AppxManifest"
    },
    @{
        Path = Join-Path $ScriptRoot "Samples\XboxGamingBarPackage\Package.Store.appxmanifest"
        Type = "AppxManifest"
    },
    @{
        Path = Join-Path $ScriptRoot "Samples\XboxGamingBar\Package.appxmanifest"
        Type = "AppxManifest"
    },
    @{
        Path = Join-Path $ScriptRoot "Samples\XboxGamingBarHelper\CouchGamingBarHelper.csproj"
        Type = "Csproj"
    },
    @{
        Path = Join-Path $ScriptRoot "Samples\XboxGamingBar\Properties\AssemblyInfo.cs"
        Type = "AssemblyInfo"
    },
    @{
        Path = Join-Path $ScriptRoot "Samples\XboxGamingBarHelper\app.manifest"
        Type = "Win32Manifest"
    },
    @{
        Path = Join-Path $ScriptRoot "Samples\XboxGamingBarHelper\app.Store.manifest"
        Type = "Win32Manifest"
    },
    @{
        Path = Join-Path $ScriptRoot "Samples\XboxGamingBar\app.manifest"
        Type = "Win32Manifest"
    }
)

$updatedCount = 0

foreach ($target in $targets) {
    $filePath = $target.Path
    if (-not (Test-Path $filePath)) {
        Write-Warning "File not found, skipping: $filePath"
        continue
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)
    $content = [System.IO.File]::ReadAllText($filePath)
    $oldContent = $content

    switch ($target.Type) {
        "AppxManifest" {
            # Update <Identity ... Version="x.x.x.x"
            $content = [regex]::Replace($content, '(?i)(<Identity\b[\s\S]*?\bVersion=")[^"]+', '${1}' + $v4)
        }
        "Csproj" {
            # Update <ApplicationVersion>x.x.x</ApplicationVersion>
            $content = [regex]::Replace($content, '(?i)(<ApplicationVersion>)[^<]+', '${1}' + $v3)
        }
        "AssemblyInfo" {
            # Update active AssemblyVersion and AssemblyFileVersion attributes (skip commented out lines)
            $content = [regex]::Replace($content, '(?m)^(\s*\[assembly:\s*AssemblyVersion\(")[^"]+', '${1}' + $v4)
            $content = [regex]::Replace($content, '(?m)^(\s*\[assembly:\s*AssemblyFileVersion\(")[^"]+', '${1}' + $v4)
        }
        "Win32Manifest" {
            # Update <assemblyIdentity version="x.x.x.x" name="MyApplication.app"/>
            $content = [regex]::Replace($content, '(?i)(<assemblyIdentity\b[^>]*?\bversion=")[^"]+([^>]*?\bname="MyApplication\.app")', '${1}' + $v4 + '${2}')
        }
    }

    if ($content -ne $oldContent) {
        [System.IO.File]::WriteAllText($filePath, $content, $encoding)
        $relativePath = $filePath.Replace($ScriptRoot, "").TrimStart("\/")
        Write-Host " [UPDATED] $relativePath" -ForegroundColor Yellow
        $updatedCount++
    } else {
        $relativePath = $filePath.Replace($ScriptRoot, "").TrimStart("\/")
        Write-Host " [NO CHANGE] $relativePath" -ForegroundColor Gray
    }
}

Write-Host "--------------------------------------------------"
Write-Host "Done. $updatedCount file(s) updated." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
