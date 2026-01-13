# GitHub Secrets Configuration

> **Version:** 1.0.0
> **Last Updated:** January 2026

This guide explains how to configure the required GitHub secrets for deploying Cadence to Azure.

---

## Table of Contents

1. [Overview](#overview)
2. [Required Secrets](#required-secrets)
3. [Setting Up AZURE_WEBAPP_PUBLISH_PROFILE](#setting-up-azure_webapp_publish_profile)
4. [Setting Up AZURE_SQL_CONNECTION_STRING](#setting-up-azure_sql_connection_string)
5. [Setting Up AZURE_STATIC_WEB_APPS_API_TOKEN](#setting-up-azure_static_web_apps_api_token-future)
6. [Environment Variables](#environment-variables)
7. [Verification](#verification)

---

## Overview

GitHub Actions workflows require secrets to securely connect to Azure resources. These secrets are stored encrypted and never exposed in logs.

### Security Best Practices

- Never commit secrets to the repository
- Use repository secrets, not organization secrets (unless shared across repos)
- Rotate credentials periodically
- Use the principle of least privilege

---

## Required Secrets

| Secret Name | Purpose | Required For |
|-------------|---------|--------------|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Deploy to Azure Web App | Backend deployment |
| `AZURE_SQL_CONNECTION_STRING` | Apply EF Core migrations | Backend deployment |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deploy to Static Web App | Frontend deployment (future) |

---

## Setting Up AZURE_WEBAPP_PUBLISH_PROFILE

The publish profile contains credentials for deploying to Azure App Service.

### Step 1: Download Publish Profile from Azure

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Web App (`app-cadence-api-uat`)
3. In the left menu, click **Overview**
4. In the top toolbar, click **Download publish profile**
5. Save the `.PublishSettings` file

### Step 2: Copy File Contents

1. Open the downloaded `.PublishSettings` file in a text editor
2. Select **all contents** (Ctrl+A)
3. Copy to clipboard (Ctrl+C)

The file looks like this (simplified):
```xml
<publishData>
  <publishProfile
    publishMethod="MSDeploy"
    publishUrl="app-cadence-api-uat.scm.azurewebsites.net:443"
    userName="$app-cadence-api-uat"
    userPWD="LONG_PASSWORD_HERE"
    ...
  </publishProfile>
</publishData>
```

### Step 3: Add to GitHub Secrets

1. Go to your GitHub repository
2. Click **Settings** tab
3. In the left sidebar, expand **Secrets and variables**
4. Click **Actions**
5. Click **New repository secret**
6. Enter:
   - **Name:** `AZURE_WEBAPP_PUBLISH_PROFILE`
   - **Secret:** Paste the entire XML content
7. Click **Add secret**

### Troubleshooting

**Error: "Invalid publish profile"**
- Ensure you copied the entire file including `<publishData>` tags
- Re-download the publish profile (they can expire)
- Check that no extra whitespace was added

---

## Setting Up AZURE_SQL_CONNECTION_STRING

The SQL connection string allows EF Core migrations to run during deployment.

### Step 1: Get Connection String from Azure

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your SQL Database (`sqldb-cadence-uat`)
3. In the left menu, click **Connection strings**
4. Copy the **ADO.NET (SQL authentication)** connection string

### Step 2: Replace Password Placeholder

The connection string contains `{your_password}`. Replace this with your actual SQL admin password.

**Before:**
```
Server=tcp:sql-cadence-uat.database.windows.net,1433;Initial Catalog=sqldb-cadence-uat;Persist Security Info=False;User ID=sqladmin;Password={your_password};...
```

**After:**
```
Server=tcp:sql-cadence-uat.database.windows.net,1433;Initial Catalog=sqldb-cadence-uat;Persist Security Info=False;User ID=sqladmin;Password=YourActualPassword123!;...
```

### Step 3: Add to GitHub Secrets

1. Go to your GitHub repository
2. Click **Settings** > **Secrets and variables** > **Actions**
3. Click **New repository secret**
4. Enter:
   - **Name:** `AZURE_SQL_CONNECTION_STRING`
   - **Secret:** Paste the complete connection string with password
5. Click **Add secret**

### Connection String Format

Full connection string format:
```
Server=tcp:sql-cadence-uat.database.windows.net,1433;Initial Catalog=sqldb-cadence-uat;Persist Security Info=False;User ID=sqladmin;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### Troubleshooting

**Error: "Cannot open server requested by the login"**
- Verify the server name matches your Azure SQL Server
- Check firewall rules allow Azure services
- Ensure the database name is correct

**Error: "Login failed for user"**
- Verify username matches SQL admin login
- Verify password is correct (no typos)
- Check if password contains special characters that need escaping

---

## Setting Up AZURE_STATIC_WEB_APPS_API_TOKEN (Future)

This secret is used for deploying the React frontend to Azure Static Web Apps.

**Note:** This is for future frontend deployment. The token is not required until Static Web App is provisioned.

### Step 1: Get Deployment Token from Azure

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Static Web App
3. In the left menu, click **Overview**
4. Click **Manage deployment token**
5. Copy the token

### Step 2: Add to GitHub Secrets

1. Go to your GitHub repository
2. Click **Settings** > **Secrets and variables** > **Actions**
3. Click **New repository secret**
4. Enter:
   - **Name:** `AZURE_STATIC_WEB_APPS_API_TOKEN`
   - **Secret:** Paste the deployment token
5. Click **Add secret**

---

## Environment Variables

In addition to secrets, you may need to configure environment variables for the frontend build.

### Repository Variables (Non-Sensitive)

1. Go to **Settings** > **Secrets and variables** > **Actions**
2. Click the **Variables** tab
3. Click **New repository variable**

| Variable Name | Value | Description |
|---------------|-------|-------------|
| `VITE_API_URL` | `https://app-cadence-api-uat.azurewebsites.net` | Backend API URL |
| `VITE_SIGNALR_URL` | `https://app-cadence-api-uat.azurewebsites.net` | SignalR hub URL |

### Environment-Specific Variables

For different environments (UAT vs Production), use GitHub Environments:

1. Go to **Settings** > **Environments**
2. Click **New environment**
3. Name it `uat` or `production`
4. Add environment-specific variables

---

## Verification

### Check Secrets Are Configured

1. Go to **Settings** > **Secrets and variables** > **Actions**
2. Verify these secrets appear in the list:
   - `AZURE_WEBAPP_PUBLISH_PROFILE`
   - `AZURE_SQL_CONNECTION_STRING`

### Test Deployment

1. Create a small change in the codebase
2. Push to main branch or trigger workflow manually
3. Go to **Actions** tab
4. Watch the workflow run
5. Verify all steps complete successfully

### Manual Workflow Trigger

To test without making code changes:

1. Go to **Actions** tab
2. Select **Deploy Backend** workflow
3. Click **Run workflow**
4. Select branch and environment
5. Click **Run workflow**

---

## Secret Rotation

Secrets should be rotated periodically for security.

### Rotating Publish Profile

1. In Azure Portal, go to Web App
2. Click **Reset publish profile** (in Overview toolbar)
3. Download new publish profile
4. Update GitHub secret with new content

### Rotating SQL Password

1. In Azure Portal, go to SQL Server
2. Click **Reset password** in Settings > SQL server admin
3. Set new password
4. Update GitHub secret with new connection string
5. Update Web App configuration with new connection string

---

## Troubleshooting

### "Secret not found" in workflow

- Check secret name matches exactly (case-sensitive)
- Verify secret is at repository level, not organization level
- Check if workflow has permission to access secrets

### Workflow fails silently

- Check workflow logs for masked values `***`
- Masked output indicates secret was read correctly
- If not masked, secret may not be configured

### "XML parsing error" for publish profile

- Ensure entire XML file was copied
- Check for hidden characters or encoding issues
- Try re-downloading from Azure Portal

---

## Next Steps

1. **Deploy Backend** - Push to main or trigger workflow manually
2. **Verify Deployment** - Check health endpoint
3. **Run Checklist** - See [DEPLOYMENT_CHECKLIST.md](./DEPLOYMENT_CHECKLIST.md)
