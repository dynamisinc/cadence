/**
 * ArchiveExerciseDialog Component
 *
 * Dialog for confirming exercise archive action.
 * Archive moves an exercise to a hidden state but preserves all data.
 * Archived exercises can be restored by administrators.
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
import { faBoxArchive } from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '../../../theme/styledComponents'
import type { ExerciseDto } from '../types'
import { ExerciseStatus } from '../../../types'

interface ArchiveExerciseDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** The exercise to archive */
  exercise: ExerciseDto | null
  /** Called when dialog is closed */
  onClose: () => void
  /** Called when user confirms archive */
  onConfirm: () => Promise<void>
  /** Whether the archive action is in progress */
  isArchiving?: boolean
}

/**
 * Get display label for exercise status
 */
const getStatusLabel = (status: ExerciseStatus): string => {
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
    default:
      return status
  }
}

/**
 * Dialog for confirming exercise archive action
 */
export const ArchiveExerciseDialog = ({
  open,
  exercise,
  onClose,
  onConfirm,
  isArchiving = false,
}: ArchiveExerciseDialogProps) => {
  if (!exercise) {
    return null
  }

  const handleConfirm = async () => {
    await onConfirm()
    onClose()
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      aria-labelledby="archive-dialog-title"
      aria-describedby="archive-dialog-description"
    >
      <DialogTitle
        id="archive-dialog-title"
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
            color: 'warning.main',
            fontSize: 24,
          }}
        >
          <FontAwesomeIcon icon={faBoxArchive} />
        </Box>
        Archive Exercise
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2}>
          <DialogContentText id="archive-dialog-description">
            Are you sure you want to archive this exercise?
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
            <Stack direction="row" spacing={2} alignItems="center">
              <Box sx={{ flex: 1 }}>
                <Box component="span" sx={{ fontWeight: 600 }}>
                  {exercise.name}
                </Box>
              </Box>
              <Chip
                label={getStatusLabel(exercise.status)}
                size="small"
                color={
                  exercise.status === ExerciseStatus.Draft
                    ? 'default'
                    : exercise.status === ExerciseStatus.Active
                      ? 'success'
                      : exercise.status === ExerciseStatus.Paused
                        ? 'warning'
                        : exercise.status === ExerciseStatus.Completed
                          ? 'info'
                          : 'default'
                }
              />
            </Stack>
          </Box>

          <DialogContentText sx={{ color: 'text.secondary' }}>
            Archived exercises are hidden from normal views but can be restored by an administrator.
            All data will be preserved.
          </DialogContentText>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton onClick={onClose} disabled={isArchiving}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleConfirm}
          disabled={isArchiving}
          startIcon={<FontAwesomeIcon icon={faBoxArchive} />}
        >
          {isArchiving ? 'Archiving...' : 'Archive Exercise'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default ArchiveExerciseDialog
