using Cadence.Core.Features.Email.Models;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Service for logging and tracking email delivery.
/// </summary>
public interface IEmailLogService
{
    /// <summary>
    /// Log an email send attempt.
    /// </summary>
    Task<EmailLog> LogEmailSentAsync(
        Guid organizationId,
        string recipientEmail,
        string subject,
        string? templateId,
        string? acsMessageId,
        EmailDeliveryStatus status,
        string? userId = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Update the delivery status of a previously sent email.
    /// </summary>
    Task<bool> UpdateStatusAsync(
        string acsMessageId,
        EmailDeliveryStatus status,
        string? statusDetail = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get email logs for an organization with optional filtering.
    /// </summary>
    Task<IReadOnlyList<EmailLog>> GetLogsAsync(
        Guid organizationId,
        EmailDeliveryStatus? statusFilter = null,
        string? recipientFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default);

    /// <summary>
    /// Get a specific email log by ID.
    /// </summary>
    Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
