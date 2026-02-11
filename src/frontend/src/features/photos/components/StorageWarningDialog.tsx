/**
 * StorageWarningDialog Component
 *
 * Dialog that warns users when device storage is running low before photo capture.
 * Shows warning (80-95% full) or critical (>95% full) alerts with queue stats.
 *
 * Features:
 * - Warning level: Yellow icon, "Continue Anyway" + "Sync Now" options
 * - Critical level: Red icon, only "Sync Now" option (no continue)
 * - Displays queue stats: X photos queued (Y MB) and Z% storage used
 * - Uses COBRA styled components and FontAwesome icons
 *
 * @module features/photos/components
 *
 * @example
 * ```tsx
 * <StorageWarningDialog
 *   open={showWarning}
 *   warningLevel="warning"
 *   usagePercent={85}
 *   queuedCount={12}
 *   queuedSizeBytes={45000000}
 *   onContinue={() => setShowWarning(false)}
 *   onSyncNow={handleSync}
 *   onClose={() => setShowWarning(false)}
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
  Typography,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faTriangleExclamation, faCircleExclamation } from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '../../../theme/styledComponents'

export interface StorageWarningDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Warning level based on storage usage */
  warningLevel: 'warning' | 'critical'
  /** Storage usage percentage (0-100) */
  usagePercent: number
  /** Number of photos queued for sync */
  queuedCount: number
  /** Total size of queued photos in bytes */
  queuedSizeBytes: number
  /** Called when user chooses to continue anyway (warning level only) */
  onContinue: () => void
  /** Called when user chooses to sync now */
  onSyncNow: () => void
  /** Called when user closes the dialog */
  onClose: () => void
}

/**
 * Format bytes as human-readable size (KB/MB/GB)
 */
function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 Bytes'

  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))

  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

/**
 * Storage warning dialog component
 */
export const StorageWarningDialog = ({
  open,
  warningLevel,
  usagePercent,
  queuedCount,
  queuedSizeBytes,
  onContinue,
  onSyncNow,
  onClose,
}: StorageWarningDialogProps) => {
  const isCritical = warningLevel === 'critical'
  const icon = isCritical ? faCircleExclamation : faTriangleExclamation
  const iconColor = isCritical ? '#ef4444' : '#f59e0b'
  const title = isCritical ? 'Storage Full' : 'Storage Getting Full'

  return (
    <Dialog
      open={open}
      onClose={onClose}
      aria-labelledby="storage-warning-dialog-title"
      aria-describedby="storage-warning-dialog-description"
      maxWidth="sm"
      fullWidth
      PaperProps={{
        sx: {
          borderRadius: 2,
        },
      }}
    >
      <DialogTitle
        id="storage-warning-dialog-title"
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
            backgroundColor: iconColor,
            opacity: 0.1,
          }}
        >
          <Box
            component="span"
            sx={{
              color: iconColor,
              fontSize: 24,
              position: 'absolute',
            }}
          >
            <FontAwesomeIcon icon={icon} />
          </Box>
        </Box>
        <Box component="span" sx={{ position: 'relative', ml: -4, color: iconColor, fontSize: 24 }}>
          <FontAwesomeIcon icon={icon} />
        </Box>
        {title}
      </DialogTitle>

      <DialogContent>
        <DialogContentText
          id="storage-warning-dialog-description"
          sx={{ mb: 2 }}
        >
          {isCritical ? (
            <>
              Your device storage is nearly full. You must sync queued photos before capturing more.
            </>
          ) : (
            <>
              Your device storage is running low. Consider syncing queued photos to free up space.
            </>
          )}
        </DialogContentText>

        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: 1,
            p: 2,
            borderRadius: 1,
            backgroundColor: 'rgba(0, 0, 0, 0.05)',
          }}
        >
          <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
            <Typography variant="body2" color="text.secondary">
              Storage Used:
            </Typography>
            <Typography variant="body2" fontWeight={600}>
              {usagePercent.toFixed(1)}%
            </Typography>
          </Box>

          <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
            <Typography variant="body2" color="text.secondary">
              Queued Photos:
            </Typography>
            <Typography variant="body2" fontWeight={600}>
              {queuedCount} {queuedCount === 1 ? 'photo' : 'photos'} ({formatBytes(queuedSizeBytes)})
            </Typography>
          </Box>
        </Box>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2, pt: 0 }}>
        {!isCritical && (
          <CobraSecondaryButton onClick={onContinue}>
            Continue Anyway
          </CobraSecondaryButton>
        )}
        <CobraPrimaryButton onClick={onSyncNow} autoFocus>
          Sync Now
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default StorageWarningDialog
