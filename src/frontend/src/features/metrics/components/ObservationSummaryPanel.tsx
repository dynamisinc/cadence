/**
 * ObservationSummaryPanel Component (S03)
 *
 * Comprehensive observation statistics for after-action review.
 * Shows P/S/M/U distribution, coverage rates, and breakdowns by evaluator/phase.
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
  LinearProgress,
  useTheme,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faClipboardList,
  faCheckCircle,
  faExclamationCircle,
  faLink,
  faUnlink,
  faBullseye,
} from '@fortawesome/free-solid-svg-icons'

import { useObservationSummary } from '../hooks/useObservationSummary'
import type {
  ObservationSummaryDto,
  RatingDistributionDto,
  EvaluatorSummaryDto,
  PhaseObservationSummaryDto,
  UncoveredObjectiveDto,
  RatingCountsDto,
} from '../types'

interface ObservationSummaryPanelProps {
  exerciseId: string
}

/**
 * Rating colors matching HSEEP P/S/M/U scale
 */
const getRatingColor = (rating: 'P' | 'S' | 'M' | 'U', theme: ReturnType<typeof useTheme>) => {
  switch (rating) {
    case 'P':
      return theme.palette.success.main
    case 'S':
      return theme.palette.info.main
    case 'M':
      return theme.palette.warning.main
    case 'U':
      return theme.palette.error.main
  }
}

/**
 * Rating distribution bar chart
 */
const RatingDistributionChart = ({ distribution }: { distribution: RatingDistributionDto }) => {
  const theme = useTheme()

  const ratings = [
    { label: 'Performed (P)', count: distribution.performedCount, pct: distribution.performedPercentage, key: 'P' as const },
    { label: 'Satisfactory (S)', count: distribution.satisfactoryCount, pct: distribution.satisfactoryPercentage, key: 'S' as const },
    { label: 'Marginal (M)', count: distribution.marginalCount, pct: distribution.marginalPercentage, key: 'M' as const },
    { label: 'Unsatisfactory (U)', count: distribution.unsatisfactoryCount, pct: distribution.unsatisfactoryPercentage, key: 'U' as const },
  ]

  const maxPct = Math.max(...ratings.map(r => r.pct), 1)

  return (
    <Box sx={{ my: 2 }}>
      {ratings.map(({ label, count, pct, key }) => (
        <Stack key={key} direction="row" spacing={2} alignItems="center" sx={{ mb: 1.5 }}>
          <Typography variant="body2" sx={{ minWidth: 140 }}>
            {label}
          </Typography>
          <Box sx={{ flex: 1, maxWidth: 300 }}>
            <LinearProgress
              variant="determinate"
              value={(pct / maxPct) * 100}
              sx={{
                height: 20,
                borderRadius: 1,
                bgcolor: 'grey.200',
                '& .MuiLinearProgress-bar': {
                  bgcolor: getRatingColor(key, theme),
                  borderRadius: 1,
                },
              }}
            />
          </Box>
          <Typography variant="body2" fontWeight="bold" sx={{ minWidth: 50, textAlign: 'right' }}>
            {count}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ minWidth: 50, textAlign: 'right' }}>
            ({pct}%)
          </Typography>
        </Stack>
      ))}

      {distribution.unratedCount > 0 && (
        <Stack direction="row" spacing={2} alignItems="center" sx={{ mt: 2 }}>
          <Typography variant="body2" color="text.secondary" sx={{ minWidth: 140 }}>
            Unrated
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {distribution.unratedCount} ({distribution.unratedPercentage}%)
          </Typography>
        </Stack>
      )}

      {distribution.averageRating !== null && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="body2" color="text.secondary">
            Average Rating:{' '}
            <Typography component="span" fontWeight="bold">
              {distribution.averageRating.toFixed(2)}
            </Typography>
            <Typography component="span" variant="caption" sx={{ ml: 1 }}>
              (P=1, S=2, M=3, U=4)
            </Typography>
          </Typography>
        </Box>
      )}
    </Box>
  )
}

/**
 * Coverage rate display
 */
