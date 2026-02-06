# Story: EM-08-S01 - Bug Report Submission

**As a** User,  
**I want** to submit bug reports directly from Cadence,  
**So that** I can report issues without leaving the application.

## Context

An integrated bug reporting system lowers the barrier to reporting issues. Auto-captured context (URL, browser, user info) helps developers diagnose problems quickly.

## Acceptance Criteria

### Submission Form

- [ ] **Given** I'm logged in, **when** I click "Report Bug" (footer or help menu), **then** I see bug report form
- [ ] **Given** bug report form, **when** opened, **then** current URL is pre-filled
- [ ] **Given** bug report form, **when** opened, **then** browser/OS info is auto-captured
- [ ] **Given** form, **when** filling out, **then** I provide: title, description, steps to reproduce, severity
- [ ] **Given** form, **when** optionally, **then** I can attach screenshot

### Email Generation

- [ ] **Given** bug report submitted, **when** saved, **then** email sent to support address
- [ ] **Given** support email, **when** received, **then** includes all form fields and auto-captured context
- [ ] **Given** support email, **when** received, **then** user's email is Reply-To for easy follow-up
- [ ] **Given** user submits, **when** successful, **then** they see confirmation with ticket reference

### Context Capture

- [ ] **Given** bug report, **when** submitted, **then** includes: URL, browser, OS, screen size, user ID
- [ ] **Given** context, **when** captured, **then** formatted clearly for support team

## Out of Scope

- Ticket tracking system
- Status updates to reporter
- Automatic screenshot capture

## Dependencies

- EM-01-S01: ACS Email Configuration

## Technical Notes

### Bug Report Model

```csharp
public class BugReport
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string StepsToReproduce { get; set; }
    public BugSeverity Severity { get; set; }
    
    // Auto-captured
    public string CurrentUrl { get; set; }
    public string Browser { get; set; }
    public string OperatingSystem { get; set; }
    public string ScreenSize { get; set; }
    public Guid ReportedByUserId { get; set; }
    public string ReportedByEmail { get; set; }
    public DateTime ReportedAt { get; set; }
}
```

## UI/UX Notes

### Bug Report Form

```
┌─────────────────────────────────────────────────────────────────┐
│ Report a Bug                                            [Close] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ Title *                                                         │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Inject status not updating                                  │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ What happened? *                                                │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ When I fire an inject, the status shows "Fired" but        │ │
│ │ reverts to "Pending" after page refresh...                 │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ Steps to reproduce                                              │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 1. Go to exercise MSEL                                     │ │
│ │ 2. Click "Fire" on any inject                              │ │
│ │ 3. Refresh the page                                        │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ Severity                                                        │
│ ○ Low - Minor inconvenience                                     │
│ ● Medium - Affects my work but has workaround                   │
│ ○ High - Blocking my work                                       │
│ ○ Critical - Data loss or security issue                        │
│                                                                 │
│ Screenshot (optional)            [Choose File] No file chosen   │
│                                                                 │
│ ─────────────────────────────────────────────────────────────── │
│ Auto-captured: Chrome 120 on Windows 11 | 1920x1080            │
│ Page: /exercises/abc-123/msel                                   │
│                                                                 │
│                                    [Cancel]  [Submit Report]    │
└─────────────────────────────────────────────────────────────────┘
```

## Effort Estimate

**3 story points** - Form UI, context capture, email formatting

---

*Feature: EM-08 Support & Feedback*  
*Priority: P1*
