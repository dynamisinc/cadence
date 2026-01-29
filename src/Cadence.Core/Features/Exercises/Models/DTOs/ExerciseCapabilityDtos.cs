namespace Cadence.Core.Features.Exercises.Models.DTOs;

/// <summary>
/// Request DTO for setting target capabilities for an exercise.
/// </summary>
public class SetExerciseCapabilitiesRequest
{
    /// <summary>
    /// List of capability IDs to set as targets for this exercise.
    /// Empty list will clear all target capabilities.
    /// </summary>
    public List<Guid> CapabilityIds { get; init; } = new();
}

/// <summary>
/// Summary of capability coverage for an exercise.
/// Shows how many target capabilities have been evaluated.
/// </summary>
public record ExerciseCapabilitySummaryDto(
    /// <summary>
    /// Total number of target capabilities for this exercise.
    /// </summary>
    int TargetCount,

    /// <summary>
    /// Number of target capabilities that have been evaluated (have observations).
    /// </summary>
    int EvaluatedCount,

    /// <summary>
    /// Percentage of target capabilities that have been evaluated (0-100).
    /// Null if there are no target capabilities.
    /// </summary>
    decimal? CoveragePercentage
);
