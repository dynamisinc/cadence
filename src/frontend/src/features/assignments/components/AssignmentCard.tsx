/**
 * AssignmentCard Component
 *
 * Displays a single exercise assignment with role, status, and progress.
 */
import { Card, CardActionArea, CardContent, Typography, Box, Chip, LinearProgress } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlay,
  faPause,
  faStop,
  faCalendarAlt,
  faMapMarkerAlt,
  faClock,
  faCheckCircle,
} from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from 'react-router-dom'
import type { AssignmentDto, AssignmentSectionType } from '../types'
import { getDefaultRouteForRole, getRoleLabel, getRoleColor } from '../utils/roleRouting'
import CobraStyles from '@/theme/CobraStyles'
import { formatDate as sharedFormatDate } from '@/shared/utils/dateUtils'

interface AssignmentCardProps {
  assignment: AssignmentDto
  sectionType: AssignmentSectionType
}

/**
 * Format elapsed time as HH:MM:SS.
 */
function formatElapsedTime(seconds: number | null): string {
  if (seconds === null || seconds === 0) return 'Not Started'

  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const secs = Math.floor(seconds % 60)

  const pad = (n: number) => n.toString().padStart(2, '0')
  return pad(hours) + ':' + pad(minutes) + ':' + pad(secs)
}

/**
 * Format date for display.
 */
function formatDate(dateString: string): string {
  return sharedFormatDate(dateString)
}

/**
 * Get clock state icon and color.
 */
function getClockStateDisplay(clockState: string | null): {
  icon: typeof faPlay
  color: string
  label: string
} {
  switch (clockState) {
    case 'Running':
      return { icon: faPlay, color: '#4caf50', label: 'Running' }
    case 'Paused':
      return { icon: faPause, color: '#ff9800', label: 'Paused' }
    default:
      return { icon: faStop, color: '#9e9e9e', label: 'Stopped' }
  }
}

export function AssignmentCard({ assignment, sectionType }: AssignmentCardProps) {
  const navigate = useNavigate()

  const handleClick = () => {
    const route = getDefaultRouteForRole(
      assignment.exerciseId,
      assignment.role,
      assignment.exerciseStatus,
    )
    navigate(route)
  }

  const progress =
    assignment.totalInjects > 0
      ? (assignment.firedInjects / assignment.totalInjects) * 100
      : 0

  const clockDisplay = getClockStateDisplay(assignment.clockState)
  const roleColor = getRoleColor(assignment.role)

  return (
    <Card
      sx={{
        mb: 2,
        '&:hover': {
          boxShadow: 3,
        },
      }}
    >
      <CardActionArea onClick={handleClick}>
        <CardContent sx={{ padding: CobraStyles.Padding.Card }}>
          {/* Header Row */}
          <Box
            display="flex"
            justifyContent="space-between"
            alignItems="flex-start"
            mb={1}
          >
            <Box flex={1}>
              <Typography variant="h6" component="div" gutterBottom>
                {assignment.exerciseName}
              </Typography>
              <Chip
                label={getRoleLabel(assignment.role)}
                color={roleColor}
                size="small"
                sx={{ mr: 1 }}
              />
              <Chip
                label={assignment.exerciseType}
                variant="outlined"
                size="small"
              />
            </Box>

            {/* Status Indicator */}
            {sectionType === 'active' && (
              <Box display="flex" alignItems="center" gap={1}>
                <FontAwesomeIcon
                  icon={clockDisplay.icon}
                  style={{ color: clockDisplay.color }}
                />
                <Typography variant="body2" color="text.secondary">
                  {formatElapsedTime(assignment.elapsedSeconds)}
                </Typography>
              </Box>
            )}

            {sectionType === 'completed' && (
              <Box display="flex" alignItems="center" gap={1}>
                <FontAwesomeIcon
                  icon={faCheckCircle}
                  style={{ color: '#4caf50' }}
                />
                <Typography variant="body2" color="text.secondary">
                  Completed
                </Typography>
              </Box>
            )}
          </Box>

          {/* Details Row */}
          <Box display="flex" gap={3} mt={2} flexWrap="wrap">
            {/* Scheduled Date */}
            <Box display="flex" alignItems="center" gap={1}>
              <FontAwesomeIcon
                icon={faCalendarAlt}
                style={{ color: '#666', fontSize: '0.875rem' }}
              />
              <Typography variant="body2" color="text.secondary">
                {formatDate(assignment.scheduledDate)}
                {assignment.startTime && ' at ' + assignment.startTime}
              </Typography>
            </Box>

            {/* Location */}
            {assignment.location && (
              <Box display="flex" alignItems="center" gap={1}>
                <FontAwesomeIcon
                  icon={faMapMarkerAlt}
                  style={{ color: '#666', fontSize: '0.875rem' }}
                />
                <Typography variant="body2" color="text.secondary">
                  {assignment.location}
                </Typography>
              </Box>
            )}

            {/* Ready Injects (for Controllers in active exercises) */}
            {sectionType === 'active' &&
              assignment.role === 'Controller' &&
              assignment.readyInjects > 0 && (
              <Box display="flex" alignItems="center" gap={1}>
                <FontAwesomeIcon
                  icon={faClock}
                  style={{ color: '#ff9800', fontSize: '0.875rem' }}
                />
                <Typography
                  variant="body2"
                  color="warning.main"
                  fontWeight="medium"
                >
                  {assignment.readyInjects} inject{assignment.readyInjects > 1 ? 's' : ''} ready
                </Typography>
              </Box>
            )}
          </Box>

          {/* Progress Bar (for active exercises) */}
          {sectionType === 'active' && assignment.totalInjects > 0 && (
            <Box mt={2}>
              <Box display="flex" justifyContent="space-between" mb={0.5}>
                <Typography variant="caption" color="text.secondary">
                  Progress
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {assignment.firedInjects} / {assignment.totalInjects} injects
                </Typography>
              </Box>
              <LinearProgress
                variant="determinate"
                value={progress}
                sx={{ height: 6, borderRadius: 3 }}
              />
            </Box>
          )}
        </CardContent>
      </CardActionArea>
    </Card>
  )
}
