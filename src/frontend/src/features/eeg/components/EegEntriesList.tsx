/**
 * EegEntriesList Component
 *
 * Displays EEG entries in a list view with filtering and sorting.
 * Used for reviewing entries during and after exercise conduct.
 */

import { useState, useMemo } from 'react'
import {
  Box,
  Typography,
  Stack,
  Chip,
  Paper,
  Divider,
  IconButton,
  Tooltip,
  Menu,
  MenuItem,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Skeleton,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronRight,
  faFilter,
  faSort,
  faXmark,
  faLink,
} from '@fortawesome/free-solid-svg-icons'
import { format, parseISO } from 'date-fns'

import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraDeleteButton,
} from '@/theme/styledComponents'
import {
  PerformanceRating,
  PERFORMANCE_RATING_SHORT_LABELS,
  PERFORMANCE_RATING_COLORS,
  PERFORMANCE_RATING_LABELS,
  PERFORMANCE_RATING_DESCRIPTIONS,
  type EegEntryDto,
} from '../types'

interface EegEntriesListProps {
  /** EEG entries to display */
  entries: EegEntryDto[]
  /** Whether data is loading */
  loading?: boolean
  /** Error message if any */
  error?: string | null
  /** Whether the user can edit entries */
  canEdit?: boolean
  /** Whether the user can delete entries */
  canDelete?: boolean
  /** Current user ID (to determine own entries) */
  currentUserId?: string
  /** Called when edit is clicked */
  onEdit?: (entry: EegEntryDto) => void
  /** Called when delete is clicked */
  onDelete?: (entryId: string) => void
  /** Called when inject link is clicked */
  onInjectClick?: (injectId: string) => void
  /** Whether delete is in progress */
  deletingId?: string | null
}

type SortField = 'recordedAt' | 'rating' | 'evaluator' | 'task'
type SortOrder = 'asc' | 'desc'

/**
 * Rating chip with appropriate color
 */
const RatingChip = ({ rating }: { rating: PerformanceRating }) => (
  <Chip
    label={PERFORMANCE_RATING_SHORT_LABELS[rating]}
    size="small"
    sx={{
      backgroundColor: `${PERFORMANCE_RATING_COLORS[rating]}20`,
      color: PERFORMANCE_RATING_COLORS[rating],
      fontWeight: 700,
      minWidth: 32,
      height: 28,
      fontSize: '0.875rem',
    }}
  />
)

/**
 * Entry detail dialog
 */
