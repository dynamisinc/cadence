using Cadence.Core.Features.Notifications.Models.DTOs;

namespace Cadence.Core.Features.Notifications.Services;

/// <summary>
/// Service for managing user notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Get notifications for a user with pagination.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Maximum notifications to return</param>
    /// <param name="offset">Number of notifications to skip</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Notifications with counts</returns>
    Task<NotificationsResponse> GetNotificationsAsync(
        string userId,
        int limit = 10,
        int offset = 0,
        CancellationToken ct = default);

    /// <summary>
    /// Get unread notification count for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Unread count</returns>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Mark a notification as read.
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if marked read, false if not found or unauthorized</returns>
    Task<bool> MarkAsReadAsync(string userId, Guid notificationId, CancellationToken ct = default);

    /// <summary>
    /// Mark all notifications as read for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of notifications marked read</returns>
    Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Create a new notification.
    /// </summary>
    /// <param name="request">Notification creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created notification DTO</returns>
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Create notifications for multiple users (e.g., all exercise participants).
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="request">Base notification request (UserId will be overridden)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of created notification DTOs</returns>
    Task<List<NotificationDto>> CreateNotificationsForUsersAsync(
        IEnumerable<string> userIds,
        CreateNotificationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Delete old notifications (for cleanup).
    /// </summary>
    /// <param name="olderThan">Delete notifications older than this date</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of notifications deleted</returns>
    Task<int> DeleteOldNotificationsAsync(DateTime olderThan, CancellationToken ct = default);
}
