/**
 * PublishBlockedDialog Component
 *
 * Dialog shown when exercise cannot be published due to unapproved injects (S07).
 *
 * @module features/exercises/components
 */

import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Box,
  Alert,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faExclamationTriangle,
  faPencil,
  faClock,
  faArrowRight,
} from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import type { PublishValidationResult } from '../types'

interface PublishBlockedDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Called when dialog is closed */
  onClose: () => void
  /** The exercise ID (for navigation) */
  exerciseId: string
  /** Validation result with details */
  validation: PublishValidationResult | undefined
  /** Optional callback to navigate to pending injects */
  onViewPending?: () => void
}

/**
 * Publish Blocked Dialog
 *
 * Explains why the exercise cannot be published and provides
 * a link to view unapproved injects.
 *
 * @example
 * <PublishBlockedDialog
 *   open={blockedDialogOpen}
 *   onClose={() => setBlockedDialogOpen(false)}
 *   exerciseId={exerciseId}
 *   validation={validation}
 *   onViewPending={() => navigate('/msel?filter=pending')}
 * />
 */
export const PublishBlockedDialog = ({
  open,
  onClose,
  validation,
  onViewPending,
}: PublishBlockedDialogProps) => {
  const theme = useTheme()

  const draftCount = validation?.draftCount ?? 0
  const submittedCount = validation?.submittedCount ?? 0
  const totalUnapproved = validation?.totalUnapprovedCount ?? 0
  const warnings = validation?.warnings ?? []
  const errors = validation?.errors ?? []

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      aria-labelledby="publish-blocked-dialog-title"
    >
      <DialogTitle id="publish-blocked-dialog-title">
        <Box display="flex" alignItems="center" gap={1}>
          <FontAwesomeIcon
            icon={faExclamationTriangle}
            style={{ color: theme.palette.warning.main }}
          />
          Cannot Activate Exercise
        </Box>
      </DialogTitle>

      <DialogContent>
        <Alert severity="warning" sx={{ mb: 3 }}>
          All injects must be approved before activating the exercise.
        </Alert>

        <Typography variant="body1" paragraph>
          The following injects need attention before you can activate:
        </Typography>

        <List dense>
          {draftCount > 0 && (
            <ListItem>
              <ListItemIcon>
                <FontAwesomeIcon
                  icon={faPencil}
                  style={{ color: theme.palette.grey[600] }}
                />
              </ListItemIcon>
              <ListItemText
                primary={
                  <Typography variant="body2">
                    <strong>{draftCount}</strong> inject{draftCount !== 1 ? 's' : ''} in{' '}
                    <strong>Draft</strong> status
                  </Typography>
                }
                secondary="Need to be submitted for approval"
              />
            </ListItem>
          )}

          {submittedCount > 0 && (
            <ListItem>
              <ListItemIcon>
                <FontAwesomeIcon
                  icon={faClock}
                  style={{ color: theme.palette.warning.main }}
                />
              </ListItemIcon>
              <ListItemText
                primary={
                  <Typography variant="body2">
                    <strong>{submittedCount}</strong> inject{submittedCount !== 1 ? 's' : ''}{' '}
                    awaiting <strong>approval</strong>
                  </Typography>
                }
                secondary="Need Exercise Director review"
              />
            </ListItem>
          )}
        </List>

        {warnings.length > 0 && (
          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle2" color="warning.main" gutterBottom>
              Warnings:
            </Typography>
            <ul style={{ margin: 0, paddingLeft: 20 }}>
              {warnings.map((warning, index) => (
                <li key={index}>
                  <Typography variant="body2" color="text.secondary">
                    {warning}
                  </Typography>
                </li>
              ))}
            </ul>
          </Box>
        )}

        {errors.length > 0 && (
          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle2" color="error.main" gutterBottom>
              Errors:
            </Typography>
            <ul style={{ margin: 0, paddingLeft: 20 }}>
              {errors.map((error, index) => (
                <li key={index}>
                  <Typography variant="body2" color="error.main">
                    {error}
                  </Typography>
                </li>
              ))}
            </ul>
          </Box>
        )}

        <Typography variant="body2" color="text.secondary" sx={{ mt: 3 }}>
          <strong>Total unapproved:</strong> {totalUnapproved} inject
          {totalUnapproved !== 1 ? 's' : ''}
        </Typography>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton onClick={onClose}>Close</CobraSecondaryButton>
        {onViewPending && totalUnapproved > 0 && (
          <CobraPrimaryButton
            onClick={() => {
              onViewPending()
              onClose()
            }}
            endIcon={<FontAwesomeIcon icon={faArrowRight} />}
          >
            View Unapproved Injects
          </CobraPrimaryButton>
        )}
      </DialogActions>
    </Dialog>
  )
}

export default PublishBlockedDialog
