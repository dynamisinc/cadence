# Security Implementation Guide

> **Status:** Implementation Guide
> **Priority:** Critical for Production
> **Prerequisite:** A Microsoft Entra ID (formerly Azure AD) tenant.

This guide outlines the steps to upgrade the Dynamis Reference App from "Dev Mode" (mock auth) to "Production Mode" (Entra ID).

---

## Phase 1: Frontend Authentication (React + MSAL)

### 1. Install Dependencies

```bash
cd src/frontend
npm install @azure/msal-browser @azure/msal-react
```

### 2. Configure MSAL

Create `src/frontend/src/core/auth/authConfig.ts`:

```typescript
export const msalConfig = {
  auth: {
    clientId: "YOUR_CLIENT_ID",
    authority: "https://login.microsoftonline.com/YOUR_TENANT_ID",
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  },
};

export const loginRequest = {
  scopes: ["User.Read", "api://YOUR_API_CLIENT_ID/access_as_user"],
};
```

### 3. Wrap App with Provider

Update `src/frontend/src/main.tsx`:

```tsx
import { MsalProvider } from "@azure/msal-react";
import { PublicClientApplication } from "@azure/msal-browser";
import { msalConfig } from "./core/auth/authConfig";

const msalInstance = new PublicClientApplication(msalConfig);

root.render(
  <MsalProvider instance={msalInstance}>
    <App />
  </MsalProvider>
);
```

### 4. Update API Client

Modify `src/frontend/src/core/services/api.ts` to acquire tokens silently:

```typescript
import { msalInstance } from "../auth/authConfig"; // You'll need to export the instance

apiClient.interceptors.request.use(async (config) => {
  const account = msalInstance.getActiveAccount();
  if (account) {
    const response = await msalInstance.acquireTokenSilent({
      ...loginRequest,
      account: account,
    });
    config.headers.Authorization = `Bearer ${response.accessToken}`;
  }
  return config;
});
```

---

## Phase 2: Backend Authentication (Azure Functions)

### 1. Install Dependencies

```bash
cd src/api
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.MicrosoftGraph
```

### 2. Configure App Settings

Update `local.settings.json` (and Azure Configuration):

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-tenant.onmicrosoft.com",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_API_CLIENT_ID",
    "CallbackPath": "/signin-oidc"
  }
}
```

### 3. Update Program.cs

```csharp
using Microsoft.Identity.Web;

// ... inside service configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();
```

### 4. Secure Functions

Remove the `GetUserId` helper and use the `ClaimsPrincipal`:

```csharp
// In NotesFunction.cs
public async Task<IActionResult> CreateNote(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
    FunctionContext context)
{
    // The middleware will populate the user
    var user = context.GetHttpContext()?.User;

    // Check if authenticated
    if (user?.Identity?.IsAuthenticated != true)
        return new UnauthorizedResult();

    // Get User ID (Object ID)
    var userId = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

    // ... rest of function
}
```

---

## Phase 3: Role-Based Access Control (RBAC)

1.  **Define App Roles** in the Entra ID App Registration manifest (e.g., `Dynamis.Contributor`, `Dynamis.Admin`).
2.  **Assign Users** to these roles in the Enterprise Application blade.
3.  **Check Roles** in code:
    ```csharp
    if (!user.IsInRole("Dynamis.Admin")) return new ForbidResult();
    ```
