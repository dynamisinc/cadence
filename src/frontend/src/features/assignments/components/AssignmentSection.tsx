/**
 * AssignmentSection Component
 *
 * Displays a collapsible section of assignments (Active, Upcoming, or Completed).
 * Completed section is collapsed by default.
 */
import { useState } from 'react'
import { Box, Typography, Skeleton, Alert, Collapse, IconButton, Chip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlay,
  faCalendarAlt,
  faCheckCircle,
  faInbox,
  faChevronDown,
  faChevronRight,
} from '@fortawesome/free-solid-svg-icons'
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
  // Completed section is collapsed by default
  const [isExpanded, setIsExpanded] = useState(type !== 'completed')

  const icon = getSectionIcon(type)
  const color = getSectionColor(type)

  const handleToggle = () => {
    setIsExpanded(!isExpanded)
  }

  return (
    <Box mb={3}>
      {/* Section Header - Clickable to toggle */}
      <Box
        display="flex"
        alignItems="center"
        gap={1}
        mb={isExpanded ? 1.5 : 0}
        onClick={handleToggle}
        sx={{
          cursor: 'pointer',
          userSelect: 'none',
          '&:hover': {
            opacity: 0.8,
          },
        }}
      >
        {/* Expand/Collapse Icon */}
        <IconButton
          size="small"
          onClick={e => {
            e.stopPropagation()
            handleToggle()
          }}
          aria-label={isExpanded ? `Collapse ${title}` : `Expand ${title}`}
          aria-expanded={isExpanded}
        >
          <FontAwesomeIcon
            icon={isExpanded ? faChevronDown : faChevronRight}
            style={{ fontSize: '0.875rem' }}
          />
        </IconButton>

        {/* Section Icon */}
        <FontAwesomeIcon
          icon={icon}
          style={{ color, fontSize: '1.25rem' }}
        />

        {/* Title */}
        <Typography variant="h6" component="h2">
          {title}
        </Typography>

        {/* Count Badge */}
        <Chip
          label={assignments.length}
          size="small"
          sx={{
            height: 22,
            minWidth: 28,
            backgroundColor: color,
            color: 'white',
            fontWeight: 'medium',
          }}
        />
      </Box>

      {/* Collapsible Content */}
      <Collapse in={isExpanded}>
        {/* Loading State */}
        {isLoading && (
          <Box>
            {[1, 2].map(n => (
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
          assignments.map(assignment => (
            <AssignmentCard
              key={assignment.exerciseId}
              assignment={assignment}
              sectionType={type}
            />
          ))}
      </Collapse>
    </Box>
  )
}
