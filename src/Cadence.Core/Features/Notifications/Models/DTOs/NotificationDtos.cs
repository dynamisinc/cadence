using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Notifications.Models.DTOs;

/// <summary>
/// DTO representing a notification for display.
/// </summary>
public record NotificationDto
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Notification type.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Priority level.
    /// </summary>
    public string Priority { get; init; } = string.Empty;

    /// <summary>
    /// Short title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Detailed message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// URL to navigate to when clicked.
    /// </summary>
    public string? ActionUrl { get; init; }

    /// <summary>
    /// Related entity type.
    /// </summary>
    public string? RelatedEntityType { get; init; }

    /// <summary>
    /// Related entity ID.
    /// </summary>
    public Guid? RelatedEntityId { get; init; }

    /// <summary>
    /// Whether read by user.
    /// </summary>
    public bool IsRead { get; init; }

    /// <summary>
    /// When created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When read.
    /// </summary>
    public DateTime? ReadAt { get; init; }
}

/// <summary>
/// Response containing notifications with pagination info.
/// </summary>
public record NotificationsResponse
{
    /// <summary>
    /// List of notifications.
    /// </summary>
    public List<NotificationDto> Items { get; init; } = new();

    /// <summary>
    /// Total count of notifications.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Unread count.
    /// </summary>
    public int UnreadCount { get; init; }
}

/// <summary>
/// Request to create a notification.
/// </summary>
public record CreateNotificationRequest
{
    /// <summary>
    /// User ID to send notification to.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Notification type.
    /// </summary>
    public NotificationType Type { get; init; }

    /// <summary>
    /// Priority level.
    /// </summary>
    public NotificationPriority Priority { get; init; }

    /// <summary>
    /// Short title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Detailed message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Optional URL to navigate to.
    /// </summary>
    public string? ActionUrl { get; init; }

    /// <summary>
    /// Type of related entity.
    /// </summary>
    public string? RelatedEntityType { get; init; }

    /// <summary>
    /// ID of related entity.
    /// </summary>
    public Guid? RelatedEntityId { get; init; }
}

/// <summary>
/// Extension methods for mapping Notification entities to DTOs.
/// </summary>
public static class NotificationMapper
{
    public static NotificationDto ToDto(this Notification entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type.ToString(),
        Priority = entity.Priority.ToString(),
        Title = entity.Title,
        Message = entity.Message,
        ActionUrl = entity.ActionUrl,
        RelatedEntityType = entity.RelatedEntityType,
        RelatedEntityId = entity.RelatedEntityId,
        IsRead = entity.IsRead,
        CreatedAt = entity.CreatedAt,
        ReadAt = entity.ReadAt
    };
}
