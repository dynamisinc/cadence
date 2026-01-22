# S18: Entra Provider Implementation

## Story

**As a** user in an organization using Azure Entra,
**I want** to sign in to Cadence with my Microsoft account,
**So that** I don't need a separate password and can use SSO.

## Context

Azure Entra (formerly Azure AD) integration enables enterprise SSO. Users click "Sign in with Microsoft," authenticate with their organizational credentials, and are redirected back to Cadence. The system links their Entra identity to a local Cadence account (creating one if needed).

**MVP Note**: This story provides the architecture and stub implementation. Full Entra integration is P2 priority - the structure is ready but throws `NotImplementedException` until implemented.

## Acceptance Criteria

### Configuration
- [ ] **Given** Entra is configured, **when** the app starts, **then** the provider registers without error
- [ ] **Given** Entra is disabled in config, **when** users try Microsoft login, **then** they see "This method is not enabled"
- [ ] **Given** `AllowedDomains` is set, **when** a user from another domain tries to login, **then** they are rejected

### OAuth Flow
- [ ] **Given** I click "Sign in with Microsoft", **when** I'm redirected, **then** I see the Microsoft login page
- [ ] **Given** I complete Microsoft login, **when** redirected back, **then** Cadence receives the auth code
- [ ] **Given** Cadence receives auth code, **when** exchanging for tokens, **then** it retrieves my profile info
- [ ] **Given** my email matches an existing account, **when** login completes, **then** I'm linked to that account
- [ ] **Given** no matching account exists, **when** login completes, **then** a new account is created for me

### Token Handling
- [ ] **Given** I authenticated via Entra, **when** I receive tokens, **then** they are Cadence JWTs (not Entra tokens)
- [ ] **Given** I authenticated via Entra, **when** my session needs refresh, **then** Cadence refresh tokens work normally

## Out of Scope (MVP)

- Azure AD group-to-role mapping
- Multi-tenant support
- Conditional access policy integration
- Token refresh via Entra (we use our own refresh tokens)

## Dependencies

- S16 (Auth Service Interface)
- S19 (User Account Linking)
- Azure AD App Registration (manual setup)

## Domain Terms

| Term | Definition |
|------|------------|
| Azure Entra | Microsoft's identity platform (formerly Azure AD) |
| OAuth 2.0 | Authorization framework used for external login |
| Authorization Code Flow | OAuth flow where code is exchanged for tokens server-side |
| PKCE | Proof Key for Code Exchange - security enhancement for OAuth |
| Object ID | Unique identifier for user in Azure AD tenant |

## OAuth Flow Diagram

```
┌─────────┐       ┌─────────┐       ┌─────────┐       ┌─────────┐
│ Browser │       │ Cadence │       │  Entra  │       │  Graph  │
│         │       │   API   │       │         │       │   API   │
└────┬────┘       └────┬────┘       └────┬────┘       └────┬────┘
     │                 │                 │                 │
     │ 1. Click        │                 │                 │
     │ "Sign in with   │                 │                 │
     │  Microsoft"     │                 │                 │
     │────────────────>│                 │                 │
     │                 │                 │                 │
     │ 2. Redirect to  │                 │                 │
     │ /authorize      │                 │                 │
     │<────────────────│                 │                 │
     │                 │                 │                 │
     │ 3. Login at Microsoft             │                 │
     │────────────────────────────────-->│                 │
     │                 │                 │                 │
     │ 4. Redirect with auth code        │                 │
     │<──────────────────────────────────│                 │
     │                 │                 │                 │
     │ 5. Send code to │                 │                 │
     │ callback        │                 │                 │
     │────────────────>│                 │                 │
     │                 │                 │                 │
     │                 │ 6. Exchange     │                 │
     │                 │ code for tokens │                 │
     │                 │────────────────>│                 │
     │                 │                 │                 │
     │                 │ 7. Tokens       │                 │
     │                 │<────────────────│                 │
     │                 │                 │                 │
     │                 │ 8. Get user     │                 │
     │                 │ profile         │                 │
     │                 │────────────────────────────────-->│
     │                 │                 │                 │
     │                 │ 9. User info    │                 │
     │                 │<──────────────────────────────────│
     │                 │                 │                 │
     │                 │ 10. Link/create │                 │
     │                 │ local user      │                 │
     │                 │                 │                 │
     │ 11. Cadence JWT │                 │                 │
     │<────────────────│                 │                 │
     │                 │                 │                 │
```

## Implementation

