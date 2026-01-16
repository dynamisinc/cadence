namespace Cadence.Core.Models.Entities;

/// <summary>
/// Objective entity - represents an exercise objective per HSEEP guidance.
///
/// Exercise objectives are measurable statements that guide exercise design and
/// are used for evaluation during the After-Action Review process.
///
/// Per HSEEP 2020, objectives should be:
/// - Specific, measurable, achievable, relevant, and time-bound (SMART)
/// - Based on capability targets from the Threat and Hazard Identification and Risk Assessment (THIRA)
/// - Limited to 3-5 objectives per exercise
/// </summary>
public class Objective : BaseEntity
{
    /// <summary>
    /// Objective number for display ordering (e.g., "1", "2", "3").
    /// Required, max 10 characters.
    /// </summary>
    public required string ObjectiveNumber { get; set; }

    /// <summary>
    /// Short objective name (e.g., "EOC Activation & Coordination").
    /// Required, max 200 characters.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Detailed description of the objective, including measurable criteria.
    /// Max 2000 characters.
    /// </summary>
    public string? Description { get; set; }

    // =========================================================================
    // Foreign Keys
    // =========================================================================

    /// <summary>
    /// The exercise this objective belongs to.
    /// </summary>
    public Guid ExerciseId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The exercise that this objective evaluates.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// Junction entities linking this objective to injects.
    /// </summary>
    public ICollection<InjectObjective> InjectObjectives { get; set; } = new List<InjectObjective>();
}
