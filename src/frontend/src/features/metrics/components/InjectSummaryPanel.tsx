/**
 * InjectSummaryPanel Component (S02)
 *
 * Comprehensive inject delivery statistics for after-action review.
 * Shows timing performance, on-time rate, and breakdowns by phase/controller.
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
  useTheme,
  LinearProgress,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faFire,
  faForward,
  faHourglassHalf,
  faClock,
  faCheck,
  faExclamationTriangle,
} from '@fortawesome/free-solid-svg-icons'

import { useInjectSummary } from '../hooks/useInjectSummary'
import { formatVariance, parseTimeSpan } from '../types'
import type {
  InjectSummaryDto,
  PhaseInjectSummaryDto,
  ControllerInjectSummaryDto,
  SkippedInjectDto,
} from '../types'

interface InjectSummaryPanelProps {
  exerciseId: string
  onTimeToleranceMinutes?: number
}

/**
 * Metric card for summary statistics
 */
const MetricCard = ({
  label,
  value,
  subValue,
  icon,
  color,
}: {
  label: string
  value: string | number
  subValue?: string
  icon: typeof faFire
  color: string
}) => {
  const theme = useTheme()

  return (
    <Paper
      elevation={0}
      sx={{
        p: 2,
        bgcolor: 'grey.50',
        borderRadius: 2,
        minWidth: 140,
        textAlign: 'center',
      }}
    >
      <Box sx={{ color, mb: 1 }}>
        <FontAwesomeIcon icon={icon} size="lg" />
      </Box>
      <Typography variant="h4" fontWeight="bold">
        {value}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      {subValue && (
        <Typography variant="caption" color="text.secondary">
          {subValue}
        </Typography>
      )}
    </Paper>
  )
}

/**
 * Status breakdown bar
 */
