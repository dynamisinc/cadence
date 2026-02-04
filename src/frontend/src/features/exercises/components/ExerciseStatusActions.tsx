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
  faExclamationTriangle,
} from '@fortawesome/free-solid-svg-icons'

import {
  CobraSecondaryButton,
  CobraPrimaryButton,
  CobraDeleteButton,
} from '../../../theme/styledComponents'
import { useExerciseStatus, usePublishValidation } from '../hooks'
import { ExerciseStatus } from '../../../types'
import type { ExerciseDto } from '../types'
import { PublishBlockedDialog } from './PublishBlockedDialog'

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
  const [blockedDialogOpen, setBlockedDialogOpen] = useState(false)

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

  // Approval validation for activation (S07)
  const {
    validation,
    canPublish,
    unapprovedCount,
    refetch: refetchValidation,
  } = usePublishValidation(exercise.id)

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

  // Handle activate with approval validation (S07)
  const handleActivate = async () => {
    handleClose()
    // Refresh validation before checking
    await refetchValidation()

    if (canPublish) {
      await activate()
    } else {
      // Show blocked dialog if there are unapproved injects
      setBlockedDialogOpen(true)
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
            title={
              !isReadyToActivate
                ? 'Add at least one inject to the MSEL before activating'
                : unapprovedCount > 0
                  ? `${unapprovedCount} inject${unapprovedCount !== 1 ? 's' : ''} need approval`
                  : ''
            }
            placement="left"
          >
            <span>
              <MenuItem
                onClick={handleActivate}
                disabled={!isReadyToActivate}
              >
                <ListItemIcon>
                  <FontAwesomeIcon
                    icon={unapprovedCount > 0 ? faExclamationTriangle : faPlay}
                    style={{
                      color: !isReadyToActivate
                        ? 'var(--mui-palette-action-disabled)'
                        : unapprovedCount > 0
                          ? 'var(--mui-palette-warning-main)'
                          : 'var(--mui-palette-success-main)',
                    }}
                  />
                </ListItemIcon>
                <ListItemText
                  primary="Activate Exercise"
                  secondary={unapprovedCount > 0 ? `${unapprovedCount} pending approval` : undefined}
                />
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
                `Are you sure you want to complete "${exercise.name}"? This will end the exercise conduct phase.`,
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
                  'WARNING: This will reset all inject statuses and delete observations. This action cannot be undone.',
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
                secondary="Resets inject statuses & deletes observations"
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

      {/* Approval Blocked Dialog (S07) */}
      <PublishBlockedDialog
        open={blockedDialogOpen}
        onClose={() => setBlockedDialogOpen(false)}
        exerciseId={exercise.id}
        validation={validation}
      />
    </>
  )
}

export default ExerciseStatusActions
