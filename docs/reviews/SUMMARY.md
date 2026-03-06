# Code Review Summary — Pass 1

> **Date:** 2026-03-06
> **Branch:** maintenance/code-review-v1
> **Reviewers:** 5 (Frontend Infrastructure, Frontend Features, API & Controllers, Core Domain & Data, Architecture)

---

## Aggregate Metrics

| Reviewer | Files Reviewed | Critical | Major | Minor | Positive |
|----------|---------------|----------|-------|-------|----------|
| Frontend Infrastructure | 50 | 2 | 5 | 4 | 7 |
| Frontend Features | 114 | 2 | 7 | 6 | 5 |
| API & Controllers | 54 | 6 | 10 | 6 | 6 |
| Core Domain & Data | 160+ | 4 | 8 | 7 | 4 |
| Architecture | 42 | 1 | 5 | 2 | 6 |
| **Total** | **420+** | **15** | **35** | **25** | **28** |

## Issues by Category

| Category | Critical | Major | Minor | Total |
|----------|----------|-------|-------|-------|
| Stability | 8 | 10 | 4 | 22 |
| Convention | 3 | 7 | 5 | 15 |
| DRY | 1 | 6 | 2 | 9 |
| FileSize | 0 | 4 | 1 | 5 |
| Orphaned | 0 | 1 | 5 | 6 |
| Readability | 1 | 2 | 2 | 5 |
| **Total** | **13** | **30** | **19** | **62** |

> Note: Some issues span multiple categories. The dominant category was used for classification.
> Two architecture issues (AR-C01 and AR-M04) overlap with issues found independently by other reviewers (AC-C05, FI-M01). These are counted once in the category table but tracked as cross-reviewer patterns below.

---

## Top 10 Most Impactful Issues

> Ranked by blast radius (how many files/features affected) and severity.
> These are the issues that fix agents should tackle first.

| Rank | ID | Title | Category | Blast Radius | Source Report |
|------|----|-------|----------|-------------|--------------|
| 1 | AC-C05 / AR-C01 | CORS allows all origins in production | Stability | All endpoints, all environments | api-controllers-review.md, architecture-review.md |
| 2 | AC-C02 | Missing exercise-scoped authorization on 15 endpoints | Stability | 7 controllers, 15 endpoints | api-controllers-review.md |
| 3 | AC-C01 | Hardcoded `SystemConstants.SystemUserIdString` in mutation endpoints | Stability | 5 controllers, all audit trails | api-controllers-review.md |
| 4 | CD-C01 | Static in-memory session state breaks multi-instance deployments | Stability | 2 import services, horizontal scaling | core-domain-review.md |
| 5 | AR-M04 / FI-M01 | Production debug `console.log` (96 occurrences across 16 files) | Readability | 16 files, all environments | architecture-review.md, frontend-infrastructure-review.md |
| 6 | AC-C03 | Business logic and direct `AppDbContext` in 6 controllers | Convention | 6 controllers, Core/WebApi separation | api-controllers-review.md |
| 7 | CD-C03 | Double SignalR broadcast for all approval actions | Stability | All connected clients, inject workflow | core-domain-review.md |
| 8 | CD-C02 | N+1 `SaveChangesAsync` in bulk import per-row loop | Stability | Bulk import feature, database performance | core-domain-review.md |
| 9 | CD-C04 | Exercise delete missing EEG, photo, and critical task cascades | Stability | Orphaned data on exercise deletion | core-domain-review.md |
| 10 | AC-C06 | `ExerciseHub.JoinUserGroup` accepts client-supplied user ID | Stability | SignalR impersonation vector | api-controllers-review.md |

---

## Cross-Reviewer Patterns

> Issues that appeared independently in multiple reviewers' findings.

### Pattern 1: CORS Wildcard in Production
- **Found by:** API & Controllers (AC-C05), Architecture (AR-C01)
- **Description:** `SetIsOriginAllowed(_ => true)` with `AllowCredentials()` runs in all environments with no `IsDevelopment()` guard. Combined with cookie-based refresh tokens, this allows any attacker site to make credentialed cross-origin requests.
- **Recommended unified fix:** Gate behind `builder.Environment.IsDevelopment()`. For non-dev environments, read `Cors:AllowedOrigins` from configuration. Single fix in `Program.cs`, add `Cors` section to `appsettings.json`.

### Pattern 2: Production Console Logging
- **Found by:** Frontend Infrastructure (FI-M01), Frontend Features (FF-C02), Architecture (AR-M04)
- **Description:** 96+ `console.log` statements across 16 frontend files ship to production, including sensitive auth data in `AuthContext.tsx` (40 logs), `OrganizationContext.tsx` (13 logs), `api.ts` (13 logs), and `authService.ts` (5 logs). Not gated behind any development flag.
- **Recommended unified fix:** Create `src/frontend/src/core/utils/logger.ts` with `devLog`/`devWarn` utilities gated on `import.meta.env.DEV`. Global find-and-replace `console.log` → `devLog` across all affected files. Preserve `console.error` for genuine production errors.

