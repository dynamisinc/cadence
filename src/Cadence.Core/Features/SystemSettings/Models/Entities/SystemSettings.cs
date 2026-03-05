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

    // ── GitHub Integration ──

    /// <summary>Personal Access Token for GitHub API. Null = not configured.</summary>
    public string? GitHubToken { get; set; }

    /// <summary>GitHub repository owner (user or org login).</summary>
    public string? GitHubOwner { get; set; }

    /// <summary>GitHub repository name.</summary>
    public string? GitHubRepo { get; set; }

    /// <summary>When true, automatically create GitHub issues for feedback and apply type labels.</summary>
    public bool GitHubLabelsEnabled { get; set; }

    // ── EULA ──

    /// <summary>EULA content in Markdown format. Null = no EULA configured.</summary>
    public string? EulaContent { get; set; }

    /// <summary>Version identifier for the current EULA (e.g. "1.0"). Null = no EULA.</summary>
    public string? EulaVersion { get; set; }

    /// <summary>When the EULA was last updated.</summary>
    public DateTime? EulaUpdatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
