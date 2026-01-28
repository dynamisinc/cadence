using Cadence.Core.Features.Metrics.Models.DTOs;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Service interface for inject delivery and controller activity metrics.
/// Provides AAR statistics for inject timing and controller performance.
/// </summary>
public interface IInjectMetricsService
{
    /// <summary>
    /// Get comprehensive inject delivery statistics for AAR (S02).
    /// Used after exercise completion to analyze inject timing and delivery.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="onTimeToleranceMinutes">Minutes tolerance for on-time calculation (default: 5).</param>
    /// <returns>Inject summary data or null if exercise not found.</returns>
    Task<InjectSummaryDto?> GetInjectSummaryAsync(Guid exerciseId, int onTimeToleranceMinutes = 5);

    /// <summary>
    /// Get controller activity metrics for AAR (S07).
    /// Shows workload distribution, timing performance, and phase activity per controller.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="onTimeToleranceMinutes">Minutes tolerance for on-time calculation (default: 5).</param>
    /// <returns>Controller activity data or null if exercise not found.</returns>
    Task<ControllerActivitySummaryDto?> GetControllerActivityAsync(Guid exerciseId, int onTimeToleranceMinutes = 5);
}
