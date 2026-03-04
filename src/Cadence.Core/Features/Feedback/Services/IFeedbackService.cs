using Cadence.Core.Features.Email.Models.DTOs;
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
}
