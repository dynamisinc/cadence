# Frontend Infrastructure Code Review

> **Review Type:** Frontend Infrastructure
> **Pass:** 1
> **Date:** 2026-03-06
> **Reviewer:** Frontend Infrastructure Specialist — React 19, TypeScript 5, MUI 7, React Query, custom hooks
> **Scope:** `src/frontend/src/` excluding `features/` — contexts, shared components/hooks, core services, theme, routing, offline

---

## Reviewer Summary

- **Files reviewed:** 50
- **Issues found:** 11 total — Critical: 2, Major: 5, Minor: 4
- **Positive patterns identified:** 7
- **Estimated remediation effort:** Medium
- **Top 3 priorities for this domain:**
  1. COBRA convention violations — raw MUI Button import and hardcoded hex colors in production components visible on every page
  2. Pervasive debug `console.log` statements in AuthContext (40+ occurrences) shipping to production with sensitive auth data
  3. DRY violations in SignalR hooks (duplicated ConnectionState type) and theme system (duplicated component overrides)

---

## Critical Issues

### FI-C01: Raw `@mui/material` `Button` used in `ProfileMenu` (COBRA violation)

- **File(s):** `src/frontend/src/core/components/ProfileMenu.tsx:14,140-185`
- **Category:** Convention
- **Impact:** ProfileMenu appears in the top-level AppHeader on every authenticated page. Using a raw MUI Button with custom `sx` styles bypasses the COBRA design system entirely.
- **Description:** `ProfileMenu` imports and renders a raw MUI `Button` directly. This is an explicit violation of the COBRA rule stated in CLAUDE.md: "NEVER import raw MUI components for styled elements. ALWAYS use COBRA components." The button applies custom `sx` styles for white text and transparent hover instead of using a COBRA variant.
- **Recommendation:** Replace with `CobraIconButton` or create a new `CobraHeaderButton` styled variant in `src/frontend/src/theme/styledComponents/` if the header-specific styling (white text, transparent hover) doesn't fit existing COBRA components. Do not use `sx` overrides to work around COBRA components.
- **Affected files:** `src/frontend/src/core/components/ProfileMenu.tsx`

---

### FI-C02: Hardcoded hex color strings in `ConnectionStatusIndicator` (COBRA violation)

- **File(s):** `src/frontend/src/core/components/ConnectionStatusIndicator.tsx:38,45,52,60,68`
- **Category:** Convention
- **Impact:** The `statusConfigs` object uses five hardcoded hex color literals (`#22c55e`, `#f59e0b`, `#ef4444`) that bypass the COBRA theme entirely. ConnectionStatusIndicator is rendered on every exercise page via AppHeader. These colors will not adapt to theme changes and are invisible to the design system.
- **Description:** COBRA_STYLING.md states: "NEVER Hardcode Colors — Use MUI `theme.palette.*` exclusively." Five hardcoded hex values map to Tailwind color names rather than COBRA theme tokens.
- **Recommendation:** Use `useTheme()` and map states to semantic palette colors: `theme.palette.success.main` for connected, `theme.palette.warning.main` for degraded, `theme.palette.error.main` for offline.
- **Affected files:** `src/frontend/src/core/components/ConnectionStatusIndicator.tsx`

---

## Major Issues

### FI-M01: Pervasive debug `console.log` statements in `AuthContext` (production logging)

- **File(s):** `src/frontend/src/contexts/AuthContext.tsx` (40+ occurrences across the file)
- **Category:** Stability
- **Impact:** Every user action triggers multiple log entries including sensitive data fields (userId, email, role, token refresh status). There is no conditional guarding on `import.meta.env.DEV`. These logs will appear for every user in every environment including production.
- **Description:** `AuthContext` contains extensive `console.log` debug output covering every code path — token refresh lifecycle, login, logout, registration, cross-tab events, and error classification. This is production code wrapping the entire application.
- **Recommendation:** Remove all `console.log` calls from `AuthContext`. Replace with a conditional dev-only logging utility gated on `import.meta.env.DEV`, or use the telemetry service (Application Insights) for auth errors that need production visibility.
- **Affected files:** `src/frontend/src/contexts/AuthContext.tsx`

---

### FI-M02: Hardcoded hex color string in `OrganizationSwitcher` (COBRA violation)

