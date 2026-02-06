# Story: EM-09-S01 - Exercise Start Reminder

**As an** Exercise Participant,  
**I want** to receive a reminder before the exercise starts,  
**So that** I can prepare and ensure I'm available.

## Context

Scheduled reminders sent 24 hours before exercise start help participants prepare. This requires a scheduled job to check for upcoming exercises.

## Acceptance Criteria

- [ ] **Given** exercise starts in 24 hours, **when** reminder job runs, **then** all participants receive email
- [ ] **Given** reminder email, **when** received, **then** shows exercise name, date, time, location
- [ ] **Given** reminder email, **when** received, **then** shows participant's assigned role
- [ ] **Given** reminder email, **when** received, **then** includes "View Exercise" link
- [ ] **Given** exercise already completed/cancelled, **when** job runs, **then** NO reminder sent
- [ ] **Given** user has "Reminders" emails disabled, **when** job runs, **then** they don't receive reminder

## Out of Scope

- Configurable reminder timing (always 24h)
- Multiple reminders (just one)
- Calendar invite attachment

## Dependencies

- EM-01-S01: ACS Email Configuration
- Scheduled job infrastructure (Azure Functions or Hosted Service)

## Technical Notes

### Scheduled Job

```csharp
// Runs daily at midnight UTC
[Function("SendExerciseReminders")]
public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo timer)
{
    var upcomingExercises = await _exerciseService
        .GetExercisesStartingBetweenAsync(
            DateTime.UtcNow.AddHours(23),
            DateTime.UtcNow.AddHours(25));
            
    foreach (var exercise in upcomingExercises)
    {
        await _emailService.SendExerciseReminderAsync(exercise);
    }
}
```

## UI/UX Notes

### Email Preview

```
Subject: Reminder: Operation Thunderstorm starts tomorrow

---

[Organization Logo]

Exercise Reminder

Operation Thunderstorm starts in 24 hours.

EXERCISE DETAILS
━━━━━━━━━━━━━━━━
Date: March 15, 2026
Time: 8:00 AM - 4:00 PM
Location: County Emergency Operations Center
Your Role: Controller

PREPARATION CHECKLIST
☐ Review your assigned injects
☐ Confirm you have app access
☐ Plan your travel to the venue

        [View Exercise]

Good luck tomorrow!

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**3 story points** - Scheduled job setup, timing logic

---

*Feature: EM-09 Scheduled Reminders*  
*Priority: P2*