### Pattern 3: Missing FluentValidation Implementations
- **Found by:** Architecture (AR-M02), Core Domain & Data (CD-M08)
- **Description:** FluentValidation is registered in DI via `AddValidatorsFromAssemblyContaining<AppDbContext>()` but zero `AbstractValidator<T>` implementations exist in the codebase. Validation is performed ad-hoc in controllers (private methods) or missing entirely. This means the validation infrastructure is wired but unused.
- **Recommended unified fix:** Create validators for the most critical DTOs first: `CreateInjectRequestValidator`, `UpdateInjectRequestValidator`, `CreateExerciseRequestValidator`. Follow the FluentValidation pattern. Inject `IValidator<T>` in controllers/services. Delete ad-hoc validation methods in controllers.

### Pattern 4: DRY — User ID Extraction Duplicated Across Controllers
- **Found by:** API & Controllers (AC-M01, AC-M08)
- **Description:** `GetCurrentUserId()` is copy-pasted in 12+ controllers with slight variations. If the claim name changes, all copies must be updated.
- **Recommended unified fix:** Create `src/Cadence.WebApi/Extensions/ClaimsPrincipalExtensions.cs` with `GetUserId()` and `GetOrganizationId()` extension methods. Replace all controller-local implementations with `User.GetUserId()`.

### Pattern 5: Raw MUI Button Imports (COBRA Violation)
- **Found by:** Frontend Infrastructure (FI-C01), Frontend Features (FF-M04, FF-M05)
- **Description:** Three components import raw `Button` from `@mui/material` instead of using COBRA button variants: `ProfileMenu.tsx`, `PendingApprovalAlert.tsx`, `VersionInfoCard.tsx`. This violates the project's core styling convention.
- **Recommended unified fix:** Replace each raw `Button` import with the appropriate COBRA variant (`CobraPrimaryButton`, `CobraSecondaryButton`, or `CobraLinkButton`). If the header button styling in `ProfileMenu` doesn't fit existing COBRA variants, create a new `CobraHeaderButton` styled component.

### Pattern 6: Hardcoded Hex Colors (COBRA Violation)
- **Found by:** Frontend Infrastructure (FI-C02, FI-M02)
- **Description:** `ConnectionStatusIndicator.tsx` and `OrganizationSwitcher.tsx` use hardcoded hex color strings instead of theme palette tokens. These colors won't adapt to theme changes.
- **Recommended unified fix:** Replace all hardcoded hex values with `theme.palette.*` references via `useTheme()`. Map status colors to semantic palette tokens (`success.main`, `warning.main`, `error.main`).

---

## Recommended Remediation Order

> For consumption by fix-agent swarm. Ordered by: (1) critical first, (2) shared patterns before isolated issues, (3) foundation fixes before feature fixes.

### Phase 1: Critical Security & Data Integrity

| ID | Title | Scope | Agent |
|----|-------|-------|-------|
| AC-C05 / AR-C01 | CORS wildcard in production | `Program.cs`, `appsettings.json` | backend-agent |
| AC-C02 | Missing exercise-scoped authorization (15 endpoints) | 7 controllers | backend-agent |
| AC-C06 | SignalR hub accepts client-supplied user ID | `ExerciseHub.cs` | backend-agent |
| AC-C04 | Hardcoded `isAdmin = true` in delete operations | `ExercisesController.cs` | backend-agent |
| AC-C01 | Hardcoded `SystemUserIdString` in 5 controllers | 5 controllers | backend-agent |
| CD-C04 | Exercise delete missing entity cascades | `ExerciseDeleteService.cs` | backend-agent |

### Phase 2: Critical Stability & Shared Infrastructure

| ID | Title | Scope | Agent |
|----|-------|-------|-------|
| CD-C01 | Static in-memory import session state | 2 import services | backend-agent |
| CD-C03 | Double SignalR broadcast for approval actions | `InjectService.cs` | backend-agent |
| CD-C02 | N+1 SaveChangesAsync in bulk import | `BulkParticipantImportService.cs` | backend-agent |
| AR-M03 | Global exception handler placement in pipeline | `Program.cs` | backend-agent |
| AC-M10 | Exception handler missing `HasStarted` guard | `Program.cs` | backend-agent |
| AC-M01 | Extract shared `GetCurrentUserId()` extension | 12 controllers + new extension | backend-agent |

### Phase 3: Frontend Shared Patterns

