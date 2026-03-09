namespace Cadence.Core.Features.SystemSettings.Models.DTOs;

/// <summary>
/// Version information response model for the API.
/// </summary>
public record VersionInfo
{
    /// <summary>Semantic version string (e.g., "1.2.0")</summary>
    public required string Version { get; init; }

    /// <summary>Abbreviated git commit SHA (7 characters)</summary>
    public string? CommitSha { get; init; }

    /// <summary>Build timestamp in UTC</summary>
    public DateTime? BuildDate { get; init; }

    /// <summary>Deployment environment (Development, UAT, Production)</summary>
    public required string Environment { get; init; }
}
