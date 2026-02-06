# Story: EM-05-S01 - Inject Submitted for Approval

**As an** Inject Approver,  
**I want** to be notified when an inject needs my approval,  
**So that** I can review and respond promptly to keep planning on track.

## Context

When inject approval workflow is enabled, injects must be reviewed before being finalized. Approvers need timely notification to prevent bottlenecks in exercise planning.

## Acceptance Criteria

- [ ] **Given** inject submitted for approval, **when** workflow triggered, **then** approver receives email
- [ ] **Given** notification email, **when** received, **then** shows inject number, title, and submitter
- [ ] **Given** notification email, **when** received, **then** includes brief inject description/content
- [ ] **Given** notification email, **when** received, **then** includes "Review Inject" button linking to approve page
- [ ] **Given** multiple approvers configured, **when** submitted, **then** all approvers receive notification
- [ ] **Given** user has "Workflow" emails disabled, **when** submitted, **then** they don't receive notification

## Out of Scope

- In-app approval without visiting page
- Reply-to-approve functionality

## Dependencies

- EM-01-S01: ACS Email Configuration
- Inject Approval Workflow feature

## UI/UX Notes

### Email Preview

```
Subject: Inject pending approval: [#47] Hospital Surge

---

[Organization Logo]

Inject Awaiting Your Approval

Inject #47 has been submitted for approval.

INJECT DETAILS
━━━━━━━━━━━━━━
Title: Hospital reports patient surge
Submitted by: Jane Controller
Exercise: Operation Thunderstorm
Phase: Initial Response

Preview:
"Mercy Hospital EOC calls to report incoming surge of 
50+ patients from incident scene. Request additional 
ambulances and staff..."

        [Review & Approve]

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Notification trigger, template

---

*Feature: EM-05 Inject Workflow Notifications*  
*Priority: P1*
