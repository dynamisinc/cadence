/**
 * EditOrganizationPage - Admin page to edit an existing organization
 *
 * Features:
 * - Edit org name, description, contact email
 * - Status display and actions (archive, deactivate, restore)
 * - Member management (add, update role, remove)
 *
 * Responsive Design:
 * - Full width on small screens
 * - Two-column layout on wider screens (md+)
 * - Info cards, form sections, and status actions arranged for optimal space usage
 *
 * @module features/organizations/pages
 * @see docs/features/organization-management/OM-03-edit-organization.md
 */
import { useState, useEffect } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Paper,
  Alert,
  CircularProgress,
  Grid,
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faArrowLeft,
  faSave,
  faUsers,
  faClipboard,
  faBuilding,
} from '@fortawesome/free-solid-svg-icons'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import {
  useOrganization,
  useUpdateOrganization,
  useArchiveOrganization,
  useDeactivateOrganization,
  useRestoreOrganization,
} from '../hooks/useOrganizations'
import { StatusChip } from '@/shared/components'
import { notify } from '@/shared/utils/notify'
import type { OrgStatus, OrgRole } from '../types'
import { organizationService } from '../services/organizationService'
import {
  AddMemberDialog,
  OrgMembersTable,
  OrganizationStatusActions,
  ApprovalPermissionsSettings,
} from '../components'
import CobraStyles from '@/theme/CobraStyles'

/** Valid status values */
const VALID_STATUSES: OrgStatus[] = ['Active', 'Archived', 'Inactive']

/**
 * Normalize status value - handles invalid/numeric values from API
 */
function normalizeStatus(status: OrgStatus | string | number): OrgStatus {
  if (VALID_STATUSES.includes(status as OrgStatus)) {
    return status as OrgStatus
  }
  // Default invalid values to 'Inactive' so restore button appears
  return 'Inactive'
}

