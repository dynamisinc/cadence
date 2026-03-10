# Fix-Agent Prompt — Hardening Pass 2

> **Input:** `docs/reviews/v2/SUMMARY.md` (11 issues from code review pass 2)
> **Branch:** `maintenance/code-hardening-v2` (created from `main`)
> **Output:** PR against `main` with all code fixes, new tests, and verification results

---

## Orchestration Model

This is a **small targeted fix pass** — not a full swarm. The scope is 11 issues (5 major, 6 minor) plus 5 test gaps. Use an **orchestrator-driven 3-phase approach** with backend and frontend agents running in parallel where files don't overlap.

### Pre-Flight Setup

```
1. Create branch: maintenance/code-hardening-v2 from main
2. Verify starting state:
   - Backend builds: dotnet build src/Cadence.WebApi
   - Backend tests pass: dotnet test src/Cadence.Core.Tests
   - Frontend compiles: cd src/frontend && npm run type-check
   - Frontend tests pass: cd src/frontend && npm run test -- --run
3. Record baseline counts:
   - Backend: 1,263 passed, 10 skipped
   - Frontend: 2,885 passed, 14 skipped
4. Read docs/reviews/v2/SUMMARY.md to load the issue list
```

---

## Testing Requirements

Same rule as pass 1: **No fix ships without a test that would have caught the original issue.**

| Fix Type | Required Test |
|----------|--------------|
| Authorization gap | Test: user not in exercise gets 403 |
| Middleware reorder | Integration test or documented skip |
| Pattern fix (setQueryData → invalidateQueries) | No test needed — pattern change |
| Convention fix (COBRA, hex color) | No test needed — verify type-check |
| Performance removal (redundant validation) | Existing tests must still pass |
| New validator tests | Full coverage: valid passes, each invalid fails |
| Logging addition | No test needed — observability improvement |
| Exception handling standardization | Existing tests must still pass |

---

## Phase Execution Plan

### Phase 1: Major Security & Quality Fixes

**Backend agent scope:**

| ID | Title | Files | Fix | Test Requirement |
|----|-------|-------|-----|-----------------|
| BV-003 | ExcelExportController POST missing exercise auth | `ExcelExportController.cs` | The POST endpoint at line 35 (`ExportMselPost`) accepts `ExerciseId` in the request body but has no `[AuthorizeExerciseAccess]` attribute. The GET endpoints on lines 76, 147, 193 correctly have it. **Fix approach:** The route-based `ExerciseAccessHandler` extracts `exerciseId` from route values, but the POST sends it in the body. **Two options:** (A) Add a service-layer check in the controller action that verifies the exercise belongs to the current org context before proceeding, using `_orgContext.OrganizationId` — e.g., `var exercise = await _context.Exercises.FirstOrDefaultAsync(e => e.Id == request.ExerciseId && e.OrganizationId == _orgContext.OrganizationId); if (exercise == null) return Forbid();`. (B) Refactor the POST to `POST /api/export/exercises/{exerciseId}/msel` with exerciseId in the route so `[AuthorizeExerciseAccess]` works. **Prefer option A** — it's lower risk and doesn't change the API contract. Also check `ExportTemplate` at line 124 which also lacks exercise auth. | Test: authenticated user in Org A calling POST with Org B's exerciseId gets 403. Test: user in correct org gets 200. |
| BV-004 | ObservationsController GET endpoints expose cross-exercise data | `ObservationsController.cs` | `GetObservationsByInject` (line 43, `GET /api/injects/{injectId}/observations`) and `GetObservation` (line 53, `GET /api/observations/{id}`) have no exercise-level authorization. The inject/observation ID is in the route but `exerciseId` is not, so `[AuthorizeExerciseAccess]` can't be used directly. **Fix approach:** Add service-layer validation in the controller actions. For `GetObservationsByInject`: look up the inject, then its MSEL's exercise, verify the exercise belongs to the current org. For `GetObservation`: look up the observation, navigate to its exercise, verify org ownership. Use the same pattern as the ExcelExport fix. Extract a shared helper if the pattern repeats. The `ObservationService` already filters by org via EF global query filters, but the controller should also validate to prevent any bypass. | Test: user in Org A querying inject from Org B gets 403. Test: user in correct org gets 200. |
| XV-M03 | Exception handler still placed after auth middleware | `Program.cs` | The global exception handler `app.Use(async (context, next) => { ... })` is at line 385, which is AFTER `UseAuthentication` (362), `UseAuthorization` (369), and `UseSerilogRequestLogging` (372). This means exceptions thrown by auth/authz middleware produce unstructured 500 responses. **Fix:** Move the entire `app.Use(async (context, next) => { ... })` block to immediately after `app.UseHttpsRedirection()` (around line 346), before `app.UseCors()`. This makes it the outermost request-processing layer. **CRITICAL:** Keep the `HasStarted` guard (AC-M10 fix) intact. Keep the `IsDevelopment()` stack trace toggle. Only change the position, not the logic. **Verify:** After moving, the middleware order should be: HttpsRedirection → ExceptionHandler → Cors → RateLimiter → RequestResponseLogging → Authentication → SerilogContext → Authorization → SerilogRequestLogging → MapControllers. | Test: This is hard to integration-test without a real server. Document with a comment explaining why the handler must be first. The existing skipped `ExceptionHandlerTests.cs` test documents the intent. Verify all existing tests still pass. |

