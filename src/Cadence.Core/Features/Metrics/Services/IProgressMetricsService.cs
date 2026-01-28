using Cadence.Core.Features.Metrics.Models.DTOs;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Service interface for real-time exercise progress metrics.
/// Provides situational awareness data during active exercises.
/// </summary>
public interface IProgressMetricsService
{
    /// <summary>
    /// Get real-time exercise progress for conduct view (S01).
    /// Used during active exercises to show situational awareness.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <returns>Progress data or null if exercise not found.</returns>
    Task<ExerciseProgressDto?> GetExerciseProgressAsync(Guid exerciseId);
}
