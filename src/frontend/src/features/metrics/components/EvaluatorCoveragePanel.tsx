/**
 * EvaluatorCoveragePanel Component (S08)
 *
 * Displays evaluator coverage metrics for after-action review.
 * Shows observation distribution, objective coverage, and rating consistency per evaluator.
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
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faUserCheck,
  faCheckCircle,
  faExclamationTriangle,
  faTimesCircle,
  faChartPie,
  faLayerGroup,
  faBalanceScale,
  faArrowUp,
  faArrowDown,
} from '@fortawesome/free-solid-svg-icons'

import { useEvaluatorCoverage } from '../hooks/useEvaluatorCoverage'
import type {
  EvaluatorCoverageDto,
  EvaluatorCoverageSummaryDto,
  EvaluatorConsistencyDto,
  ObjectiveCoverageRowDto,
} from '../types'

interface EvaluatorCoveragePanelProps {
  exerciseId: string
}

/**
 * Format rating to display string
 */
const formatRating = (rating: number | null): string => {
  if (rating === null) return 'N/A'
  if (rating <= 1.5) return 'Performed'
  if (rating <= 2.5) return 'Satisfactory'
  if (rating <= 3.5) return 'Marginal'
  return 'Unsatisfactory'
}

/**
 * Get rating color
 */
const getRatingColor = (rating: number | null, theme: ReturnType<typeof useTheme>) => {
  if (rating === null) return theme.palette.text.secondary
  if (rating <= 1.5) return theme.palette.success.main
  if (rating <= 2.5) return theme.palette.info.main
  if (rating <= 3.5) return theme.palette.warning.main
  return theme.palette.error.main
}

/**
 * Summary stats card
 */
const SummaryCard = ({ data }: { data: EvaluatorCoverageSummaryDto }) => {
  const theme = useTheme()

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faChartPie} color={theme.palette.primary.main} />
        <Typography variant="subtitle1" fontWeight="bold">
          Summary
        </Typography>
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={4} flexWrap="wrap" useFlexGap>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Evaluators
          </Typography>
          <Typography variant="h5" fontWeight="bold">
            {data.totalEvaluators}
          </Typography>
        </Box>

        <Box>
          <Typography variant="body2" color="text.secondary">
            Total Observations
          </Typography>
          <Typography variant="h5" fontWeight="bold">
            {data.totalObservations}
          </Typography>
        </Box>

        <Box>
          <Typography variant="body2" color="text.secondary">
            Objectives Covered
          </Typography>
          <Typography variant="h5" fontWeight="bold">
            {data.objectivesCovered} / {data.totalObjectives}
          </Typography>
        </Box>

        {data.capabilitiesCovered > 0 && (
          <Box>
            <Typography variant="body2" color="text.secondary">
              Capabilities Covered
            </Typography>
            <Typography variant="h5" fontWeight="bold">
              {data.capabilitiesCovered}
            </Typography>
          </Box>
        )}

        {data.objectiveCoverageRate !== null && (
          <Box>
            <Typography variant="body2" color="text.secondary">
              Coverage Rate
            </Typography>
            <Typography
              variant="h5"
              fontWeight="bold"
              color={data.objectiveCoverageRate >= 80 ? 'success.main' : data.objectiveCoverageRate >= 60 ? 'warning.main' : 'error.main'}
            >
              {data.objectiveCoverageRate}%
            </Typography>
          </Box>
        )}
      </Stack>
    </Paper>
  )
}

/**
 * Evaluator detail card
 */
