import { useEffect } from 'react'
import { createBrowserRouter, RouterProvider, useNavigate, useLocation, Outlet } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MobileBlocker, ProtectedRoute, OrgAdminRoute, PendingUserGuard, GlobalSyncStatus, UpdatePrompt, InstallBanner, ThemedApp, ErrorBoundary } from './core/components'
import { trackException } from './core/services/telemetry'
import { Box, Typography } from '@mui/material'
import { ToastContainer } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'

import { faHome } from '@fortawesome/free-solid-svg-icons'
import { cobraTheme } from './theme/cobraTheme'
import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { AppLayout } from './core/components/navigation'
import { BreadcrumbProvider, ConnectivityProvider, OfflineSyncProvider, useBreadcrumbs } from './core/contexts'
import { AuthProvider } from './contexts/AuthContext'
import { OrganizationProvider } from './contexts/OrganizationContext'
import { ExerciseNavigationProvider } from './shared/contexts'
import { UserPreferencesProvider } from './features/settings'
import { ExerciseContextWrapper, GlobalPlaceholderPage, FeatureFlagGuard } from './shared/components'
import { SystemRole } from './types'
import { AdminPage, ArchivedExercisesPage, FeatureFlagsProvider } from './admin'
import { HomePage } from './features/home'
import { PendingUserPage } from './pages/PendingUserPage'
import {
  ExerciseListPage,
  CreateExercisePage,
  ExerciseDetailPage,
  ExerciseConductPage,
  ExerciseParticipantsPage,
  ExerciseSettingsPage,
  ReportsPage,
} from './features/exercises'
import { UserSettingsPage } from './features/settings'
import { ExerciseMetricsPage } from './features/metrics'
import { ObservationsPage } from './features/observations'
import { EegEntriesPage } from './features/eeg'
import {
  InjectListPage,
  InjectDetailPage,
  CreateInjectPage,
  EditInjectPage,
} from './features/injects'
import {
  LoginPage,
  RegisterPage,
  ForgotPasswordPage,
  ResetPasswordPage,
} from './features/auth'
import { UserListPage } from './features/users'
import { CapabilityLibraryPage } from './features/capabilities'
import { MyAssignmentsPage } from './features/assignments'
import {
  OrganizationListPage,
  CreateOrganizationPage,
  EditOrganizationPage,
  OrganizationDetailsPage,
  OrganizationMembersPage,
  OrganizationApprovalPage,
  OrganizationSettingsPage,
} from './features/organizations'
import { NotificationToastProvider } from './features/notifications'
import { AboutPage, WhatsNewProvider } from './features/version'
import { CobraPrimaryButton } from './theme/styledComponents'
import CobraStyles from './theme/CobraStyles'

// Create a client with sensible defaults for exercise management
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60, // 1 minute - data considered fresh
      retry: 1, // Only retry failed requests once
      // Don't refetch on window focus (we'll use SignalR for real-time)
      refetchOnWindowFocus: false,
    },
  },
})

/**
 * Redirect component for invalid routes
 *
 * Captures the attempted path and redirects to /not-found with state
 */
const NotFoundRedirect = () => {
  const location = useLocation()
  const navigate = useNavigate()

  useEffect(() => {
    // Redirect to /not-found, replacing the invalid URL in history
    // Pass the attempted path so we can show it to the user
    navigate('/not-found', {
      replace: true,
      state: { attemptedPath: location.pathname },
    })
  }, [location.pathname, navigate])

  return null
}

/**
 * 404 Not Found Page Component
 *
 * Displayed when user navigates to a non-existent route
 */
const NotFoundPage = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const attemptedPath = (location.state as { attemptedPath?: string })?.attemptedPath

  // Set custom breadcrumb instead of auto-generating from invalid URL path
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'Page Not Found' },
  ])

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Typography variant="h4" gutterBottom>
        Page Not Found
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        {attemptedPath
          ? `The page "${attemptedPath}" doesn't exist.`
          : "The page you're looking for doesn't exist."}
      </Typography>
      <CobraPrimaryButton onClick={() => navigate('/')}>
        Go to Home
      </CobraPrimaryButton>
    </Box>
  )
}

/**
 * Root Layout Component
 *
 * Wraps all routes with the AppLayout shell and BreadcrumbProvider.
 * All routes under this layout require authentication.
 * PendingUserGuard redirects users without organization to /pending page.
 */
const RootLayout = () => {
  return (
    <ProtectedRoute>
      <PendingUserGuard>
        <BreadcrumbProvider>
          <AppLayout>
            <Outlet />
          </AppLayout>
        </BreadcrumbProvider>
      </PendingUserGuard>
    </ProtectedRoute>
  )
}

