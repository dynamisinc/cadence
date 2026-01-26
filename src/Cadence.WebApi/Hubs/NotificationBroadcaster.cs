using Cadence.Core.Features.Notifications;
using Cadence.Core.Features.Notifications.Models.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Cadence.WebApi.Hubs;

/// <summary>
/// Implementation of INotificationBroadcaster for broadcasting notifications via SignalR.
/// </summary>
public class NotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<ExerciseHub> _hubContext;
    private readonly ILogger<NotificationBroadcaster> _logger;

    public NotificationBroadcaster(
        IHubContext<ExerciseHub> hubContext,
        ILogger<NotificationBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task BroadcastToUserAsync(string userId, NotificationDto notification)
    {
        // Send to user-specific group
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("NotificationCreated", notification);

        _logger.LogDebug(
            "Broadcast notification {NotificationId} to user {UserId}",
            notification.Id, userId);
    }

    /// <inheritdoc />
    public async Task BroadcastToUsersAsync(IEnumerable<string> userIds, NotificationDto notification)
    {
        var tasks = userIds.Select(userId => BroadcastToUserAsync(userId, notification));
        await Task.WhenAll(tasks);

        _logger.LogDebug(
            "Broadcast notification {NotificationId} to {UserCount} users",
            notification.Id, userIds.Count());
    }
}
