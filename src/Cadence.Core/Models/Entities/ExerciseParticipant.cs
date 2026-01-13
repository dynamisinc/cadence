namespace Cadence.Core.Models.Entities;

/// <summary>
/// ExerciseParticipant entity - join table linking users to exercises with their role.
/// A user has exactly one role per exercise but can participate in multiple exercises.
/// </summary>
public class ExerciseParticipant
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

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

    /// <summary>
    /// When this participant was added to the exercise.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// User who added this participant.
    /// </summary>
    public Guid AddedBy { get; set; }

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

    /// <summary>
    /// The user who added this participant.
    /// May be null if the user has been soft-deleted.
    /// </summary>
    public User? AddedByUser { get; set; }
}
