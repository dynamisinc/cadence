/**
 * RestoreExerciseDialog Component
 *
 * Dialog for confirming exercise restore action from archived state.
 * Shows the target status the exercise will be restored to.
 */

import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Box,
  Chip,
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faRotateLeft, faArrowRight } from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '../../../theme/styledComponents'
import type { ExerciseDto } from '../types'
import { ExerciseStatus } from '../../../types'

interface RestoreExerciseDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** The exercise to restore */
  exercise: ExerciseDto | null
  /** Called when dialog is closed */
  onClose: () => void
  /** Called when user confirms restore */
  onConfirm: () => Promise<void>
  /** Whether the restore action is in progress */
  isRestoring?: boolean
}

/**
 * Get display label for exercise status
 */
const getStatusLabel = (status: ExerciseStatus | null): string => {
  switch (status) {
    case ExerciseStatus.Draft:
      return 'Draft'
    case ExerciseStatus.Active:
      return 'Active'
    case ExerciseStatus.Paused:
      return 'Paused'
    case ExerciseStatus.Completed:
      return 'Completed'
    case ExerciseStatus.Archived:
      return 'Archived'
    case null:
      return 'Draft'
    default:
      return String(status)
  }
}

/**
 * Get status chip color
 */
const getStatusColor = (
  status: ExerciseStatus | null,
): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
  switch (status) {
    case ExerciseStatus.Draft:
    case null:
      return 'default'
    case ExerciseStatus.Active:
      return 'success'
    case ExerciseStatus.Paused:
      return 'warning'
    case ExerciseStatus.Completed:
      return 'info'
    default:
      return 'default'
  }
}

/**
 * Dialog for confirming exercise restore action
 */
export const RestoreExerciseDialog = ({
  open,
  exercise,
  onClose,
  onConfirm,
  isRestoring = false,
}: RestoreExerciseDialogProps) => {
  if (!exercise) {
    return null
  }

  const handleConfirm = async () => {
    await onConfirm()
    onClose()
  }

  // Target status after restore (from previousStatus or default to Draft)
  const targetStatus = exercise.previousStatus ?? ExerciseStatus.Draft

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      aria-labelledby="restore-dialog-title"
      aria-describedby="restore-dialog-description"
    >
      <DialogTitle
        id="restore-dialog-title"
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'info.main',
            fontSize: 24,
          }}
        >
          <FontAwesomeIcon icon={faRotateLeft} />
        </Box>
        Restore Exercise
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2}>
          <DialogContentText id="restore-dialog-description">
            This will restore the exercise from the archive.
          </DialogContentText>

          <Box
            sx={{
              p: 2,
              bgcolor: 'background.paper',
              border: 1,
              borderColor: 'divider',
              borderRadius: 1,
            }}
          >
            <Stack spacing={1.5}>
              <Box sx={{ fontWeight: 600 }}>{exercise.name}</Box>
              <Stack direction="row" spacing={1} alignItems="center">
                <Chip label="Archived" size="small" color="default" />
                <FontAwesomeIcon
                  icon={faArrowRight}
                  style={{ color: 'var(--mui-palette-text-secondary)' }}
                />
                <Chip
                  label={getStatusLabel(targetStatus)}
                  size="small"
                  color={getStatusColor(targetStatus)}
                />
              </Stack>
            </Stack>
          </Box>

          <DialogContentText sx={{ color: 'text.secondary' }}>
            The exercise will be restored to its previous status ({getStatusLabel(targetStatus)}).
            It will become visible in the normal exercise list again.
          </DialogContentText>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton onClick={onClose} disabled={isRestoring}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleConfirm}
          disabled={isRestoring}
          startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
        >
          {isRestoring ? 'Restoring...' : 'Restore Exercise'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default RestoreExerciseDialog
