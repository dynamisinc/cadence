# Frontend Features Code Review

> **Review Type:** Frontend Features
> **Pass:** 1
> **Date:** 2026-03-06
> **Reviewer:** Frontend Feature Module Specialist — React feature architecture, hooks, services, page composition
> **Scope:** src/frontend/src/features/ — all 25 feature modules

---

## Reviewer Summary

- **Files reviewed:** 114
- **Issues found:** 15 total — Critical: 2, Major: 7, Minor: 6
- **Positive patterns identified:** 5
- **Estimated remediation effort:** Medium
- **Top 3 priorities for this domain:**
  1. Wrong query key in `useExcelImport` hook means the MSEL never refreshes after an Excel import completes — a functional bug
  2. `UserListPage` uses direct `useEffect`/`useState` pattern instead of React Query hooks, diverging from the established pattern across all other features
  3. Raw MUI `Button` imports in multiple feature components violate COBRA styling conventions

---

## Critical Issues

### FF-C01: `UserListPage` bypasses React Query with imperative `useEffect` pattern

- **File(s):** `src/frontend/src/features/users/pages/UserListPage.tsx`
- **Category:** Convention
- **Impact:** The Users feature is the only feature module in the entire codebase that manages server state with raw `useState`/`useEffect` instead of React Query hooks. This means: no caching, no stale-while-revalidate, no automatic refetching, no query deduplication, manual loading/error state management. It also means the feature cannot benefit from cache invalidation via SignalR events. Every other feature (exercises, injects, observations, etc.) correctly uses the `useQuery`/`useMutation` pattern through custom hooks.
- **Description:** `UserListPage` contains inline `useEffect` calls that fetch data with `await userService.getUsers()` and manage loading/error state manually. There is no `useUsers` hook. The page also contains mutation logic (activate, deactivate, delete) handled through inline async functions rather than `useMutation`.
- **Recommendation:** Create `src/frontend/src/features/users/hooks/useUsers.ts` following the same pattern as `useExercises.ts` or `useInjects.ts`. Extract `useUsers()` with `useQuery` and `useActivateUser()`, `useDeactivateUser()`, `useDeleteUser()` with `useMutation`. The page component should only orchestrate these hooks, not manage state directly. This brings the Users feature in line with the codebase standard.
- **Affected files:**
  - `src/frontend/src/features/users/pages/UserListPage.tsx`
  - `src/frontend/src/features/users/hooks/` (new `useUsers.ts` to create)

---

### FF-C02: Production `console.log` calls in `authService.ts` interceptors

- **File(s):** `src/frontend/src/features/auth/services/authService.ts:34-68`
- **Category:** Stability
- **Impact:** The auth service contains `console.log` calls in the axios request/response interceptors that fire on every API call made through the auth service. The `hasAccessToken` log in particular exposes authentication state information in production DevTools. These logs are not gated behind a development flag.
- **Description:** All `console.log` statements in `authService.ts` execute in production, leaking auth flow details (token presence, request URLs, response statuses) to anyone with browser DevTools open. This is the same class of issue as AR-M04 and FI-M01 but localized to the auth feature's service layer.
- **Recommendation:** Remove all `console.log` calls or replace with a dev-only logging utility gated on `import.meta.env.DEV`. Auth flow errors that need production visibility should use the telemetry service.
- **Affected files:** `src/frontend/src/features/auth/services/authService.ts`

---

## Major Issues

### FF-M01: Wrong query key in `useExcelImport` causes stale MSEL after import

- **File(s):** `src/frontend/src/features/excel-import/hooks/useExcelImport.ts:103`
- **Category:** Stability
- **Impact:** After a successful Excel import, the `onSuccess` callback invalidates `['injects', variables.exerciseId]` but the inject list query uses `injectKeys.all(exerciseId)` which produces a different key structure. This means the MSEL table does not refresh after import — the user must manually refresh the page to see imported injects. This is a functional bug.
- **Description:** The query key mismatch means `queryClient.invalidateQueries(['injects', variables.exerciseId])` does not match the actual query key used by `useInjects`, so the cache is never invalidated.
- **Recommendation:** Replace the hardcoded query key with `injectKeys.all(variables.exerciseId)` imported from the injects feature's query key factory. If no such factory exists, create one following the React Query key factory pattern.
- **Affected files:** `src/frontend/src/features/excel-import/hooks/useExcelImport.ts`

---

### FF-M02: Notification optimistic update uses hardcoded query key

