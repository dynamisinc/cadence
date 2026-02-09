# Story: EM-07-S02 - Exercise Started Notification

**As an** Exercise Participant,  
**I want** to be notified when the exercise officially starts,  
**So that** I know conduct has begun even if I'm not at the main location.

## Context

Exercise start notifications signal that the exercise clock has started. This is particularly useful for distributed participants who may not hear in-person announcements.

## Acceptance Criteria

- [ ] **Given** exercise status changes to Active, **when** clock starts, **then** all participants receive email
- [ ] **Given** notification email, **when** received, **then** shows scenario start time (if different from real time)
- [ ] **Given** notification email, **when** received, **then** shows "Exercise is now ACTIVE"
- [ ] **Given** notification email, **when** received, **then** includes quick link to MSEL/observation entry
- [ ] **Given** user has "Reminders" emails disabled, **when** started, **then** they still receive notification

## Out of Scope

- Real-time countdown
- Start delay notifications

## Dependencies

- EM-01-S01: ACS Email Configuration
- Exercise clock functionality

## UI/UX Notes

### Email Preview

```
Subject: 🟢 Exercise ACTIVE: Operation Thunderstorm

---

[Organization Logo]

Exercise Now Active

Operation Thunderstorm is now in progress.

EXERCISE STATUS: ACTIVE
━━━━━━━━━━━━━━━━━━━━━━
Started: 8:00 AM EST
Scenario Time: 6:00 AM (simulated)
Your Role: Controller

        [Open Exercise]

Good luck!

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Status change trigger, template

---

*Feature: EM-07 Exercise Status Notifications*  
*Priority: P1*
