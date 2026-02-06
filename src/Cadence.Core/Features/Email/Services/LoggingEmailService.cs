using Cadence.Core.Features.Email.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Development email service that logs email content instead of sending.
/// Used in non-production environments to verify email content without actual delivery.
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
            "[EMAIL] From: {FromName} <{FromEmail}> | To: {ToName} <{ToEmail}> | Subject: {Subject}",
            from.DisplayName, from.Email, message.To.DisplayName, message.To.Email, message.Subject);

        _logger.LogDebug(
            "[EMAIL] HTML Body:\n{HtmlBody}",
            message.HtmlBody);

        if (!string.IsNullOrEmpty(message.PlainTextBody))
        {
            _logger.LogDebug(
                "[EMAIL] Plain Text Body:\n{PlainTextBody}",
                message.PlainTextBody);
        }

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            _logger.LogDebug("[EMAIL] Reply-To: {ReplyTo}", message.ReplyTo);
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

        var rendered = await _templateRenderer.RenderAsync(templateId, model);

        var message = new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: recipient,
            ReplyTo: _options.SupportAddress
        );

        _logger.LogInformation("[EMAIL] Rendered template '{TemplateId}' for {Recipient}", templateId, recipient.Email);

        return await SendAsync(message, ct);
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
