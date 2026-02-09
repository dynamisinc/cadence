# Story: EM-06-S02 - Exercise Role Change Notification

**As an** Exercise Participant,  
**I want** to be notified when my role in an exercise changes,  
**So that** I understand my updated responsibilities.

## Context

Role changes affect a participant's responsibilities and access during an exercise. Notification ensures they're aware of the change and can prepare accordingly.

## Acceptance Criteria

- [ ] **Given** participant's role is changed, **when** change saved, **then** they receive email
- [ ] **Given** notification email, **when** received, **then** shows old role → new role
- [ ] **Given** notification email, **when** received, **then** explains new role responsibilities
- [ ] **Given** notification email, **when** received, **then** shows who made the change
- [ ] **Given** user has "Assignments" emails disabled, **when** role changed, **then** no notification

## Out of Scope

- Role change request workflow
- Permission comparison

## Dependencies

- EM-01-S01: ACS Email Configuration
- Exercise participant management

## UI/UX Notes

### Email Preview

```
Subject: Your role changed in Operation Thunderstorm

---

[Organization Logo]

Role Change Notification

Your role in Operation Thunderstorm has been updated.

ROLE CHANGE
━━━━━━━━━━━
From: Observer
To: Evaluator

Changed by: John Director
Changed: February 6, 2026

ABOUT YOUR NEW ROLE
As an Evaluator, you'll observe player actions and 
capture observations using the P/S/M/U rating system.

        [View Exercise]

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Role change trigger, template

---

*Feature: EM-06 Assignment Notifications*  
*Priority: P1*
