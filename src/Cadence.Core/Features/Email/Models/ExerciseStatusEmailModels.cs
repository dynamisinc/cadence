namespace Cadence.Core.Features.Email.Models;

/// <summary>
/// Template model for exercise published notification.
/// </summary>
public class ExercisePublishedEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string ExerciseDate { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string ExerciseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for exercise started (active) notification.
/// </summary>
public class ExerciseStartedEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string StartedAt { get; set; } = string.Empty;
    public string ScenarioTime { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string ExerciseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for exercise completed notification.
/// </summary>
public class ExerciseCompletedEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string CompletedAt { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string NextSteps { get; set; } = string.Empty;
    public string ExerciseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for exercise cancelled notification.
/// </summary>
public class ExerciseCancelledEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string ExerciseDate { get; set; } = string.Empty;
    public string CancellationReason { get; set; } = string.Empty;
    public string DirectorName { get; set; } = string.Empty;
    public string DirectorEmail { get; set; } = string.Empty;
}
