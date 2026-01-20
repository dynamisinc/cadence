import { useState } from 'react'
import {
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
  Divider,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Tooltip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlay,
  faPause,
  faStop,
  faRotateLeft,
  faBoxOpen,
  faChevronDown,
} from '@fortawesome/free-solid-svg-icons'

import {
  CobraSecondaryButton,
  CobraPrimaryButton,
  CobraDeleteButton,
} from '../../../theme/styledComponents'
import { useExerciseStatus } from '../hooks'
import { ExerciseStatus } from '../../../types'
import type { ExerciseDto } from '../types'

interface ExerciseStatusActionsProps {
  exercise: ExerciseDto
  /** Whether the exercise meets activation criteria (has at least one inject) */
  isReadyToActivate?: boolean
}

/**
 * Exercise Status Actions component
 *
 * Provides a dropdown menu with available status transitions for an exercise.
 * Shows confirmation dialogs for destructive actions like Complete and Revert to Draft.
 */
export const ExerciseStatusActions = ({
  exercise,
  isReadyToActivate = false,
}: ExerciseStatusActionsProps) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean
    action: string
    title: string
    message: string
    onConfirm: () => Promise<unknown>
    isDestructive?: boolean
  } | null>(null)

  const {
    availableTransitions,
    canTransition,
    activate,
    pause,
    resume,
    complete,
    unarchive,
    revertToDraft,
    isTransitioning,
  } = useExerciseStatus(exercise.id)

  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget)
  }

  const handleClose = () => {
    setAnchorEl(null)
  }

  const handleAction = async (action: () => Promise<unknown>) => {
    handleClose()
    await action()
  }

  const handleConfirmAction = (
    action: string,
    title: string,
    message: string,
    onConfirm: () => Promise<unknown>,
    isDestructive = false,
  ) => {
    handleClose()
    setConfirmDialog({ open: true, action, title, message, onConfirm, isDestructive })
  }

  const handleConfirmClose = () => {
    setConfirmDialog(null)
  }

  const handleConfirmSubmit = async () => {
    if (confirmDialog) {
      await confirmDialog.onConfirm()
      setConfirmDialog(null)
    }
  }

  // Don't show if no transitions available
  if (availableTransitions.length === 0) {
    return null
  }

  return (
    <>
      <CobraSecondaryButton
        onClick={handleClick}
        endIcon={<FontAwesomeIcon icon={faChevronDown} />}
        disabled={isTransitioning}
      >
        {isTransitioning ? 'Updating...' : 'Status Actions'}
      </CobraSecondaryButton>

      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
      >
        {/* Activate (Draft → Active) */}
        {canTransition(ExerciseStatus.Active) && exercise.status === ExerciseStatus.Draft && (
          <Tooltip
            title={isReadyToActivate ? '' : 'Add at least one inject to the MSEL before activating'}
            placement="left"
          >
            <span>
              <MenuItem
                onClick={() => handleAction(activate)}
                disabled={!isReadyToActivate}
              >
                <ListItemIcon>
                  <FontAwesomeIcon
                    icon={faPlay}
                    style={{
                      color: isReadyToActivate
                        ? 'var(--mui-palette-success-main)'
                        : 'var(--mui-palette-action-disabled)',
                    }}
                  />
                </ListItemIcon>
                <ListItemText>Activate Exercise</ListItemText>
              </MenuItem>
            </span>
          </Tooltip>
        )}

        {/* Resume (Paused → Active) */}
        {canTransition(ExerciseStatus.Active) && exercise.status === ExerciseStatus.Paused && (
          <MenuItem onClick={() => handleAction(resume)}>
            <ListItemIcon>
              <FontAwesomeIcon icon={faPlay} style={{ color: 'var(--mui-palette-success-main)' }} />
            </ListItemIcon>
            <ListItemText>Resume Exercise</ListItemText>
          </MenuItem>
        )}

        {/* Pause (Active → Paused) */}
        {canTransition(ExerciseStatus.Paused) && (
          <MenuItem onClick={() => handleAction(pause)}>
            <ListItemIcon>
              <FontAwesomeIcon icon={faPause} style={{ color: 'var(--mui-palette-warning-main)' }} />
            </ListItemIcon>
            <ListItemText>Pause Exercise</ListItemText>
          </MenuItem>
        )}

        {/* Complete (Active/Paused → Completed) */}
        {canTransition(ExerciseStatus.Completed) &&
          (exercise.status === ExerciseStatus.Active ||
            exercise.status === ExerciseStatus.Paused) && (
          <MenuItem
            onClick={() =>
              handleConfirmAction(
                'complete',
                'Complete Exercise',
                'Are you sure you want to complete this exercise? This will stop the clock and mark the exercise as finished. This action cannot be undone.',
                complete,
                true,
              )
            }
          >
            <ListItemIcon>
              <FontAwesomeIcon icon={faStop} style={{ color: 'var(--mui-palette-info-main)' }} />
            </ListItemIcon>
            <ListItemText>Complete Exercise</ListItemText>
          </MenuItem>
        )}

        {/* Revert to Draft (Paused → Draft) */}
        {canTransition(ExerciseStatus.Draft) && (
          <>
            <Divider />
            <MenuItem
              onClick={() =>
                handleConfirmAction(
                  'revert',
                  'Revert to Draft',
                  'WARNING: This will clear all conduct data including fired times and observations. The exercise will return to Draft status. This action cannot be undone.',
                  revertToDraft,
                  true,
                )
              }
            >
              <ListItemIcon>
                <FontAwesomeIcon icon={faRotateLeft} style={{ color: 'var(--mui-palette-error-main)' }} />
              </ListItemIcon>
              <ListItemText
                primary="Revert to Draft"
                secondary="Clears all conduct data"
              />
            </MenuItem>
          </>
        )}

        {/* Unarchive (Archived → Completed/Previous) */}
        {exercise.status === ExerciseStatus.Archived && canTransition(ExerciseStatus.Completed) && (
          <MenuItem onClick={() => handleAction(unarchive)}>
            <ListItemIcon>
              <FontAwesomeIcon icon={faBoxOpen} />
            </ListItemIcon>
            <ListItemText>Unarchive Exercise</ListItemText>
          </MenuItem>
        )}
      </Menu>

      {/* Confirmation Dialog */}
      {confirmDialog && (
        <Dialog open={confirmDialog.open} onClose={handleConfirmClose}>
          <DialogTitle>{confirmDialog.title}</DialogTitle>
          <DialogContent>
            <DialogContentText>{confirmDialog.message}</DialogContentText>
          </DialogContent>
          <DialogActions>
            <CobraSecondaryButton onClick={handleConfirmClose}>
              Cancel
            </CobraSecondaryButton>
            {confirmDialog.isDestructive ? (
              <CobraDeleteButton onClick={handleConfirmSubmit}>
                {confirmDialog.action === 'revert' ? 'Revert to Draft' : 'Complete'}
              </CobraDeleteButton>
            ) : (
              <CobraPrimaryButton onClick={handleConfirmSubmit}>
                Confirm
              </CobraPrimaryButton>
            )}
          </DialogActions>
        </Dialog>
      )}
    </>
  )
}

export default ExerciseStatusActions
