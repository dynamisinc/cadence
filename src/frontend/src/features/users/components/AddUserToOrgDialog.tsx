/**
 * AddUserToOrgDialog Component
 *
 * Dialog for adding a user to an organization from the user management page.
 * Allows selecting an organization and role.
 *
 * @module features/users/components
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
  Stack,
} from '@mui/material'
import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import type { OrganizationListItem, OrgRole } from '../../organizations/types'

interface AddUserToOrgDialogProps {
  userId: string;
  userName: string;
  availableOrgs: OrganizationListItem[];
  onClose: () => void;
  onAdd: (userId: string, orgId: string, role: OrgRole) => Promise<void>;
}

const ORG_ROLES: { value: OrgRole; label: string; description: string }[] = [
  { value: 'OrgAdmin', label: 'Organization Admin', description: 'Full organization management access' },
  { value: 'OrgManager', label: 'Organization Manager', description: 'Can manage exercises and users' },
  { value: 'OrgUser', label: 'Organization User', description: 'Standard access to organization exercises' },
]

export const AddUserToOrgDialog: FC<AddUserToOrgDialogProps> = ({
  userId,
  userName,
  availableOrgs,
  onClose,
  onAdd,
}) => {
  const [selectedOrgId, setSelectedOrgId] = useState<string>('')
  const [selectedRole, setSelectedRole] = useState<OrgRole>('OrgUser')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async () => {
    if (!selectedOrgId) return

    setIsSubmitting(true)
    setError(null)
    try {
      await onAdd(userId, selectedOrgId, selectedRole)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add user to organization')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ pb: 1 }}>
        Add {userName} to Organization
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && (
            <Alert severity="error" onClose={() => setError(null)}>
              {error}
            </Alert>
          )}

          <FormControl fullWidth size="small">
            <InputLabel>Organization</InputLabel>
            <Select
              value={selectedOrgId}
              onChange={e => setSelectedOrgId(e.target.value)}
              label="Organization"
            >
              {availableOrgs.map(org => (
                <MenuItem key={org.id} value={org.id}>
                  {org.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl fullWidth size="small">
            <InputLabel>Role</InputLabel>
            <Select
              value={selectedRole}
              onChange={e => setSelectedRole(e.target.value as OrgRole)}
              label="Role"
            >
              {ORG_ROLES.map(role => (
                <MenuItem key={role.value} value={role.value}>
                  {role.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        <CobraSecondaryButton onClick={onClose} disabled={isSubmitting}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleSubmit}
          disabled={!selectedOrgId || isSubmitting}
        >
          {isSubmitting ? 'Adding...' : 'Add to Organization'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}
