namespace Cadence.Core.Models.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between Exercises and CoreCapabilities.
/// Tracks which capabilities are specifically targeted for evaluation in an exercise.
/// </summary>
public class ExerciseTargetCapability
{
    /// <summary>
    /// The exercise.
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// The target capability.
    /// </summary>
    public Guid CoreCapabilityId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The exercise.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// The core capability.
    /// </summary>
    public CoreCapability CoreCapability { get; set; } = null!;
}
