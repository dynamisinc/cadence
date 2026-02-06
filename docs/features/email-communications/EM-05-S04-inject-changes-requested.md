# Story: EM-05-S04 - Inject Changes Requested

**As an** Inject Creator,  
**I want** to be notified when my inject needs minor changes,  
**So that** I can make quick adjustments without full rejection.

## Context

"Changes Requested" is a softer alternative to rejection for minor issues. The inject remains in workflow but awaits revision—similar to "Request Changes" in code review.

## Acceptance Criteria

- [ ] **Given** approver requests changes, **when** saved, **then** creator receives notification
- [ ] **Given** notification email, **when** received, **then** shows change request details
- [ ] **Given** notification email, **when** received, **then** clearly differs from rejection (tone/color)
- [ ] **Given** notification email, **when** received, **then** includes "Make Changes" button
- [ ] **Given** changes made, **when** resubmitted, **then** approver is notified (via EM-05-S01)

## Out of Scope

- Approval with conditions
- Auto-approve after minor changes

## Dependencies

- EM-01-S01: ACS Email Configuration
- Inject Approval Workflow feature

## UI/UX Notes

### Email Preview

```
Subject: Changes requested: [#47] Hospital Surge

---

[Organization Logo]

Minor Changes Requested

Your inject is almost ready—just a few tweaks needed.

INJECT #47
━━━━━━━━━━
Title: Hospital reports patient surge
Requested by: John Director

REQUESTED CHANGES
━━━━━━━━━━━━━━━━
"Please add the specific hospital name and clarify 
whether this is the initial call or a follow-up."

        [Make Changes]

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Similar to rejection, different messaging

---

*Feature: EM-05 Inject Workflow Notifications*  
*Priority: P1*
