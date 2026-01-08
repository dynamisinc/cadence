# Authentication Implementation Guide

> **Status:** Reference Guide - Not Yet Implemented in Template
> **Priority:** Critical for Production

This guide explains how to implement Azure AD B2C authentication in the Dynamis Reference App.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Backend Implementation](#backend-implementation)
4. [Frontend Implementation](#frontend-implementation)
5. [Testing](#testing)
6. [Troubleshooting](#troubleshooting)

---

## Overview

### Current State

The template currently uses a **development-only** header-based authentication:

```csharp
// Current implementation in NotesFunction.cs
private static string GetUserId(HttpRequest req)
{
    return req.Headers["X-User-Id"].FirstOrDefault() ?? "dev-user@example.com";
}
```

**This is NOT suitable for production.** It allows anyone to impersonate any user.

### Target State

For production, implement **Azure AD B2C** with JWT tokens:

```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│   Browser   │────▶│  Azure AD    │────▶│  Azure Function │
│   (React)   │◀────│  B2C         │◀────│  (Validates JWT)│
└─────────────┘     └──────────────┘     └─────────────────┘
```

---

## Architecture

### Authentication Flow

1. **User clicks "Sign In"** in React app
2. **Redirect to Azure AD B2C** login page
3. **User authenticates** (email/password, social login, etc.)
4. **Azure AD B2C issues JWT** token
5. **React app stores token** and sends with API requests
6. **Azure Functions validate JWT** on each request

### Token Structure

Azure AD B2C JWTs contain:

```json
{
  "iss": "https://yourtenant.b2clogin.com/.../v2.0/",
  "sub": "user-object-id",
  "aud": "your-client-id",
  "exp": 1234567890,
  "email": "user@example.com",
  "name": "John Doe",
  "extension_Role": "Contributor"
}
```

---

## Backend Implementation

### Step 1: Install NuGet Packages

```bash
cd src/api
dotnet add package Microsoft.Identity.Web
```

### Step 2: Create Authentication Middleware

Create `src/api/Core/Middleware/JwtAuthenticationMiddleware.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.IdentityModel.Tokens;

namespace DynamisReferenceApp.Api.Core.Middleware;

/// <summary>
/// Middleware to validate Azure AD B2C JWT tokens.
/// </summary>
public class JwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly bool _requireAuthentication;

    public JwtAuthenticationMiddleware(
        ILogger<JwtAuthenticationMiddleware> logger,
        IConfiguration configuration)
    {
        _logger = logger;

        // Check if auth is required (skip in development if desired)
        _requireAuthentication = configuration.GetValue<bool>("Authentication:Required");

        var tenantName = configuration["AzureAdB2C:TenantName"];
        var clientId = configuration["AzureAdB2C:ClientId"];
        var policyName = configuration["AzureAdB2C:SignUpSignInPolicy"];

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://{tenantName}.b2clogin.com/{tenantName}.onmicrosoft.com/{policyName}/v2.0",
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Azure AD B2C uses RS256
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                // Fetch signing keys from Azure AD B2C metadata
                var metadataUrl = $"https://{tenantName}.b2clogin.com/{tenantName}.onmicrosoft.com/{policyName}/v2.0/.well-known/openid-configuration";
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataUrl,
                    new OpenIdConnectConfigurationRetriever());
                var config = configManager.GetConfigurationAsync(CancellationToken.None).Result;
                return config.SigningKeys;
            }
        };
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();

        if (httpRequest == null)
        {
            await next(context);
            return;
        }

        // Skip auth for health endpoints
        if (httpRequest.Url.AbsolutePath.Contains("/health") ||
            httpRequest.Url.AbsolutePath.Contains("/ping"))
        {
            await next(context);
            return;
        }

        // Extract token from Authorization header
        var authHeader = httpRequest.Headers
            .FirstOrDefault(h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            .Value?.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            if (_requireAuthentication)
            {
                _logger.LogWarning("Missing or invalid Authorization header");
                context.Items["AuthError"] = "Unauthorized";
                return;
            }

            // Development fallback
            context.Items["UserId"] = "dev-user@example.com";
            context.Items["UserEmail"] = "dev-user@example.com";
            await next(context);
            return;
        }

        var token = authHeader.Substring("Bearer ".Length);

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _tokenValidationParameters, out _);

            // Store user info in context for functions to use
            context.Items["UserId"] = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                    ?? principal.FindFirst("sub")?.Value;
            context.Items["UserEmail"] = principal.FindFirst(ClaimTypes.Email)?.Value
                                        ?? principal.FindFirst("email")?.Value;
            context.Items["UserName"] = principal.FindFirst(ClaimTypes.Name)?.Value
                                       ?? principal.FindFirst("name")?.Value;
            context.Items["UserRole"] = principal.FindFirst("extension_Role")?.Value ?? "ReadOnly";
            context.Items["ClaimsPrincipal"] = principal;

            _logger.LogDebug("Authenticated user: {UserId}", context.Items["UserId"]);

            await next(context);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            context.Items["AuthError"] = "Invalid token";
        }
    }
}
```

### Step 3: Create User Context Service

Create `src/api/Core/Services/UserContext.cs`:

```csharp
namespace DynamisReferenceApp.Api.Core.Services;

/// <summary>
/// Provides access to the current authenticated user's information.
/// </summary>
public interface IUserContext
{
    string UserId { get; }
    string Email { get; }
    string Name { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
    bool HasRole(string role);
}

public class UserContext : IUserContext
{
    private readonly FunctionContext _context;

    public UserContext(FunctionContext context)
    {
        _context = context;
    }

    public string UserId => _context.Items.TryGetValue("UserId", out var id)
        ? id?.ToString() ?? "" : "";

    public string Email => _context.Items.TryGetValue("UserEmail", out var email)
        ? email?.ToString() ?? "" : "";

    public string Name => _context.Items.TryGetValue("UserName", out var name)
        ? name?.ToString() ?? "" : "";

    public string Role => _context.Items.TryGetValue("UserRole", out var role)
        ? role?.ToString() ?? "ReadOnly" : "ReadOnly";

    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    public bool HasRole(string requiredRole)
    {
        var roleHierarchy = new[] { "ReadOnly", "Contributor", "Manage" };
        var userRoleIndex = Array.IndexOf(roleHierarchy, Role);
        var requiredRoleIndex = Array.IndexOf(roleHierarchy, requiredRole);
        return userRoleIndex >= requiredRoleIndex;
    }
}
```

### Step 4: Update Program.cs

```csharp
// Add middleware
builder.UseMiddleware<JwtAuthenticationMiddleware>();

// Register UserContext
builder.Services.AddScoped<IUserContext, UserContext>();
```

### Step 5: Update Functions to Use UserContext

```csharp
public class NotesFunction
{
    private readonly INotesService _notesService;
    private readonly IUserContext _userContext;

    public NotesFunction(INotesService notesService, IUserContext userContext)
    {
        _notesService = notesService;
        _userContext = userContext;
    }

    [Function("GetNotes")]
    public async Task<IActionResult> GetNotes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notes")] HttpRequest req)
    {
        if (!_userContext.IsAuthenticated)
        {
            return new UnauthorizedResult();
        }

        var notes = await _notesService.GetNotesAsync(_userContext.UserId);
        return new OkObjectResult(notes);
    }
}
```

### Step 6: Add Configuration

Update `local.settings.json`:

```json
{
  "Values": {
    "AzureAdB2C:TenantName": "yourtenant",
    "AzureAdB2C:ClientId": "your-client-id",
    "AzureAdB2C:SignUpSignInPolicy": "B2C_1_signupsignin",
    "Authentication:Required": "false"
  }
}
```

---

## Frontend Implementation

### Step 1: Install MSAL

```bash
cd src/frontend
npm install @azure/msal-browser @azure/msal-react
```

### Step 2: Create Auth Configuration

Create `src/frontend/src/core/auth/authConfig.ts`:

```typescript
import { Configuration, LogLevel } from "@azure/msal-browser";

export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_AZURE_AD_CLIENT_ID,
    authority: `https://${import.meta.env.VITE_AZURE_AD_TENANT}.b2clogin.com/${import.meta.env.VITE_AZURE_AD_TENANT}.onmicrosoft.com/${import.meta.env.VITE_AZURE_AD_POLICY}`,
    knownAuthorities: [`${import.meta.env.VITE_AZURE_AD_TENANT}.b2clogin.com`],
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            break;
          case LogLevel.Warning:
            console.warn(message);
            break;
          case LogLevel.Info:
            console.info(message);
            break;
          case LogLevel.Verbose:
            console.debug(message);
            break;
        }
      },
    },
  },
};

