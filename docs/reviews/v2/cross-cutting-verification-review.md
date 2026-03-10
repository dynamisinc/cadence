# Cross-Cutting Verification Review — Pass 2

> **Date:** 2026-03-10
> **Reviewer:** Cross-Cutting Verification Specialist
> **Scope:** Architecture patterns, test coverage, cross-layer consistency, build and test gates
> **Branch:** main (post hardening pass 1 merge)
> **Reference documents:** docs/reviews/SUMMARY.md, docs/reviews/architecture-review.md, docs/reviews/FIX-AGENT-PROMPT.md

---

## Positive Pattern Verification

All 28 patterns from `docs/reviews/SUMMARY.md` "Positive Patterns to Preserve" were verified against the current codebase.

| ID | Pattern | Status | Evidence |
|----|---------|--------|---------|
| AR-P01 | Core/WebApi separation (IExerciseHubContext in Core, impl in WebApi) | PASS | `IExerciseHubContext.cs` in `Cadence.Core/Hubs/` has zero SignalR imports. `ExerciseHubContext.cs` in `Cadence.WebApi/Hubs/` holds the implementation. Separation clean. |
| AR-P02 | React Query invalidation via SignalR (never direct state mutation) | **FAIL** | `ExerciseConductPage.tsx` lines 244-300 use `queryClient.setQueryData` in ALL 6 SignalR handlers. Violates the documented rule. See XV-M01. |
| AR-P03 | Organization validation interceptor registered in DI | PASS | `OrganizationValidationInterceptor.cs` is registered with proper singleton/scoped pattern. Validates `IOrganizationScoped` entities on every write. |
| AR-P04 | Feature module consistency | PASS | New features (`Feedback/Mappers/`, `Validators/`) follow the golden path. `FeedbackMapper.cs` created (resolving AR-N02). Validators follow `Features/{Name}/Validators/` pattern. |
| AR-P05 | InjectDto type alignment across layers | PASS | Frontend `InjectDto` line 76 includes `modifiedBy: string | null`, matching backend `string? ModifiedBy`. All fields aligned. |
| AR-P06 | Secure token strategy (memory + httpOnly cookies, single-flight refresh) | PASS | `api.ts` uses `refreshPromise` deduplication. `AuthContext.tsx` stores tokens in React state only. Pattern intact. |
| FI-P01 | Single-flight token refresh with concurrent 401 deduplication | PASS | `api.ts` checks `if (!refreshPromise)` before starting new refresh. `_retry` flag prevents infinite loops. |
| FI-P02 | Offline auth preservation | PASS | `AuthContext.tsx` `classifyError` distinguishes network errors from auth failures. On network error, auth state preserved. `getCachedUserInfo()` restores from localStorage for offline. |
| FI-P03 | `notify` wrapper with time-window deduplication | PASS | `notify.ts` implements `DEDUP_WINDOW_MS = 3000` via `recentToasts` Map. Only one direct `toast` import in codebase (in `notify.ts` itself — correct). |
| FI-P04 | `useConfirmDialog` imperative Promise pattern | PASS | Returns `confirm()` function creating `Promise<boolean>`. Pattern intact and unchanged. |
| FI-P05 | Two-phase offline sync | PASS | `syncService.ts` exists with ordered action processing. |
| FI-P06 | `onReconnectedRef` pattern prevents stale closure in SignalR | PASS | `useExerciseSignalR.ts`: `useRef(onReconnected)` updated in `useEffect`, called via `.current?.()` on reconnect. |
| FI-P07 | `ProtectedRoute` allows offline access without redirect | PASS | If `!isAuthenticated && !isApiReachable`, renders children without redirecting. |
| FF-P01 | Hook-based data fetching with React Query in mature features | PASS | New `useUsers.ts` properly uses `useQuery`/`useMutation`. `useExcelImport.ts` uses `invalidateQueries`. |
| FF-P02 | Consistent feature module directory structure | PASS | All new features follow golden path structure. |
| FF-P03 | Type-safe service layer with explicit return types | PASS | New service files have explicit return type annotations. |
| FF-P04 | `notify` wrapper used consistently | PASS | No direct `toast` imports outside `notify.ts`. Convention scan confirmed. |
| FF-P05 | FontAwesome icons used consistently | PASS | Zero files import from `@mui/icons-material`. Convention scan confirmed. |
| AC-P01 | Authorization policy infrastructure | PASS | `ExerciseAccessHandler.cs`, `ExerciseRoleHandler.cs`, `AuthorizeAttributes.cs`, `PolicyNames.cs` all exist. 14 controllers reference `[ExerciseAccess]` or `[ExerciseRole]`. |
| AC-P02 | Rate limiting on authentication endpoints | PASS | `Program.cs`: `auth` policy (10 req/min/IP) and `password-reset` policy (3 req/15min/IP) registered. |
| AC-P03 | Structured Serilog context middleware | PASS | Registered after `UseAuthentication`, before `UseAuthorization`. |
| AC-P04 | AuthController delegation pattern | PASS | AuthController delegates to `IAuthenticationService`. Pattern intact. |
| AC-P05 | Request/response audit logging middleware | PASS | `RequestResponseLoggingMiddleware.cs` exists and registered. |
| AC-P06 | Clean hub group management | PASS | `ExerciseHub.cs` has `[Authorize]`. `JoinUserGroup()` uses `Context.User` claims. Group names use consistent patterns. |
| CD-P01 | Every `IgnoreQueryFilters()` has explanatory comment | PASS | All 15+ production `IgnoreQueryFilters()` calls have inline comments. Test files use it for assertions (expected). |
| CD-P02 | `ExecuteDeleteAsync` for bulk cascade deletes | PASS | `ExerciseDeleteService.cs` uses bulk `ExecuteDeleteAsync` patterns. |
| CD-P03 | Two-phase AsNoTracking-then-tracking query pattern | PASS | `InjectReadinessService.cs` uses `.AsNoTracking()` for read phase. |
| CD-P04 | Static pure-function mapper classes | PASS | All mapper classes are `static` with no constructor injection. `FeedbackMapper.cs` follows the pattern. |

