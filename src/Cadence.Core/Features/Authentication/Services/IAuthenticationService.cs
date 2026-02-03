using Cadence.Core.Features.Authentication.Models.DTOs;

namespace Cadence.Core.Features.Authentication.Services;

/// <summary>
/// Orchestrates authentication across multiple providers (local Identity, Azure Entra, etc.).
/// All authentication flows ultimately issue Cadence JWTs regardless of original auth method.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate with email/password via Identity provider.
    /// Issues Cadence JWT on success.
    /// </summary>
    /// <param name="request">Login credentials and "remember me" preference.</param>
    /// <param name="ipAddress">IP address of the request (for audit and rate limiting).</param>
    /// <param name="deviceInfo">User agent or device information (for audit).</param>
    /// <returns>Authentication result with JWT tokens or error details.</returns>
    Task<AuthResponse> AuthenticateWithPasswordAsync(
        LoginRequest request,
        string? ipAddress = null,
        string? deviceInfo = null);

    /// <summary>
    /// Complete authentication from external OAuth callback (e.g., Azure Entra).
    /// Creates or links local account as needed, then issues Cadence JWT.
    /// </summary>
    /// <param name="request">OAuth callback data (provider, code, state).</param>
    /// <param name="ipAddress">IP address of the request (for audit).</param>
    /// <param name="deviceInfo">User agent or device information (for audit).</param>
    /// <returns>Authentication result with JWT tokens or error details.</returns>
    Task<AuthResponse> AuthenticateWithExternalAsync(
        ExternalAuthRequest request,
        string? ipAddress = null,
        string? deviceInfo = null);

    /// <summary>
    /// Register a new local account (Identity provider only).
    /// Auto-authenticates on success, returning JWT tokens.
    /// First user becomes Administrator, subsequent users become User role.
    /// </summary>
    /// <param name="request">Registration details (email, password, display name).</param>
    /// <param name="ipAddress">IP address of the request (for audit).</param>
    /// <param name="deviceInfo">User agent or device information (for audit).</param>
    /// <returns>Authentication result with JWT tokens or error details.</returns>
    Task<AuthResponse> RegisterAsync(
        RegistrationRequest request,
        string? ipAddress = null,
        string? deviceInfo = null);

    /// <summary>
    /// Refresh an access token using a refresh token.
    /// Works regardless of original authentication method.
    /// </summary>
    /// <param name="refreshToken">The refresh token (from HttpOnly cookie).</param>
    /// <param name="ipAddress">IP address of the request (for security verification).</param>
    /// <returns>New access token or error if refresh token is invalid/expired.</returns>
    Task<AuthResponse> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress = null);

    /// <summary>
    /// Revoke all tokens for a user (logout from all devices).
    /// </summary>
    /// <param name="userId">User whose tokens should be revoked.</param>
    Task RevokeTokensAsync(string userId);

    /// <summary>
    /// Revoke a specific refresh token (single device logout).
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke.</param>
    Task RevokeTokenAsync(string refreshToken);

    /// <summary>
    /// Get user information by ID.
    /// </summary>
    /// <param name="userId">User's unique identifier.</param>
    /// <returns>User information or null if not found.</returns>
    Task<UserInfo?> GetUserAsync(string userId);

    /// <summary>
    /// Get all enabled authentication methods.
    /// Used by login UI to show available sign-in options.
    /// </summary>
    /// <returns>List of enabled authentication methods.</returns>
    IReadOnlyList<AuthMethod> GetAvailableMethods();

    /// <summary>
    /// Get OAuth redirect URL for external provider.
    /// Frontend redirects user to this URL to initiate OAuth flow.
    /// </summary>
    /// <param name="provider">Provider identifier (e.g., "Entra").</param>
    /// <param name="returnUrl">URL to return to after authentication.</param>
    /// <returns>OAuth authorization URL to redirect user to.</returns>
    string GetExternalLoginUrl(string provider, string returnUrl);

    /// <summary>
    /// Request a password reset for the specified email address.
    /// Always returns success to prevent email enumeration attacks.
    /// </summary>
    /// <param name="email">Email address of the account to reset.</param>
    /// <param name="ipAddress">IP address of the request (for audit and rate limiting).</param>
    /// <returns>Always returns success (for security). The token is logged in development.</returns>
    Task<bool> RequestPasswordResetAsync(string email, string? ipAddress = null);

    /// <summary>
    /// Complete a password reset using the reset token.
    /// Auto-authenticates the user on success.
    /// </summary>
    /// <param name="token">The password reset token from the email link.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <param name="ipAddress">IP address of the request (for audit).</param>
    /// <param name="deviceInfo">User agent or device information (for audit).</param>
    /// <returns>Authentication result with JWT tokens on success, or error details on failure.</returns>
    Task<AuthResponse> ResetPasswordAsync(
        string token,
        string newPassword,
        string? ipAddress = null,
        string? deviceInfo = null);
}
