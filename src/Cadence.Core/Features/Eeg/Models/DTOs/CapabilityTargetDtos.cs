using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Eeg.Models.DTOs;

/// <summary>
/// DTO for creating a new capability target.
/// </summary>
public class CreateCapabilityTargetRequest
{
    /// <summary>
    /// The capability from the organization's library to reference.
    /// </summary>
    public Guid CapabilityId { get; init; }

    /// <summary>
    /// Measurable performance threshold for this capability.
    /// Required, 1-500 characters.
    /// Example: "Establish interoperable communications within 30 minutes"
    /// </summary>
    public string TargetDescription { get; init; } = string.Empty;

    /// <summary>
    /// References to plans, policies, SOPs, or frameworks this target is based on.
    /// Optional, max 500 characters.
    /// Example: "Metro County EOP, Annex F; SOP 5.2; NIMS"
    /// </summary>
    public string? Sources { get; init; }

    /// <summary>
    /// Display order within the exercise's capability targets.
    /// If not specified, will be appended at the end.
    /// </summary>
    public int? SortOrder { get; init; }
}

/// <summary>
/// DTO for updating an existing capability target.
/// </summary>
public class UpdateCapabilityTargetRequest
{
    /// <summary>
    /// Updated target description.
    /// Required, 1-500 characters.
    /// </summary>
    public string TargetDescription { get; init; } = string.Empty;

    /// <summary>
    /// Updated references to plans, policies, SOPs, or frameworks.
    /// Optional, max 500 characters. Set to null to clear.
    /// </summary>
    public string? Sources { get; init; }

    /// <summary>
    /// Updated sort order.
    /// </summary>
    public int? SortOrder { get; init; }
}

/// <summary>
/// DTO for capability target response.
/// </summary>
public record CapabilityTargetDto(
    Guid Id,
    Guid ExerciseId,
    Guid CapabilityId,
    CapabilitySummaryDto Capability,
    string TargetDescription,
    string? Sources,
    int SortOrder,
    int CriticalTaskCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Summary DTO for capability in capability target responses.
/// </summary>
public record CapabilitySummaryDto(
    Guid Id,
    string Name,
    string? Category
);

/// <summary>
/// DTO for capability target list response.
/// </summary>
public record CapabilityTargetListResponse(
    IEnumerable<CapabilityTargetDto> Items,
    int TotalCount
);
