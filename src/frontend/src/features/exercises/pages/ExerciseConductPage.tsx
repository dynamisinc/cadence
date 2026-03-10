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
  IconButton,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faHome,
  faGaugeHigh,
  faBookOpen,
  faGrip,
  faWindowMaximize,
  faGear,
  faDesktop,
} from '@fortawesome/free-solid-svg-icons'

import { useExercise, useExerciseSettings } from '../hooks'
import {
  ExerciseHeader,
  NarrativeView,
  StickyClockHeader,
  FloatingClockChip,
  ClockDrivenConductView,
  FacilitatorPacedConductView,
  ExerciseSettingsDialog,
} from '../components'
import { ObservationPanel } from '../components/ObservationPanel'
import { CobraLinkButton, CobraPrimaryButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useBreadcrumbs } from '../../../core/contexts'
import { ExerciseStatus, ExerciseClockState, InjectStatus, DeliveryMode } from '../../../types'
import { EffectiveRoleBadge, useExerciseRole } from '@/features/auth'

// Feature imports
import {
  ClockDisplay,
  ClockControls,
  ExerciseProgress,
  useExerciseClock,
  ClockControlConfirmationDialog,
  SetClockTimeDialog,
  type ClockAction,
} from '../../exercise-clock'
import {
  ReadyToFireBadge,
  ReadyNotification,
  useInjects,
  calculateScheduledOffset,
  FireConfirmationDialog,
  SkipConfirmationDialog,
} from '../../injects'
import { useObservations } from '../../observations'
import type { ObservationDto, CreateObservationRequest } from '../../observations/types'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
import { HelpTooltip, PageHeader } from '@/shared/components'
import { QuickPhotoFab } from '../../photos'

// Extracted hooks
import { useFireSkipConfirmation } from '../hooks/useFireSkipConfirmation'
import { useExerciseConductSignalR } from '../hooks/useExerciseConductSignalR'

