/**
 * SetClockTimeDialog Component
 *
 * Dialog for manually setting the exercise clock elapsed time.
 * Only available to Exercise Directors and above when clock is paused.
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
  Stack,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faClock, faStopwatch } from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import { CobraPrimaryButton, CobraSecondaryButton, CobraTextField } from '@/theme/styledComponents'

interface SetClockTimeDialogProps {
  open: boolean
  currentTime: string
  maxDurationHours: number
  onConfirm: (elapsedTime: string) => void
  onCancel: () => void
  isLoading?: boolean
}

/**
 * Parse a display time string (HH:MM:SS) into hours, minutes, seconds
 */
const parseDisplayTime = (time: string): { hours: number; minutes: number; seconds: number } => {
  const parts = time.split(':')
  return {
    hours: parseInt(parts[0] || '0', 10) || 0,
    minutes: parseInt(parts[1] || '0', 10) || 0,
    seconds: parseInt(parts[2] || '0', 10) || 0,
  }
}

/**
 * SetClockTimeDialog
 *
 * Allows directors to manually set the exercise clock to a specific elapsed time.
 * Validates that the time is within bounds (0 to max duration).
 */
export const SetClockTimeDialog = ({
  open,
  currentTime,
  maxDurationHours,
  onConfirm,
  onCancel,
  isLoading = false,
}: SetClockTimeDialogProps) => {
  const theme = useTheme()
  const [hours, setHours] = useState(0)
  const [minutes, setMinutes] = useState(0)
  const [seconds, setSeconds] = useState(0)
  const [error, setError] = useState<string | null>(null)

  // Initialize with current time when dialog opens
  useEffect(() => {
    if (open) {
      const parsed = parseDisplayTime(currentTime)
      setHours(parsed.hours)
      setMinutes(parsed.minutes)
      setSeconds(parsed.seconds)
      setError(null)
    }
  }, [open, currentTime])

  const validate = useCallback(
    (h: number, m: number, s: number): string | null => {
      if (h < 0 || m < 0 || s < 0) {
        return 'Time values cannot be negative.'
      }
      if (m >= 60 || s >= 60) {
        return 'Minutes and seconds must be less than 60.'
      }
      const totalHours = h + m / 60 + s / 3600
      if (totalHours > maxDurationHours) {
        return `Time cannot exceed the maximum duration of ${maxDurationHours} hours.`
      }
      return null
    },
    [maxDurationHours],
  )

  const handleConfirm = useCallback(() => {
    const validationError = validate(hours, minutes, seconds)
    if (validationError) {
      setError(validationError)
      return
    }

    // Format as HH:MM:SS for the API (this is wall clock time, before multiplier)
    const formatted = [
      hours.toString().padStart(2, '0'),
      minutes.toString().padStart(2, '0'),
      seconds.toString().padStart(2, '0'),
    ].join(':')

    onConfirm(formatted)
  }, [hours, minutes, seconds, validate, onConfirm])

  // Handle Enter key
  useEffect(() => {
    if (!open) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Enter') {
        e.preventDefault()
        handleConfirm()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [open, handleConfirm])

  // Clear error when values change
  useEffect(() => {
    if (error) {
      const validationError = validate(hours, minutes, seconds)
      if (!validationError) {
        setError(null)
      }
    }
  }, [hours, minutes, seconds, error, validate])

  return (
    <Dialog
      open={open}
      onClose={onCancel}
      maxWidth="sm"
      fullWidth
      aria-labelledby="set-clock-time-title"
    >
      <DialogTitle id="set-clock-time-title">
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon icon={faClock} style={{ color: theme.palette.primary.main }} />
          <Typography variant="h6" component="span">
            Set Clock Time
          </Typography>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {/* Current time reference */}
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

          <Typography variant="body2" color="text.secondary">
            Enter the new elapsed time for the exercise clock. The clock must be paused to set
            the time. Maximum duration: {maxDurationHours} hours.
          </Typography>

          {/* Time input fields */}
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
            <CobraTextField
              label="Hours"
              type="number"
              value={hours}
              onChange={e => setHours(Math.max(0, parseInt(e.target.value, 10) || 0))}
              inputProps={{ min: 0, max: Math.ceil(maxDurationHours) }}
              sx={{ width: 100 }}
              size="small"
            />
            <Typography variant="h5" color="text.secondary">
              :
            </Typography>
            <CobraTextField
              label="Minutes"
              type="number"
              value={minutes}
              onChange={e => setMinutes(Math.max(0, Math.min(59, parseInt(e.target.value, 10) || 0)))}
              inputProps={{ min: 0, max: 59 }}
              sx={{ width: 100 }}
              size="small"
            />
            <Typography variant="h5" color="text.secondary">
              :
            </Typography>
            <CobraTextField
              label="Seconds"
              type="number"
              value={seconds}
              onChange={e => setSeconds(Math.max(0, Math.min(59, parseInt(e.target.value, 10) || 0)))}
              inputProps={{ min: 0, max: 59 }}
              sx={{ width: 100 }}
              size="small"
            />
          </Box>

          {error && (
            <Alert severity="error">{error}</Alert>
          )}

          <Alert severity="info" sx={{ mt: 1 }}>
            This sets the wall clock elapsed time. Scenario time will be calculated
            based on the clock multiplier.
          </Alert>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton onClick={onCancel} size="small" disabled={isLoading}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleConfirm}
          size="small"
          disabled={isLoading}
          startIcon={<FontAwesomeIcon icon={faClock} />}
        >
          {isLoading ? 'Setting...' : 'Set Time'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default SetClockTimeDialog
