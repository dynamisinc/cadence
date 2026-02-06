# Story: EM-10-S01 - Daily Activity Digest

**As a** User,  
**I want** to receive a daily summary of activity,  
**So that** I can stay informed without constant notifications.

## Context

Daily digests consolidate activity into a single email, reducing notification fatigue while keeping users informed about relevant changes.

## Acceptance Criteria

### Digest Generation

- [ ] **Given** scheduled job runs (6 AM user timezone), **when** activity exists from past 24h, **then** digest email sent
- [ ] **Given** no activity in past 24h, **when** job runs, **then** NO digest sent (don't send empty emails)
- [ ] **Given** user has daily digest disabled, **when** job runs, **then** no email

### Content Sections

- [ ] **Given** exercises user participates in, **when** activity occurred, **then** show exercise section
- [ ] **Given** injects assigned to user, **when** status changed, **then** include in digest
- [ ] **Given** injects user submitted, **when** approval status changed, **then** include in digest
- [ ] **Given** observations, **when** feedback received, **then** include in digest

### Formatting

- [ ] **Given** multiple exercises, **when** formatting, **then** group activity by exercise
- [ ] **Given** activity items, **when** formatting, **then** show summary counts and highlights

## Out of Scope

- Real-time activity feed
- Customizable digest timing
- Digest for non-active periods

## Dependencies

- EM-01-S01: ACS Email Configuration
- Scheduled job infrastructure
- Activity tracking

## Technical Notes

### Digest Query

```csharp
public class DailyDigestService
{
    public async Task<UserDigest?> GenerateDigestAsync(Guid userId)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        
        var exerciseActivity = await _activityService
            .GetExerciseActivityAsync(userId, since);
            
        if (!exerciseActivity.Any())
            return null; // No digest if no activity
            
        return new UserDigest
        {
            UserId = userId,
            PeriodStart = since,
            PeriodEnd = DateTime.UtcNow,
            Exercises = exerciseActivity
        };
    }
}
```

## UI/UX Notes

### Email Preview

```
Subject: Your Cadence daily digest - February 6

---

[Cadence Logo]

Daily Activity Summary
February 6, 2026

OPERATION THUNDERSTORM
━━━━━━━━━━━━━━━━━━━━━━
📝 3 injects approved (yours: #47, #52)
⏰ Exercise starts in 9 days
👥 2 new participants added

TRAINING EXERCISE #4
━━━━━━━━━━━━━━━━━━━━
📝 1 inject needs revision (#12)
   "Please clarify timing relative to Phase 2"

        [Open Cadence]

---

Manage digest preferences: [link]

Cadence - Exercise Management Platform
```

## Effort Estimate

**5 story points** - Activity aggregation, timezone handling, conditional sending

---

*Feature: EM-10 Digest & Summary Emails*  
*Priority: P2*
