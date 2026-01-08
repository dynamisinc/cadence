# Scripts

Helper scripts for development and deployment.

## Available Scripts

| Script | Description |
|--------|-------------|
| `setup-local.ps1` | Set up local development environment |
| `setup-local.sh` | Set up local development environment (Bash) |
| `run-migrations.ps1` | Apply EF Core migrations |
| `setup-github-secrets.ps1` | Configure GitHub secrets and variables for deployment |
| `create-github-issues.ps1` | Create GitHub issues from user stories |
| `deploy-azure-resources.ps1` | Deploy Azure infrastructure |

## Usage

### Windows (PowerShell)

```powershell
.\scripts\setup-local.ps1
```

### macOS/Linux (Bash)

```bash
./scripts/setup-local.sh
```

## Script Details

### setup-local.ps1

Sets up the local development environment:
- Verifies prerequisites (.NET, Node.js, Azure Functions Core Tools)
- Creates local.settings.json from template
- Creates .env from template
- Restores NuGet packages
- Installs npm packages
- Applies database migrations

### run-migrations.ps1

Applies EF Core migrations to the database:
- Supports local and Azure SQL databases
- Optionally generates SQL scripts for review

### create-github-issues.ps1

Parses USER_STORIES.md files and creates GitHub issues:
- Reads from `docs/features/{feature}/USER_STORIES.md`
- Creates issues with proper labels
- Links related issues

### setup-github-secrets.ps1

Configures GitHub repository secrets and variables for Azure deployment:

- Sets `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` secret from downloaded publish profile
- Sets `AZURE_STATIC_WEB_APPS_API_TOKEN` secret
- Sets `VITE_API_URL` and `VITE_SIGNALR_URL` variables
- Verifies all secrets and variables are configured correctly

**Prerequisites:**

- GitHub CLI installed and authenticated (`gh auth login`)
- Azure resources provisioned (see `docs/AZURE_PROVISIONING.md`)
- Publish profile downloaded from Azure Function App
- Deployment token from Azure Static Web App

**Usage:**

```powershell
# Interactive mode (prompts for values)
.\scripts\setup-github-secrets.ps1

# With parameters
.\scripts\setup-github-secrets.ps1 `
  -FunctionAppName "func-refapp-dev" `
  -PublishProfilePath ".\func-refapp-dev.PublishSettings" `
  -StaticWebAppToken "your-token-here"
```

### deploy-azure-resources.ps1

Deploys Azure resources using Bicep:
- Creates resource group if needed
- Deploys Bicep templates
- Configures GitHub secrets
- Outputs deployment summary
