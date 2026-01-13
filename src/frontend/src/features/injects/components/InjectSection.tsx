/**
 * InjectSection Component
 *
 * Collapsible section wrapper for grouping injects by status.
 * Used in the ExerciseConductPage to organize injects into
 * Ready to Fire, Upcoming, Later, Fired, and Skipped sections.
 */

import { useState } from 'react'
import {
  Box,
  Typography,
  Collapse,
  IconButton,
  Table,
  TableBody,
  TableContainer,
  Paper,
  Badge,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronDown,
  faChevronRight,
  faCircleExclamation,
  faClock,
  faHourglass,
  faCheck,
  faForwardStep,
} from '@fortawesome/free-solid-svg-icons'
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core'

export type SectionVariant = 'ready' | 'upcoming' | 'later' | 'fired' | 'skipped'

interface InjectSectionProps {
  /** Section variant determines styling */
  variant: SectionVariant
  /** Section title */
  title: string
  /** Number of items in section (shown as badge) */
  count: number
  /** Whether section is expanded by default */
  defaultExpanded?: boolean
  /** Optional subtitle (e.g., "next 30 min") */
  subtitle?: string
  /** Children to render inside the section */
  children: React.ReactNode
}

const sectionConfig: Record<
  SectionVariant,
  {
    icon: IconDefinition
    iconColor: string
    headerBg: string
    badgeColor: 'error' | 'warning' | 'default' | 'success' | 'info'
  }
> = {
  ready: {
    icon: faCircleExclamation,
    iconColor: 'error.main',
    headerBg: 'error.50',
    badgeColor: 'error',
  },
  upcoming: {
    icon: faClock,
    iconColor: 'warning.main',
    headerBg: 'warning.50',
    badgeColor: 'warning',
  },
  later: {
    icon: faHourglass,
    iconColor: 'text.secondary',
    headerBg: 'grey.100',
    badgeColor: 'default',
  },
  fired: {
    icon: faCheck,
    iconColor: 'success.main',
    headerBg: 'success.50',
    badgeColor: 'success',
  },
  skipped: {
    icon: faForwardStep,
    iconColor: 'warning.main',
    headerBg: 'warning.50',
    badgeColor: 'warning',
  },
}

export const InjectSection = ({
  variant,
  title,
  count,
  defaultExpanded = true,
  subtitle,
  children,
}: InjectSectionProps) => {
  const [expanded, setExpanded] = useState(defaultExpanded)
  const config = sectionConfig[variant]

  // Don't render empty sections
  if (count === 0) {
    return null
  }

  return (
    <Box sx={{ mb: 2 }}>
      {/* Section Header */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1,
          p: 1.5,
          backgroundColor: config.headerBg,
          borderRadius: expanded ? '8px 8px 0 0' : '8px',
          cursor: 'pointer',
          transition: 'background-color 0.2s',
          '&:hover': {
            filter: 'brightness(0.97)',
          },
        }}
        onClick={() => setExpanded(!expanded)}
      >
        <IconButton size="small" sx={{ p: 0.5 }}>
          <FontAwesomeIcon
            icon={expanded ? faChevronDown : faChevronRight}
            size="sm"
          />
        </IconButton>

        <Box sx={{ color: config.iconColor }}>
          <FontAwesomeIcon icon={config.icon} />
        </Box>

        <Typography variant="subtitle2" fontWeight={600} sx={{ flexGrow: 1 }}>
          {title}
        </Typography>

        {subtitle && (
          <Typography variant="caption" color="text.secondary" sx={{ mr: 1 }}>
            {subtitle}
          </Typography>
        )}

        <Badge
          badgeContent={count}
          color={config.badgeColor}
          sx={{
            '& .MuiBadge-badge': {
              position: 'static',
              transform: 'none',
            },
          }}
        />
      </Box>

      {/* Section Content */}
      <Collapse in={expanded}>
        <TableContainer
          component={Paper}
          variant="outlined"
          sx={{
            borderTopLeftRadius: 0,
            borderTopRightRadius: 0,
            borderTop: 0,
          }}
        >
          <Table size="small">
            <TableBody>{children}</TableBody>
          </Table>
        </TableContainer>
      </Collapse>
    </Box>
  )
}

export default InjectSection
