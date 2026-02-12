/**
 * ObservationList Component
 *
 * Displays a list of observations recorded during exercise conduct.
 * Shows rating badges, content, and optional recommendations.
 * Supports filtering by rating (P/S/M/U/Unrated).
 */

import { useState, useMemo } from 'react'
import {
  Box,
  Paper,
  List,
  ListItem,
  Typography,
  Stack,
  CircularProgress,
  IconButton,
  Tooltip,
  Divider,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
} from '@mui/material'
import { CobraLinkButton } from '@/theme/styledComponents'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPen, faTrash, faSpinner, faCamera, faXmark, faChevronLeft, faChevronRight } from '@fortawesome/free-solid-svg-icons'
import { format, parseISO } from 'date-fns'

import { RatingBadge } from './RatingBadge'
import { formatDateTime } from '../../../shared/utils/dateUtils'
import { ObservationRating, ObservationRatingLabels } from '../../../types'
import type { ObservationDto } from '../types'
import type { PhotoTagDto } from '../types'

interface ObservationListProps {
  observations: ObservationDto[]
  loading?: boolean
  error?: string | null
  /** Can the user edit/delete observations? */
  canEdit?: boolean
  /** Called when user edits an observation */
  onEdit?: (observation: ObservationDto) => void
  /** Called when user deletes an observation */
  onDelete?: (observationId: string) => Promise<void>
  /** ID of observation currently being deleted */
  deletingId?: string | null
  /** Called when user clicks on an inject reference */
  onInjectClick?: (injectId: string) => void
  /** Whether to show the internal filter bar (default: true) */
  showFilterBar?: boolean
}

type RatingFilterValue = 'all' | ObservationRating | 'unrated'

