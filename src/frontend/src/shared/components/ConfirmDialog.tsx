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
import WarningAmberIcon from '@mui/icons-material/WarningAmber'
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
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
  message: string
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

const severityConfig = {
  info: {
    Icon: InfoOutlinedIcon,
    color: 'info.main' as const,
  },
  warning: {
    Icon: WarningAmberIcon,
    color: 'warning.main' as const,
  },
  danger: {
    Icon: ErrorOutlineIcon,
    color: 'error.main' as const,
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
  const { Icon, color } = severityConfig[severity]

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
            backgroundColor: `${color}`,
            opacity: 0.1,
          }}
        >
          <Icon
            sx={{
              color: color,
              fontSize: 24,
              position: 'absolute',
            }}
          />
        </Box>
        <Box sx={{ position: 'relative', ml: -4 }}>
          <Icon
            sx={{
              color: color,
              fontSize: 24,
            }}
          />
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
