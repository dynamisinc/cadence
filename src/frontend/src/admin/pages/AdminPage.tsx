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
import { Container, Stack, Paper } from '@mui/material'
import { faGear } from '@fortawesome/free-solid-svg-icons'
import { FeatureFlagsAdmin } from '../components/FeatureFlagsAdmin'
import { SystemSettingsAdmin } from '../components/SystemSettingsAdmin'
import { PageHeader } from '@/shared/components'

export const AdminPage: React.FC = () => {
  return (
    <Container maxWidth={false} disableGutters data-testid="admin-page">
      <Stack spacing={4} sx={{ p: 2 }}>
        <PageHeader
          title="System Settings"
          icon={faGear}
          subtitle="Manage platform-wide configuration and feature availability"
        />

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
