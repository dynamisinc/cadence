using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Phases.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for exercise phase management.
/// Phases organize injects into logical time segments.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}/phases")]
public class PhasesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PhasesController> _logger;

    public PhasesController(AppDbContext context, ILogger<PhasesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all phases for an exercise.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PhaseDto>>> GetPhases(Guid exerciseId)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        var phases = await _context.Phases
            .Where(p => p.ExerciseId == exerciseId)
            .OrderBy(p => p.Sequence)
            .ToListAsync();

        // Get inject counts per phase
        var injectCounts = await _context.Injects
            .Where(i => i.Msel!.ExerciseId == exerciseId && i.PhaseId != null)
            .GroupBy(i => i.PhaseId)
            .Select(g => new { PhaseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PhaseId!.Value, x => x.Count);

        var dtos = phases.Select(p => p.ToDto(injectCounts.GetValueOrDefault(p.Id, 0)));

        return Ok(dtos);
    }

    /// <summary>
    /// Get a single phase by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PhaseDto>> GetPhase(Guid exerciseId, Guid id)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        var phase = await _context.Phases
            .FirstOrDefaultAsync(p => p.Id == id && p.ExerciseId == exerciseId);

        if (phase == null)
        {
            return NotFound(new { message = "Phase not found" });
        }

        var injectCount = await _context.Injects
            .CountAsync(i => i.PhaseId == id);

        return Ok(phase.ToDto(injectCount));
    }

    /// <summary>
    /// Create a new phase.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PhaseDto>> CreatePhase(Guid exerciseId, CreatePhaseRequest request)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        // Validate request
        var validationError = ValidatePhaseRequest(request.Name, request.Description);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        // Check status-based restrictions
        if (exercise.Status == ExerciseStatus.Archived)
        {
            return BadRequest(new { message = "Archived exercises cannot be modified" });
        }

        // Get next sequence number
        var maxSequence = await _context.Phases
            .Where(p => p.ExerciseId == exerciseId)
            .MaxAsync(p => (int?)p.Sequence) ?? 0;

        // Create phase (system user until auth is implemented)
        var createdBy = SystemConstants.SystemUserId;
        var phase = request.ToEntity(exerciseId, maxSequence + 1, createdBy);

        _context.Phases.Add(phase);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created phase {PhaseId}: {PhaseName} for exercise {ExerciseId}",
            phase.Id, phase.Name, exerciseId);

        return CreatedAtAction(
            nameof(GetPhase),
            new { exerciseId, id = phase.Id },
            phase.ToDto(0)
        );
    }

    /// <summary>
    /// Update an existing phase.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PhaseDto>> UpdatePhase(Guid exerciseId, Guid id, UpdatePhaseRequest request)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        var phase = await _context.Phases
            .FirstOrDefaultAsync(p => p.Id == id && p.ExerciseId == exerciseId);

        if (phase == null)
        {
            return NotFound(new { message = "Phase not found" });
        }

        // Validate request
        var validationError = ValidatePhaseRequest(request.Name, request.Description);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        // Check status-based restrictions
        if (exercise.Status == ExerciseStatus.Archived)
        {
            return BadRequest(new { message = "Archived exercises cannot be modified" });
        }

        // Update phase (system user until auth is implemented)
        phase.UpdateFromRequest(request, SystemConstants.SystemUserId);
        await _context.SaveChangesAsync();

        var injectCount = await _context.Injects
            .CountAsync(i => i.PhaseId == id);

        _logger.LogInformation("Updated phase {PhaseId}: {PhaseName}", phase.Id, phase.Name);

        return Ok(phase.ToDto(injectCount));
    }

    /// <summary>
    /// Delete a phase (only if no injects are assigned).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeletePhase(Guid exerciseId, Guid id)
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

        var phase = await _context.Phases
            .FirstOrDefaultAsync(p => p.Id == id && p.ExerciseId == exerciseId);

        if (phase == null)
        {
            return NotFound(new { message = "Phase not found" });
        }

        // Check if phase has injects
        var injectCount = await _context.Injects
            .CountAsync(i => i.PhaseId == id);

        if (injectCount > 0)
        {
            return BadRequest(new { message = $"Cannot delete phase with {injectCount} inject(s). Move or delete the injects first." });
        }

        // Hard delete (phases don't need soft delete since they're organizational)
        _context.Phases.Remove(phase);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted phase {PhaseId}: {PhaseName}", phase.Id, phase.Name);

        return NoContent();
    }

    /// <summary>
    /// Reorder phases by providing the new sequence of phase IDs.
    /// </summary>
    [HttpPut("reorder")]
    public async Task<ActionResult<IEnumerable<PhaseDto>>> ReorderPhases(Guid exerciseId, ReorderPhasesRequest request)
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

        if (request.PhaseIds.Count == 0)
        {
            return BadRequest(new { message = "PhaseIds list is required" });
        }

        // Get all phases for this exercise
        var phases = await _context.Phases
            .Where(p => p.ExerciseId == exerciseId)
            .ToListAsync();

        // Validate all IDs are valid and belong to this exercise
        var phaseDict = phases.ToDictionary(p => p.Id);
        foreach (var phaseId in request.PhaseIds)
        {
            if (!phaseDict.ContainsKey(phaseId))
            {
                return BadRequest(new { message = $"Phase {phaseId} not found in this exercise" });
            }
        }

        // Update sequences (system user until auth is implemented)
        for (int i = 0; i < request.PhaseIds.Count; i++)
        {
            var phase = phaseDict[request.PhaseIds[i]];
            phase.Sequence = i + 1;
            phase.ModifiedBy = SystemConstants.SystemUserId;
        }

        await _context.SaveChangesAsync();

        // Get inject counts
        var injectCounts = await _context.Injects
            .Where(i => i.Msel!.ExerciseId == exerciseId && i.PhaseId != null)
            .GroupBy(i => i.PhaseId)
            .Select(g => new { PhaseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PhaseId!.Value, x => x.Count);

        var orderedPhases = phases
            .OrderBy(p => p.Sequence)
            .Select(p => p.ToDto(injectCounts.GetValueOrDefault(p.Id, 0)));

        _logger.LogInformation("Reordered {Count} phases for exercise {ExerciseId}",
            request.PhaseIds.Count, exerciseId);

        return Ok(orderedPhases);
    }

    private static string? ValidatePhaseRequest(string name, string? description)
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
        if (name.Length > 100)
        {
            return "Name must be 100 characters or less";
        }

        // Description validation
        if (description?.Length > 500)
        {
            return "Description must be 500 characters or less";
        }

        return null;
    }
}
