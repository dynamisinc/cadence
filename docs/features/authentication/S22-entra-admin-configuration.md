# S22: Entra Admin Configuration

## Story

**As an** Administrator,
**I want** to configure Azure Entra integration settings,
**So that** users in my organization can sign in with their Microsoft accounts.

## Context

Before users can sign in with Microsoft, an Administrator must configure the Entra integration. This involves providing Azure AD app registration details and setting policies for user provisioning. For MVP, this is done via configuration files; a future enhancement could add a UI.

## Acceptance Criteria

### Configuration Settings
- [ ] **Given** I am configuring Entra, **when** I set `Enabled: true`, **then** the Microsoft login button appears
- [ ] **Given** I am configuring Entra, **when** I set `Enabled: false`, **then** the Microsoft login button is hidden
- [ ] **Given** I provide valid Azure AD settings, **when** app starts, **then** Entra provider initializes without error
- [ ] **Given** I provide invalid settings, **when** app starts, **then** Entra is disabled with warning in logs

### Required Settings
- [ ] **Given** Entra is enabled, **when** TenantId is missing, **then** startup fails with clear error
- [ ] **Given** Entra is enabled, **when** ClientId is missing, **then** startup fails with clear error
- [ ] **Given** Entra is enabled, **when** ClientSecret is missing, **then** startup fails with clear error

### Domain Restrictions
- [ ] **Given** AllowedDomains is empty, **when** any Microsoft user signs in, **then** they are allowed
- [ ] **Given** AllowedDomains contains "contoso.com", **when** user@contoso.com signs in, **then** they are allowed
- [ ] **Given** AllowedDomains contains "contoso.com", **when** user@other.com signs in, **then** they are rejected

### User Provisioning
- [ ] **Given** DefaultRole is "Observer", **when** new SSO user is created, **then** they get Observer role
- [ ] **Given** DefaultRole is "Controller", **when** new SSO user is created, **then** they get Controller role
- [ ] **Given** AutoLinkByEmail is true, **when** SSO email matches existing user, **then** accounts are linked
- [ ] **Given** AutoLinkByEmail is false, **when** SSO email matches existing user, **then** new account is created

## Out of Scope

- Admin UI for configuration (config file only for MVP)
- Testing connection from UI
- Azure AD group-to-role mapping
- Multiple Azure AD tenants

## Dependencies

- S18 (Entra Provider)
- Azure AD App Registration (external)

## Domain Terms

| Term | Definition |
|------|------------|
| Tenant ID | Unique identifier for your Azure AD directory |
| Client ID | Application (client) ID from Azure AD app registration |
| Client Secret | Secret key for server-to-server authentication |
| Allowed Domains | Email domains permitted to sign in via Entra |
| Default Role | Role assigned to new users created via SSO |

## Configuration Schema

```json
{
  "Authentication": {
    "Providers": {
      "Identity": {
        "Enabled": true,
        "AllowRegistration": true,
        "PasswordRequireDigit": true,
        "PasswordRequireUppercase": true,
        "PasswordMinLength": 8,
        "LockoutMaxAttempts": 5,
        "LockoutMinutes": 15
      },
      "Entra": {
        "Enabled": true,
        "TenantId": "your-azure-ad-tenant-id",
        "ClientId": "your-app-registration-client-id",
        "ClientSecret": "your-client-secret",
        "Instance": "https://login.microsoftonline.com/",
        "CallbackPath": "/api/auth/callback/entra",
        "AllowedDomains": ["contoso.com", "fabrikam.com"],
        "DefaultRole": "Observer",
        "AutoLinkByEmail": true
      }
    },
    "Jwt": {
      "Issuer": "Cadence",
      "Audience": "Cadence",
      "AccessTokenMinutes": 15,
      "RefreshTokenHours": 4,
      "RememberMeDays": 30
    },
    "UserLinking": {
      "AutoLinkByEmail": true,
      "RequireEmailVerification": false
    }
  }
}
```

## Configuration Options Reference

| Setting | Type | Required | Default | Description |
|---------|------|----------|---------|-------------|
| `Enabled` | bool | No | false | Enable/disable Microsoft login |
| `TenantId` | string | Yes* | - | Azure AD tenant ID |
| `ClientId` | string | Yes* | - | App registration client ID |
| `ClientSecret` | string | Yes* | - | App registration secret |
| `Instance` | string | No | `https://login.microsoftonline.com/` | Azure AD instance URL |
| `CallbackPath` | string | No | `/api/auth/callback/entra` | OAuth callback endpoint |
| `AllowedDomains` | string[] | No | [] (all allowed) | Restrict to specific email domains |
| `DefaultRole` | string | No | "Observer" | Role for new SSO users |
| `AutoLinkByEmail` | bool | No | true | Link SSO to existing accounts by email |

