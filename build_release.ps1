param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " Building Couch Game Bar ($Configuration) v$Version" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Build Helper (Release configuration - elevated process)
Write-Host "`n[1/3] Building Helper ($Configuration configuration)..." -ForegroundColor Yellow
dotnet build Samples\XboxGamingBarHelper\CouchGamingBarHelper.csproj -c $Configuration /p:Version=$Version

# 2. Build UWP AppX Package (Release configuration)
Write-Host "`n[2/3] Building UWP AppX Package ($Configuration configuration)..." -ForegroundColor Yellow
$msBuildPath = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $msBuildPath)) {
    $msBuildPath = "MSBuild.exe"
}

& $msBuildPath Samples\XboxGamingBar\CouchGameBar.csproj /p:Configuration=$Configuration /p:Platform=x64 /p:AppxBundle=Never /p:GenerateAppxPackageOnBuild=true /p:AppxPackageSigningEnabled=false /p:RemoveDisposableSigningCertificate=false /p:AppxPackageVersion="$Version.0"

# 3. Build Inno Setup Installer
Write-Host "`n[3/3] Building Inno Setup Installer..." -ForegroundColor Yellow
$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $isccPath)) {
    $isccPath = "ISCC.exe"
}

if (Get-Command $isccPath -ErrorAction SilentlyContinue) {
    & $isccPath "/dMyAppVersion=$Version" "/dBuildConfig=$Configuration" setup.iss
    Write-Host "`n[SUCCESS] Build Complete! Installer generated at: Output\CouchGameBarSetup-$Version.exe" -ForegroundColor Green
} else {
    Write-Host "`n[NOTE] Project binaries and AppX package built successfully." -ForegroundColor Green
    Write-Host "Inno Setup compiler (ISCC.exe) was not found on PATH. Open 'setup.iss' in Inno Setup to compile CouchGameBarSetup-$Version.exe." -ForegroundColor Yellow
}
