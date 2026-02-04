/**
 * OrganizationApprovalPage - OrgAdmin page to manage inject approval settings
 *
 * Features:
 * - Configure approval workflow policy (Disabled/Optional/Required)
 * - Configure which roles can approve injects
 * - Configure self-approval policy
 *
 * @module features/organizations/pages
 */
import { useState, useEffect, useMemo } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Paper,
  FormGroup,
  FormControlLabel,
  Checkbox,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  Skeleton,
  Divider,
  Stack,
  Grid,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faShieldHalved, faSave, faUndo, faBuilding } from '@fortawesome/free-solid-svg-icons'
import { faHome } from '@fortawesome/free-solid-svg-icons'
import { toast } from 'react-toastify'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import {
  ApprovalRoles,
  ApprovalPolicy,
  SelfApprovalPolicy,
  hasApprovalRole,
} from '@/types'
import {
  useCurrentOrgApprovalPermissions,
  useUpdateCurrentOrgApprovalPermissions,
} from '../hooks/useApprovalPermissions'
import { useOrganization } from '@/contexts/OrganizationContext'
import { useBreadcrumbs } from '@/core/contexts'
import CobraStyles from '@/theme/CobraStyles'

/**
 * Role checkboxes configuration - compact labels only
 */
const APPROVAL_ROLES = [
  { role: ApprovalRoles.Administrator, label: 'Administrator' },
  { role: ApprovalRoles.ExerciseDirector, label: 'Exercise Director' },
  { role: ApprovalRoles.Controller, label: 'Controller' },
  { role: ApprovalRoles.Evaluator, label: 'Evaluator' },
]

/**
 * Policy descriptions
 */
const POLICY_DESCRIPTIONS: Record<ApprovalPolicy, string> = {
  [ApprovalPolicy.Disabled]: 'No approval required - injects can be fired immediately',
  [ApprovalPolicy.Optional]: 'Approval is available but not required',
  [ApprovalPolicy.Required]: 'All injects must be approved before firing',
}

const SELF_APPROVAL_DESCRIPTIONS: Record<SelfApprovalPolicy, string> = {
  [SelfApprovalPolicy.NeverAllowed]: 'Users cannot approve their own injects',
  [SelfApprovalPolicy.AllowedWithWarning]: 'Users can approve their own injects (shows warning)',
  [SelfApprovalPolicy.AlwaysAllowed]: 'Users can approve their own injects freely',
}

