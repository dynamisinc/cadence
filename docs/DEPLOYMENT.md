# Azure Deployment Guide

> **Version:** 1.0.0
> **Last Updated:** 2025-01-09

This guide covers deploying the Cadence to Azure, including all required resources and configuration.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Azure Resources Required](#azure-resources-required)
3. [Cost Considerations](#cost-considerations)
4. [Manual Setup Guide](#manual-setup-guide)
5. [GitHub Actions Deployment](#github-actions-deployment)
6. [Environment Configuration](#environment-configuration)
7. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

```
┌────────────────────────────────────────────────────────────┐
│                    Azure Resource Group                     │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌──────────────────┐    ┌───────────────────────────┐    │
│  │ Azure Static     │    │ Azure Functions           │    │
│  │ Web App (SWA)    │───▶│ (.NET 10 Isolated)       │    │
│  │                  │    │                           │    │
│  │ - React SPA      │    │ - HTTP Triggers (API)     │    │
│  │ - Global CDN     │    │ - SignalR Triggers        │    │
│  │ - Managed SSL    │    │                           │    │
│  └──────────────────┘    └──────────┬────────────────┘    │
│                                      │                     │
│                          ┌───────────┴───────────┐        │
│                          ▼                       ▼        │
│  ┌──────────────────────────┐  ┌──────────────────────┐  │
│  │ Azure Web API            │  │ Azure SignalR        │  │
│  │ (App Service)            │  │ Service              │  │
│  │                          │  │                      │  │
│  │ - ASP.NET Core 10        │  │ - Real-time updates  │  │
│  │ - Alternative API Host   │  │ - Free tier: 20K/day │  │
│  └──────────┬───────────────┘  └──────────────────────┘  │
│             │                                              │
│             ▼                                              │
│  ┌──────────────────────────┐  ┌──────────────────────┐  │
│  │ Azure SQL Database       │  │ Storage Account      │  │
│  │                          │  │                      │  │
│  │ - SQL Server 2019+       │  │ - Blob Storage       │  │
│  │ - EF Core migrations     │  │ - Table Storage      │  │
│  │ - Automatic backups      │  │ - Queue Storage      │  │
│  └──────────────────────────┘  └──────────────────────┘  │
│                                                            │
│  ┌──────────────────────────┐                              │
│  │ Application Insights     │                              │
│  │ (Optional)               │                              │
│  │                          │                              │
│  │ - Telemetry              │                              │
│  │ - Logging                │                              │
│  │ - Metrics                │                              │
│  └──────────────────────────┘                              │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## Azure Resources Required

### Required Resources

| Resource            | SKU/Tier       | Purpose           | Monthly Cost (Est.) |
| ------------------- | -------------- | ----------------- | ------------------- |
| **Azure Functions** | Consumption Y1 | API backend       | ~$0-5 (pay per use) |
| **Storage Account** | Standard LRS   | Functions storage | ~$1-2               |
| **Azure SQL**       | Basic (5 DTU)  | Database          | ~$5                 |
| **Static Web App**  | Free           | Frontend hosting  | $0                  |
| **SignalR Service** | Free           | Real-time         | $0                  |

**Estimated Monthly Cost:** ~$6-15 for development/small workloads

### Optional Resources

| Resource                 | SKU/Tier    | Purpose            | Monthly Cost (Est.) |
| ------------------------ | ----------- | ------------------ | ------------------- |
| **Application Insights** | Pay-per-use | Monitoring/logging | ~$2-5               |
| **Key Vault**            | Standard    | Secrets management | ~$0.50              |

---

## Cost Considerations

### Free Tier Options

1. **Azure SQL Basic (5 DTU):** ~$5/month

   - Alternative: Azure SQL Serverless (auto-pause) for even lower costs during dev
   - Alternative: SQL Server in Docker for local-only development

2. **Azure SignalR Free Tier:**

   - 20,000 messages/day
   - 20 concurrent connections
   - Sufficient for development and small production workloads

3. **Azure Static Web Apps Free Tier:**

   - 2 custom domains
   - 100 GB bandwidth/month
   - SSL included

4. **Azure Functions Consumption:**
   - 1 million free executions/month
   - 400,000 GB-s free compute/month

### Production Scaling

When scaling for production:

| Resource       | Development   | Production        |
| -------------- | ------------- | ----------------- |
| Azure SQL      | Basic (5 DTU) | Standard S0+      |
| SignalR        | Free          | Standard (1 unit) |
| Functions      | Consumption   | Premium EP1       |
| Static Web App | Free          | Standard          |

---

## Manual Setup Guide

### Prerequisites

1. Azure CLI installed: `az --version`
2. Azure subscription with permissions to create resources
3. Git repository with the application code

### Step 1: Login to Azure

```bash
# Login to Azure
az login

# Set the subscription (if multiple)
az account set --subscription "Your Subscription Name"
```

### Step 2: Create Resource Group

```bash
# Variables
RESOURCE_GROUP="cadence-app-rg"
LOCATION="eastus2"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

### Step 3: Create Storage Account

```bash
STORAGE_NAME="cadenceappstorage"  # Must be globally unique

az storage account create \
  --name $STORAGE_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS
```

### Step 4: Create Azure SQL Database

```bash
SQL_SERVER_NAME="cadence-sql-server"  # Must be globally unique
SQL_ADMIN="sqladmin"
SQL_PASSWORD="YourSecurePassword123!"  # Change this!
SQL_DB_NAME="Cadence"

# Create SQL Server
az sql server create \
  --name $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user $SQL_ADMIN \
  --admin-password $SQL_PASSWORD

# Create database (Basic tier - cheapest)
az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name $SQL_DB_NAME \
  --edition Basic \
  --capacity 5

# Allow Azure services to access (required for Functions)
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Get connection string
echo "Connection String:"
echo "Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;Database=$SQL_DB_NAME;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=true;Connection Timeout=30;"
```

### Step 5: Create SignalR Service

```bash
SIGNALR_NAME="cadence-signalr"  # Must be globally unique

az signalr create \
  --name $SIGNALR_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Free_F1 \
  --service-mode Serverless

# Get connection string
az signalr key list \
  --name $SIGNALR_NAME \
  --resource-group $RESOURCE_GROUP \
  --query primaryConnectionString -o tsv
```

### Step 6: Create Azure Function App

```bash
FUNCTION_APP_NAME="cadence-api"  # Must be globally unique

# Create Function App
az functionapp create \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --storage-account $STORAGE_NAME \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --runtime-version 10.0 \
  --functions-version 4
```

### Step 7: Create Static Web App

```bash
# For GitHub-integrated deployment
az staticwebapp create \
  --name "cadence-frontend" \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --source "https://github.com/your-org/your-repo" \
  --branch "main" \
  --app-location "/src/frontend" \
  --output-location "dist"
```

### Step 8: Configure Application Settings

```bash
# Set SQL connection string
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings "ConnectionStrings__DefaultConnection=Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;Database=$SQL_DB_NAME;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=true;"

# Set SignalR connection string
SIGNALR_CONN=$(az signalr key list --name $SIGNALR_NAME --resource-group $RESOURCE_GROUP --query primaryConnectionString -o tsv)
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings "AzureSignalRConnectionString=$SIGNALR_CONN"
```

### Step 9: Deploy Application

#### Deploy Backend (Azure Functions)

```bash
cd src/api

# Build
dotnet publish -c Release -o ./publish

# Deploy
cd publish
zip -r ../deploy.zip .
az functionapp deployment source config-zip \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src ../deploy.zip
```

#### Deploy Frontend (Static Web App)

If using GitHub Actions (recommended), the deployment is automatic on push.

For manual deployment:

```bash
cd src/frontend

# Build
npm run build

# Deploy using SWA CLI
npm install -g @azure/static-web-apps-cli
swa deploy ./dist \
  --deployment-token <your-deployment-token>
```

---

## GitHub Actions Deployment

### Required Secrets

Configure these in your GitHub repository Settings > Secrets:

| Secret Name                         | Description                  | How to Get                                              |
| ----------------------------------- | ---------------------------- | ------------------------------------------------------- |
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | Function App publish profile | Azure Portal > Function App > Get publish profile       |
| `AZURE_WEBAPP_PUBLISH_PROFILE`      | Web App publish profile      | Azure Portal > App Service > Get publish profile        |
| `AZURE_STATIC_WEB_APPS_API_TOKEN`   | SWA deployment token         | Azure Portal > Static Web App > Manage deployment token |
| `SQL_CONNECTION_STRING`             | Database connection string   | From Step 4 above                                       |

### Deployment Options

The deployment workflow supports deploying either the Azure Functions app, the Web API app, or both.

**Inputs:**

- `environment`: Target environment (production/staging).
- `hosting_model`: Choose which backend to deploy:
  - `functions`: Deploy only Azure Functions.
  - `webapi`: Deploy only Web API (Default).
  - `both`: Deploy both.

### CI Workflow (`.github/workflows/ci.yml`)

```yaml
name: CI

on:
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Restore dependencies
        run: dotnet restore src/api/Api.csproj

      - name: Build
        run: dotnet build src/api/Api.csproj --no-restore --configuration Release

      - name: Test Backend
        run: dotnet test src/api.Tests/Api.Tests.csproj --configuration Release --verbosity normal

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"
          cache: "npm"
          cache-dependency-path: src/frontend/package-lock.json

      - name: Install frontend dependencies
        run: npm ci
        working-directory: src/frontend

      - name: Lint frontend
        run: npm run lint
        working-directory: src/frontend

      - name: Test frontend
        run: npm run test:run
        working-directory: src/frontend

      - name: Build frontend
        run: npm run build
        working-directory: src/frontend
```

### Deploy Workflow (`.github/workflows/deploy.yml`)

```yaml
name: Deploy

on:
  push:
    branches: [main]

jobs:
  deploy-backend:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Build
        run: dotnet publish src/api/Api.csproj -c Release -o ./publish

      - name: Deploy to Azure Functions
        uses: Azure/functions-action@v1
        with:
          app-name: "cadence-api" # Pattern: <project>-<component>
          package: "./publish"
          publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}

  deploy-frontend:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"

      - name: Install dependencies
        run: npm ci
        working-directory: src/frontend

      - name: Build
        run: npm run build
        working-directory: src/frontend
        env:
          VITE_API_URL: ${{ vars.VITE_API_URL }}

      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "/src/frontend"
          output_location: "dist"
```

---

## Environment Configuration

### Backend (Azure Functions)

Configure these Application Settings in Azure Portal or via CLI:

| Setting                                 | Description               | Example                                       |
| --------------------------------------- | ------------------------- | --------------------------------------------- |
| `ConnectionStrings__DefaultConnection`  | SQL connection string     | `Server=tcp:xxx.database.windows.net...`      |
| `AzureSignalRConnectionString`          | SignalR connection string | `Endpoint=https://xxx.service.signalr.net...` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights (optional)   | `InstrumentationKey=xxx...`                   |

### Frontend

Create `.env.production` in `src/frontend/`:

```bash
VITE_API_URL=https://cadence-api.azurewebsites.net
VITE_SIGNALR_URL=https://cadence-api.azurewebsites.net
```

---

## Troubleshooting

### Function App Returns 500

1. Check Application Insights logs (if configured)
2. Enable detailed errors:
   ```bash
   az functionapp config appsettings set \
     --name $FUNCTION_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --settings "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated"
   ```
3. Check connection strings are correct

### SQL Connection Fails

1. Verify firewall rules allow Azure services
2. Check connection string format
3. Verify credentials are correct

### SignalR Connection Issues

1. Verify SignalR service is in Serverless mode
2. Check CORS settings on Function App
3. Verify connection string includes AccessKey

### Static Web App Build Fails

1. Check `app_location` and `output_location` in workflow
2. Verify `npm run build` succeeds locally
3. Check for missing environment variables

### .NET Version Issues

If you encounter .NET version compatibility issues:

1. Verify Azure Functions supports .NET 10 in your region
2. Check the runtime version in Azure Portal > Function App > Configuration > General settings
3. Ensure `--runtime-version 10.0` is specified when creating the Function App

---

## Post-Deployment Checklist

- [ ] Verify health endpoint: `https://your-api.azurewebsites.net/api/health`
- [ ] Test API endpoints with Postman/curl
- [ ] Verify frontend loads correctly
- [ ] Test SignalR connection
- [ ] Check Application Insights for errors (if configured)
- [ ] Configure custom domain (optional)
- [ ] Enable HTTPS-only
- [ ] Review security settings
