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
}
