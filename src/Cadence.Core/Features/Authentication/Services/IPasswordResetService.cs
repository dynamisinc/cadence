using Cadence.Core.Features.Authentication.Models.DTOs;

namespace Cadence.Core.Features.Authentication.Services;

/// <summary>
/// Handles password reset flows: requesting a reset email and completing the reset
/// using the issued token. Extracted from <see cref="IAuthenticationService"/> so
/// that each service has a single, well-defined responsibility.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Request a password reset for the specified email address.
    /// Always returns success to prevent email enumeration attacks.
    /// </summary>
    /// <param name="email">Email address of the account to reset.</param>
    /// <param name="ipAddress">IP address of the request (for audit and rate limiting).</param>
    /// <returns>Always returns true (for security). The token is logged when no email service is configured.</returns>
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

    /// <summary>
    /// Validate a password reset token. Checks if token exists, is not expired,
    /// and has not been used.
    /// </summary>
    /// <param name="token">The password reset token to validate.</param>
    /// <returns>True if the token is valid; false otherwise.</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Check whether the specified email is currently rate-limited for password reset requests.
    /// Should be called before issuing a new reset token.
    /// </summary>
    /// <param name="email">Email address to check.</param>
    /// <returns>True if rate-limited (should not issue a new token); false otherwise.</returns>
    Task<bool> IsRateLimitedAsync(string email);

    /// <summary>
    /// Remove expired and used password reset tokens older than the specified date.
    /// Intended to be called by a maintenance/cleanup Azure Function on a schedule.
    /// </summary>
    /// <param name="olderThan">Remove tokens created before this date.</param>
    /// <returns>The number of tokens removed.</returns>
    Task<int> CleanupExpiredTokensAsync(DateTime olderThan);
}
