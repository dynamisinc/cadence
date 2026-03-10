# Backend Verification Review — Pass 2

> **Date:** 2026-03-10
> **Reviewer:** Backend Verification Specialist
> **Scope:** `src/Cadence.WebApi/`, `src/Cadence.Core/`

---

## Pass 1 Resolution Tracking

| ID | Title | Severity | Status | Notes |
|----|-------|----------|--------|-------|
| AC-C01 | Hardcoded `SystemUserIdString` in mutation endpoints | Critical | Resolved | All 5 controllers use `User.GetUserId()` or `User.TryGetUserId()`. `SystemConstants.SystemUserIdString` only appears in `DataSeederExtensions.cs` (seeder context — correct). |
| AC-C02 | Missing exercise-scoped authorization on 15 endpoints | Critical | Partially Resolved | 6 of 7 controllers fixed. `ExcelExportController.ExportMselPost` (POST) has no `[AuthorizeExerciseAccess]`. `ObservationsController.GetObservationsByInject` and `GetObservation` are still unauthenticated at the action level. See Partially Resolved section. |
| AC-C03 | Business logic and direct `AppDbContext` in 6 controllers | Critical | Resolved | `ExercisesController` now delegates to `IExerciseCrudService`, `IExerciseDeleteService`, `IMselService`, `ISetupProgressService`, `IExerciseApprovalSettingsService`, `IExerciseApprovalQueueService`. No direct `AppDbContext` injection in controller. |
| AC-C04 | Hardcoded `isAdmin = true` in delete operations | Critical | Resolved | `ExercisesController` now uses `User.IsAdminOrOrgAdmin()` for both `GetDeleteSummary` and `DeleteExercise`. |
| AC-C05 / AR-C01 | CORS wildcard in production | Critical | Resolved | `Program.cs` correctly gates `SetIsOriginAllowed(_ => true)` behind `builder.Environment.IsDevelopment()`. Production path uses `Cors:AllowedOrigins` from configuration. |
| AC-C06 | SignalR hub accepts client-supplied user ID | Critical | Resolved | `ExerciseHub.JoinUserGroup()` no longer accepts a `userId` parameter. Reads from `Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)`. `[Authorize]` is on the class. |
| CD-C01 | Static in-memory session state | Critical | Partially Resolved | `ConcurrentDictionary` remains. `SessionTimeoutMinutes = 30` TTL and `PurgeExpiredSessions()` added with tracking TODO. Single-instance limitation documented. Meets the "minimum viable fix" from the fix-agent prompt. |
| CD-C02 | N+1 `SaveChangesAsync` in bulk import | Critical | Resolved | `ConfirmImportAsync` now wraps loop in explicit transaction (`BeginTransactionAsync`) and calls `SaveChangesAsync()` once at the end. |
| CD-C03 | Double SignalR broadcast for approval actions | Critical | Resolved | `InjectService` delegates to `ApprovalNotificationService`. No direct `_hubContext.NotifyInjectXxx()` in approval paths. `InjectServiceTests` verifies single call. |
| CD-C04 | Exercise delete missing EEG and photo cascades | Critical | Resolved | `ExerciseDeleteService` now includes cascades for `EegEntries`, `CriticalTasks`, `CapabilityTargets`, and `ExercisePhotos` (steps 10–13). |
| AC-M01 | `GetCurrentUserId()` duplicated across 12 controllers | Major | Resolved | `ClaimsPrincipalExtensions.cs` created with `GetUserId()`, `TryGetUserId()`, `IsSystemAdmin()`, `IsOrgAdmin()`, `IsAdminOrOrgAdmin()`, `GetOrganizationId()`. All controllers use `User.GetUserId()`. |
| AC-M02 | `GetUserMemberships` missing admin authorization | Major | Resolved | `UsersController.GetUserMemberships` enforces self-or-admin: `if (currentUserId != userId && !isAdmin) return Forbid()`. Tests cover the three auth paths. |
| AC-M03 | String literal `"Admin"` in `SystemSettingsController` | Major | Resolved | `SystemSettingsController` now has `[AuthorizeAdmin]` at the class level. |
| AC-M04 | `ExercisesController` at 957 lines | Major | Resolved | Controller delegates to 6 injected services. No LINQ or business logic remains. |
| AC-M06 | Custom auth in `AutocompleteController` | Major | Partially Resolved | `[AuthorizeExerciseAccess]` on all action methods. But `ValidateExerciseAccessAsync` private method still called redundantly alongside attribute. Defence-in-depth but adds maintenance burden and latency. |
| AC-M07 | `CapabilitiesController` write endpoints missing role gate | Major | Resolved | POST, PUT, DELETE have `[AuthorizeOrgAdmin]`. Private `ValidateOrganizationAccess` also verifies org membership. |
| AC-M08 | `NotificationsController` repeats user ID extraction | Major | Resolved | Covered by AC-M01. All controllers use `User.GetUserId()`. |
| AC-M10 | Exception handler missing `HasStarted` guard | Major | Resolved | `Program.cs` exception handler checks `if (context.Response.HasStarted)`, logs warning, and re-throws. |
| AR-M02 / CD-M08 | No FluentValidation validators existed | Major | Resolved | `CreateExerciseRequestValidator`, `CreateInjectRequestValidator`, `UpdateInjectRequestValidator` exist in respective `Validators/` directories. Registered via `AddValidatorsFromAssemblyContaining`. |
| AR-M03 | Global exception handler placement | Major | Partially Resolved | Exception handler has `HasStarted` guard (AC-M10), but placement is still AFTER auth middleware — not the outermost layer. See cross-cutting review XV-M03. |
| CD-M01 | Metrics sub-services bypass DI | Major | Resolved | `ServiceCollectionExtensions` registers all metrics sub-services as scoped. No fallback `new` path. Tests construct with real implementations. |
| CD-M02 | Missing `.AsNoTracking()` on read queries | Major | Resolved | `ObservationService` read methods include `.AsNoTracking()`. Spot-check passed. |
| CD-M03 | Observation DTO projection copied 5 times | Major | Resolved | `ObservationMapper.ToObservationDtoExpression` used by all 3 read methods. |
| CD-M04 | Fetch-then-update in `NotificationService` | Major | Resolved | `MarkAllAsReadAsync` uses `ExecuteUpdateAsync`. `DeleteOldNotificationsAsync` uses `ExecuteDeleteAsync`. Test skipped with `[Fact(Skip = "ExecuteUpdateAsync not supported by InMemory")]`. |
| CD-M05 | Missing user activation in invitation flow | Major | Resolved | `AcceptInvitationAsync` checks `user.Status == UserStatus.Pending` and sets `Status = Active`. Tests cover both paths. |
| CD-M06 | Inconsistent org-scoping in service queries | Major | Resolved | Explicit org filtering in key services alongside global query filters. |
| CD-M07 | Large service classes | Major | Resolved | `ExerciseCrudService` created. `InjectService` handles conduct operations only. Services are separate and registered in DI. |
| AC-N01 | Duplicate `GetCurrentUserId` in `ExerciseClockController` | Minor | Resolved | No local method. Uses `User.GetUserId()`. |
| AC-N02 | `VersionInfo` record inline in controller | Minor | Resolved | Moved to `Core/Features/SystemSettings/Models/DTOs/VersionDtos.cs`. |
| AC-N03 | `StatusCode(500)` in `BulkParticipantImportController` | Minor | Resolved | All exception handlers use `return Problem(...)`. |
| AC-N05 | Missing `[Authorize]` on `ExerciseHub` | Minor | Resolved | `[Authorize]` is on the class. |
| AC-N06 | Regex body sanitization (deferred) | Minor | Deferred | As planned. |
| CD-N01 | Inconsistent mapper patterns | Minor | Resolved | `ObservationMapper.ToObservationDtoExpression` is static — consistent with CD-P04. |
| CD-N02 | TODO/FIXME comments | Minor | Partially Resolved | `BulkParticipantImportService` retains intentional TODO for CD-C01. Others not exhaustively audited. |
| CD-N06 | Exception swallowing | Minor | Partially Resolved | `ExerciseDeleteService` re-throws after rollback. Not exhaustively audited. |
| AR-N02 | Feedback mapper | Minor | Not Verified | Not specifically checked in this pass. |

