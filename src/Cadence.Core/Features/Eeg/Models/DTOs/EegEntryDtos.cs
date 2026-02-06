using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Eeg.Models.DTOs;

/// <summary>
/// DTO for creating a new EEG entry.
/// </summary>
public class CreateEegEntryRequest
{
    /// <summary>
    /// The critical task this entry assesses. Required.
    /// </summary>
    public Guid CriticalTaskId { get; init; }

    /// <summary>
    /// The observation/assessment text. Required, 1-4000 characters.
    /// </summary>
    public string ObservationText { get; init; } = string.Empty;

    /// <summary>
    /// HSEEP P/S/M/U performance rating. Required.
    /// </summary>
    public PerformanceRating Rating { get; init; }

    /// <summary>
    /// When the task performance was observed.
    /// Defaults to current time if not specified.
    /// </summary>
    public DateTime? ObservedAt { get; init; }

    /// <summary>
    /// Optional: The inject that triggered this observation.
    /// </summary>
    public Guid? TriggeringInjectId { get; init; }
}

/// <summary>
/// DTO for updating an existing EEG entry.
/// </summary>
public class UpdateEegEntryRequest
{
    /// <summary>
    /// Updated observation text. Required, 1-4000 characters.
    /// </summary>
    public string ObservationText { get; init; } = string.Empty;

    /// <summary>
    /// Updated P/S/M/U rating.
    /// </summary>
    public PerformanceRating Rating { get; init; }

    /// <summary>
    /// Updated observation time.
    /// </summary>
    public DateTime? ObservedAt { get; init; }

    /// <summary>
    /// Updated triggering inject.
    /// </summary>
    public Guid? TriggeringInjectId { get; init; }
}

/// <summary>
/// DTO for EEG entry response.
/// </summary>
public record EegEntryDto(
    Guid Id,
    Guid CriticalTaskId,
    CriticalTaskSummaryDto CriticalTask,
    string ObservationText,
    PerformanceRating Rating,
    string RatingDisplay,
    DateTime ObservedAt,
    DateTime RecordedAt,
    string EvaluatorId,
    string? EvaluatorName,
    Guid? TriggeringInjectId,
    InjectSummaryDto? TriggeringInject,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    /// <summary>
    /// Whether this entry has been edited since creation.
    /// True if UpdatedAt is significantly after CreatedAt.
    /// </summary>
    bool WasEdited,
    /// <summary>
    /// Information about who last edited this entry.
    /// Only populated when WasEdited is true.
    /// </summary>
    UserSummaryDto? UpdatedBy
);

/// <summary>
/// Summary DTO for critical task in EEG entry responses.
/// </summary>
public record CriticalTaskSummaryDto(
    Guid Id,
    string TaskDescription,
    /// <summary>
    /// The performance standard for this critical task.
    /// </summary>
    string? Standard,
    Guid CapabilityTargetId,
    string CapabilityTargetDescription,
    /// <summary>
    /// The sources for the capability target (plans, SOPs, frameworks).
    /// </summary>
    string? CapabilityTargetSources,
    string CapabilityName
);

/// <summary>
/// Summary DTO for inject in EEG entry responses.
/// </summary>
public record InjectSummaryDto(
    Guid Id,
    int InjectNumber,
    string Title
);

/// <summary>
/// Summary DTO for user references in EEG entry responses.
/// </summary>
public record UserSummaryDto(
    string Id,
    string Name
);

/// <summary>
/// Query parameters for EEG entry list endpoint.
/// </summary>
public class EegEntryQueryParams
{
    /// <summary>
    /// Page number (1-indexed). Default: 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Items per page (max: 100). Default: 20.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Filter by ratings (P, S, M, U). Comma-separated.
    /// </summary>
    public string? Rating { get; set; }

    /// <summary>
    /// Filter by evaluator IDs. Comma-separated.
    /// </summary>
    public string? EvaluatorId { get; set; }

    /// <summary>
    /// Filter by capability target ID.
    /// </summary>
    public Guid? CapabilityTargetId { get; set; }

    /// <summary>
    /// Filter by critical task ID.
    /// </summary>
    public Guid? CriticalTaskId { get; set; }

    /// <summary>
    /// Filter entries observed after this time.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter entries observed before this time.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Sort field: observedAt, recordedAt, rating. Default: observedAt.
    /// </summary>
    public string SortBy { get; set; } = "observedAt";

    /// <summary>
    /// Sort direction: asc, desc. Default: desc.
    /// </summary>
    public string SortOrder { get; set; } = "desc";

    /// <summary>
    /// Free-text search in observation text.
    /// </summary>
    public string? Search { get; set; }
}

/// <summary>
/// DTO for EEG entry list response with pagination.
/// </summary>
public record EegEntryListResponse(
    IEnumerable<EegEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>
/// DTO for EEG coverage statistics.
/// </summary>
public record EegCoverageDto(
    int TotalTasks,
    int EvaluatedTasks,
    decimal CoveragePercentage,
    Dictionary<PerformanceRating, int> RatingDistribution,
    IEnumerable<CapabilityTargetCoverageDto> ByCapabilityTarget,
    IEnumerable<UnevaluatedTaskDto> UnevaluatedTasks
);

/// <summary>
/// DTO for capability target coverage in EEG coverage response.
/// </summary>
public record CapabilityTargetCoverageDto(
    Guid Id,
    string TargetDescription,
    string CapabilityName,
    int TotalTasks,
    int EvaluatedTasks,
    IEnumerable<TaskRatingDto> TaskRatings
);

/// <summary>
/// DTO for task rating summary.
/// </summary>
public record TaskRatingDto(
    Guid TaskId,
    string TaskDescription,
    PerformanceRating? LatestRating
);

/// <summary>
/// DTO for unevaluated tasks.
/// </summary>
public record UnevaluatedTaskDto(
    Guid TaskId,
    string TaskDescription,
    Guid CapabilityTargetId,
    string CapabilityTargetDescription
);

/// <summary>
/// Helper class for performance rating display names.
/// </summary>
public static class PerformanceRatingExtensions
{
    public static string ToDisplayString(this PerformanceRating rating) => rating switch
    {
        PerformanceRating.Performed => "P - Performed without Challenges",
        PerformanceRating.SomeChallenges => "S - Performed with Some Challenges",
        PerformanceRating.MajorChallenges => "M - Performed with Major Challenges",
        PerformanceRating.UnableToPerform => "U - Unable to be Performed",
        _ => rating.ToString()
    };

    public static string ToShortDisplayString(this PerformanceRating rating) => rating switch
    {
        PerformanceRating.Performed => "P",
        PerformanceRating.SomeChallenges => "S",
        PerformanceRating.MajorChallenges => "M",
        PerformanceRating.UnableToPerform => "U",
        _ => rating.ToString()
    };
}
