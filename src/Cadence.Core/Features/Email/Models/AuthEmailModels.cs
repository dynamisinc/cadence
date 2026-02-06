namespace Cadence.Core.Features.Email.Models;

/// <summary>
/// Template model for password reset emails.
/// </summary>
public class PasswordResetEmailModel
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ResetUrl { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
}

/// <summary>
/// Template model for password changed confirmation emails.
/// </summary>
public class PasswordChangedEmailModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string ChangeMethod { get; set; } = string.Empty;
    public string ResetPasswordUrl { get; set; } = string.Empty;
    public string SupportUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for account verification emails.
/// </summary>
public class AccountVerificationEmailModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string VerificationUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Template model for new device login alert emails.
/// </summary>
public class NewDeviceAlertEmailModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string? ApproximateLocation { get; set; }
    public DateTime SignInTime { get; set; }
    public string SecureAccountUrl { get; set; } = string.Empty;
    public string TrustDeviceUrl { get; set; } = string.Empty;
}
