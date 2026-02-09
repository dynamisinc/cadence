using System.ComponentModel.DataAnnotations;

namespace Cadence.Core.Features.Email.Models.DTOs;

/// <summary>
/// Request to submit a bug report.
/// </summary>
public record SubmitBugReportRequest(
    [Required] string Title,
    [Required] string Description,
    string? StepsToReproduce,
    [Required] string Severity
);

/// <summary>
/// Request to submit a feature request.
/// </summary>
public record SubmitFeatureRequestRequest(
    [Required] string Title,
    [Required] string Description,
    string? UseCase
);

/// <summary>
/// Request to submit general feedback.
/// </summary>
public record SubmitGeneralFeedbackRequest(
    [Required] string Category,
    [Required] string Subject,
    [Required] string Message
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
