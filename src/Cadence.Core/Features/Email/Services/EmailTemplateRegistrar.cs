namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Registers all built-in email templates at application startup.
/// Templates use {{PropertyName}} placeholder syntax.
/// </summary>
public static class EmailTemplateRegistrar
{
    /// <summary>
    /// Register all authentication and system email templates.
    /// </summary>
    public static void RegisterAll(InMemoryEmailTemplateStore store)
    {
        RegisterLayoutTemplate(store);
        RegisterPasswordResetTemplate(store);
        RegisterPasswordChangedTemplate(store);
        RegisterAccountVerificationTemplate(store);
        RegisterNewDeviceAlertTemplate(store);
        RegisterWelcomeTemplate(store);
        RegisterAccountDeactivatedTemplate(store);
        RegisterAccountReactivatedTemplate(store);

        // Organization invitation templates
        RegisterOrganizationInviteTemplate(store);
        RegisterOrganizationInviteWithExercisesTemplate(store);
        RegisterWelcomeToOrgTemplate(store);

        // Exercise invitation templates
        RegisterExerciseInviteTemplate(store);
        RegisterExternalExerciseInviteTemplate(store);
        RegisterExerciseDetailsUpdatedTemplate(store);

        // Inject workflow templates
        RegisterInjectSubmittedTemplate(store);
        RegisterInjectApprovedTemplate(store);
        RegisterInjectRejectedTemplate(store);
        RegisterInjectChangesRequestedTemplate(store);

        // Assignment notification templates
        RegisterInjectAssignmentTemplate(store);
        RegisterRoleChangeTemplate(store);
        RegisterEvaluatorAreaAssignmentTemplate(store);

        // Exercise status templates
        RegisterExercisePublishedTemplate(store);
        RegisterExerciseStartedTemplate(store);
        RegisterExerciseCompletedTemplate(store);
        RegisterExerciseCancelledTemplate(store);

        // Support & feedback templates
        RegisterBugReportTemplate(store);
        RegisterFeatureRequestTemplate(store);
        RegisterGeneralFeedbackTemplate(store);
        RegisterSupportTicketAcknowledgmentTemplate(store);

        // Scheduled reminder templates
        RegisterExerciseStartReminderTemplate(store);
        RegisterMselReviewDeadlineTemplate(store);
        RegisterObservationFinalizationTemplate(store);

        // Digest & summary templates
        RegisterDailyDigestTemplate(store);
        RegisterDirectorDailySummaryTemplate(store);
        RegisterWeeklyOrgReportTemplate(store);
    }

    private static void RegisterLayoutTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "_Layout",
            SubjectTemplate: "",
            HtmlContent: @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Cadence</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f5f5f5; font-family: Arial, Helvetica, sans-serif;"">
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""max-width: 600px; margin: 0 auto; background-color: #ffffff;"">
  <tr>
    <td style=""padding: 24px 32px; background-color: #1a237e; text-align: center;"">
      <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">Cadence</h1>
    </td>
  </tr>
  <tr>
    <td style=""padding: 32px;"">
      {{Content}}
    </td>
  </tr>
  <tr>
    <td style=""padding: 16px 32px; background-color: #f5f5f5; text-align: center; font-size: 12px; color: #666666;"">
      <p style=""margin: 0 0 8px 0;"">This is an automated email from Cadence.</p>
      <p style=""margin: 0;"">Cadence - Exercise Management Platform</p>
    </td>
  </tr>
</table>
</body>
</html>",
            PlainTextContent: ""
        ));
    }

    private static void RegisterPasswordResetTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "PasswordReset",
            SubjectTemplate: "Reset your Cadence password",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Password Reset Request</h2>
<p>Hi {{DisplayName}},</p>
<p>We received a request to reset the password for your Cadence account ({{Email}}).</p>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ResetUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Reset Password</a>
</p>
<p>This link expires on {{ExpiresAt}}.</p>
<p style=""color: #666666; font-size: 14px;"">If you didn't request this, you can safely ignore this email. Your password won't change unless you click the link above.</p>",
            PlainTextContent: @"Password Reset Request

Hi {{DisplayName}},

We received a request to reset the password for your Cadence account ({{Email}}).

Reset your password: {{ResetUrl}}

This link expires on {{ExpiresAt}}.

If you didn't request this, you can safely ignore this email. Your password won't change unless you click the link above.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterPasswordChangedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "PasswordChanged",
            SubjectTemplate: "Your Cadence password was changed",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Password Changed</h2>
<p>Hi {{DisplayName}},</p>
<p>Your Cadence password was successfully changed.</p>
<table style=""margin: 16px 0; font-size: 14px;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666;"">Time:</td><td>{{ChangedAt}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666;"">Method:</td><td>{{ChangeMethod}}</td></tr>
</table>
<p>If you made this change, no action is needed.</p>
<p><strong>If you didn't change your password:</strong></p>
<ol>
  <li>Reset your password immediately</li>
  <li>Review your account for unauthorized changes</li>
  <li>Contact support if you need help</li>
</ol>
<p style=""text-align: center; margin: 24px 0;"">
  <a href=""{{ResetPasswordUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold; margin-right: 8px;"">Reset Password</a>
</p>
<p style=""color: #666666; font-size: 12px;"">This is an automated security email from Cadence. You cannot unsubscribe from security notifications.</p>",
            PlainTextContent: @"Password Changed

Hi {{DisplayName}},

Your Cadence password was successfully changed.

Time: {{ChangedAt}}
Method: {{ChangeMethod}}

If you made this change, no action is needed.

If you didn't change your password:
1. Reset your password immediately: {{ResetPasswordUrl}}
2. Review your account for unauthorized changes
3. Contact support if you need help

---
This is an automated security email from Cadence."
        ));
    }

    private static void RegisterAccountVerificationTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "AccountVerification",
            SubjectTemplate: "Verify your Cadence email",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Verify Your Email</h2>
