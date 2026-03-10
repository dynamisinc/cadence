# File Size & Structural Complexity Report

> **Pass:** 3 (Metrics)
> **Date:** 2026-03-10
> **Scanner:** File Size & Structural Complexity
> **Method:** Automated line counting, function length measurement, branching keyword analysis

---

## File Size Rankings

### Backend Files Over 500 Lines

Excludes all files under `Migrations/`, `bin/`, and `obj/`. Seeder files are included because they contain real executable logic.

| Rank | File | Lines | Classification | Decomposition Recommendation |
|------|------|-------|---------------|------------------------------|
| 1 | `src/Cadence.Core/Data/BetaDataSeeder.cs` | 3,625 | **Critical** | `SeedEegDataAsync` (lines 470–2578, ~2108 lines) must be split — see SC-C01 |
| 2 | `src/Cadence.Core/Data/DemoDataSeeder.cs` | 2,236 | **Critical** | Extract per-exercise seed helpers into `DemoExerciseSeeder`, `DemoInjectSeeder`, etc. — see SC-C02 |
| 3 | `src/Cadence.Core/Features/ExcelImport/Services/ExcelImportService.cs` | 1,739 | **Critical** | Extract `ValidateImportAsync` row-validation block into `InjectRowValidator`; extract `ReadAllRowsAsync` into `ExcelRowReader`; move `ImportSession` nested class to its own file — see SC-C03 |
| 4 | `src/Cadence.Core.Tests/Features/ExcelImport/ExcelImportServiceTests.cs` | 1,704 | **Critical** | Split into `ExcelImportAnalysisTests`, `ExcelImportMappingTests`, `ExcelImportExecutionTests` (each ~500 lines) |
| 5 | `src/Cadence.Core.Tests/Features/Injects/InjectServiceTests.cs` | 1,562 | **Critical** | Split into `InjectApprovalWorkflowTests`, `InjectFireSkipResetTests`, `InjectBatchOperationsTests` |
| 6 | `src/Cadence.Core.Tests/Features/Authentication/AuthenticationServiceTests.cs` | 1,059 | **Major** | Split into `AuthRegistrationTests`, `AuthLoginTests`, `AuthTokenRefreshTests` |
| 7 | `src/Cadence.Core.Tests/Features/ExerciseClock/ExerciseClockServiceTests.cs` | 1,089 | **Major** | Split into `ClockStartPauseTests`, `ClockAutoFireTests`, `ClockEventHistoryTests` |
| 8 | `src/Cadence.Core/Features/ExcelExport/Services/ExcelExportService.cs` | 938 | **Critical** | Extract `AddMselWorksheet` into `MselWorksheetBuilder`; extract `AddObservationsWorksheet` into `ObservationsWorksheetBuilder`; move template worksheets into `ExcelTemplateWorksheetBuilder` — see SC-C04 |
| 9 | `src/Cadence.WebApi/Controllers/InjectsController.cs` | 884 | **Critical** | Extract conduct operations (fire/skip/reset, lines 235–436) into `InjectConductController`; extract approval workflow endpoints (lines 477–713) into `InjectApprovalController` — see SC-C05 |
| 10 | `src/Cadence.Core/Features/Injects/Services/InjectService.cs` | 753 | **Major** | Extract batch approval methods (`BatchApproveAsync`, `BatchRejectAsync`) into `InjectBatchApprovalService` |
| 11 | `src/Cadence.Core/Features/Authentication/Services/AuthenticationService.cs` | 732 | **Major** | Extract password-reset flow into `PasswordResetService`; extract `GenerateAuthResponseAsync` into `AuthTokenFactory` — see SC-M02 |
| 12 | `src/Cadence.Core/Features/Eeg/Services/EegExportService.cs` | 688 | **Major** | Extract `ExportEegJsonAsync` (lines 121–686, ~565 lines) into a dedicated `EegJsonMapper` class — see SC-M01 |
| 13 | `src/Cadence.Core/Features/Eeg/Services/EegDocumentService.cs` | 577 | **Major** | Extract Word document generation helpers into `EegWordDocumentHelper` static class |
| 14 | `src/Cadence.Core/Features/Notifications/Services/ApprovalNotificationService.cs` | 572 | **Major** | Monitor for further growth; currently within tolerable range |
| 15 | `src/Cadence.Core/Features/Exercises/Services/ExerciseCrudService.cs` | 589 | **Major** | Extract `DuplicateExerciseAsync` (lines 274–449, ~175 lines) into `ExerciseDuplicationService` |
| 16 | `src/Cadence.Core/Features/Injects/Services/InjectCrudService.cs` | 531 | **Major** | `GetInjectsAsync` (lines 37–220, ~183 lines) should be split — see SC-M03 |
| 17 | `src/Cadence.Core/Features/Organizations/Services/MembershipService.cs` | 464 | Monitor | `AddMemberByEmailAsync` is 100+ lines — watch |
| 18 | `src/Cadence.Core/Features/Users/Services/UserService.cs` | 475 | Monitor | Approaching threshold |
| 19 | `src/Cadence.Core/Data/AppDbContext.cs` | 445 | Monitor | Healthy; single-concern configuration file |
| 20 | `src/Cadence.WebApi/Controllers/ExercisesController.cs` | 428 | Monitor | Well-structured; delegates to services cleanly |
| 21 | `src/Cadence.WebApi/Program.cs` | 432 | Monitor | Startup composition; acceptable for this pattern |