const CoverageRate = ({
  coverageRate,
  covered,
  total,
  uncovered,
}: {
  coverageRate: number | null
  covered: number
  total: number
  uncovered: UncoveredObjectiveDto[]
}) => {
  const theme = useTheme()

  if (total === 0) {
    return (
      <Alert severity="info" sx={{ my: 2 }}>
        No objectives defined for this exercise.
      </Alert>
    )
  }

  return (
    <Box sx={{ my: 3 }}>
      <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }}>
        Objective Coverage
      </Typography>

      <Stack direction="row" spacing={4} alignItems="center">
        <Box>
          <Stack direction="row" spacing={1} alignItems="center">
            <FontAwesomeIcon
              icon={coverageRate !== null && coverageRate >= 75 ? faCheckCircle : faExclamationCircle}
              color={coverageRate !== null && coverageRate >= 75 ? theme.palette.success.main : theme.palette.warning.main}
            />
            <Typography variant="h4" fontWeight="bold">
              {coverageRate !== null ? `${coverageRate}%` : '—'}
            </Typography>
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {covered} of {total} objectives observed
          </Typography>
        </Box>

        <Box sx={{ flex: 1, maxWidth: 200 }}>
          <LinearProgress
            variant="determinate"
            value={coverageRate ?? 0}
            sx={{
              height: 12,
              borderRadius: 1,
              bgcolor: 'grey.200',
              '& .MuiLinearProgress-bar': {
                bgcolor: coverageRate !== null && coverageRate >= 75 ? 'success.main' : 'warning.main',
                borderRadius: 1,
              },
            }}
          />
        </Box>
      </Stack>

      {uncovered.length > 0 && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="body2" color="error.main" sx={{ mb: 1 }}>
            Objectives without observations:
          </Typography>
          <List dense>
            {uncovered.map(obj => (
              <ListItem key={obj.id} sx={{ py: 0 }}>
                <ListItemIcon sx={{ minWidth: 32 }}>
                  <FontAwesomeIcon icon={faBullseye} size="sm" />
                </ListItemIcon>
                <ListItemText
                  primary={`${obj.objectiveNumber}: ${obj.name}`}
                  primaryTypographyProps={{ variant: 'body2' }}
                />
              </ListItem>
            ))}
          </List>
        </Box>
      )}
    </Box>
  )
}

/**
 * Rating counts inline display
 */
const RatingCountsInline = ({ counts }: { counts: RatingCountsDto }) => {
  const theme = useTheme()

  return (
    <Stack direction="row" spacing={1}>
      <Typography variant="caption" sx={{ color: getRatingColor('P', theme), fontWeight: 'bold' }}>
        P:{counts.performed}
      </Typography>
      <Typography variant="caption" sx={{ color: getRatingColor('S', theme), fontWeight: 'bold' }}>
        S:{counts.satisfactory}
      </Typography>
      <Typography variant="caption" sx={{ color: getRatingColor('M', theme), fontWeight: 'bold' }}>
        M:{counts.marginal}
      </Typography>
      <Typography variant="caption" sx={{ color: getRatingColor('U', theme), fontWeight: 'bold' }}>
        U:{counts.unsatisfactory}
      </Typography>
    </Stack>
  )
}

/**
 * By Evaluator table
 */
