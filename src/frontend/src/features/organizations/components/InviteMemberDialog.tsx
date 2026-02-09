/**
 * InviteMemberDialog - Dialog for sending organization invitations
 *
 * Allows OrgAdmins to invite users to join the organization via email.
 * The invited user will receive an email with a link to accept the invitation.
 *
 * @module features/organizations/components
 * @see EM-02-S01 Send invitation
 */
import { useState } from 'react'
import type { FC } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  Box,
  Typography,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEnvelope } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton, CobraTextField } from '@/theme/styledComponents'
import type { OrgRole } from '../types'

interface InviteMemberDialogProps {
  open: boolean;
  onClose: () => void;
  onInvite: (email: string, role: OrgRole) => Promise<void>;
  isLoading?: boolean;
}

const ORG_ROLES: { value: OrgRole; label: string; description: string }[] = [
  { value: 'OrgAdmin', label: 'Admin', description: 'Full organization management access' },
  { value: 'OrgManager', label: 'Manager', description: 'Can manage exercises and users' },
  { value: 'OrgUser', label: 'User', description: 'Standard access to organization exercises' },
]

export const InviteMemberDialog: FC<InviteMemberDialogProps> = ({
  open,
  onClose,
  onInvite,
  isLoading = false,
}) => {
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<OrgRole>('OrgUser')
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async () => {
    setError(null)

    // Basic email validation
    if (!email.trim()) {
      setError('Email is required')
      return
    }

    if (!email.includes('@')) {
      setError('Please enter a valid email address')
      return
    }

    try {
      await onInvite(email.trim(), role)
      // Reset form on success
      setEmail('')
      setRole('OrgUser')
      onClose()
    } catch (err) {
      // Error handling is done in parent, but we can show generic error
      const axiosError = err as { response?: { data?: { message?: string } } }
      setError(axiosError.response?.data?.message || 'Failed to send invitation')
    }
  }

  const handleClose = () => {
    setEmail('')
    setRole('OrgUser')
    setError(null)
    onClose()
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
          <FontAwesomeIcon icon={faEnvelope} />
          Invite Member to Organization
        </Box>
      </DialogTitle>
      <DialogContent>
        <Box sx={{ pt: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
          {error && (
            <Alert severity="error" onClose={() => setError(null)}>
              {error}
            </Alert>
          )}

          <Alert severity="info" icon={<FontAwesomeIcon icon={faEnvelope} />}>
            <Typography variant="body2">
              An email invitation will be sent with a link to join the organization. The invitation
              expires in 7 days.
            </Typography>
          </Alert>

          <CobraTextField
            label="Email Address"
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            placeholder="user@example.com"
            fullWidth
            autoFocus
            helperText="The user will receive an invitation email"
          />

          <FormControl fullWidth>
            <InputLabel>Initial Role</InputLabel>
            <Select
              value={role}
              label="Initial Role"
              onChange={e => setRole(e.target.value as OrgRole)}
            >
              {ORG_ROLES.map(r => (
                <MenuItem key={r.value} value={r.value}>
                  <Box>
                    <Box component="span" fontWeight="bold">
                      {r.label}
                    </Box>
                    <Box component="span" sx={{ ml: 1, color: 'text.secondary', fontSize: '0.85em' }}>
                      - {r.description}
                    </Box>
                  </Box>
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton onClick={handleClose} disabled={isLoading}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton onClick={handleSubmit} disabled={isLoading || !email.trim()}>
          {isLoading ? 'Sending...' : 'Send Invitation'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default InviteMemberDialog
