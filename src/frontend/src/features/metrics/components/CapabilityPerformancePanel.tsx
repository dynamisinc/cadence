/**
 * CapabilityPerformancePanel Component (S06)
 *
 * Displays core capability performance metrics for after-action review.
 * Shows P/S/M/U ratings broken down by FEMA Core Capability.
 */

import {
  Box,
  Typography,
  Stack,
  Paper,
  Skeleton,
  Alert,
  Chip,
  useTheme,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faShieldAlt,
  faChartLine,
  faExclamationTriangle,
  faCheckCircle,
  faTimesCircle,
  faChevronDown,
  faInfoCircle,
} from '@fortawesome/free-solid-svg-icons'

import { cobraTheme } from '@/theme/cobraTheme'
import { useCapabilityPerformance } from '../hooks/useCapabilityPerformance'
import type {
  CapabilityPerformanceSummaryDto,
  CapabilityPerformanceDto,
  CategorySummaryDto,
} from '../types'

interface CapabilityPerformancePanelProps {
  exerciseId: string
}

/**
 * Rating colors matching HSEEP P/S/M/U scale — sourced from COBRA theme rating palette
 */
const RATING_COLORS = {
  performed: cobraTheme.palette.rating.performed.main,
  satisfactory: cobraTheme.palette.rating.satisfactory.main,
  marginal: cobraTheme.palette.rating.marginal.main,
  unsatisfactory: cobraTheme.palette.rating.unsatisfactory.main,
}

/**
 * Get performance level color
 */
const getPerformanceColor = (level: string) => {
  switch (level) {
    case 'Good':
      return RATING_COLORS.performed
    case 'Satisfactory':
      return RATING_COLORS.satisfactory
    case 'Needs Improvement':
      return RATING_COLORS.marginal
    case 'Critical':
      return RATING_COLORS.unsatisfactory
    default:
      return cobraTheme.palette.rating.unrated.main
  }
}

/**
 * Get performance icon
 */
const getPerformanceIcon = (level: string) => {
  switch (level) {
    case 'Good':
      return faCheckCircle
    case 'Satisfactory':
      return faCheckCircle
    case 'Needs Improvement':
      return faExclamationTriangle
    case 'Critical':
      return faTimesCircle
    default:
      return faInfoCircle
  }
}

/**
 * Summary stats card
 */
const SummaryCard = ({ data }: { data: CapabilityPerformanceSummaryDto }) => {
  const theme = useTheme()

  return (
    <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faShieldAlt} color={theme.palette.primary.main} />
        <Typography variant="subtitle1" fontWeight="bold">
          Capability Summary
        </Typography>
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={4}>
        <Box>
          <Typography variant="body2" color="text.secondary">
            Capabilities Evaluated
          </Typography>
          <Typography variant="h5" fontWeight="bold">
            {data.capabilitiesEvaluated}
          </Typography>
        </Box>

        <Box>
          <Typography variant="body2" color="text.secondary">
            Tagged Observations
          </Typography>
          <Typography variant="h5" fontWeight="bold">
            {data.totalTaggedObservations} / {data.totalObservations}
          </Typography>
        </Box>

        <Box>
          <Typography variant="body2" color="text.secondary">
            Tagging Rate
          </Typography>
          <Typography
            variant="h5"
            fontWeight="bold"
            color={data.taggingRate >= 50 ? 'success.main' : 'warning.main'}
          >
            {data.taggingRate}%
          </Typography>
        </Box>

        {data.targetCapabilitiesCount > 0 && (
          <Box>
            <Typography variant="body2" color="text.secondary">
              Target Coverage
            </Typography>
            <Typography
              variant="h5"
              fontWeight="bold"
              color={
                data.targetCoverageRate !== null && data.targetCoverageRate >= 80
                  ? 'success.main'
                  : data.targetCoverageRate !== null && data.targetCoverageRate >= 60
                    ? 'warning.main'
                    : 'error.main'
              }
            >
              {data.targetCapabilitiesEvaluated} / {data.targetCapabilitiesCount}
              {data.targetCoverageRate !== null && (
                <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 1 }}>
                  ({data.targetCoverageRate}%)
                </Typography>
              )}
            </Typography>
          </Box>
        )}
      </Stack>
    </Paper>
  )
}

/**
 * Single capability card
 */
