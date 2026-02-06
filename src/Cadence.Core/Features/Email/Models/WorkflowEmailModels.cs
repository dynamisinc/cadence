namespace Cadence.Core.Features.Email.Models;

/// <summary>
/// Template model for inject submitted for approval notification.
/// </summary>
public class InjectSubmittedEmailModel
{
    public string ApproverName { get; set; } = string.Empty;
    public string InjectNumber { get; set; } = string.Empty;
    public string InjectTitle { get; set; } = string.Empty;
    public string SubmitterName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string ReviewUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for inject approved notification.
/// </summary>
public class InjectApprovedEmailModel
{
    public string SubmitterName { get; set; } = string.Empty;
    public string InjectNumber { get; set; } = string.Empty;
    public string InjectTitle { get; set; } = string.Empty;
    public string ApproverName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string InjectUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for inject rejected notification.
/// </summary>
public class InjectRejectedEmailModel
{
    public string SubmitterName { get; set; } = string.Empty;
    public string InjectNumber { get; set; } = string.Empty;
    public string InjectTitle { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string InjectUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for inject changes requested notification.
/// </summary>
public class InjectChangesRequestedEmailModel
{
    public string SubmitterName { get; set; } = string.Empty;
    public string InjectNumber { get; set; } = string.Empty;
    public string InjectTitle { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public string RequestedChanges { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string InjectUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for inject assignment notification.
/// </summary>
public class InjectAssignmentEmailModel
{
    public string ControllerName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string AssignmentSummary { get; set; } = string.Empty;
    public string ExerciseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for exercise role change notification.
/// </summary>
public class RoleChangeEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string OldRole { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
    public string ExerciseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for evaluator area assignment notification.
/// </summary>
public class EvaluatorAreaAssignmentEmailModel
{
    public string EvaluatorName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string AssignedArea { get; set; } = string.Empty;
    public string AreaDescription { get; set; } = string.Empty;
    public string ExerciseUrl { get; set; } = string.Empty;
}
