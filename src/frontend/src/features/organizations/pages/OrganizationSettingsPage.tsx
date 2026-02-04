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
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCog, faBuilding } from '@fortawesome/free-solid-svg-icons'
import { faHome } from '@fortawesome/free-solid-svg-icons'
import { useOrganization } from '@/contexts/OrganizationContext'
import { useBreadcrumbs } from '@/core/contexts'
import CobraStyles from '@/theme/CobraStyles'

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
      {/* Header - compact */}
      <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 2 }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.main',
            fontSize: 28,
          }}
        >
          <FontAwesomeIcon icon={faCog} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h5" fontWeight={600}>
            Organization Settings
          </Typography>
          <Typography variant="caption" color="text.secondary">
            General settings for {currentOrg?.name || 'your organization'}
          </Typography>
        </Box>
      </Stack>

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
