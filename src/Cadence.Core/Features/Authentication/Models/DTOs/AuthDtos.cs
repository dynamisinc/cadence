namespace Cadence.Core.Features.Authentication.Models.DTOs;

// =========================================================================
// Request DTOs
// =========================================================================

/// <summary>
/// Request to authenticate with email and password.
/// </summary>
public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);

/// <summary>
/// Request to register a new local account.
/// </summary>
public record RegistrationRequest(
    string Email,
    string Password,
    string DisplayName
);

/// <summary>
/// Request to initiate password reset.
/// </summary>
public record PasswordResetRequest(
    string Email
);

/// <summary>
/// Request to complete password reset with token.
/// </summary>
public record PasswordResetCompleteRequest(
    string Token,
    string NewPassword
);

/// <summary>
/// Request to refresh access token.
/// Refresh token comes from HttpOnly cookie.
/// </summary>
public record RefreshTokenRequest();

/// <summary>
/// External OAuth callback data from provider.
/// </summary>
public record ExternalAuthRequest(
    string Provider,
    string Code,
    string State,
    string? ReturnUrl = null
);

// =========================================================================
// Response DTOs
// =========================================================================

/// <summary>
/// Result of authentication operations (login, register, refresh).
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Whether the authentication operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// User's display name.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// User's system-wide role (Administrator, User, etc.).
    /// </summary>
    public string? Role { get; init; }

    /// <summary>
    /// JWT access token for API authorization.
    /// Valid for 15 minutes (or as configured).
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// Should be stored in HttpOnly cookie by the API.
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Access token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Token type (always "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Error details if authentication failed.
    /// </summary>
    public AuthError? Error { get; init; }

    /// <summary>
    /// True if this is a newly registered account (for welcome messaging).
    /// </summary>
    public bool IsNewAccount { get; init; }

    /// <summary>
    /// True if this is the first user in the system (becomes Administrator).
    /// Used to show admin welcome messaging.
    /// </summary>
    public bool IsFirstUser { get; init; }

    /// <summary>
    /// User's current status (Active, Deactivated, etc.).
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Whether "Remember Me" was selected during login.
    /// Used by controller to set appropriate cookie expiration.
    /// </summary>
    public bool RememberMe { get; init; }

    /// <summary>
    /// Refresh token expiration time in seconds (from now).
    /// Used by controller to set cookie expiration to match token expiration.
    /// </summary>
    public int RefreshTokenExpiresIn { get; init; }

    /// <summary>
    /// Creates a successful authentication response.
    /// </summary>
    public static AuthResponse Success(
        Guid userId,
        string displayName,
        string email,
        string role,
        string accessToken,
        string refreshToken,
        int expiresIn,
        string status = "Active",
        bool isNewAccount = false,
        bool isFirstUser = false,
        bool rememberMe = false,
        int refreshTokenExpiresIn = 0)
        => new()
        {
            IsSuccess = true,
            UserId = userId,
            DisplayName = displayName,
            Email = email,
            Role = role,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            Status = status,
            IsNewAccount = isNewAccount,
            IsFirstUser = isFirstUser,
            RememberMe = rememberMe,
            RefreshTokenExpiresIn = refreshTokenExpiresIn
        };

    /// <summary>
    /// Creates a failed authentication response.
    /// </summary>
    public static AuthResponse Failure(AuthError error)
        => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Error details for failed authentication.
/// </summary>
public class AuthError
{
    /// <summary>
    /// Error code (e.g., "invalid_credentials", "account_locked").
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Number of login attempts remaining before lockout (for failed login).
    /// </summary>
    public int? AttemptsRemaining { get; init; }

    /// <summary>
    /// When the account lockout expires (for locked accounts).
    /// </summary>
    public DateTime? LockoutEnd { get; init; }

    /// <summary>
    /// Validation errors by field name (for registration/password reset).
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; init; }

    /// <summary>
    /// Standard error: Invalid email or password.
    /// </summary>
    public static AuthError InvalidCredentials => new()
    {
        Code = "invalid_credentials",
        Message = "Invalid email or password"
    };

    /// <summary>
    /// Standard error: Account is locked due to too many failed attempts.
    /// </summary>
    public static AuthError AccountLocked(DateTime until, int minutesRemaining) => new()
    {
        Code = "account_locked",
        Message = $"Account locked. Try again in {minutesRemaining} minutes.",
        LockoutEnd = until
    };

    /// <summary>
    /// Standard error: Account has been deactivated by administrator.
    /// </summary>
    public static AuthError AccountDeactivated => new()
    {
        Code = "account_deactivated",
        Message = "Account deactivated. Contact administrator."
    };

    /// <summary>
    /// Standard error: Email already registered.
    /// </summary>
    public static AuthError DuplicateEmail => new()
    {
        Code = "duplicate_email",
        Message = "An account with this email already exists"
    };

    /// <summary>
    /// Standard error: Token is invalid or expired.
    /// </summary>
    public static AuthError InvalidToken => new()
    {
        Code = "invalid_token",
        Message = "Token is invalid or expired"
    };

    /// <summary>
    /// Standard error: Authentication provider is disabled.
    /// </summary>
    public static AuthError ProviderDisabled => new()
    {
        Code = "provider_disabled",
        Message = "This authentication method is not enabled"
    };

    /// <summary>
    /// Error for external authentication failures.
    /// </summary>
    public static AuthError ExternalAuthFailed(string reason) => new()
    {
        Code = "external_auth_failed",
        Message = reason
    };

    /// <summary>
    /// Error for validation failures with field-specific errors.
    /// </summary>
    public static AuthError ValidationFailed(Dictionary<string, string[]> errors) => new()
    {
        Code = "validation_error",
        Message = "Validation failed",
        ValidationErrors = errors
    };
}

