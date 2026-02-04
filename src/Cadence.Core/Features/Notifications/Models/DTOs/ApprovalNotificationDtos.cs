namespace Cadence.Core.Features.Notifications.Models.DTOs;

/// <summary>
/// DTO for approval notification data.
/// </summary>
public record ApprovalNotificationDto(
    Guid Id,
    string UserId,
    Guid ExerciseId,
    string ExerciseName,
    Guid? InjectId,
    string? InjectNumber,
    string Type,
    string Title,
    string Message,
    string? Metadata,
    string? TriggeredByUserId,
    string? TriggeredByName,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt
);
