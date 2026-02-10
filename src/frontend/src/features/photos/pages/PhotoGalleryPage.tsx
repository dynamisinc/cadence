/**
 * PhotoGalleryPage
 *
 * Responsive grid gallery of exercise photos with filters.
 * Displays all photos captured during an exercise with filtering by
 * link status (linked/unlinked to observations) and time range.
 *
 * Grid layout:
 * - Mobile: 2 columns
 * - Tablet: 3 columns
 * - Desktop: 4 columns
 *
 * Features:
 * - Click photo to open preview dialog
 * - Filter by linked/unlinked/all
 * - Filter by time range
 * - Real-time updates via SignalR
 * - Pagination
 *
 * @module features/photos/pages
 */

import { useState, useMemo, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Paper,
  Stack,
  CircularProgress,
  Alert,
  Chip,
  Skeleton,
  useTheme,
  useMediaQuery,
  Dialog,
  DialogContent,
  IconButton,
  Pagination,
  Tooltip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faHome,
  faImages,
  faLink,
  faCameraRetro,
  faChevronLeft,
  faChevronRight,
  faTimes,
  faTrash,
  faEye,
} from '@fortawesome/free-solid-svg-icons'
import { useExercise } from '../../exercises/hooks'
import { usePhotos } from '../hooks/usePhotos'
import { CobraTextField, CobraLinkButton, CobraDeleteButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useBreadcrumbs } from '../../../core/contexts'
import { formatDateTime } from '../../../shared/utils/dateUtils'
import { useObservations } from '../../observations/hooks/useObservations'
import { ObservationRatingShortLabels } from '../../../types'
import type { ObservationDto } from '../../observations/types'
import type { PhotoListQuery } from '../types'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'

// Filter chip options
type LinkedFilterValue = 'all' | 'linked' | 'unlinked'

