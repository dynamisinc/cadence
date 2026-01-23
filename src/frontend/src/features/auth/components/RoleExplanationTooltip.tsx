/**
 * RoleExplanationTooltip - Tooltip showing role hierarchy and permissions
 *
 * Wraps children with tooltip explaining user's effective role,
 * whether it's from system or exercise assignment, and what they can do.
 *
 * @module features/auth
 */
import type { FC, ReactNode } from 'react'
import { Tooltip, Box, Typography } from '@mui/material'
import { useExerciseRole } from '../hooks/useExerciseRole'
import { getRoleDisplayName, getRoleDescription } from '../utils/permissions'
import { ROLE_PERMISSIONS } from '../constants/rolePermissions'

export interface RoleExplanationTooltipProps {
  /** Exercise ID */
  exerciseId: string | null;
  /** Content to wrap with tooltip */
  children: ReactNode;
  /** Show list of permissions */
  showPermissions?: boolean;
}

/**
 * Tooltip that explains user's role and permissions
 *
 * @example
 * ```tsx
 * <RoleExplanationTooltip exerciseId={id} showPermissions>
 *   <InfoIcon />
 * </RoleExplanationTooltip>
 * ```
 */
export const RoleExplanationTooltip: FC<RoleExplanationTooltipProps> = ({
  exerciseId,
  children,
  showPermissions = false,
}) => {
  const { effectiveRole, systemRole, exerciseRole, isLoading } = useExerciseRole(exerciseId)

  if (isLoading) {
    return <>{children}</>
  }

  const roleName = getRoleDisplayName(effectiveRole)
  const roleDesc = getRoleDescription(effectiveRole)
  const permissions = ROLE_PERMISSIONS[effectiveRole] || []

  // Build tooltip content
  const tooltipContent = (
    <Box sx={{ p: 1 }}>
      <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 0.5 }}>
        Your Role: {roleName}
      </Typography>

      {/* Show role source */}
      {systemRole && (
        <Typography variant="caption" sx={{ display: 'block', mb: 1, color: 'grey.300' }}>
          {exerciseRole
            ? `(from exercise assignment - System Role: ${systemRole})`
            : `(from System Role: ${systemRole})`}
        </Typography>
      )}

      <Typography variant="body2" sx={{ mb: showPermissions ? 1 : 0 }}>
        {roleDesc}
      </Typography>

      {/* Show permission list */}
      {showPermissions && permissions.length > 0 && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="caption" sx={{ fontWeight: 600, display: 'block', mb: 0.5 }}>
            You can:
          </Typography>
          <Box component="ul" sx={{ m: 0, pl: 2 }}>
            {permissions.map(perm => (
              <Typography
                key={perm}
                component="li"
                variant="caption"
                sx={{ lineHeight: 1.4 }}
              >
                {formatPermission(perm)}
              </Typography>
            ))}
          </Box>
        </Box>
      )}
    </Box>
  )

  return (
    <Tooltip title={tooltipContent} arrow placement="bottom">
      <Box component="span" sx={{ display: 'inline-flex', cursor: 'help' }}>
        {children}
      </Box>
    </Tooltip>
  )
}

/**
 * Format permission name for display
 */
function formatPermission(permission: string): string {
  return permission
    .split('_')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ')
}
