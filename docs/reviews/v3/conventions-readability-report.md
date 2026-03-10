# Convention Compliance & Readability Metrics Report

> **Pass:** 3 (Metrics)
> **Date:** 2026-03-10
> **Scanner:** Convention Compliance & Readability Metrics

---

## COBRA Styling Violations

### Raw MUI Button / TextField / IconButton Imports (should use COBRA wrappers)

COBRA wrappers exist for: `Button` (→ CobraPrimaryButton, CobraSecondaryButton, CobraDeleteButton, CobraLinkButton), `TextField` (→ CobraTextField), `IconButton` (→ CobraIconButton). All imports of these raw components outside `theme/` are violations. `Select`, `MenuItem`, `Box`, `Typography`, `Paper`, `Dialog`, `Chip`, `Alert`, and other layout/display primitives have no COBRA wrapper and are not violations.

| File | Line | Import | Recommended COBRA Component |
|------|------|--------|-----------------------------|
| `src/frontend/src/core/components/navigation/AppHeader.tsx` | 15 | **`IconButton`** | `CobraIconButton` from `@/theme/styledComponents` |
| `src/frontend/src/features/notifications/components/NotificationBell.tsx` | 7 | **`IconButton`** | `CobraIconButton` from `@/theme/styledComponents` |
| `src/frontend/src/features/notifications/components/NotificationToast.tsx` | 6 | **`IconButton`** | `CobraIconButton` from `@/theme/styledComponents` |
| `src/frontend/src/features/exercises/components/TargetCapabilitiesSelector.tsx` | 13 | **`IconButton`** | `CobraIconButton` from `@/theme/styledComponents` |

**Note on `Button` / `TextField`:** All raw `Button` and `TextField` imports in production code are inside `src/frontend/src/theme/styledComponents/` where the COBRA wrappers are defined. No violations outside theme/ for these two components.

**Note on `Select`:** There is no `CobraSelect` wrapper in the COBRA system. The two `Select` usages (`RoleSelect.tsx`, `GroupByDropdown.tsx`) are acceptable.

### MUI Icon Imports (should be FontAwesome)

No violations found. Zero occurrences of `from '@mui/icons-material'` in any production or test file.

### Direct Toast Imports (should use notify)

| File | Line | Import | Notes |
|------|------|--------|-------|
| `src/frontend/src/App.tsx` | 7 | `import { ToastContainer } from 'react-toastify'` | **Acceptable** — `ToastContainer` is the container component, not `toast`. It is the single correct place to mount the toast provider. Not a violation. |
| `src/frontend/src/shared/utils/notify.ts` | 16 | `import { toast, ... } from 'react-toastify'` | **Expected** — this is the notify wrapper implementation itself. |

No violations. All application code routes through `notify`.

### Hardcoded Colors (should use theme tokens)

The following files use hex color literals in component code outside `src/frontend/src/theme/`. Theme files (`cobraTheme.ts`, `cobraTheme.test.ts`) are excluded — they are the correct location to define palette tokens.