export const EditOrganizationPage: FC = () => {
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()
  const { data: organization, isLoading, error } = useOrganization(id || '')
  const updateOrg = useUpdateOrganization()
  const archiveOrg = useArchiveOrganization()
  const deactivateOrg = useDeactivateOrganization()
  const restoreOrg = useRestoreOrganization()

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [contactEmail, setContactEmail] = useState('')
  const [hasChanges, setHasChanges] = useState(false)
  const [addMemberDialogOpen, setAddMemberDialogOpen] = useState(false)

  // Fetch organization members
  const { data: members = [], isLoading: membersLoading } = useQuery({
    queryKey: ['organization-members', id],
    queryFn: () => organizationService.getMembers(id!),
    enabled: !!id,
  })

  // Add member mutation
  const addMember = useMutation({
    mutationFn: ({ email, role }: { email: string; role: OrgRole }) =>
      organizationService.addMember(id!, { email, role }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['organization-members', id] })
      notify.success('Member added successfully')
    },
  })

  // Update member role mutation
  const updateMemberRole = useMutation({
    mutationFn: ({ membershipId, role }: { membershipId: string; role: OrgRole }) =>
      organizationService.updateMemberRole(id!, membershipId, { role }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['organization-members', id] })
      notify.success('Member role updated')
    },
  })

  // Remove member mutation
  const removeMember = useMutation({
    mutationFn: (membershipId: string) =>
      organizationService.removeMember(id!, membershipId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['organization-members', id] })
      notify.success('Member removed')
    },
  })

  // Populate form when org data loads
  useEffect(() => {
    if (organization) {
      setName(organization.name)
      setDescription(organization.description || '')
      setContactEmail(organization.contactEmail || '')
      setHasChanges(false)
    }
  }, [organization])

  // Track changes
  useEffect(() => {
    if (organization) {
      const changed =
        name !== organization.name ||
        description !== (organization.description || '') ||
        contactEmail !== (organization.contactEmail || '')
      setHasChanges(changed)
    }
  }, [name, description, contactEmail, organization])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!id || !name.trim()) {
      notify.error('Organization name is required')
      return
    }

    try {
      await updateOrg.mutateAsync({
        id,
        request: {
          name: name.trim(),
          description: description.trim() || undefined,
          contactEmail: contactEmail.trim() || undefined,
        },
      })
      notify.success('Organization updated successfully')
      setHasChanges(false)
    } catch (error: unknown) {
      console.error('[EditOrganizationPage] Failed to update:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to update organization'
      notify.error(errorMessage)
    }
  }

  const handleArchive = async () => {
    if (!id) return
    if (!confirm('Are you sure you want to archive this organization? Users will lose access.')) {
      return
    }

    try {
      await archiveOrg.mutateAsync(id)
      notify.success('Organization archived')
    } catch (error: unknown) {
      console.error('[EditOrganizationPage] Failed to archive:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to archive organization'
      notify.error(errorMessage)
    }
  }

  const handleDeactivate = async () => {
    if (!id) return
    if (!confirm('Are you sure you want to deactivate this organization? Users will lose access.')) {
      return
    }

    try {
      await deactivateOrg.mutateAsync(id)
      notify.success('Organization deactivated')
    } catch (error: unknown) {
      console.error('[EditOrganizationPage] Failed to deactivate:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to deactivate organization'
      notify.error(errorMessage)
    }
  }

  const handleRestore = async () => {
    if (!id) return

    try {
      await restoreOrg.mutateAsync(id)
      notify.success('Organization restored to active')
    } catch (error: unknown) {
      console.error('[EditOrganizationPage] Failed to restore:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to restore organization'
      notify.error(errorMessage)
    }
  }

  if (isLoading) {
    return (
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          minHeight: 400,
          padding: CobraStyles.Padding.MainWindow,
        }}
      >
        <CircularProgress size={48} />
      </Box>
    )
  }

  if (error || !organization) {
    return (
      <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
        <Alert severity="error" sx={{ mb: 3 }}>
          {error instanceof Error ? error.message : 'Failed to load organization'}
        </Alert>
        <CobraSecondaryButton
          startIcon={<FontAwesomeIcon icon={faArrowLeft} />}
          onClick={() => navigate('/admin/organizations')}
        >
          Back to Organizations
        </CobraSecondaryButton>
      </Box>
    )
  }

  const isPending =
    updateOrg.isPending || archiveOrg.isPending ||
    deactivateOrg.isPending || restoreOrg.isPending
  const status = normalizeStatus(organization.status)

  return (
    <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
      {/* Header */}
      <Stack direction="row" spacing={2} alignItems="center" sx={{ mb: 3 }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.main',
            fontSize: 32,
          }}
        >
          <FontAwesomeIcon icon={faBuilding} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h4" fontWeight={600}>
            Edit Organization
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {organization.name} — Manage organization settings, members, and permissions
          </Typography>
        </Box>
        <StatusChip status={status} />
      </Stack>

      {/* Responsive Grid Layout */}
      <Grid container spacing={2} alignItems="stretch">
        {/* Info Cards - Two columns on all screens */}
        <Grid size={{ xs: 6, sm: 6 }}>
          <Paper sx={{ p: 2, height: '100%', display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'text.secondary',
                fontSize: 20,
              }}
            >
              <FontAwesomeIcon icon={faUsers} />
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <Typography variant="caption" color="text.secondary">
                Slug
              </Typography>
              <Typography variant="body1" fontFamily="monospace" noWrap>
                {organization.slug}
              </Typography>
            </Box>
          </Paper>
        </Grid>

        <Grid size={{ xs: 6, sm: 6 }}>
          <Paper sx={{ p: 2, height: '100%', display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'text.secondary',
                fontSize: 20,
              }}
            >
              <FontAwesomeIcon icon={faClipboard} />
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">
                Created
              </Typography>
              <Typography variant="body1">
                {new Date(organization.createdAt).toLocaleDateString()}
              </Typography>
            </Box>
          </Paper>
        </Grid>

        {/* Edit Form - Half width on md+, full width on smaller screens */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3, height: '100%' }}>
            <Typography variant="h6" gutterBottom>
              Organization Details
            </Typography>
            <form onSubmit={handleSubmit}>
              <Stack spacing={CobraStyles.Spacing.FormFields}>
                {/* Organization Name */}
                <CobraTextField
                  label="Organization Name"
                  value={name}
                  onChange={e => setName(e.target.value)}
                  required
                  fullWidth
                  placeholder="e.g., CISA Region 4"
                  helperText="The display name for this organization"
                />

                {/* Description */}
                <CobraTextField
                  label="Description"
                  value={description}
                  onChange={e => setDescription(e.target.value)}
                  fullWidth
                  multiline
                  rows={3}
                  placeholder="Optional description of this organization"
                />

                {/* Contact Email */}
                <CobraTextField
                  label="Contact Email"
                  type="email"
                  value={contactEmail}
                  onChange={e => setContactEmail(e.target.value)}
                  fullWidth
                  placeholder="admin@organization.gov"
                  helperText="Organization contact email (optional)"
                />

                {/* Actions */}
                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end', pt: 1 }}>
                  <CobraSecondaryButton onClick={() => navigate('/admin/organizations')}>
                    Cancel
                  </CobraSecondaryButton>
                  <CobraPrimaryButton
                    type="submit"
                    disabled={!hasChanges || !name.trim() || isPending}
                    startIcon={
                      updateOrg.isPending ? (
                        <CircularProgress size={16} color="inherit" />
                      ) : (
                        <FontAwesomeIcon icon={faSave} />
                      )
                    }
                  >
                    {updateOrg.isPending ? 'Saving...' : 'Save Changes'}
                  </CobraPrimaryButton>
                </Box>
              </Stack>
            </form>
          </Paper>
        </Grid>

        {/* Status Actions - Half width on md+, full width on smaller screens */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Box sx={{ height: '100%' }}>
            <OrganizationStatusActions
              status={status}
              isPending={isPending}
              onArchive={handleArchive}
              onDeactivate={handleDeactivate}
              onRestore={handleRestore}
            />
          </Box>
        </Grid>

        {/* Approval Permissions Settings - Full width */}
        {id && (
          <Grid size={12}>
            <ApprovalPermissionsSettings
              organizationId={id}
              onSaved={() => notify.success('Approval permissions saved')}
            />
          </Grid>
        )}

        {/* Members Section - Full width */}
        <Grid size={12}>
          <Paper sx={{ p: 3 }}>
            {membersLoading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                <CircularProgress size={24} />
              </Box>
            ) : (
              <OrgMembersTable
                members={members}
                isLoading={
                  addMember.isPending ||
                  updateMemberRole.isPending ||
                  removeMember.isPending
                }
                onAddClick={() => setAddMemberDialogOpen(true)}
                onRoleChange={async (membershipId, newRole) => {
                  await updateMemberRole.mutateAsync({ membershipId, role: newRole })
                }}
                onRemove={async membershipId => {
                  await removeMember.mutateAsync(membershipId)
                }}
              />
            )}
          </Paper>
        </Grid>
      </Grid>

      {/* Add Member Dialog */}
      <AddMemberDialog
        open={addMemberDialogOpen}
        onClose={() => setAddMemberDialogOpen(false)}
        onAdd={async (email, role) => {
          await addMember.mutateAsync({ email, role })
        }}
        isLoading={addMember.isPending}
      />
    </Box>
  )
}

export default EditOrganizationPage