**Frontend agent scope (runs in parallel — zero file overlap):**

| ID | Title | Files | Fix | Test Requirement |
|----|-------|-------|-----|-----------------|
| XV-M01 | ExerciseConductPage SignalR handlers use setQueryData instead of invalidateQueries | `ExerciseConductPage.tsx` | Six SignalR handlers at lines 243-300 (`handleInjectFired`, `handleInjectStatusChanged`, `handleClockChanged`, `handleObservationAdded`, `handleObservationUpdated`, `handleObservationDeleted`) all use `queryClient.setQueryData` to directly mutate the cache. This violates positive pattern AR-P02. **Fix:** Replace all 6 handlers with `queryClient.invalidateQueries` calls using the appropriate query keys. For injects: `queryClient.invalidateQueries({ queryKey: injectKeys.all(exerciseId!) })`. For observations: `queryClient.invalidateQueries({ queryKey: observationsQueryKey(exerciseId!) })`. For clock: `queryClient.invalidateQueries({ queryKey: ['exercises', exerciseId, 'clock'] })` (or whatever key the clock uses). The handler callbacks still receive the payload from SignalR but don't need to use it — `invalidateQueries` triggers a fresh fetch. Remove the `useCallback` dependencies on types that were only needed for cache manipulation. Also verify the reconnect handler (`handleReconnected`) already calls `invalidateQueries` for reconciliation. If it doesn't, add it. | No new test needed — pattern change. Verify `npm run type-check` passes. |

**Commit message:** `fix(security): exercise auth on export/observations endpoints, exception handler placement, SignalR cache pattern`

---

### Phase 2: Validator Tests + Test Gaps

**Backend agent scope:**

| ID | Title | Files | Details |
|----|-------|-------|---------|
| XV-M02 | FluentValidation validators created without tests | New: `Cadence.Core.Tests/Features/Injects/Validators/InjectValidatorTests.cs`, New: `Cadence.Core.Tests/Features/Exercises/Validators/ExerciseValidatorTests.cs` | Three validators exist but have no tests. Create test classes covering each validator's rules. |

**Test requirements for each validator:**

**`CreateInjectRequestValidator` (141 lines, validates ~14 fields):**
```csharp
// Tests to write:
CreateInjectRequest_ValidRequest_PassesValidation()
CreateInjectRequest_EmptyTitle_FailsWithTitleRequired()
CreateInjectRequest_TitleTooShort_FailsWithMinLength()   // min 3
CreateInjectRequest_TitleTooLong_FailsWithMaxLength()    // max 200
CreateInjectRequest_EmptyDescription_FailsWithDescriptionRequired()
CreateInjectRequest_DescriptionTooLong_FailsWithMaxLength()  // max 4000
CreateInjectRequest_EmptyTarget_FailsWithTargetRequired()
CreateInjectRequest_TargetTooLong_FailsWithMaxLength()   // max 200
CreateInjectRequest_InvalidPriority_FailsWithRange()     // 1-5
CreateInjectRequest_InvalidInjectType_FailsWithEnumValidation()
CreateInjectRequest_OptionalFieldsTooLong_FailsWithMaxLength()  // Source, ExpectedAction, ControllerNotes, LocationName, LocationType
```