| File | Lines | Sample Values | Impact |
|------|-------|--------------|--------|
| `features/assignments/components/AssignmentSection.tsx` | 44–50 | `#4caf50`, `#2196f3`, `#9e9e9e`, `#666` | Badge color logic — should use `theme.palette.*` |
| `features/assignments/components/AssignmentCard.tsx` | 61–195 | `#4caf50`, `#ff9800`, `#9e9e9e`, `#888`, `#666` | Status icon colors + inline styles |
| `features/assignments/pages/MyAssignmentsPage.tsx` | 96 | `#ccc` | Empty-state icon color |
| `shared/constants/roleOrientation.ts` | 125–146 | `#d32f2f`, `#1976d2`, `#2e7d32`, `#757575` | Role color constants — should reference theme tokens |
| `features/version/pages/AboutPage.tsx` | 211, 232, 253 | `#4caf50`, `#ff9800`, `#f44336` | Changelog icon colors |
| `features/auth/pages/ForgotPasswordPage.tsx` | 85 | `#1e3a5f` | Icon inline style — use `theme.palette.buttonPrimary.main` |
| `shared/components/PhotoSyncIndicator.tsx` | 97, 110, 132 | `#22c55e`, `#3b82f6` | Progress bar colors — not mapped to theme |
| `features/version/components/WhatsNewModal.tsx` | 49, 73, 91 | `#ffc107`, `#4caf50`, `#ff9800` | Icon colors |
| `features/eeg/types/index.ts` | 165–168 | `#4caf50`, `#ff9800`, `#f44336`, `#9e9e9e` | PerformanceRating color map |
| `features/eeg/pages/EegEntriesPage.tsx` | 493 | `#bdbdbd` | Empty-state icon |
| `features/excel-export/components/ExportButton.tsx` | 121, 128, 135 | `#217346`, `#6b7280` | Excel icon brand colors (Excel green is acceptable branding) |
| `features/metrics/components/RatingChartsPanel.tsx` | 55–59, 132, 349 | 6 hex values | Chart dataset colors — no theme equivalent for charts |
| `features/metrics/components/CapabilityPerformancePanel.tsx` | 47–67 | 5 hex values | Chart dataset colors |
| `features/users/pages/UserListPage.tsx` | 522 | `#666` | Secondary text — use `theme.palette.text.secondary` |
| `features/excel-import/components/ImportExecutionStep.tsx` | 95, 180 | `#2e7d32`, `#1976d2` | Success/info icon colors |
| `features/excel-import/components/FileUploadStep.tsx` | 210, 222, 246, 289 | `#1976d2`, `#2e7d32`, `#757575` | State-dependent icon colors |
| `features/excel-import/components/InlineEditCell.tsx` | 134 | `#999` | Placeholder text |
| `features/excel-import/components/InjectPreviewCard.tsx` | 133, 146, 332 | `#999`, `#d32f2f`, `#ed6c02` | Validation severity colors |
| `features/excel-import/components/ColumnMappingStep.tsx` | 399 | `#d32f2f` | Required field asterisk |
| `features/eeg/components/EegCoverageDashboard.tsx` | 228, 258, 385, 495 | `#ed6c02`, `#2e7d32` | Coverage warning colors |
| `features/eeg/components/LinkedInjectsDialog.tsx` | 232 | `#999` | Secondary icon |
| `features/eeg/components/EegEntryDialogs.tsx` | 216 | `#666` | Link icon |
| `features/eeg/components/EegEntriesGroupedByCapability.tsx` | 368 | `#666` | Link icon |
| `features/eeg/components/EegEntriesList.tsx` | 254 | `#666` | Link icon |
| `core/components/UpdatePrompt.tsx` | 109 | `#4caf50`, `#ff9800` | Changelog icon colors |
| `core/components/SplashScreen.tsx` | 66, 84, 106, 133 | `#1e3a5f`, `rgba(0,0,0,0.5)`, `#ffffff` | **New uncommitted file** — uses hardcoded navy |
| `core/components/RouteErrorFallback.tsx` | 127, 236 | `#1e3a5f`, `#08682a` | Should use theme tokens |
| `core/components/ErrorBoundary.tsx` | 160, 312 | `#1e3a5f`, `#08682a` | Should use theme tokens |
| `core/components/ConflictDialog.tsx` | 70, 93 | `#f59e0b`, `#ef4444` | Warning/error icon colors |
| `core/components/navigation/AppHeader.tsx` | 91 | `#ffffff` | White text override |
| `core/components/PendingActionsPopover.tsx` | 53–98, 214, 299 | 10+ hex values | Action type color map — should be a theme extension |
| `features/organizations/pages/InviteAcceptPage.tsx` | 221–532 | `#f44336`, `#4caf50`, `#666` | Status and icon colors |
| `features/exercises/components/bulk-import/ImportUploadStep.tsx` | 172, 183, 230 | `#1976d2`, `#757575` | Icon colors |
| `features/photos/components/StorageWarningDialog.tsx` | 93 | `#ef4444`, `#f59e0b` | Warning level icon |
| `features/exercises/components/ExerciseTable.tsx` | 405, 676 | `#666`, gradient | Secondary text + gradient background |
| `features/exercises/components/ExerciseDetailRow.tsx` | 53–162 | `#4caf50`, `#ff9800`, `#757575`, `#666` | Status icon colors |
| `features/exercises/components/FloatingClockChip.tsx` | 141 | `#2e7d32`, `#ed6c02` | Clock state colors |
| `features/exercises/components/PendingInvitationsList.tsx` | 71–131 | `#2e7d32`, `#d32f2f`, `#ed6c02` | Invitation status colors |
| `features/notifications/hooks/useNotificationToast.ts` | 17–30 | `#fff3e0`, `#ff9800`, `#e3f2fd`, `#2196f3`, `#f5f5f5`, `#9e9e9e` | Notification type border/bg styles |
| `features/notifications/components/NotificationItem.tsx` | 33–49 | 8 hex values | Notification type icon colors |
| `features/notifications/components/NotificationDropdown.tsx` | 94 | `#ccc` | Empty-state icon |
| `features/injects/components/InjectFilterBar.tsx` | 160 | `#9e9e9e` | Placeholder icon color |
| `features/injects/components/InjectTypeChip.tsx` | 31–32 | `#E8DEF8`, `#4A148C` | Type chip colors — should use theme |
| `features/injects/components/InjectStatusChip.tsx` | 35–91 | 16 hex values | Status color map — extensive hardcoding |
| `features/observations/components/RatingBadge.tsx` | 28–54 | 15 hex values | Rating color map |
| `features/observations/components/ObservationList.tsx` | 287 | `#757575` | Camera icon |
| `features/observations/components/ObservationCapabilitySelector.tsx` | 68 | `#FFD700` | Gold star color |
| `features/observations/pages/ObservationsPage.tsx` | 547 | `#9e9e9e` | Empty-state icon |
| `features/home/components/RoleOrientationPanel.tsx` | 59 | gradient | Card background |
| `features/injects/pages/InjectListPage.tsx` | 1690, 1731, 1739, 1787 | `#9e9e9e`, `#1976d2`, gradient | Empty states |
| `config/version.ts` | 23 | `#1976d2` | Console styling string — dev-only, acceptable |