```csharp
/// <summary>
/// Azure Entra (Azure AD) authentication provider.
/// Implements OAuth 2.0 Authorization Code flow with PKCE.
/// </summary>
public class EntraAuthenticationProvider : IAuthenticationProvider
{
    private readonly EntraOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EntraAuthenticationProvider> _logger;

    public string ProviderType => "Entra";
    public bool IsEnabled => _options.Enabled;
    public bool SupportsPasswordAuth => false;
    public bool SupportsRegistration => false;

    public EntraAuthenticationProvider(
        IOptions<EntraOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<EntraAuthenticationProvider> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Generate OAuth authorization URL for redirect.
    /// </summary>
    public string GetAuthorizationUrl(string state, string codeVerifier)
    {
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _options.RedirectUri,
            ["scope"] = "openid profile email User.Read",
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["response_mode"] = "query"
        };
        
        var baseUrl = $"{_options.Instance}{_options.TenantId}/oauth2/v2.0/authorize";
        return $"{baseUrl}?{string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"))}";
    }

    /// <summary>
    /// Exchange authorization code for tokens and retrieve user info.
    /// </summary>
    public async Task<ExternalUserInfo?> GetExternalUserAsync(string code, string codeVerifier)
    {
        // MVP: Stub implementation
        // TODO: Implement when Entra integration is prioritized
        
        if (!_options.Enabled)
        {
            _logger.LogWarning("Entra authentication attempted but provider is disabled");
            return null;
        }

        _logger.LogWarning(
            "EntraAuthenticationProvider.GetExternalUserAsync is not yet implemented. " +
            "Enable Identity provider for functional authentication.");
            
        throw new NotImplementedException(
            "Entra authentication not yet implemented. " +
            "This is planned for a future release.");
        
        // FUTURE IMPLEMENTATION:
        // 1. Exchange code for tokens
        // var tokens = await ExchangeCodeForTokensAsync(code, codeVerifier);
        
        // 2. Get user profile from Microsoft Graph
        // var profile = await GetUserProfileAsync(tokens.AccessToken);
        
        // 3. Validate domain if restricted
        // if (_options.AllowedDomains.Any() && !_options.AllowedDomains.Contains(GetDomain(profile.Email)))
        //     return null;
        
        // 4. Return external user info
        // return new ExternalUserInfo
        // {
        //     Provider = "Entra",
        //     ProviderUserId = profile.Id,
        //     Email = profile.Mail ?? profile.UserPrincipalName,
        //     DisplayName = profile.DisplayName,
        //     Claims = new Dictionary<string, string>
        //     {
        //         ["tid"] = tokens.TenantId,
        //         ["oid"] = profile.Id
        //     }
        // };
    }

    public Task<ProviderAuthResult> AuthenticateAsync(object request)
    {
        // External providers don't use direct authentication
        // They use OAuth redirect flow instead
        throw new NotSupportedException("Use OAuth redirect flow for external authentication");
    }

    #region Future Implementation Helpers

    /*
    private async Task<EntraTokenResponse> ExchangeCodeForTokensAsync(string code, string codeVerifier)
    {
        var client = _httpClientFactory.CreateClient();
        var tokenEndpoint = $"{_options.Instance}{_options.TenantId}/oauth2/v2.0/token";
        
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        });
        
        var response = await client.PostAsync(tokenEndpoint, content);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<EntraTokenResponse>();
    }

    private async Task<GraphUserProfile> GetUserProfileAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<GraphUserProfile>();
    }
    */

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    #endregion
}

/// <summary>
/// Configuration options for Azure Entra authentication.
/// </summary>
public class EntraOptions
{
    public bool Enabled { get; set; } = false;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string RedirectUri { get; set; } = string.Empty;
    public string[] AllowedDomains { get; set; } = Array.Empty<string>();
    public string DefaultRole { get; set; } = "Observer";
    public bool AutoLinkByEmail { get; set; } = true;
}
```

## Azure AD App Registration Setup

When implementing Entra, you'll need to:

1. **Create App Registration** in Azure Portal
   - Navigate to Azure AD > App registrations > New registration
   - Name: "Cadence"
   - Supported account types: Single tenant (your org)
   - Redirect URI: `https://your-domain/api/auth/callback/entra`

2. **Configure Authentication**
   - Platform: Web
   - Redirect URIs: Add your callback URL
   - Enable ID tokens

3. **Add API Permissions**
   - Microsoft Graph > Delegated > `User.Read`
   - Microsoft Graph > Delegated > `email`
   - Microsoft Graph > Delegated > `profile`
   - Microsoft Graph > Delegated > `openid`

4. **Create Client Secret**
   - Certificates & secrets > New client secret
   - Store in Azure Key Vault or dotnet secrets

5. **Update Cadence Configuration**
   ```json
   {
     "Authentication": {
       "Providers": {
         "Entra": {
           "Enabled": true,
           "TenantId": "your-tenant-id",
           "ClientId": "your-client-id",
           "ClientSecret": "from-secrets",
           "RedirectUri": "https://your-domain/api/auth/callback/entra"
         }
       }
     }
   }
   ```

## Technical Notes

- Use PKCE (Proof Key for Code Exchange) for enhanced security
- Store code verifier in session/state during OAuth flow
- Entra tokens are NOT returned to client - we issue our own JWTs
- Consider using `Microsoft.Identity.Web` NuGet package for production
- Log authentication events for security auditing

---

*Story created: 2025-01-21*
*Updated: 2025-01-21 - Expanded from stub to full implementation story*