**`UpdateInjectRequestValidator` (141 lines, same rules):**
```csharp
// Same tests as Create, just with Update prefix
UpdateInjectRequest_ValidRequest_PassesValidation()
UpdateInjectRequest_EmptyTitle_FailsWithTitleRequired()
// ... same pattern
```

**`CreateExerciseRequestValidator` (68 lines, validates ~9 fields):**
```csharp
CreateExerciseRequest_ValidRequest_PassesValidation()
CreateExerciseRequest_EmptyName_FailsWithNameRequired()
CreateExerciseRequest_NameTooLong_FailsWithMaxLength()         // max 200
CreateExerciseRequest_InvalidExerciseType_FailsWithEnumValidation()
CreateExerciseRequest_MissingScheduledDate_FailsWithRequired()
CreateExerciseRequest_MinValueScheduledDate_FailsWithValidation()
CreateExerciseRequest_DescriptionTooLong_FailsWithMaxLength()  // max 4000
CreateExerciseRequest_LocationTooLong_FailsWithMaxLength()     // max 200
CreateExerciseRequest_EmptyTimeZoneId_FailsWithRequired()
CreateExerciseRequest_ClockMultiplierBelowMin_FailsWithRange() // min 0.5
CreateExerciseRequest_ClockMultiplierAboveMax_FailsWithRange() // max 20.0
```

**Read each validator file first** to get the exact rules, then write tests that match. Use `FluentValidation.TestHelper` for clean assertions:

```csharp
using FluentValidation.TestHelper;

public class CreateInjectRequestValidatorTests
{
    private readonly CreateInjectRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = CreateValidRequest();
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTitle_FailsWithTitleRequired()
    {
        var request = CreateValidRequest();
        request.Title = "";
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    private static CreateInjectRequest CreateValidRequest() => new()
    {
        Title = "Test Inject",
        Description = "Test description",
        Target = "Test target",
        Priority = 3,
        // ... fill all required fields with valid values
    };
}
```

**No frontend work in this phase.**

**Commit message:** `test(validators): add FluentValidation validator tests for inject and exercise requests`

---

### Phase 3: Minor Fixes (All remaining)

**Backend agent scope:**

| ID | Title | Files | Fix | Test? |
|----|-------|-------|-----|-------|
| BV-001 | `LeaveUserGroup` missing logging | `ExerciseHub.cs` | **VERIFY FIRST:** The explorer found logging IS present at lines 72-75. Re-read the file. If logging exists, mark as already resolved. If the logging level differs from `JoinUserGroup` (which uses `LogWarning`), standardize to the same level. | No |
| BV-002 | `GetUserId()` exception handling inconsistency | Multiple controllers | Controllers that call `User.GetUserId()` (the throwing version) without a try-catch will return 500 instead of 401 if the claim is missing. Since `[Authorize]` prevents this in practice, the fix is low-risk. **Fix:** In controllers that currently use `GetUserId()` directly in action bodies (not wrapped in try-catch), switch to `User.TryGetUserId()` with a null check: `var userId = User.TryGetUserId(); if (userId == null) return Unauthorized();`. **Priority files:** `ExerciseStatusController.cs` (lines 38, 59, 78, 100). Scan all controllers for unguarded `User.GetUserId()` calls and convert to `TryGetUserId()`. Leave `GetUserId()` available for contexts where you WANT the exception (e.g., inside services where the caller already verified auth). | No new tests — defensive improvement. Verify existing tests pass. |
| BV-005 | `AutocompleteController` redundant `ValidateExerciseAccessAsync` | `AutocompleteController.cs` | Now that `[AuthorizeExerciseAccess]` is on all 6 action methods, the private `ValidateExerciseAccessAsync` (lines 139-161) is redundant and doubles DB load per request. **Fix:** (1) Remove the private method `ValidateExerciseAccessAsync` entirely. (2) Remove all 6 calls to it from the action methods (lines ~43, 60, 77, 94, 111, 128). (3) If any action methods need the `organizationId` that `ValidateExerciseAccessAsync` was providing, get it from `_orgContext.OrganizationId` instead. (4) Verify that `[AuthorizeExerciseAccess]` provides equivalent authorization coverage. | No new tests. Verify existing `AutocompleteOrgAccessTests.cs` tests still pass. |

