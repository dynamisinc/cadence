/**
 * PhotoThumbnail Component
 *
 * Displays a photo thumbnail with optional sync status indicator.
 * Shows upload status for photos pending sync (temp IDs starting with "temp-").
 * Uses COBRA spacing and FontAwesome icons.
 *
 * @module features/photos/components
 */

import { Box } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCloudArrowUp } from '@fortawesome/free-solid-svg-icons'
import type { PhotoDto } from '../types'

interface PhotoThumbnailProps {
  /** Photo to display */
  photo: PhotoDto
  /** Called when thumbnail is clicked */
  onClick?: () => void
  /** Thumbnail size variant */
  size?: 'small' | 'medium' | 'large'
  /** Show sync status indicator for pending uploads */
  showSyncStatus?: boolean
}

/**
 * Get size in pixels for thumbnail variant
 */
const getThumbnailSize = (size: 'small' | 'medium' | 'large'): number => {
  switch (size) {
    case 'small':
      return 60
    case 'medium':
      return 80
    case 'large':
      return 120
    default:
      return 80
  }
}

export const PhotoThumbnail = ({
  photo,
  onClick,
  size = 'medium',
  showSyncStatus = true,
}: PhotoThumbnailProps) => {
  const thumbnailSize = getThumbnailSize(size)
  const isPending = photo.id.startsWith('temp-')

  return (
    <Box
      sx={{
        position: 'relative',
        width: thumbnailSize,
        height: thumbnailSize,
        cursor: onClick ? 'pointer' : 'default',
        '&:hover': onClick
          ? {
            opacity: 0.9,
            transform: 'scale(1.02)',
            transition: 'all 0.2s ease-in-out',
          }
          : undefined,
      }}
      onClick={onClick}
    >
      {/* Thumbnail image */}
      <Box
        component="img"
        src={photo.thumbnailUri}
        alt={photo.fileName}
        sx={{
          width: '100%',
          height: '100%',
          objectFit: 'cover',
          borderRadius: 1,
          border: 1,
          borderColor: 'divider',
        }}
      />

      {/* Sync status indicator */}
      {showSyncStatus && isPending && (
        <Box
          sx={{
            position: 'absolute',
            top: 4,
            right: 4,
            bgcolor: 'warning.main',
            color: 'white',
            borderRadius: '50%',
            width: 24,
            height: 24,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: 1,
          }}
        >
          <FontAwesomeIcon icon={faCloudArrowUp} size="xs" />
        </Box>
      )}
    </Box>
  )
}

export default PhotoThumbnail
