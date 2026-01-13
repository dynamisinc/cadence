/**
 * ExerciseConductPage
 *
 * Main conduct view for running an exercise. Displays:
 * - Exercise clock with start/pause/reset controls
 * - Inject list with fire/skip controls
 * - Observations panel for evaluators
 *
 * Uses SignalR for real-time updates across all connected clients.
 */

import { useState, useCallback, useMemo } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Paper,
  Stack,
  Grid,
  CircularProgress,
  Alert,
  Divider,
  Collapse,
  IconButton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faHome,
  faPlus,
  faChevronDown,
  faChevronUp,
  faWifi,
  faTriangleExclamation,
} from '@fortawesome/free-solid-svg-icons'
import { useQueryClient } from '@tanstack/react-query'

import { useExercise } from '../hooks'
import { ExerciseStatusChip, ExerciseTypeChip } from '../components'
import { CobraLinkButton, CobraPrimaryButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useBreadcrumbs } from '../../../core/contexts'
import { useExerciseSignalR } from '../../../shared/hooks'
import { ExerciseStatus } from '../../../types'

// Feature imports
import { ClockDisplay, ClockControls, useExerciseClock, clockQueryKey } from '../../exercise-clock'
import { InjectList, useInjects, injectsQueryKey } from '../../injects'
import {
  ObservationForm,
  ObservationList,
  useObservations,
  observationsQueryKey,
} from '../../observations'
import type { ObservationDto } from '../../observations/types'
import type { InjectDto } from '../../injects/types'
import type { ExerciseClockDto } from '../../exercise-clock/types'

