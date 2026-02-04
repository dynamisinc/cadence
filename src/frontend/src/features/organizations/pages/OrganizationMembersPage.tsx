/**
 * OrganizationMembersPage - OrgAdmin page to manage organization members
 *
 * Features:
 * - View all organization members
 * - Add new members by email
 * - Update member roles
 * - Remove members
 *
 * @module features/organizations/pages
 */
import { useState } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Paper,
  Alert,
  CircularProgress,
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faUsers, faBuilding } from '@fortawesome/free-solid-svg-icons'
import { faHome } from '@fortawesome/free-solid-svg-icons'
import {
  useCurrentOrgMembers,
  useAddCurrentOrgMember,
  useUpdateCurrentOrgMemberRole,
  useRemoveCurrentOrgMember,
} from '../hooks/useCurrentOrgMembers'
import { useOrganization } from '@/contexts/OrganizationContext'
import { useBreadcrumbs } from '@/core/contexts'
import { OrgMembersTable, AddMemberDialog } from '../components'
import { toast } from 'react-toastify'
import CobraStyles from '@/theme/CobraStyles'

export const OrganizationMembersPage: FC = () => {
  const { currentOrg } = useOrganization()
  const { data: members = [], isLoading, error } = useCurrentOrgMembers()
  const addMember = useAddCurrentOrgMember()
  const updateMemberRole = useUpdateCurrentOrgMemberRole()
  const removeMember = useRemoveCurrentOrgMember()

  const [addMemberDialogOpen, setAddMemberDialogOpen] = useState(false)

  // Set breadcrumbs
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'Organization', path: '/organization/details', icon: faBuilding },
    { label: 'Members' },
  ])

  const handleAddMember = async (email: string, role: string) => {
    try {
      await addMember.mutateAsync({ email, role: role as 'OrgAdmin' | 'OrgManager' | 'OrgUser' })
      toast.success('Member added successfully')
      setAddMemberDialogOpen(false)
    } catch (error: unknown) {
      console.error('[OrganizationMembersPage] Failed to add member:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to add member'
      toast.error(errorMessage)
      throw error // Re-throw so dialog knows the operation failed
    }
  }

  const handleRoleChange = async (membershipId: string, newRole: string) => {
    try {
      await updateMemberRole.mutateAsync({
        membershipId,
        role: newRole as 'OrgAdmin' | 'OrgManager' | 'OrgUser',
      })
      toast.success('Member role updated')
    } catch (error: unknown) {
      console.error('[OrganizationMembersPage] Failed to update role:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to update role'
      toast.error(errorMessage)
    }
  }

  const handleRemoveMember = async (membershipId: string) => {
    try {
      await removeMember.mutateAsync(membershipId)
      toast.success('Member removed')
    } catch (error: unknown) {
      console.error('[OrganizationMembersPage] Failed to remove member:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to remove member'
      toast.error(errorMessage)
    }
  }

  if (error) {
    return (
      <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
        <Alert severity="error" sx={{ mb: 3 }}>
          {error instanceof Error ? error.message : 'Failed to load members'}
        </Alert>
      </Box>
    )
  }

  const isPending = addMember.isPending || updateMemberRole.isPending || removeMember.isPending

  return (
    <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
      {/* Header - compact */}
      <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 2 }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.main',
            fontSize: 28,
          }}
        >
          <FontAwesomeIcon icon={faUsers} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h5" fontWeight={600}>
            Organization Members
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Manage members of {currentOrg?.name || 'your organization'}
          </Typography>
        </Box>
      </Stack>

      {/* Members Table */}
      <Paper sx={{ p: 3 }}>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress size={48} />
          </Box>
        ) : (
          <OrgMembersTable
            members={members}
            isLoading={isPending}
            onAddClick={() => setAddMemberDialogOpen(true)}
            onRoleChange={handleRoleChange}
            onRemove={handleRemoveMember}
          />
        )}
      </Paper>

      {/* Add Member Dialog */}
      <AddMemberDialog
        open={addMemberDialogOpen}
        onClose={() => setAddMemberDialogOpen(false)}
        onAdd={handleAddMember}
        isLoading={addMember.isPending}
      />
    </Box>
  )
}

export default OrganizationMembersPage
