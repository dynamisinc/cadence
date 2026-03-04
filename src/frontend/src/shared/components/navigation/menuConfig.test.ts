/**
 * Menu Configuration Tests
 *
 * Tests for the menu configuration including:
 * - Menu item structure
 * - Role assignments
 * - Path definitions
 * - Section assignments
 * - Helper functions
 */

import { describe, it, expect } from 'vitest'
import { HseepRole, SystemRole } from '../../../types'
import { MENU_ITEMS, getMenuItemById, getMenuItemsBySection } from './menuConfig'
import { MENU_SECTION_LABELS } from './types'

describe('menuConfig', () => {
  // ===========================================================================
  // Menu Items Structure Tests
  // ===========================================================================
  describe('MENU_ITEMS structure', () => {
    it('has exactly 17 menu items', () => {
      expect(MENU_ITEMS).toHaveLength(17)
    })

    it('all items have required properties', () => {
      MENU_ITEMS.forEach(item => {
        expect(item).toHaveProperty('id')
        expect(item).toHaveProperty('label')
        expect(item).toHaveProperty('icon')
        expect(item).toHaveProperty('path')
        expect(item).toHaveProperty('section')
        expect(item).toHaveProperty('allowedRoles')

        expect(typeof item.id).toBe('string')
        expect(typeof item.label).toBe('string')
        expect(typeof item.path).toBe('string')
        expect(typeof item.section).toBe('string')
        expect(Array.isArray(item.allowedRoles)).toBe(true)
      })
    })

    it('all items have unique IDs', () => {
      const ids = MENU_ITEMS.map(item => item.id)
      const uniqueIds = new Set(ids)
      expect(uniqueIds.size).toBe(ids.length)
    })

    it('all items have non-empty labels', () => {
      MENU_ITEMS.forEach(item => {
        expect(item.label.length).toBeGreaterThan(0)
      })
    })

    it('all items have valid sections', () => {
      const validSections = ['conduct', 'analysis', 'organization', 'system']
      MENU_ITEMS.forEach(item => {
        expect(validSections).toContain(item.section)
      })
    })

    it('all items have valid paths starting with /', () => {
      MENU_ITEMS.forEach(item => {
        expect(item.path.startsWith('/')).toBe(true)
      })
    })
  })

  // ===========================================================================
  // Section Distribution Tests
  // ===========================================================================
  describe('Section distribution', () => {
    it('CONDUCT section has 2 items', () => {
      const conductItems = MENU_ITEMS.filter(item => item.section === 'conduct')
      expect(conductItems).toHaveLength(2)
    })

    it('ANALYSIS section has 1 item', () => {
      const analysisItems = MENU_ITEMS.filter(item => item.section === 'analysis')
      expect(analysisItems).toHaveLength(1)
    })

    it('ORGANIZATION section has 7 items', () => {
      const orgItems = MENU_ITEMS.filter(item => item.section === 'organization')
      expect(orgItems).toHaveLength(7)
    })

    it('SYSTEM section has 7 items', () => {
      const systemItems = MENU_ITEMS.filter(item => item.section === 'system')
      expect(systemItems).toHaveLength(7)
    })

    it('CONDUCT section contains correct items', () => {
      const conductItems = MENU_ITEMS.filter(item => item.section === 'conduct')
      const ids = conductItems.map(item => item.id)
      expect(ids).toContain('my-assignments')
      expect(ids).toContain('exercises')
    })

    it('ANALYSIS section contains correct items', () => {
      const analysisItems = MENU_ITEMS.filter(item => item.section === 'analysis')
      const ids = analysisItems.map(item => item.id)
      expect(ids).toContain('reports')
    })

    it('SYSTEM section contains correct items', () => {
      const systemItems = MENU_ITEMS.filter(item => item.section === 'system')
      const ids = systemItems.map(item => item.id)
      expect(ids).toContain('admin')
      expect(ids).toContain('templates')
      expect(ids).toContain('users')
      expect(ids).toContain('organizations')
      expect(ids).toContain('delivery-methods')
      expect(ids).toContain('settings')
    })
  })

  // ===========================================================================
  // Individual Menu Item Tests
  // ===========================================================================
  describe('Individual menu items', () => {
    describe('My Assignments', () => {
      const item = MENU_ITEMS.find(i => i.id === 'my-assignments')!

      it('exists', () => {
        expect(item).toBeDefined()
      })

      it('has correct path', () => {
        expect(item.path).toBe('/assignments')
      })

      it('is in CONDUCT section', () => {
        expect(item.section).toBe('conduct')
      })

      it('is visible to all roles', () => {
        expect(item.allowedRoles).toContain(HseepRole.Administrator)
        expect(item.allowedRoles).toContain(HseepRole.ExerciseDirector)
        expect(item.allowedRoles).toContain(HseepRole.Controller)
        expect(item.allowedRoles).toContain(HseepRole.Evaluator)
        expect(item.allowedRoles).toContain(HseepRole.Observer)
      })

      it('does not require exercise context', () => {
        expect(item.requiresExerciseContext).toBeFalsy()
      })
    })

    describe('Exercises', () => {
      const item = MENU_ITEMS.find(i => i.id === 'exercises')!

      it('exists', () => {
        expect(item).toBeDefined()
      })

      it('has correct path', () => {
        expect(item.path).toBe('/exercises')
      })

      it('is in CONDUCT section', () => {
        expect(item.section).toBe('conduct')
      })

      it('is visible to all roles', () => {
        expect(item.allowedRoles).toHaveLength(5)
      })

      it('does not require exercise context', () => {
        expect(item.requiresExerciseContext).toBeFalsy()
      })
    })

    describe('Reports', () => {
      const item = MENU_ITEMS.find(i => i.id === 'reports')!

      it('exists', () => {
        expect(item).toBeDefined()
      })

      it('has correct path', () => {
        expect(item.path).toBe('/reports')
      })

      it('is in ANALYSIS section', () => {
        expect(item.section).toBe('analysis')
      })

      it('is visible to Admin and Director only', () => {
        expect(item.allowedRoles).toContain(HseepRole.Administrator)
        expect(item.allowedRoles).toContain(HseepRole.ExerciseDirector)
        expect(item.allowedRoles).not.toContain(HseepRole.Controller)
        expect(item.allowedRoles).not.toContain(HseepRole.Evaluator)
        expect(item.allowedRoles).not.toContain(HseepRole.Observer)
      })

      it('does not require exercise context', () => {
        expect(item.requiresExerciseContext).toBeFalsy()
      })
    })

    describe('System Settings', () => {
      const item = MENU_ITEMS.find(i => i.id === 'admin')!

      it('exists', () => {
        expect(item).toBeDefined()
      })

      it('has correct path', () => {
        expect(item.path).toBe('/admin')
      })

      it('is in SYSTEM section', () => {
        expect(item.section).toBe('system')
      })

      it('is visible to Admin only', () => {
        expect(item.allowedRoles).toContain(HseepRole.Administrator)
        expect(item.allowedRoles).toHaveLength(1)
      })

      it('requires Admin system role', () => {
        expect(item.allowedSystemRoles).toContain(SystemRole.Admin)
      })

      it('does not require exercise context', () => {
        expect(item.requiresExerciseContext).toBeFalsy()
      })
    })

    describe('Templates', () => {
      const item = MENU_ITEMS.find(i => i.id === 'templates')!

      it('exists', () => {
        expect(item).toBeDefined()
      })

      it('has correct path', () => {
        expect(item.path).toBe('/templates')
      })

      it('is in SYSTEM section', () => {
        expect(item.section).toBe('system')
      })

      it('is visible to Admin only', () => {
        expect(item.allowedRoles).toContain(HseepRole.Administrator)
        expect(item.allowedRoles).toHaveLength(1)
      })

      it('requires Admin system role', () => {
        expect(item.allowedSystemRoles).toContain(SystemRole.Admin)
      })

      it('does not require exercise context', () => {
        expect(item.requiresExerciseContext).toBeFalsy()
      })
    })

    describe('Users', () => {
      const item = MENU_ITEMS.find(i => i.id === 'users')!

      it('exists', () => {
        expect(item).toBeDefined()
      })

      it('has correct path', () => {
        expect(item.path).toBe('/admin/users')
      })

      it('is in SYSTEM section', () => {
        expect(item.section).toBe('system')
      })

      it('is visible to Admin only', () => {
        expect(item.allowedRoles).toContain(HseepRole.Administrator)
        expect(item.allowedRoles).toHaveLength(1)
      })

      it('requires Admin system role', () => {
        expect(item.allowedSystemRoles).toContain(SystemRole.Admin)
      })

      it('does not require exercise context', () => {
        expect(item.requiresExerciseContext).toBeFalsy()
      })
    })

    describe('Organizations', () => {
      const item = MENU_ITEMS.find(i => i.id === 'organizations')!

      it('exists', () => {
        expect(item).toBeDefined()
      })

      it('has correct path', () => {
        expect(item.path).toBe('/admin/organizations')
      })

      it('is in SYSTEM section', () => {
        expect(item.section).toBe('system')
      })

      it('is visible to Admin only', () => {
        expect(item.allowedRoles).toContain(HseepRole.Administrator)
        expect(item.allowedRoles).toHaveLength(1)
      })

      it('requires Admin system role', () => {
        expect(item.allowedSystemRoles).toContain(SystemRole.Admin)
      })

      it('does not require exercise context', () => {
        expect(item.requiresExerciseContext).toBeFalsy()
      })
    })

    describe('My Preferences', () => {
      const item = MENU_ITEMS.find(i => i.id === 'settings')!

      it('exists', () => {
        expect(item).toBeDefined()
      })

      it('has correct path', () => {
        expect(item.path).toBe('/settings')
      })

      it('is in SYSTEM section', () => {
        expect(item.section).toBe('system')
      })

      it('is visible to all roles', () => {
        expect(item.allowedRoles).toHaveLength(5)
      })

      it('does not require exercise context', () => {
        expect(item.requiresExerciseContext).toBeFalsy()
      })
    })
  })

  // ===========================================================================
  // Role Permission Tests
  // ===========================================================================
  describe('Role permissions', () => {
    describe('Administrator', () => {
      it('can see all 17 items', () => {
        const visibleItems = MENU_ITEMS.filter(
          item => item.allowedRoles.includes(HseepRole.Administrator),
        )
        expect(visibleItems).toHaveLength(17)
      })
    })

    describe('ExerciseDirector', () => {
      it('can see 11 items (all except Admin, Templates, Users, Organizations, Delivery Methods)', () => {
        const visibleItems = MENU_ITEMS.filter(
          item => item.allowedRoles.includes(HseepRole.ExerciseDirector),
        )
        expect(visibleItems).toHaveLength(11)
      })

      it('cannot see Admin', () => {
        const admin = MENU_ITEMS.find(i => i.id === 'admin')!
        expect(admin.allowedRoles).not.toContain(HseepRole.ExerciseDirector)
      })

      it('cannot see Templates', () => {
        const templates = MENU_ITEMS.find(i => i.id === 'templates')!
        expect(templates.allowedRoles).not.toContain(HseepRole.ExerciseDirector)
      })

      it('cannot see Users', () => {
        const users = MENU_ITEMS.find(i => i.id === 'users')!
        expect(users.allowedRoles).not.toContain(HseepRole.ExerciseDirector)
      })
    })

    describe('Controller', () => {
      it('can see 10 items (conduct + org + settings)', () => {
        const visibleItems = MENU_ITEMS.filter(
          item => item.allowedRoles.includes(HseepRole.Controller),
        )
        expect(visibleItems).toHaveLength(10)
      })

      it('can see My Assignments, Exercises, Settings', () => {
        const visibleItems = MENU_ITEMS.filter(
          item => item.allowedRoles.includes(HseepRole.Controller),
        )
        const ids = visibleItems.map(item => item.id)
        expect(ids).toContain('my-assignments')
        expect(ids).toContain('exercises')
        expect(ids).toContain('settings')
      })

      it('cannot see Reports', () => {
        const reports = MENU_ITEMS.find(i => i.id === 'reports')!
        expect(reports.allowedRoles).not.toContain(HseepRole.Controller)
      })
    })

    describe('Evaluator', () => {
      it('can see 10 items (conduct + org + settings)', () => {
        const visibleItems = MENU_ITEMS.filter(
          item => item.allowedRoles.includes(HseepRole.Evaluator),
        )
        expect(visibleItems).toHaveLength(10)
      })

      it('can see My Assignments, Exercises, Settings', () => {
        const visibleItems = MENU_ITEMS.filter(
          item => item.allowedRoles.includes(HseepRole.Evaluator),
        )
        const ids = visibleItems.map(item => item.id)
        expect(ids).toContain('my-assignments')
        expect(ids).toContain('exercises')
        expect(ids).toContain('settings')
      })

    })

    describe('Observer', () => {
      it('can see 10 items (conduct + org + settings)', () => {
        const visibleItems = MENU_ITEMS.filter(
          item => item.allowedRoles.includes(HseepRole.Observer),
        )
        expect(visibleItems).toHaveLength(10)
      })
    })
  })

  // ===========================================================================
  // Exercise Context Requirements Tests
  // ===========================================================================
  describe('Exercise context requirements', () => {
    it('no items require exercise context (exercise-scoped items are in exerciseMenuConfig)', () => {
      const contextRequired = MENU_ITEMS.filter(item => item.requiresExerciseContext)
      expect(contextRequired).toHaveLength(0)
    })
  })

  // ===========================================================================
  // Helper Functions Tests
  // ===========================================================================
  describe('Helper functions', () => {
    describe('getMenuItemById', () => {
      it('returns item for valid ID', () => {
        const item = getMenuItemById('exercises')
        expect(item).toBeDefined()
        expect(item?.id).toBe('exercises')
      })

      it('returns undefined for invalid ID', () => {
        const item = getMenuItemById('non-existent')
        expect(item).toBeUndefined()
      })

      it('returns correct item for each ID', () => {
        const ids = MENU_ITEMS.map(item => item.id)
        ids.forEach(id => {
          const item = getMenuItemById(id)
          expect(item).toBeDefined()
          expect(item?.id).toBe(id)
        })
      })
    })

    describe('getMenuItemsBySection', () => {
      it('returns correct items for CONDUCT section', () => {
        const items = getMenuItemsBySection('conduct')
        expect(items).toHaveLength(2)
        items.forEach(item => {
          expect(item.section).toBe('conduct')
        })
      })

      it('returns correct items for ANALYSIS section', () => {
        const items = getMenuItemsBySection('analysis')
        expect(items).toHaveLength(1)
        items.forEach(item => {
          expect(item.section).toBe('analysis')
        })
      })

      it('returns correct items for SYSTEM section', () => {
        const items = getMenuItemsBySection('system')
        expect(items).toHaveLength(7)
        items.forEach(item => {
          expect(item.section).toBe('system')
        })
      })
    })
  })

  // ===========================================================================
  // Section Labels Tests
  // ===========================================================================
  describe('MENU_SECTION_LABELS', () => {
    it('has all section labels', () => {
      expect(MENU_SECTION_LABELS).toHaveProperty('conduct')
      expect(MENU_SECTION_LABELS).toHaveProperty('analysis')
      expect(MENU_SECTION_LABELS).toHaveProperty('system')
    })

    it('labels are uppercase', () => {
      expect(MENU_SECTION_LABELS.conduct).toBe('CONDUCT')
      expect(MENU_SECTION_LABELS.analysis).toBe('ANALYSIS')
      expect(MENU_SECTION_LABELS.system).toBe('SYSTEM')
    })
  })

  // ===========================================================================
  // Menu Order Tests
  // ===========================================================================
  describe('Menu order', () => {
    it('items are in correct order by section', () => {
      // CONDUCT items first (0-1)
      expect(MENU_ITEMS[0].id).toBe('my-assignments')
      expect(MENU_ITEMS[1].id).toBe('exercises')

      // ANALYSIS items next (2)
      expect(MENU_ITEMS[2].id).toBe('reports')

      // ORGANIZATION items (3-9)
      expect(MENU_ITEMS[3].id).toBe('org-details')
      expect(MENU_ITEMS[4].id).toBe('org-members')
      expect(MENU_ITEMS[5].id).toBe('org-approval')
      expect(MENU_ITEMS[6].id).toBe('org-capabilities')
      expect(MENU_ITEMS[7].id).toBe('org-suggestions')
      expect(MENU_ITEMS[8].id).toBe('org-archived')
      expect(MENU_ITEMS[9].id).toBe('org-settings')

      // SYSTEM items last (10-16)
      expect(MENU_ITEMS[10].id).toBe('admin')
      expect(MENU_ITEMS[11].id).toBe('templates')
      expect(MENU_ITEMS[12].id).toBe('users')
      expect(MENU_ITEMS[13].id).toBe('organizations')
      expect(MENU_ITEMS[14].id).toBe('delivery-methods')
      expect(MENU_ITEMS[15].id).toBe('feedback')
      expect(MENU_ITEMS[16].id).toBe('settings')
    })
  })
})
