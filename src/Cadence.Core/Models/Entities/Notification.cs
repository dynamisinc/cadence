namespace Cadence.Core.Models.Entities;

/// <summary>
/// Notification entity - stores user notifications for events across the platform.
/// Notifications are user-specific and can be marked as read.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>
    /// The user who should receive this notification.
    /// References ApplicationUser.Id (string).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification (determines icon and routing).
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Priority level (determines toast behavior).
    /// </summary>
    public NotificationPriority Priority { get; set; }

    /// <summary>
    /// Short title for the notification (displayed prominently).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Longer message with details.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL to navigate to when notification is clicked.
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Type of related entity (e.g., "Exercise", "Inject").
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// ID of related entity for context.
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Whether the user has read this notification.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// When the notification was read by the user.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The user who receives this notification.
    /// </summary>
    public ApplicationUser? User { get; set; }
}

/// <summary>
/// Types of notifications that can be created.
/// </summary>
public enum NotificationType
{
    /// <summary>An inject is ready to be fired (for Controllers).</summary>
    InjectReady,

    /// <summary>An inject has been fired (for Evaluators).</summary>
    InjectFired,

    /// <summary>Exercise clock has started.</summary>
    ClockStarted,

    /// <summary>Exercise clock has been paused.</summary>
    ClockPaused,

    /// <summary>Exercise has been completed.</summary>
    ExerciseCompleted,

    /// <summary>User has been assigned to an exercise.</summary>
    AssignmentCreated,

    /// <summary>A new observation has been recorded.</summary>
    ObservationCreated,

    /// <summary>Exercise status has changed.</summary>
    ExerciseStatusChanged,

    /// <summary>Generic system notification.</summary>
    System
}

/// <summary>
/// Priority levels for notifications (affects toast behavior).
/// </summary>
public enum NotificationPriority
{
    /// <summary>Low priority - no toast, bell only. Auto-dismiss: N/A.</summary>
    Low,

    /// <summary>Medium priority - shows toast. Auto-dismiss: 10 seconds.</summary>
    Medium,

    /// <summary>High priority - shows toast. Auto-dismiss: never (manual close required).</summary>
    High
}
