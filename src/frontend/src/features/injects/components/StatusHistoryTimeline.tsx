import { useState } from 'react'
import {
  Box,
  Collapse,
  IconButton,
  Stack,
  Typography,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronDown,
  faChevronRight,
  faCircle,
} from '@fortawesome/free-solid-svg-icons'
import { formatSmartDateTime } from '../../../shared/utils/dateUtils'
import type { InjectStatusHistoryDto } from '../types'

interface StatusHistoryTimelineProps {
  history: InjectStatusHistoryDto[]
  loading?: boolean
}

const statusColors: Record<string, string> = {
  Draft: 'text.secondary',
  Submitted: 'info.main',
  Approved: 'success.main',
  Rejected: 'warning.main',
  Released: 'success.dark',
  Deferred: 'text.disabled',
  Synchronized: 'info.light',
}

const getStatusColor = (status: string): string =>
  statusColors[status] ?? 'text.secondary'

/**
 * Collapsible timeline showing inject status change history.
 * Displayed in the detail page right sidebar.
 */
export const StatusHistoryTimeline = ({
  history,
  loading,
}: StatusHistoryTimelineProps) => {
  const [expanded, setExpanded] = useState(false)

  if (loading || history.length === 0) return null

  return (
    <Box>
      <Stack
        direction="row"
        alignItems="center"
        spacing={0.5}
        onClick={() => setExpanded(!expanded)}
        sx={{ cursor: 'pointer', userSelect: 'none' }}
      >
        <IconButton size="small" aria-label={expanded ? 'Collapse history' : 'Expand history'}>
          <FontAwesomeIcon
            icon={expanded ? faChevronDown : faChevronRight}
            style={{ fontSize: '0.75rem' }}
          />
        </IconButton>
        <Typography variant="caption" color="text.secondary">
          Status History ({history.length})
        </Typography>
      </Stack>

      <Collapse in={expanded}>
        <Stack spacing={1.5} sx={{ pl: 1, pt: 1 }}>
          {history.map(entry => (
            <Stack key={entry.id} direction="row" spacing={1.5} alignItems="flex-start">
              <Box sx={{ pt: 0.5 }}>
                <FontAwesomeIcon
                  icon={faCircle}
                  style={{
                    fontSize: '0.5rem',
                    color: 'inherit',
                  }}
                />
              </Box>
              <Box sx={{ minWidth: 0 }}>
                <Typography variant="body2" sx={{ color: getStatusColor(entry.toStatus) }}>
                  {entry.fromStatus} &rarr; {entry.toStatus}
                </Typography>
                <Typography variant="caption" color="text.secondary" display="block">
                  {entry.changedByName ?? 'System'} &middot; {formatSmartDateTime(entry.changedAt)}
                </Typography>
                {entry.notes && (
                  <Typography
                    variant="caption"
                    color="text.secondary"
                    sx={{ fontStyle: 'italic', display: 'block', mt: 0.25 }}
                  >
                    {entry.notes}
                  </Typography>
                )}
              </Box>
            </Stack>
          ))}
        </Stack>
      </Collapse>
    </Box>
  )
}

export default StatusHistoryTimeline
