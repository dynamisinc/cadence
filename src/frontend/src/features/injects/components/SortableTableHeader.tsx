/**
 * SortableTableHeader Component
 *
 * A table header cell that supports sorting with visual indicators.
 * Clicking cycles through: ascending → descending → no sort
 */

import { TableCell, Box, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSort, faSortUp, faSortDown } from '@fortawesome/free-solid-svg-icons'
import type { SortableColumn, SortDirection } from '../types/organization'

export interface SortableTableHeaderProps {
  /** Column identifier */
  column: SortableColumn
  /** Display label for the header */
  label: string
  /** Currently sorted column (null if not sorted) */
  activeColumn: SortableColumn | null
  /** Current sort direction */
  direction: SortDirection
  /** Callback when header is clicked */
  onSort: (column: SortableColumn) => void
  /** Optional width for the column */
  width?: number | string
  /** Text alignment */
  align?: 'left' | 'center' | 'right'
}

/**
 * Get the appropriate sort icon based on state
 */
function getSortIcon(
  isActive: boolean,
  direction: SortDirection,
) {
  if (!isActive || !direction) {
    return faSort
  }
  return direction === 'asc' ? faSortUp : faSortDown
}

/**
 * Get the icon color based on state
 */
function getIconColor(isActive: boolean, direction: SortDirection): string {
  if (!isActive || !direction) {
    return 'text.disabled'
  }
  return 'primary.main'
}

/**
 * Get aria-sort value for accessibility
 */
function getAriaSort(
  isActive: boolean,
  direction: SortDirection,
): 'none' | 'ascending' | 'descending' {
  if (!isActive || !direction) {
    return 'none'
  }
  return direction === 'asc' ? 'ascending' : 'descending'
}

export const SortableTableHeader = ({
  column,
  label,
  activeColumn,
  direction,
  onSort,
  width,
  align = 'left',
}: SortableTableHeaderProps) => {
  const isActive = activeColumn === column
  const icon = getSortIcon(isActive, direction)
  const iconColor = getIconColor(isActive, direction)
  const ariaSort = getAriaSort(isActive, direction)

  const handleClick = () => {
    onSort(column)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      onSort(column)
    }
  }

  return (
    <TableCell
      width={width}
      align={align}
      aria-sort={ariaSort}
      sx={{
        cursor: 'pointer',
        userSelect: 'none',
        '&:hover': {
          backgroundColor: 'action.hover',
        },
        '&:focus-visible': {
          outline: '2px solid',
          outlineColor: 'primary.main',
          outlineOffset: '-2px',
        },
      }}
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      tabIndex={0}
      role="columnheader"
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 0.5,
          justifyContent: align === 'right' ? 'flex-end' : align === 'center' ? 'center' : 'flex-start',
        }}
      >
        <Typography
          variant="body2"
          component="span"
          sx={{ fontWeight: 500 }}
        >
          {label}
        </Typography>
        <Box
          component="span"
          sx={{
            color: iconColor,
            fontSize: '0.75rem',
            display: 'flex',
            alignItems: 'center',
            transition: 'color 0.2s',
          }}
        >
          <FontAwesomeIcon icon={icon} />
        </Box>
      </Box>
    </TableCell>
  )
}

export default SortableTableHeader
