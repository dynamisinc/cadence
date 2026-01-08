<#
.SYNOPSIS
    Scaffolds a new feature in the Dynamis Reference App.

.DESCRIPTION
    Creates the necessary folder structure and boilerplate files for a new feature
    in both the backend (API) and frontend (React).

.PARAMETER FeatureName
    The name of the feature (e.g., "Invoices", "Customers").
    Should be PascalCase.

.EXAMPLE
    .\New-Feature.ps1 -FeatureName "Invoices"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$FeatureName
)

$ErrorActionPreference = "Stop"

# Helper to convert PascalCase to camelCase
function ConvertTo-CamelCase {
    param([string]$InputString)
    return $InputString.Substring(0,1).ToLower() + $InputString.Substring(1)
}

$featureNamePascal = $FeatureName
$featureNameCamel = ConvertTo-CamelCase $FeatureName

Write-Host "Scaffolding feature: $featureNamePascal" -ForegroundColor Cyan

# ==============================================================================
# Core Scaffolding (Business Logic)
# ==============================================================================

$coreRoot = Join-Path $PSScriptRoot "..\src\Dynamis.Core\Tools\$featureNamePascal"
if (Test-Path $coreRoot) {
    Write-Warning "Core feature folder already exists: $coreRoot"
} else {
    Write-Host "Creating Core folders..."
    New-Item -ItemType Directory -Path "$coreRoot\Models\DTOs" -Force | Out-Null
    New-Item -ItemType Directory -Path "$coreRoot\Models\Entities" -Force | Out-Null
    New-Item -ItemType Directory -Path "$coreRoot\Services" -Force | Out-Null
    New-Item -ItemType Directory -Path "$coreRoot\Validators" -Force | Out-Null
    New-Item -ItemType Directory -Path "$coreRoot\Mappers" -Force | Out-Null

    # Create Service Interface
    $serviceInterfaceContent = @"
using DynamisReferenceApp.Api.Tools.$featureNamePascal.Models.DTOs;

namespace DynamisReferenceApp.Api.Tools.$featureNamePascal.Services;

public interface I${featureNamePascal}Service
{
    Task<IEnumerable<${featureNamePascal}Dto>> Get${featureNamePascal}sAsync(string userId, CancellationToken cancellationToken = default);
    Task<${featureNamePascal}Dto?> Get${featureNamePascal}Async(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<${featureNamePascal}Dto> Create${featureNamePascal}Async(Create${featureNamePascal}Request request, string userId, CancellationToken cancellationToken = default);
}
"@
    Set-Content -Path "$coreRoot\Services\I${featureNamePascal}Service.cs" -Value $serviceInterfaceContent

    Write-Host "Core scaffolding complete." -ForegroundColor Green
}

# ==============================================================================
# Functions Scaffolding (Azure Functions)
# ==============================================================================

$functionsProject = Join-Path $PSScriptRoot "..\src\Dynamis.Functions"
if (Test-Path $functionsProject) {
    $functionsRoot = Join-Path $functionsProject "Tools\$featureNamePascal"
    if (Test-Path $functionsRoot) {
        Write-Warning "Functions feature folder already exists: $functionsRoot"
    } else {
        Write-Host "Creating Functions folders..."
        New-Item -ItemType Directory -Path "$functionsRoot\Functions" -Force | Out-Null

        # Create Function Class
        $functionContent = @"
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using DynamisReferenceApp.Api.Tools.$featureNamePascal.Services;

namespace DynamisReferenceApp.Api.Tools.$featureNamePascal.Functions;

public class ${featureNamePascal}Function
{
    private readonly I${featureNamePascal}Service _service;

    public ${featureNamePascal}Function(I${featureNamePascal}Service service)
    {
        _service = service;
    }

    [Function("Get${featureNamePascal}s")]
    public async Task<IActionResult> Get${featureNamePascal}s(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "${featureNameCamel}")] HttpRequest req)
    {
        // TODO: Get userId from claims
        var userId = "anonymous";
        var result = await _service.Get${featureNamePascal}sAsync(userId);
        return new OkObjectResult(result);
    }
}
"@
        Set-Content -Path "$functionsRoot\Functions\${featureNamePascal}Function.cs" -Value $functionContent

        Write-Host "Functions scaffolding complete." -ForegroundColor Green
    }
}

