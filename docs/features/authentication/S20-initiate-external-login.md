# S20: Initiate External Login

## Story

**As a** user who wants to sign in with Microsoft,
**I want** to click a button and be redirected to Microsoft's login page,
**So that** I can authenticate using my organizational credentials.

## Context

When users click "Sign in with Microsoft," Cadence initiates an OAuth 2.0 Authorization Code flow. The user is redirected to Microsoft's login page, authenticates there, and is redirected back with an authorization code. This story covers the initiation of that flow.

## Acceptance Criteria

### Button Interaction
- [ ] **Given** Entra is enabled, **when** I view the login page, **then** I see "Sign in with Microsoft" button
- [ ] **Given** I click "Sign in with Microsoft", **when** the action starts, **then** I see a loading indicator
- [ ] **Given** I click the button, **when** redirect begins, **then** I am sent to Microsoft's login page
- [ ] **Given** I am on Microsoft's page, **when** I view the URL, **then** it includes correct client_id and redirect_uri

### Security (PKCE)
- [ ] **Given** I initiate login, **when** the request is built, **then** a code_verifier is generated and stored
- [ ] **Given** I initiate login, **when** the request is built, **then** code_challenge is derived from code_verifier
- [ ] **Given** I initiate login, **when** the request is built, **then** state parameter is generated for CSRF protection

### State Management
- [ ] **Given** I was on a specific page, **when** I click Microsoft login, **then** return URL is preserved in state
- [ ] **Given** I initiate login, **when** code_verifier is stored, **then** it's in server session (not browser)
- [ ] **Given** state is generated, **when** stored, **then** it expires after 10 minutes

### Offline Handling
- [ ] **Given** I am offline, **when** I click "Sign in with Microsoft", **then** I see "External sign-in requires internet"
- [ ] **Given** Entra is disabled, **when** I try to access the endpoint directly, **then** I get 404 or "Method not enabled"

## Out of Scope

- The actual Microsoft login experience (controlled by Microsoft)
- Multi-tenant configuration
- Consent prompt customization

## Dependencies

- S04 (Login Form - button display)
- S16 (Auth Service Interface)
- S18 (Entra Provider)

## Domain Terms

| Term | Definition |
|------|------------|
| PKCE | Proof Key for Code Exchange - prevents authorization code interception |
| Code Verifier | Random string generated client-side, used to verify callback |
| Code Challenge | SHA256 hash of code verifier sent in initial request |
| State | Random token to prevent CSRF attacks and carry return URL |

## API Contract

**Endpoint:** `GET /api/auth/external/entra`

**Query Parameters:**
- `returnUrl` (optional): Where to redirect after successful auth

**Response:** 302 Redirect to Microsoft

**Redirect URL Structure:**
```
https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize
  ?client_id={client_id}
  &response_type=code
  &redirect_uri={callback_url}
  &scope=openid%20profile%20email%20User.Read
  &state={encrypted_state}
  &code_challenge={code_challenge}
  &code_challenge_method=S256
  &response_mode=query
```

## Implementation

```csharp
[ApiController]
[Route("api/auth")]
public class ExternalAuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IStateStore _stateStore;

    /// <summary>
    /// Initiates OAuth flow for external provider.
    /// </summary>
    [HttpGet("external/{provider}")]
    public async Task<IActionResult> InitiateExternalLogin(
        string provider, 
        [FromQuery] string? returnUrl = null)
    {
        // Validate provider is enabled
        var methods = _authService.GetAvailableMethods();
        if (!methods.Any(m => m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) && m.IsEnabled))
        {
            return NotFound(new { error = "provider_not_found", message = "Authentication method not available" });
        }

        // Generate PKCE values
        var codeVerifier = GenerateCodeVerifier();
        var state = GenerateState();
        
        // Store state with code verifier and return URL (server-side)
        await _stateStore.SaveAsync(state, new OAuthState
        {
            CodeVerifier = codeVerifier,
            ReturnUrl = returnUrl ?? "/",
            Provider = provider,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });

        // Get redirect URL from provider
        var redirectUrl = _authService.GetExternalLoginUrl(provider, state, codeVerifier);
        
        return Redirect(redirectUrl);
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateState()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
```

## State Storage

```csharp
/// <summary>
/// Stores OAuth state temporarily during auth flow.
/// </summary>
public interface IStateStore
{
    Task SaveAsync(string key, OAuthState state);
    Task<OAuthState?> GetAndRemoveAsync(string key);
}

public class OAuthState
{
    public string CodeVerifier { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = "/";
    public string Provider { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

// Implementation options:
// - In-memory cache (simple, but lost on restart)
// - Distributed cache (Redis) for multi-instance
// - Database (durable but slower)
```

## Frontend Implementation

```typescript
// Login page component
const handleMicrosoftLogin = async () => {
  if (!navigator.onLine) {
    showError("External sign-in requires internet connection");
    return;
  }

  setLoading(true);
  
  // Preserve current location for return
  const returnUrl = encodeURIComponent(window.location.pathname);
  
  // Redirect to backend which handles OAuth initiation
  window.location.href = `/api/auth/external/entra?returnUrl=${returnUrl}`;
};

// Button component
<Button
  variant="outlined"
  fullWidth
  onClick={handleMicrosoftLogin}
  disabled={loading || !isOnline}
  startIcon={<MicrosoftIcon />}
>
  Sign in with Microsoft
</Button>
```

## Technical Notes

- Code verifier must be 43-128 characters (we use 43 from 32 bytes base64url)
- State should be unpredictable and tied to user's session
- Never expose code_verifier to the browser - keep it server-side
- Consider rate limiting this endpoint to prevent abuse

---

*Story created: 2025-01-21*
