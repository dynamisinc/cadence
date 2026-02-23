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
  faClipboardCheck,
  faCamera,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'
import { useQueryClient } from '@tanstack/react-query'

import { useExercise } from '../../exercises/hooks'
import { useExerciseClock } from '../../exercise-clock'
import { useExerciseRole } from '../../auth'
import { ObservationForm } from '../components/ObservationForm'
import { ObservationList } from '../components/ObservationList'
import { useObservations, observationsQueryKey } from '../hooks/useObservations'
import { useInjects } from '../../injects/hooks'
import { usePhotos } from '../../photos/hooks'
import { useCamera } from '../../photos/hooks/useCamera'
import { useImageCompression } from '../../photos/hooks/useImageCompression'
import { useExerciseSignalR } from '../../../shared/hooks'
import { useCapabilities } from '../../capabilities/hooks/useCapabilities'
import { useExerciseTargetCapabilities } from '../../exercises/hooks/useExerciseTargetCapabilities'
import { EegEntryForm } from '../../eeg/components'
import { eegEntryKeys } from '../../eeg/hooks/useEegEntries'
import {
  CobraPrimaryButton,
  CobraLinkButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { PageHeader } from '@/shared/components'
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
  const { displayTime } = useExerciseClock(exerciseId!)
  const { quickPhoto, uploadPhoto } = usePhotos(exerciseId!)
  const { compressImage } = useImageCompression()

  // UI state
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [editingObservation, setEditingObservation] = useState<ObservationDto | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [showEegEntryForm, setShowEegEntryForm] = useState(false)
  const [isCapturingPhoto, setIsCapturingPhoto] = useState(false)

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

  // Upload staged photos for an observation
  const uploadPendingPhotos = async (observationId: string, pendingPhotos: File[]) => {
    for (const file of pendingPhotos) {
      try {
        const { compressed } = await compressImage(file)
        const formData = new FormData()
        formData.append('photo', compressed, file.name)
        formData.append('capturedAt', new Date().toISOString())
        if (displayTime) formData.append('scenarioTime', displayTime)
        formData.append('observationId', observationId)
        await uploadPhoto(formData)
      } catch (error) {
        console.error('Failed to upload staged photo:', error)
      }
    }
    // Refresh observations to show attached photos
    queryClient.invalidateQueries({ queryKey: observationsQueryKey(exerciseId!) })
  }

  // Handlers
  const handleSubmitObservation = async (
    data: Parameters<typeof createObservation>[0],
    pendingPhotos?: File[],
  ) => {
    setIsSubmitting(true)
    try {
      if (editingObservation) {
        await updateObservation(editingObservation.id, data)
        // Upload any staged photos after the update succeeds
        if (pendingPhotos?.length && !editingObservation.id.startsWith('temp-')) {
          await uploadPendingPhotos(editingObservation.id, pendingPhotos)
        }
        setEditingObservation(null)
      } else {
        const newObservation = await createObservation(data)
        setShowCreateDialog(false)

        // Upload any staged photos after the observation is created
        if (pendingPhotos?.length && newObservation?.id && !newObservation.id.startsWith('temp-')) {
          await uploadPendingPhotos(newObservation.id, pendingPhotos)
        }
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

  // EEG Entry handlers
  const handleCloseEegEntry = () => {
    setShowEegEntryForm(false)
  }

  const handleEegEntrySaved = () => {
    // Invalidate EEG entry queries to refresh dashboard
    queryClient.invalidateQueries({ queryKey: eegEntryKeys.all })
  }

  // Quick Photo handler
  const handleQuickPhotoFile = useCallback(
    async (file: File) => {
      setIsCapturingPhoto(true)
      try {
        const { compressed } = await compressImage(file)

        // Get GPS location (best-effort)
        let latitude: number | undefined
        let longitude: number | undefined
        let locationAccuracy: number | undefined
        if (navigator.geolocation) {
          try {
            const pos = await new Promise<GeolocationPosition>((resolve, reject) =>
              navigator.geolocation.getCurrentPosition(resolve, reject, {
                enableHighAccuracy: true,
                timeout: 5000,
                maximumAge: 0,
              }),
            )
            latitude = pos.coords.latitude
            longitude = pos.coords.longitude
            locationAccuracy = pos.coords.accuracy
          } catch {
            // Location unavailable - continue without it
          }
        }

        const formData = new FormData()
        formData.append('photo', compressed, 'quick-photo.jpg')
        formData.append('capturedAt', new Date().toISOString())
        if (displayTime) formData.append('scenarioTime', displayTime)
        if (latitude != null) formData.append('latitude', latitude.toString())
        if (longitude != null) formData.append('longitude', longitude.toString())
        if (locationAccuracy != null) formData.append('locationAccuracy', locationAccuracy.toString())

        await quickPhoto(formData)
      } catch (error) {
        console.error('Quick photo capture failed:', error)
      } finally {
        setIsCapturingPhoto(false)
      }
    },
    [compressImage, displayTime, quickPhoto],
  )

  const {
    fileInputRef: quickPhotoInputRef,
    openCamera: openQuickPhotoCamera,
    handleFileChange: handleQuickPhotoChange,
  } = useCamera(handleQuickPhotoFile)

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
      {/* Hidden file input for quick photo capture */}
      <input
        ref={quickPhotoInputRef}
        type="file"
        accept="image/*"
        capture="environment"
        hidden
        onChange={handleQuickPhotoChange}
      />

      <PageHeader
        title="Observations"
        icon={faEye}
        subtitle="Record and review evaluator observations for this exercise"
        chips={
          <Chip
            label={`${observations.length} total`}
            size="small"
            variant="outlined"
          />
        }
        actions={
          <>
            {canAddObservations && (
              <>
                <CobraPrimaryButton
                  startIcon={<FontAwesomeIcon icon={faPlus} />}
                  onClick={() => setShowCreateDialog(true)}
                >
                  Add Observation
                </CobraPrimaryButton>
                <CobraPrimaryButton
                  startIcon={
                    isCapturingPhoto
                      ? <FontAwesomeIcon icon={faSpinner} spin />
                      : <FontAwesomeIcon icon={faCamera} />
                  }
                  onClick={openQuickPhotoCamera}
                  disabled={isCapturingPhoto}
                  variant="outlined"
                >
                  {isCapturingPhoto ? 'Capturing...' : 'Quick Photo'}
                </CobraPrimaryButton>
                <CobraPrimaryButton
                  startIcon={<FontAwesomeIcon icon={faClipboardCheck} />}
                  onClick={() => setShowEegEntryForm(true)}
                  variant="outlined"
                >
                  Add EEG Entry
                </CobraPrimaryButton>
              </>
            )}
            <CobraLinkButton onClick={() => navigate(`/exercises/${exerciseId}`)}>
              Back to Exercise
            </CobraLinkButton>
          </>
        }
      />

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
              exerciseId={exerciseId!}
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
                exerciseId={exerciseId!}
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
                observation={
                  observations.find(o => o.id === editingObservation.id)
                  ?? editingObservation
                }
                onSubmit={handleSubmitObservation}
                onCancel={handleCancelEdit}
                isSubmitting={isSubmitting}
              />
            )}
          </Box>
        </DialogContent>
      </Dialog>

      {/* EEG Entry Form Dialog */}
      <Dialog
        open={showEegEntryForm}
        onClose={handleCloseEegEntry}
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
            onClose={handleCloseEegEntry}
            onSaved={handleEegEntrySaved}
          />
        </DialogContent>
      </Dialog>
    </Box>
  )
}

export default ObservationsPage
