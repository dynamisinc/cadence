# Deployment Checklist

> **Version:** 1.0.0
> **Last Updated:** January 2026

Use this checklist before and after deploying Cadence to Azure.

---

## Table of Contents

1. [Pre-Deployment Checklist](#pre-deployment-checklist)
2. [Backend Deployment Verification](#backend-deployment-verification)
3. [Frontend Deployment Verification](#frontend-deployment-verification)
4. [Post-Deployment Checklist](#post-deployment-checklist)
5. [Rollback Procedures](#rollback-procedures)

---

## Pre-Deployment Checklist

Complete these checks before triggering a deployment.

### Code Quality

- [ ] All tests pass locally (`dotnet test`)
- [ ] Frontend tests pass (`npm run test:run`)
- [ ] No linting errors (`npm run lint`)
- [ ] TypeScript compiles without errors (`npm run type-check`)
- [ ] Code has been reviewed (if applicable)

### CI Pipeline

- [ ] CI workflow passed on the branch/commit being deployed
- [ ] No failing security scans
- [ ] Build artifacts generated successfully

### Azure Resources

- [ ] Resource Group exists (`rg-cadence-uat-centralus`)
- [ ] App Service is running (not stopped)
- [ ] SQL Database is accessible
- [ ] Application Insights is configured

### GitHub Secrets

- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE` is configured
- [ ] `AZURE_SQL_CONNECTION_STRING` is configured
- [ ] Secrets are not expired (publish profiles can expire)

### Database Migrations

- [ ] Review pending migrations (`dotnet ef migrations list`)
- [ ] Migrations are safe to run (no data loss)
- [ ] Database backup taken (for production)

### Environment Configuration

- [ ] `appsettings.json` has correct production settings
- [ ] Connection strings do not contain localhost
- [ ] CORS settings include production URLs
- [ ] Logging level is appropriate (Information, not Debug)

---

## Backend Deployment Verification

After backend deployment completes, verify these endpoints and features.

### Health Endpoint

```bash
# Check health endpoint
curl https://app-cadence-api-uat.azurewebsites.net/api/health
```

Expected response:
```json
{
  "status": "Healthy",
  "timestamp": "2026-01-12T12:00:00Z"
}
```

- [ ] Health endpoint returns 200 OK
- [ ] Response includes healthy status
- [ ] Response time is under 2 seconds

### API Endpoints

Test core API functionality:

```bash
# Get exercises (should return empty array or existing data)
curl https://app-cadence-api-uat.azurewebsites.net/api/exercises

# Check API documentation (Scalar)
curl https://app-cadence-api-uat.azurewebsites.net/scalar
```

- [ ] API endpoints return valid responses
- [ ] API documentation loads (Scalar/OpenAPI)
- [ ] Authentication works (if implemented)

### Database Connectivity

- [ ] Application can connect to database
- [ ] Migrations applied successfully (check deploy logs)
- [ ] No connection timeout errors in logs

### Application Insights

1. Go to Azure Portal > Application Insights > `appi-cadence-uat`
2. Check **Live Metrics** for real-time traffic
3. Check **Failures** for any errors

- [ ] Requests are being logged
- [ ] No critical errors in Failures blade
- [ ] Performance metrics are acceptable

### SignalR Hub (If Applicable)

```bash
# SignalR negotiate endpoint
curl -X POST https://app-cadence-api-uat.azurewebsites.net/hubs/exercise/negotiate
```

- [ ] SignalR hub responds to negotiate requests
- [ ] Real-time connections can be established

---

## Frontend Deployment Verification

After frontend deployment completes (Static Web App).

### Static Web App

```bash
# Check frontend loads
curl -I https://<static-web-app-name>.azurestaticapps.net
```

- [ ] Frontend loads without errors
- [ ] Static assets (CSS, JS) load correctly
- [ ] No 404 errors for routes

### API Integration

- [ ] Frontend can reach backend API
- [ ] CORS errors are not occurring
- [ ] API calls return expected data

### Browser Console

Open browser DevTools (F12) and check:

- [ ] No JavaScript errors in console
- [ ] No failed network requests
- [ ] SignalR connection established (if applicable)

### Cross-Browser Testing

- [ ] Chrome: Application loads and functions
- [ ] Firefox: Application loads and functions
- [ ] Edge: Application loads and functions
- [ ] Safari: Application loads and functions (if Mac available)

### Responsive Design

- [ ] Desktop viewport works correctly
- [ ] Tablet viewport works correctly
- [ ] Mobile viewport works correctly

---

## Post-Deployment Checklist

Complete these checks after successful deployment.

### Monitoring

- [ ] Set up alerts in Application Insights for:
  - [ ] Failed requests > 5% threshold
  - [ ] Response time > 2 second average
  - [ ] Exception count > 10 per hour

### Documentation

- [ ] Update deployment notes/changelog
- [ ] Document any manual steps performed
- [ ] Note any issues encountered

### Communication

- [ ] Notify stakeholders of deployment completion
- [ ] Update status page (if applicable)
- [ ] Close related tickets/issues

### Cleanup

- [ ] Delete any temporary resources
- [ ] Remove debug logging if added
- [ ] Archive old deployment artifacts

---

## Rollback Procedures

If deployment fails or causes issues, follow these rollback steps.

### Backend Rollback

#### Option 1: Redeploy Previous Version (GitHub Actions)

1. Go to **Actions** tab in GitHub
2. Find the last successful deployment workflow run
3. Click on it and select **Re-run all jobs**

#### Option 2: Azure Portal Rollback

1. Go to Azure Portal > Web App > `app-cadence-api-uat`
2. In left menu, click **Deployment Center**
3. Click on a previous deployment
4. Click **Redeploy**

#### Option 3: Deployment Slots (If Configured)

1. Go to Azure Portal > Web App
2. Click **Deployment slots**
3. Click **Swap** to swap back to previous slot

### Database Rollback

**Warning:** Database rollbacks can cause data loss. Always backup first.

#### Rollback Migration

```bash
# List migrations
dotnet ef migrations list

# Revert to specific migration
dotnet ef database update <PreviousMigrationName>
```

#### Point-in-Time Restore (Azure SQL)

1. Go to Azure Portal > SQL Database
2. Click **Restore**
3. Select point-in-time before the issue
4. Create new database with restored data
5. Update connection string to new database

### Frontend Rollback

#### Azure Static Web Apps

1. Go to Azure Portal > Static Web App
2. Click **Environments** in left menu
3. Previous deployments are listed
4. Click **Browse** on previous deployment to verify
5. Click **Promote** to make it production

---

## Emergency Contacts

| Role | Contact | When to Contact |
|------|---------|-----------------|
| DevOps Lead | [TBD] | Deployment failures |
| Database Admin | [TBD] | Database issues |
| Azure Admin | [TBD] | Resource/access issues |

---

## Deployment Log Template

Use this template to document each deployment:

```
## Deployment: [Date] [Time]

### Environment
- Target: UAT / Production
- Triggered by: [Name/Auto]
- Commit: [SHA]
- Branch: main

### Pre-Deployment
- [ ] Tests passed
- [ ] Code reviewed
- [ ] Secrets verified

### Deployment
- Start time: [HH:MM]
- End time: [HH:MM]
- Duration: [X minutes]
- Status: Success / Failed / Rolled Back

### Post-Deployment
- [ ] Health check passed
- [ ] API tested
- [ ] Frontend verified

### Issues Encountered
[None / Description of issues]

### Notes
[Any additional notes]
```

---

## Quick Reference

### Useful URLs

| Resource | URL |
|----------|-----|
| Backend API | `https://app-cadence-api-uat.azurewebsites.net` |
| Health Check | `https://app-cadence-api-uat.azurewebsites.net/api/health` |
| API Docs | `https://app-cadence-api-uat.azurewebsites.net/scalar` |
| Azure Portal | `https://portal.azure.com` |
| GitHub Actions | `https://github.com/[org]/cadence/actions` |

### Useful Commands

```bash
# Check deployment status
gh run list --workflow=deploy-backend.yml

# View deployment logs
gh run view [run-id] --log

# Trigger manual deployment
gh workflow run deploy-backend.yml

# Check Azure Web App status
az webapp show --name app-cadence-api-uat --resource-group rg-cadence-uat-centralus --query state
```
