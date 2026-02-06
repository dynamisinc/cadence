# Story: EM-07-S01 - Exercise Published Notification

**As an** Exercise Participant,  
**I want** to be notified when an exercise I'm part of is published,  
**So that** I know the exercise is finalized and can begin preparation.

## Context

Exercise publication signals that planning is complete and the exercise is ready for conduct. Participants should begin final preparations.

## Acceptance Criteria

- [ ] **Given** exercise status changes to Published, **when** saved, **then** all participants receive email
- [ ] **Given** notification email, **when** received, **then** shows exercise name, date, and location
- [ ] **Given** notification email, **when** received, **then** shows participant's assigned role
- [ ] **Given** notification email, **when** received, **then** includes "View Exercise" button
- [ ] **Given** user has "Reminders" emails disabled, **when** published, **then** they still receive notification (mandatory)

## Out of Scope

- Preparation checklist
- RSVP confirmation

## Dependencies

- EM-01-S01: ACS Email Configuration
- Exercise lifecycle (Published status)

## UI/UX Notes

### Email Preview

```
Subject: Exercise published: Operation Thunderstorm

---

[Organization Logo]

Exercise Ready for Conduct

Operation Thunderstorm has been published and is 
ready for conduct.

EXERCISE DETAILS
━━━━━━━━━━━━━━━━
Date: March 15, 2026 | 8:00 AM - 4:00 PM
Location: County Emergency Operations Center
Your Role: Controller

        [View Exercise Details]

Please review your assignments and prepare for 
exercise day.

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Status change trigger, template

---

*Feature: EM-07 Exercise Status Notifications*  
*Priority: P1*
