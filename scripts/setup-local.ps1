#Requires -Version 5.1
<#
.SYNOPSIS
    Sets up the local development environment for Cadence.

.DESCRIPTION
    This script:
    - Verifies prerequisites (.NET, Node.js, Azure Functions Core Tools)
    - Creates local.settings.json from template
    - Creates .env from template
    - Restores NuGet packages
    - Installs npm packages
    - Applies database migrations

.EXAMPLE
    .\scripts\setup-local.ps1
#>

[CmdletBinding()]
param(
    [switch]$SkipMigrations,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$functionsPath = Join-Path $rootPath "src\Cadence.Functions"
$webApiPath = Join-Path $rootPath "src\Cadence.WebApi"
$frontendPath = Join-Path $rootPath "src\frontend"

Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Cadence Development Setup" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check .NET SDK
$dotnetVersion = $null
try {
    $dotnetVersion = dotnet --version 2>$null
    Write-Host "  [OK] .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "  [ERROR] .NET SDK not found. Please install .NET 10 SDK." -ForegroundColor Red
    exit 1
}

# Check Node.js
$nodeVersion = $null
try {
    $nodeVersion = node --version 2>$null
    Write-Host "  [OK] Node.js: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "  [ERROR] Node.js not found. Please install Node.js 20+." -ForegroundColor Red
    exit 1
}

# Check Azure Functions Core Tools
$funcVersion = $null
try {
    $funcVersion = func --version 2>$null
    Write-Host "  [OK] Azure Functions Core Tools: $funcVersion" -ForegroundColor Green
} catch {
    Write-Host "  [WARNING] Azure Functions Core Tools not found." -ForegroundColor Yellow
    Write-Host "           Install with: npm install -g azure-functions-core-tools@4" -ForegroundColor Yellow
}

Write-Host ""

# Create local.settings.json for Functions
if (Test-Path $functionsPath) {
    Write-Host "Setting up Functions configuration..." -ForegroundColor Yellow
    $localSettingsPath = Join-Path $functionsPath "local.settings.json"
    $localSettingsExamplePath = Join-Path $functionsPath "local.settings.example.json"

    if ((Test-Path $localSettingsPath) -and -not $Force) {
        Write-Host "  [SKIP] local.settings.json already exists" -ForegroundColor Gray
    } elseif (Test-Path $localSettingsExamplePath) {
        Copy-Item $localSettingsExamplePath $localSettingsPath -Force
        Write-Host "  [OK] Created local.settings.json from template" -ForegroundColor Green
        Write-Host "       Please update the connection string in local.settings.json" -ForegroundColor Yellow
    } else {
        Write-Host "  [WARNING] local.settings.example.json not found" -ForegroundColor Yellow
    }
}

# Create .env for Frontend
if (Test-Path $frontendPath) {
    Write-Host "Setting up frontend configuration..." -ForegroundColor Yellow
    $envPath = Join-Path $frontendPath ".env"
    $envExamplePath = Join-Path $frontendPath ".env.example"

    if ((Test-Path $envPath) -and -not $Force) {
        Write-Host "  [SKIP] .env already exists" -ForegroundColor Gray
    } elseif (Test-Path $envExamplePath) {
        Copy-Item $envExamplePath $envPath -Force
        Write-Host "  [OK] Created .env from template" -ForegroundColor Green
    } else {
        Write-Host "  [WARNING] .env.example not found" -ForegroundColor Yellow
    }
}

Write-Host ""

# Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
Push-Location $webApiPath
try {
    dotnet restore --verbosity minimal
    Write-Host "  [OK] NuGet packages restored" -ForegroundColor Green
} catch {
    Write-Host "  [ERROR] Failed to restore NuGet packages: $_" -ForegroundColor Red
} finally {
    Pop-Location
}

Write-Host ""

# Install npm packages
Write-Host "Installing npm packages..." -ForegroundColor Yellow
Push-Location $frontendPath
try {
    npm install --silent
    Write-Host "  [OK] npm packages installed" -ForegroundColor Green
} catch {
    Write-Host "  [ERROR] Failed to install npm packages: $_" -ForegroundColor Red
} finally {
    Pop-Location
}

Write-Host ""

# Apply migrations
if (-not $SkipMigrations) {
    Write-Host "Applying database migrations..." -ForegroundColor Yellow
    Push-Location $webApiPath
    try {
        dotnet ef database update
        Write-Host "  [OK] Database migrations applied" -ForegroundColor Green
    } catch {
        Write-Host "  [WARNING] Failed to apply migrations. Ensure the connection string is configured." -ForegroundColor Yellow
        Write-Host "            Error: $_" -ForegroundColor Gray
    } finally {
        Pop-Location
    }
} else {
    Write-Host "Skipping database migrations (use -SkipMigrations:$false to apply)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Update the connection string in src/Cadence.WebApi/appsettings.Local.json" -ForegroundColor White
Write-Host "  2. Start the backend: cd src/Cadence.WebApi && dotnet run" -ForegroundColor White
Write-Host "  3. Start the frontend: cd src/frontend && npm run dev" -ForegroundColor White
Write-Host ""
