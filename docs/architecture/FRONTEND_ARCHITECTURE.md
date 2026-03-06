# Frontend Architecture

> **Last Updated:** 2026-03-06 | **Version:** 1.0

Complete architecture map of the Cadence React SPA.

---

## Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19.x | UI framework |
| TypeScript | 5.x | Type safety |
| Vite | 7.x | Build tool + dev server (port 5197) |
| Material-UI | 7.x | Component library (via COBRA wrappers) |
| FontAwesome | 7.x | Icon library (mandatory, no MUI icons) |
| React Query | 5.x | Server state management |
| React Router | 7.x | Client-side routing (Data Mode) |
| React Hook Form + Zod | 7.x / 4.x | Form management + validation |
| Axios | 1.x | HTTP client with auth interceptors |
| SignalR | 10.x | Real-time WebSocket client |
| Dexie | 4.x | IndexedDB wrapper (offline support) |
| Vitest | 4.x | Test runner |
| React Testing Library | 16.x | Component testing |

---

## Directory Structure

```
src/frontend/src/
├── App.tsx                  # Router + context provider stack (545 lines)
├── main.tsx                 # Entry point
│
├── core/                    # App-wide infrastructure
│   ├── components/          # Global UI: ErrorBoundary, ProtectedRoute, AppLayout
│   │   └── navigation/     # AppHeader, Sidebar, ExerciseSidebar, Breadcrumb
│   ├── contexts/            # BreadcrumbContext, ConnectivityContext, OfflineSyncContext
│   ├── offline/             # IndexedDB schema, cache/sync services
│   ├── services/            # api.ts (Axios), telemetry.ts (App Insights)
│   └── utils/               # Environment validation
│
├── contexts/                # Global React contexts
│   ├── AuthContext.tsx       # JWT tokens, login/logout, token refresh
│   └── OrganizationContext.tsx  # Org memberships, switching, pending state
│
├── features/                # 26 feature modules (see Feature Modules below)
│
├── shared/                  # Cross-feature reusable code
│   ├── components/          # ConfirmDialog, StatusChip, PageHeader, etc.
│   │   └── navigation/     # Menu configurations
│   ├── hooks/               # useSignalR, useDebounce, useConfirmDialog, etc.
│   ├── contexts/            # ExerciseNavigationContext
│   ├── constants/           # hseepGlossary, roleOrientation
│   └── utils/               # notify wrapper (toast deduplication)
│
├── theme/                   # COBRA styling system
│   ├── cobraTheme.ts        # MUI theme customization (light/dark)
│   ├── CobraStyles.ts       # Design constants (spacing, dimensions)
│   └── styledComponents/    # COBRA button/input components
│
├── types/                   # Global TypeScript types
│   └── index.ts             # ~593 lines: roles, statuses, enums, DTOs
│
└── pages/                   # Root-level pages
    └── PendingUserPage.tsx   # Shown when user has no org membership
```

---

## Routing Structure

**File:** `src/frontend/src/App.tsx`

```
AuthLayout (public, no shell)
├── /login                    → LoginPage
├── /register                 → RegisterPage
├── /forgot-password          → ForgotPasswordPage
├── /reset-password           → ResetPasswordPage
└── /invite/:code             → InviteAcceptPage

PublicLayout (no auth required, with shell)
└── /about                    → AboutPage

RootLayout (protected, full shell)
├── /                         → HomePage
├── /assignments              → MyAssignmentsPage
├── /settings                 → UserSettingsPage
│
├── /organization/*
│   ├── /members              → MembersPage
│   ├── /settings             → OrgSettingsPage
│   └── /suggestions          → SuggestionManagementPage
│
├── /exercises/*
│   ├── /                     → ExerciseListPage
│   ├── /create               → CreateExercisePage
│   ├── /:id                  → ExerciseDetailPage
│   ├── /:id/edit             → EditExercisePage
│   ├── /:id/conduct          → ExerciseConductPage (clock + injects)
│   ├── /:id/participants     → ParticipantsPage
│   ├── /:id/settings         → ExerciseSettingsPage
│   ├── /:id/metrics          → ExerciseMetricsPage
│   ├── /:id/eeg              → EegEntriesPage
│   ├── /:id/injects/*        → Inject CRUD routes
│   ├── /:id/observations/*   → Observation routes
│   └── /:id/photos/*         → Photo gallery routes
│
├── /admin/*
│   ├── /                     → AdminDashboardPage
│   ├── /organizations        → AdminOrganizationsPage
│   ├── /archived-exercises   → ArchivedExercisesPage
│   ├── /feature-flags        → FeatureFlagsPage
│   └── /feedback             → FeedbackReportListPage
│
└── /not-found                → 404 page
```

