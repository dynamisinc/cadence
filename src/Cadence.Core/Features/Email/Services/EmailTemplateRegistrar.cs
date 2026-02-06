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
}
