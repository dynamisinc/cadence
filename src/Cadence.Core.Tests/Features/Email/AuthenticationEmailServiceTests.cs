using Cadence.Core.Features.Email.Models;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.SystemSettings.Models;
using Cadence.Core.Features.SystemSettings.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Cadence.Core.Tests.Features.Email;

/// <summary>
/// Tests for AuthenticationEmailService - bridges auth email interface to generic email infrastructure.
/// </summary>
public class AuthenticationEmailServiceTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEmailTemplateRenderer> _templateRendererMock;
    private readonly Mock<ILogger<AuthenticationEmailService>> _loggerMock;
    private readonly Mock<IEmailConfigurationProvider> _emailConfigMock;
    private readonly AuthenticationEmailService _sut;

    public AuthenticationEmailServiceTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _templateRendererMock = new Mock<IEmailTemplateRenderer>();
        _loggerMock = new Mock<ILogger<AuthenticationEmailService>>();
        _emailConfigMock = new Mock<IEmailConfigurationProvider>();
        _emailConfigMock.Setup(x => x.GetConfigurationAsync())
            .ReturnsAsync(new ResolvedEmailConfiguration
            {
                DefaultSenderAddress = "noreply@test.com",
                DefaultSenderName = "Test Cadence",
                SupportAddress = "support@test.com"
            });

        _sut = new AuthenticationEmailService(
            _emailServiceMock.Object,
            _templateRendererMock.Object,
            _emailConfigMock.Object,
            _loggerMock.Object);

        // Default: all sends succeed
        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(new EmailSendResult("msg-123", EmailSendStatus.Sent, null));
    }

    // =========================================================================
    // SendPasswordResetEmailAsync Tests
    // =========================================================================

    [Fact]
    public async Task SendPasswordResetEmailAsync_ValidInput_RendersPasswordResetTemplate()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("PasswordReset", It.IsAny<PasswordResetEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Reset your password", "<p>Reset</p>", "Reset"));

        await _sut.SendPasswordResetEmailAsync("user@example.com", "Test User", "https://app.test/reset?token=abc");

        _templateRendererMock.Verify(
            r => r.RenderAsync("PasswordReset", It.Is<PasswordResetEmailModel>(m =>
                m.Email == "user@example.com" &&
                m.DisplayName == "Test User" &&
                m.ResetUrl == "https://app.test/reset?token=abc")),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ValidInput_SendsEmailToRecipient()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("PasswordReset", It.IsAny<PasswordResetEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Reset Subject", "<p>HTML</p>", "Plain text"));

        await _sut.SendPasswordResetEmailAsync("user@example.com", "Test User", "https://app.test/reset");

        _emailServiceMock.Verify(
            s => s.SendAsync(It.Is<EmailMessage>(m =>
                m.To.Email == "user@example.com" &&
                m.To.DisplayName == "Test User" &&
                m.Subject == "Reset Subject" &&
                m.HtmlBody == "<p>HTML</p>" &&
                m.PlainTextBody == "Plain text")),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_Success_ReturnsTrue()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("PasswordReset", It.IsAny<PasswordResetEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Subject", "<p>Body</p>", "Body"));

        var result = await _sut.SendPasswordResetEmailAsync("user@example.com", "User", "https://app.test/reset");

        Assert.True(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_SendFails_ReturnsFalse()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("PasswordReset", It.IsAny<PasswordResetEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Subject", "<p>Body</p>", "Body"));

        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(new EmailSendResult(null, EmailSendStatus.Failed,"SMTP error"));

        var result = await _sut.SendPasswordResetEmailAsync("user@example.com", "User", "https://app.test/reset");

        Assert.False(result);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_SetsReplyToAsSupportAddress()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("PasswordReset", It.IsAny<PasswordResetEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Subject", "<p>Body</p>", "Body"));

        await _sut.SendPasswordResetEmailAsync("user@example.com", "User", "https://app.test/reset");

        _emailServiceMock.Verify(
            s => s.SendAsync(It.Is<EmailMessage>(m => m.ReplyTo == "support@test.com")),
            Times.Once);
    }

    // =========================================================================
    // SendWelcomeEmailAsync Tests
    // =========================================================================

    [Fact]
    public async Task SendWelcomeEmailAsync_ValidInput_RendersWelcomeTemplate()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("Welcome", It.IsAny<AccountVerificationEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Welcome", "<p>Welcome</p>", "Welcome"));

        await _sut.SendWelcomeEmailAsync("new@example.com", "New User");

        _templateRendererMock.Verify(
            r => r.RenderAsync("Welcome", It.Is<AccountVerificationEmailModel>(m =>
                m.Email == "new@example.com" &&
                m.DisplayName == "New User")),
            Times.Once);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_Success_ReturnsTrue()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("Welcome", It.IsAny<AccountVerificationEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Welcome", "<p>Welcome</p>", "Welcome"));

        var result = await _sut.SendWelcomeEmailAsync("new@example.com", "New User");

        Assert.True(result);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_SendFails_ReturnsFalse()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("Welcome", It.IsAny<AccountVerificationEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Welcome", "<p>Welcome</p>", "Welcome"));

        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(new EmailSendResult(null, EmailSendStatus.Failed,"Connection refused"));

        var result = await _sut.SendWelcomeEmailAsync("new@example.com", "New User");

        Assert.False(result);
    }

    // =========================================================================
    // SendAccountDeactivatedEmailAsync Tests
    // =========================================================================

    [Fact]
    public async Task SendAccountDeactivatedEmailAsync_ValidInput_RendersAccountDeactivatedTemplate()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("AccountDeactivated", It.IsAny<PasswordChangedEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Account Deactivated", "<p>Deactivated</p>", "Deactivated"));

        await _sut.SendAccountDeactivatedEmailAsync("user@example.com", "Test User");

        _templateRendererMock.Verify(
            r => r.RenderAsync("AccountDeactivated", It.Is<PasswordChangedEmailModel>(m =>
                m.Email == "user@example.com" &&
                m.DisplayName == "Test User")),
            Times.Once);
    }

    [Fact]
    public async Task SendAccountDeactivatedEmailAsync_Success_ReturnsTrue()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("AccountDeactivated", It.IsAny<PasswordChangedEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Account Deactivated", "<p>Deactivated</p>", "Deactivated"));

        var result = await _sut.SendAccountDeactivatedEmailAsync("user@example.com", "Test User");

        Assert.True(result);
    }

    [Fact]
    public async Task SendAccountDeactivatedEmailAsync_SendFails_ReturnsFalse()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("AccountDeactivated", It.IsAny<PasswordChangedEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Account Deactivated", "<p>Deactivated</p>", "Deactivated"));

        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(new EmailSendResult(null, EmailSendStatus.Failed,"Error"));

        var result = await _sut.SendAccountDeactivatedEmailAsync("user@example.com", "Test User");

        Assert.False(result);
    }

    // =========================================================================
    // SendAccountReactivatedEmailAsync Tests
    // =========================================================================

    [Fact]
    public async Task SendAccountReactivatedEmailAsync_ValidInput_RendersAccountReactivatedTemplate()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("AccountReactivated", It.IsAny<PasswordChangedEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Account Reactivated", "<p>Reactivated</p>", "Reactivated"));

        await _sut.SendAccountReactivatedEmailAsync("user@example.com", "Test User");

        _templateRendererMock.Verify(
            r => r.RenderAsync("AccountReactivated", It.Is<PasswordChangedEmailModel>(m =>
                m.Email == "user@example.com" &&
                m.DisplayName == "Test User")),
            Times.Once);
    }

    [Fact]
    public async Task SendAccountReactivatedEmailAsync_Success_ReturnsTrue()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("AccountReactivated", It.IsAny<PasswordChangedEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Account Reactivated", "<p>Reactivated</p>", "Reactivated"));

        var result = await _sut.SendAccountReactivatedEmailAsync("user@example.com", "Test User");

        Assert.True(result);
    }

    // =========================================================================
    // SendPasswordChangedEmailAsync Tests
    // =========================================================================

    [Fact]
    public async Task SendPasswordChangedEmailAsync_ValidInput_RendersPasswordChangedTemplate()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("PasswordChanged", It.IsAny<PasswordChangedEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Password Changed", "<p>Changed</p>", "Changed"));

        await _sut.SendPasswordChangedEmailAsync(
            "user@example.com", "Test User", "Password reset",
            "https://app.test/forgot-password", "https://app.test/support");

        _templateRendererMock.Verify(
            r => r.RenderAsync("PasswordChanged", It.Is<PasswordChangedEmailModel>(m =>
                m.Email == "user@example.com" &&
                m.DisplayName == "Test User" &&
                m.ChangeMethod == "Password reset" &&
                m.ResetPasswordUrl == "https://app.test/forgot-password" &&
                m.SupportUrl == "https://app.test/support")),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordChangedEmailAsync_Success_ReturnsTrue()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("PasswordChanged", It.IsAny<PasswordChangedEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Password Changed", "<p>Changed</p>", "Changed"));

        var result = await _sut.SendPasswordChangedEmailAsync(
            "user@example.com", "Test User", "Password reset",
            "https://app.test/forgot-password", "https://app.test/support");

        Assert.True(result);
    }

    [Fact]
    public async Task SendPasswordChangedEmailAsync_SendFails_ReturnsFalse()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("PasswordChanged", It.IsAny<PasswordChangedEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Password Changed", "<p>Changed</p>", "Changed"));

        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(new EmailSendResult(null, EmailSendStatus.Failed,"Delivery failed"));

        var result = await _sut.SendPasswordChangedEmailAsync(
            "user@example.com", "Test User", "Password reset",
            "https://app.test/forgot-password", "https://app.test/support");

        Assert.False(result);
    }

    // =========================================================================
    // SendAccountVerificationEmailAsync Tests
    // =========================================================================

    [Fact]
    public async Task SendAccountVerificationEmailAsync_ValidInput_RendersVerificationTemplate()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("AccountVerification", It.IsAny<AccountVerificationEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Verify Email", "<p>Verify</p>", "Verify"));

        await _sut.SendAccountVerificationEmailAsync(
            "user@example.com", "Test User", "https://app.test/verify?token=abc");

        _templateRendererMock.Verify(
            r => r.RenderAsync("AccountVerification", It.Is<AccountVerificationEmailModel>(m =>
                m.Email == "user@example.com" &&
                m.DisplayName == "Test User" &&
                m.VerificationUrl == "https://app.test/verify?token=abc")),
            Times.Once);
    }

    [Fact]
    public async Task SendAccountVerificationEmailAsync_Success_ReturnsTrue()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("AccountVerification", It.IsAny<AccountVerificationEmailModel>()))
            .ReturnsAsync(new RenderedEmail("Verify Email", "<p>Verify</p>", "Verify"));

        var result = await _sut.SendAccountVerificationEmailAsync(
            "user@example.com", "Test User", "https://app.test/verify?token=abc");

        Assert.True(result);
    }

    // =========================================================================
    // SendNewDeviceAlertEmailAsync Tests
    // =========================================================================

    [Fact]
    public async Task SendNewDeviceAlertEmailAsync_ValidInput_RendersNewDeviceAlertTemplate()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("NewDeviceAlert", It.IsAny<NewDeviceAlertEmailModel>()))
            .ReturnsAsync(new RenderedEmail("New Sign-In", "<p>Alert</p>", "Alert"));

        await _sut.SendNewDeviceAlertEmailAsync(
            "user@example.com", "Test User", "Chrome", "Windows 11",
            "New York, US", "https://app.test/secure-account");

        _templateRendererMock.Verify(
            r => r.RenderAsync("NewDeviceAlert", It.Is<NewDeviceAlertEmailModel>(m =>
                m.Email == "user@example.com" &&
                m.DisplayName == "Test User" &&
                m.Browser == "Chrome" &&
                m.OperatingSystem == "Windows 11" &&
                m.ApproximateLocation == "New York, US" &&
                m.SecureAccountUrl == "https://app.test/secure-account")),
            Times.Once);
    }

    [Fact]
    public async Task SendNewDeviceAlertEmailAsync_NullLocation_PassesNullToModel()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("NewDeviceAlert", It.IsAny<NewDeviceAlertEmailModel>()))
            .ReturnsAsync(new RenderedEmail("New Sign-In", "<p>Alert</p>", "Alert"));

        await _sut.SendNewDeviceAlertEmailAsync(
            "user@example.com", "Test User", "Firefox", "macOS",
            null, "https://app.test/secure-account");

        _templateRendererMock.Verify(
            r => r.RenderAsync("NewDeviceAlert", It.Is<NewDeviceAlertEmailModel>(m =>
                m.ApproximateLocation == null)),
            Times.Once);
    }

    [Fact]
    public async Task SendNewDeviceAlertEmailAsync_Success_ReturnsTrue()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("NewDeviceAlert", It.IsAny<NewDeviceAlertEmailModel>()))
            .ReturnsAsync(new RenderedEmail("New Sign-In", "<p>Alert</p>", "Alert"));

        var result = await _sut.SendNewDeviceAlertEmailAsync(
            "user@example.com", "Test User", "Chrome", "Windows 11",
            "New York, US", "https://app.test/secure-account");

        Assert.True(result);
    }

    [Fact]
    public async Task SendNewDeviceAlertEmailAsync_SendFails_ReturnsFalse()
    {
        _templateRendererMock
            .Setup(r => r.RenderAsync("NewDeviceAlert", It.IsAny<NewDeviceAlertEmailModel>()))
            .ReturnsAsync(new RenderedEmail("New Sign-In", "<p>Alert</p>", "Alert"));

        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(new EmailSendResult(null, EmailSendStatus.Failed,"Error"));

        var result = await _sut.SendNewDeviceAlertEmailAsync(
            "user@example.com", "Test User", "Chrome", "Windows 11",
            "New York, US", "https://app.test/secure-account");

        Assert.False(result);
    }
}