### Frontend Files Over 500 Lines

| Rank | File | Lines | Classification | Decomposition Recommendation |
|------|------|-------|---------------|------------------------------|
| 1 | `src/frontend/src/features/injects/pages/InjectListPage.tsx` | 1,804 | **Critical** | Extract `GroupedInjectView`, `FlatInjectList`, `InjectRowCells`, `InjectRow`, `InjectEmptyState` into separate component files — see SC-C06 |
| 2 | `src/frontend/src/features/exercises/pages/ExerciseConductPage.tsx` | 1,249 | **Critical** | Extract `handleReconnected` into `useReconnectionHandler` hook; extract SignalR subscriptions into `useExerciseConductSignalR`; extract observation panel into `ObservationPanel.tsx` — see SC-C07 |
| 3 | `src/frontend/src/features/exercises/pages/ExerciseDetailPage.tsx` | 921 | **Critical** | Extract tabs rendering into `ExerciseDetailTabs.tsx`; extract setup-progress into `ExerciseSetupProgressSection.tsx`; extract action menu into `useExerciseActions` hook |
| 4 | `src/frontend/src/contexts/AuthContext.tsx` | 683 | **Major** | Extract `refreshToken` scheduling into `useTokenRefresh` hook; extract data-fetching effects into `useAuthInit` hook |
| 5 | `src/frontend/src/features/eeg/pages/EegEntriesPage.tsx` | 644 | **Major** | Extract Coverage tab JSX into `EegCoverageTab.tsx`; extract Entries tab into `EegEntriesTab.tsx` |
| 6 | `src/frontend/src/features/eeg/components/EegEntryForm.tsx` | 589 | **Major** | Extract capability-task selection section into `EegTaskSelector.tsx` |
| 7 | `src/frontend/src/features/exercises/pages/ExerciseSettingsPage.tsx` | 474 | Monitor | Approaching threshold |

### Summary

- Backend files scanned (excluding migrations/bin/obj): ~130 .cs files
- Backend files >500 lines: 21
- Backend files >800 lines: 7 (BetaDataSeeder, DemoDataSeeder, ExcelImportService, ExcelImportServiceTests, InjectServiceTests, AuthenticationServiceTests, ExerciseClockServiceTests)
- Frontend files scanned: ~180 .ts/.tsx files
- Frontend files >500 lines: 7
- Frontend files >800 lines: 3 (InjectListPage, ExerciseConductPage, ExerciseDetailPage)
- Total files scanned: ~310

---

## Long Functions/Methods (>50 lines)