export const loginRequest = {
  scopes: ["openid", "profile", "email"],
};

export const apiRequest = {
  scopes: [`https://${import.meta.env.VITE_AZURE_AD_TENANT}.onmicrosoft.com/api/access`],
};
```

### Step 3: Create Auth Provider

Create `src/frontend/src/core/auth/AuthProvider.tsx`:

```typescript
import { ReactNode } from "react";
import { MsalProvider } from "@azure/msal-react";
import { PublicClientApplication, EventType } from "@azure/msal-browser";
import { msalConfig } from "./authConfig";

const msalInstance = new PublicClientApplication(msalConfig);

// Handle redirect response
msalInstance.initialize().then(() => {
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length > 0) {
    msalInstance.setActiveAccount(accounts[0]);
  }

  msalInstance.addEventCallback((event) => {
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
      const payload = event.payload as { account: unknown };
      msalInstance.setActiveAccount(payload.account as ReturnType<typeof msalInstance.getActiveAccount>);
    }
  });
});

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
};
```

### Step 4: Create useAuth Hook

Create `src/frontend/src/core/auth/useAuth.ts`:

```typescript
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { useCallback } from "react";
import { loginRequest, apiRequest } from "./authConfig";

export const useAuth = () => {
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const login = useCallback(async () => {
    try {
      await instance.loginPopup(loginRequest);
    } catch (error) {
      console.error("Login failed:", error);
      throw error;
    }
  }, [instance]);

  const logout = useCallback(async () => {
    try {
      await instance.logoutPopup();
    } catch (error) {
      console.error("Logout failed:", error);
      throw error;
    }
  }, [instance]);

  const getAccessToken = useCallback(async (): Promise<string | null> => {
    if (!isAuthenticated || accounts.length === 0) {
      return null;
    }

    try {
      const response = await instance.acquireTokenSilent({
        ...apiRequest,
        account: accounts[0],
      });
      return response.accessToken;
    } catch (error) {
      // If silent acquisition fails, try popup
      try {
        const response = await instance.acquireTokenPopup(apiRequest);
        return response.accessToken;
      } catch (popupError) {
        console.error("Token acquisition failed:", popupError);
        return null;
      }
    }
  }, [instance, accounts, isAuthenticated]);

  const user = accounts[0]
    ? {
        id: accounts[0].localAccountId,
        email: accounts[0].username,
        name: accounts[0].name ?? accounts[0].username,
      }
    : null;

  return {
    isAuthenticated,
    isLoading: inProgress !== InteractionStatus.None,
    user,
    login,
    logout,
    getAccessToken,
  };
};
```

### Step 5: Update API Client

Update `src/frontend/src/core/services/api.ts`:

```typescript
import axios from "axios";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Token will be set by auth interceptor
let getAccessTokenFn: (() => Promise<string | null>) | null = null;

