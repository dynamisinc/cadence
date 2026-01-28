/**
 * TimelineSummaryPanel Component (S04)
 *
 * Comprehensive timeline and duration analysis for after-action review.
 * Shows pause history, phase timing, and inject pacing analysis.
 */

import {
  Box,
  Typography,
  Stack,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Skeleton,
  Alert,
  Chip,
  Divider,
  useTheme,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faClock,
  faPlay,
  faPause,
  faStop,
  faStopwatch,
  faChartLine,
  faLayerGroup,
  faFire,
} from '@fortawesome/free-solid-svg-icons'

import { useTimelineSummary } from '../hooks/useTimelineSummary'
import { parseTimeSpan, formatDuration } from '../types'
import type {
  TimelineSummaryDto,
  ClockEventDto,
  PhaseTimingDto,
  InjectPacingDto,
} from '../types'

interface TimelineSummaryPanelProps {
  exerciseId: string
}

/**
 * Format a duration from TimeSpan string to human-readable
 */
const formatTimeSpan = (timeSpan: string | null | undefined): string => {
  if (!timeSpan) return 'N/A'
  const ms = parseTimeSpan(timeSpan)
  return formatDuration(ms)
}

/**
 * Format datetime to locale string
 */
const formatDateTime = (dateStr: string | null | undefined): string => {
  if (!dateStr) return 'N/A'
  const date = new Date(dateStr)
  return date.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/**
 * Format time only
 */
const formatTime = (dateStr: string | null | undefined): string => {
  if (!dateStr) return 'N/A'
  const date = new Date(dateStr)
  return date.toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
}

/**
 * Get icon for clock event type
 */
const getEventIcon = (eventType: string) => {
  switch (eventType) {
    case 'Started':
      return faPlay
    case 'Paused':
      return faPause
    case 'Stopped':
      return faStop
    default:
      return faClock
  }
}

/**
 * Get color for clock event type
 */
const getEventColor = (eventType: string, theme: ReturnType<typeof useTheme>) => {
  switch (eventType) {
    case 'Started':
      return theme.palette.success.main
    case 'Paused':
      return theme.palette.warning.main
    case 'Stopped':
      return theme.palette.error.main
    default:
      return theme.palette.text.secondary
  }
}

/**
 * Duration summary card
 */
const DurationSummary = ({ data }: { data: TimelineSummaryDto }) => {
  const _theme = useTheme()

  const isOverTime = data.durationVariance && parseTimeSpan(data.durationVariance) > 0
  const isUnderTime = data.durationVariance && parseTimeSpan(data.durationVariance) < 0

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faStopwatch} />
        <Typography variant="subtitle1" fontWeight="bold">
          Duration Analysis
        </Typography>
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={4}>
        {/* Planned vs Actual */}
        <Box>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Planned Duration
          </Typography>
          <Typography variant="h5" fontWeight="bold">
            {data.plannedDuration ? formatTimeSpan(data.plannedDuration) : 'Not Set'}
          </Typography>
        </Box>

        <Box>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Actual Duration
          </Typography>
          <Typography variant="h5" fontWeight="bold">
            {formatTimeSpan(data.actualDuration)}
          </Typography>
        </Box>

        {data.durationVariance && (
          <Box>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Variance
            </Typography>
            <Typography
              variant="h5"
              fontWeight="bold"
              color={isOverTime ? 'error.main' : isUnderTime ? 'success.main' : 'text.primary'}
            >
              {isOverTime ? '+' : ''}
              {formatTimeSpan(data.durationVariance)}
            </Typography>
          </Box>
        )}

        {/* Wall clock time */}
        {data.wallClockDuration && (
          <Box>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Wall Clock Time
            </Typography>
            <Typography variant="h5" fontWeight="bold">
              {formatTimeSpan(data.wallClockDuration)}
            </Typography>
          </Box>
        )}
      </Stack>

      {/* Start/End times */}
      <Divider sx={{ my: 2 }} />
      <Stack direction="row" spacing={4}>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Started: <strong>{formatDateTime(data.startedAt)}</strong>
          </Typography>
        </Box>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Ended: <strong>{formatDateTime(data.endedAt)}</strong>
          </Typography>
        </Box>
      </Stack>
    </Paper>
  )
}

/**
 * Pause history summary
 */
