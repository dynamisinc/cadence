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
import { faGear, faHome } from '@fortawesome/free-solid-svg-icons'
import { FeatureFlagsAdmin } from '../components/FeatureFlagsAdmin'
import { SystemSettingsAdmin } from '../components/SystemSettingsAdmin'
import { PageHeader } from '@/shared/components'
import { useBreadcrumbs } from '@/core/contexts'
import CobraStyles from '@/theme/CobraStyles'

export const AdminPage: React.FC = () => {
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'System Settings' },
  ])

  return (
    <Container maxWidth={false} disableGutters data-testid="admin-page">
      <Stack spacing={4} sx={{ p: CobraStyles.Padding.MainWindow }}>
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
