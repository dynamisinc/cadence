using System.ComponentModel.DataAnnotations;

namespace Cadence.Core.Features.Email.Models.DTOs;

/// <summary>
/// Client-supplied context captured automatically at the moment of submission.
/// Contains fields the server cannot know (page URL, screen, app version, exercise context).
/// Roles and org identity are sourced from JWT claims server-side and are not trusted from this object.
/// </summary>
public record FeedbackClientContext(
    string? CurrentUrl,
    string? ScreenSize,
    string? AppVersion,
    string? CommitSha,
    string? ExerciseId,
    string? ExerciseName,
    string? ExerciseRole
);

/// <summary>
/// Request to submit a bug report.
/// </summary>
public record SubmitBugReportRequest(
    [Required] string Title,
    [Required] string Description,
    string? StepsToReproduce,
    [Required] string Severity,
    FeedbackClientContext? Context
);

/// <summary>
/// Request to submit a feature request.
/// </summary>
public record SubmitFeatureRequestRequest(
    [Required] string Title,
    [Required] string Description,
    string? UseCase,
    FeedbackClientContext? Context
);

/// <summary>
/// Request to submit general feedback.
/// </summary>
public record SubmitGeneralFeedbackRequest(
    [Required] string Category,
    [Required] string Subject,
    [Required] string Message,
    FeedbackClientContext? Context
);

/// <summary>
/// Request to submit an error report from the ErrorBoundary.
/// </summary>
public record SubmitErrorReportRequest(
    [Required] string ErrorMessage,
    string? StackTrace,
    string? ComponentStack,
    [Required] string Url,
    [Required] string Browser
);

/// <summary>
/// Response after submitting feedback.
/// </summary>
public record FeedbackResponse(
    string ReferenceNumber,
    string Message
);
