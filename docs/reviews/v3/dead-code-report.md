# Dead Code & Orphaned Artifacts Report

> **Pass:** 3 (Metrics)
> **Date:** 2026-03-10
> **Scanner:** Dead Code & Orphaned Artifacts

---

## Summary

| Category | Count | Severity |
|----------|-------|----------|
| Duplicate exception classes (dead namespace) | 3 | Critical |
| Unmapped SignalR hub | 1 | Critical |
| Missing runtime dependency (`lodash`) | 1 | Critical |
| Dead UI branch (const `false` + unreachable dialog) | 2 | Major |
| Placeholder `alert()` replacing unimplemented feature | 1 | Major |
| Duplicate TODO comments (same ticket, two files) | 1 | Minor |
| Commented-out code block | 1 | Minor |
| Placeholder test files (assert true) | 3 | Major |
| Misnamed interface file | 1 | Minor |

---

## Unused Exports

### Frontend

No barrel-file exports were found to be fully unreferenced after tracing all import chains. The following index files re-export from feature modules that ARE consumed by pages: `features/objectives/index.ts`, `features/expected-outcomes/index.ts`, `features/phases/index.ts`. All re-exports are imported by at least one consumer file.

No confidence-threshold violations found in this category.

### Backend

The three exception classes in `Cadence.Core/Infrastructure/Exceptions/` are the highest-confidence finding in this category — see DC-C01 below.

---

## Commented-Out Code Blocks

| File | Lines | Content Preview | Recommendation |
|------|-------|----------------|---------------|
| `src/Cadence.Core/Features/Authentication/Services/AuthenticationService.cs` | 438–448 | `// if (_options.Entra.Enabled) { methods.Add(new AuthMethod { Provider = "Entra" ... }) }` | Convert to GitHub issue for Entra/Microsoft SSO. Remove comment block. |

One block only. The comment is labelled "Future: Add Entra when enabled" and spans 9 lines of commented C# code. It is not documentation — it is an uncommitted feature stub. The intent is clear but the comment block itself constitutes dead code.

---

## TODO/FIXME/HACK Audit

| File | Line | Comment | Ticket | Recommendation |
|------|------|---------|--------|---------------|
| `src/Cadence.Core/Features/BulkParticipantImport/Services/BulkParticipantImportService.cs` | 30 | `TODO (CD-C01): Migrate _sessions to IDistributedCache` | CD-C01 | Convert to GitHub issue — this is a real production scalability risk. |
| `src/Cadence.Core/Features/BulkParticipantImport/Services/BulkParticipantImportService.cs` | 189 | `TODO: migrate _sessions to IDistributedCache (Redis/SQL)…` | CD-C01 | **Duplicate of line 30.** Remove — already tracked above. |
| `src/Cadence.Core/Features/ExcelImport/Services/ExcelImportService.cs` | 22 | `TODO (CD-C01): Migrate _sessions to IDistributedCache` | CD-C01 | Same issue, same ticket. Keep one authoritative comment. |
| `src/frontend/src/pages/PendingUserPage.tsx` | 31 | `TODO: Implement organization code redemption API call` | OM-08 | Convert to GitHub issue — contains a live `alert()` call that degrades UX. |
| `src/frontend/src/features/photos/pages/PhotoGalleryPage.tsx` | 148 | `TODO: Add SignalR real-time updates when backend implements PhotoAdded/PhotoDeleted events` | — | Convert to GitHub issue (Phase 2 scope). |
| `src/frontend/src/features/injects/pages/EditInjectPage.tsx` | 65 | `TODO: Track form changes for unsaved warning - for now always false` | — | See DC-M02 — this is not just a TODO, it causes dead UI. |
| `src/frontend/src/features/injects/pages/CreateInjectPage.tsx` | 89 | `TODO: Track form changes for unsaved warning - for now always false` | — | See DC-M02. |
| `src/frontend/src/features/eeg/components/EegEntryForm.tsx` | 369 | `TODO: Show discard confirmation dialog` | — | Convert to GitHub issue — both branches of `if (hasContent)` currently call `onClose?.()`. |

**Classification summary:**
- "Resolve now" (scalability / UX-blocking): CD-C01 (3 instances), PendingUserPage OM-08
- "Convert to GitHub issue": PhotoGalleryPage SignalR, EegEntryForm discard dialog
- "Remove (obsolete/duplicate)": `BulkParticipantImportService.cs` line 189 duplicate TODO

---

## Unused Dependencies

