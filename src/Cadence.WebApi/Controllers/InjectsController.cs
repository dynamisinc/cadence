using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for inject (MSEL item) management.
/// Injects belong to MSELs which belong to Exercises.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}/injects")]
public class InjectsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<InjectsController> _logger;
    private readonly IExerciseHubContext _hubContext;

    public InjectsController(AppDbContext context, ILogger<InjectsController> logger, IExerciseHubContext hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Get all injects for an exercise (via its active MSEL).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InjectDto>>> GetInjects(Guid exerciseId)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        if (exercise.ActiveMselId == null)
        {
            return Ok(Array.Empty<InjectDto>());
        }

        var injects = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .Where(i => i.MselId == exercise.ActiveMselId)
            .OrderBy(i => i.Sequence)
            .ToListAsync();

        return Ok(injects.Select(i => i.ToDto()));
    }

    /// <summary>
    /// Get a single inject by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InjectDto>> GetInject(Guid exerciseId, Guid id)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        var inject = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        return Ok(inject.ToDto());
    }

    /// <summary>
    /// Create a new inject.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<InjectDto>> CreateInject(Guid exerciseId, CreateInjectRequest request)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        // Validate request
        var validationError = ValidateInjectRequest(request.Title, request.Description, request.Target,
            request.ScenarioDay, request.ScenarioTime, request.Source, request.ExpectedAction,
            request.ControllerNotes, request.TriggerCondition);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        // Get or create MSEL for this exercise
        Guid mselId;
        if (exercise.ActiveMselId == null)
        {
            var msel = new Msel
            {
                Id = Guid.NewGuid(),
                Name = $"{exercise.Name} MSEL",
                Description = $"Master Scenario Events List for {exercise.Name}",
                Version = 1,
                IsActive = true,
                ExerciseId = exerciseId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            };
            _context.Msels.Add(msel);
            exercise.ActiveMselId = msel.Id;
            mselId = msel.Id;
        }
        else
        {
            mselId = exercise.ActiveMselId.Value;
        }

        // Get next inject number and sequence
        var maxInjectNumber = await _context.Injects
            .Where(i => i.MselId == mselId)
            .MaxAsync(i => (int?)i.InjectNumber) ?? 0;

        var maxSequence = await _context.Injects
            .Where(i => i.MselId == mselId)
            .MaxAsync(i => (int?)i.Sequence) ?? 0;

        // Create inject (system user until auth is implemented)
        var inject = request.ToEntity(mselId, maxInjectNumber + 1, maxSequence + 1, SystemConstants.SystemUserId);

        _context.Injects.Add(inject);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(inject).Reference(i => i.Phase).LoadAsync();

        _logger.LogInformation("Created inject {InjectId}: {InjectTitle} for exercise {ExerciseId}",
            inject.Id, inject.Title, exerciseId);

        return CreatedAtAction(
            nameof(GetInject),
            new { exerciseId, id = inject.Id },
            inject.ToDto()
        );
    }

    /// <summary>
    /// Update an existing inject.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InjectDto>> UpdateInject(Guid exerciseId, Guid id, UpdateInjectRequest request)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        var inject = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        // Check status-based edit restrictions
        if (exercise.Status == ExerciseStatus.Archived)
        {
            return BadRequest(new { message = "Archived exercises cannot be modified" });
        }

        // Validate request
        var validationError = ValidateInjectRequest(request.Title, request.Description, request.Target,
            request.ScenarioDay, request.ScenarioTime, request.Source, request.ExpectedAction,
            request.ControllerNotes, request.TriggerCondition);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        // Apply edit restrictions based on inject status
        if (inject.Status == InjectStatus.Fired)
        {
            // Only Notes can be edited on fired injects
            inject.ControllerNotes = request.ControllerNotes;
            inject.ModifiedBy = SystemConstants.SystemUserId;
        }
        else
        {
            // Full edit allowed for Pending/Skipped injects
            inject.UpdateFromRequest(request, SystemConstants.SystemUserId);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated inject {InjectId}: {InjectTitle}", inject.Id, inject.Title);

        return Ok(inject.ToDto());
    }

    /// <summary>
    /// Fire (deliver) an inject.
    /// </summary>
    [HttpPost("{id:guid}/fire")]
    public async Task<ActionResult<InjectDto>> FireInject(Guid exerciseId, Guid id, FireInjectRequest? request = null)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        var inject = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        // Only pending injects can be fired
        if (inject.Status != InjectStatus.Pending)
        {
            return BadRequest(new { message = $"Only pending injects can be fired. Current status: {inject.Status}" });
        }

        // Fire the inject (system user until auth is implemented)
        inject.Status = InjectStatus.Fired;
        inject.FiredAt = DateTime.UtcNow;
        inject.FiredBy = SystemConstants.SystemUserId;
        inject.ModifiedBy = SystemConstants.SystemUserId;

        // Add notes if provided
        if (!string.IsNullOrWhiteSpace(request?.Notes))
        {
            inject.ControllerNotes = string.IsNullOrEmpty(inject.ControllerNotes)
                ? $"[Fired] {request.Notes}"
                : $"{inject.ControllerNotes}\n[Fired] {request.Notes}";
        }

        await _context.SaveChangesAsync();

        // Reload user navigation property after setting FiredBy
        await _context.Entry(inject).Reference(i => i.FiredByUser).LoadAsync();

        var dto = inject.ToDto();

        // Broadcast SignalR notifications
        await _hubContext.NotifyInjectFired(exerciseId, dto);

        _logger.LogInformation("Fired inject {InjectId}: {InjectTitle} at {FiredAt}",
            inject.Id, inject.Title, inject.FiredAt);

        return Ok(dto);
    }

    /// <summary>
    /// Skip an inject.
    /// </summary>
    [HttpPost("{id:guid}/skip")]
    public async Task<ActionResult<InjectDto>> SkipInject(Guid exerciseId, Guid id, SkipInjectRequest request)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        var inject = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        // Only pending injects can be skipped
        if (inject.Status != InjectStatus.Pending)
        {
            return BadRequest(new { message = $"Only pending injects can be skipped. Current status: {inject.Status}" });
        }

        // Validate skip reason
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Skip reason is required" });
        }

        if (request.Reason.Length > 500)
        {
            return BadRequest(new { message = "Skip reason must be 500 characters or less" });
        }

        // Skip the inject (system user until auth is implemented)
        inject.Status = InjectStatus.Skipped;
        inject.SkippedAt = DateTime.UtcNow;
        inject.SkippedBy = SystemConstants.SystemUserId;
        inject.SkipReason = request.Reason;
        inject.ModifiedBy = SystemConstants.SystemUserId;

        await _context.SaveChangesAsync();

        // Reload user navigation property after setting SkippedBy
        await _context.Entry(inject).Reference(i => i.SkippedByUser).LoadAsync();

        var dto = inject.ToDto();

        // Broadcast SignalR notifications
        await _hubContext.NotifyInjectSkipped(exerciseId, dto);

        _logger.LogInformation("Skipped inject {InjectId}: {InjectTitle} - Reason: {SkipReason}",
            inject.Id, inject.Title, inject.SkipReason);

        return Ok(dto);
    }

    /// <summary>
    /// Reset an inject back to pending status.
    /// </summary>
    [HttpPost("{id:guid}/reset")]
    public async Task<ActionResult<InjectDto>> ResetInject(Guid exerciseId, Guid id)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        var inject = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        // Only fired or skipped injects can be reset
        if (inject.Status == InjectStatus.Pending)
        {
            return BadRequest(new { message = "Inject is already pending" });
        }

        // Reset the inject (system user until auth is implemented)
        inject.Status = InjectStatus.Pending;
        inject.FiredAt = null;
        inject.FiredBy = null;
        inject.SkippedAt = null;
        inject.SkippedBy = null;
        inject.SkipReason = null;
        inject.ModifiedBy = SystemConstants.SystemUserId;

        await _context.SaveChangesAsync();

        var dto = inject.ToDto();

        // Broadcast SignalR notifications
        await _hubContext.NotifyInjectReset(exerciseId, dto);

        _logger.LogInformation("Reset inject {InjectId}: {InjectTitle} to pending",
            inject.Id, inject.Title);

        return Ok(dto);
    }

    /// <summary>
    /// Delete an inject.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteInject(Guid exerciseId, Guid id)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        if (exercise.Status == ExerciseStatus.Archived)
        {
            return BadRequest(new { message = "Archived exercises cannot be modified" });
        }

        var inject = await _context.Injects
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        // Soft delete (system user until auth is implemented)
        inject.IsDeleted = true;
        inject.DeletedAt = DateTime.UtcNow;
        inject.DeletedBy = SystemConstants.SystemUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted inject {InjectId}: {InjectTitle}", inject.Id, inject.Title);

        return NoContent();
    }

    private static string? ValidateInjectRequest(
        string title, string description, string target,
        int? scenarioDay, TimeOnly? scenarioTime,
        string? source, string? expectedAction, string? controllerNotes, string? triggerCondition)
    {
        // Title validation
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Title is required";
        }
        if (title.Length < 3)
        {
            return "Title must be at least 3 characters";
        }
        if (title.Length > 200)
        {
            return "Title must be 200 characters or less";
        }

        // Description validation
        if (string.IsNullOrWhiteSpace(description))
        {
            return "Description is required";
        }
        if (description.Length > 4000)
        {
            return "Description must be 4000 characters or less";
        }

        // Target validation
        if (string.IsNullOrWhiteSpace(target))
        {
            return "Target is required";
        }
        if (target.Length > 200)
        {
            return "Target must be 200 characters or less";
        }

        // Scenario time validation
        if (scenarioTime.HasValue && !scenarioDay.HasValue)
        {
            return "Scenario Day is required when Scenario Time is provided";
        }
        if (scenarioDay.HasValue && (scenarioDay < 1 || scenarioDay > 99))
        {
            return "Scenario Day must be between 1 and 99";
        }

        // Optional field length validations
        if (source?.Length > 200)
        {
            return "Source must be 200 characters or less";
        }
        if (expectedAction?.Length > 2000)
        {
            return "Expected Action must be 2000 characters or less";
        }
        if (controllerNotes?.Length > 2000)
        {
            return "Controller Notes must be 2000 characters or less";
        }
        if (triggerCondition?.Length > 500)
        {
            return "Trigger Condition must be 500 characters or less";
        }

        return null;
    }
}
