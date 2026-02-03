/**
 * RoleSelect Component
 *
 * Dropdown selector for system-level user roles.
 * Shows available System roles (Admin, Manager, User) for application-level permissions.
 * NOT for exercise-specific HSEEP role assignment.
 *
 * @module features/users/components
 * @see authentication/S13 Global Role Assignment
 */
import type { FC } from 'react'
import { Select, MenuItem } from '@mui/material'
import { USER_ROLES } from '../types'

interface RoleSelectProps {
  /** Currently selected role */
  value: string;
  /** Called when role is changed */
  onChange: (role: string) => void;
  /** Whether the select is disabled */
  disabled?: boolean;
}

/**
 * System role selector dropdown
 * Displays all system roles (Admin, Manager, User) for selection
 */
export const RoleSelect: FC<RoleSelectProps> = ({ value, onChange, disabled }) => {
  const handleChange = (event: { target: { value: string } }) => {
    onChange(event.target.value)
  }

  return (
    <Select
      value={value}
      onChange={handleChange}
      size="small"
      disabled={disabled}
      sx={{
        minWidth: 100,
        fontSize: '0.875rem',
        '& .MuiSelect-select': {
          py: 0.5,
        },
      }}
    >
      {USER_ROLES.map(role => (
        <MenuItem key={role} value={role} sx={{ fontSize: '0.875rem' }}>
          {role}
        </MenuItem>
      ))}
    </Select>
  )
}