- **File(s):** `src/frontend/src/features/notifications/hooks/useNotifications.ts`
- **Category:** Stability
- **Impact:** The `markAsRead` mutation's optimistic update uses a hardcoded `{ limit: 10 }` in the query key, but the actual notification query may use different pagination parameters. This means the optimistic update targets the wrong cache entry if the user has changed pagination settings or if the default limit changes.
- **Description:** The optimistic update assumes a specific query key shape that includes pagination parameters. This is fragile and will break if pagination defaults change.
- **Recommendation:** Use `queryClient.setQueriesData` with a partial key match (e.g., `['notifications']`) to update all notification cache entries regardless of pagination parameters. Alternatively, use `queryClient.invalidateQueries` for a simpler (non-optimistic) approach.
- **Affected files:** `src/frontend/src/features/notifications/hooks/useNotifications.ts`

---

### FF-M03: Inconsistent data fetching patterns across features

- **File(s):** Multiple features
- **Category:** Convention
- **Impact:** While most features follow the `hooks/use{Feature}.ts` → `useQuery`/`useMutation` pattern, several features diverge: Users (raw useEffect), some admin pages (inline fetch), and a few smaller features. This inconsistency makes it harder for developers to know which pattern to follow and complicates cache management.
- **Description:** The "golden path" is clear in exercises, injects, and observations: a custom hook wraps React Query. But not all features follow it consistently.
- **Recommendation:** Audit all features and ensure every feature that makes API calls has a dedicated `use{Feature}.ts` hook using React Query. Priority: Users (FF-C01), then any remaining features with inline fetch patterns.
- **Affected files:**
  - `src/frontend/src/features/users/` (no React Query hooks)
  - Various admin-level feature pages

---

### FF-M04: Raw MUI `Button` import in `PendingApprovalAlert`

- **File(s):** `src/frontend/src/features/injects/components/PendingApprovalAlert.tsx:9`
- **Category:** Convention
- **Impact:** Imports raw `Button` from `@mui/material` instead of using a COBRA button variant. CLAUDE.md explicitly prohibits raw MUI component imports.
- **Description:** The component uses `Button` for a "Review" action link. This should be a `CobraLinkButton` or `CobraPrimaryButton` depending on the visual intent.
- **Recommendation:** Replace `import { Button } from '@mui/material'` with `import { CobraLinkButton } from '@/theme/styledComponents'` and update the JSX accordingly.
- **Affected files:** `src/frontend/src/features/injects/components/PendingApprovalAlert.tsx`

---

### FF-M05: Raw MUI `Button` import in `VersionInfoCard`

- **File(s):** `src/frontend/src/features/whats-new/components/VersionInfoCard.tsx:1`
- **Category:** Convention
- **Impact:** Same violation as FF-M04 — imports raw `Button` from `@mui/material` instead of using a COBRA variant.
- **Description:** The "What's New" feature card uses a raw MUI Button for the dismiss/acknowledge action.
- **Recommendation:** Replace with appropriate COBRA button variant (`CobraSecondaryButton` or `CobraLinkButton`).
- **Affected files:** `src/frontend/src/features/whats-new/components/VersionInfoCard.tsx`

---

### FF-M06: Missing test coverage in complex feature modules

- **File(s):** Multiple feature directories
- **Category:** Stability
- **Impact:** Several features with significant business logic have zero or minimal test coverage. Without tests, regressions during the hardening process are likely.
- **Description:** Features like `excel-import`, `observations`, `exercise-clock`, `feedback`, and `notifications` contain complex state management, multi-step workflows, or real-time event handling but have no test files. The features with tests (exercises, injects) demonstrate good testing patterns that could be replicated.
- **Recommendation:** Prioritize adding tests for: (1) `useExcelImport` hook (validates the import flow and query invalidation — would catch FF-M01), (2) `useNotifications` hook (validates optimistic updates — would catch FF-M02), (3) `useExerciseClock` hook (validates timer state transitions).
- **Affected files:**
  - `src/frontend/src/features/excel-import/` (no test files)
  - `src/frontend/src/features/observations/` (no test files)
  - `src/frontend/src/features/exercise-clock/` (no test files)
  - `src/frontend/src/features/feedback/` (no test files)
  - `src/frontend/src/features/notifications/` (no test files)

---

### FF-M07: Large page components that should extract sub-components

- **File(s):** Multiple page files exceeding 300 lines
- **Category:** FileSize
- **Impact:** Large page components with mixed concerns (data fetching, state management, complex JSX rendering, event handlers) are harder to maintain, test, and reason about.
- **Description:** Several page components exceed 300 lines with inline JSX blocks that could be extracted into reusable or feature-specific components. Examples include inject detail pages, exercise management pages, and observation pages.
- **Recommendation:** For each large page: extract data table sections into `{Feature}Table` components, form sections into `{Feature}Form` components, and dialog/modal content into dedicated components. The page component should orchestrate hooks and compose extracted components.
- **Affected files:** Various page components across features (identify specific files during fix pass)

