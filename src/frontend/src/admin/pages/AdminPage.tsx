/**
 * Admin Page
 *
 * Central administration hub for the application:
 * - User Management
 * - Archived Exercises management
 * - Feature Flags management
 * - (Future) System settings
 *
 * This is a template page showing how admin features are organized.
 */

import React from 'react'
import { useNavigate } from 'react-router-dom'
import { Container, Stack, Box, Paper, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faGear, faBoxArchive, faChevronRight, faUsers } from '@fortawesome/free-solid-svg-icons'
import { FeatureFlagsAdmin } from '../components/FeatureFlagsAdmin'

export const AdminPage: React.FC = () => {
  const navigate = useNavigate()

  return (
    <Container maxWidth={false} disableGutters data-testid="admin-page">
      <Stack spacing={4} sx={{ p: 2 }}>
        {/* Page Header */}
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
            <FontAwesomeIcon icon={faGear} size="lg" />
            <Typography variant="h4" component="h1">
              Administration
            </Typography>
          </Box>
          <Typography variant="body1" color="text.secondary">
            Manage application settings and feature availability
          </Typography>
        </Box>

        {/* User Management Section */}
        <Paper
          sx={{
            p: 3,
            cursor: 'pointer',
            transition: 'box-shadow 0.2s',
            '&:hover': {
              boxShadow: 3,
            },
          }}
          onClick={() => navigate('/admin/users')}
          data-testid="user-management-section"
        >
          <Stack direction="row" alignItems="center" spacing={2}>
            <Box
              sx={{
                width: 48,
                height: 48,
                borderRadius: 1,
                backgroundColor: 'primary.light',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'primary.dark',
              }}
            >
              <FontAwesomeIcon icon={faUsers} size="lg" />
            </Box>
            <Box sx={{ flex: 1 }}>
              <Typography variant="h6" gutterBottom sx={{ mb: 0.5 }}>
                User Management
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Manage user accounts, assign HSEEP roles, and control access.
              </Typography>
            </Box>
            <FontAwesomeIcon icon={faChevronRight} />
          </Stack>
        </Paper>

        {/* Archived Exercises Section */}
        <Paper
          sx={{
            p: 3,
            cursor: 'pointer',
            transition: 'box-shadow 0.2s',
            '&:hover': {
              boxShadow: 3,
            },
          }}
          onClick={() => navigate('/admin/archived-exercises')}
          data-testid="archived-exercises-section"
        >
          <Stack direction="row" alignItems="center" spacing={2}>
            <Box
              sx={{
                width: 48,
                height: 48,
                borderRadius: 1,
                backgroundColor: 'warning.light',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'warning.dark',
              }}
            >
              <FontAwesomeIcon icon={faBoxArchive} size="lg" />
            </Box>
            <Box sx={{ flex: 1 }}>
              <Typography variant="h6" gutterBottom sx={{ mb: 0.5 }}>
                Archived Exercises
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Manage archived exercises. Restore them or permanently delete.
              </Typography>
            </Box>
            <FontAwesomeIcon icon={faChevronRight} />
          </Stack>
        </Paper>

        {/* Feature Flags Section */}
        <Paper sx={{ p: 3 }} data-testid="feature-flags-section">
          <FeatureFlagsAdmin />
        </Paper>

        {/* Placeholder for future admin sections */}
        <Paper sx={{ p: 3, opacity: 0.5 }} data-testid="future-settings-section">
          <Typography variant="h6" color="text.secondary" gutterBottom>
            System Settings
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Additional admin settings will be added here as the application
            grows. This section is a placeholder demonstrating the admin page
            structure.
          </Typography>
        </Paper>
      </Stack>
    </Container>
  )
}

export default AdminPage
