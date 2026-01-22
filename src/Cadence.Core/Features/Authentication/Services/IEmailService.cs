namespace Cadence.Core.Features.Authentication.Services;

/// <summary>
/// Service for sending authentication-related emails via Azure Communication Services.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send password reset email with reset link.
    /// </summary>
    /// <param name="email">Recipient's email address.</param>
    /// <param name="displayName">Recipient's display name (for personalization).</param>
    /// <param name="resetUrl">Full URL to password reset page with token.</param>
    /// <returns>True if email sent successfully, false otherwise.</returns>
    Task<bool> SendPasswordResetEmailAsync(
        string email,
        string displayName,
        string resetUrl);

    /// <summary>
    /// Send welcome email to newly registered user.
    /// </summary>
    /// <param name="email">Recipient's email address.</param>
    /// <param name="displayName">Recipient's display name.</param>
    /// <returns>True if email sent successfully, false otherwise.</returns>
    Task<bool> SendWelcomeEmailAsync(
        string email,
        string displayName);

    /// <summary>
    /// Send email notification when user's account is deactivated.
    /// </summary>
    /// <param name="email">Recipient's email address.</param>
    /// <param name="displayName">Recipient's display name.</param>
    /// <returns>True if email sent successfully, false otherwise.</returns>
    Task<bool> SendAccountDeactivatedEmailAsync(
        string email,
        string displayName);

    /// <summary>
    /// Send email notification when user's account is reactivated.
    /// </summary>
    /// <param name="email">Recipient's email address.</param>
    /// <param name="displayName">Recipient's display name.</param>
    /// <returns>True if email sent successfully, false otherwise.</returns>
    Task<bool> SendAccountReactivatedEmailAsync(
        string email,
        string displayName);
}
