# Story: EM-03-S03 - Exercise Details Updated Notification

**As an** Exercise Participant,  
**I want** to be notified when exercise details change,  
**So that** I'm aware of date, time, or location updates that affect my participation.

## Context

Exercise logistics often change during planning. Participants need to know when significant details like dates, times, or locations are modified so they can adjust their schedules accordingly.

## Acceptance Criteria

### Trigger Conditions

- [ ] **Given** exercise date/time changes, **when** saved, **then** notification is queued
- [ ] **Given** exercise location changes, **when** saved, **then** notification is queued
- [ ] **Given** exercise is cancelled, **when** saved, **then** cancellation notification sent (see EM-07-S04)
- [ ] **Given** minor changes (description, scenario), **when** saved, **then** NO notification sent

### Notification Content

- [ ] **Given** update notification, **when** received, **then** clearly states what changed
- [ ] **Given** date change, **when** email sent, **then** shows old date → new date
- [ ] **Given** location change, **when** email sent, **then** shows old location → new location
- [ ] **Given** multiple changes, **when** email sent, **then** all changes listed in single email

### Timing

- [ ] **Given** changes made, **when** notification queued, **then** sent within 5 minutes (batched)
- [ ] **Given** multiple changes in 5 minutes, **when** sending, **then** combined into single email
- [ ] **Given** Exercise Director makes change, **when** they're a participant, **then** they don't receive notification

### Preferences

- [ ] **Given** user has "Reminders" disabled, **when** update occurs, **then** they still receive update (mandatory)
- [ ] **Given** exercise update email, **when** categorized, **then** it's in "Invitations" category (mandatory)

## Out of Scope

- In-app notifications (separate feature)
- Calendar update attachments
- Change approval workflow

## Dependencies

- EM-01-S01: ACS Email Configuration
- EM-03-S01: Invite Existing Members (participant list)

## Technical Notes

### Change Detection

```csharp
public class ExerciseChangeDetector
{
    public ExerciseChanges DetectChanges(Exercise before, Exercise after)
    {
        var changes = new ExerciseChanges();
        
        if (before.StartDate != after.StartDate)
            changes.Add(ChangeType.StartDate, before.StartDate, after.StartDate);
        
        if (before.EndDate != after.EndDate)
            changes.Add(ChangeType.EndDate, before.EndDate, after.EndDate);
            
        if (before.Location != after.Location)
            changes.Add(ChangeType.Location, before.Location, after.Location);
            
        return changes;
    }
}
```

## UI/UX Notes

### Email Preview

```
Subject: Exercise Update: Operation Thunderstorm

---

[Organization Logo]

Exercise Details Updated

Hi Jane,

The details for Operation Thunderstorm have been updated:

📅 DATE CHANGED
   Was: March 15, 2026
   Now: March 22, 2026

📍 LOCATION CHANGED  
   Was: County EOC
   Now: City Fire Station #1

Your role (Controller) remains the same.

        [View Updated Details]

Questions? Contact Exercise Director John Smith.

---

Cadence - Exercise Management Platform
```

## Domain Terms

| Term | Definition |
|------|------------|
| Exercise Update | Change to significant exercise logistics (date, time, location) |
| Change Notification | Email alerting participants to exercise modifications |

## Effort Estimate

**3 story points** - Change detection, batching, template

---

*Feature: EM-03 Exercise Invitations*  
*Priority: P1*
