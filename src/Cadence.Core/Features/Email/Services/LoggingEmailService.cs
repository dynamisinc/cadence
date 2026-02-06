using System.Diagnostics;
using Cadence.Core.Features.Email.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Development email service that logs email content instead of sending.
/// Uses the same [Email:Log] prefix pattern as ACS service for consistent log filtering.
/// </summary>
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;
    private readonly IEmailTemplateRenderer? _templateRenderer;
    private readonly EmailServiceOptions _options;

    public LoggingEmailService(
        ILogger<LoggingEmailService> logger,
        IOptions<EmailServiceOptions> options,
        IEmailTemplateRenderer? templateRenderer = null)
    {
        _logger = logger;
        _options = options.Value;
        _templateRenderer = templateRenderer;

        _logger.LogDebug(
            "[Email:Log] Initialized (development mode) - Sender: {SenderAddress}, Support: {SupportAddress}",
            _options.DefaultSenderAddress, _options.SupportAddress);
    }

    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message.To.Email))
        {
            throw new ArgumentException("Recipient email address is required.", nameof(message));
        }

        if (!IsValidEmail(message.To.Email))
        {
            throw new ArgumentException($"Invalid recipient email address: '{message.To.Email}'", nameof(message));
        }

        var messageId = Guid.NewGuid().ToString();
        var from = message.From ?? new EmailSender(_options.DefaultSenderAddress, _options.DefaultSenderName);

        _logger.LogInformation(
            "[Email:Log] SENT (logged, not delivered) - From: {FromName} <{FromEmail}>, " +
            "To: {ToName} <{ToEmail}>, Subject: {Subject}, MessageId: {MessageId}, " +
            "HtmlLength: {HtmlLength}, HasPlainText: {HasPlainText}",
            from.DisplayName, from.Email,
            message.To.DisplayName, message.To.Email,
            message.Subject, messageId,
            message.HtmlBody?.Length ?? 0,
            !string.IsNullOrEmpty(message.PlainTextBody));

        _logger.LogDebug(
            "[Email:Log] HTML Body for {MessageId}:\n{HtmlBody}",
            messageId, message.HtmlBody);

        if (!string.IsNullOrEmpty(message.PlainTextBody))
        {
            _logger.LogDebug(
                "[Email:Log] Plain Text Body for {MessageId}:\n{PlainTextBody}",
                messageId, message.PlainTextBody);
        }

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            _logger.LogDebug("[Email:Log] Reply-To: {ReplyTo}", message.ReplyTo);
        }

        return Task.FromResult(new EmailSendResult(
            MessageId: messageId,
            Status: EmailSendStatus.Sent
        ));
    }

    public async Task<EmailSendResult> SendTemplatedAsync<TModel>(
        string templateId,
        TModel model,
        EmailRecipient recipient,
        CancellationToken ct = default)
    {
        if (_templateRenderer == null)
        {
            throw new InvalidOperationException("Template renderer is not configured.");
        }

        var sw = Stopwatch.StartNew();

        _logger.LogDebug(
            "[Email:Log] Rendering template '{TemplateId}' for {ToEmail}, ModelType: {ModelType}",
            templateId, recipient.Email, typeof(TModel).Name);

        var rendered = await _templateRenderer.RenderAsync(templateId, model);
        var renderMs = sw.ElapsedMilliseconds;

        _logger.LogDebug(
            "[Email:Log] Template '{TemplateId}' rendered in {RenderMs}ms - Subject: {Subject}, HtmlLength: {HtmlLength}",
            templateId, renderMs, rendered.Subject, rendered.HtmlBody?.Length ?? 0);

        var message = new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: recipient,
            ReplyTo: _options.SupportAddress
        );

        var result = await SendAsync(message, ct);

        _logger.LogInformation(
            "[Email:Log] Templated send complete - Template: {TemplateId}, To: {ToEmail}, " +
            "Status: {Status}, MessageId: {MessageId}, TotalElapsedMs: {TotalElapsedMs}",
            templateId, recipient.Email, result.Status, result.MessageId, sw.ElapsedMilliseconds);

        return result;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
