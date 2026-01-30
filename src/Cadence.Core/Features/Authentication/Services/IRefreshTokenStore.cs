using Cadence.Core.Features.Authentication.Models.DTOs;

namespace Cadence.Core.Features.Authentication.Services;

/// <summary>
/// Result of creating a refresh token.
/// </summary>
/// <param name="Token">The unhashed refresh token string (to be sent to client).</param>
/// <param name="ExpiresIn">Token expiration time in seconds from now.</param>
/// <param name="RememberMe">Whether RememberMe was used.</param>
public record RefreshTokenCreateResult(string Token, int ExpiresIn, bool RememberMe);

/// <summary>
/// Service for refresh token persistence and management.
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>
    /// Create and store a new refresh token for a user.
    /// </summary>
    /// <param name="userId">User who owns this token.</param>
    /// <param name="rememberMe">If true, token expires in 30 days; otherwise 4 hours.</param>
    /// <param name="ipAddress">IP address where token was issued (for audit).</param>
    /// <param name="deviceInfo">Device/user agent string (for audit).</param>
    /// <returns>Result containing the unhashed refresh token and expiration info.</returns>
    Task<RefreshTokenCreateResult> CreateAsync(
        Guid userId,
        bool rememberMe,
        string? ipAddress = null,
        string? deviceInfo = null);

    /// <summary>
    /// Retrieve refresh token information by its hash.
    /// </summary>
    /// <param name="tokenHash">SHA256 hash of the refresh token.</param>
    /// <returns>Token information if found and valid, null otherwise.</returns>
    Task<RefreshTokenInfo?> GetByHashAsync(string tokenHash);

    /// <summary>
    /// Revoke a specific refresh token.
    /// </summary>
    /// <param name="tokenHash">SHA256 hash of the token to revoke.</param>
    Task RevokeAsync(string tokenHash);

    /// <summary>
    /// Revoke all refresh tokens for a user (logout from all devices).
    /// </summary>
    /// <param name="userId">User whose tokens should be revoked.</param>
    Task RevokeAllForUserAsync(Guid userId);

    /// <summary>
    /// Clean up expired or revoked tokens (called by maintenance job).
    /// </summary>
    /// <param name="olderThan">Delete tokens expired before this date.</param>
    /// <returns>Number of tokens deleted.</returns>
    Task<int> CleanupExpiredTokensAsync(DateTime olderThan);
}
