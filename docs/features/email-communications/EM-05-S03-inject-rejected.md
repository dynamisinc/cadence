# Story: EM-05-S03 - Inject Rejected

**As an** Inject Creator,  
**I want** to be notified when my inject is rejected with feedback,  
**So that** I understand what needs to change and can revise accordingly.

## Context

Rejection notifications must include clear feedback explaining why the inject wasn't approved and what changes are needed. This prevents confusion and enables efficient revision.

## Acceptance Criteria

- [ ] **Given** inject is rejected, **when** rejection saved, **then** creator receives notification email
- [ ] **Given** rejection requires feedback, **when** approver rejects, **then** feedback field is required
- [ ] **Given** notification email, **when** received, **then** shows inject number and title
- [ ] **Given** notification email, **when** received, **then** shows who rejected and when
- [ ] **Given** notification email, **when** received, **then** prominently displays rejection feedback
- [ ] **Given** notification email, **when** received, **then** includes "Edit Inject" button to make changes
- [ ] **Given** user has "Workflow" emails disabled, **when** rejected, **then** they still receive notification (important feedback)

## Out of Scope

- Threaded discussion on rejection
- Reject without feedback

## Dependencies

- EM-01-S01: ACS Email Configuration
- Inject Approval Workflow feature

## UI/UX Notes

### Email Preview

```
Subject: Inject needs revision: [#47] Hospital Surge

---

[Organization Logo]

Inject Requires Changes

Your inject was reviewed but needs revisions before approval.

INJECT #47
━━━━━━━━━━
Title: Hospital reports patient surge
Reviewed by: John Director
Reviewed: February 6, 2026 at 5:15 PM

FEEDBACK
━━━━━━━━
"The inject timing conflicts with inject #42. Please 
adjust the scheduled time to at least 15 minutes after 
the facility evacuation completes. Also, consider 
adding specific patient count expectations."

        [Edit Inject]

Questions? Reply to this email or contact John Director.

---

Cadence - Exercise Management Platform
```

## Domain Terms

| Term | Definition |
|------|------------|
| Rejection Feedback | Explanation from approver about required changes |

## Effort Estimate

**3 story points** - Required feedback validation, prominent display

---

*Feature: EM-05 Inject Workflow Notifications*  
*Priority: P1*
