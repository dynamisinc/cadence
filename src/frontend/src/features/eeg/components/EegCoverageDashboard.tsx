/**
 * EegCoverageDashboard Component
 *
 * Real-time dashboard showing EEG coverage metrics during exercise conduct.
 * Displays task coverage, rating distribution, and unevaluated tasks.
 */

import { useState, useMemo } from 'react'
import {
  Box,
  Typography,
  Paper,
  Stack,
  Chip,
  LinearProgress,
  Collapse,
  IconButton,
  Tooltip,
  Skeleton,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronDown,
  faChevronUp,
  faCheckCircle,
  faTriangleExclamation,
  faClipboardCheck,
  faArrowRight,
} from '@fortawesome/free-solid-svg-icons'

import { CobraLinkButton } from '@/theme/styledComponents'
import { useEegCoverage } from '../hooks/useEegEntries'
import {
  PerformanceRating,
  PERFORMANCE_RATING_SHORT_LABELS,
  PERFORMANCE_RATING_COLORS,
  type EegCoverageDto,
  type UnevaluatedTaskDto,
} from '../types'

interface EegCoverageDashboardProps {
  /** Exercise ID */
  exerciseId: string
  /** Whether to show compact mode (single line summary) */
  compact?: boolean
  /** Called when "Assess" is clicked for an unevaluated task */
  onAssessTask?: (taskId: string, capabilityTargetId: string) => void
  /** Called when "Details" is clicked to expand */
  onDetailsClick?: () => void
}

/**
 * Rating chip showing P/S/M/U with appropriate color
 */
const RatingChip = ({ rating }: { rating: PerformanceRating }) => (
  <Chip
    label={PERFORMANCE_RATING_SHORT_LABELS[rating]}
    size="small"
    sx={{
      backgroundColor: `${PERFORMANCE_RATING_COLORS[rating]}20`,
      color: PERFORMANCE_RATING_COLORS[rating],
      fontWeight: 700,
      minWidth: 28,
      height: 24,
    }}
  />
)

/**
 * Progress bar with coverage percentage
 */
const CoverageProgressBar = ({
  evaluated,
  total,
  percentage,
}: {
  evaluated: number
  total: number
  percentage: number
}) => {
  const isLowCoverage = percentage < 50

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={0.5}>
        <Typography variant="body2" color="text.secondary">
          {evaluated} of {total} tasks evaluated
        </Typography>
        <Typography
          variant="body2"
          fontWeight={600}
          color={isLowCoverage ? 'warning.main' : 'success.main'}
        >
          {percentage}%
        </Typography>
      </Stack>
      <LinearProgress
        variant="determinate"
        value={percentage}
        sx={{
          height: 8,
          borderRadius: 4,
          backgroundColor: 'grey.200',
          '& .MuiLinearProgress-bar': {
            backgroundColor: isLowCoverage ? 'warning.main' : 'success.main',
            borderRadius: 4,
          },
        }}
      />
    </Box>
  )
}

/**
 * Rating distribution bar
 */