| ID | Title | Scope | Agent |
|----|-------|-------|-------|
| AR-M04 / FI-M01 / FF-C02 | Create dev-only logger, remove console.log | 16 files + new utility | frontend-agent |
| FI-C01 | Raw MUI Button in ProfileMenu | `ProfileMenu.tsx` | frontend-agent |
| FI-C02 | Hardcoded hex colors in ConnectionStatusIndicator | `ConnectionStatusIndicator.tsx` | frontend-agent |
| FI-M02 | Hardcoded hex in OrganizationSwitcher | `OrganizationSwitcher.tsx` | frontend-agent |
| FF-M04 | Raw MUI Button in PendingApprovalAlert | `PendingApprovalAlert.tsx` | frontend-agent |
| FF-M05 | Raw MUI Button in VersionInfoCard | `VersionInfoCard.tsx` | frontend-agent |
| FI-M03 | Duplicated `ConnectionState` type in SignalR hooks | 2 hook files + new types file | frontend-agent |
| FI-M04 | Duplicated network error detection logic | `AuthContext.tsx`, `api.ts` + new utility | frontend-agent |
| FI-M05 | Duplicated theme component overrides | `cobraTheme.ts` | frontend-agent |

### Phase 4: Major Backend Stability & DRY

| ID | Title | Scope | Agent |
|----|-------|-------|-------|
| AC-C03 | Move business logic from 6 controllers to Core services | 6 controllers | backend-agent |
| CD-M01 | Register metrics sub-services in DI | `ExerciseMetricsService.cs`, `ServiceCollectionExtensions.cs` | backend-agent |
| CD-M02 | Add `.AsNoTracking()` to read-only queries | Multiple services | backend-agent |
| CD-M03 | Extract observation DTO projection (5 copies) | `ObservationService.cs` | backend-agent |
| CD-M04 | Use `ExecuteUpdateAsync`/`ExecuteDeleteAsync` in notifications | `NotificationService.cs` | backend-agent |
| CD-M05 | Fix missing user activation in invitation flow | `OrganizationInvitationService.cs` | backend-agent |
| CD-M06 | Standardize org-scoping pattern in services | Multiple services | backend-agent |
| CD-M07 | Decompose large service classes | `InjectService.cs`, `ExerciseService.cs` | backend-agent |
| AR-M02 / CD-M08 | Create FluentValidation validators | New validator files | backend-agent |

### Phase 5: Major Frontend Features & Patterns

| ID | Title | Scope | Agent |
|----|-------|-------|-------|
| FF-C01 | Refactor UserListPage to use React Query | `UserListPage.tsx` + new `useUsers.ts` | frontend-agent |
| FF-M01 | Fix wrong query key in useExcelImport | `useExcelImport.ts` | frontend-agent |
| FF-M02 | Fix notification optimistic update query key | `useNotifications.ts` | frontend-agent |
| FF-M03 | Standardize data fetching patterns across features | Multiple features | frontend-agent |
| AR-M01 | Add missing `modifiedBy` field to frontend InjectDto | `types/index.ts` | frontend-agent |
| AC-M04 | Decompose ExercisesController (957 lines) | `ExercisesController.cs` | backend-agent |
| AC-M02 | Fix `GetUserMemberships` missing admin authorization | `UsersController.cs` | backend-agent |
| AC-M03 | Replace string literal `"Admin"` with `[AuthorizeAdmin]` | `SystemSettingsController.cs` | backend-agent |
| AC-M06 | Replace custom auth in AutocompleteController | `AutocompleteController.cs` | backend-agent |
| AC-M07 | Add role gate to CapabilitiesController write endpoints | `CapabilitiesController.cs` | backend-agent |

### Phase 6: Minor Cleanup (Batch)

| ID | Title | Scope |
|----|-------|-------|
| FI-N01 | Remove unused `_tokenExpiry` state in AuthContext | `AuthContext.tsx` |
| FI-N02 | Replace deprecated `PaperProps` with `slotProps` | `ProfileMenu.tsx` |
| FI-N03 | Replace imperative fetch with React Query in ProfileMenu | `ProfileMenu.tsx` |
| FI-N04 | Use layout route for admin routes (9 repeated wrappers) | `App.tsx` |
| FF-N01 | Standardize query key factories across features | Multiple hooks |
| FF-N02 | Add missing loading states to feature pages | Multiple pages |
| FF-N03 | Remove commented-out/unused service methods | Multiple services |
| FF-N04 | Standardize mutation error handling patterns | Multiple hooks |
| FF-N05 | Move cross-feature imports to shared types | Multiple services |
| FF-N06 | Remove over-exported internal components | Multiple index files |
| AC-N01 | Remove duplicate `GetCurrentUserId` in ExerciseClockController | `ExerciseClockController.cs` |
| AC-N02 | Move `VersionInfo` record to DTOs directory | `VersionController.cs` |
| AC-N03 | Replace `StatusCode(500)` with ProblemDetails | `BulkParticipantImportController.cs` |
| AC-N04 | Align export route path with convention | `ExcelExportController.cs` |
| AC-N05 | Add `[Authorize]` attribute to ExerciseHub | `ExerciseHub.cs` |
| AC-N06 | Replace regex body sanitization with JSON parsing | `RequestResponseLoggingMiddleware.cs` |
| CD-N01 | Standardize mapper patterns (static methods) | Various mappers |
| CD-N02 | Resolve TODO/FIXME comments | Various files |
| CD-N03 | Remove unused entity navigation properties | Various entities |
| CD-N04 | Extract large entity configurations from DbContext | `AppDbContext.cs` |
| CD-N05 | Document seeder ordering dependencies | Seeders |
| CD-N06 | Audit and fix exception swallowing | Various services |
| CD-N07 | Add composite indexes for common query patterns | `AppDbContext.cs` |
| AR-N01 | Extract leaf providers into AppShell + create test utility | `App.tsx` |
| AR-N02 | Add mapper to Feedback feature | `Feedback/` |
| AR-M05 | Migrate test infrastructure from InMemory to SQL Server | `TestDbContextFactory.cs` (long-term) |

