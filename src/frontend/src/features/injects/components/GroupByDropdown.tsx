/**
 * GroupByDropdown Component
 *
 * A dropdown for selecting how to group injects.
 */

import { Box, Typography, Select, MenuItem, Tooltip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faLayerGroup } from '@fortawesome/free-solid-svg-icons'
import type { GroupBy } from '../types/organization'
import { getGroupByOptions } from '../utils/groupUtils'

export interface GroupByDropdownProps {
  /** Current grouping value */
  value: GroupBy
  /** Callback when grouping changes */
  onChange: (groupBy: GroupBy) => void
  /** When true, hide the "Group:" label text */
  compact?: boolean
}

export const GroupByDropdown = ({ value, onChange, compact = false }: GroupByDropdownProps) => {
  const options = getGroupByOptions()

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      <Tooltip title={compact ? 'Group' : ''}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, color: 'text.secondary' }}>
          <FontAwesomeIcon icon={faLayerGroup} style={{ fontSize: '0.875rem' }} />
          {!compact && (
            <Typography variant="body2" component="span">
              Group:
            </Typography>
          )}
        </Box>
      </Tooltip>
      <Select
        value={value}
        onChange={e => onChange(e.target.value as GroupBy)}
        size="small"
        variant="outlined"
        sx={{
          minWidth: 100,
          '& .MuiSelect-select': {
            py: 0.5,
            fontSize: '0.875rem',
          },
        }}
      >
        {options.map(option => (
          <MenuItem key={option.value} value={option.value}>
            {option.label}
          </MenuItem>
        ))}
      </Select>
    </Box>
  )
}

export default GroupByDropdown