const RatingDistributionBar = ({
  distribution,
  totalEntries,
}: {
  distribution: Record<PerformanceRating, number>
  totalEntries: number
}) => {
  if (totalEntries === 0) {
    return (
      <Typography variant="body2" color="text.secondary" fontStyle="italic">
        No entries yet
      </Typography>
    )
  }

  const ratingOrder = [
    PerformanceRating.Performed,
    PerformanceRating.SomeChallenges,
    PerformanceRating.MajorChallenges,
    PerformanceRating.UnableToPerform,
  ]

  return (
    <Box>
      {/* Visual bar */}
      <Box
        sx={{
          display: 'flex',
          height: 24,
          borderRadius: 1,
          overflow: 'hidden',
          mb: 1,
        }}
      >
        {ratingOrder.map(rating => {
          const count = distribution[rating] || 0
          const percentage = (count / totalEntries) * 100
          if (percentage === 0) return null

          return (
            <Tooltip
              key={rating}
              title={`${PERFORMANCE_RATING_SHORT_LABELS[rating]}: ${count} (${Math.round(percentage)}%)`}
            >
              <Box
                sx={{
                  width: `${percentage}%`,
                  backgroundColor: PERFORMANCE_RATING_COLORS[rating],
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: 'white',
                  fontSize: '0.75rem',
                  fontWeight: 700,
                  minWidth: percentage > 10 ? 24 : 0,
                }}
              >
                {percentage > 15 && PERFORMANCE_RATING_SHORT_LABELS[rating]}
              </Box>
            </Tooltip>
          )
        })}
      </Box>

      {/* Legend */}
      <Stack direction="row" spacing={2} flexWrap="wrap">
        {ratingOrder.map(rating => {
          const count = distribution[rating] || 0
          const percentage = totalEntries > 0 ? Math.round((count / totalEntries) * 100) : 0

          return (
            <Stack key={rating} direction="row" spacing={0.5} alignItems="center">
              <Box
                sx={{
                  width: 12,
                  height: 12,
                  borderRadius: 0.5,
                  backgroundColor: PERFORMANCE_RATING_COLORS[rating],
                }}
              />
              <Typography variant="caption" color="text.secondary">
                {PERFORMANCE_RATING_SHORT_LABELS[rating]}: {count} ({percentage}%)
              </Typography>
            </Stack>
          )
        })}
      </Stack>
    </Box>
  )
}

/**
 * Compact summary for inline display
 */
const CompactSummary = ({
  coverage,
  onDetailsClick,
}: {
  coverage: EegCoverageDto
  onDetailsClick?: () => void
}) => {
  const unevaluatedCount = coverage.totalTasks - coverage.evaluatedTasks
  const hasWarning = coverage.coveragePercentage < 50

  return (
    <Paper sx={{ p: 1.5 }}>
      <Stack direction="row" alignItems="center" spacing={2} flexWrap="wrap">
        <Stack direction="row" alignItems="center" spacing={1}>
          <FontAwesomeIcon
            icon={faClipboardCheck}
            style={{ color: hasWarning ? '#ed6c02' : '#2e7d32' }}
          />
          <Typography variant="body2" fontWeight={600}>
            EEG: {coverage.evaluatedTasks}/{coverage.totalTasks} tasks ({coverage.coveragePercentage}%)
          </Typography>
        </Stack>

        <Stack direction="row" spacing={0.5}>
          {Object.values(PerformanceRating).map(rating => {
            const count = coverage.ratingDistribution[rating] || 0
            if (count === 0) return null
            return (
              <Chip
                key={rating}
                label={`${PERFORMANCE_RATING_SHORT_LABELS[rating]}:${count}`}
                size="small"
                sx={{
                  backgroundColor: `${PERFORMANCE_RATING_COLORS[rating]}20`,
                  color: PERFORMANCE_RATING_COLORS[rating],
                  fontWeight: 600,
                  fontSize: '0.7rem',
                }}
              />
            )
          })}
        </Stack>

        {unevaluatedCount > 0 && (
          <Stack direction="row" alignItems="center" spacing={0.5}>
            <FontAwesomeIcon icon={faTriangleExclamation} style={{ color: '#ed6c02' }} />
            <Typography variant="body2" color="warning.main">
              {unevaluatedCount} tasks pending
            </Typography>
          </Stack>
        )}

        {onDetailsClick && (
          <CobraLinkButton size="small" onClick={onDetailsClick}>
            Details
          </CobraLinkButton>
        )}
      </Stack>
    </Paper>
  )
}

/**
 * EEG Coverage Dashboard
 *
 * Features:
 * - Task coverage progress bar with percentage
 * - P/S/M/U rating distribution visualization
 * - Breakdown by capability target
 * - List of unevaluated tasks with "Assess" action
 */