### Pattern Summary

- **Passed: 27/28**
- **Failed: 1/28** (AR-P02 — React Query cache invalidation via SignalR)

---

## AR-P02 Regression Detail

**File:** `src/frontend/src/features/exercises/pages/ExerciseConductPage.tsx`, lines 243-300

All six SignalR event handlers use `queryClient.setQueryData` to directly mutate the React Query cache:

- `handleInjectFired` — direct `setQueryData`
- `handleInjectStatusChanged` — direct `setQueryData`
- `handleClockChanged` — direct `setQueryData`
- `handleObservationAdded` — direct `setQueryData`
- `handleObservationUpdated` — direct `setQueryData`
- `handleObservationDeleted` — direct `setQueryData`

The documented pattern (SUMMARY.md AR-P02) requires `queryClient.invalidateQueries` instead. The direct mutation approach was likely chosen for exercise conduct performance (avoiding full refetch on every status change during a live exercise), but this diverges from the documented pattern without an explanatory comment.

**Risk:** Local state can diverge from server state during reconnection scenarios or concurrent updates. The `handleObservationAdded` handler includes deduplication logic that depends on local state being in sync — not guaranteed with direct mutation.

**Fix options:**
1. (Preferred) Replace `setQueryData` with `invalidateQueries` for consistency
2. (If performance-critical) Document the intentional deviation with a block comment + add reconciliation `invalidateQueries` in the reconnect handler

---

## Test Coverage Audit

### New Tests Added During Hardening

| Test File | Tests Added | Covers Issue |
|-----------|-------------|-------------|
| `Cadence.WebApi.Tests/Extensions/ClaimsPrincipalExtensionsTests.cs` | 20 | AC-M01 |
| `Cadence.WebApi.Tests/Middleware/ExceptionHandlerTests.cs` | 1 (skipped) | AR-M03 + AC-M10 |
| `Cadence.WebApi.Tests/Controllers/AdminOrganizationsControllerIntegrationTests.cs` | 17 | AC-C02 |
| `Cadence.WebApi.Tests/Controllers/AuthControllerIntegrationTests.cs` | 18 | Auth controller |
| `Cadence.Core.Tests/Features/Injects/InjectServiceTests.cs` | 48 | CD-C03 |
| `Cadence.Core.Tests/Features/Injects/InjectCrudServiceTests.cs` | 25 | Inject CRUD + validators |
| `Cadence.Core.Tests/Features/BulkParticipantImport/BulkParticipantImportServiceTests.cs` | 14 | CD-C02 |
| `Cadence.Core.Tests/Features/BulkParticipantImport/BulkParticipantImportSessionTests.cs` | 2 | CD-C01 |
| `Cadence.Core.Tests/Features/Users/GetUserMembershipsAuthorizationTests.cs` | 4 | AC-M02 |
| `Cadence.Core.Tests/Features/Authorization/AutocompleteOrgAccessTests.cs` | 5 | AC-M06 |
| `Cadence.Core.Tests/Features/Capabilities/CapabilitiesOrgAccessTests.cs` | 4 | AC-M07 |
| `Cadence.Core.Tests/Features/Exercises/ExerciseDeleteServiceTests.cs` | 16 | CD-C04 |
| `Cadence.Core.Tests/Security/OrganizationIsolationTests.cs` | 10 | Multi-tenancy |
| `Cadence.Core.Tests/Features/Notifications/ApprovalNotificationServiceTests.cs` | 15 | Approval notifications |
| `Cadence.Core.Tests/Features/Exercises/ExerciseApprovalQueueTests.cs` | 6 | Approval queue |
| `Cadence.Core.Tests/Features/Exercises/ExerciseApprovalSettingsServiceTests.cs` | 13 | Approval settings |
| `frontend/src/core/utils/logger.test.ts` | 6 | AR-M04/FI-M01 |
| `frontend/src/core/utils/networkErrors.test.ts` | 15 | FI-M04 |
| `frontend/src/features/users/hooks/useUsers.test.ts` | 12 | FF-C01 |

