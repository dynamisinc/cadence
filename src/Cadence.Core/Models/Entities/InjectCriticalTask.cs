using System.ComponentModel.DataAnnotations;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Injects and Critical Tasks.
/// An inject can test multiple critical tasks, and a critical task can be tested
/// by multiple injects in the MSEL.
///
/// Includes audit fields (CreatedAt, CreatedBy) for HSEEP compliance tracking
/// of when inject-to-task linkages were established.
/// </summary>
public class InjectCriticalTask
{
    // =========================================================================
    // Composite Key Properties
    // =========================================================================

    /// <summary>
    /// The inject that tests the critical task.
    /// </summary>
    public Guid InjectId { get; set; }

    /// <summary>
    /// The critical task being tested by the inject.
    /// </summary>
    public Guid CriticalTaskId { get; set; }

    // =========================================================================
    // Audit Fields (for HSEEP compliance)
    // =========================================================================

    /// <summary>
    /// When this linkage was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who created this linkage.
    /// </summary>
    [MaxLength(450)]
    public string CreatedBy { get; set; } = string.Empty;

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The inject entity.
    /// </summary>
    public Inject Inject { get; set; } = null!;

    /// <summary>
    /// The critical task entity.
    /// </summary>
    public CriticalTask CriticalTask { get; set; } = null!;
}