export const ObservationList = ({
  observations,
  loading = false,
  error = null,
  canEdit = false,
  onEdit,
  onDelete,
  deletingId = null,
  onInjectClick,
  showFilterBar = true,
}: ObservationListProps) => {
  // Filter state
  const [ratingFilter, setRatingFilter] = useState<RatingFilterValue>('all')

  // Photo gallery state
  const [galleryPhotos, setGalleryPhotos] = useState<PhotoTagDto[] | null>(null)
  const [galleryIndex, setGalleryIndex] = useState(0)

  const formatTime = (dateStr: string) => {
    try {
      return format(parseISO(dateStr), 'h:mm a')
    } catch {
      return ''
    }
  }

  // Apply filters using useMemo for performance (only when filter bar is shown)
  const filteredObservations = useMemo(() => {
    // If filter bar is hidden, don't apply internal filtering
    if (!showFilterBar) return observations

    let filtered = [...observations]

    // Filter by rating
    if (ratingFilter !== 'all') {
      if (ratingFilter === 'unrated') {
        filtered = filtered.filter(obs => obs.rating === null)
      } else {
        filtered = filtered.filter(obs => obs.rating === ratingFilter)
      }
    }

    return filtered
  }, [observations, ratingFilter, showFilterBar])

  // Check if any filters are active
  const hasActiveFilters = showFilterBar && ratingFilter !== 'all'

  // Clear all filters
  const handleClearFilters = () => {
    setRatingFilter('all')
  }

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    )
  }

  if (error) {
    return (
      <Paper sx={{ p: 2 }}>
        <Typography color="error">{error}</Typography>
      </Paper>
    )
  }

  if (observations.length === 0) {
    return (
      <Paper sx={{ p: 3, textAlign: 'center' }}>
        <Typography color="text.secondary">
          No observations recorded yet.
        </Typography>
      </Paper>
    )
  }

  return (
    <Box>
      {/* Filter Bar (optional) */}
      {showFilterBar && (
        <Paper sx={{ p: 2, mb: 2 }}>
          <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
            <FormControl size="small" sx={{ minWidth: 200 }}>
              <InputLabel id="rating-filter-label">Filter by Rating</InputLabel>
              <Select
                labelId="rating-filter-label"
                id="rating-filter"
                value={ratingFilter}
                label="Filter by Rating"
                onChange={e => setRatingFilter(e.target.value as RatingFilterValue)}
              >
                <MenuItem value="all">All Ratings</MenuItem>
                <Divider />
                <MenuItem value={ObservationRating.Performed}>
                  {ObservationRatingLabels[ObservationRating.Performed]}
                </MenuItem>
                <MenuItem value={ObservationRating.Satisfactory}>
                  {ObservationRatingLabels[ObservationRating.Satisfactory]}
                </MenuItem>
                <MenuItem value={ObservationRating.Marginal}>
                  {ObservationRatingLabels[ObservationRating.Marginal]}
                </MenuItem>
                <MenuItem value={ObservationRating.Unsatisfactory}>
                  {ObservationRatingLabels[ObservationRating.Unsatisfactory]}
                </MenuItem>
                <Divider />
                <MenuItem value="unrated">Unrated</MenuItem>
              </Select>
            </FormControl>

            {hasActiveFilters && (
              <CobraLinkButton onClick={handleClearFilters} size="small">
                Clear Filters
              </CobraLinkButton>
            )}
          </Stack>

          {/* Filter count */}
          {hasActiveFilters && (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              Showing {filteredObservations.length} of {observations.length} observations
            </Typography>
          )}
        </Paper>
      )}

      {/* Empty state for filtered results */}
      {filteredObservations.length === 0 ? (
        <Paper sx={{ p: 3, textAlign: 'center' }}>
          <Typography color="text.secondary">
            No observations match your filters.
          </Typography>
        </Paper>
      ) : (
        <Paper>
          <List disablePadding>
            {filteredObservations.map((observation, index) => {
              const isDeleting = deletingId === observation.id

              return (
                <Box key={observation.id}>
                  {index > 0 && <Divider />}
                  <ListItem
                    sx={{
                      py: 2,
                      px: 2,
                      alignItems: 'flex-start',
                    }}
                    secondaryAction={
                      canEdit ? (
                        <Stack direction="row" spacing={0.5}>
                          <Tooltip title="Edit">
                            <IconButton
                              size="small"
                              onClick={() => onEdit?.(observation)}
                              disabled={isDeleting}
                            >
                              <FontAwesomeIcon icon={faPen} size="sm" />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="Delete">
                            <IconButton
                              size="small"
                              onClick={() => onDelete?.(observation.id)}
                              disabled={isDeleting}
                              color="error"
                            >
                              {isDeleting ? (
                                <FontAwesomeIcon icon={faSpinner} spin size="sm" />
                              ) : (
                                <FontAwesomeIcon icon={faTrash} size="sm" />
                              )}
                            </IconButton>
                          </Tooltip>
                        </Stack>
                      ) : undefined
                    }
                  >
                    <Stack spacing={1} sx={{ flex: 1, pr: canEdit ? 8 : 0 }}>
                      {/* Header with rating and timestamp */}
                      <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
                        <RatingBadge rating={observation.rating} />
                        <Typography variant="caption" color="text.secondary">
                          {formatTime(observation.createdAt)}
                        </Typography>
                        {/* Inject Reference or General observation label */}
                        {observation.injectId && observation.injectTitle ? (
                          onInjectClick ? (
                            <Typography
                              component="button"
                              variant="caption"
                              onClick={() => onInjectClick(observation.injectId!)}
                              sx={{
                                color: 'primary.main',
                                cursor: 'pointer',
                                textDecoration: 'underline',
                                border: 'none',
                                background: 'none',
                                padding: 0,
                                fontFamily: 'inherit',
                                '&:hover': {
                                  color: 'primary.dark',
                                },
                              }}
                              aria-label={`View inject: ${observation.injectTitle}`}
                            >
                              Re: #{observation.injectNumber} {observation.injectTitle}
                            </Typography>
                          ) : (
                            <Typography variant="caption" color="text.secondary">
                              Re: #{observation.injectNumber} {observation.injectTitle}
                            </Typography>
                          )
                        ) : (
                          <Typography variant="caption" color="text.secondary" fontStyle="italic">
                            General observation
                          </Typography>
                        )}
                        {/* Photo indicator */}
                        {observation.photos?.length > 0 && (
                          <Tooltip title={`${observation.photos.length} photo${observation.photos.length === 1 ? '' : 's'} - click to view`}>
                            <Stack
                              direction="row"
                              spacing={0.5}
                              alignItems="center"
                              onClick={() => {
                                setGalleryPhotos(observation.photos)
                                setGalleryIndex(0)
                              }}
                              sx={{ cursor: 'pointer', '&:hover': { opacity: 0.7 } }}
                              role="button"
                              aria-label={`View ${observation.photos.length} photo${observation.photos.length === 1 ? '' : 's'}`}
                            >
                              <FontAwesomeIcon icon={faCamera} size="xs" color="#757575" />
                              <Typography variant="caption" color="text.secondary">
                                {observation.photos.length}
                              </Typography>
                            </Stack>
                          </Tooltip>
                        )}
                      </Stack>

                      {/* Observation content */}
                      <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                        {observation.content}
                      </Typography>

                      {/* Capability Tags (S05) */}
                      {observation.capabilities && observation.capabilities.length > 0 && (
                        <Stack direction="row" flexWrap="wrap" gap={0.5} sx={{ mt: 1 }}>
                          {observation.capabilities.map(cap => (
                            <Chip
                              key={cap.id}
                              label={cap.name}
                              size="small"
                              variant="outlined"
                              color="primary"
                            />
                          ))}
                        </Stack>
                      )}

                      {/* Recommendation if present */}
                      {observation.recommendation && (
                        <Box
                          sx={{
                            mt: 1,
                            p: 1.5,
                            bgcolor: 'grey.50',
                            borderRadius: 1,
                            borderLeft: '3px solid',
                            borderLeftColor: 'primary.main',
                          }}
                        >
                          <Typography variant="caption" color="text.secondary" display="block">
                            Recommendation
                          </Typography>
                          <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                            {observation.recommendation}
                          </Typography>
                        </Box>
                      )}
                    </Stack>
                  </ListItem>
                </Box>
              )
            })}
          </List>
        </Paper>
      )}

      {/* Photo Gallery Dialog */}
      <Dialog
        open={!!galleryPhotos}
        onClose={() => setGalleryPhotos(null)}
        maxWidth="md"
        fullWidth
      >
        {galleryPhotos && galleryPhotos.length > 0 && (
          <>
            <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="h6">
                Photo {galleryIndex + 1} of {galleryPhotos.length}
              </Typography>
              <IconButton onClick={() => setGalleryPhotos(null)} size="small">
                <FontAwesomeIcon icon={faXmark} />
              </IconButton>
            </DialogTitle>
            <DialogContent sx={{ p: 0, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
              {/* Main image */}
              <Box
                component="img"
                src={galleryPhotos[galleryIndex]?.thumbnailUri}
                alt={`Photo ${galleryIndex + 1}`}
                sx={{
                  maxWidth: '100%',
                  maxHeight: '60vh',
                  objectFit: 'contain',
                  display: 'block',
                }}
              />

              {/* Caption */}
              <Typography variant="caption" color="text.secondary" sx={{ py: 1 }}>
                {formatDateTime(galleryPhotos[galleryIndex]?.capturedAt)}
              </Typography>

              {/* Navigation */}
              {galleryPhotos.length > 1 && (
                <Stack direction="row" spacing={2} alignItems="center" sx={{ pb: 2 }}>
                  <IconButton
                    onClick={() => setGalleryIndex(
                      i => (i - 1 + galleryPhotos.length) % galleryPhotos.length,
                    )}
                    size="small"
                  >
                    <FontAwesomeIcon icon={faChevronLeft} />
                  </IconButton>

                  {/* Thumbnail strip */}
                  <Stack direction="row" spacing={1}>
                    {galleryPhotos
                      .sort((a, b) => a.displayOrder - b.displayOrder)
                      .map((photo, idx) => (
                        <Box
                          key={photo.id}
                          component="img"
                          src={photo.thumbnailUri}
                          alt={`Thumbnail ${idx + 1}`}
                          onClick={() => setGalleryIndex(idx)}
                          sx={{
                            width: 48,
                            height: 48,
                            objectFit: 'cover',
                            borderRadius: 1,
                            cursor: 'pointer',
                            border: 2,
                            borderColor: idx === galleryIndex ? 'primary.main' : 'transparent',
                            opacity: idx === galleryIndex ? 1 : 0.6,
                            '&:hover': { opacity: 1 },
                          }}
                        />
                      ))}
                  </Stack>

                  <IconButton
                    onClick={() => setGalleryIndex(i => (i + 1) % galleryPhotos.length)}
                    size="small"
                  >
                    <FontAwesomeIcon icon={faChevronRight} />
                  </IconButton>
                </Stack>
              )}
            </DialogContent>
          </>
        )}
      </Dialog>
    </Box>
  )
}

export default ObservationList
