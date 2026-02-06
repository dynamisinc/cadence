using Azure;
using Azure.Communication.Email;
using Cadence.Core.Features.Email.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Production email service using Azure Communication Services.
/// </summary>
public class AzureCommunicationEmailService : IEmailService
{
    private readonly ILogger<AzureCommunicationEmailService> _logger;
    private readonly IEmailTemplateRenderer? _templateRenderer;
    private readonly EmailServiceOptions _options;
    private readonly EmailClient? _client;

    public AzureCommunicationEmailService(
        ILogger<AzureCommunicationEmailService> logger,
        IOptions<EmailServiceOptions> options,
        IEmailTemplateRenderer? templateRenderer = null)
    {
        _logger = logger;
        _options = options.Value;
        _templateRenderer = templateRenderer;

        if (!string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _client = new EmailClient(_options.ConnectionString);
        }
        else
        {
            _logger.LogWarning(
                "ACS connection string is not configured. Email sending will fail. " +
                "Set Email:ConnectionString in appsettings or use Provider=Logging for development.");
        }
    }

    public async Task<Models.EmailSendResult> SendAsync(Models.EmailMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message.To.Email))
        {
            throw new ArgumentException("Recipient email address is required.", nameof(message));
        }

        if (!IsValidEmail(message.To.Email))
        {
            throw new ArgumentException($"Invalid recipient email address: '{message.To.Email}'", nameof(message));
        }

        if (_client == null)
        {
            _logger.LogError("Cannot send email: ACS connection string is not configured.");
            return new Models.EmailSendResult(
                MessageId: null,
                Status: Models.EmailSendStatus.Failed,
                ErrorMessage: "ACS connection string is not configured."
            );
        }

        var from = message.From ?? new Models.EmailSender(_options.DefaultSenderAddress, _options.DefaultSenderName);
        var senderAddress = from.Email;

        try
        {
            var acsContent = new EmailContent(message.Subject);
            if (!string.IsNullOrEmpty(message.HtmlBody))
                acsContent.Html = message.HtmlBody;
            if (!string.IsNullOrEmpty(message.PlainTextBody))
                acsContent.PlainText = message.PlainTextBody;

            var acsMessage = new Azure.Communication.Email.EmailMessage(
                senderAddress: senderAddress,
                recipientAddress: message.To.Email,
                content: acsContent);

            if (!string.IsNullOrEmpty(message.ReplyTo))
            {
                acsMessage.ReplyTo.Add(new EmailAddress(message.ReplyTo));
            }

            _logger.LogInformation(
                "Sending email via ACS - From: {FromEmail}, To: {ToEmail}, Subject: {Subject}",
                senderAddress, message.To.Email, message.Subject);

            var operation = await _client.SendAsync(WaitUntil.Started, acsMessage, ct);

            _logger.LogInformation(
                "Email queued via ACS - OperationId: {OperationId}, To: {ToEmail}",
                operation.Id, message.To.Email);

            return new Models.EmailSendResult(
                MessageId: operation.Id,
                Status: Models.EmailSendStatus.Queued
            );
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "ACS email send failed - To: {ToEmail}, Subject: {Subject}, ErrorCode: {ErrorCode}",
                message.To.Email, message.Subject, ex.ErrorCode);

            return new Models.EmailSendResult(
                MessageId: null,
                Status: Models.EmailSendStatus.Failed,
                ErrorMessage: $"ACS error ({ex.ErrorCode}): {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error sending email - To: {ToEmail}, Subject: {Subject}",
                message.To.Email, message.Subject);

            return new Models.EmailSendResult(
                MessageId: null,
                Status: Models.EmailSendStatus.Failed,
                ErrorMessage: ex.Message
            );
        }
    }

    public async Task<Models.EmailSendResult> SendTemplatedAsync<TModel>(
        string templateId,
        TModel model,
        Models.EmailRecipient recipient,
        CancellationToken ct = default)
    {
        if (_templateRenderer == null)
        {
            throw new InvalidOperationException("Template renderer is not configured.");
        }

        var rendered = await _templateRenderer.RenderAsync(templateId, model);

        var emailMessage = new Models.EmailMessage(
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
