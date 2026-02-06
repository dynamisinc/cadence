using System.Diagnostics;
using Azure;
using Azure.Communication.Email;
using Cadence.Core.Features.Email.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Production email service using Azure Communication Services.
/// Includes structured logging for production troubleshooting.
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
            _logger.LogInformation(
                "[Email:ACS] Initialized - Sender: {SenderAddress}, Support: {SupportAddress}",
                _options.DefaultSenderAddress, _options.SupportAddress);
        }
        else
        {
            _logger.LogWarning(
                "[Email:ACS] Connection string is not configured. Email sending will fail. " +
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
            _logger.LogError(
                "[Email:ACS] Cannot send - connection string not configured. To: {ToEmail}, Subject: {Subject}",
                message.To.Email, message.Subject);
            return new Models.EmailSendResult(
                MessageId: null,
                Status: Models.EmailSendStatus.Failed,
                ErrorMessage: "ACS connection string is not configured."
            );
        }

        var from = message.From ?? new Models.EmailSender(_options.DefaultSenderAddress, _options.DefaultSenderName);
        var senderAddress = from.Email;
        var sw = Stopwatch.StartNew();

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

            _logger.LogDebug(
                "[Email:ACS] Sending - From: {FromName} <{FromEmail}>, To: {ToName} <{ToEmail}>, " +
                "Subject: {Subject}, HasHtml: {HasHtml}, HasPlainText: {HasPlainText}, HtmlLength: {HtmlLength}",
                from.DisplayName, senderAddress,
                message.To.DisplayName, message.To.Email,
                message.Subject,
                !string.IsNullOrEmpty(message.HtmlBody),
                !string.IsNullOrEmpty(message.PlainTextBody),
                message.HtmlBody?.Length ?? 0);

            var operation = await _client.SendAsync(WaitUntil.Started, acsMessage, ct);
            sw.Stop();

            _logger.LogInformation(
                "[Email:ACS] Queued - To: {ToEmail}, Subject: {Subject}, " +
                "OperationId: {OperationId}, ElapsedMs: {ElapsedMs}",
                message.To.Email, message.Subject, operation.Id, sw.ElapsedMilliseconds);

            return new Models.EmailSendResult(
                MessageId: operation.Id,
                Status: Models.EmailSendStatus.Queued
            );
        }
        catch (RequestFailedException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Email:ACS] FAILED - To: {ToEmail}, Subject: {Subject}, " +
                "ErrorCode: {ErrorCode}, HttpStatus: {HttpStatus}, ElapsedMs: {ElapsedMs}",
                message.To.Email, message.Subject, ex.ErrorCode, ex.Status, sw.ElapsedMilliseconds);

            return new Models.EmailSendResult(
                MessageId: null,
                Status: Models.EmailSendStatus.Failed,
                ErrorMessage: $"ACS error ({ex.ErrorCode}): {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Email:ACS] Unexpected error - To: {ToEmail}, Subject: {Subject}, " +
                "ExceptionType: {ExceptionType}, ElapsedMs: {ElapsedMs}",
                message.To.Email, message.Subject, ex.GetType().Name, sw.ElapsedMilliseconds);

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

        var sw = Stopwatch.StartNew();

        _logger.LogDebug(
            "[Email:ACS] Rendering template '{TemplateId}' for {ToEmail}, ModelType: {ModelType}",
            templateId, recipient.Email, typeof(TModel).Name);

        var rendered = await _templateRenderer.RenderAsync(templateId, model);
        var renderMs = sw.ElapsedMilliseconds;

        _logger.LogDebug(
            "[Email:ACS] Template '{TemplateId}' rendered in {RenderMs}ms - Subject: {Subject}, HtmlLength: {HtmlLength}",
            templateId, renderMs, rendered.Subject, rendered.HtmlBody?.Length ?? 0);

        var emailMessage = new Models.EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: recipient,
            ReplyTo: _options.SupportAddress
        );

        var result = await SendAsync(emailMessage, ct);

        _logger.LogInformation(
            "[Email:ACS] Templated send complete - Template: {TemplateId}, To: {ToEmail}, " +
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
