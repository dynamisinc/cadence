# Story: EM-01-S01 - ACS Email Configuration

**As a** System,  
**I want** a configured Azure Communication Services email integration,  
**So that** Cadence can send transactional emails reliably.

## Context

Azure Communication Services (ACS) provides email delivery with high deliverability rates, tracking, and cost-effective pricing. This story establishes the foundational email sending capability that all other email features depend on. The implementation uses an abstraction layer (IEmailService) to enable easy testing and potential future provider changes.

## Acceptance Criteria

### Configuration

- [ ] **Given** ACS is provisioned in Azure, **when** connection string is configured in appsettings, **then** the email service initializes successfully on app startup
- [ ] **Given** ACS configuration is missing, **when** app starts, **then** a clear error message is logged and email-dependent features are disabled gracefully
- [ ] **Given** valid configuration, **when** IEmailService is injected, **then** it resolves to AzureCommunicationEmailService in production

### Email Sending

- [ ] **Given** a valid email request, **when** SendAsync is called, **then** the email is queued for delivery and returns a tracking ID
- [ ] **Given** an invalid recipient address, **when** SendAsync is called, **then** a validation exception is thrown before sending
- [ ] **Given** ACS service is unavailable, **when** SendAsync is called, **then** an appropriate exception is thrown with retry guidance

### Development Mode

- [ ] **Given** environment is Development, **when** IEmailService is injected, **then** LoggingEmailService is used (no actual emails sent)
- [ ] **Given** LoggingEmailService is active, **when** SendAsync is called, **then** email content is logged to console/file for verification
- [ ] **Given** development mode, **when** viewing logs, **then** full email content (HTML, plain text, recipients) is visible

### Sender Configuration

- [ ] **Given** organization has custom sender configured, **when** sending email, **then** use organization's sender address
- [ ] **Given** organization has no custom sender, **when** sending email, **then** use default Cadence sender address
- [ ] **Given** any email sent, **when** composing, **then** Reply-To header points to appropriate address (support or no-reply)

## Out of Scope

- Custom domain setup (uses ACS default domain)
- Email receiving/inbound processing
- Attachment handling (separate story if needed)
- Rate limiting (handled by ACS)

## Dependencies

- Azure Communication Services resource provisioned
- Connection string stored in Azure Key Vault or appsettings

## Technical Notes

### IEmailService Interface

```csharp
public interface IEmailService
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default);
    Task<EmailSendResult> SendTemplatedAsync(string templateId, object model, EmailRecipient recipient, CancellationToken ct = default);
}

public record EmailMessage(
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    EmailRecipient To,
    EmailSender? From = null,
    string? ReplyTo = null
);

public record EmailSendResult(
    string MessageId,
    EmailSendStatus Status,
    string? ErrorMessage = null
);
```

### Configuration Structure

```json
{
  "Email": {
    "Provider": "AzureCommunicationServices",
    "ConnectionString": "endpoint=https://xxx.communication.azure.com/;accesskey=xxx",
    "DefaultSenderAddress": "noreply@cadence-app.com",
    "DefaultSenderName": "Cadence",
    "SupportAddress": "support@cadence-app.com"
  }
}
```

### Service Registration

```csharp
// In Program.cs or DI configuration
if (environment.IsDevelopment())
{
    services.AddSingleton<IEmailService, LoggingEmailService>();
}
else
{
    services.AddSingleton<IEmailService, AzureCommunicationEmailService>();
}
```

## Domain Terms

| Term | Definition |
|------|------------|
| ACS | Azure Communication Services - Microsoft's cloud communication platform |
| Transactional Email | Automated emails triggered by user actions (not marketing) |
| Email Tracking ID | Unique identifier returned by ACS for delivery status lookup |

## Open Questions

- [ ] Should we implement retry logic in the service or rely on calling code?
- [ ] What is the fallback behavior if ACS quota is exceeded?

## Effort Estimate

**5 story points** - Moderate complexity, external service integration, abstraction layer setup

---

*Feature: EM-01 Email Infrastructure*  
*Priority: P0*