| File | Function/Method | Lines (approx) | Classification | Recommendation |
|------|----------------|----------------|---------------|----------------|
| `BetaDataSeeder.cs` | `SeedEegDataAsync` | ~2,108 (470–2578) | **Critical** | Decompose into per-exercise seed helpers: `SeedMciEegDataAsync`, `SeedHazmatEegDataAsync` |
| `EegExportService.cs` | `ExportEegJsonAsync` | ~565 (121–686) | **Critical** | Extract query section into `BuildEegExerciseQueryAsync`; extract DTO mapping into `EegExportMapper` |
| `InjectCrudService.cs` | `GetInjectsAsync` | ~183 (37–220) | **Critical** | Extract query-building into `BuildInjectQuery`; extract DTO projection into extension method |
| `ExcelImportService.cs` | `ExecuteImportAsync` | ~180 (544–724) | **Critical** | Extract row-to-inject mapping into `MapRowToInjectAsync`; extract MSEL auto-creation into `EnsureMselExistsAsync` |
| `ExerciseCrudService.cs` | `DuplicateExerciseAsync` | ~175 (274–449) | **Critical** | Extract phase/objective/inject copying into `CopyExercisePhases`, `CopyExerciseObjectives`, `CopyExerciseInjects` |
| `AuthenticationService.cs` | `RegisterAsync` | ~152 (164–316) | **Critical** | Extract organization bootstrap into `EnsureDefaultOrganizationAsync`; extract membership creation into `CreateFirstUserMembershipAsync` |
| `ExcelImportService.cs` | `AnalyzeFileAsync` | ~151 (199–350) | **Critical** | Extract per-format analysis into `AnalyzeCsvFileAsync`, `AnalyzeXlsxFileAsync` |
| `ExcelExportService.cs` | `ExportFullPackageAsync` | ~141 (297–438) | **Critical** | Extract worksheet orchestration into separate builder calls |
| `ExcelExportService.cs` | `AddObservationsWorksheet` | ~143 (605–748) | **Major** | Extract row data population loop into `PopulateObservationRows` |
| `InjectService.cs` | `BatchApproveAsync` | ~126 (422–548) | **Major** | Extract per-inject approval loop into `ApproveInjectBatchItemAsync` |
| `InjectCrudService.cs` | `UpdateInjectAsync` | ~121 (371–492) | **Major** | Extract approval-status-revert check into `CheckAndRevertApprovalStatus` |
| `InjectService.cs` | `BatchRejectAsync` | ~105 (551–656) | **Major** | Extract per-inject rejection loop into `RejectInjectBatchItemAsync` |
| `AuthenticationService.cs` | `GenerateAuthResponseAsync` | ~80 (640–720) | **Major** | Extract organization context lookup into `GetOrganizationContextAsync` |
| `ExerciseConductPage.tsx` | `ExerciseConductPage` component | ~1,165 (84–1249) | **Critical** | See SC-C07 |
| `InjectListPage.tsx` | `InjectListPageContent` component | ~557 (129–686) | **Critical** | See SC-C06 |
| `AuthContext.tsx` | `AuthProvider` component | ~550 | **Critical** | Extract effects and state management into `useAuthInit` hook |
| `EegEntryForm.tsx` | `EegEntryForm` component | ~589 | **Major** | Extract task-selector section into `EegTaskSelector.tsx` |

---

## High Complexity Files (>30 branching keywords)

| File | `if` Count | Est. Total Branch Points | Classification | Notes |
|------|-----------|--------------------------|---------------|-------|
| `ExcelImportService.cs` | 96 | ~180 (est.) | **Critical** | Highest complexity in codebase; dense conditional logic for file format handling, column mapping, row validation |
| `InjectsController.cs` | 51 | ~90 (est.) | **Critical** | Controller-level business logic compounds the count; conduct operations bypass the service layer |
| `AuthenticationService.cs` | 44 | ~75 (est.) | **Critical** | Multiple concurrent auth flows each with independent error paths |
| `ExerciseConductPage.tsx` | 44 | ~85 (est.) | **Critical** | Clock state management, fire/skip confirmation flows, SignalR reconnection logic, and observation handling all in one component |
| `ExcelExportService.cs` | 41 | ~70 (est.) | **Major** | Worksheet-building branches for formatting options; multiple export formats |
| `InjectService.cs` | 39 | ~65 (est.) | **Major** | Approval workflow state machine; batch operations with per-item error handling |
| `AuthContext.tsx` | 37 | ~65 (est.) | **Major** | Token lifecycle management, refresh scheduling, error recovery, and org-switch flows |

---

## Deep Nesting Violations (5+ levels)

| File | Deep-Nesting Line Count | Max Observed Depth | Classification | Context |
|------|------------------------|--------------------|---------------|---------|
| `ExcelImportService.cs` | 108 lines at 24+ spaces | 7+ levels | **Critical** | Row-processing loops inside try/catch inside format-specific branches; see SC-C03 |
| `InjectListPage.tsx` | 585 lines at 12+ spaces | 6+ levels | **Critical** | Deeply nested JSX within table cell renderers inside grouped view renderers; see SC-C06 |
| `ExerciseConductPage.tsx` | 362 lines at 12+ spaces | 5–6 levels | **Critical** | `handleReconnected` callback contains conditional clock-sync logic nested 5+ levels deep |
| `InjectService.cs` | 2 lines at 24+ spaces | 5 levels | Monitor | Isolated case within `ApproveInjectInternalAsync`; not systemic |

