/**
 * CompletedSection
 *
 * Displays fired and skipped injects in a collapsible section.
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
import { formatDeliveryTime, parseDeliveryTime } from '../../../injects/types'

interface CompletedSectionProps {
  /** Fired and skipped injects */
  injects: InjectDto[]
  /** Whether section is expanded */
  expanded: boolean
  /** Called when expand/collapse button clicked */
  onToggle: () => void
}

export const CompletedSection = ({
  injects,
  expanded,
  onToggle,
}: CompletedSectionProps) => {
  // Don't render if no completed injects
  if (injects.length === 0) {
    return null
  }

  const firedCount = injects.filter(i => i.status === InjectStatus.Fired).length
  const skippedCount = injects.filter(i => i.status === InjectStatus.Skipped).length

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
              label={`${firedCount} fired`}
              size="small"
              color="success"
              variant="outlined"
            />
          )}
          {skippedCount > 0 && (
            <Chip
              label={`${skippedCount} skipped`}
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
                const isFired = inject.status === InjectStatus.Fired
                const isSkipped = inject.status === InjectStatus.Skipped
                const actionTime = isFired ? inject.firedAt : inject.skippedAt
                const actionBy = isFired ? inject.firedByName : inject.skippedByName
                const deliveryTimeMs = parseDeliveryTime(inject.deliveryTime)

                return (
                  <TableRow key={inject.id}>
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
                            {new Date(actionTime).toLocaleTimeString([], {
                              hour: '2-digit',
                              minute: '2-digit',
                            })}
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
