/**
 * PermissionGate - Conditional rendering based on permissions
 *
 * Shows children only if user has required permission(s).
 * Optionally shows fallback message when permission is denied.
 *
 * @module features/auth
 */
import type { FC, ReactNode } from 'react'
import { useExerciseRole } from '../hooks/useExerciseRole'
import type { Permission } from '../constants/rolePermissions'

export interface PermissionGateProps {
  /** Exercise ID for context */
  exerciseId: string | null;
  /** Required permission(s) */
  action: Permission | Permission[];
  /** Require all permissions (true) or any permission (false) when array */
  requireAll?: boolean;
  /** Content to show when user has permission */
  children: ReactNode;
  /** Optional content to show when user lacks permission */
  fallback?: ReactNode;
}

/**
 * Conditionally renders children based on user permissions
 *
 * @example
 * ```tsx
 * // Single permission
 * <PermissionGate exerciseId={id} action="fire_inject">
 *   <FireInjectButton />
 * </PermissionGate>
 *
 * // Multiple permissions (any)
 * <PermissionGate exerciseId={id} action={['fire_inject', 'edit_inject']} requireAll={false}>
 *   <InjectActions />
 * </PermissionGate>
 *
 * // With fallback
 * <PermissionGate
 *   exerciseId={id}
 *   action="manage_participants"
 *   fallback={<Alert severity="info">Requires Director role</Alert>}
 * >
 *   <ParticipantManager />
 * </PermissionGate>
 * ```
 */
export const PermissionGate: FC<PermissionGateProps> = ({
  exerciseId,
  action,
  requireAll = true,
  children,
  fallback = null,
}) => {
  const { can, isLoading } = useExerciseRole(exerciseId)

  // Don't render anything while loading
  if (isLoading) {
    return null
  }

  // Check permissions
  const hasPermission = Array.isArray(action)
    ? requireAll
      ? action.every(perm => can(perm))
      : action.some(perm => can(perm))
    : can(action)

  if (hasPermission) {
    return <>{children}</>
  }

  return <>{fallback}</>
}
