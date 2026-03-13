using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Hubs;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for inject approval workflow.
/// Handles submit, approve, reject, batch operations, revert, and permission checks.
/// Shares the same route prefix as <see cref="InjectsController"/> — action routes do not collide.
/// </summary>
[ApiController]
[Route("api/exercises/{exerciseId:guid}/injects")]
[Authorize]
public class InjectApprovalsController : ControllerBase
{
    private readonly ILogger<InjectApprovalsController> _logger;
    private readonly IExerciseHubContext _hubContext;
    private readonly IInjectService _injectService;
    private readonly IInjectBatchApprovalService _injectBatchApprovalService;
    private readonly IApprovalPermissionService _approvalPermissionService;

    /// <summary>
    /// Initializes a new instance of <see cref="InjectApprovalsController"/>.
    /// </summary>
    public InjectApprovalsController(
        ILogger<InjectApprovalsController> logger,
        IExerciseHubContext hubContext,
        IInjectService injectService,
        IInjectBatchApprovalService injectBatchApprovalService,
        IApprovalPermissionService approvalPermissionService)
    {
        _logger = logger;
        _hubContext = hubContext;
        _injectService = injectService;
        _injectBatchApprovalService = injectBatchApprovalService;
        _approvalPermissionService = approvalPermissionService;
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
            var result = await _injectBatchApprovalService.BatchApproveAsync(exerciseId, request.InjectIds, request.Notes, userId);

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
            var result = await _injectBatchApprovalService.BatchRejectAsync(exerciseId, request.InjectIds, request.Reason, userId);

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
}
