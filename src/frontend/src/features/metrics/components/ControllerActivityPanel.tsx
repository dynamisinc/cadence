/**
 * ControllerActivityPanel Component (S07)
 *
 * Displays controller activity metrics for after-action review.
 * Shows workload distribution, timing performance, and phase activity per controller.
 */

import {
  Box,
  Typography,
  Stack,
  Paper,
  Skeleton,
  Alert,
  Chip,
  LinearProgress,
  useTheme,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faUserGear,
  faFire,
  faForwardStep,
  faClock,
  faChartPie,
} from '@fortawesome/free-solid-svg-icons'

import { useControllerActivity } from '../hooks/useControllerActivity'
import { parseTimeSpan } from '../types'
import type { ControllerActivityDto, ControllerActivitySummaryDto } from '../types'

interface ControllerActivityPanelProps {
  exerciseId: string
}

/**
 * Format average variance to display string
 */
const formatVariance = (variance: string | null | undefined): string => {
  if (!variance) return 'N/A'
  const ms = parseTimeSpan(variance)
  const minutes = Math.round(ms / 60000)
  if (minutes === 0) return 'On time'
  if (minutes > 0) return `+${minutes} min`
  return `${minutes} min`
}

/**
 * Summary stats card
 */
const SummaryCard = ({ data }: { data: ControllerActivitySummaryDto }) => {
  const theme = useTheme()

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faChartPie} color={theme.palette.primary.main} />
        <Typography variant="subtitle1" fontWeight="bold">
          Summary
        </Typography>
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={4}>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Controllers
          </Typography>
          <Typography variant="h5" fontWeight="bold">
            {data.totalControllers}
          </Typography>
        </Box>

        <Box>
          <Typography variant="body2" color="text.secondary">
            Injects Fired
          </Typography>
          <Typography variant="h5" fontWeight="bold">
            {data.totalInjectsFired}
          </Typography>
        </Box>

        <Box>
          <Typography variant="body2" color="text.secondary">
            Injects Skipped
          </Typography>
          <Typography variant="h5" fontWeight="bold">
            {data.totalInjectsSkipped}
          </Typography>
        </Box>

        {data.overallOnTimeRate !== null && (
          <Box>
            <Typography variant="body2" color="text.secondary">
              Overall On-Time Rate
            </Typography>
            <Typography
              variant="h5"
              fontWeight="bold"
              color={data.overallOnTimeRate >= 80 ? 'success.main' : data.overallOnTimeRate >= 60 ? 'warning.main' : 'error.main'}
            >
              {data.overallOnTimeRate}%
            </Typography>
          </Box>
        )}
      </Stack>
    </Paper>
  )
}

/**
 * Workload distribution bar
 */
const WorkloadBar = ({ controller }: { controller: ControllerActivityDto }) => {
  const theme = useTheme()
  const percentage = controller.workloadPercentage

  return (
    <Box sx={{ mb: 1 }}>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 0.5 }}>
        <Typography variant="body2" fontWeight="medium">
          {controller.controllerName}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {percentage}% ({controller.injectsFired} injects)
        </Typography>
      </Stack>
      <LinearProgress
        variant="determinate"
        value={percentage}
        sx={{
          height: 8,
          borderRadius: 1,
          bgcolor: theme.palette.grey[200],
          '& .MuiLinearProgress-bar': {
            bgcolor: theme.palette.primary.main,
          },
        }}
      />
    </Box>
  )
}

/**
 * Controller detail card
 */
