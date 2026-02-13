/**
 * ApprovalPermissionsSettings Component (S11)
 *
 * Organization-level settings for configuring inject approval permissions.
 * Allows SysAdmins to configure which roles can approve injects and
 * the self-approval policy.
 *
 * @module features/organizations/components
 */
import { useState, useEffect, useRef } from 'react'
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
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faShieldHalved, faSave, faUndo } from '@fortawesome/free-solid-svg-icons'
import { notify } from '@/shared/utils/notify'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import {
  ApprovalRoles,
  ApprovalPolicy,
  SelfApprovalPolicy,
} from '@/types'
import {
  useApprovalPermissions,
  useUpdateApprovalPermissions,
} from '../hooks/useApprovalPermissions'
import { useOrganization } from '../hooks/useOrganizations'
import { organizationService } from '../services/organizationService'
import { useMutation, useQueryClient } from '@tanstack/react-query'

interface ApprovalPermissionsSettingsProps {
  /** Organization ID */
  organizationId: string
  /** Callback when settings are saved */
  onSaved?: () => void
}

/**
 * ApprovalPermissionsSettings Component
 *
 * Displays and allows editing of organization-level approval permissions.
 *
 * @example
 * <ApprovalPermissionsSettings
 *   organizationId={orgId}
 *   onSaved={() => notify.success('Permissions updated')}
 * />
 */
