/**
 * EditOrganizationPage - Admin page to edit an existing organization
 *
 * Features:
 * - Edit org name, description, contact email
 * - Status display and actions (archive, deactivate, restore)
 * - Member management (add, update role, remove)
 *
 * @module features/organizations/pages
 * @see docs/features/organization-management/OM-03-edit-organization.md
 */
import { useState, useEffect } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  TextField,
  Paper,
  Alert,
  CircularProgress,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faArrowLeft,
  faSave,
  faArchive,
  faBan,
  faRotateLeft,
  faUsers,
  faClipboard,
} from '@fortawesome/free-solid-svg-icons'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraDeleteButton,
} from '@/theme/styledComponents'
import {
  useOrganization,
  useUpdateOrganization,
  useArchiveOrganization,
  useDeactivateOrganization,
  useRestoreOrganization,
} from '../hooks/useOrganizations'
import { StatusChip } from '@/shared/components'
import { toast } from 'react-toastify'
import type { OrgStatus, OrgRole } from '../types'
import { organizationService } from '../services/organizationService'
import { AddMemberDialog, OrgMembersTable } from '../components'

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
      toast.success('Member added successfully')
    },
  })

  // Update member role mutation
  const updateMemberRole = useMutation({
    mutationFn: ({ membershipId, role }: { membershipId: string; role: OrgRole }) =>
      organizationService.updateMemberRole(id!, membershipId, { role }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['organization-members', id] })
      toast.success('Member role updated')
    },
  })

  // Remove member mutation
  const removeMember = useMutation({
    mutationFn: (membershipId: string) =>
      organizationService.removeMember(id!, membershipId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['organization-members', id] })
      toast.success('Member removed')
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
      toast.error('Organization name is required')
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
      toast.success('Organization updated successfully')
      setHasChanges(false)
    } catch (error: unknown) {
      console.error('[EditOrganizationPage] Failed to update:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to update organization'
      toast.error(errorMessage)
    }
  }

  const handleArchive = async () => {
    if (!id) return
    if (!confirm('Are you sure you want to archive this organization? Users will lose access.')) {
      return
    }

    try {
      await archiveOrg.mutateAsync(id)
      toast.success('Organization archived')
    } catch (error: unknown) {
      console.error('[EditOrganizationPage] Failed to archive:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to archive organization'
      toast.error(errorMessage)
    }
  }

  const handleDeactivate = async () => {
    if (!id) return
    if (!confirm('Are you sure you want to deactivate this organization? Users will lose access.')) {
      return
    }

    try {
      await deactivateOrg.mutateAsync(id)
      toast.success('Organization deactivated')
    } catch (error: unknown) {
      console.error('[EditOrganizationPage] Failed to deactivate:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to deactivate organization'
      toast.error(errorMessage)
    }
  }

  const handleRestore = async () => {
    if (!id) return

    try {
      await restoreOrg.mutateAsync(id)
      toast.success('Organization restored to active')
    } catch (error: unknown) {
      console.error('[EditOrganizationPage] Failed to restore:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to restore organization'
      toast.error(errorMessage)
    }
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (error || !organization) {
    return (
      <Box sx={{ p: 3, maxWidth: 800, mx: 'auto' }}>
        <Alert severity="error">
          {error instanceof Error ? error.message : 'Failed to load organization'}
        </Alert>
        <Box sx={{ mt: 2 }}>
          <CobraSecondaryButton
            startIcon={<FontAwesomeIcon icon={faArrowLeft} />}
            onClick={() => navigate('/admin/organizations')}
          >
            Back to Organizations
          </CobraSecondaryButton>
        </Box>
      </Box>
    )
  }

  const isPending = updateOrg.isPending || archiveOrg.isPending || deactivateOrg.isPending || restoreOrg.isPending
  const status = normalizeStatus(organization.status)

  return (
    <Box sx={{ p: 3, maxWidth: 800, mx: 'auto' }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <CobraSecondaryButton
          startIcon={<FontAwesomeIcon icon={faArrowLeft} />}
          onClick={() => navigate('/admin/organizations')}
        >
          Back
        </CobraSecondaryButton>
        <Typography variant="h4" component="h1" sx={{ flex: 1 }}>
          Edit Organization
        </Typography>
        <StatusChip status={status} />
      </Box>

      {/* Info Cards */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
        <Paper sx={{ p: 2, flex: 1, display: 'flex', alignItems: 'center', gap: 2 }}>
          <FontAwesomeIcon icon={faUsers} size="lg" />
          <Box>
            <Typography variant="caption" color="text.secondary">
              Slug
            </Typography>
            <Typography variant="body1" fontFamily="monospace">
              {organization.slug}
            </Typography>
          </Box>
        </Paper>
        <Paper sx={{ p: 2, flex: 1, display: 'flex', alignItems: 'center', gap: 2 }}>
          <FontAwesomeIcon icon={faClipboard} size="lg" />
          <Box>
            <Typography variant="caption" color="text.secondary">
              Created
            </Typography>
            <Typography variant="body1">
              {new Date(organization.createdAt).toLocaleDateString()}
            </Typography>
          </Box>
        </Paper>
      </Box>

      {/* Edit Form */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          Organization Details
        </Typography>
        <form onSubmit={handleSubmit}>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            {/* Organization Name */}
            <TextField
              label="Organization Name"
              value={name}
              onChange={e => setName(e.target.value)}
              required
              fullWidth
              placeholder="e.g., CISA Region 4"
              helperText="The display name for this organization"
            />

            {/* Description */}
            <TextField
              label="Description"
              value={description}
              onChange={e => setDescription(e.target.value)}
              fullWidth
              multiline
              rows={3}
              placeholder="Optional description of this organization"
            />

            {/* Contact Email */}
            <TextField
              label="Contact Email"
              type="email"
              value={contactEmail}
              onChange={e => setContactEmail(e.target.value)}
              fullWidth
              placeholder="admin@organization.gov"
              helperText="Organization contact email (optional)"
            />

            {/* Actions */}
            <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
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
          </Box>
        </form>
      </Paper>

      {/* Status Actions */}
      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          Organization Status
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Current status: <StatusChip status={status} />
        </Typography>

        <Divider sx={{ my: 2 }} />

        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          {status === 'Active' && (
            <>
              <CobraSecondaryButton
                startIcon={<FontAwesomeIcon icon={faArchive} />}
                onClick={handleArchive}
                disabled={isPending}
              >
                Archive Organization
              </CobraSecondaryButton>
              <CobraDeleteButton
                startIcon={<FontAwesomeIcon icon={faBan} />}
                onClick={handleDeactivate}
                disabled={isPending}
              >
                Deactivate Organization
              </CobraDeleteButton>
            </>
          )}

          {status === 'Archived' && (
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
              onClick={handleRestore}
              disabled={isPending}
            >
              Restore to Active
            </CobraPrimaryButton>
          )}

          {status === 'Inactive' && (
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
              onClick={handleRestore}
              disabled={isPending}
            >
              Restore to Active
            </CobraPrimaryButton>
          )}
        </Box>

        {status !== 'Active' && (
          <Alert severity="warning" sx={{ mt: 2 }}>
            This organization is {status.toLowerCase()}. Users cannot access it until it is restored.
          </Alert>
        )}
      </Paper>

      {/* Members Section */}
      <Paper sx={{ p: 3, mt: 3 }}>
        {membersLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress size={24} />
          </Box>
        ) : (
          <OrgMembersTable
            members={members}
            isLoading={addMember.isPending || updateMemberRole.isPending || removeMember.isPending}
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
