using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Notifications.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Service for inject conduct operations (firing, skipping, resetting, approval workflow).
/// Batch approve/reject are delegated to <see cref="IInjectBatchApprovalService"/> when
/// available via DI; otherwise an inline instance is constructed to preserve test compatibility.
/// </summary>
public class InjectService : IInjectService
{
    private readonly AppDbContext _context;
    private readonly IExerciseHubContext _hubContext;
    private readonly IApprovalPermissionService _approvalPermissionService;
    private readonly IApprovalNotificationService _approvalNotificationService;
    private readonly IInjectBatchApprovalService _batchApprovalService;

    /// <summary>
    /// Primary constructor used by production DI — receives all dependencies explicitly.
    /// </summary>
    public InjectService(
        AppDbContext context,
        IExerciseHubContext hubContext,
        IApprovalPermissionService approvalPermissionService,
        IApprovalNotificationService approvalNotificationService,
        IInjectBatchApprovalService? batchApprovalService = null)
    {
        _context = context;
        _hubContext = hubContext;
        _approvalPermissionService = approvalPermissionService;
        _approvalNotificationService = approvalNotificationService;

        // Fall back to an inline instance so that tests which construct InjectService
        // directly (without providing IInjectBatchApprovalService) continue to work.
        _batchApprovalService = batchApprovalService
            ?? new InjectBatchApprovalService(context, approvalNotificationService, Microsoft.Extensions.Logging.Abstractions.NullLogger<InjectBatchApprovalService>.Instance);
    }

