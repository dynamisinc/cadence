import { Box, Typography, LinearProgress, Stack } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faMapMarkerAlt,
  faBuilding,
  faClock,
  faBolt,
  faPlay,
  faPause,
  faStop,
} from '@fortawesome/free-solid-svg-icons'
import type { ExerciseDto } from '../types'
import type { ExerciseRole } from '@/features/auth'
import type { Theme } from '@mui/material/styles'

// =========================================================================
// Types
// =========================================================================

interface ExerciseDetailRowProps {
  /** Exercise data to display */
  exercise: ExerciseDto
  /** Whether the row is expanded (for animation) */
  isExpanded: boolean
  /** User's role in this exercise */
  userRole?: ExerciseRole
  /** Whether to show organization name (multi-org context) */
  showOrganization: boolean
}

// =========================================================================
// Helper Functions
// =========================================================================

/**
 * Format elapsed seconds as HH:MM:SS
 */
const formatElapsedTime = (seconds: number): string => {
  const hrs = Math.floor(seconds / 3600)
  const mins = Math.floor((seconds % 3600) / 60)
  const secs = seconds % 60

  return `${hrs.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
}

/**
 * Get clock state icon and color based on state
 */
const getClockDisplay = (
  clockState: string | null | undefined,
  theme: Theme,
): { icon: typeof faPlay; color: string } => {
  switch (clockState) {
    case 'Running':
      return { icon: faPlay, color: theme.palette.clockStatus.running }
    case 'Paused':
      return { icon: faPause, color: theme.palette.clockStatus.paused }
    case 'Stopped':
      return { icon: faStop, color: theme.palette.clockStatus.stopped }
    default:
      return { icon: faStop, color: theme.palette.clockStatus.stopped }
  }
}

// =========================================================================
// Main Component
// =========================================================================

/**
 * ExerciseDetailRow Component
 *
 * Displays expanded detail information for an exercise in a table row.
 * Shows location, organization, clock state, ready injects, and progress bar.
 *
 * Designed to be used within a Collapse component in ExerciseTable.
 */
export const ExerciseDetailRow = ({
  exercise,
  isExpanded: _isExpanded,
  userRole,
  showOrganization,
}: ExerciseDetailRowProps) => {
  const theme = useTheme()
  const clockDisplay = getClockDisplay(exercise.clockState, theme)
  const progressPercentage =
    exercise.injectCount > 0
      ? (exercise.firedInjectCount / exercise.injectCount) * 100
      : 0

  // Controller-specific: show ready injects count
  const showReadyInjects = Boolean(
    userRole === 'Controller' &&
    exercise.status === 'Active' &&
    (exercise.readyInjectCount ?? 0) > 0,
  )

  // Show clock info for active exercises
  const showClockInfo = exercise.status === 'Active' && exercise.elapsedSeconds !== undefined

  return (
    <Box
      sx={{
        px: 2,
        py: 1.5,
        bgcolor: 'grey.50',
        borderLeft: '3px solid',
        borderLeftColor: 'primary.light',
        borderRadius: 1,
        mx: 2,
        my: 1,
      }}
    >
      <Stack spacing={1.5}>
        {/* Location */}
        {exercise.location && (
          <Box display="flex" alignItems="center" gap={1}>
            <FontAwesomeIcon
              icon={faMapMarkerAlt}
              style={{ color: theme.palette.neutral[600], fontSize: '0.875rem' }}
            />
            <Typography variant="body2" color="text.secondary">
              {exercise.location}
            </Typography>
          </Box>
        )}

        {/* Organization Name (multi-org only) */}
        {showOrganization && exercise.organizationName && (
          <Box display="flex" alignItems="center" gap={1}>
            <FontAwesomeIcon
              icon={faBuilding}
              style={{ color: theme.palette.neutral[600], fontSize: '0.875rem' }}
            />
            <Typography variant="body2" color="text.secondary">
              {exercise.organizationName}
            </Typography>
          </Box>
        )}

        {/* Clock State & Elapsed Time (active exercises only) */}
        {showClockInfo && (
          <Box display="flex" alignItems="center" gap={1}>
            <FontAwesomeIcon
              icon={faClock}
              style={{ color: theme.palette.neutral[600], fontSize: '0.875rem' }}
            />
            <Typography variant="body2" color="text.secondary">
              Elapsed: {formatElapsedTime(exercise.elapsedSeconds || 0)}
            </Typography>
            <FontAwesomeIcon
              icon={clockDisplay.icon}
              style={{ color: clockDisplay.color, fontSize: '0.875rem', marginLeft: 8 }}
            />
            <Typography variant="body2" color="text.secondary">
              {exercise.clockState}
            </Typography>
          </Box>
        )}

        {/* Ready Injects (Controllers only) */}
        {showReadyInjects && (
          <Box display="flex" alignItems="center" gap={1}>
            <FontAwesomeIcon
              icon={faBolt}
              style={{ color: theme.palette.clockStatus.paused, fontSize: '0.875rem' }}
            />
            <Typography variant="body2" sx={{ color: 'warning.main', fontWeight: 500 }}>
              {exercise.readyInjectCount}{' '}
              {exercise.readyInjectCount === 1 ? 'inject' : 'injects'} ready
            </Typography>
          </Box>
        )}

        {/* Progress Bar */}
        {exercise.injectCount > 0 && (
          <Box>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={0.5}>
              <Typography variant="caption" color="text.secondary" fontWeight={500}>
                Progress
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {exercise.firedInjectCount} / {exercise.injectCount} injects
              </Typography>
            </Box>
            <LinearProgress
              variant="determinate"
              value={progressPercentage}
              sx={{
                height: 6,
                borderRadius: 3,
                bgcolor: 'grey.300',
                '& .MuiLinearProgress-bar': {
                  borderRadius: 3,
                  bgcolor: 'primary.main',
                },
              }}
            />
          </Box>
        )}
      </Stack>
    </Box>
  )
}

export default ExerciseDetailRow
