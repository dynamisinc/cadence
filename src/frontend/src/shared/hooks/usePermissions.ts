/**
 * usePermissions Hook
 *
 * Provides permission checking utilities based on the current user's role.
 * Uses the mock user profile from localStorage for demo/testing purposes.
 *
 * Usage:
 * ```tsx
 * const { canEdit, canDelete, canManage, role } = usePermissions();
 *
 * if (canEdit) {
 *   // Show edit button
 * }
 * ```
 */

import { useState, useEffect, useCallback } from 'react'
import { PermissionRole, type MockUserProfile } from '../../types'

const PROFILE_STORAGE_KEYS = ['cadence-mock-profile', 'cadenceUserProfile']

interface UsePermissionsReturn {
  /** Current user role */
  role: PermissionRole;
  /** Whether user has at least Readonly access */
  canView: boolean;
  /** Whether user has at least Contributor access (can create/edit) */
  canEdit: boolean;
  /** Whether user has Manage access (full control including delete) */
  canDelete: boolean;
  /** Alias for canDelete - full management permissions */
  canManage: boolean;
  /** Check if user has a specific role or higher */
  hasRole: (requiredRole: PermissionRole) => boolean;
  /** Check if user has exactly this role */
  isRole: (role: PermissionRole) => boolean;
}

/**
 * Role hierarchy for permission checking
 * Higher index = more permissions
 */
const ROLE_HIERARCHY: PermissionRole[] = [
  PermissionRole.READONLY,
  PermissionRole.CONTRIBUTOR,
  PermissionRole.MANAGE,
]

function normalizeRole(role: string): string {
  return role.trim().toLowerCase()
}

/**
 * Get the stored user profile from localStorage
 */
const getStoredProfile = (): MockUserProfile | null => {
  for (const key of PROFILE_STORAGE_KEYS) {
    try {
      const stored = localStorage.getItem(key)
      if (stored) {
        return JSON.parse(stored) as MockUserProfile
      }
    } catch (error) {
      console.error(`Failed to load user profile from ${key}:`, error)
    }
  }
  return null
}

/**
 * Hook for checking user permissions based on their role
 */
export const usePermissions = (): UsePermissionsReturn => {
  const [role, setRole] = useState<PermissionRole>(() => {
    const profile = getStoredProfile()
    // Always store as the correct casing, but compare case-insensitively
    const storedRole = profile?.role ?? PermissionRole.READONLY
    // Find the matching role from ROLE_HIERARCHY
    const normalized = normalizeRole(storedRole)
    const found = Object.values(PermissionRole).find(
      r => normalizeRole(r) === normalized,
    )
    return (found as PermissionRole) ?? PermissionRole.READONLY
  })

  // Listen for storage changes (from ProfileMenu role switching)
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key && PROFILE_STORAGE_KEYS.includes(e.key)) {
        const profile = getStoredProfile()
        const storedRole = profile?.role ?? PermissionRole.READONLY
        const normalized = normalizeRole(storedRole)
        const found = Object.values(PermissionRole).find(
          r => normalizeRole(r) === normalized,
        )
        setRole((found as PermissionRole) ?? PermissionRole.READONLY)
      }
    }

    // Also listen for custom events within the same tab
    const handleProfileChange = () => {
      const profile = getStoredProfile()
      const storedRole = profile?.role ?? PermissionRole.READONLY
      const normalized = normalizeRole(storedRole)
      const found = Object.values(PermissionRole).find(
        r => normalizeRole(r) === normalized,
      )
      setRole((found as PermissionRole) ?? PermissionRole.READONLY)
    }

    window.addEventListener('storage', handleStorageChange)
    window.addEventListener('profile-changed', handleProfileChange)

    return () => {
      window.removeEventListener('storage', handleStorageChange)
      window.removeEventListener('profile-changed', handleProfileChange)
    }
  }, [])

  /**
   * Check if user has at least the required role level
   */
  const hasRole = useCallback(
    (requiredRole: PermissionRole): boolean => {
      // Compare using normalized roles
      const currentRoleIndex = ROLE_HIERARCHY.findIndex(
        r => normalizeRole(r) === normalizeRole(role),
      )
      const requiredRoleIndex = ROLE_HIERARCHY.findIndex(
        r => normalizeRole(r) === normalizeRole(requiredRole),
      )
      return currentRoleIndex >= requiredRoleIndex
    },
    [role],
  )

  /**
   * Check if user has exactly this role
   */
  const isRole = useCallback(
    (checkRole: PermissionRole): boolean => {
      return normalizeRole(role) === normalizeRole(checkRole)
    },
    [role],
  )

  // Derived permission flags
  const canView = hasRole(PermissionRole.READONLY)
  const canEdit = hasRole(PermissionRole.CONTRIBUTOR)
  const canDelete = hasRole(PermissionRole.MANAGE)
  const canManage = canDelete

  return {
    role,
    canView,
    canEdit,
    canDelete,
    canManage,
    hasRole,
    isRole,
  }
}

export default usePermissions
