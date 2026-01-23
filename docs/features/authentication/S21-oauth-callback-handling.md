# S21: OAuth Callback Handling

## Story

**As a** user returning from Microsoft login,
**I want** Cadence to process my authentication and sign me in,
**So that** I can access the application with my Microsoft identity.

## Context

After authenticating with Microsoft, the user is redirected back to Cadence with an authorization code. Cadence must exchange this code for tokens, retrieve the user's profile, link or create their account, and issue Cadence JWTs. This is the most critical part of the OAuth flow.

## Acceptance Criteria

### Successful Flow
- [ ] **Given** I authenticated with Microsoft, **when** redirected to callback, **then** Cadence receives the auth code
- [ ] **Given** valid auth code received, **when** exchanged with Microsoft, **then** Cadence receives access/ID tokens
- [ ] **Given** valid tokens received, **when** profile is fetched, **then** user's email and name are retrieved
- [ ] **Given** profile retrieved, **when** account linking runs, **then** I'm linked to existing or new account
- [ ] **Given** account linked, **when** Cadence JWT issued, **then** I'm redirected to my original destination
- [ ] **Given** successful login, **when** I check my session, **then** I'm fully authenticated

### State Validation
- [ ] **Given** callback received, **when** state parameter is missing, **then** error "Invalid request" is shown
- [ ] **Given** callback received, **when** state doesn't match stored state, **then** error "Session expired" is shown
- [ ] **Given** callback received, **when** state has expired (>10 min), **then** error "Session expired" is shown
- [ ] **Given** state is validated, **when** used once, **then** it cannot be reused (one-time use)

### Error Handling
- [ ] **Given** Microsoft returns error, **when** callback processes, **then** user sees friendly error message
- [ ] **Given** code exchange fails, **when** error occurs, **then** user sees "Authentication failed" message
- [ ] **Given** user cancelled at Microsoft, **when** redirected back, **then** user sees "Sign in was cancelled"

### New User Flow
- [ ] **Given** no existing account for my email, **when** auth completes, **then** new account is created
- [ ] **Given** new account created, **when** I'm redirected, **then** I see welcome message for new users
- [ ] **Given** new account created, **when** I check my role, **then** I have the configured default role

## Out of Scope

- Token refresh via Microsoft (we use our own refresh tokens)
- Storing Microsoft tokens long-term
- Microsoft Graph API calls beyond profile

## Dependencies

- S18 (Entra Provider - token exchange)
- S19 (User Account Linking)
- S20 (Initiate External Login - state generation)

## Domain Terms

| Term | Definition |
|------|------------|
| Authorization Code | Short-lived code from Microsoft exchanged for tokens |
| Callback URL | Cadence endpoint that receives the redirect from Microsoft |
| Token Exchange | Server-to-server call to swap auth code for access tokens |
| ID Token | JWT from Microsoft containing user claims |

## OAuth Callback Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    CALLBACK PROCESSING                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. Receive callback with ?code=xxx&state=yyy                  │
│                     │                                           │
│                     ▼                                           │
│   2. Validate state (exists, not expired, matches)              │
│                     │                                           │
│            ┌───────┴───────┐                                    │
│            ▼               ▼                                    │
│      [Valid]          [Invalid]                                 │
│         │                  │                                    │
│         │                  └──► Redirect to /login?error=...    │
│         ▼                                                       │
│   3. Retrieve code_verifier from state store                    │
│                     │                                           │
│                     ▼                                           │
│   4. Exchange code + verifier for tokens (server-to-server)     │
│                     │                                           │
│            ┌───────┴───────┐                                    │
│            ▼               ▼                                    │
│      [Success]        [Failed]                                  │
│         │                  │                                    │
│         │                  └──► Redirect to /login?error=...    │
│         ▼                                                       │
│   5. Fetch user profile from Microsoft Graph                    │
│                     │                                           │
│                     ▼                                           │
│   6. Link or create local account (UserLinkingService)          │
│                     │                                           │
│                     ▼                                           │
│   7. Generate Cadence JWT (access + refresh tokens)             │
│                     │                                           │
│                     ▼                                           │
│   8. Set refresh token cookie, redirect with access token       │
│                     │                                           │
│                     ▼                                           │
│   9. Redirect to original returnUrl (from state)                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## API Contract

**Endpoint:** `GET /api/auth/callback/entra`

**Query Parameters (from Microsoft):**
- `code`: Authorization code
- `state`: State parameter we sent
- `error`: Error code (if auth failed)
- `error_description`: Error details (if auth failed)

**Success Response:** 302 Redirect to returnUrl with tokens

**Redirect URL Structure:**
```
{returnUrl}?auth=success&new={isNewAccount}
```

*Access token passed via secure mechanism (see Technical Notes)*

**Error Response:** 302 Redirect to login with error

```
/login?error={error_code}&message={friendly_message}
```

## Implementation

