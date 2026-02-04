/**
 * ExerciseApprovalToggle Component
 *
 * Toggle switch for enabling/disabling inject approval on an exercise (S01-S02).
 *
 * @module features/exercises/components
 */

import { useState } from 'react'
import {
  Box,
  Typography,
  FormControlLabel,
  Switch,
  Alert,
  Skeleton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faShieldHalved, faInfoCircle } from '@fortawesome/free-solid-svg-icons'
import { ApprovalPolicy } from '@/types'
import { useApprovalSettings } from '../hooks'
import { CobraTextField } from '@/theme/styledComponents'

interface ExerciseApprovalToggleProps {
  /** The exercise ID */
  exerciseId: string
  /** Whether the current user is an Admin (can override required policy) */
  isAdmin?: boolean
  /** Callback when settings change */
  onChange?: (requireApproval: boolean) => void
}

/**
 * Exercise Approval Toggle
 *
 * Allows Exercise Directors to enable/disable inject approval for an exercise.
 * Behavior depends on organization policy:
 * - Disabled: Toggle is not shown (approval not available)
 * - Optional: Director can enable/disable (default off)
 * - Required: Toggle is locked on, only Admins can disable with override
 *
 * @example
 * <ExerciseApprovalToggle
 *   exerciseId={exerciseId}
 *   isAdmin={user.role === 'Admin'}
 * />
 */
export const ExerciseApprovalToggle = ({
  exerciseId,
  isAdmin = false,
  onChange,
}: ExerciseApprovalToggleProps) => {
  const [overrideReason, setOverrideReason] = useState('')
  const { settings, isLoading, updateSettings, isUpdating } =
    useApprovalSettings(exerciseId)

  if (isLoading) {
    return (
      <Box>
        <Skeleton width="80%" height={40} />
        <Skeleton width="60%" height={20} />
      </Box>
    )
  }

  // If policy is Disabled, don't show the toggle
  if (settings?.organizationPolicy === ApprovalPolicy.Disabled) {
    return (
      <Alert severity="info" icon={<FontAwesomeIcon icon={faInfoCircle} />}>
        Inject approval is disabled for this organization.
      </Alert>
    )
  }

  const isRequired = settings?.organizationPolicy === ApprovalPolicy.Required
  const isLocked = isRequired && !isAdmin
  const canOverride = isRequired && isAdmin
  const requireApproval = settings?.requireInjectApproval ?? false

  const handleToggle = async (checked: boolean) => {
    const isOverride = isRequired && !checked
    try {
      await updateSettings({
        requireInjectApproval: checked,
        isOverride,
        overrideReason: isOverride ? overrideReason || undefined : undefined,
      })
      onChange?.(checked)
      if (isOverride) {
        setOverrideReason('')
      }
    } catch {
      // Error handling is done in the hook
    }
  }

  return (
    <Box>
      <Box display="flex" alignItems="flex-start" gap={2}>
        <FontAwesomeIcon
          icon={faShieldHalved}
          style={{ marginTop: 4, opacity: 0.6 }}
        />
        <Box flex={1}>
          <FormControlLabel
            control={
              <Switch
                checked={requireApproval}
                onChange={e => handleToggle(e.target.checked)}
                disabled={isLocked || isUpdating}
              />
            }
            label={
              <Typography variant="body1" fontWeight={500}>
                Require inject approval for this exercise
              </Typography>
            }
          />

          {settings?.organizationPolicy === ApprovalPolicy.Optional && (
            <Typography variant="body2" color="text.secondary" sx={{ ml: 6 }}>
              When enabled, injects must be approved by an Exercise Director before
              the exercise can be activated.
            </Typography>
          )}

          {isLocked && (
            <Alert severity="info" sx={{ mt: 1, ml: 6 }} icon={false}>
              <Typography variant="body2">
                Organization policy requires approval for all exercises.
              </Typography>
            </Alert>
          )}

          {canOverride && requireApproval && (
            <Alert severity="warning" sx={{ mt: 1, ml: 6 }} icon={false}>
              <Typography variant="body2" gutterBottom>
                As an admin, you can disable approval for this exercise.
                This overrides the organization policy.
              </Typography>
              <CobraTextField
                fullWidth
                size="small"
                label="Override reason (optional)"
                placeholder="Why is approval being disabled?"
                value={overrideReason}
                onChange={e => setOverrideReason(e.target.value)}
                sx={{ mt: 1 }}
              />
            </Alert>
          )}

          {settings?.approvalPolicyOverridden && (
            <Alert severity="warning" sx={{ mt: 1, ml: 6 }} icon={false}>
              <Typography variant="body2">
                <strong>Policy Override:</strong> Approval has been{' '}
                {requireApproval ? 'enabled' : 'disabled'} for this exercise,
                overriding the organization policy.
              </Typography>
              {settings.approvalOverrideReason && (
                <Typography variant="caption" display="block" sx={{ mt: 0.5 }}>
                  Reason: {settings.approvalOverrideReason}
                </Typography>
              )}
            </Alert>
          )}
        </Box>
      </Box>
    </Box>
  )
}

export default ExerciseApprovalToggle