### Route Guards

| Guard | File | Purpose |
|-------|------|---------|
| `ProtectedRoute` | `core/components/ProtectedRoute.tsx` | Requires authentication |
| `OrgAdminRoute` | `core/components/` | Requires OrgAdmin or SystemAdmin |
| `PendingUserGuard` | `core/components/PendingUserGuard.tsx` | Redirects users without org |
| `FeatureFlagGuard` | `shared/components/FeatureFlagGuard.tsx` | Conditional on feature flag |
| `ExerciseContextWrapper` | `shared/components/ExerciseContextWrapper.tsx` | Wraps exercise-scoped routes |

---

## Context Provider Hierarchy

Providers wrap the app in this order (innermost = last):

```
1.  QueryClientProvider         React Query (staleTime: 1min, retry: 1)
2.    ThemeProvider              MUI base theme
3.      ErrorBoundary            Catches render errors
4.        AuthProvider           JWT tokens, login/logout, token refresh
5.          OrganizationProvider Memberships, org switching, pending state
6.            UserPreferencesProvider  Theme preference (light/dark/system)
7.              ThemedApp        Applies user-selected theme
8.                EulaGate       Agreement check before access
9.                  ExerciseNavigationProvider  Exercise context (sessionStorage)
10.                   ConnectivityProvider  Online/offline + SignalR state
11.                     OfflineSyncProvider  Offline action queue
12.                       MobileBlocker  Prevents mobile access
13.                         FeatureFlagsProvider  Feature flag state
14.                           NotificationToastProvider  Notification toasts
15.                             WhatsNewProvider  Release notes
16.                               RouterProvider  React Router
```

---

## State Management

### Server State: React Query

All API data uses React Query with custom hooks:

```
Component
  └── useExercises()                    # Custom hook
      └── useQuery({                    # React Query
            queryKey: ['exercises'],
            queryFn: exerciseService.getExercises
          })
          └── exerciseService           # API client (Axios)
              └── apiClient.get('/exercises')
```

**Configuration:**
- Stale time: 1 minute
- Retry: 1 attempt
- No refetch on window focus (SignalR handles cache invalidation)

### Client State: React Context

| Context | File | State |
|---------|------|-------|
| `AuthContext` | `contexts/AuthContext.tsx` | Access token (memory), user info, login/logout |
| `OrganizationContext` | `contexts/OrganizationContext.tsx` | Current org, memberships, switching |
| `ExerciseNavigationContext` | `shared/contexts/` | Current exercise ID, name, status, user role (sessionStorage) |
| `ConnectivityContext` | `core/contexts/` | Online/offline status, SignalR connection state |
| `OfflineSyncContext` | `core/contexts/` | Pending action queue, sync status |
| `FeatureFlagsContext` | `admin/contexts/` | Feature flag values |
| `BreadcrumbContext` | `core/contexts/` | Dynamic breadcrumb trail |

---

## API Layer

**File:** `src/frontend/src/core/services/api.ts`

### Axios Client Configuration
- Base URL: `${VITE_API_URL}/api`
- Credentials: `withCredentials: true` (for httpOnly cookies)
- Request interceptor: Adds `Authorization: Bearer <token>` + `X-Correlation-Id`
- Response interceptor: Handles 401 with single-flight token refresh

### Feature Service Pattern

Each feature has a service file:

```
features/{feature}/services/{feature}Service.ts

export const exerciseService = {
  getExercises: () => apiClient.get<ExerciseDto[]>('/exercises'),
  getExercise: (id: string) => apiClient.get<ExerciseDto>(`/exercises/${id}`),
  createExercise: (req: CreateExerciseRequest) =>
    apiClient.post<ExerciseDto>('/exercises', req),
  // ...
};
```

### Feature Hook Pattern

Each feature wraps services in React Query hooks:

```
features/{feature}/hooks/use{Feature}.ts

export const useExercises = () => {
  return useQuery({
    queryKey: ['exercises'],
    queryFn: () => exerciseService.getExercises(),
  });
};

export const useCreateExercise = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: exerciseService.createExercise,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['exercises'] }),
  });
};
```

---

## COBRA Styling System

**Documentation:** `docs/COBRA_STYLING.md`

### Rules

- **Never** import raw MUI `Button`, `TextField` - use COBRA wrappers
- **Never** use `@mui/icons-material` - use FontAwesome exclusively
- **Never** import `toast` from `react-toastify` - use `notify` wrapper

### COBRA Components

**Location:** `src/frontend/src/theme/styledComponents/`

