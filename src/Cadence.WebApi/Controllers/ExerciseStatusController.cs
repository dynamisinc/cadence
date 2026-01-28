using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for exercise status workflow operations.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}")]
[Authorize]
public class ExerciseStatusController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IExerciseStatusService _statusService;
    private readonly ILogger<ExerciseStatusController> _logger;

    public ExerciseStatusController(
        AppDbContext context,
        IExerciseStatusService statusService,
        ILogger<ExerciseStatusController> logger)
    {
        _context = context;
        _statusService = statusService;
        _logger = logger;
    }

    /// <summary>
    /// Activate an exercise (Draft → Active).
    /// Requires at least one inject in the MSEL.
    /// </summary>
    [HttpPost("activate")]
    public async Task<ActionResult<ExerciseDto>> ActivateExercise(Guid exerciseId)
    {
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.ActivateAsync(exerciseId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Activated exercise {ExerciseId}", exerciseId);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Pause an exercise (Active → Paused).
    /// Preserves clock elapsed time.
    /// </summary>
    [HttpPost("pause")]
    public async Task<ActionResult<ExerciseDto>> PauseExercise(Guid exerciseId)
    {
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.PauseAsync(exerciseId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Paused exercise {ExerciseId}", exerciseId);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Resume a paused exercise (Paused → Active).
    /// </summary>
    [HttpPost("resume")]
    public async Task<ActionResult<ExerciseDto>> ResumeExercise(Guid exerciseId)
    {
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.ResumeAsync(exerciseId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Resumed exercise {ExerciseId}", exerciseId);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Complete an exercise (Active/Paused → Completed).
    /// Permanently stops the clock.
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<ExerciseDto>> CompleteExercise(Guid exerciseId)
    {
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.CompleteAsync(exerciseId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Completed exercise {ExerciseId}", exerciseId);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Archive a completed exercise (Completed → Archived).
    /// Makes the exercise fully read-only.
    /// </summary>
    [HttpPost("archive")]
    public async Task<ActionResult<ExerciseDto>> ArchiveExercise(Guid exerciseId)
    {
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.ArchiveAsync(exerciseId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Archived exercise {ExerciseId}", exerciseId);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Unarchive an exercise (Archived → Completed).
    /// Restores the exercise to completed status.
    /// </summary>
    [HttpPost("unarchive")]
    public async Task<ActionResult<ExerciseDto>> UnarchiveExercise(Guid exerciseId)
    {
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.UnarchiveAsync(exerciseId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Unarchived exercise {ExerciseId}", exerciseId);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Revert a paused exercise to draft (Paused → Draft).
    /// WARNING: This clears all conduct data (fired times, observations).
    /// </summary>
    [HttpPost("revert-to-draft")]
    public async Task<ActionResult<ExerciseDto>> RevertToDraft(Guid exerciseId)
    {
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.RevertToDraftAsync(exerciseId, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogWarning(
            "Exercise {ExerciseId} reverted to Draft - conduct data cleared",
            exerciseId);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Get available status transitions for an exercise.
    /// </summary>
    [HttpGet("available-transitions")]
    public async Task<ActionResult<IReadOnlyList<ExerciseStatus>>> GetAvailableTransitions(Guid exerciseId)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);

        if (exercise == null)
        {
            return NotFound();
        }

        var transitions = _statusService.GetAvailableTransitions(exercise.Status);

        return Ok(transitions);
    }
}
