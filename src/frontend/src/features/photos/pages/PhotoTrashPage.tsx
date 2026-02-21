/**
 * PhotoTrashPage
 *
 * Displays soft-deleted photos with restore and permanent delete actions.
 * Shows deleted photo metadata overlay with deletion timestamp.
 *
 * Grid layout:
 * - Mobile: 2 columns
 * - Tablet: 3 columns
 * - Desktop: 4 columns
 *
 * Features:
 * - Click photo to open preview dialog
 * - Restore deleted photos
 * - Permanently delete photos (irreversible)
 * - Confirmation dialogs for restore and permanent delete
 *
 * @module features/photos/pages
 */

import { useState, useMemo, useEffect, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Paper,
  Stack,
  CircularProgress,
  Alert,
  Skeleton,
  useTheme,
  useMediaQuery,
  Dialog,
  DialogContent,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faHome,
  faTrash,
  faRotateLeft,
  faChevronLeft,
  faChevronRight,
  faTimes,
} from '@fortawesome/free-solid-svg-icons'

import { useExercise } from '../../exercises/hooks'
import { usePhotoAdmin } from '../hooks/usePhotoAdmin'
import { CobraIconButton, CobraLinkButton, CobraPrimaryButton, CobraDeleteButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { PageHeader } from '@/shared/components'
import { useBreadcrumbs } from '../../../core/contexts'
import { formatDateTime } from '../../../shared/utils/dateUtils'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'

export const PhotoTrashPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const theme = useTheme()

  // Responsive breakpoints
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const isTablet = useMediaQuery(theme.breakpoints.between('sm', 'md'))

  // Determine grid columns based on screen size
  const gridColumns = isMobile ? 2 : isTablet ? 3 : 4

  // Core data hooks
  const { exercise, loading: exerciseLoading, error: exerciseError } = useExercise(exerciseId)
  const {
    deletedPhotos,
    isLoading,
    error,
    restorePhoto,
    isRestoring,
    permanentDeletePhoto,
    isPermanentDeleting,
  } = usePhotoAdmin(exerciseId!)

  // Preview state
  const [previewPhotoId, setPreviewPhotoId] = useState<string | null>(null)

  // Confirm dialog state
  const [restorePhotoId, setRestorePhotoId] = useState<string | null>(null)
  const [permanentDeletePhotoId, setPermanentDeletePhotoId] = useState<string | null>(null)

  // Set breadcrumbs
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'Photo Gallery', path: `/exercises/${exerciseId}/photos` },
        { label: 'Trash' },
      ]
      : undefined,
  )

  // Get preview photo and navigation
  const previewPhoto = useMemo(
    () => deletedPhotos.find(p => p.id === previewPhotoId) ?? null,
    [deletedPhotos, previewPhotoId],
  )

  const currentIndex = useMemo(
    () => (previewPhotoId ? deletedPhotos.findIndex(p => p.id === previewPhotoId) : -1),
    [deletedPhotos, previewPhotoId],
  )

  const handlePreviousPhoto = useCallback(() => {
    if (currentIndex > 0) {
      setPreviewPhotoId(deletedPhotos[currentIndex - 1].id)
    }
  }, [currentIndex, deletedPhotos])

  const handleNextPhoto = useCallback(() => {
    if (currentIndex < deletedPhotos.length - 1) {
      setPreviewPhotoId(deletedPhotos[currentIndex + 1].id)
    }
  }, [currentIndex, deletedPhotos])

  const handleClosePreview = () => {
    setPreviewPhotoId(null)
  }

  // Handle keyboard navigation in preview
  useEffect(() => {
    if (!previewPhotoId) return

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'ArrowLeft') {
        handlePreviousPhoto()
      } else if (event.key === 'ArrowRight') {
        handleNextPhoto()
      } else if (event.key === 'Escape') {
        handleClosePreview()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [previewPhotoId, handleNextPhoto, handlePreviousPhoto])

  // Handle restore
  const handleRestore = async () => {
    if (restorePhotoId) {
      await restorePhoto(restorePhotoId)
      setRestorePhotoId(null)
      setPreviewPhotoId(null)
    }
  }

  // Handle permanent delete
  const handlePermanentDelete = async () => {
    if (permanentDeletePhotoId) {
      await permanentDeletePhoto(permanentDeletePhotoId)
      setPermanentDeletePhotoId(null)
      setPreviewPhotoId(null)
    }
  }

  // Loading state
  if (exerciseLoading && !exercise) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
        <CircularProgress />
      </Box>
    )
  }

  // Error state
  if (exerciseError && !exercise) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Alert severity="error" sx={{ mb: 2 }}>
          {exerciseError}
        </Alert>
        <CobraLinkButton onClick={() => navigate('/exercises')}>
          Back to Exercises
        </CobraLinkButton>
      </Box>
    )
  }

  // Not found
  if (!exercise) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography variant="h6" gutterBottom>
          Exercise not found
        </Typography>
        <CobraLinkButton onClick={() => navigate('/exercises')}>
          Back to Exercises
        </CobraLinkButton>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title="Photo Trash"
        icon={faTrash}
        chips={
          <Typography variant="body2" color="text.secondary">
            ({deletedPhotos.length} {deletedPhotos.length === 1 ? 'photo' : 'photos'})
          </Typography>
        }
        actions={
          <CobraLinkButton onClick={() => navigate(`/exercises/${exerciseId}/photos`)}>
            Back to Photos
          </CobraLinkButton>
        }
      />

      {/* Main content area */}
      <Paper sx={{ p: 3, minHeight: 400 }}>
        {error ? (
          <Alert severity="error">{error}</Alert>
        ) : isLoading ? (
          // Loading skeleton
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: `repeat(${gridColumns}, 1fr)`,
              gap: 2,
            }}
          >
            {Array.from({ length: 20 }).map((_, i) => (
              <Box key={i} sx={{ aspectRatio: '1' }}>
                <Skeleton variant="rectangular" width="100%" height="100%" />
              </Box>
            ))}
          </Box>
        ) : deletedPhotos.length === 0 ? (
          // Empty state
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              minHeight: 300,
              gap: 2,
            }}
          >
            <FontAwesomeIcon icon={faTrash} size="3x" color={theme.palette.text.secondary} />
            <Typography variant="h6" color="text.secondary">
              No deleted photos
            </Typography>
          </Box>
        ) : (
          // Photo Grid
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: `repeat(${gridColumns}, 1fr)`,
              gap: 2,
            }}
          >
            {deletedPhotos.map(photo => (
              <Box
                key={photo.id}
                onClick={() => setPreviewPhotoId(photo.id)}
                sx={{
                  position: 'relative',
                  aspectRatio: '1',
                  cursor: 'pointer',
                  borderRadius: 1,
                  overflow: 'hidden',
                  border: 1,
                  borderColor: 'divider',
                  '&:hover': {
                    borderColor: 'primary.main',
                    transform: 'scale(1.02)',
                    transition: 'all 0.2s ease-in-out',
                    boxShadow: 2,
                  },
                }}
              >
                {/* Photo thumbnail */}
                <Box
                  component="img"
                  src={photo.thumbnailUri}
                  alt={photo.fileName}
                  sx={{
                    width: '100%',
                    height: '100%',
                    objectFit: 'cover',
                  }}
                />

                {/* Dark overlay with deletion info */}
                <Box
                  sx={{
                    position: 'absolute',
                    top: 0,
                    left: 0,
                    right: 0,
                    bottom: 0,
                    background: 'rgba(0, 0, 0, 0.5)',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'center',
                    alignItems: 'center',
                    gap: 1,
                    p: 1,
                  }}
                >
                  <FontAwesomeIcon icon={faTrash} size="2x" color="white" />
                  <Typography variant="body2" sx={{ color: 'white', fontWeight: 500, textAlign: 'center' }}>
                    Deleted
                  </Typography>
                  {photo.deletedAt && (
                    <Typography variant="caption" sx={{ color: 'white', fontSize: '0.7rem', textAlign: 'center' }}>
                      {formatDateTime(photo.deletedAt)}
                    </Typography>
                  )}
                </Box>
              </Box>
            ))}
          </Box>
        )}
      </Paper>

      {/* Photo Preview Dialog */}
      <Dialog
        open={!!previewPhotoId}
        onClose={handleClosePreview}
        maxWidth="lg"
        fullWidth
        PaperProps={{
          sx: {
            bgcolor: 'rgba(0, 0, 0, 0.95)',
            maxHeight: '90vh',
          },
        }}
      >
        <DialogContent sx={{ p: 0, position: 'relative' }}>
          {previewPhoto && (
            <>
              {/* Close button */}
              <CobraIconButton
                onClick={handleClosePreview}
                sx={{
                  position: 'absolute',
                  top: 16,
                  right: 16,
                  color: 'white',
                  bgcolor: 'rgba(0, 0, 0, 0.5)',
                  '&:hover': { bgcolor: 'rgba(0, 0, 0, 0.7)' },
                  zIndex: 1,
                }}
              >
                <FontAwesomeIcon icon={faTimes} />
              </CobraIconButton>

              {/* Previous button */}
              {currentIndex > 0 && (
                <CobraIconButton
                  onClick={handlePreviousPhoto}
                  sx={{
                    position: 'absolute',
                    left: 16,
                    top: '50%',
                    transform: 'translateY(-50%)',
                    color: 'white',
                    bgcolor: 'rgba(0, 0, 0, 0.5)',
                    '&:hover': { bgcolor: 'rgba(0, 0, 0, 0.7)' },
                    zIndex: 1,
                  }}
                >
                  <FontAwesomeIcon icon={faChevronLeft} />
                </CobraIconButton>
              )}

              {/* Next button */}
              {currentIndex < deletedPhotos.length - 1 && (
                <CobraIconButton
                  onClick={handleNextPhoto}
                  sx={{
                    position: 'absolute',
                    right: 16,
                    top: '50%',
                    transform: 'translateY(-50%)',
                    color: 'white',
                    bgcolor: 'rgba(0, 0, 0, 0.5)',
                    '&:hover': { bgcolor: 'rgba(0, 0, 0, 0.7)' },
                    zIndex: 1,
                  }}
                >
                  <FontAwesomeIcon icon={faChevronRight} />
                </CobraIconButton>
              )}

              {/* Full-size image */}
              <Box
                component="img"
                src={previewPhoto.blobUri}
                alt={previewPhoto.fileName}
                sx={{
                  width: '100%',
                  height: 'auto',
                  maxHeight: '80vh',
                  objectFit: 'contain',
                }}
              />

              {/* Photo metadata and actions */}
              <Box sx={{ p: 2, bgcolor: 'rgba(0, 0, 0, 0.8)' }}>
                <Stack spacing={1}>
                  <Typography variant="body1" sx={{ color: 'white', fontWeight: 500 }}>
                    {previewPhoto.fileName}
                  </Typography>
                  <Typography variant="body2" sx={{ color: 'grey.400' }}>
                    Captured by: {previewPhoto.capturedByName || 'Unknown'}
                  </Typography>
                  <Typography variant="body2" sx={{ color: 'grey.400' }}>
                    Captured: {formatDateTime(previewPhoto.capturedAt)}
                  </Typography>
                  {previewPhoto.scenarioTime && (
                    <Typography variant="body2" sx={{ color: 'grey.400' }}>
                      Scenario Time: {previewPhoto.scenarioTime}
                    </Typography>
                  )}
                  {previewPhoto.deletedAt && (
                    <Typography variant="body2" sx={{ color: 'error.main', fontWeight: 500 }}>
                      Deleted: {formatDateTime(previewPhoto.deletedAt)}
                    </Typography>
                  )}

                  {/* Action buttons */}
                  <Stack direction="row" spacing={2} sx={{ mt: 2 }}>
                    <CobraPrimaryButton
                      size="small"
                      startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
                      onClick={() => setRestorePhotoId(previewPhoto.id)}
                      sx={{ width: 'fit-content' }}
                    >
                      Restore Photo
                    </CobraPrimaryButton>
                    <CobraDeleteButton
                      size="small"
                      startIcon={<FontAwesomeIcon icon={faTrash} />}
                      onClick={() => setPermanentDeletePhotoId(previewPhoto.id)}
                      sx={{ width: 'fit-content' }}
                    >
                      Permanently Delete
                    </CobraDeleteButton>
                  </Stack>
                </Stack>
              </Box>
            </>
          )}
        </DialogContent>
      </Dialog>

      {/* Restore Confirmation */}
      <ConfirmDialog
        open={!!restorePhotoId}
        title="Restore Photo?"
        message="This photo will be restored to the gallery."
        severity="info"
        confirmLabel="Restore"
        cancelLabel="Cancel"
        onConfirm={handleRestore}
        onCancel={() => setRestorePhotoId(null)}
        isConfirming={isRestoring}
      />

      {/* Permanent Delete Confirmation */}
      <ConfirmDialog
        open={!!permanentDeletePhotoId}
        title="Permanently Delete Photo?"
        message="This photo and its image files will be permanently deleted. This action cannot be undone."
        severity="danger"
        confirmLabel="Permanently Delete"
        cancelLabel="Cancel"
        onConfirm={handlePermanentDelete}
        onCancel={() => setPermanentDeletePhotoId(null)}
        isConfirming={isPermanentDeleting}
      />
    </Box>
  )
}

export default PhotoTrashPage
