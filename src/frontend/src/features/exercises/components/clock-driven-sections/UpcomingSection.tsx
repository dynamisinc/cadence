/**
 * UpcomingSection
 *
 * Displays pending injects that will become ready within the next 30 minutes.
 * Shows countdown timers for each inject.
 *
 * @module features/exercises
 * @see exercise-config/S06-clock-driven-conduct-view
 */

import { useState } from 'react'
import {
  Box,
  Typography,
  Paper,
  IconButton,
  Collapse,
  Chip,
  Table,
  TableBody,
  TableRow,
  TableCell,
  TableContainer,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faClock,
  faChevronDown,
  faChevronUp,
} from '@fortawesome/free-solid-svg-icons'
import { keyframes } from '@mui/system'

import type { InjectDto } from '../../../injects/types'
import { parseDeliveryTime, formatDeliveryTime, formatScenarioTime } from '../../../injects/types'
import { formatCountdown } from '../../../injects/utils/clockDrivenGrouping'

// Pulse animation for imminent injects (< 5 min)
const pulse = keyframes`
  0%, 100% { opacity: 1; }
  50% { opacity: 0.6; }
`

const IMMINENT_THRESHOLD_MS = 5 * 60 * 1000 // 5 minutes

interface UpcomingSectionProps {
  /** Pending injects with DeliveryTime in the next 30 minutes */
  injects: InjectDto[]
  /** Current elapsed time in milliseconds */
  elapsedTimeMs: number
  /** Called when inject row is clicked to open details drawer */
  onInjectClick?: (inject: InjectDto) => void
}

export const UpcomingSection = ({
  injects,
  elapsedTimeMs,
  onInjectClick,
}: UpcomingSectionProps) => {
  const [expanded, setExpanded] = useState(true)

  // Don't render if no upcoming injects
  if (injects.length === 0) {
    return null
  }

  return (
    <Paper variant="outlined">
      {/* Section Header */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
          p: 2,
          backgroundColor: 'grey.50',
          cursor: 'pointer',
          '&:hover': {
            backgroundColor: 'grey.100',
          },
        }}
        onClick={() => setExpanded(!expanded)}
      >
        <Box sx={{ color: 'info.main' }}>
          <FontAwesomeIcon icon={faClock} />
        </Box>

        <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 600 }}>
          UPCOMING
        </Typography>

        <Typography variant="caption" color="text.secondary" sx={{ mr: 1 }}>
          next 30 min
        </Typography>

        <Chip label={injects.length} color="info" size="small" />

        <IconButton size="small">
          <FontAwesomeIcon icon={expanded ? faChevronUp : faChevronDown} />
        </IconButton>
      </Box>

      {/* Section Content */}
      <Collapse in={expanded}>
        <TableContainer>
          <Table size="small">
            <TableBody>
              {injects.map(inject => {
                const deliveryTimeMs = parseDeliveryTime(inject.deliveryTime)
                if (deliveryTimeMs === null) return null

                const timeUntilMs = deliveryTimeMs - elapsedTimeMs
                const isImminent = timeUntilMs > 0 && timeUntilMs <= IMMINENT_THRESHOLD_MS
                const countdown = formatCountdown(deliveryTimeMs, elapsedTimeMs)

                return (
                  <TableRow
                    key={inject.id}
                    sx={{
                      backgroundColor: isImminent ? 'warning.50' : 'inherit',
                      cursor: onInjectClick ? 'pointer' : 'default',
                      '&:hover': {
                        backgroundColor: isImminent ? 'warning.100' : 'action.hover',
                      },
                    }}
                    onClick={() => onInjectClick?.(inject)}
                  >
                    {/* Inject Number */}
                    <TableCell sx={{ width: 80 }}>
                      <Chip
                        label={`#${inject.injectNumber}`}
                        size="small"
                        variant="outlined"
                      />
                    </TableCell>

                    {/* Title */}
                    <TableCell>
                      <Typography variant="body2" fontWeight={isImminent ? 600 : 400}>
                        {inject.title}
                      </Typography>
                      {inject.target && (
                        <Typography variant="caption" color="text.secondary">
                          To: {inject.target}
                        </Typography>
                      )}
                    </TableCell>

                    {/* Delivery Time */}
                    <TableCell sx={{ width: 120 }}>
                      <Typography
                        variant="body2"
                        fontFamily="monospace"
                        color="text.secondary"
                      >
                        {formatDeliveryTime(deliveryTimeMs)}
                      </Typography>
                    </TableCell>

                    {/* Scenario Time */}
                    <TableCell sx={{ width: 120 }}>
                      {formatScenarioTime(inject.scenarioDay, inject.scenarioTime) && (
                        <Typography
                          variant="body2"
                          fontFamily="monospace"
                          color="info.main"
                        >
                          {formatScenarioTime(inject.scenarioDay, inject.scenarioTime)}
                        </Typography>
                      )}
                    </TableCell>

                    {/* Countdown */}
                    <TableCell sx={{ width: 120 }}>
                      <Chip
                        label={countdown}
                        size="small"
                        color={isImminent ? 'warning' : 'default'}
                        sx={{
                          fontFamily: 'monospace',
                          fontWeight: 600,
                          animation: isImminent ? `${pulse} 1.5s ease-in-out infinite` : 'none',
                        }}
                      />
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </TableContainer>
      </Collapse>
    </Paper>
  )
}

export default UpcomingSection
