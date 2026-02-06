# Story: EM-05-S02 - Inject Approved

**As an** Inject Creator,  
**I want** to be notified when my inject is approved,  
**So that** I know it's finalized and ready for the exercise.

## Context

Approval confirmation provides closure to the submission process and allows creators to track which injects are ready for conduct.

## Acceptance Criteria

- [ ] **Given** inject is approved, **when** approval saved, **then** creator receives notification email
- [ ] **Given** notification email, **when** received, **then** shows inject number and title
- [ ] **Given** notification email, **when** received, **then** shows who approved and when
- [ ] **Given** notification email, **when** received, **then** includes link to view inject
- [ ] **Given** user has "Workflow" emails disabled, **when** approved, **then** they don't receive notification

## Out of Scope

- Approval with modifications (separate story)

## Dependencies

- EM-01-S01: ACS Email Configuration
- Inject Approval Workflow feature

## UI/UX Notes

### Email Preview

```
Subject: ✓ Inject approved: [#47] Hospital Surge

---

[Organization Logo]

Inject Approved

Good news! Your inject has been approved.

INJECT #47
━━━━━━━━━━
Title: Hospital reports patient surge
Approved by: John Director
Approved: February 6, 2026 at 5:00 PM

        [View Inject]

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Notification trigger, template

---

*Feature: EM-05 Inject Workflow Notifications*  
*Priority: P1*
