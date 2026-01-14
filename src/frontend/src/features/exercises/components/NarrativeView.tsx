/**
 * NarrativeView Component
 *
 * Observer-friendly, story-centric read-only view during exercise conduct.
 * Shows the exercise narrative without control buttons.
 */

import { useMemo } from 'react'
import { Box, Paper, Stack, Typography, Divider } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faClock, faBookOpen, faBullhorn, faForward, faClipboardList } from '@fortawesome/free-solid-svg-icons'

import { InjectStatus } from '../../../types'
import type { InjectDto } from '../../injects/types'
import type { ObservationDto } from '../../observations/types'
import type { ExerciseDto } from '../types'
import { RatingBadge } from '../../observations/components/RatingBadge'
import {
  generateStorySoFar,
  generateCurrentEvent,
  generateUpcomingPreview,
} from '../utils/narrativeGenerator'

interface NarrativeViewProps {
  /** The exercise being conducted */
  exercise: ExerciseDto
  /** All injects in the exercise */
  injects: InjectDto[]
  /** Observations recorded during the exercise */
  observations: ObservationDto[]
  /** Current display time string (HH:MM:SS) */
  displayTime: string
  /** Elapsed time in milliseconds */
  elapsedTimeMs: number
}

export const NarrativeView = ({
  exercise,
  injects,
  observations,
  displayTime,
}: NarrativeViewProps) => {
  // Calculate current phase from most recently fired inject
  const currentPhase = useMemo(() => {
    const firedInjects = injects.filter(
      i => i.status === InjectStatus.Fired && i.firedAt && i.phaseName,
    )
    if (firedInjects.length === 0) {
      // Use first pending inject's phase
      const firstPending = injects.find(
        i => i.status === InjectStatus.Pending && i.phaseName,
      )
      return firstPending?.phaseName || null
    }
    // Sort by firedAt descending and get most recent
    firedInjects.sort((a, b) => {
      const aTime = a.firedAt ? new Date(a.firedAt).getTime() : 0
      const bTime = b.firedAt ? new Date(b.firedAt).getTime() : 0
      return bTime - aTime
    })
    return firedInjects[0]?.phaseName || null
  }, [injects])

  // Generate narrative sections
  const storySoFar = useMemo(() => generateStorySoFar(injects), [injects])

  const nextPendingInject = useMemo(() => {
    const pending = injects.filter(i => i.status === InjectStatus.Pending)
    pending.sort((a, b) => a.sequence - b.sequence)
    return pending[0] || null
  }, [injects])

  const currentEvent = useMemo(
    () => generateCurrentEvent(nextPendingInject),
    [nextPendingInject],
  )

  const upcomingInjects = useMemo(() => {
    const pending = injects.filter(i => i.status === InjectStatus.Pending)
    pending.sort((a, b) => a.sequence - b.sequence)
    return pending
  }, [injects])

  const upcomingPreviews = useMemo(
    () => generateUpcomingPreview(upcomingInjects),
    [upcomingInjects],
  )

  return (
    <Box>
      {/* Header with exercise title and clock */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Box>
            <Typography variant="h5" component="h1">
              {exercise.name}
            </Typography>
            {currentPhase && (
              <Typography variant="subtitle1" color="text.secondary">
                Current Phase: {currentPhase}
              </Typography>
            )}
          </Box>
          <Stack alignItems="center" spacing={0.5}>
            <FontAwesomeIcon icon={faClock} size="lg" />
            <Typography variant="h4" fontFamily="monospace">
              {displayTime}
            </Typography>
          </Stack>
        </Stack>
      </Paper>

      {/* The Story So Far */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Stack direction="row" spacing={1} alignItems="center" mb={2}>
          <FontAwesomeIcon icon={faBookOpen} />
          <Typography variant="h6">The Story So Far</Typography>
        </Stack>
        <Divider sx={{ mb: 2 }} />
        {storySoFar.length > 0 ? (
          <Stack spacing={2}>
            {storySoFar.map((paragraph, index) => (
              <Typography key={index} variant="body1">
                {paragraph}
              </Typography>
            ))}
          </Stack>
        ) : (
          <Typography variant="body1" color="text.secondary" fontStyle="italic">
            The exercise has just begun. Events will appear here as they unfold.
          </Typography>
        )}
      </Paper>

      {/* What's Happening Now */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Stack direction="row" spacing={1} alignItems="center" mb={2}>
          <FontAwesomeIcon icon={faBullhorn} />
          <Typography variant="h6">What's Happening Now</Typography>
        </Stack>
        <Divider sx={{ mb: 2 }} />
        {currentEvent ? (
          <Typography variant="body1">{currentEvent}</Typography>
        ) : (
          <Typography variant="body1" color="text.secondary" fontStyle="italic">
            All injects have been completed.
          </Typography>
        )}
      </Paper>

      {/* Coming Up */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Stack direction="row" spacing={1} alignItems="center" mb={2}>
          <FontAwesomeIcon icon={faForward} />
          <Typography variant="h6">Coming Up</Typography>
        </Stack>
        <Divider sx={{ mb: 2 }} />
        {upcomingPreviews.length > 0 ? (
          <Stack spacing={1}>
            {upcomingPreviews.map((preview, index) => (
              <Typography key={index} variant="body2">
                {preview}
              </Typography>
            ))}
          </Stack>
        ) : (
          <Typography variant="body1" color="text.secondary" fontStyle="italic">
            No upcoming injects remaining.
          </Typography>
        )}
      </Paper>

      {/* Evaluator Observations */}
      <Paper sx={{ p: 3 }}>
        <Stack direction="row" spacing={1} alignItems="center" mb={2}>
          <FontAwesomeIcon icon={faClipboardList} />
          <Typography variant="h6">Evaluator Observations</Typography>
        </Stack>
        <Divider sx={{ mb: 2 }} />
        {observations.length > 0 ? (
          <Stack spacing={2}>
            {observations.map(observation => (
              <Box
                key={observation.id}
                sx={{
                  p: 2,
                  bgcolor: 'grey.50',
                  borderRadius: 1,
                  borderLeft: '3px solid',
                  borderLeftColor: 'primary.main',
                }}
              >
                <Stack direction="row" spacing={2} alignItems="center" mb={1}>
                  <RatingBadge rating={observation.rating} />
                  <Typography variant="caption" color="text.secondary">
                    {observation.createdByName}
                  </Typography>
                </Stack>
                <Typography variant="body2">{observation.content}</Typography>
                {observation.recommendation && (
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mt: 1, fontStyle: 'italic' }}
                  >
                    Recommendation: {observation.recommendation}
                  </Typography>
                )}
              </Box>
            ))}
          </Stack>
        ) : (
          <Typography variant="body1" color="text.secondary" fontStyle="italic">
            No observations recorded yet.
          </Typography>
        )}
      </Paper>
    </Box>
  )
}

export default NarrativeView
