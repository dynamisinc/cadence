/**
 * ObservationsPage
 *
 * Standalone page for viewing and managing all observations in an exercise.
 * Provides enhanced filtering, search, and full CRUD capabilities.
 * Accessible via in-exercise navigation at /exercises/:id/observations.
 */

import { useState, useMemo, useCallback, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Paper,
  Stack,
  CircularProgress,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Divider,
  Chip,
  InputAdornment,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faHome,
  faPlus,
  faSearch,
  faTimes,
  faEye,
} from '@fortawesome/free-solid-svg-icons'
import { useQueryClient } from '@tanstack/react-query'

import { useExercise } from '../../exercises/hooks'
import { useExerciseRole } from '../../auth'
import { ObservationForm } from '../components/ObservationForm'
import { ObservationList } from '../components/ObservationList'
import { useObservations, observationsQueryKey } from '../hooks/useObservations'
import { useInjects } from '../../injects/hooks'
import { useExerciseSignalR } from '../../../shared/hooks'
import { useCapabilities } from '../../capabilities/hooks/useCapabilities'
import { useExerciseTargetCapabilities } from '../../exercises/hooks/useExerciseTargetCapabilities'
import {
  CobraPrimaryButton,
  CobraLinkButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useBreadcrumbs } from '../../../core/contexts'
import { ExerciseStatus, ObservationRating, ObservationRatingLabels } from '../../../types'
import type { ObservationDto } from '../types'

type RatingFilterValue = 'all' | ObservationRating | 'unrated'

