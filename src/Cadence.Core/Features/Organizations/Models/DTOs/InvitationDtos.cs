using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Organizations.Models.DTOs;

/// <summary>
/// Request to create an organization invitation.
/// </summary>
public record CreateInvitationRequest(
    string Email,
    OrgRole Role = OrgRole.OrgUser
);

/// <summary>
/// DTO representing an organization invitation.
/// </summary>
public record InvitationDto(
    Guid Id,
    string Email,
    string Code,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string InvitedByName,
    string InvitedByEmail,
    DateTime? AcceptedAt,
    DateTime? CancelledAt,
    string? AcceptedByName,
    string? OrganizationName = null,
    bool? EmailSent = null,
    string? EmailError = null,
    bool AccountExists = false,
    List<PendingExerciseInfoDto>? PendingExercises = null
);

/// <summary>
/// Info about an exercise the invited user will be assigned to upon acceptance.
/// </summary>
public record PendingExerciseInfoDto(
    string ExerciseName,
    string ExerciseRole,
    string ExerciseType,
    DateOnly? ScheduledDate
);

/// <summary>
/// Response returned after sending an invitation.
/// </summary>
public record InvitationSentResponse(
    Guid InvitationId,
    string Email,
    string Message
);