**Frontend agent scope (runs in parallel):**

| ID | Title | Files | Fix | Test? |
|----|-------|-------|-----|-------|
| FV-01 / XV-N02 | `UpdatePrompt.tsx` raw MUI Button | `UpdatePrompt.tsx` | Line 16 imports `Button` from `@mui/material`. Lines 88-94 (collapse toggle), 139-147 (update button), and 148-154 (later/dismiss button) use raw `Button`. **Fix:** (1) Remove `Button` from the MUI import. (2) Import `CobraPrimaryButton` and `CobraLinkButton` from `@/theme/styledComponents`. (3) Replace the "Update" button (lines 139-147) with `CobraPrimaryButton`. (4) Replace the "Later"/"Dismiss" button (lines 148-154) with `CobraLinkButton`. (5) For the collapse toggle button (lines 88-94), if it's an icon-only button, consider `CobraIconButton` from `@/theme/styledComponents`; if it has text, use `CobraLinkButton`. Match the existing visual behavior — just swap the component, keep the same `onClick`, `sx`, and other props. If any COBRA button doesn't support an existing prop (e.g., `size="small"`), the COBRA components extend MUI's ButtonProps so most props pass through. | No test. Verify `npm run type-check`. |
| FV-02 | `ProfileMenu.tsx` residual hardcoded hex | `ProfileMenu.tsx` | Line 146 has `color: '#ffffff'` (avatar text color). **Fix:** Replace with `color: theme.palette.common.white` or `color: theme.palette.getContrastText(theme.palette.secondary.main)`. Use `useTheme()` if not already imported, or reference the theme via the `sx` prop callback pattern: `sx={(theme) => ({ color: theme.palette.common.white })}`. Scan the file for any other hardcoded hex values and replace with theme tokens. | No test. Verify `npm run type-check`. |
| FV-03 | Multiple components import `IconButton` directly from MUI | 6 files: `InstallBanner.tsx`, `PageHeader.tsx`, `AppHeader.tsx`, `AssignmentSection.tsx`, `NotificationBell.tsx`, `GroupHeader.tsx` | `CobraIconButton` exists in `@/theme/styledComponents` (styled with text.secondary color, 8px border radius, hover with action.hover bg + buttonPrimary.main text color). **Fix per file:** (1) Remove `IconButton` from the `@mui/material` import. (2) Add `import { CobraIconButton } from '@/theme/styledComponents'`. (3) Replace all `<IconButton>` JSX with `<CobraIconButton>`. (4) Review any custom `sx` styling on the IconButton — if it conflicts with CobraIconButton's defaults, adjust or override in `sx`. **IMPORTANT:** Some IconButtons may have specific styling (e.g., AppHeader's menu button) that differs from the COBRA defaults. If replacing breaks the visual, keep the raw import for that specific case and add a `// COBRA exception: AppHeader menu button requires custom styling` comment. | No test. Verify `npm run type-check`. |
| XV-N03 | `useUpdateUser` uses `setQueryData` in mutation callback | `useUsers.ts` | Line 77 uses `queryClient.setQueryData(userKeys.detail(updatedUser.id), updatedUser)` in the `onSuccess` callback. This is from a trusted server response (mutation result), NOT from a SignalR event — so it's a valid React Query pattern, distinct from AR-P02. **Fix:** Add a clarifying comment above the `setQueryData` call: `// Eager cache update from trusted server response (mutation result). // This is distinct from AR-P02 (SignalR event handlers must use invalidateQueries).` | No test. |

**Commit message:** `fix(minor): hub logging, controller GetUserId safety, autocomplete perf, COBRA compliance, theme tokens`

---

## Verification Gates

### After Each Phase

Run all four checks. ALL must pass before proceeding.

```bash
# 1. Backend build
dotnet build src/Cadence.WebApi/Cadence.WebApi.csproj

# 2. Backend tests
dotnet test src/Cadence.Core.Tests/Cadence.Core.Tests.csproj

# 3. Frontend type-check
cd src/frontend && npm run type-check

# 4. Frontend tests
cd src/frontend && npm run test -- --run
```