**Most severe hardcoding clusters (by violation density):**
1. `InjectStatusChip.tsx` — 16 hardcoded hex values for a status color map
2. `PendingActionsPopover.tsx` — 10+ hardcoded hex values for action type colors
3. `RatingBadge.tsx` — 15 hardcoded hex values for rating colors
4. `NotificationItem.tsx` — 8 hardcoded hex values for notification type colors

### Production Console Logging (should use devLog)

No violations. All `console.log` / `console.warn` occurrences are:
- Inside `core/utils/logger.ts` (the gated implementation)
- Inside `*.test.ts` test files (acceptable in test context)
- Inside JSDoc comment blocks in `useSignalR.ts` and `useCamera.ts` (not executable code)

Zero ungated `console.log` calls in production component code.

### Summary: COBRA Violations

| Category | Violation Count | Affected Files |
|----------|----------------|----------------|
| Raw `IconButton` imports (COBRA wrapper exists) | 4 | 4 |
| Raw `Button` imports outside theme/ | 0 | 0 |
| Raw `TextField` imports outside theme/ | 0 | 0 |
| MUI icon imports | 0 | 0 |
| Direct `toast` imports | 0 | 0 |
| Hardcoded hex colors in components | ~200+ instances | ~45 files |
| Ungated console.log | 0 | 0 |
| **Total actionable violations** | **~204** | **~49 files** |

---

## Naming Convention Violations

### C# Private Fields

Scan checked for `private [A-Z]` patterns. All matches are **private method declarations** (e.g., `private AppDbContext CreateContext()`), not field declarations. Private field naming is correctly `_camelCase` throughout the codebase.

No violations found.

### TypeScript Interfaces

Scan for `interface [a-z]` found **zero matches**. All interfaces use PascalCase as required.

No violations found.

### Hook Naming Convention

