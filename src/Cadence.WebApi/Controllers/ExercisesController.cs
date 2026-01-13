using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.ExerciseClock.Models.DTOs;
using Cadence.Core.Features.ExerciseClock.Services;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for exercise management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IExerciseClockService _clockService;
    private readonly ILogger<ExercisesController> _logger;

    public ExercisesController(
        AppDbContext context,
        IExerciseClockService clockService,
        ILogger<ExercisesController> logger)
    {
        _context = context;
        _clockService = clockService;
        _logger = logger;
    }

    /// <summary>
    /// Get all exercises.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExerciseDto>>> GetExercises()
    {
        var exercises = await _context.Exercises
            .OrderByDescending(e => e.ScheduledDate)
            .ToListAsync();

        return Ok(exercises.Select(e => e.ToDto()));
    }

    /// <summary>
    /// Get a single exercise by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExerciseDto>> GetExercise(Guid id)
    {
        var exercise = await _context.Exercises.FindAsync(id);

        if (exercise == null)
        {
            return NotFound();
        }

        return Ok(exercise.ToDto());
    }

    /// <summary>
    /// Create a new exercise.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExerciseDto>> CreateExercise(CreateExerciseRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        if (request.Name.Length > 200)
        {
            return BadRequest(new { message = "Name must be 200 characters or less" });
        }

        // Use default organization (seeded in database)
        var organization = await _context.Organizations.FirstOrDefaultAsync();
        if (organization == null)
        {
            return StatusCode(500, new { message = "Default organization not found. Please run database migrations." });
        }

        // System user until auth is implemented
        var createdBy = SystemConstants.SystemUserId;
        var exercise = request.ToEntity(organization.Id, createdBy);

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created exercise {ExerciseId}: {ExerciseName}", exercise.Id, exercise.Name);

        return CreatedAtAction(
            nameof(GetExercise),
            new { id = exercise.Id },
            exercise.ToDto()
        );
    }

    /// <summary>
    /// Update an existing exercise.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExerciseDto>> UpdateExercise(Guid id, UpdateExerciseRequest request)
    {
        var exercise = await _context.Exercises.FindAsync(id);

        if (exercise == null)
        {
            return NotFound();
        }

        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        if (request.Name.Length > 200)
        {
            return BadRequest(new { message = "Name must be 200 characters or less" });
        }

        // Check status-based edit restrictions
        if (exercise.Status == ExerciseStatus.Completed || exercise.Status == ExerciseStatus.Archived)
        {
            return BadRequest(new { message = $"{exercise.Status} exercises cannot be modified" });
        }

        // Update fields (respecting status-based restrictions)
        exercise.Name = request.Name;
        exercise.Description = request.Description;

        // These fields can only be changed in Draft status
        if (exercise.Status == ExerciseStatus.Draft)
        {
            exercise.ExerciseType = request.ExerciseType;
            exercise.ScheduledDate = request.ScheduledDate;
            exercise.StartTime = request.StartTime;
        }

        // End time can always be updated (as long as not Completed/Archived)
        exercise.EndTime = request.EndTime;
        exercise.Location = request.Location;
        exercise.TimeZoneId = request.TimeZoneId;

        // System user until auth is implemented
        exercise.ModifiedBy = SystemConstants.SystemUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated exercise {ExerciseId}: {ExerciseName}", exercise.Id, exercise.Name);

        return Ok(exercise.ToDto());
    }

    // =========================================================================
    // Exercise Clock Endpoints
    // =========================================================================

    /// <summary>
    /// Get the current clock state for an exercise.
    /// </summary>
    [HttpGet("{id:guid}/clock")]
    public async Task<ActionResult<ClockStateDto>> GetClockState(Guid id)
    {
        var clockState = await _clockService.GetClockStateAsync(id);

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
    [HttpPost("{id:guid}/clock/start")]
    public async Task<ActionResult<ClockStateDto>> StartClock(Guid id)
    {
        try
        {
            // System user until auth is implemented
            var startedBy = SystemConstants.SystemUserId;

            var clockState = await _clockService.StartClockAsync(id, startedBy);

            _logger.LogInformation("Started clock for exercise {ExerciseId}", id);

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
    [HttpPost("{id:guid}/clock/pause")]
    public async Task<ActionResult<ClockStateDto>> PauseClock(Guid id)
    {
        try
        {
            // System user until auth is implemented
            var pausedBy = SystemConstants.SystemUserId;

            var clockState = await _clockService.PauseClockAsync(id, pausedBy);

            _logger.LogInformation("Paused clock for exercise {ExerciseId}", id);

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
    [HttpPost("{id:guid}/clock/stop")]
    public async Task<ActionResult<ClockStateDto>> StopClock(Guid id)
    {
        try
        {
            // System user until auth is implemented
            var stoppedBy = SystemConstants.SystemUserId;

            var clockState = await _clockService.StopClockAsync(id, stoppedBy);

            _logger.LogInformation("Stopped clock for exercise {ExerciseId}. Exercise completed.", id);

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
    [HttpPost("{id:guid}/clock/reset")]
    public async Task<ActionResult<ClockStateDto>> ResetClock(Guid id)
    {
        try
        {
            // System user until auth is implemented
            var resetBy = SystemConstants.SystemUserId;

            var clockState = await _clockService.ResetClockAsync(id, resetBy);

            _logger.LogInformation("Reset clock for exercise {ExerciseId}", id);

            return Ok(clockState);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
