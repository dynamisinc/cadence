using Cadence.Core.Features.Email.Models.DTOs;
using Cadence.Core.Features.Feedback.Models.DTOs;
using Cadence.Core.Features.Feedback.Models.Enums;

namespace Cadence.Core.Features.Feedback.Services;

public interface IFeedbackService
{
    /// <summary>
    /// Persists a feedback submission to the database.
    /// </summary>
    Task SaveAsync(
        string referenceNumber,
        FeedbackType type,
        string reporterEmail,
        string? reporterName,
        string? userRole,
        string? orgName,
        string? orgRole,
        FeedbackClientContext? clientContext,
        string title,
        string? severity,
        string? contentJson);

    /// <summary>
    /// Updates the status and optional admin notes on a feedback report.
    /// Returns the confirmed status and admin notes after persistence.
    /// </summary>
    Task<(FeedbackStatus Status, string? AdminNotes)> UpdateStatusAsync(Guid id, FeedbackStatus status, string? adminNotes);

    /// <summary>
    /// Soft-deletes a feedback report.
    /// </summary>
    Task SoftDeleteAsync(Guid id, string deletedBy);

    /// <summary>
    /// Paginated query of all feedback reports. System-wide (no org filter).
    /// </summary>
    Task<FeedbackListResponse> GetReportsAsync(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        FeedbackType? type = null,
        FeedbackStatus? status = null,
        string? sortBy = null,
        bool sortDesc = true);
}
