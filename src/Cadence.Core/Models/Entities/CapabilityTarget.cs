using System.ComponentModel.DataAnnotations;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// Capability Target entity - an exercise-specific measurable performance threshold
/// for a capability. Per HSEEP 2020 Doctrine, capability targets define what "success"
/// looks like for a specific capability within the context of a particular exercise.
///
/// Example: For the "Operational Communications" capability, an exercise-specific target
/// might be "Establish interoperable communications within 30 minutes of EOC activation."
/// </summary>
public class CapabilityTarget : BaseEntity, IOrganizationScoped
{
    // =========================================================================
    // Core Properties
    // =========================================================================

    /// <summary>
    /// Measurable performance threshold for this capability in this exercise.
    /// Required, 1-500 characters.
    /// Example: "Activate EOC within 60 minutes of notification"
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string TargetDescription { get; set; } = string.Empty;

    /// <summary>
    /// Display order within the exercise's capability targets.
    /// Lower numbers appear first.
    /// </summary>
    public int SortOrder { get; set; }

    // =========================================================================
    // Foreign Keys
    // =========================================================================

    /// <summary>
    /// The organization this capability target belongs to. Required for data isolation.
    /// Denormalized from Exercise for query filter efficiency.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The exercise this capability target belongs to. Required.
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// The capability from the organization's library this target references.
    /// </summary>
    public Guid CapabilityId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The organization this capability target belongs to.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// The exercise this capability target belongs to.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// The capability from the organization's library this target references.
    /// </summary>
    public Capability Capability { get; set; } = null!;

    /// <summary>
    /// Critical tasks required to achieve this target.
    /// </summary>
    public ICollection<CriticalTask> CriticalTasks { get; set; } = new List<CriticalTask>();
}
