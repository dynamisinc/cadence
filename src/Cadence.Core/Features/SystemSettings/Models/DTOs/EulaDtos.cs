namespace Cadence.Core.Features.SystemSettings.Models.DTOs;

/// <summary>
/// Returned from GET /api/eula/status — tells the frontend whether the user needs to accept.
/// </summary>
public class EulaStatusDto
{
    /// <summary>True if a EULA is configured and the user has not accepted the current version.</summary>
    public bool Required { get; set; }

    /// <summary>Current EULA version (null if none configured).</summary>
    public string? Version { get; set; }

    /// <summary>Markdown content (only populated when Required is true).</summary>
    public string? Content { get; set; }
}

/// <summary>
/// Request body for POST /api/eula/accept.
/// </summary>
public class AcceptEulaRequest
{
    /// <summary>The version the user is accepting.</summary>
    public string Version { get; set; } = string.Empty;
}
