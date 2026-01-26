/**
 * AssignmentSection Component
 *
 * Displays a section of assignments (Active, Upcoming, or Completed).
 */
import { Box, Typography, Skeleton, Alert } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlay, faCalendarAlt, faCheckCircle, faInbox } from '@fortawesome/free-solid-svg-icons'
import type { AssignmentSectionProps } from '../types'
import { AssignmentCard } from './AssignmentCard'

/**
 * Get section icon based on type.
 */
function getSectionIcon(type: string) {
  switch (type) {
    case 'active':
      return faPlay
    case 'upcoming':
      return faCalendarAlt
    case 'completed':
      return faCheckCircle
    default:
      return faInbox
  }
}

/**
 * Get section color based on type.
 */
function getSectionColor(type: string): string {
  switch (type) {
    case 'active':
      return '#4caf50'
    case 'upcoming':
      return '#2196f3'
    case 'completed':
      return '#9e9e9e'
    default:
      return '#666'
  }
}

export function AssignmentSection({
  title,
  type,
  assignments,
  isLoading = false,
  emptyMessage = 'No assignments',
}: AssignmentSectionProps) {
  const icon = getSectionIcon(type)
  const color = getSectionColor(type)

  return (
    <Box mb={4}>
      {/* Section Header */}
      <Box display="flex" alignItems="center" gap={1.5} mb={2}>
        <FontAwesomeIcon
          icon={icon}
          style={{ color, fontSize: '1.25rem' }}
        />
        <Typography variant="h6" component="h2">
          {title}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          ({assignments.length})
        </Typography>
      </Box>

      {/* Loading State */}
      {isLoading && (
        <Box>
          {[1, 2].map((n) => (
            <Skeleton
              key={n}
              variant="rectangular"
              height={120}
              sx={{ mb: 2, borderRadius: 1 }}
            />
          ))}
        </Box>
      )}

      {/* Empty State */}
      {!isLoading && assignments.length === 0 && (
        <Alert severity="info" sx={{ mb: 2 }}>
          {emptyMessage}
        </Alert>
      )}

      {/* Assignment Cards */}
      {!isLoading &&
        assignments.map((assignment) => (
          <AssignmentCard
            key={assignment.exerciseId}
            assignment={assignment}
            sectionType={type}
          />
        ))}
    </Box>
  )
}
