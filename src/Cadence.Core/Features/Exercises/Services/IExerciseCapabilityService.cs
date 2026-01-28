using Cadence.Core.Features.Capabilities.Models.DTOs;
using Cadence.Core.Features.Exercises.Models.DTOs;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for managing exercise target capabilities (S04).
/// Handles the linking of capabilities to exercises for evaluation purposes.
/// </summary>
public interface IExerciseCapabilityService
{
    /// <summary>
    /// Gets all active target capabilities for an exercise.
    /// Returns only active capabilities, ordered by category then name.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of capability DTOs linked to this exercise.</returns>
    Task<IEnumerable<CapabilityDto>> GetTargetCapabilitiesAsync(
        Guid exerciseId,
        CancellationToken ct = default);

    /// <summary>
    /// Sets the target capabilities for an exercise.
    /// Replaces all existing links with the provided list.
    /// Pass empty list to clear all target capabilities.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="capabilityIds">List of capability IDs to set as targets.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetTargetCapabilitiesAsync(
        Guid exerciseId,
        List<Guid> capabilityIds,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a summary of capability coverage for an exercise.
    /// Shows how many target capabilities have been evaluated.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Summary with target count, evaluated count, and coverage percentage.</returns>
    Task<ExerciseCapabilitySummaryDto> GetCapabilitySummaryAsync(
        Guid exerciseId,
        CancellationToken ct = default);
}
