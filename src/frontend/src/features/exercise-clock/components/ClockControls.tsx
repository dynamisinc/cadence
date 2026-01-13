/**
 * ClockControls Component
 *
 * Control buttons for the exercise clock (Start, Pause, Stop).
 * Uses COBRA styled buttons and FontAwesome icons.
 */

import { Box, Tooltip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlay, faPause, faStop, faRotateLeft } from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraDeleteButton,
} from '../../../theme/styledComponents'
import { ExerciseClockState } from '../../../types'

interface ClockControlsProps {
  state: ExerciseClockState | undefined
  onStart: () => void
  onPause: () => void
  onStop: () => void
  onReset?: () => void
  isStarting?: boolean
  isPausing?: boolean
  isStopping?: boolean
  isResetting?: boolean
  disabled?: boolean
  showReset?: boolean
  size?: 'small' | 'medium'
}

export const ClockControls = ({
  state,
  onStart,
  onPause,
  onStop,
  onReset,
  isStarting = false,
  isPausing = false,
  isStopping = false,
  isResetting = false,
  disabled = false,
  showReset = false,
  size = 'medium',
}: ClockControlsProps) => {
  const isStopped = state === ExerciseClockState.Stopped || !state
  const isRunning = state === ExerciseClockState.Running
  const isPaused = state === ExerciseClockState.Paused

  const buttonSize = size === 'small' ? 'small' : 'medium'

  return (
    <Box
      sx={{
        display: 'flex',
        gap: 1,
        alignItems: 'center',
        flexWrap: 'wrap',
      }}
    >
      {/* Start / Resume Button */}
      {(isStopped || isPaused) && (
        <Tooltip title={isStopped ? 'Start Exercise' : 'Resume Exercise'}>
          <span>
            <CobraPrimaryButton
              onClick={onStart}
              disabled={disabled || isStarting}
              size={buttonSize}
              startIcon={<FontAwesomeIcon icon={faPlay} />}
            >
              {isStarting ? 'Starting...' : isStopped ? 'Start' : 'Resume'}
            </CobraPrimaryButton>
          </span>
        </Tooltip>
      )}

      {/* Pause Button */}
      {isRunning && (
        <Tooltip title="Pause Exercise">
          <span>
            <CobraSecondaryButton
              onClick={onPause}
              disabled={disabled || isPausing}
              size={buttonSize}
              startIcon={<FontAwesomeIcon icon={faPause} />}
            >
              {isPausing ? 'Pausing...' : 'Pause'}
            </CobraSecondaryButton>
          </span>
        </Tooltip>
      )}

      {/* Stop Button - only available when running or paused */}
      {(isRunning || isPaused) && (
        <Tooltip title="Stop Exercise (marks as Completed)">
          <span>
            <CobraDeleteButton
              onClick={onStop}
              disabled={disabled || isStopping}
              size={buttonSize}
              startIcon={<FontAwesomeIcon icon={faStop} />}
            >
              {isStopping ? 'Stopping...' : 'Stop'}
            </CobraDeleteButton>
          </span>
        </Tooltip>
      )}

      {/* Reset Button - optional, only shown when appropriate */}
      {showReset && onReset && isStopped && (
        <Tooltip title="Reset Clock">
          <span>
            <CobraSecondaryButton
              onClick={onReset}
              disabled={disabled || isResetting}
              size={buttonSize}
              startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
            >
              {isResetting ? 'Resetting...' : 'Reset'}
            </CobraSecondaryButton>
          </span>
        </Tooltip>
      )}
    </Box>
  )
}

export default ClockControls
