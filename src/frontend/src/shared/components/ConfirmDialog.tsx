/**
 * ConfirmDialog Component
 *
 * A reusable confirmation dialog that provides a friendly alternative
 * to browser's native window.confirm().
 *
 * Features:
 * - Customizable title, message, and button labels
 * - Supports different severity levels (warning, danger, info)
 * - Accessible with proper focus management
 * - Keyboard support (Enter to confirm, Escape to cancel)
 *
 * Usage:
 * ```tsx
 * <ConfirmDialog
 *   open={showConfirm}
 *   title="Discard changes?"
 *   message="You have unsaved changes that will be lost."
 *   confirmLabel="Discard"
 *   cancelLabel="Keep Editing"
 *   severity="warning"
 *   onConfirm={handleDiscard}
 *   onCancel={() => setShowConfirm(false)}
 * />
 * ```
 */

import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Box,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faTriangleExclamation, faCircleExclamation, faCircleInfo } from '@fortawesome/free-solid-svg-icons'
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraDeleteButton,
} from '../../theme/styledComponents'

export type ConfirmDialogSeverity = 'info' | 'warning' | 'danger'

export interface ConfirmDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Dialog title */
  title: string
  /** Dialog message/description */
  message: React.ReactNode
  /** Label for the confirm button */
  confirmLabel?: string
  /** Label for the cancel button */
  cancelLabel?: string
  /** Severity level affects icon and confirm button style */
  severity?: ConfirmDialogSeverity
  /** Called when user confirms */
  onConfirm: () => void
  /** Called when user cancels */
  onCancel: () => void
  /** Whether confirm action is in progress */
  isConfirming?: boolean
}

const severityConfig: Record<ConfirmDialogSeverity, { icon: IconDefinition; color: 'info.main' | 'warning.main' | 'error.main' }> = {
  info: {
    icon: faCircleInfo,
    color: 'info.main',
  },
  warning: {
    icon: faTriangleExclamation,
    color: 'warning.main',
  },
  danger: {
    icon: faCircleExclamation,
    color: 'error.main',
  },
}

/**
 * Reusable confirmation dialog component
 */
export const ConfirmDialog = ({
  open,
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  severity = 'warning',
  onConfirm,
  onCancel,
  isConfirming = false,
}: ConfirmDialogProps) => {
  const theme = useTheme()
  const { icon, color } = severityConfig[severity]

  const ConfirmButton = severity === 'danger' ? CobraDeleteButton : CobraPrimaryButton

  return (
    <Dialog
      open={open}
      onClose={onCancel}
      aria-labelledby="confirm-dialog-title"
      aria-describedby="confirm-dialog-description"
      maxWidth="xs"
      fullWidth
      PaperProps={{
        sx: {
          borderRadius: 2,
        },
      }}
    >
      <DialogTitle
        id="confirm-dialog-title"
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
          pb: 1,
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            width: 40,
            height: 40,
            borderRadius: '50%',
            backgroundColor: color,
            opacity: 0.1,
          }}
        >
          <Box
            component="span"
            sx={{
              color: color,
              fontSize: 24,
              position: 'absolute',
            }}
          >
            <FontAwesomeIcon icon={icon} />
          </Box>
        </Box>
        <Box component="span" sx={{ position: 'relative', ml: -4, color: color, fontSize: 24 }}>
          <FontAwesomeIcon icon={icon} />
        </Box>
        {title}
      </DialogTitle>

      <DialogContent>
        <DialogContentText
          id="confirm-dialog-description"
          sx={{
            color: theme.palette.text.secondary,
          }}
        >
          {message}
        </DialogContentText>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2, pt: 0 }}>
        <CobraSecondaryButton
          onClick={onCancel}
          disabled={isConfirming}
        >
          {cancelLabel}
        </CobraSecondaryButton>
        <ConfirmButton
          onClick={onConfirm}
          disabled={isConfirming}
          autoFocus
        >
          {isConfirming ? 'Please wait...' : confirmLabel}
        </ConfirmButton>
      </DialogActions>
    </Dialog>
  )
}

export default ConfirmDialog
