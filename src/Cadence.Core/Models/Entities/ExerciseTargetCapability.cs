namespace Cadence.Core.Models.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between Exercises and Capabilities.
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
    public Guid CapabilityId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The exercise.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// The capability.
    /// </summary>
    public Capability Capability { get; set; } = null!;
}