export const ApprovalPermissionsSettings: FC<ApprovalPermissionsSettingsProps> = ({
  organizationId,
  onSaved,
}) => {
  const queryClient = useQueryClient()
  const { data: organization } = useOrganization(organizationId)
  const { data: permissions, isLoading, error } = useApprovalPermissions(organizationId)
  const updateMutation = useUpdateApprovalPermissions()

  // Track initial load with refs to avoid re-syncing on every render
  const orgInitializedRef = useRef(false)
  const permissionsInitializedRef = useRef(false)

  // Local state for editing
  const [approvalPolicy, setApprovalPolicy] = useState<string>(ApprovalPolicy.Disabled)
  const [authorizedRoles, setAuthorizedRoles] = useState<number>(
    ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector,
  )
  const [selfApprovalPolicy, setSelfApprovalPolicy] = useState<string>(
    SelfApprovalPolicy.NeverAllowed,
  )
  const [hasChanges, setHasChanges] = useState(false)

  // Mutation for approval policy
  const updatePolicyMutation = useMutation({
    mutationFn: (policy: string) =>
      organizationService.updateApprovalPolicy(organizationId, policy),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['organization', organizationId] })
    },
  })

  // Sync state with loaded organization (only on initial load)
  useEffect(() => {
    if (organization && !orgInitializedRef.current) {
      setApprovalPolicy(organization.injectApprovalPolicy || ApprovalPolicy.Disabled)
      orgInitializedRef.current = true
    }
  }, [organization])

  // Sync state with loaded permissions (only on initial load)
  useEffect(() => {
    if (permissions && !permissionsInitializedRef.current) {
      // Use sensible defaults if no roles are configured yet
      const defaultRoles = ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector
      // Ensure authorizedRoles is a number (handle potential string from old data)
      const rolesValue = typeof permissions.authorizedRoles === 'number'
        ? permissions.authorizedRoles
        : defaultRoles
      setAuthorizedRoles(rolesValue || defaultRoles)
      setSelfApprovalPolicy(permissions.selfApprovalPolicy || SelfApprovalPolicy.NeverAllowed)
      permissionsInitializedRef.current = true
    }
  }, [permissions])

  const handleApprovalPolicyChange = (value: string) => {
    setApprovalPolicy(value)
    setHasChanges(true)
  }

  const handleSelfApprovalChange = (value: string) => {
    setSelfApprovalPolicy(value)
    setHasChanges(true)
  }

  const handleSave = async () => {
    try {
      // Save approval policy if changed
      if (organization?.injectApprovalPolicy !== approvalPolicy) {
        await updatePolicyMutation.mutateAsync(approvalPolicy)
      }
      // Save permissions
      await updateMutation.mutateAsync({
        orgId: organizationId,
        request: {
          authorizedRoles,
          selfApprovalPolicy:
            selfApprovalPolicy as typeof SelfApprovalPolicy[keyof typeof SelfApprovalPolicy],
        },
      })
      setHasChanges(false)
      notify.success('Approval settings saved')
      onSaved?.()
    } catch {
      notify.error('Failed to save approval settings')
    }
  }

  const handleReset = () => {
    if (organization) {
      setApprovalPolicy(organization.injectApprovalPolicy || ApprovalPolicy.Disabled)
    }
    if (permissions) {
      const defaultRoles = ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector
      const rolesValue = typeof permissions.authorizedRoles === 'number'
        ? permissions.authorizedRoles
        : defaultRoles
      setAuthorizedRoles(rolesValue || defaultRoles)
      setSelfApprovalPolicy(permissions.selfApprovalPolicy || SelfApprovalPolicy.NeverAllowed)
    }
    setHasChanges(false)
  }

  if (isLoading) {
    return (
      <Paper sx={{ p: 3 }}>
        <Skeleton width="40%" height={32} />
        <Skeleton width="60%" height={24} sx={{ mt: 1 }} />
        <Skeleton width="100%" height={150} sx={{ mt: 2 }} />
      </Paper>
    )
  }

  if (error) {
    return (
      <Paper sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load approval permissions. Please try again.
        </Alert>
      </Paper>
    )
  }

  return (
    <Paper sx={{ p: 3 }}>
      <Box display="flex" alignItems="center" gap={1} mb={1}>
        <FontAwesomeIcon icon={faShieldHalved} />
        <Typography variant="h6">Inject Approval Permissions</Typography>
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Configure which roles can approve injects and self-approval policy for this
        organization.
      </Typography>

      <Divider sx={{ mb: 3 }} />

      {/* Approval Workflow Policy */}
      <Typography variant="subtitle1" fontWeight={500} gutterBottom>
        Approval Workflow
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Control whether inject approval is available for exercises in this organization.
      </Typography>

      <FormControl fullWidth sx={{ mb: 3 }}>
        <InputLabel id="approval-policy-label">Approval Workflow Policy</InputLabel>
        <Select
          labelId="approval-policy-label"
          value={approvalPolicy}
          label="Approval Workflow Policy"
          onChange={e => handleApprovalPolicyChange(e.target.value)}
        >
          <MenuItem value={ApprovalPolicy.Disabled}>
            <Box>
              <Typography variant="body1">Disabled</Typography>
              <Typography variant="caption" color="text.secondary">
                Approval workflow not available. Injects can be fired directly.
              </Typography>
            </Box>
          </MenuItem>
          <MenuItem value={ApprovalPolicy.Optional}>
            <Box>
              <Typography variant="body1">Optional (Recommended)</Typography>
              <Typography variant="caption" color="text.secondary">
                Exercise Directors can enable approval per exercise.
              </Typography>
            </Box>
          </MenuItem>
          <MenuItem value={ApprovalPolicy.Required}>
            <Box>
              <Typography variant="body1">Required</Typography>
              <Typography variant="caption" color="text.secondary">
                All exercises require approval. Admins can override if needed.
              </Typography>
            </Box>
          </MenuItem>
        </Select>
      </FormControl>

      {approvalPolicy === ApprovalPolicy.Disabled && (
        <Alert severity="info" sx={{ mb: 3 }}>
          Approval workflow is disabled. Injects will move directly from Draft to ready-to-fire
          without requiring approval.
        </Alert>
      )}

      <Divider sx={{ mb: 3 }} />

      {/* Authorized Roles Section */}
      <Typography variant="subtitle1" fontWeight={500} gutterBottom>
        Roles Authorized to Approve
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Select which exercise roles can approve injects. At least one role must be
        selected.
      </Typography>

      <FormGroup sx={{ ml: 1, mb: 3 }}>
        <FormControlLabel
          control={
            <Checkbox
              checked={(authorizedRoles & ApprovalRoles.Administrator) !== 0}
              onChange={(_e, checked) => {
                const newRoles = checked
                  ? authorizedRoles | ApprovalRoles.Administrator
                  : authorizedRoles & ~ApprovalRoles.Administrator
                setAuthorizedRoles(newRoles)
                setHasChanges(true)
              }}
            />
          }
          label={
            <Box>
              <Typography variant="body1">Administrator</Typography>
              <Typography variant="caption" color="text.secondary">
                System administrators with full platform access
              </Typography>
            </Box>
          }
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={(authorizedRoles & ApprovalRoles.ExerciseDirector) !== 0}
              onChange={(_e, checked) => {
                const newRoles = checked
                  ? authorizedRoles | ApprovalRoles.ExerciseDirector
                  : authorizedRoles & ~ApprovalRoles.ExerciseDirector
                setAuthorizedRoles(newRoles)
                setHasChanges(true)
              }}
            />
          }
          label={
            <Box>
              <Typography variant="body1">Exercise Director</Typography>
              <Typography variant="caption" color="text.secondary">
                Overall exercise authority and Go/No-Go decisions
              </Typography>
            </Box>
          }
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={(authorizedRoles & ApprovalRoles.Controller) !== 0}
              onChange={(_e, checked) => {
                const newRoles = checked
                  ? authorizedRoles | ApprovalRoles.Controller
                  : authorizedRoles & ~ApprovalRoles.Controller
                setAuthorizedRoles(newRoles)
                setHasChanges(true)
              }}
            />
          }
          label={
            <Box>
              <Typography variant="body1">Controller</Typography>
              <Typography variant="caption" color="text.secondary">
                Inject delivery and conduct management
              </Typography>
            </Box>
          }
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={(authorizedRoles & ApprovalRoles.Evaluator) !== 0}
              onChange={(_e, checked) => {
                const newRoles = checked
                  ? authorizedRoles | ApprovalRoles.Evaluator
                  : authorizedRoles & ~ApprovalRoles.Evaluator
                setAuthorizedRoles(newRoles)
                setHasChanges(true)
              }}
            />
          }
          label={
            <Box>
              <Typography variant="body1">Evaluator</Typography>
              <Typography variant="caption" color="text.secondary">
                Observation recording for AAR
              </Typography>
            </Box>
          }
        />
      </FormGroup>

      {authorizedRoles === ApprovalRoles.None && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          At least one role must be authorized to approve injects.
        </Alert>
      )}

      <Divider sx={{ mb: 3 }} />

      {/* Self-Approval Policy Section */}
      <Typography variant="subtitle1" fontWeight={500} gutterBottom>
        Self-Approval Policy
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Control whether users can approve injects they submitted.
      </Typography>

      <FormControl fullWidth sx={{ mb: 3 }}>
        <InputLabel id="self-approval-policy-label">Self-Approval Policy</InputLabel>
        <Select
          labelId="self-approval-policy-label"
          value={selfApprovalPolicy}
          label="Self-Approval Policy"
          onChange={e => handleSelfApprovalChange(e.target.value)}
        >
          <MenuItem value={SelfApprovalPolicy.NeverAllowed}>
            <Box>
              <Typography variant="body1">Never Allowed (Recommended)</Typography>
              <Typography variant="caption" color="text.secondary">
                Users cannot approve injects they submitted. Enforces separation of duties.
              </Typography>
            </Box>
          </MenuItem>
          <MenuItem value={SelfApprovalPolicy.AllowedWithWarning}>
            <Box>
              <Typography variant="body1">Allowed with Warning</Typography>
              <Typography variant="caption" color="text.secondary">
                Users can self-approve with confirmation. Self-approvals are flagged in
                audit logs.
              </Typography>
            </Box>
          </MenuItem>
          <MenuItem value={SelfApprovalPolicy.AlwaysAllowed}>
            <Box>
              <Typography variant="body1">Always Allowed</Typography>
              <Typography variant="caption" color="text.secondary">
                No restrictions on self-approval. Not recommended for compliance-sensitive
                exercises.
              </Typography>
            </Box>
          </MenuItem>
        </Select>
      </FormControl>

      {selfApprovalPolicy === SelfApprovalPolicy.AlwaysAllowed && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          Self-approval is not recommended for compliance-sensitive exercises as it
          bypasses separation of duties.
        </Alert>
      )}

      <Divider sx={{ mb: 3 }} />

      {/* Action Buttons */}
      <Box display="flex" gap={2} justifyContent="flex-end">
        <CobraSecondaryButton
          startIcon={<FontAwesomeIcon icon={faUndo} />}
          onClick={handleReset}
          disabled={!hasChanges || updateMutation.isPending || updatePolicyMutation.isPending}
        >
          Reset
        </CobraSecondaryButton>
        <CobraPrimaryButton
          startIcon={<FontAwesomeIcon icon={faSave} />}
          onClick={handleSave}
          disabled={
            !hasChanges ||
            updateMutation.isPending ||
            updatePolicyMutation.isPending ||
            authorizedRoles === ApprovalRoles.None
          }
        >
          {updateMutation.isPending || updatePolicyMutation.isPending ? 'Saving...' : 'Save Changes'}
        </CobraPrimaryButton>
      </Box>

      {(updateMutation.isError || updatePolicyMutation.isError) && (
        <Alert severity="error" sx={{ mt: 2 }}>
          Failed to save changes. Please try again.
        </Alert>
      )}

      {updateMutation.isSuccess && !hasChanges && (
        <Alert severity="success" sx={{ mt: 2 }}>
          Approval settings saved successfully.
        </Alert>
      )}
    </Paper>
  )
}

export default ApprovalPermissionsSettings
