# Story: EM-06-S03 - Evaluator Area Assignment

**As an** Evaluator,  
**I want** to be notified when I'm assigned to evaluate specific areas,  
**So that** I know where to focus my observations during the exercise.

## Context

Evaluators may be assigned to specific functional areas, capabilities, or objectives. Assignment notifications help them prepare by reviewing relevant evaluation criteria.

## Acceptance Criteria

- [ ] **Given** Evaluator assigned to areas/capabilities, **when** assignment saved, **then** they receive email
- [ ] **Given** notification email, **when** received, **then** lists assigned areas/capabilities
- [ ] **Given** notification email, **when** received, **then** includes relevant objectives if defined
- [ ] **Given** notification email, **when** received, **then** includes "View Evaluation Guide" link
- [ ] **Given** user has "Assignments" emails disabled, **when** assigned, **then** no notification

## Out of Scope

- Evaluation criteria details in email
- Area reassignment notifications

## Dependencies

- EM-01-S01: ACS Email Configuration
- Evaluator assignment functionality
- Exercise Evaluation Guide (EEG) feature

## UI/UX Notes

### Email Preview

```
Subject: Your evaluation assignment: Operation Thunderstorm

---

[Organization Logo]

Evaluation Area Assignment

You've been assigned to evaluate the following areas 
in Operation Thunderstorm.

YOUR ASSIGNED AREAS
━━━━━━━━━━━━━━━━━━
• Emergency Operations Center (EOC) Management
• Communications / Public Information

KEY OBJECTIVES
• EOC activates within 30 minutes of notification
• JIS established and functional within 1 hour
• Initial press release issued within 2 hours

        [View Evaluation Guide]

Exercise: March 15, 2026 | County EOC

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Area assignment trigger, list formatting

---

*Feature: EM-06 Assignment Notifications*  
*Priority: P1*