- **File(s):** `src/frontend/src/shared/components/OrganizationSwitcher.tsx:118-119`
- **Category:** Convention
- **Impact:** Two hardcoded `#ffffff` values appear as inline styles on the FontAwesome icon and Typography in the organization switcher, which is visible in the app header on every page.
- **Description:** Same category of COBRA violation as FI-C02. Even though white is less likely to change, the pattern is explicitly prohibited and bypasses theme switching if a light-mode variant is ever added.
- **Recommendation:** Replace with `theme.palette.common.white` via `useTheme()` or use `sx={{ color: 'common.white' }}` shorthand.
- **Affected files:** `src/frontend/src/shared/components/OrganizationSwitcher.tsx`

---

### FI-M03: `ConnectionState` type duplicated across two SignalR hooks

- **File(s):** `src/frontend/src/shared/hooks/useSignalR.ts:26-32` and `src/frontend/src/shared/hooks/useExerciseSignalR.ts:25-31`
- **Category:** DRY
- **Impact:** Both hooks define an identical `ConnectionState` union type independently. If a new state is ever added to one definition and not the other, components consuming either hook will behave inconsistently.
- **Description:** `useExerciseSignalR` does not use `useSignalR` internally — it manages its own connection. But the type is still duplicated verbatim. The shared `index.ts` only re-exports `ConnectionState` from `useSignalR`.
- **Recommendation:** Extract `ConnectionState` into a shared types file `src/frontend/src/shared/hooks/types.ts` and import it in both hooks.
- **Affected files:**
  - `src/frontend/src/shared/hooks/useSignalR.ts`
  - `src/frontend/src/shared/hooks/useExerciseSignalR.ts`

---

### FI-M04: Network error detection logic duplicated in `AuthContext` and `api.ts`

- **File(s):** `src/frontend/src/contexts/AuthContext.tsx:103-235` and `src/frontend/src/core/services/api.ts:142-167`
- **Category:** DRY
- **Impact:** Both files independently implement network error detection by checking the same string literals (`'Network Error'`, `'ECONNREFUSED'`, `'ERR_NETWORK'`). These two implementations can diverge silently.
- **Description:** The `classifyError` function in AuthContext and the 401 refresh interceptor in api.ts both contain nearly identical network error detection logic. If a new network error variant emerges, it must be added to both places independently.
- **Recommendation:** Extract a shared `isNetworkError(error: unknown): boolean` utility into `src/frontend/src/shared/utils/errorUtils.ts` and import it in both files.
- **Affected files:**
  - `src/frontend/src/contexts/AuthContext.tsx`
  - `src/frontend/src/core/services/api.ts`

---

### FI-M05: Theme component defaults duplicated verbatim in `cobraTheme.ts`

- **File(s):** `src/frontend/src/theme/cobraTheme.ts:248-312,548-613`
- **Category:** DRY
- **Impact:** The `components` override block (~65 lines covering MuiTextField, MuiAutocomplete, MuiSelect, MuiButtonGroup, MuiButton, MuiTableHead, MuiTableCell, MuiListItemIcon) appears twice: once in the static `cobraTheme` export and again verbatim in the `createCobraTheme(mode)` factory function. If a developer updates one and not the other, theming inconsistencies will arise.
- **Description:** These two blocks must be kept in sync manually. This creates a real maintenance hazard between dark and light mode contexts.
- **Recommendation:** Extract the component overrides into a shared constant `cobraComponentOverrides` and reference it in both `cobraTheme` and `createCobraTheme`.
- **Affected files:** `src/frontend/src/theme/cobraTheme.ts`

---

## Minor Issues

### FI-N01: `_tokenExpiry` state declared but value is never consumed in `AuthContext`

- **File(s):** `src/frontend/src/contexts/AuthContext.tsx:290`
- **Category:** Orphaned
- **Description:** `_tokenExpiry` state value is never read, but `setTokenExpiry` is called in 5 places. Every `setTokenExpiry` call triggers a re-render with no effect on rendered output. Token expiry is already tracked via the scheduled refresh timer using a `setTimeout` ref.
- **Recommendation:** Replace with a `useRef` if the expiry value needs to be read programmatically without triggering re-renders, or remove entirely if the scheduler ref is sufficient.

---

### FI-N02: `PaperProps` is deprecated in MUI v7; should use `slotProps`

