# Code Metrics Summary — Pass 3

> **Date:** 2026-03-10
> **Branch:** maintenance/code-review-v3-metrics
> **Scanners:** 3 (Size & Complexity, Dead Code, Convention & Readability)

---

## Aggregate Metrics

| Metric | Count | Critical | Major | Minor |
|--------|-------|----------|-------|-------|
| Files over 500 lines | 28 | 10 | 14 | 4 |
| Functions over 50 lines | 17 | 10 | 5 | 2 |
| High complexity files (>30 branches) | 7 | 4 | 3 | — |
| Deep nesting violations (5+ levels) | 4 | 3 | 1 | — |
| Unused/dead code artifacts | 4 | 3 | 1 | — |
| Commented-out code blocks | 1 | — | — | 1 |
| TODO/FIXME/HACK comments | 8 | — | 4 | 4 |
| Unused dependencies | 1 | 1 | — | — |
| Placeholder test files | 3 | — | 3 | — |
| COBRA violations (IconButton) | 4 | — | 4 | — |
| Hardcoded hex colors | ~200 | — | ~200 | — |
| Magic numbers/strings | 5 | — | — | 5 |
| `any` type usage (fixable) | 0 | — | — | — |
| Functions with >4 params | 5 | — | 1 | 4 |
| Backend features with zero tests | 3 | 3 | — | — |
| Frontend features with zero tests | 7 | 7 | — | — |

---

## Top 10 Worst Files

Ranked by composite score: (line count × 0.3) + (complexity × 0.3) + (convention violations × 0.2) + (dead code × 0.2). Scores normalized to 100.

| Rank | File | Lines | Complexity | Convention Violations | Dead Code / Other | Composite Score |
|------|------|-------|-----------|----------------------|-------------------|----------------|
| 1 | `Cadence.Core/Features/ExcelImport/Services/ExcelImportService.cs` | 1,739 | 96 `if`, 7+ nesting depth, 108 deep lines | 0 | 1 TODO | **98** |
| 2 | `frontend/src/features/injects/pages/InjectListPage.tsx` | 1,804 | 6+ nesting depth, 585 deep lines | 4 hex colors | 0 | **92** |
| 3 | `Cadence.Core/Data/BetaDataSeeder.cs` | 3,625 | 2,108-line method | 0 | 0 | **90** |
| 4 | `frontend/src/features/exercises/pages/ExerciseConductPage.tsx` | 1,249 | 44 `if`, 5-6 nesting, 362 deep lines | 0 | 0 | **88** |
| 5 | `Cadence.Core/Data/DemoDataSeeder.cs` | 2,236 | Low (data declarations) | 0 | 0 | **82** |
| 6 | `Cadence.WebApi/Controllers/InjectsController.cs` | 884 | 51 `if`, 3× duplicated Include chains | 0 | 0 | **80** |
| 7 | `Cadence.Core/Features/ExcelExport/Services/ExcelExportService.cs` | 938 | 41 `if`, 143-line method | 0 | 0 | **76** |
| 8 | `frontend/src/features/exercises/pages/ExerciseDetailPage.tsx` | 921 | Moderate | 0 | 0 | **72** |
| 9 | `Cadence.Core/Features/Authentication/Services/AuthenticationService.cs` | 732 | 44 `if`, 152-line method | 0 | 1 commented-out block | **70** |
| 10 | `Cadence.Core/Features/Injects/Services/InjectService.cs` | 753 | 39 `if`, 126-line batch methods | 0 | 0 | **68** |

### Decomposition Plans for Top 10

**1. ExcelImportService.cs (1,739 lines, score 98)**
Split into 6 files: `ExcelImportService.cs` (orchestration, ~300 lines), `ExcelFileAnalyzer.cs`, `ExcelRowReader.cs`, `InjectRowMapper.cs`, `InjectRowValidator.cs`, `ImportSessionStore.cs`. Move `ImportSession` nested class to its own file. This removes the deepest nesting (7+ levels) and highest branch count (96 `if`) in the codebase.

**2. InjectListPage.tsx (1,804 lines, score 92)**
Extract 5 embedded components into separate files: `GroupedInjectView.tsx`, `FlatInjectList.tsx`, `InjectRowCells.tsx`, `InjectRow.tsx`, `InjectEmptyState.tsx`. All receive data via props — no closures over parent state. Reduces main file to ~200 lines.

**3. BetaDataSeeder.cs (3,625 lines, score 90)**
Use partial classes: `BetaDataSeeder.MciTtx.cs`, `BetaDataSeeder.HazmatFe.cs`. Move per-exercise GUID constants and seed methods to each partial. `SeedEegDataAsync` (2,108 lines) splits into `SeedMciEegDataAsync` and `SeedHazmatEegDataAsync`.

