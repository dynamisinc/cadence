/**
 * StickyClockHeader Component
 *
 * A compact, sticky header bar showing clock, story time, controls, phase, and ready count.
 * Stays visible at the top while scrolling through injects/observations.
 *
 * @module features/exercises
 * @see CLK-08 Display Story Time in Clock Area
 */

import { Box, Stack, Typography, LinearProgress } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlay, faPause, faStop, faRotateLeft, faSpinner } from '@fortawesome/free-solid-svg-icons'

import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import { ReadyToFireBadge } from '../../injects'
import { StoryTimeDisplay, useStoryTime } from '../../exercise-clock'
import type { ExerciseClockDto } from '../../exercise-clock/types'
import type { InjectDto } from '../../injects/types'
import type { ExerciseDto } from '../types'
import { InjectStatus, TimelineMode } from '../../../types'

interface StickyClockHeaderProps {
  /** Current exercise */
  exercise: ExerciseDto
  /** Current clock state */
  clockState: ExerciseClockDto | null
  /** Formatted display time (HH:MM:SS) */
  displayTime: string
  /** Elapsed time in milliseconds */
  elapsedTimeMs: number
  /** Is clock data loading? */
  loading?: boolean
  /** All injects for progress calculation */
  injects: InjectDto[]
  /** Number of ready-to-fire injects */
  readyToFireCount: number
  /** Can user control the clock? */
  canControl?: boolean
  /** Clock control handlers */
  onStart?: () => void
  onPause?: () => void
  onStop?: () => void
  onReset?: () => void
  /** Control button loading states */
  isStarting?: boolean
  isPausing?: boolean
  isStopping?: boolean
  isResetting?: boolean
}

