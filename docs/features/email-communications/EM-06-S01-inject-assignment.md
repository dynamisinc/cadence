# Story: EM-06-S01 - Inject Assignment Notification

**As a** Controller,  
**I want** to be notified when I'm assigned to deliver an inject,  
**So that** I can prepare and know my responsibilities.

## Context

Controllers need to know which injects they're responsible for delivering. Assignment notifications provide this visibility and link to inject details for preparation.

## Acceptance Criteria

- [ ] **Given** inject is assigned to Controller, **when** assignment saved, **then** Controller receives email
- [ ] **Given** notification email, **when** received, **then** shows inject number, title, and scheduled time
- [ ] **Given** notification email, **when** received, **then** shows delivery method and target
- [ ] **Given** notification email, **when** received, **then** includes "View Inject" button
- [ ] **Given** batch assignment (multiple injects), **when** assigned, **then** single email with list
- [ ] **Given** user has "Assignments" emails disabled, **when** assigned, **then** no notification

## Out of Scope

- Assignment acknowledgment
- Reassignment notifications

## Dependencies

- EM-01-S01: ACS Email Configuration
- Inject assignment functionality

## UI/UX Notes

### Email Preview

```
Subject: You've been assigned injects for Operation Thunderstorm

---

[Organization Logo]

Inject Assignment

You've been assigned to deliver injects for 
Operation Thunderstorm.

YOUR ASSIGNED INJECTS
━━━━━━━━━━━━━━━━━━━━
#47 | Hospital reports surge | 10:30 AM | Phone call
#52 | Media inquiry | 11:15 AM | In-person
#58 | Resource request | 12:00 PM | Radio

        [View Your Injects]

Exercise: March 15, 2026 | County EOC

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**3 story points** - Batch handling, list formatting

---

*Feature: EM-06 Assignment Notifications*  
*Priority: P1*
