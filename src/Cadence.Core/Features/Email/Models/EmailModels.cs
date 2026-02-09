namespace Cadence.Core.Features.Email.Models;

/// <summary>
/// Represents an email message to be sent.
/// </summary>
public record EmailMessage(
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    EmailRecipient To,
    EmailSender? From = null,
    string? ReplyTo = null
);

/// <summary>
/// Represents an email recipient.
/// </summary>
public record EmailRecipient(
    string Email,
    string? DisplayName = null
);

/// <summary>
/// Represents an email sender.
/// </summary>
public record EmailSender(
    string Email,
    string? DisplayName = null
);

/// <summary>
/// Result of an email send operation.
/// </summary>
public record EmailSendResult(
    string? MessageId,
    EmailSendStatus Status,
    string? ErrorMessage = null
);

/// <summary>
/// Represents a rendered email with both HTML and plain text versions.
/// </summary>
public record RenderedEmail(
    string Subject,
    string HtmlBody,
    string PlainTextBody
);

/// <summary>
/// Status of an email send operation.
/// </summary>
public enum EmailSendStatus
{
    Queued,
    Sent,
    Failed,
    Suppressed
}

/// <summary>
/// Delivery status of an email (tracked over time).
/// </summary>
public enum EmailDeliveryStatus
{
    Queued,
    Sent,
    Delivered,
    Bounced,
    Failed,
    Suppressed
}

/// <summary>
/// Categories of emails for preference management.
/// </summary>
public enum EmailCategory
{
    /// <summary>Password reset, login alerts - cannot be disabled.</summary>
    Security,

    /// <summary>Org and exercise invitations - cannot be disabled.</summary>
    Invitations,

    /// <summary>Inject assigned, role changed - default on.</summary>
    Assignments,

    /// <summary>Inject approved/rejected - default on.</summary>
    Workflow,

    /// <summary>Exercise starting, deadlines - default on.</summary>
    Reminders,

    /// <summary>Daily activity summary - default off.</summary>
    DailyDigest,

    /// <summary>Weekly organization report - default off.</summary>
    WeeklyDigest
}
