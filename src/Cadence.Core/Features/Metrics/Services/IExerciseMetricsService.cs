using Cadence.Core.Features.Metrics.Models.DTOs;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Service interface for exercise metrics calculations.
/// Provides real-time progress data (S01) and post-conduct AAR metrics (S02-S08).
/// </summary>
public interface IExerciseMetricsService
{
    /// <summary>
    /// Get real-time exercise progress for conduct view (S01).
    /// Used during active exercises to show situational awareness.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <returns>Progress data or null if exercise not found.</returns>
    Task<ExerciseProgressDto?> GetExerciseProgressAsync(Guid exerciseId);

    /// <summary>
    /// Get comprehensive inject delivery statistics for AAR (S02).
    /// Used after exercise completion to analyze inject timing and delivery.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="onTimeToleranceMinutes">Minutes tolerance for on-time calculation (default: 5).</param>
    /// <returns>Inject summary data or null if exercise not found.</returns>
    Task<InjectSummaryDto?> GetInjectSummaryAsync(Guid exerciseId, int onTimeToleranceMinutes = 5);

    /// <summary>
    /// Get comprehensive observation statistics for AAR (S03).
    /// Used after exercise completion to analyze evaluation coverage and ratings.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <returns>Observation summary data or null if exercise not found.</returns>
    Task<ObservationSummaryDto?> GetObservationSummaryAsync(Guid exerciseId);

    /// <summary>
    /// Get comprehensive timeline and duration analysis for AAR (S04).
    /// Used after exercise completion to analyze timing, pauses, and pacing.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <returns>Timeline summary data or null if exercise not found.</returns>
    Task<TimelineSummaryDto?> GetTimelineSummaryAsync(Guid exerciseId);

    /// <summary>
    /// Get controller activity metrics for AAR (S07).
    /// Shows workload distribution, timing performance, and phase activity per controller.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="onTimeToleranceMinutes">Minutes tolerance for on-time calculation (default: 5).</param>
    /// <returns>Controller activity data or null if exercise not found.</returns>
    Task<ControllerActivitySummaryDto?> GetControllerActivityAsync(Guid exerciseId, int onTimeToleranceMinutes = 5);

    /// <summary>
    /// Get evaluator coverage metrics for AAR (S08).
    /// Shows observation distribution, objective coverage, and rating consistency per evaluator.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <returns>Evaluator coverage data or null if exercise not found.</returns>
    Task<EvaluatorCoverageSummaryDto?> GetEvaluatorCoverageAsync(Guid exerciseId);

    /// <summary>
    /// Get core capability performance metrics for AAR (S06).
    /// Shows P/S/M/U ratings broken down by FEMA Core Capability.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <returns>Capability performance data or null if exercise not found.</returns>
    Task<CapabilityPerformanceSummaryDto?> GetCapabilityPerformanceAsync(Guid exerciseId);
}
