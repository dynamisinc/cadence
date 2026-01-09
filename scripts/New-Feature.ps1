<#
.SYNOPSIS
    Scaffolds a new feature in the Cadence application.

.DESCRIPTION
    Creates the necessary folder structure and boilerplate files for a new feature
    in both the backend (Core/WebApi) and frontend (React).

.PARAMETER FeatureName
    The name of the feature (e.g., "Exercises", "Injects").
    Should be PascalCase.

.EXAMPLE
    .\New-Feature.ps1 -FeatureName "Exercises"
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

$coreRoot = Join-Path $PSScriptRoot "..\src\Cadence.Core\Features\$featureNamePascal"
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
using Cadence.Core.Features.$featureNamePascal.Models.DTOs;

namespace Cadence.Core.Features.$featureNamePascal.Services;

public interface I${featureNamePascal}Service
{
    Task<IEnumerable<${featureNamePascal}Dto>> Get${featureNamePascal}sAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<${featureNamePascal}Dto?> Get${featureNamePascal}Async(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<${featureNamePascal}Dto> Create${featureNamePascal}Async(Create${featureNamePascal}Request request, Guid userId, CancellationToken cancellationToken = default);
}
"@
    Set-Content -Path "$coreRoot\Services\I${featureNamePascal}Service.cs" -Value $serviceInterfaceContent

    Write-Host "Core scaffolding complete." -ForegroundColor Green
}

# ==============================================================================
# Functions Scaffolding (Azure Functions - Background Jobs Only)
# ==============================================================================

$functionsProject = Join-Path $PSScriptRoot "..\src\Cadence.Functions"
if (Test-Path $functionsProject) {
    Write-Host "Note: Azure Functions are for background jobs only. REST API endpoints go in WebApi." -ForegroundColor Yellow
}

# ==============================================================================
# WebApi Scaffolding (Controllers)
# ==============================================================================

$webApiProject = Join-Path $PSScriptRoot "..\src\Cadence.WebApi"
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
using Cadence.Core.Features.$featureNamePascal.Models.DTOs;
using Cadence.Core.Features.$featureNamePascal.Services;

namespace Cadence.WebApi.Controllers;

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
        var userId = Guid.Empty;
        var result = await _service.Get${featureNamePascal}sAsync(userId, cancellationToken);
        return Ok(result);
    }
}
"@
        Set-Content -Path $controllerPath -Value $controllerContent

        Write-Host "WebApi scaffolding complete." -ForegroundColor Green
    }
}

# ==============================================================================
# Frontend Scaffolding
# ==============================================================================

$frontendRoot = Join-Path $PSScriptRoot "..\src\frontend\src\features\$featureNameCamel"
if (Test-Path $frontendRoot) {
    Write-Warning "Frontend feature folder already exists: $frontendRoot"
} else {
    Write-Host "Creating frontend folders..."
    New-Item -ItemType Directory -Path "$frontendRoot\components" -Force | Out-Null
    New-Item -ItemType Directory -Path "$frontendRoot\hooks" -Force | Out-Null
    New-Item -ItemType Directory -Path "$frontendRoot\pages" -Force | Out-Null
    New-Item -ItemType Directory -Path "$frontendRoot\services" -Force | Out-Null
    New-Item -ItemType Directory -Path "$frontendRoot\types" -Force | Out-Null

    # Create types file
    $typesContent = @"
export interface ${featureNamePascal}Dto {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
}

export interface Create${featureNamePascal}Request {
  title: string;
}
"@
    Set-Content -Path "$frontendRoot\types\index.ts" -Value $typesContent

    # Create Page Component
    $pageContent = @"
import { Box, Typography } from '@mui/material'
import CobraStyles from '@/theme/CobraStyles'

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
Write-Host "1. Implement the backend Service in Core/Features/$featureNamePascal/Services/"
Write-Host "2. Register the service in Program.cs"
Write-Host "3. Add the route to App.tsx in the frontend"
