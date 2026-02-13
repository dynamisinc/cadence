/**
 * Tests for useOfflineSync Hook
 *
 * Tests the offline sync hook including sync functionality and state management.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { renderHook, act, waitFor } from '@testing-library/react'
import React, { type ReactNode } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useOfflineSync } from './useOfflineSync'
import { clearAllCache } from './db'

// Mock notify wrapper
vi.mock('@/shared/utils/notify', () => ({
  notify: {
    success: vi.fn(),
    warning: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
  },
}))

// Mock connectivity context
const mockConnectivityState = vi.fn().mockReturnValue('online')
const mockSetPendingCount = vi.fn()

vi.mock('../contexts', () => ({
  useConnectivity: () => ({
    isOnline: mockConnectivityState() === 'online',
    connectivityState: mockConnectivityState(),
    setPendingCount: mockSetPendingCount,
  }),
}))

// Mock sync module functions
const mockSyncPendingActions = vi.fn()
const mockGetSyncStatus = vi.fn().mockReturnValue('idle')
const mockGetPendingActionCount = vi.fn().mockResolvedValue(0)

vi.mock('./index', () => ({
  syncPendingActions: (...args: unknown[]) => mockSyncPendingActions(...args),
  getSyncStatus: () => mockGetSyncStatus(),
  getPendingActionCount: (...args: unknown[]) => mockGetPendingActionCount(...args),
}))

describe('useOfflineSync', () => {
  let queryClient: QueryClient

  const wrapper = ({ children }: { children: ReactNode }) =>
    React.createElement(QueryClientProvider, { client: queryClient }, children)

  beforeEach(async () => {
    await clearAllCache()
    vi.clearAllMocks()
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    })
    mockConnectivityState.mockReturnValue('online')
    mockGetPendingActionCount.mockResolvedValue(0)
    mockGetSyncStatus.mockReturnValue('idle')
    mockSyncPendingActions.mockResolvedValue({
      totalActions: 0,
      succeeded: 0,
      failed: 0,
      failedActions: [],
    })
  })

  afterEach(async () => {
    await clearAllCache()
    queryClient.clear()
  })

  // ============================================================================
  // Initial State Tests
  // ============================================================================

  describe('initial state', () => {
    it('returns idle sync status initially', async () => {
      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      expect(result.current.syncStatus).toBe('idle')
    })

    it('returns isSyncing false initially', async () => {
      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      expect(result.current.isSyncing).toBe(false)
    })

    it('fetches initial pending count', async () => {
      mockGetPendingActionCount.mockResolvedValue(5)

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await waitFor(() => {
        expect(result.current.pendingCount).toBe(5)
      })
    })

    it('calls setPendingCount with initial count', async () => {
      mockGetPendingActionCount.mockResolvedValue(3)

      renderHook(() => useOfflineSync(), { wrapper })

      await waitFor(() => {
        expect(mockSetPendingCount).toHaveBeenCalledWith(3)
      })
    })

    it('returns empty conflicts array initially', () => {
      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      expect(result.current.conflicts).toEqual([])
    })

    it('returns null lastResult initially', () => {
      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      expect(result.current.lastResult).toBeNull()
    })

    it('returns null progress initially', () => {
      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      expect(result.current.progress).toBeNull()
    })
  })

  // ============================================================================
  // Sync Function Tests
  // ============================================================================

  describe('sync function', () => {
    it('calls syncPendingActions without exerciseId by default', async () => {
      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(mockSyncPendingActions).toHaveBeenCalledWith(undefined, expect.any(Function))
    })

    it('calls syncPendingActions with exerciseId when provided', async () => {
      const { result } = renderHook(() => useOfflineSync({ exerciseId: 'ex-123' }), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(mockSyncPendingActions).toHaveBeenCalledWith('ex-123', expect.any(Function))
    })

    it('updates lastResult after sync', async () => {
      const syncResult = {
        totalActions: 2,
        succeeded: 2,
        failed: 0,
        failedActions: [],
      }
      mockSyncPendingActions.mockResolvedValue(syncResult)

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(result.current.lastResult).toEqual(syncResult)
    })

    it('refreshes pending count after sync', async () => {
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 2,
        succeeded: 2,
        failed: 0,
        failedActions: [],
      })
      mockGetPendingActionCount.mockResolvedValueOnce(2).mockResolvedValueOnce(0)

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await waitFor(() => {
        expect(result.current.pendingCount).toBe(2)
      })

      await act(async () => {
        await result.current.sync()
      })

      await waitFor(() => {
        expect(result.current.pendingCount).toBe(0)
      })
    })

    it('returns sync result from sync function', async () => {
      const syncResult = {
        totalActions: 1,
        succeeded: 1,
        failed: 0,
        failedActions: [],
      }
      mockSyncPendingActions.mockResolvedValue(syncResult)

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      let returnedResult
      await act(async () => {
        returnedResult = await result.current.sync()
      })

      expect(returnedResult).toEqual(syncResult)
    })
  })

  // ============================================================================
  // Toast Notification Tests
  // ============================================================================

  describe('toast notifications', () => {
    it('shows success toast when all actions succeed', async () => {
      const { notify } = await import('@/shared/utils/notify')
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 3,
        succeeded: 3,
        failed: 0,
        failedActions: [],
      })

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(notify.success).toHaveBeenCalledWith(
        'All 3 change(s) synced successfully!',
        expect.any(Object),
      )
    })

    it('shows warning toast for partial sync', async () => {
      const { notify } = await import('@/shared/utils/notify')
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 3,
        succeeded: 2,
        failed: 1,
        failedActions: [],
      })

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(notify.warning).toHaveBeenCalledWith(
        'Partial sync: 2 of 3 action(s) synced. 1 failed.',
        expect.any(Object),
      )
    })

    it('shows error toast when all actions fail', async () => {
      const { notify } = await import('@/shared/utils/notify')
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 2,
        succeeded: 0,
        failed: 2,
        failedActions: [],
      })

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(notify.error).toHaveBeenCalledWith(
        'Sync failed. 2 action(s) could not be synced.',
        expect.any(Object),
      )
    })

    it('shows error toast on sync exception', async () => {
      const { notify } = await import('@/shared/utils/notify')
      mockSyncPendingActions.mockRejectedValue(new Error('Network error'))

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(notify.error).toHaveBeenCalledWith(
        'Sync error: Network error',
        expect.any(Object),
      )
    })

    it('does not show toast when no actions to sync', async () => {
      const { notify } = await import('@/shared/utils/notify')
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 0,
        succeeded: 0,
        failed: 0,
        failedActions: [],
      })

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(notify.success).not.toHaveBeenCalled()
      expect(notify.warning).not.toHaveBeenCalled()
      expect(notify.error).not.toHaveBeenCalled()
    })
  })

  // ============================================================================
  // Conflict Management Tests
  // ============================================================================

  describe('conflict management', () => {
    it('clearConflicts clears the conflicts array', async () => {
      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      act(() => {
        result.current.clearConflicts()
      })

      expect(result.current.conflicts).toEqual([])
    })
  })

  // ============================================================================
  // Query Invalidation Tests
  // ============================================================================

  describe('query invalidation', () => {
    it('invalidates all queries when no exerciseId', async () => {
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 1,
        succeeded: 1,
        failed: 0,
        failedActions: [],
      })

      const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries')

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(invalidateQueriesSpy).toHaveBeenCalled()
    })

    it('invalidates specific queries when exerciseId provided', async () => {
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 1,
        succeeded: 1,
        failed: 0,
        failedActions: [],
      })

      const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries')

      const { result } = renderHook(() => useOfflineSync({ exerciseId: 'ex-123' }), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['injects', 'ex-123'] })
      expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['observations', 'ex-123'] })
      expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['exercise', 'ex-123'] })
    })
  })

  // ============================================================================
  // Error Handling Tests
  // ============================================================================

  describe('error handling', () => {
    it('returns empty result on sync exception', async () => {
      mockSyncPendingActions.mockRejectedValue(new Error('Network error'))

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      let returnedResult
      await act(async () => {
        returnedResult = await result.current.sync()
      })

      expect(returnedResult).toEqual({
        totalActions: 0,
        succeeded: 0,
        failed: 0,
        failedActions: [],
      })
    })

    it('sets syncStatus to failed on exception', async () => {
      mockSyncPendingActions.mockRejectedValue(new Error('Network error'))

      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      await act(async () => {
        await result.current.sync()
      })

      expect(result.current.syncStatus).toBe('failed')
    })
  })

  // ============================================================================
  // Options Tests
  // ============================================================================

  describe('options', () => {
    it('uses default options when none provided', () => {
      const { result } = renderHook(() => useOfflineSync(), { wrapper })

      // Just verify it doesn't crash and returns expected shape
      expect(result.current.sync).toBeDefined()
      expect(result.current.clearConflicts).toBeDefined()
    })

    it('accepts exerciseId option', async () => {
      mockGetPendingActionCount.mockResolvedValue(3)

      renderHook(() => useOfflineSync({ exerciseId: 'ex-456' }), { wrapper })

      await waitFor(() => {
        expect(mockGetPendingActionCount).toHaveBeenCalledWith('ex-456')
      })
    })
  })
})
