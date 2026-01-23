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
import { HseepRole, SystemRole } from '@/types'

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

describe('useFilteredMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Default: authenticated admin user
    mockUseAuth.mockReturnValue({
      user: { id: 'user-1', email: 'admin@example.com', role: 'Admin' },
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

      it('sees all 9 menu items', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(9)
      })

      it('sees all menu items in correct order', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.map(i => i.id)).toEqual([
          'my-assignments',
          'exercises',
          'control-room',
          'inject-queue',
          'observations',
          'reports',
          'templates',
          'users',
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

      it('sees all three sections', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual(['conduct', 'analysis', 'system'])
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

      it('sees 7 menu items (all except Templates and Users)', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(7)
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

      it('sees Observations', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.find(i => i.id === 'observations')).toBeDefined()
      })

      it('sees Control Room', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems.find(i => i.id === 'control-room')).toBeDefined()
      })

      it('sees all three sections', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual(['conduct', 'analysis', 'system'])
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

      it('sees exactly 5 menu items', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(5)
      })

      it('sees My Assignments', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'my-assignments')).toBeDefined()
      })

      it('sees Exercises', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'exercises')).toBeDefined()
      })

      it('sees Control Room', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'control-room')).toBeDefined()
      })

      it('sees Inject Queue', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'inject-queue')).toBeDefined()
      })

      it('sees Settings', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'settings')).toBeDefined()
      })

      it('does NOT see Observations', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'observations')).toBeUndefined()
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

      it('does NOT see ANALYSIS section (no visible items)', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).not.toContain('analysis')
        expect(result.current.groupedBySection.analysis).toHaveLength(0)
      })

      it('sees CONDUCT and SYSTEM sections only', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual(['conduct', 'system'])
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

      it('sees exactly 4 menu items', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(4)
      })

      it('sees My Assignments', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'my-assignments')).toBeDefined()
      })

      it('sees Exercises', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'exercises')).toBeDefined()
      })

      it('sees Observations', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'observations')).toBeDefined()
      })

      it('sees Settings', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'settings')).toBeDefined()
      })

      it('does NOT see Control Room', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'control-room')).toBeUndefined()
      })

      it('does NOT see Inject Queue', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'inject-queue')).toBeUndefined()
      })

      it('does NOT see Reports', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'reports')).toBeUndefined()
      })

      it('sees CONDUCT, ANALYSIS, and SYSTEM sections', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual(['conduct', 'analysis', 'system'])
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

      it('sees exactly 3 menu items (minimum set)', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(3)
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

      it('does NOT see Control Room', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'control-room')).toBeUndefined()
      })

      it('does NOT see Inject Queue', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'inject-queue')).toBeUndefined()
      })

      it('does NOT see Observations', () => {
        const { result } = renderHook(() => useFilteredMenu())
        expect(result.current.filteredItems.find(i => i.id === 'observations')).toBeUndefined()
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

      it('does NOT see ANALYSIS section (empty)', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).not.toContain('analysis')
        expect(result.current.groupedBySection.analysis).toHaveLength(0)
      })

      it('sees only CONDUCT and SYSTEM sections', () => {
        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.visibleSections).toEqual(['conduct', 'system'])
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

      it('Control Room is disabled', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.isItemDisabled('control-room')).toBe(true)
      })

      it('Control Room has correct disabled tooltip', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.getDisabledTooltip('control-room')).toBe('Enter an exercise first')
      })

      it('Inject Queue is disabled', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.isItemDisabled('inject-queue')).toBe(true)
      })

      it('Inject Queue has correct disabled tooltip', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.getDisabledTooltip('inject-queue')).toBe('Enter an exercise first')
      })

      it('Observations is disabled', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.isItemDisabled('observations')).toBe(true)
      })

      it('Observations has correct disabled tooltip', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

        expect(result.current.getDisabledTooltip('observations')).toBe('Enter an exercise first')
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

      it('Control Room is enabled', () => {
        const { result } = renderHook(() =>
          useFilteredMenu({ exerciseId: 'exercise-123' }),
        )

        expect(result.current.isItemDisabled('control-room')).toBe(false)
      })

      it('Control Room has no disabled tooltip', () => {
        const { result } = renderHook(() =>
          useFilteredMenu({ exerciseId: 'exercise-123' }),
        )

        expect(result.current.getDisabledTooltip('control-room')).toBeUndefined()
      })

      it('Inject Queue is enabled', () => {
        const { result } = renderHook(() =>
          useFilteredMenu({ exerciseId: 'exercise-123' }),
        )

        expect(result.current.isItemDisabled('inject-queue')).toBe(false)
      })

      it('Observations is enabled', () => {
        const { result } = renderHook(() =>
          useFilteredMenu({ exerciseId: 'exercise-123' }),
        )

        expect(result.current.isItemDisabled('observations')).toBe(false)
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

        expect(result.current.getDisabledTooltip('control-room')).toBeUndefined()
      })

      it('handles empty string exerciseId as no context', () => {
        const { result } = renderHook(() => useFilteredMenu({ exerciseId: '' }))

        // Empty string is falsy, so items requiring context should be disabled
        expect(result.current.isItemDisabled('control-room')).toBe(true)
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
        'control-room',
        'inject-queue',
      ])
    })

    it('groups ANALYSIS section items correctly', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.analysis.map(i => i.id)).toEqual([
        'observations',
        'reports',
      ])
    })

    it('groups SYSTEM section items correctly', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.system.map(i => i.id)).toEqual([
        'templates',
        'users',
        'settings',
      ])
    })

    it('CONDUCT section has 4 items for Admin', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.conduct).toHaveLength(4)
    })

    it('ANALYSIS section has 2 items for Admin', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.analysis).toHaveLength(2)
    })

    it('SYSTEM section has 3 items for Admin', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.system).toHaveLength(3)
    })

    it('returns sections in correct order (conduct, analysis, system)', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.visibleSections).toEqual(['conduct', 'analysis', 'system'])
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

    it('shows ANALYSIS section when Evaluator has Observations', () => {
      mockUseExerciseRole.mockReturnValue({
        effectiveRole: HseepRole.Evaluator,
        systemRole: 'User',
        exerciseRole: HseepRole.Evaluator,
        isLoading: false,
      })

      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.groupedBySection.analysis.length).toBeGreaterThan(0)
      expect(result.current.visibleSections).toContain('analysis')
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
      it('User mapped to Observer sees minimal menu', () => {
        mockUseExerciseRole.mockReturnValue({
          effectiveRole: HseepRole.Observer,
          systemRole: 'User',
          exerciseRole: null,
          isLoading: false,
        })

        const { result } = renderHook(() => useFilteredMenu())

        expect(result.current.filteredItems).toHaveLength(3)
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

    it('Control Room has parameterized path', () => {
      const { result } = renderHook(() => useFilteredMenu())

      const item = result.current.filteredItems.find(i => i.id === 'control-room')
      expect(item?.path).toBe('/exercises/:id/control')
    })

    it('Inject Queue has parameterized path', () => {
      const { result } = renderHook(() => useFilteredMenu())

      const item = result.current.filteredItems.find(i => i.id === 'inject-queue')
      expect(item?.path).toBe('/exercises/:id/queue')
    })

    it('Observations has parameterized path', () => {
      const { result } = renderHook(() => useFilteredMenu())

      const item = result.current.filteredItems.find(i => i.id === 'observations')
      expect(item?.path).toBe('/exercises/:id/observations')
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
      expect(item?.path).toBe('/users')
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

      expect(result.current.filteredItems).toHaveLength(9)
    })

    it('works with no options (undefined)', () => {
      const { result } = renderHook(() => useFilteredMenu())

      expect(result.current.filteredItems).toHaveLength(9)
    })

    it('works with null exerciseId', () => {
      const { result } = renderHook(() => useFilteredMenu({ exerciseId: null }))

      expect(result.current.filteredItems).toHaveLength(9)
      expect(result.current.isItemDisabled('control-room')).toBe(true)
    })

    it('works with undefined exerciseId', () => {
      const { result } = renderHook(() => useFilteredMenu({ exerciseId: undefined }))

      expect(result.current.filteredItems).toHaveLength(9)
      expect(result.current.isItemDisabled('control-room')).toBe(true)
    })

    it('works with valid exerciseId', () => {
      const { result } = renderHook(() => useFilteredMenu({ exerciseId: 'valid-id' }))

      expect(result.current.filteredItems).toHaveLength(9)
      expect(result.current.isItemDisabled('control-room')).toBe(false)
    })

    it('works with UUID exerciseId', () => {
      const { result } = renderHook(() =>
        useFilteredMenu({ exerciseId: '550e8400-e29b-41d4-a716-446655440000' }),
      )

      expect(result.current.filteredItems).toHaveLength(9)
      expect(result.current.isItemDisabled('control-room')).toBe(false)
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
      ['my-assignments', 'exercises', 'control-room', 'inject-queue', 'observations', 'reports', 'templates', 'users', 'settings'],
      [],
    )

    testRolePermissions(
      HseepRole.ExerciseDirector,
      ['my-assignments', 'exercises', 'control-room', 'inject-queue', 'observations', 'reports', 'settings'],
      ['templates', 'users'],
    )

    testRolePermissions(
      HseepRole.Controller,
      ['my-assignments', 'exercises', 'control-room', 'inject-queue', 'settings'],
      ['observations', 'reports', 'templates', 'users'],
    )

    testRolePermissions(
      HseepRole.Evaluator,
      ['my-assignments', 'exercises', 'observations', 'settings'],
      ['control-room', 'inject-queue', 'reports', 'templates', 'users'],
    )

    testRolePermissions(
      HseepRole.Observer,
      ['my-assignments', 'exercises', 'settings'],
      ['control-room', 'inject-queue', 'observations', 'reports', 'templates', 'users'],
    )
  })
})