const ControllerCard = ({ controller }: { controller: ControllerActivityDto }) => {
  const theme = useTheme()

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 2 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faUserGear} />
        <Typography variant="subtitle1" fontWeight="bold">
          {controller.controllerName}
        </Typography>
        <Chip
          label={`${controller.workloadPercentage}% workload`}
          size="small"
          color="primary"
          variant="outlined"
        />
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={4} sx={{ mb: 2 }}>
        <Box>
          <Stack direction="row" spacing={1} alignItems="center">
            <FontAwesomeIcon icon={faFire} color={theme.palette.success.main} size="sm" />
            <Typography variant="body2" color="text.secondary">
              Fired
            </Typography>
          </Stack>
          <Typography variant="h6" fontWeight="bold">
            {controller.injectsFired}
          </Typography>
        </Box>

        <Box>
          <Stack direction="row" spacing={1} alignItems="center">
            <FontAwesomeIcon icon={faForwardStep} color={theme.palette.warning.main} size="sm" />
            <Typography variant="body2" color="text.secondary">
              Skipped
            </Typography>
          </Stack>
          <Typography variant="h6" fontWeight="bold">
            {controller.injectsSkipped}
          </Typography>
        </Box>

        {controller.onTimeRate !== null && (
          <Box>
            <Stack direction="row" spacing={1} alignItems="center">
              <FontAwesomeIcon icon={faClock} size="sm" />
              <Typography variant="body2" color="text.secondary">
                On-Time Rate
              </Typography>
            </Stack>
            <Typography
              variant="h6"
              fontWeight="bold"
              color={controller.onTimeRate >= 80 ? 'success.main' : controller.onTimeRate >= 60 ? 'warning.main' : 'error.main'}
            >
              {controller.onTimeRate}%
            </Typography>
          </Box>
        )}

        <Box>
          <Typography variant="body2" color="text.secondary">
            Avg. Variance
          </Typography>
          <Typography variant="h6" fontWeight="bold">
            {formatVariance(controller.averageVariance)}
          </Typography>
        </Box>
      </Stack>

      {/* Phase activity */}
      {controller.phaseActivity.length > 0 && (
        <Box>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Active Phases
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {controller.phaseActivity.map((phase, idx) => (
              <Chip
                key={idx}
                label={`${phase.phaseName}: ${phase.injectsFired} fired${phase.injectsSkipped > 0 ? `, ${phase.injectsSkipped} skipped` : ''}`}
                size="small"
                variant="outlined"
              />
            ))}
          </Stack>
        </Box>
      )}
    </Paper>
  )
}

/**
 * Loading skeleton
 */
const LoadingSkeleton = () => (
  <Box>
    <Skeleton variant="rectangular" height={150} sx={{ mb: 3, borderRadius: 1 }} />
    <Skeleton variant="rectangular" height={100} sx={{ mb: 2, borderRadius: 1 }} />
    <Skeleton variant="rectangular" height={150} sx={{ mb: 2, borderRadius: 1 }} />
    <Skeleton variant="rectangular" height={150} sx={{ mb: 2, borderRadius: 1 }} />
  </Box>
)

/**
 * Main ControllerActivityPanel component
 */
export const ControllerActivityPanel = ({ exerciseId }: ControllerActivityPanelProps) => {
  const { data, isLoading, error } = useControllerActivity(exerciseId)

  if (isLoading) {
    return <LoadingSkeleton />
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        Failed to load controller activity data. Please try again later.
      </Alert>
    )
  }

  if (!data) {
    return (
      <Alert severity="info" sx={{ mb: 2 }}>
        No controller activity data available for this exercise.
      </Alert>
    )
  }

  if (data.totalControllers === 0) {
    return (
      <Alert severity="info" sx={{ mb: 2 }}>
        No injects have been fired yet. Controller activity will appear here once injects are delivered.
      </Alert>
    )
  }

  return (
    <Box>
      {/* Summary */}
      <SummaryCard data={data} />

      {/* Workload Distribution */}
      <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
          <FontAwesomeIcon icon={faChartPie} />
          <Typography variant="subtitle1" fontWeight="bold">
            Workload Distribution
          </Typography>
        </Stack>

        {data.controllers.map((controller, idx) => (
          <WorkloadBar
            key={idx}
            controller={controller}
          />
        ))}
      </Paper>

      {/* Detailed Activity */}
      <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }}>
        Detailed Activity
      </Typography>

      {data.controllers.map((controller, idx) => (
        <ControllerCard key={idx} controller={controller} />
      ))}
    </Box>
  )
}

export default ControllerActivityPanel
