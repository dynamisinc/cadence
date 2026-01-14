/**
 * FilterChip Component
 *
 * Displays an active filter as a removable chip.
 */

import { Chip, Box, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faXmark } from '@fortawesome/free-solid-svg-icons'

export interface FilterChipProps {
  /** Filter category label (e.g., "Status", "Phase") */
  label: string
  /** Filter value (e.g., "Pending", "Phase 1") */
  value: string
  /** Callback when chip is dismissed */
  onRemove: () => void
}

export const FilterChip = ({ label, value, onRemove }: FilterChipProps) => {
  return (
    <Chip
      size="small"
      onDelete={onRemove}
      deleteIcon={
        <Box
          component="span"
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '0.65rem',
          }}
        >
          <FontAwesomeIcon icon={faXmark} />
        </Box>
      }
      label={
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
          <Typography
            variant="caption"
            component="span"
            sx={{ fontWeight: 500, color: 'text.secondary' }}
          >
            {label}:
          </Typography>
          <Typography variant="caption" component="span">
            {value}
          </Typography>
        </Box>
      }
      sx={{
        borderRadius: 1,
        '& .MuiChip-deleteIcon': {
          color: 'text.secondary',
          '&:hover': {
            color: 'text.primary',
          },
        },
      }}
    />
  )
}

export default FilterChip
