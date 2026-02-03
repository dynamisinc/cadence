using Cadence.Core.Features.Injects.Models.DTOs;

namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Service interface for inject conduct operations (firing, skipping, resetting).
/// </summary>
public interface IInjectService
{
    /// <summary>
    /// Fire an inject (deliver to players).
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="injectId">The inject ID</param>
    /// <param name="userId">The user who fired the inject, or null for system auto-fire</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<InjectDto> FireInjectAsync(Guid exerciseId, Guid injectId, string? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Skip an inject (intentionally not delivered).
    /// </summary>
    Task<InjectDto> SkipInjectAsync(Guid exerciseId, Guid injectId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset an inject back to pending status.
    /// </summary>
    Task<InjectDto> ResetInjectAsync(Guid exerciseId, Guid injectId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorder injects by updating their sequence values.
    /// </summary>
    Task<IEnumerable<InjectDto>> ReorderInjectsAsync(Guid exerciseId, IEnumerable<Guid> injectIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit an inject for approval, changing status from Draft to Submitted.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="injectId">The inject ID</param>
    /// <param name="userId">The user who is submitting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<InjectDto> SubmitForApprovalAsync(Guid exerciseId, Guid injectId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve a submitted inject.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="injectId">The inject ID</param>
    /// <param name="userId">The user who is approving (must be Director or Admin)</param>
    /// <param name="notes">Optional approver notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<InjectDto> ApproveInjectAsync(Guid exerciseId, Guid injectId, string userId, string? notes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reject a submitted inject, returning it to Draft status.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="injectId">The inject ID</param>
    /// <param name="userId">The user who is rejecting (must be Director or Admin)</param>
    /// <param name="reason">Required rejection reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<InjectDto> RejectInjectAsync(Guid exerciseId, Guid injectId, string userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch approve multiple submitted injects.
    /// Self-submissions are automatically skipped.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="injectIds">List of inject IDs to approve</param>
    /// <param name="notes">Optional approver notes</param>
    /// <param name="userId">The user who is approving (must be Director or Admin)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<BatchApprovalResult> BatchApproveAsync(Guid exerciseId, IEnumerable<Guid> injectIds, string? notes, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch reject multiple submitted injects.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="injectIds">List of inject IDs to reject</param>
    /// <param name="reason">Required rejection reason</param>
    /// <param name="userId">The user who is rejecting (must be Director or Admin)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<BatchApprovalResult> BatchRejectAsync(Guid exerciseId, IEnumerable<Guid> injectIds, string reason, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revert an approved inject back to Submitted status for re-review.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="injectId">The inject ID</param>
    /// <param name="userId">The user who is reverting (must be Director or Admin)</param>
    /// <param name="reason">Required revert reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<InjectDto> RevertApprovalAsync(Guid exerciseId, Guid injectId, string userId, string reason, CancellationToken cancellationToken = default);
}