export const StickyClockHeader = ({
  exercise,
  clockState,
  displayTime,
  elapsedTimeMs,
  loading = false,
  injects,
  readyToFireCount,
  canControl = false,
  onStart,
  onPause,
  onStop,
  onReset,
  isStarting = false,
  isPausing = false,
  isStopping = false,
  isResetting = false,
}: StickyClockHeaderProps) => {
  const isRunning = clockState?.state === 'Running'
  const isPaused = clockState?.state === 'Paused'
  const isStopped = clockState?.state === 'Stopped'

  // Get current inject for StoryOnly mode
  const currentInject = (() => {
    if (exercise.timelineMode !== TimelineMode.StoryOnly) return null

    // Get most recently fired inject, or first pending inject
    const firedInjects = injects.filter(i => i.status === InjectStatus.Fired && i.firedAt)
    if (firedInjects.length > 0) {
      firedInjects.sort((a, b) => {
        const aTime = a.firedAt ? new Date(a.firedAt).getTime() : 0
        const bTime = b.firedAt ? new Date(b.firedAt).getTime() : 0
        return bTime - aTime
      })
      return firedInjects[0]
    }

    // No fired injects - use first pending
    const firstPending = injects.find(i => i.status === InjectStatus.Pending)
    return firstPending ?? null
  })()

  // Calculate story time
  const { storyTime, formattedStoryTime, isStoryOnly } = useStoryTime({
    exercise,
    elapsedTimeMs,
    currentInject,
  })

  // Calculate progress
  const total = injects.length
  const completed = injects.filter(
    i => i.status === InjectStatus.Fired || i.status === InjectStatus.Skipped,
  ).length
  const percentage = total > 0 ? Math.round((completed / total) * 100) : 0

  // Get current phase from most recently fired inject
  const currentPhase = (() => {
    const firedInjects = injects.filter(
      i => i.status === InjectStatus.Fired && i.firedAt && i.phaseName,
    )
    if (firedInjects.length === 0) {
      const firstPending = injects.find(
        i => i.status === InjectStatus.Pending && i.phaseName,
      )
      return firstPending?.phaseName || null
    }
    firedInjects.sort((a, b) => {
      const aTime = a.firedAt ? new Date(a.firedAt).getTime() : 0
      const bTime = b.firedAt ? new Date(b.firedAt).getTime() : 0
      return bTime - aTime
    })
    return firedInjects[0]?.phaseName || null
  })()

  return (
    <Box
      sx={{
        position: 'sticky',
        top: 0,
        zIndex: 1100,
        bgcolor: 'background.paper',
        borderBottom: '1px solid',
        borderColor: 'divider',
        px: 2,
        py: 1.5,
        mb: 2,
        mx: -3, // Extend to edges of parent padding
        mt: -3,
        width: 'calc(100% + 48px)', // Compensate for negative margins
      }}
    >
      {/* Main row with clock and controls */}
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        {/* Clock Display */}
        <Stack direction="column" spacing={0.5}>
          <Stack direction="row" alignItems="center" spacing={2}>
            {/* Hide elapsed clock in StoryOnly mode */}
            {!isStoryOnly && (
              <Typography
                variant="h5"
                component="span"
                sx={{
                  fontFamily: 'monospace',
                  fontWeight: 600,
                  color: isRunning ? 'success.main' : isPaused ? 'warning.main' : 'text.primary',
                  minWidth: 100,
                }}
              >
                {loading ? '--:--:--' : displayTime}
              </Typography>
            )}

            {/* Compact Clock Controls */}
            {canControl && !isStoryOnly && (
              <Stack direction="row" spacing={0.5}>
                {(isStopped || isPaused) && (
                  <CobraPrimaryButton
                    size="small"
                    onClick={onStart}
                    disabled={isStarting}
                    sx={{ minWidth: 'auto', px: 1.5 }}
                  >
                    {isStarting ? (
                      <FontAwesomeIcon icon={faSpinner} spin />
                    ) : (
                      <FontAwesomeIcon icon={faPlay} />
                    )}
                  </CobraPrimaryButton>
                )}
                {isRunning && (
                  <CobraSecondaryButton
                    size="small"
                    onClick={onPause}
                    disabled={isPausing}
                    sx={{ minWidth: 'auto', px: 1.5 }}
                  >
                    {isPausing ? (
                      <FontAwesomeIcon icon={faSpinner} spin />
                    ) : (
                      <FontAwesomeIcon icon={faPause} />
                    )}
                  </CobraSecondaryButton>
                )}
                {(isRunning || isPaused) && (
                  <CobraSecondaryButton
                    size="small"
                    onClick={onStop}
                    disabled={isStopping}
                    sx={{ minWidth: 'auto', px: 1.5 }}
                    color="error"
                  >
                    {isStopping ? (
                      <FontAwesomeIcon icon={faSpinner} spin />
                    ) : (
                      <FontAwesomeIcon icon={faStop} />
                    )}
                  </CobraSecondaryButton>
                )}
                {isStopped && (
                  <CobraSecondaryButton
                    size="small"
                    onClick={onReset}
                    disabled={isResetting}
                    sx={{ minWidth: 'auto', px: 1.5 }}
                  >
                    {isResetting ? (
                      <FontAwesomeIcon icon={faSpinner} spin />
                    ) : (
                      <FontAwesomeIcon icon={faRotateLeft} />
                    )}
                  </CobraSecondaryButton>
                )}
              </Stack>
            )}
          </Stack>

          {/* Story Time Display */}
          <StoryTimeDisplay
            storyTime={storyTime}
            formattedStoryTime={formattedStoryTime}
            isStoryOnly={isStoryOnly}
            timelineMode={exercise.timelineMode}
            timeScale={exercise.timeScale}
          />
        </Stack>

        {/* Phase + Progress */}
        <Stack direction="row" alignItems="center" spacing={3} sx={{ flex: 1, mx: 3 }}>
          {currentPhase && (
            <Typography variant="body2" color="text.secondary" noWrap>
              {currentPhase}
            </Typography>
          )}
          <Box sx={{ flex: 1, maxWidth: 200 }}>
            <LinearProgress
              variant="determinate"
              value={percentage}
              sx={{ height: 6, borderRadius: 3 }}
            />
          </Box>
          <Typography variant="caption" color="text.secondary" noWrap>
            {completed}/{total} injects
          </Typography>
        </Stack>

        {/* Ready Badge */}
        <ReadyToFireBadge count={readyToFireCount} />
      </Stack>
    </Box>
  )
}

export default StickyClockHeader