const ByEvaluatorTable = ({ evaluators }: { evaluators: EvaluatorSummaryDto[] }) => {
  if (evaluators.length === 0) return null

  return (
    <Box sx={{ my: 3 }}>
      <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }}>
        By Evaluator
      </Typography>
      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Evaluator</TableCell>
              <TableCell align="right">Observations</TableCell>
              <TableCell align="right">Avg Rating</TableCell>
              <TableCell>Breakdown</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {evaluators.map(evaluator => (
              <TableRow key={evaluator.evaluatorId ?? 'unknown'}>
                <TableCell>{evaluator.evaluatorName}</TableCell>
                <TableCell align="right">{evaluator.observationCount}</TableCell>
                <TableCell align="right">
                  {evaluator.averageRating !== null ? evaluator.averageRating.toFixed(2) : '—'}
                </TableCell>
                <TableCell>
                  <RatingCountsInline counts={evaluator.ratingCounts} />
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
 * By Phase table
 */
const ByPhaseTable = ({ phases }: { phases: PhaseObservationSummaryDto[] }) => {
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
              <TableCell align="right">Observations</TableCell>
              <TableCell>Breakdown</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {phases.map(phase => (
              <TableRow key={phase.phaseId ?? 'no-phase'}>
                <TableCell>{phase.phaseName}</TableCell>
                <TableCell align="right">{phase.observationCount}</TableCell>
                <TableCell>
                  <RatingCountsInline counts={phase.ratingCounts} />
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
 * Data quality section
 */
const DataQuality = ({ data }: { data: ObservationSummaryDto }) => {
  const linkedPct = data.totalCount > 0
    ? Math.round((data.linkedToInjectCount / data.totalCount) * 100)
    : 0
  const objectivePct = data.totalCount > 0
    ? Math.round((data.linkedToObjectiveCount / data.totalCount) * 100)
    : 0

  return (
    <Box sx={{ my: 3 }}>
      <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }}>
        Data Quality
      </Typography>
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={2} alignItems="center">
          <FontAwesomeIcon icon={faLink} />
          <Typography variant="body2">
            Linked to inject: <strong>{data.linkedToInjectCount}</strong> ({linkedPct}%)
          </Typography>
        </Stack>
        <Stack direction="row" spacing={2} alignItems="center">
          <FontAwesomeIcon icon={faBullseye} />
          <Typography variant="body2">
            Linked to objective: <strong>{data.linkedToObjectiveCount}</strong> ({objectivePct}%)
          </Typography>
        </Stack>
        {data.unlinkedCount > 0 && (
          <Stack direction="row" spacing={2} alignItems="center">
            <FontAwesomeIcon icon={faUnlink} color="warning" />
            <Typography variant="body2" color="warning.main">
              Unlinked: <strong>{data.unlinkedCount}</strong>
            </Typography>
          </Stack>
        )}
      </Stack>
    </Box>
  )
}

/**
 * Loading skeleton
 */
const LoadingSkeleton = () => (
  <Box>
    <Skeleton variant="rectangular" width={140} height={100} sx={{ borderRadius: 2, mb: 3 }} />
    <Skeleton variant="rectangular" height={150} sx={{ mb: 2 }} />
    <Skeleton variant="rectangular" height={100} sx={{ mb: 2 }} />
    <Skeleton variant="rectangular" height={200} />
  </Box>
)

export const ObservationSummaryPanel = ({ exerciseId }: ObservationSummaryPanelProps) => {
  const theme = useTheme()
  const { data, isLoading, error } = useObservationSummary(exerciseId)

  if (isLoading) {
    return <LoadingSkeleton />
  }

  if (error) {
    return (
      <Alert severity="error">
        Failed to load observation summary. Please try again.
      </Alert>
    )
  }

  if (!data) {
    return (
      <Alert severity="info">
        No observation data available for this exercise.
      </Alert>
    )
  }

  return (
    <Box>
      {/* Total Count Card */}
      <Paper
        elevation={0}
        sx={{
          p: 2,
          bgcolor: 'grey.50',
          borderRadius: 2,
          display: 'inline-flex',
          flexDirection: 'column',
          alignItems: 'center',
          mb: 3,
        }}
      >
        <FontAwesomeIcon icon={faClipboardList} size="lg" color={theme.palette.primary.main} />
        <Typography variant="h3" fontWeight="bold" sx={{ mt: 1 }}>
          {data.totalCount}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Total Observations
        </Typography>
      </Paper>

      {/* Rating Distribution */}
      <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 1 }}>
        Performance Rating Distribution
      </Typography>
      <RatingDistributionChart distribution={data.ratingDistribution} />

      {/* Coverage Rate */}
      <CoverageRate
        coverageRate={data.coverageRate}
        covered={data.objectivesCovered}
        total={data.totalObjectives}
        uncovered={data.uncoveredObjectives}
      />

      {/* By Evaluator */}
      <ByEvaluatorTable evaluators={data.byEvaluator} />

      {/* By Phase */}
      <ByPhaseTable phases={data.byPhase} />

      {/* Data Quality */}
      <DataQuality data={data} />
    </Box>
  )
}

export default ObservationSummaryPanel
