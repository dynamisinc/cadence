/**
 * AssignmentSection Component
 *
 * Displays a collapsible section of assignments (Active, Upcoming, or Completed).
 * Completed section is collapsed by default.
 */
import { useState } from 'react'
import { Box, Typography, Skeleton, Alert, Collapse, Chip } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { CobraIconButton } from '@/theme/styledComponents'
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
 * Get section color based on type (uses theme tokens, passed as parameter).
 */
function getSectionColor(
  type: string,
  palette: { semantic: { success: string; info: string }; neutral: { 500: string; 600: string } },
): string {
  switch (type) {
    case 'active':
      return palette.semantic.success
    case 'upcoming':
      return palette.semantic.info
    case 'completed':
      return palette.neutral[500]
    default:
      return palette.neutral[600]
  }
}

export function AssignmentSection({
  title,
  type,
  assignments,
  isLoading = false,
  emptyMessage = 'No assignments',
  showOrganization = false,
}: AssignmentSectionProps) {
  // Completed section is collapsed by default
  const [isExpanded, setIsExpanded] = useState(type !== 'completed')
  const theme = useTheme()

  const icon = getSectionIcon(type)
  const color = getSectionColor(type, theme.palette)

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
        <CobraIconButton
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
        </CobraIconButton>

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
              showOrganization={showOrganization}
            />
          ))}
      </Collapse>
    </Box>
  )
}
