using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Exercises.Models.DTOs;

/// <summary>
/// DTO for creating a new exercise (minimal required fields).
/// </summary>
public class CreateExerciseRequest
{
    public string Name { get; init; } = string.Empty;
    public ExerciseType ExerciseType { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public string? Description { get; init; }
    public string? Location { get; init; }
    public string TimeZoneId { get; init; } = "UTC";
    public bool IsPracticeMode { get; init; }
    public DeliveryMode DeliveryMode { get; init; } = DeliveryMode.ClockDriven;
    public TimelineMode TimelineMode { get; init; } = TimelineMode.RealTime;

    /// <summary>
    /// Clock speed multiplier (1, 2, 5, 10, or 20).
    /// Default: 1 (real-time).
    /// </summary>
    public decimal ClockMultiplier { get; init; } = 1.0m;

    /// <summary>
    /// Optional ID of user to assign as Exercise Director.
    /// If not provided, the creator will be auto-assigned if they are Admin or Manager.
    /// Must be an Admin or Manager (SystemRole check).
    /// </summary>
    public string? DirectorId { get; init; }
}

/// <summary>
/// DTO for updating an existing exercise.
/// </summary>
public class UpdateExerciseRequest
{
    public string Name { get; init; } = string.Empty;
    public ExerciseType ExerciseType { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public string? Description { get; init; }
    public string? Location { get; init; }
    public string TimeZoneId { get; init; } = "UTC";
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public bool IsPracticeMode { get; init; }
    public DeliveryMode DeliveryMode { get; init; } = DeliveryMode.ClockDriven;
    public TimelineMode TimelineMode { get; init; } = TimelineMode.RealTime;

    /// <summary>
    /// Clock speed multiplier (1, 2, 5, 10, or 20).
    /// Default: 1 (real-time).
    /// </summary>
    public decimal ClockMultiplier { get; init; } = 1.0m;

    /// <summary>
    /// Optional ID of user to assign as Exercise Director.
    /// If provided, will update the Exercise Director assignment.
    /// Must be an Admin or Manager (SystemRole check).
    /// </summary>
    public string? DirectorId { get; init; }
}

/// <summary>
/// DTO for duplicating an exercise.
/// Optional fields can override the source exercise values.
/// </summary>
public class DuplicateExerciseRequest
{
    /// <summary>
    /// Name for the new exercise. Defaults to "Copy of {original name}".
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Scheduled date for the new exercise. Defaults to the original date.
    /// </summary>
    public DateOnly? ScheduledDate { get; init; }
}

/// <summary>
/// DTO for updating exercise settings (S03-S05).
/// Used by Directors+ to configure exercise behavior.
/// </summary>
public class UpdateExerciseSettingsRequest
{
    /// <summary>
    /// Clock speed multiplier. 1.0 = real-time.
    /// Valid range: 0.5 to 20.0
    /// </summary>
    public decimal? ClockMultiplier { get; init; }

    /// <summary>
    /// Whether injects should automatically fire at scheduled time.
    /// </summary>
    public bool? AutoFireEnabled { get; init; }

    /// <summary>
    /// Whether to show confirmation dialog before firing an inject.
    /// </summary>
    public bool? ConfirmFireInject { get; init; }

    /// <summary>
    /// Whether to show confirmation dialog before skipping an inject.
    /// </summary>
    public bool? ConfirmSkipInject { get; init; }