- **File(s):** `src/frontend/src/core/components/ProfileMenu.tsx:192`
- **Category:** Convention
- **Description:** MUI v7 migrated component sub-part configuration to the `slotProps` API. `PaperProps` is the MUI v5/v6 pattern. While it may still work via compatibility shims, it will generate a deprecation warning.
- **Recommendation:** Replace `PaperProps={{ elevation: 3, sx: { ... } }}` with `slotProps={{ paper: { elevation: 3, sx: { ... } } }}`.

---

### FI-N03: `ProfileMenu` fetches exercise assignments imperatively instead of React Query

- **File(s):** `src/frontend/src/core/components/ProfileMenu.tsx:69-89`
- **Category:** Convention
- **Description:** Ad-hoc `useEffect` fetch pattern is inconsistent with the project-wide React Query standard. It does not benefit from caching (the request fires on every menu open), has no deduplication, and manages loading/error state manually.
- **Recommendation:** Replace with `useQuery` with appropriate `staleTime` and `enabled: open && !!user`.

---

### FI-N04: Admin route group repeats `ProtectedRoute` wrapper 9 times in `App.tsx`

- **File(s):** `src/frontend/src/App.tsx:374-446`
- **Category:** DRY
- **Description:** Nine admin routes each individually wrap their element in `<ProtectedRoute requiredRole={SystemRole.Admin}>`. React Router v7 supports layout routes which would eliminate all repetition.
- **Recommendation:** Use a parent route with `<ProtectedRoute requiredRole={SystemRole.Admin}><Outlet /></ProtectedRoute>` as the element, then define admin routes as `children`.

---

## Positive Patterns

### FI-P01: Single-flight token refresh with concurrent 401 deduplication

- **Where:** `src/frontend/src/core/services/api.ts` + `src/frontend/src/contexts/AuthContext.tsx`
- **What:** The `refreshAccessToken` implementation uses a module-level `Promise` reference to ensure concurrent 401 responses share a single token refresh request. This is the correct pattern for high-concurrency SPA authentication.
- **Replicate in:** Any future interceptor patterns that need request deduplication.

---

### FI-P02: Offline auth state preservation with graceful degradation

- **Where:** `src/frontend/src/contexts/AuthContext.tsx`
- **What:** The auth flow correctly handles network failure during token refresh by checking `navigator.onLine` and restoring user state from `localStorage` cache for offline mode. The two-layer offline detection is well-designed for the exercise-conduct use case.
- **Replicate in:** Document as mandated offline pattern in CODING_STANDARDS.md.

---

### FI-P03: `notify` wrapper with time-window deduplication

- **Where:** `src/frontend/src/shared/utils/notify.ts`
- **What:** Correctly encapsulates all `react-toastify` calls with a 3-second deduplication window and 3-toast maximum. The `_resetNotifyForTesting` export demonstrates deliberate testability design.
- **Replicate in:** Continue enforcing project-wide — no direct `toast` imports.

---

### FI-P04: `useConfirmDialog` imperative Promise pattern

- **Where:** `src/frontend/src/shared/hooks/useConfirmDialog.tsx`
- **What:** Uses a `Promise`-based imperative API (`await confirmDialog('Are you sure?')`) backed by a ref-stored resolve function. Avoids callback threading through component trees and is the correct React pattern for modal confirmation flows.
- **Replicate in:** Any future modal/dialog flows that need imperative invocation.

---

### FI-P05: Two-phase offline sync with ordered action processing

- **Where:** `src/frontend/src/core/offline/syncService.ts`
- **What:** Correctly processes data actions before blob/photo uploads in separate phases, preventing a photo upload from blocking a more critical inject status update. Exponential backoff on sync failures and the `syncResult` summary object are well-structured.
- **Replicate in:** Any future offline-capable features.

---

### FI-P06: `onReconnectedRef` pattern prevents stale closure in SignalR handler

- **Where:** `src/frontend/src/shared/hooks/useExerciseSignalR.ts`
- **What:** Using a mutable ref to hold the `onReconnected` callback and calling `onReconnectedRef.current?.()` inside the `onreconnected` handler prevents the stale-closure problem. This is a subtle but correct React+SignalR integration pattern.
- **Replicate in:** Any future SignalR hooks.

---

### FI-P07: `ProtectedRoute` allows offline access without redirecting to login

- **Where:** `src/frontend/src/core/components/ProtectedRoute.tsx`
- **What:** Correctly distinguishes between "not authenticated" (redirect to login) and "offline with cached auth" (allow access). Critical for the exercise-conduct use case where network drops mid-session.
- **Replicate in:** Any future auth guards.
