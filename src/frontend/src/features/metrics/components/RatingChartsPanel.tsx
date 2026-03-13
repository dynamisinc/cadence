/**
 * RatingChartsPanel Component (S05)
 *
 * Interactive P/S/M/U distribution charts for AAR presentations.
 * Provides donut chart, stacked bar by phase, and export capabilities.
 */

import { useState, useRef, useCallback } from 'react'
import {
  Box,
  Typography,
  Stack,
  Paper,
  ToggleButton,
  ToggleButtonGroup,
  Skeleton,
  Alert,
  Button,
  useTheme,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChartPie,
  faChartBar,
  faDownload,
} from '@fortawesome/free-solid-svg-icons'
import {
  PieChart,
  Pie,
  Cell,
  ResponsiveContainer,
  Tooltip,
  Legend,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
} from 'recharts'

import { cobraTheme } from '@/theme/cobraTheme'
import { useObservationSummary } from '../hooks/useObservationSummary'
import type {
  RatingDistributionDto,
  PhaseObservationSummaryDto,
} from '../types'

interface RatingChartsPanelProps {
  exerciseId: string
}

/**
 * HSEEP P/S/M/U rating colors — sourced from COBRA theme rating palette
 */
const RATING_COLORS = {
  performed: cobraTheme.palette.rating.performed.main,
  satisfactory: cobraTheme.palette.rating.satisfactory.main,
  marginal: cobraTheme.palette.rating.marginal.main,
  unsatisfactory: cobraTheme.palette.rating.unsatisfactory.main,
  unrated: cobraTheme.palette.rating.unrated.main,
}

type ViewMode = 'overall' | 'byPhase'

/**
 * Custom tooltip for donut chart
 */
interface DonutPayload {
  payload: { name: string; value: number; percentage: number }
}

const DonutTooltip = ({
  active,
  payload,
}: {
  active?: boolean
  payload?: Array<DonutPayload>
}) => {
  if (active && payload && payload.length) {
    const data = payload[0].payload
    return (
      <Paper elevation={3} sx={{ p: 1.5 }}>
        <Typography variant="body2" fontWeight="bold">
          {data.name}
        </Typography>
        <Typography variant="body2">
          Count: {data.value}
        </Typography>
        <Typography variant="body2">
          {data.percentage.toFixed(1)}%
        </Typography>
      </Paper>
    )
  }
  return null
}

/**
 * Custom tooltip for bar chart
 */
interface BarPayload {
  name: string
  value: number
  fill: string
}

const BarTooltip = ({
  active,
  payload,
  label,
}: {
  active?: boolean
  payload?: Array<BarPayload>
  label?: string
}) => {
  if (active && payload && payload.length) {
    const total = payload.reduce((sum, p) => sum + (p.value || 0), 0)
    return (
      <Paper elevation={3} sx={{ p: 1.5 }}>
        <Typography variant="body2" fontWeight="bold" gutterBottom>
          {label}
        </Typography>
        {payload.map((entry, index) => (
          <Typography
            key={index}
            variant="body2"
            sx={{ color: entry.fill }}
          >
            {entry.name}: {entry.value} (
            {total > 0 ? ((entry.value / total) * 100).toFixed(0) : 0}%)
          </Typography>
        ))}
        <Typography variant="body2" sx={{ mt: 0.5, pt: 0.5, borderTop: `1px solid ${cobraTheme.palette.neutral[200]}` }}>
          Total: {total}
        </Typography>
      </Paper>
    )
  }
  return null
}

/**
 * Overall donut chart view
 */
