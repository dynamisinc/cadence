# Story: EM-10-S02 - Exercise Director Daily Summary

**As an** Exercise Director,
**I want** to receive a daily summary of my exercise's status,
**So that** I can track planning progress and identify blockers.

## Context

Exercise Directors need visibility into overall exercise readiness. This summary provides metrics and highlights issues requiring attention. It is sent as part of the same daily timer function as the user activity digest (EM-10-S01), but uses a Director-specific template and content model.

Directors receive this summary for each active exercise they direct. It is gated by the `DailyDigest` email preference category — the same preference controls both user digests and Director summaries.

## Acceptance Criteria

### Eligibility

- [ ] **Given** a user is Exercise Director for an active exercise and has `DailyDigest` preference enabled, **when** the daily digest function runs, **then** they receive a Director-specific summary for that exercise
- [ ] **Given** a user directs multiple active exercises, **when** the function runs, **then** they receive one summary email per exercise (or a combined email with sections per exercise)
- [ ] **Given** a user has no active exercises (all completed/archived), **when** the function runs, **then** no Director summary is sent
- [ ] **Given** a user has `DailyDigest` preference disabled, **when** the function runs, **then** no Director summary is sent

### Content

- [ ] **Given** the Director summary email, **when** received, **then** it shows MSEL completion percentage (approved / total injects)
- [ ] **Given** the Director summary email, **when** received, **then** it shows pending approvals count
- [ ] **Given** the Director summary email, **when** received, **then** it shows participant confirmation status (invited vs confirmed)
- [ ] **Given** the Director summary email, **when** received, **then** it highlights blockers (approvals pending >3 days, unassigned injects)
- [ ] **Given** the Director summary email, **when** received, **then** it shows exercise countdown (days until scheduled start)
- [ ] **Given** the digest content model, **when** rendering, **then** use the existing `DirectorDailySummary` email template from `EmailTemplateRegistrar`

## Out of Scope

- Dashboard analytics
- Trend analysis (week-over-week comparisons)
- Configurable blocker thresholds

## Dependencies

- EM-10-S01: Daily Activity Digest (shares the timer function)
- EM-01-S01: ACS Email Configuration (implemented)
- `EmailPreferenceService` with `DailyDigest` category (implemented)
- `DirectorDailySummary` email template (implemented in `EmailTemplateRegistrar`)
- `ExerciseParticipant` and `ExerciseUser` entities for role resolution (implemented)

## Implementation

### Architecture

This story extends the `DailyDigestFunction` from EM-10-S01 rather than creating a separate timer. After processing user digests, the same function processes Director summaries.

```
DailyDigestFunction (Timer Trigger — shared with S01)
    │
    ├─ [S01] Process user activity digests (see EM-10-S01)
    │
    └─ [S02] Process Director summaries:
        ├─ Query users with ExerciseDirector role on active exercises
        │   └─ Filter by DailyDigest preference enabled
        │
        ├─ For each eligible Director + exercise pair:
        │   ├─ IDirectorSummaryService.GenerateSummaryAsync(exerciseId)
        │   │   ├─ Count injects by status (approved, pending, draft)
        │   │   ├─ Count participants by status (invited, confirmed)
        │   │   ├─ Find blockers (approvals >3 days, unassigned injects)
        │   │   └─ Calculate exercise countdown
        │   │
        │   ├─ IEmailTemplateRenderer.RenderAsync("DirectorDailySummary", model)
        │   │
        │   └─ IEmailService.SendAsync(email)
        │
        └─ IEmailLogService.LogEmailSentAsync(...)
```

### New Service

```csharp
// Cadence.Core/Features/Email/Services/IDirectorSummaryService.cs
public interface IDirectorSummaryService
{
    Task<List<DirectorExercisePair>> GetEligibleDirectorsAsync(CancellationToken ct = default);
    Task<DirectorSummaryModel> GenerateSummaryAsync(Guid exerciseId, CancellationToken ct = default);
}
```

### Metrics Queries

| Metric | Source | Query |
| ------ | ------ | ----- |
| MSEL completion % | `Inject` | Count by `ApprovalStatus` where `MselId` belongs to exercise |
| Pending approvals | `Inject` | Where `ApprovalStatus == PendingApproval` |
| Overdue approvals | `Inject` + `InjectStatusHistory` | Pending approval where last status change >3 days ago |
| Unassigned injects | `Inject` | Where `AssignedToUserId == null` |
| Participant status | `ExerciseParticipant` | Count by confirmation/invitation status |
| Exercise countdown | `Exercise` | `ScheduledStartDate - DateTime.UtcNow` |

### Shared Function Pattern

The `DailyDigestFunction` calls both services sequentially:

```csharp
[Function("DailyDigest")]
public async Task RunAsync(
    [TimerTrigger("0 0 6 * * *")] TimerInfo timer,
    FunctionContext context)
{
    // Phase 1: User activity digests (S01)
    await _dailyDigestService.ProcessUserDigestsAsync();

    // Phase 2: Director summaries (S02)
    await _directorSummaryService.ProcessDirectorSummariesAsync();
}
```

## UI/UX Notes

### Email Preview

```
Subject: Director Summary: Operation Thunderstorm - Feb 6

---

[Organization Logo]

Exercise Director Summary
Operation Thunderstorm | February 6, 2026

EXERCISE COUNTDOWN: 9 DAYS
━━━━━━━━━━━━━━━━━━━━━━━━━━

MSEL STATUS
━━━━━━━━━━━
Total Injects: 47
Approved: 38 (81%)
Pending Review: 6
In Draft: 3

ATTENTION NEEDED
• 2 injects pending approval >3 days
• 5 injects unassigned to controllers

PARTICIPANT STATUS
━━━━━━━━━━━━━━━━━━
Invited: 24 | Confirmed: 19 | Pending: 5

        [Open Exercise Dashboard]

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**3 story points** - Metrics aggregation queries, Director-specific content model (timer function infrastructure built in S01)

---

*Feature: EM-10 Digest & Summary Emails*
*Priority: P2*
