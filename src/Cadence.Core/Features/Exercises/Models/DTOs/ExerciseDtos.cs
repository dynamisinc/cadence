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

    /// <summary>
    /// Maximum allowed duration for exercise conduct (wall clock time).
    /// Null means no change. Must be positive and &lt;= 336 hours (2 weeks).
    /// </summary>
    public TimeSpan? MaxDuration { get; init; }
}

/// <summary>
/// DTO for exercise settings response.
/// </summary>
public record ExerciseSettingsDto(
    decimal ClockMultiplier,
    bool AutoFireEnabled,
    bool ConfirmFireInject,
    bool ConfirmSkipInject,
    bool ConfirmClockControl,
    TimeSpan? MaxDuration
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
    string CreatedBy,
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
    bool ConfirmClockControl,
    TimeSpan? MaxDuration,
    // Summary counts (for list views)
    int InjectCount = 0,
    int FiredInjectCount = 0
);

/// <summary>
/// Extension methods for mapping between Exercise entity and DTOs.
/// </summary>
public static class ExerciseMapper
{
    /// <summary>
    /// Maps an Exercise entity to ExerciseDto with inject counts = 0.
    /// Use the overload with count parameters for list views.
    /// </summary>
    public static ExerciseDto ToDto(this Exercise entity) => entity.ToDto(0, 0);

    /// <summary>
    /// Maps an Exercise entity to ExerciseDto with the specified inject counts.
    /// </summary>
    public static ExerciseDto ToDto(this Exercise entity, int injectCount, int firedInjectCount) => new(
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
        entity.ConfirmClockControl,
        entity.MaxDuration,
        injectCount,
        firedInjectCount
    );

    public static Exercise ToEntity(this CreateExerciseRequest request, Guid organizationId, string createdBy) => new()
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

// =========================================================================
// Approval Settings DTOs (Inject Approval Workflow)
// =========================================================================

/// <summary>
/// Request DTO for updating exercise approval settings.
/// </summary>
public record UpdateApprovalSettingsRequest(
    bool RequireInjectApproval,
    bool IsOverride = false,
    string? OverrideReason = null
);

/// <summary>
/// Response DTO for exercise approval settings.
/// Includes organization policy context for UI rendering.
/// </summary>
public record ApprovalSettingsDto(
    bool RequireInjectApproval,
    bool ApprovalPolicyOverridden,
    string? ApprovalOverrideReason,
    string? ApprovalOverriddenById,
    DateTime? ApprovalOverriddenAt,
    ApprovalPolicy OrganizationPolicy,
    SelfApprovalPolicy SelfApprovalPolicy
);

/// <summary>
/// Response DTO for exercise approval status summary (S06: Approval Queue View).
/// Shows counts of injects by approval status for queue management.
/// </summary>
public record ApprovalStatusDto(
    int TotalInjects,
    int ApprovedCount,
    int PendingApprovalCount,
    int DraftCount,
    decimal ApprovalPercentage,
    bool AllApproved
);

// =========================================================================
// Exercise Publish Validation DTOs (Go-Live Gate - S07)
// =========================================================================

/// <summary>
/// Result of validating whether an exercise can be published.
/// When approval is enabled, blocks publish if any injects are unapproved (Draft or Submitted).
/// </summary>
public class PublishValidationResult
{
    /// <summary>
    /// Whether the exercise can be published.
    /// False if approval is enabled and unapproved injects exist.
    /// </summary>
    public bool CanPublish { get; set; }

    /// <summary>
    /// Count of injects in Draft status.
    /// </summary>
    public int DraftCount { get; set; }

    /// <summary>
    /// Count of injects in Submitted status (awaiting approval).
    /// </summary>
    public int SubmittedCount { get; set; }

    /// <summary>
    /// Total count of unapproved injects (Draft + Submitted).
    /// </summary>
    public int TotalUnapprovedCount => DraftCount + SubmittedCount;

    /// <summary>
    /// Warning messages that don't block publish (e.g., no injects, all deferred).
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Error messages that block publish (unapproved injects when approval enabled).
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

// =========================================================================
// Approval Permissions DTOs (S11: Configurable Approval Permissions)
// =========================================================================

/// <summary>
/// Result of checking approval permission for a user on an inject.
/// </summary>
public enum ApprovalPermissionResult
{
    /// <summary>User is allowed to approve.</summary>
    Allowed,

    /// <summary>User's role is not authorized to approve.</summary>
    NotAuthorized,

    /// <summary>Self-approval is not permitted by organization policy.</summary>
    SelfApprovalDenied,

    /// <summary>Self-approval is allowed but requires confirmation.</summary>
    SelfApprovalWithWarning
}

/// <summary>
/// Response DTO for organization approval permission settings.
/// </summary>
public record ApprovalPermissionsDto(
    /// <summary>Roles authorized to approve injects (flags enum value).</summary>
    ApprovalRoles AuthorizedRoles,

    /// <summary>Policy for self-approval of injects.</summary>
    SelfApprovalPolicy SelfApprovalPolicy,

    /// <summary>Human-readable list of authorized role names.</summary>
    List<string> AuthorizedRoleNames
);

/// <summary>
/// Request to update organization approval permissions.
/// </summary>
public record UpdateApprovalPermissionsRequest(
    /// <summary>Roles authorized to approve injects (flags enum value).</summary>
    ApprovalRoles AuthorizedRoles,

    /// <summary>Policy for self-approval of injects.</summary>
    SelfApprovalPolicy SelfApprovalPolicy
);

/// <summary>
/// DTO for checking if a user can approve a specific inject.
/// Used by frontend to conditionally show approve/reject buttons.
/// </summary>
public record InjectApprovalCheckDto(
    /// <summary>Whether the user can approve this inject.</summary>
    bool CanApprove,

    /// <summary>The permission result explaining why or why not.</summary>
    ApprovalPermissionResult PermissionResult,

    /// <summary>Whether this is a self-approval attempt.</summary>
    bool IsSelfApproval,

    /// <summary>Whether self-approval requires confirmation dialog.</summary>
    bool RequiresConfirmation,

    /// <summary>Message explaining the permission result.</summary>
    string? Message
);

/// <summary>
/// Extended approve request that includes self-approval confirmation.
/// </summary>
public record ApproveInjectWithConfirmationRequest(
    /// <summary>Optional approver notes.</summary>
    string? Notes,

    /// <summary>Set to true to confirm self-approval when policy allows with warning.</summary>
    bool ConfirmSelfApproval = false
);
