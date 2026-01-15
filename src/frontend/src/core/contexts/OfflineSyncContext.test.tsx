/**
 * Tests for OfflineSyncContext
 *
 * Tests the app-level offline sync provider.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import React from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import {
  OfflineSyncProvider,
  useOfflineSyncContext,
} from './OfflineSyncContext'
import { clearAllCache } from '../offline/db'

// Mock react-toastify
vi.mock('react-toastify', () => ({
  toast: {
    success: vi.fn(),
    warning: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
  },
}))

// Mock connectivity context
const mockConnectivityState = vi.fn().mockReturnValue('online')
const mockSetPendingCount = vi.fn()

vi.mock('./ConnectivityContext', () => ({
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
const mockDeleteFailedActions = vi.fn().mockResolvedValue(0)

vi.mock('../offline', () => ({
  syncPendingActions: (...args: unknown[]) => mockSyncPendingActions(...args),
  getSyncStatus: () => mockGetSyncStatus(),
  getPendingActionCount: (...args: unknown[]) => mockGetPendingActionCount(...args),
  deleteFailedActions: () => mockDeleteFailedActions(),
}))

// Test component that consumes the context
const TestConsumer: React.FC = () => {
  const {
    syncStatus,
    isSyncing,
    progress,
    conflicts,
    lastResult,
    manualSync,
    clearConflicts,
  } = useOfflineSyncContext()

  return (
    <div>
      <div data-testid="sync-status">{syncStatus}</div>
      <div data-testid="is-syncing">{isSyncing.toString()}</div>
      <div data-testid="progress">{progress ? JSON.stringify(progress) : 'null'}</div>
      <div data-testid="conflicts">{conflicts.length}</div>
      <div data-testid="last-result">{lastResult ? JSON.stringify(lastResult) : 'null'}</div>
      <button data-testid="sync-btn" onClick={() => manualSync()}>Sync</button>
      <button data-testid="clear-btn" onClick={() => clearConflicts()}>Clear</button>
    </div>
  )
}

describe('OfflineSyncContext', () => {
  let queryClient: QueryClient

  const renderWithProvider = (ui: React.ReactElement) => {
    return render(
      <QueryClientProvider client={queryClient}>
        <OfflineSyncProvider>{ui}</OfflineSyncProvider>
      </QueryClientProvider>,
    )
  }

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
  // Provider Tests
  // ============================================================================

  describe('OfflineSyncProvider', () => {
    it('provides context to children', () => {
      renderWithProvider(<TestConsumer />)

      expect(screen.getByTestId('sync-status')).toHaveTextContent('idle')
      expect(screen.getByTestId('is-syncing')).toHaveTextContent('false')
    })

    it('fetches initial pending count on mount', async () => {
      mockGetPendingActionCount.mockResolvedValue(5)

      renderWithProvider(<TestConsumer />)

      await waitFor(() => {
        expect(mockGetPendingActionCount).toHaveBeenCalled()
      })

      await waitFor(() => {
        expect(mockSetPendingCount).toHaveBeenCalledWith(5)
      })
    })
  })

  // ============================================================================
  // useOfflineSyncContext Tests
  // ============================================================================

  describe('useOfflineSyncContext', () => {
    it('throws when used outside provider', () => {
      // Suppress console.error for this test
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      expect(() => {
        render(<TestConsumer />)
      }).toThrow('useOfflineSyncContext must be used within an OfflineSyncProvider')

      consoleSpy.mockRestore()
    })
  })

  // ============================================================================
  // manualSync Tests
  // ============================================================================

  describe('manualSync', () => {
    it('calls syncPendingActions', async () => {
      const user = userEvent.setup()

      renderWithProvider(<TestConsumer />)

      await user.click(screen.getByTestId('sync-btn'))

      await waitFor(() => {
        expect(mockSyncPendingActions).toHaveBeenCalled()
      })
    })

    it('updates lastResult after sync', async () => {
      const user = userEvent.setup()
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 2,
        succeeded: 2,
        failed: 0,
        failedActions: [],
      })

      renderWithProvider(<TestConsumer />)

      await user.click(screen.getByTestId('sync-btn'))

      await waitFor(() => {
        expect(screen.getByTestId('last-result')).toHaveTextContent(
          '"totalActions":2',
        )
      })
    })

    it('does not start if already syncing', async () => {
      mockGetSyncStatus.mockReturnValue('syncing')

      const user = userEvent.setup()
      renderWithProvider(<TestConsumer />)

      await user.click(screen.getByTestId('sync-btn'))

      // Should return early without calling sync
      expect(mockSyncPendingActions).not.toHaveBeenCalled()
    })

    it('invalidates queries after sync', async () => {
      const user = userEvent.setup()
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 1,
        succeeded: 1,
        failed: 0,
        failedActions: [],
      })

      const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries')

      renderWithProvider(<TestConsumer />)

      await user.click(screen.getByTestId('sync-btn'))

      await waitFor(() => {
        expect(invalidateQueriesSpy).toHaveBeenCalled()
      })
    })
  })

  // ============================================================================
  // clearConflicts Tests
  // ============================================================================

  describe('clearConflicts', () => {
    it('clears conflicts state', async () => {
      const user = userEvent.setup()

      renderWithProvider(<TestConsumer />)

      await user.click(screen.getByTestId('clear-btn'))

      await waitFor(() => {
        expect(screen.getByTestId('conflicts')).toHaveTextContent('0')
      })
    })

    it('deletes failed actions from IndexedDB', async () => {
      const user = userEvent.setup()

      renderWithProvider(<TestConsumer />)

      await user.click(screen.getByTestId('clear-btn'))

      await waitFor(() => {
        expect(mockDeleteFailedActions).toHaveBeenCalled()
      })
    })

    it('refreshes pending count after clearing', async () => {
      const user = userEvent.setup()
      mockGetPendingActionCount.mockResolvedValueOnce(3).mockResolvedValueOnce(0)

      renderWithProvider(<TestConsumer />)

      await waitFor(() => {
        expect(mockSetPendingCount).toHaveBeenCalledWith(3)
      })

      await user.click(screen.getByTestId('clear-btn'))

      await waitFor(() => {
        expect(mockSetPendingCount).toHaveBeenCalledWith(0)
      })
    })
  })

  // ============================================================================
  // Toast Notifications Tests
  // ============================================================================

  describe('toast notifications', () => {
    it('shows success toast when all actions succeed', async () => {
      const { toast } = await import('react-toastify')
      const user = userEvent.setup()
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 3,
        succeeded: 3,
        failed: 0,
        failedActions: [],
      })

      renderWithProvider(<TestConsumer />)

      await user.click(screen.getByTestId('sync-btn'))

      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith(
          'All 3 change(s) synced successfully!',
          expect.any(Object),
        )
      })
    })

    it('shows warning toast for partial sync', async () => {
      const { toast } = await import('react-toastify')
      const user = userEvent.setup()
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 3,
        succeeded: 2,
        failed: 1,
        failedActions: [],
      })

      renderWithProvider(<TestConsumer />)

      await user.click(screen.getByTestId('sync-btn'))

      await waitFor(() => {
        expect(toast.warning).toHaveBeenCalled()
      })
    })

    it('shows error toast when all actions fail', async () => {
      const { toast } = await import('react-toastify')
      const user = userEvent.setup()
      mockSyncPendingActions.mockResolvedValue({
        totalActions: 2,
        succeeded: 0,
        failed: 2,
        failedActions: [],
      })

      renderWithProvider(<TestConsumer />)

      await user.click(screen.getByTestId('sync-btn'))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalled()
      })
    })
  })
})
