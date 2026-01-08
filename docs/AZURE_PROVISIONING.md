# Azure Resource Provisioning Guide

> **Last Updated:** December 2025
> **Estimated Time:** 30-45 minutes
> **Estimated Cost:** ~$6-15/month for development workloads

This guide walks you through creating and configuring all Azure resources needed to run the Dynamis Reference App. No prior Azure experience required.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Overview of Resources](#overview-of-resources)
3. [Step 1: Create Resource Group](#step-1-create-resource-group)
4. [Step 2: Create Storage Account](#step-2-create-storage-account)
5. [Step 3: Create Azure SQL Database](#step-3-create-azure-sql-database)
6. [Step 4: Create Azure SignalR Service](#step-4-create-azure-signalr-service)
7. [Step 5: Create Azure Function App](#step-5-create-azure-function-app)
8. [Step 6: Create Azure Static Web App](#step-6-create-azure-static-web-app)
9. [Step 7: Create Application Insights (Optional)](#step-7-create-application-insights-optional)
10. [Step 8: Link Static Web App to Function App](#step-8-link-static-web-app-to-function-app)
11. [Step 9: Configure GitHub Secrets](#step-9-configure-github-secrets)
12. [Verification Checklist](#verification-checklist)
13. [Cleanup](#cleanup)

---

## Prerequisites

### Azure Account

1. Go to [Azure Portal](https://portal.azure.com)
2. Sign in or create a free account
   - Free accounts include $200 credit for 30 days
   - Many services have perpetual free tiers

### Tools (Choose One)

**Option A: Azure Portal (Web UI)**

- Just need a web browser
- Best for beginners

**Option B: Azure CLI (Command Line)**

- Install: [Azure CLI Installation Guide](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- Verify: `az --version`
- Login: `az login`

This guide provides instructions for both options.

---

## Overview of Resources

All resources will be created in a single **Resource Group** for easy management.

```
┌─────────────────────────────────────────────────────────────┐
│              Resource Group: rg-refapp-dev                   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────┐  ┌─────────────────────────────────┐   │
│  │ strefappdev     │  │ sql-refapp-dev                  │   │
│  │ (Storage Acct)  │  │ └── sqldb-refapp-dev            │   │
│  └─────────────────┘  └─────────────────────────────────┘   │
│                                                              │
│  ┌─────────────────┐  ┌─────────────────────────────────┐   │
│  │ sigr-refapp-dev │  │ func-refapp-dev                 │   │
│  │ (SignalR)       │  │ (Function App)                  │   │
│  └─────────────────┘  └─────────────────────────────────┘   │
│                                                              │
│  ┌─────────────────┐  ┌─────────────────────────────────┐   │
│  │ stapp-refapp-   │  │ appi-refapp-dev (optional)      │   │
│  │ dev (SWA)       │  │ (App Insights)                  │   │
│  └─────────────────┘  └─────────────────────────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Naming Convention

Following [Microsoft's Cloud Adoption Framework (CAF)](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming), Azure resources should use a consistent naming pattern:

```
<resource-type-abbreviation>-<workload/app>-<environment>[-<region>][-<###>]
```

**Components:**

- **Resource type abbreviation**: Standard abbreviations from CAF (see table below)
- **Workload/app**: Your application name (e.g., `refapp`, `invoicing`, `crm`)
- **Environment**: `dev`, `test`, `staging`, `prod`
- **Region** (optional): Short region code (e.g., `eus2` for East US 2)
- **Instance** (optional): Numeric suffix for multiple instances

**Standard Abbreviations:**

| Resource Type           | Abbreviation             |
| ----------------------- | ------------------------ |
| Resource Group          | `rg`                     |
| Storage Account         | `st` (no dashes allowed) |
| SQL Server              | `sql`                    |
| SQL Database            | `sqldb`                  |
| SignalR Service         | `sigr`                   |
| Function App            | `func`                   |
| Static Web App          | `stapp`                  |
| Application Insights    | `appi`                   |
| Log Analytics Workspace | `log`                    |

**Example Names for Development Environment:**

| Resource             | Name Example                                       |
| -------------------- | -------------------------------------------------- |
| Resource Group       | `rg-refapp-dev`                                    |
| Storage Account      | `strefappdev` (lowercase, no dashes, max 24 chars) |
| SQL Server           | `sql-refapp-dev`                                   |
| SQL Database         | `sqldb-refapp-dev`                                 |
| SignalR Service      | `sigr-refapp-dev`                                  |
| Function App         | `func-refapp-dev`                                  |
| Static Web App       | `stapp-refapp-dev`                                 |
| Application Insights | `appi-refapp-dev`                                  |

> **Note:** Region codes in names are optional. Microsoft's CAF guidance notes that including region can cause confusion if resources are later moved. Only include region if you have multi-region deployments where distinction is necessary.

---

## Step 1: Create Resource Group

A Resource Group is a container that holds all your Azure resources.

### Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** (+ icon in top left)
3. Search for **Resource group**
4. Click **Create**
5. Fill in:
   - **Subscription:** Your subscription
   - **Resource group:** `rg-refapp-dev`
   - **Region:** Choose closest to your users (e.g., `East US 2`)
6. Click **Review + create** → **Create**

### CLI

```bash
# Set variables (customize these)
# Pattern: rg-<workload>-<environment>
APP_NAME="refapp"
ENVIRONMENT="dev"
RESOURCE_GROUP="rg-${APP_NAME}-${ENVIRONMENT}"
LOCATION="eastus2"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

---

## Step 2: Create Storage Account

Azure Functions require a Storage Account for internal operations.

### Portal

1. Click **Create a resource** → Search **Storage account**
2. Click **Create**
3. Fill in:
   - **Resource group:** `rg-refapp-dev`
   - **Storage account name:** `strefappdev` (must be globally unique, lowercase, no dashes, 3-24 chars)
   - **Region:** Same as resource group
   - **Performance:** Standard
   - **Redundancy:** Locally-redundant storage (LRS) - cheapest option
4. Click **Review + create** → **Create**
5. Wait for deployment (~1 minute)

### CLI

```bash
# Pattern: st<workload><environment> (no dashes allowed for storage accounts)
STORAGE_NAME="st${APP_NAME}${ENVIRONMENT}"  # e.g., strefappdev

az storage account create \
  --name $STORAGE_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2
```

---

## Step 3: Create Azure SQL Database

### Portal

#### 3a. Create SQL Server

1. Click **Create a resource** → Search **SQL Database**
2. Click **Create**
3. On the **Basics** tab:
   - **Resource group:** `rg-refapp-dev`
   - **Database name:** `sqldb-refapp-dev`
   - **Server:** Click **Create new**
     - **Server name:** `sql-refapp-dev` (must be globally unique)
     - **Location:** Same as resource group
     - **Authentication method:** Use SQL authentication
     - **Server admin login:** `sqladmin`
     - **Password:** Create a strong password (save this!)
     - Click **OK**
   - **Workload environment:** Development
   - **Compute + storage:** Click **Configure database**
     - Select **Basic** tier (~$5/month)
     - Click **Apply**
4. Click **Next: Networking**
5. On the **Networking** tab:
   - **Connectivity method:** Public endpoint
   - **Allow Azure services:** Yes ✓
   - **Add current client IP:** Yes ✓ (for local development)
6. Click **Review + create** → **Create**
7. Wait for deployment (~5 minutes)

#### 3b. Get Connection String

1. Go to your SQL Database resource
2. Click **Connection strings** in the left menu
3. Copy the **ADO.NET (SQL authentication)** connection string
4. Replace `{your_password}` with your actual password
5. Save this for later

### CLI

```bash
# Pattern: sql-<workload>-<environment> for server, sqldb-<workload>-<environment> for database
SQL_SERVER="sql-${APP_NAME}-${ENVIRONMENT}"  # e.g., sql-refapp-dev
SQL_ADMIN="sqladmin"
SQL_PASSWORD="YourStr0ngP@ssword!"  # Change this!
SQL_DB="sqldb-${APP_NAME}-${ENVIRONMENT}"  # e.g., sqldb-refapp-dev

# Create SQL Server
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user $SQL_ADMIN \
  --admin-password "$SQL_PASSWORD"

# Create Database (Basic tier - ~$5/month)
az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DB \
  --edition Basic \
  --capacity 5

# Allow Azure services to access
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Allow your current IP (for local development)
MY_IP=$(curl -s ifconfig.me)
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name "AllowMyIP" \
  --start-ip-address $MY_IP \
  --end-ip-address $MY_IP

# Print connection string
echo "Connection String:"
echo "Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$SQL_DB;Persist Security Info=False;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

---

## Step 4: Create Azure SignalR Service

SignalR enables real-time updates between browser sessions.

### Portal

1. Click **Create a resource** → Search **SignalR Service**
2. Click **Create**
3. Fill in:
   - **Resource group:** `rg-refapp-dev`
   - **Resource name:** `sigr-refapp-dev` (must be globally unique)
   - **Region:** Same as resource group
   - **Pricing tier:** **Free** (F1)
     - 20 concurrent connections
     - 20,000 messages/day
   - **Service mode:** **Serverless** ⚠️ **Important!**
4. Click **Review + create** → **Create**
5. Wait for deployment (~1 minute)

#### Get Connection String

1. Go to your SignalR resource
2. Click **Keys** in the left menu under Settings
3. Copy the **Connection string** (Primary)
4. Save this for later

### CLI

```bash
# Pattern: sigr-<workload>-<environment>
SIGNALR_NAME="sigr-${APP_NAME}-${ENVIRONMENT}"  # e.g., sigr-refapp-dev

# Create SignalR Service (Free tier, Serverless mode)
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
  --query primaryConnectionString \
  --output tsv
```

---

## Step 5: Create Azure Function App

The Function App hosts your .NET API backend.

### Portal

1. Click **Create a resource** → Search **Function App**
2. Click **Create**
3. On the **Basics** tab:
   - **Resource group:** `rg-refapp-dev`
   - **Function App name:** `func-refapp-dev` (must be globally unique)
   - **Runtime stack:** .NET
   - **Version:** 10 (Isolated)
   - **Region:** Same as resource group
   - **Operating System:** Linux (recommended - cheaper, faster cold starts)
   - **Hosting plan:** Consumption (Serverless)
4. Click **Next: Storage**
   - **Storage account:** Select `strefappdev` (created earlier)
5. Click **Next: Networking** → Skip (use defaults)
6. Click **Next: Monitoring**
   - **Enable Application Insights:** Yes (recommended) or No
   - If Yes, create new or select existing
7. Click **Review + create** → **Create**
8. Wait for deployment (~2 minutes)

#### Configure Application Settings

1. Go to your Function App resource
2. Click **Environment variables** in the left menu (under Settings)
3. Click **+ Add** and add these settings:

| Name                                   | Value                                      |
| -------------------------------------- | ------------------------------------------ |
| `ConnectionStrings__DefaultConnection` | Your SQL connection string from Step 3     |
| `AzureSignalRConnectionString`         | Your SignalR connection string from Step 4 |

4. Click **Apply** → **Confirm**

### CLI

```bash
# Pattern: func-<workload>-<environment>
FUNCTION_APP="func-${APP_NAME}-${ENVIRONMENT}"  # e.g., func-refapp-dev

# Create Function App
az functionapp create \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --storage-account $STORAGE_NAME \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --runtime-version 10.0 \
  --functions-version 4 \
  --os-type Linux

# Get SignalR connection string
SIGNALR_CONN=$(az signalr key list \
  --name $SIGNALR_NAME \
  --resource-group $RESOURCE_GROUP \
  --query primaryConnectionString \
  --output tsv)

# Configure app settings
az functionapp config appsettings set \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$SQL_DB;Persist Security Info=False;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
    "AzureSignalRConnectionString=$SIGNALR_CONN"
```

---

## Step 5b: Create Azure Web App (Alternative Backend)

If you prefer to host the API as a standard ASP.NET Core Web API instead of (or in addition to) Azure Functions, create an App Service.

### Portal

1. Click **Create a resource** → Search **Web App**
2. Click **Create**
3. On the **Basics** tab:
   - **Resource group:** `rg-refapp-dev`
   - **Name:** `web-refapp-dev` (must be globally unique)
   - **Publish:** Code
   - **Runtime stack:** .NET 10 (LTS)
   - **Operating System:** Linux
   - **Region:** Same as resource group
   - **Pricing Plan:** Basic B1 (recommended for dev) or Free F1 (limited)
4. Click **Review + create** → **Create**

#### Configure Application Settings

1. Go to your Web App resource
2. Click **Environment variables** in the left menu
3. Click **+ Add** and add these settings:

| Name                                   | Value                                      |
| -------------------------------------- | ------------------------------------------ |
| `ConnectionStrings__DefaultConnection` | Your SQL connection string from Step 3     |
| `Azure__SignalR__ConnectionString`     | Your SignalR connection string from Step 4 |

4. Click **Apply** → **Confirm**

### CLI

```bash
# Pattern: web-<workload>-<environment>
WEB_APP="web-${APP_NAME}-${ENVIRONMENT}"  # e.g., web-refapp-dev
PLAN_NAME="plan-${APP_NAME}-${ENVIRONMENT}"

# Create App Service Plan
az appservice plan create \
  --name $PLAN_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name $WEB_APP \
  --resource-group $RESOURCE_GROUP \
  --plan $PLAN_NAME \
  --runtime "DOTNETCORE:10.0"

# Configure app settings
az webapp config appsettings set \
  --name $WEB_APP \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$SQL_DB;Persist Security Info=False;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
    "Azure__SignalR__ConnectionString=$SIGNALR_CONN"
```

---

## Step 6: Create Azure Static Web App

The Static Web App hosts your React frontend with global CDN distribution.

### Portal

1. Click **Create a resource** → Search **Static Web App**
2. Click **Create**
3. On the **Basics** tab:
   - **Resource group:** `rg-refapp-dev`
   - **Name:** `stapp-refapp-dev`
   - **Plan type:** Free
   - **Region:** Choose closest to users
   - **Source:** GitHub
4. Click **Sign in with GitHub**
5. Authorize Azure to access your repositories
6. Select:
   - **Organization:** Your GitHub org or username
   - **Repository:** Your repository name
   - **Branch:** `main`
7. In **Build Details**:
   - **Build Presets:** Custom
   - **App location:** `/src/frontend`
   - **Api location:** Leave empty (we'll link Function App separately)
   - **Output location:** `dist`
8. Click **Review + create** → **Create**
9. Wait for deployment (~2 minutes)

### CLI

```bash
# Pattern: stapp-<workload>-<environment>
STATIC_WEB_APP="stapp-${APP_NAME}-${ENVIRONMENT}"  # e.g., stapp-refapp-dev
GITHUB_REPO="https://github.com/your-org/your-repo"  # Your repo URL

az staticwebapp create \
  --name $STATIC_WEB_APP \
  --resource-group $RESOURCE_GROUP \
  --location "eastus2" \
  --source $GITHUB_REPO \
  --branch "main" \
  --app-location "/src/frontend" \
  --output-location "dist" \
  --login-with-github
```

---

## Step 7: Create Application Insights (Optional)

Application Insights provides monitoring, logging, and performance metrics.

### Portal

1. Click **Create a resource** → Search **Application Insights**
2. Click **Create**
3. Fill in:
   - **Resource group:** `rg-refapp-dev`
   - **Name:** `appi-refapp-dev`
   - **Region:** Same as resource group
   - **Resource Mode:** Workspace-based
   - **Log Analytics Workspace:** Create new or select existing
4. Click **Review + create** → **Create**

#### Connect to Function App

1. Go to your Function App
2. Click **Application Insights** in the left menu
3. Click **Turn on Application Insights**
4. Select your `appi-refapp-dev` resource
5. Click **Apply**

### CLI

```bash
# Pattern: log-<workload>-<environment> for workspace, appi-<workload>-<environment> for insights
LOG_WORKSPACE="log-${APP_NAME}-${ENVIRONMENT}"  # e.g., log-refapp-dev
APP_INSIGHTS="appi-${APP_NAME}-${ENVIRONMENT}"  # e.g., appi-refapp-dev

# Create Log Analytics Workspace (required for Application Insights)
az monitor log-analytics workspace create \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LOG_WORKSPACE

# Create Application Insights
az monitor app-insights component create \
  --app $APP_INSIGHTS \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --workspace $LOG_WORKSPACE

# Get connection string
APP_INSIGHTS_CONN=$(az monitor app-insights component show \
  --app $APP_INSIGHTS \
  --resource-group $RESOURCE_GROUP \
  --query connectionString \
  --output tsv)

# Add to Function App
az functionapp config appsettings set \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=$APP_INSIGHTS_CONN"
```

---

## Step 8: Link Static Web App to Function App

This routes `/api/*` requests from your frontend to your Function App.

### Portal

1. Go to your Static Web App resource
2. Click **APIs** in the left menu (under Settings)
3. In the **Production** environment row, click **Link**
4. Fill in:
   - **Backend resource type:** Function App
   - **Subscription:** Your subscription
   - **Resource name:** Select `func-refapp-dev`
5. Click **Link**

### CLI

```bash
# Get Function App resource ID
FUNCTION_ID=$(az functionapp show \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --query id \
  --output tsv)

# Link to Static Web App
az staticwebapp backends link \
  --name $STATIC_WEB_APP \
  --resource-group $RESOURCE_GROUP \
  --backend-resource-id $FUNCTION_ID \
  --backend-region $LOCATION
```

**Important:** After linking, update your GitHub workflow file to set `api_location: ""` (empty string) so the Static Web App doesn't try to manage its own API.

---

## Step 9: Configure GitHub Secrets

Your GitHub Actions workflows need secrets to deploy to Azure.

### Prerequisites Checklist

Before configuring GitHub, gather these credentials from Azure Portal:

| Credential                          | Where to Get It                                                    | What It Looks Like                          |
| ----------------------------------- | ------------------------------------------------------------------ | ------------------------------------------- |
| **Function App Publish Profile**    | Azure Portal → Function App → Overview → Download publish profile  | XML file (~3KB)                             |
| **Static Web App Deployment Token** | Azure Portal → Static Web App → Overview → Manage deployment token | Long alphanumeric string                    |
| **Function App URL**                | Azure Portal → Function App → Overview → URL                       | `https://func-refapp-dev.azurewebsites.net` |

### Option A: Use the Helper Script (Recommended)

The easiest way to configure GitHub is using the provided PowerShell script:

```powershell
# Requires GitHub CLI: https://cli.github.com/
gh auth login

# Run the script (interactive mode)
.\scripts\setup-github-secrets.ps1

# Or with parameters
.\scripts\setup-github-secrets.ps1 `
  -FunctionAppName "func-refapp-dev" `
  -PublishProfilePath ".\func-refapp-dev.PublishSettings" `
  -StaticWebAppToken "your-token-here"
```

### Option B: Manual Configuration

#### Get Deployment Credentials

**Function App Publish Profile:**

1. Go to your Function App in Azure Portal
2. Click **Overview**
3. Click **Download publish profile** (top menu)
4. Save the downloaded `.PublishSettings` file

**Static Web App Deployment Token:**

1. Go to your Static Web App in Azure Portal
2. Click **Overview**
3. Click **Manage deployment token**
4. Copy the token

#### Add Secrets to GitHub

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** and add:

| Secret Name                         | Value                                   |
| ----------------------------------- | --------------------------------------- |
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | Entire contents of publish profile file |
| `AZURE_STATIC_WEB_APPS_API_TOKEN`   | Deployment token from Static Web App    |

#### Add Environment Variables

1. In the same GitHub settings, click **Variables** tab
2. Click **New repository variable** and add:

| Variable Name      | Value                                       |
| ------------------ | ------------------------------------------- |
| `VITE_API_URL`     | `https://func-refapp-dev.azurewebsites.net` |
| `VITE_SIGNALR_URL` | `https://func-refapp-dev.azurewebsites.net` |

---

## Verification Checklist

After completing all steps, verify everything is working:

### ☐ Resource Group

```bash
az group show --name rg-refapp-dev
```

### ☐ Function App Health

Visit: `https://func-refapp-dev.azurewebsites.net/api/health`

Expected response:

```json
{
  "status": "Healthy",
  "timestamp": "2025-...",
  "components": { ... }
}
```

### ☐ Static Web App

Visit: `https://stapp-refapp-dev.azurestaticapps.net`

Should show your React application.

### ☐ API Connection

From the frontend, the Notes feature should load without errors.

### ☐ SignalR (Optional)

If SignalR is configured:

1. Open app in two browser windows
2. Create a note in one window
3. It should appear in the other window automatically

### ☐ Application Insights (Optional)

1. Go to Application Insights in Azure Portal
2. Click **Live Metrics** to see real-time requests
3. Click **Failures** to check for any errors

---

## Cleanup

To delete all resources and stop billing:

### Portal

1. Go to your Resource Group (`rg-refapp-dev`)
2. Click **Delete resource group**
3. Type the resource group name to confirm
4. Click **Delete**

### CLI

```bash
az group delete --name rg-refapp-dev --yes --no-wait
```

---

## Troubleshooting

### "Cannot connect to SQL Server"

1. Check firewall rules allow Azure services
2. Verify connection string has correct password
3. Ensure the database exists

### "SignalR negotiation failed"

1. Verify SignalR is in **Serverless** mode
2. Check `AzureSignalRConnectionString` is set correctly
3. Ensure CORS allows your frontend URL

### "Function App returns 500"

1. Check Application Insights for detailed errors
2. Verify all app settings are configured
3. Check that migrations have been applied

### "Static Web App shows 404"

1. Verify `output_location` is `dist` in workflow
2. Check that `npm run build` succeeds
3. Ensure `app_location` is `/src/frontend`

### "API calls fail from frontend"

1. Verify the backend is linked in Static Web App → APIs
2. Check that `api_location` is empty (`""`) in workflow
3. Ensure Function App is running

---

## Cost Summary

| Resource             | Tier          | Monthly Cost |
| -------------------- | ------------- | ------------ |
| Storage Account      | Standard LRS  | ~$1-2        |
| Azure SQL            | Basic (5 DTU) | ~$5          |
| SignalR Service      | Free (F1)     | $0           |
| Function App         | Consumption   | ~$0-5        |
| Static Web App       | Free          | $0           |
| Application Insights | Pay-per-use   | ~$2-5        |

**Total: ~$8-17/month** for development/small production workloads

---

## Next Steps

### Step 10: Test Deployment

After configuring GitHub secrets, test the deployment:

#### Option A: Push to main branch

```bash
git add .
git commit -m "Configure deployment"
git push origin main
```

#### Option B: Manual workflow trigger

```bash
# Using GitHub CLI
gh workflow run deploy.yml

# Or via GitHub UI:
# Repository → Actions → Deploy to Azure → Run workflow
```

#### Monitor the deployment

```bash
# Watch workflow progress
gh run watch

# Or view in browser
gh run view --web
```

### Step 11: Verify Deployment

After the workflow completes:

1. **Check API health:** `https://func-refapp-dev.azurewebsites.net/api/health`
2. **Check frontend:** `https://<your-static-web-app>.azurestaticapps.net`
3. **Test Notes feature:** Create, edit, and delete notes
4. **Check SignalR:** Open two browser tabs and verify real-time sync

### Further Configuration

1. **Configure custom domain** - In Static Web App → Custom domains
2. **Enable HTTPS-only** - In Function App → Configuration → General settings
3. **Set up alerts** - In Application Insights → Alerts
4. **Review security** - See [SECURITY_HEADERS.md](guides/SECURITY_HEADERS.md)

---

## Sources

- [Azure Cloud Adoption Framework - Naming Conventions](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming)
- [Azure Resource Abbreviations](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations)
- [Azure Static Web Apps Documentation](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [Create Azure SQL Database](https://learn.microsoft.com/en-us/azure/azure-sql/database/single-database-create-quickstart)
- [Azure SignalR Service Quickstart](https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-quickstart-azure-functions-csharp)
- [Azure Functions Consumption Plan](https://learn.microsoft.com/en-us/azure/azure-functions/consumption-plan)
- [Application Insights for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-monitor/app/monitor-functions)
- [Bring Your Own Functions to Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/functions-bring-your-own)