    /// <summary>
    /// Whether to show confirmation for clock control actions.
    /// </summary>
    public bool? ConfirmClockControl { get; init; }
}

/// <summary>
/// DTO for exercise settings response.
/// </summary>
public record ExerciseSettingsDto(
    decimal ClockMultiplier,
    bool AutoFireEnabled,
    bool ConfirmFireInject,
    bool ConfirmSkipInject,
    bool ConfirmClockControl
);

/// <summary>
/// DTO for exercise response.
/// </summary>
public record ExerciseDto(
    Guid Id,
    string Name,
    string? Description,
    ExerciseType ExerciseType,
    ExerciseStatus Status,
    bool IsPracticeMode,
    DateOnly ScheduledDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string TimeZoneId,
    string? Location,
    Guid OrganizationId,
    Guid? ActiveMselId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid CreatedBy,
    // Status transition audit fields
    DateTime? ActivatedAt,
    string? ActivatedBy,
    DateTime? CompletedAt,
    string? CompletedBy,
    DateTime? ArchivedAt,
    string? ArchivedBy,
    // Archive/delete tracking fields
    bool HasBeenPublished,
    ExerciseStatus? PreviousStatus,
    // Timing configuration fields
    DeliveryMode DeliveryMode,
    TimelineMode TimelineMode,
    decimal? TimeScale,
    // Exercise settings (S03-S05)
    decimal ClockMultiplier,
    bool AutoFireEnabled,
    bool ConfirmFireInject,
    bool ConfirmSkipInject,
    bool ConfirmClockControl
);

/// <summary>
/// Extension methods for mapping between Exercise entity and DTOs.
/// </summary>
public static class ExerciseMapper
{
    public static ExerciseDto ToDto(this Exercise entity) => new(
        entity.Id,
        entity.Name,
        entity.Description,
        entity.ExerciseType,
        entity.Status,
        entity.IsPracticeMode,
        entity.ScheduledDate,
        entity.StartTime,
        entity.EndTime,
        entity.TimeZoneId,
        entity.Location,
        entity.OrganizationId,
        entity.ActiveMselId,
        entity.CreatedAt,
        entity.UpdatedAt,
        entity.CreatedBy,
        entity.ActivatedAt,
        entity.ActivatedBy,
        entity.CompletedAt,
        entity.CompletedBy,
        entity.ArchivedAt,
        entity.ArchivedBy,
        entity.HasBeenPublished,
        entity.PreviousStatus,
        entity.DeliveryMode,
        entity.TimelineMode,
        entity.TimeScale,
        entity.ClockMultiplier,
        entity.AutoFireEnabled,
        entity.ConfirmFireInject,
        entity.ConfirmSkipInject,
        entity.ConfirmClockControl
    );

    public static Exercise ToEntity(this CreateExerciseRequest request, Guid organizationId, Guid createdBy) => new()
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Description = request.Description,
        ExerciseType = request.ExerciseType,
        Status = ExerciseStatus.Draft,
        IsPracticeMode = request.IsPracticeMode,
        ScheduledDate = request.ScheduledDate,
        TimeZoneId = request.TimeZoneId,
        Location = request.Location,
        OrganizationId = organizationId,
        CreatedBy = createdBy,
        ModifiedBy = createdBy,
        DeliveryMode = request.DeliveryMode,
        TimelineMode = request.TimelineMode,
        // ClockMultiplier is the source of truth; TimeScale is kept in sync for backwards compatibility
        ClockMultiplier = request.ClockMultiplier,
        TimeScale = request.ClockMultiplier
    };
}

// =========================================================================
// Delete-related DTOs
// =========================================================================

/// <summary>
/// Reasons why an exercise can be deleted.
/// </summary>
public enum DeleteEligibilityReason
{
    /// <summary>Exercise has never been published (always in Draft) and user is creator or admin.</summary>
    NeverPublished,

    /// <summary>Exercise is archived and user is admin.</summary>
    Archived
}

/// <summary>
/// Reasons why an exercise cannot be deleted.
/// </summary>
public enum CannotDeleteReason
{
    /// <summary>Exercise has been published and is not archived. Must archive first.</summary>
    MustArchiveFirst,

    /// <summary>User is not authorized to delete this exercise.</summary>
    NotAuthorized,

    /// <summary>Exercise not found.</summary>
    NotFound
}

/// <summary>
/// Summary of data that would be deleted with an exercise.
/// </summary>
public record DeleteDataSummary(
    int InjectCount,
    int PhaseCount,
    int ObservationCount,
    int ParticipantCount,
    int ExpectedOutcomeCount,
    int ObjectiveCount,
    int MselCount
);

/// <summary>
/// Response from the delete summary endpoint.
/// Shows whether deletion is allowed and what data would be affected.
/// </summary>
public record DeleteSummaryResponse(
    Guid ExerciseId,
    string ExerciseName,
    bool CanDelete,
    DeleteEligibilityReason? DeleteReason,
    CannotDeleteReason? CannotDeleteReason,
    DeleteDataSummary Summary
);

/// <summary>
/// Result of a delete operation.
/// </summary>
public class DeleteExerciseResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CannotDeleteReason? CannotDeleteReason { get; init; }

    public static DeleteExerciseResult Succeeded() => new() { Success = true };

    public static DeleteExerciseResult Failed(string message, CannotDeleteReason reason) =>
        new() { Success = false, ErrorMessage = message, CannotDeleteReason = reason };
}

// =========================================================================
// Exercise Assignment DTOs (for Profile Menu)
// =========================================================================

/// <summary>
/// DTO representing a user's exercise assignment.
/// Used in profile menu to show all exercises where the user has a role.
/// </summary>
public record ExerciseAssignmentDto(
    Guid ExerciseId,
    string ExerciseName,
    string ExerciseRole,
    DateTime AssignedAt
);
