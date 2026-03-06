# Fix-Agent Swarm Prompt — Hardening Pass 1

> **Input:** `docs/reviews/SUMMARY.md` (prioritized remediation order from code review pass 1)
> **Branch:** `maintenance/code-hardening-v1` (created from `main` after review branch merge)
> **Output:** PR against `main` with all code fixes, new tests, and verification results

---

## Orchestration Model

This swarm uses an **orchestrator-driven model with phase gates**. The orchestrator (you) controls all sequencing, dispatches agents, and makes priority decisions. No agent self-selects work.

### Why Orchestrator-Driven

1. **Dependency ordering** — Shared utilities must be created before features that import them. A `devLog` utility must exist before 16 files can be updated to use it. `ClaimsPrincipalExtensions` must exist before 12 controllers can adopt it.
2. **Conflict prevention** — Multiple agents modifying the same file concurrently creates merge hell. The orchestrator ensures file-level exclusivity.
3. **Phase gates** — The build and test suite must pass after each phase before the next phase begins. If Phase 2 breaks tests, Phase 3 agents would compound the damage.
4. **Triage decisions** — Fixes sometimes reveal new issues or contradict each other. The orchestrator resolves these in real-time rather than agents making independent (potentially conflicting) choices.

### Orchestrator Responsibilities

1. Read `docs/reviews/SUMMARY.md` remediation order at session start
2. For each phase: dispatch agents, wait for completion, run verification gate
3. If a gate fails: diagnose, dispatch fix agent, re-verify before proceeding
4. If a fix reveals a new issue: triage — fix now if blocking, defer to next review pass if not
5. After all phases: run final verification, create PR

---

## Pre-Flight Setup

Before dispatching any agents:

```
1. Ensure maintenance/code-review-v1 has been merged to main
2. Create branch: maintenance/code-hardening-v1 from main
3. Verify starting state:
   - Backend builds: dotnet build src/Cadence.WebApi
   - Backend tests pass: dotnet test src/Cadence.Core.Tests
   - Frontend compiles: cd src/frontend && npm run type-check
   - Frontend tests pass: cd src/frontend && npm run test -- --run
4. Record baseline test counts (so we can verify new tests were added)
5. Read docs/reviews/SUMMARY.md to load the remediation plan
```

---

## Testing Requirements

**This is a hardening pass, not a feature pass. Every change must be provably safe.**

### The Testing Rule

> **No fix ships without a test that would have caught the original issue.**

This means:

| Fix Type | Required Test | Example |
|----------|--------------|---------|
| **Logic moved from controller to service** | Unit test for the new/modified service method covering the same behavior | Moving `ValidateInjectRequest` from controller to `CreateInjectRequestValidator` → test that valid input passes, test that each invalid field fails with correct message |
| **New shared utility created** | Unit test for every public function | `ClaimsPrincipalExtensions.GetUserId()` → test with valid claims, test with missing claim throws, test with malformed claim throws |
| **Bug fix** (wrong query key, missing cascade, etc.) | Regression test that reproduces the bug scenario | Wrong query key in `useExcelImport` → test that `onSuccess` invalidates the correct query key |
| **Security fix** (missing auth, CORS) | Test that unauthorized access is rejected | Missing `[ExerciseAccess]` → test that a user not assigned to the exercise gets 403 |
| **DRY extraction** (deduplication) | Tests for the extracted shared code + verify callers still work | Extracted `ObservationMapper.ToDto` projection → test mapping completeness, verify existing service tests still pass |
| **Convention fix** (COBRA, imports) | No test required — visual/style change only | Replacing raw MUI Button with CobraPrimaryButton |
| **Configuration fix** (CORS, middleware order) | Integration test or documented manual verification | CORS env-gating → test that non-dev rejects unknown origins |

### Test Naming Conventions

Follow the project's established patterns:

