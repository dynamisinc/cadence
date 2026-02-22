/**
 * EegEntriesPage
 *
 * Standalone page for viewing and managing all EEG entries in an exercise.
 * Features:
 * - Tab navigation between Entries and Coverage views
 * - Filtering and sorting capabilities
 * - Full CRUD capabilities for evaluator observations
 * Accessible via in-exercise navigation at /exercises/:id/eeg-entries.
 */

import { useState, useMemo, useCallback, useEffect } from 'react'
import { useParams, useNavigate, useSearchParams } from 'react-router-dom'
import {
  Box,
  Typography,
  Paper,
  Stack,
  CircularProgress,
  Alert,
  Dialog,
  DialogContent,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  InputAdornment,
  Tabs,
  Tab,
  ToggleButtonGroup,
  ToggleButton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faHome,
  faPlus,
  faSearch,
  faTimes,
  faClipboardCheck,
  faFileExport,
  faFileWord,
  faChartBar,
  faList,
  faLayerGroup,
  faUsers,
} from '@fortawesome/free-solid-svg-icons'
import { useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'

import { useExercise } from '../../exercises/hooks'
import { useExerciseRole } from '../../auth'
import { useAuth } from '../../../contexts/AuthContext'
import { useEegEntries, eegEntryKeys, useEegCoverage } from '../hooks/useEegEntries'
import { useInjects } from '../../injects/hooks'
import { EegEntriesList } from '../components/EegEntriesList'
import { EegEntryForm } from '../components/EegEntryForm'
import { EegCoverageDashboard } from '../components/EegCoverageDashboard'
import { EegExportDialog } from '../components/EegExportDialog'
import { EegDocumentDialog } from '../components/EegDocumentDialog'
import { EegEntriesGroupedByCapability } from '../components/EegEntriesGroupedByCapability'
import { EegEntriesGroupedByEvaluator } from '../components/EegEntriesGroupedByEvaluator'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraLinkButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import { useBreadcrumbs } from '../../../core/contexts'
import {
  PerformanceRating,
  PERFORMANCE_RATING_SHORT_LABELS,
  type EegEntryDto,
} from '../types'
import { PageHeader } from '@/shared/components'

type RatingFilterValue = 'all' | PerformanceRating
type TabValue = 'entries' | 'coverage'
type ViewMode = 'list' | 'byCapability' | 'byEvaluator'

export const EegEntriesPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()

  // Core data hooks
  const { user } = useAuth()
  const { exercise, loading: exerciseLoading, error: exerciseError } = useExercise(exerciseId)
  const { can } = useExerciseRole(exerciseId ?? null)
  const {
    eegEntries,
    loading: entriesLoading,
    error: entriesError,
  } = useEegEntries(exerciseId!)
  const { injects } = useInjects(exerciseId!)
  const { coverage } = useEegCoverage(exerciseId!)

  // Tab state - sync with URL
  const [activeTab, setActiveTab] = useState<TabValue>(() => {
    const tabParam = searchParams.get('tab')
    return tabParam === 'coverage' ? 'coverage' : 'entries'
  })

  // UI state
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [editingEntry, setEditingEntry] = useState<EegEntryDto | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [showExportDialog, setShowExportDialog] = useState(false)
  const [showDocumentDialog, setShowDocumentDialog] = useState(false)
  const [preSelectedCapabilityTargetId, setPreSelectedCapabilityTargetId] =
    useState<string | null>(null)
  const [preSelectedTaskId, setPreSelectedTaskId] = useState<string | null>(null)

  // Filter state
  const [ratingFilter, setRatingFilter] = useState<RatingFilterValue>('all')
  const [searchQuery, setSearchQuery] = useState('')

  // View mode state (for Entries tab)
  const [viewMode, setViewMode] = useState<ViewMode>('list')

  // Set breadcrumbs
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'EEG Entries', path: `/exercises/${exerciseId}/eeg-entries` },
      ]
      : [],
  )

  // Sync tab changes with URL
  useEffect(() => {
    const tabParam = searchParams.get('tab')
    const expectedTab = tabParam === 'coverage' ? 'coverage' : 'entries'
    if (activeTab !== expectedTab) {
      setActiveTab(expectedTab)
    }
  }, [searchParams, activeTab])

  // Permissions
  const canCreate = can('add_observation') // Evaluators can add EEG entries
  const canEdit = can('add_observation')
  const canDelete = can('delete_observation') // Directors only can delete
  const canExport = can('delete_observation') // Directors can export

  // Filter entries
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

  // Handlers
  const handleCreateClick = () => {
    setPreSelectedCapabilityTargetId(null)
    setPreSelectedTaskId(null)
    setShowCreateDialog(true)
  }

  const handleAssessTask = useCallback((taskId: string, capabilityTargetId: string) => {
    setPreSelectedCapabilityTargetId(capabilityTargetId)
    setPreSelectedTaskId(taskId)
    setShowCreateDialog(true)
  }, [])

  const handleCloseCreateDialog = () => {
    setShowCreateDialog(false)
    setPreSelectedCapabilityTargetId(null)
    setPreSelectedTaskId(null)
  }

  const handleEntrySaved = () => {
    queryClient.invalidateQueries({ queryKey: eegEntryKeys.byExercise(exerciseId!) })
    queryClient.invalidateQueries({ queryKey: eegEntryKeys.coverage(exerciseId!) })
    handleCloseCreateDialog()
    setEditingEntry(null)
  }

  const handleEdit = (entry: EegEntryDto) => {
    setEditingEntry(entry)
  }

  const handleCloseEditDialog = () => {
    setEditingEntry(null)
  }

  const handleDelete = async (entryId: string) => {
    // We need to use the task-level hook to delete, which requires the task ID
    const entry = eegEntries.find(e => e.id === entryId)
    if (!entry) return

    setDeletingId(entryId)
    try {
      // Direct API call for delete since we need exerciseId
      const { eegEntryService } = await import('../services/eegService')
      await eegEntryService.delete(exerciseId!, entryId)
      queryClient.invalidateQueries({ queryKey: eegEntryKeys.byExercise(exerciseId!) })
      queryClient.invalidateQueries({ queryKey: eegEntryKeys.coverage(exerciseId!) })
      notify.success('EEG entry deleted')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete EEG entry'
      notify.error(message)
    } finally {
      setDeletingId(null)
    }
  }

  const handleInjectClick = (injectId: string) => {
    navigate(`/exercises/${exerciseId}/msel?inject=${injectId}`)
  }

  const handleTabChange = (_event: React.SyntheticEvent, newValue: TabValue) => {
    setActiveTab(newValue)
    setSearchParams({ tab: newValue })
  }

  const clearFilters = () => {
    setRatingFilter('all')
    setSearchQuery('')
  }

  const hasActiveFilters = ratingFilter !== 'all' || searchQuery.trim()

  // Check if EEG tasks are configured
  const hasNoTasks = coverage && coverage.totalTasks === 0
  const hasNoEntries = eegEntries.length === 0

  // Loading state
  if (exerciseLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  // Error state
  if (exerciseError || !exercise) {
    return (
      <Alert severity="error" sx={{ m: 2 }}>
        {exerciseError || 'Exercise not found'}
      </Alert>
    )
  }

  return (
    <Box sx={{ p: 3 }}>
      <PageHeader
        title="EEG Entries"
        icon={faClipboardCheck}
        subtitle={`Exercise Evaluation Guide entries for ${exercise.name}`}
        actions={
          <Stack direction="row" spacing={2}>
          {/* Show Generate button only on Coverage tab */}
          {activeTab === 'coverage' && (
            <CobraSecondaryButton
              startIcon={<FontAwesomeIcon icon={faFileWord} />}
              onClick={() => setShowDocumentDialog(true)}
            >
              Generate EEG
            </CobraSecondaryButton>
          )}
          {canExport && (
            <CobraSecondaryButton
              startIcon={<FontAwesomeIcon icon={faFileExport} />}
              onClick={() => setShowExportDialog(true)}
            >
              Export
            </CobraSecondaryButton>
          )}
          {canCreate && activeTab === 'entries' && (
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={handleCreateClick}
            >
              Add Entry
            </CobraPrimaryButton>
          )}
          </Stack>
        }
        mb={2}
      />

      {/* Tab Navigation */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          aria-label="EEG page tabs"
        >
          <Tab
            label="Entries"
            value="entries"
            icon={<FontAwesomeIcon icon={faClipboardCheck} />}
            iconPosition="start"
            aria-label="Entries tab"
          />
          <Tab
            label="Coverage"
            value="coverage"
            icon={<FontAwesomeIcon icon={faChartBar} />}
            iconPosition="start"
            aria-label="Coverage tab"
          />
        </Tabs>
      </Box>

      {/* Entries Tab */}
      {activeTab === 'entries' && (
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
                exerciseId={exerciseId!}
                compact
                onDetailsClick={() => handleTabChange({} as React.SyntheticEvent, 'coverage')}
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
                  onClick={handleCreateClick}
                >
                  Add First Entry
                </CobraPrimaryButton>
              )}
            </Paper>
          ) : (
            /* Entries Views - Conditional based on viewMode */
            <Paper sx={{ p: 2 }}>
              {viewMode === 'list' && (
                <EegEntriesList
                  entries={filteredEntries}
                  loading={entriesLoading}
                  error={entriesError}
                  canEdit={canEdit}
                  canDelete={canDelete}
                  currentUserId={user?.id}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onInjectClick={handleInjectClick}
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
                  currentUserId={user?.id}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onInjectClick={handleInjectClick}
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
                  currentUserId={user?.id}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onInjectClick={handleInjectClick}
                  deletingId={deletingId}
                />
              )}
            </Paper>
          )}
        </Box>
      )}

      {/* Coverage Tab */}
      {activeTab === 'coverage' && (
        <Box role="tabpanel" aria-labelledby="coverage-tab">
          <EegCoverageDashboard
            exerciseId={exerciseId!}
            onAssessTask={canCreate ? handleAssessTask : undefined}
          />
        </Box>
      )}

      {/* Create Entry Dialog */}
      <Dialog
        open={showCreateDialog}
        onClose={handleCloseCreateDialog}
        maxWidth="md"
        fullWidth
        PaperProps={{
          sx: { minHeight: '600px', maxHeight: '90vh' },
        }}
      >
        <DialogContent sx={{ p: 0 }}>
          <EegEntryForm
            exerciseId={exerciseId!}
            availableInjects={injects.filter(i => i.status !== 'Draft')}
            preSelectedCapabilityTargetId={preSelectedCapabilityTargetId ?? undefined}
            preSelectedTaskId={preSelectedTaskId ?? undefined}
            onClose={handleCloseCreateDialog}
            onSaved={handleEntrySaved}
          />
        </DialogContent>
      </Dialog>

      {/* Edit Entry Dialog */}
      <Dialog
        open={!!editingEntry}
        onClose={handleCloseEditDialog}
        maxWidth="md"
        fullWidth
        PaperProps={{
          sx: { minHeight: '600px', maxHeight: '90vh' },
        }}
      >
        <DialogContent sx={{ p: 0 }}>
          {editingEntry && (
            <EegEntryForm
              exerciseId={exerciseId!}
              editEntry={editingEntry}
              availableInjects={injects.filter(i => i.status !== 'Draft')}
              onClose={handleCloseEditDialog}
              onSaved={handleEntrySaved}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Export Dialog */}
      {showExportDialog && (
        <EegExportDialog
          open={showExportDialog}
          exerciseId={exerciseId!}
          exerciseName={exercise.name}
          coverage={coverage}
          onClose={() => setShowExportDialog(false)}
        />
      )}

      {/* Document Generation Dialog */}
      {showDocumentDialog && (
        <EegDocumentDialog
          open={showDocumentDialog}
          exerciseId={exerciseId!}
          exerciseName={exercise.name}
          coverage={coverage}
          onClose={() => setShowDocumentDialog(false)}
        />
      )}
    </Box>
  )
}

export default EegEntriesPage