| Component | Use Case |
|-----------|----------|
| `CobraPrimaryButton` | Primary actions (Save, Create, Fire) |
| `CobraSecondaryButton` | Secondary actions (Cancel, Back) |
| `CobraDeleteButton` | Destructive actions (Delete, Remove) |
| `CobraLinkButton` | Text-only actions |
| `CobraIconButton` | Icon-only buttons |
| `CobraTextField` | All text inputs |

### Icons

```typescript
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faPlus, faTrash, faPen, faCheck, faXmark, faSpinner } from '@fortawesome/free-solid-svg-icons';
```

### Toast Notifications

**File:** `src/frontend/src/shared/utils/notify.ts`

```typescript
import { notify } from '@/shared/utils/notify';

notify.success('Exercise created');     // Auto-deduplicates within 3 seconds
notify.error('Failed to save');
notify.warning('Connection lost', { toastId: 'conn', autoClose: false });
notify.dismiss('conn');                 // Dismiss specific toast
```

---

## Feature Modules

**Location:** `src/frontend/src/features/`

Each module follows this structure:

```
features/{featureName}/
├── components/      # UI components (no direct API calls)
├── hooks/           # Custom hooks (React Query + business logic)
├── services/        # API client functions
├── pages/           # Route page components
├── types/           # Feature-specific TypeScript types
├── utils/           # Helper functions
└── index.ts         # Public barrel export
```

### Module Inventory

| Module | Purpose | Key Pages |
|--------|---------|-----------|
| `exercises` | Exercise CRUD, lifecycle, setup | List, Create, Detail, Conduct |
| `injects` | Inject CRUD, firing, MSEL management | MSEL List, Detail, Create/Edit |
| `observations` | Evaluator note-taking | ObservationsPage |
| `exercise-clock` | Real-time clock sync | Embedded in ConductPage |
| `auth` | Login, register, password reset | Login, Register, ForgotPassword |
| `organizations` | Multi-tenancy, members, invites | List, Details, Members, Settings |
| `users` | User management (admin) | UserListPage |
| `assignments` | Role-based task routing | MyAssignmentsPage |
| `settings` | User preferences (theme, format) | UserSettingsPage |
| `admin` | System administration | AdminPage, FeatureFlags |
| `capabilities` | Capability library | CapabilityLibraryPage |
| `delivery-methods` | Inject delivery type management | DeliveryMethodsPage |
| `autocomplete` | Search suggestion management | SuggestionManagementPage |
| `metrics` | Exercise analytics dashboard | ExerciseMetricsPage |
| `eeg` | Exercise Evaluation Guide | EegEntriesPage |
| `expected-outcomes` | Inject outcome definitions | Embedded in exercise pages |
| `objectives` | Exercise objectives | Embedded in exercise pages |
| `phases` | Exercise time segments | Embedded in exercise pages |
| `photos` | Photo capture and gallery | PhotoGalleryPage, PhotoTrashPage |
| `excel-import` | Upload and map Excel MSELs | ExcelImportDialog (modal) |
| `excel-export` | Generate Excel MSELs | Hook-based (no page) |
| `feedback` | User feedback submission | FeedbackPage |
| `notifications` | Toast/bell notifications | NotificationBell (global) |
| `version` | App version, release notes | AboutPage, WhatsNewPage |
| `eula` | Terms acceptance gate | EulaGate (global) |
| `home` | Dashboard landing page | HomePage |

---

## Offline / PWA Architecture

### IndexedDB Schema (Dexie)

**File:** `src/frontend/src/core/offline/db.ts`

| Table | Purpose |
|-------|---------|
| `exercises` | Cached exercise data |
| `phases` | Exercise phases |
| `injects` | Cached injects with sync status |
| `observations` | Cached observations with sync status |
| `photos` | Photo metadata + blob references |
| `pendingActions` | Queue of actions created offline |
| `deletedItems` | Track deletions for sync |
| `syncMetadata` | Last sync timestamps |

### Offline Flow

```
User goes offline
  ├── Read: Serve from IndexedDB cache
  ├── Write: Queue to pendingActions table
  └── UI: Show GlobalSyncStatus indicator

User comes online
  ├── ConnectivityContext detects online
  ├── OfflineSyncProvider retries pendingActions in order
  ├── On success: Remove from queue, invalidate React Query cache
  ├── On conflict: Show ConflictDialog (server vs local)
  └── UI: Clear GlobalSyncStatus
```

### Service Worker

