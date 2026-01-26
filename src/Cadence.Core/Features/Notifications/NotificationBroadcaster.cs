using Cadence.Core.Features.Notifications.Models.DTOs;

namespace Cadence.Core.Features.Notifications;

/// <summary>
/// Interface for broadcasting notifications to connected users via SignalR.
/// Implemented in WebApi project to avoid SignalR dependency in Core.
/// </summary>
public interface INotificationBroadcaster
{
    /// <summary>
    /// Broadcast a notification to a specific user.
    /// </summary>
    /// <param name="userId">Target user ID</param>
    /// <param name="notification">Notification to send</param>
    Task BroadcastToUserAsync(string userId, NotificationDto notification);

    /// <summary>
    /// Broadcast a notification to multiple users.
    /// </summary>
    /// <param name="userIds">Target user IDs</param>
    /// <param name="notification">Notification to send</param>
    Task BroadcastToUsersAsync(IEnumerable<string> userIds, NotificationDto notification);
}
