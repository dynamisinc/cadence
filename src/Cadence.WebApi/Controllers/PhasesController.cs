using System.Security.Claims;
using Cadence.Core.Features.Phases.Models.DTOs;
using Cadence.Core.Features.Phases.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for exercise phase management.
/// Phases organize injects into logical time segments.
/// Requires authentication for all endpoints.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}/phases")]
[Authorize]
public class PhasesController : ControllerBase
{
    private readonly IPhaseService _phaseService;
    private readonly ILogger<PhasesController> _logger;

    public PhasesController(IPhaseService phaseService, ILogger<PhasesController> logger)
    {
        _phaseService = phaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all phases for an exercise.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PhaseDto>>> GetPhases(Guid exerciseId)
    {
        var phases = await _phaseService.GetPhasesAsync(exerciseId);

        if (phases == null)
            return NotFound(new { message = "Exercise not found" });

        return Ok(phases);
    }

    /// <summary>
    /// Get a single phase by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PhaseDto>> GetPhase(Guid exerciseId, Guid id)
    {
        var phase = await _phaseService.GetPhaseAsync(exerciseId, id);

        if (phase == null)
            return NotFound(new { message = "Phase not found" });

        return Ok(phase);
    }

    /// <summary>
    /// Create a new phase.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PhaseDto>> CreatePhase(Guid exerciseId, CreatePhaseRequest request)
    {
        try
        {
            var phase = await _phaseService.CreatePhaseAsync(exerciseId, request, GetCurrentUserId());

            _logger.LogInformation("Created phase {PhaseId}: {PhaseName} for exercise {ExerciseId}",
                phase.Id, phase.Name, exerciseId);

            return CreatedAtAction(
                nameof(GetPhase),
                new { exerciseId, id = phase.Id },
                phase
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing phase.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PhaseDto>> UpdatePhase(Guid exerciseId, Guid id, UpdatePhaseRequest request)
    {
        try
        {
            var phase = await _phaseService.UpdatePhaseAsync(exerciseId, id, request, GetCurrentUserId());

            if (phase == null)
                return NotFound(new { message = "Phase not found" });

            _logger.LogInformation("Updated phase {PhaseId}: {PhaseName}", id, phase.Name);

            return Ok(phase);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a phase (only if no injects are assigned).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeletePhase(Guid exerciseId, Guid id)
    {
        try
        {
            var deleted = await _phaseService.DeletePhaseAsync(exerciseId, id);

            if (!deleted)
                return NotFound(new { message = "Phase not found" });

            _logger.LogInformation("Deleted phase {PhaseId} from exercise {ExerciseId}", id, exerciseId);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reorder phases by providing the new sequence of phase IDs.
    /// </summary>
    [HttpPut("reorder")]
    public async Task<ActionResult<IEnumerable<PhaseDto>>> ReorderPhases(Guid exerciseId, ReorderPhasesRequest request)
    {
        try
        {
            var phases = await _phaseService.ReorderPhasesAsync(exerciseId, request, GetCurrentUserId());

            if (phases == null)
                return NotFound(new { message = "Exercise not found" });

            _logger.LogInformation("Reordered {Count} phases for exercise {ExerciseId}",
                request.PhaseIds.Count, exerciseId);

            return Ok(phases);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the authenticated user's ID from JWT claims.
    /// </summary>
    private string GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User not authenticated");
        return userId;
    }
}