### After All Phases (Final Gate)

```bash
# Count new tests (compare to baseline: 1,263 backend, 2,885 frontend)
dotnet test src/Cadence.Core.Tests --list-tests 2>&1 | grep -c "\[Fact\]"
cd src/frontend && npm run test -- --run --reporter=verbose 2>&1 | grep -c "✓"
```

### Gate Failure Protocol

Same as pass 1: diagnose, fix, re-verify before proceeding. If a test breaks, do not skip it — fix the underlying issue.

---

## Positive Patterns — Do Not Regress

All 27 passing patterns from V2 review must remain intact. The key ones at risk during this fix pass:

| Pattern | Risk Area |
|---------|-----------|
| AR-P02 (React Query invalidation via SignalR) | XV-M01 fix directly addresses this — ensure `invalidateQueries` is used, not `setQueryData` |
| AC-P01 (Authorization policy infrastructure) | BV-003/BV-004 service-layer auth must complement, not bypass, existing attribute infrastructure |
| CD-P04 (Static pure-function mappers) | No mappers being modified — low risk |
| AC-P06 (Clean hub group management) | BV-001 only adds logging — low risk |

---

## Commit Strategy

Three commits, one per phase:

```
Phase 1: fix(security): exercise auth on export/observations, exception handler placement, SignalR cache pattern
Phase 2: test(validators): add FluentValidation validator tests for inject and exercise requests
Phase 3: fix(minor): hub logging, controller GetUserId safety, autocomplete perf, COBRA compliance, theme tokens
```

After all phases, create PR:

```bash
gh pr create \
  --base main \
  --head maintenance/code-hardening-v2 \
  --title "fix: code hardening pass 2 — auth gaps, validator tests, minor cleanup" \
  --body "$(cat <<'EOF'
## Summary

Targeted fixes addressing 11 issues from code review pass 2 (docs/reviews/v2/SUMMARY.md).

### Changes by Phase
- **Phase 1 (Security & Quality):** Exercise-scoped auth on ExcelExport POST + Observations GET endpoints, exception handler moved to outermost middleware position, ExerciseConductPage SignalR handlers migrated from setQueryData to invalidateQueries
- **Phase 2 (Test Gaps):** FluentValidation validator tests for CreateInjectRequest, UpdateInjectRequest, CreateExerciseRequest (~30 tests)
- **Phase 3 (Minor):** Hub logging symmetry, GetUserId→TryGetUserId safety, AutocompleteController perf (remove redundant validation), COBRA compliance (UpdatePrompt, 6 IconButton files), theme token for ProfileMenu hex color, setQueryData comment in useUsers

### Test Impact
- **New backend tests:** ~30 (validator tests)
- **Pre-existing test regressions:** 0

### Issues Resolved
- BV-003 (Major): ExcelExportController POST auth
- BV-004 (Major): ObservationsController cross-exercise data
- XV-M01 (Major): AR-P02 pattern regression fixed
- XV-M02 (Major): Validator tests added
- XV-M03 (Major): Exception handler placement
- BV-001 (Minor): Hub logging
- BV-002 (Minor): GetUserId safety
- BV-005 (Minor): Autocomplete perf
- FV-01/XV-N02 (Minor): UpdatePrompt COBRA
- FV-02 (Minor): ProfileMenu hex color
- FV-03 (Minor): IconButton COBRA migration
- XV-N03 (Minor): setQueryData documentation

## Test Plan
- [ ] All backend tests pass (baseline + ~30 new)
- [ ] All frontend tests pass
- [ ] Frontend type-check clean
- [ ] Manual smoke: exercise CRUD, inject firing, Excel export, observations

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

---

## Estimated Effort

| Phase | Backend | Frontend | Risk |
|-------|---------|----------|------|
| Phase 1: Security & Quality | 1-2 hours | 30 min | Low-Medium |
| Phase 2: Validator Tests | 1 hour | None | Low |
| Phase 3: Minor Fixes | 30 min | 1 hour | Low |
| **Total** | **~3.5 hours** | **~1.5 hours** | |

Phase 1 is the most critical — the auth fixes must be correct. Phase 3 has the most files but lowest risk (convention changes).
