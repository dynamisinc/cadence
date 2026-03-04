using Cadence.Core.Features.Feedback.Models.Enums;

namespace Cadence.Core.Features.Feedback.Services;

public interface IGitHubIssueService
{
    /// <summary>
    /// Creates a GitHub issue for the given feedback report.
    /// Returns (issueNumber, issueUrl) on success, or null if not configured or on failure.
    /// </summary>
    Task<(int IssueNumber, string IssueUrl)?> CreateIssueAsync(
        string referenceNumber,
        FeedbackType type,
        string title,
        string? severity,
        string? contentJson,
        string reporterEmail,
        string? reporterName,
        string? orgName);

    /// <summary>
    /// Closes a GitHub issue. Best-effort — failures are logged but not thrown.
    /// </summary>
    Task CloseIssueAsync(int issueNumber, string? comment = null);

    /// <summary>
    /// Adds a comment to a GitHub issue. Best-effort — failures are logged but not thrown.
    /// </summary>
    Task AddIssueCommentAsync(int issueNumber, string comment);

    /// <summary>
    /// Tests whether the configured GitHub token and repo are valid.
    /// </summary>
    Task<(bool Success, string Message)> TestConnectionAsync();
}
