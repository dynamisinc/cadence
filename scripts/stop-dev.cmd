@echo off
REM Stop Cadence Development Environment
REM This is a wrapper for the PowerShell script

echo.
echo ============================================
echo  Stopping Cadence Dev Environment
echo ============================================
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0stop-dev.ps1"

pause
