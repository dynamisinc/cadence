/**
 * SkipConfirmationDialog Component
 *
 * Initial confirmation dialog shown before skipping an inject.
 * This is the "are you sure?" step before the skip reason dialog.
 * Only shown when confirmSkipInject setting is enabled.
 *
 * Flow: Click Skip -> SkipConfirmationDialog -> SkipReasonDialog -> Skip API
 *
 * @module features/injects/components
 */

import { useState, useEffect, useCallback } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Box,
  Paper,
  FormControlLabel,
  Checkbox,
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faForwardStep, faCrosshairs, faBook } from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import type { InjectDto } from '../types'
import { formatScenarioTime } from '../types'

interface SkipConfirmationDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** The inject to skip (null if none selected) */
  inject: InjectDto | null
  /** Called when user confirms (proceed to reason dialog) */
  onConfirm: () => void
  /** Called when user cancels */
  onCancel: () => void
  /** Called when user checks "don't ask again" */
  onDontAskAgain: (value: boolean) => void
}

/**
 * Skip Confirmation Dialog
 *
 * Shows inject details and asks for confirmation before skipping.
 * After confirmation, the user proceeds to enter a skip reason.
 * Includes option to skip future confirmations for this session.
 *
 * @example
 * <SkipConfirmationDialog
 *   open={!!confirmSkipInject}
 *   inject={confirmSkipInject}
 *   onConfirm={handleProceedToReason}
 *   onCancel={handleCancelSkip}
 *   onDontAskAgain={setSkipConfirmation}
 * />
 */
export const SkipConfirmationDialog = ({
  open,
  inject,
  onConfirm,
  onCancel,
  onDontAskAgain,
}: SkipConfirmationDialogProps) => {
  const theme = useTheme()
  const [dontAsk, setDontAsk] = useState(false)

  const handleConfirm = useCallback(() => {
    if (dontAsk) {
      onDontAskAgain(true)
    }
    onConfirm()
  }, [dontAsk, onDontAskAgain, onConfirm])

  // Handle keyboard shortcuts
  useEffect(() => {
    if (!open) return

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
  }, [open, handleConfirm, onCancel])

  // Reset "don't ask" checkbox when dialog opens
  useEffect(() => {
    if (open) {
      setDontAsk(false)
    }
  }, [open])

  if (!inject) return null

  return (
    <Dialog
      open={open}
      onClose={onCancel}
      maxWidth="sm"
      fullWidth
      aria-labelledby="skip-confirmation-title"
      aria-describedby="skip-confirmation-description"
    >
      <DialogTitle id="skip-confirmation-title">
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon
            icon={faForwardStep}
            style={{ color: theme.palette.warning.main }}
          />
          <Typography variant="h6" component="span">
            Skip Inject?
          </Typography>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2}>
          <Typography variant="body1" id="skip-confirmation-description">
            You are about to skip:
          </Typography>

          <Paper
            variant="outlined"
            sx={{
              p: 2,
              borderColor: 'warning.main',
              backgroundColor: 'warning.50',
            }}
          >
            <Typography variant="h6" gutterBottom>
              #{inject.injectNumber} - {inject.title}
            </Typography>
            <Stack spacing={0.5}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <FontAwesomeIcon
                  icon={faCrosshairs}
                  size="sm"
                  style={{ color: theme.palette.text.secondary }}
                />
                <Typography variant="body2" color="text.secondary">
                  Target:
                </Typography>
                <Typography variant="body2">{inject.target}</Typography>
              </Box>
              {inject.scenarioDay !== null && inject.scenarioTime !== null && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <FontAwesomeIcon
                    icon={faBook}
                    size="sm"
                    style={{ color: theme.palette.text.secondary }}
                  />
                  <Typography variant="body2" color="text.secondary">
                    Story Time:
                  </Typography>
                  <Typography variant="body2">
                    {formatScenarioTime(inject.scenarioDay, inject.scenarioTime)}
                  </Typography>
                </Box>
              )}
            </Stack>
          </Paper>

          <Typography variant="body2" color="text.secondary">
            Skipped injects are marked as not delivered and recorded for the after-action report.
            You will need to provide a reason in the next step.
          </Typography>

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
          startIcon={<FontAwesomeIcon icon={faForwardStep} />}
        >
          Continue to Skip
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default SkipConfirmationDialog
