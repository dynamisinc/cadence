/**
 * useExerciseRole - Determine user's effective role in an exercise
 *
 * Resolves role hierarchy:
 * 1. Exercise-specific role (if assigned as participant)
 * 2. System role mapped to exercise role (fallback)
 *
 * @module features/auth
 */
import { useState, useEffect, useCallback } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { roleResolutionService } from '../services/roleResolutionService'
import { hasPermission } from '../utils/permissions'
import type { ExerciseRole, Permission, SystemRole } from '../constants/rolePermissions'

export interface UseExerciseRoleReturn {
  /** Effective role in this exercise (exercise role or mapped system role) */
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
  const [exerciseRole, setExerciseRole] = useState<ExerciseRole | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  // Get system role from user
  const systemRole = (user?.role as SystemRole) || null

  // Determine effective role
  const effectiveRole = exerciseRole || mapSystemRoleToExerciseRole(systemRole)

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
