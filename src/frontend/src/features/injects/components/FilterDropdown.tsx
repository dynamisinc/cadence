/**
 * FilterDropdown Component
 *
 * A multi-select dropdown for filtering with checkboxes.
 * Used for Status, Phase, and Method filters.
 */

import { useState, useRef } from 'react'
import {
  Box,
  Typography,
  Popover,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Checkbox,
  Badge,
  Divider,
  Stack,
  Tooltip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChevronDown, faFilter } from '@fortawesome/free-solid-svg-icons'
import { CobraSecondaryButton, CobraLinkButton } from '../../../theme/styledComponents'

export interface FilterOption<T> {
  /** Unique value for the option */
  value: T
  /** Display label */
  label: string
}

export interface FilterDropdownProps<T> {
  /** Label for the dropdown button */
  label: string
  /** Available filter options */
  options: FilterOption<T>[]
  /** Currently selected values */
  selected: T[]
  /** Callback when selection changes */
  onChange: (selected: T[]) => void
  /** Optional icon to display */
  icon?: typeof faFilter
  /** When true, show only icon with tooltip (no label text or chevron) */
  compact?: boolean
}

export function FilterDropdown<T extends string | null>({
  label,
  options,
  selected,
  onChange,
  icon = faFilter,
  compact = false,
}: FilterDropdownProps<T>) {
  const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)

  const open = Boolean(anchorEl)
  const hasSelections = selected.length > 0

  const handleOpen = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget)
  }

  const handleClose = () => {
    setAnchorEl(null)
  }

  const handleToggle = (value: T) => {
    const newSelected = selected.includes(value)
      ? selected.filter(v => v !== value)
      : [...selected, value]
    onChange(newSelected)
  }

  const handleSelectAll = () => {
    onChange(options.map(o => o.value))
  }

  const handleClear = () => {
    onChange([])
  }

  const buttonContent = (
    <Badge
      badgeContent={hasSelections ? selected.length : 0}
      color="primary"
      overlap="rectangular"
      anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
      sx={{
        '& .MuiBadge-badge': {
          fontSize: '0.65rem',
          height: 16,
          minWidth: 16,
          transform: 'translate(25%, -25%)',
        },
      }}
    >
      <CobraSecondaryButton
        ref={buttonRef}
        onClick={handleOpen}
        size="small"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-label={`Filter by ${label}`}
        sx={{
          minWidth: 'auto',
          px: compact ? 1 : 1.5,
          borderColor: hasSelections ? 'primary.main' : undefined,
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
          <FontAwesomeIcon icon={icon} style={{ fontSize: '0.75rem' }} />
          {!compact && (
            <>
              <Typography variant="body2" component="span">
                {label}
              </Typography>
              <FontAwesomeIcon
                icon={faChevronDown}
                style={{
                  fontSize: '0.65rem',
                  transition: 'transform 0.2s',
                  transform: open ? 'rotate(180deg)' : undefined,
                }}
              />
            </>
          )}
        </Box>
      </CobraSecondaryButton>
    </Badge>
  )

  return (
    <>
      {compact ? <Tooltip title={label}>{buttonContent}</Tooltip> : buttonContent}

      <Popover
        open={open}
        anchorEl={anchorEl}
        onClose={handleClose}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'left',
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: 'left',
        }}
        slotProps={{
          paper: {
            sx: {
              minWidth: 200,
              maxHeight: 350,
              mt: 0.5,
            },
          },
        }}
      >
        {/* Quick actions */}
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          sx={{ px: 1.5, py: 1 }}
        >
          <CobraLinkButton
            size="small"
            onClick={handleSelectAll}
            disabled={selected.length === options.length}
          >
            Select all
          </CobraLinkButton>
          <CobraLinkButton
            size="small"
            onClick={handleClear}
            disabled={!hasSelections}
          >
            Clear
          </CobraLinkButton>
        </Stack>

        <Divider />

        {/* Options list */}
        <List dense sx={{ py: 0.5 }}>
          {options.map(option => {
            const isSelected = selected.includes(option.value)
            return (
              <ListItem key={String(option.value)} disablePadding>
                <ListItemButton
                  onClick={() => handleToggle(option.value)}
                  dense
                  selected={isSelected}
                  sx={{ py: 0.5 }}
                >
                  <ListItemIcon sx={{ minWidth: 36 }}>
                    <Checkbox
                      edge="start"
                      checked={isSelected}
                      tabIndex={-1}
                      disableRipple
                      size="small"
                      inputProps={{
                        'aria-labelledby': `filter-option-${String(option.value)}`,
                      }}
                    />
                  </ListItemIcon>
                  <ListItemText
                    id={`filter-option-${String(option.value)}`}
                    primary={option.label}
                    primaryTypographyProps={{
                      variant: 'body2',
                    }}
                  />
                </ListItemButton>
              </ListItem>
            )
          })}
        </List>
      </Popover>
    </>
  )
}

export default FilterDropdown
