namespace Cadence.Core.Features.Email.Models;

/// <summary>
/// Template model for exercise start reminder (24 hours before).
/// </summary>
public class ExerciseStartReminderEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string ExerciseDate { get; set; } = string.Empty;
    public string ExerciseTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string ExerciseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for MSEL review deadline reminder.
/// </summary>
public class MselReviewDeadlineEmailModel
{
    public string ApproverName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string PendingCount { get; set; } = string.Empty;
    public string PendingInjectsList { get; set; } = string.Empty;
    public string ExerciseDate { get; set; } = string.Empty;
    public string ReviewUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for observation finalization reminder.
/// </summary>
public class ObservationFinalizationEmailModel
{
    public string EvaluatorName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string DraftCount { get; set; } = string.Empty;
    public string Deadline { get; set; } = string.Empty;
    public string ObservationsUrl { get; set; } = string.Empty;
}
