namespace Cadence.Core.Models.Entities;

/// <summary>
/// Phase entity - represents a time segment within an exercise.
/// Phases help organize injects into logical groupings.
/// Implements IOrganizationScoped for automatic organization-based data isolation.
/// </summary>
public class Phase : BaseEntity, IOrganizationScoped
{
    /// <summary>
    /// Phase name (e.g., "Initial Response", "Recovery").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Phase description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order within the exercise.
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Phase start time (wall clock).
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// Phase end time (wall clock).
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    // =========================================================================
    // Foreign Keys
    // =========================================================================

    /// <summary>
    /// The organization this phase belongs to. Required for data isolation.
    /// Denormalized from Exercise for query filter efficiency.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Parent exercise.
    /// </summary>
    public Guid ExerciseId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The exercise this phase belongs to.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// Injects assigned to this phase.
    /// </summary>
    public ICollection<Inject> Injects { get; set; } = new List<Inject>();
}
