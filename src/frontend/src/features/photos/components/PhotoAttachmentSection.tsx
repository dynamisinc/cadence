/**
 * PhotoAttachmentSection Component
 *
 * Horizontal scrolling strip of photo thumbnails for observation forms.
 * Allows selecting new photos and displays existing ones.
 * Always stages files locally - the parent is responsible for uploading on form submit.
 *
 * @module features/photos/components
 */

import { type FC, useEffect, useMemo } from 'react'
import { Box, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faXmark } from '@fortawesome/free-solid-svg-icons'

import { CobraIconButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import { formatDateTime } from '../../../shared/utils/dateUtils'
import { useCamera } from '../hooks/useCamera'
import type { PhotoTagDto } from '../../observations/types'

interface PhotoAttachmentSectionProps {
  /** Current photos already saved on the observation */
  photos: PhotoTagDto[]
  /** Staged files awaiting upload on form submit */
  pendingFiles?: File[]
  /** Called when staged files change */
  onPendingFilesChange?: (files: File[]) => void
}

/**
 * Photo attachment section for observation forms
 *
 * Displays a horizontal scrollable row of existing thumbnails plus local file previews,
 * with an "Add Photo" button. All new photos are staged locally and only uploaded
 * when the parent form is submitted.
 */
export const PhotoAttachmentSection: FC<PhotoAttachmentSectionProps> = ({
  photos,
  pendingFiles = [],
  onPendingFilesChange,
}) => {
  // Compute preview URLs from pending files (safe to use during render)
  const previewUrls = useMemo(
    () => pendingFiles.map(file => URL.createObjectURL(file)),
    [pendingFiles],
  )

  // Clean up object URLs when pending files change or on unmount
  useEffect(() => {
    return () => {
      previewUrls.forEach(url => URL.revokeObjectURL(url))
    }
  }, [previewUrls])

  // Stage file locally - parent handles upload on submit
  const handleFileSelected = (file: File) => {
    onPendingFilesChange?.([...pendingFiles, file])
  }

  const handleRemovePendingFile = (index: number) => {
    onPendingFilesChange?.(pendingFiles.filter((_, i) => i !== index))
  }

  const { fileInputRef, handleFileChange, openGallery } = useCamera(handleFileSelected)

  const hasPendingFiles = pendingFiles.length > 0
  const hasExistingPhotos = photos.length > 0

  if (!onPendingFilesChange) {
    return null
  }

  return (
    <Box sx={{ py: 1 }}>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        {hasExistingPhotos ? 'Attached Photos' : 'Attach Photos (Optional)'}
      </Typography>

      <Box
        sx={{
          display: 'flex',
          gap: 1,
          overflowX: 'auto',
          alignItems: 'center',
          pb: 1,
        }}
      >
        {/* Existing Photo Thumbnails (already saved) */}
        {hasExistingPhotos && photos
          .sort((a, b) => a.displayOrder - b.displayOrder)
          .map(photo => (
            <Box
              key={photo.id}
              component="img"
              src={photo.thumbnailUri}
              alt={`Photo from ${formatDateTime(photo.capturedAt)}`}
              sx={{
                height: 60,
                width: 60,
                objectFit: 'cover',
                borderRadius: 1,
                border: 1,
                borderColor: 'divider',
                flexShrink: 0,
              }}
            />
          ))}

        {/* Pending File Previews (not yet uploaded) */}
        {hasPendingFiles && pendingFiles.map((file, index) => (
          <Box
            key={`pending-${index}`}
            sx={{ position: 'relative', flexShrink: 0 }}
          >
            <Box
              component="img"
              src={previewUrls[index]}
              alt={file.name}
              sx={{
                height: 60,
                width: 60,
                objectFit: 'cover',
                borderRadius: 1,
                border: 1,
                borderColor: 'primary.main',
                flexShrink: 0,
              }}
            />
            <CobraIconButton
              size="small"
              onClick={() => handleRemovePendingFile(index)}
              sx={{
                position: 'absolute',
                top: 2,
                right: 2,
                bgcolor: 'error.main',
                color: 'white',
                width: 18,
                height: 18,
                minWidth: 0,
                p: 0,
                '&:hover': { bgcolor: 'error.dark' },
              }}
            >
              <FontAwesomeIcon icon={faXmark} style={{ fontSize: 10 }} />
            </CobraIconButton>
          </Box>
        ))}

        {/* Add Photo Button */}
        <CobraSecondaryButton
          size="small"
          onClick={openGallery}
          startIcon={<FontAwesomeIcon icon={faPlus} />}
          sx={{
            flexShrink: 0,
            height: 60,
            minWidth: 100,
          }}
        >
          Add Photo
        </CobraSecondaryButton>

        {/* Hidden file input */}
        <input
          ref={fileInputRef}
          type="file"
          accept="image/*"
          hidden
          onChange={handleFileChange}
        />
      </Box>
    </Box>
  )
}

export default PhotoAttachmentSection