---

## Positive Patterns to Preserve

> Consolidated list of things done well. Fix agents must NOT regress these.

| ID | Pattern | Where | Replicate In |
|----|---------|-------|-------------|
| AR-P01 | Core/WebApi separation with interface in Core, implementation in WebApi | `IExerciseHubContext` → `ExerciseHubContext` | Any future event-driven features |
| AR-P02 | React Query cache invalidation via SignalR (never direct state mutation) | `useInjects.ts`, `useExerciseSignalR.ts` | All real-time features |
| AR-P03 | Organization validation interceptor for write-side data isolation | `OrganizationValidationInterceptor.cs` | Future interceptor patterns |
| AR-P04 | Feature module consistency in mature features | Exercises, Injects, Organizations | All new features |
| AR-P05 | InjectDto 99% type alignment across layers without code generation | `InjectDtos.cs` ↔ `types/index.ts` | All DTOs |
| AR-P06 | Secure token strategy (memory storage, httpOnly cookies, single-flight refresh) | `api.ts`, `AuthContext.tsx`, auth services | Never change; document in CODING_STANDARDS |
| FI-P01 | Single-flight token refresh with concurrent 401 deduplication | `api.ts` | Future interceptors |
| FI-P02 | Offline auth state preservation with graceful degradation | `AuthContext.tsx` | Document as mandated offline pattern |
| FI-P03 | `notify` wrapper with time-window deduplication | `notify.ts` | Continue enforcing project-wide |
| FI-P04 | `useConfirmDialog` imperative Promise pattern | `useConfirmDialog.tsx` | Future modal/dialog flows |
| FI-P05 | Two-phase offline sync with ordered action processing | `syncService.ts` | Future offline features |
| FI-P06 | `onReconnectedRef` pattern prevents stale closure in SignalR | `useExerciseSignalR.ts` | Future SignalR hooks |
| FI-P07 | `ProtectedRoute` allows offline access without redirect | `ProtectedRoute.tsx` | Future auth guards |
| FF-P01 | Hook-based data fetching with React Query in mature features | `useExercises.ts`, `useInjects.ts` | All features (standardize) |
| FF-P02 | Consistent feature module directory structure | All 25 features | All new features |
| FF-P03 | Type-safe service layer with explicit return types | All service files | Continue enforcing |
| FF-P04 | `notify` wrapper used consistently (no direct toast imports) | All features | Continue enforcing |
| FF-P05 | FontAwesome icons used consistently (no MUI icons) | All features | Continue enforcing |
| AC-P01 | Authorization policy infrastructure (`[ExerciseAccess]`, `[ExerciseRole]`) | `Authorization/` | Apply to all controllers (see AC-C02) |
| AC-P02 | Rate limiting on authentication endpoints | `Program.cs` | Extend to other sensitive endpoints |
| AC-P03 | Structured Serilog context middleware | `Program.cs` | Ensure all services use ILogger |
| AC-P04 | AuthController as exemplary delegation pattern | `AuthController.cs` | All controllers should follow |
| AC-P05 | Request/response audit logging middleware | `RequestResponseLoggingMiddleware.cs` | Maintain; improve sanitization |
| AC-P06 | Clean hub group management pattern | `ExerciseHub.cs` | Future hub implementations |
| CD-P01 | Every `IgnoreQueryFilters()` call has explanatory comment | All services | Continue enforcing |
| CD-P02 | `ExecuteDeleteAsync` for bulk cascade deletes | `ExerciseDeleteService.cs` | Apply to notifications (CD-M04) |
| CD-P03 | Two-phase AsNoTracking-then-tracking query pattern | `InjectReadinessService.cs` | Selective-update services |
| CD-P04 | Static pure-function mapper classes | All mappers | Never add dependencies to mappers |
