#Requires -Version 5.1
<#
.SYNOPSIS
    Configures GitHub repository secrets and variables for Azure deployment.

.DESCRIPTION
    This script uses the GitHub CLI (gh) to set up:
    - Repository secrets for Azure deployment credentials
    - Repository variables for environment configuration

    Prerequisites:
    - GitHub CLI installed and authenticated (gh auth login)
    - Azure resources already provisioned (see docs/AZURE_PROVISIONING.md)
    - Publish profile downloaded from Azure Function App
    - Deployment token copied from Azure Static Web App

.PARAMETER FunctionAppName
    The name of your Azure Function App (e.g., func-refapp-dev)

.PARAMETER PublishProfilePath
    Path to the downloaded publish profile XML file from Azure Function App

.PARAMETER StaticWebAppToken
    The deployment token from Azure Static Web App (from Azure Portal)

.PARAMETER ApiUrl
    The URL of your Azure Function App API (e.g., https://func-refapp-dev.azurewebsites.net)

.EXAMPLE
    .\scripts\setup-github-secrets.ps1 -FunctionAppName "func-refapp-dev" -PublishProfilePath ".\func-refapp-dev.PublishSettings" -StaticWebAppToken "your-token-here"

.EXAMPLE
    # Interactive mode - prompts for all values
    .\scripts\setup-github-secrets.ps1
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$FunctionAppName,

    [Parameter(Mandatory = $false)]
    [string]$PublishProfilePath,

    [Parameter(Mandatory = $false)]
    [string]$StaticWebAppToken,

    [Parameter(Mandatory = $false)]
    [string]$ApiUrl
)

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "GitHub Secrets & Variables Setup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check GitHub CLI
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    $ghVersion = gh --version 2>$null | Select-Object -First 1
    Write-Host "  [OK] GitHub CLI: $ghVersion" -ForegroundColor Green
} catch {
    Write-Host "  [ERROR] GitHub CLI (gh) not found." -ForegroundColor Red
    Write-Host "  Install from: https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}

# Check if authenticated
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  [ERROR] Not authenticated with GitHub CLI." -ForegroundColor Red
    Write-Host "  Run: gh auth login" -ForegroundColor Yellow
    exit 1
}
Write-Host "  [OK] GitHub CLI authenticated" -ForegroundColor Green

# Check if in a git repo
try {
    $repoInfo = gh repo view --json nameWithOwner 2>$null | ConvertFrom-Json
    $repoName = $repoInfo.nameWithOwner
    Write-Host "  [OK] Repository: $repoName" -ForegroundColor Green
} catch {
    Write-Host "  [ERROR] Not in a GitHub repository or repo not found." -ForegroundColor Red
    exit 1
}

Write-Host ""

# Collect values interactively if not provided
if (-not $FunctionAppName) {
    $FunctionAppName = Read-Host "Enter your Azure Function App name (e.g., func-refapp-dev)"
}

if (-not $ApiUrl) {
    $defaultApiUrl = "https://$FunctionAppName.azurewebsites.net"
    $ApiUrl = Read-Host "Enter your API URL [$defaultApiUrl]"
    if ([string]::IsNullOrWhiteSpace($ApiUrl)) {
        $ApiUrl = $defaultApiUrl
    }
}

if (-not $PublishProfilePath) {
    Write-Host ""
    Write-Host "To get the publish profile:" -ForegroundColor Yellow
    Write-Host "  1. Go to Azure Portal > Function App > Overview" -ForegroundColor Gray
    Write-Host "  2. Click 'Download publish profile'" -ForegroundColor Gray
    Write-Host "  3. Save the file and provide the path below" -ForegroundColor Gray
    Write-Host ""
    $PublishProfilePath = Read-Host "Enter path to publish profile file"
}

if (-not $StaticWebAppToken) {
    Write-Host ""
    Write-Host "To get the Static Web App deployment token:" -ForegroundColor Yellow
    Write-Host "  1. Go to Azure Portal > Static Web App > Overview" -ForegroundColor Gray
    Write-Host "  2. Click 'Manage deployment token'" -ForegroundColor Gray
    Write-Host "  3. Copy the token" -ForegroundColor Gray
    Write-Host ""
    $StaticWebAppToken = Read-Host "Enter Static Web App deployment token"
}

