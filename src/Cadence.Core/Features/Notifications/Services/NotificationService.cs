using Cadence.Core.Data;
using Cadence.Core.Features.Notifications.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Notifications.Services;

/// <summary>
/// Service for managing user notifications.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<NotificationsResponse> GetNotificationsAsync(
        string userId,
        int limit = 10,
        int offset = 0,
        CancellationToken ct = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var unreadCount = await query.CountAsync(n => !n.IsRead, ct);

        var items = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        _logger.LogDebug(
            "Retrieved {Count} notifications for user {UserId} (total: {TotalCount}, unread: {UnreadCount})",
            items.Count, userId, totalCount, unreadCount);

        return new NotificationsResponse
        {
            Items = items.Select(n => n.ToDto()).ToList(),
            TotalCount = totalCount,
            UnreadCount = unreadCount
        };
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsDeleted && !n.IsRead)
            .CountAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsReadAsync(string userId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _context.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId && !n.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (notification == null)
        {
            _logger.LogWarning(
                "Notification {NotificationId} not found for user {UserId}",
                notificationId, userId);
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogDebug(
                "Marked notification {NotificationId} as read for user {UserId}",
                notificationId, userId);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Fetch unread notifications and update them
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsDeleted && !n.IsRead)
            .ToListAsync(ct);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Marked {Count} notifications as read for user {UserId}",
            unreadNotifications.Count, userId);

        return unreadNotifications.Count;
    }

    /// <inheritdoc />
    public async Task<NotificationDto> CreateNotificationAsync(
        CreateNotificationRequest request,
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Type = request.Type,
            Priority = request.Priority,
            Title = request.Title,
            Message = request.Message,
            ActionUrl = request.ActionUrl,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Created notification {NotificationId} of type {Type} for user {UserId}",
            notification.Id, notification.Type, notification.UserId);

        return notification.ToDto();
    }

    /// <inheritdoc />
    public async Task<List<NotificationDto>> CreateNotificationsForUsersAsync(
        IEnumerable<string> userIds,
        CreateNotificationRequest request,
        CancellationToken ct = default)
    {
        var notifications = userIds.Select(userId => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = request.Type,
            Priority = request.Priority,
            Title = request.Title,
            Message = request.Message,
            ActionUrl = request.ActionUrl,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            IsRead = false
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created {Count} notifications of type {Type}",
            notifications.Count, request.Type);

        return notifications.Select(n => n.ToDto()).ToList();
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldNotificationsAsync(DateTime olderThan, CancellationToken ct = default)
    {
        var oldNotifications = await _context.Notifications
            .Where(n => n.CreatedAt < olderThan)
            .ToListAsync(ct);

        _context.Notifications.RemoveRange(oldNotifications);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Deleted {Count} notifications older than {OlderThan}",
            oldNotifications.Count, olderThan);

        return oldNotifications.Count;
    }
}
