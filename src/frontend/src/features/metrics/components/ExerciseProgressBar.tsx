/**
 * ExerciseProgressBar Component (S01)
 *
 * Real-time progress indicator for exercise conduct view.
 * Shows inject completion, observation count, and clock status at a glance.
 * Expandable for additional detail (upcoming injects, P/S/M/U breakdown).
 */

import { useState } from 'react'
import {
  Box,
  Typography,
  LinearProgress,
  Stack,
  Collapse,
  Paper,
  Divider,
  Chip,
  Skeleton,
  useTheme,
  useMediaQuery,
  IconButton,
  type Theme,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronDown,
  faChevronUp,
  faClock,
  faFire,
  faForward,
  faHourglassHalf,
  faClipboardList,
  faPlay,
  faPause,
  faStop,
} from '@fortawesome/free-solid-svg-icons'

import { useExerciseProgress } from '../hooks/useExerciseProgress'
import { parseTimeSpan } from '../types'
import type { ExerciseProgressDto, UpcomingInjectDto } from '../types'
import { ExerciseClockState, InjectStatus } from '../../../types'

interface ExerciseProgressBarProps {
  /** The exercise ID to show progress for */
  exerciseId: string
  /** Whether to auto-refresh progress data (default: true) */
  autoRefresh?: boolean
  /** Refetch interval in milliseconds (default: 5000) */
  refetchInterval?: number
}

/**
 * Format elapsed time for display
 */
