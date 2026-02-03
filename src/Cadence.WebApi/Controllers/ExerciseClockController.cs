using System.Security.Claims;
using Cadence.Core.Features.ExerciseClock.Models.DTOs;
using Cadence.Core.Features.ExerciseClock.Services;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for exercise clock operations.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}/clock")]
[Authorize]
public class ExerciseClockController : ControllerBase
{
    private readonly IExerciseClockService _clockService;
    private readonly ILogger<ExerciseClockController> _logger;

    public ExerciseClockController(
        IExerciseClockService clockService,
        ILogger<ExerciseClockController> logger)
    {
        _clockService = clockService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current clock state for an exercise.
    /// </summary>
    [HttpGet]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<ClockStateDto>> GetClockState(Guid exerciseId)
    {
        var clockState = await _clockService.GetClockStateAsync(exerciseId);

        if (clockState == null)
        {
            return NotFound();
        }

        return Ok(clockState);
    }

    /// <summary>
    /// Start the exercise clock.
    /// This also transitions the exercise from Draft to Active status.
    /// </summary>
    [HttpPost("start")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<ClockStateDto>> StartClock(Guid exerciseId)
    {
        try
        {
            var startedBy = GetCurrentUserIdString();

            var clockState = await _clockService.StartClockAsync(exerciseId, startedBy);

            _logger.LogInformation("Started clock for exercise {ExerciseId}", exerciseId);

            return Ok(clockState);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Pause the exercise clock.
    /// Preserves elapsed time for later resumption.
    /// </summary>
    [HttpPost("pause")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<ClockStateDto>> PauseClock(Guid exerciseId)
    {
        try
        {
            var pausedBy = GetCurrentUserId();

            var clockState = await _clockService.PauseClockAsync(exerciseId, pausedBy);

            _logger.LogInformation("Paused clock for exercise {ExerciseId}", exerciseId);

            return Ok(clockState);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Stop the exercise clock and complete the exercise.
    /// This transitions the exercise to Completed status.
    /// </summary>
    [HttpPost("stop")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<ClockStateDto>> StopClock(Guid exerciseId)
    {
        try
        {
            var stoppedBy = GetCurrentUserId();

            var clockState = await _clockService.StopClockAsync(exerciseId, stoppedBy);

            _logger.LogInformation("Stopped clock for exercise {ExerciseId}. Exercise completed.", exerciseId);

            return Ok(clockState);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reset the exercise clock to zero.
    /// Only allowed for Draft exercises or when clock is Stopped.
    /// </summary>
    [HttpPost("reset")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<ClockStateDto>> ResetClock(Guid exerciseId)
    {
        try
        {
            var resetBy = GetCurrentUserId();

            var clockState = await _clockService.ResetClockAsync(exerciseId, resetBy);

            _logger.LogInformation("Reset clock for exercise {ExerciseId}", exerciseId);

            return Ok(clockState);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    private string GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return userIdClaim;
    }

    private string GetCurrentUserIdString()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return userIdClaim;
    }
}
