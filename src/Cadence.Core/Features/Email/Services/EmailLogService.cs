using Cadence.Core.Data;
using Cadence.Core.Features.Email.Models;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Service for logging and tracking email delivery status.
/// </summary>
public class EmailLogService : IEmailLogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmailLogService> _logger;

    public EmailLogService(AppDbContext context, ILogger<EmailLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmailLog> LogEmailSentAsync(
        Guid organizationId,
        string recipientEmail,
        string subject,
        string? templateId,
        string? acsMessageId,
        EmailDeliveryStatus status,
        string? userId = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken ct = default)
    {
        var log = new EmailLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            TemplateId = templateId,
            AcsMessageId = acsMessageId,
            Status = status,
            UserId = userId,
            SentAt = DateTime.UtcNow,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId
        };

        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Logged email send: {EmailLogId} to {Recipient} with status {Status}",
            log.Id, recipientEmail, status);

        return log;
    }

    public async Task<bool> UpdateStatusAsync(
        string acsMessageId,
        EmailDeliveryStatus status,
        string? statusDetail = null,
        CancellationToken ct = default)
    {
        var log = await _context.EmailLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.AcsMessageId == acsMessageId, ct);

        if (log == null)
        {
            _logger.LogWarning("Email log not found for ACS message ID: {AcsMessageId}", acsMessageId);
            return false;
        }

        log.Status = status;
        log.StatusDetail = statusDetail;

        if (status == EmailDeliveryStatus.Delivered)
        {
            log.DeliveredAt = DateTime.UtcNow;
        }
        else if (status == EmailDeliveryStatus.Bounced)
        {
            log.BouncedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Updated email status: {AcsMessageId} -> {Status}",
            acsMessageId, status);

        return true;
    }

    public async Task<IReadOnlyList<EmailLog>> GetLogsAsync(
        Guid organizationId,
        EmailDeliveryStatus? statusFilter = null,
        string? recipientFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default)
    {
        var query = _context.EmailLogs
            .Where(e => e.OrganizationId == organizationId);

        if (statusFilter.HasValue)
        {
            query = query.Where(e => e.Status == statusFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(recipientFilter))
        {
            query = query.Where(e => e.RecipientEmail.Contains(recipientFilter));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.SentAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.SentAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(e => e.SentAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.EmailLogs.FindAsync([id], ct);
    }
}
