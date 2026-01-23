using Cadence.Core.Constants;
using Cadence.Core.Features.Objectives.Models.DTOs;
using Cadence.Core.Features.Objectives.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for exercise objective management.
/// Objectives define the capabilities being tested during an exercise per HSEEP guidance.
/// Requires authentication for all endpoints.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}/objectives")]
[Authorize]
public class ObjectivesController : ControllerBase
{
    private readonly IObjectiveService _objectiveService;
    private readonly ILogger<ObjectivesController> _logger;

    public ObjectivesController(IObjectiveService objectiveService, ILogger<ObjectivesController> logger)
    {
        _objectiveService = objectiveService;
        _logger = logger;
    }

    /// <summary>
    /// Get all objectives for an exercise.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ObjectiveDto>>> GetObjectives(Guid exerciseId)
    {
        var objectives = await _objectiveService.GetObjectivesByExerciseAsync(exerciseId);
        return Ok(objectives);
    }

    /// <summary>
    /// Get lightweight objective summaries for selection dropdowns.
    /// </summary>
    [HttpGet("summaries")]
    public async Task<ActionResult<IEnumerable<ObjectiveSummaryDto>>> GetObjectiveSummaries(Guid exerciseId)
    {
        var summaries = await _objectiveService.GetObjectiveSummariesAsync(exerciseId);
        return Ok(summaries);
    }

    /// <summary>
    /// Get a single objective by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ObjectiveDto>> GetObjective(Guid exerciseId, Guid id)
    {
        var objective = await _objectiveService.GetObjectiveAsync(exerciseId, id);

        if (objective == null)
        {
            return NotFound(new { message = "Objective not found" });
        }

        return Ok(objective);
    }

    /// <summary>
    /// Create a new objective for an exercise.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ObjectiveDto>> CreateObjective(Guid exerciseId, CreateObjectiveRequest request)
    {
        // Validation
        var validationError = ValidateObjectiveRequest(request.Name, request.ObjectiveNumber, request.Description);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        try
        {
            // System user until auth is implemented
            var createdBy = SystemConstants.SystemUserId;

            var objective = await _objectiveService.CreateObjectiveAsync(exerciseId, request, createdBy);

            _logger.LogInformation("Created objective {ObjectiveId}: {ObjectiveName} for exercise {ExerciseId}",
                objective.Id, objective.Name, exerciseId);

            return CreatedAtAction(
                nameof(GetObjective),
                new { exerciseId, id = objective.Id },
                objective
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing objective.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ObjectiveDto>> UpdateObjective(Guid exerciseId, Guid id, UpdateObjectiveRequest request)
    {
        // Validation
        var validationError = ValidateObjectiveRequest(request.Name, request.ObjectiveNumber, request.Description);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        try
        {
            // System user until auth is implemented
            var modifiedBy = SystemConstants.SystemUserId;

            var objective = await _objectiveService.UpdateObjectiveAsync(exerciseId, id, request, modifiedBy);

            if (objective == null)
            {
                return NotFound(new { message = "Objective not found" });
            }

            _logger.LogInformation("Updated objective {ObjectiveId}: {ObjectiveName}", id, objective.Name);

            return Ok(objective);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete an objective (soft delete).
    /// Only allowed if no injects are linked.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteObjective(Guid exerciseId, Guid id)
    {
        try
        {
            // System user until auth is implemented
            var deletedBy = SystemConstants.SystemUserId;

            var deleted = await _objectiveService.DeleteObjectiveAsync(exerciseId, id, deletedBy);

            if (!deleted)
            {
                return NotFound(new { message = "Objective not found" });
            }

            _logger.LogInformation("Deleted objective {ObjectiveId}", id);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Check if an objective number is available.
    /// </summary>
    [HttpGet("check-number")]
    public async Task<ActionResult<object>> CheckObjectiveNumber(
        Guid exerciseId,
        [FromQuery] string number,
        [FromQuery] Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return BadRequest(new { message = "Number is required" });
        }

        var isAvailable = await _objectiveService.IsObjectiveNumberUniqueAsync(exerciseId, number, excludeId);
        return Ok(new { isAvailable });
    }

    private static string? ValidateObjectiveRequest(string name, string? objectiveNumber, string? description)
    {
        // Name validation
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required";
        }
        if (name.Length < 3)
        {
            return "Name must be at least 3 characters";
        }
        if (name.Length > 200)
        {
            return "Name must be 200 characters or less";
        }

        // Objective number validation
        if (objectiveNumber?.Length > 10)
        {
            return "Objective number must be 10 characters or less";
        }

        // Description validation
        if (description?.Length > 2000)
        {
            return "Description must be 2000 characters or less";
        }

        return null;
    }
}
