/**
 * SelfApprovalConfirmDialog Component (S11)
 *
 * Warning dialog shown when a user attempts to approve their own inject
 * and the organization policy allows it with warning.
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
import {
  faExclamationTriangle,
  faCheck,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import { APPROVAL_FIELD_LIMITS } from '../types'
import type { InjectDto } from '../types'

interface SelfApprovalConfirmDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** The inject being approved */
  inject: InjectDto | null
  /** Called when user confirms self-approval */
  onConfirm: (notes?: string) => void
  /** Called when user cancels */
  onCancel: () => void
  /** Whether the action is in progress */
  isLoading?: boolean
}

/**
 * Self-Approval Confirmation Dialog
 *
 * Shows a warning when a user is about to approve their own inject.
 * Self-approvals are logged in audit trail.
 *
 * @example
 * <SelfApprovalConfirmDialog
 *   open={selfApprovalDialogOpen}
 *   inject={selectedInject}
 *   onConfirm={handleConfirmSelfApproval}
 *   onCancel={() => setSelfApprovalDialogOpen(false)}
 *   isLoading={isApproving}
 * />
 */
export const SelfApprovalConfirmDialog = ({
  open,
  inject,
  onConfirm,
  onCancel,
  isLoading = false,
}: SelfApprovalConfirmDialogProps) => {
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
      aria-labelledby="self-approval-dialog-title"
    >
      <DialogTitle id="self-approval-dialog-title">
        <Box display="flex" alignItems="center" gap={1}>
          <FontAwesomeIcon
            icon={faExclamationTriangle}
            style={{ color: theme.palette.warning.main }}
          />
          Self-Approval Confirmation
        </Box>
      </DialogTitle>

      <DialogContent>
        <Alert severity="warning" sx={{ mb: 3 }}>
          <Typography variant="body2" fontWeight={500} gutterBottom>
            You are about to approve your own inject.
          </Typography>
          <Typography variant="body2">
            Self-approvals bypass normal separation of duties. This action will be
            recorded in the audit log. Only proceed if this is necessary and
            authorized.
          </Typography>
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
          {inject.description && (
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{
                mt: 1,
                display: '-webkit-box',
                WebkitLineClamp: 2,
                WebkitBoxOrient: 'vertical',
                overflow: 'hidden',
              }}
            >
              {inject.description}
            </Typography>
          )}
        </Paper>

        <CobraTextField
          fullWidth
          multiline
          rows={3}
          label="Justification (recommended)"
          placeholder="Explain why self-approval is necessary..."
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          inputProps={{ maxLength: APPROVAL_FIELD_LIMITS.approverNotes.max }}
          helperText={`${notes.length}/${APPROVAL_FIELD_LIMITS.approverNotes.max} characters`}
        />

        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ display: 'block', mt: 2 }}
        >
          Press Ctrl+Enter to confirm, Escape to cancel
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
          color="warning"
        >
          Confirm Self-Approval
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default SelfApprovalConfirmDialog