All exported symbols from `**/hooks/*.ts(x)` files were checked. All custom hooks start with `use`. The following non-hook exports found in hook files are **acceptable** — they are query key factories and constants co-located with hooks as a common React Query pattern:

- `systemSettingsKeys`, `feedbackAdminKeys` (admin hooks)
- `organizationKeys`, `capabilityKeys`, `exercisesQueryKey`, `injectKeys`, etc.
- `evaluatorCoverageQueryKey`, `controllerActivityQueryKey`, etc.

These are query key objects/functions, not hooks. Their naming is correct for their purpose.

No hook naming violations found.

---

## Magic Numbers & Strings

### setTimeout / setInterval with Raw Numeric Literals

| File | Line | Value | Recommended Constant Name |
|------|------|-------|--------------------------|
| `core/contexts/OfflineSyncContext.tsx` | 242 | `1500` (ms) | `SYNC_RETRY_DELAY_MS` |
| `core/components/RouteErrorFallback.tsx` | 75 | `2000` (ms) | `COPY_FEEDBACK_DURATION_MS` |
| `features/auth/pages/ResetPasswordPage.tsx` | 111 | `3000` (ms) | `REDIRECT_AFTER_RESET_DELAY_MS` |
| `features/users/components/CreateUserModal.tsx` | 181 | `2000` (ms) | `COPY_FEEDBACK_DURATION_MS` |

**Note:** `features/capabilities/components/ImportLibraryMenu.test.tsx:211` is a test file mock delay — acceptable in tests.

`setInterval` usages:
- `core/contexts/ConnectivityContext.tsx:159` — passes a variable `interval` (not a raw literal) — acceptable.
- `features/exercise-clock/hooks/useExerciseClock.ts:123` — `setInterval(updateDisplay, 1000)` — `1000` is a well-understood 1-second clock tick; could be named `CLOCK_UPDATE_INTERVAL_MS` but is low-priority.

---

## Functions With >4 Parameters

### C# Backend

| File | Function | Param Count | Recommendation |
|------|----------|-------------|----------------|
| `Core/Features/Authentication/Services/JwtTokenService.cs:76` | `GenerateAccessToken(UserInfo, Guid?, string?, string?, string?)` | 5 | Extract org context params into a `TokenOrganizationContext` record |
| `Core/Features/Injects/Services/InjectService.cs:259` | `ApproveInjectAsync(Guid, Guid, string, string?, CancellationToken)` | 5 (4 meaningful + CT) | CancellationToken is infrastructure — effectively 4 domain params; borderline, acceptable |
| `Core/Features/Injects/Services/InjectService.cs:358` | `RejectInjectAsync(Guid, Guid, string, string, CancellationToken)` | 5 (4 + CT) | Same pattern — borderline acceptable |
| `Core/Features/Injects/Services/InjectService.cs:659` | `RevertApprovalAsync(Guid, Guid, string, string, CancellationToken)` | 5 (4 + CT) | Same pattern — borderline acceptable |
| `Core/Features/Injects/Models/DTOs/InjectDtos.cs:558` | `ToEntity(request, Guid, int, int, string)` | 5 | Consider an `InjectCreationContext` record |

**Most significant:** `JwtTokenService.GenerateAccessToken` takes 5 parameters where 4 of them are all org-related nullable strings/guid. These should be extracted into a `TokenOrganizationContext` value object.

### TypeScript Frontend

No functions with >4 parameters were found in production hook or component code during the scan. The largest parameter counts are in test mock factories, which are acceptable.

---

## TypeScript `any` Usage

### Production Code (non-test files)

| File | Line(s) | Context | Classification |
|------|---------|---------|---------------|
| `core/offline/syncService.ts` | 343, 358 | `error as any` in `getErrorMessage` / `getErrorStatus` helpers to introspect Axios error shape | **Acceptable** — axios error type is a third-party boundary; comment explicitly notes this with eslint-disable. The pattern is idiomatic for unknown error narrowing. |

### Test Files

All remaining `as any` usages are in test files (`*.test.tsx`). These are test mock casting patterns where React Query hook return types are partially mocked. This is a common pattern in Vitest/RTL testing and is **acceptable** in test context, though over-reliance on `as any` for mock return values indicates the mock factories could benefit from typed mock builders.

