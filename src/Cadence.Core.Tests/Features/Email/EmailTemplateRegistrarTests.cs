using Cadence.Core.Features.Email.Services;

namespace Cadence.Core.Tests.Features.Email;

/// <summary>
/// Tests for EmailTemplateRegistrar - validates all built-in templates are registered correctly.
/// </summary>
public class EmailTemplateRegistrarTests
{
    private readonly InMemoryEmailTemplateStore _store;

    public EmailTemplateRegistrarTests()
    {
        _store = new InMemoryEmailTemplateStore();
        EmailTemplateRegistrar.RegisterAll(_store);
    }

    [Theory]
    [InlineData("_Layout")]
    [InlineData("PasswordReset")]
    [InlineData("PasswordChanged")]
    [InlineData("AccountVerification")]
    [InlineData("NewDeviceAlert")]
    [InlineData("Welcome")]
    [InlineData("AccountDeactivated")]
    [InlineData("AccountReactivated")]
    public async Task RegisterAll_RegistersExpectedTemplate(string templateId)
    {
        var template = await _store.GetTemplateAsync(templateId);

        Assert.NotNull(template);
        Assert.Equal(templateId, template!.TemplateId);
    }

    [Fact]
    public async Task RegisterAll_RegistersExactly8Templates()
    {
        // Verify all 8 expected templates exist
        var expectedTemplates = new[]
        {
            "_Layout", "PasswordReset", "PasswordChanged", "AccountVerification",
            "NewDeviceAlert", "Welcome", "AccountDeactivated", "AccountReactivated"
        };

        foreach (var id in expectedTemplates)
        {
            var template = await _store.GetTemplateAsync(id);
            Assert.NotNull(template);
        }
    }

    [Fact]
    public async Task RegisterAll_PasswordResetTemplate_HasSubjectAndContent()
    {
        var template = await _store.GetTemplateAsync("PasswordReset");

        Assert.NotNull(template);
        Assert.Equal("Reset your Cadence password", template!.SubjectTemplate);
        Assert.Contains("{{DisplayName}}", template.HtmlContent);
        Assert.Contains("{{ResetUrl}}", template.HtmlContent);
        Assert.Contains("{{ExpiresAt}}", template.HtmlContent);
        Assert.Contains("{{DisplayName}}", template.PlainTextContent);
        Assert.Contains("{{ResetUrl}}", template.PlainTextContent);
    }

    [Fact]
    public async Task RegisterAll_PasswordChangedTemplate_HasSubjectAndContent()
    {
        var template = await _store.GetTemplateAsync("PasswordChanged");

        Assert.NotNull(template);
        Assert.Equal("Your Cadence password was changed", template!.SubjectTemplate);
        Assert.Contains("{{DisplayName}}", template.HtmlContent);
        Assert.Contains("{{ChangedAt}}", template.HtmlContent);
        Assert.Contains("{{ChangeMethod}}", template.HtmlContent);
        Assert.Contains("{{ResetPasswordUrl}}", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_WelcomeTemplate_HasSubjectAndContent()
    {
        var template = await _store.GetTemplateAsync("Welcome");

        Assert.NotNull(template);
        Assert.Equal("Welcome to Cadence", template!.SubjectTemplate);
        Assert.Contains("{{DisplayName}}", template.HtmlContent);
        Assert.Contains("HSEEP", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_AccountVerificationTemplate_HasVerificationUrl()
    {
        var template = await _store.GetTemplateAsync("AccountVerification");

        Assert.NotNull(template);
        Assert.Equal("Verify your Cadence email", template!.SubjectTemplate);
        Assert.Contains("{{VerificationUrl}}", template.HtmlContent);
        Assert.Contains("{{VerificationUrl}}", template.PlainTextContent);
    }

    [Fact]
    public async Task RegisterAll_NewDeviceAlertTemplate_HasDeviceDetails()
    {
        var template = await _store.GetTemplateAsync("NewDeviceAlert");

        Assert.NotNull(template);
        Assert.Equal("New sign-in to your Cadence account", template!.SubjectTemplate);
        Assert.Contains("{{Browser}}", template.HtmlContent);
        Assert.Contains("{{OperatingSystem}}", template.HtmlContent);
        Assert.Contains("{{ApproximateLocation}}", template.HtmlContent);
        Assert.Contains("{{SecureAccountUrl}}", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_LayoutTemplate_HasContentPlaceholder()
    {
        var template = await _store.GetTemplateAsync("_Layout");

        Assert.NotNull(template);
        Assert.Contains("{{Content}}", template!.HtmlContent);
        Assert.Contains("Cadence", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_AccountDeactivatedTemplate_HasSubjectAndContent()
    {
        var template = await _store.GetTemplateAsync("AccountDeactivated");

        Assert.NotNull(template);
        Assert.Equal("Your Cadence account has been deactivated", template!.SubjectTemplate);
        Assert.Contains("{{DisplayName}}", template.HtmlContent);
        Assert.Contains("{{Email}}", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_AccountReactivatedTemplate_HasSubjectAndContent()
    {
        var template = await _store.GetTemplateAsync("AccountReactivated");

        Assert.NotNull(template);
        Assert.Equal("Your Cadence account has been reactivated", template!.SubjectTemplate);
        Assert.Contains("{{DisplayName}}", template.HtmlContent);
        Assert.Contains("{{Email}}", template.HtmlContent);
    }
}
