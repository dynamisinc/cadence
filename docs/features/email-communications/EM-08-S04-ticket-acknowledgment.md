# Story: EM-08-S04 - Support Ticket Acknowledgment

**As a** User,  
**I want** to receive acknowledgment when I submit a support request,  
**So that** I know my submission was received and what to expect.

## Context

Acknowledgment emails confirm receipt and set expectations for response time.

## Acceptance Criteria

- [ ] **Given** any support form submitted (bug, feature, feedback), **when** saved, **then** user receives acknowledgment email
- [ ] **Given** acknowledgment email, **when** received, **then** includes reference number
- [ ] **Given** acknowledgment email, **when** received, **then** includes copy of submission
- [ ] **Given** acknowledgment email, **when** received, **then** sets response time expectation
- [ ] **Given** acknowledgment email, **when** received, **then** explains how to follow up

## Out of Scope

- Ticket status updates
- Ticket portal

## Dependencies

- EM-01-S01: ACS Email Configuration
- EM-08-S01 through S03

## UI/UX Notes

### Email Preview

```
Subject: We received your feedback [#FB-2026-0215]

---

[Cadence Logo]

Thanks for Reaching Out

We've received your submission and will review it shortly.

REFERENCE: #FB-2026-0215
━━━━━━━━━━━━━━━━━━━━━━━
Type: Bug Report
Title: Inject status not updating
Submitted: February 6, 2026 at 5:30 PM

YOUR MESSAGE
━━━━━━━━━━━━
When I fire an inject, the status shows "Fired" but 
reverts to "Pending" after page refresh...

WHAT'S NEXT
We typically respond within 1-2 business days. For 
urgent issues during an exercise, please contact your
Exercise Director.

To follow up on this ticket, reply to this email.

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Triggered on submission, template

---

*Feature: EM-08 Support & Feedback*  
*Priority: P1*
