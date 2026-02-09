namespace Cadence.Core.Features.Email.Models;

/// <summary>
/// Template model for exercise invitation emails (existing org members).
/// </summary>
public class ExerciseInviteEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string ExerciseType { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string RoleDescription { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
    public string? Scenario { get; set; }
    public string DirectorName { get; set; } = string.Empty;
    public string DirectorEmail { get; set; } = string.Empty;
    public string ExerciseUrl { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}

/// <summary>
/// Template model for external participant exercise invitation emails.
/// Sent to non-org users who need to create an account and join an exercise.
/// </summary>
public class ExternalExerciseInviteEmailModel
{
    public string ExerciseName { get; set; } = string.Empty;
    public string ExerciseType { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string DirectorName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string? Location { get; set; }
    public string InviteUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Template model for exercise details updated notification emails.
/// Sent to all participants when significant exercise logistics change.
/// </summary>
public class ExerciseDetailsUpdatedEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty;
    public string ExerciseUrl { get; set; } = string.Empty;
    public string DirectorName { get; set; } = string.Empty;
}
