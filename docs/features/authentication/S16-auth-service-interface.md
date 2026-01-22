# S16: Auth Service Interface Design

## Story

**As a** developer,
**I want** an authentication service that orchestrates multiple providers,
**So that** users can sign in with local credentials OR external providers simultaneously.

## Context

Cadence needs to support hybrid authentication: local email/password (MVP) alongside Azure Entra SSO (future). Rather than switching between providers, both should work together. The `IAuthenticationService` orchestrates requests to the appropriate provider and handles user account linking.

## Acceptance Criteria

- [ ] **Given** the service, **when** I call `AuthenticateWithPasswordAsync`, **then** it routes to Identity provider
- [ ] **Given** the service, **when** I call `AuthenticateWithExternalAsync`, **then** it routes to the appropriate external provider
- [ ] **Given** the service, **when** I call `GetAvailableMethods`, **then** it returns all enabled providers
- [ ] **Given** multiple providers enabled, **when** a user authenticates, **then** Cadence issues its own JWT (not provider tokens)
- [ ] **Given** external auth succeeds, **when** no linked account exists, **then** user linking service is invoked
- [ ] **Given** external auth succeeds, **when** linked account exists, **then** that account is used

## Out of Scope

- Actual provider implementations (separate stories)
- MFA orchestration
- Provider-specific admin settings UI

## Dependencies

None (foundational)

## Service Interface Definition

```csharp
/// <summary>
/// Orchestrates authentication across multiple providers.
/// All authentication flows ultimately issue Cadence JWTs.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate with email/password via Identity provider.
    /// </summary>
    Task<AuthResult> AuthenticateWithPasswordAsync(LoginRequest request);
    
    /// <summary>
    /// Complete authentication from external OAuth callback.
    /// Creates or links local account as needed.
    /// </summary>
    Task<AuthResult> AuthenticateWithExternalAsync(ExternalAuthRequest request);
    
    /// <summary>
    /// Register a new local account (Identity provider only).
    /// </summary>
    Task<AuthResult> RegisterAsync(RegistrationRequest request);
    
    /// <summary>
    /// Refresh an access token using a refresh token.
    /// Works regardless of original auth method.
    /// </summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// Revoke all tokens for a user (logout).
    /// </summary>
    Task RevokeTokensAsync(Guid userId);
    
    /// <summary>
    /// Get user information by ID.
    /// </summary>
    Task<UserInfo?> GetUserAsync(Guid userId);
    
    /// <summary>
    /// Get all enabled authentication methods.
    /// Used by login UI to show available options.
    /// </summary>
    IReadOnlyList<AuthMethod> GetAvailableMethods();
    
    /// <summary>
    /// Get OAuth redirect URL for external provider.
    /// </summary>
    string GetExternalLoginUrl(string provider, string returnUrl);
}

/// <summary>
/// Available authentication method for UI display.
/// </summary>
public class AuthMethod
{
    public string Provider { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsExternal { get; init; }
}

/// <summary>
/// External OAuth callback data.
/// </summary>
public record ExternalAuthRequest(
    string Provider,
    string Code,
    string State,
    string? ReturnUrl = null
);

/// <summary>
/// Result of authentication operations.
/// </summary>
public class AuthResult
{
    public bool IsSuccess { get; init; }
    public UserInfo? User { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public AuthError? Error { get; init; }
    public bool IsNewAccount { get; init; }  // True if account was just created via external login
    
    public static AuthResult Success(UserInfo user, string accessToken, string refreshToken, int expiresIn, bool isNew = false)
        => new() { IsSuccess = true, User = user, AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = expiresIn, IsNewAccount = isNew };
    
    public static AuthResult Failure(AuthError error)
        => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Provider-agnostic authentication error.
/// </summary>
public class AuthError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, string[]>? ValidationErrors { get; init; }
    
    public static AuthError InvalidCredentials => new() { Code = "invalid_credentials", Message = "Invalid email or password" };
    public static AuthError AccountLocked(DateTime until) => new() { Code = "account_locked", Message = $"Account locked until {until:g}" };
    public static AuthError AccountDeactivated => new() { Code = "account_deactivated", Message = "Account deactivated. Contact administrator." };
    public static AuthError DuplicateEmail => new() { Code = "duplicate_email", Message = "An account with this email already exists" };
    public static AuthError InvalidToken => new() { Code = "invalid_token", Message = "Token is invalid or expired" };
    public static AuthError ProviderDisabled => new() { Code = "provider_disabled", Message = "This authentication method is not enabled" };
    public static AuthError ExternalAuthFailed(string reason) => new() { Code = "external_auth_failed", Message = reason };
}

/// <summary>
/// User information returned from authentication.
/// </summary>
public class UserInfo
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public Dictionary<Guid, string>? ExerciseRoles { get; init; }
    public List<string>? LinkedProviders { get; init; }  // ["Identity", "Entra"]
}

/// <summary>
/// Login request for password authentication.
/// </summary>
public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);

/// <summary>
/// Registration request for new local account.
/// </summary>
public record RegistrationRequest(
    string Email,
    string Password,
    string DisplayName
);
```