/// <summary>
/// User information for authenticated user.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// User's system-wide role (Administrator, User, etc.).
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// User's account status (Active, Deactivated, etc.).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// When the user last logged in (for session tracking).
    /// </summary>
    public DateTime? LastLoginAt { get; init; }

    /// <summary>
    /// User's roles within exercises (exercise ID -> role name).
    /// Example: { exerciseId: "Controller" }
    /// </summary>
    public Dictionary<Guid, string>? ExerciseRoles { get; init; }

    /// <summary>
    /// List of linked authentication providers.
    /// Example: ["Identity", "Entra"]
    /// </summary>
    public List<string>? LinkedProviders { get; init; }
}

/// <summary>
/// Available authentication method for UI display.
/// </summary>
public class AuthMethod
{
    /// <summary>
    /// Provider identifier (e.g., "Identity", "Entra").
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// Display name for UI (e.g., "Email &amp; Password", "Sign in with Microsoft").
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Icon identifier for UI (e.g., "microsoft", "email").
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Whether this authentication method is enabled.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Whether this is an external OAuth provider (vs local password).
    /// </summary>
    public bool IsExternal { get; init; }
}

/// <summary>
/// User information from external OAuth provider (before linking to local account).
/// </summary>
public class ExternalUserInfo
{
    /// <summary>
    /// Provider identifier (e.g., "Entra").
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// User's unique identifier from the provider (e.g., Azure AD Object ID).
    /// </summary>
    public string ProviderUserId { get; init; } = string.Empty;

    /// <summary>
    /// User's email from the provider.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's display name from the provider.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Additional claims from the provider.
    /// </summary>
    public Dictionary<string, string>? Claims { get; init; }
}

// =========================================================================
// Token-related DTOs
// =========================================================================

/// <summary>
/// Claims extracted from a JWT access token.
/// </summary>
public class TokenClaims
{
    /// <summary>
    /// User's unique identifier (from "sub" claim).
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// User's email address (from "email" claim).
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's display name (from "name" claim).
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// User's system role (from "role" claim).
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// When the token was issued (from "iat" claim).
    /// </summary>
    public DateTime IssuedAt { get; init; }

    /// <summary>
    /// When the token expires (from "exp" claim).
    /// </summary>
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Information about a stored refresh token.
/// </summary>
public class RefreshTokenInfo
{
    /// <summary>
    /// Refresh token record identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// User who owns this token.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// SHA256 hash of the refresh token (token itself never stored).
    /// </summary>
    public string TokenHash { get; init; } = string.Empty;

    /// <summary>
    /// When the refresh token expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; init; }

    /// <summary>
    /// Whether the user selected "Remember Me" during login.
    /// Affects token expiration duration.
    /// </summary>
    public bool RememberMe { get; init; }

    /// <summary>
    /// IP address where token was issued (for audit).
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Device/user agent string (for audit).
    /// </summary>
    public string? DeviceInfo { get; init; }

    /// <summary>
    /// When the token was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

// =========================================================================
// Password Reset DTOs
// =========================================================================

/// <summary>
/// Result of validating a password reset token.
/// </summary>
public class PasswordResetValidation
{
    /// <summary>
    /// Whether the token is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// User ID associated with the token (if valid).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Error message if token is invalid.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a valid token result.
    /// </summary>
    public static PasswordResetValidation Valid(Guid userId) => new()
    {
        IsValid = true,
        UserId = userId
    };

    /// <summary>
    /// Creates an invalid token result.
    /// </summary>
    public static PasswordResetValidation Invalid(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}