*Required only when Enabled is true

## Secrets Management

**Never store ClientSecret in appsettings.json in source control!**

### Development (User Secrets)
```bash
# Initialize user secrets
dotnet user-secrets init

# Set the client secret
dotnet user-secrets set "Authentication:Providers:Entra:ClientSecret" "your-secret-here"
```

### Production (Azure Key Vault)
```json
// appsettings.json - reference Key Vault
{
  "Authentication": {
    "Providers": {
      "Entra": {
        "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/EntraClientSecret)"
      }
    }
  }
}
```

### Production (Environment Variables)
```bash
# Azure App Service Configuration
Authentication__Providers__Entra__ClientSecret=your-secret-here
```

## Startup Validation

```csharp
public static class EntraConfigurationExtensions
{
    public static IServiceCollection AddEntraAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var entraConfig = configuration
            .GetSection("Authentication:Providers:Entra")
            .Get<EntraOptions>();

        if (entraConfig?.Enabled != true)
        {
            // Entra disabled - register null/stub provider
            services.AddSingleton<EntraAuthenticationProvider?>(sp => null);
            return services;
        }

        // Validate required settings
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(entraConfig.TenantId))
            errors.Add("TenantId is required when Entra is enabled");
        
        if (string.IsNullOrWhiteSpace(entraConfig.ClientId))
            errors.Add("ClientId is required when Entra is enabled");
        
        if (string.IsNullOrWhiteSpace(entraConfig.ClientSecret))
            errors.Add("ClientSecret is required when Entra is enabled");

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Entra configuration errors:\n{string.Join("\n", errors)}");
        }

        // Validate TenantId format (GUID)
        if (!Guid.TryParse(entraConfig.TenantId, out _))
        {
            throw new InvalidOperationException(
                "TenantId must be a valid GUID");
        }

        services.Configure<EntraOptions>(
            configuration.GetSection("Authentication:Providers:Entra"));
        
        services.AddScoped<EntraAuthenticationProvider>();
        
        return services;
    }
}
```

## Azure AD App Registration Checklist

Before configuring Cadence, complete these steps in Azure Portal:

### 1. Create App Registration
- [ ] Navigate to Azure AD > App registrations > New registration
- [ ] Name: "Cadence" (or your preferred name)
- [ ] Supported account types: "Accounts in this organizational directory only"
- [ ] Redirect URI: Web > `https://your-domain/api/auth/callback/entra`

### 2. Note Required Values
- [ ] Copy **Application (client) ID** → `ClientId`
- [ ] Copy **Directory (tenant) ID** → `TenantId`

### 3. Create Client Secret
- [ ] Certificates & secrets > Client secrets > New client secret
- [ ] Description: "Cadence Production" (or environment name)
- [ ] Expiration: Choose appropriate (recommend 24 months)
- [ ] Copy the **Value** immediately → `ClientSecret`
- [ ] ⚠️ You cannot view this value again!

### 4. Configure API Permissions
- [ ] API permissions > Add a permission > Microsoft Graph
- [ ] Delegated permissions:
  - [ ] `openid`
  - [ ] `profile`
  - [ ] `email`
  - [ ] `User.Read`
- [ ] Click "Grant admin consent for [Org]"

### 5. Configure Token Settings
- [ ] Token configuration > Optional claims
- [ ] Add optional claim > ID > `email`
- [ ] Add optional claim > ID > `preferred_username`

### 6. Update Redirect URIs (if needed)
- [ ] Authentication > Add redirect URIs for each environment:
  - `https://localhost:5001/api/auth/callback/entra` (dev)
  - `https://uat.yourdomain.com/api/auth/callback/entra` (uat)
  - `https://cadence.yourdomain.com/api/auth/callback/entra` (prod)

## Technical Notes

- Configuration is loaded at startup; changes require restart
- Consider adding health check endpoint to verify Entra connectivity
- Log configuration validation results (without secrets) for troubleshooting
- Future enhancement: Admin UI with "Test Connection" button

---

*Story created: 2025-01-21*
