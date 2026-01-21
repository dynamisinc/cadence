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

import { useState, useCallback, useMemo, useEffect, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { toast } from 'react-toastify'
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
import {
  ExerciseHeader,
  NarrativeView,
  StickyClockHeader,
  FloatingClockChip,
  ClockDrivenConductView,
  FacilitatorPacedConductView,
} from '../components'
import { CobraLinkButton, CobraPrimaryButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useBreadcrumbs, useConnectivity } from '../../../core/contexts'
import { useExerciseSignalR } from '../../../shared/hooks'
import { ExerciseStatus, InjectStatus, ExerciseClockState, DeliveryMode } from '../../../types'

// Feature imports
import { ClockDisplay, ClockControls, ExerciseProgress, useExerciseClock, clockQueryKey, parseElapsedTime, formatElapsedTime } from '../../exercise-clock'
import { ReadyToFireBadge, ReadyNotification, useInjects, injectKeys, calculateScheduledOffset } from '../../injects'
import {
  ObservationForm,
  ObservationList,
  useObservations,
  observationsQueryKey,
} from '../../observations'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
import { useObjectiveSummaries } from '../../objectives/hooks'
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
    fireInject,
    skipInject,
  } = useInjects(exerciseId!)
  const {
    observations,
    loading: observationsLoading,
    error: observationsError,
    createObservation,
    updateObservation,
    deleteObservation,
  } = useObservations(exerciseId!)
  const { summaries: _objectives } = useObjectiveSummaries(exerciseId!)

  // UI state
  const [showObservationForm, setShowObservationForm] = useState(false)
  const [editingObservation, setEditingObservation] = useState<ObservationDto | null>(null)
  const [isSubmittingObservation, setIsSubmittingObservation] = useState(false)
  const [deletingObservationId, setDeletingObservationId] = useState<string | null>(null)
  const [observationsExpanded, setObservationsExpanded] = useState(true)
  const [showStopConfirm, setShowStopConfirm] = useState(false)
  const [openInjectId, setOpenInjectId] = useState<string | null>(null)

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
      queryClient.setQueryData<ObservationDto[]>(observationsQueryKey(exerciseId!), old => {
        // Check if observation already exists (from optimistic update or prior SignalR)
        // to avoid duplicates when the creator receives their own broadcast
        const existing = old?.find(o => o.id === observation.id)
        if (existing) {
          // Update existing observation with server data
          return old!.map(o => (o.id === observation.id ? observation : o))
        }
        // New observation from another client
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

  // Track clock state and elapsed time before disconnection to detect changes during offline period
  const previousClockStateRef = useRef<ExerciseClockState | null>(null)
  const previousElapsedTimeMsRef = useRef<number>(0)
  const disconnectedAtRef = useRef<number | null>(null)

  // Update clock state ref whenever it changes
  useEffect(() => {
    if (clockState?.state) {
      previousClockStateRef.current = clockState.state
    }
  }, [clockState?.state])

  // Handle SignalR reconnection - refresh state and notify user of changes
  const handleReconnected = useCallback(async () => {
    const previousState = previousClockStateRef.current
    const previousElapsedMs = previousElapsedTimeMsRef.current
    const wasDisconnectedAt = disconnectedAtRef.current

    // Clear the disconnected timestamp
    disconnectedAtRef.current = null

    // Refresh clock and inject data - use refetchQueries to wait for completion
    await Promise.all([
      queryClient.refetchQueries({ queryKey: clockQueryKey(exerciseId!) }),
      queryClient.refetchQueries({ queryKey: injectKeys.all(exerciseId!) }),
      queryClient.refetchQueries({ queryKey: observationsQueryKey(exerciseId!) }),
    ])

    // Now get the fresh data
    const currentClockData = queryClient.getQueryData<ExerciseClockDto>(clockQueryKey(exerciseId!))
    const currentState = currentClockData?.state
    const currentElapsedMs = currentClockData?.elapsedTime
      ? parseElapsedTime(currentClockData.elapsedTime)
      : 0

    // Calculate time delta for informative message
    const timeDeltaMs = currentElapsedMs - previousElapsedMs
    const timeDeltaFormatted = formatElapsedTime(Math.abs(timeDeltaMs))

    // Calculate how long we were disconnected
    const disconnectedDuration = wasDisconnectedAt ? Date.now() - wasDisconnectedAt : 0
    // Only show notification if offline > 2 seconds
    const wasDisconnectedLongEnough = disconnectedDuration > 2000

    if (!wasDisconnectedLongEnough) {
      // Brief disconnection, just update refs silently
      if (currentState) {
        previousClockStateRef.current = currentState
      }
      previousElapsedTimeMsRef.current = currentElapsedMs
      return
    }

    if (previousState && currentState && previousState !== currentState) {
      // Clock state changed while offline
      if (currentState === ExerciseClockState.Running) {
        const currentTimeStr = formatElapsedTime(currentElapsedMs)
        const message = previousState === ExerciseClockState.Stopped
          ? `Exercise clock was started while you were offline. Current time: ${currentTimeStr}`
          : 'Exercise clock resumed while you were offline. ' +
            `Clock jumped forward by ${timeDeltaFormatted}. Current time: ${currentTimeStr}`
        toast.warning(message, { autoClose: false })
      } else if (
        currentState === ExerciseClockState.Paused &&
        previousState === ExerciseClockState.Running
      ) {
        toast.warning(
          `Exercise clock was paused while you were offline at ${formatElapsedTime(currentElapsedMs)}.`,
          { autoClose: 8000 },
        )
      } else if (currentState === ExerciseClockState.Stopped) {
        toast.warning(
          'Exercise was stopped while you were offline. The exercise has ended.',
          { autoClose: false },
        )
      }
    } else if (currentState === ExerciseClockState.Running && timeDeltaMs > 5000) {
      // Same state but significant time jump (>5 seconds) while offline
      toast.warning(
        `Clock synchronized. Time jumped forward by ${timeDeltaFormatted}. Current time: ${formatElapsedTime(currentElapsedMs)}`,
        { autoClose: false },
      )
    } else if (!previousState && currentState === ExerciseClockState.Running) {
      // First time connecting and clock is already running
      toast.info(
        `Exercise clock is running. Current time: ${formatElapsedTime(currentElapsedMs)}`,
        { autoClose: 5000 },
      )
    }

    // Update the refs with the current state
    if (currentState) {
      previousClockStateRef.current = currentState
    }
    previousElapsedTimeMsRef.current = currentElapsedMs
  }, [exerciseId, queryClient])

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
    onObservationUpdated: handleObservationUpdated,
    onObservationDeleted: handleObservationDeleted,
    onReconnected: handleReconnected,
    enabled: !!exerciseId,
  })

  // Sync SignalR state with global connectivity context
  const { setSignalRState, setIsInExercise } = useConnectivity()
  const previousConnectionStateRef = useRef<typeof connectionState | null>(null)

  useEffect(() => {
    // Mark that we're in exercise conduct mode
    setIsInExercise(true)
    return () => {
      setIsInExercise(false)
      setSignalRState(null)
    }
  }, [setIsInExercise, setSignalRState])

  useEffect(() => {
    // Report SignalR connection state to global context
    setSignalRState(connectionState)

    const wasConnected = previousConnectionStateRef.current === 'connected'
    const isNowDisconnected = connectionState === 'disconnected' ||
                              connectionState === 'error' ||
                              connectionState === 'reconnecting'
    const wasDisconnected = previousConnectionStateRef.current === 'disconnected' ||
                            previousConnectionStateRef.current === 'error' ||
                            previousConnectionStateRef.current === 'reconnecting'
    const isNowConnected = connectionState === 'connected'

    // Capture elapsed time when we go offline
    if (wasConnected && isNowDisconnected) {
      previousElapsedTimeMsRef.current = elapsedTimeMs
      disconnectedAtRef.current = Date.now()
    }

    // Trigger reconnection handler when transitioning from disconnected/error to connected
    // This handles cases where the SignalR auto-reconnect doesn't fire the onreconnected callback
    if (wasDisconnected && isNowConnected && previousConnectionStateRef.current !== null) {
      // Connection was restored - trigger the reconnection handler
      handleReconnected()
    }

    previousConnectionStateRef.current = connectionState
  }, [connectionState, setSignalRState, handleReconnected, elapsedTimeMs])

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
  const handleSubmitObservation = async (data: Parameters<typeof createObservation>[0]) => {
    setIsSubmittingObservation(true)
    try {
      if (editingObservation) {
        await updateObservation(editingObservation.id, data)
        setEditingObservation(null)
      } else {
        await createObservation(data)
      }
      setShowObservationForm(false)
    } finally {
      setIsSubmittingObservation(false)
    }
  }

  const handleEditObservation = (observation: ObservationDto) => {
    setEditingObservation(observation)
    setShowObservationForm(true)
  }

  const handleCancelObservationForm = () => {
    setShowObservationForm(false)
    setEditingObservation(null)
  }

  const handleDeleteObservation = async (observationId: string) => {
    setDeletingObservationId(observationId)
    try {
      await deleteObservation(observationId)
    } finally {
      setDeletingObservationId(null)
    }
  }

  // Handle clicking inject link in observation
  const handleInjectClick = (injectId: string) => {
    setOpenInjectId(injectId)
  }

  // Stop clock with confirmation
  const handleStopClick = () => {
    setShowStopConfirm(true)
  }

  const handleStopConfirmed = async () => {
    await stopClock()
    setShowStopConfirm(false)
  }

  // Handle jump to inject (facilitator-paced mode)
  // Skips all injects between current and target, then the target becomes current
  const handleJumpTo = async (_targetInjectId: string, skipInjectIds: string[]) => {
    // Skip all injects in the skipInjectIds list
    for (const injectId of skipInjectIds) {
      await skipInject(injectId, { reason: 'Jumped to later inject' })
    }
    // The target inject will naturally become the current inject
    // since all prior pending injects have been skipped
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
              clockState={clockState ?? null}
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
          {layoutMode === 'sticky' && exercise && (
            <StickyClockHeader
              exercise={exercise}
              clockState={clockState ?? null}
              displayTime={displayTime}
              elapsedTimeMs={elapsedTimeMs}
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
                {exercise.deliveryMode !== DeliveryMode.ClockDriven && (
                  <Box sx={{ flexShrink: 0 }}>
                    <ReadyNotification readyCount={readyToFireCount} />
                  </Box>
                )}
                <Box
                  sx={{
                    flex: 1,
                    overflowY: 'auto',
                    overflowX: 'hidden',
                    pr: `${CobraStyles.Scrollbar.ContentSpacing}px`,
                    ...CobraStyles.Scrollbar.Styling,
                  }}
                >
                  {/* Clock-Driven View */}
                  {exercise.deliveryMode === DeliveryMode.ClockDriven ? (
                    <ClockDrivenConductView
                      exercise={exercise}
                      injects={injects}
                      elapsedTimeMs={elapsedTimeMs}
                      canControl={canControl}
                      isSubmitting={false}
                      onFire={async (id) => { await fireInject(id) }}
                      onSkip={async (id, req) => { await skipInject(id, req) }}
                      openInjectId={openInjectId}
                      onDrawerClose={() => setOpenInjectId(null)}
                    />
                  ) : (
                    /* Facilitator-Paced View - Current Inject focused, no clock */
                    <FacilitatorPacedConductView
                      exercise={exercise}
                      injects={injects}
                      canControl={canControl}
                      isSubmitting={false}
                      isLoading={injectsLoading}
                      onFire={async (id) => { await fireInject(id) }}
                      onSkip={async (id, req) => { await skipInject(id, req) }}
                      onJumpTo={handleJumpTo}
                    />
                  )}
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
                      initialValues={
                        editingObservation
                          ? {
                            rating: editingObservation.rating!,
                            content: editingObservation.content,
                            recommendation: editingObservation.recommendation ?? undefined,
                            injectId: editingObservation.injectId ?? undefined,
                          }
                          : undefined
                      }
                      onSubmit={handleSubmitObservation}
                      onCancel={handleCancelObservationForm}
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
                      onEdit={handleEditObservation}
                      onDelete={handleDeleteObservation}
                      deletingId={deletingObservationId}
                      onInjectClick={handleInjectClick}
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