export const ObservationsPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  // Core data hooks
  const { exercise, loading: exerciseLoading, error: exerciseError } = useExercise(exerciseId)
  const { can } = useExerciseRole(exerciseId ?? null)
  const {
    observations,
    loading: observationsLoading,
    error: observationsError,
    createObservation,
    updateObservation,
    deleteObservation,
  } = useObservations(exerciseId!)
  const { injects } = useInjects(exerciseId!)
  const { capabilities } = useCapabilities(false) // Active capabilities only
  const { targetCapabilities } = useExerciseTargetCapabilities(exerciseId)

  // UI state
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [editingObservation, setEditingObservation] = useState<ObservationDto | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [deletingId, setDeletingId] = useState<string | null>(null)

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
        { label: 'Observations' },
      ]
      : undefined,
  )

  // SignalR handlers for real-time updates
  const handleObservationAdded = useCallback(
    (observation: ObservationDto) => {
      queryClient.setQueryData<ObservationDto[]>(observationsQueryKey(exerciseId!), old => {
        const existing = old?.find(o => o.id === observation.id)
        if (existing) {
          return old!.map(o => (o.id === observation.id ? observation : o))
        }
        return [observation, ...(old ?? [])]
      })
    },
    [exerciseId, queryClient],
  )

  const handleObservationUpdated = useCallback(
    (observation: ObservationDto) => {
      queryClient.setQueryData<ObservationDto[]>(observationsQueryKey(exerciseId!), old =>
        old?.map(o => (o.id === observation.id ? observation : o)) ?? [],
      )
    },
    [exerciseId, queryClient],
  )

  const handleObservationDeleted = useCallback(
    (observationId: string) => {
      queryClient.setQueryData<ObservationDto[]>(observationsQueryKey(exerciseId!), old =>
        old?.filter(o => o.id !== observationId) ?? [],
      )
    },
    [exerciseId, queryClient],
  )

  // Connect to SignalR for real-time updates
  useExerciseSignalR({
    exerciseId: exerciseId!,
    onObservationAdded: handleObservationAdded,
    onObservationUpdated: handleObservationUpdated,
    onObservationDeleted: handleObservationDeleted,
    enabled: !!exerciseId,
  })

  // Permission checks
  const canAddObservations = useMemo(() => {
    return exercise?.status === ExerciseStatus.Active && can('add_observation')
  }, [exercise, can])

  const canDeleteObservations = useMemo(() => {
    // Directors and Admins can delete
    return can('delete_observation')
  }, [can])

  // Filter observations
  const filteredObservations = useMemo(() => {
    let filtered = [...observations]

    // Filter by rating
    if (ratingFilter !== 'all') {
      if (ratingFilter === 'unrated') {
        filtered = filtered.filter(obs => obs.rating === null)
      } else {
        filtered = filtered.filter(obs => obs.rating === ratingFilter)
      }
    }

    // Filter by search query
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase().trim()
      filtered = filtered.filter(
        obs =>
          obs.content.toLowerCase().includes(query) ||
          obs.recommendation?.toLowerCase().includes(query) ||
          obs.injectTitle?.toLowerCase().includes(query) ||
          obs.createdByName?.toLowerCase().includes(query),
      )
    }

    return filtered
  }, [observations, ratingFilter, searchQuery])

  // Check if filters are active
  const hasActiveFilters = ratingFilter !== 'all' || searchQuery.trim() !== ''

  // Clear all filters
  const handleClearFilters = () => {
    setRatingFilter('all')
    setSearchQuery('')
  }

  // Handlers
  const handleSubmitObservation = async (data: Parameters<typeof createObservation>[0]) => {
    setIsSubmitting(true)
    try {
      if (editingObservation) {
        await updateObservation(editingObservation.id, data)
        setEditingObservation(null)
      } else {
        await createObservation(data)
        setShowCreateDialog(false)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleEdit = (observation: ObservationDto) => {
    setEditingObservation(observation)
  }

  const handleCancelEdit = () => {
    setEditingObservation(null)
  }

  const handleDelete = async (observationId: string) => {
    setDeletingId(observationId)
    try {
      await deleteObservation(observationId)
    } finally {
      setDeletingId(null)
    }
  }

  // Keyboard shortcut: Ctrl+O to open create dialog
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Check for Ctrl+O (or Cmd+O on Mac)
      if ((e.ctrlKey || e.metaKey) && e.key === 'o') {
        e.preventDefault()
        if (canAddObservations && !showCreateDialog && !editingObservation) {
          setShowCreateDialog(true)
        }
      }
      // Also allow just 'O' when not in an input field
      if (
        e.key === 'o' &&
        !e.ctrlKey &&
        !e.metaKey &&
        !e.altKey &&
        !['INPUT', 'TEXTAREA', 'SELECT'].includes((e.target as HTMLElement).tagName)
      ) {
        if (canAddObservations && !showCreateDialog && !editingObservation) {
          setShowCreateDialog(true)
        }
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [canAddObservations, showCreateDialog, editingObservation])

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
        <CobraPrimaryButton onClick={() => navigate('/exercises')}>
          Back to Exercises
        </CobraPrimaryButton>
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
        <CobraPrimaryButton onClick={() => navigate('/exercises')}>
          Back to Exercises
        </CobraPrimaryButton>
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
        flexWrap="wrap"
        gap={2}
      >
        <Stack direction="row" alignItems="center" spacing={2}>
          <FontAwesomeIcon icon={faEye} size="lg" />
          <Typography variant="h5">Observations</Typography>
          <Chip
            label={`${observations.length} total`}
            size="small"
            variant="outlined"
          />
        </Stack>
        <Stack direction="row" spacing={2}>
          {canAddObservations && (
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={() => setShowCreateDialog(true)}
            >
              Add Observation
            </CobraPrimaryButton>
          )}
          <CobraLinkButton onClick={() => navigate(`/exercises/${exerciseId}`)}>
            Back to Exercise
          </CobraLinkButton>
        </Stack>
      </Stack>

      {/* Keyboard shortcut hint */}
      {canAddObservations && (
        <Typography variant="caption" color="text.secondary" sx={{ mb: 2, display: 'block' }}>
          Tip: Press <strong>Ctrl+O</strong> or <strong>O</strong> to quickly add an observation
        </Typography>
      )}

      {/* Filter Bar */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap" gap={1}>
          {/* Search */}
          <CobraTextField
            size="small"
            placeholder="Search observations..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <FontAwesomeIcon icon={faSearch} />
                  </InputAdornment>
                ),
              },
            }}
            sx={{ minWidth: 250 }}
          />

          {/* Rating Filter */}
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel id="rating-filter-label">Filter by Rating</InputLabel>
            <Select
              labelId="rating-filter-label"
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

          {/* Clear Filters */}
          {hasActiveFilters && (
            <CobraLinkButton
              onClick={handleClearFilters}
              startIcon={<FontAwesomeIcon icon={faTimes} />}
            >
              Clear Filters
            </CobraLinkButton>
          )}
        </Stack>

        {/* Results count */}
        {hasActiveFilters && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Showing {filteredObservations.length} of {observations.length} observations
          </Typography>
        )}
      </Paper>

      {/* Observations List */}
      {observationsLoading ? (
        <Box display="flex" justifyContent="center" p={4}>
          <CircularProgress />
        </Box>
      ) : observationsError ? (
        <Alert severity="error">{observationsError}</Alert>
      ) : filteredObservations.length === 0 ? (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <FontAwesomeIcon
            icon={faEye}
            size="3x"
            style={{ color: '#9e9e9e', marginBottom: 16 }}
          />
          <Typography variant="h6" gutterBottom>
            {hasActiveFilters ? 'No matching observations' : 'No observations yet'}
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 2 }}>
            {hasActiveFilters
              ? 'Try adjusting your filters or search query.'
              : 'Observations recorded during exercise conduct will appear here.'}
          </Typography>
          {hasActiveFilters ? (
            <CobraLinkButton onClick={handleClearFilters}>Clear Filters</CobraLinkButton>
          ) : canAddObservations ? (
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={() => setShowCreateDialog(true)}
            >
              Add Observation
            </CobraPrimaryButton>
          ) : null}
        </Paper>
      ) : (
        <ObservationList
          observations={filteredObservations}
          canEdit={canAddObservations}
          onEdit={handleEdit}
          onDelete={canDeleteObservations ? handleDelete : undefined}
          deletingId={deletingId}
          showFilterBar={false}
        />
      )}

      {/* Create Dialog */}
      <Dialog
        open={showCreateDialog}
        onClose={() => setShowCreateDialog(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>New Observation</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            <ObservationForm
              injects={injects}
              capabilities={capabilities}
              targetCapabilityIds={targetCapabilities.map(c => c.id)}
              onSubmit={handleSubmitObservation}
              onCancel={() => setShowCreateDialog(false)}
              isSubmitting={isSubmitting}
            />
          </Box>
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog
        open={!!editingObservation}
        onClose={handleCancelEdit}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Edit Observation</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            {editingObservation && (
              <ObservationForm
                injects={injects}
                capabilities={capabilities}
                targetCapabilityIds={targetCapabilities.map(c => c.id)}
                initialValues={{
                  rating: editingObservation.rating!,
                  content: editingObservation.content,
                  recommendation: editingObservation.recommendation ?? undefined,
                  injectId: editingObservation.injectId ?? undefined,
                  capabilityIds: editingObservation.capabilities.map(c => c.id),
                }}
                onSubmit={handleSubmitObservation}
                onCancel={handleCancelEdit}
                isSubmitting={isSubmitting}
              />
            )}
          </Box>
        </DialogContent>
      </Dialog>
    </Box>
  )
}

export default ObservationsPage