const EntryDetailDialog = ({
  entry,
  open,
  onClose,
  canEdit,
  canDelete,
  onEdit,
  onDelete,
  onInjectClick,
}: {
  entry: EegEntryDto | null
  open: boolean
  onClose: () => void
  canEdit?: boolean
  canDelete?: boolean
  onEdit?: () => void
  onDelete?: () => void
  onInjectClick?: (injectId: string) => void
}) => {
  if (!entry) return null

  // Helper function to format dates consistently using date-fns
  // Ensures UTC dates are properly converted to local time
  const formatDateTime = (dateStr: string) => {
    try {
      // Append 'Z' if not present to ensure parseISO treats the date as UTC
      const utcDateStr = dateStr.endsWith('Z') ? dateStr : `${dateStr}Z`
      return format(parseISO(utcDateStr), 'MMM d, yyyy h:mm a')
    } catch {
      return dateStr
    }
  }

  // Use backend-provided wasEdited flag
  const wasEdited = entry.wasEdited

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Typography variant="h6">EEG Entry Detail</Typography>
          <IconButton onClick={onClose} size="small">
            <FontAwesomeIcon icon={faXmark} />
          </IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2}>
          {/* Capability Target */}
          <Box>
            <Typography variant="caption" color="text.secondary">
              Capability Target
            </Typography>
            <Typography variant="body1" fontWeight={600}>
              {entry.criticalTask.capabilityName}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {entry.criticalTask.capabilityTargetDescription}
            </Typography>
            {entry.criticalTask.capabilityTargetSources && (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5, fontStyle: 'italic' }}>
                Sources: {entry.criticalTask.capabilityTargetSources}
              </Typography>
            )}
          </Box>

          {/* Critical Task */}
          <Box>
            <Typography variant="caption" color="text.secondary">
              Critical Task
            </Typography>
            <Typography variant="body1" fontWeight={600}>
              {entry.criticalTask.taskDescription}
            </Typography>
            {entry.criticalTask.standard && (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                Standard: {entry.criticalTask.standard}
              </Typography>
            )}
          </Box>

          <Divider />

          {/* Rating */}
          <Box>
            <Typography variant="caption" color="text.secondary" gutterBottom>
              Performance Rating
            </Typography>
            <Stack direction="row" alignItems="center" spacing={2} mt={0.5}>
              <RatingChip rating={entry.rating} />
              <Box>
                <Typography
                  variant="body2"
                  fontWeight={600}
                  sx={{ color: PERFORMANCE_RATING_COLORS[entry.rating] }}
                >
                  {PERFORMANCE_RATING_LABELS[entry.rating]}
                </Typography>
              </Box>
            </Stack>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1, fontStyle: 'italic' }}>
              {PERFORMANCE_RATING_DESCRIPTIONS[entry.rating]}
            </Typography>
          </Box>

          <Divider />

          {/* Observation */}
          <Box>
            <Typography variant="caption" color="text.secondary">
              Observation
            </Typography>
            <Paper
              variant="outlined"
              sx={{ p: 2, mt: 0.5, backgroundColor: 'grey.50' }}
            >
              <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                {entry.observationText}
              </Typography>
            </Paper>
          </Box>

          {/* Metadata */}
          <Stack spacing={1}>
            <Stack direction="row" justifyContent="space-between">
              <Typography variant="caption" color="text.secondary">
                Observed at
              </Typography>
              <Typography variant="body2">
                {formatDateTime(entry.observedAt)}
              </Typography>
            </Stack>
            <Stack direction="row" justifyContent="space-between">
              <Typography variant="caption" color="text.secondary">
                Recorded at
              </Typography>
              <Typography variant="body2">
                {formatDateTime(entry.recordedAt)}
              </Typography>
            </Stack>
            <Stack direction="row" justifyContent="space-between">
              <Typography variant="caption" color="text.secondary">
                Evaluator
              </Typography>
              <Typography variant="body2">{entry.evaluatorName ?? 'Unknown'}</Typography>
            </Stack>
            {wasEdited && (
              <Stack direction="row" justifyContent="space-between">
                <Typography variant="caption" color="text.secondary">
                  Edited
                </Typography>
                <Typography variant="body2" fontStyle="italic">
                  {entry.updatedBy ? `by ${entry.updatedBy.name} at ` : ''}
                  {formatDateTime(entry.updatedAt)}
                </Typography>
              </Stack>
            )}
          </Stack>

          {/* Triggering Inject */}
          {entry.triggeringInject && (
            <>
              <Divider />
              <Box>
                <Typography variant="caption" color="text.secondary">
                  Triggered by Inject
                </Typography>
                <Stack direction="row" alignItems="center" spacing={1} mt={0.5}>
                  <FontAwesomeIcon icon={faLink} style={{ color: '#666' }} />
                  <Typography
                    variant="body2"
                    sx={{
                      cursor: onInjectClick ? 'pointer' : 'default',
                      '&:hover': onInjectClick ? { textDecoration: 'underline' } : {},
                    }}
                    onClick={() => onInjectClick?.(entry.triggeringInject!.id)}
                  >
                    INJ-{entry.triggeringInject.injectNumber.toString().padStart(3, '0')}:{' '}
                    {entry.triggeringInject.title}
                  </Typography>
                </Stack>
              </Box>
            </>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        {canDelete && (
          <CobraDeleteButton onClick={onDelete} size="small">
            Delete
          </CobraDeleteButton>
        )}
        {canEdit && (
          <CobraPrimaryButton onClick={onEdit} size="small">
            Edit
          </CobraPrimaryButton>
        )}
        <CobraSecondaryButton onClick={onClose}>Close</CobraSecondaryButton>
      </DialogActions>
    </Dialog>
  )
}

