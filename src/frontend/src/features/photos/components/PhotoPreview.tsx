/**
 * PhotoPreview Component
 *
 * Full-screen dialog for viewing photos with navigation.
 * Shows photo metadata (captured by, timestamp, location).
 * Supports keyboard navigation (arrows, escape).
 * Uses COBRA styled buttons and FontAwesome icons.
 *
 * @module features/photos/components
 */

import { useEffect } from 'react'
import {
  Dialog,
  Box,
  Typography,
  Stack,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faXmark,
  faChevronLeft,
  faChevronRight,
  faLocationDot,
} from '@fortawesome/free-solid-svg-icons'
import { CobraIconButton } from '@/theme/styledComponents'
import { formatDateTime } from '@/shared/utils/dateUtils'
import type { PhotoDto } from '../types'

interface PhotoPreviewProps {
  /** List of photos to preview */
  photos: PhotoDto[]
  /** Index of currently displayed photo */
  currentIndex: number
  /** Whether dialog is open */
  open: boolean
  /** Called when dialog should close */
  onClose: () => void
  /** Called when user navigates to different photo */
  onNavigate: (index: number) => void
}

/**
 * Format coordinates for display
 */
const formatCoordinates = (lat: number, lon: number): string => {
  const latDir = lat >= 0 ? 'N' : 'S'
  const lonDir = lon >= 0 ? 'E' : 'W'
  return `${Math.abs(lat).toFixed(6)}° ${latDir}, ${Math.abs(lon).toFixed(6)}° ${lonDir}`
}

export const PhotoPreview = ({
  photos,
  currentIndex,
  open,
  onClose,
  onNavigate,
}: PhotoPreviewProps) => {
  const photo = photos[currentIndex]
  const hasPrevious = currentIndex > 0
  const hasNext = currentIndex < photos.length - 1

  /**
   * Handle keyboard navigation
   */
  useEffect(() => {
    if (!open) return

    const handleKeyDown = (event: KeyboardEvent) => {
      switch (event.key) {
        case 'ArrowLeft':
          if (hasPrevious) {
            onNavigate(currentIndex - 1)
          }
          break
        case 'ArrowRight':
          if (hasNext) {
            onNavigate(currentIndex + 1)
          }
          break
        case 'Escape':
          onClose()
          break
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [open, currentIndex, hasPrevious, hasNext, onNavigate, onClose])

  if (!photo) return null

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullScreen
      PaperProps={{
        sx: {
          bgcolor: 'black',
        },
      }}
    >
      {/* Close button */}
      <Box
        sx={{
          position: 'absolute',
          top: 16,
          right: 16,
          zIndex: 1,
        }}
      >
        <CobraIconButton
          onClick={onClose}
          aria-label="Close preview"
          sx={{
            color: 'white',
            bgcolor: 'rgba(0, 0, 0, 0.5)',
            '&:hover': {
              bgcolor: 'rgba(0, 0, 0, 0.7)',
            },
          }}
        >
          <FontAwesomeIcon icon={faXmark} />
        </CobraIconButton>
      </Box>

      {/* Main content */}
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          height: '100vh',
          position: 'relative',
        }}
      >
        {/* Image container */}
        <Box
          sx={{
            flex: 1,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            p: 4,
            position: 'relative',
          }}
        >
          {/* Previous button */}
          {hasPrevious && (
            <Box
              sx={{
                position: 'absolute',
                left: 16,
                top: '50%',
                transform: 'translateY(-50%)',
              }}
            >
              <CobraIconButton
                onClick={() => onNavigate(currentIndex - 1)}
                aria-label="Previous photo"
                sx={{
                  color: 'white',
                  bgcolor: 'rgba(0, 0, 0, 0.5)',
                  '&:hover': {
                    bgcolor: 'rgba(0, 0, 0, 0.7)',
                  },
                }}
              >
                <FontAwesomeIcon icon={faChevronLeft} />
              </CobraIconButton>
            </Box>
          )}

          {/* Photo */}
          <Box
            component="img"
            src={photo.blobUri}
            alt={photo.fileName}
            sx={{
              maxWidth: '100%',
              maxHeight: '100%',
              objectFit: 'contain',
            }}
          />

          {/* Next button */}
          {hasNext && (
            <Box
              sx={{
                position: 'absolute',
                right: 16,
                top: '50%',
                transform: 'translateY(-50%)',
              }}
            >
              <CobraIconButton
                onClick={() => onNavigate(currentIndex + 1)}
                aria-label="Next photo"
                sx={{
                  color: 'white',
                  bgcolor: 'rgba(0, 0, 0, 0.5)',
                  '&:hover': {
                    bgcolor: 'rgba(0, 0, 0, 0.7)',
                  },
                }}
              >
                <FontAwesomeIcon icon={faChevronRight} />
              </CobraIconButton>
            </Box>
          )}
        </Box>

        {/* Metadata footer */}
        <Box
          sx={{
            bgcolor: 'rgba(0, 0, 0, 0.8)',
            color: 'white',
            p: 2,
            borderTop: 1,
            borderColor: 'rgba(255, 255, 255, 0.1)',
          }}
        >
          <Stack spacing={1}>
            {/* Photo counter */}
            <Typography variant="body2" color="rgba(255, 255, 255, 0.7)">
              Photo {currentIndex + 1} of {photos.length}
            </Typography>

            <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.1)' }} />

            {/* Captured by */}
            {photo.capturedByName && (
              <Typography variant="body2">
                <strong>Captured by:</strong> {photo.capturedByName}
              </Typography>
            )}

            {/* Timestamp */}
            <Typography variant="body2">
              <strong>Time:</strong> {formatDateTime(photo.capturedAt)}
            </Typography>

            {/* Scenario time */}
            {photo.scenarioTime && (
              <Typography variant="body2">
                <strong>Scenario Time:</strong> {photo.scenarioTime}
              </Typography>
            )}

            {/* Location */}
            {photo.latitude !== null && photo.longitude !== null && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <FontAwesomeIcon icon={faLocationDot} />
                <Typography variant="body2">
                  {formatCoordinates(photo.latitude, photo.longitude)}
                  {photo.locationAccuracy && (
                    <Typography
                      component="span"
                      variant="caption"
                      sx={{ ml: 1, opacity: 0.7 }}
                    >
                      (±{Math.round(photo.locationAccuracy)}m)
                    </Typography>
                  )}
                </Typography>
              </Box>
            )}

            {/* File name */}
            <Typography variant="caption" color="rgba(255, 255, 255, 0.5)">
              {photo.fileName}
            </Typography>
          </Stack>
        </Box>
      </Box>
    </Dialog>
  )
}

export default PhotoPreview
