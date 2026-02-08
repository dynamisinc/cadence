/**
 * OrganizationMembersPage - OrgAdmin page to manage organization members
 *
 * Features:
 * - View all organization members
 * - Add new members by email (existing users)
 * - Send email invitations to new users (EM-02)
 * - Update member roles
 * - Remove members
 * - Manage pending invitations (resend, cancel)
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
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faUsers, faBuilding, faEnvelope } from '@fortawesome/free-solid-svg-icons'
import { faHome } from '@fortawesome/free-solid-svg-icons'
import {
  useCurrentOrgMembers,
  useAddCurrentOrgMember,
  useUpdateCurrentOrgMemberRole,
  useRemoveCurrentOrgMember,
} from '../hooks/useCurrentOrgMembers'
import {
  useInvitations,
  useCreateInvitation,
  useResendInvitation,
  useCancelInvitation,
} from '../hooks/useInvitations'
import { useOrganization } from '@/contexts/OrganizationContext'
import { useBreadcrumbs } from '@/core/contexts'
import {
  OrgMembersTable,
  AddMemberDialog,
  InviteMemberDialog,
  InvitationsTable,
} from '../components'
import { toast } from 'react-toastify'
import CobraStyles from '@/theme/CobraStyles'
import { CobraPrimaryButton } from '@/theme/styledComponents'

export const OrganizationMembersPage: FC = () => {
  const { currentOrg } = useOrganization()
  const { data: members = [], isLoading, error } = useCurrentOrgMembers()
  const addMember = useAddCurrentOrgMember()
  const updateMemberRole = useUpdateCurrentOrgMemberRole()
  const removeMember = useRemoveCurrentOrgMember()

  // Invitation hooks
  const { data: invitations = [], isLoading: isLoadingInvitations } = useInvitations('Pending')
  const createInvitation = useCreateInvitation()
  const resendInvitation = useResendInvitation()
  const cancelInvitation = useCancelInvitation()

  const [addMemberDialogOpen, setAddMemberDialogOpen] = useState(false)
  const [inviteMemberDialogOpen, setInviteMemberDialogOpen] = useState(false)

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

  const handleInviteMember = async (email: string, role: string) => {
    try {
      const response = await createInvitation.mutateAsync({
        email,
        role: role as 'OrgAdmin' | 'OrgManager' | 'OrgUser',
      })
      if (response.emailSent === false) {
        toast.warning(`Invitation created for ${email}, but the email could not be delivered. You may need to resend.`)
      } else {
        toast.success(`Invitation sent to ${email}`)
      }
      setInviteMemberDialogOpen(false)
    } catch (error: unknown) {
      console.error('[OrganizationMembersPage] Failed to send invitation:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to send invitation'
      toast.error(errorMessage)
      throw error // Re-throw so dialog knows the operation failed
    }
  }

  const handleResendInvitation = async (invitationId: string) => {
    try {
      const response = await resendInvitation.mutateAsync(invitationId)
      if (response.emailSent === false) {
        toast.warning('Invitation updated but the email could not be delivered. Check your email configuration.')
      } else {
        toast.success(`Invitation resent to ${response.email}`)
      }
    } catch (error: unknown) {
      console.error('[OrganizationMembersPage] Failed to resend invitation:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to resend invitation'
      toast.error(errorMessage)
    }
  }

  const handleCancelInvitation = async (invitationId: string) => {
    try {
      await cancelInvitation.mutateAsync(invitationId)
      toast.success('Invitation cancelled')
    } catch (error: unknown) {
      console.error('[OrganizationMembersPage] Failed to cancel invitation:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to cancel invitation'
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

  const isPending =
    addMember.isPending ||
    updateMemberRole.isPending ||
    removeMember.isPending ||
    resendInvitation.isPending ||
    cancelInvitation.isPending

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
      <Paper sx={{ p: 3, mb: 3 }}>
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

      {/* Pending Invitations Section */}
      <Paper sx={{ p: 3 }}>
        <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 2 }}>
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'primary.main',
              fontSize: 24,
            }}
          >
            <FontAwesomeIcon icon={faEnvelope} />
          </Box>
          <Box sx={{ flex: 1 }}>
            <Typography variant="h6" fontWeight={600}>
              Pending Invitations
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Email invitations sent to join the organization
            </Typography>
          </Box>
          <CobraPrimaryButton
            onClick={() => setInviteMemberDialogOpen(true)}
            startIcon={<FontAwesomeIcon icon={faEnvelope} />}
          >
            Send Invitation
          </CobraPrimaryButton>
        </Stack>

        <Divider sx={{ mb: 2 }} />

        {isLoadingInvitations ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress size={48} />
          </Box>
        ) : (
          <InvitationsTable
            invitations={invitations}
            isLoading={isPending}
            onResend={handleResendInvitation}
            onCancel={handleCancelInvitation}
          />
        )}
      </Paper>

      {/* Add Member Dialog (existing users) */}
      <AddMemberDialog
        open={addMemberDialogOpen}
        onClose={() => setAddMemberDialogOpen(false)}
        onAdd={handleAddMember}
        isLoading={addMember.isPending}
      />

      {/* Invite Member Dialog (send email invitation) */}
      <InviteMemberDialog
        open={inviteMemberDialogOpen}
        onClose={() => setInviteMemberDialogOpen(false)}
        onInvite={handleInviteMember}
        isLoading={createInvitation.isPending}
      />
    </Box>
  )
}

export default OrganizationMembersPage