const OverallDonutChart = ({ distribution }: { distribution: RatingDistributionDto }) => {
  const total = distribution.performedCount +
    distribution.satisfactoryCount +
    distribution.marginalCount +
    distribution.unsatisfactoryCount +
    distribution.unratedCount

  const data = [
    { name: 'Performed (P)', value: distribution.performedCount, percentage: distribution.performedPercentage, color: RATING_COLORS.performed },
    { name: 'Satisfactory (S)', value: distribution.satisfactoryCount, percentage: distribution.satisfactoryPercentage, color: RATING_COLORS.satisfactory },
    { name: 'Marginal (M)', value: distribution.marginalCount, percentage: distribution.marginalPercentage, color: RATING_COLORS.marginal },
    { name: 'Unsatisfactory (U)', value: distribution.unsatisfactoryCount, percentage: distribution.unsatisfactoryPercentage, color: RATING_COLORS.unsatisfactory },
  ].filter(d => d.value > 0)

  if (distribution.unratedCount > 0) {
    data.push({ name: 'Unrated', value: distribution.unratedCount, percentage: distribution.unratedPercentage, color: RATING_COLORS.unrated })
  }

  if (total === 0) {
    return (
      <Alert severity="info" sx={{ my: 2 }}>
        No observations to display.
      </Alert>
    )
  }

  return (
    <Box sx={{ display: 'flex', alignItems: 'flex-start', flexWrap: 'wrap', gap: 4 }}>
      {/* Donut Chart */}
      <Box sx={{ width: 320, height: 320 }}>
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={data}
              cx="50%"
              cy="50%"
              innerRadius={60}
              outerRadius={120}
              dataKey="value"
              nameKey="name"
              paddingAngle={2}
            >
              {data.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={entry.color} cursor="pointer" />
              ))}
            </Pie>
            <Tooltip content={<DonutTooltip />} />
          </PieChart>
        </ResponsiveContainer>
        {/* Center label */}
        <Typography
          variant="h4"
          fontWeight="bold"
          sx={{
            position: 'relative',
            top: -190,
            textAlign: 'center',
            pointerEvents: 'none',
          }}
        >
          {total}
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{
            position: 'relative',
            top: -195,
            textAlign: 'center',
            pointerEvents: 'none',
          }}
        >
          observations
        </Typography>
      </Box>

      {/* Legend / Stats */}
      <Box sx={{ flex: 1, minWidth: 250 }}>
        <Stack spacing={2}>
          {data.map((item, index) => (
            <Stack key={index} direction="row" spacing={2} alignItems="center">
              <Box
                sx={{
                  width: 16,
                  height: 16,
                  borderRadius: 0.5,
                  bgcolor: item.color,
                  flexShrink: 0,
                }}
              />
              <Typography variant="body1" sx={{ minWidth: 160 }}>
                {item.name}
              </Typography>
              <Typography variant="body1" fontWeight="bold" sx={{ minWidth: 40, textAlign: 'right' }}>
                {item.value}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ minWidth: 50, textAlign: 'right' }}>
                {item.percentage.toFixed(1)}%
              </Typography>
            </Stack>
          ))}
        </Stack>

        {distribution.averageRating !== null && (
          <Box sx={{ mt: 3, pt: 2, borderTop: '1px solid', borderColor: 'divider' }}>
            <Typography variant="body2" color="text.secondary">
              Average Rating
            </Typography>
            <Typography variant="h5" fontWeight="bold">
              {distribution.averageRating.toFixed(2)}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Scale: P=1, S=2, M=3, U=4
            </Typography>
          </Box>
        )}
      </Box>
    </Box>
  )
}

/**
 * By Phase stacked bar chart view
 */
const ByPhaseBarChart = ({ phases }: { phases: PhaseObservationSummaryDto[] }) => {
  if (phases.length === 0) {
    return (
      <Alert severity="info" sx={{ my: 2 }}>
        No phase data available.
      </Alert>
    )
  }

  const data = phases.map(phase => ({
    name: phase.phaseName.length > 20 ? phase.phaseName.substring(0, 18) + '...' : phase.phaseName,
    fullName: phase.phaseName,
    Performed: phase.ratingCounts.performed,
    Satisfactory: phase.ratingCounts.satisfactory,
    Marginal: phase.ratingCounts.marginal,
    Unsatisfactory: phase.ratingCounts.unsatisfactory,
  }))

  return (
    <Box sx={{ width: '100%', height: 400 }}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart
          data={data}
          layout="vertical"
          margin={{ top: 20, right: 30, left: 120, bottom: 20 }}
        >
          <CartesianGrid strokeDasharray="3 3" horizontal={true} vertical={false} />
          <XAxis type="number" />
          <YAxis
            dataKey="name"
            type="category"
            tick={{ fontSize: 12 }}
            width={110}
          />
          <Tooltip content={<BarTooltip />} />
          <Legend />
          <Bar dataKey="Performed" stackId="a" fill={RATING_COLORS.performed} name="Performed (P)" />
          <Bar dataKey="Satisfactory" stackId="a" fill={RATING_COLORS.satisfactory} name="Satisfactory (S)" />
          <Bar dataKey="Marginal" stackId="a" fill={RATING_COLORS.marginal} name="Marginal (M)" />
          <Bar dataKey="Unsatisfactory" stackId="a" fill={RATING_COLORS.unsatisfactory} name="Unsatisfactory (U)" />
        </BarChart>
      </ResponsiveContainer>
    </Box>
  )
}

/**
 * Loading skeleton
 */
const LoadingSkeleton = () => (
  <Box>
    <Skeleton variant="rectangular" height={48} sx={{ mb: 3 }} />
    <Skeleton variant="circular" width={320} height={320} sx={{ mb: 2 }} />
    <Skeleton variant="rectangular" height={200} />
  </Box>
)