---

## Minor Issues

### FF-N01: Inconsistent query key patterns across features

- **File(s):** Various hooks across features
- **Category:** Convention
- **Description:** Some features use a query key factory pattern (`exerciseKeys.all()`, `exerciseKeys.detail(id)`), while others use inline array keys (`['notifications', { limit }]`). The factory pattern is more maintainable.
- **Recommendation:** Standardize on query key factories for all features. Create `{feature}Keys` objects in each feature's hooks directory.

---

### FF-N02: Missing loading states in some feature pages

- **File(s):** Various page components
- **Category:** Readability
- **Description:** Some pages render empty content while data is loading instead of showing a loading indicator. The `Loading` component exists in shared components but is not used consistently.
- **Recommendation:** Audit all pages and ensure `isLoading` states from React Query hooks render the shared `Loading` component.

---

### FF-N03: Service files with unused or commented-out methods

- **File(s):** Various service files
- **Category:** Orphaned
- **Description:** Some service files contain commented-out API call methods or methods that are defined but never imported elsewhere.
- **Recommendation:** Remove commented-out code and unused exports. If methods are planned for future use, document them in the feature's README.md instead.

---

### FF-N04: Inconsistent error handling in mutation callbacks

- **File(s):** Various hooks with `useMutation`
- **Category:** Stability
- **Description:** Some mutations use `onError` callbacks with `notify.error()`, some use try-catch in the calling component, and some have no error handling at all. The pattern should be consistent.
- **Recommendation:** Standardize: `onError` callback in the `useMutation` definition should call `notify.error()` with a user-friendly message. Components should not need their own try-catch for mutations.

---

### FF-N05: Feature service files sometimes import from other features' services

- **File(s):** Various service files
- **Category:** Convention
- **Description:** A few service files import types or functions from other feature modules' service files, creating cross-feature dependencies. Features should be as independent as possible.
- **Recommendation:** Move shared types to `src/frontend/src/shared/types/` and shared utilities to `src/frontend/src/shared/utils/`.

---

### FF-N06: Some features export components that are only used internally

- **File(s):** Various component index files
- **Category:** Orphaned
- **Description:** Some features export sub-components through barrel files (`index.ts`) that are only ever imported within the same feature. This pollutes the public API of the feature module.
- **Recommendation:** Only export components, hooks, and types that are consumed by other features or pages. Keep internal components as default exports within the feature directory.

---

## Positive Patterns

### FF-P01: Consistent hook-based data fetching in mature features

- **Where:** `src/frontend/src/features/exercises/hooks/useExercises.ts`, `src/frontend/src/features/injects/hooks/useInjects.ts`, `src/frontend/src/features/observations/hooks/useObservations.ts`
- **What:** The mature features demonstrate an excellent pattern: each feature has a dedicated hook file that wraps React Query, provides typed query/mutation functions, handles cache invalidation, and exposes a clean API to page components. This separates data concerns from UI concerns.
- **Replicate in:** All features, especially Users (FF-C01) and any features that still use inline fetch patterns.

---

### FF-P02: Feature module directory structure is consistent

- **Where:** All 25 feature modules follow the pattern: `components/`, `hooks/`, `pages/`, `services/`, `types/`
- **What:** The directory structure is remarkably consistent across features regardless of complexity. Even simple features like `delivery-methods` follow the same layout. This makes it easy to navigate and understand any feature.
- **Replicate in:** Maintain this pattern for all new features.

---

### FF-P03: Type-safe service layer

- **Where:** `src/frontend/src/features/*/services/*.ts`
- **What:** Service files consistently define typed API call functions that return `Promise<T>` with explicit TypeScript interfaces. This provides compile-time type safety from the API call through to the component.
- **Replicate in:** Continue enforcing typed service functions.

---

### FF-P04: `notify` wrapper used consistently across features

- **Where:** All feature modules that show toast notifications
- **What:** No feature directly imports `toast` from `react-toastify`. All use the `notify` wrapper from `@/shared/utils/notify`, providing consistent deduplication behavior.
- **Replicate in:** Continue enforcing this pattern.

---

### FF-P05: FontAwesome icons used consistently

- **Where:** All feature component files
- **What:** Features consistently use FontAwesome icons via `@fortawesome/react-fontawesome` instead of MUI icons. The icon choices are semantically appropriate (faPlus for create, faTrash for delete, etc.).
- **Replicate in:** Continue enforcing — no MUI icon imports.
