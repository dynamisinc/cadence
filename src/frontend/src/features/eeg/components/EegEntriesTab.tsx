/**
 * EegEntriesTab - Entries tab content for the EEG Entries page
 *
 * Displays the list of EEG entries with filtering, view-mode switching,
 * empty states, and a compact coverage summary banner.
 *
 * Owns its own filter state (searchQuery, ratingFilter) and view-mode
 * state (list / byCapability / byEvaluator) since these are exclusively
 * used by this tab and do not need to survive tab switches.
 *
 * @module features/eeg
 * @see EegEntriesPage
 */

import { useState, useMemo } from 'react'
import {
  Box,
  Typography,
  Paper,
  Stack,
  Alert,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  InputAdornment,
  ToggleButtonGroup,
  ToggleButton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlus,
  faSearch,
  faTimes,
  faClipboardCheck,
  faList,
  faLayerGroup,
  faUsers,
} from '@fortawesome/free-solid-svg-icons'

import { EegEntriesList } from './EegEntriesList'
import { EegCoverageDashboard } from './EegCoverageDashboard'
import { EegEntriesGroupedByCapability } from './EegEntriesGroupedByCapability'
import { EegEntriesGroupedByEvaluator } from './EegEntriesGroupedByEvaluator'
import {
  CobraPrimaryButton,
  CobraLinkButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import {
  PerformanceRating,
  PERFORMANCE_RATING_SHORT_LABELS,
  type EegEntryDto,
  type EegCoverageDto,
} from '../types'

type RatingFilterValue = 'all' | PerformanceRating
type ViewMode = 'list' | 'byCapability' | 'byEvaluator'

interface EegEntriesTabProps {
  /** The exercise ID scoping all data */
  exerciseId: string
  /** All (unfiltered) entries for this exercise */
  eegEntries: EegEntryDto[]
  /** Whether entries are currently loading */
  entriesLoading: boolean
  /** Error message if entries failed to load */
  entriesError: string | null
  /** Coverage data — used for the compact summary banner */
  coverage: EegCoverageDto | null
  /** Whether the current user can create/edit EEG entries */
  canCreate: boolean
  /** Whether the current user can edit EEG entries */
  canEdit: boolean
  /** Whether the current user can delete EEG entries */
  canDelete: boolean
  /** Current user ID (for evaluator-owned entry checks) */
  currentUserId?: string
  /** Entry currently being deleted (shows spinner) */
  deletingId: string | null
  /** Called when the user clicks "Add Entry" */
  onCreateClick: () => void
  /** Called when the user clicks edit on an entry */
  onEdit: (entry: EegEntryDto) => void
  /** Called when the user confirms deletion of an entry */
  onDelete: (entryId: string) => void
  /** Called when the user clicks the inject link on an entry */
  onInjectClick: (injectId: string) => void
  /** Called when the user clicks "View Details" in the compact coverage banner */
  onCoverageDetailsClick: () => void
}

/**
 * Entries tab panel rendered inside EegEntriesPage when the user is
 * on the "Entries" tab.
 */
export const EegEntriesTab = ({
  exerciseId,
  eegEntries,
  entriesLoading,
  entriesError,
  coverage,
  canCreate,
  canEdit,
  canDelete,
  currentUserId,
  deletingId,
  onCreateClick,
  onEdit,
  onDelete,
  onInjectClick,
  onCoverageDetailsClick,
}: EegEntriesTabProps) => {
  // Filter state — owned here since it is exclusive to this tab
  const [ratingFilter, setRatingFilter] = useState<RatingFilterValue>('all')
  const [searchQuery, setSearchQuery] = useState('')

  // View mode state — owned here since it is exclusive to this tab
  const [viewMode, setViewMode] = useState<ViewMode>('list')

  // Derived state
  const filteredEntries = useMemo(() => {
    let result = [...eegEntries]

    // Rating filter
    if (ratingFilter !== 'all') {
      result = result.filter(entry => entry.rating === ratingFilter)
    }

    // Search filter (searches observation text, task description, evaluator name)
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase()
      result = result.filter(
        entry =>
          entry.observationText.toLowerCase().includes(query) ||
          entry.criticalTask?.taskDescription?.toLowerCase().includes(query) ||
          entry.evaluatorName?.toLowerCase().includes(query),
      )
    }

    return result
  }, [eegEntries, ratingFilter, searchQuery])

  const hasActiveFilters = ratingFilter !== 'all' || !!searchQuery.trim()
  const hasNoTasks = coverage && coverage.totalTasks === 0
  const hasNoEntries = eegEntries.length === 0

  const clearFilters = () => {
    setRatingFilter('all')
    setSearchQuery('')
  }

  return (
    <Box role="tabpanel" aria-labelledby="entries-tab">
      {/* No tasks configured alert */}
      {hasNoTasks && (
        <Alert severity="info" sx={{ mb: 3 }}>
          <Typography variant="subtitle2" gutterBottom>
            No critical tasks configured
          </Typography>
          <Typography variant="body2">
            Add capability targets and critical tasks in the EEG Setup to enable evaluation
            tracking.
          </Typography>
        </Alert>
      )}

      {/* Compact coverage summary */}
      {!hasNoTasks && (
        <Box sx={{ mb: 3 }}>
          <EegCoverageDashboard
            exerciseId={exerciseId}
            compact
            onDetailsClick={onCoverageDetailsClick}
          />
        </Box>
      )}

      {/* View Mode Toggle */}
      <Paper sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" spacing={2} alignItems="center">
          <Typography variant="body2" fontWeight={600}>
            View:
          </Typography>
          <ToggleButtonGroup
            value={viewMode}
            exclusive
            onChange={(_, newValue) => {
              if (newValue) setViewMode(newValue)
            }}
            size="small"
            aria-label="View mode"
          >
            <ToggleButton value="list" aria-label="List view">
              <Stack direction="row" spacing={0.5} alignItems="center">
                <FontAwesomeIcon icon={faList} size="sm" />
                <span>List</span>
              </Stack>
            </ToggleButton>
            <ToggleButton value="byCapability" aria-label="Group by capability">
              <Stack direction="row" spacing={0.5} alignItems="center">
                <FontAwesomeIcon icon={faLayerGroup} size="sm" />
                <span>By Capability</span>
              </Stack>
            </ToggleButton>
            {canDelete && (
              <ToggleButton value="byEvaluator" aria-label="Group by evaluator">
                <Stack direction="row" spacing={0.5} alignItems="center">
                  <FontAwesomeIcon icon={faUsers} size="sm" />
                  <span>By Evaluator</span>
                </Stack>
              </ToggleButton>
            )}
          </ToggleButtonGroup>
        </Stack>
      </Paper>

      {/* Filters */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          alignItems={{ sm: 'center' }}
        >
          {/* Search */}
          <CobraTextField
            placeholder="Search entries..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            size="small"
            sx={{ minWidth: 250 }}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <FontAwesomeIcon icon={faSearch} />
                  </InputAdornment>
                ),
                endAdornment: searchQuery && (
                  <InputAdornment position="end">
                    <CobraLinkButton size="small" onClick={() => setSearchQuery('')}>
                      <FontAwesomeIcon icon={faTimes} />
                    </CobraLinkButton>
                  </InputAdornment>
                ),
              },
            }}
          />

          {/* Rating Filter */}
          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel id="rating-filter-label">Rating</InputLabel>
            <Select
              labelId="rating-filter-label"
              value={ratingFilter}
              onChange={e => setRatingFilter(e.target.value as RatingFilterValue)}
              label="Rating"
            >
              <MenuItem value="all">All Ratings</MenuItem>
              {Object.values(PerformanceRating).map(rating => (
                <MenuItem key={rating} value={rating}>
                  {PERFORMANCE_RATING_SHORT_LABELS[rating]} - {rating.replace(/([A-Z])/g, ' $1').trim()}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Clear Filters */}
          {hasActiveFilters && (
            <CobraLinkButton onClick={clearFilters}>
              Clear Filters
            </CobraLinkButton>
          )}

          {/* Results Count */}
          <Box sx={{ flex: 1 }} />
          <Typography
            variant="body2"
            color="text.secondary"
            role="status"
            aria-live="polite"
            aria-atomic="true"
          >
            {filteredEntries.length} of {eegEntries.length} entries
          </Typography>
        </Stack>

        {/* Active Filters Display */}
        {hasActiveFilters && (
          <Stack direction="row" spacing={1} sx={{ mt: 2 }} flexWrap="wrap">
            {ratingFilter !== 'all' && (
              <Chip
                label={`Rating: ${PERFORMANCE_RATING_SHORT_LABELS[ratingFilter]}`}
                onDelete={() => setRatingFilter('all')}
                size="small"
              />
            )}
            {searchQuery && (
              <Chip
                label={`Search: "${searchQuery}"`}
                onDelete={() => setSearchQuery('')}
                size="small"
              />
            )}
          </Stack>
        )}
      </Paper>

      {/* Empty state when no entries */}
      {hasNoEntries && !entriesLoading ? (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Box sx={{ mb: 2 }}>
            <FontAwesomeIcon
              icon={faClipboardCheck}
              size="3x"
              style={{ color: '#bdbdbd' }}
            />
          </Box>
          <Typography variant="h6" gutterBottom>
            No EEG entries yet
          </Typography>
          <Typography variant="body2" color="text.secondary" paragraph>
            {hasNoTasks
              ? 'Configure critical tasks in EEG Setup before adding entries.'
              : 'Record structured observations during exercise conduct using the EEG Entry form.'}
          </Typography>
          {canCreate && !hasNoTasks && (
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={onCreateClick}
            >
              Add First Entry
            </CobraPrimaryButton>
          )}
        </Paper>
      ) : (
        /* Entries Views - conditional based on viewMode */
        <Paper sx={{ p: 2 }}>
          {viewMode === 'list' && (
            <EegEntriesList
              entries={filteredEntries}
              loading={entriesLoading}
              error={entriesError}
              canEdit={canEdit}
              canDelete={canDelete}
              currentUserId={currentUserId}
              onEdit={onEdit}
              onDelete={onDelete}
              onInjectClick={onInjectClick}
              deletingId={deletingId}
            />
          )}
          {viewMode === 'byCapability' && (
            <EegEntriesGroupedByCapability
              entries={filteredEntries}
              loading={entriesLoading}
              error={entriesError}
              canEdit={canEdit}
              canDelete={canDelete}
              currentUserId={currentUserId}
              onEdit={onEdit}
              onDelete={onDelete}
              onInjectClick={onInjectClick}
              deletingId={deletingId}
            />
          )}
          {viewMode === 'byEvaluator' && canDelete && (
            <EegEntriesGroupedByEvaluator
              entries={filteredEntries}
              loading={entriesLoading}
              error={entriesError}
              canEdit={canEdit}
              canDelete={canDelete}
              currentUserId={currentUserId}
              onEdit={onEdit}
              onDelete={onDelete}
              onInjectClick={onInjectClick}
              deletingId={deletingId}
            />
          )}
        </Paper>
      )}
    </Box>
  )
}
