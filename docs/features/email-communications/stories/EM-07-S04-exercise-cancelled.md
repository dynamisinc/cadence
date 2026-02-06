# Story: EM-07-S04 - Exercise Cancelled Notification

**As an** Exercise Participant,  
**I want** to be notified if an exercise is cancelled,  
**So that** I don't show up to a cancelled event.

## Context

Exercise cancellation is a critical notification that must reach all participants promptly. The email should be clearly marked as cancellation and provide any available context.

## Acceptance Criteria

- [ ] **Given** exercise status changes to Cancelled, **when** saved, **then** all participants receive email immediately
- [ ] **Given** notification email, **when** received, **then** subject clearly indicates "CANCELLED"
- [ ] **Given** cancellation includes reason, **when** email sent, **then** reason is displayed
- [ ] **Given** notification email, **when** received, **then** shows contact for questions
- [ ] **Given** this notification type, **when** checking preferences, **then** it's mandatory (cannot disable)

## Out of Scope

- Automatic rescheduling
- Cancellation approval workflow

## Dependencies

- EM-01-S01: ACS Email Configuration
- Exercise lifecycle (Cancelled status)

## UI/UX Notes

### Email Preview

```
Subject: ❌ CANCELLED: Operation Thunderstorm

---

[Organization Logo]

Exercise Cancelled

Operation Thunderstorm scheduled for March 15, 2026 
has been CANCELLED.

REASON
━━━━━━
Severe weather forecast for exercise area. Safety of 
participants is our priority.

We apologize for any inconvenience. The exercise may 
be rescheduled—watch for updates.

Questions? Contact Exercise Director John Smith at
john.smith@example.com

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Status change trigger, urgent styling

---

*Feature: EM-07 Exercise Status Notifications*  
*Priority: P1*