<p>Hi {{DisplayName}},</p>
<p>Please verify your email address to complete your Cadence account setup.</p>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{VerificationUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Verify Email Address</a>
</p>
<p>This link expires on {{ExpiresAt}}.</p>
<p style=""color: #666666; font-size: 14px;"">If you didn't create a Cadence account, you can safely ignore this email.</p>",
            PlainTextContent: @"Verify Your Email

Hi {{DisplayName}},

Please verify your email address to complete your Cadence account setup.

Verify your email: {{VerificationUrl}}

This link expires on {{ExpiresAt}}.

If you didn't create a Cadence account, you can safely ignore this email.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterNewDeviceAlertTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "NewDeviceAlert",
            SubjectTemplate: "New sign-in to your Cadence account",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">New Sign-In Detected</h2>
<p>Hi {{DisplayName}},</p>
<p>We noticed a sign-in to your Cadence account from a new device.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Location:</td><td>{{ApproximateLocation}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Device:</td><td>{{Browser}} on {{OperatingSystem}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Time:</td><td>{{SignInTime}}</td></tr>
</table>
<p>If this was you, you can ignore this email.</p>
<p>If this wasn't you, secure your account immediately:</p>
<p style=""text-align: center; margin: 24px 0;"">
  <a href=""{{SecureAccountUrl}}"" style=""background-color: #d32f2f; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Secure My Account</a>
</p>
<p style=""color: #666666; font-size: 12px;"">This is an automated security email from Cadence. You cannot unsubscribe from security notifications.</p>",
            PlainTextContent: @"New Sign-In Detected

Hi {{DisplayName}},

We noticed a sign-in to your Cadence account from a new device.

Location: {{ApproximateLocation}}
Device: {{Browser}} on {{OperatingSystem}}
Time: {{SignInTime}}

If this was you, you can ignore this email.

If this wasn't you, secure your account immediately:
{{SecureAccountUrl}}

---
This is an automated security email from Cadence."
        ));
    }

    private static void RegisterWelcomeTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "Welcome",
            SubjectTemplate: "Welcome to Cadence",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Welcome to Cadence!</h2>
<p>Hi {{DisplayName}},</p>
<p>Your Cadence account has been created successfully.</p>
<p>Cadence is a HSEEP-compliant exercise management platform that helps emergency management professionals plan, conduct, and evaluate exercises.</p>
<p style=""color: #666666; font-size: 14px;"">If you didn't create this account, please contact support.</p>",
            PlainTextContent: @"Welcome to Cadence!

Hi {{DisplayName}},

Your Cadence account has been created successfully.

Cadence is a HSEEP-compliant exercise management platform that helps emergency management professionals plan, conduct, and evaluate exercises.

If you didn't create this account, please contact support.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterAccountDeactivatedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "AccountDeactivated",
            SubjectTemplate: "Your Cadence account has been deactivated",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Account Deactivated</h2>
<p>Hi {{DisplayName}},</p>
<p>Your Cadence account ({{Email}}) has been deactivated.</p>
<p>If you believe this was done in error, please contact your organization administrator or Cadence support.</p>",
            PlainTextContent: @"Account Deactivated

Hi {{DisplayName}},

Your Cadence account ({{Email}}) has been deactivated.

If you believe this was done in error, please contact your organization administrator or Cadence support.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterAccountReactivatedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "AccountReactivated",
            SubjectTemplate: "Your Cadence account has been reactivated",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Account Reactivated</h2>
<p>Hi {{DisplayName}},</p>
<p>Your Cadence account ({{Email}}) has been reactivated. You can now sign in and access your exercises.</p>",
            PlainTextContent: @"Account Reactivated

Hi {{DisplayName}},

Your Cadence account ({{Email}}) has been reactivated. You can now sign in and access your exercises.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterOrganizationInviteTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "OrganizationInvite",
            SubjectTemplate: "You're invited to join {{OrganizationName}} on Cadence",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">You're Invited!</h2>