const PauseSummary = ({ data }: { data: TimelineSummaryDto }) => {
  const theme = useTheme()

  if (data.pauseCount === 0) {
    return (
      <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
          <FontAwesomeIcon icon={faPause} color={theme.palette.warning.main} />
          <Typography variant="subtitle1" fontWeight="bold">
            Pause History
          </Typography>
        </Stack>
        <Typography variant="body2" color="text.secondary">
          No pauses were recorded during this exercise.
        </Typography>
      </Paper>
    )
  }

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faPause} color={theme.palette.warning.main} />
        <Typography variant="subtitle1" fontWeight="bold">
          Pause History
        </Typography>
        <Chip label={`${data.pauseCount} pause${data.pauseCount > 1 ? 's' : ''}`} size="small" />
      </Stack>

      {/* Summary stats */}
      <Stack direction="row" spacing={4} sx={{ mb: 2 }}>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Total Pause Time
          </Typography>
          <Typography variant="h6" fontWeight="bold">
            {formatTimeSpan(data.totalPauseTime)}
          </Typography>
        </Box>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Average Pause
          </Typography>
          <Typography variant="h6" fontWeight="bold">
            {formatTimeSpan(data.averagePauseDuration)}
          </Typography>
        </Box>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Longest Pause
          </Typography>
          <Typography variant="h6" fontWeight="bold">
            {formatTimeSpan(data.longestPauseDuration)}
          </Typography>
        </Box>
      </Stack>

      {/* Pause events table */}
      {data.pauseEvents.length > 0 && (
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Time</TableCell>
                <TableCell>Duration</TableCell>
                <TableCell>Paused By</TableCell>
                <TableCell>Resumed By</TableCell>
                <TableCell>Notes</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data.pauseEvents.map((pause, idx) => (
                <TableRow key={idx}>
                  <TableCell>{formatTime(pause.pausedAt)}</TableCell>
                  <TableCell>{formatTimeSpan(pause.duration)}</TableCell>
                  <TableCell>{pause.pausedByName || 'Unknown'}</TableCell>
                  <TableCell>{pause.resumedByName || (pause.resumedAt ? 'Unknown' : '—')}</TableCell>
                  <TableCell>{pause.notes || '—'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Paper>
  )
}

/**
 * Clock events timeline
 */
const ClockEventsTimeline = ({ events }: { events: ClockEventDto[] }) => {
  const theme = useTheme()

  if (events.length === 0) {
    return null
  }

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faClock} />
        <Typography variant="subtitle1" fontWeight="bold">
          Clock Events Timeline
        </Typography>
      </Stack>

      <Stack spacing={1}>
        {events.map((event, idx) => (
          <Stack key={idx} direction="row" spacing={2} alignItems="center">
            <FontAwesomeIcon
              icon={getEventIcon(event.eventType)}
              color={getEventColor(event.eventType, theme)}
              fixedWidth
            />
            <Typography variant="body2" sx={{ minWidth: 80 }}>
              {formatTime(event.occurredAt)}
            </Typography>
            <Chip
              label={event.eventType}
              size="small"
              sx={{
                bgcolor: getEventColor(event.eventType, theme),
                color: 'white',
                fontWeight: 'bold',
              }}
            />
            <Typography variant="body2" color="text.secondary">
              at {formatTimeSpan(event.elapsedTime)} elapsed
            </Typography>
            {event.userName && (
              <Typography variant="body2" color="text.secondary">
                by {event.userName}
              </Typography>
            )}
          </Stack>
        ))}
      </Stack>
    </Paper>
  )
}

/**
 * Phase timing analysis
 */
const PhaseTimingTable = ({ phases }: { phases: PhaseTimingDto[] }) => {
  if (phases.length === 0) {
    return (
      <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
          <FontAwesomeIcon icon={faLayerGroup} />
          <Typography variant="subtitle1" fontWeight="bold">
            Phase Timing
          </Typography>
        </Stack>
        <Typography variant="body2" color="text.secondary">
          No phase timing data available.
        </Typography>
      </Paper>
    )
  }

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faLayerGroup} />
        <Typography variant="subtitle1" fontWeight="bold">
          Phase Timing
        </Typography>
      </Stack>

      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Phase</TableCell>
              <TableCell align="right">Injects Fired</TableCell>
              <TableCell>Started</TableCell>
              <TableCell>Ended</TableCell>
              <TableCell>Duration</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {phases.map((phase, idx) => (
              <TableRow key={idx}>
                <TableCell>
                  <Typography variant="body2" fontWeight="medium">
                    {phase.phaseName}
                  </Typography>
                </TableCell>
                <TableCell align="right">{phase.injectsFired}</TableCell>
                <TableCell>{formatTime(phase.startedAt)}</TableCell>
                <TableCell>{formatTime(phase.endedAt)}</TableCell>
                <TableCell>{formatTimeSpan(phase.duration)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Paper>
  )
}

/**
 * Inject pacing analysis
 */
const InjectPacingCard = ({ pacing }: { pacing: InjectPacingDto }) => {
  const theme = useTheme()

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faChartLine} color={theme.palette.primary.main} />
        <Typography variant="subtitle1" fontWeight="bold">
          Inject Pacing
        </Typography>
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={4}>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Total Fired
          </Typography>
          <Typography variant="h6" fontWeight="bold">
            {pacing.totalFired}
          </Typography>
        </Box>

        {pacing.injectsPerHour && (
          <Box>
            <Typography variant="body2" color="text.secondary">
              Firing Rate
            </Typography>
            <Typography variant="h6" fontWeight="bold">
              {pacing.injectsPerHour}/hour
            </Typography>
          </Box>
        )}

        {pacing.averageTimeBetweenInjects && (
          <Box>
            <Typography variant="body2" color="text.secondary">
              Avg. Gap
            </Typography>
            <Typography variant="h6" fontWeight="bold">
              {formatTimeSpan(pacing.averageTimeBetweenInjects)}
            </Typography>
          </Box>
        )}

        {pacing.shortestGap && (
          <Box>
            <Typography variant="body2" color="text.secondary">
              Shortest Gap
            </Typography>
            <Typography variant="h6" fontWeight="bold">
              {formatTimeSpan(pacing.shortestGap)}
            </Typography>
          </Box>
        )}

        {pacing.longestGap && (
          <Box>
            <Typography variant="body2" color="text.secondary">
              Longest Gap
            </Typography>
            <Typography variant="h6" fontWeight="bold">
              {formatTimeSpan(pacing.longestGap)}
            </Typography>
          </Box>
        )}
      </Stack>

      {pacing.busiestPeriod && (
        <>
          <Divider sx={{ my: 2 }} />
          <Stack direction="row" spacing={1} alignItems="center">
            <FontAwesomeIcon icon={faFire} color={theme.palette.warning.main} />
            <Typography variant="body2">
              <strong>Busiest Period:</strong> {pacing.busiestPeriod.injectCount} injects between{' '}
              {formatTime(pacing.busiestPeriod.startedAt)} and {formatTime(pacing.busiestPeriod.endedAt)}
            </Typography>
          </Stack>
        </>
      )}
    </Paper>
  )
}

/**
 * Loading skeleton
 */
const LoadingSkeleton = () => (
  <Box>
    <Skeleton variant="rectangular" height={200} sx={{ mb: 3, borderRadius: 1 }} />
    <Skeleton variant="rectangular" height={150} sx={{ mb: 3, borderRadius: 1 }} />
    <Skeleton variant="rectangular" height={200} sx={{ mb: 3, borderRadius: 1 }} />
  </Box>
)

/**
 * Main TimelineSummaryPanel component
 */
export const TimelineSummaryPanel = ({ exerciseId }: TimelineSummaryPanelProps) => {
  const { data, isLoading, error } = useTimelineSummary(exerciseId)

  if (isLoading) {
    return <LoadingSkeleton />
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        Failed to load timeline data. Please try again later.
      </Alert>
    )
  }

  if (!data) {
    return (
      <Alert severity="info" sx={{ mb: 2 }}>
        No timeline data available for this exercise.
      </Alert>
    )
  }

  return (
    <Box>
      <DurationSummary data={data} />
      <PauseSummary data={data} />
      <InjectPacingCard pacing={data.injectPacing} />
      <PhaseTimingTable phases={data.phaseTimings} />
      <ClockEventsTimeline events={data.clockEvents} />
    </Box>
  )
}

export default TimelineSummaryPanel
