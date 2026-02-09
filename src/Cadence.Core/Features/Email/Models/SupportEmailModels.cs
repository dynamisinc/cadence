namespace Cadence.Core.Features.Email.Models;

/// <summary>
/// Template model for bug report email sent to support team.
/// </summary>
public class BugReportEmailModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StepsToReproduce { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;
    public string CurrentUrl { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string ScreenSize { get; set; } = string.Empty;
    public string ReportedAt { get; set; } = string.Empty;
}

/// <summary>
/// Template model for feature request email sent to product team.
/// </summary>
public class FeatureRequestEmailModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UseCase { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;
    public string ReportedAt { get; set; } = string.Empty;
}

/// <summary>
/// Template model for general feedback email sent to support team.
/// </summary>
public class GeneralFeedbackEmailModel
{
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SentAt { get; set; } = string.Empty;
}

/// <summary>
/// Template model for support ticket acknowledgment sent to the user.
/// </summary>
public class SupportTicketAcknowledgmentEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string TicketType { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public string SubmittedAt { get; set; } = string.Empty;
    public string MessagePreview { get; set; } = string.Empty;
}
