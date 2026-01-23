/**
 * EffectiveRoleBadge - Display user's effective role in an exercise
 *
 * Shows color-coded badge with tooltip explaining role and permissions.
 * Indicates when exercise role overrides system role.
 *
 * @module features/auth
 */
import type { FC } from 'react'
import { Chip, Tooltip, Skeleton, Box } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faShield } from '@fortawesome/free-solid-svg-icons'
import { useExerciseRole } from '../hooks/useExerciseRole'
import { getRoleDisplayName, getRoleDescription, getRoleColor } from '../utils/permissions'

export interface EffectiveRoleBadgeProps {
  /** Exercise ID */
  exerciseId: string | null;
  /** Show override indicator in tooltip */
  showOverride?: boolean;
  /** Chip size */
  size?: 'small' | 'medium';
}

/**
 * Displays user's effective role as a colored badge with tooltip
 *
 * Color coding:
 * - Red (error): Administrator, Exercise Director
 * - Blue (primary): Controller
 * - Green (success): Evaluator
 * - Gray (default): Observer
 *
 * @example
 * ```tsx
 * <EffectiveRoleBadge exerciseId={exercise.id} showOverride />
 * ```
 */
export const EffectiveRoleBadge: FC<EffectiveRoleBadgeProps> = ({
  exerciseId,
  showOverride = false,
  size = 'medium',
}) => {
  const { effectiveRole, systemRole, exerciseRole, isLoading } = useExerciseRole(exerciseId)

  if (isLoading) {
    return (
      <Box data-testid="role-badge-skeleton">
        <Skeleton variant="rounded" width={100} height={size === 'small' ? 24 : 32} />
      </Box>
    )
  }

  const displayName = getRoleDisplayName(effectiveRole)
  const description = getRoleDescription(effectiveRole)
  const color = getRoleColor(effectiveRole)

  // Build tooltip content
  let tooltipContent = description

  if (showOverride && systemRole) {
    if (exerciseRole) {
      // Exercise role is being used (may override system role)
      const systemRoleDisplay =
        systemRole === 'Admin'
          ? 'Administrator'
          : systemRole === 'Manager'
            ? 'Manager'
            : 'User'

      if (effectiveRole !== systemRoleDisplay) {
        tooltipContent = `Your role: ${displayName}\n(overrides System Role: ${systemRoleDisplay})\n\n${description}`
      } else {
        tooltipContent = `Your role: ${displayName}\n(from exercise assignment)\n\n${description}`
      }
    } else {
      // Using system role as default
      tooltipContent = `Your role: ${displayName}\n(from System Role: ${systemRole})\n\n${description}`
    }
  }

  return (
    <Tooltip title={tooltipContent} arrow>
      <Chip
        icon={<FontAwesomeIcon icon={faShield} />}
        label={displayName}
        color={color}
        size={size}
        sx={{
          fontWeight: 500,
          cursor: 'help',
        }}
      />
    </Tooltip>
  )
}
