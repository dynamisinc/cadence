/**
 * useSystemPermissions Hook Tests
 *
 * Tests for system-level permission checking based on JWT SystemRole.
 * This hook provides permissions for:
 * - Creating exercises (Admin, Manager)
 * - Accessing admin pages (Admin only)
 * - Managing users (Admin only)
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useSystemPermissions, getSystemRoleDisplayName, getSystemRoleDescription } from './useSystemPermissions'
import { SystemRole } from '../../types'

// Mock useAuth
const mockUseAuth = vi.fn()
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}))

describe('useSystemPermissions', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('when not authenticated', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      })
    })

    it('returns null systemRole', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.systemRole).toBeNull()
    })

    it('returns isAuthenticated as false', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.isAuthenticated).toBe(false)
    })

    it('denies all permissions', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.canCreateExercise).toBe(false)
      expect(result.current.canAccessAdmin).toBe(false)
      expect(result.current.canManageUsers).toBe(false)
      expect(result.current.canManageOrganization).toBe(false)
    })
  })

  describe('when authenticated as User', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-1', role: SystemRole.User },
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('returns User systemRole', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.systemRole).toBe(SystemRole.User)
    })

    it('returns isAuthenticated as true', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.isAuthenticated).toBe(true)
    })

    it('denies canCreateExercise', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.canCreateExercise).toBe(false)
    })

    it('denies admin permissions', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.canAccessAdmin).toBe(false)
      expect(result.current.canManageUsers).toBe(false)
      expect(result.current.canManageOrganization).toBe(false)
    })

    it('hasSystemRole returns true for User', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.hasSystemRole(SystemRole.User)).toBe(true)
      expect(result.current.hasSystemRole(SystemRole.Manager)).toBe(false)
      expect(result.current.hasSystemRole(SystemRole.Admin)).toBe(false)
    })

    it('hasAtLeastRole works correctly', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.hasAtLeastRole(SystemRole.User)).toBe(true)
      expect(result.current.hasAtLeastRole(SystemRole.Manager)).toBe(false)
      expect(result.current.hasAtLeastRole(SystemRole.Admin)).toBe(false)
    })
  })

  describe('when authenticated as Manager', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-1', role: SystemRole.Manager },
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('returns Manager systemRole', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.systemRole).toBe(SystemRole.Manager)
    })

    it('grants canCreateExercise', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.canCreateExercise).toBe(true)
    })

    it('denies admin-only permissions', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.canAccessAdmin).toBe(false)
      expect(result.current.canManageUsers).toBe(false)
      expect(result.current.canManageOrganization).toBe(false)
    })

    it('hasAtLeastRole works correctly', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.hasAtLeastRole(SystemRole.User)).toBe(true)
      expect(result.current.hasAtLeastRole(SystemRole.Manager)).toBe(true)
      expect(result.current.hasAtLeastRole(SystemRole.Admin)).toBe(false)
    })
  })

  describe('when authenticated as Admin', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-1', role: SystemRole.Admin },
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('returns Admin systemRole', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.systemRole).toBe(SystemRole.Admin)
    })

    it('grants canCreateExercise', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.canCreateExercise).toBe(true)
    })

    it('grants all admin permissions', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.canAccessAdmin).toBe(true)
      expect(result.current.canManageUsers).toBe(true)
      expect(result.current.canManageOrganization).toBe(true)
    })

    it('hasAtLeastRole returns true for all roles', () => {
      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.hasAtLeastRole(SystemRole.User)).toBe(true)
      expect(result.current.hasAtLeastRole(SystemRole.Manager)).toBe(true)
      expect(result.current.hasAtLeastRole(SystemRole.Admin)).toBe(true)
    })
  })

  describe('loading state', () => {
    it('reflects isLoading from useAuth', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
      })

      const { result } = renderHook(() => useSystemPermissions())

      expect(result.current.isLoading).toBe(true)
    })
  })
})

describe('getSystemRoleDisplayName', () => {
  it('returns correct display name for Admin', () => {
    expect(getSystemRoleDisplayName(SystemRole.Admin)).toBe('System Administrator')
  })

  it('returns correct display name for Manager', () => {
    expect(getSystemRoleDisplayName(SystemRole.Manager)).toBe('Manager')
  })

  it('returns correct display name for User', () => {
    expect(getSystemRoleDisplayName(SystemRole.User)).toBe('User')
  })

  it('returns "Not Authenticated" for null', () => {
    expect(getSystemRoleDisplayName(null)).toBe('Not Authenticated')
  })
})

describe('getSystemRoleDescription', () => {
  it('returns correct description for Admin', () => {
    expect(getSystemRoleDescription(SystemRole.Admin)).toContain('Full system access')
  })

  it('returns correct description for Manager', () => {
    expect(getSystemRoleDescription(SystemRole.Manager)).toContain('create and manage exercises')
  })

  it('returns correct description for User', () => {
    expect(getSystemRoleDescription(SystemRole.User)).toContain('Standard user')
  })

  it('returns appropriate message for null', () => {
    expect(getSystemRoleDescription(null)).toContain('not currently authenticated')
  })
})