# Validate publish profile file
if (-not (Test-Path $PublishProfilePath)) {
    Write-Host "[ERROR] Publish profile file not found: $PublishProfilePath" -ForegroundColor Red
    exit 1
}

$publishProfileContent = Get-Content -Path $PublishProfilePath -Raw

Write-Host ""
Write-Host "Setting up secrets and variables..." -ForegroundColor Yellow
Write-Host ""

# Set secrets
Write-Host "Setting AZURE_FUNCTIONAPP_PUBLISH_PROFILE..." -ForegroundColor Gray
$publishProfileContent | gh secret set AZURE_FUNCTIONAPP_PUBLISH_PROFILE
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] AZURE_FUNCTIONAPP_PUBLISH_PROFILE set" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Failed to set AZURE_FUNCTIONAPP_PUBLISH_PROFILE" -ForegroundColor Red
    exit 1
}

Write-Host "Setting AZURE_STATIC_WEB_APPS_API_TOKEN..." -ForegroundColor Gray
$StaticWebAppToken | gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] AZURE_STATIC_WEB_APPS_API_TOKEN set" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Failed to set AZURE_STATIC_WEB_APPS_API_TOKEN" -ForegroundColor Red
    exit 1
}

# Set variables
Write-Host "Setting VITE_API_URL variable..." -ForegroundColor Gray
gh variable set VITE_API_URL --body $ApiUrl
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] VITE_API_URL set to: $ApiUrl" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Failed to set VITE_API_URL" -ForegroundColor Red
    exit 1
}

Write-Host "Setting VITE_SIGNALR_URL variable..." -ForegroundColor Gray
gh variable set VITE_SIGNALR_URL --body $ApiUrl
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] VITE_SIGNALR_URL set to: $ApiUrl" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Failed to set VITE_SIGNALR_URL" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Configured secrets:" -ForegroundColor Cyan
Write-Host "  - AZURE_FUNCTIONAPP_PUBLISH_PROFILE" -ForegroundColor Gray
Write-Host "  - AZURE_STATIC_WEB_APPS_API_TOKEN" -ForegroundColor Gray
Write-Host ""
Write-Host "Configured variables:" -ForegroundColor Cyan
Write-Host "  - VITE_API_URL = $ApiUrl" -ForegroundColor Gray
Write-Host "  - VITE_SIGNALR_URL = $ApiUrl" -ForegroundColor Gray
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Update AZURE_FUNCTIONAPP_NAME in .github/workflows/deploy.yml" -ForegroundColor Gray
Write-Host "     Current value should be: $FunctionAppName" -ForegroundColor Gray
Write-Host "  2. Push to main branch to trigger deployment" -ForegroundColor Gray
Write-Host "  3. Or manually trigger: gh workflow run deploy.yml" -ForegroundColor Gray
Write-Host ""

# Verify secrets are set
Write-Host "Verifying configuration..." -ForegroundColor Yellow
$secrets = gh secret list --json name | ConvertFrom-Json
$variables = gh variable list --json name | ConvertFrom-Json

$requiredSecrets = @("AZURE_FUNCTIONAPP_PUBLISH_PROFILE", "AZURE_STATIC_WEB_APPS_API_TOKEN")
$requiredVars = @("VITE_API_URL", "VITE_SIGNALR_URL")

$allGood = $true

foreach ($secret in $requiredSecrets) {
    if ($secrets.name -contains $secret) {
        Write-Host "  [OK] Secret: $secret" -ForegroundColor Green
    } else {
        Write-Host "  [MISSING] Secret: $secret" -ForegroundColor Red
        $allGood = $false
    }
}

foreach ($var in $requiredVars) {
    if ($variables.name -contains $var) {
        Write-Host "  [OK] Variable: $var" -ForegroundColor Green
    } else {
        Write-Host "  [MISSING] Variable: $var" -ForegroundColor Red
        $allGood = $false
    }
}

if ($allGood) {
    Write-Host ""
    Write-Host "All secrets and variables are configured correctly!" -ForegroundColor Green
}
