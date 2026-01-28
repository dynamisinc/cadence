using Cadence.Core.Features.Capabilities.Models.DTOs;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for managing exercise target capabilities (S04).
/// Allows Exercise Directors+ to define which capabilities will be evaluated during an exercise.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}/capabilities")]
[Authorize]
public class ExerciseCapabilitiesController : ControllerBase
{
    private readonly IExerciseCapabilityService _capabilityService;
    private readonly ILogger<ExerciseCapabilitiesController> _logger;

    public ExerciseCapabilitiesController(
        IExerciseCapabilityService capabilityService,
        ILogger<ExerciseCapabilitiesController> logger)
    {
        _capabilityService = capabilityService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all target capabilities for an exercise.
    /// Returns only active capabilities.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of capability DTOs.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CapabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CapabilityDto>>> GetTargetCapabilities(
        Guid exerciseId,
        CancellationToken ct)
    {
        _logger.LogInformation("GET api/exercises/{ExerciseId}/capabilities", exerciseId);

        var capabilities = await _capabilityService.GetTargetCapabilitiesAsync(exerciseId, ct);

        return Ok(capabilities);
    }

    /// <summary>
    /// Sets the target capabilities for an exercise.
    /// Replaces all existing target capabilities with the provided list.
    /// Pass empty array to clear all target capabilities.
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="request">Request containing list of capability IDs.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTargetCapabilities(
        Guid exerciseId,
        [FromBody] SetExerciseCapabilitiesRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("PUT api/exercises/{ExerciseId}/capabilities with {Count} capability IDs",
            exerciseId, request.CapabilityIds.Count);

        await _capabilityService.SetTargetCapabilitiesAsync(exerciseId, request.CapabilityIds, ct);

        return NoContent();
    }

    /// <summary>
    /// Gets a summary of capability coverage for an exercise.
    /// Shows how many target capabilities have been evaluated (have observations).
    /// </summary>
    /// <param name="exerciseId">The exercise ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Summary with target count, evaluated count, and coverage percentage.</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ExerciseCapabilitySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseCapabilitySummaryDto>> GetCapabilitySummary(
        Guid exerciseId,
        CancellationToken ct)
    {
        _logger.LogInformation("GET api/exercises/{ExerciseId}/capabilities/summary", exerciseId);

        var summary = await _capabilityService.GetCapabilitySummaryAsync(exerciseId, ct);

        return Ok(summary);
    }
}
