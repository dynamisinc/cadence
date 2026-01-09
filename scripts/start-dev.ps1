<#
.SYNOPSIS
    Starts the Cadence development environment (frontend + backend)

.DESCRIPTION
    This script:
    1. Stops any existing API processes on port 5050
    2. Creates timestamped log files for both frontend and backend
    3. Starts the backend API with verbose logging
    4. Starts the frontend dev server with verbose logging
    5. Provides real-time status updates

.PARAMETER SkipBackend
    Skip starting the backend API

.PARAMETER SkipFrontend
    Skip starting the frontend dev server

.PARAMETER Background
    Run processes in background without opening terminal windows.
    Use this to start services and close the terminal afterwards.
    Monitor via log files or use stop-dev.ps1 to stop.

.PARAMETER LogDir
    Directory for log files (default: ./logs)

.EXAMPLE
    .\start-dev.ps1

.EXAMPLE
    .\start-dev.ps1 -SkipFrontend

.EXAMPLE
    .\start-dev.ps1 -Background
    # Starts both services in background - you can close the terminal
#>

param(
    [switch]$SkipBackend,
    [switch]$SkipFrontend,
    [switch]$Background,  # Run processes in background (no visible windows)
    [string]$LogDir = "$PSScriptRoot\..\logs"
)

# Script configuration
$ErrorActionPreference = "Continue"
$ProjectRoot = Resolve-Path "$PSScriptRoot\.."
$BackendDir = "$ProjectRoot\src\Cadence.WebApi"
$FrontendDir = "$ProjectRoot\src\frontend"
$Timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