```csharp
[ApiController]
[Route("api/auth")]
public class ExternalAuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IStateStore _stateStore;
    private readonly ILogger<ExternalAuthController> _logger;

    /// <summary>
    /// Handles OAuth callback from external provider.
    /// </summary>
    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> HandleCallback(
        string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description)
    {
        // Handle Microsoft errors
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth error from {Provider}: {Error} - {Description}", 
                provider, error, error_description);
            
            var errorMessage = error switch
            {
                "access_denied" => "Sign in was cancelled",
                "consent_required" => "Permission was not granted",
                _ => "Authentication failed"
            };
            
            return Redirect($"/login?error={error}&message={Uri.EscapeDataString(errorMessage)}");
        }

        // Validate required parameters
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            _logger.LogWarning("OAuth callback missing code or state");
            return Redirect("/login?error=invalid_request&message=Invalid+authentication+request");
        }

        // Retrieve and validate state
        var oauthState = await _stateStore.GetAndRemoveAsync(state);
        if (oauthState == null)
        {
            _logger.LogWarning("OAuth state not found or already used: {State}", state);
            return Redirect("/login?error=invalid_state&message=Session+expired.+Please+try+again.");
        }

        if (oauthState.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("OAuth state expired");
            return Redirect("/login?error=expired&message=Session+expired.+Please+try+again.");
        }

        if (!oauthState.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("OAuth state provider mismatch");
            return Redirect("/login?error=invalid_state&message=Invalid+authentication+request");
        }

        try
        {
            // Process the authentication
            var result = await _authService.AuthenticateWithExternalAsync(new ExternalAuthRequest(
                Provider: provider,
                Code: code,
                CodeVerifier: oauthState.CodeVerifier,
                ReturnUrl: oauthState.ReturnUrl
            ));

            if (!result.IsSuccess)
            {
                _logger.LogWarning("External auth failed: {Error}", result.Error?.Code);
                var message = Uri.EscapeDataString(result.Error?.Message ?? "Authentication failed");
                return Redirect($"/login?error={result.Error?.Code}&message={message}");
            }

            // Set refresh token as HttpOnly cookie
            Response.Cookies.Append("refresh_token", result.RefreshToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(4)
            });

            // Redirect to frontend with access token
            // Option 1: Query parameter (simple but visible in URL)
            // Option 2: POST to frontend (more secure)
            // Option 3: Store in server session, frontend fetches
            
            var returnUrl = oauthState.ReturnUrl ?? "/";
            var separator = returnUrl.Contains('?') ? '&' : '?';
            var newParam = result.IsNewAccount ? "&new=true" : "";
            
            // Using a short-lived token transfer mechanism
            var transferToken = await CreateTokenTransfer(result.AccessToken!);
            
            return Redirect($"{returnUrl}{separator}token_transfer={transferToken}{newParam}");
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(ex, "Authentication exception during OAuth callback");
            return Redirect($"/login?error=auth_failed&message={Uri.EscapeDataString(ex.Message)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during OAuth callback");
            return Redirect("/login?error=server_error&message=An+unexpected+error+occurred");
        }
    }

    /// <summary>
    /// Creates a short-lived token transfer for secure access token delivery.
    /// </summary>
    private async Task<string> CreateTokenTransfer(string accessToken)
    {
        var transferId = Guid.NewGuid().ToString("N");
        
        // Store access token temporarily (30 seconds)
        await _stateStore.SaveAsync($"transfer:{transferId}", new TokenTransfer
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(30)
        });
        
        return transferId;
    }
}

/// <summary>
/// Endpoint for frontend to retrieve access token from transfer.
/// </summary>
[HttpPost("token/claim")]
public async Task<IActionResult> ClaimToken([FromBody] ClaimTokenRequest request)
{
    var transfer = await _stateStore.GetAndRemoveAsync($"transfer:{request.TransferId}");
    
    if (transfer == null || transfer.ExpiresAt < DateTime.UtcNow)
    {
        return BadRequest(new { error = "invalid_transfer", message = "Token transfer expired or invalid" });
    }
    
    return Ok(new { accessToken = transfer.AccessToken });
}
```

## Frontend Token Handling

```typescript
// App initialization - check for token transfer
useEffect(() => {
  const params = new URLSearchParams(window.location.search);
  const transferId = params.get('token_transfer');
  const isNew = params.get('new') === 'true';
  
  if (transferId) {
    claimToken(transferId, isNew);
  }
}, []);

const claimToken = async (transferId: string, isNewAccount: boolean) => {
  try {
    const response = await axios.post('/api/auth/token/claim', { transferId });
    const { accessToken } = response.data;
    
    // Store in memory (not localStorage)
    setAccessToken(accessToken);
    
    // Clean up URL
    const url = new URL(window.location.href);
    url.searchParams.delete('token_transfer');
    url.searchParams.delete('new');
    window.history.replaceState({}, '', url.pathname + url.search);
    
    if (isNewAccount) {
      showWelcomeMessage("Welcome to Cadence! Your account has been created.");
    }
  } catch (error) {
    console.error('Failed to claim token', error);
    navigate('/login?error=token_claim_failed');
  }
};
```

## Technical Notes

- **Token Transfer Pattern**: Access tokens shouldn't be in URLs (logs, history). Use a short-lived transfer ID that frontend exchanges for the actual token.
- **State Store**: Must be server-side (not cookies) to prevent tampering
- **One-Time Use**: State must be deleted after use to prevent replay attacks
- **Timing**: Entire callback should complete in <5 seconds to avoid user confusion

---

*Story created: 2025-01-21*
