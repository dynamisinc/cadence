using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for inject (MSEL item) CRUD, conduct operations, and critical task linking.
/// Approval workflow endpoints are in <see cref="InjectApprovalsController"/>.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}/injects")]
[Authorize]
public class InjectsController : ControllerBase
{
    private readonly ILogger<InjectsController> _logger;
    private readonly IExerciseHubContext _hubContext;
    private readonly IInjectService _injectService;
    private readonly IInjectCrudService _injectCrudService;
    private readonly ICriticalTaskService _criticalTaskService;

    /// <summary>
    /// Initializes a new instance of <see cref="InjectsController"/>.
    /// </summary>
    public InjectsController(
        ILogger<InjectsController> logger,
        IExerciseHubContext hubContext,
        IInjectService injectService,
        IInjectCrudService injectCrudService,
        ICriticalTaskService criticalTaskService)
    {
        _logger = logger;
        _hubContext = hubContext;
        _injectService = injectService;
        _injectCrudService = injectCrudService;
        _criticalTaskService = criticalTaskService;
    }

    // =========================================================================
    // Read Operations
    // =========================================================================

    /// <summary>
    /// Get all injects for an exercise (via its active MSEL).
    /// Uses split query approach to avoid cartesian explosion with objectives.
    /// Supports filtering by status and by user submissions (S06: Approval Queue View).
    /// </summary>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="status">Optional filter by inject status (e.g., Submitted for pending approval)</param>
    /// <param name="mySubmissionsOnly">If true, only return injects submitted by current user</param>
    [HttpGet]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<IEnumerable<InjectDto>>> GetInjects(
        Guid exerciseId,
        [FromQuery] InjectStatus? status = null,
        [FromQuery] bool mySubmissionsOnly = false)
    {
        try
        {
            var currentUserId = User.TryGetUserId();
            if (currentUserId == null) return Unauthorized();
            var injects = await _injectCrudService.GetInjectsAsync(
                exerciseId, status, currentUserId, mySubmissionsOnly);
            return Ok(injects);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get a single inject by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<InjectDto>> GetInject(Guid exerciseId, Guid id)
    {
        try
        {
            var inject = await _injectCrudService.GetInjectAsync(exerciseId, id);
            if (inject == null)
            {
                return NotFound(new { message = "Inject not found" });
            }
            return Ok(inject);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get status change history for an inject (audit trail).
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<IEnumerable<InjectStatusHistoryDto>>> GetInjectHistory(
        Guid exerciseId, Guid id)
    {
        try
        {
            var history = await _injectCrudService.GetInjectHistoryAsync(exerciseId, id);
            return Ok(history);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // =========================================================================
    // Write Operations (CRUD)
    // =========================================================================

    /// <summary>
    /// Create a new inject.
    /// </summary>
    [HttpPost]
    [AuthorizeExerciseController]
    public async Task<ActionResult<InjectDto>> CreateInject(Guid exerciseId, CreateInjectRequest request)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            var inject = await _injectCrudService.CreateInjectAsync(exerciseId, request, userId);

            return CreatedAtAction(
                nameof(GetInject),
                new { exerciseId, id = inject.Id },
                inject);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message, errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    /// <summary>
    /// Update an existing inject.
    /// </summary>
    [HttpPut("{id:guid}")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<InjectDto>> UpdateInject(Guid exerciseId, Guid id, UpdateInjectRequest request)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            var (dto, statusReverted) = await _injectCrudService.UpdateInjectAsync(
                exerciseId, id, request, userId);

            // Notify via SignalR if approval status was automatically reverted due to content edit
            if (statusReverted)
            {
                await _hubContext.NotifyInjectStatusChanged(exerciseId, dto);
            }

            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message, errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    /// <summary>
    /// Delete an inject.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [AuthorizeExerciseController]
    public async Task<ActionResult> DeleteInject(Guid exerciseId, Guid id)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            await _injectCrudService.DeleteInjectAsync(exerciseId, id, userId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================================================================
    // Conduct Operations (Fire / Skip / Reset)
    // =========================================================================

    /// <summary>
    /// Fire (deliver) an inject.
    /// </summary>
    [HttpPost("{id:guid}/fire")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<InjectDto>> FireInject(Guid exerciseId, Guid id, FireInjectRequest? request = null)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();

            var dto = await _injectService.FireInjectAsync(exerciseId, id, userId, request?.Notes);

            _logger.LogInformation("Fired inject {InjectId} in exercise {ExerciseId} at {FiredAt}",
                id, exerciseId, DateTime.UtcNow);

            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Skip an inject.
    /// </summary>
    [HttpPost("{id:guid}/skip")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<InjectDto>> SkipInject(Guid exerciseId, Guid id, SkipInjectRequest request)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();

            var dto = await _injectService.SkipInjectAsync(exerciseId, id, userId, request.Reason);

            _logger.LogInformation("Skipped inject {InjectId} in exercise {ExerciseId} - Reason: {SkipReason}",
                id, exerciseId, request.Reason);

            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reset an inject back to pending status.
    /// </summary>
    [HttpPost("{id:guid}/reset")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<InjectDto>> ResetInject(Guid exerciseId, Guid id)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();

            var dto = await _injectService.ResetInjectAsync(exerciseId, id, userId);

            _logger.LogInformation("Reset inject {InjectId} in exercise {ExerciseId} to pending",
                id, exerciseId);

            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reorder injects by updating their sequence values.
    /// </summary>
    [HttpPost("reorder")]
    [AuthorizeExerciseController]
    public async Task<ActionResult> ReorderInjects(Guid exerciseId, ReorderInjectsRequest request)
    {
        if (request.InjectIds == null || request.InjectIds.Count == 0)
        {
            return BadRequest(new { message = "InjectIds is required" });
        }

        try
        {
            await _injectService.ReorderInjectsAsync(exerciseId, request.InjectIds);

            _logger.LogInformation("Reordered {Count} injects in exercise {ExerciseId}",
                request.InjectIds.Count, exerciseId);

            return Ok(new { message = "Injects reordered successfully" });
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

    // =========================================================================
    // Critical Task Linking (S05 - EEG)
    // =========================================================================

    /// <summary>
    /// Get linked Critical Tasks for an inject.
    /// Returns task IDs for populating multi-select in inject form.
    /// </summary>
    [HttpGet("{id:guid}/critical-tasks")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<List<Guid>>> GetLinkedCriticalTasks(Guid exerciseId, Guid id)
    {
        try
        {
            var taskIds = await _criticalTaskService.GetLinkedCriticalTaskIdsForInjectAsync(exerciseId, id);
            return Ok(taskIds);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Set linked Critical Tasks for an inject.
    /// Replaces all existing links with the provided task IDs.
    /// Tasks must belong to the same exercise.
    /// </summary>
    [HttpPut("{id:guid}/critical-tasks")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<List<CriticalTaskDto>>> SetLinkedCriticalTasks(
        Guid exerciseId,
        Guid id,
        [FromBody] SetLinkedCriticalTasksRequest request)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();

            var dtos = await _criticalTaskService.SetLinkedCriticalTasksForInjectAsync(
                exerciseId, id, request.CriticalTaskIds, userId);

            _logger.LogInformation(
                "Updated critical task links for inject {InjectId} in exercise {ExerciseId}: {TaskCount} tasks linked",
                id, exerciseId, dtos.Count);

            return Ok(dtos);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
