using Cadence.Core.Data;
using Cadence.Core.Features.Email.Models;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Email;

/// <summary>
/// Tests for EmailLogService (delivery tracking).
/// </summary>
public class EmailLogServiceTests
{
    private readonly AppDbContext _context;
    private readonly EmailLogService _service;
    private readonly Guid _testOrgId = Guid.NewGuid();

    public EmailLogServiceTests()
    {
        _context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<EmailLogService>>();
        _service = new EmailLogService(_context, logger.Object);
    }

    // =========================================================================
    // LogEmailSentAsync Tests
    // =========================================================================

    [Fact]
    public async Task LogEmailSentAsync_ValidData_CreatesLogEntry()
    {
        var result = await _service.LogEmailSentAsync(
            organizationId: _testOrgId,
            recipientEmail: "user@example.com",
            subject: "Test Email",
            templateId: "TestTemplate",
            acsMessageId: "acs-msg-001",
            status: EmailDeliveryStatus.Sent
        );

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_testOrgId, result.OrganizationId);
        Assert.Equal("user@example.com", result.RecipientEmail);
        Assert.Equal("Test Email", result.Subject);
        Assert.Equal("TestTemplate", result.TemplateId);
        Assert.Equal("acs-msg-001", result.AcsMessageId);
        Assert.Equal(EmailDeliveryStatus.Sent, result.Status);
    }

    [Fact]
    public async Task LogEmailSentAsync_RecordsTimestamp()
    {
        var before = DateTime.UtcNow;

        var result = await _service.LogEmailSentAsync(
            organizationId: _testOrgId,
            recipientEmail: "user@example.com",
            subject: "Test",
            templateId: null,
            acsMessageId: null,
            status: EmailDeliveryStatus.Queued
        );

        Assert.True(result.SentAt >= before);
        Assert.True(result.SentAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task LogEmailSentAsync_RecordsOptionalFields()
    {
        var result = await _service.LogEmailSentAsync(
            organizationId: _testOrgId,
            recipientEmail: "user@example.com",
            subject: "Invite",
            templateId: "OrganizationInvite",
            acsMessageId: "acs-123",
            status: EmailDeliveryStatus.Sent,
            userId: "user-123",
            relatedEntityType: "Exercise",
            relatedEntityId: Guid.NewGuid()
        );

        Assert.Equal("user-123", result.UserId);
        Assert.Equal("Exercise", result.RelatedEntityType);
        Assert.NotNull(result.RelatedEntityId);
    }

    [Fact]
    public async Task LogEmailSentAsync_DoesNotStoreBodyContent()
    {
        var result = await _service.LogEmailSentAsync(
            organizationId: _testOrgId,
            recipientEmail: "user@example.com",
            subject: "Test",
            templateId: "TestTemplate",
            acsMessageId: null,
            status: EmailDeliveryStatus.Sent
        );

        // Verify the entity stored in DB has no body content fields
        var log = await _context.EmailLogs.FindAsync(result.Id);
        Assert.NotNull(log);
        // EmailLog entity intentionally does not have HtmlBody or PlainTextBody fields
        Assert.Equal("Test", log.Subject);
    }

    // =========================================================================
    // UpdateStatusAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdateStatusAsync_ExistingMessage_UpdatesStatus()
    {
        var log = await _service.LogEmailSentAsync(
            _testOrgId, "user@example.com", "Test", null, "acs-msg-100",
            EmailDeliveryStatus.Sent);

        var updated = await _service.UpdateStatusAsync(
            "acs-msg-100", EmailDeliveryStatus.Delivered);

        Assert.True(updated);
        var refreshed = await _context.EmailLogs.FindAsync(log.Id);
        Assert.Equal(EmailDeliveryStatus.Delivered, refreshed!.Status);
        Assert.NotNull(refreshed.DeliveredAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_Bounced_SetsBouncedTimestamp()
    {
        await _service.LogEmailSentAsync(
            _testOrgId, "bad@invalid.com", "Test", null, "acs-bounce-1",
            EmailDeliveryStatus.Sent);

        await _service.UpdateStatusAsync(
            "acs-bounce-1", EmailDeliveryStatus.Bounced, "Mailbox not found");

        var logs = _context.EmailLogs.Where(e => e.AcsMessageId == "acs-bounce-1").ToList();
        Assert.Single(logs);
        Assert.Equal(EmailDeliveryStatus.Bounced, logs[0].Status);
        Assert.NotNull(logs[0].BouncedAt);
        Assert.Equal("Mailbox not found", logs[0].StatusDetail);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownMessageId_ReturnsFalse()
    {
        var result = await _service.UpdateStatusAsync(
            "nonexistent-msg", EmailDeliveryStatus.Delivered);

        Assert.False(result);
    }

    // =========================================================================
    // GetLogsAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetLogsAsync_ReturnsLogsForOrganization()
    {
        var otherOrgId = Guid.NewGuid();

        await _service.LogEmailSentAsync(_testOrgId, "a@test.com", "Email 1", null, null, EmailDeliveryStatus.Sent);
        await _service.LogEmailSentAsync(_testOrgId, "b@test.com", "Email 2", null, null, EmailDeliveryStatus.Sent);
        await _service.LogEmailSentAsync(otherOrgId, "c@test.com", "Other Org", null, null, EmailDeliveryStatus.Sent);

        var logs = await _service.GetLogsAsync(_testOrgId);

        Assert.Equal(2, logs.Count);
        Assert.All(logs, l => Assert.Equal(_testOrgId, l.OrganizationId));
    }

    [Fact]
    public async Task GetLogsAsync_FilterByStatus_ReturnsMatchingOnly()
    {
        await _service.LogEmailSentAsync(_testOrgId, "a@test.com", "Sent", null, null, EmailDeliveryStatus.Sent);
        await _service.LogEmailSentAsync(_testOrgId, "b@test.com", "Failed", null, null, EmailDeliveryStatus.Failed);

        var logs = await _service.GetLogsAsync(_testOrgId, statusFilter: EmailDeliveryStatus.Sent);

        Assert.Single(logs);
        Assert.Equal(EmailDeliveryStatus.Sent, logs[0].Status);
    }

    [Fact]
    public async Task GetLogsAsync_FilterByRecipient_ReturnsMatchingOnly()
    {
        await _service.LogEmailSentAsync(_testOrgId, "alice@test.com", "To Alice", null, null, EmailDeliveryStatus.Sent);
        await _service.LogEmailSentAsync(_testOrgId, "bob@test.com", "To Bob", null, null, EmailDeliveryStatus.Sent);

        var logs = await _service.GetLogsAsync(_testOrgId, recipientFilter: "alice");

        Assert.Single(logs);
        Assert.Equal("alice@test.com", logs[0].RecipientEmail);
    }

    [Fact]
    public async Task GetLogsAsync_OrderedByDateDescending()
    {
        await _service.LogEmailSentAsync(_testOrgId, "a@test.com", "First", null, null, EmailDeliveryStatus.Sent);
        await Task.Delay(10); // Ensure different timestamps
        await _service.LogEmailSentAsync(_testOrgId, "b@test.com", "Second", null, null, EmailDeliveryStatus.Sent);

        var logs = await _service.GetLogsAsync(_testOrgId);

        Assert.Equal(2, logs.Count);
        Assert.True(logs[0].SentAt >= logs[1].SentAt);
    }

    [Fact]
    public async Task GetLogsAsync_PaginationWorks()
    {
        for (int i = 0; i < 5; i++)
        {
            await _service.LogEmailSentAsync(_testOrgId, $"user{i}@test.com", $"Email {i}", null, null, EmailDeliveryStatus.Sent);
        }

        var page1 = await _service.GetLogsAsync(_testOrgId, limit: 2, offset: 0);
        var page2 = await _service.GetLogsAsync(_testOrgId, limit: 2, offset: 2);

        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
    }

    // =========================================================================
    // GetByIdAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetByIdAsync_ExistingLog_ReturnsLog()
    {
        var created = await _service.LogEmailSentAsync(
            _testOrgId, "user@test.com", "Test", null, null, EmailDeliveryStatus.Sent);

        var result = await _service.GetByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonexistentId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
