@echo off
title CouchGamingBar - Register Autostart Task
setlocal
cd /d "%~dp0"

echo ========================================================
echo   CouchGamingBar - Register Task Scheduler Autostart
echo ========================================================
echo.

if not exist "%~dp0Register-AutostartTask.ps1" (
    echo [ERROR] Could not find Register-AutostartTask.ps1 in "%~dp0"
    echo.
    pause
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Register-AutostartTask.ps1" %*

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Task registration failed with error code %ERRORLEVEL%.
    echo.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================================
echo   Autostart task registration completed.
echo ========================================================
echo.
pause
endlocal
