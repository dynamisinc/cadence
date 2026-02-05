/**
 * EegEntriesPage
 *
 * Standalone page for viewing and managing all EEG entries in an exercise.
 * Provides filtering, sorting, and full CRUD capabilities for evaluator observations.
 * Accessible via in-exercise navigation at /exercises/:id/eeg-entries.
 */

import { useState, useMemo, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
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
} from '@fortawesome/free-solid-svg-icons'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'

import { useExercise } from '../../exercises/hooks'
import { useExerciseRole } from '../../auth'
import { useAuth } from '../../../contexts/AuthContext'
import { useEegEntries, eegEntryKeys, useEegEntriesByTask, useEegCoverage } from '../hooks/useEegEntries'
import { useInjects } from '../../injects/hooks'
import { EegEntriesList } from '../components/EegEntriesList'
import { EegEntryForm } from '../components/EegEntryForm'
import { EegCoverageDashboard } from '../components/EegCoverageDashboard'
import { EegExportDialog } from '../components/EegExportDialog'
import { EegDocumentDialog } from '../components/EegDocumentDialog'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraLinkButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useBreadcrumbs } from '../../../core/contexts'
import { ExerciseStatus } from '../../../types'
import {
  PerformanceRating,
  PERFORMANCE_RATING_SHORT_LABELS,
  type EegEntryDto,
} from '../types'

type RatingFilterValue = 'all' | PerformanceRating

export const EegEntriesPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  // Core data hooks
  const { user } = useAuth()
  const { exercise, loading: exerciseLoading, error: exerciseError } = useExercise(exerciseId)
  const { can } = useExerciseRole(exerciseId ?? null)
  const {
    eegEntries,
    loading: entriesLoading,
    error: entriesError,
    createEntry,
    isCreating,
  } = useEegEntries(exerciseId!)
  const { injects } = useInjects(exerciseId!)
  const { coverage } = useEegCoverage(exerciseId!)

  // UI state
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [editingEntry, setEditingEntry] = useState<EegEntryDto | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [showExportDialog, setShowExportDialog] = useState(false)
  const [showDocumentDialog, setShowDocumentDialog] = useState(false)
  const [preSelectedCapabilityTargetId, setPreSelectedCapabilityTargetId] = useState<string | null>(null)
  const [preSelectedTaskId, setPreSelectedTaskId] = useState<string | null>(null)

  // Filter state
  const [ratingFilter, setRatingFilter] = useState<RatingFilterValue>('all')
  const [searchQuery, setSearchQuery] = useState('')

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
    [exercise, exerciseId],
  )

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
      toast.success('EEG entry deleted')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete EEG entry'
      toast.error(message)
    } finally {
      setDeletingId(null)
    }
  }

  const handleInjectClick = (injectId: string) => {
    navigate(`/exercises/${exerciseId}/msel?inject=${injectId}`)
  }

  const clearFilters = () => {
    setRatingFilter('all')
    setSearchQuery('')
  }

  const hasActiveFilters = ratingFilter !== 'all' || searchQuery.trim()

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

  // Check exercise status
  const isExerciseActive = exercise.status === ExerciseStatus.Active

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
        sx={{ mb: 3 }}
      >
        <Box>
          <Typography variant="h4" fontWeight={600}>
            EEG Entries
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Exercise Evaluation Guide entries for {exercise.name}
          </Typography>
        </Box>
        <Stack direction="row" spacing={2}>
          <CobraSecondaryButton
            startIcon={<FontAwesomeIcon icon={faFileWord} />}
            onClick={() => setShowDocumentDialog(true)}
          >
            Generate EEG
          </CobraSecondaryButton>
          {canExport && (
            <CobraSecondaryButton
              startIcon={<FontAwesomeIcon icon={faFileExport} />}
              onClick={() => setShowExportDialog(true)}
            >
              Export
            </CobraSecondaryButton>
          )}
          {canCreate && (
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={handleCreateClick}
            >
              Add Entry
            </CobraPrimaryButton>
          )}
        </Stack>
      </Stack>

      {/* Coverage Dashboard */}
      <Box sx={{ mb: 3 }}>
        <EegCoverageDashboard
          exerciseId={exerciseId!}
          onAssessTask={canCreate ? handleAssessTask : undefined}
        />
      </Box>

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
            InputProps={{
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
            }}
          />

          {/* Rating Filter */}
          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel>Rating</InputLabel>
            <Select
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
          <Typography variant="body2" color="text.secondary">
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

      {/* Entries List */}
      <Paper sx={{ p: 2 }}>
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
      </Paper>

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
