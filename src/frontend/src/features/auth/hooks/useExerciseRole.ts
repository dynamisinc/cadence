/**
 * useExerciseRole - Determine user's effective role in an exercise
 *
 * Resolves role hierarchy by taking the HIGHEST of:
 * 1. Exercise-specific role (if assigned as participant)
 * 2. System role mapped to exercise role
 * 3. Org role mapped to exercise role
 *
 * This ensures System Admins and Org Admins retain their elevated
 * permissions even when assigned a limited exercise-level role.
 *
 * @module features/auth
 */
import { useState, useEffect, useCallback } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { useOrganization } from '@/contexts/OrganizationContext'
import { roleResolutionService } from '../services/roleResolutionService'
import { hasPermission } from '../utils/permissions'
import { ROLE_HIERARCHY } from '../constants/rolePermissions'
import type { ExerciseRole, Permission, SystemRole } from '../constants/rolePermissions'

/**
 * Map org roles to exercise role equivalents for escalation.
 * OrgAdmin/OrgManager get ExerciseDirector-equivalent permissions.
 */
function mapOrgRoleToExerciseRole(orgRole: string | null): ExerciseRole | null {
  switch (orgRole) {
    case 'OrgAdmin':
    case 'OrgManager':
      return 'ExerciseDirector'
    default:
      return null
  }
}

/**
 * Return the higher of two exercise roles based on hierarchy.
 * Null roles are treated as having no permissions.
 */
function higherRole(a: ExerciseRole | null, b: ExerciseRole | null): ExerciseRole | null {
  if (!a) return b
  if (!b) return a
  return ROLE_HIERARCHY[a] >= ROLE_HIERARCHY[b] ? a : b
}

export interface UseExerciseRoleReturn {
  /** Effective role in this exercise (highest of exercise/system/org role) */
  effectiveRole: ExerciseRole;
  /** User's system-level role */
  systemRole: SystemRole | null;
  /** User's exercise-specific role (null if not a participant) */
  exerciseRole: ExerciseRole | null;
  /** Check if user has a specific permission */
  can: (permission: Permission) => boolean;
  /** Loading state */
  isLoading: boolean;
}

/**
 * Map system roles to default exercise roles
 * Used when user has no exercise-specific assignment
 */
function mapSystemRoleToExerciseRole(systemRole: string | null): ExerciseRole {
  switch (systemRole) {
    case 'Admin':
      return 'Administrator'
    case 'Manager':
      return 'ExerciseDirector'
    case 'User':
    default:
      return 'Observer'
  }
}

/**
 * Hook to get user's effective role and permissions in an exercise
 *
 * @param exerciseId - Exercise ID (null for non-exercise contexts)
 * @returns Role information and permission checker
 *
 * @example
 * ```tsx
 * const { effectiveRole, can } = useExerciseRole(exerciseId);
 *
 * if (can('fire_inject')) {
 *   return <FireInjectButton />;
 * }
 * ```
 */
export function useExerciseRole(exerciseId: string | null): UseExerciseRoleReturn {
  const { user } = useAuth()
  const { currentOrg } = useOrganization()
  const [exerciseRole, setExerciseRole] = useState<ExerciseRole | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  // Get system role from user
  const systemRole = (user?.role as SystemRole) || null

  // Get org role from current organization context
  const orgRole = currentOrg?.role || null

  // Determine effective role as the HIGHEST of all three sources:
  // 1. Exercise-specific role (from participant assignment)
  // 2. Mapped system role (Admin -> Administrator, Manager -> ExerciseDirector)
  // 3. Mapped org role (OrgAdmin/OrgManager -> ExerciseDirector)
  const mappedSystemRole = mapSystemRoleToExerciseRole(systemRole)
  const mappedOrgRole = mapOrgRoleToExerciseRole(orgRole)
  const effectiveRole = higherRole(
    higherRole(exerciseRole, mappedOrgRole),
    mappedSystemRole,
  ) || 'Observer'

  // Permission checker using effective role
  const can = useCallback(
    (permission: Permission): boolean => {
      return hasPermission(effectiveRole, permission)
    },
    [effectiveRole],
  )

  // Get stable user ID reference to avoid infinite loops
  // Using user object directly would cause re-fetches on every render
  // because object references change even when values are the same
  const userId = user?.id

  // Fetch exercise-specific role
  useEffect(() => {
    if (!exerciseId || !userId) {
      setIsLoading(false)
      setExerciseRole(null)
      return
    }

    let isMounted = true

    const fetchExerciseRole = async () => {
      try {
        setIsLoading(true)
        const role = await roleResolutionService.getUserExerciseRole(exerciseId, userId)

        if (isMounted) {
          setExerciseRole(role)
        }
      } catch (error) {
        // On error, fall back to system role
        console.error('Failed to fetch exercise role:', error)
        if (isMounted) {
          setExerciseRole(null)
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    fetchExerciseRole()

    return () => {
      isMounted = false
    }
  }, [exerciseId, userId])

  return {
    effectiveRole,
    systemRole,
    exerciseRole,
    can,
    isLoading,
  }
}
