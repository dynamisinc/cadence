/**
 * Tests for Sync Service (syncService.ts)
 *
 * Tests offline action sync with server, error handling, and retry logic.
 */

import { describe, it, expect, beforeEach, afterEach, vi, type Mock } from 'vitest'
import {
  syncPendingActions,
  retryAction,
  discardAction,
  getSyncStatus,
  cancelSync,
  type SyncProgress,
} from './syncService'
import { db, clearAllCache, addPendingAction } from './db'
import { injectService } from '../../features/injects/services/injectService'
import { observationService } from '../../features/observations/services/observationService'

// Mock the services
vi.mock('../../features/injects/services/injectService', () => ({
  injectService: {
    fireInject: vi.fn(),
    skipInject: vi.fn(),
    resetInject: vi.fn(),
  },
}))

vi.mock('../../features/observations/services/observationService', () => ({
  observationService: {
    createObservation: vi.fn(),
    updateObservation: vi.fn(),
    deleteObservation: vi.fn(),
  },
}))

describe('syncService', () => {
  beforeEach(async () => {
    await clearAllCache()
    vi.clearAllMocks()
  })

  afterEach(async () => {
    await clearAllCache()
  })

  // ============================================================================
  // getSyncStatus Tests
  // ============================================================================

  describe('getSyncStatus', () => {
    it('returns idle initially', () => {
      expect(getSyncStatus()).toBe('idle')
    })
  })

  // ============================================================================
  // syncPendingActions Tests - Basic Flow
  // ============================================================================

  describe('syncPendingActions', () => {
    it('returns empty result when no pending actions', async () => {
      const result = await syncPendingActions()

      expect(result).toEqual({
        totalActions: 0,
        succeeded: 0,
        failed: 0,
        failedActions: [],
      })
    })

    it('processes actions in FIFO order', async () => {
      const callOrder: string[] = []

      ;(injectService.fireInject as Mock).mockImplementation(async (_, injectId) => {
        callOrder.push(injectId)
      })

      // Add actions with timestamps to ensure order
      await db.pendingActions.add({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'first', firedAt: new Date().toISOString() },
        timestamp: new Date('2025-01-15T10:00:00Z'),
        retryCount: 0,
        status: 'pending',
      })
      await db.pendingActions.add({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'second', firedAt: new Date().toISOString() },
        timestamp: new Date('2025-01-15T10:00:01Z'),
        retryCount: 0,
        status: 'pending',
      })

      await syncPendingActions()

      expect(callOrder).toEqual(['first', 'second'])
    })

    it('calls progress callback during sync', async () => {
      ;(injectService.fireInject as Mock).mockResolvedValue({})

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })
      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-2', firedAt: new Date().toISOString() },
      })

      const progressCalls: SyncProgress[] = []
      await syncPendingActions(undefined, progress => progressCalls.push({ ...progress }))

      // Should have progress calls for each action + final
      expect(progressCalls.length).toBeGreaterThanOrEqual(2)
      expect(progressCalls[0].current).toBe(1)
      expect(progressCalls[0].total).toBe(2)
    })

    it('sets final status to completed when all succeed', async () => {
      ;(injectService.fireInject as Mock).mockResolvedValue({})

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })

      const result = await syncPendingActions()

      expect(result.succeeded).toBe(1)
      expect(result.failed).toBe(0)
      expect(getSyncStatus()).toBe('completed')
    })

    it('sets final status to failed when all fail', async () => {
      ;(injectService.fireInject as Mock).mockRejectedValue({
        response: { status: 400, data: { message: 'Bad request' } },
      })

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })

      const result = await syncPendingActions()

      expect(result.succeeded).toBe(0)
      expect(result.failed).toBe(1)
      expect(getSyncStatus()).toBe('failed')
    })

    it('sets final status to partial when some succeed and some fail', async () => {
      ;(injectService.fireInject as Mock)
        .mockResolvedValueOnce({})
        .mockRejectedValueOnce({
          response: { status: 400, data: { message: 'Bad request' } },
        })

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })
      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-2', firedAt: new Date().toISOString() },
      })

      const result = await syncPendingActions()

      expect(result.succeeded).toBe(1)
      expect(result.failed).toBe(1)
      expect(getSyncStatus()).toBe('partial')
    })

    it('filters actions by exerciseId when provided', async () => {
      ;(injectService.fireInject as Mock).mockResolvedValue({})

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })
      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-2',
        payload: { injectId: 'inj-2', firedAt: new Date().toISOString() },
      })

      const result = await syncPendingActions('ex-1')

      expect(result.totalActions).toBe(1)
      expect(injectService.fireInject).toHaveBeenCalledTimes(1)
    })

    it('removes successful actions from queue', async () => {
      ;(injectService.fireInject as Mock).mockResolvedValue({})

      const actionId = await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })

      await syncPendingActions()

      const action = await db.pendingActions.get(actionId)
      expect(action).toBeUndefined()
    })
  })

  // ============================================================================
  // Action Handler Tests - FIRE_INJECT
  // ============================================================================

  describe('FIRE_INJECT action', () => {
    it('calls injectService.fireInject with correct params', async () => {
      ;(injectService.fireInject as Mock).mockResolvedValue({})

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-123',
        payload: { injectId: 'inj-456', firedAt: '2025-01-15T12:00:00Z' },
      })

      await syncPendingActions()

      expect(injectService.fireInject).toHaveBeenCalledWith('ex-123', 'inj-456', {
        firedAt: '2025-01-15T12:00:00Z',
      })
    })

    it('updates cached inject on success', async () => {
      ;(injectService.fireInject as Mock).mockResolvedValue({})

      // Add cached inject with pendingSync
      await db.injects.put({
        id: 'inj-1',
        exerciseId: 'ex-1',
        injectNumber: 1,
        title: 'Test',
        status: 'Delivered',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
        pendingSync: true,
      })

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })

      await syncPendingActions()

      const inject = await db.injects.get('inj-1')
      expect(inject?.pendingSync).toBe(false)
    })
  })

  // ============================================================================
  // Action Handler Tests - SKIP_INJECT
  // ============================================================================

  describe('SKIP_INJECT action', () => {
    it('calls injectService.skipInject with correct params', async () => {
      ;(injectService.skipInject as Mock).mockResolvedValue({})

      await addPendingAction({
        type: 'SKIP_INJECT',
        exerciseId: 'ex-123',
        payload: { injectId: 'inj-456', reason: 'Not applicable', skippedAt: '2025-01-15T12:00:00Z' },
      })

      await syncPendingActions()

      expect(injectService.skipInject).toHaveBeenCalledWith('ex-123', 'inj-456', {
        reason: 'Not applicable',
      })
    })
  })

  // ============================================================================
  // Action Handler Tests - RESET_INJECT
  // ============================================================================

  describe('RESET_INJECT action', () => {
    it('calls injectService.resetInject with correct params', async () => {
      ;(injectService.resetInject as Mock).mockResolvedValue({})

      await addPendingAction({
        type: 'RESET_INJECT',
        exerciseId: 'ex-123',
        payload: { injectId: 'inj-456' },
      })

      await syncPendingActions()

      expect(injectService.resetInject).toHaveBeenCalledWith('ex-123', 'inj-456')
    })
  })

  // ============================================================================
  // Action Handler Tests - CREATE_OBSERVATION
  // ============================================================================

  describe('CREATE_OBSERVATION action', () => {
    it('calls observationService.createObservation with correct params', async () => {
      ;(observationService.createObservation as Mock).mockResolvedValue({
        id: 'obs-real-id',
        exerciseId: 'ex-123',
        content: 'Test observation',
        updatedAt: new Date().toISOString(),
      })

      await addPendingAction({
        type: 'CREATE_OBSERVATION',
        exerciseId: 'ex-123',
        payload: {
          observation: { content: 'Test observation', rating: 'Good' },
          tempId: 'temp-123',
        },
      })

      await syncPendingActions()

      expect(observationService.createObservation).toHaveBeenCalledWith('ex-123', {
        content: 'Test observation',
        rating: 'Good',
      })
    })

    it('replaces temp observation with real one in cache', async () => {
      ;(observationService.createObservation as Mock).mockResolvedValue({
        id: 'obs-real-id',
        exerciseId: 'ex-123',
        content: 'Test observation',
        updatedAt: new Date().toISOString(),
      })

      // Add temp observation
      await db.observations.put({
        id: 'temp-123',
        exerciseId: 'ex-123',
        content: 'Test observation',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
        pendingSync: true,
        tempId: 'temp-123',
      })

      await addPendingAction({
        type: 'CREATE_OBSERVATION',
        exerciseId: 'ex-123',
        payload: {
          observation: { content: 'Test observation' },
          tempId: 'temp-123',
        },
      })

      await syncPendingActions()

      // Temp observation should be gone
      const temp = await db.observations.get('temp-123')
      expect(temp).toBeUndefined()

      // Real observation should exist
      const real = await db.observations.get('obs-real-id')
      expect(real).toBeDefined()
      expect(real?.pendingSync).toBe(false)
    })
  })

  // ============================================================================
  // Action Handler Tests - UPDATE_OBSERVATION
  // ============================================================================

  describe('UPDATE_OBSERVATION action', () => {
    it('calls observationService.updateObservation with correct params', async () => {
      ;(observationService.updateObservation as Mock).mockResolvedValue({})

      await addPendingAction({
        type: 'UPDATE_OBSERVATION',
        exerciseId: 'ex-123',
        payload: {
          observationId: 'obs-456',
          changes: { content: 'Updated content', rating: 'Excellent' },
        },
      })

      await syncPendingActions()

      expect(observationService.updateObservation).toHaveBeenCalledWith('obs-456', {
        content: 'Updated content',
        rating: 'Excellent',
      })
    })

    it('removes observation from cache if 404 returned', async () => {
      ;(observationService.updateObservation as Mock).mockRejectedValue({
        response: { status: 404, data: { message: 'Not found' } },
      })

      await db.observations.put({
        id: 'obs-456',
        exerciseId: 'ex-123',
        content: 'Test',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
        pendingSync: true,
      })

      await addPendingAction({
        type: 'UPDATE_OBSERVATION',
        exerciseId: 'ex-123',
        payload: {
          observationId: 'obs-456',
          changes: { content: 'Updated' },
        },
      })

      await syncPendingActions()

      const observation = await db.observations.get('obs-456')
      expect(observation).toBeUndefined()
    })
  })

  // ============================================================================
  // Action Handler Tests - DELETE_OBSERVATION
  // ============================================================================

  describe('DELETE_OBSERVATION action', () => {
    it('calls observationService.deleteObservation with correct id', async () => {
      ;(observationService.deleteObservation as Mock).mockResolvedValue({})

      await addPendingAction({
        type: 'DELETE_OBSERVATION',
        exerciseId: 'ex-123',
        payload: { observationId: 'obs-456' },
      })

      await syncPendingActions()

      expect(observationService.deleteObservation).toHaveBeenCalledWith('obs-456')
    })

    it('treats 404 as success (already deleted)', async () => {
      ;(observationService.deleteObservation as Mock).mockRejectedValue({
        response: { status: 404, data: { message: 'Not found' } },
      })

      await addPendingAction({
        type: 'DELETE_OBSERVATION',
        exerciseId: 'ex-123',
        payload: { observationId: 'obs-456' },
      })

      const result = await syncPendingActions()

      expect(result.succeeded).toBe(1)
      expect(result.failed).toBe(0)
    })

    it('removes observation from local cache', async () => {
      ;(observationService.deleteObservation as Mock).mockResolvedValue({})

      await db.observations.put({
        id: 'obs-456',
        exerciseId: 'ex-123',
        content: 'To delete',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
      })

      await addPendingAction({
        type: 'DELETE_OBSERVATION',
        exerciseId: 'ex-123',
        payload: { observationId: 'obs-456' },
      })

      await syncPendingActions()

      const observation = await db.observations.get('obs-456')
      expect(observation).toBeUndefined()
    })
  })

  // ============================================================================
  // Error Handling Tests
  // ============================================================================

  describe('error handling', () => {
    it('classifies 409 as conflict error', async () => {
      ;(injectService.fireInject as Mock).mockRejectedValue({
        response: { status: 409, data: { message: 'Conflict' } },
      })

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })

      const result = await syncPendingActions()

      expect(result.failed).toBe(1)
      const action = await db.pendingActions.toArray()
      expect(action[0].status).toBe('failed')
    })

    it('classifies 400 with conflict message as conflict', async () => {
      ;(injectService.fireInject as Mock).mockRejectedValue({
        response: { status: 400, data: { message: 'Only pending injects can be fired. Current status: Delivered' } },
      })

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })

      const conflicts: Array<{
        type: string
        message: string
      }> = []
      await syncPendingActions(undefined, p => {
        conflicts.push(...p.conflicts)
      })

      expect(conflicts.length).toBeGreaterThan(0)
      expect(conflicts[0].message).toContain('Only pending injects can be fired')
    })

    it('keeps server errors for retry with backoff', async () => {
      ;(injectService.fireInject as Mock).mockRejectedValue({
        response: { status: 500, data: { message: 'Internal server error' } },
      })

      const actionId = await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })

      await syncPendingActions()

      const action = await db.pendingActions.get(actionId)
      expect(action?.status).toBe('pending') // Still pending for retry
      expect(action?.retryCount).toBe(1)
    })

    it('marks as failed after 5 retries', async () => {
      ;(injectService.fireInject as Mock).mockRejectedValue({
        response: { status: 500, data: { message: 'Internal server error' } },
      })

      // Add action with 5 retries already
      await db.pendingActions.add({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
        timestamp: new Date(),
        retryCount: 5, // Max retries
        status: 'pending',
      })

      const result = await syncPendingActions()

      expect(result.failed).toBe(1)
      const actions = await db.pendingActions.toArray()
      expect(actions[0].status).toBe('failed')
    })

    it('marks non-conflict 4xx errors as failed without retry', async () => {
      ;(injectService.fireInject as Mock).mockRejectedValue({
        response: { status: 422, data: { message: 'Validation failed' } },
      })

      await addPendingAction({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
      })

      const result = await syncPendingActions()

      expect(result.failed).toBe(1)
      const actions = await db.pendingActions.toArray()
      expect(actions[0].status).toBe('failed')
    })
  })

  // ============================================================================
  // cancelSync Tests
  // ============================================================================

  describe('cancelSync', () => {
    it('cancelSync function exists and can be called', () => {
      // cancelSync is designed to be called while sync is in progress
      // Calling it when not syncing is a no-op
      expect(() => cancelSync()).not.toThrow()
    })
  })

  // ============================================================================
  // retryAction Tests
  // ============================================================================

  describe('retryAction', () => {
    it('retries a single failed action', async () => {
      ;(injectService.fireInject as Mock).mockResolvedValue({})

      const actionId = await db.pendingActions.add({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
        timestamp: new Date(),
        retryCount: 1,
        status: 'failed',
        error: 'Previous error',
      })

      const success = await retryAction(actionId)

      expect(success).toBe(true)
      expect(injectService.fireInject).toHaveBeenCalled()
      const action = await db.pendingActions.get(actionId)
      expect(action).toBeUndefined() // Removed after success
    })

    it('returns false if action not found', async () => {
      const success = await retryAction(999)
      expect(success).toBe(false)
    })

    it('marks as failed if retry fails', async () => {
      ;(injectService.fireInject as Mock).mockRejectedValue(new Error('Still failing'))

      const actionId = await db.pendingActions.add({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
        timestamp: new Date(),
        retryCount: 1,
        status: 'failed',
      })

      const success = await retryAction(actionId)

      expect(success).toBe(false)
      const action = await db.pendingActions.get(actionId)
      expect(action?.status).toBe('failed')
    })
  })

  // ============================================================================
  // discardAction Tests
  // ============================================================================

  describe('discardAction', () => {
    it('removes action from queue', async () => {
      const actionId = await db.pendingActions.add({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
        timestamp: new Date(),
        retryCount: 0,
        status: 'failed',
      })

      await discardAction(actionId)

      const action = await db.pendingActions.get(actionId)
      expect(action).toBeUndefined()
    })

    it('reverts cached inject pendingSync flag for inject actions', async () => {
      await db.injects.put({
        id: 'inj-1',
        exerciseId: 'ex-1',
        injectNumber: 1,
        title: 'Test',
        status: 'Delivered',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
        pendingSync: true,
      })

      const actionId = await db.pendingActions.add({
        type: 'FIRE_INJECT',
        exerciseId: 'ex-1',
        payload: { injectId: 'inj-1', firedAt: new Date().toISOString() },
        timestamp: new Date(),
        retryCount: 0,
        status: 'failed',
      })

      await discardAction(actionId)

      const inject = await db.injects.get('inj-1')
      expect(inject?.pendingSync).toBe(false)
    })

    it('removes temp observation for CREATE_OBSERVATION', async () => {
      await db.observations.put({
        id: 'temp-123',
        exerciseId: 'ex-1',
        content: 'Temp observation',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
        pendingSync: true,
        tempId: 'temp-123',
      })

      const actionId = await db.pendingActions.add({
        type: 'CREATE_OBSERVATION',
        exerciseId: 'ex-1',
        payload: {
          observation: { content: 'Temp observation' },
          tempId: 'temp-123',
        },
        timestamp: new Date(),
        retryCount: 0,
        status: 'failed',
      })

      await discardAction(actionId)

      const observation = await db.observations.get('temp-123')
      expect(observation).toBeUndefined()
    })

    it('reverts pendingSync for UPDATE_OBSERVATION', async () => {
      await db.observations.put({
        id: 'obs-1',
        exerciseId: 'ex-1',
        content: 'Updated content',
        updatedAt: new Date().toISOString(),
        cachedAt: new Date(),
        pendingSync: true,
      })

      const actionId = await db.pendingActions.add({
        type: 'UPDATE_OBSERVATION',
        exerciseId: 'ex-1',
        payload: {
          observationId: 'obs-1',
          changes: { content: 'Updated content' },
        },
        timestamp: new Date(),
        retryCount: 0,
        status: 'failed',
      })

      await discardAction(actionId)

      const observation = await db.observations.get('obs-1')
      expect(observation?.pendingSync).toBe(false)
    })

    it('handles nonexistent action gracefully', async () => {
      // Should not throw
      await discardAction(999)
    })
  })
})
