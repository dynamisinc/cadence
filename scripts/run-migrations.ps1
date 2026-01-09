#Requires -Version 5.1
<#
.SYNOPSIS
    Applies EF Core migrations to the database.

.DESCRIPTION
    This script applies Entity Framework Core migrations to the configured database.
    It can target local or Azure SQL databases and optionally generate SQL scripts.

.PARAMETER GenerateScript
    Generate a SQL script instead of applying migrations directly.

.PARAMETER OutputPath
    Path for the generated SQL script (used with -GenerateScript).

.PARAMETER MigrationName
    Specific migration to apply (default: applies all pending migrations).

.EXAMPLE
    .\scripts\run-migrations.ps1

.EXAMPLE
    .\scripts\run-migrations.ps1 -GenerateScript -OutputPath "./migration.sql"
#>

[CmdletBinding()]
param(
    [switch]$GenerateScript,
    [string]$OutputPath = "./migration.sql",
    [string]$MigrationName
)

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$apiPath = Join-Path $rootPath "src\Cadence.WebApi"

Write-Host "=================================" -ForegroundColor Cyan
Write-Host "EF Core Migration Runner" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Check for EF Core tools
Write-Host "Checking EF Core tools..." -ForegroundColor Yellow
try {
    $efVersion = dotnet ef --version 2>$null
    Write-Host "  [OK] EF Core Tools: $efVersion" -ForegroundColor Green
} catch {
    Write-Host "  [ERROR] EF Core tools not found." -ForegroundColor Red
    Write-Host "          Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

if (-not (Test-Path $apiPath)) {
    Write-Host "  [ERROR] WebApi project not found at $apiPath" -ForegroundColor Red
    Write-Host "          Migrations require the WebApi project to be present." -ForegroundColor Yellow
    exit 1
}

Push-Location $apiPath
try {
    if ($GenerateScript) {
        Write-Host "Generating migration script..." -ForegroundColor Yellow

        $args = @("ef", "migrations", "script", "--output", $OutputPath, "--idempotent")
        if ($MigrationName) {
            $args += @("--to", $MigrationName)
        }

        & dotnet @args

        Write-Host ""
        Write-Host "  [OK] Migration script generated: $OutputPath" -ForegroundColor Green
    } else {
        Write-Host "Applying migrations..." -ForegroundColor Yellow

        # List pending migrations
        Write-Host ""
        Write-Host "Pending migrations:" -ForegroundColor Yellow
        dotnet ef migrations list --pending

        Write-Host ""

        $args = @("ef", "database", "update")
        if ($MigrationName) {
            $args += $MigrationName
        }

        & dotnet @args

        Write-Host ""
        Write-Host "  [OK] Migrations applied successfully" -ForegroundColor Green
    }
} catch {
    Write-Host "  [ERROR] Migration failed: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Done!" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Cyan
