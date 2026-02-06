# Epic: Email Communications

## Vision

Cadence provides comprehensive email communications that keep exercise participants informed throughout the exercise lifecycle—from initial invitations through post-exercise reporting. Emails are professionally branded, reliably delivered, and respect user preferences while ensuring critical security and invitation messages always reach recipients.

## Business Value

- **Reduced manual coordination**: Automated notifications eliminate need for manual email composition
- **Professional appearance**: Branded templates present organizations professionally
- **Improved participation**: Timely reminders increase exercise engagement
- **Security compliance**: Mandatory security emails protect user accounts
- **Audit trail**: Delivery tracking provides accountability for communications

## Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Email delivery rate | N/A | >98% |
| Invitation acceptance rate | Manual tracking | >85% within 7 days |
| Password reset completion | N/A | >90% within 1 hour |
| User preference compliance | N/A | 100% (no unwanted emails) |

## User Personas

| Persona | Email Needs |
|---------|-------------|
| **OrgAdmin** | Invite users, receive org reports, configure branding |
| **Exercise Director** | Invite participants, send updates, receive summaries |
| **Controller** | Receive assignments, inject notifications |
| **Evaluator** | Receive assignments, area notifications |
| **All Users** | Security emails, preference management |

## Features

| Feature | Description | Priority | Stories |
|---------|-------------|----------|---------|
| **EM-01** | Email Infrastructure | P0 | 4 |
| **EM-02** | Organization Invitations | P0 | 5 |
| **EM-03** | Exercise Invitations | P0 | 3 |
| **EM-04** | Authentication Emails | P0 | 4 |
| **EM-05** | Inject Workflow Notifications | P1 | 4 |
| **EM-06** | Assignment Notifications | P1 | 3 |
| **EM-07** | Exercise Status Notifications | P1 | 4 |
| **EM-08** | Support & Feedback | P1 | 4 |
| **EM-09** | Scheduled Reminders | P2 | 3 |
| **EM-10** | Digest & Summary Emails | P2 | 3 |

**Total: 37 stories**

## Story Index

### EM-01: Email Infrastructure (P0)

| Story | File | Priority | Effort |
|-------|------|----------|--------|
| EM-01-S01 | [ACS Email Configuration](stories/EM-01-S01-acs-configuration.md) | P0 | 5 pts |
| EM-01-S02 | [Email Template System](stories/EM-01-S02-template-system.md) | P0 | 5 pts |
| EM-01-S03 | [Delivery Tracking](stories/EM-01-S03-delivery-tracking.md) | P0 | 3 pts |
| EM-01-S04 | [Email Preferences Foundation](stories/EM-01-S04-preferences-foundation.md) | P0 | 3 pts |

### EM-02: Organization Invitations (P0)

| Story | File | Priority | Effort |
|-------|------|----------|--------|
| EM-02-S01 | [Send Organization Invitation](stories/EM-02-S01-send-org-invitation.md) | P0 | 5 pts |
| EM-02-S02 | [Resend Invitation](stories/EM-02-S02-resend-invitation.md) | P0 | 2 pts |
| EM-02-S03 | [Cancel Invitation](stories/EM-02-S03-cancel-invitation.md) | P0 | 2 pts |
| EM-02-S04 | [View Pending Invitations](stories/EM-02-S04-view-pending-invitations.md) | P0 | 2 pts |
| EM-02-S05 | [Welcome Email](stories/EM-02-S05-welcome-email.md) | P0 | 2 pts |

### EM-03: Exercise Invitations (P0)

| Story | File | Priority | Effort |
|-------|------|----------|--------|
| EM-03-S01 | [Invite Existing Members](stories/EM-03-S01-invite-existing-members.md) | P0 | 5 pts |
| EM-03-S02 | [Invite External Participants](stories/EM-03-S02-invite-external-participants.md) | P0 | 5 pts |
| EM-03-S03 | [Exercise Details Updated](stories/EM-03-S03-exercise-details-updated.md) | P1 | 3 pts |

