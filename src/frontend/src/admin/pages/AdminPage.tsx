/**
 * Admin Page
 *
 * Central administration hub for system-wide features:
 * - Feature Flags management
 * - System Settings (email configuration overrides)
 *
 * Note: User Management and Organizations are accessible via the SYSTEM menu.
 * Organization-scoped features (Capability Library, Archived Exercises)
 * have been moved to the Organization section in the sidebar.
 */

import React from 'react'
import { Container, Stack, Box, Paper, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faGear } from '@fortawesome/free-solid-svg-icons'
import { FeatureFlagsAdmin } from '../components/FeatureFlagsAdmin'
import { SystemSettingsAdmin } from '../components/SystemSettingsAdmin'

export const AdminPage: React.FC = () => {
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

        {/* System Settings Section */}
        <Paper sx={{ p: 3 }} data-testid="system-settings-section">
          <SystemSettingsAdmin />
        </Paper>

        {/* Feature Flags Section */}
        <Paper sx={{ p: 3 }} data-testid="feature-flags-section">
          <FeatureFlagsAdmin />
        </Paper>
      </Stack>
    </Container>
  )
}

export default AdminPage
