/**
 * FireConfirmationDialog Component
 *
 * Confirmation dialog shown before firing an inject.
 * Displays inject details and provides option to skip future confirmations.
 * Supports keyboard shortcuts (Enter to confirm, Escape to cancel).
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
import { faFire, faCrosshairs, faBook } from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import type { InjectDto } from '../types'
import { formatScenarioTime } from '../types'

interface FireConfirmationDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** The inject to fire (null if none selected) */
  inject: InjectDto | null
  /** Called when user confirms firing */
  onConfirm: () => void
  /** Called when user cancels */
  onCancel: () => void
  /** Called when user checks "don't ask again" */
  onDontAskAgain: (value: boolean) => void
}

/**
 * Fire Confirmation Dialog
 *
 * Shows inject details and asks for confirmation before firing.
 * Includes option to skip future confirmations for this session.
 *
 * @example
 * <FireConfirmationDialog
 *   open={!!confirmInject}
 *   inject={confirmInject}
 *   onConfirm={handleConfirmFire}
 *   onCancel={handleCancelFire}
 *   onDontAskAgain={setSkipConfirmation}
 * />
 */
export const FireConfirmationDialog = ({
  open,
  inject,
  onConfirm,
  onCancel,
  onDontAskAgain,
}: FireConfirmationDialogProps) => {
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
      aria-labelledby="fire-confirmation-title"
      aria-describedby="fire-confirmation-description"
    >
      <DialogTitle id="fire-confirmation-title">
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon
            icon={faFire}
            style={{ color: theme.palette.warning.main }}
          />
          <Typography variant="h6" component="span">
            Fire Inject?
          </Typography>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2}>
          <Typography variant="body1" id="fire-confirmation-description">
            You are about to fire:
          </Typography>

          <Paper
            variant="outlined"
            sx={{
              p: 2,
              borderColor: 'primary.main',
              backgroundColor: 'grey.50',
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
            This action will be broadcast to all exercise participants.
          </Typography>

          <FormControlLabel
            control={
              <Checkbox
                checked={dontAsk}
                onChange={e => setDontAsk(e.target.checked)}
                size="small"
              />
            }
            label="Don't ask again this session"
          />
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton
          onClick={onCancel}
          size="small"
        >
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleConfirm}
          size="small"
          startIcon={<FontAwesomeIcon icon={faFire} />}
        >
          Confirm Fire
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}
