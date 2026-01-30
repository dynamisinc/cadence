namespace Cadence.Core.Models.Entities;

/// <summary>
/// MSEL (Master Scenario Events List) entity - container for injects.
/// An exercise can have multiple MSEL versions, but only one can be active at a time.
/// Implements IOrganizationScoped for automatic organization-based data isolation.
/// </summary>
public class Msel : BaseEntity, IOrganizationScoped
{
    /// <summary>
    /// MSEL name/version identifier (e.g., "v1.0", "Final Draft").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of this MSEL version.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Version number for ordering.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Whether this is the active MSEL for the exercise.
    /// </summary>
    public bool IsActive { get; set; }

    // =========================================================================
    // Foreign Keys
    // =========================================================================

    /// <summary>
    /// The organization this MSEL belongs to. Required for data isolation.
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
    /// The exercise this MSEL belongs to.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// Injects in this MSEL.
    /// </summary>
    public ICollection<Inject> Injects { get; set; } = new List<Inject>();
}