**4. ExerciseConductPage.tsx (1,249 lines, score 88)**
Extract 3 hooks: `useReconnectionHandler.ts` (100 lines of clock-drift logic), `useExerciseConductSignalR.ts` (SignalR subscriptions), `useFireSkipConfirmation.ts` (dialog state machine). Extract `ObservationPanel.tsx` component. Reduces main file to ~300 lines.

**5. DemoDataSeeder.cs (2,236 lines, score 82)**
Same partial-class pattern as BetaDataSeeder: one partial per exercise (`HurricaneTtx.cs`, `CyberIncidentTtx.cs`, `EarthquakeFe.cs`).

**6. InjectsController.cs (884 lines, score 80)**
Split into 3 controllers: `InjectsController.cs` (CRUD), `InjectConductController.cs` (fire/skip/reset), `InjectApprovalController.cs` (approval workflow). Deduplicate the 3× repeated `Include` chain.

**7. ExcelExportService.cs (938 lines, score 76)**
Extract worksheet builders: `MselWorksheetBuilder.cs`, `ObservationsWorksheetBuilder.cs`, `ExcelTemplateBuilder.cs` (instructions + lookups), `ExcelFormattingHelper.cs` (shared styles).

**8. ExerciseDetailPage.tsx (921 lines, score 72)**
Extract `ExerciseDetailTabs.tsx`, `ExerciseSetupProgressSection.tsx`, and `useExerciseActions.ts` hook.

**9. AuthenticationService.cs (732 lines, score 70)**
Extract `PasswordResetService.cs` (~165 lines), `EnsureDefaultOrganizationAsync` helper, and `CreateFirstUserMembershipAsync` helper. Delete 9-line commented-out Entra SSO block.

**10. InjectService.cs (753 lines, score 68)**
Extract `InjectBatchApprovalService.cs` containing `BatchApproveAsync` and `BatchRejectAsync`.

---

## Test Coverage Gaps

| Priority | Feature | Backend Tests | Frontend Tests | Risk If Untested |
|----------|---------|--------------|----------------|-----------------|
| 1 | Msel | 0 | N/A | Core domain object for exercise conduct — MSEL ordering, summary, navigation are untested |
| 2 | Metrics (backend) | 1 (of 6 services) | 0 | Aggregation logic across injects/observations drives the real-time conduct dashboard |
| 3 | Metrics (frontend) | — | 0 (8 components, 8 hooks) | Largest untested frontend feature; hook logic untested |
| 4 | ExpectedOutcomes | 0 | 0 | Service logic untested across both stacks |
| 5 | Feedback | 0 | 0 | GitHub issue creation and dialog untested |
| 6 | Objectives | N/A | 0 | All components and hooks untested |
| 7 | Delivery Methods | N/A | 0 | Hooks and management page untested |
| 8 | Settings | N/A | 0 | UserSettingsPage and dialog untested |
| 9 | Autocomplete | N/A | 0 | SuggestionManagementPage and hooks untested |
| 10 | WebApi.Tests project | 0 real tests | — | Controller integration tests missing; only placeholder `Assert.True(true)` exists |
| 11 | Functions.Tests project | 0 real tests | — | Function unit tests missing; only placeholder exists |

---

## Dead Code Summary

| Issue | Files Affected | Fix |
|-------|---------------|-----|
| DC-C01: 3 duplicate exception classes in dead namespace | 3 files in `Infrastructure/Exceptions/` | Delete all 3 files |
| DC-C02: Unmapped `NotificationHub` in WebApi | 1 file | Delete `NotificationHub.cs` |
| DC-C03: `lodash` missing from runtime dependencies | 1 import in `CreateOrganizationPage.tsx` | Add to `dependencies` or replace with native debounce |
| DC-M01: 3 placeholder test files | 3 files across test projects | Delete placeholders |
| DC-M02: Dead unsaved-changes dialog in 2 inject pages | 2 files | Wire `useUnsavedChangesWarning` or remove dead code |
| DC-M03: Native `alert()` in PendingUserPage | 1 file | Replace with `notify.info()` |

---

## Convention Compliance Summary

| Category | Status |
|----------|--------|
| Raw MUI `Button`/`TextField` imports | **Clean** — 0 violations (all go through COBRA wrappers) |
| Raw MUI `IconButton` imports | **4 violations** — `AppHeader.tsx`, `NotificationBell.tsx`, `NotificationToast.tsx`, `TargetCapabilitiesSelector.tsx` |
| MUI icon imports | **Clean** — 0 violations (all use FontAwesome) |
| Direct `toast` imports | **Clean** — 0 violations (all route through `notify`) |
| Hardcoded hex colors | **~200 instances across ~45 files** — largest convention debt |
| Ungated `console.log` | **Clean** — 0 violations (all migrated to `devLog` in pass 2) |
| C# naming conventions | **Clean** — all private fields use `_camelCase` |
| TypeScript interface naming | **Clean** — all use PascalCase |
| Hook naming | **Clean** — all start with `use` |
| Production `any` types | **Clean** — 0 fixable (2 acceptable at library boundary) |

