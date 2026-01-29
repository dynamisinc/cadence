/**
 * useSystemPermissions Hook
 *
 * Provides system-level permission checks based on the user's SystemRole from JWT.
 *
 * This hook is for SYSTEM-LEVEL permissions:
 * - Can create exercises
 * - Can access admin pages
 * - Can manage users
 *
 * For EXERCISE-SCOPED permissions (fire injects, record observations),
 * use `useExerciseRole(exerciseId)` from @/features/auth instead.
 *
 * @module shared/hooks
 * @see {@link @/features/auth/hooks/useExerciseRole} for exercise-scoped permissions
 *
 * Usage:
 * ```tsx
 * const { canCreateExercise, canAccessAdmin, systemRole } = useSystemPermissions();
 *
 * if (canCreateExercise) {
 *   return <CreateExerciseButton />;
 * }
 * ```
 */

import { useMemo } from 'react'
import { useAuth } from '../../contexts/AuthContext'
import { SystemRole } from '../../types'

export interface UseSystemPermissionsReturn {
  /** User's system-level role (Admin/Manager/User) - null if not authenticated */
  systemRole: SystemRole | null;
  /** Whether user is authenticated with a valid JWT */
  isAuthenticated: boolean;
  /** Whether auth state is still loading */
  isLoading: boolean;
  /** Whether user can create new exercises (Admin or Manager) */
  canCreateExercise: boolean;
  /** Whether user can access admin pages (Admin only) */
  canAccessAdmin: boolean;
  /** Whether user can manage system users (Admin only) */
  canManageUsers: boolean;
  /** Whether user can manage organization settings (Admin only) */
  canManageOrganization: boolean;
  /** Check if user has a specific system role */
  hasSystemRole: (role: SystemRole) => boolean;
  /** Check if user has at least the specified role level (Admin > Manager > User) */
  hasAtLeastRole: (role: SystemRole) => boolean;
}

/**
 * System role hierarchy values for comparison
 * Higher number = more permissions
 */
const SYSTEM_ROLE_HIERARCHY: Record<SystemRole, number> = {
  [SystemRole.User]: 1,
  [SystemRole.Manager]: 2,
  [SystemRole.Admin]: 3,
}

/**
 * Get display name for a system role
 */
export function getSystemRoleDisplayName(role: SystemRole | null): string {
  if (!role) return 'Not Authenticated'

  const displayNames: Record<SystemRole, string> = {
    [SystemRole.Admin]: 'System Administrator',
    [SystemRole.Manager]: 'Manager',
    [SystemRole.User]: 'User',
  }

  return displayNames[role] || role
}

/**
 * Get description for a system role
 */
export function getSystemRoleDescription(role: SystemRole | null): string {
  if (!role) return 'You are not currently authenticated.'

  const descriptions: Record<SystemRole, string> = {
    [SystemRole.Admin]: 'Full system access - user management, all exercises, system settings',
    [SystemRole.Manager]: 'Can create and manage exercises',
    [SystemRole.User]: 'Standard user - can participate in exercises you are assigned to',
  }

  return descriptions[role] || 'Unknown role'
}

/**
 * Hook for checking system-level permissions based on SystemRole from JWT
 *
 * @returns System permissions and role information
 *
 * @example
 * ```tsx
 * function ExerciseListHeader() {
 *   const { canCreateExercise, systemRole } = useSystemPermissions();
 *
 *   return (
 *     <Stack direction="row" justifyContent="space-between">
 *       <Typography>Exercises</Typography>
 *       {canCreateExercise && (
 *         <Button onClick={handleCreate}>Create Exercise</Button>
 *       )}
 *     </Stack>
 *   );
 * }
 * ```
 */
export function useSystemPermissions(): UseSystemPermissionsReturn {
  const { user, isAuthenticated, isLoading } = useAuth()

  return useMemo(() => {
    // Get system role from JWT
    const systemRole = (user?.role as SystemRole) || null

    // Helper to check if user has a specific role
    const hasSystemRole = (role: SystemRole): boolean => {
      return systemRole === role
    }

    // Helper to check if user has at least the specified role level
    const hasAtLeastRole = (role: SystemRole): boolean => {
      if (!systemRole) return false
      const userLevel = SYSTEM_ROLE_HIERARCHY[systemRole] ?? 0
      const requiredLevel = SYSTEM_ROLE_HIERARCHY[role] ?? 0
      return userLevel >= requiredLevel
    }

    // System-level permission flags
    const canCreateExercise = hasAtLeastRole(SystemRole.Manager)
    const canAccessAdmin = hasSystemRole(SystemRole.Admin)
    const canManageUsers = hasSystemRole(SystemRole.Admin)
    const canManageOrganization = hasSystemRole(SystemRole.Admin)

    return {
      systemRole,
      isAuthenticated,
      isLoading,
      canCreateExercise,
      canAccessAdmin,
      canManageUsers,
      canManageOrganization,
      hasSystemRole,
      hasAtLeastRole,
    }
  }, [user?.role, isAuthenticated, isLoading])
}

export default useSystemPermissions
