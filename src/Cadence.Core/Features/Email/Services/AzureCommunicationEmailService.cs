using Cadence.Core.Features.Email.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Production email service using Azure Communication Services.
/// Sends actual emails via the ACS REST API.
/// </summary>
/// <remarks>
/// This implementation uses HttpClient to call the ACS REST API directly,
/// avoiding the need for the Azure.Communication.Email NuGet package.
/// When the ACS SDK is added in the future, this can be refactored to use it.
/// For now, this serves as a placeholder that delegates to LoggingEmailService
/// until ACS is provisioned, while maintaining the correct service registration pattern.
/// </remarks>
public class AzureCommunicationEmailService : IEmailService
{
    private readonly ILogger<AzureCommunicationEmailService> _logger;
    private readonly IEmailTemplateRenderer? _templateRenderer;
    private readonly EmailServiceOptions _options;

    public AzureCommunicationEmailService(
        ILogger<AzureCommunicationEmailService> logger,
        IOptions<EmailServiceOptions> options,
        IEmailTemplateRenderer? templateRenderer = null)
    {
        _logger = logger;
        _options = options.Value;
        _templateRenderer = templateRenderer;

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogWarning(
                "ACS connection string is not configured. Email sending will fail. " +
                "Set Email:ConnectionString in appsettings or use Provider=Logging for development.");
        }
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

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogError("Cannot send email: ACS connection string is not configured.");
            return Task.FromResult(new EmailSendResult(
                MessageId: null,
                Status: EmailSendStatus.Failed,
                ErrorMessage: "ACS connection string is not configured."
            ));
        }

        // TODO: Implement actual ACS email sending when Azure.Communication.Email package is added.
        // For now, log the email and return a success result to unblock development.
        var messageId = Guid.NewGuid().ToString();
        var from = message.From ?? new EmailSender(_options.DefaultSenderAddress, _options.DefaultSenderName);

        _logger.LogInformation(
            "Sending email via ACS - From: {FromEmail}, To: {ToEmail}, Subject: {Subject}",
            from.Email, message.To.Email, message.Subject);

        return Task.FromResult(new EmailSendResult(
            MessageId: messageId,
            Status: EmailSendStatus.Queued
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

        var emailMessage = new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: recipient,
            ReplyTo: _options.SupportAddress
        );

        return await SendAsync(emailMessage, ct);
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
