/**
 * GlobalPlaceholderPage
 *
 * Temporary placeholder for top-level features under development.
 * Shows a "Coming Soon" message with the feature name.
 * Used for pages that are NOT exercise-scoped (e.g., /reports, /templates).
 */

import { Box, Typography, Paper } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faHardHat, faHome } from '@fortawesome/free-solid-svg-icons'
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core'
import CobraStyles from '@/theme/CobraStyles'
import { useBreadcrumbs } from '@/core/contexts'

interface GlobalPlaceholderPageProps {
  featureName: string
  description?: string
  icon?: IconDefinition
}

export const GlobalPlaceholderPage = ({
  featureName,
  description,
  icon,
}: GlobalPlaceholderPageProps) => {
  // Set breadcrumbs
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: featureName },
  ])

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Paper
        sx={{
          p: 4,
          textAlign: 'center',
          maxWidth: 500,
          mx: 'auto',
          mt: 4,
        }}
      >
        <FontAwesomeIcon
          icon={icon ?? faHardHat}
          size="3x"
          style={{ color: '#f59e0b', marginBottom: 16 }}
        />
        <Typography variant="h5" gutterBottom>
          {featureName}
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 2 }}>
          {description || 'This feature is coming soon.'}
        </Typography>
      </Paper>
    </Box>
  )
}

export default GlobalPlaceholderPage