const StatusBreakdownBar = ({ data }: { data: InjectSummaryDto }) => {
  const theme = useTheme()

  return (
    <Box sx={{ my: 2 }}>
      <Box
        sx={{
          display: 'flex',
          height: 24,
          borderRadius: 1,
          overflow: 'hidden',
        }}
      >
        {data.firedPercentage > 0 && (
          <Box
            sx={{
              width: `${data.firedPercentage}%`,
              bgcolor: theme.palette.success.main,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            {data.firedPercentage >= 15 && (
              <Typography variant="caption" sx={{ color: 'white', fontWeight: 'bold' }}>
                {data.firedPercentage}%
              </Typography>
            )}
          </Box>
        )}
        {data.skippedPercentage > 0 && (
          <Box
            sx={{
              width: `${data.skippedPercentage}%`,
              bgcolor: theme.palette.warning.main,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            {data.skippedPercentage >= 10 && (
              <Typography variant="caption" sx={{ color: 'white', fontWeight: 'bold' }}>
                {data.skippedPercentage}%
              </Typography>
            )}
          </Box>
        )}
        {data.notExecutedPercentage > 0 && (
          <Box
            sx={{
              width: `${data.notExecutedPercentage}%`,
              bgcolor: theme.palette.grey[400],
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            {data.notExecutedPercentage >= 10 && (
              <Typography variant="caption" sx={{ color: 'white', fontWeight: 'bold' }}>
                {data.notExecutedPercentage}%
              </Typography>
            )}
          </Box>
        )}
      </Box>
      <Stack direction="row" spacing={2} sx={{ mt: 1 }}>
        <Stack direction="row" spacing={0.5} alignItems="center">
          <Box sx={{ width: 12, height: 12, bgcolor: 'success.main', borderRadius: '50%' }} />
          <Typography variant="caption">Fired</Typography>
        </Stack>
        <Stack direction="row" spacing={0.5} alignItems="center">
          <Box sx={{ width: 12, height: 12, bgcolor: 'warning.main', borderRadius: '50%' }} />
          <Typography variant="caption">Skipped</Typography>
        </Stack>
        <Stack direction="row" spacing={0.5} alignItems="center">
          <Box sx={{ width: 12, height: 12, bgcolor: 'grey.400', borderRadius: '50%' }} />
          <Typography variant="caption">Not Executed</Typography>
        </Stack>
      </Stack>
    </Box>
  )
}

/**
 * Timing performance section
 */
const TimingPerformance = ({ data }: { data: InjectSummaryDto }) => {
  const theme = useTheme()

  if (data.onTimeRate === null) {
    return (
      <Alert severity="info" sx={{ my: 2 }}>
        No timing data available for this exercise.
      </Alert>
    )
  }

  return (
    <Box sx={{ my: 3 }}>
      <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }}>
        Timing Performance
      </Typography>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3}>
        {/* On-Time Rate */}
        <Box sx={{ flex: 1 }}>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
            <FontAwesomeIcon
              icon={data.onTimeRate >= 80 ? faCheck : faExclamationTriangle}
              color={data.onTimeRate >= 80 ? theme.palette.success.main : theme.palette.warning.main}
            />
            <Typography variant="body2">On-Time Rate</Typography>
          </Stack>
          <Typography variant="h4" fontWeight="bold">
            {data.onTimeRate}%
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {data.onTimeCount} of {data.firedCount} injects
          </Typography>
        </Box>

        {/* Average Variance */}
        <Box sx={{ flex: 1 }}>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
            <FontAwesomeIcon icon={faClock} />
            <Typography variant="body2">Average Variance</Typography>
          </Stack>
          <Typography variant="h4" fontWeight="bold">
            {formatVariance(data.averageVariance)}
          </Typography>
        </Box>

        {/* Min/Max */}
        <Box sx={{ flex: 1 }}>
          <Typography variant="body2" sx={{ mb: 1 }}>
            Variance Range
          </Typography>
          {data.earliestVariance && (
            <Typography variant="body2">
              Earliest: <strong>{formatVariance(data.earliestVariance.variance)}</strong>
              <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 1 }}>
                (INJ-{data.earliestVariance.injectNumber.toString().padStart(3, '0')})
              </Typography>
            </Typography>
          )}
          {data.latestVariance && (
            <Typography variant="body2">
              Latest: <strong>{formatVariance(data.latestVariance.variance)}</strong>
              <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 1 }}>
                (INJ-{data.latestVariance.injectNumber.toString().padStart(3, '0')})
              </Typography>
            </Typography>
          )}
        </Box>
      </Stack>
    </Box>
  )
}

/**
 * By Phase table
 */
