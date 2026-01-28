namespace Cadence.Core.Features.Capabilities.Models.DTOs;

/// <summary>
/// Response DTO for a capability.
/// </summary>
public record CapabilityDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Description,
    string? Category,
    int SortOrder,
    bool IsActive,
    string? SourceLibrary,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Lightweight DTO for capability selection dropdowns.
/// </summary>
public record CapabilitySummaryDto(
    Guid Id,
    string Name,
    string? Category,
    bool IsActive
);

/// <summary>
/// Request DTO for creating a new capability.
/// </summary>
public class CreateCapabilityRequest
{
    /// <summary>
    /// Display name of the capability. Required, max 200 characters.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Detailed description. Optional, max 1000 characters.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Grouping category. Optional, max 100 characters.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Display order within category. Default 0.
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Source library identifier (FEMA, NATO, NIST, ISO). Optional, max 50 characters.
    /// Null for custom capabilities.
    /// </summary>
    public string? SourceLibrary { get; init; }
}

/// <summary>
/// Request DTO for updating an existing capability.
/// </summary>
public class UpdateCapabilityRequest
{
    /// <summary>
    /// Display name of the capability. Required, max 200 characters.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Detailed description. Optional, max 1000 characters.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Grouping category. Optional, max 100 characters.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Display order within category.
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Whether this capability is active for selection.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