---

## Recommended Hardening Pass 3 Scope

### Must Fix (Critical metrics)

**File decomposition (highest ROI):**
1. `ExcelImportService.cs` → 6 files (removes deepest nesting + highest branching in codebase)
2. `InjectListPage.tsx` → 6 files (5 extractable sub-components)
3. `InjectsController.cs` → 3 controllers (removes duplicated Include chains + mixed concerns)
4. `ExerciseConductPage.tsx` → 3 hooks + 1 component

**Dead code removal:**
5. Delete 3 duplicate exception classes in `Infrastructure/Exceptions/`
6. Delete unmapped `NotificationHub.cs`
7. Fix `lodash` missing from runtime dependencies
8. Delete 3 placeholder test files

### Should Fix (Major metrics)

**File decomposition:**
- `BetaDataSeeder.cs` → partial classes per exercise
- `DemoDataSeeder.cs` → partial classes per exercise
- `ExcelExportService.cs` → worksheet builder classes
- `AuthenticationService.cs` → extract password reset + commented-out code
- `ExerciseDetailPage.tsx` → tabs + setup-progress + actions hook
- `AuthContext.tsx` → token refresh + auth init hooks
- `EegExportService.cs` → extract `EegExportMapper`

**Dead code cleanup:**
- Wire `useUnsavedChangesWarning` in inject pages or remove dead dialog code
- Replace `alert()` with `notify.info()` in PendingUserPage
- Remove duplicate TODO comments

**Convention:**
- Replace 4 raw `IconButton` imports with `CobraIconButton`

### Nice to Fix (Minor metrics — batch in cleanup pass)

**Hardcoded hex colors (~200 instances):**
- Highest-impact targets: `InjectStatusChip.tsx` (16 hex), `RatingBadge.tsx` (15 hex), `PendingActionsPopover.tsx` (10+ hex)
- Create theme palette extensions: `theme.palette.injectStatus.*`, `theme.palette.rating.*`, `theme.palette.actionType.*`
- Progressively migrate remaining ~45 files to use theme tokens

**Magic numbers:**
- Extract 5 setTimeout/setInterval numeric literals to named constants

**Test creation:**
- Create tests for MselService (highest-priority backend gap)
- Create tests for frontend metrics feature (largest untested feature)
- Replace placeholder tests with real tests in WebApi.Tests and Functions.Tests

### Out of Scope (Acceptable)

| File | Lines | Rationale |
|------|-------|-----------|
| `AppDbContext.cs` | 445 | Single-concern EF configuration; entity count drives size naturally |
| `Program.cs` | 432 | ASP.NET startup composition; splitting would reduce readability |
| `ExercisesController.cs` | 428 | Well-structured; delegates to services cleanly; under 500 threshold |
| `MembershipService.cs` | 464 | Approaching threshold but cohesive; monitor for growth |
| `UserService.cs` | 475 | Approaching threshold but cohesive; monitor for growth |
| Test files >800 lines | 4 files | Test files are naturally verbose; splitting is lower priority than production code. Flag for eventual cleanup. |
| `any` in test mocks | ~84 instances | Test context; typed mock factories would be nice but not critical |

---

## Cross-Reference with Pass 1-2 Findings

| Pass 1-2 Issue | Pass 3 Metric Confirmation |
|----------------|---------------------------|
| AC-M04: Decompose ExercisesController (957 lines) | Pass 3 shows 428 lines (hardened in pass 2). **Resolved.** |
| CD-M07: Decompose large service classes | Pass 3 confirms `ExcelImportService` (1,739), `InjectService` (753), `AuthenticationService` (732) as top candidates |
| FI-C01/FF-M04/FF-M05: Raw MUI Button imports | Pass 3 confirms `Button` violations are fixed. 4 `IconButton` violations remain. |
| FI-C02: Hardcoded hex colors | Pass 3 quantifies: ~200 instances across ~45 files (far larger than pass 1-2 estimated) |
| AR-M04/FI-M01: Production console.log | Pass 3 confirms: **0 violations remain** (fully resolved in pass 2) |
| CD-C01: Static in-memory session state | Pass 3 confirms 3 TODO comments tracking this; issue remains open |

---

*Generated by Pass 3 metric scanners — 2026-03-10*
