# Story: EM-10-S03 - Weekly Organization Report

**As an** OrgAdmin,
**I want** to receive a weekly summary of organization activity,
**So that** I can track usage and identify trends.

## Context

Organization administrators benefit from high-level visibility into how their team uses Cadence. Weekly reports provide metrics without overwhelming detail.

This story introduces a **second timer-triggered Azure Function** (`WeeklyOrgReportFunction`) separate from the daily digest function. It runs once per week and is gated by the `WeeklyDigest` email preference category (default: OFF).

Unlike the daily digest (which skips users with no activity), the weekly org report is always sent to opted-in OrgAdmins — even with zero activity — to confirm the system is working and the organization had a quiet week.

## Acceptance Criteria

### Eligibility

- [ ] **Given** a user is OrgAdmin and has `WeeklyDigest` preference enabled, **when** the weekly function runs (Monday 8 AM UTC), **then** they receive an org report
- [ ] **Given** a user is OrgAdmin but has `WeeklyDigest` preference disabled (the default), **when** the function runs, **then** no report is sent
- [ ] **Given** a user is OrgAdmin of multiple organizations, **when** the function runs, **then** they receive one report per organization
- [ ] **Given** no activity in the past week, **when** the function runs, **then** the report is still sent with zero counts (confirms system working)

### Content

- [ ] **Given** the report email, **when** received, **then** it shows active exercises count for the organization
- [ ] **Given** the report email, **when** received, **then** it shows total injects created and delivered this week
- [ ] **Given** the report email, **when** received, **then** it shows active users count (users who logged in or performed actions)
- [ ] **Given** the report email, **when** received, **then** it shows upcoming exercises with dates and countdowns
- [ ] **Given** the report email, **when** received, **then** it shows team updates (new members joined, pending invitations)
- [ ] **Given** the report content model, **when** rendering, **then** use the existing `WeeklyOrgReport` email template from `EmailTemplateRegistrar`

## Out of Scope

- Exportable reports (PDF, CSV)
- Historical comparison (week-over-week trends)
- Usage billing details
- Customizable report day/time

## Dependencies

- EM-01-S01: ACS Email Configuration (implemented)
- `EmailPreferenceService` with `WeeklyDigest` category (implemented)
- `WeeklyOrgReport` email template (implemented in `EmailTemplateRegistrar`)
- Organization membership and exercise entities (implemented)

## Implementation

### Architecture

This is a **separate timer function** from the daily digest because it runs on a different schedule (weekly vs daily) and has a different scope (organization-wide vs user-specific).

```
WeeklyOrgReportFunction (Timer Trigger, Cadence.Functions)
    │
    ├─ Query all OrgAdmins with WeeklyDigest preference enabled
    │   └─ IEmailPreferenceService.CanSendAsync(userId, WeeklyDigest)
    │
    ├─ Group by Organization (an OrgAdmin may belong to multiple orgs)
    │
    ├─ For each eligible OrgAdmin + organization pair:
    │   ├─ IWeeklyReportService.GenerateReportAsync(organizationId, weekStart, weekEnd)
    │   │   ├─ Count active exercises (Status == Active or InProgress)
    │   │   ├─ Count injects created/delivered this week
    │   │   ├─ Count active users (via ExerciseParticipant activity)
    │   │   ├─ List upcoming exercises with dates
    │   │   ├─ Count new OrganizationMemberships this week
    │   │   └─ Count pending OrganizationInvites
    │   │
    │   ├─ IEmailTemplateRenderer.RenderAsync("WeeklyOrgReport", model)
    │   │
    │   └─ IEmailService.SendAsync(email)
    │
    └─ IEmailLogService.LogEmailSentAsync(...)
```

### Timer Function

```csharp
// Cadence.Functions/Functions/WeeklyOrgReportFunction.cs
[Function("WeeklyOrgReport")]
public async Task RunAsync(
    [TimerTrigger("0 0 8 * * 1")] TimerInfo timer,  // Monday 8:00 AM UTC
    FunctionContext context)
{
    _logger.LogInformation("Weekly org report job started at {Time}", DateTime.UtcNow);

    var weekEnd = DateTime.UtcNow;
    var weekStart = weekEnd.AddDays(-7);

    var eligibleAdmins = await _reportService.GetEligibleOrgAdminsAsync();

    foreach (var (admin, orgId) in eligibleAdmins)
    {
        var report = await _reportService.GenerateReportAsync(orgId, weekStart, weekEnd);

        var html = await _templateRenderer.RenderAsync("WeeklyOrgReport", report.ToTemplateModel());
        await _emailService.SendAsync(admin.Email, report.Subject, html);
    }

    _logger.LogInformation("Weekly org report job completed. Processed {Count} reports", eligibleAdmins.Count);
}
```

### New Service

```csharp
// Cadence.Core/Features/Email/Services/IWeeklyReportService.cs
public interface IWeeklyReportService
{
    Task<List<(ApplicationUser Admin, Guid OrganizationId)>> GetEligibleOrgAdminsAsync(CancellationToken ct = default);
    Task<OrgWeeklyReportModel> GenerateReportAsync(Guid organizationId, DateTime weekStart, DateTime weekEnd, CancellationToken ct = default);
}
```

### Metrics Queries

All queries are scoped to the organization via `OrganizationId` (the standard multi-tenancy pattern).

| Metric | Source | Query |
| ------ | ------ | ----- |
| Active exercises | `Exercise` | Where `OrganizationId` matches and status is active |
| Injects created | `Inject` (via `Msel` → `Exercise`) | `CreatedAt` within week range |
| Injects delivered | `InjectStatusHistory` | `ToStatus == Delivered` and `ChangedAt` within week range |
| Active users | `ExerciseParticipant` | Distinct `UserId` with activity in week range |
| Upcoming exercises | `Exercise` | Where `ScheduledStartDate > now` ordered by date |
| New members | `OrganizationMembership` | `CreatedAt` within week range |
| Pending invitations | `OrganizationInvite` | Where `Status == Pending` |

### Key Difference from Daily Digest

| Aspect | Daily Digest (S01/S02) | Weekly Org Report (S03) |
| ------ | ---------------------- | ----------------------- |
| Schedule | Daily 6 AM UTC | Monday 8 AM UTC |
| Scope | User-specific activity | Organization-wide metrics |
| Empty behavior | Skip (no empty digests) | Always send (confirms system working) |
| Preference | `DailyDigest` category | `WeeklyDigest` category |
| Timer function | `DailyDigestFunction` | `WeeklyOrgReportFunction` (separate) |
| Target audience | All users / Directors | OrgAdmins |

## UI/UX Notes

### Email Preview

```
Subject: Weekly Report: Acme Emergency Management - Feb 3-9

---

[Organization Logo]

Weekly Organization Report
Acme Emergency Management
February 3-9, 2026

THIS WEEK'S ACTIVITY
━━━━━━━━━━━━━━━━━━━
Exercises Active: 2
Injects Created: 23
Injects Delivered: 47
Observations Captured: 31
Active Users: 18

UPCOMING EXERCISES
━━━━━━━━━━━━━━━━━━
• Operation Thunderstorm - March 15 (9 days)
• Quarterly Drill #2 - March 28 (22 days)

TEAM UPDATES
━━━━━━━━━━━━
• 2 new members joined
• 1 pending invitation

        [View Organization Dashboard]

---

Cadence - Exercise Management Platform
```

## Effort Estimate

**5 story points** - Organization-wide metrics aggregation, separate weekly timer function, multi-org handling

---

*Feature: EM-10 Digest & Summary Emails*
*Priority: P2*
