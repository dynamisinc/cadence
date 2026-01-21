/**
 * LaterSection
 *
 * Displays pending injects that are outside the 30-minute upcoming window
 * or don't have a delivery time set. Collapsed by default.
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
  faCalendarDays,
  faChevronDown,
  faChevronUp,
} from '@fortawesome/free-solid-svg-icons'

import type { InjectDto } from '../../../injects/types'
import { parseDeliveryTime, formatDeliveryTime, formatScenarioTime } from '../../../injects/types'

interface LaterSectionProps {
  /** Pending injects outside the 30-min window or without DeliveryTime */
  injects: InjectDto[]
  /** Called when inject row is clicked to open details drawer */
  onInjectClick?: (inject: InjectDto) => void
}

export const LaterSection = ({ injects, onInjectClick }: LaterSectionProps) => {
  const [expanded, setExpanded] = useState(false) // Collapsed by default

  // Don't render if no later injects
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
        <Box sx={{ color: 'text.secondary' }}>
          <FontAwesomeIcon icon={faCalendarDays} />
        </Box>

        <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 600 }}>
          LATER
        </Typography>

        <Typography variant="caption" color="text.secondary" sx={{ mr: 1 }}>
          remaining injects
        </Typography>

        <Chip label={injects.length} size="small" variant="outlined" />

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

                return (
                  <TableRow
                    key={inject.id}
                    sx={{
                      cursor: onInjectClick ? 'pointer' : 'default',
                      '&:hover': {
                        backgroundColor: 'action.hover',
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
                      <Typography variant="body2">
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
                      {deliveryTimeMs !== null ? (
                        <Typography
                          variant="body2"
                          fontFamily="monospace"
                          color="text.secondary"
                        >
                          {formatDeliveryTime(deliveryTimeMs)}
                        </Typography>
                      ) : (
                        <Typography
                          variant="body2"
                          color="text.disabled"
                          fontStyle="italic"
                        >
                          No time set
                        </Typography>
                      )}
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

                    {/* Sequence (for ordering reference) */}
                    <TableCell sx={{ width: 80 }}>
                      <Typography variant="caption" color="text.secondary">
                        Seq {inject.sequence}
                      </Typography>
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

export default LaterSection
