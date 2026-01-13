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
    /// The user participating.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user's role in this exercise.
    /// </summary>
    public ExerciseRole Role { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The exercise.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// The participating user.
    /// May be null if the user has been soft-deleted.
    /// For historical reports, use IgnoreQueryFilters() to include deleted users.
    /// </summary>
    public User? User { get; set; }
}