const formatElapsedTime = (elapsedTime: string): string => {
  const ms = parseTimeSpan(elapsedTime)
  const totalSeconds = Math.floor(ms / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
}

/**
 * Get clock state icon
 */
const getClockIcon = (status: string) => {
  switch (status) {
    case ExerciseClockState.Running:
      return faPlay
    case ExerciseClockState.Paused:
      return faPause
    default:
      return faStop
  }
}

/**
 * Get clock state color
 */
const getClockColor = (status: string, theme: Theme) => {
  switch (status) {
    case ExerciseClockState.Running:
      return theme.palette.success.main
    case ExerciseClockState.Paused:
      return theme.palette.warning.main
    default:
      return theme.palette.text.secondary
  }
}

/**
 * Compact view for mobile/tablet
 */
const CompactView = ({
  data,
  onExpand,
}: {
  data: ExerciseProgressDto
  onExpand: () => void
}) => {
  const theme = useTheme()
  const clockColor = getClockColor(data.clockStatus, theme)

  return (
    <Paper
      elevation={1}
      sx={{
        p: 1.5,
        cursor: 'pointer',
        '&:hover': { bgcolor: 'action.hover' },
      }}
      onClick={onExpand}
    >
      <Stack direction="row" spacing={2} alignItems="center" justifyContent="space-between">
        {/* Clock */}
        <Stack direction="row" spacing={1} alignItems="center">
          <FontAwesomeIcon icon={getClockIcon(data.clockStatus)} color={clockColor} />
          <Typography variant="body2" fontFamily="monospace" fontWeight="bold">
            {formatElapsedTime(data.elapsedTime)}
          </Typography>
        </Stack>

        {/* Progress */}
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography variant="body2">
            {data.firedCount + data.skippedCount}/{data.totalInjects}
          </Typography>
          <Box sx={{ width: 60 }}>
            <LinearProgress
              variant="determinate"
              value={data.progressPercentage}
              sx={{ height: 6, borderRadius: 3 }}
            />
          </Box>
        </Stack>

        {/* Observations */}
        <Stack direction="row" spacing={0.5} alignItems="center">
          <FontAwesomeIcon icon={faClipboardList} size="sm" />
          <Typography variant="body2">{data.observationCount}</Typography>
        </Stack>

        <FontAwesomeIcon icon={faChevronDown} size="sm" />
      </Stack>
    </Paper>
  )
}

/**
 * Upcoming inject row
 */
const UpcomingInjectRow = ({ inject }: { inject: UpcomingInjectDto }) => {
  const isReady = inject.status === InjectStatus.Ready

  return (
    <Stack
      direction="row"
      spacing={2}
      alignItems="center"
      sx={{
        py: 0.75,
        px: 1,
        bgcolor: isReady ? 'warning.50' : 'transparent',
        borderRadius: 1,
      }}
    >
      <Typography
        variant="caption"
        fontFamily="monospace"
        color="text.secondary"
        sx={{ minWidth: 48 }}
      >
        {inject.scheduledTime.substring(0, 5)}
      </Typography>
      <Chip
        label={`INJ-${inject.injectNumber.toString().padStart(3, '0')}`}
        size="small"
        variant={isReady ? 'filled' : 'outlined'}
        color={isReady ? 'warning' : 'default'}
        sx={{ fontFamily: 'monospace', fontSize: '0.7rem' }}
      />
      <Typography
        variant="body2"
        noWrap
        sx={{ flex: 1, fontSize: '0.8rem' }}
        title={inject.title}
      >
        {inject.title}
      </Typography>
    </Stack>
  )
}

/**
 * Rating counts display
 */
const RatingCounts = ({ counts }: { counts: ExerciseProgressDto['ratingCounts'] }) => {
  const theme = useTheme()

  const ratings = [
    { label: 'P', value: counts.performed, color: theme.palette.success.main },
    { label: 'S', value: counts.satisfactory, color: theme.palette.info.main },
    { label: 'M', value: counts.marginal, color: theme.palette.warning.main },
    { label: 'U', value: counts.unsatisfactory, color: theme.palette.error.main },
  ]

  return (
    <Stack direction="row" spacing={1.5}>
      {ratings.map(({ label, value, color }) => (
        <Stack key={label} direction="row" spacing={0.5} alignItems="center">
          <Box
            sx={{
              width: 16,
              height: 16,
              borderRadius: '50%',
              bgcolor: color,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <Typography
              variant="caption"
              sx={{ color: 'white', fontWeight: 'bold', fontSize: '0.6rem' }}
            >
              {label}
            </Typography>
          </Box>
          <Typography variant="body2">{value}</Typography>
        </Stack>
      ))}
    </Stack>
  )
}

/**
 * Full expanded view
 */
const ExpandedView = ({
  data,
  onCollapse,
}: {
  data: ExerciseProgressDto
  onCollapse: () => void
}) => {
  const theme = useTheme()
  const clockColor = getClockColor(data.clockStatus, theme)

  return (
    <Paper elevation={2} sx={{ p: 2 }}>
      {/* Header */}
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="subtitle1" fontWeight="bold">
          Exercise Progress
        </Typography>
        <IconButton size="small" onClick={onCollapse}>
          <FontAwesomeIcon icon={faChevronUp} />
        </IconButton>
      </Stack>

      {/* Main Stats */}
      <Stack direction="row" spacing={4} sx={{ mb: 2 }}>
        {/* Clock */}
        <Box>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
            <FontAwesomeIcon icon={faClock} />
            <Typography variant="body2" color="text.secondary">
              Elapsed
            </Typography>
          </Stack>
          <Stack direction="row" spacing={1} alignItems="center">
            <FontAwesomeIcon icon={getClockIcon(data.clockStatus)} color={clockColor} size="sm" />
            <Typography variant="h5" fontFamily="monospace" fontWeight="bold">
              {formatElapsedTime(data.elapsedTime)}
            </Typography>
          </Stack>
        </Box>

        {/* Progress Bar */}
        <Box sx={{ flex: 1 }}>
          <Stack direction="row" justifyContent="space-between" sx={{ mb: 0.5 }}>
            <Typography variant="body2" color="text.secondary">
              Injects
            </Typography>
            <Typography variant="body2" fontWeight="bold">
              {data.firedCount + data.skippedCount} / {data.totalInjects} ({data.progressPercentage}%)
            </Typography>
          </Stack>
          <LinearProgress
            variant="determinate"
            value={data.progressPercentage}
            sx={{
              height: 10,
              borderRadius: 5,
              bgcolor: 'grey.200',
              '& .MuiLinearProgress-bar': {
                borderRadius: 5,
                bgcolor: data.progressPercentage === 100 ? 'success.main' : 'primary.main',
              },
            }}
          />
        </Box>

        {/* Observations */}
        <Box>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
            <FontAwesomeIcon icon={faClipboardList} />
            <Typography variant="body2" color="text.secondary">
              Observations
            </Typography>
          </Stack>
          <Typography variant="h5" fontWeight="bold">
            {data.observationCount}
          </Typography>
        </Box>
      </Stack>

      {/* Inject Breakdown */}
      <Stack direction="row" spacing={3} sx={{ mb: 2 }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <FontAwesomeIcon icon={faFire} color={theme.palette.success.main} />
          <Typography variant="body2">Fired: {data.firedCount}</Typography>
        </Stack>
        <Stack direction="row" spacing={1} alignItems="center">
          <FontAwesomeIcon icon={faForward} color={theme.palette.warning.main} />
          <Typography variant="body2">Skipped: {data.skippedCount}</Typography>
        </Stack>
        <Stack direction="row" spacing={1} alignItems="center">
          <FontAwesomeIcon icon={faHourglassHalf} color={theme.palette.text.secondary} />
          <Typography variant="body2">Pending: {data.pendingCount}</Typography>
        </Stack>
        {data.readyCount > 0 && (
          <Chip
            label={`${data.readyCount} Ready`}
            size="small"
            color="warning"
            variant="filled"
          />
        )}
      </Stack>

      <Divider sx={{ my: 2 }} />

      {/* Two Column Layout */}
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={3}>
        {/* Left: Upcoming Injects */}
        <Box sx={{ flex: 1 }}>
          <Typography variant="subtitle2" sx={{ mb: 1 }}>
            Upcoming Injects
          </Typography>
          {data.nextInjects.length > 0 ? (
            <Stack spacing={0.5}>
              {data.nextInjects.map(inject => (
                <UpcomingInjectRow key={inject.id} inject={inject} />
              ))}
            </Stack>
          ) : (
            <Typography variant="body2" color="text.secondary">
              No pending injects
            </Typography>
          )}
        </Box>

        {/* Right: Rating Breakdown */}
        <Box>
          <Typography variant="subtitle2" sx={{ mb: 1 }}>
            Observation Ratings
          </Typography>
          <RatingCounts counts={data.ratingCounts} />
        </Box>
      </Stack>

      {/* Current Phase */}
      {data.currentPhaseName && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="body2" color="text.secondary">
            Current Phase:{' '}
            <Typography component="span" fontWeight="bold">
              {data.currentPhaseName}
            </Typography>
          </Typography>
        </Box>
      )}
    </Paper>
  )
}

/**
 * Loading skeleton
 */
const LoadingSkeleton = () => (
  <Paper elevation={1} sx={{ p: 1.5 }}>
    <Stack direction="row" spacing={2} alignItems="center" justifyContent="space-between">
      <Skeleton variant="text" width={80} />
      <Skeleton variant="rectangular" width={100} height={24} />
      <Skeleton variant="text" width={40} />
    </Stack>
  </Paper>
)

export const ExerciseProgressBar = ({
  exerciseId,
  autoRefresh = true,
  refetchInterval = 5000,
}: ExerciseProgressBarProps) => {
  const [isExpanded, setIsExpanded] = useState(false)
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))

  const { data, isLoading, error } = useExerciseProgress(exerciseId, {
    enabled: !!exerciseId,
    refetchInterval: autoRefresh ? refetchInterval : false,
  })

  if (isLoading) {
    return <LoadingSkeleton />
  }

  if (error || !data) {
    return null // Don't show error, just hide the component
  }

  // On mobile, always show compact view that expands
  if (isMobile) {
    return (
      <Box>
        <CompactView data={data} onExpand={() => setIsExpanded(true)} />
        <Collapse in={isExpanded}>
          <Box sx={{ mt: 1 }}>
            <ExpandedView data={data} onCollapse={() => setIsExpanded(false)} />
          </Box>
        </Collapse>
      </Box>
    )
  }

  // On desktop, show either collapsed or expanded
  return isExpanded ? (
    <ExpandedView data={data} onCollapse={() => setIsExpanded(false)} />
  ) : (
    <Paper
      elevation={1}
      sx={{
        p: 2,
        cursor: 'pointer',
        '&:hover': { bgcolor: 'action.hover' },
      }}
      onClick={() => setIsExpanded(true)}
    >
      <Stack direction="row" spacing={4} alignItems="center" justifyContent="space-between">
        {/* Clock */}
        <Stack direction="row" spacing={1.5} alignItems="center">
          <FontAwesomeIcon
            icon={getClockIcon(data.clockStatus)}
            color={getClockColor(data.clockStatus, theme)}
          />
          <Typography variant="h6" fontFamily="monospace" fontWeight="bold">
            {formatElapsedTime(data.elapsedTime)}
          </Typography>
          <Chip
            label={data.clockStatus}
            size="small"
            color={
              data.clockStatus === ExerciseClockState.Running
                ? 'success'
                : data.clockStatus === ExerciseClockState.Paused
                  ? 'warning'
                  : 'default'
            }
          />
        </Stack>

        {/* Progress */}
        <Stack direction="row" spacing={2} alignItems="center" sx={{ flex: 1, maxWidth: 400 }}>
          <Typography variant="body2" color="text.secondary" sx={{ minWidth: 'fit-content' }}>
            Progress:
          </Typography>
          <Box sx={{ flex: 1 }}>
            <LinearProgress
              variant="determinate"
              value={data.progressPercentage}
              sx={{
                height: 8,
                borderRadius: 4,
                bgcolor: 'grey.200',
                '& .MuiLinearProgress-bar': {
                  borderRadius: 4,
                  bgcolor: data.progressPercentage === 100 ? 'success.main' : 'primary.main',
                },
              }}
            />
          </Box>
          <Typography variant="body2" fontWeight="bold" sx={{ minWidth: 'fit-content' }}>
            {data.firedCount + data.skippedCount}/{data.totalInjects} ({data.progressPercentage}%)
          </Typography>
        </Stack>

        {/* Observations */}
        <Stack direction="row" spacing={1} alignItems="center">
          <FontAwesomeIcon icon={faClipboardList} />
          <Typography variant="body2">
            <strong>{data.observationCount}</strong> obs
          </Typography>
        </Stack>

        {/* Expand Icon */}
        <FontAwesomeIcon icon={faChevronDown} />
      </Stack>
    </Paper>
  )
}

export default ExerciseProgressBar
