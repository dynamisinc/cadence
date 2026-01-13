/**
 * ObservationList Component
 *
 * Displays a list of observations recorded during exercise conduct.
 * Shows rating badges, content, and optional recommendations.
 */

import {
  Box,
  Paper,
  List,
  ListItem,
  Typography,
  Stack,
  CircularProgress,
  IconButton,
  Tooltip,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPen, faTrash, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { format, parseISO } from 'date-fns'

import { RatingBadge } from './RatingBadge'
import type { ObservationDto } from '../types'

interface ObservationListProps {
  observations: ObservationDto[]
  loading?: boolean
  error?: string | null
  /** Can the user edit/delete observations? */
  canEdit?: boolean
  /** Called when user edits an observation */
  onEdit?: (observation: ObservationDto) => void
  /** Called when user deletes an observation */
  onDelete?: (observationId: string) => Promise<void>
  /** ID of observation currently being deleted */
  deletingId?: string | null
}

export const ObservationList = ({
  observations,
  loading = false,
  error = null,
  canEdit = false,
  onEdit,
  onDelete,
  deletingId = null,
}: ObservationListProps) => {
  const formatTime = (dateStr: string) => {
    try {
      return format(parseISO(dateStr), 'h:mm a')
    } catch {
      return ''
    }
  }

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    )
  }

  if (error) {
    return (
      <Paper sx={{ p: 2 }}>
        <Typography color="error">{error}</Typography>
      </Paper>
    )
  }

  if (observations.length === 0) {
    return (
      <Paper sx={{ p: 3, textAlign: 'center' }}>
        <Typography color="text.secondary">
          No observations recorded yet.
        </Typography>
      </Paper>
    )
  }

  return (
    <Paper>
      <List disablePadding>
        {observations.map((observation, index) => {
          const isDeleting = deletingId === observation.id

          return (
            <Box key={observation.id}>
              {index > 0 && <Divider />}
              <ListItem
                sx={{
                  py: 2,
                  px: 2,
                  alignItems: 'flex-start',
                }}
                secondaryAction={
                  canEdit ? (
                    <Stack direction="row" spacing={0.5}>
                      <Tooltip title="Edit">
                        <IconButton
                          size="small"
                          onClick={() => onEdit?.(observation)}
                          disabled={isDeleting}
                        >
                          <FontAwesomeIcon icon={faPen} size="sm" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Delete">
                        <IconButton
                          size="small"
                          onClick={() => onDelete?.(observation.id)}
                          disabled={isDeleting}
                          color="error"
                        >
                          {isDeleting ? (
                            <FontAwesomeIcon icon={faSpinner} spin size="sm" />
                          ) : (
                            <FontAwesomeIcon icon={faTrash} size="sm" />
                          )}
                        </IconButton>
                      </Tooltip>
                    </Stack>
                  ) : undefined
                }
              >
                <Stack spacing={1} sx={{ flex: 1, pr: canEdit ? 8 : 0 }}>
                  {/* Header with rating and timestamp */}
                  <Stack direction="row" spacing={2} alignItems="center">
                    <RatingBadge rating={observation.rating} />
                    <Typography variant="caption" color="text.secondary">
                      {formatTime(observation.createdAt)}
                    </Typography>
                    {observation.injectNumber && (
                      <Typography variant="caption" color="text.secondary">
                        Inject #{observation.injectNumber}
                      </Typography>
                    )}
                  </Stack>

                  {/* Observation content */}
                  <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                    {observation.content}
                  </Typography>

                  {/* Recommendation if present */}
                  {observation.recommendation && (
                    <Box
                      sx={{
                        mt: 1,
                        p: 1.5,
                        bgcolor: 'grey.50',
                        borderRadius: 1,
                        borderLeft: '3px solid',
                        borderLeftColor: 'primary.main',
                      }}
                    >
                      <Typography variant="caption" color="text.secondary" display="block">
                        Recommendation
                      </Typography>
                      <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                        {observation.recommendation}
                      </Typography>
                    </Box>
                  )}
                </Stack>
              </ListItem>
            </Box>
          )
        })}
      </List>
    </Paper>
  )
}

export default ObservationList
