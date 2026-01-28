using Cadence.Core.Features.Metrics.Models.DTOs;
using Cadence.Core.Features.Metrics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for exercise metrics and analytics.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}")]
[Authorize]
public class ExerciseMetricsController : ControllerBase
{
    private readonly IExerciseMetricsService _metricsService;

    public ExerciseMetricsController(IExerciseMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    /// <summary>
    /// Get real-time exercise progress for conduct view.
    /// Provides situational awareness: inject counts, observation counts, clock status.
    /// Used by Controllers and Directors during active exercises.
    /// </summary>
    [HttpGet("progress")]
    public async Task<ActionResult<ExerciseProgressDto>> GetExerciseProgress(Guid exerciseId)
    {
        var progress = await _metricsService.GetExerciseProgressAsync(exerciseId);

        if (progress == null)
        {
            return NotFound();
        }

        return Ok(progress);
    }

    /// <summary>
    /// Get comprehensive inject delivery statistics for after-action review.
    /// Shows timing performance, on-time rate, and breakdowns by phase/controller.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="onTimeToleranceMinutes">Minutes tolerance for on-time calculation (default: 5).</param>
    [HttpGet("metrics/injects")]
    public async Task<ActionResult<InjectSummaryDto>> GetInjectMetrics(
        Guid exerciseId,
        [FromQuery] int onTimeToleranceMinutes = 5)
    {
        var summary = await _metricsService.GetInjectSummaryAsync(exerciseId, onTimeToleranceMinutes);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get comprehensive observation statistics for after-action review.
    /// Shows P/S/M/U distribution, coverage rates, and breakdowns by evaluator/phase.
    /// </summary>
    [HttpGet("metrics/observations")]
    public async Task<ActionResult<ObservationSummaryDto>> GetObservationMetrics(Guid exerciseId)
    {
        var summary = await _metricsService.GetObservationSummaryAsync(exerciseId);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get comprehensive timeline and duration analysis for after-action review.
    /// Includes pause history, phase timing, and inject pacing analysis.
    /// </summary>
    /// <param name="exerciseId">Exercise ID.</param>
    /// <returns>Timeline summary data.</returns>
    [HttpGet("metrics/timeline")]
    [ProducesResponseType(typeof(TimelineSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TimelineSummaryDto>> GetTimelineMetrics(Guid exerciseId)
    {
        var summary = await _metricsService.GetTimelineSummaryAsync(exerciseId);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get controller activity metrics for after-action review.
    /// Shows workload distribution, timing performance, and phase activity per controller.
    /// </summary>
    /// <param name="exerciseId">Exercise ID.</param>
    /// <param name="onTimeToleranceMinutes">Minutes tolerance for on-time calculation (default: 5).</param>
    /// <returns>Controller activity summary data.</returns>
    [HttpGet("metrics/controllers")]
    [ProducesResponseType(typeof(ControllerActivitySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ControllerActivitySummaryDto>> GetControllerMetrics(
        Guid exerciseId,
        [FromQuery] int onTimeToleranceMinutes = 5)
    {
        var summary = await _metricsService.GetControllerActivityAsync(exerciseId, onTimeToleranceMinutes);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get evaluator coverage metrics for after-action review.
    /// Shows observation distribution, objective coverage, and rating consistency per evaluator.
    /// </summary>
    /// <param name="exerciseId">Exercise ID.</param>
    /// <returns>Evaluator coverage summary data.</returns>
    [HttpGet("metrics/evaluators")]
    [ProducesResponseType(typeof(EvaluatorCoverageSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluatorCoverageSummaryDto>> GetEvaluatorMetrics(Guid exerciseId)
    {
        var summary = await _metricsService.GetEvaluatorCoverageAsync(exerciseId);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get capability performance metrics for an exercise (S06).
    /// Shows P/S/M/U ratings broken down by FEMA Core Capability.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <returns>Capability performance summary or 404 if not found.</returns>
    [HttpGet("metrics/capabilities")]
    [ProducesResponseType(typeof(CapabilityPerformanceSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CapabilityPerformanceSummaryDto>> GetCapabilityMetrics(Guid exerciseId)
    {
        var summary = await _metricsService.GetCapabilityPerformanceAsync(exerciseId);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }
}
