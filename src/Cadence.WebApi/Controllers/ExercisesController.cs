using Cadence.Core.Data;
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
    private readonly ILogger<ExercisesController> _logger;

    public ExercisesController(AppDbContext context, ILogger<ExercisesController> logger)
    {
        _context = context;
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

        // For now, use a default organization (we'll create one if none exists)
        var organization = await _context.Organizations.FirstOrDefaultAsync();
        if (organization == null)
        {
            organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Default Organization",
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            };
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();
        }

        // Create exercise with placeholder user ID (no auth yet)
        var placeholderUserId = Guid.Empty;
        var exercise = request.ToEntity(organization.Id, placeholderUserId);

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

        // ModifiedBy would be set to current user once auth is implemented
        exercise.ModifiedBy = Guid.Empty;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated exercise {ExerciseId}: {ExerciseName}", exercise.Id, exercise.Name);

        return Ok(exercise.ToDto());
    }
}
