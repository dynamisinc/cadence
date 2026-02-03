using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Service for inject conduct operations (firing, skipping, resetting).
/// </summary>
public class InjectService : IInjectService
{
    private readonly AppDbContext _context;
    private readonly IExerciseHubContext _hubContext;

    public InjectService(AppDbContext context, IExerciseHubContext hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task<InjectDto> FireInjectAsync(Guid exerciseId, Guid injectId, string? userId, CancellationToken cancellationToken = default)
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

        await _context.SaveChangesAsync(cancellationToken);

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectFired(exerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<InjectDto> SkipInjectAsync(Guid exerciseId, Guid injectId, string userId, CancellationToken cancellationToken = default)
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

        inject.Status = InjectStatus.Deferred;
        inject.SkippedAt = DateTime.UtcNow;
        inject.SkippedByUserId = userId;
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

        // Update sequence values based on the new order
        for (int i = 0; i < injectIdsList.Count; i++)
        {
            var inject = injectDict[injectIdsList[i]];
            inject.Sequence = i + 1;
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

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectSubmitted(exerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<InjectDto> ApproveInjectAsync(Guid exerciseId, Guid injectId, string userId, string? notes, CancellationToken cancellationToken = default)
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

        // Prevent self-approval (separation of duties)
        if (inject.SubmittedByUserId == userId)
        {
            throw new InvalidOperationException(
                "Cannot approve your own submission. Another Director or Administrator must approve.");
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

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectApproved(exerciseId, dto);

        return dto;
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

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectRejected(exerciseId, dto);

        return dto;
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
            .Where(i => injectIdsList.Contains(i.Id) && i.MselId == exercise.ActiveMselId)
            .ToListAsync(cancellationToken);

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

            // Skip self-submissions (separation of duties)
            if (inject.SubmittedByUserId == userId)
            {
                result.SkippedCount++;
                result.SkippedReasons.Add(
                    $"INJ-{inject.InjectNumber:D3}: Cannot approve your own submission");
                continue;
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
        }

        // Validate at least one inject was approved
        if (result.ApprovedCount == 0)
        {
            throw new InvalidOperationException(
                "Cannot approve - all selected injects were submitted by you or are not in Submitted status.");
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast SignalR notification for each approved inject
        foreach (var dto in result.ProcessedInjects)
        {
            await _hubContext.NotifyInjectApproved(exerciseId, dto);
        }

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
            .Where(i => injectIdsList.Contains(i.Id) && i.MselId == exercise.ActiveMselId)
            .ToListAsync(cancellationToken);

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
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast SignalR notification for each rejected inject
        foreach (var dto in result.ProcessedInjects)
        {
            await _hubContext.NotifyInjectRejected(exerciseId, dto);
        }

        return result;
    }

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
            .FirstOrDefaultAsync(i => i.Id == injectId && i.MselId == exercise.ActiveMselId, cancellationToken)
            ?? throw new KeyNotFoundException($"Inject {injectId} not found in exercise's active MSEL.");

        return (inject, exercise);
    }
}
