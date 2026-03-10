using Cadence.Core.Data;
using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for inject (MSEL item) management.
/// Injects belong to MSELs which belong to Exercises.
/// Requires authentication for all endpoints.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}/injects")]
[Authorize]
public class InjectsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<InjectsController> _logger;
    private readonly IExerciseHubContext _hubContext;
    private readonly IInjectService _injectService;
    private readonly IInjectCrudService _injectCrudService;
    private readonly IApprovalPermissionService _approvalPermissionService;
    private readonly ICriticalTaskService _criticalTaskService;

    /// <summary>
    /// Initializes a new instance of <see cref="InjectsController"/>.
    /// </summary>
    public InjectsController(
        AppDbContext context,
        ILogger<InjectsController> logger,
        IExerciseHubContext hubContext,
        IInjectService injectService,
        IInjectCrudService injectCrudService,
        IApprovalPermissionService approvalPermissionService,
        ICriticalTaskService criticalTaskService)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
        _injectService = injectService;
        _injectCrudService = injectCrudService;
        _approvalPermissionService = approvalPermissionService;
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
    // These remain in the controller because their parameter signatures
    // (Notes, Reason) do not match the IInjectService interface, and they
    // also perform delivery-mode validation and SignalR broadcasting
    // that is tightly coupled to the HTTP request context.
    // =========================================================================

    /// <summary>
    /// Fire (deliver) an inject.
    /// </summary>
    [HttpPost("{id:guid}/fire")]
    [AuthorizeExerciseController]
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
            .Include(i => i.InjectObjectives)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.ApprovedByUser)
            .Include(i => i.RejectedByUser)
            .Include(i => i.RevertedByUser)
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        // Validate inject can be fired based on delivery mode.
        // Clock-driven mode: inject must be Synchronized.
        // Facilitator-paced mode: inject can be Draft or Synchronized.
        if (exercise.DeliveryMode == DeliveryMode.ClockDriven)
        {
            if (inject.Status != InjectStatus.Synchronized)
            {
                return BadRequest(new { message = $"Inject must be Synchronized to fire in clock-driven mode. Current status: {inject.Status}" });
            }
        }
        else // FacilitatorPaced
        {
            if (inject.Status != InjectStatus.Draft && inject.Status != InjectStatus.Synchronized)
            {
                return BadRequest(new { message = $"Inject is already {inject.Status}. Only Draft or Synchronized injects can be fired." });
            }
        }

        var userId = User.TryGetUserId();
        if (userId == null) return Unauthorized();

        inject.Status = InjectStatus.Released;
        inject.FiredAt = DateTime.UtcNow;
        inject.FiredByUserId = userId;
        inject.ModifiedBy = userId;

        // Append optional notes to ControllerNotes for a clear delivery record
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

        await _hubContext.NotifyInjectFired(exerciseId, dto);

        _logger.LogInformation("Fired inject {InjectId}: {InjectTitle} at {FiredAt}",
            inject.Id, inject.Title, inject.FiredAt);

        return Ok(dto);
    }

    /// <summary>
    /// Skip an inject.
    /// </summary>
    [HttpPost("{id:guid}/skip")]
    [AuthorizeExerciseController]
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
            .Include(i => i.InjectObjectives)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.ApprovedByUser)
            .Include(i => i.RejectedByUser)
            .Include(i => i.RevertedByUser)
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        // Injects can be skipped from Draft or Synchronized status
        if (inject.Status != InjectStatus.Draft && inject.Status != InjectStatus.Synchronized)
        {
            return BadRequest(new { message = $"Only Draft or Synchronized injects can be skipped. Current status: {inject.Status}" });
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

        var userId = User.TryGetUserId();
        if (userId == null) return Unauthorized();

        inject.Status = InjectStatus.Deferred;
        inject.SkippedAt = DateTime.UtcNow;
        inject.SkippedByUserId = userId;
        inject.SkipReason = request.Reason;
        inject.ModifiedBy = userId;

        await _context.SaveChangesAsync();

        // Reload user navigation property after setting SkippedBy
        await _context.Entry(inject).Reference(i => i.SkippedByUser).LoadAsync();

        var dto = inject.ToDto();

        await _hubContext.NotifyInjectSkipped(exerciseId, dto);

        _logger.LogInformation("Skipped inject {InjectId}: {InjectTitle} - Reason: {SkipReason}",
            inject.Id, inject.Title, inject.SkipReason);

        return Ok(dto);
    }

    /// <summary>
    /// Reset an inject back to pending status.
    /// </summary>
    [HttpPost("{id:guid}/reset")]
    [AuthorizeExerciseController]
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
            .Include(i => i.InjectObjectives)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.ApprovedByUser)
            .Include(i => i.RejectedByUser)
            .Include(i => i.RevertedByUser)
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        // Only released or deferred injects can be reset
        if (inject.Status == InjectStatus.Draft)
        {
            return BadRequest(new { message = "Inject is already in draft" });
        }

        var userId = User.TryGetUserId();
        if (userId == null) return Unauthorized();

        inject.Status = InjectStatus.Draft;
        inject.FiredAt = null;
        inject.FiredByUserId = null;
        inject.SkippedAt = null;
        inject.SkippedByUserId = null;
        inject.SkipReason = null;
        inject.ModifiedBy = userId;

        await _context.SaveChangesAsync();

        var dto = inject.ToDto();

        await _hubContext.NotifyInjectReset(exerciseId, dto);

        _logger.LogInformation("Reset inject {InjectId}: {InjectTitle} to pending",
            inject.Id, inject.Title);

        return Ok(dto);
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
    // Approval Workflow Operations
    // =========================================================================

    /// <summary>
    /// Submit an inject for approval.
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<InjectDto>> SubmitForApproval(Guid exerciseId, Guid id)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            var result = await _injectService.SubmitForApprovalAsync(exerciseId, id, userId);

            _logger.LogInformation("Inject {InjectId} submitted for approval by {UserId}", id, userId);

            return Ok(result);
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
    /// Approve a submitted inject.
    /// Only Exercise Directors or Administrators can approve injects.
    /// Self-approval behavior depends on organization policy (S11):
    /// - NeverAllowed: Cannot approve own submissions
    /// - AllowedWithWarning: Requires confirmSelfApproval=true
    /// - AlwaysAllowed: No restrictions
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<InjectDto>> ApproveInject(Guid exerciseId, Guid id, [FromBody] ApproveInjectRequest? request = null)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            InjectDto result;

            // Use the confirmation-aware method when self-approval flag is provided (S11)
            if (request?.ConfirmSelfApproval == true)
            {
                result = await _injectService.ApproveInjectWithConfirmationAsync(
                    exerciseId, id, userId, request.Notes, confirmSelfApproval: true);
            }
            else
            {
                result = await _injectService.ApproveInjectAsync(exerciseId, id, userId, request?.Notes);
            }

            _logger.LogInformation("Approved inject {InjectId} in exercise {ExerciseId} by user {UserId}",
                id, exerciseId, userId);

            return Ok(result);
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
    /// Reject a submitted inject, returning it to Draft status.
    /// Only Exercise Directors or Administrators can reject injects.
    /// Rejection reason is required (min 10 characters) to provide feedback to the author.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<InjectDto>> RejectInject(Guid exerciseId, Guid id, [FromBody] RejectInjectRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { message = "Rejection reason is required" });
            }

            if (request.Reason.Length < 10)
            {
                return BadRequest(new { message = "Rejection reason must be at least 10 characters" });
            }

            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            var result = await _injectService.RejectInjectAsync(exerciseId, id, userId, request.Reason);

            _logger.LogInformation("Rejected inject {InjectId} in exercise {ExerciseId} by user {UserId}: {Reason}",
                id, exerciseId, userId, request.Reason);

            return Ok(result);
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
    /// Batch approve multiple submitted injects.
    /// Only Exercise Directors or Administrators can approve injects.
    /// Self-submissions are automatically skipped (separation of duties).
    /// </summary>
    [HttpPost("batch/approve")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<BatchApprovalResult>> BatchApprove(
        Guid exerciseId,
        [FromBody] BatchApproveRequest request)
    {
        try
        {
            if (request.InjectIds == null || request.InjectIds.Count == 0)
            {
                return BadRequest(new { message = "InjectIds is required and must contain at least one inject" });
            }

            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            var result = await _injectService.BatchApproveAsync(exerciseId, request.InjectIds, request.Notes, userId);

            _logger.LogInformation("Batch approved {Count} injects in exercise {ExerciseId} by user {UserId}",
                result.ApprovedCount, exerciseId, userId);

            return Ok(result);
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
    /// Batch reject multiple submitted injects, returning them to Draft status.
    /// Only Exercise Directors or Administrators can reject injects.
    /// Rejection reason is required (min 10 characters) to provide feedback to authors.
    /// </summary>
    [HttpPost("batch/reject")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<BatchApprovalResult>> BatchReject(
        Guid exerciseId,
        [FromBody] BatchRejectRequest request)
    {
        try
        {
            if (request.InjectIds == null || request.InjectIds.Count == 0)
            {
                return BadRequest(new { message = "InjectIds is required and must contain at least one inject" });
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { message = "Rejection reason is required" });
            }

            if (request.Reason.Length < 10)
            {
                return BadRequest(new { message = "Rejection reason must be at least 10 characters" });
            }

            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            var result = await _injectService.BatchRejectAsync(exerciseId, request.InjectIds, request.Reason, userId);

            _logger.LogInformation("Batch rejected {Count} injects in exercise {ExerciseId} by user {UserId}: {Reason}",
                result.RejectedCount, exerciseId, userId, request.Reason);

            return Ok(result);
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
    /// Revert an approved inject back to Submitted status for re-review.
    /// Only Exercise Directors or Administrators can revert approvals.
    /// Revert reason is required (min 10 characters) to explain why re-review is needed.
    /// </summary>
    [HttpPost("{id:guid}/revert")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<InjectDto>> RevertApproval(
        Guid exerciseId,
        Guid id,
        [FromBody] RevertApprovalRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { message = "Revert reason is required" });
            }

            if (request.Reason.Length < 10)
            {
                return BadRequest(new { message = "Revert reason must be at least 10 characters" });
            }

            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            var result = await _injectService.RevertApprovalAsync(exerciseId, id, userId, request.Reason);

            _logger.LogInformation("Reverted inject {InjectId} in exercise {ExerciseId} by user {UserId}: {Reason}",
                id, exerciseId, userId, request.Reason);

            return Ok(result);
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
    // Approval Permission Check (S11)
    // =========================================================================

    /// <summary>
    /// Check if the current user can approve a specific inject.
    /// Returns permission details including whether self-approval is allowed/required confirmation.
    /// Used by frontend to conditionally render approve/reject buttons.
    /// </summary>
    [HttpGet("{id:guid}/can-approve")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<InjectApprovalCheckDto>> CheckApprovalPermission(Guid exerciseId, Guid id)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            var result = await _approvalPermissionService.CanApproveInjectAsync(userId, id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Check if the current user can approve injects for this exercise (general check).
    /// Returns true if user's role is authorized to approve.
    /// </summary>
    [HttpGet("can-approve")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<bool>> CheckExerciseApprovalPermission(Guid exerciseId)
    {
        try
        {
            var userId = User.TryGetUserId();
            if (userId == null) return Unauthorized();
            var canApprove = await _approvalPermissionService.CanApproveAsync(userId, exerciseId);
            return Ok(new { canApprove });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        // Verify inject belongs to this exercise
        var inject = await _context.Injects
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        var taskIds = await _context.InjectCriticalTasks
            .Where(ict => ict.InjectId == id)
            .Select(ict => ict.CriticalTaskId)
            .ToListAsync();

        return Ok(taskIds);
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
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        // Verify inject belongs to this exercise
        var inject = await _context.Injects
            .Include(i => i.LinkedCriticalTasks)
            .FirstOrDefaultAsync(i => i.Id == id && i.MselId == exercise.ActiveMselId);

        if (inject == null)
        {
            return NotFound(new { message = "Inject not found" });
        }

        // Validate all task IDs belong to this exercise
        var validTaskIds = await _context.CriticalTasks
            .Where(ct => request.CriticalTaskIds.Contains(ct.Id))
            .Where(ct => ct.CapabilityTarget.ExerciseId == exerciseId)
            .Select(ct => ct.Id)
            .ToListAsync();

        var invalidTaskIds = request.CriticalTaskIds.Except(validTaskIds).ToList();
        if (invalidTaskIds.Count > 0)
        {
            return BadRequest(new { message = $"Invalid or cross-exercise task IDs: {string.Join(", ", invalidTaskIds)}" });
        }

        // Clear existing links
        _context.InjectCriticalTasks.RemoveRange(inject.LinkedCriticalTasks);

        // Add new links with audit fields
        var userId = User.TryGetUserId();
        if (userId == null) return Unauthorized();
        var now = DateTime.UtcNow;
        foreach (var taskId in validTaskIds)
        {
            inject.LinkedCriticalTasks.Add(new InjectCriticalTask
            {
                InjectId = id,
                CriticalTaskId = taskId,
                CreatedAt = now,
                CreatedBy = userId
            });
        }

        inject.ModifiedBy = userId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated critical task links for inject {InjectId}: {TaskCount} tasks linked",
            id, validTaskIds.Count);

        // Return the linked tasks with full details
        var linkedTasks = await _context.CriticalTasks
            .Include(ct => ct.LinkedInjects)
            .Include(ct => ct.EegEntries)
            .Where(ct => validTaskIds.Contains(ct.Id))
            .OrderBy(ct => ct.SortOrder)
            .ToListAsync();

        var dtos = linkedTasks.Select(ct => new CriticalTaskDto(
            ct.Id,
            ct.CapabilityTargetId,
            ct.TaskDescription,
            ct.Standard,
            ct.SortOrder,
            ct.LinkedInjects?.Count ?? 0,
            ct.EegEntries?.Count ?? 0,
            ct.CreatedAt,
            ct.UpdatedAt
        )).ToList();

        return Ok(dtos);
    }
}
