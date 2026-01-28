/**
 * ClockControlConfirmationDialog Component
 *
 * Confirmation dialog shown before clock control actions (start, pause, stop).
 * Only shown when confirmClockControl setting is enabled.
 *
 * @module features/exercise-clock/components
 */

import { useState, useEffect, useCallback } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Box,
  FormControlLabel,
  Checkbox,
  Stack,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlay,
  faPause,
  faStop,
  faStopwatch,
  type IconDefinition,
} from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'

/** Clock control action types */
export type ClockAction = 'start' | 'pause' | 'stop' | 'resume'

interface ClockControlConfirmationDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** The action being performed (null when dialog is closed) */
  action: ClockAction | null
  /** Current elapsed time display (e.g., "01:23:45") */
  currentTime?: string
  /** Called when user confirms */
  onConfirm: () => void
  /** Called when user cancels */
  onCancel: () => void
  /** Called when user checks "don't ask again" */
  onDontAskAgain: (value: boolean) => void
}

/** Configuration for each clock action */
const actionConfig: Record<
  ClockAction,
  {
    title: string
    icon: IconDefinition
    description: string
    warning?: string
    buttonText: string
    color: 'success' | 'warning' | 'error'
  }
> = {
  start: {
    title: 'Start Exercise Clock?',
    icon: faPlay,
    description:
      'Starting the clock will begin the exercise. Injects will become ready according to their scheduled delivery times.',
    buttonText: 'Start Clock',
    color: 'success',
  },
  resume: {
    title: 'Resume Exercise Clock?',
    icon: faPlay,
    description:
      'Resuming the clock will continue the exercise from where it was paused. Elapsed time will continue counting.',
    buttonText: 'Resume Clock',
    color: 'success',
  },
  pause: {
    title: 'Pause Exercise Clock?',
    icon: faPause,
    description:
      'Pausing the clock will temporarily halt the exercise. You can resume at any time to continue from the current elapsed time.',
    buttonText: 'Pause Clock',
    color: 'warning',
  },
  stop: {
    title: 'Stop Exercise Clock?',
    icon: faStop,
    description:
      'Stopping the clock will end the exercise. The exercise will be marked as Completed.',
    warning: 'This action will complete the exercise. You will not be able to restart the clock.',
    buttonText: 'Stop Clock',
    color: 'error',
  },
}

/**
 * Clock Control Confirmation Dialog
 *
 * Shows confirmation before starting, pausing, or stopping the exercise clock.
 * Includes option to skip future confirmations for this session.
 *
 * @example
 * <ClockControlConfirmationDialog
 *   open={!!confirmAction}
 *   action={confirmAction}
 *   currentTime={displayTime}
 *   onConfirm={handleConfirmAction}
 *   onCancel={handleCancelAction}
 *   onDontAskAgain={setSkipClockConfirmation}
 * />
 */
export const ClockControlConfirmationDialog = ({
  open,
  action,
  currentTime,
  onConfirm,
  onCancel,
  onDontAskAgain,
}: ClockControlConfirmationDialogProps) => {
  const theme = useTheme()
  const [dontAsk, setDontAsk] = useState(false)

  // All hooks must be called before any early returns to comply with Rules of Hooks
  const handleConfirm = useCallback(() => {
    if (dontAsk) {
      onDontAskAgain(true)
    }
    onConfirm()
  }, [dontAsk, onDontAskAgain, onConfirm])

  // Handle keyboard shortcuts
  useEffect(() => {
    if (!open || !action) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Enter') {
        e.preventDefault()
        handleConfirm()
      } else if (e.key === 'Escape') {
        e.preventDefault()
        onCancel()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [open, action, handleConfirm, onCancel])

  // Reset "don't ask" checkbox when dialog opens
  useEffect(() => {
    if (open) {
      setDontAsk(false)
    }
  }, [open])

  // Don't render if no action specified (after all hooks)
  if (!action) return null

  const config = actionConfig[action]

  // Get theme color for the action
  const getColor = () => {
    switch (config.color) {
      case 'success':
        return theme.palette.success.main
      case 'warning':
        return theme.palette.warning.main
      case 'error':
        return theme.palette.error.main
      default:
        return theme.palette.primary.main
    }
  }

  return (
    <Dialog
      open={open}
      onClose={onCancel}
      maxWidth="sm"
      fullWidth
      aria-labelledby="clock-confirmation-title"
      aria-describedby="clock-confirmation-description"
    >
      <DialogTitle id="clock-confirmation-title">
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon icon={config.icon} style={{ color: getColor() }} />
          <Typography variant="h6" component="span">
            {config.title}
          </Typography>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2}>
          {currentTime && (
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 1.5,
                p: 2,
                bgcolor: 'grey.100',
                borderRadius: 1,
              }}
            >
              <FontAwesomeIcon icon={faStopwatch} style={{ color: theme.palette.text.secondary }} />
              <Typography variant="body2" color="text.secondary">
                Current elapsed time:
              </Typography>
              <Typography variant="h6" fontFamily="monospace">
                {currentTime}
              </Typography>
            </Box>
          )}

          <Typography variant="body1" id="clock-confirmation-description">
            {config.description}
          </Typography>

          {config.warning && (
            <Alert severity="warning" sx={{ mt: 1 }}>
              {config.warning}
            </Alert>
          )}

          <FormControlLabel
            control={
              <Checkbox
                checked={dontAsk}
                onChange={e => setDontAsk(e.target.checked)}
                size="small"
              />
            }
            label="Don't ask again for this exercise"
          />
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton onClick={onCancel} size="small">
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleConfirm}
          size="small"
          startIcon={<FontAwesomeIcon icon={config.icon} />}
          sx={{
            bgcolor: getColor(),
            '&:hover': {
              bgcolor: getColor(),
              filter: 'brightness(0.9)',
            },
          }}
        >
          {config.buttonText}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default ClockControlConfirmationDialog