/**
 * Delete confirmation dialog
 */
const DeleteConfirmDialog = ({
  entry,
  open,
  onConfirm,
  onCancel,
  isDeleting,
}: {
  entry: EegEntryDto | null
  open: boolean
  onConfirm: () => void
  onCancel: () => void
  isDeleting?: boolean
}) => {
  if (!entry) return null

  return (
    <Dialog open={open} onClose={onCancel} maxWidth="sm">
      <DialogTitle>Delete EEG Entry?</DialogTitle>
      <DialogContent>
        <Typography variant="body2" gutterBottom>
          Are you sure you want to delete this EEG entry?
        </Typography>
        <Paper variant="outlined" sx={{ p: 2, mt: 2, backgroundColor: 'grey.50' }}>
          <Stack spacing={0.5}>
            <Typography variant="body2" fontWeight={600}>
              {entry.criticalTask.taskDescription}
            </Typography>
            <Stack direction="row" spacing={1} alignItems="center">
              <RatingChip rating={entry.rating} />
              <Typography variant="caption" color="text.secondary">
                by {entry.evaluatorName ?? 'Unknown'}
              </Typography>
            </Stack>
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                display: '-webkit-box',
                WebkitLineClamp: 2,
                WebkitBoxOrient: 'vertical',
              }}
            >
              "{entry.observationText}"
            </Typography>
          </Stack>
        </Paper>
        <Typography variant="caption" color="error" sx={{ display: 'block', mt: 2 }}>
          This action cannot be undone.
        </Typography>
      </DialogContent>
      <DialogActions>
        <CobraSecondaryButton onClick={onCancel} disabled={isDeleting}>
          Cancel
        </CobraSecondaryButton>
        <CobraDeleteButton onClick={onConfirm} disabled={isDeleting}>
          {isDeleting ? 'Deleting...' : 'Delete Entry'}
        </CobraDeleteButton>
      </DialogActions>
    </Dialog>
  )
}

/**
 * EEG Entries List Component
 *
 * Features:
 * - List view with entry summary
 * - Sorting by time, rating, evaluator, task
 * - Filtering by rating
 * - Entry detail dialog
 * - Edit/delete actions with permissions
 */
