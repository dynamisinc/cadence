/**
 * FloatingClockChip Component
 *
 * A compact floating clock display in the corner.
 * Expands on click/hover to show controls.
 */

import { useState } from 'react'
import { Box, Stack, Typography, Collapse, Paper, ClickAwayListener } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlay,
  faPause,
  faStop,
  faRotateLeft,
  faSpinner,
  faClock,
  faChevronDown,
  faChevronUp,
} from '@fortawesome/free-solid-svg-icons'

import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import { ReadyToFireBadge } from '../../injects'
import type { ExerciseClockDto } from '../../exercise-clock/types'
import type { InjectDto } from '../../injects/types'
import { InjectStatus } from '../../../types'

interface FloatingClockChipProps {
  /** Current clock state */
  clockState: ExerciseClockDto | null
  /** Formatted display time (HH:MM:SS) */
  displayTime: string
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

export const FloatingClockChip = ({
  clockState,
  displayTime,
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
}: FloatingClockChipProps) => {
  const [expanded, setExpanded] = useState(false)

  const isRunning = clockState?.state === 'Running'
  const isPaused = clockState?.state === 'Paused'
  const isStopped = clockState?.state === 'Stopped'

  // Calculate progress
  const total = injects.length
  const completed = injects.filter(
    i => i.status === InjectStatus.Fired || i.status === InjectStatus.Skipped,
  ).length

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

  const getStatusColor = () => {
    if (isRunning) return 'success.main'
    if (isPaused) return 'warning.main'
    return 'text.primary'
  }

  return (
    <ClickAwayListener onClickAway={() => setExpanded(false)}>
      <Box
        sx={{
          position: 'fixed',
          top: 80, // Below app header
          right: 24,
          zIndex: 1200,
        }}
      >
        <Paper
          elevation={4}
          sx={{
            borderRadius: 2,
            overflow: 'hidden',
            minWidth: expanded ? 280 : 'auto',
            transition: 'min-width 0.2s ease',
          }}
        >
          {/* Compact Clock Display - Always Visible */}
          <Box
            onClick={() => setExpanded(!expanded)}
            sx={{
              px: 2,
              py: 1,
              cursor: 'pointer',
              bgcolor: 'background.paper',
              '&:hover': {
                bgcolor: 'action.hover',
              },
            }}
          >
            <Stack direction="row" alignItems="center" spacing={1.5}>
              <FontAwesomeIcon
                icon={faClock}
                style={{ color: isRunning ? '#2e7d32' : isPaused ? '#ed6c02' : undefined }}
              />
              <Typography
                variant="h6"
                component="span"
                sx={{
                  fontFamily: 'monospace',
                  fontWeight: 600,
                  color: getStatusColor(),
                }}
              >
                {loading ? '--:--:--' : displayTime}
              </Typography>
              {readyToFireCount > 0 && (
                <ReadyToFireBadge count={readyToFireCount} />
              )}
              <FontAwesomeIcon
                icon={expanded ? faChevronUp : faChevronDown}
                size="sm"
                style={{ opacity: 0.5 }}
              />
            </Stack>
          </Box>

          {/* Expanded Controls Panel */}
          <Collapse in={expanded}>
            <Box
              sx={{
                px: 2,
                py: 1.5,
                borderTop: '1px solid',
                borderColor: 'divider',
                bgcolor: 'grey.50',
              }}
            >
              <Stack spacing={1.5}>
                {/* Phase & Progress */}
                {currentPhase && (
                  <Typography variant="body2" color="text.secondary">
                    Phase: {currentPhase}
                  </Typography>
                )}
                <Typography variant="body2" color="text.secondary">
                  Progress: {completed} of {total} injects
                </Typography>

                {/* Clock Controls */}
                {canControl && (
                  <Stack direction="row" spacing={1} justifyContent="center">
                    {(isStopped || isPaused) && (
                      <CobraPrimaryButton
                        size="small"
                        onClick={onStart}
                        disabled={isStarting}
                        startIcon={
                          isStarting ? (
                            <FontAwesomeIcon icon={faSpinner} spin />
                          ) : (
                            <FontAwesomeIcon icon={faPlay} />
                          )
                        }
                      >
                        {isPaused ? 'Resume' : 'Start'}
                      </CobraPrimaryButton>
                    )}
                    {isRunning && (
                      <CobraSecondaryButton
                        size="small"
                        onClick={onPause}
                        disabled={isPausing}
                        startIcon={
                          isPausing ? (
                            <FontAwesomeIcon icon={faSpinner} spin />
                          ) : (
                            <FontAwesomeIcon icon={faPause} />
                          )
                        }
                      >
                        Pause
                      </CobraSecondaryButton>
                    )}
                    {(isRunning || isPaused) && (
                      <CobraSecondaryButton
                        size="small"
                        onClick={onStop}
                        disabled={isStopping}
                        color="error"
                        startIcon={
                          isStopping ? (
                            <FontAwesomeIcon icon={faSpinner} spin />
                          ) : (
                            <FontAwesomeIcon icon={faStop} />
                          )
                        }
                      >
                        Stop
                      </CobraSecondaryButton>
                    )}
                    {isStopped && (
                      <CobraSecondaryButton
                        size="small"
                        onClick={onReset}
                        disabled={isResetting}
                        startIcon={
                          isResetting ? (
                            <FontAwesomeIcon icon={faSpinner} spin />
                          ) : (
                            <FontAwesomeIcon icon={faRotateLeft} />
                          )
                        }
                      >
                        Reset
                      </CobraSecondaryButton>
                    )}
                  </Stack>
                )}
              </Stack>
            </Box>
          </Collapse>
        </Paper>
      </Box>
    </ClickAwayListener>
  )
}

export default FloatingClockChip
