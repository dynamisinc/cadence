namespace Cadence.Core.Features.Authentication.Models;

/// <summary>
/// Configuration options for authentication system.
/// </summary>
public class AuthenticationOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Authentication";

    /// <summary>
    /// Identity provider configuration (local password authentication).
    /// </summary>
    public IdentityProviderOptions Identity { get; set; } = new();
}

/// <summary>
/// Configuration for ASP.NET Core Identity provider.
/// </summary>
public class IdentityProviderOptions
{
    /// <summary>
    /// Whether Identity provider is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether new user registration is allowed.
    /// </summary>
    public bool AllowRegistration { get; set; } = true;

    /// <summary>
    /// Minimum password length.
    /// </summary>
    public int PasswordMinLength { get; set; } = 8;

    /// <summary>
    /// Whether password must contain at least one digit.
    /// </summary>
    public bool PasswordRequireDigit { get; set; } = true;

    /// <summary>
    /// Whether password must contain at least one uppercase letter.
    /// </summary>
    public bool PasswordRequireUppercase { get; set; } = true;

    /// <summary>
    /// Whether password must contain at least one lowercase letter.
    /// </summary>
    public bool PasswordRequireLowercase { get; set; } = true;

    /// <summary>
    /// Whether password must contain at least one non-alphanumeric character.
    /// </summary>
    public bool PasswordRequireNonAlphanumeric { get; set; } = false;

    /// <summary>
    /// Maximum failed login attempts before account lockout.
    /// </summary>
    public int LockoutMaxAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes.
    /// </summary>
    public int LockoutMinutes { get; set; } = 15;
}
