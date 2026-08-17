@echo off
setlocal
cd /d "%~dp0"

set "VERSION=%~1"
if "%VERSION%"=="" (
    set /p "VERSION=Enter new version number (e.g. 1.2.0 or 1.2.0.0): "
)

if "%VERSION%"=="" (
    echo [ERROR] No version specified. Aborting.
    pause
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Bump-Version.ps1" "%VERSION%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Version bump failed.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Version bump completed successfully.
endlocal
