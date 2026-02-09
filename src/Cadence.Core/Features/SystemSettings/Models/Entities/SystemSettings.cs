namespace Cadence.Core.Features.SystemSettings.Models.Entities;

/// <summary>
/// Singleton entity storing platform-wide system settings overrides.
/// Nullable fields mean "use the appsettings.json default".
/// There should be at most one row in this table.
/// </summary>
public class SystemSettings
{
    public Guid Id { get; set; }

    /// <summary>
    /// Override for the support/feedback email address.
    /// When null, falls back to EmailServiceOptions.SupportAddress from appsettings.
    /// </summary>
    public string? SupportAddress { get; set; }

    /// <summary>
    /// Override for the default sender email address (From).
    /// When null, falls back to EmailServiceOptions.DefaultSenderAddress from appsettings.
    /// </summary>
    public string? DefaultSenderAddress { get; set; }

    /// <summary>
    /// Override for the default sender display name.
    /// When null, falls back to EmailServiceOptions.DefaultSenderName from appsettings.
    /// </summary>
    public string? DefaultSenderName { get; set; }

    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