---

## Critical Issues

### SC-C01: BetaDataSeeder.SeedEegDataAsync is 2,108 Lines

**File:** `src/Cadence.Core/Data/BetaDataSeeder.cs` (lines 470–2578)

The `SeedEegDataAsync` static method is 2,108 lines long. The entire class is 3,625 lines. This is by far the largest single method in the codebase. Although seeder code has naturally repetitive data-declaration patterns, this function reaches a scale where:

- It is impossible to review in a single mental pass
- Any edit carries a high risk of colliding with another section
- The method does all EEG seeding for two exercises (MCI TTX and Hazmat FE) sequentially with no internal decomposition

The companion `SeedObservationsAndEegAsync` (line 2579, spanning through line ~3620) adds another ~1,040 lines.

**Recommended decomposition:**
- Extract `SeedMciTtxEegDataAsync` (MCI exercise EEG data) — approximately lines 470–1500
- Extract `SeedHazmatFeEegDataAsync` (Hazmat exercise EEG data) — approximately lines 1500–2578
- Extract `SeedMciObservationsAsync` and `SeedHazmatObservationsAsync` from `SeedObservationsAndEegAsync`
- Each resulting method should target <300 lines
- Move all fixed GUID constants for phases, objectives, injects, and capability targets into nested static classes (`BetaDataSeeder.MciTtx`, `BetaDataSeeder.HazmatFe`) to eliminate scrolling across 300+ GUID declarations at the top of the file

### SC-C02: DemoDataSeeder is 2,236 Lines with No Internal Decomposition

**File:** `src/Cadence.Core/Data/DemoDataSeeder.cs`

`SeedAsync` is 67 lines and delegates well, but the GUID constant region at the top of the file spans approximately 200 lines. The data seeding itself for MSELs, injects, phases, and observations for five exercises is all inlined within the seeder's single file, creating a 2,236-line monolith.

**Recommended decomposition:**
- Create `DemoDataSeeder.HurricaneTtx.cs` (partial class) containing Hurricane TTX GUID constants and the `SeedHurricaneTtxAsync` helper
- Create `DemoDataSeeder.CyberIncidentTtx.cs` and `DemoDataSeeder.EarthquakeFe.cs` similarly
- The `SeedAsync` orchestrator method remains in `DemoDataSeeder.cs` and calls the per-exercise helpers

### SC-C03: ExcelImportService is 1,739 Lines with 7+ Levels of Nesting

**File:** `src/Cadence.Core/Features/ExcelImport/Services/ExcelImportService.cs`

This is the most complex production service in the codebase. 96 `if` statements (estimated 180 total branch points), 108 lines at 7+ nesting levels, and 12 public/private methods with individual lengths between 50 and 180 lines.

**Recommended decomposition:**
```
Features/ExcelImport/
├── Services/
│   ├── ExcelImportService.cs              (~300 lines, orchestration only)
│   ├── ExcelFileAnalyzer.cs               (AnalyzeFileAsync per format)
│   ├── ExcelRowReader.cs                  (ReadAllRowsAsync, ParseCsvLine)
│   ├── InjectRowMapper.cs                 (row dict → CreateInjectRequest)
│   ├── InjectRowValidator.cs              (ValidateImportAsync row logic)
│   └── ImportSessionStore.cs              (ConcurrentDictionary + cleanup)
├── Models/
│   └── ImportSession.cs                   (extracted from nested class)
```

### SC-C04: ExcelExportService is 938 Lines with Each Worksheet as a Long Private Method

**File:** `src/Cadence.Core/Features/ExcelExport/Services/ExcelExportService.cs`

The service has 938 lines containing five worksheet-building private methods.

**Recommended decomposition:**
```
Features/ExcelExport/
├── Services/
│   ├── ExcelExportService.cs             (~200 lines, orchestration)
│   ├── Builders/
│   │   ├── MselWorksheetBuilder.cs
│   │   ├── ObservationsWorksheetBuilder.cs
│   │   ├── ExcelTemplateBuilder.cs       (instructions + lookups)
│   │   └── ExcelFormattingHelper.cs      (shared style/formatting logic)
```

### SC-C05: InjectsController is 884 Lines Mixing Three Distinct Concerns

**File:** `src/Cadence.WebApi/Controllers/InjectsController.cs`