# Colors for console output
function Write-Info { param($Message) Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Write-Warning { param($Message) Write-Host "[WARNING] $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "[ERROR] $Message" -ForegroundColor Red }
function Write-Step { param($Message) Write-Host "`n=== $Message ===" -ForegroundColor Magenta }

# Create log directory
Write-Step "Initializing Development Environment"
Write-Info "Project root: $ProjectRoot"
Write-Info "Timestamp: $Timestamp"

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
    Write-Info "Created log directory: $LogDir"
}

$BackendLog = "$LogDir\backend_$Timestamp.log"
$FrontendLog = "$LogDir\frontend_$Timestamp.log"
$StartupLog = "$LogDir\startup_$Timestamp.log"

# Start logging
$LogContent = @"
================================================================================
Cadence Development Environment Startup
================================================================================
Timestamp: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
User: $env:USERNAME
Machine: $env:COMPUTERNAME
Project Root: $ProjectRoot
================================================================================

"@
$LogContent | Out-File -FilePath $StartupLog -Encoding UTF8

function Log-Message {
    param($Message, $Level = "INFO")
    $LogLine = "[$(Get-Date -Format 'HH:mm:ss')] [$Level] $Message"
    Add-Content -Path $StartupLog -Value $LogLine
}

# Function to stop processes on a specific port
function Stop-ProcessOnPort {
    param([int]$Port)

    Write-Info "Checking for existing processes on port $Port..."
    Log-Message "Checking for processes on port $Port"

    try {
        $connections = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue

        if ($connections) {
            $processIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique

            foreach ($procId in $processIds) {
                $process = Get-Process -Id $procId -ErrorAction SilentlyContinue
                if ($process) {
                    Write-Warning "Found process '$($process.ProcessName)' (PID: $procId) on port $Port"
                    Log-Message "Stopping process: $($process.ProcessName) (PID: $procId)" "WARNING"

                    # Try graceful stop first
                    Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
                    Start-Sleep -Milliseconds 500

                    # Verify it's stopped
                    $stillRunning = Get-Process -Id $procId -ErrorAction SilentlyContinue
                    if ($stillRunning) {
                        Write-Warning "Process still running, forcing termination..."
                        taskkill /F /PID $procId 2>$null
                    }

                    Write-Success "Stopped process on port $Port"
                    Log-Message "Successfully stopped process on port $Port" "SUCCESS"
                }
            }
        } else {
            Write-Info "No existing processes found on port $Port"
            Log-Message "No processes found on port $Port"
        }
    } catch {
        Write-Warning "Could not check port ${Port}: $_"
        Log-Message "Error checking port ${Port}: $_" "WARNING"
    }
}

# Function to check if a port is available
function Test-PortAvailable {
    param([int]$Port)

    try {
        $connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
        return ($null -eq $connection)
    } catch {
        return $true
    }
}

# Function to wait for a port to become available
function Wait-ForPort {
    param(
        [int]$Port,
        [int]$TimeoutSeconds = 60,
        [string]$ServiceName = "Service"
    )

    Write-Info "Waiting for $ServiceName to start on port $Port..."
    $elapsed = 0
    $interval = 2

    while ($elapsed -lt $TimeoutSeconds) {
        $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
        if ($connection) {
            Write-Success "$ServiceName is now listening on port $Port"
            Log-Message "$ServiceName started successfully on port $Port" "SUCCESS"
            return $true
        }
        Start-Sleep -Seconds $interval
        $elapsed += $interval
        Write-Host "." -NoNewline
    }

    Write-Error "$ServiceName failed to start within $TimeoutSeconds seconds"
    Log-Message "$ServiceName failed to start within timeout" "ERROR"
    return $false
}

# Function to wait for any of multiple ports (for Vite fallback behavior)
function Wait-ForAnyPort {
    param(
        [int[]]$Ports,
        [int]$TimeoutSeconds = 60,
        [string]$ServiceName = "Service"
    )

    $portsString = $Ports -join ", "
    Write-Info "Waiting for $ServiceName to start on one of ports: $portsString..."
    $elapsed = 0
    $interval = 2

    while ($elapsed -lt $TimeoutSeconds) {
        foreach ($port in $Ports) {
            $connection = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
            if ($connection) {
                Write-Success "$ServiceName is now listening on port $port"
                Log-Message "$ServiceName started successfully on port $port" "SUCCESS"
                return $port
            }
        }
        Start-Sleep -Seconds $interval
        $elapsed += $interval
        Write-Host "." -NoNewline
    }

    Write-Error "$ServiceName failed to start within $TimeoutSeconds seconds"
    Log-Message "$ServiceName failed to start within timeout" "ERROR"
    return 0
}

# Store process IDs for cleanup
$script:BackendProcess = $null
$script:FrontendProcess = $null

# Cleanup function
function Stop-DevEnvironment {
    Write-Step "Shutting Down Development Environment"

    if ($script:BackendProcess -and -not $script:BackendProcess.HasExited) {
        Write-Info "Stopping backend process (PID: $($script:BackendProcess.Id))..."
        Stop-Process -Id $script:BackendProcess.Id -Force -ErrorAction SilentlyContinue
    }

    if ($script:FrontendProcess -and -not $script:FrontendProcess.HasExited) {
        Write-Info "Stopping frontend process (PID: $($script:FrontendProcess.Id))..."
        Stop-Process -Id $script:FrontendProcess.Id -Force -ErrorAction SilentlyContinue
    }

    # Also stop any npm processes that might be orphaned
    Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
        $_.MainWindowTitle -match "vite|npm"
    } | Stop-Process -Force -ErrorAction SilentlyContinue

    Write-Success "Development environment stopped"
    Log-Message "Development environment stopped" "INFO"
}

# Register cleanup on script exit
Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-DevEnvironment } -ErrorAction SilentlyContinue