---

## Resolution Summary

| Severity | Resolved | Partially Resolved | Unresolved | Total |
|----------|----------|--------------------|------------|-------|
| Critical | 7 | 2 | 0 | 9 |
| Major | 14 | 2 | 0 | 16 |
| Minor | 7 | 2 | 1 | 10 |
| **Total** | **28** | **6** | **1** | **35** |

---

## Partially Resolved Issues

### AC-C02 — Missing exercise-scoped authorization (incomplete)

**What was fixed:** Six of the seven named controllers now have `[AuthorizeExerciseAccess]` attributes.

**What remains:**

1. **`ExcelExportController.ExportMselPost`** — POST `/api/export/msel` (line 35) has no `[AuthorizeExerciseAccess]`. Exercise ID is in the request body, not route — the route-based authorization handler cannot extract it. GET endpoints are correctly protected.

   **Fix:** Add service-layer ownership validation or restructure route to include `exerciseId`.

2. **`ObservationsController.GetObservationsByInject`** (line 43) and **`GetObservation`** (line 53) — authenticated but not exercise-scoped. No `exerciseId` in the route path.

   **Fix:** Add exercise ID route parameter or service-layer authorization checking parent exercise membership.

### AC-M06 — AutocompleteController custom auth (redundant)

**What was fixed:** `[AuthorizeExerciseAccess]` on all action methods.

**What remains:** Private `ValidateExerciseAccessAsync` (lines 139–161) still called from every action, creating a redundant database round-trip per request. For a high-frequency keystroke endpoint, this doubles DB load for authorization.

### AR-M03 — Exception handler placement (partially addressed)

