/**
 * CreateOrganizationPage - Admin page to create a new organization
 *
 * Features:
 * - Form for org name, slug, description, contact email
 * - Auto-generate slug from name
 * - Slug availability check
 * - First admin email (required)
 *
 * @module features/organizations/pages
 * @see docs/features/organization-management/OM-02-create-organization.md
 */
import { useState, useEffect, useMemo } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Paper,
  Alert,
  InputAdornment,
  CircularProgress,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faTimes, faArrowLeft, faSave } from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from 'react-router-dom'
import { CobraPrimaryButton, CobraSecondaryButton, CobraTextField } from '@/theme/styledComponents'
import { useCreateOrganization, useCheckSlug } from '../hooks/useOrganizations'
import { toast } from 'react-toastify'
import { debounce } from 'lodash'

/**
 * Generate URL-safe slug from name
 */
function generateSlug(name: string): string {
  return name
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .substring(0, 50)
}

export const CreateOrganizationPage: FC = () => {
  const navigate = useNavigate()
  const createOrg = useCreateOrganization()

  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [slugManuallyEdited, setSlugManuallyEdited] = useState(false)
  const [description, setDescription] = useState('')
  const [contactEmail, setContactEmail] = useState('')
  const [firstAdminEmail, setFirstAdminEmail] = useState('')
  const [slugToCheck, setSlugToCheck] = useState('')

  const { data: slugCheck, isLoading: isCheckingSlug } = useCheckSlug(slugToCheck)

  // Auto-generate slug from name (unless manually edited)
  useEffect(() => {
    if (!slugManuallyEdited && name) {
      setSlug(generateSlug(name))
    }
  }, [name, slugManuallyEdited])

  // Debounced slug check
  const debouncedSlugCheck = useMemo(
    () =>
      debounce((value: string) => {
        if (value.length >= 3) {
          setSlugToCheck(value)
        }
      }, 500),
    [],
  )

  useEffect(() => {
    if (slug) {
      debouncedSlugCheck(slug)
    }
    return () => debouncedSlugCheck.cancel()
  }, [slug, debouncedSlugCheck])

  const handleSlugChange = (value: string) => {
    setSlugManuallyEdited(true)
    setSlug(generateSlug(value))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!name.trim() || !slug.trim() || !firstAdminEmail.trim()) {
      toast.error('Please fill in all required fields')
      return
    }

    if (slugCheck && !slugCheck.available) {
      toast.error('Please choose a different slug')
      return
    }

    try {
      await createOrg.mutateAsync({
        name: name.trim(),
        slug: slug.trim(),
        description: description.trim() || undefined,
        contactEmail: contactEmail.trim() || undefined,
        firstAdminEmail: firstAdminEmail.trim(),
      })
      toast.success('Organization created successfully')
      navigate('/admin/organizations')
    } catch (error: unknown) {
      console.error('[CreateOrganizationPage] Failed to create:', error)
      const axiosError = error as { response?: { data?: { message?: string } } }
      toast.error(axiosError.response?.data?.message || 'Failed to create organization')
    }
  }

  const isSlugValid = slug.length >= 3 && (!slugCheck || slugCheck.available)
  const canSubmit = name.trim() && slug.trim() && firstAdminEmail.trim() && isSlugValid

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
        <Typography variant="h4" component="h1">
          Create Organization
        </Typography>
      </Box>

      <Paper sx={{ p: 3 }}>
        <form onSubmit={handleSubmit}>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
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

            {/* Slug */}
            <CobraTextField
              label="Slug"
              value={slug}
              onChange={e => handleSlugChange(e.target.value)}
              required
              fullWidth
              placeholder="e.g., cisa-r4"
              helperText={
                slugCheck?.available === false
                  ? `Slug is taken. Suggestion: ${slugCheck.suggestion || slug + '-1'}`
                  : 'URL-friendly identifier (auto-generated from name)'
              }
              error={slug.length > 0 && slugCheck?.available === false}
              InputProps={{
                endAdornment: slug.length >= 3 && (
                  <InputAdornment position="end">
                    {isCheckingSlug ? (
                      <CircularProgress size={20} />
                    ) : slugCheck?.available ? (
                      <FontAwesomeIcon icon={faCheck} style={{ color: 'green' }} />
                    ) : slugCheck?.available === false ? (
                      <FontAwesomeIcon icon={faTimes} style={{ color: 'red' }} />
                    ) : null}
                  </InputAdornment>
                ),
              }}
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

            {/* First Admin Email */}
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                First Administrator
              </Typography>
              <Alert severity="info" sx={{ mb: 2 }}>
                Every organization needs at least one administrator. Enter the email
                of the first admin.
                If this user doesn't exist, they'll be invited to create an account.
              </Alert>
              <CobraTextField
                label="Admin Email"
                type="email"
                value={firstAdminEmail}
                onChange={e => setFirstAdminEmail(e.target.value)}
                required
                fullWidth
                placeholder="admin@example.com"
              />
            </Box>

            {/* Actions */}
            <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end', mt: 2 }}>
              <CobraSecondaryButton onClick={() => navigate('/admin/organizations')}>
                Cancel
              </CobraSecondaryButton>
              <CobraPrimaryButton
                type="submit"
                disabled={!canSubmit || createOrg.isPending}
                startIcon={
                  createOrg.isPending ? (
                    <CircularProgress size={16} color="inherit" />
                  ) : (
                    <FontAwesomeIcon icon={faSave} />
                  )
                }
              >
                {createOrg.isPending ? 'Creating...' : 'Create Organization'}
              </CobraPrimaryButton>
            </Box>
          </Box>
        </form>
      </Paper>
    </Box>
  )
}

export default CreateOrganizationPage