# Main execution
try {
    # Stop existing processes
    Write-Step "Stopping Existing Processes"
    Stop-ProcessOnPort -Port 5050  # Backend API
    Stop-ProcessOnPort -Port 5173  # Frontend Vite

    # Give ports time to release
    Start-Sleep -Seconds 2

    # Start Backend
    if (-not $SkipBackend) {
        Write-Step "Starting Backend API"

        if (-not (Test-Path $BackendDir)) {
            Write-Error "Backend directory not found: $BackendDir"
            Log-Message "Backend directory not found: $BackendDir" "ERROR"
            exit 1
        }

        Write-Info "Backend directory: $BackendDir"
        Write-Info "Backend log file: $BackendLog"
        Log-Message "Starting backend from: $BackendDir"

        # Create a temporary script file for the backend
        $BackendScriptPath = "$LogDir\backend-start-$Timestamp.ps1"
        $BackendScriptContent = @"
`$ErrorActionPreference = 'Continue'
Set-Location '$BackendDir'
`$LogFile = '$BackendLog'

# Log environment info
"Backend Process Started: `$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File -FilePath `$LogFile -Encoding UTF8
"Working Directory: `$PWD" | Out-File -FilePath `$LogFile -Append -Encoding UTF8
"dotnet version: `$(dotnet --version)" | Out-File -FilePath `$LogFile -Append -Encoding UTF8
"" | Out-File -FilePath `$LogFile -Append -Encoding UTF8
"=================================================================================" | Out-File -FilePath `$LogFile -Append -Encoding UTF8
"" | Out-File -FilePath `$LogFile -Append -Encoding UTF8

# Build first, then run with the http profile (port 5050)
"Building..." | Out-File -FilePath `$LogFile -Append -Encoding UTF8
dotnet build 2>&1 | Out-File -FilePath `$LogFile -Append -Encoding UTF8
"Starting..." | Out-File -FilePath `$LogFile -Append -Encoding UTF8
dotnet run --launch-profile http --no-build 2>&1 | Tee-Object -FilePath `$LogFile -Append
"@
        $BackendScriptContent | Out-File -FilePath $BackendScriptPath -Encoding UTF8

        $windowStyle = if ($Background) { "Hidden" } else { "Normal" }
        $script:BackendProcess = Start-Process -FilePath "powershell.exe" `
            -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $BackendScriptPath `
            -WindowStyle $windowStyle `
            -PassThru

        Write-Info "Backend process started (PID: $($script:BackendProcess.Id))"
        Log-Message "Backend process started with PID: $($script:BackendProcess.Id)"

        # Wait for backend to be ready
        $backendReady = Wait-ForPort -Port 5050 -TimeoutSeconds 120 -ServiceName "Backend API"

        if (-not $backendReady) {
            Write-Error "Backend failed to start. Check log: $BackendLog"
            Log-Message "Backend startup failed" "ERROR"
            throw "Backend startup failed"
        }
    } else {
        Write-Info "Skipping backend startup (--SkipBackend)"
        Log-Message "Backend startup skipped by user"
    }

    # Start Frontend
    if (-not $SkipFrontend) {
        Write-Step "Starting Frontend Dev Server"

        if (-not (Test-Path $FrontendDir)) {
            Write-Error "Frontend directory not found: $FrontendDir"
            Log-Message "Frontend directory not found: $FrontendDir" "ERROR"
            exit 1
        }

        Write-Info "Frontend directory: $FrontendDir"
        Write-Info "Frontend log file: $FrontendLog"
        Log-Message "Starting frontend from: $FrontendDir"

        # Create a temporary script file for the frontend
        $FrontendScriptPath = "$LogDir\frontend-start-$Timestamp.ps1"
        $FrontendScriptContent = @"
`$ErrorActionPreference = 'Continue'
Set-Location '$FrontendDir'
`$LogFile = '$FrontendLog'

# Log environment info
"Frontend Process Started: `$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File -FilePath `$LogFile -Encoding UTF8
"Working Directory: `$PWD" | Out-File -FilePath `$LogFile -Append -Encoding UTF8
"Node version: `$(node --version)" | Out-File -FilePath `$LogFile -Append -Encoding UTF8
"npm version: `$(npm --version)" | Out-File -FilePath `$LogFile -Append -Encoding UTF8
"" | Out-File -FilePath `$LogFile -Append -Encoding UTF8
"=================================================================================" | Out-File -FilePath `$LogFile -Append -Encoding UTF8
"" | Out-File -FilePath `$LogFile -Append -Encoding UTF8

# Run npm dev with verbose logging
npm run dev 2>&1 | Tee-Object -FilePath `$LogFile -Append
"@
        $FrontendScriptContent | Out-File -FilePath $FrontendScriptPath -Encoding UTF8

        $windowStyle = if ($Background) { "Hidden" } else { "Normal" }
        $script:FrontendProcess = Start-Process -FilePath "powershell.exe" `
            -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $FrontendScriptPath `
            -WindowStyle $windowStyle `
            -PassThru

        Write-Info "Frontend process started (PID: $($script:FrontendProcess.Id))"
        Log-Message "Frontend process started with PID: $($script:FrontendProcess.Id)"

        # Wait for frontend to be ready (Vite may use fallback ports if 5173 is busy)
        $vitePorts = @(5173, 5174, 5175, 5176, 5177, 5178, 5179, 5180)
        $script:FrontendPort = Wait-ForAnyPort -Ports $vitePorts -TimeoutSeconds 60 -ServiceName "Frontend Dev Server"

        if ($script:FrontendPort -eq 0) {
            Write-Error "Frontend failed to start. Check log: $FrontendLog"
            Log-Message "Frontend startup failed" "ERROR"
            throw "Frontend startup failed"
        }
    } else {
        Write-Info "Skipping frontend startup (--SkipFrontend)"
        Log-Message "Frontend startup skipped by user"
    }

    # Summary
    Write-Step "Development Environment Ready"
    Write-Host ""
    Write-Success "All services started successfully!"
    Write-Host ""
    Write-Info "URLs:"
    if (-not $SkipFrontend) {
        Write-Host "  Frontend:  " -NoNewline -ForegroundColor White
        Write-Host "http://localhost:$($script:FrontendPort)" -ForegroundColor Green
    }
    if (-not $SkipBackend) {
        Write-Host "  Backend:   " -NoNewline -ForegroundColor White
        Write-Host "http://localhost:5050" -ForegroundColor Green
        Write-Host "  API Docs:  " -NoNewline -ForegroundColor White
        Write-Host "http://localhost:5050/api/docs" -ForegroundColor Green
    }
    Write-Host ""
    Write-Info "Log Files:"
    Write-Host "  Startup:   $StartupLog" -ForegroundColor Gray
    if (-not $SkipBackend) {
        Write-Host "  Backend:   $BackendLog" -ForegroundColor Gray
    }
    if (-not $SkipFrontend) {
        Write-Host "  Frontend:  $FrontendLog" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Info "Process IDs:"
    if (-not $SkipBackend) {
        Write-Host "  Backend:   $($script:BackendProcess.Id)" -ForegroundColor Gray
    }
    if (-not $SkipFrontend) {
        Write-Host "  Frontend:  $($script:FrontendProcess.Id)" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "Press Ctrl+C to stop all services..." -ForegroundColor Yellow

    Log-Message "Development environment ready"
    Log-Message "Frontend URL: http://localhost:$($script:FrontendPort)"
    Log-Message "Backend URL: http://localhost:5050"

    # Keep script running and monitor processes
    while ($true) {
        Start-Sleep -Seconds 5

        # Check if processes are still running
        if (-not $SkipBackend -and $script:BackendProcess.HasExited) {
            Write-Warning "Backend process has exited with code: $($script:BackendProcess.ExitCode)"
            Log-Message "Backend process exited with code: $($script:BackendProcess.ExitCode)" "WARNING"
            break
        }

        if (-not $SkipFrontend -and $script:FrontendProcess.HasExited) {
            Write-Warning "Frontend process has exited with code: $($script:FrontendProcess.ExitCode)"
            Log-Message "Frontend process exited with code: $($script:FrontendProcess.ExitCode)" "WARNING"
            break
        }
    }

} catch {
    Write-Error "Error starting development environment: $_"
    Log-Message "Error: $_" "ERROR"
    Stop-DevEnvironment
    exit 1
} finally {
    # Cleanup will be called on exit
}
