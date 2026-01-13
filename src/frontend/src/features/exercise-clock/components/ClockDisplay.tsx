/**
 * ClockDisplay Component
 *
 * Displays the exercise clock with elapsed time and state indicator.
 * Uses COBRA styling and FontAwesome icons.
 */

import { Box, Typography, Chip, CircularProgress } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faClock, faPlay, faPause, faStop } from '@fortawesome/free-solid-svg-icons'
import { ExerciseClockState } from '../../../types'
import type { ClockStateDto } from '../types'

interface ClockDisplayProps {
  clockState: ClockStateDto | undefined
  displayTime: string
  loading?: boolean
  size?: 'small' | 'medium' | 'large'
}

/**
 * Get state-specific styling
 */
const getStateStyles = (state: ExerciseClockState | undefined) => {
  switch (state) {
    case ExerciseClockState.Running:
      return {
        color: 'success.main',
        bgColor: 'success.light',
        icon: faPlay,
        label: 'Running',
      }
    case ExerciseClockState.Paused:
      return {
        color: 'warning.main',
        bgColor: 'warning.light',
        icon: faPause,
        label: 'Paused',
      }
    case ExerciseClockState.Stopped:
    default:
      return {
        color: 'text.secondary',
        bgColor: 'action.hover',
        icon: faStop,
        label: 'Stopped',
      }
  }
}

/**
 * Get size-specific styling
 */
const getSizeStyles = (size: 'small' | 'medium' | 'large') => {
  switch (size) {
    case 'small':
      return {
        fontSize: '1.25rem',
        iconSize: 'sm' as const,
        padding: 1,
      }
    case 'large':
      return {
        fontSize: '3rem',
        iconSize: 'lg' as const,
        padding: 3,
      }
    case 'medium':
    default:
      return {
        fontSize: '2rem',
        iconSize: '1x' as const,
        padding: 2,
      }
  }
}

export const ClockDisplay = ({
  clockState,
  displayTime,
  loading = false,
  size = 'medium',
}: ClockDisplayProps) => {
  const stateStyles = getStateStyles(clockState?.state)
  const sizeStyles = getSizeStyles(size)

  if (loading) {
    return (
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          p: sizeStyles.padding,
        }}
      >
        <CircularProgress size={24} />
      </Box>
    )
  }

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 1,
        p: sizeStyles.padding,
      }}
    >
      {/* Clock Icon and Time */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
        }}
      >
        <FontAwesomeIcon
          icon={faClock}
          style={{ color: 'inherit', opacity: 0.7 }}
        />
        <Typography
          variant="h4"
          component="span"
          sx={{
            fontFamily: 'monospace',
            fontWeight: 600,
            fontSize: sizeStyles.fontSize,
            color: stateStyles.color,
          }}
        >
          {displayTime}
        </Typography>
      </Box>

      {/* State Chip */}
      <Chip
        icon={
          <FontAwesomeIcon
            icon={stateStyles.icon}
            size={sizeStyles.iconSize}
          />
        }
        label={stateStyles.label}
        size={size === 'large' ? 'medium' : 'small'}
        sx={{
          bgcolor: stateStyles.bgColor,
          color: stateStyles.color,
          fontWeight: 500,
          '& .MuiChip-icon': {
            color: 'inherit',
          },
        }}
      />

      {/* Started by info (optional) */}
      {clockState?.startedByName && clockState.state !== ExerciseClockState.Stopped && (
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ mt: 0.5 }}
        >
          Started by {clockState.startedByName}
        </Typography>
      )}
    </Box>
  )
}

export default ClockDisplay
