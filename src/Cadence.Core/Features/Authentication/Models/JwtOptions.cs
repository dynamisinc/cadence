namespace Cadence.Core.Features.Authentication.Models;

/// <summary>
/// Configuration options for JWT token generation and validation.
/// Bind this to the "Jwt" section in appsettings.json.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// JWT issuer claim (iss).
    /// Identifies who issued the token.
    /// </summary>
    public string Issuer { get; set; } = "Cadence";

    /// <summary>
    /// JWT audience claim (aud).
    /// Identifies who the token is intended for.
    /// </summary>
    public string Audience { get; set; } = "Cadence";

    /// <summary>
    /// Secret key used to sign and validate JWTs.
    /// CRITICAL: Store this in Azure Key Vault or user secrets, NOT in appsettings.json.
    /// Minimum 32 characters for HS256.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Access token lifetime in minutes.
    /// Default: 15 minutes (short-lived for security).
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>
    /// Standard refresh token lifetime in hours.
    /// Default: 4 hours (when RememberMe is false).
    /// </summary>
    public int RefreshTokenHours { get; set; } = 4;

    /// <summary>
    /// Remember Me refresh token lifetime in days.
    /// Default: 30 days (when RememberMe is true).
    /// </summary>
    public int RememberMeDays { get; set; } = 30;
}
