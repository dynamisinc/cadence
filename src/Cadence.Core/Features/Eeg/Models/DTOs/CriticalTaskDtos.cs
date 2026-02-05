namespace Cadence.Core.Features.Eeg.Models.DTOs;

/// <summary>
/// DTO for creating a new critical task.
/// </summary>
public class CreateCriticalTaskRequest
{
    /// <summary>
    /// Specific action required to achieve the capability target.
    /// Required, 1-500 characters.
    /// Example: "Issue EOC activation notification to all stakeholders"
    /// </summary>
    public string TaskDescription { get; init; } = string.Empty;

    /// <summary>
    /// Optional: Conditions and standards for task performance.
    /// Max 1000 characters.
    /// Example: "Per SOP 5.2, using emergency notification system"
    /// </summary>
    public string? Standard { get; init; }

    /// <summary>
    /// Display order within the capability target's tasks.
    /// If not specified, will be appended at the end.
    /// </summary>
    public int? SortOrder { get; init; }
}

/// <summary>
/// DTO for updating an existing critical task.
/// </summary>
public class UpdateCriticalTaskRequest
{
    /// <summary>
    /// Updated task description.
    /// Required, 1-500 characters.
    /// </summary>
    public string TaskDescription { get; init; } = string.Empty;

    /// <summary>
    /// Updated standard.
    /// </summary>
    public string? Standard { get; init; }

    /// <summary>
    /// Updated sort order.
    /// </summary>
    public int? SortOrder { get; init; }
}

/// <summary>
/// DTO for critical task response.
/// </summary>
public record CriticalTaskDto(
    Guid Id,
    Guid CapabilityTargetId,
    string TaskDescription,
    string? Standard,
    int SortOrder,
    int LinkedInjectCount,
    int EegEntryCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO for critical task list response.
/// </summary>
public record CriticalTaskListResponse(
    IEnumerable<CriticalTaskDto> Items,
    int TotalCount
);

/// <summary>
/// DTO for setting linked injects on a critical task.
/// </summary>
public class SetLinkedInjectsRequest
{
    /// <summary>
    /// List of inject IDs to link to this critical task.
    /// Replaces all existing links.
    /// </summary>
    public List<Guid> InjectIds { get; init; } = new();
}

/// <summary>
/// DTO for setting linked critical tasks on an inject.
/// </summary>
public class SetLinkedCriticalTasksRequest
{
    /// <summary>
    /// List of critical task IDs to link to this inject.
    /// Replaces all existing links.
    /// </summary>
    public List<Guid> CriticalTaskIds { get; init; } = new();
}
