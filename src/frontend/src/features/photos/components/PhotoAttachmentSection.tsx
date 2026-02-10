/**
 * PhotoAttachmentSection Component
 *
 * Horizontal scrolling strip of photo thumbnails for observation forms.
 * Allows adding new photos and displays existing ones.
 * Supports two modes:
 * - Edit mode (has observationId): uploads photos immediately to API
 * - Create/staging mode (no observationId): collects files locally for upload after creation
 *
 * @module features/photos/components
 */

import { type FC, useState, useEffect, useRef } from 'react'
import { Box, CircularProgress, IconButton, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faXmark } from '@fortawesome/free-solid-svg-icons'

import { CobraSecondaryButton } from '../../../theme/styledComponents'
import { formatDateTime } from '../../../shared/utils/dateUtils'
import { useCamera } from '../hooks/useCamera'
import { useImageCompression } from '../hooks/useImageCompression'
import { usePhotos } from '../hooks/usePhotos'
import type { PhotoTagDto } from '../../observations/types'

interface PhotoAttachmentSectionProps {
  /** The exercise this observation belongs to */
  exerciseId: string
  /** The observation ID (when editing an existing observation) */
  observationId?: string
  /** Current photos attached to the observation */
  photos: PhotoTagDto[]
  /** Called after a photo is successfully added */
  onPhotoAdded?: () => void
  /** Scenario time to stamp the photo with (optional) */
  scenarioTime?: string | null
  /** Staged files for creation mode (no observationId yet) */
  pendingFiles?: File[]
  /** Called when staged files change in creation mode */
  onPendingFilesChange?: (files: File[]) => void
}

/**
 * Photo attachment section for observation forms
 *
 * Displays a horizontal scrollable row of thumbnails with an "Add Photo" button.
 * In edit mode (observationId present), uploads photos immediately.
 * In create mode (no observationId), stages files locally for later upload.
 */
export const PhotoAttachmentSection: FC<PhotoAttachmentSectionProps> = ({
  exerciseId,
  observationId,
  photos,
  onPhotoAdded,
  scenarioTime,
  pendingFiles = [],
  onPendingFilesChange,
}) => {
  const [isUploading, setIsUploading] = useState(false)
  const { compressImage } = useImageCompression()
  const { uploadPhoto } = usePhotos(exerciseId)

  // Track object URLs for pending file previews so we can revoke them
  const previewUrlsRef = useRef<Map<File, string>>(new Map())

  // Clean up object URLs on unmount or when pendingFiles change
  useEffect(() => {
    return () => {
      previewUrlsRef.current.forEach(url => URL.revokeObjectURL(url))
      previewUrlsRef.current.clear()
    }
  }, [])

  const getPreviewUrl = (file: File): string => {
    if (!previewUrlsRef.current.has(file)) {
      previewUrlsRef.current.set(file, URL.createObjectURL(file))
    }
    return previewUrlsRef.current.get(file)!
  }

  // Handle file selection - either upload immediately or stage locally
  const handleFileSelected = async (file: File) => {
    if (observationId) {
      // Edit mode: upload immediately
      try {
        setIsUploading(true)
        const { compressed } = await compressImage(file)

        const formData = new FormData()
        formData.append('photo', compressed, file.name)
        formData.append('capturedAt', new Date().toISOString())
        if (scenarioTime) {
          formData.append('scenarioTime', scenarioTime)
        }
        formData.append('observationId', observationId)

        await uploadPhoto(formData)
        onPhotoAdded?.()
      } catch (error) {
        console.error('Failed to upload photo:', error)
      } finally {
        setIsUploading(false)
      }
    } else {
      // Create/staging mode: collect file locally
      onPendingFilesChange?.([...pendingFiles, file])
    }
  }

  const handleRemovePendingFile = (index: number) => {
    const file = pendingFiles[index]
    const url = previewUrlsRef.current.get(file)
    if (url) {
      URL.revokeObjectURL(url)
      previewUrlsRef.current.delete(file)
    }
    onPendingFilesChange?.(pendingFiles.filter((_, i) => i !== index))
  }

  const { fileInputRef, handleFileChange, openGallery } = useCamera(handleFileSelected)

  const hasPendingFiles = pendingFiles.length > 0
  const hasExistingPhotos = photos.length > 0

  // Show section if there's an observationId (edit mode) or pending files or we're in create mode with the callback
  const isCreateMode = !observationId && !!onPendingFilesChange

  if (!observationId && !isCreateMode) {
    return null
  }

  return (
    <Box sx={{ py: 1 }}>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        {isCreateMode ? 'Attach Photos (Optional)' : 'Attached Photos'}
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
        {/* Existing Photo Thumbnails (edit mode) */}
        {hasExistingPhotos && photos
          .sort((a, b) => a.displayOrder - b.displayOrder)
          .map((photo) => (
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
                cursor: 'pointer',
                flexShrink: 0,
                '&:hover': {
                  opacity: 0.8,
                },
              }}
              onClick={() => {
                // TODO: Open PhotoPreview in future
                console.log('Photo clicked:', photo.id)
              }}
            />
          ))}

        {/* Pending File Previews (create mode) */}
        {hasPendingFiles && pendingFiles.map((file, index) => (
          <Box
            key={`pending-${index}`}
            sx={{ position: 'relative', flexShrink: 0 }}
          >
            <Box
              component="img"
              src={getPreviewUrl(file)}
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
            <IconButton
              size="small"
              onClick={() => handleRemovePendingFile(index)}
              sx={{
                position: 'absolute',
                top: -8,
                right: -8,
                bgcolor: 'error.main',
                color: 'white',
                width: 20,
                height: 20,
                '&:hover': { bgcolor: 'error.dark' },
              }}
            >
              <FontAwesomeIcon icon={faXmark} style={{ fontSize: 10 }} />
            </IconButton>
          </Box>
        ))}

        {/* Add Photo Button */}
        <CobraSecondaryButton
          size="small"
          onClick={openGallery}
          disabled={isUploading}
          startIcon={
            isUploading ? (
              <CircularProgress size={16} />
            ) : (
              <FontAwesomeIcon icon={faPlus} />
            )
          }
          sx={{
            flexShrink: 0,
            height: 60,
            minWidth: 100,
          }}
        >
          {isUploading ? 'Uploading...' : 'Add Photo'}
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