export const ExerciseConductPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  // Core data hooks
  const { exercise, loading: exerciseLoading, error: exerciseError } = useExercise(exerciseId)
  const {
    clock,
    loading: clockLoading,
    startClock,
    pauseClock,
    resetClock,
  } = useExerciseClock(exerciseId!)
  const {
    injects,
    loading: injectsLoading,
    error: injectsError,
    fireInject,
    skipInject,
    resetInject,
  } = useInjects(exerciseId!)
  const {
    observations,
    loading: observationsLoading,
    error: observationsError,
    createObservation,
    deleteObservation,
  } = useObservations(exerciseId!)

  // UI state
  const [showObservationForm, setShowObservationForm] = useState(false)
  const [isSubmittingObservation, setIsSubmittingObservation] = useState(false)
  const [deletingObservationId, setDeletingObservationId] = useState<string | null>(null)
  const [observationsExpanded, setObservationsExpanded] = useState(true)

  // Set breadcrumbs
  useBreadcrumbs(
    exercise
      ? [
          { label: 'Home', path: '/', icon: faHome },
          { label: 'Exercises', path: '/exercises' },
          { label: exercise.name, path: `/exercises/${exerciseId}` },
          { label: 'Conduct' },
        ]
      : undefined
  )

  // SignalR real-time handlers
  const handleInjectFired = useCallback(
    (inject: InjectDto) => {
      queryClient.setQueryData<InjectDto[]>(injectsQueryKey(exerciseId!), (old) =>
        old?.map((i) => (i.id === inject.id ? inject : i)) ?? []
      )
    },
    [exerciseId, queryClient]
  )

  const handleInjectStatusChanged = useCallback(
    (inject: InjectDto) => {
      queryClient.setQueryData<InjectDto[]>(injectsQueryKey(exerciseId!), (old) =>
        old?.map((i) => (i.id === inject.id ? inject : i)) ?? []
      )
    },
    [exerciseId, queryClient]
  )

  const handleClockChanged = useCallback(
    (clockDto: ExerciseClockDto) => {
      queryClient.setQueryData<ExerciseClockDto>(clockQueryKey(exerciseId!), clockDto)
    },
    [exerciseId, queryClient]
  )

  const handleObservationAdded = useCallback(
    (observation: ObservationDto) => {
      queryClient.setQueryData<ObservationDto[]>(observationsQueryKey(exerciseId!), (old) => [
        observation,
        ...(old ?? []),
      ])
    },
    [exerciseId, queryClient]
  )

  const handleObservationDeleted = useCallback(
    (observationId: string) => {
      queryClient.setQueryData<ObservationDto[]>(observationsQueryKey(exerciseId!), (old) =>
        old?.filter((o) => o.id !== observationId) ?? []
      )
    },
    [exerciseId, queryClient]
  )

  // Connect to SignalR
  const { connectionState, isJoined, error: signalRError } = useExerciseSignalR({
    exerciseId: exerciseId!,
    onInjectFired: handleInjectFired,
    onInjectStatusChanged: handleInjectStatusChanged,
    onClockStarted: handleClockChanged,
    onClockPaused: handleClockChanged,
    onClockReset: handleClockChanged,
    onClockChanged: handleClockChanged,
    onObservationAdded: handleObservationAdded,
    onObservationDeleted: handleObservationDeleted,
    enabled: !!exerciseId,
  })

  // Permission checks (simplified - should use actual RBAC)
  const canControl = useMemo(() => {
    // Controllers and Exercise Directors can control injects/clock
    return exercise?.status === ExerciseStatus.Active
  }, [exercise])

  const canAddObservations = useMemo(() => {
    // Evaluators can add observations during active exercises
    return exercise?.status === ExerciseStatus.Active
  }, [exercise])

  // Handlers
  const handleCreateObservation = async (data: Parameters<typeof createObservation>[0]) => {
    setIsSubmittingObservation(true)
    try {
      await createObservation(data)
      setShowObservationForm(false)
    } finally {
      setIsSubmittingObservation(false)
    }
  }

  const handleDeleteObservation = async (observationId: string) => {
    setDeletingObservationId(observationId)
    try {
      await deleteObservation(observationId)
    } finally {
      setDeletingObservationId(null)
    }
  }

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

  // Not active warning
  if (exercise.status !== ExerciseStatus.Active) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Alert severity="warning" sx={{ mb: 2 }}>
          Exercise conduct is only available for Active exercises. This exercise is currently{' '}
          {exercise.status}.
        </Alert>
        <CobraLinkButton onClick={() => navigate(`/exercises/${exerciseId}`)}>
          Back to Exercise Details
        </CobraLinkButton>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Header */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="flex-start"
        marginBottom={3}
      >
        <Box>
          <Typography variant="h5" component="h1">
            {exercise.name}
          </Typography>
          <Stack direction="row" spacing={1} mt={1}>
            <ExerciseTypeChip type={exercise.exerciseType} />
            <ExerciseStatusChip status={exercise.status} />
          </Stack>
        </Box>

        <Stack direction="row" spacing={2} alignItems="center">
          {/* Connection status indicator */}
          <Stack direction="row" spacing={1} alignItems="center">
            {connectionState === 'connected' && isJoined ? (
              <Box sx={{ color: 'success.main' }}>
                <FontAwesomeIcon icon={faWifi} />
              </Box>
            ) : connectionState === 'error' || signalRError ? (
              <Box sx={{ color: 'error.main' }}>
                <FontAwesomeIcon icon={faTriangleExclamation} />
              </Box>
            ) : (
              <CircularProgress size={16} />
            )}
            <Typography variant="caption" color="text.secondary">
              {connectionState === 'connected' && isJoined
                ? 'Live'
                : connectionState === 'connecting'
                  ? 'Connecting...'
                  : connectionState === 'reconnecting'
                    ? 'Reconnecting...'
                    : signalRError || 'Disconnected'}
            </Typography>
          </Stack>

          <CobraLinkButton onClick={() => navigate(`/exercises/${exerciseId}`)}>
            Exit Conduct
          </CobraLinkButton>
        </Stack>
      </Stack>

      {/* Main Content Grid */}
      <Grid container spacing={3}>
        {/* Left Column: Clock & Injects */}
        <Grid size={{ xs: 12, md: 8 }}>
          {/* Exercise Clock */}
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="h6" gutterBottom>
              Exercise Clock
            </Typography>
            <Stack spacing={2} alignItems="center">
              <ClockDisplay clock={clock} loading={clockLoading} size="large" />
              {canControl && (
                <ClockControls
                  clock={clock}
                  onStart={startClock}
                  onPause={pauseClock}
                  onReset={resetClock}
                />
              )}
            </Stack>
          </Paper>

          {/* Inject List */}
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Injects
            </Typography>
            <InjectList
              injects={injects}
              loading={injectsLoading}
              error={injectsError}
              canControl={canControl}
              onFire={fireInject}
              onSkip={skipInject}
              onReset={resetInject}
            />
          </Paper>
        </Grid>

        {/* Right Column: Observations */}
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper sx={{ p: 3 }}>
            <Stack
              direction="row"
              justifyContent="space-between"
              alignItems="center"
              sx={{ mb: 2 }}
            >
              <Stack direction="row" spacing={1} alignItems="center">
                <Typography variant="h6">Observations</Typography>
                <IconButton
                  size="small"
                  onClick={() => setObservationsExpanded(!observationsExpanded)}
                >
                  <FontAwesomeIcon
                    icon={observationsExpanded ? faChevronUp : faChevronDown}
                  />
                </IconButton>
              </Stack>
              {canAddObservations && !showObservationForm && (
                <CobraPrimaryButton
                  size="small"
                  startIcon={<FontAwesomeIcon icon={faPlus} />}
                  onClick={() => setShowObservationForm(true)}
                >
                  Add
                </CobraPrimaryButton>
              )}
            </Stack>

            <Collapse in={observationsExpanded}>
              {/* Observation Form */}
              {showObservationForm && (
                <Box sx={{ mb: 2 }}>
                  <ObservationForm
                    exerciseId={exerciseId!}
                    injects={injects}
                    onSubmit={handleCreateObservation}
                    onCancel={() => setShowObservationForm(false)}
                    isSubmitting={isSubmittingObservation}
                  />
                  <Divider sx={{ my: 2 }} />
                </Box>
              )}

              {/* Observation List */}
              <ObservationList
                observations={observations}
                loading={observationsLoading}
                error={observationsError}
                canEdit={canAddObservations}
                onDelete={handleDeleteObservation}
                deletingId={deletingObservationId}
              />
            </Collapse>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  )
}

export default ExerciseConductPage