### Frontend (`package.json`)

| Package | Location | Used? | Evidence | Recommendation |
|---------|----------|-------|----------|---------------|
| `lodash` | NOT IN dependencies | Imported | `CreateOrganizationPage.tsx:32 import { debounce } from 'lodash'` | **CRITICAL: `lodash` is only in `devDependencies` as `@types/lodash`. The runtime package is missing from `dependencies`. Add `lodash` to `dependencies` or replace with native debounce.** |
| `@fortawesome/free-brands-svg-icons` | dependencies | Yes | `faGithub` used in admin components | Keep |
| `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities` | dependencies | Yes | Drag-and-drop in injects and EEG | Keep |
| `browser-image-compression` | dependencies | Yes | `useImageCompression.ts` | Keep |
| `html2canvas` | dependencies | Yes | `RatingChartsPanel.tsx` | Keep |
| `konva`, `react-konva` | dependencies | Yes | `AnnotationEditor.tsx` | Keep |
| `recharts` | dependencies | Yes | Metrics charts | Keep |
| `dexie` | dependencies | Yes | `core/offline/db.ts` | Keep |
| `react-markdown` | dependencies | Yes | EULA rendering | Keep |
| `zod` | dependencies | Yes | Exercise form validation | Keep |
| `date-fns` | dependencies | Yes | 36+ files | Keep |
| `@hookform/resolvers`, `react-hook-form` | dependencies | Yes | Exercise form | Keep |

### Backend (`.csproj`)

**`Cadence.Core.csproj`:**

| Package | Used? | Evidence | Recommendation |
|---------|-------|----------|---------------|
| `Azure.Communication.Email` | Yes | `AzureCommunicationEmailService.cs` | Keep |
| `ClosedXML` | Yes | Excel import/export services | Keep |
| `DocumentFormat.OpenXml` | Yes | `EegDocumentService.cs` | Keep |
| `ExcelDataReader` + `ExcelDataReader.DataSet` | Yes | `LegacyExcelReader.cs` | Keep |
| `FluentValidation` + DI Extensions | Yes | Multiple validators | Keep |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Yes | `AppDbContext`, `Program.cs` | Keep |
| `Microsoft.EntityFrameworkCore.*` | Yes | Widespread | Keep |
| `Microsoft.Extensions.Configuration` | Yes | `AppDbContextFactory.cs` | Keep |
| `Microsoft.Extensions.Configuration.EnvironmentVariables` | Yes | `AppDbContextFactory.cs` | Keep |
| `Microsoft.Extensions.Configuration.Json` | Yes | `AppDbContextFactory.cs` | Keep |
| `Microsoft.Extensions.Http` | Yes | `GitHubIssueService.cs` | Keep |
| `Microsoft.IdentityModel.Tokens` + `System.IdentityModel.Tokens.Jwt` | Yes | `JwtTokenService` | Keep |

**`Cadence.WebApi.csproj`:** All packages verified in use. No orphaned references found.

---

## Unused Variables/Imports (TypeScript Compiler)

| File | Line | Variable/Import | Issue |
|------|------|----------------|-------|
| `src/frontend/src/features/injects/pages/EditInjectPage.tsx` | 64 | `showUnsavedDialog` (`useState`) | State is set but `hasChanges` is permanently `false` so `setShowUnsavedDialog(true)` is never reached. Dialog at line 179 can never open. |
| `src/frontend/src/features/injects/pages/EditInjectPage.tsx` | 66 | `hasChanges` | Hardcoded `const hasChanges = false`. Dead constant — guards are always false. |
| `src/frontend/src/features/injects/pages/CreateInjectPage.tsx` | 88 | `showUnsavedDialog` (`useState`) | Same pattern — unreachable state. |
| `src/frontend/src/features/injects/pages/CreateInjectPage.tsx` | 90 | `hasChanges` | Hardcoded `const hasChanges = false`. Dead constant. |

---

## Issues

---

### DC-C01: Duplicate Exception Classes in Dead Namespace `Cadence.Core.Infrastructure.Exceptions`

**Severity:** Critical
**Confidence:** 100

Three exception classes exist in TWO namespaces:

- `Cadence.Core.Exceptions.BusinessRuleException` (`src/Cadence.Core/Exceptions/BusinessRuleException.cs`)
- `Cadence.Core.Infrastructure.Exceptions.BusinessRuleException` (`src/Cadence.Core/Infrastructure/Exceptions/BusinessRuleException.cs`)

