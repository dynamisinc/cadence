namespace Cadence.Core.Features.SystemSettings.Models.DTOs;

/// <summary>
/// DTO returned from the system settings API.
/// Contains both the raw override values and the effective (resolved) values.
/// </summary>
public class SystemSettingsDto
{
    public Guid? Id { get; set; }

    // Override values (null = using default)
    public string? SupportAddress { get; set; }
    public string? DefaultSenderAddress { get; set; }
    public string? DefaultSenderName { get; set; }

    // Effective values (override ?? appsettings default)
    public string EffectiveSupportAddress { get; set; } = string.Empty;
    public string EffectiveDefaultSenderAddress { get; set; } = string.Empty;
    public string EffectiveDefaultSenderName { get; set; } = string.Empty;

    // GitHub integration
    public string? GitHubOwner { get; set; }
    public string? GitHubRepo { get; set; }
    public bool GitHubLabelsEnabled { get; set; }
    /// <summary>Masked token (last 4 chars only). Null if not configured.</summary>
    public string? GitHubTokenMasked { get; set; }
    /// <summary>True if a GitHub token is stored.</summary>
    public bool GitHubTokenConfigured { get; set; }

    // EULA
    public string? EulaContent { get; set; }
    public string? EulaVersion { get; set; }
    public DateTime? EulaUpdatedAt { get; set; }
    public bool EulaConfigured { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Request to update system settings overrides.
/// Set a field to null or empty string to clear the override and revert to default.
/// </summary>
public class UpdateSystemSettingsRequest
{
    public string? SupportAddress { get; set; }
    public string? DefaultSenderAddress { get; set; }
    public string? DefaultSenderName { get; set; }

    // GitHub integration
    /// <summary>New token. Null/empty = no change. "__clear__" = remove stored token.</summary>
    public string? GitHubToken { get; set; }
    public string? GitHubOwner { get; set; }
    public string? GitHubRepo { get; set; }
    public bool? GitHubLabelsEnabled { get; set; }

    // EULA
    /// <summary>Markdown content for the EULA. Null/empty = remove EULA.</summary>
    public string? EulaContent { get; set; }
    /// <summary>Version identifier. Required when EulaContent is provided.</summary>
    public string? EulaVersion { get; set; }
}
