/**
 * TargetCapabilitiesDisplay - Read-only display of exercise target capabilities
 *
 * Shows target capabilities as chips with summary count.
 * Used in exercise detail view (non-editing mode).
 * Implements S04 acceptance criteria.
 *
 * @module features/exercises
 * @see docs/features/exercise-capabilities/S04-exercise-target-capabilities.md
 */

import { Box, Typography, Chip, Stack, Alert } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faShieldAlt, faInfoCircle } from '@fortawesome/free-solid-svg-icons'
import { useExerciseTargetCapabilities } from '../hooks/useExerciseCapabilities'

interface TargetCapabilitiesDisplayProps {
  /** Exercise ID */
  exerciseId: string
}

/**
 * Target Capabilities Display Component
 *
 * Features:
 * - Shows selected target capabilities as chips
 * - Displays count summary
 * - Empty state when no capabilities selected
 * - Loading state
 */
export const TargetCapabilitiesDisplay = ({
  exerciseId,
}: TargetCapabilitiesDisplayProps) => {
  const { data: targetCapabilities, isLoading } = useExerciseTargetCapabilities(exerciseId)

  if (isLoading) {
    return (
      <Box sx={{ mb: 3 }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
          <FontAwesomeIcon icon={faShieldAlt} />
          <Typography variant="subtitle2" fontWeight="bold">
            Target Capabilities
          </Typography>
        </Stack>
        <Typography variant="body2" color="text.secondary">
          Loading...
        </Typography>
      </Box>
    )
  }

  if (!targetCapabilities || targetCapabilities.length === 0) {
    return (
      <Box sx={{ mb: 3 }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
          <FontAwesomeIcon icon={faShieldAlt} />
          <Typography variant="subtitle2" fontWeight="bold">
            Target Capabilities
          </Typography>
        </Stack>
        <Alert severity="info" icon={<FontAwesomeIcon icon={faInfoCircle} />}>
          <Typography variant="body2">
            No target capabilities selected. Edit this exercise to specify which
            capabilities will be evaluated.
          </Typography>
        </Alert>
      </Box>
    )
  }

  return (
    <Box sx={{ mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
        <FontAwesomeIcon icon={faShieldAlt} />
        <Typography variant="subtitle2" fontWeight="bold">
          Target Capabilities
        </Typography>
        <Typography variant="body2" color="text.secondary">
          ({targetCapabilities.length} {targetCapabilities.length === 1 ? 'capability' : 'capabilities'})
        </Typography>
      </Stack>

      <Stack direction="row" flexWrap="wrap" gap={1}>
        {targetCapabilities.map(cap => (
          <Chip
            key={cap.id}
            label={cap.name}
            size="small"
            variant="outlined"
            color="primary"
          />
        ))}
      </Stack>

      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
        {targetCapabilities.length} {targetCapabilities.length === 1 ? 'capability' : 'capabilities'} targeted for evaluation
      </Typography>
    </Box>
  )
}

export default TargetCapabilitiesDisplay
