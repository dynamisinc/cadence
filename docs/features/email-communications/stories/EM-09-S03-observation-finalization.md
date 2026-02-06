# Story: EM-09-S03 - Observation Finalization Reminder

**As an** Exercise Director,  
**I want** evaluators to receive reminders to finalize their observations,  
**So that** AAR can proceed on schedule.

## Context

After exercise completion, evaluators need to review and finalize their observations. This reminder is sent to evaluators with draft observations.

## Acceptance Criteria

- [ ] **Given** exercise completed, **when** 48 hours pass with draft observations, **then** evaluators receive reminder
- [ ] **Given** reminder email, **when** received, **then** shows count of draft observations
- [ ] **Given** reminder email, **when** received, **then** shows finalization deadline if set
- [ ] **Given** reminder email, **when** received, **then** includes "Review Observations" link
- [ ] **Given** all observations finalized, **when** job runs, **then** no reminder sent
- [ ] **Given** user has "Reminders" disabled, **when** job runs, **then** no reminder

## Out of Scope

- Auto-finalization
- Multiple reminders

## Dependencies

- EM-01-S01: ACS Email Configuration
- Observation functionality
- Scheduled job infrastructure

## UI/UX Notes

### Email Preview

```
Subject: Please finalize your observations: Operation Thunderstorm

---

[Organization Logo]

Observation Finalization Reminder

You have draft observations from Operation Thunderstorm 
that need to be finalized for the After-Action Report.

YOUR DRAFT OBSERVATIONS
━━━━━━━━━━━━━━━━━━━━━━
12 observations pending review

Deadline: March 18, 2026 (2 days)

        [Review & Finalize]

Please review your observations, add any missing details,
and mark them as final.

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**2 story points** - Query draft observations, simple reminder

---

*Feature: EM-09 Scheduled Reminders*  
*Priority: P2*