export const OrganizationApprovalPage: FC = () => {
  const { currentOrg } = useOrganization()
  const { data: permissions, isLoading, error } = useCurrentOrgApprovalPermissions()
  const updatePermissions = useUpdateCurrentOrgApprovalPermissions()

  // Local state for form
  const [approvalPolicy, setApprovalPolicy] = useState<ApprovalPolicy>(ApprovalPolicy.Disabled)
  const [authorizedRoles, setAuthorizedRoles] = useState<number>(0)
  const [selfApprovalPolicy, setSelfApprovalPolicy] = useState<SelfApprovalPolicy>(
    SelfApprovalPolicy.NeverAllowed,
  )

  // Set breadcrumbs
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'Organization', path: '/organization/details', icon: faBuilding },
    { label: 'Inject Approval' },
  ])

  // Initialize form from permissions
  useEffect(() => {
    if (permissions) {
      setApprovalPolicy(permissions.approvalPolicy as ApprovalPolicy)
      setAuthorizedRoles(permissions.authorizedApproverRoles)
      setSelfApprovalPolicy(permissions.selfApprovalPolicy as SelfApprovalPolicy)
    }
  }, [permissions])

  // Check if form has changes
  const hasChanges = useMemo(() => {
    if (!permissions) return false
    return (
      approvalPolicy !== permissions.approvalPolicy ||
      authorizedRoles !== permissions.authorizedApproverRoles ||
      selfApprovalPolicy !== permissions.selfApprovalPolicy
    )
  }, [permissions, approvalPolicy, authorizedRoles, selfApprovalPolicy])

  // Handle role checkbox toggle
  const handleRoleToggle = (role: ApprovalRoles) => {
    if (hasApprovalRole(authorizedRoles, role)) {
      setAuthorizedRoles(authorizedRoles & ~role)
    } else {
      setAuthorizedRoles(authorizedRoles | role)
    }
  }

  // Reset to saved values
  const handleReset = () => {
    if (permissions) {
      setApprovalPolicy(permissions.approvalPolicy as ApprovalPolicy)
      setAuthorizedRoles(permissions.authorizedApproverRoles)
      setSelfApprovalPolicy(permissions.selfApprovalPolicy as SelfApprovalPolicy)
    }
  }

  // Save changes
  const handleSave = async () => {
    try {
      await updatePermissions.mutateAsync({
        approvalPolicy,
        authorizedApproverRoles: authorizedRoles,
        selfApprovalPolicy,
      })
      toast.success('Approval settings saved')
    } catch (err) {
      console.error('[OrganizationApprovalPage] Failed to save:', err)
      const errorMessage = err instanceof Error ? err.message : 'Failed to save settings'
      toast.error(errorMessage)
    }
  }

  if (error) {
    return (
      <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
        <Alert severity="error">
          {error instanceof Error ? error.message : 'Failed to load approval settings'}
        </Alert>
      </Box>
    )
  }

  const isDisabled = approvalPolicy === ApprovalPolicy.Disabled

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
          <FontAwesomeIcon icon={faShieldHalved} />
        </Box>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h5" fontWeight={600}>
            Inject Approval Settings
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Configure approval workflow for {currentOrg?.name || 'your organization'}
          </Typography>
        </Box>
      </Stack>

      {/* Settings Form */}
      <Paper sx={{ p: 2.5 }}>
        {isLoading ? (
          <Box>
            <Skeleton variant="text" width="60%" height={32} />
            <Skeleton variant="rectangular" height={56} sx={{ mt: 2 }} />
            <Skeleton variant="text" width="40%" height={24} sx={{ mt: 3 }} />
            <Skeleton variant="rectangular" height={120} sx={{ mt: 1 }} />
          </Box>
        ) : (
          <Stack spacing={2}>
            {/* Policy Dropdowns - Same Row */}
            <Grid container spacing={3}>
              {/* Approval Workflow Policy */}
              <Grid size={{ xs: 12, md: 6 }}>
                <Typography variant="h6" fontWeight={600} sx={{ mb: 1 }}>
                  Approval Workflow Policy
                </Typography>
                <FormControl fullWidth size="small">
                  <InputLabel id="approval-policy-label">Policy</InputLabel>
                  <Select
                    labelId="approval-policy-label"
                    value={approvalPolicy}
                    label="Policy"
                    onChange={e => setApprovalPolicy(e.target.value as ApprovalPolicy)}
                  >
                    <MenuItem value={ApprovalPolicy.Disabled}>Disabled</MenuItem>
                    <MenuItem value={ApprovalPolicy.Optional}>Optional</MenuItem>
                    <MenuItem value={ApprovalPolicy.Required}>Required</MenuItem>
                  </Select>
                </FormControl>
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
                  {POLICY_DESCRIPTIONS[approvalPolicy]}
                </Typography>
              </Grid>

              {/* Self-Approval Policy */}
              <Grid size={{ xs: 12, md: 6 }} sx={{ opacity: isDisabled ? 0.5 : 1 }}>
                <Typography variant="h6" fontWeight={600} sx={{ mb: 1 }}>
                  Self-Approval Policy
                </Typography>
                <FormControl fullWidth size="small">
                  <InputLabel id="self-approval-label">Policy</InputLabel>
                  <Select
                    labelId="self-approval-label"
                    value={selfApprovalPolicy}
                    label="Policy"
                    onChange={e => setSelfApprovalPolicy(e.target.value as SelfApprovalPolicy)}
                    disabled={isDisabled}
                  >
                    <MenuItem value={SelfApprovalPolicy.NeverAllowed}>Never Allowed</MenuItem>
                    <MenuItem value={SelfApprovalPolicy.AllowedWithWarning}>Allowed with Warning</MenuItem>
                    <MenuItem value={SelfApprovalPolicy.AlwaysAllowed}>Always Allowed</MenuItem>
                  </Select>
                </FormControl>
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
                  {SELF_APPROVAL_DESCRIPTIONS[selfApprovalPolicy]}
                </Typography>
              </Grid>
            </Grid>

            <Divider />

            {/* Authorized Roles - compact horizontal layout */}
            <Box sx={{ opacity: isDisabled ? 0.5 : 1 }}>
              <Typography variant="h6" fontWeight={600} sx={{ mb: 0.5 }}>
                Authorized Approver Roles
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1, display: 'block' }}>
                Select which roles can approve injects
              </Typography>
              <FormGroup row>
                {APPROVAL_ROLES.map(({ role, label }) => (
                  <FormControlLabel
                    key={role}
                    control={
                      <Checkbox
                        size="small"
                        checked={hasApprovalRole(authorizedRoles, role)}
                        onChange={() => handleRoleToggle(role)}
                        disabled={isDisabled}
                      />
                    }
                    label={<Typography variant="body2">{label}</Typography>}
                    sx={{ mr: 3 }}
                  />
                ))}
              </FormGroup>
              {authorizedRoles === 0 && !isDisabled && (
                <Alert severity="warning" sx={{ mt: 1, py: 0.5 }}>
                  No roles selected. At least one role should be authorized.
                </Alert>
              )}
            </Box>

            {/* Actions - no divider above */}
            <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end', pt: 1 }}>
              <CobraSecondaryButton
                onClick={handleReset}
                disabled={!hasChanges || updatePermissions.isPending}
                startIcon={<FontAwesomeIcon icon={faUndo} />}
              >
                Reset
              </CobraSecondaryButton>
              <CobraPrimaryButton
                onClick={handleSave}
                disabled={!hasChanges || updatePermissions.isPending}
                startIcon={<FontAwesomeIcon icon={faSave} />}
              >
                {updatePermissions.isPending ? 'Saving...' : 'Save Changes'}
              </CobraPrimaryButton>
            </Box>
          </Stack>
        )}
      </Paper>
    </Box>
  )
}

export default OrganizationApprovalPage
