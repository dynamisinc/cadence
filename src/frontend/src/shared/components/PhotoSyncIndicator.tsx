/**
 * PhotoSyncIndicator Component
 *
 * Visual indicator showing photo-specific sync progress during upload.
 * Shows "Syncing X of Y photos..." with a progress bar during sync,
 * then displays "All photos synced" with a check icon when complete.
 *
 * Features:
 * - Linear progress bar during sync
 * - Auto-dismisses success message after 3 seconds
 * - Compact design suitable for app header or status bar
 * - Uses COBRA styled components and FontAwesome icons
 *
 * @module shared/components
 *
 * @example
 * ```tsx
 * <PhotoSyncIndicator current={3} total={7} />
 * ```
 */

import { useState, useEffect } from 'react'
import { Box, Typography, LinearProgress } from '@mui/material'
import { useTheme, alpha } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCloudArrowUp, faCircleCheck } from '@fortawesome/free-solid-svg-icons'

export interface PhotoSyncIndicatorProps {
  /** Number of photos synced so far */
  current: number
  /** Total number of photos to sync */
  total: number
}

/**
 * Photo sync progress indicator
 */
export const PhotoSyncIndicator = ({ current, total }: PhotoSyncIndicatorProps) => {
  const theme = useTheme()
  const [showSuccess, setShowSuccess] = useState(false)

  const isComplete = current >= total && total > 0
  const progress = total > 0 ? (current / total) * 100 : 0

  // Auto-dismiss "All synced" message after 3 seconds
  useEffect(() => {
    if (isComplete) {
      setShowSuccess(true)
      const timer = setTimeout(() => {
        setShowSuccess(false)
      }, 3000)

      return () => clearTimeout(timer)
    } else {
      setShowSuccess(false)
    }
  }, [isComplete])

  // Don't show if no photos to sync
  if (total === 0) {
    return null
  }

  // Don't show if success message has been dismissed
  if (isComplete && !showSuccess) {
    return null
  }

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 1,
        px: 2,
        py: 1.5,
        borderRadius: 1,
        backgroundColor: isComplete ? alpha(theme.palette.semantic.success, 0.1) : alpha(theme.palette.semantic.info, 0.1),
        border: '1px solid',
        borderColor: isComplete ? alpha(theme.palette.semantic.success, 0.3) : alpha(theme.palette.semantic.info, 0.3),
        minWidth: 280,
      }}
      data-testid="photo-sync-indicator"
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            width: 20,
            height: 20,
            color: isComplete ? theme.palette.semantic.success : theme.palette.semantic.info,
          }}
        >
          <FontAwesomeIcon
            icon={isComplete ? faCircleCheck : faCloudArrowUp}
            size="lg"
          />
        </Box>

        <Typography
          variant="body2"
          sx={{
            fontWeight: 500,
            color: isComplete ? theme.palette.semantic.success : theme.palette.semantic.info,
          }}
        >
          {isComplete ? (
            'All photos synced'
          ) : (
            <>
              Syncing {current} of {total} photos...
            </>
          )}
        </Typography>
      </Box>

      {!isComplete && (
        <LinearProgress
          variant="determinate"
          value={progress}
          sx={{
            height: 6,
            borderRadius: 3,
            backgroundColor: alpha(theme.palette.semantic.info, 0.1),
            '& .MuiLinearProgress-bar': {
              backgroundColor: theme.palette.semantic.info,
              borderRadius: 3,
            },
          }}
        />
      )}
    </Box>
  )
}

export default PhotoSyncIndicator
