/**
 * AddParticipantDialog - Dialog for adding participants to an exercise
 *
 * Provides user search/autocomplete and exercise role selection.
 * Shows user's system role for context during selection.
 * Allows Admin/Manager users to create new users inline.
 *
 * @module features/exercises/components
 * @see authentication/S25-inline-user-creation.md
 */

import { useState, useEffect } from 'react'
import type { FC } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Stack,
  Autocomplete,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Typography,
  Box,
  CircularProgress,
  Button,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faUserPlus } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraLinkButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useAuth } from '../../../contexts/AuthContext'
import { userService } from '../../users/services/userService'
import { CreateUserModal } from '../../users/components/CreateUserModal'
import type { UserDto } from '../../users/types'
import type { AddParticipantRequest } from '../types'

interface AddParticipantDialogProps {
  open: boolean
  onAdd: (request: AddParticipantRequest) => void
  onClose: () => void
}

// Exercise roles available for assignment
const EXERCISE_ROLES = [
  { value: 'Observer', label: 'Observer' },
  { value: 'Evaluator', label: 'Evaluator' },
  { value: 'Controller', label: 'Controller' },
  { value: 'ExerciseDirector', label: 'Exercise Director' },
]

export const AddParticipantDialog: FC<AddParticipantDialogProps> = ({
  open,
  onAdd,
  onClose,
}) => {
  const { user: currentUser } = useAuth()
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null)
  const [selectedRole, setSelectedRole] = useState<string>('Observer')
  const [users, setUsers] = useState<UserDto[]>([])
  const [loading, setLoading] = useState(false)
  const [showCreateUserModal, setShowCreateUserModal] = useState(false)

  // Check if current user can create users (Admin or Manager)
  const canCreateUsers = currentUser?.role === 'Admin' || currentUser?.role === 'Manager'

  // Load users when dialog opens
  useEffect(() => {
    if (open) {
      loadUsers()
    }
  }, [open])

  // Reset form when dialog closes
  useEffect(() => {
    if (!open) {
      setSelectedUser(null)
      setSelectedRole('Observer')
      setShowCreateUserModal(false)
    }
  }, [open])

  const loadUsers = async () => {
    try {
      setLoading(true)
      const response = await userService.getUsers({ pageSize: 100 })
      setUsers(response.users.filter(u => u.status === 'Active'))
    } catch (error) {
      console.error('Failed to load users:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleAdd = () => {
    if (selectedUser) {
      onAdd({
        userId: selectedUser.id,
        role: selectedRole,
      })
    }
  }

  const handleUserCreated = (newUser: UserDto) => {
    // Add the new user to the list and auto-select them
    setUsers(prev => [...prev, newUser])
    setSelectedUser(newUser)
    setShowCreateUserModal(false)
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Add Participant</DialogTitle>

      <DialogContent>
        <Stack spacing={CobraStyles.Spacing.FormFields} sx={{ mt: 1 }}>
          {/* User Autocomplete */}
          <Autocomplete
            options={users}
            value={selectedUser}
            onChange={(_event, newValue) => setSelectedUser(newValue)}
            getOptionLabel={user => user.displayName}
            renderOption={(props, user) => (
              <Box component="li" {...props}>
                <Box>
                  <Typography variant="body1">{user.displayName}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {user.email} • System: {user.systemRole}
                  </Typography>
                </Box>
              </Box>
            )}
            renderInput={params => (
              <TextField
                {...params}
                label="Select User"
                required
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
            loading={loading}
            noOptionsText="No users found"
          />

          {/* Create New User option for Admin/Manager users */}
          {canCreateUsers && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Can't find the person you're looking for?
              </Typography>
              <Button
                size="small"
                startIcon={<FontAwesomeIcon icon={faUserPlus} />}
                onClick={() => setShowCreateUserModal(true)}
                sx={{ textTransform: 'none' }}
              >
                Create New User
              </Button>
            </Box>
          )}

          {/* Exercise Role Selector */}
          <FormControl fullWidth required>
            <InputLabel id="exercise-role-label">Exercise Role</InputLabel>
            <Select
              labelId="exercise-role-label"
              id="exercise-role"
              value={selectedRole}
              label="Exercise Role"
              onChange={e => setSelectedRole(e.target.value)}
            >
              {EXERCISE_ROLES.map(role => (
                <MenuItem key={role.value} value={role.value}>
                  {role.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Show selected user's system role for context */}
          {selectedUser && (
            <Box
              sx={{
                p: 1.5,
                bgcolor: 'background.paper',
                border: 1,
                borderColor: 'divider',
                borderRadius: 1,
              }}
            >
              <Typography variant="body2" color="text.secondary">
                <strong>System Role:</strong> {selectedUser.systemRole}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                The exercise role will override the system role for this exercise.
              </Typography>
            </Box>
          )}
        </Stack>
      </DialogContent>

      <DialogActions>
        <CobraLinkButton onClick={onClose}>Cancel</CobraLinkButton>
        <CobraPrimaryButton onClick={handleAdd} disabled={!selectedUser}>
          Add Participant
        </CobraPrimaryButton>
      </DialogActions>

      {/* Create User Modal - nested dialog */}
      <CreateUserModal
        open={showCreateUserModal}
        onClose={() => setShowCreateUserModal(false)}
        onUserCreated={handleUserCreated}
      />
    </Dialog>
  )
}
