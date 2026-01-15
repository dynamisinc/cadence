/**
 * Tests for Cadence Offline Database (db.ts)
 *
 * Uses fake-indexeddb to test IndexedDB operations.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import {
  db,
  clearExerciseCache,
  clearAllCache,
  getPendingActionCount,
  getPendingActions,
  addPendingAction,
  updatePendingActionStatus,
  incrementRetryCount,
  deletePendingAction,
  deleteFailedActions,
  pruneOldCache,
  getStorageEstimate,
} from './db'

describe('CadenceDatabase', () => {
  beforeEach(async () => {
    // Clear database before each test
    await clearAllCache()
  })

  afterEach(async () => {
    // Clean up after each test
    await clearAllCache()
  })

  // ============================================================================
  // clearExerciseCache Tests
  // ============================================================================

  describe('clearExerciseCache', () => {
    it('deletes exercise by id', async () => {
      const exerciseId = 'ex-123'
      await db.exercises.put({
        id: exerciseId,
        name: 'Test Exercise',
        exerciseType: 'TTX',
        status: 'Draft',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
      })

      await clearExerciseCache(exerciseId)

      const result = await db.exercises.get(exerciseId)
      expect(result).toBeUndefined()
    })

    it('deletes all phases for exercise', async () => {
      const exerciseId = 'ex-123'
      await db.phases.bulkPut([
        {
          id: 'phase-1',
          exerciseId,
          name: 'Phase 1',
          order: 1,
          updatedAt: new Date().toISOString(),
          cachedAt: new Date(),
        },
        {
          id: 'phase-2',
          exerciseId,
          name: 'Phase 2',
          order: 2,
          updatedAt: new Date().toISOString(),
          cachedAt: new Date(),
        },
      ])

      await clearExerciseCache(exerciseId)

      const phases = await db.phases.where('exerciseId').equals(exerciseId).toArray()
      expect(phases).toHaveLength(0)
    })

    it('deletes all injects for exercise', async () => {
      const exerciseId = 'ex-123'
      await db.injects.bulkPut([
        {
          id: 'inject-1',
          exerciseId,
          injectNumber: 1,
          title: 'Inject 1',
          status: 'Pending',
          updatedAt: new Date().toISOString(),
          cachedAt: new Date(),
        },
        {
          id: 'inject-2',
          exerciseId,
          injectNumber: 2,
          title: 'Inject 2',
          status: 'Pending',
          updatedAt: new Date().toISOString(),
          cachedAt: new Date(),
        },
      ])

      await clearExerciseCache(exerciseId)

      const injects = await db.injects.where('exerciseId').equals(exerciseId).toArray()
      expect(injects).toHaveLength(0)
    })

    it('deletes all observations for exercise', async () => {
      const exerciseId = 'ex-123'
      await db.observations.bulkPut([
        {
          id: 'obs-1',
          exerciseId,
          content: 'Observation 1',
          updatedAt: new Date().toISOString(),
          cachedAt: new Date(),
        },
        {
          id: 'obs-2',
          exerciseId,
          content: 'Observation 2',
          updatedAt: new Date().toISOString(),
          cachedAt: new Date(),
        },
      ])

      await clearExerciseCache(exerciseId)

      const observations = await db.observations.where('exerciseId').equals(exerciseId).toArray()
      expect(observations).toHaveLength(0)
    })

    it('deletes sync metadata for exercise', async () => {
      const exerciseId = 'ex-123'
      await db.syncMetadata.put({
        key: `exercise-${exerciseId}`,
        lastSyncAt: new Date(),
      })

      await clearExerciseCache(exerciseId)

      const metadata = await db.syncMetadata.get(`exercise-${exerciseId}`)
      expect(metadata).toBeUndefined()
    })

    it('does not affect other exercises', async () => {
      const exerciseId1 = 'ex-123'
      const exerciseId2 = 'ex-456'

      await db.exercises.bulkPut([
        {
          id: exerciseId1,
          name: 'Exercise 1',
          exerciseType: 'TTX',
          status: 'Draft',
          updatedAt: new Date().toISOString(),
          cachedAt: new Date(),
        },
        {
          id: exerciseId2,
          name: 'Exercise 2',
          exerciseType: 'FSE',
          status: 'Active',
          updatedAt: new Date().toISOString(),
          cachedAt: new Date(),
        },
      ])

      await clearExerciseCache(exerciseId1)

      const remaining = await db.exercises.get(exerciseId2)
      expect(remaining).toBeDefined()
      expect(remaining?.name).toBe('Exercise 2')
    })

    it('operates atomically within transaction', async () => {
      const exerciseId = 'ex-123'

      // Add data to all tables
      await db.exercises.put({
        id: exerciseId,
        name: 'Test',
        exerciseType: 'TTX',
        status: 'Draft',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
      })
      await db.phases.put({
        id: 'phase-1',
        exerciseId,
        name: 'Phase',
        order: 1,
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
      })
      await db.injects.put({
        id: 'inject-1',
        exerciseId,
        injectNumber: 1,
        title: 'Inject',
        status: 'Pending',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
      })
      await db.observations.put({
        id: 'obs-1',
        exerciseId,
        content: 'Observation',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
      })

      // Clear should delete all atomically
      await clearExerciseCache(exerciseId)

      expect(await db.exercises.count()).toBe(0)
      expect(await db.phases.count()).toBe(0)
      expect(await db.injects.count()).toBe(0)
      expect(await db.observations.count()).toBe(0)
    })
  })

  // ============================================================================
  // clearAllCache Tests
  // ============================================================================

  describe('clearAllCache', () => {
    it('clears all tables', async () => {
      // Add data to all tables
      await db.exercises.put({
        id: 'ex-1',
        name: 'Exercise',
        exerciseType: 'TTX',
        status: 'Draft',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
      })
      await db.phases.put({
        id: 'phase-1',
        exerciseId: 'ex-1',
        name: 'Phase',
        order: 1,
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
      })
      await db.injects.put({
        id: 'inject-1',
        exerciseId: 'ex-1',
        injectNumber: 1,
        title: 'Inject',
        status: 'Pending',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
      })
      await db.observations.put({
        id: 'obs-1',
        exerciseId: 'ex-1',
        content: 'Observation',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
      })
      await db.pendingActions.add({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: {},
        timestamp: new Date(),
        retryCount: 0,
        status: 'pending',
      })
      await db.syncMetadata.put({
        key: 'test',
        lastSyncAt: new Date(),
      })

      await clearAllCache()

      expect(await db.exercises.count()).toBe(0)
      expect(await db.phases.count()).toBe(0)
      expect(await db.injects.count()).toBe(0)
      expect(await db.observations.count()).toBe(0)
      expect(await db.pendingActions.count()).toBe(0)
      expect(await db.syncMetadata.count()).toBe(0)
    })
  })

  // ============================================================================
  // getPendingActionCount Tests
  // ============================================================================

  describe('getPendingActionCount', () => {
    it('returns 0 when no pending actions', async () => {
      const count = await getPendingActionCount()
      expect(count).toBe(0)
    })

    it('counts pending actions excluding failed', async () => {
      await db.pendingActions.bulkAdd([
        {
          type: 'FIRE_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 0,
          status: 'pending',
        },
        {
          type: 'SKIP_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 0,
          status: 'syncing',
        },
        {
          type: 'RESET_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 5,
          status: 'failed',
        },
      ])

      const count = await getPendingActionCount()
      expect(count).toBe(2) // pending + syncing, not failed
    })

    it('filters by exerciseId when provided', async () => {
      await db.pendingActions.bulkAdd([
        {
          type: 'FIRE_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 0,
          status: 'pending',
        },
        {
          type: 'SKIP_INJECT',
          exerciseId: 'ex-2',
          payload: {},
          timestamp: new Date(),
          retryCount: 0,
          status: 'pending',
        },
        {
          type: 'RESET_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 0,
          status: 'pending',
        },
      ])

      const count = await getPendingActionCount('ex-1')
      expect(count).toBe(2)
    })

    it('excludes failed actions when filtering by exerciseId', async () => {
      await db.pendingActions.bulkAdd([
        {
          type: 'FIRE_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 0,
          status: 'pending',
        },
        {
          type: 'SKIP_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 5,
          status: 'failed',
        },
      ])

      const count = await getPendingActionCount('ex-1')
      expect(count).toBe(1) // Only pending, not failed
    })
  })

  // ============================================================================
  // getPendingActions Tests
  // ============================================================================

  describe('getPendingActions', () => {
    it('returns empty array when no pending actions', async () => {
      const actions = await getPendingActions()
      expect(actions).toEqual([])
    })

    it('returns actions in FIFO order by timestamp', async () => {
      const now = new Date()
      const earlier = new Date(now.getTime() - 10000)
      const later = new Date(now.getTime() + 10000)

      await db.pendingActions.bulkAdd([
        {
          type: 'FIRE_INJECT',
          exerciseId: 'ex-1',
          payload: { order: 2 },
          timestamp: now,
          retryCount: 0,
          status: 'pending',
        },
        {
          type: 'SKIP_INJECT',
          exerciseId: 'ex-1',
          payload: { order: 1 },
          timestamp: earlier,
          retryCount: 0,
          status: 'pending',
        },
        {
          type: 'RESET_INJECT',
          exerciseId: 'ex-1',
          payload: { order: 3 },
          timestamp: later,
          retryCount: 0,
          status: 'pending',
        },
      ])

      const actions = await getPendingActions()
      expect(actions[0].payload).toEqual({ order: 1 }) // earliest
      expect(actions[1].payload).toEqual({ order: 2 })
      expect(actions[2].payload).toEqual({ order: 3 }) // latest
    })

    it('filters by exerciseId when provided', async () => {
      await db.pendingActions.bulkAdd([
        {
          type: 'FIRE_INJECT',
          exerciseId: 'ex-1',
          payload: { ex: 1 },
          timestamp: new Date(),
          retryCount: 0,
          status: 'pending',
        },
        {
          type: 'SKIP_INJECT',
          exerciseId: 'ex-2',
          payload: { ex: 2 },
          timestamp: new Date(),
          retryCount: 0,
          status: 'pending',
        },
      ])

      const actions = await getPendingActions('ex-1')
      expect(actions).toHaveLength(1)
      expect(actions[0].exerciseId).toBe('ex-1')
    })

    it('includes all statuses (pending, syncing, failed)', async () => {
      await db.pendingActions.bulkAdd([
        {
          type: 'FIRE_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 0,
          status: 'pending',
        },
        {
          type: 'SKIP_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 0,
          status: 'syncing',
        },
        {
          type: 'RESET_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 5,
          status: 'failed',
        },
      ])

      const actions = await getPendingActions()
      expect(actions).toHaveLength(3)
    })
  })

  // ============================================================================
  // addPendingAction Tests
  // ============================================================================

  describe('addPendingAction', () => {
    it('adds action with auto-generated timestamp', async () => {
      const before = new Date()

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inject-1' },
      })

      const after = new Date()
      const actions = await db.pendingActions.toArray()

      expect(actions).toHaveLength(1)
      expect(actions[0].timestamp.getTime()).toBeGreaterThanOrEqual(before.getTime())
      expect(actions[0].timestamp.getTime()).toBeLessThanOrEqual(after.getTime())
    })

    it('sets retryCount to 0', async () => {
      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: {},
      })

      const actions = await db.pendingActions.toArray()
      expect(actions[0].retryCount).toBe(0)
    })

    it('sets status to pending', async () => {
      await addPendingAction({
        type: 'SKIP_INJECT',
        exerciseId: 'ex-1',
        payload: { reason: 'Test' },
      })

      const actions = await db.pendingActions.toArray()
      expect(actions[0].status).toBe('pending')
    })

    it('returns the auto-incremented id', async () => {
      const id1 = await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: {},
      })

      const id2 = await addPendingAction({
        type: 'SKIP_INJECT',
        exerciseId: 'ex-1',
        payload: {},
      })

      expect(id1).toBeGreaterThan(0)
      expect(id2).toBe(id1 + 1)
    })

    it('preserves action type and payload', async () => {
      const payload = { injectId: 'inject-123', userId: 'user-456' }

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload,
      })

      const actions = await db.pendingActions.toArray()
      expect(actions[0].type).toBe('FIRE_INJECT')
      expect(actions[0].payload).toEqual(payload)
    })
  })

  // ============================================================================
  // updatePendingActionStatus Tests
  // ============================================================================

  describe('updatePendingActionStatus', () => {
    it('updates status to syncing', async () => {
      const id = await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: {},
      })

      await updatePendingActionStatus(id, 'syncing')

      const action = await db.pendingActions.get(id)
      expect(action?.status).toBe('syncing')
    })

    it('updates status to failed with error message', async () => {
      const id = await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: {},
      })

      await updatePendingActionStatus(id, 'failed', 'Network error')

      const action = await db.pendingActions.get(id)
      expect(action?.status).toBe('failed')
      expect(action?.error).toBe('Network error')
    })

    it('clears error when status changes back to pending', async () => {
      const id = await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: {},
      })

      await updatePendingActionStatus(id, 'failed', 'Error')
      await updatePendingActionStatus(id, 'pending', undefined)

      const action = await db.pendingActions.get(id)
      expect(action?.status).toBe('pending')
      expect(action?.error).toBeUndefined()
    })
  })

  // ============================================================================
  // incrementRetryCount Tests
  // ============================================================================

  describe('incrementRetryCount', () => {
    it('increments retry count by 1', async () => {
      const id = await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: {},
      })

      await incrementRetryCount(id)

      const action = await db.pendingActions.get(id)
      expect(action?.retryCount).toBe(1)
    })

    it('can increment multiple times', async () => {
      const id = await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: {},
      })

      await incrementRetryCount(id)
      await incrementRetryCount(id)
      await incrementRetryCount(id)

      const action = await db.pendingActions.get(id)
      expect(action?.retryCount).toBe(3)
    })
  })

  // ============================================================================
  // deletePendingAction Tests
  // ============================================================================

  describe('deletePendingAction', () => {
    it('deletes action by id', async () => {
      const id = await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: {},
      })

      await deletePendingAction(id)

      const action = await db.pendingActions.get(id)
      expect(action).toBeUndefined()
    })

    it('does not affect other actions', async () => {
      const id1 = await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { id: 1 },
      })
      const id2 = await addPendingAction({
        type: 'SKIP_INJECT',
        exerciseId: 'ex-1',
        payload: { id: 2 },
      })

      await deletePendingAction(id1)

      expect(await db.pendingActions.get(id1)).toBeUndefined()
      expect(await db.pendingActions.get(id2)).toBeDefined()
    })
  })

  // ============================================================================
  // deleteFailedActions Tests
  // ============================================================================

  describe('deleteFailedActions', () => {
    it('deletes only failed actions', async () => {
      await db.pendingActions.bulkAdd([
        {
          type: 'FIRE_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 0,
          status: 'pending',
        },
        {
          type: 'SKIP_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 0,
          status: 'syncing',
        },
        {
          type: 'RESET_INJECT',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 5,
          status: 'failed',
        },
        {
          type: 'CREATE_OBSERVATION',
          exerciseId: 'ex-1',
          payload: {},
          timestamp: new Date(),
          retryCount: 3,
          status: 'failed',
        },
      ])

      const deletedCount = await deleteFailedActions()

      expect(deletedCount).toBe(2) // Two failed actions deleted
      expect(await db.pendingActions.count()).toBe(2) // pending + syncing remain
    })

    it('returns 0 when no failed actions', async () => {
      await db.pendingActions.add({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: {},
        timestamp: new Date(),
        retryCount: 0,
        status: 'pending',
      })

      const deletedCount = await deleteFailedActions()
      expect(deletedCount).toBe(0)
    })
  })

  // ============================================================================
  // pruneOldCache Tests
  // ============================================================================

  describe('pruneOldCache', () => {
    it('deletes exercises older than specified days', async () => {
      const now = new Date()
      const oldDate = new Date(now.getTime() - 8 * 24 * 60 * 60 * 1000) // 8 days ago

      await db.exercises.bulkPut([
        {
          id: 'old-ex',
          name: 'Old Exercise',
          exerciseType: 'TTX',
          status: 'Completed',
          updatedAt: oldDate.toISOString(),
          cachedAt: oldDate,
        },
        {
          id: 'new-ex',
          name: 'New Exercise',
          exerciseType: 'FSE',
          status: 'Draft',
          updatedAt: now.toISOString(),
          cachedAt: now,
        },
      ])

      await pruneOldCache(7)

      const exercises = await db.exercises.toArray()
      expect(exercises).toHaveLength(1)
      expect(exercises[0].id).toBe('new-ex')
    })

    it('also deletes related phases, injects, observations', async () => {
      const oldDate = new Date(Date.now() - 10 * 24 * 60 * 60 * 1000) // 10 days ago
      const exerciseId = 'old-ex'

      await db.exercises.put({
        id: exerciseId,
        name: 'Old Exercise',
        exerciseType: 'TTX',
        status: 'Completed',
        updatedAt: oldDate.toISOString(),
        cachedAt: oldDate,
      })
      await db.phases.put({
        id: 'phase-1',
        exerciseId,
        name: 'Phase',
        order: 1,
        updatedAt: oldDate.toISOString(),
        cachedAt: oldDate,
      })
      await db.injects.put({
        id: 'inject-1',
        exerciseId,
        injectNumber: 1,
        title: 'Inject',
        status: 'Pending',
        updatedAt: oldDate.toISOString(),
        cachedAt: oldDate,
      })
      await db.observations.put({
        id: 'obs-1',
        exerciseId,
        content: 'Observation',
        updatedAt: oldDate.toISOString(),
        cachedAt: oldDate,
      })

      await pruneOldCache(7)

      expect(await db.exercises.count()).toBe(0)
      expect(await db.phases.count()).toBe(0)
      expect(await db.injects.count()).toBe(0)
      expect(await db.observations.count()).toBe(0)
    })

    it('uses 7 days as default', async () => {
      const sixDaysAgo = new Date(Date.now() - 6 * 24 * 60 * 60 * 1000)
      const eightDaysAgo = new Date(Date.now() - 8 * 24 * 60 * 60 * 1000)

      await db.exercises.bulkPut([
        {
          id: 'recent-ex',
          name: 'Recent',
          exerciseType: 'TTX',
          status: 'Draft',
          updatedAt: sixDaysAgo.toISOString(),
          cachedAt: sixDaysAgo,
        },
        {
          id: 'old-ex',
          name: 'Old',
          exerciseType: 'TTX',
          status: 'Draft',
          updatedAt: eightDaysAgo.toISOString(),
          cachedAt: eightDaysAgo,
        },
      ])

      await pruneOldCache() // default 7 days

      const exercises = await db.exercises.toArray()
      expect(exercises).toHaveLength(1)
      expect(exercises[0].id).toBe('recent-ex')
    })
  })

  // ============================================================================
  // getStorageEstimate Tests
  // ============================================================================

  describe('getStorageEstimate', () => {
    it('returns usage and quota when navigator.storage available', async () => {
      // Mock navigator.storage.estimate
      const originalStorage = navigator.storage
      Object.defineProperty(navigator, 'storage', {
        value: {
          estimate: vi.fn().mockResolvedValue({ usage: 1000, quota: 1000000 }),
        },
        configurable: true,
      })

      const estimate = await getStorageEstimate()

      expect(estimate).toEqual({ usage: 1000, quota: 1000000 })

      // Restore
      Object.defineProperty(navigator, 'storage', {
        value: originalStorage,
        configurable: true,
      })
    })

    it('returns null when navigator.storage not available', async () => {
      const originalStorage = navigator.storage
      Object.defineProperty(navigator, 'storage', {
        value: undefined,
        configurable: true,
      })

      const estimate = await getStorageEstimate()

      expect(estimate).toBeNull()

      // Restore
      Object.defineProperty(navigator, 'storage', {
        value: originalStorage,
        configurable: true,
      })
    })

    it('handles missing usage/quota values', async () => {
      const originalStorage = navigator.storage
      Object.defineProperty(navigator, 'storage', {
        value: {
          estimate: vi.fn().mockResolvedValue({}),
        },
        configurable: true,
      })

      const estimate = await getStorageEstimate()

      expect(estimate).toEqual({ usage: 0, quota: 0 })

      // Restore
      Object.defineProperty(navigator, 'storage', {
        value: originalStorage,
        configurable: true,
      })
    })
  })
})
