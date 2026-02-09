# Story: EM-09-S02 - MSEL Review Deadline Reminder

**As an** Exercise Director,  
**I want** participants to receive reminders about pending MSEL reviews,  
**So that** injects are approved before the exercise.

## Context

When inject approval workflow is enabled, pending injects need timely review. This reminder targets approvers with injects awaiting their action.

## Acceptance Criteria

- [ ] **Given** inject pending approval >48 hours, **when** reminder job runs, **then** approver receives reminder
- [ ] **Given** reminder email, **when** received, **then** lists pending injects count
- [ ] **Given** reminder email, **when** received, **then** shows exercise and deadline context
- [ ] **Given** reminder email, **when** received, **then** includes "Review Pending" link
- [ ] **Given** user has "Reminders" disabled, **when** job runs, **then** no reminder

## Out of Scope

- Escalation to Exercise Director
- Daily reminder (just one at 48h mark)

## Dependencies

- EM-01-S01: ACS Email Configuration
- Inject Approval Workflow feature
- Scheduled job infrastructure

## UI/UX Notes

### Email Preview

```
Subject: 3 injects awaiting your review: Operation Thunderstorm

---

[Organization Logo]

MSEL Review Reminder

You have injects awaiting approval for Operation Thunderstorm.

PENDING YOUR REVIEW
━━━━━━━━━━━━━━━━━━
• #47 - Hospital reports surge (submitted 3 days ago)
• #52 - Media inquiry (submitted 2 days ago)  
• #58 - Resource request (submitted 2 days ago)

Exercise date: March 15, 2026 (9 days away)

        [Review Pending Injects]

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**3 story points** - Query pending injects, aggregate by approver

---

*Feature: EM-09 Scheduled Reminders*  
*Priority: P2*