export const setAuthTokenProvider = (fn: () => Promise<string | null>) => {
  getAccessTokenFn = fn;
};

apiClient.interceptors.request.use(
  async (config) => {
    // Add correlation ID
    config.headers["X-Correlation-Id"] = crypto.randomUUID();

    // Add auth token if available
    if (getAccessTokenFn) {
      const token = await getAccessTokenFn();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    }

    return config;
  },
  (error) => Promise.reject(error)
);

export { apiClient };
```

### Step 6: Initialize Auth in App

Update `src/frontend/src/App.tsx`:

```typescript
import { useEffect } from "react";
import { AuthProvider } from "./core/auth/AuthProvider";
import { useAuth } from "./core/auth/useAuth";
import { setAuthTokenProvider } from "./core/services/api";

const AppContent: React.FC = () => {
  const { getAccessToken } = useAuth();

  useEffect(() => {
    setAuthTokenProvider(getAccessToken);
  }, [getAccessToken]);

  // ... rest of app
};

const App: React.FC = () => {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
};
```

### Step 7: Add Environment Variables

Update `src/frontend/.env.example`:

```env
# Azure AD B2C Configuration
VITE_AZURE_AD_TENANT=yourtenant
VITE_AZURE_AD_CLIENT_ID=your-client-id
VITE_AZURE_AD_POLICY=B2C_1_signupsignin
```

---

## Testing

### Backend Testing with Mock User

```csharp
// In test setup, mock the user context
var mockUserContext = new Mock<IUserContext>();
mockUserContext.Setup(x => x.UserId).Returns("test-user-id");
mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
```

### Frontend Testing with Mock Auth

```typescript
// Mock useAuth hook in tests
vi.mock("@/core/auth/useAuth", () => ({
  useAuth: () => ({
    isAuthenticated: true,
    user: { id: "test-user", email: "test@example.com", name: "Test User" },
    login: vi.fn(),
    logout: vi.fn(),
    getAccessToken: vi.fn().mockResolvedValue("mock-token"),
  }),
}));
```

---

## Troubleshooting

### "AADSTS50011: Reply URL mismatch"

Ensure the redirect URI in Azure AD B2C matches exactly:
- Development: `http://localhost:5197`
- Production: `https://your-app.azurestaticapps.net`

### "Token validation failed"

Check:
1. Clock skew between client and server
2. Token hasn't expired
3. Audience matches client ID
4. Issuer URL is correct

### "CORS error on token acquisition"

Ensure Azure AD B2C is configured to allow your frontend origin.

### "Silent token acquisition failed"

This happens when:
- Session expired
- User needs to re-consent
- Popup blockers are active

The code handles this by falling back to popup acquisition.

---

## Azure AD B2C Setup Checklist

1. [ ] Create Azure AD B2C tenant
2. [ ] Register frontend application
3. [ ] Register backend API application
4. [ ] Create user flows (sign-up/sign-in)
5. [ ] Configure token claims
6. [ ] Add custom attributes if needed
7. [ ] Configure redirect URIs
8. [ ] Test authentication flow

---

## Related Documentation

- [Azure AD B2C Documentation](https://docs.microsoft.com/azure/active-directory-b2c/)
- [MSAL.js Documentation](https://github.com/AzureAD/microsoft-authentication-library-for-js)
- [.NET Identity Web](https://github.com/AzureAD/microsoft-identity-web)
