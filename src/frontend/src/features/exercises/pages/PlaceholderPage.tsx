/**
 * PlaceholderPage
 *
 * Temporary placeholder for features under development.
 * Shows a "Coming Soon" message with the feature name.
 */

import { useParams } from 'react-router-dom'
import { Box, Typography, Paper } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faHardHat, faHome } from '@fortawesome/free-solid-svg-icons'
import CobraStyles from '@/theme/CobraStyles'
import { useBreadcrumbs } from '@/core/contexts'
import { useExercise } from '../hooks'

interface PlaceholderPageProps {
  featureName: string
  description?: string
}

export const PlaceholderPage = ({ featureName, description }: PlaceholderPageProps) => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const { exercise } = useExercise(exerciseId)

  // Set breadcrumbs with exercise name
  useBreadcrumbs(
    exercise
      ? [
          { label: 'Home', path: '/', icon: faHome },
          { label: 'Exercises', path: '/exercises' },
          { label: exercise.name, path: `/exercises/${exerciseId}` },
          { label: featureName },
        ]
      : undefined,
  )

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
        {exercise && (
          <Typography variant="caption" color="text.disabled">
            Exercise: {exercise.name}
          </Typography>
        )}
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

export const ReportsPlaceholderPage = () => (
  <PlaceholderPage
    featureName="Reports"
    description="Generate and view exercise reports and after-action documentation."
  />
)

export const TemplatesPlaceholderPage = () => (
  <PlaceholderPage
    featureName="Templates"
    description="Manage inject templates and exercise blueprints."
  />
)

export default PlaceholderPage