export const EegEntriesList = ({
  entries,
  loading = false,
  error = null,
  canEdit = false,
  canDelete = false,
  currentUserId,
  onEdit,
  onDelete,
  onInjectClick,
  deletingId,
}: EegEntriesListProps) => {
  // State
  const [sortField, setSortField] = useState<SortField>('recordedAt')
  const [sortOrder, setSortOrder] = useState<SortOrder>('desc')
  const [filterRating, setFilterRating] = useState<PerformanceRating | null>(null)
  const [selectedEntry, setSelectedEntry] = useState<EegEntryDto | null>(null)
  const [deleteEntry, setDeleteEntry] = useState<EegEntryDto | null>(null)

  // Menus
  const [sortAnchor, setSortAnchor] = useState<null | HTMLElement>(null)
  const [filterAnchor, setFilterAnchor] = useState<null | HTMLElement>(null)

  // Sort and filter entries
  const sortedEntries = useMemo(() => {
    const filtered = filterRating
      ? entries.filter(e => e.rating === filterRating)
      : entries

    return [...filtered].sort((a, b) => {
      let comparison = 0
      switch (sortField) {
        case 'recordedAt':
          comparison = new Date(a.recordedAt).getTime() - new Date(b.recordedAt).getTime()
          break
        case 'rating': {
          const ratingOrder = {
            [PerformanceRating.Performed]: 1,
            [PerformanceRating.SomeChallenges]: 2,
            [PerformanceRating.MajorChallenges]: 3,
            [PerformanceRating.UnableToPerform]: 4,
          }
          comparison = ratingOrder[a.rating] - ratingOrder[b.rating]
          break
        }
        case 'evaluator':
          comparison = (a.evaluatorName ?? '').localeCompare(b.evaluatorName ?? '')
          break
        case 'task':
          comparison = a.criticalTask.taskDescription.localeCompare(
            b.criticalTask.taskDescription,
          )
          break
      }
      return sortOrder === 'asc' ? comparison : -comparison
    })
  }, [entries, sortField, sortOrder, filterRating])

  // Check if user can edit/delete a specific entry
  const canEditEntry = (entry: EegEntryDto) => {
    if (!canEdit) return false
    // Directors can edit any entry, evaluators can only edit their own
    return entry.evaluatorId === currentUserId || canDelete // canDelete implies Director+
  }

  const canDeleteEntry = (entry: EegEntryDto) => {
    if (!canDelete) return false
    return entry.evaluatorId === currentUserId || canDelete
  }

  // Handlers
  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortOrder(prev => (prev === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortField(field)
      setSortOrder('desc')
    }
    setSortAnchor(null)
  }

  const handleFilter = (rating: PerformanceRating | null) => {
    setFilterRating(rating)
    setFilterAnchor(null)
  }

  const handleEntryClick = (entry: EegEntryDto) => {
    setSelectedEntry(entry)
  }

  const handleEdit = () => {
    if (selectedEntry && onEdit) {
      onEdit(selectedEntry)
      setSelectedEntry(null)
    }
  }

  const handleDeleteClick = () => {
    if (selectedEntry) {
      setDeleteEntry(selectedEntry)
      setSelectedEntry(null)
    }
  }

  const handleDeleteConfirm = () => {
    if (deleteEntry && onDelete) {
      onDelete(deleteEntry.id)
      setDeleteEntry(null)
    }
  }

  // Loading state
  if (loading && entries.length === 0) {
    return (
      <Stack spacing={1}>
        {[1, 2, 3].map(i => (
          <Skeleton key={i} variant="rectangular" height={80} />
        ))}
      </Stack>
    )
  }

  // Error state
  if (error) {
    return (
      <Alert severity="error">{error}</Alert>
    )
  }

  // Empty state
  if (entries.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography variant="body1" color="text.secondary" gutterBottom>
          No EEG entries recorded yet
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Use the EEG Entry form to record structured observations during exercise conduct.
        </Typography>
      </Box>
    )
  }

  return (
    <Box>
      {/* Summary and Controls */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
        sx={{ mb: 2 }}
        flexWrap="wrap"
        gap={1}
      >
        <Typography variant="body2" color="text.secondary">
          {sortedEntries.length} {sortedEntries.length === 1 ? 'entry' : 'entries'}
          {filterRating && ` (filtered: ${PERFORMANCE_RATING_SHORT_LABELS[filterRating]})`}
        </Typography>
        <Stack direction="row" spacing={1}>
          <Tooltip title="Filter by rating">
            <IconButton
              size="small"
              onClick={e => setFilterAnchor(e.currentTarget)}
              color={filterRating ? 'primary' : 'default'}
            >
              <FontAwesomeIcon icon={faFilter} />
            </IconButton>
          </Tooltip>
          <Tooltip title="Sort entries">
            <IconButton size="small" onClick={e => setSortAnchor(e.currentTarget)}>
              <FontAwesomeIcon icon={faSort} />
            </IconButton>
          </Tooltip>
        </Stack>
      </Stack>

      {/* Filter Menu */}
      <Menu
        anchorEl={filterAnchor}
        open={Boolean(filterAnchor)}
        onClose={() => setFilterAnchor(null)}
      >
        <MenuItem onClick={() => handleFilter(null)} selected={filterRating === null}>
          All Ratings
        </MenuItem>
        <Divider />
        {Object.values(PerformanceRating).map(rating => (
          <MenuItem
            key={rating}
            onClick={() => handleFilter(rating)}
            selected={filterRating === rating}
          >
            <RatingChip rating={rating} />
            <Typography sx={{ ml: 1 }}>{rating}</Typography>
          </MenuItem>
        ))}
      </Menu>

      {/* Sort Menu */}
      <Menu
        anchorEl={sortAnchor}
        open={Boolean(sortAnchor)}
        onClose={() => setSortAnchor(null)}
      >
        <MenuItem onClick={() => handleSort('recordedAt')} selected={sortField === 'recordedAt'}>
          Time {sortField === 'recordedAt' && (sortOrder === 'desc' ? '(newest)' : '(oldest)')}
        </MenuItem>
        <MenuItem onClick={() => handleSort('rating')} selected={sortField === 'rating'}>
          Rating {sortField === 'rating' && (sortOrder === 'asc' ? '(P→U)' : '(U→P)')}
        </MenuItem>
        <MenuItem onClick={() => handleSort('evaluator')} selected={sortField === 'evaluator'}>
          Evaluator {sortField === 'evaluator' && (sortOrder === 'asc' ? '(A-Z)' : '(Z-A)')}
        </MenuItem>
        <MenuItem onClick={() => handleSort('task')} selected={sortField === 'task'}>
          Task {sortField === 'task' && (sortOrder === 'asc' ? '(A-Z)' : '(Z-A)')}
        </MenuItem>
      </Menu>

      {/* Entries List */}
      <Stack spacing={1}>
        {sortedEntries.map(entry => {
          // Helper to ensure dates are parsed as UTC
          const parseAsUtc = (dateStr: string) => parseISO(dateStr.endsWith('Z') ? dateStr : `${dateStr}Z`)
          // Format time using date-fns for consistent timezone handling
          const timeStr = format(parseAsUtc(entry.recordedAt), 'h:mm a')

          return (
            <Paper
              key={entry.id}
              sx={{
                p: 2,
                cursor: 'pointer',
                '&:hover': {
                  backgroundColor: 'action.hover',
                },
                borderLeft: 3,
                borderColor: PERFORMANCE_RATING_COLORS[entry.rating],
              }}
              onClick={() => handleEntryClick(entry)}
            >
              <Stack direction="row" alignItems="flex-start" spacing={2}>
                {/* Time */}
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{ minWidth: 50, fontFamily: 'monospace' }}
                >
                  {timeStr}
                </Typography>

                {/* Content */}
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Stack direction="row" alignItems="center" spacing={1} mb={0.5}>
                    <Typography variant="body2" fontWeight={600} noWrap>
                      {entry.criticalTask.taskDescription}
                    </Typography>
                    {entry.wasEdited && (
                      <Chip label="edited" size="small" variant="outlined" sx={{ height: 18 }} />
                    )}
                  </Stack>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      display: '-webkit-box',
                      WebkitLineClamp: 2,
                      WebkitBoxOrient: 'vertical',
                    }}
                  >
                    {entry.observationText}
                  </Typography>
                </Box>

                {/* Rating */}
                <RatingChip rating={entry.rating} />

                {/* Evaluator */}
                <Typography
                  variant="caption"
                  color="text.secondary"
                  sx={{ minWidth: 80, textAlign: 'right' }}
                >
                  {entry.evaluatorName?.split(' ').map(n => n[0]).join('.') ?? '—'}
                </Typography>

                {/* Arrow */}
                <IconButton size="small" sx={{ ml: 'auto' }}>
                  <FontAwesomeIcon icon={faChevronRight} />
                </IconButton>
              </Stack>
            </Paper>
          )
        })}
      </Stack>

      {/* Entry Detail Dialog */}
      <EntryDetailDialog
        entry={selectedEntry}
        open={!!selectedEntry}
        onClose={() => setSelectedEntry(null)}
        canEdit={selectedEntry ? canEditEntry(selectedEntry) : false}
        canDelete={selectedEntry ? canDeleteEntry(selectedEntry) : false}
        onEdit={handleEdit}
        onDelete={handleDeleteClick}
        onInjectClick={onInjectClick}
      />

      {/* Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        entry={deleteEntry}
        open={!!deleteEntry}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteEntry(null)}
        isDeleting={deletingId === deleteEntry?.id}
      />
    </Box>
  )
}

export default EegEntriesList
