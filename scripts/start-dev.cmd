@echo off
REM Start Cadence Development Environment
REM This is a wrapper for the PowerShell script

echo.
echo ============================================
echo  Cadence - Development Environment
echo ============================================
echo.

REM Check if PowerShell is available
where powershell >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo ERROR: PowerShell is not available on this system
    pause
    exit /b 1
)

REM Run the PowerShell script
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-dev.ps1" %*

REM If we get here, something exited
echo.
echo Development environment has stopped.
pause
