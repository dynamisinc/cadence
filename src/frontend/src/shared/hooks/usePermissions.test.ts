/**
 * usePermissions Hook Tests
 *
 * Tests for permission checking functionality including:
 * - Role-based permission flags
 * - Role hierarchy checking
 * - localStorage integration
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { usePermissions } from './usePermissions'
import { PermissionRole } from '../../types'

const PROFILE_STORAGE_KEY = 'dynamis-mock-profile'

describe('usePermissions', () => {
  let mockLocalStorage: Record<string, string> = {}

  beforeEach(() => {
    mockLocalStorage = {}

    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(key => {
      return mockLocalStorage[key] || null
    })

    vi.spyOn(Storage.prototype, 'setItem').mockImplementation((key, value) => {
      mockLocalStorage[key] = value
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  const setMockRole = (role: PermissionRole) => {
    mockLocalStorage[PROFILE_STORAGE_KEY] = JSON.stringify({
      role,
      email: 'test@example.com',
      fullName: 'Test User',
    })
  }

  describe('default state', () => {
    it('defaults to READONLY when no profile stored', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.role).toBe(PermissionRole.READONLY)
    })

    it('reads role from localStorage on mount', () => {
      setMockRole(PermissionRole.CONTRIBUTOR)

      const { result } = renderHook(() => usePermissions())

      expect(result.current.role).toBe(PermissionRole.CONTRIBUTOR)
    })
  })

  describe('READONLY role', () => {
    beforeEach(() => {
      setMockRole(PermissionRole.READONLY)
    })

    it('has canView permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canView).toBe(true)
    })

    it('does not have canEdit permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canEdit).toBe(false)
    })

    it('does not have canDelete permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canDelete).toBe(false)
    })

    it('does not have canManage permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canManage).toBe(false)
    })
  })

  describe('CONTRIBUTOR role', () => {
    beforeEach(() => {
      setMockRole(PermissionRole.CONTRIBUTOR)
    })

    it('has canView permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canView).toBe(true)
    })

    it('has canEdit permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canEdit).toBe(true)
    })

    it('does not have canDelete permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canDelete).toBe(false)
    })

    it('does not have canManage permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canManage).toBe(false)
    })
  })

  describe('MANAGE role', () => {
    beforeEach(() => {
      setMockRole(PermissionRole.MANAGE)
    })

    it('has canView permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canView).toBe(true)
    })

    it('has canEdit permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canEdit).toBe(true)
    })

    it('has canDelete permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canDelete).toBe(true)
    })

    it('has canManage permission', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.canManage).toBe(true)
    })
  })

  describe('hasRole', () => {
    it('returns true when user has required role', () => {
      setMockRole(PermissionRole.CONTRIBUTOR)
      const { result } = renderHook(() => usePermissions())

      expect(result.current.hasRole(PermissionRole.CONTRIBUTOR)).toBe(true)
    })

    it('returns true when user has higher role than required', () => {
      setMockRole(PermissionRole.MANAGE)
      const { result } = renderHook(() => usePermissions())

      expect(result.current.hasRole(PermissionRole.CONTRIBUTOR)).toBe(true)
      expect(result.current.hasRole(PermissionRole.READONLY)).toBe(true)
    })

    it('returns false when user has lower role than required', () => {
      setMockRole(PermissionRole.READONLY)
      const { result } = renderHook(() => usePermissions())

      expect(result.current.hasRole(PermissionRole.CONTRIBUTOR)).toBe(false)
      expect(result.current.hasRole(PermissionRole.MANAGE)).toBe(false)
    })
  })

  describe('isRole', () => {
    it('returns true for exact role match', () => {
      setMockRole(PermissionRole.CONTRIBUTOR)
      const { result } = renderHook(() => usePermissions())

      expect(result.current.isRole(PermissionRole.CONTRIBUTOR)).toBe(true)
    })

    it('returns false for different role', () => {
      setMockRole(PermissionRole.CONTRIBUTOR)
      const { result } = renderHook(() => usePermissions())

      expect(result.current.isRole(PermissionRole.READONLY)).toBe(false)
      expect(result.current.isRole(PermissionRole.MANAGE)).toBe(false)
    })
  })

  describe('profile change events', () => {
    it('updates when profile-changed event is dispatched', () => {
      setMockRole(PermissionRole.READONLY)
      const { result } = renderHook(() => usePermissions())

      expect(result.current.role).toBe(PermissionRole.READONLY)

      // Simulate profile change
      act(() => {
        setMockRole(PermissionRole.MANAGE)
        window.dispatchEvent(new Event('profile-changed'))
      })

      expect(result.current.role).toBe(PermissionRole.MANAGE)
    })
  })
})
