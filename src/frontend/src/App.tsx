import { useEffect } from 'react'
import { createBrowserRouter, RouterProvider, useNavigate, useLocation, Outlet } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MobileBlocker, ProtectedRoute, GlobalSyncStatus, UpdatePrompt, InstallBanner } from './core/components'
import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { Box, Typography } from '@mui/material'
import { ToastContainer } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'

import { faHome } from '@fortawesome/free-solid-svg-icons'
import { cobraTheme } from './theme/cobraTheme'
import { AppLayout } from './core/components/navigation'
import { BreadcrumbProvider, ConnectivityProvider, OfflineSyncProvider, useBreadcrumbs } from './core/contexts'
import { AuthProvider } from './contexts/AuthContext'
import { SystemRole } from './types'
import { AdminPage, ArchivedExercisesPage, FeatureFlagsProvider } from './admin'
import { HomePage } from './features/home'
import {
  ExerciseListPage,
  CreateExercisePage,
  ExerciseDetailPage,
  ExerciseConductPage,
} from './features/exercises'
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
 */
const RootLayout = () => {
  return (
    <ProtectedRoute>
      <BreadcrumbProvider>
        <AppLayout>
          <Outlet />
        </AppLayout>
      </BreadcrumbProvider>
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

  // Protected routes (require authentication)
  {
    path: '/',
    element: <RootLayout />,
    children: [
      // Home page
      { index: true, element: <HomePage /> },

      // Exercise routes
      { path: 'exercises', element: <ExerciseListPage /> },
      { path: 'exercises/new', element: <CreateExercisePage /> },
      { path: 'exercises/:id', element: <ExerciseDetailPage /> },
      { path: 'exercises/:id/edit', element: <ExerciseDetailPage /> },
      { path: 'exercises/:id/conduct', element: <ExerciseConductPage /> },

      // Inject (MSEL) routes
      { path: 'exercises/:exerciseId/msel', element: <InjectListPage /> },
      { path: 'exercises/:exerciseId/injects/new', element: <CreateInjectPage /> },
      { path: 'exercises/:exerciseId/injects/:injectId', element: <InjectDetailPage /> },
      { path: 'exercises/:exerciseId/injects/:injectId/edit', element: <EditInjectPage /> },

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
      <ThemeProvider theme={cobraTheme}>
        <CssBaseline />
        <AuthProvider>
          <ConnectivityProvider>
            <OfflineSyncProvider>
              <MobileBlocker>
                <FeatureFlagsProvider>
                  <RouterProvider router={router} />
                  <GlobalSyncStatus />
                  <UpdatePrompt />
                  <InstallBanner />
                </FeatureFlagsProvider>
              </MobileBlocker>
            </OfflineSyncProvider>
          </ConnectivityProvider>
        </AuthProvider>

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