const CapabilityCard = ({ capability }: { capability: CapabilityPerformanceDto }) => {
  const ratingCounts = capability.ratingCounts

  return (
    <Paper
      elevation={1}
      sx={{
        p: 2,
        mb: 2,
        borderLeft: `4px solid ${getPerformanceColor(capability.performanceLevel)}`,
      }}
    >
      <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
        <Box sx={{ flex: 1 }}>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
            <FontAwesomeIcon
              icon={getPerformanceIcon(capability.performanceLevel)}
              color={getPerformanceColor(capability.performanceLevel)}
            />
            <Typography variant="subtitle1" fontWeight="bold">
              {capability.name}
            </Typography>
            {capability.isTargetCapability && (
              <Chip label="Target" size="small" color="primary" variant="outlined" />
            )}
          </Stack>

          <Stack direction="row" spacing={2} alignItems="center" sx={{ mb: 1 }}>
            <Chip
              label={capability.category}
              size="small"
              variant="outlined"
              sx={{ bgcolor: 'grey.100' }}
            />
            <Typography variant="body2" color="text.secondary">
              {capability.observationCount} observations
            </Typography>
          </Stack>
        </Box>

        <Box sx={{ textAlign: 'right' }}>
          {capability.averageRating !== null ? (
            <>
              <Typography
                variant="h5"
                fontWeight="bold"
                color={getPerformanceColor(capability.performanceLevel)}
              >
                {capability.averageRating.toFixed(1)}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {capability.ratingCategory}
              </Typography>
            </>
          ) : (
            <Typography variant="body2" color="text.secondary">
              No rated observations
            </Typography>
          )}
        </Box>
      </Stack>

      {/* Rating distribution */}
      <Box sx={{ mt: 2 }}>
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          {ratingCounts.performed > 0 && (
            <Chip
              label={`P: ${ratingCounts.performed}`}
              size="small"
              sx={{ bgcolor: RATING_COLORS.performed, color: 'white' }}
            />
          )}
          {ratingCounts.satisfactory > 0 && (
            <Chip
              label={`S: ${ratingCounts.satisfactory}`}
              size="small"
              sx={{ bgcolor: RATING_COLORS.satisfactory, color: 'white' }}
            />
          )}
          {ratingCounts.marginal > 0 && (
            <Chip
              label={`M: ${ratingCounts.marginal}`}
              size="small"
              sx={{ bgcolor: RATING_COLORS.marginal, color: 'white' }}
            />
          )}
          {ratingCounts.unsatisfactory > 0 && (
            <Chip
              label={`U: ${ratingCounts.unsatisfactory}`}
              size="small"
              sx={{ bgcolor: RATING_COLORS.unsatisfactory, color: 'white' }}
            />
          )}
          {ratingCounts.unrated > 0 && (
            <Chip
              label={`Unrated: ${ratingCounts.unrated}`}
              size="small"
              variant="outlined"
            />
          )}
        </Stack>
      </Box>
    </Paper>
  )
}

/**
 * Unevaluated targets alert
 */
const UnevaluatedTargetsAlert = ({ data }: { data: CapabilityPerformanceSummaryDto }) => {
  const theme = useTheme()

  if (data.unevaluatedTargets.length === 0) {
    return null
  }

  return (
    <Paper
      elevation={1}
      sx={{ p: 2, mb: 3, borderLeft: `4px solid ${theme.palette.warning.main}` }}
    >
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faExclamationTriangle} color={theme.palette.warning.main} />
        <Typography variant="subtitle1" fontWeight="bold">
          Unevaluated Target Capabilities ({data.unevaluatedTargets.length})
        </Typography>
      </Stack>

      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
        {data.unevaluatedTargets.map((cap, idx) => (
          <Chip
            key={idx}
            label={cap.name}
            size="small"
            color="warning"
            variant="outlined"
          />
        ))}
      </Stack>
    </Paper>
  )
}

/**
 * Category accordion
 */
const CategoryAccordion = ({ area }: { area: CategorySummaryDto }) => {
  const ratingCounts = area.ratingCounts

  return (
    <Accordion>
      <AccordionSummary expandIcon={<FontAwesomeIcon icon={faChevronDown} />}>
        <Stack direction="row" spacing={2} alignItems="center" sx={{ flex: 1, mr: 2 }}>
          <Typography fontWeight="medium">{area.category}</Typography>
          <Chip
            label={`${area.capabilitiesEvaluated} capabilities`}
            size="small"
            variant="outlined"
          />
          {area.averageRating !== null && (
            <Typography
              variant="body2"
              fontWeight="bold"
              color={
                area.averageRating <= 1.5
                  ? 'success.main'
                  : area.averageRating <= 2.5
                    ? 'info.main'
                    : area.averageRating <= 3.5
                      ? 'warning.main'
                      : 'error.main'
              }
            >
              Avg: {area.averageRating.toFixed(1)}
            </Typography>
          )}
        </Stack>
      </AccordionSummary>
      <AccordionDetails>
        <Stack direction="row" spacing={2} alignItems="center">
          <Typography variant="body2" color="text.secondary">
            {area.observationCount} observations
          </Typography>
          <Stack direction="row" spacing={1}>
            {ratingCounts.performed > 0 && (
              <Chip
                label={`P: ${ratingCounts.performed}`}
                size="small"
                sx={{ bgcolor: RATING_COLORS.performed, color: 'white' }}
              />
            )}
            {ratingCounts.satisfactory > 0 && (
              <Chip
                label={`S: ${ratingCounts.satisfactory}`}
                size="small"
                sx={{ bgcolor: RATING_COLORS.satisfactory, color: 'white' }}
              />
            )}
            {ratingCounts.marginal > 0 && (
              <Chip
                label={`M: ${ratingCounts.marginal}`}
                size="small"
                sx={{ bgcolor: RATING_COLORS.marginal, color: 'white' }}
              />
            )}
            {ratingCounts.unsatisfactory > 0 && (
              <Chip
                label={`U: ${ratingCounts.unsatisfactory}`}
                size="small"
                sx={{ bgcolor: RATING_COLORS.unsatisfactory, color: 'white' }}
              />
            )}
          </Stack>
        </Stack>
      </AccordionDetails>
    </Accordion>
  )
}