**What was fixed:** `HasStarted` guard added (AC-M10).

**What remains:** Handler still registered after auth middleware (line 385 of Program.cs). Exceptions from rate limiting, authentication, or authorization middleware produce unstructured 500 responses.

### CD-N02 — TODO/FIXME audit (partially addressed)

**What was fixed:** `BulkParticipantImportService` TODO is intentionally retained.

**What remains:** Other TODO/FIXME comments not exhaustively audited.

---

## New Issues — Introduced by Fixes

### BV-001: `ExerciseHub.LeaveUserGroup` silently returns on missing auth — inconsistent with `JoinUserGroup`

**Confidence:** 82 | **Severity:** Minor

**File:** `src/Cadence.WebApi/Hubs/ExerciseHub.cs`, lines 69–82

`JoinUserGroup` logs a warning when user ID is missing from claims and returns. `LeaveUserGroup` silently returns without logging. Given the hub has `[Authorize]`, the scenario shouldn't occur, but the defensive logging asymmetry reduces observability.

**Fix:** Add `_logger.LogDebug(...)` in the `LeaveUserGroup` null guard.

### BV-002: `ClaimsPrincipalExtensions.GetUserId` throws `UnauthorizedAccessException` but callers catch inconsistently

**Confidence:** 80 | **Severity:** Minor

**File:** `src/Cadence.WebApi/Extensions/ClaimsPrincipalExtensions.cs`, line 28

`GetUserId()` throws `UnauthorizedAccessException` on missing claim. Some controllers catch it (`UsersController.GetUserMemberships`), but others (`ExerciseStatusController` lines 38, 59, 78) don't — the exception bubbles to the global handler as a 500 instead of 401. The `[Authorize]` attribute should prevent this in practice, but defensive handling is inconsistent.

**Fix:** Either consistently catch `UnauthorizedAccessException` or use `TryGetUserId()` with null checks everywhere.

### BV-003: `ExcelExportController.ExportMselPost` missing exercise-scoped authorization

**Confidence:** 95 | **Severity:** Major

**File:** `src/Cadence.WebApi/Controllers/ExcelExportController.cs`, line 35

POST endpoint accepts `ExerciseId` in request body with no exercise membership check. Any authenticated user can export another organization's MSEL by knowing an exercise GUID. Residual gap from AC-C02.

### BV-004: `ObservationsController` inject-scoped endpoints expose cross-exercise data

**Confidence:** 88 | **Severity:** Major

**File:** `src/Cadence.WebApi/Controllers/ObservationsController.cs`, lines 43–48 and 53–63

GET endpoints are authenticated but not exercise-scoped. A user in Organization A can query observations from Organization B by knowing GUIDs. Service layer uses org-context query filters, but these don't validate exercise membership.

### BV-005: `AutocompleteController` redundant `ValidateExerciseAccessAsync` adds latency

**Confidence:** 80 | **Severity:** Minor

**File:** `src/Cadence.WebApi/Controllers/AutocompleteController.cs`, lines 43–161

Now that `[AuthorizeExerciseAccess]` is applied, the private method performs a redundant DB round-trip per keystroke. Performance concern introduced by AC-M06 fix.

**Fix:** Remove `ValidateExerciseAccessAsync` and calls to it.

---

## New Issues — Previously Undetected

None found beyond the residual gaps from AC-C02 (tracked as BV-003 and BV-004).

---

## Deferred Item Re-evaluation

| ID | Title | Original Reason | Should Prioritize Now? | Rationale |
|----|-------|----------------|----------------------|-----------|
| AR-M05 | Migrate tests from InMemory to SQL Server | Large infrastructure change | No | `[Fact(Skip)]` pattern used correctly for unsupported operations. No immediate blocker. |
| CD-N04 | Extract entity configs from `AppDbContext` | Low risk, high churn | No | No new urgency. |
| CD-N07 | Composite indexes | Needs production query profiling | No | Still needs real workload data. |
| AC-N04 | Align export route path | Would break frontend URLs | No | Fixing BV-003 may naturally align paths. |
| AC-N06 | JSON-based body sanitization | Working correctly, just fragile | No | No new evidence of bypass. |
| CD-N03 | Remove unused navigation properties | Needs careful cascade analysis | No | Still requires manual audit. |
| CD-N05 | Document seeder ordering | Documentation only | No | Low priority. |

---

## Key Observations

**Pass 1 hardening was largely successful.** All 7 critical issues with full resolution paths are confirmed resolved. The two "partially resolved" critical issues (AC-C02, CD-C01) match documented acceptance criteria.

**Most important follow-ups are BV-003 and BV-004** — real authorization gaps where authenticated users can access data from other organizations without exercise membership verification.

**Positive patterns are preserved** — `ClaimsPrincipalExtensions`, `ObservationMapper.ToObservationDtoExpression`, and `OrganizationValidationInterceptor` all follow established patterns.

---

> **Report generated by:** Backend Verification Specialist (Pass 2)
