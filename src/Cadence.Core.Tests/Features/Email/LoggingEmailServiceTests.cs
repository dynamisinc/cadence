using Cadence.Core.Features.Email.Models;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.SystemSettings.Models;
using Cadence.Core.Features.SystemSettings.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Cadence.Core.Tests.Features.Email;

/// <summary>
/// Tests for LoggingEmailService (development email service).
/// </summary>
public class LoggingEmailServiceTests
{
    private readonly Mock<ILogger<LoggingEmailService>> _logger;
    private readonly IOptions<EmailServiceOptions> _options;
    private readonly Mock<IEmailConfigurationProvider> _emailConfigMock;
    private readonly LoggingEmailService _service;

    public LoggingEmailServiceTests()
    {
        _logger = new Mock<ILogger<LoggingEmailService>>();
        _options = Options.Create(new EmailServiceOptions
        {
            DefaultSenderAddress = "noreply@test.com",
            DefaultSenderName = "Test Cadence",
            SupportAddress = "support@test.com"
        });
        _emailConfigMock = new Mock<IEmailConfigurationProvider>();
        _emailConfigMock.Setup(x => x.GetConfigurationAsync())
            .ReturnsAsync(new ResolvedEmailConfiguration
            {
                DefaultSenderAddress = "noreply@test.com",
                DefaultSenderName = "Test Cadence",
                SupportAddress = "support@test.com"
            });
        _service = new LoggingEmailService(_logger.Object, _options, _emailConfigMock.Object);
    }

    // =========================================================================
    // SendAsync Tests
    // =========================================================================

    [Fact]
    public async Task SendAsync_ValidMessage_ReturnsSentStatus()
    {
        var message = new EmailMessage(
            Subject: "Test Subject",
            HtmlBody: "<p>Hello</p>",
            PlainTextBody: "Hello",
            To: new EmailRecipient("user@example.com", "User")
        );

        var result = await _service.SendAsync(message);

        Assert.Equal(EmailSendStatus.Sent, result.Status);
        Assert.NotNull(result.MessageId);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task SendAsync_ValidMessage_ReturnsTrackingId()
    {
        var message = new EmailMessage(
            Subject: "Test",
            HtmlBody: "<p>Test</p>",
            PlainTextBody: null,
            To: new EmailRecipient("user@example.com")
        );

        var result = await _service.SendAsync(message);

        Assert.NotNull(result.MessageId);
        Assert.True(Guid.TryParse(result.MessageId, out _));
    }

    [Fact]
    public async Task SendAsync_EmptyRecipient_ThrowsArgumentException()
    {
        var message = new EmailMessage(
            Subject: "Test",
            HtmlBody: "<p>Test</p>",
            PlainTextBody: null,
            To: new EmailRecipient("")
        );

        await Assert.ThrowsAsync<ArgumentException>(() => _service.SendAsync(message));
    }

    [Fact]
    public async Task SendAsync_InvalidRecipient_ThrowsArgumentException()
    {
        var message = new EmailMessage(
            Subject: "Test",
            HtmlBody: "<p>Test</p>",
            PlainTextBody: null,
            To: new EmailRecipient("not-an-email")
        );

        await Assert.ThrowsAsync<ArgumentException>(() => _service.SendAsync(message));
    }

    [Fact]
    public async Task SendAsync_LogsEmailContent()
    {
        var message = new EmailMessage(
            Subject: "Important Subject",
            HtmlBody: "<p>Hello World</p>",
            PlainTextBody: "Hello World",
            To: new EmailRecipient("user@example.com", "Test User")
        );

        await _service.SendAsync(message);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("user@example.com")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_UsesDefaultSenderWhenNotSpecified()
    {
        var message = new EmailMessage(
            Subject: "Test",
            HtmlBody: "<p>Test</p>",
            PlainTextBody: null,
            To: new EmailRecipient("user@example.com")
        );

        var result = await _service.SendAsync(message);

        Assert.Equal(EmailSendStatus.Sent, result.Status);
        // Default sender is used internally - verified via logging
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("noreply@test.com")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_UsesCustomSenderWhenSpecified()
    {
        var message = new EmailMessage(
            Subject: "Test",
            HtmlBody: "<p>Test</p>",
            PlainTextBody: null,
            To: new EmailRecipient("user@example.com"),
            From: new EmailSender("custom@org.com", "My Org")
        );

        var result = await _service.SendAsync(message);

        Assert.Equal(EmailSendStatus.Sent, result.Status);
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("custom@org.com")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // =========================================================================
    // SendTemplatedAsync Tests
    // =========================================================================

    [Fact]
    public async Task SendTemplatedAsync_NoTemplateRenderer_ThrowsInvalidOperation()
    {
        // LoggingEmailService created without template renderer
        var service = new LoggingEmailService(_logger.Object, _options, _emailConfigMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendTemplatedAsync(
                "TestTemplate",
                new { Name = "Test" },
                new EmailRecipient("user@example.com")));
    }

    [Fact]
    public async Task SendTemplatedAsync_WithTemplateRenderer_RendersAndSends()
    {
        var templateRenderer = new Mock<IEmailTemplateRenderer>();
        templateRenderer
            .Setup(r => r.RenderAsync("TestTemplate", It.IsAny<object>()))
            .ReturnsAsync(new RenderedEmail("Rendered Subject", "<p>Rendered HTML</p>", "Rendered Text"));

        var service = new LoggingEmailService(_logger.Object, _options, _emailConfigMock.Object, templateRenderer.Object);

        var result = await service.SendTemplatedAsync(
            "TestTemplate",
            new { Name = "Test" },
            new EmailRecipient("user@example.com"));

        Assert.Equal(EmailSendStatus.Sent, result.Status);
        Assert.NotNull(result.MessageId);
    }
}
