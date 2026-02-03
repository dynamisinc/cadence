/**
 * DeleteExerciseDialog Component
 *
 * Two-step confirmation dialog for permanently deleting an exercise.
 * Requires the user to:
 * 1. Type the exact exercise name
 * 2. Check a confirmation checkbox
 *
 * Shows a summary of all data that will be deleted.
 */

import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Box,
  Stack,
  Checkbox,
  FormControlLabel,
  CircularProgress,
  Alert,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faTriangleExclamation,
  faBolt,
  faClipboardList,
  faUsers,
  faBullseye,
  faEye,
  faLayerGroup,
  faFileLines,
} from '@fortawesome/free-solid-svg-icons'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import {
  CobraDeleteButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import type { ExerciseDto, DeleteSummaryResponse } from '../types'
import { exerciseService } from '../services/exerciseService'
import { exercisesQueryKey } from '../hooks/useExercises'

interface DeleteExerciseDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** The exercise to delete */
  exercise: ExerciseDto | null
  /** Called when dialog is closed */
  onClose: () => void
  /** Called when deletion is complete */
  onDeleted: () => void
}

/**
 * Dialog for permanently deleting an exercise with two-step confirmation
 */
export const DeleteExerciseDialog = ({
  open,
  exercise,
  onClose,
  onDeleted,
}: DeleteExerciseDialogProps) => {
  const queryClient = useQueryClient()
  const [confirmName, setConfirmName] = useState('')
  const [confirmCheckbox, setConfirmCheckbox] = useState(false)
  const [summary, setSummary] = useState<DeleteSummaryResponse | null>(null)
  const [isLoadingSummary, setIsLoadingSummary] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Reset state when dialog opens or closes
  useEffect(() => {
    if (open && exercise) {
      setConfirmName('')
      setConfirmCheckbox(false)
      setError(null)
      fetchDeleteSummary()
    } else {
      setSummary(null)
      setIsLoadingSummary(false)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, exercise])

  const fetchDeleteSummary = async () => {
    if (!exercise) return

    setIsLoadingSummary(true)
    setError(null)

    try {
      const data = await exerciseService.getDeleteSummary(exercise.id)
      setSummary(data)
    } catch (err) {
      setError('Failed to load delete summary. Please try again.')
      console.error('Error fetching delete summary:', err)
    } finally {
      setIsLoadingSummary(false)
    }
  }

  const handleDelete = async () => {
    if (!exercise || !canDelete) return

    setIsDeleting(true)
    setError(null)

    try {
      await exerciseService.deleteExercise(exercise.id)

      // Remove the deleted exercise from the cache
      queryClient.setQueryData<ExerciseDto[]>(exercisesQueryKey, (old = []) =>
        old.filter(e => e.id !== exercise.id),
      )

      // Also remove from single exercise cache
      queryClient.removeQueries({ queryKey: ['exercise', exercise.id] })

      toast.success('Exercise permanently deleted')
      onDeleted()
      onClose()
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to delete exercise. Please try again.'
      setError(errorMessage)
      console.error('Error deleting exercise:', err)
    } finally {
      setIsDeleting(false)
    }
  }

  if (!exercise) {
    return null
  }

  // Name must match exactly (case-insensitive)
  const nameMatches = confirmName.toLowerCase().trim() === exercise.name.toLowerCase().trim()
  const canDelete = nameMatches && confirmCheckbox && !isLoadingSummary && summary?.canDelete

  // Get the reason why deletion is not allowed
  const getCannotDeleteMessage = () => {
    if (!summary) return null
    if (summary.canDelete) return null

    switch (summary.cannotDeleteReason) {
      case 'MustArchiveFirst':
        return 'This exercise must be archived before it can be deleted. Published/Active/Completed exercises cannot be deleted directly.'
      case 'NotAuthorized':
        return 'You do not have permission to delete this exercise.'
      case 'NotFound':
        return 'Exercise not found.'
      default:
        return 'This exercise cannot be deleted.'
    }
  }

  const cannotDeleteMessage = getCannotDeleteMessage()

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      aria-labelledby="delete-dialog-title"
      aria-describedby="delete-dialog-description"
    >
      <DialogTitle
        id="delete-dialog-title"
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
          color: 'error.main',
        }}
      >
        <FontAwesomeIcon icon={faTriangleExclamation} />
        Permanently Delete Exercise
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2}>
          {isLoadingSummary ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress />
            </Box>
          ) : error ? (
            <Alert severity="error">{error}</Alert>
          ) : cannotDeleteMessage ? (
            <Alert severity="warning">{cannotDeleteMessage}</Alert>
          ) : (
            <>
              <Alert severity="error" icon={<FontAwesomeIcon icon={faTriangleExclamation} />}>
                This action <strong>CANNOT</strong> be undone. All data will be permanently deleted.
              </Alert>

              {summary && (
                <Box
                  sx={{
                    bgcolor: 'error.main',
                    color: 'error.contrastText',
                    p: 2,
                    borderRadius: 1,
                    opacity: 0.9,
                  }}
                >
                  <Box sx={{ fontWeight: 600, mb: 1 }}>Data that will be deleted:</Box>
                  <List dense disablePadding>
                    {summary.summary.injectCount > 0 && (
                      <ListItem disableGutters>
                        <ListItemIcon sx={{ minWidth: 32, color: 'inherit' }}>
                          <FontAwesomeIcon icon={faBolt} />
                        </ListItemIcon>
                        <ListItemText primary={`${summary.summary.injectCount} injects`} />
                      </ListItem>
                    )}
                    {summary.summary.phaseCount > 0 && (
                      <ListItem disableGutters>
                        <ListItemIcon sx={{ minWidth: 32, color: 'inherit' }}>
                          <FontAwesomeIcon icon={faLayerGroup} />
                        </ListItemIcon>
                        <ListItemText primary={`${summary.summary.phaseCount} phases`} />
                      </ListItem>
                    )}
                    {summary.summary.observationCount > 0 && (
                      <ListItem disableGutters>
                        <ListItemIcon sx={{ minWidth: 32, color: 'inherit' }}>
                          <FontAwesomeIcon icon={faEye} />
                        </ListItemIcon>
                        <ListItemText
                          primary={`${summary.summary.observationCount} observations`}
                        />
                      </ListItem>
                    )}
                    {summary.summary.participantCount > 0 && (
                      <ListItem disableGutters>
                        <ListItemIcon sx={{ minWidth: 32, color: 'inherit' }}>
                          <FontAwesomeIcon icon={faUsers} />
                        </ListItemIcon>
                        <ListItemText
                          primary={`${summary.summary.participantCount} participants`}
                        />
                      </ListItem>
                    )}
                    {summary.summary.objectiveCount > 0 && (
                      <ListItem disableGutters>
                        <ListItemIcon sx={{ minWidth: 32, color: 'inherit' }}>
                          <FontAwesomeIcon icon={faBullseye} />
                        </ListItemIcon>
                        <ListItemText primary={`${summary.summary.objectiveCount} objectives`} />
                      </ListItem>
                    )}
                    {summary.summary.expectedOutcomeCount > 0 && (
                      <ListItem disableGutters>
                        <ListItemIcon sx={{ minWidth: 32, color: 'inherit' }}>
                          <FontAwesomeIcon icon={faClipboardList} />
                        </ListItemIcon>
                        <ListItemText
                          primary={`${summary.summary.expectedOutcomeCount} expected outcomes`}
                        />
                      </ListItem>
                    )}
                    {summary.summary.mselCount > 0 && (
                      <ListItem disableGutters>
                        <ListItemIcon sx={{ minWidth: 32, color: 'inherit' }}>
                          <FontAwesomeIcon icon={faFileLines} />
                        </ListItemIcon>
                        <ListItemText primary={`${summary.summary.mselCount} MSEL versions`} />
                      </ListItem>
                    )}
                  </List>
                </Box>
              )}

              <DialogContentText>
                To confirm, type the exercise name:
              </DialogContentText>
              <Box
                sx={{
                  px: 2,
                  py: 1,
                  bgcolor: 'grey.100',
                  borderRadius: 1,
                  fontFamily: 'monospace',
                  fontWeight: 600,
                }}
              >
                {exercise.name}
              </Box>

              <CobraTextField
                label="Type exercise name to confirm"
                value={confirmName}
                onChange={e => setConfirmName(e.target.value)}
                fullWidth
                autoFocus
                error={confirmName.length > 0 && !nameMatches}
                helperText={
                  confirmName.length > 0 && !nameMatches ? 'Name does not match' : undefined
                }
              />

              <FormControlLabel
                control={
                  <Checkbox
                    checked={confirmCheckbox}
                    onChange={e => setConfirmCheckbox(e.target.checked)}
                    color="error"
                  />
                }
                label="I understand this action is permanent and irreversible"
              />
            </>
          )}
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton onClick={onClose} disabled={isDeleting}>
          Cancel
        </CobraSecondaryButton>
        <CobraDeleteButton
          onClick={handleDelete}
          disabled={!canDelete || isDeleting}
          startIcon={
            isDeleting ? (
              <CircularProgress size={16} color="inherit" />
            ) : (
              <FontAwesomeIcon icon={faTriangleExclamation} />
            )
          }
        >
          {isDeleting ? 'Deleting...' : 'Delete Permanently'}
        </CobraDeleteButton>
      </DialogActions>
    </Dialog>
  )
}

export default DeleteExerciseDialog