Same pattern for `NotFoundException` and `ConflictException`.

The `Cadence.Core.Infrastructure.Exceptions` namespace is **never imported anywhere**. A grep for `using Cadence.Core.Infrastructure.Exceptions` returns zero matches across the entire codebase. The `Cadence.Core.Exceptions` namespace IS used (5 files import it).

The `Infrastructure.Exceptions` variants are also slightly less complete — they lack the zero-argument constructors that the `Core.Exceptions` variants provide.

**Files to delete:**
- `src/Cadence.Core/Infrastructure/Exceptions/BusinessRuleException.cs`
- `src/Cadence.Core/Infrastructure/Exceptions/ConflictException.cs`
- `src/Cadence.Core/Infrastructure/Exceptions/NotFoundException.cs`

---

### DC-C02: `NotificationHub` Mapped in Azure Functions but Unmapped in WebApi

**Severity:** Critical
**Confidence:** 95

`src/Cadence.WebApi/Hubs/NotificationHub.cs` defines an empty `Hub` subclass:

```csharp
public class NotificationHub : Hub
{
    // This hub is primarily used for server-to-client notifications.
    // Clients connect to receive updates.
}
```

`Program.cs` only maps one hub: `app.MapHub<ExerciseHub>("/hubs/exercise")`. `NotificationHub` is **never mapped**, so no SignalR clients can connect to it. It is not referenced anywhere else in WebApi or Core.

Separately, `src/Cadence.Functions/Hubs/NotificationHub.cs` is an Azure Functions SignalR negotiate stub — a different class entirely (not a `Hub` subclass). The WebApi file appears to be an abandoned placeholder from before the notification architecture was routed through `ExerciseHub` via `INotificationBroadcaster`.

**File to delete:** `src/Cadence.WebApi/Hubs/NotificationHub.cs`

---

### DC-C03: `lodash` Runtime Package Missing from `package.json` Dependencies

**Severity:** Critical
**Confidence:** 100

`src/frontend/src/features/organizations/pages/CreateOrganizationPage.tsx` line 32:

```typescript
import { debounce } from 'lodash'
```

`lodash` appears **only** in `devDependencies` as `@types/lodash: "^4.17.24"` (types only). The runtime `lodash` package is not listed in `dependencies`. Vite may resolve this during local dev via `node_modules` hoisting, masking the issue, but a clean production install (`npm ci`) will fail to resolve the import.

**Fix:** Add `"lodash": "^4.17.18"` to `dependencies` in `src/frontend/package.json`, OR replace the single usage with the native `useMemo`+callback debounce pattern already used elsewhere in the codebase (e.g., `useAutocomplete.ts` uses `useCallback` + `setTimeout`).

---

### DC-M01: Three Placeholder Test Files Containing Only `Assert.True(true)`

**Severity:** Major
**Confidence:** 100

Three test files contain only a placeholder test that will always pass regardless of functionality:

- `src/Cadence.WebApi.Tests/UnitTest1.cs` — class `UnitTest1`, method `PlaceholderTest_ShouldPass`
- `src/Cadence.Functions.Tests/PlaceholderTests.cs` — class `PlaceholderTests`
- `src/Cadence.Core.Tests/Helpers/PlaceholderTests.cs` — class `PlaceholderTests`

These inflate the test count without providing coverage. `Cadence.WebApi.Tests` and `Cadence.Functions.Tests` have no other test files — the projects exist but contain zero real tests.

**Recommendation:** Delete the placeholder files. Create GitHub issues to track test coverage for `Cadence.WebApi.Tests` (controller integration tests) and `Cadence.Functions.Tests` (function unit tests).

---

### DC-M02: Dead UI Branch — `hasChanges = false` Makes Unsaved Changes Dialog Unreachable in Two Pages

**Severity:** Major
**Confidence:** 100

Both `EditInjectPage.tsx` and `CreateInjectPage.tsx` contain this pattern:

```typescript
const [showUnsavedDialog, setShowUnsavedDialog] = useState(false)
// TODO: Track form changes for unsaved warning - for now always false
const hasChanges = false

const handleBackClick = () => {
  if (hasChanges) {          // ← always false
    setShowUnsavedDialog(true)
  } else {
    navigate(...)
  }
}
```

And then render a `<Dialog open={showUnsavedDialog}>` that can never open. The `useState` hook, four handler functions, and the full Dialog JSX are all dead code.

