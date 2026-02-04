using Cadence.Core.Features.Notifications.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Notifications.Services;

/// <summary>
/// Service interface for approval notification operations (S08: Approval Notifications).
/// </summary>
public interface IApprovalNotificationService
{
    /// <summary>
    /// Create notifications when an inject is submitted for approval.
    /// Notifies all Exercise Directors on the exercise.
    /// </summary>
    /// <param name="inject">The inject that was submitted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyInjectSubmittedAsync(Inject inject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create notification when an inject is approved.
    /// Notifies the inject author (unless they approved it themselves).
    /// </summary>
    /// <param name="inject">The inject that was approved</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyInjectApprovedAsync(Inject inject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create notification when an inject is rejected.
    /// Notifies the inject author with the rejection reason.
    /// </summary>
    /// <param name="inject">The inject that was rejected</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyInjectRejectedAsync(Inject inject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create notification when an approved inject is reverted.
    /// Notifies the inject author with the revert reason.
    /// </summary>
    /// <param name="inject">The inject that was reverted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyInjectRevertedAsync(Inject inject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create consolidated notifications for batch approval.
    /// Groups injects by author and creates one notification per author.
    /// </summary>
    /// <param name="approverUserId">User who approved the injects</param>
    /// <param name="injects">List of injects that were approved</param>
    /// <param name="notes">Optional notes from the approver</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyBatchApprovedAsync(
        string approverUserId,
        List<Inject> injects,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create consolidated notifications for batch rejection.
    /// Groups injects by author and creates one notification per author.
    /// </summary>
    /// <param name="approverUserId">User who rejected the injects</param>
    /// <param name="injects">List of injects that were rejected</param>
    /// <param name="reason">Reason for rejection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyBatchRejectedAsync(
        string approverUserId,
        List<Inject> injects,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notifications for the specified user.
    /// </summary>
    /// <param name="userId">User ID to get notifications for</param>
    /// <param name="limit">Maximum number of notifications to return (default: 20)</param>
    /// <param name="unreadOnly">Return only unread notifications (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of notifications ordered by created date descending</returns>
    Task<List<ApprovalNotificationDto>> GetNotificationsAsync(
        string userId,
        int limit = 20,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the count of unread notifications for the specified user.
    /// </summary>
    /// <param name="userId">User ID to get unread count for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of unread notifications</returns>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a single notification as read.
    /// </summary>
    /// <param name="userId">User ID (for authorization check)</param>
    /// <param name="notificationId">ID of the notification to mark as read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsReadAsync(string userId, Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications for the specified user as read.
    /// </summary>
    /// <param name="userId">User ID to mark all notifications as read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}
