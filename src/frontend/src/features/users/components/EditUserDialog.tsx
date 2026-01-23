/**
 * EditUserDialog Component
 *
 * Modal dialog for editing user details (display name and email).
 * Validates input and shows error messages.
 *
 * @module features/users/components
 * @see authentication/S11 Edit User Details
 */
import { FC, useState } from 'react'
import { Dialog, DialogTitle, DialogContent, DialogActions, Box, Alert, Stack } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSave, faXmark } from '@fortawesome/free-solid-svg-icons'
import { CobraTextField, CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import type { UserDto, UpdateUserRequest } from '../types'

interface EditUserDialogProps {
  /** User to edit */
  user: UserDto;
  /** Called when dialog should close */
  onClose: () => void;
  /** Called when save is clicked with updated fields */
  onSave: (updates: UpdateUserRequest) => Promise<void>;
}

/**
 * Edit user details dialog
 * Allows updating display name and email
 */
export const EditUserDialog: FC<EditUserDialogProps> = ({ user, onClose, onSave }) => {
  const [displayName, setDisplayName] = useState(user.displayName)
  const [email, setEmail] = useState(user.email)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSave = async () => {
    setIsLoading(true)
    setError(null)

    try {
      // Only include fields that changed
      const updates: UpdateUserRequest = {}
      if (displayName !== user.displayName) {
        updates.displayName = displayName
      }
      if (email !== user.email) {
        updates.email = email
      }

      await onSave(updates)
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update user')
      setIsLoading(false)
    }
  }

  return (
    <Dialog open onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit User</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <Stack spacing={CobraStyles.Spacing.FormFields} sx={{ mt: 1 }}>
          <CobraTextField
            label="Display Name"
            value={displayName}
            onChange={e => setDisplayName(e.target.value)}
            fullWidth
            required
          />
          <CobraTextField
            label="Email"
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            fullWidth
            required
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <CobraSecondaryButton onClick={onClose} disabled={isLoading}>
          <FontAwesomeIcon icon={faXmark} style={{ marginRight: 8 }} />
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton onClick={handleSave} disabled={isLoading}>
          <FontAwesomeIcon icon={faSave} style={{ marginRight: 8 }} />
          Save
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}