    /// <inheritdoc />
    public async Task<InjectDto> FireInjectAsync(Guid exerciseId, Guid injectId, string? userId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var (inject, exercise) = await GetInjectAndExerciseAsync(exerciseId, injectId, cancellationToken);

        // Validate exercise is active
        if (exercise.Status != ExerciseStatus.Active)
        {
            throw new InvalidOperationException($"Cannot fire inject. Exercise is {exercise.Status}. Injects can only be fired during an active exercise.");
        }

        // Validate inject can be fired based on delivery mode
        // In clock-driven mode, inject must be Synchronized
        // In facilitator-paced mode, inject can be Draft or Synchronized
        if (exercise.DeliveryMode == DeliveryMode.ClockDriven)
        {
            if (inject.Status != InjectStatus.Synchronized)
            {
                throw new InvalidOperationException($"Inject must be Synchronized to fire in clock-driven mode. Current status: {inject.Status}");
            }
        }
        else // FacilitatorPaced
        {
            if (inject.Status != InjectStatus.Draft && inject.Status != InjectStatus.Synchronized)
            {
                throw new InvalidOperationException($"Inject is already {inject.Status}. Only Draft or Synchronized injects can be fired.");
            }
        }

        inject.Status = InjectStatus.Released;
        inject.FiredAt = DateTime.UtcNow;
        // FiredByUserId is null for system auto-fire, otherwise store the user's ID
        inject.FiredByUserId = userId;
        inject.ModifiedBy = userId ?? Constants.SystemConstants.SystemUserIdString;
        inject.SkippedAt = null;
        inject.SkippedByUserId = null;

        // Append optional delivery notes to ControllerNotes for a clear delivery record
        if (!string.IsNullOrWhiteSpace(notes))
        {
            inject.ControllerNotes = string.IsNullOrEmpty(inject.ControllerNotes)
                ? $"[Fired] {notes}"
                : $"{inject.ControllerNotes}\n[Fired] {notes}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectFired(exerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<InjectDto> SkipInjectAsync(Guid exerciseId, Guid injectId, string userId, string skipReason = "Skipped", CancellationToken cancellationToken = default)
    {
        var (inject, exercise) = await GetInjectAndExerciseAsync(exerciseId, injectId, cancellationToken);

        // Validate exercise is active
        if (exercise.Status != ExerciseStatus.Active)
        {
            throw new InvalidOperationException($"Cannot skip inject. Exercise is {exercise.Status}. Injects can only be skipped during an active exercise.");
        }

        // Injects can be skipped from Draft or Synchronized status
        if (inject.Status != InjectStatus.Draft && inject.Status != InjectStatus.Synchronized)
        {
            throw new InvalidOperationException($"Inject is already {inject.Status}. Only Draft or Synchronized injects can be skipped.");
        }

        // Validate skip reason
        if (string.IsNullOrWhiteSpace(skipReason))
        {
            throw new InvalidOperationException("Skip reason is required.");
        }
        if (skipReason.Length > 500)
        {
            throw new InvalidOperationException("Skip reason must be 500 characters or less.");
        }

        inject.Status = InjectStatus.Deferred;
        inject.SkippedAt = DateTime.UtcNow;
        inject.SkippedByUserId = userId;
        inject.SkipReason = skipReason;
        inject.ModifiedBy = userId;
        inject.FiredAt = null;
        inject.FiredByUserId = null;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectSkipped(exerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<InjectDto> ResetInjectAsync(Guid exerciseId, Guid injectId, string userId, CancellationToken cancellationToken = default)
    {
        var (inject, exercise) = await GetInjectAndExerciseAsync(exerciseId, injectId, cancellationToken);

        // Validate exercise is active
        if (exercise.Status != ExerciseStatus.Active)
        {
            throw new InvalidOperationException($"Cannot reset inject. Exercise is {exercise.Status}. Injects can only be reset during an active exercise.");
        }

        inject.Status = InjectStatus.Draft;
        inject.ReadyAt = null;
        inject.FiredAt = null;
        inject.FiredByUserId = null;
        inject.SkippedAt = null;
        inject.SkippedByUserId = null;
        inject.ModifiedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectReset(exerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InjectDto>> ReorderInjectsAsync(Guid exerciseId, IEnumerable<Guid> injectIds, CancellationToken cancellationToken = default)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Exercise {exerciseId} not found.");

        if (exercise.ActiveMselId == null)
        {
            throw new InvalidOperationException("Exercise has no active MSEL.");
        }

        // Block reordering for archived exercises
        if (exercise.Status == ExerciseStatus.Archived)
        {
            throw new InvalidOperationException("Cannot reorder injects in an archived exercise.");
        }

        var injectIdsList = injectIds.ToList();
        if (injectIdsList.Count == 0)
        {
            throw new ArgumentException("InjectIds cannot be empty.", nameof(injectIds));
        }

        // Get all injects for this MSEL
        var injects = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .Include(i => i.InjectObjectives)
            .Where(i => i.MselId == exercise.ActiveMselId)
            .ToListAsync(cancellationToken);

        // Verify all provided IDs exist in this MSEL
        var injectDict = injects.ToDictionary(i => i.Id);
        foreach (var id in injectIdsList)
        {
            if (!injectDict.ContainsKey(id))
            {
                throw new KeyNotFoundException($"Inject {id} not found in this exercise.");
            }
        }

        // Two-phase update to avoid unique constraint violation on (MselId, InjectNumber).
        // Phase 1: Set InjectNumber to negative temporary values to clear the unique index.
        for (int i = 0; i < injectIdsList.Count; i++)
        {
            var inject = injectDict[injectIdsList[i]];
            inject.InjectNumber = -(i + 1);
        }
        await _context.SaveChangesAsync(cancellationToken);

        // Phase 2: Set the real sequence and inject number values.
        for (int i = 0; i < injectIdsList.Count; i++)
        {
            var inject = injectDict[injectIdsList[i]];
            inject.Sequence = i + 1;
            inject.InjectNumber = i + 1;
        }
        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast SignalR notification for inject reorder
        await _hubContext.NotifyInjectsReordered(exerciseId, injectIdsList);

        // Return the updated injects in the new order
        return injectIdsList.Select(id => injectDict[id].ToDto());
    }

    /// <inheritdoc />
    public async Task<InjectDto> SubmitForApprovalAsync(Guid exerciseId, Guid injectId, string userId, CancellationToken cancellationToken = default)
    {
        var (inject, exercise) = await GetInjectAndExerciseAsync(exerciseId, injectId, cancellationToken);

        // Validate approval workflow is enabled
        if (!exercise.RequireInjectApproval)
        {
            throw new InvalidOperationException(
                "Cannot submit for approval - approval workflow is not enabled for this exercise.");
        }

        // Validate current status
        if (inject.Status != InjectStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Only Draft injects can be submitted. Current status: {inject.Status}");
        }

        // Update status
        inject.Status = InjectStatus.Submitted;
        inject.SubmittedByUserId = userId;
        inject.SubmittedAt = DateTime.UtcNow;
        inject.ModifiedBy = userId;

        // Clear any previous rejection
        inject.RejectionReason = null;
        inject.RejectedByUserId = null;
        inject.RejectedAt = null;

        // Record status history
        var history = new InjectStatusHistory
        {
            Id = Guid.NewGuid(),
            InjectId = inject.Id,
            FromStatus = InjectStatus.Draft,
            ToStatus = InjectStatus.Submitted,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        _context.InjectStatusHistories.Add(history);

        await _context.SaveChangesAsync(cancellationToken);

        // Delegate broadcast and user notification to ApprovalNotificationService
        // (single point of truth for approval-related SignalR events + in-app notifications)
        await _approvalNotificationService.NotifyInjectSubmittedAsync(inject, cancellationToken);

        return inject.ToDto();
    }

    /// <inheritdoc />
    public async Task<InjectDto> ApproveInjectAsync(Guid exerciseId, Guid injectId, string userId, string? notes, CancellationToken cancellationToken = default)
    {
        return await ApproveInjectInternalAsync(exerciseId, injectId, userId, notes, confirmSelfApproval: false, cancellationToken);
    }

    /// <summary>
    /// Approve an inject with optional self-approval confirmation.
    /// Called by the controller when self-approval requires confirmation.
    /// </summary>
    public async Task<InjectDto> ApproveInjectWithConfirmationAsync(
        Guid exerciseId,
        Guid injectId,
        string userId,
        string? notes,
        bool confirmSelfApproval,
        CancellationToken cancellationToken = default)
    {
        return await ApproveInjectInternalAsync(exerciseId, injectId, userId, notes, confirmSelfApproval, cancellationToken);
    }

    private async Task<InjectDto> ApproveInjectInternalAsync(
        Guid exerciseId,
        Guid injectId,
        string userId,
        string? notes,
        bool confirmSelfApproval,
        CancellationToken cancellationToken)
    {
        var (inject, exercise) = await GetInjectAndExerciseAsync(exerciseId, injectId, cancellationToken);

        // Validate approval workflow is enabled
        if (!exercise.RequireInjectApproval)
        {
            throw new InvalidOperationException(
                "Cannot approve inject - approval workflow is not enabled for this exercise.");
        }

        // Validate current status
        if (inject.Status != InjectStatus.Submitted)
        {
            throw new InvalidOperationException(
                $"Only Submitted injects can be approved. Current status: {inject.Status}");
        }

        // Check approval permissions using the permission service
        var permissionCheck = await _approvalPermissionService.CanApproveInjectAsync(userId, injectId, cancellationToken);

        switch (permissionCheck.PermissionResult)
        {
            case ApprovalPermissionResult.NotAuthorized:
                throw new InvalidOperationException(
                    "You do not have permission to approve injects.");

            case ApprovalPermissionResult.SelfApprovalDenied:
                throw new InvalidOperationException(
                    "Cannot approve your own submission. Self-approval is not permitted by your organization.");

            case ApprovalPermissionResult.SelfApprovalWithWarning when !confirmSelfApproval:
                throw new InvalidOperationException(
                    "SELF_APPROVAL_CONFIRMATION_REQUIRED: You are approving your own submission. Set confirmSelfApproval to true to proceed.");
        }

        // Update status
        inject.Status = InjectStatus.Approved;
        inject.ApprovedByUserId = userId;
        inject.ApprovedAt = DateTime.UtcNow;
        inject.ApproverNotes = notes;
        inject.ModifiedBy = userId;

        // Clear any previous rejection
        inject.RejectionReason = null;
        inject.RejectedByUserId = null;
        inject.RejectedAt = null;

        // Record status history
        var history = new InjectStatusHistory
        {
            Id = Guid.NewGuid(),
            InjectId = inject.Id,
            FromStatus = InjectStatus.Submitted,
            ToStatus = InjectStatus.Approved,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow,
            Notes = notes,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        _context.InjectStatusHistories.Add(history);

        await _context.SaveChangesAsync(cancellationToken);

        // Delegate broadcast and user notification to ApprovalNotificationService
        // (single point of truth for approval-related SignalR events + in-app notifications)
        await _approvalNotificationService.NotifyInjectApprovedAsync(inject, cancellationToken);

        return inject.ToDto();
    }

    /// <inheritdoc />
    public async Task<InjectDto> RejectInjectAsync(Guid exerciseId, Guid injectId, string userId, string reason, CancellationToken cancellationToken = default)
    {
        var (inject, exercise) = await GetInjectAndExerciseAsync(exerciseId, injectId, cancellationToken);

        // Validate approval workflow is enabled
        if (!exercise.RequireInjectApproval)
        {
            throw new InvalidOperationException(
                "Cannot reject inject - approval workflow is not enabled for this exercise.");
        }

        // Validate current status
        if (inject.Status != InjectStatus.Submitted)
        {
            throw new InvalidOperationException(
                $"Only Submitted injects can be rejected. Current status: {inject.Status}");
        }

        // Validate reason is provided and has minimum length
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Rejection reason is required.");
        }
        if (reason.Length < 10)
        {
            throw new InvalidOperationException("Rejection reason must be at least 10 characters.");
        }

        // Update status - return to Draft
        inject.Status = InjectStatus.Draft;
        inject.RejectedByUserId = userId;
        inject.RejectedAt = DateTime.UtcNow;
        inject.RejectionReason = reason;
        inject.ModifiedBy = userId;

        // Clear submission tracking (will be re-set on resubmit)
        inject.SubmittedByUserId = null;
        inject.SubmittedAt = null;

        // Record status history
        var history = new InjectStatusHistory
        {
            Id = Guid.NewGuid(),
            InjectId = inject.Id,
            FromStatus = InjectStatus.Submitted,
            ToStatus = InjectStatus.Draft,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow,
            Notes = reason,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        _context.InjectStatusHistories.Add(history);

        await _context.SaveChangesAsync(cancellationToken);

        // Delegate broadcast and user notification to ApprovalNotificationService
        // (single point of truth for approval-related SignalR events + in-app notifications)
        await _approvalNotificationService.NotifyInjectRejectedAsync(inject, cancellationToken);

        return inject.ToDto();
    }

    /// <inheritdoc />
    public async Task<InjectDto> RevertApprovalAsync(Guid exerciseId, Guid injectId, string userId, string reason, CancellationToken cancellationToken = default)
    {
        var (inject, exercise) = await GetInjectAndExerciseAsync(exerciseId, injectId, cancellationToken);

        // Validate approval workflow is enabled
        if (!exercise.RequireInjectApproval)
        {
            throw new InvalidOperationException(
                "Cannot revert approval - approval workflow is not enabled for this exercise.");
        }

        // Validate current status - only Approved injects can be reverted
        if (inject.Status != InjectStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Only Approved injects can be reverted. Current status: {inject.Status}");
        }

        // Validate reason is provided and has minimum length
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Revert reason is required.");
        }
        if (reason.Length < 10)
        {
            throw new InvalidOperationException("Revert reason must be at least 10 characters.");
        }

        // Record who reverted and why
        inject.RevertedByUserId = userId;
        inject.RevertedAt = DateTime.UtcNow;
        inject.RevertReason = reason;

        // Clear approval info
        inject.ApprovedByUserId = null;
        inject.ApprovedAt = null;
        inject.ApproverNotes = null;

        // Return to Submitted status
        inject.Status = InjectStatus.Submitted;

        // Re-set submission timestamp (keep original submitter)
        inject.SubmittedAt = DateTime.UtcNow;

        inject.ModifiedBy = userId;

        // Record status history
        var history = new InjectStatusHistory
        {
            Id = Guid.NewGuid(),
            InjectId = inject.Id,
            FromStatus = InjectStatus.Approved,
            ToStatus = InjectStatus.Submitted,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow,
            Notes = $"Approval reverted: {reason}",
            CreatedBy = userId,
            ModifiedBy = userId
        };
        _context.InjectStatusHistories.Add(history);

        await _context.SaveChangesAsync(cancellationToken);

        // Delegate broadcast and user notification to ApprovalNotificationService
        // (single point of truth for approval-related SignalR events + in-app notifications)
        await _approvalNotificationService.NotifyInjectRevertedAsync(inject, cancellationToken);

        return inject.ToDto();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="IInjectBatchApprovalService"/> which holds the canonical
    /// implementation. Kept on <see cref="IInjectService"/> so that tests that construct
    /// <see cref="InjectService"/> directly continue to work without modification.
    /// </remarks>
    public Task<BatchApprovalResult> BatchApproveAsync(
        Guid exerciseId,
        IEnumerable<Guid> injectIds,
        string? notes,
        string userId,
        CancellationToken cancellationToken = default)
        => _batchApprovalService.BatchApproveAsync(exerciseId, injectIds, notes, userId, cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="IInjectBatchApprovalService"/> which holds the canonical
    /// implementation. Kept on <see cref="IInjectService"/> so that tests that construct
    /// <see cref="InjectService"/> directly continue to work without modification.
    /// </remarks>
    public Task<BatchApprovalResult> BatchRejectAsync(
        Guid exerciseId,
        IEnumerable<Guid> injectIds,
        string reason,
        string userId,
        CancellationToken cancellationToken = default)
        => _batchApprovalService.BatchRejectAsync(exerciseId, injectIds, reason, userId, cancellationToken);

    private async Task<(Inject inject, Exercise exercise)> GetInjectAndExerciseAsync(Guid exerciseId, Guid injectId, CancellationToken cancellationToken)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Exercise {exerciseId} not found.");

        if (exercise.ActiveMselId == null)
        {
            throw new InvalidOperationException("Exercise has no active MSEL.");
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
            .FirstOrDefaultAsync(i => i.Id == injectId && i.MselId == exercise.ActiveMselId, cancellationToken)
            ?? throw new KeyNotFoundException($"Inject {injectId} not found in exercise's active MSEL.");

        return (inject, exercise);
    }
}