<p>Hi,</p>
<p><strong>{{InviterName}}</strong> has invited you to join <strong>{{OrganizationName}}</strong> on Cadence, the exercise management platform.</p>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{InviteUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Accept Invitation</a>
</p>
<p>This invitation expires on {{ExpiresAt}}.</p>
<p style=""color: #666666; font-size: 14px;"">If you weren't expecting this invitation, you can safely ignore this email.</p>",
            PlainTextContent: @"You're Invited!

Hi,

{{InviterName}} has invited you to join {{OrganizationName}} on Cadence, the exercise management platform.

Accept your invitation: {{InviteUrl}}

This invitation expires on {{ExpiresAt}}.

If you weren't expecting this invitation, you can safely ignore this email.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterOrganizationInviteWithExercisesTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "OrganizationInviteWithExercises",
            SubjectTemplate: "You're invited to {{OrganizationName}} for upcoming exercises",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">You're Invited to Participate!</h2>
<p>Hi,</p>
<p><strong>{{InviterName}}</strong> has invited you to join <strong>{{OrganizationName}}</strong> on Cadence and participate in the following exercise(s):</p>
<div style=""margin: 24px 0; padding: 16px; background-color: #e3f2fd; border-left: 4px solid #1a237e; border-radius: 4px;"">
  <h3 style=""color: #1a237e; margin-top: 0;"">Your Exercise Assignments</h3>
  {{PendingExercisesHtml}}
</div>
<p>To access exercise materials and prepare for your role, you'll need to accept this invitation.</p>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{InviteUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Accept Invitation &amp; View Exercises</a>
</p>
<p>This invitation expires on {{ExpiresAt}}.</p>
<p style=""color: #666666; font-size: 14px;"">If you weren't expecting this invitation, you can safely ignore this email.</p>",
            PlainTextContent: @"You're Invited to Participate!

Hi,

{{InviterName}} has invited you to join {{OrganizationName}} on Cadence and participate in the following exercise(s):

YOUR EXERCISE ASSIGNMENTS
{{PendingExercisesText}}

To access exercise materials and prepare for your role, you'll need to accept this invitation.

Accept invitation and view exercises: {{InviteUrl}}

This invitation expires on {{ExpiresAt}}.

If you weren't expecting this invitation, you can safely ignore this email.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterWelcomeToOrgTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "WelcomeToOrg",
            SubjectTemplate: "Welcome to {{OrganizationName}} on Cadence",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Welcome to {{OrganizationName}}!</h2>
<p>Hi {{DisplayName}},</p>
<p>You've successfully joined <strong>{{OrganizationName}}</strong> on Cadence.</p>
<table style=""margin: 16px 0; font-size: 14px;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666;"">Organization:</td><td>{{OrganizationName}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666;"">Your Role:</td><td>{{Role}}</td></tr>
</table>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{SignInUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Sign In to Cadence</a>
</p>
<p style=""color: #666666; font-size: 14px;"">You can manage your notification preferences in your account settings.</p>",
            PlainTextContent: @"Welcome to {{OrganizationName}}!

Hi {{DisplayName}},

You've successfully joined {{OrganizationName}} on Cadence.

Organization: {{OrganizationName}}
Your Role: {{Role}}

Sign in to Cadence: {{SignInUrl}}

You can manage your notification preferences in your account settings.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterExerciseInviteTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "ExerciseInvite",
            SubjectTemplate: "You're invited to participate in {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Exercise Invitation</h2>
<p>Hi {{RecipientName}},</p>
<p>You've been invited to participate in <strong>{{ExerciseName}}</strong> as a <strong>{{RoleName}}</strong>.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Type:</td><td>{{ExerciseType}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Date:</td><td>{{StartDate}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Location:</td><td>{{Location}}</td></tr>
</table>
<h3 style=""color: #1a237e;"">Your Role: {{RoleName}}</h3>
<p>{{RoleDescription}}</p>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ExerciseUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">View Exercise Details</a>
</p>
<p style=""color: #666666; font-size: 14px;"">Questions? Contact Exercise Director {{DirectorName}} at {{DirectorEmail}}.</p>",
            PlainTextContent: @"Exercise Invitation

Hi {{RecipientName}},

You've been invited to participate in {{ExerciseName}} as a {{RoleName}}.

EXERCISE DETAILS
Type: {{ExerciseType}}
Date: {{StartDate}}
Location: {{Location}}

YOUR ROLE: {{RoleName}}
{{RoleDescription}}

View exercise details: {{ExerciseUrl}}

Questions? Contact Exercise Director {{DirectorName}} at {{DirectorEmail}}.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterExternalExerciseInviteTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "ExternalExerciseInvite",
            SubjectTemplate: "You're invited to {{ExerciseName}} by {{OrganizationName}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Exercise Invitation</h2>
<p>Hi,</p>
<p><strong>{{OrganizationName}}</strong> has invited you to participate in <strong>{{ExerciseName}}</strong> as a <strong>{{RoleName}}</strong>.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Exercise:</td><td>{{ExerciseName}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Type:</td><td>{{ExerciseType}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Date:</td><td>{{StartDate}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Location:</td><td>{{Location}}</td></tr>
</table>
<p>To participate, you'll need to create a Cadence account. Click the button below to get started.</p>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{InviteUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Accept Invitation</a>
</p>
<p>This invitation expires on {{ExpiresAt}}.</p>
<p style=""color: #666666; font-size: 14px;"">Questions? Contact Exercise Director {{DirectorName}}.</p>
<p style=""color: #666666; font-size: 14px;"">If you weren't expecting this invitation, you can safely ignore this email.</p>",
            PlainTextContent: @"Exercise Invitation

Hi,

{{OrganizationName}} has invited you to participate in {{ExerciseName}} as a {{RoleName}}.

EXERCISE DETAILS
Exercise: {{ExerciseName}}
Type: {{ExerciseType}}
Date: {{StartDate}}
Location: {{Location}}

To participate, you'll need to create a Cadence account.

Accept your invitation: {{InviteUrl}}

This invitation expires on {{ExpiresAt}}.

Questions? Contact Exercise Director {{DirectorName}}.

If you weren't expecting this invitation, you can safely ignore this email.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterExerciseDetailsUpdatedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "ExerciseDetailsUpdated",
            SubjectTemplate: "Exercise Update: {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Exercise Details Updated</h2>
<p>Hi {{RecipientName}},</p>
<p>The details for <strong>{{ExerciseName}}</strong> have been updated:</p>
<div style=""margin: 16px 0; padding: 16px; background-color: #fff3e0; border-left: 4px solid #ff9800; border-radius: 4px;"">
{{Changes}}
</div>
<p>Your role (<strong>{{RoleName}}</strong>) remains the same.</p>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ExerciseUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">View Updated Details</a>
</p>
<p style=""color: #666666; font-size: 14px;"">Questions? Contact Exercise Director {{DirectorName}}.</p>",
            PlainTextContent: @"Exercise Details Updated

Hi {{RecipientName}},

The details for {{ExerciseName}} have been updated:

{{Changes}}

Your role ({{RoleName}}) remains the same.

View updated details: {{ExerciseUrl}}

Questions? Contact Exercise Director {{DirectorName}}.

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterInjectSubmittedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "InjectSubmitted",
            SubjectTemplate: "Inject pending approval: [#{{InjectNumber}}] {{InjectTitle}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Inject Awaiting Your Approval</h2>
<p>Hi {{ApproverName}},</p>
<p>Inject <strong>#{{InjectNumber}}</strong> has been submitted for approval.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Title:</td><td>{{InjectTitle}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Submitted by:</td><td>{{SubmitterName}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Exercise:</td><td>{{ExerciseName}}</td></tr>
</table>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ReviewUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Review &amp; Approve</a>
</p>",
            PlainTextContent: @"Inject Awaiting Your Approval

Hi {{ApproverName}},

Inject #{{InjectNumber}} has been submitted for approval.

INJECT DETAILS
Title: {{InjectTitle}}
Submitted by: {{SubmitterName}}
Exercise: {{ExerciseName}}

Review and approve: {{ReviewUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterInjectApprovedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "InjectApproved",
            SubjectTemplate: "Inject approved: [#{{InjectNumber}}] {{InjectTitle}}",
            HtmlContent: @"<h2 style=""color: #2e7d32; margin-top: 0;"">Inject Approved</h2>
<p>Hi {{SubmitterName}},</p>
<p>Good news! Your inject has been approved.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Inject:</td><td>#{{InjectNumber}} - {{InjectTitle}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Approved by:</td><td>{{ApproverName}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Exercise:</td><td>{{ExerciseName}}</td></tr>
</table>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{InjectUrl}}"" style=""background-color: #2e7d32; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">View Inject</a>
</p>",
            PlainTextContent: @"Inject Approved

Hi {{SubmitterName}},

Good news! Your inject has been approved.

Inject: #{{InjectNumber}} - {{InjectTitle}}
Approved by: {{ApproverName}}
Exercise: {{ExerciseName}}

View inject: {{InjectUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterInjectRejectedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "InjectRejected",
            SubjectTemplate: "Inject needs revision: [#{{InjectNumber}}] {{InjectTitle}}",
            HtmlContent: @"<h2 style=""color: #d32f2f; margin-top: 0;"">Inject Requires Changes</h2>
<p>Hi {{SubmitterName}},</p>
<p>Your inject was reviewed but needs revisions before approval.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Inject:</td><td>#{{InjectNumber}} - {{InjectTitle}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Reviewed by:</td><td>{{ReviewerName}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Exercise:</td><td>{{ExerciseName}}</td></tr>
</table>
<div style=""margin: 16px 0; padding: 16px; background-color: #ffebee; border-left: 4px solid #d32f2f; border-radius: 4px;"">
  <strong style=""color: #d32f2f;"">Feedback:</strong>
  <p style=""margin: 8px 0 0 0;"">{{RejectionReason}}</p>
</div>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{InjectUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Edit Inject</a>
</p>",
            PlainTextContent: @"Inject Requires Changes

Hi {{SubmitterName}},

Your inject was reviewed but needs revisions before approval.

Inject: #{{InjectNumber}} - {{InjectTitle}}
Reviewed by: {{ReviewerName}}
Exercise: {{ExerciseName}}

FEEDBACK
{{RejectionReason}}

Edit inject: {{InjectUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterInjectChangesRequestedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "InjectChangesRequested",
            SubjectTemplate: "Changes requested: [#{{InjectNumber}}] {{InjectTitle}}",
            HtmlContent: @"<h2 style=""color: #f57c00; margin-top: 0;"">Minor Changes Requested</h2>
<p>Hi {{SubmitterName}},</p>
<p>Your inject is almost ready — just a few tweaks needed.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Inject:</td><td>#{{InjectNumber}} - {{InjectTitle}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Requested by:</td><td>{{ReviewerName}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Exercise:</td><td>{{ExerciseName}}</td></tr>
</table>
<div style=""margin: 16px 0; padding: 16px; background-color: #fff3e0; border-left: 4px solid #f57c00; border-radius: 4px;"">
  <strong style=""color: #f57c00;"">Requested Changes:</strong>
  <p style=""margin: 8px 0 0 0;"">{{RequestedChanges}}</p>
</div>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{InjectUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Make Changes</a>
</p>",
            PlainTextContent: @"Minor Changes Requested

Hi {{SubmitterName}},

Your inject is almost ready - just a few tweaks needed.

Inject: #{{InjectNumber}} - {{InjectTitle}}
Requested by: {{ReviewerName}}
Exercise: {{ExerciseName}}

REQUESTED CHANGES
{{RequestedChanges}}

Make changes: {{InjectUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterInjectAssignmentTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "InjectAssignment",
            SubjectTemplate: "You've been assigned injects for {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Inject Assignment</h2>
<p>Hi {{ControllerName}},</p>
<p>You've been assigned to deliver injects for <strong>{{ExerciseName}}</strong>.</p>
<div style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px;"">
  <strong>Your Assigned Injects</strong>
  <div style=""margin-top: 12px;"">{{AssignmentSummary}}</div>
</div>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ExerciseUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">View Your Injects</a>
</p>",
            PlainTextContent: @"Inject Assignment

Hi {{ControllerName}},

You've been assigned to deliver injects for {{ExerciseName}}.

YOUR ASSIGNED INJECTS
{{AssignmentSummary}}

View your injects: {{ExerciseUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterRoleChangeTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "RoleChange",
            SubjectTemplate: "Your role changed in {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Role Change Notification</h2>
<p>Hi {{RecipientName}},</p>
<p>Your role in <strong>{{ExerciseName}}</strong> has been updated.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">From:</td><td>{{OldRole}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">To:</td><td>{{NewRole}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Changed by:</td><td>{{ChangedByName}}</td></tr>
</table>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ExerciseUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">View Exercise</a>
</p>",
            PlainTextContent: @"Role Change Notification

Hi {{RecipientName}},

Your role in {{ExerciseName}} has been updated.

ROLE CHANGE
From: {{OldRole}}
To: {{NewRole}}
Changed by: {{ChangedByName}}

View exercise: {{ExerciseUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterEvaluatorAreaAssignmentTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "EvaluatorAreaAssignment",
            SubjectTemplate: "Your evaluation assignment: {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Evaluation Area Assignment</h2>
<p>Hi {{EvaluatorName}},</p>
<p>You've been assigned to evaluate the following area in <strong>{{ExerciseName}}</strong>.</p>
<div style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px;"">
  <strong>Assigned Area</strong>
  <p style=""margin: 8px 0 0 0; font-size: 16px;"">{{AssignedArea}}</p>
  <p style=""margin: 8px 0 0 0; color: #666666;"">{{AreaDescription}}</p>
</div>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ExerciseUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">View Evaluation Guide</a>
</p>",
            PlainTextContent: @"Evaluation Area Assignment

Hi {{EvaluatorName}},

You've been assigned to evaluate the following area in {{ExerciseName}}.

ASSIGNED AREA
{{AssignedArea}}
{{AreaDescription}}

View evaluation guide: {{ExerciseUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    // ── EM-07: Exercise Status Templates ──────────────────────────

    private static void RegisterExercisePublishedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "ExercisePublished",
            SubjectTemplate: "Exercise published: {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Exercise Ready for Conduct</h2>
<p>Hi {{RecipientName}},</p>
<p><strong>{{ExerciseName}}</strong> has been published and is ready for conduct.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Date:</td><td>{{ExerciseDate}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Location:</td><td>{{Location}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Your Role:</td><td>{{RoleName}}</td></tr>
</table>
<p>Please review your assignments and prepare for exercise day.</p>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ExerciseUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">View Exercise Details</a>
</p>",
            PlainTextContent: @"Exercise Ready for Conduct

Hi {{RecipientName}},

{{ExerciseName}} has been published and is ready for conduct.

EXERCISE DETAILS
Date: {{ExerciseDate}}
Location: {{Location}}
Your Role: {{RoleName}}

Please review your assignments and prepare for exercise day.

View exercise details: {{ExerciseUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterExerciseStartedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "ExerciseStarted",
            SubjectTemplate: "Exercise ACTIVE: {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #2e7d32; margin-top: 0;"">Exercise Now Active</h2>
<p>Hi {{RecipientName}},</p>
<p><strong>{{ExerciseName}}</strong> is now in progress.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #e8f5e9; border-left: 4px solid #2e7d32; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Status:</td><td><strong style=""color: #2e7d32;"">ACTIVE</strong></td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Started:</td><td>{{StartedAt}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Scenario Time:</td><td>{{ScenarioTime}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Your Role:</td><td>{{RoleName}}</td></tr>
</table>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ExerciseUrl}}"" style=""background-color: #2e7d32; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Open Exercise</a>
</p>
<p>Good luck!</p>",
            PlainTextContent: @"Exercise Now Active

Hi {{RecipientName}},

{{ExerciseName}} is now in progress.

EXERCISE STATUS: ACTIVE
Started: {{StartedAt}}
Scenario Time: {{ScenarioTime}}
Your Role: {{RoleName}}

Open exercise: {{ExerciseUrl}}

Good luck!

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterExerciseCompletedTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "ExerciseCompleted",
            SubjectTemplate: "Exercise complete: {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Exercise Complete</h2>
<p>Hi {{RecipientName}},</p>
<p><strong>{{ExerciseName}}</strong> has concluded. Thank you for your participation!</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Duration:</td><td>{{Duration}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Completed:</td><td>{{CompletedAt}}</td></tr>
</table>
<div style=""margin: 16px 0; padding: 16px; background-color: #e3f2fd; border-left: 4px solid #1a237e; border-radius: 4px;"">
  <strong>Next Steps</strong>
  <div style=""margin-top: 8px;"">{{NextSteps}}</div>
</div>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ExerciseUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">View Exercise Summary</a>
</p>
<p>Thank you for making this exercise a success!</p>",
            PlainTextContent: @"Exercise Complete

Hi {{RecipientName}},

{{ExerciseName}} has concluded. Thank you for your participation!

SUMMARY
Duration: {{Duration}}
Completed: {{CompletedAt}}

NEXT STEPS
{{NextSteps}}

View exercise summary: {{ExerciseUrl}}

Thank you for making this exercise a success!

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterExerciseCancelledTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "ExerciseCancelled",
            SubjectTemplate: "CANCELLED: {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #d32f2f; margin-top: 0;"">Exercise Cancelled</h2>
<p>Hi {{RecipientName}},</p>
<p><strong>{{ExerciseName}}</strong> scheduled for {{ExerciseDate}} has been <strong style=""color: #d32f2f;"">CANCELLED</strong>.</p>
<div style=""margin: 16px 0; padding: 16px; background-color: #ffebee; border-left: 4px solid #d32f2f; border-radius: 4px;"">
  <strong style=""color: #d32f2f;"">Reason:</strong>
  <p style=""margin: 8px 0 0 0;"">{{CancellationReason}}</p>
</div>
<p>We apologize for any inconvenience. The exercise may be rescheduled — watch for updates.</p>
<p style=""color: #666666; font-size: 14px;"">Questions? Contact Exercise Director {{DirectorName}} at {{DirectorEmail}}.</p>",
            PlainTextContent: @"Exercise Cancelled

Hi {{RecipientName}},

{{ExerciseName}} scheduled for {{ExerciseDate}} has been CANCELLED.

REASON
{{CancellationReason}}

We apologize for any inconvenience. The exercise may be rescheduled - watch for updates.

Questions? Contact Exercise Director {{DirectorName}} at {{DirectorEmail}}.

---
Cadence - Exercise Management Platform"
        ));
    }

    // ── EM-08: Support & Feedback Templates ───────────────────────

    private static void RegisterBugReportTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "BugReport",
            SubjectTemplate: "Bug Report: {{Title}}",
            HtmlContent: @"<h2 style=""color: #d32f2f; margin-top: 0;"">Bug Report</h2>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Title:</td><td>{{Title}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Severity:</td><td>{{Severity}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Reporter:</td><td>{{ReporterName}} ({{ReporterEmail}})</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Reported:</td><td>{{ReportedAt}}</td></tr>
</table>
<h3>Description</h3>
<p>{{Description}}</p>
<h3>Steps to Reproduce</h3>
<p>{{StepsToReproduce}}</p>
<h3>Environment</h3>
<table style=""margin: 8px 0; font-size: 14px;"">
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">URL:</td><td>{{CurrentUrl}}</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">Browser:</td><td>{{Browser}}</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">OS:</td><td>{{OperatingSystem}}</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">Screen:</td><td>{{ScreenSize}}</td></tr>
</table>
<h3>Submission Context</h3>
<table style=""margin: 8px 0; font-size: 14px;"">
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">App Version:</td><td>{{AppVersion}} ({{CommitSha}})</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">Organisation:</td><td>{{OrgName}} [{{OrgRole}}]</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">Exercise:</td><td>{{ExerciseName}}{{ExerciseRole}}</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">User Role:</td><td>{{UserRole}}</td></tr>
</table>",
            PlainTextContent: @"Bug Report

Title: {{Title}}
Severity: {{Severity}}
Reporter: {{ReporterName}} ({{ReporterEmail}})
Reported: {{ReportedAt}}

DESCRIPTION
{{Description}}

STEPS TO REPRODUCE
{{StepsToReproduce}}

ENVIRONMENT
URL: {{CurrentUrl}}
Browser: {{Browser}}
OS: {{OperatingSystem}}
Screen: {{ScreenSize}}

SUBMISSION CONTEXT
App Version: {{AppVersion}} ({{CommitSha}})
Organisation: {{OrgName}} [{{OrgRole}}]
Exercise: {{ExerciseName}}{{ExerciseRole}}
User Role: {{UserRole}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterFeatureRequestTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "FeatureRequest",
            SubjectTemplate: "Feature Request: {{Title}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Feature Request</h2>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Title:</td><td>{{Title}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">From:</td><td>{{ReporterName}} ({{ReporterEmail}})</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Submitted:</td><td>{{ReportedAt}}</td></tr>
</table>
<h3>Description</h3>
<p>{{Description}}</p>
<h3>Use Case</h3>
<p>{{UseCase}}</p>
<h3>Submission Context</h3>
<table style=""margin: 8px 0; font-size: 14px;"">
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">App Version:</td><td>{{AppVersion}} ({{CommitSha}})</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">Organisation:</td><td>{{OrgName}} [{{OrgRole}}]</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">Exercise:</td><td>{{ExerciseName}}{{ExerciseRole}}</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">User Role:</td><td>{{UserRole}}</td></tr>
</table>",
            PlainTextContent: @"Feature Request

Title: {{Title}}
From: {{ReporterName}} ({{ReporterEmail}})
Submitted: {{ReportedAt}}

DESCRIPTION
{{Description}}

USE CASE
{{UseCase}}

SUBMISSION CONTEXT
App Version: {{AppVersion}} ({{CommitSha}})
Organisation: {{OrgName}} [{{OrgRole}}]
Exercise: {{ExerciseName}}{{ExerciseRole}}
User Role: {{UserRole}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterGeneralFeedbackTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "GeneralFeedback",
            SubjectTemplate: "Feedback [{{Category}}]: {{Subject}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">User Feedback</h2>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Category:</td><td>{{Category}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Subject:</td><td>{{Subject}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">From:</td><td>{{SenderName}} ({{SenderEmail}})</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Sent:</td><td>{{SentAt}}</td></tr>
</table>
<h3>Message</h3>
<p>{{Message}}</p>
<h3>Submission Context</h3>
<table style=""margin: 8px 0; font-size: 14px;"">
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">App Version:</td><td>{{AppVersion}} ({{CommitSha}})</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">Organisation:</td><td>{{OrgName}} [{{OrgRole}}]</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">Exercise:</td><td>{{ExerciseName}}{{ExerciseRole}}</td></tr>
  <tr><td style=""padding: 2px 12px 2px 0; color: #666666;"">User Role:</td><td>{{UserRole}}</td></tr>
</table>",
            PlainTextContent: @"User Feedback

Category: {{Category}}
Subject: {{Subject}}
From: {{SenderName}} ({{SenderEmail}})
Sent: {{SentAt}}

MESSAGE
{{Message}}

SUBMISSION CONTEXT
App Version: {{AppVersion}} ({{CommitSha}})
Organisation: {{OrgName}} [{{OrgRole}}]
Exercise: {{ExerciseName}}{{ExerciseRole}}
User Role: {{UserRole}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterSupportTicketAcknowledgmentTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "SupportTicketAcknowledgment",
            SubjectTemplate: "We received your feedback [{{ReferenceNumber}}]",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Thanks for Reaching Out</h2>
<p>Hi {{RecipientName}},</p>
<p>We've received your submission and will review it shortly.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Reference:</td><td>{{ReferenceNumber}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Type:</td><td>{{TicketType}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Title:</td><td>{{TicketTitle}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Submitted:</td><td>{{SubmittedAt}}</td></tr>
</table>
<div style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px;"">
  <strong>Your Message</strong>
  <p style=""margin: 8px 0 0 0;"">{{MessagePreview}}</p>
</div>
<h3>What's Next</h3>
<p>We typically respond within 1-2 business days. For urgent issues during an exercise, please contact your Exercise Director.</p>
<p style=""color: #666666; font-size: 14px;"">To follow up on this ticket, reply to this email.</p>",
            PlainTextContent: @"Thanks for Reaching Out

Hi {{RecipientName}},

We've received your submission and will review it shortly.

REFERENCE: {{ReferenceNumber}}
Type: {{TicketType}}
Title: {{TicketTitle}}
Submitted: {{SubmittedAt}}

YOUR MESSAGE
{{MessagePreview}}

WHAT'S NEXT
We typically respond within 1-2 business days. For urgent issues during an exercise, please contact your Exercise Director.

To follow up on this ticket, reply to this email.

---
Cadence - Exercise Management Platform"
        ));
    }

    // ── EM-09: Scheduled Reminder Templates ───────────────────────

    private static void RegisterExerciseStartReminderTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "ExerciseStartReminder",
            SubjectTemplate: "Reminder: {{ExerciseName}} starts tomorrow",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Exercise Reminder</h2>
<p>Hi {{RecipientName}},</p>
<p><strong>{{ExerciseName}}</strong> starts in 24 hours.</p>
<table style=""margin: 16px 0; padding: 16px; background-color: #f5f5f5; border-radius: 4px; width: 100%;"">
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Date:</td><td>{{ExerciseDate}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Time:</td><td>{{ExerciseTime}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Location:</td><td>{{Location}}</td></tr>
  <tr><td style=""padding: 4px 16px 4px 0; color: #666666; font-weight: bold;"">Your Role:</td><td>{{RoleName}}</td></tr>
</table>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ExerciseUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">View Exercise</a>
</p>
<p>Good luck tomorrow!</p>",
            PlainTextContent: @"Exercise Reminder

Hi {{RecipientName}},

{{ExerciseName}} starts in 24 hours.

EXERCISE DETAILS
Date: {{ExerciseDate}}
Time: {{ExerciseTime}}
Location: {{Location}}
Your Role: {{RoleName}}

View exercise: {{ExerciseUrl}}

Good luck tomorrow!

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterMselReviewDeadlineTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "MselReviewDeadline",
            SubjectTemplate: "{{PendingCount}} injects awaiting your review: {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #f57c00; margin-top: 0;"">MSEL Review Reminder</h2>
<p>Hi {{ApproverName}},</p>
<p>You have injects awaiting approval for <strong>{{ExerciseName}}</strong>.</p>
<div style=""margin: 16px 0; padding: 16px; background-color: #fff3e0; border-left: 4px solid #f57c00; border-radius: 4px;"">
  <strong>Pending Your Review</strong>
  <div style=""margin-top: 8px;"">{{PendingInjectsList}}</div>
</div>
<p>Exercise date: {{ExerciseDate}}</p>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ReviewUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Review Pending Injects</a>
</p>",
            PlainTextContent: @"MSEL Review Reminder

Hi {{ApproverName}},

You have injects awaiting approval for {{ExerciseName}}.

PENDING YOUR REVIEW
{{PendingInjectsList}}

Exercise date: {{ExerciseDate}}

Review pending injects: {{ReviewUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterObservationFinalizationTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "ObservationFinalization",
            SubjectTemplate: "Please finalize your observations: {{ExerciseName}}",
            HtmlContent: @"<h2 style=""color: #f57c00; margin-top: 0;"">Observation Finalization Reminder</h2>
<p>Hi {{EvaluatorName}},</p>
<p>You have draft observations from <strong>{{ExerciseName}}</strong> that need to be finalized for the After-Action Report.</p>
<div style=""margin: 16px 0; padding: 16px; background-color: #fff3e0; border-left: 4px solid #f57c00; border-radius: 4px;"">
  <strong>{{DraftCount}} observations pending review</strong>
  <p style=""margin: 8px 0 0 0;"">Deadline: {{Deadline}}</p>
</div>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ObservationsUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Review &amp; Finalize</a>
</p>
<p>Please review your observations, add any missing details, and mark them as final.</p>",
            PlainTextContent: @"Observation Finalization Reminder

Hi {{EvaluatorName}},

You have draft observations from {{ExerciseName}} that need to be finalized for the After-Action Report.

{{DraftCount}} observations pending review
Deadline: {{Deadline}}

Review and finalize: {{ObservationsUrl}}

Please review your observations, add any missing details, and mark them as final.

---
Cadence - Exercise Management Platform"
        ));
    }

    // ── EM-10: Digest & Summary Templates ─────────────────────────

    private static void RegisterDailyDigestTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "DailyDigest",
            SubjectTemplate: "Your Cadence daily digest - {{DigestDate}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Daily Activity Summary</h2>
<p style=""color: #666666;"">{{DigestDate}}</p>
<p>Hi {{RecipientName}},</p>
<p>Here's a summary of activity across your exercises.</p>
<div style=""margin: 16px 0;"">{{ActivitySummary}}</div>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{DashboardUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Open Cadence</a>
</p>
<p style=""color: #666666; font-size: 12px;"">Manage digest preferences: <a href=""{{PreferencesUrl}}"" style=""color: #1a237e;"">Notification Settings</a></p>",
            PlainTextContent: @"Daily Activity Summary
{{DigestDate}}

Hi {{RecipientName}},

Here's a summary of activity across your exercises.

{{ActivitySummary}}

Open Cadence: {{DashboardUrl}}

Manage digest preferences: {{PreferencesUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterDirectorDailySummaryTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "DirectorDailySummary",
            SubjectTemplate: "Director Summary: {{ExerciseName}} - {{SummaryDate}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Exercise Director Summary</h2>
<p><strong>{{ExerciseName}}</strong> | {{SummaryDate}}</p>
<p>Hi {{DirectorName}},</p>
<div style=""margin: 16px 0; padding: 16px; background-color: #e3f2fd; border-radius: 4px; text-align: center;"">
  <strong style=""font-size: 18px;"">Exercise Countdown: {{DaysUntilExercise}} days</strong>
</div>
<h3 style=""color: #1a237e;"">MSEL Status</h3>
<div style=""margin: 8px 0;"">{{MselStatus}}</div>
<div style=""margin: 16px 0; padding: 16px; background-color: #fff3e0; border-left: 4px solid #f57c00; border-radius: 4px;"">
  <strong style=""color: #f57c00;"">Attention Needed</strong>
  <div style=""margin-top: 8px;"">{{AttentionItems}}</div>
</div>
<h3 style=""color: #1a237e;"">Participant Status</h3>
<div style=""margin: 8px 0;"">{{ParticipantStatus}}</div>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{ExerciseUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">Open Exercise Dashboard</a>
</p>",
            PlainTextContent: @"Exercise Director Summary
{{ExerciseName}} | {{SummaryDate}}

Hi {{DirectorName}},

EXERCISE COUNTDOWN: {{DaysUntilExercise}} DAYS

MSEL STATUS
{{MselStatus}}

ATTENTION NEEDED
{{AttentionItems}}

PARTICIPANT STATUS
{{ParticipantStatus}}

Open exercise dashboard: {{ExerciseUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }

    private static void RegisterWeeklyOrgReportTemplate(InMemoryEmailTemplateStore store)
    {
        store.AddTemplate(new EmailTemplate(
            TemplateId: "WeeklyOrgReport",
            SubjectTemplate: "Weekly Report: {{OrganizationName}} - {{ReportPeriod}}",
            HtmlContent: @"<h2 style=""color: #1a237e; margin-top: 0;"">Weekly Organization Report</h2>
<p><strong>{{OrganizationName}}</strong></p>
<p style=""color: #666666;"">{{ReportPeriod}}</p>
<p>Hi {{RecipientName}},</p>
<h3 style=""color: #1a237e;"">This Week's Activity</h3>
<div style=""margin: 8px 0;"">{{ActivityMetrics}}</div>
<h3 style=""color: #1a237e;"">Upcoming Exercises</h3>
<div style=""margin: 8px 0;"">{{UpcomingExercises}}</div>
<h3 style=""color: #1a237e;"">Team Updates</h3>
<div style=""margin: 8px 0;"">{{TeamUpdates}}</div>
<p style=""text-align: center; margin: 32px 0;"">
  <a href=""{{DashboardUrl}}"" style=""background-color: #1a237e; color: #ffffff; padding: 12px 32px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold;"">View Organization Dashboard</a>
</p>",
            PlainTextContent: @"Weekly Organization Report
{{OrganizationName}}
{{ReportPeriod}}

Hi {{RecipientName}},

THIS WEEK'S ACTIVITY
{{ActivityMetrics}}

UPCOMING EXERCISES
{{UpcomingExercises}}

TEAM UPDATES
{{TeamUpdates}}

View organization dashboard: {{DashboardUrl}}

---
Cadence - Exercise Management Platform"
        ));
    }
}
