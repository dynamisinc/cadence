using Cadence.Core.Features.Email.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuthIEmailService = Cadence.Core.Features.Authentication.Services.IEmailService;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Implements the authentication-specific email service using the generic email infrastructure.
/// Bridges between the Auth IEmailService interface and the templated email delivery system.
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
        var model = new PasswordResetEmailModel
        {
            Email = email,
            DisplayName = displayName,
            ResetUrl = resetUrl,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var rendered = await _templateRenderer.RenderAsync("PasswordReset", model);

        var result = await _emailService.SendAsync(new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: new EmailRecipient(email, displayName),
            ReplyTo: _options.SupportAddress
        ));

        if (result.Status == EmailSendStatus.Failed)
        {
            _logger.LogError("Failed to send password reset email to {Email}: {Error}", email, result.ErrorMessage);
            return false;
        }

        _logger.LogInformation("Password reset email sent to {Email}", email);
        return true;
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string displayName)
    {
        var model = new AccountVerificationEmailModel
        {
            Email = email,
            DisplayName = displayName,
            VerificationUrl = string.Empty,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        var rendered = await _templateRenderer.RenderAsync("Welcome", model);

        var result = await _emailService.SendAsync(new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: new EmailRecipient(email, displayName),
            ReplyTo: _options.SupportAddress
        ));

        if (result.Status == EmailSendStatus.Failed)
        {
            _logger.LogError("Failed to send welcome email to {Email}: {Error}", email, result.ErrorMessage);
            return false;
        }

        _logger.LogInformation("Welcome email sent to {Email}", email);
        return true;
    }

    public async Task<bool> SendAccountDeactivatedEmailAsync(string email, string displayName)
    {
        var model = new PasswordChangedEmailModel
        {
            Email = email,
            DisplayName = displayName,
            ChangedAt = DateTime.UtcNow,
            ChangeMethod = "Account deactivated",
            SupportUrl = _options.SupportAddress
        };

        var rendered = await _templateRenderer.RenderAsync("AccountDeactivated", model);

        var result = await _emailService.SendAsync(new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: new EmailRecipient(email, displayName),
            ReplyTo: _options.SupportAddress
        ));

        if (result.Status == EmailSendStatus.Failed)
        {
            _logger.LogError("Failed to send account deactivated email to {Email}: {Error}", email, result.ErrorMessage);
            return false;
        }

        return true;
    }

    public async Task<bool> SendAccountReactivatedEmailAsync(string email, string displayName)
    {
        var model = new PasswordChangedEmailModel
        {
            Email = email,
            DisplayName = displayName,
            ChangedAt = DateTime.UtcNow,
            ChangeMethod = "Account reactivated",
            SupportUrl = _options.SupportAddress
        };

        var rendered = await _templateRenderer.RenderAsync("AccountReactivated", model);

        var result = await _emailService.SendAsync(new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: new EmailRecipient(email, displayName),
            ReplyTo: _options.SupportAddress
        ));

        if (result.Status == EmailSendStatus.Failed)
        {
            _logger.LogError("Failed to send account reactivated email to {Email}: {Error}", email, result.ErrorMessage);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Send password changed confirmation email.
    /// This is a security-mandatory email that cannot be disabled.
    /// </summary>
    public async Task<bool> SendPasswordChangedEmailAsync(
        string email,
        string displayName,
        string changeMethod,
        string resetPasswordUrl,
        string supportUrl)
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

        var rendered = await _templateRenderer.RenderAsync("PasswordChanged", model);

        var result = await _emailService.SendAsync(new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: new EmailRecipient(email, displayName),
            ReplyTo: _options.SupportAddress
        ));

        if (result.Status == EmailSendStatus.Failed)
        {
            _logger.LogError("Failed to send password changed email to {Email}: {Error}", email, result.ErrorMessage);
            return false;
        }

        _logger.LogInformation("Password changed confirmation email sent to {Email}", email);
        return true;
    }

    /// <summary>
    /// Send account verification email.
    /// </summary>
    public async Task<bool> SendAccountVerificationEmailAsync(
        string email,
        string displayName,
        string verificationUrl)
    {
        var model = new AccountVerificationEmailModel
        {
            Email = email,
            DisplayName = displayName,
            VerificationUrl = verificationUrl,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        var rendered = await _templateRenderer.RenderAsync("AccountVerification", model);

        var result = await _emailService.SendAsync(new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: new EmailRecipient(email, displayName),
            ReplyTo: _options.SupportAddress
        ));

        if (result.Status == EmailSendStatus.Failed)
        {
            _logger.LogError("Failed to send verification email to {Email}: {Error}", email, result.ErrorMessage);
            return false;
        }

        _logger.LogInformation("Account verification email sent to {Email}", email);
        return true;
    }

    /// <summary>
    /// Send new device login alert email.
    /// </summary>
    public async Task<bool> SendNewDeviceAlertEmailAsync(
        string email,
        string displayName,
        string browser,
        string operatingSystem,
        string? approximateLocation,
        string secureAccountUrl)
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

        var rendered = await _templateRenderer.RenderAsync("NewDeviceAlert", model);

        var result = await _emailService.SendAsync(new EmailMessage(
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            To: new EmailRecipient(email, displayName),
            ReplyTo: _options.SupportAddress
        ));

        if (result.Status == EmailSendStatus.Failed)
        {
            _logger.LogError("Failed to send new device alert to {Email}: {Error}", email, result.ErrorMessage);
            return false;
        }

        _logger.LogInformation("New device alert email sent to {Email}", email);
        return true;
    }
}
