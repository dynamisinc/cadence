using System.ComponentModel.DataAnnotations;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// Critical Task entity - a specific action required to achieve a Capability Target.
/// Per HSEEP 2020 Doctrine, critical tasks are the observable, assessable activities
/// that demonstrate capability performance during exercise conduct.
///
/// Example: Under a Capability Target of "Activate EOC within 60 minutes", critical tasks
/// might include "Issue EOC activation notification", "Staff EOC positions per roster", etc.
/// </summary>
public class CriticalTask : BaseEntity, IOrganizationScoped
{
    // =========================================================================
    // Core Properties
    // =========================================================================

    /// <summary>
    /// Specific action required to achieve the capability target.
    /// Required, 1-500 characters.
    /// Example: "Issue EOC activation notification to all stakeholders"
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string TaskDescription { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Conditions and standards for task performance.
    /// Max 1000 characters.
    /// Example: "Per SOP 5.2, using emergency notification system"
    /// </summary>
    [MaxLength(1000)]
    public string? Standard { get; set; }

    /// <summary>
    /// Display order within the capability target's tasks.
    /// Lower numbers appear first.
    /// </summary>
    public int SortOrder { get; set; }

    // =========================================================================
    // Foreign Keys
    // =========================================================================

    /// <summary>
    /// The organization this critical task belongs to. Required for data isolation.
    /// Denormalized from CapabilityTarget for query filter efficiency.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The capability target this task belongs to. Required.
    /// </summary>
    public Guid CapabilityTargetId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The organization this critical task belongs to.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// The capability target this task belongs to.
    /// </summary>
    public CapabilityTarget CapabilityTarget { get; set; } = null!;

    /// <summary>
    /// Injects that test this task (many-to-many via junction table).
    /// </summary>
    public ICollection<InjectCriticalTask> LinkedInjects { get; set; } = new List<InjectCriticalTask>();

    /// <summary>
    /// EEG entries recorded against this task.
    /// </summary>
    public ICollection<EegEntry> EegEntries { get; set; } = new List<EegEntry>();
}
