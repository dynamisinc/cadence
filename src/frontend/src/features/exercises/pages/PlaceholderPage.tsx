/**
 * PlaceholderPage
 *
 * Temporary placeholder for features under development.
 * Shows a "Coming Soon" message with the feature name.
 */

import { useParams } from 'react-router-dom'
import { Box, Typography, Paper } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faHardHat } from '@fortawesome/free-solid-svg-icons'
import CobraStyles from '@/theme/CobraStyles'

interface PlaceholderPageProps {
  featureName: string
  description?: string
}

export const PlaceholderPage = ({ featureName, description }: PlaceholderPageProps) => {
  const { id: exerciseId } = useParams<{ id: string }>()

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
          icon={faHardHat}
          size="3x"
          style={{ color: '#f59e0b', marginBottom: 16 }}
        />
        <Typography variant="h5" gutterBottom>
          {featureName}
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 2 }}>
          {description || 'This feature is coming soon.'}
        </Typography>
        <Typography variant="caption" color="text.disabled">
          Exercise ID: {exerciseId}
        </Typography>
      </Paper>
    </Box>
  )
}

// Pre-configured placeholder pages for each feature
export const ObservationsPlaceholderPage = () => (
  <PlaceholderPage
    featureName="Observations"
    description="View and record evaluator observations during exercise conduct."
  />
)

export const ParticipantsPlaceholderPage = () => (
  <PlaceholderPage
    featureName="Participants"
    description="Manage exercise participants and role assignments."
  />
)

export const MetricsPlaceholderPage = () => (
  <PlaceholderPage
    featureName="Metrics"
    description="View exercise performance metrics and analytics."
  />
)

export const SettingsPlaceholderPage = () => (
  <PlaceholderPage
    featureName="Settings"
    description="Configure exercise settings and preferences."
  />
)

export default PlaceholderPage