- Workbox with precaching (4 MiB limit)
- API calls: `NetworkOnly` (app-level sync handles caching)
- Fonts: `CacheFirst` (1-year expiry)
- Images: `CacheFirst` (30-day expiry)
- PWA update: `autoUpdate` (UAT/dev) vs `prompt` (production)

---

## SignalR Integration

### Connection Hook

**File:** `src/frontend/src/shared/hooks/useSignalR.ts`

Manages WebSocket connection lifecycle with automatic reconnection.

### Exercise-Scoped Subscriptions

SignalR events trigger React Query cache invalidation:

```typescript
connection.on('InjectFired', (inject) => {
  queryClient.invalidateQueries({ queryKey: ['injects', exerciseId] });
});
```

**Key pattern:** Events invalidate query caches rather than updating state directly, ensuring consistency via API re-fetch.

See [SIGNALR_EVENTS.md](./SIGNALR_EVENTS.md) for the complete event catalog.

---

## Authentication Flow

### Token Management
- **Access token:** In-memory (React state) - never persisted to storage
- **Refresh token:** httpOnly cookie (browser-managed)
- **Proactive refresh:** Timer fires 2 minutes before expiry
- **Single-flight:** Concurrent 401 responses share one refresh request
- **Cross-tab logout:** `storage` event listener detects logout in other tabs

### Interceptor Chain

```
Every API request:
  ├── Request interceptor:
  │   ├── Add "Authorization: Bearer <token>"
  │   └── Add "X-Correlation-Id" (uuid)
  │
  └── Response interceptor on 401:
      ├── Is refresh already in-flight? → Wait for shared promise
      ├── Call refreshAccessToken()
      ├── On success: Retry original request
      └── On failure: Clear token, redirect to /login
```

---

## Type System

### Global Types

**File:** `src/frontend/src/types/index.ts` (~593 lines)

Defines all shared enums, interfaces, and helper functions:

| Category | Key Types |
|----------|-----------|
| Roles | `SystemRole`, `HseepRole`, `HSEEP_ROLES` array with metadata |
| Exercise | `ExerciseType`, `ExerciseStatus`, `ExerciseDto`, `CreateExerciseRequest` |
| Inject | `InjectType`, `InjectStatus`, `InjectDto`, `ACTIVE_INJECT_STATUSES`, `TERMINAL_INJECT_STATUSES` |
| Clock | `ExerciseClockState`, `DeliveryMode`, `TimelineMode` |
| Observation | `ObservationRating` with labels and helpers |
| Approval | `ApprovalPolicy`, `SelfApprovalPolicy`, `ApprovalRoles`, `ApprovalPermissionResult` |

### Feature Types

Each feature has local types in `features/{feature}/types/index.ts` for DTOs, form values, and local state.

---

## Test Infrastructure

### Configuration

| Setting | Value |
|---------|-------|
| Runner | Vitest 4 |
| Environment | jsdom |
| Pool | forks (isolation for stability) |
| Timeout | 10 seconds |
| CI bail | 10 failures |
| Coverage | v8 provider |

### Test Patterns

```typescript
// Component test
describe('ExerciseTable', () => {
  it('renders exercise list', () => {
    render(<ExerciseTable exercises={mockExercises} />);
    expect(screen.getByText('Hurricane TTX')).toBeInTheDocument();
  });
});

// Hook test
describe('useExercises', () => {
  it('fetches exercises on mount', async () => {
    const { result } = renderHook(() => useExercises());
    await waitFor(() => expect(result.current.data).toBeDefined());
  });
});
```

### Test File Count

~188 test files across features, with highest coverage in:
- `core/offline/` - Cache and sync service tests
- `features/injects/` - Filtering, sorting, grouping utilities
- `features/exercises/` - Service and hook tests
- `features/auth/` - Role resolution and permissions

---

## Build Configuration

### Scripts

| Script | Purpose | Safe with dev server? |
|--------|---------|----------------------|
| `npm run dev` | Start Vite dev server (port 5197) | - |
| `npm run type-check` | TypeScript checking only | Yes |
| `npm run build:check` | Type check without bundling | Yes |
| `npm run build` | Full tsc + vite build | No (can conflict) |
| `npm run test` | Vitest watch mode | Yes |
| `npm run test:ci` | Vitest with --bail=10 | Yes |
| `npm run lint` | ESLint | Yes |

### Environment Variables

| Variable | Dev | Production |
|----------|-----|------------|
| `VITE_API_URL` | `http://localhost:5071` | (empty, same-origin) |
| `VITE_APP_INSIGHTS` | (optional) | Connection string |
| `VITE_ENVIRONMENT` | `development` | `production` |
| `VITE_PWA_REGISTER_TYPE` | `autoUpdate` | `prompt` |