/**
 * Main RatingChartsPanel component
 */
export const RatingChartsPanel = ({ exerciseId }: RatingChartsPanelProps) => {
  const theme = useTheme()
  const chartRef = useRef<HTMLDivElement>(null)
  const [viewMode, setViewMode] = useState<ViewMode>('overall')
  const { data, isLoading, error } = useObservationSummary(exerciseId)

  const handleViewChange = (_event: React.MouseEvent<HTMLElement>, newView: ViewMode | null) => {
    if (newView !== null) {
      setViewMode(newView)
    }
  }

  const handleExport = useCallback(async () => {
    if (!chartRef.current) return

    try {
      // Dynamic import of html2canvas for PNG export
      const html2canvasModule = await import('html2canvas')
      const html2canvas = html2canvasModule.default

      const canvas = await html2canvas(chartRef.current, {
        backgroundColor: cobraTheme.palette.common.white,
        scale: 2, // High resolution
      })

      const link = document.createElement('a')
      link.download = `rating-distribution-${viewMode}-${new Date().toISOString().split('T')[0]}.png`
      link.href = canvas.toDataURL('image/png')
      link.click()
    } catch (err) {
      console.error('Failed to export chart:', err)
    }
  }, [viewMode])

  if (isLoading) {
    return <LoadingSkeleton />
  }

  if (error) {
    return (
      <Alert severity="error">
        Failed to load observation data for charts. Please try again.
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

  const hasObservations = data.totalCount > 0

  return (
    <Box>
      {/* Header with view toggle and export */}
      <Paper elevation={1} sx={{ p: 2, mb: 3 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={2}>
          <Stack direction="row" spacing={1} alignItems="center">
            <FontAwesomeIcon icon={faChartPie} color={theme.palette.primary.main} />
            <Typography variant="h6" fontWeight="bold">
              Performance Rating Analysis
            </Typography>
          </Stack>

          <Stack direction="row" spacing={2} alignItems="center">
            <ToggleButtonGroup
              value={viewMode}
              exclusive
              onChange={handleViewChange}
              size="small"
              aria-label="Chart view mode"
            >
              <ToggleButton value="overall" aria-label="Overall view">
                <FontAwesomeIcon icon={faChartPie} style={{ marginRight: 8 }} />
                Overall
              </ToggleButton>
              <ToggleButton value="byPhase" aria-label="By phase view">
                <FontAwesomeIcon icon={faChartBar} style={{ marginRight: 8 }} />
                By Phase
              </ToggleButton>
            </ToggleButtonGroup>

            <Button
              variant="outlined"
              size="small"
              startIcon={<FontAwesomeIcon icon={faDownload} />}
              onClick={handleExport}
              disabled={!hasObservations}
            >
              Export PNG
            </Button>
          </Stack>
        </Stack>
      </Paper>

      {/* Chart Area */}
      <Paper ref={chartRef} elevation={1} sx={{ p: 3 }}>
        {!hasObservations ? (
          <Alert severity="info" icon={<FontAwesomeIcon icon={faChartPie} />}>
            No observations have been recorded yet. Charts will appear here once
            observations are added.
          </Alert>
        ) : viewMode === 'overall' ? (
          <OverallDonutChart distribution={data.ratingDistribution} />
        ) : (
          <ByPhaseBarChart phases={data.byPhase} />
        )}
      </Paper>

      {/* Color Legend */}
      {hasObservations && (
        <Paper elevation={0} sx={{ p: 2, mt: 2, bgcolor: 'grey.50' }}>
          <Typography variant="caption" color="text.secondary" display="block" gutterBottom>
            Rating Scale (HSEEP Standard)
          </Typography>
          <Stack direction="row" spacing={3} flexWrap="wrap" useFlexGap>
            {[
              { label: 'Performed (P)', color: RATING_COLORS.performed, desc: 'Excellent performance' },
              { label: 'Satisfactory (S)', color: RATING_COLORS.satisfactory, desc: 'Adequate performance' },
              { label: 'Marginal (M)', color: RATING_COLORS.marginal, desc: 'Needs improvement' },
              { label: 'Unsatisfactory (U)', color: RATING_COLORS.unsatisfactory, desc: 'Significant gaps' },
            ].map((item, idx) => (
              <Stack key={idx} direction="row" spacing={1} alignItems="center">
                <Box
                  sx={{
                    width: 12,
                    height: 12,
                    borderRadius: 0.5,
                    bgcolor: item.color,
                  }}
                />
                <Typography variant="caption">
                  {item.label}
                </Typography>
              </Stack>
            ))}
          </Stack>
        </Paper>
      )}
    </Box>
  )
}

export default RatingChartsPanel
