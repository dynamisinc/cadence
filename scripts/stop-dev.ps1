<#
.SYNOPSIS
    Stops all Cadence development processes

.DESCRIPTION
    This script stops:
    1. Backend API (dotnet processes on port 5071)
    2. Frontend dev server (node/vite processes on port 5173)
    3. Any orphaned node processes related to the project

.EXAMPLE
    .\stop-dev.ps1
#>

$ErrorActionPreference = "Continue"

function Write-Info { param($Message) Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Write-Warning { param($Message) Write-Host "[WARNING] $Message" -ForegroundColor Yellow }

Write-Host "`n=== Stopping Cadence Development Environment ===" -ForegroundColor Magenta
Write-Host ""

# Stop processes on port 5071 (Backend)
Write-Info "Checking for backend processes on port 5071..."
try {
    $backendConnections = Get-NetTCPConnection -LocalPort 5071 -ErrorAction SilentlyContinue
    if ($backendConnections) {
        $processIds = $backendConnections | Select-Object -ExpandProperty OwningProcess -Unique
        foreach ($procId in $processIds) {
            $process = Get-Process -Id $procId -ErrorAction SilentlyContinue
            if ($process) {
                Write-Warning "Stopping $($process.ProcessName) (PID: $procId) on port 5071"
                Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
            }
        }
        Write-Success "Backend processes stopped"
    } else {
        Write-Info "No backend processes found on port 5071"
    }
} catch {
    Write-Info "No backend processes to stop"
}

# Stop processes on Vite ports (5173-5180) - Vite uses fallback ports when default is in use
Write-Info "Checking for frontend processes on Vite ports (5173-5180)..."
$frontendStopped = $false
foreach ($port in 5173..5180) {
    try {
        $frontendConnections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
        if ($frontendConnections) {
            $processIds = $frontendConnections | Select-Object -ExpandProperty OwningProcess -Unique
            foreach ($procId in $processIds) {
                $process = Get-Process -Id $procId -ErrorAction SilentlyContinue
                if ($process -and $process.ProcessName -eq "node") {
                    Write-Warning "Stopping $($process.ProcessName) (PID: $procId) on port $port"
                    Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
                    $frontendStopped = $true
                }
            }
        }
    } catch {
        # Ignore errors for each port check
    }
}
if ($frontendStopped) {
    Write-Success "Frontend processes stopped"
} else {
    Write-Info "No frontend processes found on Vite ports"
}

# Stop any dotnet processes running Cadence.WebApi
Write-Info "Checking for Cadence.WebApi dotnet processes..."
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    try {
        $_.MainModule.FileName -and $_.CommandLine -match "Cadence"
    } catch {
        $false
    }
}

if ($dotnetProcesses) {
    foreach ($proc in $dotnetProcesses) {
        Write-Warning "Stopping dotnet process (PID: $($proc.Id))"
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Success "Dotnet processes stopped"
}

# Give ports time to release
Start-Sleep -Seconds 1

# Verify ports are free
Write-Host ""
Write-Info "Verifying ports are released..."

$port5071Free = $null -eq (Get-NetTCPConnection -LocalPort 5071 -ErrorAction SilentlyContinue)
$port5173Free = $null -eq (Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue)

if ($port5071Free) {
    Write-Success "Port 5071 is free"
} else {
    Write-Warning "Port 5071 may still be in use"
}

if ($port5173Free) {
    Write-Success "Port 5173 is free"
} else {
    Write-Warning "Port 5173 may still be in use"
}

Write-Host ""
Write-Success "Development environment stopped"
Write-Host ""
