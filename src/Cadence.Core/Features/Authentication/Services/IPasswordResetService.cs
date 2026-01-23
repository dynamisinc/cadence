using Cadence.Core.Features.Authentication.Models.DTOs;

namespace Cadence.Core.Features.Authentication.Services;

/// <summary>
/// Service for self-service password reset functionality.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Request a password reset email for a given email address.
    /// Always returns success to prevent email enumeration attacks.
    /// Only sends email if account exists and is active.
    /// </summary>
    /// <param name="email">Email address to send reset link to.</param>
    /// <param name="resetUrl">Base URL for password reset page (token will be appended).</param>
    /// <param name="ipAddress">IP address of requester (for rate limiting and audit).</param>
    /// <returns>Always true (to prevent enumeration). Email sent only if account exists.</returns>
    Task<bool> RequestResetAsync(
        string email,
        string resetUrl,
        string? ipAddress = null);

    /// <summary>
    /// Validate a password reset token.
    /// Checks if token exists, is not expired, and has not been used.
    /// </summary>
    /// <param name="token">The reset token from email link.</param>
    /// <returns>Validation result with user ID if valid, error message otherwise.</returns>
    Task<PasswordResetValidation> ValidateTokenAsync(string token);

    /// <summary>
    /// Complete password reset by setting new password.
    /// Validates token, updates password, revokes all sessions, and invalidates token.
    /// </summary>
    /// <param name="token">The reset token from email link.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <param name="ipAddress">IP address of requester (for audit).</param>
    /// <returns>Authentication response (auto-login after reset) or error.</returns>
    Task<AuthResponse> CompleteResetAsync(
        string token,
        string newPassword,
        string? ipAddress = null);

    /// <summary>
    /// Check if an email address is currently rate limited for password reset requests.
    /// </summary>
    /// <param name="email">Email address to check.</param>
    /// <returns>True if rate limited, false if request allowed.</returns>
    Task<bool> IsRateLimitedAsync(string email);

    /// <summary>
    /// Clean up expired password reset tokens (called by maintenance job).
    /// </summary>
    /// <param name="olderThan">Delete tokens expired before this date.</param>
    /// <returns>Number of tokens deleted.</returns>
    Task<int> CleanupExpiredTokensAsync(DateTime olderThan);
}
