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
    [InlineData("OrganizationInvite")]
    [InlineData("WelcomeToOrg")]
    [InlineData("ExerciseInvite")]
    [InlineData("ExternalExerciseInvite")]
    [InlineData("ExerciseDetailsUpdated")]
    [InlineData("InjectSubmitted")]
    [InlineData("InjectApproved")]
    [InlineData("InjectRejected")]
    [InlineData("InjectChangesRequested")]
    [InlineData("InjectAssignment")]
    [InlineData("RoleChange")]
    [InlineData("EvaluatorAreaAssignment")]
    public async Task RegisterAll_RegistersExpectedTemplate(string templateId)
    {
        var template = await _store.GetTemplateAsync(templateId);

        Assert.NotNull(template);
        Assert.Equal(templateId, template!.TemplateId);
    }

    [Fact]
    public async Task RegisterAll_RegistersAll20Templates()
    {
        // Verify all 20 expected templates exist
        var expectedTemplates = new[]
        {
            "_Layout", "PasswordReset", "PasswordChanged", "AccountVerification",
            "NewDeviceAlert", "Welcome", "AccountDeactivated", "AccountReactivated",
            "OrganizationInvite", "WelcomeToOrg",
            "ExerciseInvite", "ExternalExerciseInvite", "ExerciseDetailsUpdated",
            "InjectSubmitted", "InjectApproved", "InjectRejected", "InjectChangesRequested",
            "InjectAssignment", "RoleChange", "EvaluatorAreaAssignment"
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

    [Fact]
    public async Task RegisterAll_OrganizationInviteTemplate_HasInviteDetails()
    {
        var template = await _store.GetTemplateAsync("OrganizationInvite");

        Assert.NotNull(template);
        Assert.Contains("{{OrganizationName}}", template!.SubjectTemplate);
        Assert.Contains("{{InviterName}}", template.HtmlContent);
        Assert.Contains("{{InviteUrl}}", template.HtmlContent);
        Assert.Contains("{{ExpiresAt}}", template.HtmlContent);
        Assert.Contains("{{OrganizationName}}", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_WelcomeToOrgTemplate_HasOrgDetails()
    {
        var template = await _store.GetTemplateAsync("WelcomeToOrg");

        Assert.NotNull(template);
        Assert.Contains("{{OrganizationName}}", template!.SubjectTemplate);
        Assert.Contains("{{DisplayName}}", template.HtmlContent);
        Assert.Contains("{{OrganizationName}}", template.HtmlContent);
        Assert.Contains("{{Role}}", template.HtmlContent);
        Assert.Contains("{{SignInUrl}}", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_ExerciseInviteTemplate_HasExerciseDetails()
    {
        var template = await _store.GetTemplateAsync("ExerciseInvite");

        Assert.NotNull(template);
        Assert.Contains("{{ExerciseName}}", template!.SubjectTemplate);
        Assert.Contains("{{RecipientName}}", template.HtmlContent);
        Assert.Contains("{{ExerciseType}}", template.HtmlContent);
        Assert.Contains("{{RoleName}}", template.HtmlContent);
        Assert.Contains("{{StartDate}}", template.HtmlContent);
        Assert.Contains("{{ExerciseUrl}}", template.HtmlContent);
        Assert.Contains("{{DirectorName}}", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_ExternalExerciseInviteTemplate_HasInviteAndExerciseDetails()
    {
        var template = await _store.GetTemplateAsync("ExternalExerciseInvite");

        Assert.NotNull(template);
        Assert.Contains("{{ExerciseName}}", template!.SubjectTemplate);
        Assert.Contains("{{OrganizationName}}", template.SubjectTemplate);
        Assert.Contains("{{InviteUrl}}", template.HtmlContent);
        Assert.Contains("{{ExpiresAt}}", template.HtmlContent);
        Assert.Contains("{{RoleName}}", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_ExerciseDetailsUpdatedTemplate_HasChangeDetails()
    {
        var template = await _store.GetTemplateAsync("ExerciseDetailsUpdated");

        Assert.NotNull(template);
        Assert.Contains("{{ExerciseName}}", template!.SubjectTemplate);
        Assert.Contains("{{RecipientName}}", template.HtmlContent);
        Assert.Contains("{{Changes}}", template.HtmlContent);
        Assert.Contains("{{RoleName}}", template.HtmlContent);
        Assert.Contains("{{ExerciseUrl}}", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_InjectSubmittedTemplate_HasApprovalDetails()
    {
        var template = await _store.GetTemplateAsync("InjectSubmitted");

        Assert.NotNull(template);
        Assert.Contains("{{InjectNumber}}", template!.SubjectTemplate);
        Assert.Contains("{{InjectTitle}}", template.SubjectTemplate);
        Assert.Contains("{{ApproverName}}", template.HtmlContent);
        Assert.Contains("{{SubmitterName}}", template.HtmlContent);
        Assert.Contains("{{ExerciseName}}", template.HtmlContent);
        Assert.Contains("{{ReviewUrl}}", template.HtmlContent);
        Assert.Contains("{{ReviewUrl}}", template.PlainTextContent);
    }

    [Fact]
    public async Task RegisterAll_InjectApprovedTemplate_HasApprovalDetails()
    {
        var template = await _store.GetTemplateAsync("InjectApproved");

        Assert.NotNull(template);
        Assert.Contains("{{InjectNumber}}", template!.SubjectTemplate);
        Assert.Contains("{{InjectTitle}}", template.SubjectTemplate);
        Assert.Contains("{{SubmitterName}}", template.HtmlContent);
        Assert.Contains("{{ApproverName}}", template.HtmlContent);
        Assert.Contains("{{ExerciseName}}", template.HtmlContent);
        Assert.Contains("{{InjectUrl}}", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_InjectRejectedTemplate_HasFeedbackDetails()
    {
        var template = await _store.GetTemplateAsync("InjectRejected");

        Assert.NotNull(template);
        Assert.Contains("{{InjectNumber}}", template!.SubjectTemplate);
        Assert.Contains("{{SubmitterName}}", template.HtmlContent);
        Assert.Contains("{{ReviewerName}}", template.HtmlContent);
        Assert.Contains("{{RejectionReason}}", template.HtmlContent);
        Assert.Contains("{{InjectUrl}}", template.HtmlContent);
        Assert.Contains("{{RejectionReason}}", template.PlainTextContent);
    }

    [Fact]
    public async Task RegisterAll_InjectChangesRequestedTemplate_HasChangeRequestDetails()
    {
        var template = await _store.GetTemplateAsync("InjectChangesRequested");

        Assert.NotNull(template);
        Assert.Contains("{{InjectNumber}}", template!.SubjectTemplate);
        Assert.Contains("{{SubmitterName}}", template.HtmlContent);
        Assert.Contains("{{ReviewerName}}", template.HtmlContent);
        Assert.Contains("{{RequestedChanges}}", template.HtmlContent);
        Assert.Contains("{{InjectUrl}}", template.HtmlContent);
        Assert.Contains("{{RequestedChanges}}", template.PlainTextContent);
    }

    [Fact]
    public async Task RegisterAll_InjectAssignmentTemplate_HasAssignmentDetails()
    {
        var template = await _store.GetTemplateAsync("InjectAssignment");

        Assert.NotNull(template);
        Assert.Contains("{{ExerciseName}}", template!.SubjectTemplate);
        Assert.Contains("{{ControllerName}}", template.HtmlContent);
        Assert.Contains("{{AssignmentSummary}}", template.HtmlContent);
        Assert.Contains("{{ExerciseUrl}}", template.HtmlContent);
        Assert.Contains("{{AssignmentSummary}}", template.PlainTextContent);
    }

    [Fact]
    public async Task RegisterAll_RoleChangeTemplate_HasRoleDetails()
    {
        var template = await _store.GetTemplateAsync("RoleChange");

        Assert.NotNull(template);
        Assert.Contains("{{ExerciseName}}", template!.SubjectTemplate);
        Assert.Contains("{{RecipientName}}", template.HtmlContent);
        Assert.Contains("{{OldRole}}", template.HtmlContent);
        Assert.Contains("{{NewRole}}", template.HtmlContent);
        Assert.Contains("{{ChangedByName}}", template.HtmlContent);
        Assert.Contains("{{ExerciseUrl}}", template.HtmlContent);
    }

    [Fact]
    public async Task RegisterAll_EvaluatorAreaAssignmentTemplate_HasAreaDetails()
    {
        var template = await _store.GetTemplateAsync("EvaluatorAreaAssignment");

        Assert.NotNull(template);
        Assert.Contains("{{ExerciseName}}", template!.SubjectTemplate);
        Assert.Contains("{{EvaluatorName}}", template.HtmlContent);
        Assert.Contains("{{AssignedArea}}", template.HtmlContent);
        Assert.Contains("{{AreaDescription}}", template.HtmlContent);
        Assert.Contains("{{ExerciseUrl}}", template.HtmlContent);
    }
}
