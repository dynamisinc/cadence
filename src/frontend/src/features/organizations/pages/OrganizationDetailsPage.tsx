/**
 * OrganizationDetailsPage - OrgAdmin page to view/edit current organization
 *
 * Features:
 * - View organization details (name, description, contact email)
 * - Edit organization details (OrgAdmin only)
 * - View organization slug and creation date
 *
 * @module features/organizations/pages
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
  faSave,
  faBuilding,
  faCalendar,
  faLink,
} from '@fortawesome/free-solid-svg-icons'
import { faHome } from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import {
  useCurrentOrganization,
  useUpdateCurrentOrganization,
} from '../hooks/useOrganizations'
import { StatusChip } from '@/shared/components'
import { useBreadcrumbs } from '@/core/contexts'
import { notify } from '@/shared/utils/notify'
import CobraStyles from '@/theme/CobraStyles'

export const OrganizationDetailsPage: FC = () => {
  const { data: organization, isLoading, error } = useCurrentOrganization()
  const updateOrg = useUpdateCurrentOrganization()

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [contactEmail, setContactEmail] = useState('')
  const [hasChanges, setHasChanges] = useState(false)

  // Set breadcrumbs
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'Organization', path: '/organization/details', icon: faBuilding },
    { label: 'Details' },
  ])

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

    if (!name.trim()) {
      notify.error('Organization name is required')
      return
    }

    try {
      await updateOrg.mutateAsync({
        name: name.trim(),
        description: description.trim() || undefined,
        contactEmail: contactEmail.trim() || undefined,
      })
      notify.success('Organization updated successfully')
      setHasChanges(false)
    } catch (error: unknown) {
      console.error('[OrganizationDetailsPage] Failed to update:', error)
      const errorMessage = error instanceof Error ? error.message : 'Failed to update organization'
      notify.error(errorMessage)
    }
  }

  const handleReset = () => {
    if (organization) {
      setName(organization.name)
      setDescription(organization.description || '')
      setContactEmail(organization.contactEmail || '')
      setHasChanges(false)
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
      </Box>
    )
  }

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
          <FontAwesomeIcon icon={faBuilding} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h5" fontWeight={600}>
            Organization Details
          </Typography>
          <Typography variant="caption" color="text.secondary">
            View and update your organization information
          </Typography>
        </Box>
        <StatusChip status={organization.status} />
      </Stack>

      {/* Content */}
      <Grid container spacing={2}>
        {/* Info Cards */}
        <Grid size={{ xs: 6, md: 4 }}>
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
              <FontAwesomeIcon icon={faLink} />
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

        <Grid size={{ xs: 6, md: 4 }}>
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
              <FontAwesomeIcon icon={faCalendar} />
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

        {/* Edit Form */}
        <Grid size={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Edit Details
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
                  <CobraSecondaryButton
                    onClick={handleReset}
                    disabled={!hasChanges || updateOrg.isPending}
                  >
                    Reset
                  </CobraSecondaryButton>
                  <CobraPrimaryButton
                    type="submit"
                    disabled={!hasChanges || !name.trim() || updateOrg.isPending}
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
      </Grid>
    </Box>
  )
}

export default OrganizationDetailsPage