**Backend (C#):**
```csharp
// Pattern: {Method}_{Scenario}_{ExpectedResult}
public async Task GetUserId_ValidSubClaim_ReturnsGuid()
public async Task GetUserId_MissingSubClaim_ThrowsUnauthorized()
public async Task ValidateInject_EmptyTitle_ReturnsFieldError()
public async Task DeleteExercise_CascadesToEegEntries_AllMarkedDeleted()
```

**Frontend (TypeScript):**
```typescript
// Pattern: describe('{Unit}') → it('{behavior}')
describe('devLog', () => {
  it('logs in development environment');
  it('does not log in production environment');
});

describe('isNetworkError', () => {
  it('returns true for "Network Error" message');
  it('returns true for ERR_NETWORK code');
  it('returns false for 404 response');
});
```

### Test-First Workflow for Each Fix

```
1. UNDERSTAND the issue from the review report (read the affected files)
2. WRITE the test that exposes the problem (RED — test fails or demonstrates the gap)
3. IMPLEMENT the fix (GREEN — test passes)
4. VERIFY no regressions (run full test suite)
5. REFACTOR if needed (keep tests green)
```

For convention fixes (COBRA, imports) where no test is needed, skip steps 2-3 and verify with `npm run type-check` instead.

---

## Phase Execution Plan

Each phase follows the same lifecycle:

```
PHASE N:
  1. Orchestrator reads the phase's issue list from SUMMARY.md
  2. Orchestrator groups issues by agent type (backend vs frontend)
  3. Orchestrator dispatches backend-agent and frontend-agent IN PARALLEL
     (only if they don't touch the same files)
  4. Agents complete their work, including tests
  5. Orchestrator runs verification gate:
     a. dotnet build src/Cadence.WebApi
     b. dotnet test src/Cadence.Core.Tests
     c. cd src/frontend && npm run type-check
     d. cd src/frontend && npm run test -- --run
  6. If gate passes → commit phase, proceed to next phase
  7. If gate fails → diagnose, fix, re-verify
  8. Commit with message: fix({scope}): {phase description}
```

### Phase 1: Critical Security & Data Integrity

**Priority:** These issues represent active security vulnerabilities or data integrity risks. Fix first, no exceptions.

**Backend agent scope:**

| ID | Title | Files | Test Requirement |
|----|-------|-------|-----------------|
| AC-C05 / AR-C01 | CORS wildcard in production | `Program.cs`, `appsettings.json` | Test: non-dev environment rejects unknown origins. Test: dev environment allows any origin. |
| AC-C02 | Missing exercise-scoped authorization (15 endpoints) | 7 controllers | Test per controller: unauthenticated user gets 401, user not in exercise gets 403, user in exercise with correct role gets 200. |
| AC-C06 | SignalR hub accepts client-supplied user ID | `ExerciseHub.cs` | Test: `JoinUserGroup` uses `Context.UserIdentifier`, not client parameter. |
| AC-C04 | Hardcoded `isAdmin = true` in delete operations | `ExercisesController.cs` | Test: non-admin user calling delete does not get admin privileges. |
| AC-C01 | Hardcoded `SystemUserIdString` in 5 controllers | 5 controllers | Test per controller: mutation records the authenticated user's ID, not system constant. |
| CD-C04 | Exercise delete missing entity cascades | `ExerciseDeleteService.cs` | Test: deleting an exercise cascades soft-delete to EegEntry, ExercisePhoto, CriticalTask, CapabilityTarget, InjectCriticalTask. Consider a "completeness test" that reflects on all IOrganizationScoped types to catch future regressions. |

**No frontend work in this phase.**

**Commit message:** `fix(security): CORS env-gating, exercise authorization, hub identity, audit trail user IDs, delete cascades`

---

### Phase 2: Critical Stability & Shared Infrastructure

**Priority:** These are foundation fixes — shared utilities and infrastructure that later phases depend on.

**Backend agent scope:**

| ID | Title | Files | Test Requirement |
|----|-------|-------|-----------------|
| AC-M01 | Extract shared `GetCurrentUserId()` extension | New: `Extensions/ClaimsPrincipalExtensions.cs`, modify 12 controllers | Test: `GetUserId()` with valid/missing/malformed claims. Test: `GetOrganizationId()` same. Verify all 12 controllers compile and existing tests pass. |
| CD-C03 | Double SignalR broadcast for approval actions | `InjectService.cs` | Test: approval action triggers exactly one broadcast (mock hub context, verify single call). |
| CD-C02 | N+1 SaveChangesAsync in bulk import | `BulkParticipantImportService.cs` | Test: 10-row import calls SaveChangesAsync ≤3 times (not 30). Test: all-or-nothing — if row 8 fails, rows 1-7 are not committed. |
| AR-M03 + AC-M10 | Exception handler placement + HasStarted guard | `Program.cs` | Test: exception in auth middleware returns structured ProblemDetails. Test: exception after response started logs warning, does not throw secondary. |
| CD-C01 | Static in-memory import session state | 2 import services | **Design decision needed:** Replace with IDistributedCache (Redis/SQL) or serialize to database. For V1 hardening, minimum viable fix is: add session expiration + cleanup timer, document the single-instance limitation. Full distributed cache is a Phase 2 feature. Test: expired sessions return 404. Test: cleanup removes sessions older than TTL. |

**Frontend agent scope:**

| ID | Title | Files | Test Requirement |
|----|-------|-------|-----------------|
| AR-M04 / FI-M01 / FF-C02 | Create dev-only logger, remove console.log | New: `core/utils/logger.ts`, modify 16 files | Test: `devLog` emits in dev, silent in prod. Test: `devWarn` same. Then global find-replace `console.log` → `devLog`. Verify with `npm run type-check`. |
| FI-M03 | Duplicated `ConnectionState` type | 2 hooks + new types file | Test: both hooks reference same type (TypeScript compilation is the test). |
| FI-M04 | Duplicated network error detection | `AuthContext.tsx`, `api.ts` + new utility | Test: `isNetworkError()` for each known error pattern (`'Network Error'`, `'ECONNREFUSED'`, `'ERR_NETWORK'`, timeout, non-network error returns false). |

**These two agents can run in parallel — zero file overlap.**

**Commit message:** `fix(stability): shared extensions, single broadcast, batched imports, exception handling, dev logger`

---

### Phase 3: Frontend Convention & DRY Fixes

**Priority:** COBRA compliance and frontend DRY. All frontend, no backend.

**Frontend agent scope:**

| ID | Title | Files | Test Requirement |
|----|-------|-------|-----------------|
| FI-C01 | Raw MUI Button in ProfileMenu | `ProfileMenu.tsx` | No test — convention fix. Verify `npm run type-check`. |
| FI-C02 | Hardcoded hex colors in ConnectionStatusIndicator | `ConnectionStatusIndicator.tsx` | No test — convention fix. Verify compiles. |
| FI-M02 | Hardcoded hex in OrganizationSwitcher | `OrganizationSwitcher.tsx` | No test — convention fix. |
| FF-M04 | Raw MUI Button in PendingApprovalAlert | `PendingApprovalAlert.tsx` | No test — convention fix. |
| FF-M05 | Raw MUI Button in VersionInfoCard | `VersionInfoCard.tsx` | No test — convention fix. |
| FI-M05 | Duplicated theme component overrides | `cobraTheme.ts` | No test — extract shared constant. Verify compiles. |

**Commit message:** `fix(frontend): COBRA compliance — replace raw MUI, use theme tokens, deduplicate theme overrides`

---

### Phase 4: Major Backend Stability & DRY

**Priority:** Service-layer improvements. This is the largest phase and the one most likely to surface new issues. The orchestrator should dispatch these in sub-batches to keep commits reviewable.

**Sub-batch 4A: Service extractions and DI fixes**

| ID | Title | Files | Test Requirement |
|----|-------|-------|-----------------|
| CD-M01 | Register metrics sub-services in DI | `ExerciseMetricsService.cs`, `ServiceCollectionExtensions.cs` | Test: each sub-service can be resolved from DI container. Test: `ExerciseMetricsService` works with injected (not `new`'d) sub-services. |
| CD-M03 | Extract observation DTO projection (5 copies → 1) | `ObservationService.cs`, new/modified `ObservationMapper.cs` | Test: extracted projection maps all fields correctly (compare output to hardcoded expected DTO). Verify existing observation tests still pass. |
| CD-M04 | Use ExecuteUpdateAsync/ExecuteDeleteAsync in notifications | `NotificationService.cs` | Test: `MarkAllAsReadAsync` marks correct notifications. Test: `DeleteOldNotificationsAsync` deletes correct set. (Note: these EF Core bulk methods may need real SQL for testing — if InMemory doesn't support them, document and skip the test with `[Fact(Skip = "Requires SQL Server")]`.) |
| CD-M05 | Fix missing user activation in invitation flow | `OrganizationInvitationService.cs` | Test: pending user accepting invitation transitions to Active status. Test: already-active user accepting invitation stays Active. |

**Sub-batch 4B: Controller-to-service extraction (HIGH RISK — largest refactoring)**

| ID | Title | Files | Test Requirement |
|----|-------|-------|-----------------|
| AC-C03 | Move business logic from 6 controllers to Core services | 6 controllers, new/extended service files | **For each controller being refactored:** (1) Identify all business logic (LINQ queries, entity manipulation, validation) in the controller. (2) Write tests for the target service method covering the same behavior. (3) Extract logic to service. (4) Controller calls service. (5) Verify controller compiles and existing tests pass. **Priority order:** `ExercisesController` (most complex), `PhasesController`, `ExpectedOutcomesController`, `AutocompleteController`, `ExerciseStatusController`, `InjectsController` (partial — some actions bypass service). |
| AC-M04 | Decompose ExercisesController (957 lines) | `ExercisesController.cs` | Will be partially addressed by AC-C03. After extraction, if still >500 lines, split into sub-controllers. Tests: existing exercise tests still pass. |

**Sub-batch 4C: Query patterns and validation**

| ID | Title | Files | Test Requirement |
|----|-------|-------|-----------------|
| CD-M02 | Add `.AsNoTracking()` to read-only queries | Multiple services | No new tests — performance improvement. Verify existing tests pass (AsNoTracking changes EF tracking behavior, could surface hidden bugs). |
| CD-M06 | Standardize org-scoping pattern | Multiple services | Test: verify explicit org-filter is present in key service queries (can be a code-level assertion in tests or a grep-based verification). |
| CD-M07 | Decompose large service classes | `InjectService.cs`, `ExerciseService.cs` | Extract `InjectApprovalService`, `ExerciseCloneService`. Tests: move relevant tests to new test classes, verify all pass. |
| AR-M02 / CD-M08 | Create FluentValidation validators | New validator files | Test per validator: valid input passes, each invalid field fails with correct error message and field name. Priority validators: `CreateInjectRequestValidator`, `UpdateInjectRequestValidator`, `CreateExerciseRequestValidator`. |

**Sub-batches 4A, 4B, 4C should be executed sequentially** (4B depends on services created in 4A, 4C builds on the decomposed services from 4B).

**Commit messages:**
- `fix(services): register metrics DI, extract observation projection, bulk notifications, invitation activation`
- `fix(controllers): extract business logic to Core services, decompose ExercisesController`
- `fix(data): AsNoTracking on reads, org-scoping standardization, service decomposition, FluentValidation`

---

### Phase 5: Major Frontend Features & Patterns

**Priority:** Frontend functional fixes and pattern standardization.

**Frontend agent scope:**

| ID | Title | Files | Test Requirement |
|----|-------|-------|-----------------|
| FF-M01 | Fix wrong query key in useExcelImport | `useExcelImport.ts` | Test: `onSuccess` callback invalidates using the correct query key (mock queryClient, verify `invalidateQueries` called with key matching `injectKeys.all(exerciseId)`). |
| FF-M02 | Fix notification optimistic update query key | `useNotifications.ts` | Test: optimistic update targets correct cache entry regardless of pagination params. |
| FF-C01 | Refactor UserListPage to use React Query | `UserListPage.tsx`, new `useUsers.ts` | Test: `useUsers` hook fetches users via React Query. Test: activate/deactivate mutations invalidate user list query. Test: loading state renders Loading component. |
| FF-M03 | Standardize data fetching patterns | Audit remaining features | No new tests if just wiring existing service calls through React Query hooks. Verify compiles. |
| AR-M01 | Add missing `modifiedBy` field to frontend InjectDto | `types/index.ts` | No test — type addition. Verify compiles. |

**Backend agent scope (can run in parallel):**

| ID | Title | Files | Test Requirement |
|----|-------|-------|-----------------|
| AC-M02 | Fix GetUserMemberships missing admin auth | `UsersController.cs` | Test: non-admin user querying another user's memberships gets 403. Test: admin user gets 200. Test: user querying own memberships gets 200. |
| AC-M03 | Replace string literal with `[AuthorizeAdmin]` | `SystemSettingsController.cs` | No test — attribute swap. Verify compiles. |
| AC-M06 | Replace custom auth in AutocompleteController | `AutocompleteController.cs` | Test: user not in exercise gets 403. Verify existing autocomplete tests pass. |
| AC-M07 | Add role gate to Capabilities write endpoints | `CapabilitiesController.cs` | Test: OrgUser calling POST/PUT/DELETE gets 403. Test: OrgAdmin gets 200. |

**Commit messages:**
- `fix(frontend): query key fixes, UserListPage React Query migration, InjectDto alignment`
- `fix(controllers): authorization gaps in Users, SystemSettings, Autocomplete, Capabilities`

---

### Phase 6: Minor Cleanup (Batch)

**Priority:** Low-risk improvements. Batch into a single commit per domain.

**Frontend agent scope:**

| ID | Title | Test? |
|----|-------|-------|
| FI-N01 | Remove unused `_tokenExpiry` state | No |
| FI-N02 | Replace deprecated `PaperProps` with `slotProps` | No |
| FI-N03 | Replace imperative fetch with React Query in ProfileMenu | Test: hook returns data correctly |
| FI-N04 | Use layout route for admin routes | No — verify compiles + routes still work |
| FF-N01 | Standardize query key factories | No — refactor only |
| FF-N02 | Add missing loading states | No — visual change |
| FF-N04 | Standardize mutation error handling | No — pattern change |

**Backend agent scope:**

| ID | Title | Test? |
|----|-------|-------|
| AC-N01 | Remove duplicate GetCurrentUserId (already fixed by AC-M01) | Verify compiles |
| AC-N02 | Move VersionInfo to DTOs directory | No |
| AC-N03 | Replace StatusCode(500) with ProblemDetails | No |
| AC-N05 | Add [Authorize] to ExerciseHub | Test: unauthenticated connection rejected |
| CD-N01 | Standardize mapper patterns | No |
| CD-N02 | Resolve TODO/FIXME comments | No — audit and clean |
| CD-N06 | Audit exception swallowing | Test: re-thrown exceptions propagate |
| AR-N02 | Add mapper to Feedback feature | Test: mapper maps all fields |

**Deferred items (not for this pass):**
| ID | Title | Reason |
|----|-------|--------|
| AR-M05 | Migrate tests from InMemory to SQL Server | Large infrastructure change — needs its own planning pass |
| CD-N04 | Extract entity configs from DbContext | Low risk, high churn — defer to next pass |
| CD-N07 | Add composite indexes | Needs query profiling data from production first |
| FF-N03 | Remove commented-out service methods | Needs manual audit — low priority |
| FF-N05 | Move cross-feature imports to shared types | Defer — may cause circular dependency issues |
| FF-N06 | Remove over-exported components | Defer — needs usage analysis |
| AC-N04 | Align export route path | Would break frontend URLs — needs coordinated change |
| AC-N06 | Replace regex body sanitization | Working correctly, just fragile — defer |
| CD-N03 | Remove unused navigation properties | Needs careful analysis of cascade delete configs |
| CD-N05 | Document seeder ordering | Documentation only — no code change |

**Commit messages:**
- `fix(frontend): minor cleanup — remove dead state, deprecation fixes, loading states, layout routes`
- `fix(backend): minor cleanup — ProblemDetails, hub auth, mapper consistency, exception handling`

---

## Verification Gates

### After Each Phase

Run all four checks. ALL must pass before proceeding.

```bash
# 1. Backend build
dotnet build src/Cadence.WebApi/Cadence.WebApi.csproj

# 2. Backend tests
dotnet test src/Cadence.Core.Tests/Cadence.Core.Tests.csproj

# 3. Frontend type-check (does not kill dev server)
cd src/frontend && npm run type-check

# 4. Frontend tests
cd src/frontend && npm run test -- --run
```

### After All Phases (Final Gate)

```bash
# Full clean build
dotnet clean && dotnet build

# Full test suite
dotnet test

# Frontend production build check
cd src/frontend && npm run build:check

# Frontend tests with coverage
cd src/frontend && npm run test -- --run --coverage

# Count new tests added (compare to baseline)
# Backend: dotnet test --list-tests | wc -l
# Frontend: npm run test -- --run --reporter=verbose 2>&1 | grep -c "✓"
```

### Gate Failure Protocol

If a gate fails:

1. **Read the error output** — is it a test failure, build error, or type error?
2. **Diagnose the root cause** — did the current phase introduce it, or was it pre-existing?
3. **If introduced by current phase:** Dispatch a targeted fix agent for just the failing issue. Do not proceed to the next phase.
4. **If pre-existing:** Document it as a known issue, skip the affected test with a clear reason (`[Fact(Skip = "Pre-existing: CD-N06")]`), and proceed.
5. **Re-run the full gate** after any fix.

---

## Agent Dispatch Patterns

### Backend Agent

```
Agent type: backend-agent
Expertise: .NET 10, EF Core 10, ASP.NET Core, C#, xUnit

For each issue assigned:
1. Read the review report entry (exact file paths, line numbers, description)
2. Read the affected source files
3. Read existing tests for context
4. Write tests FIRST (TDD)
5. Implement the fix
6. Verify tests pass: dotnet test
7. Report completion with: files changed, tests added, any complications
```

### Frontend Agent

```
Agent type: frontend-agent
Expertise: React 19, TypeScript 5, MUI 7, Vitest, React Testing Library

For each issue assigned:
1. Read the review report entry
2. Read the affected source files
3. Read COBRA_STYLING.md if it's a convention fix
4. Write tests FIRST for functional fixes (TDD)
5. Implement the fix
6. Verify: npm run type-check && npm run test -- --run
7. Report completion with: files changed, tests added, any complications

CRITICAL CONVENTIONS (from CLAUDE.md):
- NEVER import raw MUI components — use COBRA wrappers
- NEVER use MUI icons — use FontAwesome
- NEVER use toast directly — use notify wrapper
- Use npm run type-check, NOT npm run build (don't kill dev server)
```

### Orchestrator Prompts

When dispatching an agent, include:

```
You are working on maintenance/code-hardening-v1. This is a code hardening pass.

YOUR ASSIGNED ISSUES:
{paste the specific issue table for this agent from the phase}

CONTEXT:
- Read docs/reviews/SUMMARY.md for the full review context
- Read the specific review file for detailed issue descriptions
- Read CLAUDE.md for project conventions

TESTING REQUIREMENT:
{paste the test requirement column for each issue}

POSITIVE PATTERNS TO PRESERVE:
{paste relevant positive patterns from SUMMARY.md that apply to this agent's scope}

VERIFICATION:
After completing all fixes, run:
{paste the appropriate verification commands}

Report back with:
1. Files modified (list)
2. Files created (list)
3. Tests added (count and names)
4. Any issues discovered during fixing that weren't in the original review
5. Any deviations from the recommended fix approach (and why)
```

---

## Commit Strategy

One commit per phase (or sub-batch for Phase 4). Each commit message follows the project's convention:

```
fix({scope}): {brief description}

{Bullet list of issue IDs resolved}

Resolves: AC-C05, AR-C01, AC-C02, AC-C06, AC-C04, AC-C01, CD-C04
Tests added: {count}

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
```

After all phases complete, the orchestrator creates a PR:

```bash
gh pr create \
  --base main \
  --head maintenance/code-hardening-v1 \
  --title "fix: code hardening pass 1 — security, stability, conventions" \
  --body "$(cat <<'EOF'
## Summary

Code hardening addressing 75 issues from code review pass 1 (docs/reviews/SUMMARY.md).

### Changes by Phase
- **Phase 1 (Security):** CORS env-gating, exercise authorization on 15 endpoints, hub identity fix, audit trail user IDs, delete cascades
- **Phase 2 (Stability):** Shared ClaimsPrincipal extensions, single SignalR broadcast, batched imports, exception handling, dev-only logger
- **Phase 3 (Frontend Convention):** COBRA compliance across 6 components, theme token usage, deduplicated theme overrides
- **Phase 4 (Backend DRY):** Metrics DI, observation projection extraction, bulk EF operations, FluentValidation, controller-to-service extraction
- **Phase 5 (Feature Fixes):** Query key fixes, UserListPage React Query migration, authorization gaps in 4 controllers
- **Phase 6 (Cleanup):** Dead code removal, deprecation fixes, loading states, mapper consistency

### Test Impact
- **New backend tests:** {count}
- **New frontend tests:** {count}
- **Pre-existing test regressions:** 0

### Issues Deferred to Pass 2
- AR-M05: InMemory → SQL Server test migration
- CD-N04: Extract entity configs from DbContext
- CD-N07: Composite index optimization
- AC-N04: Export route path alignment
- AC-N06: JSON-based body sanitization

## Test Plan
- [ ] All backend tests pass
- [ ] All frontend tests pass
- [ ] Frontend type-check clean
- [ ] Manual smoke test: exercise CRUD, inject firing, Excel import
- [ ] Verify CORS rejects unknown origins in non-dev

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

---

## Positive Patterns — Do Not Regress

Fix agents MUST preserve these patterns. If a fix would break one of these, stop and consult the orchestrator.

| Pattern | Rule |
|---------|------|
| Core/WebApi separation | Business logic stays in Core. Controllers only delegate. |
| React Query cache invalidation via SignalR | Never mutate cache directly from SignalR events. Always invalidate. |
| Organization validation interceptor | Do not bypass. Do not disable global query filters without comment. |
| Feature module directory structure | New files follow `Features/{Name}/Services/`, `features/{name}/hooks/`. |
| Static pure-function mappers | No dependencies in mapper classes. No side effects. |
| Secure token strategy | Access tokens in memory. Refresh tokens in httpOnly cookies. Single-flight refresh. |
| `notify` wrapper | No direct `toast` imports anywhere. |
| FontAwesome icons | No MUI icon imports anywhere. |
| COBRA styled components | No raw MUI component imports for styled elements. |
| `IgnoreQueryFilters()` requires comment | Every bypass must explain why. |

---

## Estimated Effort

| Phase | Backend Effort | Frontend Effort | Risk Level |
|-------|---------------|----------------|------------|
| Phase 1: Security | 2-3 hours | None | Medium (auth attribute changes) |
| Phase 2: Shared Infra | 2-3 hours | 1-2 hours | Medium (exception handler reorder) |
| Phase 3: Frontend Convention | None | 1 hour | Low (style-only changes) |
| Phase 4: Backend DRY | 4-6 hours | None | **High** (controller refactoring) |
| Phase 5: Feature Fixes | 1-2 hours | 2-3 hours | Medium (React Query migrations) |
| Phase 6: Cleanup | 1 hour | 1 hour | Low (minor fixes) |
| **Total** | **~12 hours** | **~6 hours** | |

Phase 4 is the highest risk due to the controller-to-service extraction. The orchestrator should review the diffs carefully before committing and consider splitting into smaller sub-batches if needed.
