import { createBrowserRouter, RouterProvider, useNavigate, Outlet } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MobileBlocker, ProtectedRoute } from './core/components'
import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { Box, Typography, Stack, Card, CardContent } from '@mui/material'
import { ToastContainer } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faCheckCircle,
  faStickyNote,
  faUser,
  faPalette,
  faHome,
} from '@fortawesome/free-solid-svg-icons'

import { cobraTheme } from './theme/cobraTheme'
import { AppLayout } from './core/components/navigation'
import { PermissionRole } from './types'
import { NotesPage } from './tools/notes/pages/NotesPage'
import { AdminPage, FeatureFlagsProvider } from './admin'
import {
  ExerciseListPage,
  CreateExercisePage,
  ExerciseDetailPage,
} from './features/exercises'
import {
  InjectListPage,
  InjectDetailPage,
  CreateInjectPage,
  EditInjectPage,
} from './features/injects'
import { CobraPrimaryButton } from './theme/styledComponents'
import CobraStyles from './theme/CobraStyles'

// Create a client with sensible defaults for exercise management
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60, // 1 minute - data considered fresh
      retry: 1, // Only retry failed requests once
      refetchOnWindowFocus: false, // Don't refetch on window focus (we'll use SignalR for real-time)
    },
  },
})

/**
 * Home Page Component
 *
 * Landing page with quick links and overview
 */
const HomePage = () => {
  const navigate = useNavigate()

  const features = [
    { icon: faPalette, text: 'COBRA/C5 Design System theme' },
    { icon: faHome, text: 'Navigation layout (Header, Sidebar, Breadcrumbs)' },
    { icon: faUser, text: 'User profile with role switching' },
    { icon: faStickyNote, text: 'Notes tool example' },
  ]

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Typography variant="h4" gutterBottom>
        Welcome to Cadence
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 2 }}>
        This is a template application demonstrating COBRA styling patterns.
      </Typography>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Features
          </Typography>
          <Stack spacing={1}>
            {features.map((feature, index) => (
              <Stack
                key={index}
                direction="row"
                spacing={1.5}
                alignItems="center"
              >
                <FontAwesomeIcon
                  icon={faCheckCircle}
                  style={{ color: cobraTheme.palette.success.main }}
                />
                <FontAwesomeIcon icon={feature.icon} style={{ width: 16 }} />
                <Typography variant="body2">{feature.text}</Typography>
              </Stack>
            ))}
          </Stack>
        </CardContent>
      </Card>

      <CobraPrimaryButton onClick={() => navigate('/notes')}>
        Get Started with Notes
      </CobraPrimaryButton>
    </Box>
  )
}

/**
 * 404 Not Found Page Component
 *
 * Displayed when user navigates to a non-existent route
 */
const NotFoundPage = () => {
  const navigate = useNavigate()

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Typography variant="h4" gutterBottom>
        404 - Not Found
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        The page you're looking for doesn't exist.
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
 * Wraps all routes with the AppLayout shell
 */
const RootLayout = () => {
  return (
    <AppLayout>
      <Outlet />
    </AppLayout>
  )
}

/**
 * Router configuration using createBrowserRouter (Data Mode)
 *
 * This enables modern React Router features including:
 * - useBlocker for navigation blocking
 * - Data loading with loaders
 * - Error boundaries per route
 */
const router = createBrowserRouter([
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

      // Inject (MSEL) routes
      { path: 'exercises/:exerciseId/msel', element: <InjectListPage /> },
      { path: 'exercises/:exerciseId/injects/new', element: <CreateInjectPage /> },
      { path: 'exercises/:exerciseId/injects/:injectId', element: <InjectDetailPage /> },
      { path: 'exercises/:exerciseId/injects/:injectId/edit', element: <EditInjectPage /> },

      // Notes tool
      { path: 'notes', element: <NotesPage /> },

      // Admin page - protected
      {
        path: 'admin',
        element: (
          <ProtectedRoute requiredRole={PermissionRole.MANAGE}>
            <AdminPage />
          </ProtectedRoute>
        ),
      },

      // 404 fallback
      { path: '*', element: <NotFoundPage /> },
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
        <MobileBlocker>
          <FeatureFlagsProvider>
            <RouterProvider router={router} />
          </FeatureFlagsProvider>
        </MobileBlocker>

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
