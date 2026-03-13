using Cadence.Core.Features.Injects.Models.DTOs;

namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Service interface for batch inject approval and rejection operations.
/// Extracted from <see cref="IInjectService"/> to keep that interface focused on
/// per-inject conduct operations (fire, skip, reset, single approve/reject).
/// </summary>
public interface IInjectBatchApprovalService
{
    /// <summary>
    /// Batch approve multiple submitted injects.
    /// Self-submissions are automatically skipped based on the organization's self-approval policy.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="injectIds">List of inject IDs to approve</param>
    /// <param name="notes">Optional approver notes applied to all approved injects</param>
    /// <param name="userId">The user who is approving (must be Director or Admin)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing counts of approved, skipped injects and skip reasons</returns>
    Task<BatchApprovalResult> BatchApproveAsync(
        Guid exerciseId,
        IEnumerable<Guid> injectIds,
        string? notes,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch reject multiple submitted injects, returning them to Draft status.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="injectIds">List of inject IDs to reject</param>
    /// <param name="reason">Required rejection reason (min 10, max 1000 characters)</param>
    /// <param name="userId">The user who is rejecting (must be Director or Admin)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing counts of rejected, skipped injects and skip reasons</returns>
    Task<BatchApprovalResult> BatchRejectAsync(
        Guid exerciseId,
        IEnumerable<Guid> injectIds,
        string reason,
        string userId,
        CancellationToken cancellationToken = default);
}
