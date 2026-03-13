/**
 * Shared dialog components for EEG entries
 *
 * EntryDetailDialog - Shows full entry details
 * DeleteConfirmDialog - Confirms entry deletion
 */

import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Stack,
  Box,
  Paper,
  Divider,
  IconButton,
  Chip,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faXmark, faLink } from '@fortawesome/free-solid-svg-icons'
import { formatDateTime } from '@/shared/utils/dateUtils'

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
export const EntryDetailDialog = ({
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
  const theme = useTheme()
  if (!entry) return null

  // Use backend-provided wasEdited flag
  const wasEdited = entry.wasEdited

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Typography variant="h6">EEG Entry Detail</Typography>
          <IconButton onClick={onClose} size="small" aria-label="Close dialog">
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
                  <FontAwesomeIcon icon={faLink} style={{ color: theme.palette.neutral[600] }} />
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
export const DeleteConfirmDialog = ({
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
