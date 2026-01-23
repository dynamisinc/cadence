# Environment Configuration Guide

> **Version:** 1.0.0
> **Last Updated:** January 2026

This guide covers configuration for local development versus Azure deployment environments.

---

## Table of Contents

1. [Overview](#overview)
2. [Local Development Configuration](#local-development-configuration)
3. [Azure Configuration](#azure-configuration)
4. [Connection String Formats](#connection-string-formats)
5. [appsettings Patterns](#appsettings-patterns)
6. [Authentication Cookie Configuration](#authentication-cookie-configuration)
7. [Frontend Environment Variables](#frontend-environment-variables)
8. [Secrets Management](#secrets-management)

---

## Overview

Cadence supports multiple environments with different configuration sources:

| Environment | Database | Config Source | Secrets |
|------------|----------|---------------|---------|
| Local Development | LocalDB or SQL Server | appsettings.Local.json | User Secrets |
| CI/Test | In-Memory or SQLite | appsettings.json | GitHub Secrets |
| UAT | Azure SQL | App Service Config | Azure + GitHub |
| Production | Azure SQL | App Service Config | Azure Key Vault |

---

## Local Development Configuration

### Database Options

#### Option 1: SQL Server LocalDB (Recommended for Windows)

LocalDB is included with Visual Studio and SQL Server Express.

**Connection String:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CadenceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

**Verify LocalDB is available:**
```powershell
sqllocaldb info
```

**Create LocalDB instance if needed:**
```powershell
sqllocaldb create mssqllocaldb
sqllocaldb start mssqllocaldb
```

#### Option 2: SQL Server Express

For a full SQL Server installation.

**Connection String:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=CadenceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

#### Option 3: SQL Server in Docker

For cross-platform development or consistent environments.

**Start SQL Server container:**
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name cadence-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

**Connection String:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=CadenceDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  }
}
```

### appsettings.Local.json

Create `src/Cadence.WebApi/appsettings.Local.json` for local overrides:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CadenceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
  }
}
```

**Important:** Add `appsettings.Local.json` to `.gitignore` to prevent committing local settings.

### User Secrets (Alternative)

For sensitive local configuration, use .NET User Secrets:

```bash
cd src/Cadence.WebApi

# Initialize user secrets
dotnet user-secrets init

# Set connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=CadenceDb;Trusted_Connection=True;TrustServerCertificate=True"
```

### Apply Migrations Locally

```bash
cd src/Cadence.WebApi

# Apply pending migrations
dotnet ef database update --project ../Cadence.Core/Cadence.Core.csproj

# Or create database from scratch
dotnet ef database update --project ../Cadence.Core/Cadence.Core.csproj
```

---

## Azure Configuration

### App Service Configuration

Azure App Service configuration is managed through:
1. **Azure Portal** - Configuration blade
2. **GitHub Actions** - During deployment
3. **Azure CLI** - For automation

### Required App Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Database connection | `Server=tcp:sql-cadence-uat...` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights | `InstrumentationKey=...` |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |

### Configure via Azure Portal

1. Go to Azure Portal > App Service > `app-cadence-api-uat`
2. Click **Configuration** in left menu
3. Click **Application settings** tab
4. Click **+ New application setting**
5. Add settings (name/value pairs)
6. Click **Save**

### Configure via Azure CLI

```bash
# Set single setting
az webapp config appsettings set \
  --name app-cadence-api-uat \
  --resource-group rg-cadence-uat-centralus \
  --settings ASPNETCORE_ENVIRONMENT=Production

# Set connection string
az webapp config connection-string set \
  --name app-cadence-api-uat \
  --resource-group rg-cadence-uat-centralus \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=tcp:sql-cadence-uat.database.windows.net..."
```

---

## Connection String Formats

### LocalDB (Development)

```
Server=(localdb)\mssqllocaldb;Database=CadenceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

### SQL Server Express (Development)

```
Server=localhost\SQLEXPRESS;Database=CadenceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

### SQL Server Docker (Development)

```
Server=localhost,1433;Database=CadenceDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True
```

### Azure SQL (UAT/Production)

```
Server=tcp:sql-cadence-uat.database.windows.net,1433;Initial Catalog=sqldb-cadence-uat;Persist Security Info=False;User ID=sqladmin;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### Connection String Components

| Component | LocalDB | Azure SQL |
|-----------|---------|-----------|
| Server | `(localdb)\mssqllocaldb` | `tcp:server.database.windows.net,1433` |
| Database | `CadenceDb` | `sqldb-cadence-uat` |
| Authentication | `Trusted_Connection=True` | `User ID=x;Password=y` |
| Encryption | Optional | `Encrypt=True` (required) |
| Certificate | `TrustServerCertificate=True` | `TrustServerCertificate=False` |

---

## appsettings Patterns

### Configuration Hierarchy

ASP.NET Core loads configuration in this order (later sources override earlier):

1. `appsettings.json` - Base configuration
2. `appsettings.{Environment}.json` - Environment-specific
3. `appsettings.Local.json` - Local overrides (gitignored)
4. Environment variables - Azure App Service settings
5. User Secrets - Development secrets (dev only)
6. Command line - Runtime arguments

### appsettings.json (Base)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "DetailedErrors": true
}
```

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

---

## Authentication Cookie Configuration

The refresh token is stored in an HttpOnly cookie. Cookie settings are **environment-aware** to handle both local development and production cross-origin deployments.

### How It Works

| Environment                | Secure  | SameSite | Why                               |
|----------------------------|---------|----------|-----------------------------------|
| Development                | `false` | `Lax`    | Works with HTTP localhost         |
| Production (same-origin)   | `true`  | `Strict` | Maximum security                  |
| Production (cross-origin)  | `true`  | `None`   | Required for cross-origin cookies |

### SWA + App Service Deployment (Cross-Origin)

When deploying with **Azure Static Web App** (frontend) and **Azure App Service** (API) on **different domains**, you have three options:

#### Option 1: SWA Backend Proxy (Recommended)

Azure SWA can proxy `/api/*` requests to your App Service, making them appear same-origin. This is the most secure option.

Add to `staticwebapp.config.json` in your frontend:

```json
{
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous", "authenticated"]
    }
  ],
  "navigationFallback": {
    "rewrite": "/index.html"
  },
  "backend": {
    "baseUrl": "https://your-app-service.azurewebsites.net"
  }
}
```

With this configuration:
- Browser sees all requests as same-origin
- Cookies work with `SameSite=Strict` (default)
- No additional configuration needed
- Frontend `VITE_API_URL` should be empty (relative `/api` path)

#### Option 2: SameSite=None (Cross-Origin Cookies)

If you cannot use the SWA proxy, configure the API to allow cross-origin cookies.

Add to `appsettings.Production.json`:

```json
{
  "Authentication": {
    "Cookie": {
      "SameSite": "None"
    }
  }
}
```

Or set via Azure App Service Configuration:
```
Authentication__Cookie__SameSite = None
```

**Important considerations:**
- `SameSite=None` requires `Secure=true` (HTTPS) - this is enforced automatically
- Slightly reduced security: cookies sent on all cross-origin requests
- Ensure your CORS policy is properly configured

#### Option 3: Custom Domain (Same Root Domain)

Put both SWA and App Service under the same root domain:
- `app.cadence.io` → Azure SWA
- `api.cadence.io` → Azure App Service

With this setup, use `SameSite=Lax`:

```json
{
  "Authentication": {
    "Cookie": {
      "SameSite": "Lax"
    }
  }
}
```

### Configuration Reference

| Setting                          | Values                  | Default                      |
|----------------------------------|-------------------------|------------------------------|
| `Authentication:Cookie:SameSite` | `Strict`, `Lax`, `None` | `Lax` (dev), `Strict` (prod) |

### Troubleshooting

**Symptom:** Token refresh fails silently, user gets logged out unexpectedly.

**Check:**
1. Open browser DevTools → Application → Cookies
2. Look for `refreshToken` cookie
3. If missing or has warnings, check:
   - `Secure` flag matches HTTPS status
   - `SameSite` setting matches your deployment topology

**Development on HTTP:** If running locally without HTTPS, cookies work automatically (development mode uses `Secure=false`).

**Production cross-origin:** If SWA and App Service are on different domains, you MUST either:
- Use SWA backend proxy (recommended), OR
- Set `Authentication:Cookie:SameSite` to `None`

---

### Program.cs Configuration Loading

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add local settings file (optional, for development)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Connection string loaded automatically from configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## Frontend Environment Variables

### Vite Environment Files

| File | Purpose | Committed |
|------|---------|-----------|
| `.env` | Default values | Yes |
| `.env.local` | Local overrides | No (gitignored) |
| `.env.development` | Development mode | Yes |
| `.env.production` | Production mode | Yes |

### .env.example

```bash
# Copy to .env.local for local development
VITE_API_URL=http://localhost:5062
VITE_SIGNALR_URL=http://localhost:5062
```

### .env.development

```bash
VITE_API_URL=http://localhost:5062
VITE_SIGNALR_URL=http://localhost:5062
```

### .env.production

```bash
# These are overridden by GitHub Actions during build
VITE_API_URL=
VITE_SIGNALR_URL=
```

### Accessing Environment Variables in Code

```typescript
// src/core/services/api.ts
const API_URL = import.meta.env.VITE_API_URL || '';

export const apiClient = axios.create({
  baseURL: API_URL,
});
```

### Build-Time Variables

Environment variables prefixed with `VITE_` are embedded at build time:

```bash
# Build with production API URL
VITE_API_URL=https://app-cadence-api-uat.azurewebsites.net npm run build
```

---

## Secrets Management

### Development Secrets

Use .NET User Secrets for local development:

```bash
# Initialize
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
dotnet user-secrets set "ExternalApi:ApiKey" "your-api-key"

# List secrets
dotnet user-secrets list

# Remove secrets
dotnet user-secrets remove "ExternalApi:ApiKey"
```

### GitHub Secrets

For CI/CD pipelines. See [GITHUB_SECRETS.md](./GITHUB_SECRETS.md).

### Azure Key Vault (Production)

For production environments, use Azure Key Vault:

```csharp
// Program.cs
if (builder.Environment.IsProduction())
{
    var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri")!);
    builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
}
```

---

## Environment Detection

### Backend (C#)

```csharp
// Check environment
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/error");
}

// Get environment name
var envName = app.Environment.EnvironmentName; // "Development", "Production", etc.
```

### Frontend (TypeScript)

```typescript
// Check if development
if (import.meta.env.DEV) {
  console.log('Running in development mode');
}

// Check if production
if (import.meta.env.PROD) {
  // Production-only code
}

// Get mode
const mode = import.meta.env.MODE; // "development" or "production"
```

---

## Configuration Validation

### Startup Validation

Add configuration validation in Program.cs:

```csharp
// Validate required configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("DefaultConnection connection string is required");
}
```

### Options Pattern Validation

```csharp
// Define options class
public class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

// Register with validation
builder.Services.AddOptions<CorsOptions>()
    .BindConfiguration("Cors")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

---

## Quick Reference

### Local Development Setup

```bash
# 1. Copy example settings
cp src/Cadence.WebApi/appsettings.Local.example.json src/Cadence.WebApi/appsettings.Local.json

# 2. Edit connection string in appsettings.Local.json

# 3. Apply migrations
cd src/Cadence.WebApi
dotnet ef database update --project ../Cadence.Core/Cadence.Core.csproj

# 4. Run backend
dotnet run

# 5. In another terminal, run frontend
cd src/frontend
npm install
npm run dev
```

### Verify Configuration

```bash
# Check which environment is active
dotnet run --environment Development
dotnet run --environment Production

# Check loaded configuration (add temporarily to Program.cs)
foreach (var item in builder.Configuration.AsEnumerable())
{
    Console.WriteLine($"{item.Key}: {item.Value}");
}
```