Files with significant test `as any` usage:
- `features/exercises/pages/ExerciseDetailPage.test.tsx` — 25 instances (hook mock returns)
- `features/exercises/pages/ReportsPage.test.tsx` — 17 instances (hook mock returns)
- `features/organizations/pages/CreateOrganizationPage.test.tsx` — 17 instances
- `features/organizations/pages/EditOrganizationPage.test.tsx` — 16 instances
- `features/organizations/pages/OrganizationListPage.test.tsx` — 9 instances

### Summary: `any` Usage

| Category | Count | Files |
|----------|-------|-------|
| Fixable production `any` | 0 | 0 |
| Acceptable production `any` (3rd-party boundary) | 2 | 1 |
| Test file `any` (mock casting) | ~84 | 8 |

**0 fixable `any` usages in production code, 2 acceptable (with eslint-disable comment).**

---

## Test Coverage Matrix

### Backend

| Feature | Service Tests | Validator Tests | Mapper Tests | Total |
|---------|--------------|----------------|-------------|-------|
| Authentication | 3 | 0 | 0 | 3 |
| Capabilities | 3 | 0 | 0 | 3 |
| Exercises | 8 | 0 | 0 | 8 |
| Injects | 3 | 2 | 0 | 5 |
| Observations | 2 | 0 | 0 | 2 |
| Organizations | 3 | 0 | 0 | 3 |
| ExerciseClock | 1 | 0 | 0 | 1 |
| ExcelExport | 2 | 0 | 0 | 2 |
| ExcelImport | 2 | 0 | 0 | 2 |
| BulkParticipantImport | 4 | 0 | 0 | 4 |
| Eeg | 2 | 0 | 0 | 2 |
| Email | 6 | 0 | 0 | 6 |
| SystemSettings | 3 | 0 | 0 | 3 |
| Notifications | 2 | 0 | 0 | 2 |
| Metrics | 1 | 0 | 0 | 1 |
| Users | 3 | 0 | 0 | 3 |
| Phases | 1 | 0 | 0 | 1 |
| Authorization | 4 | 0 | 0 | 4 |
| Photos | 1 | 0 | 0 | 1 |
| Assignments | 1 | 0 | 0 | 1 |
| **Msel** | **0** | **0** | **0** | **0** |
| **ExpectedOutcomes** | **0** | **0** | **0** | **0** |
| **Feedback** | **0** | **0** | **0** | **0** |

### Frontend

| Feature | Hook Tests | Component Tests | Page Tests | Total |
|---------|-----------|----------------|-----------|-------|
| auth | 2 | 5 | 3 | 10 |
| exercises | 5 | 10 | 5+ | 20+ |
| injects | 5 | 10 | 5+ | 20+ |
| exercise-clock | 2 | 2 | 1 | 5 |
| eeg | 1 | 3 | 1 | 5 |
| observations | 1 | 2 | 0 | 3 |
| organizations | 1 | 2 | 3 | 6 |
| notifications | 2 | 3 | 1 | 6 |
| assignments | 1 | 2 | 0 | 3 |
| capabilities | 0 | 2 | 0 | 2 |
| excel-export | 1 | 1 | 0 | 4 |
| version | 2 | 2 | 1 | 5 |
| phases | 1 | 2 | 1 | 4 |
| users | 0 | 3 | 1 | 4 |
| eula | 0 | 1 | 0 | 1 |
| home | 0 | 2 | 0 | 2 |
| photos | 0 | 3 | 0 | 3 |
| **metrics** | **0** | **0** | **0** | **0** |
| **delivery-methods** | **0** | **0** | **0** | **0** |
| **expected-outcomes** | **0** | **0** | **0** | **0** |
| **objectives** | **0** | **0** | **0** | **0** |
| **settings** | **0** | **0** | **0** | **0** |
| **feedback** | **0** | **0** | **0** | **0** |
| **autocomplete** | **0** | **0** | **0** | **0** |

### Features with ZERO Tests (priority for test creation)

