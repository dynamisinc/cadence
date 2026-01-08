import { BrowserRouter, Routes, Route, useNavigate } from 'react-router-dom'
import { ProtectedRoute } from './core/components/ProtectedRoute'
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
import { CobraPrimaryButton } from './theme/styledComponents'
import CobraStyles from './theme/CobraStyles'

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
        Welcome to Dynamis Reference App
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
 * Main Application Component
 *
 * Provides:
 * - MUI Theme (COBRA styling)
 * - React Router for navigation
 * - AppLayout with Header, Sidebar, Breadcrumbs
 * - Toast notifications
 */
function App() {
  return (
    <ThemeProvider theme={cobraTheme}>
      <CssBaseline />
      <FeatureFlagsProvider>
        <BrowserRouter>
          <AppLayout>
            <Routes>
              {/* Home page */}
              <Route path="/" element={<HomePage />} />

              {/* Notes tool */}
              <Route path="/notes" element={<NotesPage />} />

              {/* Admin page - protected */}
              <Route
                path="/admin"
                element={
                  <ProtectedRoute requiredRole={PermissionRole.MANAGE}>
                    <AdminPage />
                  </ProtectedRoute>
                }
              />

              {/* 404 fallback */}
              <Route path="*" element={<NotFoundPage />} />
            </Routes>
          </AppLayout>
        </BrowserRouter>
      </FeatureFlagsProvider>

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
  )
}

export default App
