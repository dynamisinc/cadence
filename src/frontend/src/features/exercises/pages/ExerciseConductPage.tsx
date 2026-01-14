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
  CircularProgress,
  Alert,
  Divider,
  IconButton,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faHome,
  faPlus,
  faChevronDown,
  faChevronUp,
  faWifi,
  faTriangleExclamation,
  faGaugeHigh,
  faBookOpen,
  faGrip,
  faWindowMaximize,
} from '@fortawesome/free-solid-svg-icons'
import { useQueryClient } from '@tanstack/react-query'

import { useExercise } from '../hooks'
import { ExerciseHeader, NarrativeView, StickyClockHeader, FloatingClockChip } from '../components'
import { CobraLinkButton, CobraPrimaryButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useBreadcrumbs } from '../../../core/contexts'
import { useExerciseSignalR } from '../../../shared/hooks'
import { ExerciseStatus, InjectStatus } from '../../../types'

// Feature imports
import { ClockDisplay, ClockControls, ExerciseProgress, useExerciseClock, clockQueryKey } from '../../exercise-clock'
import { InjectListByStatus, ReadyToFireBadge, ReadyNotification, useInjects, injectKeys, calculateScheduledOffset } from '../../injects'
import {
  ObservationForm,
  ObservationList,
  useObservations,
  observationsQueryKey,
} from '../../observations'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
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
    clockState,
    displayTime,
    elapsedTimeMs,
    exerciseStartTime,
    loading: clockLoading,
    startClock,
    pauseClock,
    stopClock,
    resetClock,
    isStarting,
    isPausing,
    isStopping,
    isResetting,
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
  const [showStopConfirm, setShowStopConfirm] = useState(false)

  // View mode state with localStorage persistence
  const [viewMode, setViewMode] = useState<'controller' | 'narrative'>(() => {
    const saved = localStorage.getItem('cadence-conduct-view-mode')
    return saved === 'narrative' ? 'narrative' : 'controller'
  })

  const handleViewModeChange = (
    _event: React.MouseEvent<HTMLElement>,
    newMode: 'controller' | 'narrative' | null,
  ) => {
    if (newMode) {
      setViewMode(newMode)
      localStorage.setItem('cadence-conduct-view-mode', newMode)
    }
  }

  // Layout mode state with localStorage persistence (for A/B testing)
  // 'classic' = original large clock panel, 'sticky' = sticky header, 'floating' = floating chip
  const [layoutMode, setLayoutMode] = useState<'classic' | 'sticky' | 'floating'>(() => {
    const saved = localStorage.getItem('cadence-conduct-layout-mode')
    if (saved === 'sticky' || saved === 'floating') return saved
    return 'classic'
  })

  const handleLayoutModeChange = (
    _event: React.MouseEvent<HTMLElement>,
    newLayout: 'classic' | 'sticky' | 'floating' | null,
  ) => {
    if (newLayout) {
      setLayoutMode(newLayout)
      localStorage.setItem('cadence-conduct-layout-mode', newLayout)
    }
  }

  // Set breadcrumbs
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'Conduct' },
      ]
      : undefined,
  )

  // SignalR real-time handlers
  const handleInjectFired = useCallback(
    (inject: InjectDto) => {
      queryClient.setQueryData<InjectDto[]>(injectKeys.all(exerciseId!), old =>
        old?.map(i => (i.id === inject.id ? inject : i)) ?? [],
      )
    },
    [exerciseId, queryClient],
  )

  const handleInjectStatusChanged = useCallback(
    (inject: InjectDto) => {
      queryClient.setQueryData<InjectDto[]>(injectKeys.all(exerciseId!), old =>
        old?.map(i => (i.id === inject.id ? inject : i)) ?? [],
      )
    },
    [exerciseId, queryClient],
  )

  const handleClockChanged = useCallback(
    (clockDto: ExerciseClockDto) => {
      queryClient.setQueryData<ExerciseClockDto>(clockQueryKey(exerciseId!), clockDto)
    },
    [exerciseId, queryClient],
  )

  const handleObservationAdded = useCallback(
    (observation: ObservationDto) => {
      queryClient.setQueryData<ObservationDto[]>(observationsQueryKey(exerciseId!), old => [
        observation,
        ...(old ?? []),
      ])
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

  // Calculate ready-to-fire count for badge
  const readyToFireCount = useMemo(() => {
    if (!injects || injects.length === 0) return 0
    return injects.filter(inject => {
      if (inject.status !== InjectStatus.Pending) return false
      const offsetMs = calculateScheduledOffset(inject.scheduledTime, exerciseStartTime)
      return offsetMs <= elapsedTimeMs
    }).length
  }, [injects, exerciseStartTime, elapsedTimeMs])

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

  // Stop clock with confirmation
  const handleStopClick = () => {
    setShowStopConfirm(true)
  }

  const handleStopConfirmed = async () => {
    await stopClock()
    setShowStopConfirm(false)
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
    <Box
      sx={{
        padding: CobraStyles.Padding.MainWindow,
        // Fill parent container (AppLayout handles the height constraints)
        height: '100%',
        overflow: 'hidden',
        display: 'flex',
        flexDirection: 'column',
        boxSizing: 'border-box',
      }}
    >
      {/* Header */}
      <ExerciseHeader
        exercise={exercise}
        actions={
          <>
            {/* View Mode Toggle */}
            <ToggleButtonGroup
              value={viewMode}
              exclusive
              onChange={handleViewModeChange}
              size="small"
              aria-label="view mode"
            >
              <ToggleButton value="controller" aria-label="controller view">
                <FontAwesomeIcon icon={faGaugeHigh} style={{ marginRight: 6 }} />
                Controller
              </ToggleButton>
              <ToggleButton value="narrative" aria-label="narrative view">
                <FontAwesomeIcon icon={faBookOpen} style={{ marginRight: 6 }} />
                Narrative
              </ToggleButton>
            </ToggleButtonGroup>

            {/* Layout Mode Toggle (only in Controller view) */}
            {viewMode === 'controller' && (
              <ToggleButtonGroup
                value={layoutMode}
                exclusive
                onChange={handleLayoutModeChange}
                size="small"
                aria-label="layout mode"
              >
                <Tooltip title="Classic layout with clock panel" arrow>
                  <ToggleButton value="classic" aria-label="classic layout">
                    <FontAwesomeIcon icon={faGrip} />
                  </ToggleButton>
                </Tooltip>
                <Tooltip title="Sticky clock header" arrow>
                  <ToggleButton value="sticky" aria-label="sticky header layout">
                    <FontAwesomeIcon icon={faWindowMaximize} />
                  </ToggleButton>
                </Tooltip>
                <Tooltip title="Floating clock chip" arrow>
                  <ToggleButton value="floating" aria-label="floating clock layout">
                    <FontAwesomeIcon icon={faGaugeHigh} />
                  </ToggleButton>
                </Tooltip>
              </ToggleButtonGroup>
            )}

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
          </>
        }
      />

      {/* Conditional View Rendering */}
      {viewMode === 'narrative' ? (
        <Box sx={{ flex: 1, overflow: 'auto' }}>
          <NarrativeView
            exercise={exercise}
            injects={injects}
            observations={observations}
            displayTime={displayTime}
            elapsedTimeMs={elapsedTimeMs}
          />
        </Box>
      ) : (
        /* Controller View with Layout Options */
        <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
          {/* Floating Clock Chip (Option 2) */}
          {layoutMode === 'floating' && (
            <FloatingClockChip
              clockState={clockState}
              displayTime={displayTime}
              loading={clockLoading}
              injects={injects}
              readyToFireCount={readyToFireCount}
              canControl={canControl}
              onStart={startClock}
              onPause={pauseClock}
              onStop={handleStopClick}
              onReset={resetClock}
              isStarting={isStarting}
              isPausing={isPausing}
              isStopping={isStopping}
              isResetting={isResetting}
            />
          )}

          {/* Sticky Clock Header (Option 1) */}
          {layoutMode === 'sticky' && (
            <StickyClockHeader
              clockState={clockState}
              displayTime={displayTime}
              loading={clockLoading}
              injects={injects}
              readyToFireCount={readyToFireCount}
              canControl={canControl}
              onStart={startClock}
              onPause={pauseClock}
              onStop={handleStopClick}
              onReset={resetClock}
              isStarting={isStarting}
              isPausing={isPausing}
              isStopping={isStopping}
              isResetting={isResetting}
            />
          )}

          {/* Two-Column Layout with Independent Scrolling */}
          <Box
            sx={{
              display: 'flex',
              gap: 3,
              flex: 1,
              minHeight: 0, // Critical for nested flex scrolling
              flexDirection: { xs: 'column', md: 'row' },
            }}
          >
            {/* Left Column: Injects (and Clock for classic layout) */}
            <Box
              sx={{
                flex: { xs: 1, md: 2 },
                display: 'flex',
                flexDirection: 'column',
                minWidth: 0, // Prevent flex item overflow
                minHeight: 0, // Critical for nested flex scrolling
              }}
            >
              {/* Exercise Clock - Compact layout (non-scrolling header) */}
              {layoutMode === 'classic' && (
                <Paper sx={{ p: 2, mb: 2, flexShrink: 0 }}>
                  <Stack
                    direction="row"
                    alignItems="center"
                    justifyContent="space-between"
                    spacing={2}
                    flexWrap="wrap"
                  >
                    {/* Clock + Controls */}
                    <Stack direction="row" alignItems="center" spacing={2}>
                      <ClockDisplay
                        clockState={clockState}
                        displayTime={displayTime}
                        loading={clockLoading}
                        size="medium"
                      />
                      {canControl && (
                        <ClockControls
                          state={clockState?.state}
                          onStart={startClock}
                          onPause={pauseClock}
                          onStop={handleStopClick}
                          onReset={resetClock}
                          isStarting={isStarting}
                          isPausing={isPausing}
                          isStopping={isStopping}
                          isResetting={isResetting}
                          showReset
                        />
                      )}
                    </Stack>

                    {/* Progress + Badge */}
                    <Stack direction="row" alignItems="center" spacing={2} sx={{ flex: 1, minWidth: 200 }}>
                      <ExerciseProgress injects={injects} />
                      <ReadyToFireBadge count={readyToFireCount} />
                    </Stack>
                  </Stack>
                </Paper>
              )}

              {/* Inject List - Scrollable */}
              <Paper
                sx={{
                  p: 3,
                  flex: 1,
                  overflow: 'hidden',
                  display: 'flex',
                  flexDirection: 'column',
                  minHeight: 0,
                }}
              >
                <Typography variant="h6" gutterBottom sx={{ flexShrink: 0 }}>
                  Injects
                </Typography>
                {/* Visual notification when new injects become ready */}
                <Box sx={{ flexShrink: 0 }}>
                  <ReadyNotification readyCount={readyToFireCount} />
                </Box>
                <Box
                  sx={{
                    flex: 1,
                    overflowY: 'auto',
                    overflowX: 'hidden',
                    pr: `${CobraStyles.Scrollbar.ContentSpacing}px`,
                    ...CobraStyles.Scrollbar.Styling,
                  }}
                >
                  <InjectListByStatus
                    injects={injects}
                    exerciseStartTime={exerciseStartTime}
                    elapsedTimeMs={elapsedTimeMs}
                    canControl={canControl}
                    loading={injectsLoading}
                    error={injectsError}
                    onFire={fireInject}
                    onSkip={skipInject}
                    onReset={resetInject}
                  />
                </Box>
              </Paper>
            </Box>

            {/* Right Column: Observations - Scrollable */}
            <Box
              sx={{
                flex: 1,
                minWidth: 0,
                minHeight: 0, // Critical for nested flex scrolling
              }}
            >
              <Paper
                sx={{
                  p: 3,
                  height: '100%',
                  overflow: 'hidden',
                  display: 'flex',
                  flexDirection: 'column',
                  minHeight: 0,
                }}
              >
                <Stack
                  direction="row"
                  justifyContent="space-between"
                  alignItems="center"
                  sx={{ mb: 2, flexShrink: 0 }}
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

                {/* Observation Form - Fixed at top (when expanded) */}
                {observationsExpanded && showObservationForm && (
                  <Box sx={{ mb: 2, flexShrink: 0 }}>
                    <ObservationForm
                      injects={injects}
                      onSubmit={handleCreateObservation}
                      onCancel={() => setShowObservationForm(false)}
                      isSubmitting={isSubmittingObservation}
                    />
                    <Divider sx={{ my: 2 }} />
                  </Box>
                )}

                {/* Observation List - Scrollable (when expanded) */}
                {observationsExpanded && (
                  <Box
                    sx={{
                      flex: 1,
                      overflowY: 'auto',
                      overflowX: 'hidden',
                      pr: `${CobraStyles.Scrollbar.ContentSpacing}px`,
                      ...CobraStyles.Scrollbar.Styling,
                    }}
                  >
                    <ObservationList
                      observations={observations}
                      loading={observationsLoading}
                      error={observationsError}
                      canEdit={canAddObservations}
                      onDelete={handleDeleteObservation}
                      deletingId={deletingObservationId}
                    />
                  </Box>
                )}
              </Paper>
            </Box>
          </Box>
        </Box>
      )}

      {/* Stop Exercise Confirmation Dialog */}
      <ConfirmDialog
        open={showStopConfirm}
        title="Stop Exercise?"
        message="This will mark the exercise as Completed and end the active conduct phase. The exercise cannot be reopened once completed."
        confirmLabel="Stop Exercise"
        cancelLabel="Cancel"
        severity="warning"
        onConfirm={handleStopConfirmed}
        onCancel={() => setShowStopConfirm(false)}
        isConfirming={isStopping}
      />
    </Box>
  )
}

export default ExerciseConductPage
