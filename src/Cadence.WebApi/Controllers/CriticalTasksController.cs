using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Features.Eeg.Services;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for critical task management.
/// Critical tasks are specific actions required to achieve a capability target.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class CriticalTasksController : ControllerBase
{
    private readonly ICriticalTaskService _criticalTaskService;
    private readonly ILogger<CriticalTasksController> _logger;

    public CriticalTasksController(
        ICriticalTaskService criticalTaskService,
        ILogger<CriticalTasksController> logger)
    {
        _criticalTaskService = criticalTaskService;
        _logger = logger;
    }

    /// <summary>
    /// Get all critical tasks for a capability target.
    /// </summary>
    [HttpGet("capability-targets/{targetId:guid}/critical-tasks")]
    public async Task<ActionResult<CriticalTaskListResponse>> GetByCapabilityTarget(Guid targetId)
    {
        var response = await _criticalTaskService.GetByCapabilityTargetAsync(targetId);
        return Ok(response);
    }

    /// <summary>
    /// Get all critical tasks for an exercise (across all capability targets).
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/critical-tasks")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<CriticalTaskListResponse>> GetByExercise(
        Guid exerciseId,
        [FromQuery] bool? hasInjects = null,
        [FromQuery] bool? hasEegEntries = null)
    {
        var response = await _criticalTaskService.GetByExerciseAsync(exerciseId, hasInjects, hasEegEntries);
        return Ok(response);
    }

    /// <summary>
    /// Get a single critical task by ID.
    /// </summary>
    [HttpGet("critical-tasks/{id:guid}")]
    public async Task<ActionResult<CriticalTaskDto>> GetById(Guid id)
    {
        var task = await _criticalTaskService.GetByIdAsync(id);

        if (task == null)
            return NotFound();

        return Ok(task);
    }

    /// <summary>
    /// Create a new critical task for a capability target.
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpPost("exercises/{exerciseId:guid}/capability-targets/{targetId:guid}/critical-tasks")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<CriticalTaskDto>> Create(Guid exerciseId, Guid targetId, CreateCriticalTaskRequest request)
    {
        // exerciseId is used for authorization only - the targetId determines the parent
        // Validation
        if (string.IsNullOrWhiteSpace(request.TaskDescription))
            return BadRequest(new { message = "Task description is required" });

        if (request.TaskDescription.Length > 500)
            return BadRequest(new { message = "Task description must be 500 characters or less" });

        if (request.Standard?.Length > 1000)
            return BadRequest(new { message = "Standard must be 1000 characters or less" });

        try
        {
            var createdBy = User.GetUserId();
            var task = await _criticalTaskService.CreateAsync(targetId, request, createdBy);

            return CreatedAtAction(
                nameof(GetById),
                new { id = task.Id },
                task
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing critical task.
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpPut("exercises/{exerciseId:guid}/critical-tasks/{id:guid}")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<CriticalTaskDto>> Update(Guid exerciseId, Guid id, UpdateCriticalTaskRequest request)
    {
        // exerciseId is used for authorization only
        // Validation
        if (string.IsNullOrWhiteSpace(request.TaskDescription))
            return BadRequest(new { message = "Task description is required" });

        if (request.TaskDescription.Length > 500)
            return BadRequest(new { message = "Task description must be 500 characters or less" });

        if (request.Standard?.Length > 1000)
            return BadRequest(new { message = "Standard must be 1000 characters or less" });

        try
        {
            var modifiedBy = User.GetUserId();
            var task = await _criticalTaskService.UpdateAsync(id, request, modifiedBy);

            if (task == null)
                return NotFound();

            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a critical task (cascades to EEG entries and inject links).
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpDelete("exercises/{exerciseId:guid}/critical-tasks/{id:guid}")]
    [AuthorizeExerciseDirector]
    public async Task<IActionResult> Delete(Guid exerciseId, Guid id)
    {
        // exerciseId is used for authorization only
        var deletedBy = User.GetUserId();
        var deleted = await _criticalTaskService.DeleteAsync(id, deletedBy);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Reorder critical tasks within a capability target.
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpPut("exercises/{exerciseId:guid}/capability-targets/{targetId:guid}/critical-tasks/reorder")]
    [AuthorizeExerciseDirector]
    public async Task<IActionResult> Reorder(Guid exerciseId, Guid targetId, [FromBody] List<Guid> orderedIds)
    {
        // exerciseId is used for authorization only
        var success = await _criticalTaskService.ReorderAsync(targetId, orderedIds);

        if (!success)
            return BadRequest(new { message = "Failed to reorder critical tasks" });

        return NoContent();
    }

    /// <summary>
    /// Set linked injects for a critical task.
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpPut("exercises/{exerciseId:guid}/critical-tasks/{id:guid}/injects")]
    [AuthorizeExerciseDirector]
    public async Task<IActionResult> SetLinkedInjects(Guid exerciseId, Guid id, SetLinkedInjectsRequest request)
    {
        // exerciseId is used for authorization only
        var userId = User.GetUserId();
        var success = await _criticalTaskService.SetLinkedInjectsAsync(id, request.InjectIds, userId);

        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Get linked inject IDs for a critical task.
    /// </summary>
    [HttpGet("critical-tasks/{id:guid}/injects")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetLinkedInjects(Guid id)
    {
        var injectIds = await _criticalTaskService.GetLinkedInjectIdsAsync(id);
        return Ok(injectIds);
    }

}
