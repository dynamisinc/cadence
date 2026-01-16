import {
  Box,
  Card,
  CardContent,
  Typography,
  LinearProgress,
  Stack,
  Skeleton,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faCheck,
  faCircle,
  faExclamationTriangle,
} from '@fortawesome/free-solid-svg-icons'

import type { SetupProgressDto, SetupAreaDto } from '../types'

interface SetupProgressProps {
  /** Setup progress data */
  progress: SetupProgressDto | undefined
  /** Whether data is loading */
  isLoading?: boolean
  /** Error message if failed to load */
  error?: Error | null
}

interface SetupAreaItemProps {
  area: SetupAreaDto
}

const SetupAreaItem = ({ area }: SetupAreaItemProps) => {
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: 1.5,
        py: 1,
        borderBottom: 1,
        borderColor: 'divider',
        '&:last-child': {
          borderBottom: 0,
        },
      }}
    >
      {/* Status Icon */}
      <Box
        sx={{
          width: 24,
          height: 24,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          flexShrink: 0,
          mt: 0.25,
        }}
      >
        {area.isComplete ? (
          <FontAwesomeIcon
            icon={faCheck}
            style={{ color: 'var(--mui-palette-success-main)' }}
          />
        ) : (
          <FontAwesomeIcon
            icon={faCircle}
            style={{
              color: 'var(--mui-palette-text-disabled)',
              fontSize: '0.5rem',
            }}
          />
        )}
      </Box>

      {/* Content */}
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 1,
          }}
        >
          <Typography
            variant="body2"
            fontWeight={500}
            color={area.isComplete ? 'text.primary' : 'text.secondary'}
          >
            {area.name}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {area.weight}%
          </Typography>
        </Box>
        <Typography
          variant="caption"
          color={area.isComplete ? 'success.main' : 'text.secondary'}
        >
          {area.statusMessage}
        </Typography>
      </Box>
    </Box>
  )
}

/**
 * SetupProgress component displays exercise setup completion status
 *
 * Shows overall progress bar and individual area status with checkmarks.
 * Used on exercise detail page for Draft exercises.
 */
export const SetupProgress = ({
  progress,
  isLoading = false,
  error,
}: SetupProgressProps) => {
  if (isLoading) {
    return (
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Skeleton variant="text" width="60%" height={28} />
            <Skeleton variant="rectangular" height={8} />
            <Stack spacing={1}>
              {[1, 2, 3, 4].map(i => (
                <Skeleton key={i} variant="rectangular" height={40} />
              ))}
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        Failed to load setup progress: {error.message}
      </Alert>
    )
  }

  if (!progress) {
    return null
  }

  const progressColor =
    progress.overallPercentage >= 100
      ? 'success'
      : progress.overallPercentage >= 40
        ? 'primary'
        : 'warning'

  return (
    <Card>
      <CardContent>
        <Stack spacing={2}>
          {/* Header */}
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}
          >
            <Typography variant="subtitle1" fontWeight={600}>
              Setup Progress
            </Typography>
            <Typography
              variant="h6"
              fontWeight={700}
              color={`${progressColor}.main`}
            >
              {progress.overallPercentage}%
            </Typography>
          </Box>

          {/* Progress Bar */}
          <LinearProgress
            variant="determinate"
            value={progress.overallPercentage}
            color={progressColor}
            sx={{
              height: 8,
              borderRadius: 1,
              bgcolor: 'action.hover',
            }}
          />

          {/* Ready to Activate Warning */}
          {!progress.isReadyToActivate && (
            <Alert
              severity="warning"
              icon={<FontAwesomeIcon icon={faExclamationTriangle} />}
              sx={{ py: 0.5 }}
            >
              <Typography variant="body2">
                Add at least one inject to the MSEL before activating
              </Typography>
            </Alert>
          )}

          {/* Areas */}
          <Box>
            {progress.areas.map(area => (
              <SetupAreaItem key={area.id} area={area} />
            ))}
          </Box>
        </Stack>
      </CardContent>
    </Card>
  )
}

export default SetupProgress