**Backend:**
1. `Msel` — `MselService` handles MSEL listing, summary, and navigation; core domain service with zero test coverage
2. `ExpectedOutcomes` — service logic untested
3. `Feedback/GitHubIssueService` — issue creation logic untested
4. `Eeg/EegEntryService`, `EegExportService` — EEG entry CRUD and export untested (partial coverage only)
5. `Metrics` (5 services) — IInjectMetricsService, IObservationMetricsService, IProgressMetricsService, ITimelineMetricsService, IEvaluatorCoverageService all lack tests

**Frontend:**
1. `metrics` — all 8 metric panel components and 8 hooks have zero tests; this feature has the highest test debt
2. `delivery-methods` — hooks and management page untested
3. `expected-outcomes` — components and hooks untested
4. `objectives` — all components and hooks untested
5. `settings` — UserSettingsPage and UserSettingsDialog untested
6. `feedback` — FeedbackDialog untested
7. `autocomplete` — SuggestionManagementPage and hooks untested

---

## Issues

### CR-C01: Raw `IconButton` Used Instead of `CobraIconButton` in 4 Files

**Confidence: 88**

`CobraIconButton` is exported from `@/theme/styledComponents` and represents the COBRA-compliant wrapped version of `IconButton`. Four production files import `IconButton` directly from `@mui/material` instead.

Files:
- `src/frontend/src/core/components/navigation/AppHeader.tsx` line 15
- `src/frontend/src/features/notifications/components/NotificationBell.tsx` line 7
- `src/frontend/src/features/notifications/components/NotificationToast.tsx` line 6
- `src/frontend/src/features/exercises/components/TargetCapabilitiesSelector.tsx` line 13

Fix: Replace `IconButton` imports with `CobraIconButton` from `@/theme/styledComponents`.

---

### CR-C02: Pervasive Hardcoded Hex Colors Across ~45 Component Files

**Confidence: 85**

The COBRA convention requires using theme palette tokens instead of hardcoded hex values. Approximately 200+ hex literals appear across ~45 feature component files. The worst offenders are:

- `src/frontend/src/features/injects/components/InjectStatusChip.tsx` — 16 hex values (complete status color map)
- `src/frontend/src/core/components/PendingActionsPopover.tsx` — 10+ hex values for action type colors
- `src/frontend/src/features/observations/components/RatingBadge.tsx` — 15 hex values for rating colors
- `src/frontend/src/features/notifications/hooks/useNotificationToast.ts` — 6 hex values for toast styles
- `src/frontend/src/features/notifications/components/NotificationItem.tsx` — 8 hex values

The `cobraTheme.ts` palette already defines semantic tokens (e.g., `theme.palette.buttonPrimary.main` for navy blue, `theme.palette.buttonDelete.main` for red, `theme.palette.success.main` for green, `theme.palette.notifications.*` for toast colors). Many of these hardcoded values duplicate existing palette entries.

Priority fix pattern:
```typescript
// Before
style={{ color: '#1e3a5f' }}

// After
const theme = useTheme()
style={{ color: theme.palette.buttonPrimary.main }}
```

For color maps used in status chips and rating badges, extend the theme's palette with semantic tokens (`theme.palette.injectStatus.*`, `theme.palette.rating.*`) rather than using inline object literals.

---

### CR-C03: `SplashScreen.tsx` (New Uncommitted File) Contains Multiple Hardcoded Colors

**Confidence: 88**

The new `src/frontend/src/core/components/SplashScreen.tsx` file (uncommitted, from the active splash screen work) contains 4 hardcoded hex values on lines 66, 84, 106, and 133:
- `#1e3a5f` (navy blue — duplicates `theme.palette.buttonPrimary.main`)
- `rgba(0, 0, 0, 0.5)` (overlay)
- `#ffffff` (white text)

Since this file is new and not yet committed, it is the right time to correct this before it enters version history.

---

### CR-M01: Backend Metrics Services Lack Tests (5 Interfaces, Zero Tests)

**Confidence: 82**