export const ExerciseConductPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const navigate = useNavigate()

  // Core data hooks
  const { exercise, loading: exerciseLoading, error: exerciseError } = useExercise(exerciseId)
  const { effectiveRole, can } = useExerciseRole(exerciseId ?? null)
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
    setClockTime,
    maxDurationMs,
    isStarting,
    isPausing,
    isStopping,
    isResetting,
    isSettingTime,
  } = useExerciseClock(exerciseId!)
  const {
    injects,
    loading: injectsLoading,
    fireInject,
    skipInject,
    resetInject,
  } = useInjects(exerciseId!)

  // Exercise settings for confirmation dialogs
  const {
    confirmFireInject,
    confirmSkipInject,
    confirmClockControl,
  } = useExerciseSettings(exerciseId)

  // Observations — kept at page level so both NarrativeView and ObservationPanel share the same data
  const {
    observations,
    loading: observationsLoading,
    error: observationsError,
    createObservation,
    updateObservation,
    deleteObservation,
  } = useObservations(exerciseId!)

  // UI state
  const [showStopConfirm, setShowStopConfirm] = useState(false)
  const [openInjectId, setOpenInjectId] = useState<string | null>(null)
  const [settingsDialogOpen, setSettingsDialogOpen] = useState(false)
  const [showSetTimeDialog, setShowSetTimeDialog] = useState(false)
  const [clockConfirmAction, setClockConfirmAction] = useState<ClockAction | null>(null)

  // Pre-selected inject ID for observations (set when adding from inject drawer)
  const [preSelectedInjectId, setPreSelectedInjectId] = useState<string | null>(null)

  // Observation panel state — managed here so ObservationPanel can be a controlled component
  const [showObservationForm, setShowObservationForm] = useState(false)
  const [editingObservation, setEditingObservation] = useState<ObservationDto | null>(null)
  const [deletingObservationId, setDeletingObservationId] = useState<string | null>(null)

  // Observation mutation handlers — passed down to ObservationPanel
  const handleSubmitObservation = async (data: CreateObservationRequest) => {
    if (editingObservation) {
      await updateObservation(editingObservation.id, data)
    } else {
      await createObservation(data)
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

  // User-level "don't ask again" flag for clock (localStorage-persisted per exercise)
  const [skipClockConfirmation, setSkipClockConfirmation] = useState(() => {
    if (!exerciseId) return false
    return localStorage.getItem(`cadence:skipConfirmation:${exerciseId}:clock`) === 'true'
  })

  const handleSkipClockConfirmation = useCallback(() => {
    setSkipClockConfirmation(true)
    if (exerciseId) {
      localStorage.setItem(`cadence:skipConfirmation:${exerciseId}:clock`, 'true')
    }
  }, [exerciseId])

  // Fire/skip confirmation state machine (extracted hook)
  const {
    fireConfirmInject,
    skipConfirmInject,
    pendingSkipInjectId,
    handleFireWithConfirmation,
    handleFireConfirmed,
    handleFireCancelled,
    handleSkipWithConfirmation,
    handleSkipPreConfirmation,
    handleSkipConfirmProceed,
    handleSkipConfirmCancelled,
    handlePendingSkipClear,
    handleSkipFireConfirmation,
    handleSkipSkipConfirmation,
  } = useFireSkipConfirmation({
    exerciseId,
    confirmFireInject,
    confirmSkipInject,
    fireInject,
    skipInject,
    injects,
  })

  // SignalR real-time subscriptions + connectivity sync (extracted hook)
  useExerciseConductSignalR({
    exerciseId: exerciseId!,
    clockState,
    elapsedTimeMs,
  })

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

  // Permission checks using role-based access control
  const canControl = useMemo(() => {
    // Controllers and Exercise Directors can control injects/clock
    return exercise?.status === ExerciseStatus.Active && can('fire_inject')
  }, [exercise, can])

  const canSetTime = useMemo(() => {
    // Exercise Directors and above can set clock time when paused
    return can('set_clock_time')
  }, [can])

  const canAddObservations = useMemo(() => {
    // Evaluators can add observations during active exercises
    return exercise?.status === ExerciseStatus.Active && can('add_observation')
  }, [exercise, can])

  // Calculate ready-to-fire count for badge
  const readyToFireCount = useMemo(() => {
    if (!injects || injects.length === 0) return 0
    return injects.filter(inject => {
      if (inject.status !== InjectStatus.Draft) return false
      const offsetMs = calculateScheduledOffset(inject.scheduledTime, exerciseStartTime)
      return offsetMs <= elapsedTimeMs
    }).length
  }, [injects, exerciseStartTime, elapsedTimeMs])

  // Handle clicking inject link in observation list
  const handleInjectClick = (injectId: string) => {
    setOpenInjectId(injectId)
  }

  // Handle adding observation from inject drawer — pre-select the inject and open the form
  const handleAddObservationForInject = (injectId: string) => {
    setPreSelectedInjectId(injectId)
    setShowObservationForm(true)
    setOpenInjectId(null) // Close the drawer
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
    for (const injectId of skipInjectIds) {
      await skipInject(injectId, { reason: 'Jumped to later inject' })
    }
  }

  // =========================================================================
  // Clock control with optional confirmation
  // =========================================================================

  const handleClockAction = useCallback(
    async (action: ClockAction) => {
      // Stop always shows confirmation (it's destructive)
      if (action === 'stop') {
        setShowStopConfirm(true)
        return
      }

      if (confirmClockControl && !skipClockConfirmation) {
        setClockConfirmAction(action)
      } else {
        if (action === 'start' || action === 'resume') {
          await startClock()
        } else if (action === 'pause') {
          await pauseClock()
        }
      }
    },
    [confirmClockControl, skipClockConfirmation, startClock, pauseClock],
  )

  const handleClockConfirmed = useCallback(async () => {
    if (clockConfirmAction === 'start' || clockConfirmAction === 'resume') {
      await startClock()
    } else if (clockConfirmAction === 'pause') {
      await pauseClock()
    }
    setClockConfirmAction(null)
  }, [clockConfirmAction, startClock, pauseClock])

  const handleClockCancelled = useCallback(() => {
    setClockConfirmAction(null)
  }, [])

  const handleStartWithConfirmation = useCallback(() => {
    const action = clockState?.state === ExerciseClockState.Paused ? 'resume' : 'start'
    handleClockAction(action)
  }, [clockState?.state, handleClockAction])

  const handlePauseWithConfirmation = useCallback(() => {
    handleClockAction('pause')
  }, [handleClockAction])

  const handleSetTimeConfirm = useCallback(async (elapsedTime: string) => {
    await setClockTime(elapsedTime)
    setShowSetTimeDialog(false)
  }, [setClockTime])

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
      {/* Page Header */}
      <PageHeader
        title="Exercise Conduct"
        icon={faDesktop}
        subtitle={exercise ? `Conduct ${exercise.name}` : undefined}
        chips={<HelpTooltip helpKey="conduct.fire" exerciseRole={effectiveRole ?? undefined} />}
        mb={2}
      />

      {/* Exercise Info Header */}
      <ExerciseHeader
        exercise={exercise}
        marginBottom={3}
        actions={
          <>
            {/* User's Exercise Role Badge */}
            <EffectiveRoleBadge exerciseId={exerciseId ?? null} showOverride />

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

            {/* Connection status is shown in the AppHeader via ConnectionStatusIndicator */}

            {/* Exercise Settings */}
            <Tooltip title="Exercise Settings" arrow>
              <IconButton
                onClick={() => setSettingsDialogOpen(true)}
                size="small"
                aria-label="Exercise settings"
              >
                <FontAwesomeIcon icon={faGear} />
              </IconButton>
            </Tooltip>

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
              onStart={handleStartWithConfirmation}
              onPause={handlePauseWithConfirmation}
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
              onStart={handleStartWithConfirmation}
              onPause={handlePauseWithConfirmation}
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
                          onStart={handleStartWithConfirmation}
                          onPause={handlePauseWithConfirmation}
                          onStop={handleStopClick}
                          onReset={resetClock}
                          onSetTime={() => setShowSetTimeDialog(true)}
                          canSetTime={canSetTime}
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
                      canAddObservation={canAddObservations}
                      isSubmitting={false}
                      onFire={handleFireWithConfirmation}
                      onSkip={handleSkipWithConfirmation}
                      onReset={(id: string) => { resetInject(id) }}
                      onSkipPreConfirmation={handleSkipPreConfirmation}
                      pendingSkipInjectId={pendingSkipInjectId}
                      onPendingSkipClear={handlePendingSkipClear}
                      openInjectId={openInjectId}
                      onDrawerClose={() => setOpenInjectId(null)}
                      onAddObservation={handleAddObservationForInject}
                    />
                  ) : (
                    /* Facilitator-Paced View - Current Inject focused, no clock */
                    <FacilitatorPacedConductView
                      exercise={exercise}
                      injects={injects}
                      canControl={canControl}
                      isSubmitting={false}
                      isLoading={injectsLoading}
                      onFire={handleFireWithConfirmation}
                      onSkip={handleSkipWithConfirmation}
                      onReset={(id: string) => { resetInject(id) }}
                      onSkipPreConfirmation={handleSkipPreConfirmation}
                      pendingSkipInjectId={pendingSkipInjectId}
                      onPendingSkipClear={handlePendingSkipClear}
                      onJumpTo={handleJumpTo}
                    />
                  )}
                </Box>
              </Paper>
            </Box>

            {/* Right Column: Observations Panel */}
            <Box
              sx={{
                flex: 1,
                minWidth: 0,
                minHeight: 0, // Critical for nested flex scrolling
              }}
            >
              <ObservationPanel
                exerciseId={exerciseId!}
                canAddObservations={canAddObservations}
                injects={injects}
                observations={observations}
                observationsLoading={observationsLoading}
                observationsError={observationsError}
                displayTime={displayTime}
                onInjectClick={handleInjectClick}
                preSelectedInjectId={preSelectedInjectId}
                onClearPreSelectedInjectId={() => setPreSelectedInjectId(null)}
                onSubmitObservation={handleSubmitObservation}
                onDeleteObservation={handleDeleteObservation}
                deletingObservationId={deletingObservationId}
                editingObservation={editingObservation}
                onSetEditingObservation={setEditingObservation}
                showObservationForm={showObservationForm}
                onShowObservationFormChange={setShowObservationForm}
              />
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

      {/* Fire Inject Confirmation Dialog */}
      <FireConfirmationDialog
        open={!!fireConfirmInject}
        inject={fireConfirmInject}
        onConfirm={handleFireConfirmed}
        onCancel={handleFireCancelled}
        onDontAskAgain={handleSkipFireConfirmation}
      />

      {/* Skip Inject Confirmation Dialog (pre-confirmation before reason) */}
      <SkipConfirmationDialog
        open={!!skipConfirmInject}
        inject={skipConfirmInject}
        onConfirm={handleSkipConfirmProceed}
        onCancel={handleSkipConfirmCancelled}
        onDontAskAgain={handleSkipSkipConfirmation}
      />

      {/* Clock Control Confirmation Dialog */}
      <ClockControlConfirmationDialog
        open={!!clockConfirmAction}
        action={clockConfirmAction}
        currentTime={displayTime}
        onConfirm={handleClockConfirmed}
        onCancel={handleClockCancelled}
        onDontAskAgain={handleSkipClockConfirmation}
      />

      {/* Set Clock Time Dialog */}
      <SetClockTimeDialog
        open={showSetTimeDialog}
        currentTime={displayTime}
        maxDurationHours={maxDurationMs / (60 * 60 * 1000)}
        onConfirm={handleSetTimeConfirm}
        onCancel={() => setShowSetTimeDialog(false)}
        isLoading={isSettingTime}
      />

      {/* Exercise Settings Dialog */}
      <ExerciseSettingsDialog
        open={settingsDialogOpen}
        exerciseId={exerciseId!}
        exerciseName={exercise.name}
        onClose={() => setSettingsDialogOpen(false)}
      />

      {/* Quick Photo FAB */}
      <QuickPhotoFab exerciseId={exerciseId!} scenarioTime={displayTime} />
    </Box>
  )
}
