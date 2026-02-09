# Story: EM-10-S01 - Daily Activity Digest

**As a** User,
**I want** to receive a daily summary of activity,
**So that** I can stay informed without constant notifications.

## Context

Daily digests consolidate activity into a single email, reducing notification fatigue while keeping users informed about relevant changes. Users opt in via the `DailyDigest` email preference category (default: OFF).

## Acceptance Criteria

### Digest Generation

- [ ] **Given** the daily digest timer function runs, **when** a user has `DailyDigest` preference enabled and activity exists from the past 24h, **then** a digest email is sent to that user
- [ ] **Given** no activity in the past 24h for a user, **when** the function runs, **then** NO digest is sent (don't send empty emails)
- [ ] **Given** a user has `DailyDigest` preference disabled (the default), **when** the function runs, **then** no email is sent

### Content Sections

- [ ] **Given** exercises the user participates in, **when** activity occurred, **then** show an exercise section
- [ ] **Given** injects assigned to the user, **when** status changed (via `InjectStatusHistory`), **then** include in digest
- [ ] **Given** injects the user submitted, **when** approval status changed, **then** include in digest
- [ ] **Given** observations on exercises the user participates in, **when** new observations were added, **then** include in digest

### Formatting

- [ ] **Given** multiple exercises with activity, **when** formatting, **then** group activity by exercise
- [ ] **Given** activity items, **when** formatting, **then** show summary counts and highlights
- [ ] **Given** the digest content model, **when** rendering, **then** use the existing `DailyDigest` email template from `EmailTemplateRegistrar`

## Out of Scope

- Real-time activity feed
- Customizable digest timing (per-user timezone scheduling)
- Digest for non-active periods

## Dependencies

- EM-01-S01: ACS Email Configuration (implemented)
- `EmailPreferenceService` with `DailyDigest` category (implemented)
- `DailyDigest` email template (implemented in `EmailTemplateRegistrar`)
- `InjectStatusHistory` entity for tracking inject changes (implemented)

## Implementation

### Architecture

This story introduces the first **timer-triggered Azure Function** in the `Cadence.Functions` project. The architecture follows the established pattern: Azure Functions for background jobs, `Cadence.Core` for business logic.

```
DailyDigestFunction (Timer Trigger, Cadence.Functions)
    │
    ├─ Query active users with DailyDigest preference enabled
    │   └─ IEmailPreferenceService.CanSendAsync(userId, DailyDigest)
    │
    ├─ For each eligible user:
    │   ├─ IDailyDigestService.GenerateDigestAsync(userId, since)
    │   │   ├─ Query InjectStatusHistory (status changes in past 24h)
    │   │   ├─ Query ExerciseParticipant (new assignments)
    │   │   ├─ Query Observation (new observations)
    │   │   └─ Group by Exercise, build UserDigestModel
    │   │
    │   ├─ If no activity → skip (no empty digests)
    │   │
    │   ├─ IEmailTemplateRenderer.RenderAsync("DailyDigest", model)
    │   │
    │   └─ IEmailService.SendAsync(email)
    │
    └─ IEmailLogService.LogEmailSentAsync(...)
```

### Timer Function

```csharp
// Cadence.Functions/Functions/DailyDigestFunction.cs
[Function("DailyDigest")]
public async Task RunAsync(
    [TimerTrigger("0 0 6 * * *")] TimerInfo timer,  // 6:00 AM UTC daily
    FunctionContext context)
{
    _logger.LogInformation("Daily digest job started at {Time}", DateTime.UtcNow);

    var eligibleUsers = await _digestService.GetEligibleUsersAsync(EmailCategory.DailyDigest);

    foreach (var user in eligibleUsers)
    {
        var digest = await _digestService.GenerateDigestAsync(user.Id, DateTime.UtcNow.AddHours(-24));
        if (digest == null) continue; // No activity

        var html = await _templateRenderer.RenderAsync("DailyDigest", digest.ToTemplateModel());
        await _emailService.SendAsync(user.Email, digest.Subject, html);
    }

    _logger.LogInformation("Daily digest job completed. Processed {Count} users", eligibleUsers.Count);
}
```

### New Service

```csharp
// Cadence.Core/Features/Email/Services/IDailyDigestService.cs
public interface IDailyDigestService
{
    Task<List<ApplicationUser>> GetEligibleUsersAsync(EmailCategory category, CancellationToken ct = default);
    Task<UserDigestModel?> GenerateDigestAsync(string userId, DateTime since, CancellationToken ct = default);
}
```

### Activity Data Sources

| Data Source | What It Provides |
| ----------- | ---------------- |
| `InjectStatusHistory` | Inject approvals, rejections, status changes with `ChangedAt` timestamp |
| `ExerciseParticipant` | New participant assignments with `AssignedAt` timestamp |
| `Observation` | New evaluator observations with `CreatedAt` timestamp |
| `Inject` | New injects created with `CreatedAt` timestamp |

No dedicated activity log table is needed — the existing entities already track timestamps sufficient for digest aggregation.

### Timezone Note

The initial implementation uses a single UTC schedule (`6:00 AM UTC`). Per-user timezone scheduling is explicitly out of scope. If needed later, the function can be changed to run hourly and filter users by local time zone offset.

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
3 injects approved (yours: #47, #52)
Exercise starts in 9 days
2 new participants added

TRAINING EXERCISE #4
━━━━━━━━━━━━━━━━━━━━
1 inject needs revision (#12)
   "Please clarify timing relative to Phase 2"

        [Open Cadence]

---

Manage digest preferences: [link]

Cadence - Exercise Management Platform
```

## Effort Estimate

**5 story points** - Activity aggregation across multiple tables, first timer function setup, conditional sending

---

*Feature: EM-10 Digest & Summary Emails*
*Priority: P2*