Five metrics service interfaces in `Cadence.Core/Features/Metrics/Services/` have no corresponding test files:
- `IInjectMetricsService`
- `IObservationMetricsService`
- `IProgressMetricsService`
- `ITimelineMetricsService`
- `IEvaluatorCoverageService` (implied by frontend `useEvaluatorCoverage`)

Only `ExerciseMetricsServiceTests.cs` exists. The metrics feature drives the `ExerciseMetricsPage` which aggregates data across injects, observations, and capabilities. These services contain aggregation logic that is high-value to test.

---

### CR-M02: `MselService` Has Zero Test Coverage

**Confidence: 85**

`src/Cadence.Core/Features/Msel/Services/MselService.cs` has no corresponding test file. The Msel is the core domain object for exercise conduct — it holds the ordered inject list. The service builds `MselSummaryDto` objects and handles navigation. Per TDD mandate, all services require tests.

---

### CR-M03: Frontend `metrics` Feature Has Zero Tests

**Confidence: 88**

The entire `src/frontend/src/features/metrics/` feature module — 8 panel components and 8 metric hooks — has zero test files. Per the TDD mandate, hook tests are mandatory. This is the largest untested feature in the frontend codebase.

Priority hooks to test first:
- `useInjectSummary` (most critical — drives real-time conduct dashboard)
- `useExerciseProgress`
- `useEvaluatorCoverage`

---

### CR-M04: `JwtTokenService.GenerateAccessToken` Has 5 Parameters — Refactor Recommended

**Confidence: 80**

`src/Cadence.Core/Features/Authentication/Services/JwtTokenService.cs` line 76:
```csharp
public (string Token, int ExpiresIn) GenerateAccessToken(
    UserInfo user,
    Guid? organizationId,
    string? orgName,
    string? orgSlug,
    string? orgRole)
```

The four organization-related parameters (`organizationId`, `orgName`, `orgSlug`, `orgRole`) form a natural value object. An `OrganizationContext` record already exists conceptually in the system (`ICurrentOrganizationContext`). Extracting these into a record would reduce the parameter list to 2 and make call sites clearer.

Recommended refactor:
```csharp
public record TokenOrganizationContext(
    Guid? OrganizationId,
    string? Name,
    string? Slug,
    string? Role);

public (string Token, int ExpiresIn) GenerateAccessToken(
    UserInfo user,
    TokenOrganizationContext? orgContext)
```

---

### CR-R01: Test Mock Over-Reliance on `as any` in Page Tests

**Confidence: 80**

Test files for page components use `as any` heavily to satisfy React Query hook mock return types:
- `ExerciseDetailPage.test.tsx` — 25 instances
- `ReportsPage.test.tsx` — 17 instances
- `CreateOrganizationPage.test.tsx` — 17 instances
- `EditOrganizationPage.test.tsx` — 16 instances

While `as any` in tests is generally acceptable, the volume here indicates that typed mock factory functions would improve test maintainability. A shared `createMockUseQueryResult<T>()` helper in the test utilities would eliminate these casts and catch type drift when DTOs change.

---

## Summary Statistics

| Metric | Count |
|--------|-------|
| Total COBRA raw-component violations (Button/TextField/IconButton) | 4 |
| Total hardcoded hex violations (production components) | ~200 instances / ~45 files |
| MUI icon import violations | 0 |
| Direct `toast` import violations | 0 |
| Ungated `console.log` violations | 0 |
| C# private field naming violations | 0 |
| TypeScript interface naming violations | 0 |
| Hook naming violations | 0 |
| Production `any` type (fixable) | 0 |
| Production `any` type (acceptable) | 2 |
| C# functions >4 params | 5 (1 significant, 4 borderline with CancellationToken) |
| Backend features with zero tests | 3 (Msel, ExpectedOutcomes, Feedback) |
| Backend service groups with weak coverage | 2 (Metrics x5, Eeg x2) |
| Frontend features with zero tests | 7 (metrics, delivery-methods, expected-outcomes, objectives, settings, feedback, autocomplete) |
| Total issues at confidence >= 80 | 7 (CR-C01, CR-C02, CR-C03, CR-M01, CR-M02, CR-M03, CR-M04) |
