/**
 * EegEntriesGroupedByCapability Component
 *
 * Groups EEG entries by Capability Target, then by Critical Task.
 * Shows aggregate rating distribution at each level.
 * Follows the S07 wireframe structure.
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
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  IconButton,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronDown,
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

interface EegEntriesGroupedByCapabilityProps {
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

interface TaskGroup {
  taskId: string
  taskDescription: string
  standard: string | null
  entries: EegEntryDto[]
}

interface CapabilityGroup {
  capabilityTargetId: string
  capabilityName: string
  targetDescription: string
  sources: string | null
  tasks: TaskGroup[]
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
        <Typography variant="body2" color="text.secondary" sx={{ minWidth: 60 }}>
          {entry.evaluatorName?.split(' ').map(n => n[0]).join('.') ?? '—'}
        </Typography>
        <Box sx={{ flex: 1, minWidth: 0 }}>
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

  const formatDateTime = (dateStr: string) => {
    try {
      const utcDateStr = dateStr.endsWith('Z') ? dateStr : `${dateStr}Z`
      return format(parseISO(utcDateStr), 'MMM d, yyyy h:mm a')
    } catch {
      return dateStr
    }
  }

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
            {entry.wasEdited && (
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
 * EegEntriesGroupedByCapability Component
 */
export const EegEntriesGroupedByCapability = ({
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
}: EegEntriesGroupedByCapabilityProps) => {
  const [selectedEntry, setSelectedEntry] = useState<EegEntryDto | null>(null)
  const [deleteEntry, setDeleteEntry] = useState<EegEntryDto | null>(null)

  // Group entries by capability target, then by critical task
  const capabilityGroups = useMemo<CapabilityGroup[]>(() => {
    const capGroups = new Map<string, CapabilityGroup>()

    entries.forEach(entry => {
      const capId = entry.criticalTask.capabilityTargetId
      const capName = entry.criticalTask.capabilityName
      const targetDesc = entry.criticalTask.capabilityTargetDescription
      const sources = entry.criticalTask.capabilityTargetSources ?? null

      if (!capGroups.has(capId)) {
        capGroups.set(capId, {
          capabilityTargetId: capId,
          capabilityName: capName,
          targetDescription: targetDesc,
          sources,
          tasks: [],
          entries: [],
        })
      }

      const capGroup = capGroups.get(capId)!
      capGroup.entries.push(entry)

      // Find or create task group
      const taskId = entry.criticalTaskId
      let taskGroup = capGroup.tasks.find(t => t.taskId === taskId)
      if (!taskGroup) {
        taskGroup = {
          taskId,
          taskDescription: entry.criticalTask.taskDescription,
          standard: entry.criticalTask.standard ?? null,
          entries: [],
        }
        capGroup.tasks.push(taskGroup)
      }
      taskGroup.entries.push(entry)
    })

    // Sort by capability name
    return Array.from(capGroups.values()).sort((a, b) =>
      a.capabilityName.localeCompare(b.capabilityName),
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
          Entries will be grouped by capability target and critical task.
        </Typography>
      </Box>
    )
  }

  return (
    <Box>
      {/* Capability Target Groups */}
      <Stack spacing={2}>
        {capabilityGroups.map(capGroup => (
          <Accordion
            key={capGroup.capabilityTargetId}
            defaultExpanded={false}
            slotProps={{ transition: { unmountOnExit: true } }}
          >
            <AccordionSummary
              expandIcon={<FontAwesomeIcon icon={faChevronDown} />}
              aria-label={`Expand ${capGroup.capabilityName} entries`}
            >
              <Stack spacing={1} sx={{ width: '100%', pr: 2 }}>
                <Stack
                  direction="row"
                  justifyContent="space-between"
                  alignItems="center"
                  spacing={2}
                >
                  <Typography variant="subtitle1" fontWeight={600}>
                    {capGroup.capabilityName}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {capGroup.entries.length}{' '}
                    {capGroup.entries.length === 1 ? 'entry' : 'entries'}
                  </Typography>
                </Stack>
                <Typography variant="body2" color="text.secondary" noWrap>
                  "{capGroup.targetDescription}"
                </Typography>
                <RatingDistribution entries={capGroup.entries} />
              </Stack>
            </AccordionSummary>
            <AccordionDetails>
              {/* Critical Task Groups within Capability */}
              <Stack spacing={1}>
                {capGroup.tasks.map(taskGroup => (
                  <Accordion
                    key={taskGroup.taskId}
                    defaultExpanded={false}
                    sx={{ backgroundColor: 'grey.50' }}
                    slotProps={{ transition: { unmountOnExit: true } }}
                  >
                    <AccordionSummary
                      expandIcon={<FontAwesomeIcon icon={faChevronDown} size="sm" />}
                      aria-label={`Expand ${taskGroup.taskDescription} entries`}
                    >
                      <Stack spacing={0.5} sx={{ width: '100%', pr: 2 }}>
                        <Stack
                          direction="row"
                          justifyContent="space-between"
                          alignItems="center"
                          spacing={2}
                        >
                          <Typography variant="body2" fontWeight={600}>
                            {taskGroup.taskDescription}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {taskGroup.entries.length}{' '}
                            {taskGroup.entries.length === 1 ? 'entry' : 'entries'}
                          </Typography>
                        </Stack>
                      </Stack>
                    </AccordionSummary>
                    <AccordionDetails>
                      <Stack spacing={1}>
                        {taskGroup.entries.map(entry => (
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

export default EegEntriesGroupedByCapability
