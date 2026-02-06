using System.Diagnostics;
using Cadence.Core.Features.Email.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuthIEmailService = Cadence.Core.Features.Authentication.Services.IEmailService;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Implements the authentication-specific email service using the generic email infrastructure.
/// Bridges between the Auth IEmailService interface and the templated email delivery system.
/// All methods are security-category emails and include structured logging for production troubleshooting.
/// </summary>
public class AuthenticationEmailService : AuthIEmailService
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly EmailServiceOptions _options;
    private readonly ILogger<AuthenticationEmailService> _logger;

    public AuthenticationEmailService(
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        IOptions<EmailServiceOptions> options,
        ILogger<AuthenticationEmailService> logger)
    {
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email, string displayName, string resetUrl)
    {
        return await SendAuthEmailAsync("PasswordReset", email, displayName, async () =>
        {
            var model = new PasswordResetEmailModel
            {
                Email = email,
                DisplayName = displayName,
                ResetUrl = resetUrl,
                RequestedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            return await RenderAndSendAsync("PasswordReset", model, email, displayName);
        });
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string displayName)
    {
        return await SendAuthEmailAsync("Welcome", email, displayName, async () =>
        {
            var model = new AccountVerificationEmailModel
            {
                Email = email,
                DisplayName = displayName,
                VerificationUrl = string.Empty,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            return await RenderAndSendAsync("Welcome", model, email, displayName);
        });
    }

    public async Task<bool> SendAccountDeactivatedEmailAsync(string email, string displayName)
    {
        return await SendAuthEmailAsync("AccountDeactivated", email, displayName, async () =>
        {
            var model = new PasswordChangedEmailModel
            {
                Email = email,
                DisplayName = displayName,
                ChangedAt = DateTime.UtcNow,
                ChangeMethod = "Account deactivated",
                SupportUrl = _options.SupportAddress
            };

            return await RenderAndSendAsync("AccountDeactivated", model, email, displayName);
        });
    }

    public async Task<bool> SendAccountReactivatedEmailAsync(string email, string displayName)
    {
        return await SendAuthEmailAsync("AccountReactivated", email, displayName, async () =>
        {
            var model = new PasswordChangedEmailModel
            {
                Email = email,
                DisplayName = displayName,
                ChangedAt = DateTime.UtcNow,
                ChangeMethod = "Account reactivated",
                SupportUrl = _options.SupportAddress
            };

            return await RenderAndSendAsync("AccountReactivated", model, email, displayName);
        });
    }

    public async Task<bool> SendPasswordChangedEmailAsync(
        string email,
        string displayName,
        string changeMethod,
        string resetPasswordUrl,
        string supportUrl)
    {
        return await SendAuthEmailAsync("PasswordChanged", email, displayName, async () =>
        {
            var model = new PasswordChangedEmailModel
            {
                Email = email,
                DisplayName = displayName,
                ChangedAt = DateTime.UtcNow,
                ChangeMethod = changeMethod,
                ResetPasswordUrl = resetPasswordUrl,
                SupportUrl = supportUrl
            };

            return await RenderAndSendAsync("PasswordChanged", model, email, displayName);
        });
    }

    public async Task<bool> SendAccountVerificationEmailAsync(
        string email,
        string displayName,
        string verificationUrl)
    {
        return await SendAuthEmailAsync("AccountVerification", email, displayName, async () =>
        {
            var model = new AccountVerificationEmailModel
            {
                Email = email,
                DisplayName = displayName,
                VerificationUrl = verificationUrl,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            return await RenderAndSendAsync("AccountVerification", model, email, displayName);
        });
    }

    public async Task<bool> SendNewDeviceAlertEmailAsync(
        string email,
        string displayName,
        string browser,
        string operatingSystem,
        string? approximateLocation,
        string secureAccountUrl)
    {
        return await SendAuthEmailAsync("NewDeviceAlert", email, displayName, async () =>
        {
            var model = new NewDeviceAlertEmailModel
            {
                Email = email,
                DisplayName = displayName,
                Browser = browser,
                OperatingSystem = operatingSystem,
                ApproximateLocation = approximateLocation,
                SignInTime = DateTime.UtcNow,
                SecureAccountUrl = secureAccountUrl
            };

            _logger.LogDebug(
                "[Email:Auth] New device alert details - Email: {Email}, Browser: {Browser}, " +
                "OS: {OperatingSystem}, Location: {Location}",
                email, browser, operatingSystem, approximateLocation ?? "Unknown");

            return await RenderAndSendAsync("NewDeviceAlert", model, email, displayName);
        });
    }

    /// <summary>
    /// Common wrapper for all auth email sends with consistent logging and error handling.
    /// </summary>
    private async Task<bool> SendAuthEmailAsync(string templateName, string email, string displayName, Func<Task<EmailSendResult>> sendAction)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[Email:Auth] Sending '{Template}' to {Email}",
            templateName, email);

        try
        {
            var result = await sendAction();
            sw.Stop();

            if (result.Status == EmailSendStatus.Failed)
            {
                _logger.LogError(
                    "[Email:Auth] FAILED '{Template}' to {Email} - Error: {Error}, ElapsedMs: {ElapsedMs}",
                    templateName, email, result.ErrorMessage, sw.ElapsedMilliseconds);
                return false;
            }

            _logger.LogInformation(
                "[Email:Auth] Sent '{Template}' to {Email} - Status: {Status}, " +
                "MessageId: {MessageId}, ElapsedMs: {ElapsedMs}",
                templateName, email, result.Status, result.MessageId, sw.ElapsedMilliseconds);
            return true;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Email:Auth] Exception sending '{Template}' to {Email} - " +
                "ExceptionType: {ExceptionType}, ElapsedMs: {ElapsedMs}",
                templateName, email, ex.GetType().Name, sw.ElapsedMilliseconds);
            return false;
        }
    }

    /// <summary>
    /// Render template and send email via the underlying IEmailService.
    /// </summary>
    private async Task<EmailSendResult> RenderAndSendAsync<TModel>(
        string templateId, TModel model, string recipientEmail, string recipientName)
    {
        var rendered = await _templateRenderer.RenderAsync(templateId, model);

        return await _emailService.SendAsync(new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: new EmailRecipient(recipientEmail, recipientName),
            ReplyTo: _options.SupportAddress
        ));
    }
}