/**
 * Auth Layout Component
 *
 * Simple layout for authentication pages (login, register, etc.)
 * These pages do not require authentication.
 */
const AuthLayout = () => {
  return <Outlet />
}

/**
 * Public Layout Component
 *
 * Layout for public pages that don't require authentication but
 * benefit from the app shell (header, navigation).
 * Examples: About page, release notes
 */
const PublicLayout = () => {
  return (
    <BreadcrumbProvider>
      <AppLayout>
        <Outlet />
      </AppLayout>
    </BreadcrumbProvider>
  )
}

/**
 * Router configuration using createBrowserRouter (Data Mode)
 *
 * This enables modern React Router features including:
 * - useBlocker for navigation blocking
 * - Data loading with loaders
 * - Error boundaries per route
 *
 * Route structure:
 * - /login, /register, etc. - Public auth pages
 * - / and all other routes - Protected, require authentication
 */
const router = createBrowserRouter([
  // Authentication routes (public - no auth required)
  {
    element: <AuthLayout />,
    children: [
      { path: 'login', element: <LoginPage /> },
      { path: 'register', element: <RegisterPage /> },
      { path: 'forgot-password', element: <ForgotPasswordPage /> },
      { path: 'reset-password', element: <ResetPasswordPage /> },
    ],
  },

  // Public routes with app shell (no auth required)
  {
    element: <PublicLayout />,
    children: [
      // About page (version info and release notes) - publicly accessible
      { path: 'about', element: <AboutPage /> },
    ],
  },

  // Protected routes (require authentication)
  {
    path: '/',
    element: <RootLayout />,
    children: [
      // Home page
      { index: true, element: <HomePage /> },

      // Assignments page
      { path: 'assignments', element: <MyAssignmentsPage /> },

      // Reports page (top-level, not exercise-scoped)
      // Feature flagged - redirects when Hidden, shows placeholder when ComingSoon
      {
        path: 'reports',
        element: (
          <FeatureFlagGuard
            feature="reports"
            featureName="Organization Reports"
            description="Generate and view cross-exercise reports and analytics at the organization level."
          >
            {/* When Active, render the actual reports page (placeholder for now) */}
            <GlobalPlaceholderPage
              featureName="Organization Reports"
              description="Generate and view cross-exercise reports and analytics at the organization level."
            />
          </FeatureFlagGuard>
        ),
      },

      // Templates page (top-level, not exercise-scoped)
      // Feature flagged - redirects when Hidden, shows placeholder when ComingSoon
      {
        path: 'templates',
        element: (
          <FeatureFlagGuard
            feature="templates"
            featureName="Templates"
            description="Manage inject templates and exercise blueprints for reuse."
          >
            {/* When Active, render the actual templates page (placeholder for now) */}
            <GlobalPlaceholderPage
              featureName="Templates"
              description="Manage inject templates and exercise blueprints for reuse."
            />
          </FeatureFlagGuard>
        ),
      },

      // Settings page (top-level, not exercise-scoped)
      {
        path: 'settings',
        element: <UserSettingsPage />,
      },

      // Organization management routes (OrgAdmin or SysAdmin)
      {
        path: 'organization/details',
        element: (
          <OrgAdminRoute>
            <OrganizationDetailsPage />
          </OrgAdminRoute>
        ),
      },
      {
        path: 'organization/members',
        element: (
          <OrgAdminRoute>
            <OrganizationMembersPage />
          </OrgAdminRoute>
        ),
      },
      {
        path: 'organization/approval',
        element: (
          <OrgAdminRoute>
            <OrganizationApprovalPage />
          </OrgAdminRoute>
        ),
      },
      {
        path: 'organization/settings',
        element: (
          <OrgAdminRoute>
            <FeatureFlagGuard
              feature="orgSettings"
              featureName="Organization Settings"
              description="Configure general organization settings."
            >
              <OrganizationSettingsPage />
            </FeatureFlagGuard>
          </OrgAdminRoute>
        ),
      },
      {
        path: 'organization/capabilities',
        element: (
          <OrgAdminRoute>
            <CapabilityLibraryPage />
          </OrgAdminRoute>
        ),
      },
      {
        path: 'organization/archived',
        element: (
          <OrgAdminRoute>
            <ArchivedExercisesPage />
          </OrgAdminRoute>
        ),
      },

      // Pending user page (no organization assigned)
      { path: 'pending', element: <PendingUserPage /> },

      // Exercise list and create (no context needed)
      { path: 'exercises', element: <ExerciseListPage /> },
      { path: 'exercises/new', element: <CreateExercisePage /> },

      // Exercise-scoped routes (wrapped with ExerciseContextWrapper)
      {
        path: 'exercises/:id',
        element: <ExerciseContextWrapper />,
        children: [
          { index: true, element: <ExerciseDetailPage /> },
          { path: 'edit', element: <ExerciseDetailPage /> },
          { path: 'conduct', element: <ExerciseConductPage /> },
          { path: 'msel', element: <InjectListPage /> },
          { path: 'injects/new', element: <CreateInjectPage /> },
          { path: 'injects/:injectId', element: <InjectDetailPage /> },
          { path: 'injects/:injectId/edit', element: <EditInjectPage /> },
          { path: 'observations', element: <ObservationsPage /> },
          { path: 'eeg-entries', element: <EegEntriesPage /> },
          { path: 'participants', element: <ExerciseParticipantsPage /> },
          { path: 'reports', element: <ReportsPage /> },
          { path: 'metrics', element: <ExerciseMetricsPage /> },
          { path: 'settings', element: <ExerciseSettingsPage /> },
        ],
      },

      // Admin pages - Admin system role required
      {
        path: 'admin',
        element: (
          <ProtectedRoute requiredRole={SystemRole.Admin}>
            <AdminPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'admin/archived-exercises',
        element: (
          <ProtectedRoute requiredRole={SystemRole.Admin}>
            <ArchivedExercisesPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'admin/users',
        element: (
          <ProtectedRoute requiredRole={SystemRole.Admin}>
            <UserListPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'admin/capabilities',
        element: (
          <ProtectedRoute requiredRole={SystemRole.Admin}>
            <CapabilityLibraryPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'admin/organizations',
        element: (
          <ProtectedRoute requiredRole={SystemRole.Admin}>
            <OrganizationListPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'admin/organizations/new',
        element: (
          <ProtectedRoute requiredRole={SystemRole.Admin}>
            <CreateOrganizationPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'admin/organizations/:id',
        element: (
          <ProtectedRoute requiredRole={SystemRole.Admin}>
            <EditOrganizationPage />
          </ProtectedRoute>
        ),
      },

      // 404 page - explicit route
      { path: 'not-found', element: <NotFoundPage /> },

      // 404 fallback - redirects invalid URLs to /not-found
      { path: '*', element: <NotFoundRedirect /> },
    ],
  },
])

/**
 * Main Application Component
 *
 * Provides:
 * - MUI Theme (COBRA styling)
 * - React Router (Data Mode) for navigation with useBlocker support
 * - AppLayout with Header, Sidebar, Breadcrumbs
 * - Toast notifications
 */
function App() {
  return (
    <QueryClientProvider client={queryClient}>
      {/* Base theme for auth pages (before preferences load) */}
      <ThemeProvider theme={cobraTheme}>
        <CssBaseline />
        <ErrorBoundary
          onError={(error, errorInfo) => {
            trackException(error, {
              componentStack: errorInfo.componentStack || '',
              source: 'ErrorBoundary',
            })
          }}
        >
          <AuthProvider>
            {/* Organization provider loads after auth */}
            <OrganizationProvider>
              {/* User preferences provider loads after auth */}
              <UserPreferencesProvider>
                {/* ThemedApp applies dynamic theme based on user preferences */}
                <ThemedApp>
                  <ExerciseNavigationProvider>
                    <ConnectivityProvider>
                      <OfflineSyncProvider>
                        <MobileBlocker>
                          <FeatureFlagsProvider>
                            <NotificationToastProvider>
                              <WhatsNewProvider>
                                <RouterProvider router={router} />
                              </WhatsNewProvider>
                            </NotificationToastProvider>
                            <GlobalSyncStatus />
                            <UpdatePrompt />
                            <InstallBanner />
                          </FeatureFlagsProvider>
                        </MobileBlocker>
                      </OfflineSyncProvider>
                    </ConnectivityProvider>
                  </ExerciseNavigationProvider>
                </ThemedApp>
              </UserPreferencesProvider>
            </OrganizationProvider>
          </AuthProvider>
        </ErrorBoundary>

        <ToastContainer
          position="top-right"
          autoClose={3000}
          hideProgressBar={false}
          newestOnTop
          closeOnClick
          rtl={false}
          pauseOnFocusLoss
          draggable
          pauseOnHover
        />
      </ThemeProvider>
    </QueryClientProvider>
  )
}

export default App