export const PhotoGalleryPage = () => {
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

  // Filter state
  const [linkedFilter, setLinkedFilter] = useState<LinkedFilterValue>('all')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 20

  // Build query from filters
  const query: PhotoListQuery = useMemo(
    () => ({
      linkedOnly:
        linkedFilter === 'linked' ? true : linkedFilter === 'unlinked' ? false : undefined,
      from: fromDate || undefined,
      to: toDate || undefined,
      page,
      pageSize,
    }),
    [linkedFilter, fromDate, toDate, page],
  )

  // Fetch photos with filters
  const { photos, totalCount, isLoading, error, deletePhoto, isDeleting } = usePhotos(exerciseId!, query)

  // Fetch observations for linked photo details
  const { observations } = useObservations(exerciseId!)

  // Build lookup map: observationId -> ObservationDto
  const observationMap = useMemo(() => {
    const map = new Map<string, ObservationDto>()
    for (const obs of observations) {
      map.set(obs.id, obs)
    }
    return map
  }, [observations])

  // Preview state
  const [previewPhotoId, setPreviewPhotoId] = useState<string | null>(null)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)

  // Calculate total pages
  const totalPages = Math.ceil(totalCount / pageSize)

  // Set breadcrumbs
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'Photo Gallery' },
      ]
      : undefined,
  )

  // TODO: Add SignalR real-time updates when backend implements PhotoAdded/PhotoDeleted events
  // For now, photos will update on page refresh or when navigating away and back

  // Get preview photo and navigation
  const previewPhoto = useMemo(
    () => photos.find(p => p.id === previewPhotoId) ?? null,
    [photos, previewPhotoId],
  )

  const currentIndex = useMemo(
    () => (previewPhotoId ? photos.findIndex(p => p.id === previewPhotoId) : -1),
    [photos, previewPhotoId],
  )

  const handlePreviousPhoto = () => {
    if (currentIndex > 0) {
      setPreviewPhotoId(photos[currentIndex - 1].id)
    }
  }

  const handleNextPhoto = () => {
    if (currentIndex < photos.length - 1) {
      setPreviewPhotoId(photos[currentIndex + 1].id)
    }
  }

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
  }, [previewPhotoId, currentIndex, photos])

  // Handle page change
  const handlePageChange = (_event: React.ChangeEvent<unknown>, value: number) => {
    setPage(value)
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
      {/* Header */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
        sx={{ mb: 3 }}
      >
        <Stack direction="row" spacing={1} alignItems="center">
          <FontAwesomeIcon icon={faImages} />
          <Typography variant="h4">Photo Gallery</Typography>
          <Typography variant="body2" color="text.secondary">
            ({totalCount} {totalCount === 1 ? 'photo' : 'photos'})
          </Typography>
        </Stack>
        <CobraLinkButton onClick={() => navigate(`/exercises/${exerciseId}`)}>
          Back to Exercise
        </CobraLinkButton>
      </Stack>

      {/* Filter Bar */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Stack direction="row" spacing={2} flexWrap="wrap" alignItems="center">
          {/* Link Status Filter */}
          <Stack direction="row" spacing={1}>
            <Chip
              label="All"
              color={linkedFilter === 'all' ? 'primary' : 'default'}
              onClick={() => setLinkedFilter('all')}
              clickable
            />
            <Chip
              label="Linked"
              icon={<FontAwesomeIcon icon={faLink} />}
              color={linkedFilter === 'linked' ? 'primary' : 'default'}
              onClick={() => setLinkedFilter('linked')}
              clickable
            />
            <Chip
              label="Unlinked"
              color={linkedFilter === 'unlinked' ? 'primary' : 'default'}
              onClick={() => setLinkedFilter('unlinked')}
              clickable
            />
          </Stack>

          {/* Time Range Filter */}
          <CobraTextField
            type="datetime-local"
            label="From"
            value={fromDate}
            onChange={e => setFromDate(e.target.value)}
            size="small"
            sx={{ width: 220 }}
          />
          <CobraTextField
            type="datetime-local"
            label="To"
            value={toDate}
            onChange={e => setToDate(e.target.value)}
            size="small"
            sx={{ width: 220 }}
          />
        </Stack>
      </Paper>

      {/* Photo Grid */}
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
            {Array.from({ length: pageSize }).map((_, i) => (
              <Box key={i} sx={{ aspectRatio: '1' }}>
                <Skeleton variant="rectangular" width="100%" height="100%" />
              </Box>
            ))}
          </Box>
        ) : photos.length === 0 ? (
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
            <FontAwesomeIcon icon={faCameraRetro} size="3x" color={theme.palette.text.secondary} />
            <Typography variant="h6" color="text.secondary">
              No photos captured yet
            </Typography>
          </Box>
        ) : (
          <>
            {/* Photo Grid */}
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: `repeat(${gridColumns}, 1fr)`,
                gap: 2,
                mb: 3,
              }}
            >
              {photos.map(photo => (
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

                  {/* Overlay gradient with metadata */}
                  <Box
                    sx={{
                      position: 'absolute',
                      bottom: 0,
                      left: 0,
                      right: 0,
                      background:
                        'linear-gradient(to top, rgba(0,0,0,0.7) 0%, rgba(0,0,0,0) 100%)',
                      p: 1,
                    }}
                  >
                    <Stack spacing={0.5}>
                      <Typography variant="caption" sx={{ color: 'white', fontWeight: 500 }}>
                        {photo.capturedByName || 'Unknown'}
                      </Typography>
                      <Typography variant="caption" sx={{ color: 'white', fontSize: '0.7rem' }}>
                        {formatDateTime(photo.capturedAt)}
                      </Typography>
                    </Stack>

                    {/* Linked indicator with observation tooltip */}
                    {photo.observationId && (() => {
                      const obs = observationMap.get(photo.observationId!)
                      const tooltipContent = obs
                        ? `${obs.rating ? ObservationRatingShortLabels[obs.rating] + ' — ' : ''}${obs.content.length > 80 ? obs.content.slice(0, 80) + '...' : obs.content}`
                        : 'Linked to observation'
                      return (
                        <Tooltip title={tooltipContent} arrow placement="top">
                          <Box
                            sx={{
                              position: 'absolute',
                              top: -24,
                              right: 8,
                              bgcolor: 'primary.main',
                              color: 'white',
                              borderRadius: '50%',
                              width: 20,
                              height: 20,
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                            }}
                          >
                            <FontAwesomeIcon icon={faLink} size="xs" />
                          </Box>
                        </Tooltip>
                      )
                    })()}
                  </Box>
                </Box>
              ))}
            </Box>

            {/* Pagination */}
            {totalPages > 1 && (
              <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
                <Pagination
                  count={totalPages}
                  page={page}
                  onChange={handlePageChange}
                  color="primary"
                  showFirstButton
                  showLastButton
                />
              </Box>
            )}
          </>
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
              <IconButton
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
              </IconButton>

              {/* Previous button */}
              {currentIndex > 0 && (
                <IconButton
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
                </IconButton>
              )}

              {/* Next button */}
              {currentIndex < photos.length - 1 && (
                <IconButton
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
                </IconButton>
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

              {/* Photo metadata */}
              <Box sx={{ p: 2, bgcolor: 'rgba(0, 0, 0, 0.8)' }}>
                <Stack spacing={1}>
                  <Typography variant="body1" sx={{ color: 'white' }}>
                    Captured by: {previewPhoto.capturedByName || 'Unknown'}
                  </Typography>
                  <Typography variant="body2" sx={{ color: 'grey.400' }}>
                    {formatDateTime(previewPhoto.capturedAt)}
                  </Typography>
                  {previewPhoto.scenarioTime && (
                    <Typography variant="body2" sx={{ color: 'grey.400' }}>
                      Scenario Time: {previewPhoto.scenarioTime}
                    </Typography>
                  )}
                  {previewPhoto.observationId && (() => {
                    const obs = observationMap.get(previewPhoto.observationId!)
                    return (
                      <Box
                        sx={{
                          bgcolor: 'rgba(255, 255, 255, 0.08)',
                          borderRadius: 1,
                          p: 1.5,
                          border: 1,
                          borderColor: 'rgba(255, 255, 255, 0.15)',
                        }}
                      >
                        <Stack spacing={0.75}>
                          <Stack direction="row" spacing={1} alignItems="center">
                            <FontAwesomeIcon icon={faEye} style={{ color: 'white', fontSize: '0.85rem' }} />
                            <Typography variant="body2" sx={{ color: 'white', fontWeight: 600 }}>
                              Linked Observation
                            </Typography>
                            {obs?.rating && (
                              <Chip
                                label={ObservationRatingShortLabels[obs.rating]}
                                size="small"
                                color="primary"
                                sx={{ height: 20, fontSize: '0.7rem' }}
                              />
                            )}
                          </Stack>
                          {obs ? (
                            <>
                              <Typography variant="body2" sx={{ color: 'grey.300', lineHeight: 1.4 }}>
                                {obs.content.length > 150 ? obs.content.slice(0, 150) + '...' : obs.content}
                              </Typography>
                              {obs.createdByName && (
                                <Typography variant="caption" sx={{ color: 'grey.500' }}>
                                  By {obs.createdByName}
                                </Typography>
                              )}
                              <CobraLinkButton
                                size="small"
                                onClick={() => navigate(`/exercises/${exerciseId}/observations`)}
                                sx={{ width: 'fit-content', color: 'primary.light', p: 0, minWidth: 'auto' }}
                              >
                                View Observations
                              </CobraLinkButton>
                            </>
                          ) : (
                            <CobraLinkButton
                              size="small"
                              onClick={() => navigate(`/exercises/${exerciseId}/observations`)}
                              sx={{ width: 'fit-content', color: 'primary.light', p: 0, minWidth: 'auto' }}
                            >
                              View Observations
                            </CobraLinkButton>
                          )}
                        </Stack>
                      </Box>
                    )
                  })()}
                  {previewPhoto.latitude && previewPhoto.longitude && (
                    <Typography variant="caption" sx={{ color: 'grey.500' }}>
                      Location: {previewPhoto.latitude.toFixed(6)}, {previewPhoto.longitude.toFixed(6)}
                      {previewPhoto.locationAccuracy && ` (±${Math.round(previewPhoto.locationAccuracy)}m)`}
                    </Typography>
                  )}
                  <CobraDeleteButton
                    size="small"
                    startIcon={<FontAwesomeIcon icon={faTrash} />}
                    onClick={() => setShowDeleteConfirm(true)}
                    sx={{ mt: 1, width: 'fit-content' }}
                  >
                    Delete Photo
                  </CobraDeleteButton>
                </Stack>
              </Box>
            </>
          )}
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        open={showDeleteConfirm}
        title="Delete Photo?"
        message="This photo will be moved to trash. An administrator can restore it later."
        severity="danger"
        confirmLabel="Delete"
        cancelLabel="Cancel"
        onConfirm={async () => {
          if (previewPhotoId) {
            await deletePhoto(previewPhotoId)
            setShowDeleteConfirm(false)
            handleClosePreview()
          }
        }}
        onCancel={() => setShowDeleteConfirm(false)}
        isConfirming={isDeleting}
      />
    </Box>
  )
}

export default PhotoGalleryPage
