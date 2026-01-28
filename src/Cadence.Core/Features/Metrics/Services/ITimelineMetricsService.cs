using Cadence.Core.Features.Metrics.Models.DTOs;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Service interface for timeline and duration metrics.
/// Provides AAR statistics for exercise timing and pacing.
/// </summary>
public interface ITimelineMetricsService
{
    /// <summary>
    /// Get comprehensive timeline and duration analysis for AAR (S04).
    /// Used after exercise completion to analyze timing, pauses, and pacing.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <returns>Timeline summary data or null if exercise not found.</returns>
    Task<TimelineSummaryDto?> GetTimelineSummaryAsync(Guid exerciseId);
}
