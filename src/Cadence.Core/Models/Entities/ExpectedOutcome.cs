namespace Cadence.Core.Models.Entities;

/// <summary>
/// Expected outcome for an inject - what should happen when the inject is delivered.
/// Multiple outcomes can be defined per inject.
/// </summary>
public class ExpectedOutcome : BaseEntity
{
    /// <summary>
    /// Parent inject.
    /// </summary>
    public Guid InjectId { get; set; }

    /// <summary>
    /// Description of the expected outcome. Required, max 1000 characters.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Display order within the inject's outcomes list.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this outcome was achieved during the exercise.
    /// Null = not yet evaluated, True = achieved, False = not achieved.
    /// </summary>
    public bool? WasAchieved { get; set; }

    /// <summary>
    /// Evaluator's notes on this outcome. Max 2000 characters.
    /// </summary>
    public string? EvaluatorNotes { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The inject this outcome belongs to.
    /// </summary>
    public Inject Inject { get; set; } = null!;
}
