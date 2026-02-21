/**
 * OrganizationSettingsPage - OrgAdmin page for general organization settings
 *
 * This is a placeholder page for future general organization settings.
 * Feature flagged as "Coming Soon" until settings are implemented.
 *
 * @module features/organizations/pages
 */
import type { FC } from 'react'
import {
  Box,
  Typography,
  Paper,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCog, faBuilding } from '@fortawesome/free-solid-svg-icons'
import { faHome } from '@fortawesome/free-solid-svg-icons'
import { useOrganization } from '@/contexts/OrganizationContext'
import { useBreadcrumbs } from '@/core/contexts'
import CobraStyles from '@/theme/CobraStyles'
import { PageHeader } from '@/shared/components'

export const OrganizationSettingsPage: FC = () => {
  const { currentOrg } = useOrganization()

  // Set breadcrumbs
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'Organization', path: '/organization/details', icon: faBuilding },
    { label: 'Settings' },
  ])

  return (
    <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
      <PageHeader
        title="Organization Settings"
        icon={faCog}
        subtitle={`General settings for ${currentOrg?.name || 'your organization'}`}
        mb={2}
      />

      {/* Placeholder Content */}
      <Paper sx={{ p: 4, textAlign: 'center' }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'text.secondary',
            fontSize: 48,
            mb: 2,
          }}
        >
          <FontAwesomeIcon icon={faCog} />
        </Box>
        <Typography variant="h6" color="text.secondary" gutterBottom>
          Coming Soon
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Additional organization settings will be available in a future release.
        </Typography>
      </Paper>
    </Box>
  )
}

export default OrganizationSettingsPage
