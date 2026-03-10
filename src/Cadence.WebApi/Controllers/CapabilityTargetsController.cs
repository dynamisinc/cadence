using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Features.Eeg.Services;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for capability target management.
/// Capability targets are exercise-specific measurable performance thresholds for capabilities.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class CapabilityTargetsController : ControllerBase
{
    private readonly ICapabilityTargetService _capabilityTargetService;
    private readonly ILogger<CapabilityTargetsController> _logger;

    public CapabilityTargetsController(
        ICapabilityTargetService capabilityTargetService,
        ILogger<CapabilityTargetsController> logger)
    {
        _capabilityTargetService = capabilityTargetService;
        _logger = logger;
    }

    /// <summary>
    /// Get all capability targets for an exercise.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/capability-targets")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<CapabilityTargetListResponse>> GetByExercise(Guid exerciseId)
    {
        var response = await _capabilityTargetService.GetByExerciseAsync(exerciseId);
        return Ok(response);
    }

    /// <summary>
    /// Get a single capability target by ID.
    /// </summary>
    [HttpGet("capability-targets/{id:guid}")]
    public async Task<ActionResult<CapabilityTargetDto>> GetById(Guid id)
    {
        var target = await _capabilityTargetService.GetByIdAsync(id);

        if (target == null)
            return NotFound();

        return Ok(target);
    }

    /// <summary>
    /// Create a new capability target for an exercise.
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpPost("exercises/{exerciseId:guid}/capability-targets")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<CapabilityTargetDto>> Create(Guid exerciseId, CreateCapabilityTargetRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.TargetDescription))
            return BadRequest(new { message = "Target description is required" });

        if (request.TargetDescription.Length > 500)
            return BadRequest(new { message = "Target description must be 500 characters or less" });

        if (request.CapabilityId == Guid.Empty)
            return BadRequest(new { message = "Capability ID is required" });

        try
        {
            var createdBy = User.GetUserId();
            var target = await _capabilityTargetService.CreateAsync(exerciseId, request, createdBy);

            return CreatedAtAction(
                nameof(GetById),
                new { id = target.Id },
                target
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing capability target.
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpPut("exercises/{exerciseId:guid}/capability-targets/{id:guid}")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<CapabilityTargetDto>> Update(Guid exerciseId, Guid id, UpdateCapabilityTargetRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.TargetDescription))
            return BadRequest(new { message = "Target description is required" });

        if (request.TargetDescription.Length > 500)
            return BadRequest(new { message = "Target description must be 500 characters or less" });

        try
        {
            var modifiedBy = User.GetUserId();
            var target = await _capabilityTargetService.UpdateAsync(id, request, modifiedBy);

            if (target == null)
                return NotFound();

            return Ok(target);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a capability target (cascades to critical tasks).
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpDelete("exercises/{exerciseId:guid}/capability-targets/{id:guid}")]
    [AuthorizeExerciseDirector]
    public async Task<IActionResult> Delete(Guid exerciseId, Guid id)
    {
        var deletedBy = User.GetUserId();
        var deleted = await _capabilityTargetService.DeleteAsync(id, deletedBy);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Reorder capability targets within an exercise.
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpPut("exercises/{exerciseId:guid}/capability-targets/reorder")]
    [AuthorizeExerciseDirector]
    public async Task<IActionResult> Reorder(Guid exerciseId, [FromBody] List<Guid> orderedIds)
    {
        var success = await _capabilityTargetService.ReorderAsync(exerciseId, orderedIds);

        if (!success)
            return BadRequest(new { message = "Failed to reorder capability targets" });

        return NoContent();
    }

}
