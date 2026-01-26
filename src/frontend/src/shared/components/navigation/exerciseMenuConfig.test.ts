/**
 * Exercise Menu Configuration Tests
 *
 * Tests for role-based menu filtering in exercise context.
 */

import { describe, it, expect } from 'vitest'
import {
  EXERCISE_MENU_ITEMS,
  getExerciseMenuItems,
  buildExerciseMenuPath,
} from './exerciseMenuConfig'
import { HseepRole } from '@/types'

describe('exerciseMenuConfig', () => {
  describe('EXERCISE_MENU_ITEMS', () => {
    it('contains expected menu items', () => {
      const itemIds = EXERCISE_MENU_ITEMS.map(item => item.id)

      expect(itemIds).toContain('hub')
      expect(itemIds).toContain('msel')
      expect(itemIds).toContain('inject-queue')
      expect(itemIds).toContain('observations')
      expect(itemIds).toContain('participants')
      expect(itemIds).toContain('metrics')
      expect(itemIds).toContain('settings')
    })

    it('has icons for all items', () => {
      EXERCISE_MENU_ITEMS.forEach(item => {
        expect(item.icon).toBeDefined()
      })
    })

    it('has paths for all items', () => {
      EXERCISE_MENU_ITEMS.forEach(item => {
        expect(item.path).toBeDefined()
      })
    })
  })

  describe('getExerciseMenuItems', () => {
    describe('Administrator role', () => {
      it('sees all menu items', () => {
        const items = getExerciseMenuItems(HseepRole.Administrator)
        const itemIds = items.map(item => item.id)

        expect(itemIds).toContain('hub')
        expect(itemIds).toContain('msel')
        expect(itemIds).toContain('inject-queue')
        expect(itemIds).toContain('observations')
        expect(itemIds).toContain('participants')
        expect(itemIds).toContain('metrics')
        expect(itemIds).toContain('settings')
        expect(items.length).toBe(7)
      })
    })

    describe('Exercise Director role', () => {
      it('sees all menu items', () => {
        const items = getExerciseMenuItems(HseepRole.ExerciseDirector)
        const itemIds = items.map(item => item.id)

        expect(itemIds).toContain('hub')
        expect(itemIds).toContain('msel')
        expect(itemIds).toContain('inject-queue')
        expect(itemIds).toContain('observations')
        expect(itemIds).toContain('participants')
        expect(itemIds).toContain('metrics')
        expect(itemIds).toContain('settings')
        expect(items.length).toBe(7)
      })
    })

    describe('Controller role', () => {
      it('sees Hub, MSEL, and Inject Queue', () => {
        const items = getExerciseMenuItems(HseepRole.Controller)
        const itemIds = items.map(item => item.id)

        expect(itemIds).toContain('hub')
        expect(itemIds).toContain('msel')
        expect(itemIds).toContain('inject-queue')
        expect(itemIds).not.toContain('observations')
        expect(itemIds).not.toContain('participants')
        expect(itemIds).not.toContain('metrics')
        expect(itemIds).not.toContain('settings')
        expect(items.length).toBe(3)
      })
    })

    describe('Evaluator role', () => {
      it('sees Hub and Observations', () => {
        const items = getExerciseMenuItems(HseepRole.Evaluator)
        const itemIds = items.map(item => item.id)

        expect(itemIds).toContain('hub')
        expect(itemIds).toContain('observations')
        expect(itemIds).not.toContain('msel')
        expect(itemIds).not.toContain('inject-queue')
        expect(itemIds).not.toContain('participants')
        expect(itemIds).not.toContain('metrics')
        expect(itemIds).not.toContain('settings')
        expect(items.length).toBe(2)
      })
    })

    describe('Observer role', () => {
      it('sees Hub and Observations', () => {
        const items = getExerciseMenuItems(HseepRole.Observer)
        const itemIds = items.map(item => item.id)

        expect(itemIds).toContain('hub')
        expect(itemIds).toContain('observations')
        expect(itemIds).not.toContain('msel')
        expect(itemIds).not.toContain('inject-queue')
        expect(items.length).toBe(2)
      })
    })
  })

  describe('buildExerciseMenuPath', () => {
    const exerciseId = 'abc-123'

    it('builds path for hub (empty path)', () => {
      const path = buildExerciseMenuPath(exerciseId, '')
      expect(path).toBe('/exercises/abc-123')
    })

    it('builds path for msel', () => {
      const path = buildExerciseMenuPath(exerciseId, 'msel')
      expect(path).toBe('/exercises/abc-123/msel')
    })

    it('builds path for conduct', () => {
      const path = buildExerciseMenuPath(exerciseId, 'conduct')
      expect(path).toBe('/exercises/abc-123/conduct')
    })

    it('builds path for observations', () => {
      const path = buildExerciseMenuPath(exerciseId, 'observations')
      expect(path).toBe('/exercises/abc-123/observations')
    })

    it('handles different exercise IDs', () => {
      const path1 = buildExerciseMenuPath('ex-1', 'msel')
      const path2 = buildExerciseMenuPath('ex-2', 'msel')

      expect(path1).toBe('/exercises/ex-1/msel')
      expect(path2).toBe('/exercises/ex-2/msel')
    })
  })
})
