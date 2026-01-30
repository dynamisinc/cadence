/**
 * OrgMembersTable - Table displaying organization members
 *
 * Shows all members with role management and removal capabilities.
 *
 * @module features/organizations/components
 */
import { useState } from 'react'
import type { FC } from 'react'
import {
  Box,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  IconButton,
  Select,
  MenuItem,
  Typography,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faTrash, faUserPlus } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import type { OrgMember, OrgRole } from '../types'

interface OrgMembersTableProps {
  members: OrgMember[];
  isLoading?: boolean;
  onAddClick: () => void;
  onRoleChange: (membershipId: string, newRole: OrgRole) => Promise<void>;
  onRemove: (membershipId: string, memberName: string) => Promise<void>;
}

export const OrgMembersTable: FC<OrgMembersTableProps> = ({
  members,
  isLoading = false,
  onAddClick,
  onRoleChange,
  onRemove,
}) => {
  const [error, setError] = useState<string | null>(null)
  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean;
    membershipId: string;
    memberName: string;
  }>({ open: false, membershipId: '', memberName: '' })

  const handleRoleChange = async (membershipId: string, newRole: OrgRole) => {
    setError(null)
    try {
      await onRoleChange(membershipId, newRole)
    } catch (err) {
      const axiosError = err as { response?: { data?: { message?: string } } }
      setError(axiosError.response?.data?.message || 'Failed to update role')
    }
  }

  const handleRemoveClick = (membershipId: string, memberName: string) => {
    setConfirmDialog({ open: true, membershipId, memberName })
  }

  const handleConfirmRemove = async () => {
    setError(null)
    try {
      await onRemove(confirmDialog.membershipId, confirmDialog.memberName)
      setConfirmDialog({ open: false, membershipId: '', memberName: '' })
    } catch (err) {
      const axiosError = err as { response?: { data?: { message?: string } } }
      setError(axiosError.response?.data?.message || 'Failed to remove member')
      setConfirmDialog({ open: false, membershipId: '', memberName: '' })
    }
  }

  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Members ({members.length})</Typography>
        <CobraPrimaryButton
          startIcon={<FontAwesomeIcon icon={faUserPlus} />}
          onClick={onAddClick}
          size="small"
        >
          Add Member
        </CobraPrimaryButton>
      </Box>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Members Table */}
      {members.length === 0 ? (
        <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
          No members in this organization
        </Typography>
      ) : (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>Role</TableCell>
              <TableCell>Joined</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {members.map(member => (
              <TableRow key={member.membershipId}>
                <TableCell>{member.displayName || '-'}</TableCell>
                <TableCell>{member.email}</TableCell>
                <TableCell>
                  <Select
                    value={member.role}
                    onChange={e => handleRoleChange(member.membershipId, e.target.value as OrgRole)}
                    size="small"
                    disabled={isLoading}
                    sx={{ minWidth: 120 }}
                  >
                    <MenuItem value="OrgAdmin">Admin</MenuItem>
                    <MenuItem value="OrgManager">Manager</MenuItem>
                    <MenuItem value="OrgUser">User</MenuItem>
                  </Select>
                </TableCell>
                <TableCell>
                  {new Date(member.joinedAt).toLocaleDateString()}
                </TableCell>
                <TableCell align="right">
                  <IconButton
                    onClick={() => handleRemoveClick(
                      member.membershipId,
                      member.displayName || member.email,
                    )}
                    color="error"
                    size="small"
                    title="Remove member"
                    disabled={isLoading}
                  >
                    <FontAwesomeIcon icon={faTrash} />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {/* Confirm Remove Dialog */}
      <ConfirmDialog
        open={confirmDialog.open}
        title="Remove Member"
        message={`Are you sure you want to remove ${confirmDialog.memberName} from this organization? They will lose access to all organization resources.`}
        severity="danger"
        confirmLabel="Remove"
        onConfirm={handleConfirmRemove}
        onCancel={() => setConfirmDialog({ open: false, membershipId: '', memberName: '' })}
      />
    </Box>
  )
}

export default OrgMembersTable
