/**
 * RevertApprovalAction Component
 *
 * Menu item or button to revert an approved inject back to submitted (S09).
 *
 * @module features/injects/components
 */

import { useState, useEffect } from 'react'
import {
  MenuItem,
  ListItemIcon,
  ListItemText,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Box,
  Paper,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faUndo,
  faSpinner,
  faExclamationTriangle,
} from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import {
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import { styled } from '@mui/material/styles'
import { InjectStatus } from '@/types'
import { useInjectApproval } from '../hooks'
import { APPROVAL_FIELD_LIMITS } from '../types'
import type { InjectDto } from '../types'

// Warning-colored button for destructive-ish action
const CobraWarningButton = styled(CobraSecondaryButton)(({ theme }) => ({
  color: theme.palette.warning.main,
  borderColor: theme.palette.warning.main,
  '&:hover': {
    borderColor: theme.palette.warning.dark,
    backgroundColor: `${theme.palette.warning.main}10`,
  },
}))

interface RevertApprovalActionProps {
  /** The inject to potentially revert */
  inject: InjectDto
  /** The exercise ID */
  exerciseId: string
  /** Whether to render as a MenuItem (for dropdown menus) */
  asMenuItem?: boolean
  /** Optional callback after successful revert */
  onReverted?: (inject: InjectDto) => void
}

/**
 * Revert Approval Action
 *
 * Allows reverting an approved inject back to submitted status.
 * Only visible for Approved injects. Requires a reason for the revert.
 *
 * @example
 * // In a dropdown menu
 * <RevertApprovalAction
 *   inject={inject}
 *   exerciseId={exerciseId}
 *   asMenuItem
 * />
 *
 * // As a standalone button (uses the same dialog)
 * <RevertApprovalAction
 *   inject={inject}
 *   exerciseId={exerciseId}
 * />
 */
export const RevertApprovalAction = ({
  inject,
  exerciseId,
  asMenuItem = false,
  onReverted,
}: RevertApprovalActionProps) => {
  const theme = useTheme()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [reason, setReason] = useState('')
  const [touched, setTouched] = useState(false)

  const { revertApproval, isReverting } = useInjectApproval(exerciseId)

  const isApproved = inject.status === InjectStatus.Approved

  const isValid =
    reason.length >= APPROVAL_FIELD_LIMITS.revertReason.min &&
    reason.length <= APPROVAL_FIELD_LIMITS.revertReason.max

  const handleRevert = async () => {
    if (!isValid) return
    try {
      const revertedInject = await revertApproval(inject.id, { reason: reason.trim() })
      setDialogOpen(false)
      onReverted?.(revertedInject)
    } catch {
      // Error handling is done in the hook
    }
  }

  // Reset form when dialog opens
  useEffect(() => {
    if (dialogOpen) {
      setReason('')
      setTouched(false)
    }
  }, [dialogOpen])

  // Handle keyboard shortcuts
  useEffect(() => {
    if (!dialogOpen) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Enter' && e.ctrlKey && isValid) {
        e.preventDefault()
        handleRevert()
      } else if (e.key === 'Escape') {
        e.preventDefault()
        setDialogOpen(false)
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dialogOpen, isValid])

  // Only show for Approved injects
  if (!isApproved) {
    return null
  }

  const showError = touched && !isValid
  const errorMessage =
    reason.length < APPROVAL_FIELD_LIMITS.revertReason.min
      ? `Minimum ${APPROVAL_FIELD_LIMITS.revertReason.min} characters required`
      : reason.length > APPROVAL_FIELD_LIMITS.revertReason.max
        ? `Maximum ${APPROVAL_FIELD_LIMITS.revertReason.max} characters allowed`
        : ''

  const trigger = asMenuItem ? (
    <MenuItem onClick={() => setDialogOpen(true)}>
      <ListItemIcon>
        <FontAwesomeIcon icon={faUndo} />
      </ListItemIcon>
      <ListItemText>Revert Approval</ListItemText>
    </MenuItem>
  ) : (
    <CobraWarningButton
      size="small"
      onClick={() => setDialogOpen(true)}
      startIcon={<FontAwesomeIcon icon={faUndo} />}
    >
      Revert
    </CobraWarningButton>
  )

  return (
    <>
      {trigger}

      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        maxWidth="sm"
        fullWidth
        aria-labelledby="revert-dialog-title"
      >
        <DialogTitle id="revert-dialog-title">
          <Box display="flex" alignItems="center" gap={1}>
            <FontAwesomeIcon
              icon={faExclamationTriangle}
              style={{ color: theme.palette.warning.main }}
            />
            Revert Approval
          </Box>
        </DialogTitle>

        <DialogContent>
          <Alert severity="warning" sx={{ mb: 3 }}>
            This will return the inject to <strong>Submitted</strong> status for re-review.
          </Alert>

          <Paper
            variant="outlined"
            sx={{ p: 2, mb: 3, bgcolor: 'background.default' }}
          >
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              Inject #{inject.injectNumber}
            </Typography>
            <Typography variant="body1" fontWeight={500}>
              {inject.title}
            </Typography>
          </Paper>

          <CobraTextField
            fullWidth
            required
            multiline
            rows={4}
            label="Reason for Revert"
            placeholder="Explain why this approval needs to be reverted..."
            value={reason}
            onChange={e => setReason(e.target.value)}
            onBlur={() => setTouched(true)}
            error={showError}
            helperText={
              showError
                ? errorMessage
                : `${reason.length}/${APPROVAL_FIELD_LIMITS.revertReason.max} characters (min ${APPROVAL_FIELD_LIMITS.revertReason.min})`
            }
            inputProps={{ maxLength: APPROVAL_FIELD_LIMITS.revertReason.max }}
          />

          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ display: 'block', mt: 2 }}
          >
            Press Ctrl+Enter to revert, Escape to cancel
          </Typography>
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2 }}>
          <CobraSecondaryButton
            onClick={() => setDialogOpen(false)}
            disabled={isReverting}
          >
            Cancel
          </CobraSecondaryButton>
          <CobraWarningButton
            onClick={handleRevert}
            disabled={isReverting || !isValid}
            startIcon={
              <FontAwesomeIcon
                icon={isReverting ? faSpinner : faUndo}
                spin={isReverting}
              />
            }
          >
            Revert
          </CobraWarningButton>
        </DialogActions>
      </Dialog>
    </>
  )
}

export default RevertApprovalAction
