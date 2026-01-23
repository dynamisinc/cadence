namespace Cadence.Core.Models.Entities;

/// <summary>
/// ExerciseParticipant entity - join table linking users to exercises with their role.
/// A user has exactly one role per exercise but can participate in multiple exercises.
/// Inherits from BaseEntity for consistent audit trail and soft delete support.
/// </summary>
public class ExerciseParticipant : BaseEntity
{
    /// <summary>
    /// The exercise this participation is for.
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// The user participating (references ApplicationUser, not the deprecated User table).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The user's HSEEP role in this specific exercise.
    /// This is distinct from the user's SystemRole which grants application-level permissions.
    /// </summary>
    public ExerciseRole Role { get; set; }

    /// <summary>
    /// When this participant was assigned to the exercise.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who assigned this participant (for audit trail).
    /// </summary>
    public string? AssignedById { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The exercise.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// The participating user from ApplicationUser (ASP.NET Core Identity).
    /// May be null if the user has been deactivated.
    /// For historical reports, use IgnoreQueryFilters() if needed.
    /// </summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// The user who assigned this participant (for audit).
    /// </summary>
    public ApplicationUser? AssignedBy { get; set; }
}
