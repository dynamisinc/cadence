namespace Cadence.Core.Features.Exercises.Models.DTOs;

/// <summary>
/// Represents the setup progress for an exercise.
/// Shows completion status for each configuration area with weighted scoring.
/// </summary>
public record SetupProgressDto
{
    /// <summary>
    /// Overall completion percentage (0-100)
    /// </summary>
    public int OverallPercentage { get; init; }

    /// <summary>
    /// Whether the exercise is ready to activate
    /// </summary>
    public bool IsReadyToActivate { get; init; }

    /// <summary>
    /// Individual area progress details
    /// </summary>
    public required IReadOnlyList<SetupAreaDto> Areas { get; init; }
}

/// <summary>
/// Represents a single setup area's progress
/// </summary>
public record SetupAreaDto
{
    /// <summary>
    /// Unique identifier for this area
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name for this area
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of what this area covers
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Whether this area is complete
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Weight of this area in overall calculation (0-100)
    /// </summary>
    public int Weight { get; init; }

    /// <summary>
    /// Current count (e.g., number of injects)
    /// </summary>
    public int CurrentCount { get; init; }

    /// <summary>
    /// Required count to be complete (0 means just needs at least 1)
    /// </summary>
    public int RequiredCount { get; init; }

    /// <summary>
    /// Status message to display
    /// </summary>
    public required string StatusMessage { get; init; }
}
