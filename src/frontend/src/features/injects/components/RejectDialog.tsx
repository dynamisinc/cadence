/**
 * RejectDialog Component
 *
 * Dialog for rejecting an inject with required reason (S04).
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
import { faTimes, faSpinner, faInfoCircle } from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import {
  CobraDeleteButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import { APPROVAL_FIELD_LIMITS } from '../types'
import type { InjectDto } from '../types'

interface RejectDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** The inject being rejected */
  inject: InjectDto | null
  /** Called when user confirms rejection */
  onConfirm: (reason: string) => void
  /** Called when user cancels */
  onCancel: () => void
  /** Whether the action is in progress */
  isLoading?: boolean
}

/**
 * Reject Dialog
 *
 * Shows inject details and requires a reason before rejecting.
 * The inject will be returned to Draft status so the author can make corrections.
 *
 * @example
 * <RejectDialog
 *   open={rejectDialogOpen}
 *   inject={selectedInject}
 *   onConfirm={handleReject}
 *   onCancel={() => setRejectDialogOpen(false)}
 *   isLoading={isRejecting}
 * />
 */
export const RejectDialog = ({
  open,
  inject,
  onConfirm,
  onCancel,
  isLoading = false,
}: RejectDialogProps) => {
  const theme = useTheme()
  const [reason, setReason] = useState('')
  const [touched, setTouched] = useState(false)

  const isValid =
    reason.length >= APPROVAL_FIELD_LIMITS.rejectionReason.min &&
    reason.length <= APPROVAL_FIELD_LIMITS.rejectionReason.max

  const handleConfirm = useCallback(() => {
    if (!isValid) return
    onConfirm(reason.trim())
  }, [reason, isValid, onConfirm])

  // Handle keyboard shortcuts
  useEffect(() => {
    if (!open) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Enter' && e.ctrlKey && isValid) {
        e.preventDefault()
        handleConfirm()
      } else if (e.key === 'Escape') {
        e.preventDefault()
        onCancel()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [open, handleConfirm, onCancel, isValid])

  // Reset form when dialog opens
  useEffect(() => {
    if (open) {
      setReason('')
      setTouched(false)
    }
  }, [open])

  if (!inject) return null

  const showError = touched && !isValid
  const errorMessage =
    reason.length < APPROVAL_FIELD_LIMITS.rejectionReason.min
      ? `Minimum ${APPROVAL_FIELD_LIMITS.rejectionReason.min} characters required`
      : reason.length > APPROVAL_FIELD_LIMITS.rejectionReason.max
        ? `Maximum ${APPROVAL_FIELD_LIMITS.rejectionReason.max} characters allowed`
        : ''

  return (
    <Dialog
      open={open}
      onClose={onCancel}
      maxWidth="sm"
      fullWidth
      aria-labelledby="reject-dialog-title"
    >
      <DialogTitle id="reject-dialog-title">
        <Box display="flex" alignItems="center" gap={1}>
          <FontAwesomeIcon
            icon={faTimes}
            style={{ color: theme.palette.error.main }}
          />
          Reject Inject
        </Box>
      </DialogTitle>

      <DialogContent>
        <Alert severity="info" icon={<FontAwesomeIcon icon={faInfoCircle} />} sx={{ mb: 3 }}>
          Provide feedback so the author can address the issues.
          The inject will be returned to Draft status.
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
              sx={{ mt: 1, display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}
            >
              {inject.description}
            </Typography>
          )}
        </Paper>

        <CobraTextField
          fullWidth
          required
          multiline
          rows={4}
          label="Rejection Reason"
          placeholder="Explain what needs to be corrected..."
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          onBlur={() => setTouched(true)}
          error={showError}
          helperText={
            showError
              ? errorMessage
              : `${reason.length}/${APPROVAL_FIELD_LIMITS.rejectionReason.max} characters (min ${APPROVAL_FIELD_LIMITS.rejectionReason.min})`
          }
          inputProps={{ maxLength: APPROVAL_FIELD_LIMITS.rejectionReason.max }}
        />

        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ display: 'block', mt: 2 }}
        >
          Press Ctrl+Enter to reject, Escape to cancel
        </Typography>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton onClick={onCancel} disabled={isLoading}>
          Cancel
        </CobraSecondaryButton>
        <CobraDeleteButton
          onClick={handleConfirm}
          disabled={isLoading || !isValid}
          startIcon={
            <FontAwesomeIcon
              icon={isLoading ? faSpinner : faTimes}
              spin={isLoading}
            />
          }
        >
          Reject
        </CobraDeleteButton>
      </DialogActions>
    </Dialog>
  )
}

export default RejectDialog