The `useUnsavedChangesWarning` hook already exists at `src/frontend/src/shared/hooks/useUnsavedChangesWarning.tsx` and is used in `ExerciseDetailPage.tsx` and `CreateExercisePage.tsx` — it should be used here too.

**Fix options:**
1. Replace `const hasChanges = false` with `const { hasChanges, ... } = useUnsavedChangesWarning(isDirty)` from the existing hook.
2. Or, remove the entire dialog block and the four dead handlers until the feature is properly implemented.

Files affected:
- `src/frontend/src/features/injects/pages/EditInjectPage.tsx` (lines 64–98, 179–194)
- `src/frontend/src/features/injects/pages/CreateInjectPage.tsx` (lines 88–130, 150–165)

---

### DC-M03: `PendingUserPage.tsx` Uses Native `alert()` as Feature Stub

**Severity:** Major
**Confidence:** 100

`src/frontend/src/pages/PendingUserPage.tsx` line 36:

```typescript
// TODO: Implement organization code redemption API call
// This will be part of P1 stories (OM-08)
devLog('Joining organization with code:', orgCode)

// For now, just show a message
alert('Organization code redemption will be implemented in a future release.')
```

`alert()` is a browser-blocking modal that bypasses the COBRA notification system (`notify` from `@/shared/utils/notify`). Users who navigate to this page and enter an org code will receive a native browser dialog — not a styled toast.

**Fix:** Replace `alert(...)` with `notify.info(...)` until the real API is implemented. Disable the input/button UI if the feature is not yet implemented rather than silently doing nothing after the alert is dismissed.

---

### DC-M04: Duplicate TODO Comments for the Same Tracked Issue (CD-C01)

**Severity:** Minor
**Confidence:** 100

The in-memory session limitation (single-instance only) is documented three times across two files:

- `BulkParticipantImportService.cs` line 30: `// TODO (CD-C01): Migrate _sessions to IDistributedCache`
- `BulkParticipantImportService.cs` line 189: `// TODO: migrate _sessions to IDistributedCache…` (no ticket reference)
- `ExcelImportService.cs` line 22: `// TODO (CD-C01): Migrate _sessions to IDistributedCache`

The duplicate at line 189 of `BulkParticipantImportService.cs` adds no information. It should be removed to keep the codebase comment-clean. The other two instances are appropriate as they appear at the field declaration where the limitation lives.

---

### DC-M05: Commented-Out Code Block — Entra SSO Stub in `AuthenticationService.cs`

**Severity:** Minor
**Confidence:** 100

`src/Cadence.Core/Features/Authentication/Services/AuthenticationService.cs` lines 438–448:

Nine lines of commented-out C# code for a Microsoft Entra authentication stub. Convert to a GitHub issue and delete the comment block.

---

### DC-M06: Interface Declared in Misnamed File — `NotificationBroadcaster.cs` Contains `INotificationBroadcaster`

**Severity:** Minor
**Confidence:** 100

`src/Cadence.Core/Features/Notifications/NotificationBroadcaster.cs` contains the **interface** `INotificationBroadcaster`, not a class. By .NET convention and the project's own patterns, interface files are named with an `I` prefix:

- `IExerciseHubContext.cs` → contains `IExerciseHubContext` ✓
- `NotificationBroadcaster.cs` → contains `INotificationBroadcaster` ✗

The WebApi implementation file `Cadence.WebApi/Hubs/NotificationBroadcaster.cs` is correctly named (it contains the concrete class). The Core file should be renamed to `INotificationBroadcaster.cs`.

---

## Scan Notes

- **`HseepRole` entity:** Confirmed used in `AppDbContext.HseepRoles` DbSet and seeded via `HseepRoleConfiguration`. Not referenced by any services at runtime but is a seeded lookup table — this is expected for reference data.
- **`Cadence.Functions/Hubs/NotificationHub.cs`:** The Functions version is a genuine Azure Functions SignalR negotiate endpoint stub, distinct from the WebApi dead stub in DC-C02. It is registered and plausibly active.
- **Azure.Communication.Email in Core:** Used exclusively by `AzureCommunicationEmailService`, which is the production email implementation. The package placement in Core (rather than WebApi) is intentional since Core owns all email service implementations.
- **`Microsoft.Extensions.Configuration.Json` and `.EnvironmentVariables` in Core:** Used directly by `AppDbContextFactory.cs` for EF migrations design-time support. Not dead.

---

*Report generated by Dead Code Scanner (Pass 3) — 2026-03-10*
