namespace Cadence.Core.Features.Exercises.Models.DTOs;

/// <summary>
/// DTO representing an exercise participant with role information.
/// </summary>
public record ExerciseParticipantDto
{
    /// <summary>
    /// Participant record ID (ExerciseParticipant.Id).
    /// </summary>
    public string ParticipantId { get; init; } = string.Empty;

    /// <summary>
    /// User ID (ApplicationUser.Id string).
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// User's system-level role (Admin, Manager, User).
    /// </summary>
    public string SystemRole { get; init; } = string.Empty;

    /// <summary>
    /// User's HSEEP role in this specific exercise.
    /// </summary>
    public string ExerciseRole { get; init; } = string.Empty;

    /// <summary>
    /// Effective role (same as ExerciseRole for participants).
    /// </summary>
    public string EffectiveRole { get; init; } = string.Empty;

    /// <summary>
    /// When this participant was added to the exercise.
    /// </summary>
    public string AddedAt { get; init; } = string.Empty;

    /// <summary>
    /// Who added this participant (nullable).
    /// </summary>
    public string? AddedBy { get; init; }
}

/// <summary>
/// Request to add a participant to an exercise.
/// </summary>
public record AddParticipantRequest
{
    /// <summary>
    /// User ID to add as participant (ApplicationUser.Id string).
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// HSEEP role for this participant in the exercise.
    /// Required - must specify one of the ExerciseRole enum values.
    /// </summary>
    public string Role { get; init; } = string.Empty;
}

/// <summary>
/// Request to update a participant's HSEEP role in an exercise.
/// </summary>
public record UpdateParticipantRoleRequest
{
    /// <summary>
    /// New HSEEP role for the participant in this exercise.
    /// Must be one of the ExerciseRole enum values.
    /// </summary>
    public string Role { get; init; } = string.Empty;
}

/// <summary>
/// Request to update multiple participants at once.
/// </summary>
public record BulkUpdateParticipantsRequest
{
    /// <summary>
    /// List of participants to add or update.
    /// </summary>
    public List<AddParticipantRequest> Participants { get; init; } = new();
}
