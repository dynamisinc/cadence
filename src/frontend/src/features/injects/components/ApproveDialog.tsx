/**
 * ApproveDialog Component
 *
 * Dialog for approving an inject with optional notes (S04).
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
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import { APPROVAL_FIELD_LIMITS } from '../types'
import type { InjectDto } from '../types'

interface ApproveDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** The inject being approved */
  inject: InjectDto | null
  /** Called when user confirms approval */
  onConfirm: (notes?: string) => void
  /** Called when user cancels */
  onCancel: () => void
  /** Whether the action is in progress */
  isLoading?: boolean
  /** Whether this is a self-approval (S11) */
  isSelfApproval?: boolean
  /** Whether self-approval requires confirmation (S11) */
  requiresConfirmation?: boolean
}

/**
 * Approve Dialog
 *
 * Shows inject details and allows the user to add optional review notes
 * before approving. Shows a warning when self-approval requires confirmation.
 *
 * @example
 * <ApproveDialog
 *   open={approveDialogOpen}
 *   inject={selectedInject}
 *   onConfirm={handleApprove}
 *   onCancel={() => setApproveDialogOpen(false)}
 *   isLoading={isApproving}
 *   isSelfApproval={true}
 *   requiresConfirmation={true}
 * />
 */
export const ApproveDialog = ({
  open,
  inject,
  onConfirm,
  onCancel,
  isLoading = false,
  isSelfApproval = false,
  requiresConfirmation = false,
}: ApproveDialogProps) => {
  const theme = useTheme()
  const [notes, setNotes] = useState('')

  const handleConfirm = useCallback(() => {
    onConfirm(notes.trim() || undefined)
  }, [notes, onConfirm])

  // Handle keyboard shortcuts
  useEffect(() => {
    if (!open) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Enter' && e.ctrlKey) {
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

  // Reset notes when dialog opens
  useEffect(() => {
    if (open) {
      setNotes('')
    }
  }, [open])

  if (!inject) return null

  return (
    <Dialog
      open={open}
      onClose={onCancel}
      maxWidth="sm"
      fullWidth
      aria-labelledby="approve-dialog-title"
    >
      <DialogTitle id="approve-dialog-title">
        <Box display="flex" alignItems="center" gap={1}>
          <FontAwesomeIcon
            icon={faCheck}
            style={{ color: theme.palette.success.main }}
          />
          Approve Inject
        </Box>
      </DialogTitle>

      <DialogContent>
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
          {inject.description && (
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{ mt: 1, display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}
            >
              {inject.description}
            </Typography>
          )}
        </Paper>

        {isSelfApproval && requiresConfirmation && (
          <Alert severity="warning" sx={{ mb: 3 }}>
            <Typography variant="body2" fontWeight={500} gutterBottom>
              Self-Approval Notice
            </Typography>
            <Typography variant="body2">
              You are approving an inject you submitted. This action
              will be recorded in the audit log. Click Approve to
              confirm.
            </Typography>
          </Alert>
        )}

        <CobraTextField
          fullWidth
          multiline
          rows={3}
          label="Review Notes (optional)"
          placeholder="Add any notes about your review..."
          value={notes}
          onChange={e => setNotes(e.target.value)}
          inputProps={{ maxLength: APPROVAL_FIELD_LIMITS.approverNotes.max }}
          helperText={`${notes.length}/${APPROVAL_FIELD_LIMITS.approverNotes.max} characters`}
        />

        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ display: 'block', mt: 2 }}
        >
          Press Ctrl+Enter to approve, Escape to cancel
        </Typography>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton onClick={onCancel} disabled={isLoading}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleConfirm}
          disabled={isLoading}
          startIcon={
            <FontAwesomeIcon
              icon={isLoading ? faSpinner : faCheck}
              spin={isLoading}
            />
          }
        >
          Approve
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default ApproveDialog