export const EegCoverageDashboard = ({
  exerciseId,
  compact = false,
  onAssessTask,
  onDetailsClick,
}: EegCoverageDashboardProps) => {
  const { coverage, loading, error } = useEegCoverage(exerciseId)
  const [unevaluatedExpanded, setUnevaluatedExpanded] = useState(true)
  const [capabilityTargetsExpanded, setCapabilityTargetsExpanded] = useState(true)

  // Calculate total entries from rating distribution
  const totalEntries = useMemo(() => {
    if (!coverage) return 0
    return Object.values(coverage.ratingDistribution).reduce((sum, count) => sum + count, 0)
  }, [coverage])

  // Group unevaluated tasks by capability target
  const groupedUnevaluatedTasks = useMemo(() => {
    if (!coverage?.unevaluatedTasks) return new Map<string, UnevaluatedTaskDto[]>()

    const groups = new Map<string, UnevaluatedTaskDto[]>()
    for (const task of coverage.unevaluatedTasks) {
      const key = task.capabilityTargetDescription
      if (!groups.has(key)) {
        groups.set(key, [])
      }
      groups.get(key)!.push(task)
    }
    return groups
  }, [coverage?.unevaluatedTasks])

  // Loading state
  if (loading && !coverage) {
    return (
      <Paper sx={{ p: 2 }}>
        <Skeleton variant="text" width="60%" height={24} />
        <Skeleton variant="rectangular" height={8} sx={{ my: 1 }} />
        <Skeleton variant="text" width="80%" />
      </Paper>
    )
  }

  // Error state
  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        {error}
      </Alert>
    )
  }

  // No coverage data
  if (!coverage) {
    return (
      <Alert severity="info" sx={{ mb: 2 }}>
        No EEG data available. Add capability targets and critical tasks to enable evaluation tracking.
      </Alert>
    )
  }

  // Compact mode
  if (compact) {
    return <CompactSummary coverage={coverage} onDetailsClick={onDetailsClick} />
  }

  // All tasks evaluated
  const allEvaluated = coverage.evaluatedTasks === coverage.totalTasks && coverage.totalTasks > 0

  return (
    <Paper sx={{ p: 2 }}>
      <Typography variant="h6" gutterBottom>
        EEG Coverage
      </Typography>

      {/* All tasks evaluated success state */}
      {allEvaluated && (
        <Alert
          severity="success"
          icon={<FontAwesomeIcon icon={faCheckCircle} />}
          sx={{ mb: 2 }}
        >
          <Typography variant="subtitle2">All Critical Tasks Evaluated</Typography>
          <Typography variant="body2">
            {coverage.totalTasks} of {coverage.totalTasks} tasks have at least one EEG entry.
          </Typography>
        </Alert>
      )}

      {/* Task Coverage Section */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="subtitle2" color="text.secondary" gutterBottom>
          Task Coverage
        </Typography>
        <CoverageProgressBar
          evaluated={coverage.evaluatedTasks}
          total={coverage.totalTasks}
          percentage={coverage.coveragePercentage}
        />
        {coverage.coveragePercentage < 50 && coverage.totalTasks > 0 && (
          <Stack direction="row" alignItems="center" spacing={0.5} mt={1}>
            <FontAwesomeIcon icon={faTriangleExclamation} style={{ color: '#ed6c02' }} />
            <Typography variant="body2" color="warning.main">
              {coverage.totalTasks - coverage.evaluatedTasks} tasks need evaluation
            </Typography>
          </Stack>
        )}
      </Box>

      {/* Rating Distribution Section */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="subtitle2" color="text.secondary" gutterBottom>
          Rating Distribution
        </Typography>
        <RatingDistributionBar
          distribution={coverage.ratingDistribution}
          totalEntries={totalEntries}
        />
      </Box>

      {/* By Capability Target Section */}
      {coverage.byCapabilityTarget.length > 0 && (
        <Box sx={{ mb: 3 }}>
          <Stack
            direction="row"
            alignItems="center"
            justifyContent="space-between"
            sx={{ cursor: 'pointer' }}
            onClick={() => setCapabilityTargetsExpanded(!capabilityTargetsExpanded)}
          >
            <Typography variant="subtitle2" color="text.secondary">
              By Capability Target
            </Typography>
            <IconButton size="small">
              <FontAwesomeIcon
                icon={capabilityTargetsExpanded ? faChevronUp : faChevronDown}
              />
            </IconButton>
          </Stack>

          <Collapse in={capabilityTargetsExpanded}>
            <Stack spacing={1} mt={1}>
              {coverage.byCapabilityTarget.map(target => {
                const isComplete = target.evaluatedTasks === target.totalTasks
                const hasEntries = target.evaluatedTasks > 0

                return (
                  <Box
                    key={target.id}
                    sx={{
                      p: 1.5,
                      borderRadius: 1,
                      backgroundColor: 'grey.50',
                      borderLeft: 3,
                      borderColor: isComplete ? 'success.main' : hasEntries ? 'warning.main' : 'error.main',
                    }}
                  >
                    <Stack
                      direction="row"
                      justifyContent="space-between"
                      alignItems="center"
                      flexWrap="wrap"
                      gap={1}
                    >
                      <Box sx={{ flex: 1, minWidth: 200 }}>
                        <Typography variant="body2" fontWeight={600}>
                          {target.capabilityName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary" noWrap>
                          {target.targetDescription}
                        </Typography>
                      </Box>

                      <Stack direction="row" alignItems="center" spacing={1}>
                        <Chip
                          label={`${target.evaluatedTasks}/${target.totalTasks}`}
                          size="small"
                          color={isComplete ? 'success' : target.evaluatedTasks === 0 ? 'error' : 'warning'}
                          variant="outlined"
                        />

                        {target.taskRatings.length > 0 && (
                          <Stack direction="row" spacing={0.25}>
                            {target.taskRatings
                              .filter(t => t.latestRating)
                              .map((taskRating, idx) => (
                                <RatingChip key={idx} rating={taskRating.latestRating!} />
                              ))}
                          </Stack>
                        )}
                      </Stack>
                    </Stack>
                  </Box>
                )
              })}
            </Stack>
          </Collapse>
        </Box>
      )}

      {/* Unevaluated Tasks Section */}
      {coverage.unevaluatedTasks.length > 0 && (
        <Box>
          <Stack
            direction="row"
            alignItems="center"
            justifyContent="space-between"
            sx={{ cursor: 'pointer' }}
            onClick={() => setUnevaluatedExpanded(!unevaluatedExpanded)}
          >
            <Stack direction="row" alignItems="center" spacing={1}>
              <FontAwesomeIcon icon={faTriangleExclamation} style={{ color: '#ed6c02' }} />
              <Typography variant="subtitle2" color="warning.main">
                Tasks Needing Evaluation ({coverage.unevaluatedTasks.length})
              </Typography>
            </Stack>
            <IconButton size="small">
              <FontAwesomeIcon icon={unevaluatedExpanded ? faChevronUp : faChevronDown} />
            </IconButton>
          </Stack>

          <Collapse in={unevaluatedExpanded}>
            <Stack spacing={2} mt={1}>
              {Array.from(groupedUnevaluatedTasks.entries()).map(([capTargetDesc, tasks]) => (
                <Box key={capTargetDesc}>
                  <Typography variant="body2" fontWeight={600} color="text.secondary" gutterBottom>
                    {capTargetDesc}
                  </Typography>
                  <Stack spacing={0.5}>
                    {tasks.map(task => (
                      <Stack
                        key={task.taskId}
                        direction="row"
                        alignItems="center"
                        justifyContent="space-between"
                        sx={{
                          p: 1,
                          borderRadius: 1,
                          backgroundColor: 'grey.50',
                          '&:hover': {
                            backgroundColor: 'grey.100',
                          },
                        }}
                      >
                        <Typography variant="body2" sx={{ flex: 1 }}>
                          {task.taskDescription}
                        </Typography>
                        {onAssessTask && (
                          <CobraLinkButton
                            size="small"
                            onClick={() => onAssessTask(task.taskId, task.capabilityTargetId)}
                            endIcon={<FontAwesomeIcon icon={faArrowRight} />}
                          >
                            Assess
                          </CobraLinkButton>
                        )}
                      </Stack>
                    ))}
                  </Stack>
                </Box>
              ))}
            </Stack>
          </Collapse>
        </Box>
      )}

      {/* No tasks message */}
      {coverage.totalTasks === 0 && (
        <Alert severity="info">
          No critical tasks defined. Add capability targets and critical tasks in the EEG Setup to
          enable evaluation tracking.
        </Alert>
      )}
    </Paper>
  )
}

export default EegCoverageDashboard
