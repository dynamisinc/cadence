using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for exercise participant management.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}/participants")]
[Authorize]
public class ExerciseParticipantsController : ControllerBase
{
    private readonly IExerciseParticipantService _participantService;
    private readonly ILogger<ExerciseParticipantsController> _logger;

    public ExerciseParticipantsController(
        IExerciseParticipantService participantService,
        ILogger<ExerciseParticipantsController> logger)
    {
        _participantService = participantService;
        _logger = logger;
    }

    /// <summary>
    /// Get all participants for an exercise.
    /// Shows exercise-specific roles and effective roles.
    /// </summary>
    [HttpGet]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<List<ExerciseParticipantDto>>> GetParticipants(Guid exerciseId)
    {
        var participants = await _participantService.GetParticipantsAsync(exerciseId);
        return Ok(participants);
    }

    /// <summary>
    /// Get a specific participant for an exercise.
    /// </summary>
    [HttpGet("{userId}")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<ExerciseParticipantDto>> GetParticipant(Guid exerciseId, string userId)
    {
        var participant = await _participantService.GetParticipantAsync(exerciseId, userId);

        if (participant == null)
        {
            return NotFound(new { message = "Participant not found" });
        }

        return Ok(participant);
    }

    /// <summary>
    /// Add a participant to an exercise with an optional exercise-specific role.
    /// If no role is specified, the user's global role is used.
    /// Only Administrators and Exercise Directors can add participants.
    /// </summary>
    [HttpPost]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<ExerciseParticipantDto>> AddParticipant(
        Guid exerciseId,
        [FromBody] AddParticipantRequest request)
    {
        try
        {
            var result = await _participantService.AddParticipantAsync(exerciseId, request);

            _logger.LogInformation(
                "Added participant {UserId} to exercise {ExerciseId}",
                request.UserId, exerciseId);

            return CreatedAtAction(
                nameof(GetParticipant),
                new { exerciseId, userId = request.UserId },
                result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a participant's exercise-specific role.
    /// Setting role to null removes the override and uses the user's global role.
    /// Only Administrators and Exercise Directors can update participant roles.
    /// </summary>
    [HttpPut("{userId}/role")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<ExerciseParticipantDto>> UpdateParticipantRole(
        Guid exerciseId,
        string userId,
        [FromBody] UpdateParticipantRoleRequest request)
    {
        try
        {
            var result = await _participantService.UpdateParticipantRoleAsync(exerciseId, userId, request);

            _logger.LogInformation(
                "Updated participant {UserId} role in exercise {ExerciseId}",
                userId, exerciseId);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a participant from an exercise.
    /// Only Administrators and Exercise Directors can remove participants.
    /// </summary>
    [HttpDelete("{userId}")]
    [AuthorizeExerciseDirector]
    public async Task<IActionResult> RemoveParticipant(Guid exerciseId, string userId)
    {
        try
        {
            await _participantService.RemoveParticipantAsync(exerciseId, userId);

            _logger.LogInformation(
                "Removed participant {UserId} from exercise {ExerciseId}",
                userId, exerciseId);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Bulk update participants for an exercise.
    /// Adds or updates multiple participants in a single request.
    /// Only Administrators and Exercise Directors can bulk update participants.
    /// </summary>
    [HttpPut]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<List<ExerciseParticipantDto>>> BulkUpdateParticipants(
        Guid exerciseId,
        [FromBody] BulkUpdateParticipantsRequest request)
    {
        try
        {
            await _participantService.BulkUpdateParticipantsAsync(exerciseId, request);

            var participants = await _participantService.GetParticipantsAsync(exerciseId);

            _logger.LogInformation(
                "Bulk updated {Count} participants for exercise {ExerciseId}",
                request.Participants.Count, exerciseId);

            return Ok(participants);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
