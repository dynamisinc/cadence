namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Configuration options for the email service.
/// Bound from the "Email" section of appsettings.json.
/// </summary>
public class EmailServiceOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// The email provider to use. Supported values: "AzureCommunicationServices", "Logging".
    /// </summary>
    public string Provider { get; set; } = "Logging";

    /// <summary>
    /// ACS connection string. Required when Provider is "AzureCommunicationServices".
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Default sender email address (e.g., "noreply@cadence-app.com").
    /// </summary>
    public string DefaultSenderAddress { get; set; } = "noreply@cadence-app.com";

    /// <summary>
    /// Default sender display name.
    /// </summary>
    public string DefaultSenderName { get; set; } = "Cadence";

    /// <summary>
    /// Support email address for Reply-To headers.
    /// </summary>
    public string SupportAddress { get; set; } = "support@cadence-app.com";
}
