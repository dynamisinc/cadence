/**
 * ActiveFiltersBar Component
 *
 * Displays active filters as chips with clear options and result count.
 */

import { Box, Stack, Typography } from '@mui/material'
import { FilterChip } from './FilterChip'
import { CobraLinkButton } from '../../../theme/styledComponents'
import type { FilterType } from '../types/organization'

export interface ActiveFilter {
  /** Filter type for removal callback */
  type: FilterType
  /** Display label */
  label: string
  /** Display value */
  value: string
}

export interface ActiveFiltersBarProps {
  /** Array of active filters to display */
  filters: ActiveFilter[]
  /** Total count of all injects */
  totalCount: number
  /** Count of filtered injects */
  filteredCount: number
  /** Callback when a filter is removed */
  onRemoveFilter: (type: FilterType) => void
  /** Callback when all filters are cleared */
  onClearAll: () => void
}

export const ActiveFiltersBar = ({
  filters,
  totalCount,
  filteredCount,
  onRemoveFilter,
  onClearAll,
}: ActiveFiltersBarProps) => {
  // Don't render if no filters active
  if (filters.length === 0) {
    return null
  }

  const isFiltered = filteredCount !== totalCount

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: 1,
        py: 1,
      }}
    >
      {/* Filter chips */}
      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
        <Typography variant="body2" color="text.secondary" sx={{ mr: 0.5 }}>
          Active filters:
        </Typography>
        {filters.map(filter => (
          <FilterChip
            key={filter.type}
            label={filter.label}
            value={filter.value}
            onRemove={() => onRemoveFilter(filter.type)}
          />
        ))}
        <CobraLinkButton
          size="small"
          onClick={onClearAll}
          sx={{ ml: 0.5 }}
        >
          Clear all
        </CobraLinkButton>
      </Stack>

      {/* Result count */}
      <Typography
        variant="body2"
        color={isFiltered ? 'text.primary' : 'text.secondary'}
        sx={{ whiteSpace: 'nowrap' }}
      >
        Showing{' '}
        <Box component="span" fontWeight={isFiltered ? 600 : 400}>
          {filteredCount}
        </Box>
        {' '}of {totalCount} inject{totalCount !== 1 ? 's' : ''}
      </Typography>
    </Box>
  )
}

export default ActiveFiltersBar