const ByPhaseTable = ({ phases }: { phases: PhaseInjectSummaryDto[] }) => {
  if (phases.length === 0) return null

  return (
    <Box sx={{ my: 3 }}>
      <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }}>
        By Phase
      </Typography>
      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Phase</TableCell>
              <TableCell align="right">Total</TableCell>
              <TableCell align="right">Fired</TableCell>
              <TableCell align="right">Skipped</TableCell>
              <TableCell align="right">Not Executed</TableCell>
              <TableCell align="right">On-Time</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {phases.map(phase => (
              <TableRow key={phase.phaseId ?? 'no-phase'}>
                <TableCell>{phase.phaseName}</TableCell>
                <TableCell align="right">{phase.totalCount}</TableCell>
                <TableCell align="right">{phase.firedCount}</TableCell>
                <TableCell align="right">{phase.skippedCount}</TableCell>
                <TableCell align="right">{phase.notExecutedCount}</TableCell>
                <TableCell align="right">
                  {phase.onTimeRate !== null ? `${phase.onTimeRate}%` : '—'}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  )
}

/**
 * By Controller table
 */
const ByControllerTable = ({ controllers }: { controllers: ControllerInjectSummaryDto[] }) => {
  if (controllers.length === 0) return null

  return (
    <Box sx={{ my: 3 }}>
      <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }}>
        By Controller
      </Typography>
      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Controller</TableCell>
              <TableCell align="right">Fired</TableCell>
              <TableCell align="right">Avg Variance</TableCell>
              <TableCell align="right">On-Time</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {controllers.map(controller => (
              <TableRow key={controller.controllerId ?? 'unknown'}>
                <TableCell>{controller.controllerName}</TableCell>
                <TableCell align="right">{controller.firedCount}</TableCell>
                <TableCell align="right">{formatVariance(controller.averageVariance)}</TableCell>
                <TableCell align="right">
                  {controller.onTimeRate !== null ? `${controller.onTimeRate}%` : '—'}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  )
}

/**
 * Skipped injects list
 */
const SkippedInjectsList = ({ injects }: { injects: SkippedInjectDto[] }) => {
  if (injects.length === 0) return null

  return (
    <Box sx={{ my: 3 }}>
      <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }}>
        Skipped Injects
      </Typography>
      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Inject</TableCell>
              <TableCell>Phase</TableCell>
              <TableCell>Reason</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {injects.map(inject => (
              <TableRow key={inject.id}>
                <TableCell>
                  <Typography variant="body2" fontFamily="monospace">
                    INJ-{inject.injectNumber.toString().padStart(3, '0')}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {inject.title}
                  </Typography>
                </TableCell>
                <TableCell>{inject.phaseName ?? '—'}</TableCell>
                <TableCell>{inject.skipReason ?? 'No reason provided'}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  )
}

/**
 * Loading skeleton
 */
const LoadingSkeleton = () => (
  <Box>
    <Stack direction="row" spacing={2} sx={{ mb: 3 }}>
      {[1, 2, 3, 4].map(i => (
        <Skeleton key={i} variant="rectangular" width={140} height={120} sx={{ borderRadius: 2 }} />
      ))}
    </Stack>
    <Skeleton variant="rectangular" height={24} sx={{ mb: 2 }} />
    <Skeleton variant="rectangular" height={200} />
  </Box>
)

export const InjectSummaryPanel = ({
  exerciseId,
  onTimeToleranceMinutes = 5,
}: InjectSummaryPanelProps) => {
  const theme = useTheme()
  const { data, isLoading, error } = useInjectSummary(exerciseId, onTimeToleranceMinutes)

  if (isLoading) {
    return <LoadingSkeleton />
  }

  if (error) {
    return (
      <Alert severity="error">
        Failed to load inject summary. Please try again.
      </Alert>
    )
  }

  if (!data) {
    return (
      <Alert severity="info">
        No inject data available for this exercise.
      </Alert>
    )
  }

  return (
    <Box>
      {/* Summary Cards */}
      <Stack direction="row" spacing={2} sx={{ mb: 3 }} flexWrap="wrap" useFlexGap>
        <MetricCard
          label="Total Injects"
          value={data.totalCount}
          icon={faFire}
          color={theme.palette.text.primary}
        />
        <MetricCard
          label="Fired"
          value={data.firedCount}
          subValue={`${data.firedPercentage}%`}
          icon={faFire}
          color={theme.palette.success.main}
        />
        <MetricCard
          label="Skipped"
          value={data.skippedCount}
          subValue={`${data.skippedPercentage}%`}
          icon={faForward}
          color={theme.palette.warning.main}
        />
        <MetricCard
          label="Not Executed"
          value={data.notExecutedCount}
          subValue={`${data.notExecutedPercentage}%`}
          icon={faHourglassHalf}
          color={theme.palette.grey[500]}
        />
      </Stack>

      {/* Status Breakdown Bar */}
      <StatusBreakdownBar data={data} />

      {/* Timing Performance */}
      <TimingPerformance data={data} />

      {/* By Phase */}
      <ByPhaseTable phases={data.byPhase} />

      {/* By Controller */}
      <ByControllerTable controllers={data.byController} />

      {/* Skipped Injects */}
      <SkippedInjectsList injects={data.skippedInjects} />
    </Box>
  )
}

export default InjectSummaryPanel
