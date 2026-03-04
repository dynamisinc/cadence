/**
 * useFilteredMenu Hook Tests
 *
 * Exhaustive tests for role-based menu filtering per:
 * @see docs/features/navigation-shell/S02-role-based-menu-visibility.md
 *
 * Test coverage:
 * - All HSEEP roles (Administrator, ExerciseDirector, Controller, Evaluator, Observer)
 * - System roles (Admin, Manager, User)
 * - Exercise context enabling/disabling
 * - Section grouping and visibility
 * - Edge cases and error handling
 */

import { renderHook } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { useFilteredMenu } from './useFilteredMenu'
import { HseepRole } from '@/types'

// Mock useExerciseRole hook
const mockUseExerciseRole = vi.fn()
vi.mock('@/features/auth/hooks/useExerciseRole', () => ({
  useExerciseRole: () => mockUseExerciseRole(),
}))

// Mock useAuth hook
const mockUseAuth = vi.fn()
vi.mock('@/contexts/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}))

// Mock useOrganization hook
const mockUseOrganization = vi.fn()
vi.mock('@/contexts/OrganizationContext', () => ({
  useOrganization: () => mockUseOrganization(),
}))

// Mock useFeatureFlags hook
const mockUseFeatureFlags = vi.fn()
vi.mock('@/admin/contexts/FeatureFlagsContext', () => ({
  useFeatureFlags: () => mockUseFeatureFlags(),
}))

describe('useFilteredMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Default: authenticated admin user
    mockUseAuth.mockReturnValue({
      user: { id: 'user-1', email: 'admin@example.com', role: 'Admin' },
    })
    // Default: user has an organization with OrgAdmin role
    mockUseOrganization.mockReturnValue({
      currentOrg: {
        id: 'org-1',
        name: 'Test Org',
        role: 'OrgAdmin',
      },
      memberships: [],
      isLoading: false,
      isPending: false,
    })
    // Default: all feature flags active (fully visible)
    mockUseFeatureFlags.mockReturnValue({
      flags: {},
      isVisible: () => true,
      isComingSoon: () => false,
      isLoading: false,
    })
  })

  afterEach(() => {
    vi.resetAllMocks()
  })

  // ===========================================================================
  // Role-Based Filtering Tests
  // ===========================================================================
  describe('Role-Based Filtering', () => {
    describe('Administrator role', () => {
      beforeEach(() => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.Administrator,
          systemRole: 'Admin',
          exerciseRole: null,
          isLoading: false,
        })
      })

      it('sees all 17 menu items', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(17)
      })

      it('sees all menu items in correct order', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.map(i => i.id)).toEqual([
          'my-assignments',
          'exercises',
          'reports',
          'org-details',
          'org-members',
          'org-approval',
          'org-capabilities',
          'org-suggestions',
          'org-archived',
          'org-settings',
          'admin',
          'templates',
          'users',
          'organizations',
          'delivery-methods',
          'feedback',
          'settings',
        ])
      })

      it('sees Templates menu item', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.find(i => i.id === 'templates')).toBeDefined()
      })

      it('sees Users menu item', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.find(i => i.id === 'users')).toBeDefined()
      })

      it('sees all four sections', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual([
          'conduct',
          'analysis',
          'organization',
          'system',
        ])
      })
    })

    describe('Exercise Director role', () => {
      beforeEach(() => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.ExerciseDirector,
          systemRole: 'Manager',
          exerciseRole: HseepRole.ExerciseDirector,
          isLoading: false,
        })
      })

      it('sees 11 menu items (conduct+analysis+org+settings)', () => {
        // ExerciseDirector w/OrgAdmin: 2 conduct + 1 analysis + 7 org + 1 settings = 11
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(11)
      })

      it('does NOT see Templates', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.find(i => i.id === 'templates')).toBeUndefined()
      })

      it('does NOT see Users', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.find(i => i.id === 'users')).toBeUndefined()
      })

      it('sees Reports', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.find(i => i.id === 'reports')).toBeDefined()
      })

      it('sees all four sections', () => {
        // ExerciseDirector+OrgAdmin: conduct, analysis, org, system
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual([
          'conduct',
          'analysis',
          'organization',
          'system',
        ])
      })
    })

    describe('Controller role', () => {
      beforeEach(() => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.Controller,
          systemRole: 'User',
          exerciseRole: HseepRole.Controller,
          isLoading: false,
        })
      })

      it('sees 10 menu items (conduct + org items + settings)', () => {
        // Controller with OrgAdmin role sees: 2 conduct + 7 org + 1 settings = 10
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(10)
      })

      it('sees My Assignments', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'my-assignments')).toBeDefined()
      })

      it('sees Exercises', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'exercises')).toBeDefined()
      })

      it('sees Settings', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'settings')).toBeDefined()
      })

      it('does NOT see Reports', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'reports')).toBeUndefined()
      })

      it('does NOT see Templates', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'templates')).toBeUndefined()
      })

      it('does NOT see Users', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'users')).toBeUndefined()
      })

      it('sees conduct, organization, and system sections (no analysis)', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual(['conduct', 'organization', 'system'])
        expect(result.current.groupedBySection.analysis).toHaveLength(0)
      })

    })

    describe('Evaluator role', () => {
      beforeEach(() => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.Evaluator,
          systemRole: 'User',
          exerciseRole: HseepRole.Evaluator,
          isLoading: false,
        })
      })

      it('sees 10 menu items (conduct + org items + settings)', () => {
        // Evaluator with OrgAdmin role sees: 2 conduct + 7 org + 1 settings = 10
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(10)
      })

      it('sees My Assignments', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'my-assignments')).toBeDefined()
      })

      it('sees Exercises', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'exercises')).toBeDefined()
      })

      it('sees Settings', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'settings')).toBeDefined()
      })

      it('does NOT see Reports', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'reports')).toBeUndefined()
      })

      it('sees CONDUCT, ORGANIZATION, and SYSTEM sections (no analysis)', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual([
          'conduct',
          'organization',
          'system',
        ])
        expect(result.current.groupedBySection.analysis).toHaveLength(0)
      })
    })

    describe('Observer role', () => {
      beforeEach(() => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.Observer,
          systemRole: 'User',
          exerciseRole: HseepRole.Observer,
          isLoading: false,
        })
      })

      it('sees 10 menu items (conduct + org items + settings)', () => {
        // Observer with OrgAdmin role sees: 2 conduct + 7 org + 1 settings = 10
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(10)
      })

      it('sees My Assignments', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'my-assignments')).toBeDefined()
      })

      it('sees Exercises', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'exercises')).toBeDefined()
      })

      it('sees Settings', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'settings')).toBeDefined()
      })

      it('does NOT see Reports', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'reports')).toBeUndefined()
      })

      it('does NOT see Templates', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'templates')).toBeUndefined()
      })

      it('does NOT see Users', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'users')).toBeUndefined()
      })

      it('sees CONDUCT, ORGANIZATION, and SYSTEM sections (no analysis)', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual([
          'conduct',
          'organization',
          'system',
        ])
        expect(result.current.groupedBySection.analysis).toHaveLength(0)
      })

      it('sees CONDUCT, ORGANIZATION, and SYSTEM sections (no analysis)', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual([
          'conduct',
          'organization',
          'system',
        ])
      })
    })
  })

  // ===========================================================================
  // Disabled State Tests (Context Required)
  // ===========================================================================
  describe('Disabled State (Context Required)', () => {
    describe('without exercise context', () => {
      beforeEach(() => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.Administrator,
          systemRole: 'Admin',
          exerciseRole: null,
          isLoading: false,
        })
      })

      it('My Assignments is NOT disabled (no exercise context needed)', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.isItemDisabled('my-assignments')).toBe(false)
      })

      it('Exercises is NOT disabled (no exercise context needed)', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.isItemDisabled('exercises')).toBe(false)
      })

      it('Reports is NOT disabled (no exercise context needed)', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.isItemDisabled('reports')).toBe(false)
      })

      it('Templates is NOT disabled (no exercise context needed)', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.isItemDisabled('templates')).toBe(false)
      })

      it('Users is NOT disabled (no exercise context needed)', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.isItemDisabled('users')).toBe(false)
      })

      it('Settings is NOT disabled (no exercise context needed)', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.isItemDisabled('settings')).toBe(false)
      })
    })

    describe('with exercise context', () => {
      beforeEach(() => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.Administrator,
          systemRole: 'Admin',
          exerciseRole: HseepRole.ExerciseDirector,
          isLoading: false,
        })
      })

      it('all items are enabled when in exercise context', () => {
        const { result } = renderHook(() =>
          useFilteredMenu({ exerciseId: 'exercise-123' }),
        )

        result.current.filteredItems.forEach(item => {
          expect(result.current.isItemDisabled(item.id)).toBe(false)
        })
      })
    })

    describe('disabled state edge cases', () => {
      beforeEach(() => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.Administrator,
          systemRole: 'Admin',
          exerciseRole: null,
          isLoading: false,
        })
      })

      it('isItemDisabled returns false for non-existent item', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.isItemDisabled('non-existent-item')).toBe(false)
      })

      it('getDisabledTooltip returns undefined for non-existent item', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.getDisabledTooltip('non-existent-item')).toBeUndefined()
      })

      it('getDisabledTooltip returns undefined for enabled items', () => {
        const { result } = renderHook(() =>
          useFilteredMenu({ exerciseId: 'exercise-123' }),
        )

        expect(result.current.getDisabledTooltip('my-assignments')).toBeUndefined()
      })

      it('handles empty string exerciseId as no context', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: '' }))

        // Empty string is falsy - all main menu items are enabled regardless
        expect(result.current.isItemDisabled('my-assignments')).toBe(false)
      })
    })
  })

  // ===========================================================================
  // Section Grouping Tests
  // ===========================================================================
  describe('Section Grouping', () => {
    beforeEach(() => {
      mockUseExerciseRole.mockReturnValue({
        effectiveRole: HseepRole.Administrator,
        systemRole: 'Admin',
        exerciseRole: null,
        isLoading: false,
      })
    })

    it('groups CONDUCT section items correctly', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.conduct.map(i => i.id)).toEqual([
        'my-assignments',
        'exercises',
      ])
    })

    it('groups ANALYSIS section items correctly', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.analysis.map(i => i.id)).toEqual([
        'reports',
      ])
    })

    it('groups SYSTEM section items correctly', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.system.map(i => i.id)).toEqual([
        'admin',
        'templates',
        'users',
        'organizations',
        'delivery-methods',
        'feedback',
        'settings',
      ])
    })

    it('CONDUCT section has 2 items for Admin', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.conduct).toHaveLength(2)
    })

    it('ANALYSIS section has 1 item for Admin', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.analysis).toHaveLength(1)
    })

    it('ORGANIZATION section has 7 items for Admin', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.organization).toHaveLength(7)
    })

    it('SYSTEM section has 7 items for Admin', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.system).toHaveLength(7)
    })

    it('returns sections in correct order (conduct, analysis, organization, system)', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.visibleSections).toEqual([
        'conduct',
        'analysis',
        'organization',
        'system',
      ])
    })
  })

  // ===========================================================================
  // Empty Sections Tests
  // ===========================================================================
  describe('Empty Sections', () => {
    it('hides ANALYSIS section when Observer has no permitted items', () => {
      mockUseExerciseRole.mockReturnValue({
        effectiveRole: HseepRole.Observer,
        systemRole: 'User',
        exerciseRole: HseepRole.Observer,
        isLoading: false,
      })

      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.analysis).toHaveLength(0)
      expect(result.current.visibleSections).not.toContain('analysis')
    })

    it('hides ANALYSIS section when Controller has no permitted items', () => {
      mockUseExerciseRole.mockReturnValue({
        effectiveRole: HseepRole.Controller,
        systemRole: 'User',
        exerciseRole: HseepRole.Controller,
        isLoading: false,
      })

      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.analysis).toHaveLength(0)
      expect(result.current.visibleSections).not.toContain('analysis')
    })

    it('hides ANALYSIS section when Evaluator has no permitted items', () => {
      mockUseExerciseRole.mockReturnValue({
        effectiveRole: HseepRole.Evaluator,
        systemRole: 'User',
        exerciseRole: HseepRole.Evaluator,
        isLoading: false,
      })

      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.analysis).toHaveLength(0)
      expect(result.current.visibleSections).not.toContain('analysis')
    })
  })

  // ===========================================================================
  // Authentication Tests
  // ===========================================================================
  describe('Unauthenticated User', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({ user: null })
      mockUseExerciseRole.mockReturnValue({
        effectiveRole: HseepRole.Observer,
        systemRole: null,
        exerciseRole: null,
        isLoading: false,
      })
    })

    it('returns empty items when user is not authenticated', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.filteredItems).toHaveLength(0)
    })

    it('returns no visible sections when user is not authenticated', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.visibleSections).toHaveLength(0)
    })

    it('returns empty grouped sections when user is not authenticated', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.conduct).toHaveLength(0)
      expect(result.current.groupedBySection.analysis).toHaveLength(0)
      expect(result.current.groupedBySection.system).toHaveLength(0)
    })
  })

  // ===========================================================================
  // System Role Integration Tests
  // ===========================================================================
  describe('System Role Integration', () => {
    describe('Admin system role with various exercise roles', () => {
      it('Admin with no exercise role sees Templates and Users', () => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.Administrator,
          systemRole: 'Admin',
          exerciseRole: null,
          isLoading: false,
        })

        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.find(i => i.id === 'templates')).toBeDefined()
        expect(result.current.filteredItems.find(i => i.id === 'users')).toBeDefined()
      })
    })

    describe('Manager system role', () => {
      it('Manager mapped to ExerciseDirector does NOT see Templates', () => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.ExerciseDirector,
          systemRole: 'Manager',
          exerciseRole: null,
          isLoading: false,
        })

        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.find(i => i.id === 'templates')).toBeUndefined()
      })

      it('Manager mapped to ExerciseDirector does NOT see Users', () => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.ExerciseDirector,
          systemRole: 'Manager',
          exerciseRole: null,
          isLoading: false,
        })

        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.find(i => i.id === 'users')).toBeUndefined()
      })
    })

    describe('User system role', () => {
      it('User mapped to Observer with OrgAdmin role sees org items', () => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.Observer,
          systemRole: 'User',
          exerciseRole: null,
          isLoading: false,
        })

        const { result } = renderHook(() => useFilteredMenu())

        // Observer with OrgAdmin: 2 conduct + 7 org + 1 settings = 10
        expect(result.current.filteredItems).toHaveLength(10)
      })
    })
  })

  // ===========================================================================
  // Menu Item Properties Tests
  // ===========================================================================
  describe('Menu Item Properties', () => {
    beforeEach(() => {
      mockUseExerciseRole.mockReturnValue({
        effectiveRole: HseepRole.Administrator,
        systemRole: 'Admin',
        exerciseRole: null,
        isLoading: false,
      })
    })

    it('each item has required properties', () => {
      const { result } = renderHook(() => useFilteredMenu())

      result.current.filteredItems.forEach(item => {
        expect(item).toHaveProperty('id')
        expect(item).toHaveProperty('label')
        expect(item).toHaveProperty('icon')
        expect(item).toHaveProperty('path')
        expect(item).toHaveProperty('section')
        expect(item).toHaveProperty('allowedRoles')
      })
    })

    it('My Assignments has correct path', () => {
      const { result } = renderHook(() => useFilteredMenu())

      const item = result.current.filteredItems.find(i => i.id === 'my-assignments')
      expect(item?.path).toBe('/assignments')
    })

    it('Exercises has correct path', () => {
      const { result } = renderHook(() => useFilteredMenu())

      const item = result.current.filteredItems.find(i => i.id === 'exercises')
      expect(item?.path).toBe('/exercises')
    })

    it('Reports has correct path', () => {
      const { result } = renderHook(() => useFilteredMenu())

      const item = result.current.filteredItems.find(i => i.id === 'reports')
      expect(item?.path).toBe('/reports')
    })

    it('Templates has correct path', () => {
      const { result } = renderHook(() => useFilteredMenu())

      const item = result.current.filteredItems.find(i => i.id === 'templates')
      expect(item?.path).toBe('/templates')
    })

    it('Users has correct path', () => {
      const { result } = renderHook(() => useFilteredMenu())

      const item = result.current.filteredItems.find(i => i.id === 'users')
      expect(item?.path).toBe('/admin/users')
    })

    it('Settings has correct path', () => {
      const { result } = renderHook(() => useFilteredMenu())

      const item = result.current.filteredItems.find(i => i.id === 'settings')
      expect(item?.path).toBe('/settings')
    })
  })

  // ===========================================================================
  // Hook Options Tests
  // ===========================================================================
  describe('Hook Options', () => {
    beforeEach(() => {
      mockUseExerciseRole.mockReturnValue({
        effectiveRole: HseepRole.Administrator,
        systemRole: 'Admin',
        exerciseRole: null,
        isLoading: false,
      })
    })

    it('works with empty options object', () => {
      const { result } = renderHook(() => useFilteredMenu({}))

      expect(result.current.filteredItems).toHaveLength(17)
    })

    it('works with no options (undefined)', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.filteredItems).toHaveLength(17)
    })

    it('works with null exerciseId', () => {
      const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

      expect(result.current.filteredItems).toHaveLength(17)
    })

    it('works with undefined exerciseId', () => {
      const { result } = renderHook(() => useFilteredMenu({ exerciseId: undefined }))

      expect(result.current.filteredItems).toHaveLength(17)
    })

    it('works with valid exerciseId', () => {
      const { result } = renderHook(() => useFilteredMenu({ exerciseId: 'valid-id' }))

      expect(result.current.filteredItems).toHaveLength(17)
    })

    it('works with UUID exerciseId', () => {
      const { result } = renderHook(() =>
        useFilteredMenu({ exerciseId: '550e8400-e29b-41d4-a716-446655440000' }),
      )

      expect(result.current.filteredItems).toHaveLength(17)
    })
  })

  // ===========================================================================
  // Role Permission Matrix Verification
  // ===========================================================================
  describe('Role Permission Matrix (Complete Verification)', () => {
    const testRolePermissions = (
      role: string,
      expectedItems: string[],
      notExpectedItems: string[],
    ) => {
      describe(`${role} permissions`, () => {
        beforeEach(() => {
          mockUseExerciseRole.mockReturnValue({
            effectiveRole: role as HseepRole,
            systemRole: role === HseepRole.Administrator ? 'Admin' : 'User',
            exerciseRole: role as HseepRole,
            isLoading: false,
          })
        })

        expectedItems.forEach(itemId => {
          it(`CAN see ${itemId}`, () => {
            const { result } = renderHook(() => useFilteredMenu())
            expect(result.current.filteredItems.find(i => i.id === itemId)).toBeDefined()
          })
        })

        notExpectedItems.forEach(itemId => {
          it(`CANNOT see ${itemId}`, () => {
            const { result } = renderHook(() => useFilteredMenu())
            expect(result.current.filteredItems.find(i => i.id === itemId)).toBeUndefined()
          })
        })
      })
    }

    testRolePermissions(
      HseepRole.Administrator,
      ['my-assignments', 'exercises', 'reports', 'admin', 'templates', 'users', 'settings'],
      [],
    )

    testRolePermissions(
      HseepRole.ExerciseDirector,
      ['my-assignments', 'exercises', 'reports', 'settings'],
      ['admin', 'templates', 'users'],
    )

    testRolePermissions(
      HseepRole.Controller,
      ['my-assignments', 'exercises', 'settings'],
      ['reports', 'admin', 'templates', 'users'],
    )

    testRolePermissions(
      HseepRole.Evaluator,
      ['my-assignments', 'exercises', 'settings'],
      ['reports', 'admin', 'templates', 'users'],
    )

    testRolePermissions(
      HseepRole.Observer,
      ['my-assignments', 'exercises', 'settings'],
      ['reports', 'admin', 'templates', 'users'],
    )
  })
})