The controller handles CRUD (lines 62–223), conduct operations with inline DB access (lines 235–471), approval workflow (lines 477–713), approval permission checks (lines 715–760), and EEG critical-task linking (lines 762–883). The conduct operations (`FireInject`, `SkipInject`, `ResetInject`) access `AppDbContext` directly and duplicate the `Include` chain three times.

**Recommended decomposition:**
- Extract `InjectConductController` for fire/skip/reset
- Extract `InjectApprovalController` for submit/approve/reject/batch-approve/batch-reject/revert
- Extract `InjectCriticalTasksController` or route the EEG linking into `CriticalTasksController`
- Deduplicate the repeated `Include` chain into a shared `GetInjectWithNavigationsAsync` private method

### SC-C06: InjectListPage.tsx is 1,804 Lines with Five Embedded Sub-Components

**File:** `src/frontend/src/features/injects/pages/InjectListPage.tsx`

The file contains the main page component (`InjectListPage` at line 111), a content component (`InjectListPageContent` at line 129) with ~557 lines of handler logic, and four additional embedded components that each receive all data via props and are fully extractable:

- `InjectTableSkeleton` (line 686, ~90 lines)
- `GroupedInjectView` (line 777, ~264 lines)
- `FlatInjectList` (line 1041, ~197 lines)
- `InjectRowCells` (line 1238, ~211 lines)
- `InjectRow` (line 1449, ~207 lines)
- `EmptyState` (line 1656, ~148 lines)

**Recommended decomposition:**
```
features/injects/
├── pages/
│   └── InjectListPage.tsx                 (~200 lines, wires hooks + renders content)
├── components/
│   ├── GroupedInjectView.tsx
│   ├── FlatInjectList.tsx
│   ├── InjectRowCells.tsx
│   ├── InjectRow.tsx
│   ├── InjectTableSkeleton.tsx
│   └── InjectEmptyState.tsx
```

### SC-C07: ExerciseConductPage.tsx is 1,249 Lines with Overloaded State Management

**File:** `src/frontend/src/features/exercises/pages/ExerciseConductPage.tsx`

The main component starts at line 84 and ends at line 1249. It directly manages fire/skip confirmation state (7 `useCallback` handlers), clock action confirmation state (6 `useCallback` handlers), SignalR reconnection detection with clock-drift compensation (~100 lines at 5+ nesting levels), observation panel state, EEG entry dialog state, and jump-to inject logic.

**Recommended decomposition:**
```
features/exercises/
├── pages/
│   └── ExerciseConductPage.tsx            (~300 lines, layout + wires hooks)
├── hooks/
│   ├── useReconnectionHandler.ts          (handleReconnected logic, ~100 lines)
│   ├── useExerciseConductSignalR.ts       (all SignalR event subscriptions)
│   └── useFireSkipConfirmation.ts         (fire/skip dialog state machine)
├── components/
│   ├── ObservationPanel.tsx
│   └── ConductViewModeToggle.tsx
```

---

## Major Issues

### SC-M01: EegExportService.ExportEegJsonAsync is 565 Lines

**File:** `src/Cadence.Core/Features/Eeg/Services/EegExportService.cs`, lines 121–686

A single method that queries the database, maps all EEG entities, and builds a multi-level nested DTO graph. The query section and DTO mapping section are distinct phases that should not coexist in one method.

**Recommended fix:** Extract lines ~250–686 into an `EegExportMapper` static class with a `MapToExportDto(Exercise, IEnumerable<CapabilityTarget>, ...)` method.

### SC-M02: AuthenticationService.RegisterAsync is 152 Lines

**File:** `src/Cadence.Core/Features/Authentication/Services/AuthenticationService.cs`, lines 164–316

Combines validation guards, transaction management, first-user detection, organization bootstrapping, user creation, membership creation, and fire-and-forget email dispatch.

**Recommended fix:** Extract lines ~200–270 into `EnsureDefaultOrganizationAsync` private method; extract first-user membership creation into `CreateFirstUserMembershipAsync`.

### SC-M03: InjectCrudService.GetInjectsAsync is 183 Lines

**File:** `src/Cadence.Core/Features/Injects/Services/InjectCrudService.cs`, lines 37–220

A single query method with 12+ `Include`/`ThenInclude` chains, conditional filters, and a 40+ line LINQ `Select` projection.

**Recommended fix:** Extract the `Include` chain into `BuildInjectBaseQuery(Guid exerciseId)`; extract the `Select` projection into `InjectExtensions.ToDto()`.

---

*Report generated: 2026-03-10. All line numbers are approximations based on method signature positions; exact counts may vary by ±5 lines.*
