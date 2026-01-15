/**
 * ConflictDialog Component
 *
 * Displays sync conflicts to the user for acknowledgment.
 * Shows what actions failed and why.
 */

import React from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Typography,
  Box,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faTriangleExclamation,
  faTimesCircle,
  faBolt,
  faForward,
  faRotateLeft,
  faPlus,
  faPen,
  faTrash,
} from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton } from '../../theme/styledComponents'
import type { ConflictInfo, PendingActionType } from '../offline'

const actionLabels: Record<PendingActionType, { label: string; icon: typeof faBolt }> = {
  FIRE_INJECT: { label: 'Fire Inject', icon: faBolt },
  SKIP_INJECT: { label: 'Skip Inject', icon: faForward },
  RESET_INJECT: { label: 'Reset Inject', icon: faRotateLeft },
  CREATE_OBSERVATION: { label: 'Create Observation', icon: faPlus },
  UPDATE_OBSERVATION: { label: 'Update Observation', icon: faPen },
  DELETE_OBSERVATION: { label: 'Delete Observation', icon: faTrash },
}

interface ConflictDialogProps {
  open: boolean
  conflicts: ConflictInfo[]
  onClose: () => void
}

export const ConflictDialog: React.FC<ConflictDialogProps> = ({
  open,
  conflicts,
  onClose,
}) => {
  if (conflicts.length === 0) return null

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      data-testid="conflict-dialog"
    >
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <FontAwesomeIcon icon={faTriangleExclamation} color="#f59e0b" />
        <Typography variant="h6" component="span">
          Sync Conflicts
        </Typography>
      </DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Some of your offline changes couldn't be applied because they conflict with
          changes made by other users:
        </Typography>
        <List dense>
          {conflicts.map(conflict => {
            const actionInfo = actionLabels[conflict.type]
            return (
              <ListItem
                key={conflict.actionId}
                sx={{
                  bgcolor: 'rgba(239, 68, 68, 0.05)',
                  borderRadius: 1,
                  mb: 1,
                }}
              >
                <ListItemIcon sx={{ minWidth: 36 }}>
                  <FontAwesomeIcon icon={faTimesCircle} color="#ef4444" />
                </ListItemIcon>
                <ListItemText
                  primary={
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <FontAwesomeIcon icon={actionInfo.icon} size="sm" />
                      <Typography variant="body2" fontWeight={500}>
                        {actionInfo.label}
                      </Typography>
                    </Box>
                  }
                  secondary={
                    <Typography variant="caption" color="text.secondary">
                      {conflict.message}
                      {conflict.conflictingUser && (
                        <> (by {conflict.conflictingUser})</>
                      )}
                    </Typography>
                  }
                />
              </ListItem>
            )
          })}
        </List>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
          These actions have been discarded. You may need to take follow-up action
          based on the current state of the exercise.
        </Typography>
      </DialogContent>
      <DialogActions>
        <CobraPrimaryButton onClick={onClose}>
          OK, I Understand
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default ConflictDialog
