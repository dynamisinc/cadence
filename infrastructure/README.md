# Infrastructure as Code

> **Status: Reference Only** - These Bicep templates are provided as a starting point and have not been fully validated in production Azure pipelines. Test thoroughly before using in production.

## Overview

This directory contains Azure Bicep templates for deploying the application infrastructure.

## Resources Deployed

| Resource | Purpose |
|----------|---------|
| Azure Functions | Backend API hosting |
| Azure Static Web App | Frontend hosting |
| Azure SQL Database | Data persistence |
| Azure SignalR Service | Real-time communication |
| Application Insights | Monitoring and logging |
| Storage Account | Function app storage |

## Usage

### Prerequisites

- Azure CLI installed
- Azure subscription with appropriate permissions
- Resource group created

### Deploy Infrastructure

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "Your Subscription Name"

# Create resource group (if not exists)
az group create --name rg-myapp-dev --location eastus2

# Deploy infrastructure
az deployment group create \
  --resource-group rg-myapp-dev \
  --template-file main.bicep \
  --parameters environment=dev \
  --parameters sqlAdminLogin=sqladmin \
  --parameters sqlAdminPassword=<secure-password>
```

### Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `environment` | Environment name (dev, staging, prod) | `dev` |
| `location` | Azure region | Resource group location |
| `appName` | Application name prefix | `dynamis` |
| `sqlAdminLogin` | SQL Server admin username | Required |
| `sqlAdminPassword` | SQL Server admin password | Required |

## Module Structure

```
infrastructure/
├── main.bicep              # Main orchestration template
└── modules/
    ├── function-app.bicep  # Azure Functions
    ├── static-web-app.bicep # Azure SWA
    ├── sql-server.bicep    # Azure SQL
    ├── storage.bicep       # Storage Account
    ├── signalr.bicep       # SignalR Service
    └── app-insights.bicep  # Application Insights
```

## Cost Estimation (Development Tier)

| Resource | SKU | Est. Monthly Cost |
|----------|-----|-------------------|
| Azure Functions | Consumption | ~$0-10 |
| Static Web App | Free | $0 |
| Azure SQL | Basic (DTU) | ~$5 |
| SignalR | Free | $0 |
| Application Insights | Per GB | ~$2-5 |
| Storage | Standard LRS | ~$1 |
| **Total** | | **~$8-20/month** |

## Important Notes

1. **SQL Server Free Tier**: For development, use the Basic tier or Serverless with auto-pause
2. **SignalR Free Tier**: Limited to 20 concurrent connections - upgrade for production
3. **Static Web App**: Free tier has 100GB bandwidth/month
4. **Always review** generated resources before deploying to production

## See Also

- [DEPLOYMENT.md](../docs/DEPLOYMENT.md) - Full deployment guide
- [Azure Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