**Estimated total new tests:** ~251 (backend: ~218, frontend: ~33)

### Test Counts

- **Backend (`Cadence.Core.Tests`):** ~1,034 `[Fact]` methods across 57 test files
- **Backend (`Cadence.WebApi.Tests`):** ~57 `[Fact]` methods across 5 test files
- **Total backend:** ~1,091 test methods
- **Frontend:** ~130 test files (.test.ts + .test.tsx)

### Test Naming Convention Compliance

**Backend (C#) — PASS**

All new tests follow `{Method}_{Scenario}_{Result}`:
```csharp
GetUserId_ValidNameIdentifierClaim_ReturnsUserId()
GetUserId_MissingClaim_ThrowsUnauthorizedAccessException()
DeleteExercise_WithMselAndInjects_CascadesSoftDeleteToAllChildren()
AcceptInvitationAsync_PendingUser_ActivatesUser()
```

**Frontend (TypeScript) — PASS**

New tests follow `describe/it`:
```typescript
describe('devLog', () => { it('calls console.log in development mode') })
describe('isNetworkError', () => { it('returns true for ERR_NETWORK code') })
describe('useUserList', () => { it('fetches users via React Query') })
```

### Fixes Missing Required Tests

| Fix ID | Expected Test per Prompt | Actual | Status |
|--------|--------------------------|--------|--------|
| AC-C05/AR-C01 (CORS) | Non-dev rejects unknown origins; dev allows any | No dedicated CORS test | **GAP** |
| AC-C06 (Hub identity) | `JoinUserGroup` uses `Context.UserIdentifier` | No dedicated hub unit test | **GAP** |
| AC-C04 (isAdmin) | Non-admin delete doesn't get admin privileges | No dedicated test for this case | **GAP** |
| AR-M02/CD-M08 (Validators) | Test per validator: valid passes, invalid fails | No `*ValidatorTests.cs` files found | **GAP** |
| AR-M03+AC-M10 (Exception handler) | Exception returns structured response | 1 test, SKIPPED (`ExceptionHandlerTests.cs`) | **SKIPPED** |
| AC-C01 (SystemUserIdString) | Mutation records authenticated user's ID | Partially covered by integration tests | PARTIAL |
| CD-C03 (Double broadcast) | Approval triggers exactly one broadcast | `InjectServiceTests.cs` — mock verify | PRESENT |
| CD-C02 (N+1 batching) | 10-row import calls SaveChanges ≤3 times | `BulkParticipantImportServiceTests.cs` — 14 tests | PRESENT |
| CD-C04 (Delete cascade) | Cascades to EegEntry, Photo, CriticalTask | `ExerciseDeleteServiceTests.cs` — 16 tests | PRESENT |
| devLog utility | Emits in dev, silent in prod | `logger.test.ts` — 6 tests | PRESENT |
| isNetworkError | Each error pattern tested | `networkErrors.test.ts` — 15 tests | PRESENT |
| useUsers hook | Fetches via React Query, mutations, loading | `useUsers.test.ts` — 12 tests | PRESENT |

**Summary:** 4 test gaps (CORS, hub identity, isAdmin, validators) + 1 skipped test (exception handler).

---

## Cross-Layer Consistency

### DTO Alignment

| Entity | Backend | Frontend | Match | Notes |
|--------|---------|----------|-------|-------|
| InjectDto | 43 fields incl. `ModifiedBy` | `modifiedBy: string | null` | PASS | AR-M01 resolved — fully aligned |
| ExerciseDto | Stable | Stable | Not fully audited | No changes during hardening |
| ObservationDto | Stable | Stable | Not fully audited | No changes during hardening |
| UserDto | Stable | New `useUsers.ts` uses it | Not fully audited | New hook references existing types |

**The critical DTO (InjectDto) is now 100% aligned.** No other DTO alignment issues found.

### API Route Alignment

| Feature | Frontend Service URL | Backend Controller Route | Match |
|---------|---------------------|------------------------|-------|
| Exercises CRUD | `/exercises`, `/exercises/{id}` | `api/exercises`, `api/exercises/{id:guid}` | PASS |
| Injects list | `/exercises/{id}/injects` | `api/exercises/{id}/injects` | PASS |
| Auth endpoints | `/auth/login`, `/auth/refresh` | `api/auth/login`, `api/auth/refresh` | PASS |
| Exercise settings | `/exercises/{id}/settings` | `api/exercises/{id:guid}/settings` | PASS |

No route alignment gaps found.

---

## Build and Test Gate Results

> Gates executed on 2026-03-10 during review.

| Gate | Result | Details |
|------|--------|---------|
| `dotnet build` | **PASS** | 0 errors, 0 warnings |
| `dotnet test` | **PASS** | 1,263 passed, 10 skipped, 0 failed |
| `npm run type-check` | **PASS** | Clean — no type errors |
| `npm run test -- --run` | **PASS** | 2,885 passed, 14 skipped, 0 failed (192 test files) |

---

## New Issues

### XV-M01: AR-P02 Regression — SignalR Handlers Directly Mutate React Query Cache

**Severity:** Major | **Confidence:** 100

**File:** `src/frontend/src/features/exercises/pages/ExerciseConductPage.tsx`, lines 243-300

All 6 SignalR event handlers (`handleInjectFired`, `handleInjectStatusChanged`, `handleClockChanged`, `handleObservationAdded`, `handleObservationUpdated`, `handleObservationDeleted`) use `queryClient.setQueryData` instead of `queryClient.invalidateQueries`. This directly violates documented positive pattern AR-P02.

**Fix:** Replace `setQueryData` with `invalidateQueries`, or document the intentional deviation with performance rationale + add reconciliation invalidation in reconnect handler.

### XV-M02: FluentValidation Validators Lack Tests

**Severity:** Major | **Confidence:** 90

**Files:** `CreateInjectRequestValidator.cs`, `UpdateInjectRequestValidator.cs`, `CreateExerciseRequestValidator.cs`

Three validator classes created with no corresponding test files. FIX-AGENT-PROMPT.md Phase 4C explicitly required: "Test per validator: valid input passes, each invalid field fails with correct error message and field name."

**Fix:** Create `InjectValidatorTests.cs` and `ExerciseValidatorTests.cs` covering valid/invalid inputs.

### XV-M03: Exception Handler Still After Auth Middleware in Pipeline

**Severity:** Major | **Confidence:** 90

**File:** `src/Cadence.WebApi/Program.cs`, line 385

The global exception handler is registered after `UseAuthentication` (362), `UseAuthorization` (369), and `UseSerilogRequestLogging` (372). The `HasStarted` guard (AC-M10) was added, but AR-M03 (placement) remains unresolved. Exceptions from auth middleware produce unstructured 500 responses.

**Fix:** Move exception handler to immediately after `UseHttpsRedirection`, before all other middleware.

### XV-N01: ~~Build/Test Gates Not Directly Executed~~ — RESOLVED

Gates were executed during review finalization. All 4 gates pass. See Build and Test Gate Results above.

### XV-N02: `UpdatePrompt.tsx` Raw MUI Button (COBRA Violation)

**Severity:** Minor | **Confidence:** 85

**File:** `src/frontend/src/core/components/UpdatePrompt.tsx`, line 16

Imports `Button` directly from `@mui/material`. Not in original pass 1 scope.

**Fix:** Replace with `CobraPrimaryButton`/`CobraSecondaryButton`.

### XV-N03: `useUpdateUser` Uses `setQueryData` in Mutation Callback

**Severity:** Minor (informational) | **Confidence:** 80

**File:** `src/frontend/src/features/users/hooks/useUsers.ts`, line 77

Uses `setQueryData` to update detail cache on mutation success. Unlike AR-P02 (SignalR handlers), this is from a trusted server response in a mutation `onSuccess`. This is standard React Query pattern and is not a bug — flagged for documentation consistency only.

**Recommendation:** Add comment clarifying this is intentional eager update from server response (distinct from AR-P02 SignalR prohibition).

---

## Hardening Pass 1 Assessment Summary

### Successfully Fixed
- CORS environment-gating
- ExerciseHub identity fix + [Authorize]
- ClaimsPrincipalExtensions (20 tests)
- Exercise delete cascade coverage
- devLog/devWarn utility (6 tests)
- isNetworkError extraction (15 tests)
- ConnectionState deduplication
- COBRA button compliance (3 original files)
- useUsers React Query migration (12 tests)
- FluentValidation validators (3 created)
- InjectDto alignment
- ExercisesController decomposition
- Metrics DI registration
- Observation projection extraction
- Notification bulk operations
- Invitation flow user activation

### Remaining Open
1. **XV-M01** — AR-P02 regression in ExerciseConductPage (6 handlers)
2. **XV-M02** — Validator tests missing
3. **XV-M03** — Exception handler placement (AR-M03 not fully resolved)
4. **BV-003** — ExcelExportController POST missing exercise auth
5. **BV-004** — ObservationsController missing exercise scoping

---

> **Report generated by:** Cross-Cutting Verification Specialist (Pass 2)