/**
 * Loading skeleton
 */
const LoadingSkeleton = () => (
  <Box>
    <Skeleton variant="rectangular" height={150} sx={{ mb: 3, borderRadius: 1 }} />
    <Skeleton variant="rectangular" height={100} sx={{ mb: 2, borderRadius: 1 }} />
    <Skeleton variant="rectangular" height={100} sx={{ mb: 2, borderRadius: 1 }} />
    <Skeleton variant="rectangular" height={100} sx={{ mb: 2, borderRadius: 1 }} />
  </Box>
)

/**
 * Main CapabilityPerformancePanel component
 */
export const CapabilityPerformancePanel = ({ exerciseId }: CapabilityPerformancePanelProps) => {
  const { data, isLoading, error } = useCapabilityPerformance(exerciseId)

  if (isLoading) {
    return <LoadingSkeleton />
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        Failed to load capability performance data. Please try again later.
      </Alert>
    )
  }

  if (!data) {
    return (
      <Alert severity="info" sx={{ mb: 2 }}>
        No capability performance data available for this exercise.
      </Alert>
    )
  }

  if (data.totalTaggedObservations === 0) {
    return (
      <Alert severity="info" sx={{ mb: 2 }} icon={<FontAwesomeIcon icon={faShieldAlt} />}>
        No observations have been tagged with core capabilities yet. Capability
        performance will appear here once evaluators tag their observations with
        FEMA Core Capabilities.
      </Alert>
    )
  }

  // Group capabilities by performance level
  const criticalCapabilities = data.capabilities.filter(c => c.performanceLevel === 'Critical')
  const needsImprovementCapabilities = data.capabilities.filter(c => c.performanceLevel === 'Needs Improvement')
  const satisfactoryCapabilities = data.capabilities.filter(c => c.performanceLevel === 'Satisfactory' || c.performanceLevel === 'Good')

  return (
    <Box>
      {/* Summary */}
      <SummaryCard data={data} />

      {/* Unevaluated Targets */}
      <UnevaluatedTargetsAlert data={data} />

      {/* Critical / Needs Improvement */}
      {(criticalCapabilities.length > 0 || needsImprovementCapabilities.length > 0) && (
        <Box sx={{ mb: 3 }}>
          <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }} color="warning.main">
            <FontAwesomeIcon icon={faExclamationTriangle} style={{ marginRight: 8 }} />
            Improvement Needed
          </Typography>
          {criticalCapabilities.map((cap, idx) => (
            <CapabilityCard key={idx} capability={cap} />
          ))}
          {needsImprovementCapabilities.map((cap, idx) => (
            <CapabilityCard key={idx} capability={cap} />
          ))}
        </Box>
      )}

      {/* Satisfactory Performance */}
      {satisfactoryCapabilities.length > 0 && (
        <Box sx={{ mb: 3 }}>
          <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }} color="success.main">
            <FontAwesomeIcon icon={faCheckCircle} style={{ marginRight: 8 }} />
            Satisfactory Performance
          </Typography>
          {satisfactoryCapabilities.map((cap, idx) => (
            <CapabilityCard key={idx} capability={cap} />
          ))}
        </Box>
      )}

      {/* By Category */}
      {data.byCategory.length > 0 && (
        <Box>
          <Typography variant="subtitle1" fontWeight="bold" sx={{ mb: 2 }}>
            <FontAwesomeIcon icon={faChartLine} style={{ marginRight: 8 }} />
            By Category
          </Typography>
          {data.byCategory.map((area, idx) => (
            <CategoryAccordion key={idx} area={area} />
          ))}
        </Box>
      )}
    </Box>
  )
}

export default CapabilityPerformancePanel
