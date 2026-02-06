# Story: EM-01-S03 - Email Delivery Tracking

**As an** OrgAdmin,  
**I want** to know that emails are being delivered successfully,  
**So that** I can troubleshoot if participants aren't receiving communications.

## Context

Email delivery tracking provides visibility into whether emails reach recipients. This enables troubleshooting delivery issues, provides an audit trail for compliance, and helps identify potential problems before they affect exercise operations.

## Acceptance Criteria

### Delivery Logging

- [ ] **Given** an email is sent, **when** ACS returns, **then** a log entry is created with message ID, recipient, template, status
- [ ] **Given** an email is sent, **when** logged, **then** timestamp, sender, and subject are recorded
- [ ] **Given** email contains sensitive data, **when** logged, **then** body content is NOT stored (only metadata)
- [ ] **Given** ACS reports delivery status update, **when** webhook fires, **then** log entry is updated

### Status Tracking

- [ ] **Given** email is queued, **when** viewing log, **then** status shows "Queued"
- [ ] **Given** email is delivered, **when** ACS confirms, **then** status updates to "Delivered"
- [ ] **Given** email bounces, **when** ACS reports, **then** status updates to "Bounced" with reason
- [ ] **Given** email fails, **when** ACS reports, **then** status updates to "Failed" with error details

### Application Insights Integration

- [ ] **Given** email is sent, **when** successful, **then** custom metric "EmailSent" is recorded
- [ ] **Given** email fails, **when** error occurs, **then** exception is logged to Application Insights
- [ ] **Given** any email operation, **when** completed, **then** correlation ID links to request trace

### Admin Visibility

- [ ] **Given** I'm an OrgAdmin, **when** I view email logs, **then** I see emails for my organization only
- [ ] **Given** email log list, **when** viewing, **then** I see recipient, subject, status, sent date
- [ ] **Given** a bounced email, **when** viewing details, **then** I see bounce reason and suggested action
- [ ] **Given** email logs exist, **when** filtering, **then** I can filter by status, date range, recipient

## Out of Scope

- Open/click tracking (privacy concerns, not needed for transactional)
- Email analytics dashboard (beyond basic delivery status)
- Automatic retry of failed emails (manual resend via invitation features)
- Real-time delivery notifications

## Dependencies

- EM-01-S01: ACS Email Configuration (provides message IDs)

## Technical Notes

### EmailLog Entity

```csharp
public class EmailLog
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }              // Recipient if known user
    public string RecipientEmail { get; set; }
    public string Subject { get; set; }
    public string TemplateId { get; set; }
    public string AcsMessageId { get; set; }       // For status lookups
    public EmailStatus Status { get; set; }
    public string? StatusDetail { get; set; }      // Bounce reason, error message
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public string? RelatedEntityType { get; set; } // "Exercise", "Inject", etc.
    public Guid? RelatedEntityId { get; set; }
}

public enum EmailStatus
{
    Queued,
    Sent,
    Delivered,
    Bounced,
    Failed,
    Suppressed  // User opted out
}
```

### ACS Webhook Handler

```csharp
[ApiController]
[Route("api/webhooks/email")]
public class EmailWebhookController : ControllerBase
{
    [HttpPost("status")]
    public async Task<IActionResult> HandleStatusUpdate(
        [FromBody] AcsEmailStatusEvent statusEvent)
    {
        // Update EmailLog based on ACS event
        // Log to Application Insights
        return Ok();
    }
}
```

### Application Insights Telemetry

```csharp
public class EmailTelemetry
{
    private readonly TelemetryClient _telemetry;
    
    public void TrackEmailSent(EmailLog log)
    {
        _telemetry.TrackEvent("EmailSent", new Dictionary<string, string>
        {
            ["TemplateId"] = log.TemplateId,
            ["OrganizationId"] = log.OrganizationId.ToString(),
            ["Status"] = log.Status.ToString()
        });
    }
    
    public void TrackEmailFailed(EmailLog log, Exception ex)
    {
        _telemetry.TrackException(ex, new Dictionary<string, string>
        {
            ["TemplateId"] = log.TemplateId,
            ["RecipientEmail"] = log.RecipientEmail // Consider masking
        });
    }
}
```

## UI/UX Notes

### Email Log List View

```
┌─────────────────────────────────────────────────────────────────┐
│ Email Logs                                          [Filter ▼]  │
├─────────────────────────────────────────────────────────────────┤
│ Status   │ Recipient              │ Subject           │ Sent    │
├──────────┼────────────────────────┼───────────────────┼─────────┤
│ ✓ Sent   │ jane@example.com       │ Exercise Invite   │ 2h ago  │
│ ✓ Sent   │ bob@example.com        │ Password Reset    │ 3h ago  │
│ ⚠ Bounce │ old@invalid.com        │ Exercise Invite   │ 1d ago  │
│ ✓ Sent   │ alice@example.com      │ Welcome to Cade...│ 2d ago  │
└──────────┴────────────────────────┴───────────────────┴─────────┘
```

### Bounce Detail View

```
┌─────────────────────────────────────────────────────────────────┐
│ Email Details                                           [Close] │
├─────────────────────────────────────────────────────────────────┤
│ To: old@invalid.com                                             │
│ Subject: You're Invited to Operation Thunderstorm               │
│ Sent: February 5, 2026 at 3:42 PM                              │
│ Status: ⚠ Bounced                                               │
│                                                                 │
│ Bounce Reason: Mailbox not found                                │
│ Suggested Action: Verify the email address with the recipient   │
│                   and resend the invitation.                    │
│                                                                 │
│ Related: Exercise Invitation for Operation Thunderstorm         │
│                                                                 │
│                                              [Resend Invitation] │
└─────────────────────────────────────────────────────────────────┘
```

## Domain Terms

| Term | Definition |
|------|------------|
| Bounce | Email rejected by recipient's mail server |
| Hard Bounce | Permanent failure (invalid address) |
| Soft Bounce | Temporary failure (mailbox full, server down) |
| Delivery Status | Current state of email in delivery pipeline |

## Open Questions

- [ ] How long should email logs be retained? (30 days? 90 days?)
- [ ] Should we mask email addresses in logs for privacy?

## Effort Estimate

**3 story points** - Database logging, webhook handler, basic UI

---

*Feature: EM-01 Email Infrastructure*  
*Priority: P0*
