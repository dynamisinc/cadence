namespace Cadence.Core.Models.Entities;

/// <summary>
/// Approval notification entity - stores notifications specifically for inject approval workflow.
/// Separate from general Notification entity to support approval-specific fields and batch operations.
/// </summary>
public class ApprovalNotification : BaseEntity, IOrganizationScoped
{
    /// <summary>
    /// The organization this notification belongs to.
    /// Inherited from the exercise's organization.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The user who should receive this notification (Exercise Director).
    /// References ApplicationUser.Id (string).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Exercise this notification relates to.
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// Inject this notification relates to (null for batch notifications).
    /// </summary>
    public Guid? InjectId { get; set; }

    /// <summary>
    /// Type of approval notification.
    /// </summary>
    public ApprovalNotificationType Type { get; set; }

    /// <summary>
    /// Short title for the notification (displayed prominently).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Longer message with details.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// JSON metadata for batch notifications (e.g., inject IDs, counts).
    /// Stores structured data about multiple injects when Type is BatchSubmitted.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// User who triggered this notification (Controller who submitted inject).
    /// References ApplicationUser.Id (string).
    /// Null for system-generated notifications.
    /// </summary>
    public string? TriggeredByUserId { get; set; }

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
    /// The organization this notification belongs to.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// The user who receives this notification.
    /// </summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// The exercise this notification relates to.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// The inject this notification relates to (if single inject notification).
    /// </summary>
    public Inject? Inject { get; set; }

    /// <summary>
    /// The user who triggered this notification (e.g., Controller who submitted).
    /// </summary>
    public ApplicationUser? TriggeredByUser { get; set; }
}

/// <summary>
/// Types of approval notifications.
/// </summary>
public enum ApprovalNotificationType
{
    /// <summary>Single inject submitted for approval.</summary>
    InjectSubmitted,

    /// <summary>Multiple injects submitted together (batch operation).</summary>
    BatchSubmitted,

    /// <summary>Inject was approved.</summary>
    InjectApproved,

    /// <summary>Inject was rejected.</summary>
    InjectRejected,

    /// <summary>Inject approval was reverted.</summary>
    InjectReverted
}
