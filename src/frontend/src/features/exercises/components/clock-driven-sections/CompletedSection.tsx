/**
 * CompletedSection
 *
 * Displays released and deferred injects in a collapsible section.
 * Collapsed by default to keep focus on upcoming work.
 *
 * @module features/exercises
 * @see exercise-config/S06-clock-driven-conduct-view
 */

import {
  Box,
  Typography,
  Paper,
  Stack,
  IconButton,
  Collapse,
  Chip,
  Table,
  TableBody,
  TableRow,
  TableCell,
  TableContainer,
  Tooltip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faCheck,
  faChevronDown,
  faChevronUp,
  faForwardStep,
} from '@fortawesome/free-solid-svg-icons'

import type { InjectDto } from '../../../injects/types'
import { InjectStatus } from '../../../../types'
import { formatDeliveryTime, parseDeliveryTime, formatScenarioTime } from '../../../injects/types'
import { formatTime } from '@/shared/utils/dateUtils'

interface CompletedSectionProps {
  /** Released and deferred injects */
  injects: InjectDto[]
  /** Whether section is expanded */
  expanded: boolean
  /** Called when expand/collapse button clicked */
  onToggle: () => void
  /** Called when inject row is clicked to open details drawer */
  onInjectClick?: (inject: InjectDto) => void
}

export const CompletedSection = ({
  injects,
  expanded,
  onToggle,
  onInjectClick,
}: CompletedSectionProps) => {
  // Don't render if no completed injects
  if (injects.length === 0) {
    return null
  }

  const firedCount = injects.filter(i => i.status === InjectStatus.Released).length
  const skippedCount = injects.filter(i => i.status === InjectStatus.Deferred).length

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
        onClick={onToggle}
      >
        <Box sx={{ color: 'success.main' }}>
          <FontAwesomeIcon icon={faCheck} />
        </Box>

        <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 600 }}>
          COMPLETED
        </Typography>

        <Stack direction="row" spacing={1}>
          {firedCount > 0 && (
            <Chip
              label={`${firedCount} released`}
              size="small"
              color="success"
              variant="outlined"
            />
          )}
          {skippedCount > 0 && (
            <Chip
              label={`${skippedCount} deferred`}
              size="small"
              color="warning"
              variant="outlined"
            />
          )}
        </Stack>

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
                const isFired = inject.status === InjectStatus.Released
                const isSkipped = inject.status === InjectStatus.Deferred
                const actionTime = isFired ? inject.firedAt : inject.skippedAt
                const actionBy = isFired ? inject.firedByName : inject.skippedByName
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
                      {deliveryTimeMs !== null && (
                        <Typography
                          variant="body2"
                          fontFamily="monospace"
                          color="text.secondary"
                        >
                          {formatDeliveryTime(deliveryTimeMs)}
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

                    {/* Status */}
                    <TableCell sx={{ width: 120 }}>
                      <Stack spacing={0.5}>
                        <Chip
                          icon={
                            <FontAwesomeIcon
                              icon={isFired ? faCheck : faForwardStep}
                              size="xs"
                            />
                          }
                          label={inject.status}
                          size="small"
                          color={isFired ? 'success' : 'warning'}
                        />
                        {actionTime && (
                          <Typography variant="caption" color="text.secondary">
                            {formatTime(actionTime)}
                          </Typography>
                        )}
                        {actionBy && (
                          <Typography variant="caption" color="text.secondary">
                            by {actionBy}
                          </Typography>
                        )}
                      </Stack>
                    </TableCell>

                    {/* Skip Reason */}
                    {isSkipped && inject.skipReason && (
                      <TableCell sx={{ width: 200 }}>
                        <Tooltip title={inject.skipReason} placement="top">
                          <Typography
                            variant="caption"
                            color="text.secondary"
                            sx={{
                              fontStyle: 'italic',
                              display: 'block',
                              overflow: 'hidden',
                              textOverflow: 'ellipsis',
                              whiteSpace: 'nowrap',
                            }}
                          >
                            "{inject.skipReason}"
                          </Typography>
                        </Tooltip>
                      </TableCell>
                    )}
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

export default CompletedSection
