/**
 * GroupByDropdown Component
 *
 * A dropdown for selecting how to group injects.
 */

import { Box, Typography, Select, MenuItem, FormControl, InputLabel } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faLayerGroup } from '@fortawesome/free-solid-svg-icons'
import type { GroupBy } from '../types/organization'
import { getGroupByOptions } from '../utils/groupUtils'

export interface GroupByDropdownProps {
  /** Current grouping value */
  value: GroupBy
  /** Callback when grouping changes */
  onChange: (groupBy: GroupBy) => void
}

export const GroupByDropdown = ({ value, onChange }: GroupByDropdownProps) => {
  const options = getGroupByOptions()

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, color: 'text.secondary' }}>
        <FontAwesomeIcon icon={faLayerGroup} style={{ fontSize: '0.875rem' }} />
        <Typography variant="body2" component="span">
          Group:
        </Typography>
      </Box>
      <Select
        value={value}
        onChange={(e) => onChange(e.target.value as GroupBy)}
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