# ==============================================================================
# WebApi Scaffolding (Controllers)
# ==============================================================================

$webApiProject = Join-Path $PSScriptRoot "..\src\Dynamis.WebApi"
if (Test-Path $webApiProject) {
    $webApiRoot = Join-Path $webApiProject "Controllers"
    $controllerPath = Join-Path $webApiRoot "${featureNamePascal}Controller.cs"

    if (Test-Path $controllerPath) {
        Write-Warning "WebApi Controller already exists: $controllerPath"
    } else {
        Write-Host "Creating WebApi Controller..."

        # Create Controller Class
        $controllerContent = @"
using Microsoft.AspNetCore.Mvc;
using DynamisReferenceApp.Api.Tools.$featureNamePascal.Models.DTOs;
using DynamisReferenceApp.Api.Tools.$featureNamePascal.Services;

namespace DynamisReferenceApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ${featureNamePascal}Controller : ControllerBase
{
    private readonly I${featureNamePascal}Service _service;

    public ${featureNamePascal}Controller(I${featureNamePascal}Service service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<${featureNamePascal}Dto>>> Get${featureNamePascal}s(CancellationToken cancellationToken)
    {
        // TODO: Get userId from User.Identity
        var userId = "anonymous";
        var result = await _service.Get${featureNamePascal}sAsync(userId, cancellationToken);
        return Ok(result);
    }
}
"@
        Set-Content -Path $controllerPath -Value $controllerContent

        Write-Host "WebApi scaffolding complete." -ForegroundColor Green
    }
}# ==============================================================================
# Frontend Scaffolding
# ==============================================================================

$frontendRoot = Join-Path $PSScriptRoot "..\src\frontend\src\tools\$featureNameCamel"
if (Test-Path $frontendRoot) {
    Write-Warning "Frontend feature folder already exists: $frontendRoot"
} else {
    Write-Host "Creating frontend folders..."
    New-Item -ItemType Directory -Path "$frontendRoot\components" -Force | Out-Null
    New-Item -ItemType Directory -Path "$frontendRoot\hooks" -Force | Out-Null
    New-Item -ItemType Directory -Path "$frontendRoot\pages" -Force | Out-Null
    New-Item -ItemType Directory -Path "$frontendRoot\services" -Force | Out-Null

    # Create types file
    $typesContent = @"
export interface ${featureNamePascal}Dto {
  id: string;
  title: string;
  createdAt: string;
}

export interface Create${featureNamePascal}Request {
  title: string;
}
"@
    Set-Content -Path "$frontendRoot\types.ts" -Value $typesContent

    # Create Page Component
    $pageContent = @"
import { Box, Typography } from '@mui/material'
import CobraStyles from '../../../theme/CobraStyles'

export const ${featureNamePascal}Page = () => {
  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Typography variant="h4" gutterBottom>
        $featureNamePascal
      </Typography>
      <Typography variant="body1">
        Welcome to the $featureNamePascal feature.
      </Typography>
    </Box>
  )
}
"@
    Set-Content -Path "$frontendRoot\pages\${featureNamePascal}Page.tsx" -Value $pageContent

    Write-Host "Frontend scaffolding complete." -ForegroundColor Green
}

Write-Host "Feature '$featureNamePascal' created successfully!" -ForegroundColor Cyan
Write-Host "Next steps:"
Write-Host "1. Implement the backend Service and Function."
Write-Host "2. Register the service in ServiceCollectionExtensions.cs."
Write-Host "3. Add the route to App.tsx in the frontend."
