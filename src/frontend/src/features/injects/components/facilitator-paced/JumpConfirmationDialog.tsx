/**
 * JumpConfirmationDialog
 *
 * Confirmation dialog shown before jumping to a later inject in facilitator-paced mode.
 * Lists all injects that will be skipped and warns about the action.
 *
 * @module features/injects
 * @see exercise-config/S07-facilitator-paced-conduct-view
 */

import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  List,
  ListItem,
  ListItemText,
  Alert,
  Box,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faForwardFast } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraLinkButton } from '@/theme/styledComponents'
import type { InjectDto } from '../../types'

interface JumpConfirmationDialogProps {
  /** Whether dialog is open */
  open: boolean
  /** The inject to jump to */
  targetInject: InjectDto | null
  /** Injects that will be skipped */
  skippedInjects: InjectDto[]
  /** Called when user confirms the jump */
  onConfirm: () => void
  /** Called when user cancels */
  onCancel: () => void
}

export const JumpConfirmationDialog = ({
  open,
  targetInject,
  skippedInjects,
  onConfirm,
  onCancel,
}: JumpConfirmationDialogProps) => {
  const skippedCount = skippedInjects.length

  return (
    <Dialog
      open={open}
      onClose={onCancel}
      maxWidth="sm"
      fullWidth
      aria-labelledby="jump-dialog-title"
    >
      <DialogTitle id="jump-dialog-title">
        Jump to Inject #{targetInject?.injectNumber || '?'}?
      </DialogTitle>

      <DialogContent>
        {skippedCount > 0 ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Typography variant="body1">
              This will skip the following {skippedCount} inject{skippedCount !== 1 ? 's' : ''}:
            </Typography>

            <List dense disablePadding>
              {skippedInjects.map(inject => (
                <ListItem key={inject.id} disableGutters>
                  <ListItemText
                    primary={`#${inject.injectNumber} - ${inject.title}`}
                    primaryTypographyProps={{
                      variant: 'body2',
                    }}
                  />
                </ListItem>
              ))}
            </List>

            <Alert severity="warning">
              Skipped injects will be marked as "Deferred" and can be reviewed later if needed.
            </Alert>
          </Box>
        ) : (
          <Typography variant="body1">
            No injects will be skipped.
          </Typography>
        )}
      </DialogContent>

      <DialogActions>
        <CobraLinkButton onClick={onCancel}>
          Cancel
        </CobraLinkButton>
        <CobraPrimaryButton
          onClick={onConfirm}
          startIcon={<FontAwesomeIcon icon={faForwardFast} />}
        >
          Skip & Jump
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default JumpConfirmationDialog