### EM-04: Authentication Emails (P0)

| Story | File | Priority | Effort |
|-------|------|----------|--------|
| EM-04-S01 | [Password Reset](stories/EM-04-S01-password-reset.md) | P0 | 3 pts |
| EM-04-S02 | [Password Changed Confirmation](stories/EM-04-S02-password-changed.md) | P0 | 2 pts |
| EM-04-S03 | [Account Verification](stories/EM-04-S03-account-verification.md) | P0 | 3 pts |
| EM-04-S04 | [New Device Login Alert](stories/EM-04-S04-new-device-alert.md) | P1 | 3 pts |

### EM-05: Inject Workflow Notifications (P1)

| Story | File | Priority | Effort |
|-------|------|----------|--------|
| EM-05-S01 | [Inject Submitted for Approval](stories/EM-05-S01-inject-submitted.md) | P1 | 2 pts |
| EM-05-S02 | [Inject Approved](stories/EM-05-S02-inject-approved.md) | P1 | 2 pts |
| EM-05-S03 | [Inject Rejected](stories/EM-05-S03-inject-rejected.md) | P1 | 3 pts |
| EM-05-S04 | [Inject Changes Requested](stories/EM-05-S04-inject-changes-requested.md) | P1 | 2 pts |

### EM-06: Assignment Notifications (P1)

| Story | File | Priority | Effort |
|-------|------|----------|--------|
| EM-06-S01 | [Inject Assignment](stories/EM-06-S01-inject-assignment.md) | P1 | 3 pts |
| EM-06-S02 | [Exercise Role Change](stories/EM-06-S02-role-change.md) | P1 | 2 pts |
| EM-06-S03 | [Evaluator Area Assignment](stories/EM-06-S03-evaluator-area-assignment.md) | P1 | 2 pts |

### EM-07: Exercise Status Notifications (P1)

| Story | File | Priority | Effort |
|-------|------|----------|--------|
| EM-07-S01 | [Exercise Published](stories/EM-07-S01-exercise-published.md) | P1 | 2 pts |
| EM-07-S02 | [Exercise Started](stories/EM-07-S02-exercise-started.md) | P1 | 2 pts |
| EM-07-S03 | [Exercise Completed](stories/EM-07-S03-exercise-completed.md) | P1 | 2 pts |
| EM-07-S04 | [Exercise Cancelled](stories/EM-07-S04-exercise-cancelled.md) | P1 | 2 pts |

### EM-08: Support & Feedback (P1)

| Story | File | Priority | Effort |
|-------|------|----------|--------|
| EM-08-S01 | [Bug Report Submission](stories/EM-08-S01-bug-report.md) | P1 | 3 pts |
| EM-08-S02 | [Feature Request Submission](stories/EM-08-S02-feature-request.md) | P1 | 2 pts |
| EM-08-S03 | [General Feedback](stories/EM-08-S03-general-feedback.md) | P1 | 2 pts |
| EM-08-S04 | [Support Ticket Acknowledgment](stories/EM-08-S04-ticket-acknowledgment.md) | P1 | 2 pts |

### EM-09: Scheduled Reminders (P2)

| Story | File | Priority | Effort |
|-------|------|----------|--------|
| EM-09-S01 | [Exercise Start Reminder](stories/EM-09-S01-exercise-start-reminder.md) | P2 | 3 pts |
| EM-09-S02 | [MSEL Review Deadline](stories/EM-09-S02-msel-review-deadline.md) | P2 | 3 pts |
| EM-09-S03 | [Observation Finalization](stories/EM-09-S03-observation-finalization.md) | P2 | 2 pts |

### EM-10: Digest & Summary Emails (P2)

