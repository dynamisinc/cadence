using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Notifications.Services;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Service for batch inject approval and rejection operations.
/// Handles the multi-inject workflow including self-approval policy enforcement,
/// status history recording, and consolidated approval notifications.
/// </summary>
public class InjectBatchApprovalService : IInjectBatchApprovalService
{
    private readonly AppDbContext _context;
    private readonly IApprovalNotificationService _approvalNotificationService;
    private readonly ILogger<InjectBatchApprovalService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="InjectBatchApprovalService"/>.
    /// </summary>
    public InjectBatchApprovalService(
        AppDbContext context,
        IApprovalNotificationService approvalNotificationService,
        ILogger<InjectBatchApprovalService> logger)
    {
        _context = context;
        _approvalNotificationService = approvalNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BatchApprovalResult> BatchApproveAsync(
        Guid exerciseId,
        IEnumerable<Guid> injectIds,
        string? notes,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var injectIdsList = injectIds.ToList();
        if (injectIdsList.Count == 0)
        {
            throw new InvalidOperationException("Must select at least one inject to approve.");
        }

        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Exercise {exerciseId} not found.");

        if (!exercise.RequireInjectApproval)
        {
            throw new InvalidOperationException(
                "Cannot batch approve - approval workflow is not enabled for this exercise.");
        }

        if (exercise.ActiveMselId == null)
        {
            throw new InvalidOperationException("Exercise has no active MSEL.");
        }

        var result = new BatchApprovalResult();

        // Get all requested injects
        var injects = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.InjectObjectives)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.ApprovedByUser)
            .Include(i => i.RejectedByUser)
            .Include(i => i.RevertedByUser)
            .Where(i => injectIdsList.Contains(i.Id) && i.MselId == exercise.ActiveMselId)
            .ToListAsync(cancellationToken);

        // Get organization self-approval policy (need org context)
        var org = await _context.Organizations.FindAsync(new object[] { exercise.OrganizationId }, cancellationToken);
        var selfApprovalPolicy = org?.SelfApprovalPolicy ?? SelfApprovalPolicy.NeverAllowed;

        var approvedInjects = new List<Inject>();

        foreach (var inject in injects)
        {
            // Skip non-submitted injects
            if (inject.Status != InjectStatus.Submitted)
            {
                result.SkippedCount++;
                result.SkippedReasons.Add(
                    $"INJ-{inject.InjectNumber:D3}: Not in Submitted status (current: {inject.Status})");
                continue;
            }

            // Check self-approval based on organization policy
            var isSelfSubmission = inject.SubmittedByUserId == userId;
            if (isSelfSubmission)
            {
                if (selfApprovalPolicy == SelfApprovalPolicy.NeverAllowed)
                {
                    result.SkippedCount++;
                    result.SkippedReasons.Add(
                        $"INJ-{inject.InjectNumber:D3}: Cannot approve your own submission (self-approval not permitted)");
                    continue;
                }
                else if (selfApprovalPolicy == SelfApprovalPolicy.AllowedWithWarning)
                {
                    // For batch approval, skip self-submissions that require confirmation.
                    // Users should use individual approval with confirmation for their own submissions.
                    result.SkippedCount++;
                    result.SkippedReasons.Add(
                        $"INJ-{inject.InjectNumber:D3}: Self-approval requires individual confirmation");
                    continue;
                }
                // If AlwaysAllowed, continue with approval
            }

            // Approve the inject
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

            result.ApprovedCount++;
            result.ProcessedInjects.Add(inject.ToDto());
            approvedInjects.Add(inject);
        }

        // Validate at least one inject was approved
        if (result.ApprovedCount == 0)
        {
            throw new InvalidOperationException(
                "Cannot approve - all selected injects were submitted by you or are not in Submitted status.");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Batch approved {ApprovedCount} injects in exercise {ExerciseId} by user {UserId}",
            result.ApprovedCount, exerciseId, userId);

        // Delegate broadcast and user notification to ApprovalNotificationService
        // (single point of truth for approval-related SignalR events + in-app notifications)
        await _approvalNotificationService.NotifyBatchApprovedAsync(userId, approvedInjects, notes, cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<BatchApprovalResult> BatchRejectAsync(
        Guid exerciseId,
        IEnumerable<Guid> injectIds,
        string reason,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var injectIdsList = injectIds.ToList();
        if (injectIdsList.Count == 0)
        {
            throw new InvalidOperationException("Must select at least one inject to reject.");
        }

        // Validate reason
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Rejection reason is required.");
        }
        if (reason.Length < 10)
        {
            throw new InvalidOperationException("Rejection reason must be at least 10 characters.");
        }
        if (reason.Length > 1000)
        {
            throw new InvalidOperationException("Rejection reason must be 1000 characters or less.");
        }

        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Exercise {exerciseId} not found.");

        if (!exercise.RequireInjectApproval)
        {
            throw new InvalidOperationException(
                "Cannot batch reject - approval workflow is not enabled for this exercise.");
        }

        if (exercise.ActiveMselId == null)
        {
            throw new InvalidOperationException("Exercise has no active MSEL.");
        }

        var result = new BatchApprovalResult();

        // Get all requested injects
        var injects = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.InjectObjectives)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.ApprovedByUser)
            .Include(i => i.RejectedByUser)
            .Include(i => i.RevertedByUser)
            .Where(i => injectIdsList.Contains(i.Id) && i.MselId == exercise.ActiveMselId)
            .ToListAsync(cancellationToken);

        var rejectedInjects = new List<Inject>();

        foreach (var inject in injects)
        {
            // Skip non-submitted injects
            if (inject.Status != InjectStatus.Submitted)
            {
                result.SkippedCount++;
                result.SkippedReasons.Add(
                    $"INJ-{inject.InjectNumber:D3}: Not in Submitted status (current: {inject.Status})");
                continue;
            }

            // Reject the inject (return to Draft)
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

            result.RejectedCount++;
            result.ProcessedInjects.Add(inject.ToDto());
            rejectedInjects.Add(inject);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Batch rejected {RejectedCount} injects in exercise {ExerciseId} by user {UserId}: {Reason}",
            result.RejectedCount, exerciseId, userId, reason);

        // Delegate broadcast and user notification to ApprovalNotificationService
        // (single point of truth for approval-related SignalR events + in-app notifications)
        await _approvalNotificationService.NotifyBatchRejectedAsync(userId, rejectedInjects, reason, cancellationToken);

        return result;
    }
}