## Provider Interface (Internal)

```csharp
/// <summary>
/// Individual authentication provider implementation.
/// Used internally by IAuthenticationService.
/// </summary>
internal interface IAuthenticationProvider
{
    string ProviderType { get; }
    bool IsEnabled { get; }
    bool SupportsPasswordAuth { get; }
    bool SupportsRegistration { get; }
    
    Task<ProviderAuthResult> AuthenticateAsync(object request);
    Task<ExternalUserInfo?> GetExternalUserAsync(string code, string state);
}

/// <summary>
/// User info from external provider (before linking to local account).
/// </summary>
public class ExternalUserInfo
{
    public string Provider { get; init; } = string.Empty;
    public string ProviderUserId { get; init; } = string.Empty;  // Object ID for Entra
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public Dictionary<string, string>? Claims { get; init; }
}
```

## Configuration Model

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
        "Enabled": false,
        "TenantId": "",
        "ClientId": "",
        "ClientSecret": "",
        "Instance": "https://login.microsoftonline.com/",
        "CallbackPath": "/api/auth/callback/entra",
        "AutoLinkByEmail": true,
        "DefaultRole": "Observer",
        "AllowedDomains": []
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

## Service Implementation Skeleton

```csharp
public class HybridAuthenticationService : IAuthenticationService
{
    private readonly IdentityAuthenticationProvider _identityProvider;
    private readonly EntraAuthenticationProvider? _entraProvider;
    private readonly IUserLinkingService _linkingService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly AuthenticationOptions _options;

    public IReadOnlyList<AuthMethod> GetAvailableMethods()
    {
        var methods = new List<AuthMethod>();
        
        if (_options.Providers.Identity.Enabled)
        {
            methods.Add(new AuthMethod
            {
                Provider = "Identity",
                DisplayName = "Email & Password",
                IsEnabled = true,
                IsExternal = false
            });
        }
        
        if (_options.Providers.Entra?.Enabled == true)
        {
            methods.Add(new AuthMethod
            {
                Provider = "Entra",
                DisplayName = "Sign in with Microsoft",
                Icon = "microsoft",
                IsEnabled = true,
                IsExternal = true
            });
        }
        
        return methods;
    }

    public async Task<AuthResult> AuthenticateWithPasswordAsync(LoginRequest request)
    {
        if (!_options.Providers.Identity.Enabled)
            return AuthResult.Failure(AuthError.ProviderDisabled);
            
        return await _identityProvider.AuthenticateAsync(request);
    }

    public async Task<AuthResult> AuthenticateWithExternalAsync(ExternalAuthRequest request)
    {
        // Get appropriate provider
        var provider = request.Provider switch
        {
            "Entra" => _entraProvider as IAuthenticationProvider,
            _ => null
        };
        
        if (provider == null || !provider.IsEnabled)
            return AuthResult.Failure(AuthError.ProviderDisabled);
        
        // Get external user info
        var externalUser = await provider.GetExternalUserAsync(request.Code, request.State);
        if (externalUser == null)
            return AuthResult.Failure(AuthError.ExternalAuthFailed("Could not retrieve user info"));
        
        // Link or create local account
        var (localUser, isNew) = await _linkingService.GetOrCreateLinkedUserAsync(externalUser);
        
        // Issue Cadence tokens
        return await GenerateTokensAsync(localUser, rememberMe: false, isNew);
    }

    // ... other methods
}
```

## Technical Notes

- `IAuthenticationService` is the public contract used by controllers
- `IAuthenticationProvider` is internal, used only by the service
- All auth methods result in Cadence-issued JWTs (consistent offline behavior)
- External providers never directly issue tokens to the client
- User linking happens transparently on external auth

---

*Story created: 2025-01-21*
*Updated: 2025-01-21 - Changed from single provider to orchestrator pattern*
