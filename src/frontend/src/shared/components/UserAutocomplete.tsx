/**
 * UserAutocomplete - Reusable user selection component
 *
 * Provides autocomplete functionality for selecting users.
 * Can filter to specific roles (e.g., Admin/Manager for directors).
 * Displays user name, email, and system role.
 *
 * @module shared/components
 */

import { useState, useEffect, useCallback } from 'react'
import type { FC } from 'react'
import {
  Autocomplete,
  TextField,
  Box,
  Typography,
  CircularProgress,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faUserTie } from '@fortawesome/free-solid-svg-icons'
import { userService } from '../../features/users/services/userService'
import type { UserDto } from '../../features/users/types'

export interface UserAutocompleteProps {
  /** Currently selected user */
  value: UserDto | null
  /** Callback when selection changes */
  onChange: (user: UserDto | null) => void
  /** Field label */
  label: string
  /** Helper text to display below field */
  helperText?: string
  /** Whether field is required */
  required?: boolean
  /** Whether field is disabled */
  disabled?: boolean
  /** Error state */
  error?: boolean
  /** If true, only show Admin and Manager users (for director selection) */
  filterToDirectorEligible?: boolean
}

/**
 * Autocomplete component for selecting users
 *
 * Features:
 * - Loads active users from API
 * - Filters by role when filterToDirectorEligible is true
 * - Displays name, email, and role in dropdown
 * - COBRA styling compatible
 */
export const UserAutocomplete: FC<UserAutocompleteProps> = ({
  value,
  onChange,
  label,
  helperText,
  required = false,
  disabled = false,
  error = false,
  filterToDirectorEligible = false,
}) => {
  const [users, setUsers] = useState<UserDto[]>([])
  const [loading, setLoading] = useState(false)

  const loadUsers = useCallback(async () => {
    try {
      setLoading(true)
      const response = await userService.getUsers({
        pageSize: 100,
        // Filter by role if director-eligible only
        ...(filterToDirectorEligible && { role: 'Admin,Manager' }),
      })
      // Only show active users
      if (response && response.users) {
        setUsers(response.users.filter(u => u.status === 'Active'))
      } else {
        setUsers([])
      }
    } catch (error) {
      console.error('Failed to load users:', error)
      setUsers([])
    } finally {
      setLoading(false)
    }
  }, [filterToDirectorEligible])

  useEffect(() => {
    loadUsers()
  }, [loadUsers])

  return (
    <Autocomplete
      size="small"
      options={users}
      value={value}
      onChange={(_, newValue) => onChange(newValue)}
      getOptionLabel={user => user.displayName}
      isOptionEqualToValue={(option, val) => option.id === val.id}
      disabled={disabled}
      loading={loading}
      noOptionsText="No users found"
      renderOption={(props, user) => (
        <Box component="li" {...props} key={user.id}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, py: 0.5 }}>
            <FontAwesomeIcon icon={faUserTie} color="text.secondary" />
            <Box sx={{ flex: 1 }}>
              <Typography variant="body2" fontWeight={500}>
                {user.displayName}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {user.email} • {user.systemRole}
              </Typography>
            </Box>
          </Box>
        </Box>
      )}
      renderInput={params => (
        <TextField
          {...params}
          label={label}
          required={required}
          error={error}
          helperText={helperText}
          InputProps={{
            ...params.InputProps,
            endAdornment: (
              <>
                {loading ? <CircularProgress size={20} /> : null}
                {params.InputProps.endAdornment}
              </>
            ),
          }}
        />
      )}
    />
  )
}
