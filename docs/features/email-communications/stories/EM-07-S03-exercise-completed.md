# Story: EM-07-S03 - Exercise Completed Notification

**As an** Exercise Participant,  
**I want** to be notified when the exercise completes,  
**So that** I know conduct has ended and can transition to post-exercise activities.

## Context

Exercise completion signals the end of conduct phase. The notification provides next steps for after-action activities.

## Acceptance Criteria

- [ ] **Given** exercise status changes to Completed, **when** saved, **then** all participants receive email
- [ ] **Given** notification email, **when** received, **then** shows exercise completion time
- [ ] **Given** notification email, **when** received, **then** thanks participants
- [ ] **Given** notification email, **when** received, **then** mentions hot wash/AAR if scheduled
- [ ] **Given** notification email, **when** received, **then** reminds evaluators to finalize observations
- [ ] **Given** user has "Reminders" emails disabled, **when** completed, **then** they still receive notification

## Out of Scope

- AAR scheduling
- Exercise metrics summary

## Dependencies

- EM-01-S01: ACS Email Configuration
- Exercise lifecycle (Completed status)

## UI/UX Notes

### Email Preview

```
Subject: ✅ Exercise complete: Operation Thunderstorm

---

[Organization Logo]

Exercise Complete

Operation Thunderstorm has concluded. Thank you for 
your participation!

SUMMARY
━━━━━━━
Duration: 8 hours
Completed: 4:00 PM EST

NEXT STEPS
• Hot Wash: Today at 4:30 PM in the EOC
• Evaluators: Please finalize observations by March 18
• AAR: Scheduled for March 22

        [View Exercise Summary]

Thank you for making this exercise a success!

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Status change trigger, template

---

*Feature: EM-07 Exercise Status Notifications*  
*Priority: P1*
