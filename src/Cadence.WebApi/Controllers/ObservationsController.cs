using Cadence.Core.Constants;
using Cadence.Core.Features.Observations.Models.DTOs;
using Cadence.Core.Features.Observations.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for observation management.
/// Observations are evaluator assessments of player performance during exercise conduct.
/// </summary>
[ApiController]
[Route("api")]
public class ObservationsController : ControllerBase
{
    private readonly IObservationService _observationService;
    private readonly ILogger<ObservationsController> _logger;

    public ObservationsController(IObservationService observationService, ILogger<ObservationsController> logger)
    {
        _observationService = observationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all observations for an exercise.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/observations")]
    public async Task<ActionResult<IEnumerable<ObservationDto>>> GetObservationsByExercise(Guid exerciseId)
    {
        var observations = await _observationService.GetObservationsByExerciseAsync(exerciseId);
        return Ok(observations);
    }

    /// <summary>
    /// Get all observations for a specific inject.
    /// </summary>
    [HttpGet("injects/{injectId:guid}/observations")]
    public async Task<ActionResult<IEnumerable<ObservationDto>>> GetObservationsByInject(Guid injectId)
    {
        var observations = await _observationService.GetObservationsByInjectAsync(injectId);
        return Ok(observations);
    }

    /// <summary>
    /// Get a single observation by ID.
    /// </summary>
    [HttpGet("observations/{id:guid}")]
    public async Task<ActionResult<ObservationDto>> GetObservation(Guid id)
    {
        var observation = await _observationService.GetObservationAsync(id);

        if (observation == null)
        {
            return NotFound();
        }

        return Ok(observation);
    }

    /// <summary>
    /// Create a new observation for an exercise.
    /// </summary>
    [HttpPost("exercises/{exerciseId:guid}/observations")]
    public async Task<ActionResult<ObservationDto>> CreateObservation(Guid exerciseId, CreateObservationRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Content is required" });
        }

        if (request.Content.Length > 4000)
        {
            return BadRequest(new { message = "Content must be 4000 characters or less" });
        }

        if (request.Recommendation?.Length > 2000)
        {
            return BadRequest(new { message = "Recommendation must be 2000 characters or less" });
        }

        if (request.Location?.Length > 200)
        {
            return BadRequest(new { message = "Location must be 200 characters or less" });
        }

        try
        {
            // System user until auth is implemented
            var createdBy = SystemConstants.SystemUserId;

            var observation = await _observationService.CreateObservationAsync(exerciseId, request, createdBy);

            return CreatedAtAction(
                nameof(GetObservation),
                new { id = observation.Id },
                observation
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing observation.
    /// </summary>
    [HttpPut("observations/{id:guid}")]
    public async Task<ActionResult<ObservationDto>> UpdateObservation(Guid id, UpdateObservationRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Content is required" });
        }

        if (request.Content.Length > 4000)
        {
            return BadRequest(new { message = "Content must be 4000 characters or less" });
        }

        if (request.Recommendation?.Length > 2000)
        {
            return BadRequest(new { message = "Recommendation must be 2000 characters or less" });
        }

        if (request.Location?.Length > 200)
        {
            return BadRequest(new { message = "Location must be 200 characters or less" });
        }

        try
        {
            // System user until auth is implemented
            var modifiedBy = SystemConstants.SystemUserId;

            var observation = await _observationService.UpdateObservationAsync(id, request, modifiedBy);

            if (observation == null)
            {
                return NotFound();
            }

            return Ok(observation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete an observation (soft delete).
    /// </summary>
    [HttpDelete("observations/{id:guid}")]
    public async Task<IActionResult> DeleteObservation(Guid id)
    {
        // System user until auth is implemented
        var deletedBy = SystemConstants.SystemUserId;

        var deleted = await _observationService.DeleteObservationAsync(id, deletedBy);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
