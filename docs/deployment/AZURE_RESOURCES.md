# Azure Resources Setup Guide

> **Version:** 1.0.0
> **Last Updated:** January 2026
> **Estimated Time:** 30-45 minutes
> **Estimated Monthly Cost:** ~$20 for UAT environment

This guide provides step-by-step Azure Portal instructions for creating all resources needed to deploy Cadence to a UAT environment.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Resource Overview](#resource-overview)
3. [Naming Convention](#naming-convention)
4. [Step 1: Create Resource Group](#step-1-create-resource-group)
5. [Step 2: Create Log Analytics Workspace](#step-2-create-log-analytics-workspace)
6. [Step 3: Create Application Insights](#step-3-create-application-insights)
7. [Step 4: Create App Service Plan](#step-4-create-app-service-plan)
8. [Step 5: Create Web App](#step-5-create-web-app)
9. [Step 6: Create SQL Server](#step-6-create-sql-server)
10. [Step 7: Create SQL Database](#step-7-create-sql-database)
11. [Cost Summary](#cost-summary)
12. [Future: Azure DevOps Migration](#future-azure-devops-migration)

---

## Prerequisites

1. **Azure Account** with an active subscription
2. **Permissions** to create resources in the subscription
3. **Web Browser** - All steps use Azure Portal

---

## Resource Overview

```
+----------------------------------------------------------+
|              rg-cadence-uat-centralus                     |
|              (Resource Group)                             |
+----------------------------------------------------------+
|                                                          |
|  +-------------------+    +------------------------+     |
|  | log-cadence-uat   |    | appi-cadence-uat       |     |
|  | (Log Analytics)   |<---| (Application Insights) |     |
|  +-------------------+    +------------------------+     |
|                                  |                       |
|  +-------------------+           |                       |
|  | asp-cadence-uat   |           v                       |
|  | (App Service Plan)|    +------------------------+     |
|  | B1 tier           |    | app-cadence-api-uat    |     |
|  +--------+----------+    | (Web App)              |     |
|           |               | ASP.NET Core 10        |     |
|           +-------------->| REST API + SignalR     |     |
|                           +------------+-----------+     |
|                                        |                 |
|  +-------------------+                 |                 |
|  | sql-cadence-uat   |                 v                 |
|  | (SQL Server)      |    +------------------------+     |
|  +--------+----------+    | sqldb-cadence-uat      |     |
|           |               | (SQL Database)         |     |
|           +-------------->| Basic 5 DTU            |     |
|                           +------------------------+     |
|                                                          |
+----------------------------------------------------------+
```

---

## Naming Convention

Following [Microsoft Cloud Adoption Framework](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming):

```
<resource-type-prefix>-<workload>-<environment>[-<region>]
```

| Resource Type        | Prefix | UAT Name                    |
|---------------------|--------|------------------------------|
| Resource Group      | rg     | rg-cadence-uat-centralus     |
| Log Analytics       | log    | log-cadence-uat              |
| Application Insights| appi   | appi-cadence-uat             |
| App Service Plan    | asp    | asp-cadence-uat              |
| Web App             | app    | app-cadence-api-uat          |
| SQL Server          | sql    | sql-cadence-uat              |
| SQL Database        | sqldb  | sqldb-cadence-uat            |

---

## Step 1: Create Resource Group

The Resource Group is a container for all related Azure resources.

### Portal Navigation

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** (+ icon in top-left corner)
3. Search for **Resource group**
4. Click **Create**

### Configuration

| Field | Value |
|-------|-------|
| Subscription | Your subscription |
| Resource group | `rg-cadence-uat-centralus` |
| Region | `Central US` (or closest to your users) |

### Steps

1. Fill in the fields above
2. Click **Review + create**
3. Click **Create**

**Time:** ~30 seconds

---

## Step 2: Create Log Analytics Workspace

Log Analytics stores logs and metrics from Application Insights.

### Portal Navigation

1. Click **Create a resource**
2. Search for **Log Analytics workspace**
3. Click **Create**

### Configuration

| Field | Value |
|-------|-------|
| Subscription | Your subscription |
| Resource group | `rg-cadence-uat-centralus` |
| Name | `log-cadence-uat` |
| Region | `Central US` |

### Steps

1. Fill in the fields above
2. Click **Review + create**
3. Click **Create**

**Time:** ~1 minute

---

## Step 3: Create Application Insights

Application Insights provides monitoring, logging, and performance metrics.

### Portal Navigation

1. Click **Create a resource**
2. Search for **Application Insights**
3. Click **Create**

### Configuration

| Field | Value |
|-------|-------|
| Subscription | Your subscription |
| Resource group | `rg-cadence-uat-centralus` |
| Name | `appi-cadence-uat` |
| Region | `Central US` |
| Resource Mode | Workspace-based |
| Log Analytics Workspace | `log-cadence-uat` |

### Steps

1. Fill in the fields above
2. Select the Log Analytics workspace created in Step 2
3. Click **Review + create**
4. Click **Create**

**Time:** ~1 minute

### Get Connection String (Save for Later)

1. After creation, go to the resource
2. Click **Overview** in the left menu
3. Copy the **Connection String** (starts with `InstrumentationKey=...`)
4. Save this - you will need it for the Web App configuration

---

## Step 4: Create App Service Plan

The App Service Plan defines the compute resources for your Web App.

### Portal Navigation

1. Click **Create a resource**
2. Search for **App Service Plan**
3. Click **Create**

### Configuration

| Field | Value |
|-------|-------|
| Subscription | Your subscription |
| Resource group | `rg-cadence-uat-centralus` |
| Name | `asp-cadence-uat` |
| Operating System | Linux |
| Region | `Central US` |
| Pricing plan | Basic B1 |

### Why B1 Tier?

| Feature | B1 | Free/Shared | Premium |
|---------|-----|-------------|---------|
| Always On | Yes | No | Yes |
| Custom domains | Yes | Limited | Yes |
| SSL | Yes | No | Yes |
| Monthly cost | ~$13 | $0 | ~$100+ |

**B1 is recommended** because:
- Exercise conduct requires instant responses (no cold starts)
- SignalR persistent connections need Always On
- SSL/custom domains are supported
- Cost-effective for UAT/small production

### Steps

1. Fill in the fields above
2. Select **Linux** for Operating System
3. Select **Basic B1** pricing tier
4. Click **Review + create**
5. Click **Create**

**Time:** ~1 minute

---

## Step 5: Create Web App

The Web App hosts the ASP.NET Core REST API and SignalR hub.

### Portal Navigation

1. Click **Create a resource**
2. Search for **Web App**
3. Click **Create**

### Configuration - Basics Tab

| Field | Value |
|-------|-------|
| Subscription | Your subscription |
| Resource group | `rg-cadence-uat-centralus` |
| Name | `app-cadence-api-uat` |
| Publish | Code |
| Runtime stack | .NET 10 |
| Operating System | Linux |
| Region | `Central US` |
| App Service Plan | `asp-cadence-uat` |

### Configuration - Deployment Tab

| Field | Value |
|-------|-------|
| Continuous deployment | Disable (we use GitHub Actions) |

### Configuration - Monitoring Tab

| Field | Value |
|-------|-------|
| Enable Application Insights | Yes |
| Application Insights | `appi-cadence-uat` |

### Steps

1. Fill in the Basics tab
2. Click **Next: Deployment** - Disable continuous deployment
3. Click **Next: Networking** - Use defaults
4. Click **Next: Monitoring** - Enable Application Insights
5. Click **Review + create**
6. Click **Create**

**Time:** ~2 minutes

### Get Publish Profile (Save for GitHub)

1. After creation, go to the resource
2. Click **Overview** in the left menu
3. Click **Download publish profile** (top toolbar)
4. Save the downloaded `.PublishSettings` file
5. This file contents will be used as a GitHub secret

---

## Step 6: Create SQL Server

The SQL Server hosts your database. This is the logical server (not the database itself).

### Portal Navigation

1. Click **Create a resource**
2. Search for **SQL Server**
3. Select **SQL server (logical server)**
4. Click **Create**

### Configuration - Basics Tab

| Field | Value |
|-------|-------|
| Subscription | Your subscription |
| Resource group | `rg-cadence-uat-centralus` |
| Server name | `sql-cadence-uat` |
| Location | `Central US` |
| Authentication method | Use SQL authentication |
| Server admin login | `sqladmin` (or your preferred username) |
| Password | Strong password (save this!) |

**Important:** Save the admin username and password securely. You will need these for the connection string.

### Configuration - Networking Tab

| Field | Value |
|-------|-------|
| Allow Azure services | Yes (check the box) |
| Add current client IP | Yes (for local development) |

### Steps

1. Fill in the Basics tab
2. Click **Next: Networking**
3. Check **Allow Azure services and resources to access this server**
4. Check **Add current client IP address**
5. Click **Review + create**
6. Click **Create**

**Time:** ~2 minutes

---

## Step 7: Create SQL Database

The SQL Database stores all application data.

### Portal Navigation

1. Click **Create a resource**
2. Search for **SQL Database**
3. Click **Create**

### Configuration - Basics Tab

| Field | Value |
|-------|-------|
| Subscription | Your subscription |
| Resource group | `rg-cadence-uat-centralus` |
| Database name | `sqldb-cadence-uat` |
| Server | `sql-cadence-uat` (select existing) |
| Want to use SQL elastic pool? | No |
| Workload environment | Development |
| Compute + storage | Basic (5 DTU) |

### Selecting Basic Tier

1. Click **Configure database** under Compute + storage
2. Select **Basic** tier
3. Confirm settings:
   - DTUs: 5
   - Storage: 2 GB
   - Monthly cost: ~$5
4. Click **Apply**

### Configuration - Networking Tab

| Field | Value |
|-------|-------|
| Connectivity method | Public endpoint |
| Allow Azure services | Yes |

### Steps

1. Fill in the Basics tab
2. Select the SQL Server created in Step 6
3. Click **Configure database** and select **Basic**
4. Click **Next: Networking** - Use defaults
5. Click **Review + create**
6. Click **Create**

**Time:** ~3 minutes

### Get Connection String (Save for GitHub)

1. After creation, go to the database resource
2. Click **Connection strings** in the left menu
3. Copy the **ADO.NET (SQL authentication)** connection string
4. Replace `{your_password}` with your actual password
5. This will be used as a GitHub secret

**Example connection string format:**
```
Server=tcp:sql-cadence-uat.database.windows.net,1433;Initial Catalog=sqldb-cadence-uat;Persist Security Info=False;User ID=sqladmin;Password=YOUR_PASSWORD_HERE;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

---

## Cost Summary

| Resource | SKU | Monthly Cost (Est.) |
|----------|-----|---------------------|
| Resource Group | N/A | $0 |
| Log Analytics | Pay-per-GB | ~$0-2 |
| Application Insights | Pay-per-GB | ~$0-2 |
| App Service Plan | B1 | ~$13 |
| Web App | (included in plan) | $0 |
| SQL Server | (logical) | $0 |
| SQL Database | Basic 5 DTU | ~$5 |
| **Total** | | **~$18-22/month** |

### Cost Optimization Tips

1. **Dev/Test Pricing:** If eligible, apply Azure Dev/Test subscription pricing
2. **Reserved Capacity:** For production, consider 1-year reserved capacity for SQL
3. **Auto-pause:** For dev environments, consider Serverless SQL tier with auto-pause
4. **Scale Down:** Use Free tier App Service for pure development (no custom domain/SSL)

---

## Future: Azure DevOps Migration

This project currently uses GitHub Actions for CI/CD. A future migration to Azure DevOps pipelines is planned for:

- **Azure Pipelines** for build and release
- **Azure Repos** (optional) for source control
- **Azure Boards** for work item tracking

### Migration Considerations

1. **Service Connections:** Will need Azure Resource Manager service connection
2. **Variable Groups:** Secrets will move from GitHub Secrets to Azure DevOps variable groups
3. **Pipeline YAML:** GitHub Actions workflows will be converted to Azure Pipelines YAML
4. **Environments:** Azure DevOps environments will replace GitHub environments

### Timeline

Migration is not scheduled. Current GitHub Actions workflows are production-ready.

---

## Next Steps

After creating all resources:

1. **Configure GitHub Secrets** - See [GITHUB_SECRETS.md](./GITHUB_SECRETS.md)
2. **Review Environment Config** - See [ENVIRONMENT_CONFIG.md](./ENVIRONMENT_CONFIG.md)
3. **Run Deployment Checklist** - See [DEPLOYMENT_CHECKLIST.md](./DEPLOYMENT_CHECKLIST.md)

---

## Troubleshooting

### "Server not found" when creating database

- Ensure SQL Server deployment completed successfully
- Refresh the server dropdown list
- Verify you are in the same subscription

### "Insufficient permissions"

- Verify you have Contributor role on the subscription
- Check with your Azure administrator for required permissions

### "Name already taken"

- Azure resource names must be globally unique
- Try adding a suffix (e.g., `sql-cadence-uat-001`)
- For storage accounts, use only lowercase letters and numbers

---

## References

- [Azure Cloud Adoption Framework - Naming](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming)
- [App Service Pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/linux/)
- [Azure SQL Database Pricing](https://azure.microsoft.com/en-us/pricing/details/azure-sql-database/single/)
- [Application Insights Pricing](https://azure.microsoft.com/en-us/pricing/details/monitor/)
