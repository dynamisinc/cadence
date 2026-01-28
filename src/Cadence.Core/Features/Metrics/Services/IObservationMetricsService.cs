using Cadence.Core.Features.Metrics.Models.DTOs;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Service interface for observation, evaluator, and capability metrics.
/// Provides AAR statistics for evaluation coverage and performance.
/// </summary>
public interface IObservationMetricsService
{
    /// <summary>
    /// Get comprehensive observation statistics for AAR (S03).
    /// Used after exercise completion to analyze evaluation coverage and ratings.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <returns>Observation summary data or null if exercise not found.</returns>
    Task<ObservationSummaryDto?> GetObservationSummaryAsync(Guid exerciseId);

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
