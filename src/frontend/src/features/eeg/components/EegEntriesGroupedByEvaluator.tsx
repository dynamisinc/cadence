/**
 * EegEntriesGroupedByEvaluator Component
 *
 * Groups EEG entries by evaluator.
 * Shows entry count and rating distribution per evaluator.
 * Director+ only view.
 */

import { useState, useMemo } from 'react'
import {
  Box,
  Typography,
  Stack,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  Paper,
  Skeleton,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChevronDown } from '@fortawesome/free-solid-svg-icons'
import { format, parseISO } from 'date-fns'

import {
  PerformanceRating,
  PERFORMANCE_RATING_SHORT_LABELS,
  PERFORMANCE_RATING_COLORS,
  type EegEntryDto,
} from '../types'
import { EntryDetailDialog, DeleteConfirmDialog } from './EegEntryDialogs'

interface EegEntriesGroupedByEvaluatorProps {
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

interface EvaluatorGroup {
  evaluatorId: string
  evaluatorName: string
  entries: EegEntryDto[]
}

/**
 * Rating distribution chip display
 */
const RatingDistribution = ({ entries }: { entries: EegEntryDto[] }) => {
  const distribution = useMemo(() => {
    const counts = {
      [PerformanceRating.Performed]: 0,
      [PerformanceRating.SomeChallenges]: 0,
      [PerformanceRating.MajorChallenges]: 0,
      [PerformanceRating.UnableToPerform]: 0,
    }
    entries.forEach(e => {
      counts[e.rating]++
    })
    return counts
  }, [entries])

  return (
    <Stack direction="row" spacing={0.5} alignItems="center">
      {Object.entries(distribution).map(([rating, count]) => (
        <Chip
          key={rating}
          label={`${PERFORMANCE_RATING_SHORT_LABELS[rating as PerformanceRating]}:${count}`}
          size="small"
          sx={{
            backgroundColor: `${PERFORMANCE_RATING_COLORS[rating as PerformanceRating]}20`,
            color: PERFORMANCE_RATING_COLORS[rating as PerformanceRating],
            fontWeight: 600,
            fontSize: '0.75rem',
            height: 24,
          }}
        />
      ))}
    </Stack>
  )
}

/**
 * Single rating chip
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
 * Individual entry card
 */
const EntryCard = ({
  entry,
  onClick,
}: {
  entry: EegEntryDto
  onClick: () => void
}) => {
  const parseAsUtc = (dateStr: string) =>
    parseISO(dateStr.endsWith('Z') ? dateStr : `${dateStr}Z`)
  const timeStr = format(parseAsUtc(entry.observedAt), 'h:mm a')

  return (
    <Paper
      sx={{
        p: 1.5,
        cursor: 'pointer',
        '&:hover': {
          backgroundColor: 'action.hover',
        },
        borderLeft: 3,
        borderColor: PERFORMANCE_RATING_COLORS[entry.rating],
      }}
      onClick={onClick}
      role="button"
      tabIndex={0}
      onKeyDown={e => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          onClick()
        }
      }}
    >
      <Stack direction="row" alignItems="flex-start" spacing={2}>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ minWidth: 50, fontFamily: 'monospace' }}
        >
          {timeStr}
        </Typography>
        <RatingChip rating={entry.rating} />
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography variant="body2" fontWeight={600} noWrap>
            {entry.criticalTask.taskDescription}
          </Typography>
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
        {entry.wasEdited && (
          <Chip label="edited" size="small" variant="outlined" sx={{ height: 18 }} />
        )}
      </Stack>
    </Paper>
  )
}

/**
 * EegEntriesGroupedByEvaluator Component
 */
export const EegEntriesGroupedByEvaluator = ({
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
}: EegEntriesGroupedByEvaluatorProps) => {
  const [selectedEntry, setSelectedEntry] = useState<EegEntryDto | null>(null)
  const [deleteEntry, setDeleteEntry] = useState<EegEntryDto | null>(null)

  // Group entries by evaluator
  const evaluatorGroups = useMemo<EvaluatorGroup[]>(() => {
    const groups = new Map<string, EvaluatorGroup>()

    entries.forEach(entry => {
      const evalId = entry.evaluatorId
      const evalName = entry.evaluatorName ?? 'Unknown Evaluator'

      if (!groups.has(evalId)) {
        groups.set(evalId, {
          evaluatorId: evalId,
          evaluatorName: evalName,
          entries: [],
        })
      }
      groups.get(evalId)!.entries.push(entry)
    })

    // Sort by evaluator name
    return Array.from(groups.values()).sort((a, b) =>
      a.evaluatorName.localeCompare(b.evaluatorName),
    )
  }, [entries])

  // Handle entry click
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

  // Check if user can edit/delete a specific entry
  const canEditEntry = (entry: EegEntryDto) => {
    if (!canEdit) return false
    return entry.evaluatorId === currentUserId || canDelete
  }

  const canDeleteEntry = (entry: EegEntryDto) => {
    if (!canDelete) return false
    return entry.evaluatorId === currentUserId || canDelete
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
    return <Alert severity="error">{error}</Alert>
  }

  // Empty state
  if (entries.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography variant="body1" color="text.secondary" gutterBottom>
          No EEG entries recorded yet
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Evaluator entries will appear here once observations are recorded.
        </Typography>
      </Box>
    )
  }

  return (
    <Box>
      {/* Evaluator Groups */}
      <Stack spacing={2}>
        {evaluatorGroups.map(evalGroup => (
          <Accordion key={evalGroup.evaluatorId} defaultExpanded={false}>
            <AccordionSummary
              expandIcon={<FontAwesomeIcon icon={faChevronDown} />}
              aria-label={`Expand ${evalGroup.evaluatorName} entries`}
            >
              <Stack spacing={1} sx={{ width: '100%', pr: 2 }}>
                <Stack
                  direction="row"
                  justifyContent="space-between"
                  alignItems="center"
                  spacing={2}
                >
                  <Typography variant="subtitle1" fontWeight={600}>
                    {evalGroup.evaluatorName}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {evalGroup.entries.length}{' '}
                    {evalGroup.entries.length === 1 ? 'entry' : 'entries'}
                  </Typography>
                </Stack>
                <RatingDistribution entries={evalGroup.entries} />
              </Stack>
            </AccordionSummary>
            <AccordionDetails>
              <Stack spacing={1}>
                {evalGroup.entries.map(entry => (
                  <EntryCard
                    key={entry.id}
                    entry={entry}
                    onClick={() => handleEntryClick(entry)}
                  />
                ))}
              </Stack>
            </AccordionDetails>
          </Accordion>
        ))}
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

export default EegEntriesGroupedByEvaluator