const EvaluatorCard = ({ evaluator }: { evaluator: EvaluatorCoverageDto }) => {
  const _theme = useTheme()
  const ratingCounts = evaluator.ratingCounts
  const _total = ratingCounts.performed + ratingCounts.satisfactory + ratingCounts.marginal + ratingCounts.unsatisfactory + ratingCounts.unrated

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 2 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faUserCheck} />
        <Typography variant="subtitle1" fontWeight="bold">
          {evaluator.evaluatorName}
        </Typography>
        <Chip
          label={`${evaluator.observationCount} observations`}
          size="small"
          color="primary"
          variant="outlined"
        />
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={4} sx={{ mb: 2 }} flexWrap="wrap" useFlexGap>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Objectives Covered
          </Typography>
          <Typography variant="h6" fontWeight="bold">
            {evaluator.objectivesCovered}
          </Typography>
        </Box>

        {evaluator.capabilitiesCovered > 0 && (
          <Box>
            <Typography variant="body2" color="text.secondary">
              Capabilities Covered
            </Typography>
            <Typography variant="h6" fontWeight="bold">
              {evaluator.capabilitiesCovered}
            </Typography>
          </Box>
        )}

        <Box>
          <Typography variant="body2" color="text.secondary">
            Avg. Rating
          </Typography>
          <Typography
            variant="h6"
            fontWeight="bold"
            color={getRatingColor(evaluator.averageRating, theme)}
          >
            {evaluator.averageRating !== null ? evaluator.averageRating.toFixed(1) : 'N/A'}
            {evaluator.averageRating !== null && (
              <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.5 }}>
                ({formatRating(evaluator.averageRating)})
              </Typography>
            )}
          </Typography>
        </Box>
      </Stack>

      {/* Rating distribution */}
      <Box sx={{ mb: 2 }}>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Rating Distribution
        </Typography>
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          {ratingCounts.performed > 0 && (
            <Chip label={`P: ${ratingCounts.performed}`} size="small" sx={{ bgcolor: theme.palette.success.main, color: 'white' }} />
          )}
          {ratingCounts.satisfactory > 0 && (
            <Chip label={`S: ${ratingCounts.satisfactory}`} size="small" sx={{ bgcolor: theme.palette.info.main, color: 'white' }} />
          )}
          {ratingCounts.marginal > 0 && (
            <Chip label={`M: ${ratingCounts.marginal}`} size="small" sx={{ bgcolor: theme.palette.warning.main, color: 'white' }} />
          )}
          {ratingCounts.unsatisfactory > 0 && (
            <Chip label={`U: ${ratingCounts.unsatisfactory}`} size="small" sx={{ bgcolor: theme.palette.error.main, color: 'white' }} />
          )}
          {ratingCounts.unrated > 0 && (
            <Chip label={`Unrated: ${ratingCounts.unrated}`} size="small" variant="outlined" />
          )}
        </Stack>
      </Box>

      {/* Phase activity */}
      {evaluator.phaseActivity.length > 0 && (
        <Box>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Active Phases
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {evaluator.phaseActivity.map((phase, idx) => (
              <Chip
                key={idx}
                label={`${phase.phaseName}: ${phase.observationCount}`}
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
 * Coverage matrix
 */
const CoverageMatrix = ({
  matrix,
  evaluators,
}: {
  matrix: ObjectiveCoverageRowDto[]
  evaluators: EvaluatorCoverageDto[]
}) => {
  const theme = useTheme()

  if (matrix.length === 0) {
    return null
  }

  const getCoverageIcon = (status: string) => {
    switch (status) {
      case 'Good':
        return <FontAwesomeIcon icon={faCheckCircle} color={theme.palette.success.main} />
      case 'Low':
        return <FontAwesomeIcon icon={faExclamationTriangle} color={theme.palette.warning.main} />
      case 'None':
        return <FontAwesomeIcon icon={faTimesCircle} color={theme.palette.error.main} />
      default:
        return null
    }
  }

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faLayerGroup} />
        <Typography variant="subtitle1" fontWeight="bold">
          Coverage Matrix
        </Typography>
      </Stack>

      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Objective</TableCell>
              {evaluators.map((e, idx) => (
                <TableCell key={idx} align="center" sx={{ minWidth: 80 }}>
                  {e.evaluatorName.split(' ')[0]}
                </TableCell>
              ))}
              <TableCell align="center">Total</TableCell>
              <TableCell align="center">Status</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {matrix.map((row, idx) => (
              <TableRow key={idx}>
                <TableCell>
                  <Typography variant="body2" fontWeight="medium">
                    {row.objectiveNumber}. {row.objectiveName}
                  </Typography>
                </TableCell>
                {evaluators.map((e, eIdx) => (
                  <TableCell key={eIdx} align="center">
                    {e.evaluatorId && row.byEvaluator[e.evaluatorId] > 0
                      ? row.byEvaluator[e.evaluatorId]
                      : '-'}
                  </TableCell>
                ))}
                <TableCell align="center">
                  <Typography variant="body2" fontWeight="bold">
                    {row.totalObservations}
                  </Typography>
                </TableCell>
                <TableCell align="center">
                  {getCoverageIcon(row.coverageStatus)}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Stack direction="row" spacing={2} sx={{ mt: 2 }}>
        <Stack direction="row" spacing={0.5} alignItems="center">
          <FontAwesomeIcon icon={faCheckCircle} color={theme.palette.success.main} size="sm" />
          <Typography variant="caption">Good (3+)</Typography>
        </Stack>
        <Stack direction="row" spacing={0.5} alignItems="center">
          <FontAwesomeIcon icon={faExclamationTriangle} color={theme.palette.warning.main} size="sm" />
          <Typography variant="caption">Low (1-2)</Typography>
        </Stack>
        <Stack direction="row" spacing={0.5} alignItems="center">
          <FontAwesomeIcon icon={faTimesCircle} color={theme.palette.error.main} size="sm" />
          <Typography variant="caption">None (0)</Typography>
        </Stack>
      </Stack>
    </Paper>
  )
}

/**
 * Coverage gaps alert
 */
const CoverageGaps = ({ data }: { data: EvaluatorCoverageSummaryDto }) => {
  const theme = useTheme()

  if (data.uncoveredObjectives.length === 0 && data.lowCoverageObjectives.length === 0) {
    return null
  }

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3, borderLeft: `4px solid ${theme.palette.warning.main}` }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faExclamationTriangle} color={theme.palette.warning.main} />
        <Typography variant="subtitle1" fontWeight="bold">
          Coverage Gaps
        </Typography>
      </Stack>

      {data.uncoveredObjectives.length > 0 && (
        <Box sx={{ mb: 2 }}>
          <Typography variant="body2" color="error.main" fontWeight="medium" gutterBottom>
            No Observations ({data.uncoveredObjectives.length})
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {data.uncoveredObjectives.map((obj, idx) => (
              <Chip
                key={idx}
                label={`${obj.objectiveNumber}. ${obj.name}`}
                size="small"
                color="error"
                variant="outlined"
              />
            ))}
          </Stack>
        </Box>
      )}

      {data.lowCoverageObjectives.length > 0 && (
        <Box>
          <Typography variant="body2" color="warning.main" fontWeight="medium" gutterBottom>
            Low Coverage ({data.lowCoverageObjectives.length})
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {data.lowCoverageObjectives.map((obj, idx) => (
              <Chip
                key={idx}
                label={`${obj.objectiveNumber}. ${obj.name} (${obj.observationCount})`}
                size="small"
                color="warning"
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
 * Consistency indicator card
 */
const ConsistencyIndicator = ({ consistency }: { consistency: EvaluatorConsistencyDto | null }) => {
  const theme = useTheme()

  if (!consistency) {
    return null
  }

  const getConsistencyColor = (level: string) => {
    switch (level) {
      case 'High':
        return theme.palette.success.main
      case 'Moderate':
        return theme.palette.info.main
      case 'Low':
        return theme.palette.warning.main
      default:
        return theme.palette.text.secondary
    }
  }

  const getConsistencyIcon = (level: string) => {
    switch (level) {
      case 'High':
        return faCheckCircle
      case 'Moderate':
        return faBalanceScale
      case 'Low':
        return faExclamationTriangle
      default:
        return faBalanceScale
    }
  }

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faBalanceScale} color={theme.palette.primary.main} />
        <Typography variant="subtitle1" fontWeight="bold">
          Evaluator Consistency
        </Typography>
        <Chip
          icon={<FontAwesomeIcon icon={getConsistencyIcon(consistency.level)} />}
          label={consistency.level}
          size="small"
          sx={{
            bgcolor: getConsistencyColor(consistency.level),
            color: 'white',
            '& .MuiChip-icon': { color: 'white' },
          }}
        />
      </Stack>

      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {consistency.description}
      </Typography>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={4} sx={{ mb: 2 }}>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Overall Avg. Rating
          </Typography>
          <Typography variant="h6" fontWeight="bold">
            {consistency.overallAverageRating.toFixed(2)}
          </Typography>
        </Box>

        <Box>
          <Typography variant="body2" color="text.secondary">
            Rating Std. Deviation
          </Typography>
          <Typography variant="h6" fontWeight="bold">
            {consistency.ratingStandardDeviation.toFixed(2)}
          </Typography>
        </Box>
      </Stack>

      {/* Harsh raters */}
      {consistency.harshRaters.length > 0 && (
        <Box sx={{ mb: 2 }}>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
            <FontAwesomeIcon icon={faArrowUp} color={theme.palette.error.main} size="sm" />
            <Typography variant="body2" color="error.main" fontWeight="medium">
              Harsher Than Average
            </Typography>
          </Stack>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {consistency.harshRaters.map((rater, idx) => (
              <Chip
                key={idx}
                label={`${rater.evaluatorName}: ${rater.averageRating.toFixed(1)} (+${rater.deviation.toFixed(1)})`}
                size="small"
                color="error"
                variant="outlined"
              />
            ))}
          </Stack>
        </Box>
      )}

      {/* Lenient raters */}
      {consistency.lenientRaters.length > 0 && (
        <Box>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
            <FontAwesomeIcon icon={faArrowDown} color={theme.palette.success.main} size="sm" />
            <Typography variant="body2" color="success.main" fontWeight="medium">
              More Lenient Than Average
            </Typography>
          </Stack>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {consistency.lenientRaters.map((rater, idx) => (
              <Chip
                key={idx}
                label={`${rater.evaluatorName}: ${rater.averageRating.toFixed(1)} (${rater.deviation.toFixed(1)})`}
                size="small"
                color="success"
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
    <Skeleton variant="rectangular" height={200} sx={{ mb: 3, borderRadius: 1 }} />
    <Skeleton variant="rectangular" height={150} sx={{ mb: 2, borderRadius: 1 }} />
    <Skeleton variant="rectangular" height={150} sx={{ mb: 2, borderRadius: 1 }} />
  </Box>
)

/**
 * Main EvaluatorCoveragePanel component
 */
export const EvaluatorCoveragePanel = ({ exerciseId }: EvaluatorCoveragePanelProps) => {
  const { data, isLoading, error } = useEvaluatorCoverage(exerciseId)

  if (isLoading) {
    return <LoadingSkeleton />
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        Failed to load evaluator coverage data. Please try again later.
      </Alert>
    )
  }

  if (!data) {
    return (
      <Alert severity="info" sx={{ mb: 2 }}>
        No evaluator coverage data available for this exercise.
      </Alert>
    )
  }

  if (data.totalEvaluators === 0) {
    return (
      <Alert severity="info" sx={{ mb: 2 }}>
        No observations have been recorded yet. Evaluator coverage will appear here once observations are added.
      </Alert>
    )
  }

  return (
    <Box>
      {/* Summary */}
      <SummaryCard data={data} />

      {/* Consistency Indicator */}
      <ConsistencyIndicator consistency={data.consistency} />

      {/* Coverage Gaps */}
      <CoverageGaps data={data} />

      {/* Coverage Matrix */}
      {data.coverageMatrix.length > 0 && (
        <CoverageMatrix matrix={data.coverageMatrix} evaluators={data.evaluators} />
      )}

      {/* Evaluator Details */}
      <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }}>
        Evaluator Details
      </Typography>

      {data.evaluators.map((evaluator, idx) => (
        <EvaluatorCard key={idx} evaluator={evaluator} />
      ))}
    </Box>
  )
}

export default EvaluatorCoveragePanel
