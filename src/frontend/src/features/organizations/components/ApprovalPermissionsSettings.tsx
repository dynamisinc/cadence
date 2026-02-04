/**
 * ApprovalPermissionsSettings Component (S11)
 *
 * Organization-level settings for configuring inject approval permissions.
 * Allows SysAdmins to configure which roles can approve injects and
 * the self-approval policy.
 *
 * @module features/organizations/components
 */
import { useState, useEffect } from 'react'
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
import { faShieldCheck, faSave, faUndo } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import {
  ApprovalRoles,
  SelfApprovalPolicy,
  hasApprovalRole,
  createApprovalRoles,
} from '@/types'
import {
  useApprovalPermissions,
  useUpdateApprovalPermissions,
} from '../hooks/useApprovalPermissions'

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
 *   onSaved={() => toast.success('Permissions updated')}
 * />
 */
export const ApprovalPermissionsSettings: FC<ApprovalPermissionsSettingsProps> = ({
  organizationId,
  onSaved,
}) => {
  const { data: permissions, isLoading, error } = useApprovalPermissions(organizationId)
  const updateMutation = useUpdateApprovalPermissions()

  // Local state for editing
  const [authorizedRoles, setAuthorizedRoles] = useState<number>(
    ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector,
  )
  const [selfApprovalPolicy, setSelfApprovalPolicy] = useState<string>(
    SelfApprovalPolicy.NeverAllowed,
  )
  const [hasChanges, setHasChanges] = useState(false)

  // Sync state with loaded permissions
  useEffect(() => {
    if (permissions) {
      setAuthorizedRoles(permissions.authorizedRoles)
      setSelfApprovalPolicy(permissions.selfApprovalPolicy)
      setHasChanges(false)
    }
  }, [permissions])

  const handleRoleToggle = (role: number) => {
    const newRoles = hasApprovalRole(authorizedRoles, role)
      ? authorizedRoles & ~role
      : authorizedRoles | role
    setAuthorizedRoles(newRoles)
    setHasChanges(true)
  }

  const handleSelfApprovalChange = (value: string) => {
    setSelfApprovalPolicy(value)
    setHasChanges(true)
  }

  const handleSave = async () => {
    try {
      await updateMutation.mutateAsync({
        orgId: organizationId,
        request: {
          authorizedRoles: createApprovalRoles(authorizedRoles),
          selfApprovalPolicy: selfApprovalPolicy as typeof SelfApprovalPolicy[keyof typeof SelfApprovalPolicy],
        },
      })
      setHasChanges(false)
      onSaved?.()
    } catch {
      // Error is handled by React Query
    }
  }

  const handleReset = () => {
    if (permissions) {
      setAuthorizedRoles(permissions.authorizedRoles)
      setSelfApprovalPolicy(permissions.selfApprovalPolicy)
      setHasChanges(false)
    }
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
        <FontAwesomeIcon icon={faShieldCheck} />
        <Typography variant="h6">Inject Approval Permissions</Typography>
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Configure which roles can approve injects and self-approval policy for this
        organization.
      </Typography>

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
              checked={hasApprovalRole(authorizedRoles, ApprovalRoles.Administrator)}
              onChange={() => handleRoleToggle(ApprovalRoles.Administrator)}
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
              checked={hasApprovalRole(authorizedRoles, ApprovalRoles.ExerciseDirector)}
              onChange={() => handleRoleToggle(ApprovalRoles.ExerciseDirector)}
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
              checked={hasApprovalRole(authorizedRoles, ApprovalRoles.Controller)}
              onChange={() => handleRoleToggle(ApprovalRoles.Controller)}
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
              checked={hasApprovalRole(authorizedRoles, ApprovalRoles.Evaluator)}
              onChange={() => handleRoleToggle(ApprovalRoles.Evaluator)}
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
          onChange={(e) => handleSelfApprovalChange(e.target.value)}
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
          disabled={!hasChanges || updateMutation.isPending}
        >
          Reset
        </CobraSecondaryButton>
        <CobraPrimaryButton
          startIcon={<FontAwesomeIcon icon={faSave} />}
          onClick={handleSave}
          disabled={
            !hasChanges ||
            updateMutation.isPending ||
            authorizedRoles === ApprovalRoles.None
          }
        >
          {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
        </CobraPrimaryButton>
      </Box>

      {updateMutation.isError && (
        <Alert severity="error" sx={{ mt: 2 }}>
          Failed to save changes. Please try again.
        </Alert>
      )}

      {updateMutation.isSuccess && !hasChanges && (
        <Alert severity="success" sx={{ mt: 2 }}>
          Approval permissions saved successfully.
        </Alert>
      )}
    </Paper>
  )
}

export default ApprovalPermissionsSettings