| Story | File | Priority | Effort |
|-------|------|----------|--------|
| EM-10-S01 | [Daily Activity Digest](stories/EM-10-S01-daily-digest.md) | P2 | 5 pts |
| EM-10-S02 | [Exercise Director Daily Summary](stories/EM-10-S02-director-daily-summary.md) | P2 | 3 pts |
| EM-10-S03 | [Weekly Organization Report](stories/EM-10-S03-weekly-org-report.md) | P2 | 5 pts |

## Email Categories & Preferences

| Category | Mandatory | Examples |
|----------|-----------|----------|
| **Security** | Yes | Password reset, login alerts, account verification |
| **Invitations** | Yes | Org invite, exercise invite |
| **Assignments** | No (opt-out) | Inject assigned, role changed |
| **Workflow** | No (opt-out) | Inject approved/rejected |
| **Reminders** | No (opt-out) | Exercise starting, deadlines |
| **Digests** | No (opt-in) | Daily/weekly summaries |

## Technical Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Email Service                        │
├─────────────────────────────────────────────────────────┤
│  IEmailService (abstraction)                            │
│      │                                                  │
│      ├── AzureCommunicationEmailService (production)   │
│      │       └── Azure Communication Services           │
│      │                                                  │
│      └── LoggingEmailService (development/testing)     │
│              └── Console/File output                    │
├─────────────────────────────────────────────────────────┤
│  Template Engine                                        │
│      ├── HTML templates with Razor syntax              │
│      ├── Plain text fallback generation                │
│      └── Organization branding injection               │
├─────────────────────────────────────────────────────────┤
│  Delivery Tracking                                      │
│      ├── Application Insights telemetry                │
│      ├── EmailLog table for audit                      │
│      └── Bounce/complaint handling                     │
└─────────────────────────────────────────────────────────┘
```

## Cost Estimate

| Item | Unit Cost | Monthly Volume | Monthly Cost |
|------|-----------|----------------|--------------|
| Email sends | $0.00025/email | 1,250 | $0.31 |
| **Total** | | | **~$0.31/month** |

*Based on 25 users × 50 emails/user/month*

## Out of Scope

- Email marketing/bulk campaigns
- Email analytics dashboard (beyond delivery tracking)
- Custom email domain setup (uses ACS default)
- Rich media emails (video embedding)
- A/B testing for email content

## Dependencies

| Dependency | Required For | Status |
|------------|--------------|--------|
| Azure Communication Services | All email features | Not provisioned |
| Inject Approval Workflow | EM-05 (Workflow notifications) | Not started |
| Exercise Lifecycle | EM-07 (Status notifications) | Partial |
| Scheduled Jobs (Azure Functions) | EM-09, EM-10 (Reminders, digests) | Not started |

## Implementation Phases

| Phase | Features | Effort | Dependencies |
|-------|----------|--------|--------------|
| 1 | EM-01 (Infrastructure) | 16 pts | ACS provisioning |
| 2 | EM-04 (Auth emails) | 11 pts | Phase 1 |
| 3 | EM-02 (Org invitations) | 13 pts | Phase 1 |
| 4 | EM-03 (Exercise invitations) | 13 pts | Phase 1 |
| 5 | EM-05 (Workflow) | 9 pts | Phase 1 + Inject Approval |
| 6 | EM-06, EM-07 (Assignment, Status) | 15 pts | Phase 1 + Exercise Lifecycle |
| 7 | EM-08 (Support) | 9 pts | Phase 1 |
| 8 | EM-09, EM-10 (Reminders, Digests) | 21 pts | Phase 1 + Scheduled Jobs |

**Total Effort: ~107 story points**

## Risks & Assumptions

| Risk/Assumption | Mitigation/Validation |
|-----------------|----------------------|
| Email deliverability issues | Use ACS reputation, monitor bounces |
| Users ignore emails | Keep emails focused, actionable |
| Template maintenance burden | Use shared components, limit variations |
| Cost overruns with growth | Monitor usage, set alerts |
| Spam folder placement | Follow email best practices, test |

---

*Document version: 1.0*  
*Last updated: 2026-02-06*
