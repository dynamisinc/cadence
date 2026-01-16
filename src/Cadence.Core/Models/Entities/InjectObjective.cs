namespace Cadence.Core.Models.Entities;

/// <summary>
/// Junction entity for many-to-many relationship between Inject and Objective.
/// An inject can exercise multiple objectives, and each objective can have multiple injects.
/// </summary>
public class InjectObjective
{
    /// <summary>
    /// The inject ID.
    /// </summary>
    public Guid InjectId { get; set; }

    /// <summary>
    /// The objective ID.
    /// </summary>
    public Guid ObjectiveId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The inject in this relationship.
    /// </summary>
    public Inject Inject { get; set; } = null!;

    /// <summary>
    /// The objective in this relationship.
    /// </summary>
    public Objective Objective { get; set; } = null!;
}
