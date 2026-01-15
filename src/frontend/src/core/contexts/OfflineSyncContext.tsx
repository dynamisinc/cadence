/**
 * OfflineSyncContext
 *
 * Provides app-level offline sync management. This context:
 * - Monitors connectivity state changes
 * - Auto-syncs pending actions when coming back online
 * - Manages conflict state for display
 * - Persists across page navigation
 *
 * Usage:
 * ```tsx
 * // In App.tsx - wrap with provider
 * <OfflineSyncProvider>
 *   <App />
 * </OfflineSyncProvider>
 *
 * // In components - access sync state
 * const { isSyncing, conflicts, clearConflicts, manualSync } = useOfflineSyncContext();
 * ```
 */

import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  useRef,
  useMemo,
  type ReactNode,
} from 'react'
import { toast } from 'react-toastify'
import { useQueryClient } from '@tanstack/react-query'
import { useConnectivity } from './ConnectivityContext'
import {
  syncPendingActions,
  getSyncStatus,
  getPendingActionCount,
  deleteFailedActions,
  type SyncProgress,
  type SyncResult,
  type ConflictInfo,
} from '../offline'

interface OfflineSyncContextValue {
  /** Current sync status */
  syncStatus: SyncProgress['status']
  /** Whether sync is in progress */
  isSyncing: boolean
  /** Current sync progress */
  progress: SyncProgress | null
  /** Conflicts from last sync */
  conflicts: ConflictInfo[]
  /** Last sync result */
  lastResult: SyncResult | null
  /** Trigger manual sync */
  manualSync: () => Promise<SyncResult>
  /** Clear conflicts (after user acknowledgment) - also deletes failed actions */
  clearConflicts: () => Promise<void>
}

const OfflineSyncContext = createContext<OfflineSyncContextValue | null>(null)

interface OfflineSyncProviderProps {
  children: ReactNode
  /** Delay before auto-sync (ms) */
  autoSyncDelay?: number
}

export const OfflineSyncProvider: React.FC<OfflineSyncProviderProps> = ({
  children,
  autoSyncDelay = 2000,
}) => {
  const queryClient = useQueryClient()
  const { connectivityState, setPendingCount } = useConnectivity()

  const [syncStatus, setSyncStatus] = useState<SyncProgress['status']>('idle')
  const [progress, setProgress] = useState<SyncProgress | null>(null)
  const [conflicts, setConflicts] = useState<ConflictInfo[]>([])
  const [lastResult, setLastResult] = useState<SyncResult | null>(null)

  const wasOfflineRef = useRef(false)
  const syncTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const hasInitializedRef = useRef(false)
  const isMountedRef = useRef(true)

  // Track mounted state to prevent state updates after unmount
  useEffect(() => {
    isMountedRef.current = true
    return () => {
      isMountedRef.current = false
    }
  }, [])

  // Refresh pending count from IndexedDB
  const refreshPendingCount = useCallback(async () => {
    try {
      const count = await getPendingActionCount()
      setPendingCount(count)
    } catch (error) {
      console.error('Failed to get pending action count:', error)
    }
  }, [setPendingCount])

  // Initial pending count on mount
  useEffect(() => {
    if (!hasInitializedRef.current) {
      hasInitializedRef.current = true
      refreshPendingCount()
    }
  }, [refreshPendingCount])

  // Sync function
  const sync = useCallback(async (): Promise<SyncResult> => {
    // Don't start if already syncing
    if (getSyncStatus() === 'syncing') {
      return {
        totalActions: 0,
        succeeded: 0,
        failed: 0,
        failedActions: [],
      }
    }

    setSyncStatus('syncing')
    setProgress({ status: 'syncing', current: 0, total: 0, conflicts: [] })

    // Track the latest progress to capture conflicts
    let latestProgress: SyncProgress = { status: 'syncing', current: 0, total: 0, conflicts: [] }

    const handleProgress = (prog: SyncProgress) => {
      latestProgress = prog
      setProgress(prog)
      setSyncStatus(prog.status)
    }

    try {
      const result = await syncPendingActions(undefined, handleProgress)
      setLastResult(result)

      // Update pending count after sync
      await refreshPendingCount()

      // Handle results
      if (result.failed > 0) {
        // Use latestProgress which has the final conflicts
        setConflicts(latestProgress.conflicts)

        if (result.succeeded === 0) {
          toast.error(`Sync failed. ${result.failed} action(s) could not be synced.`, {
            autoClose: 5000,
          })
        } else {
          toast.warning(
            `Partial sync: ${result.succeeded} of ${result.totalActions} action(s) synced. ${result.failed} failed.`,
            { autoClose: 5000 },
          )
        }
      } else if (result.succeeded > 0) {
        toast.success(`All ${result.succeeded} change(s) synced successfully!`, {
          autoClose: 3000,
        })
      }

      // Invalidate all queries to refresh data from server
      // This ensures all pages get fresh data after sync
      await queryClient.invalidateQueries()

      return result
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error'
      toast.error(`Sync error: ${errorMessage}`, { autoClose: 5000 })
      setSyncStatus('failed')
      return {
        totalActions: 0,
        succeeded: 0,
        failed: 0,
        failedActions: [],
      }
    }
  }, [queryClient, refreshPendingCount])

  // Auto-sync on reconnect
  useEffect(() => {
    // Track if we were offline or reconnecting (backend down but browser online)
    if (connectivityState === 'offline' || connectivityState === 'reconnecting') {
      wasOfflineRef.current = true
    }

    // Trigger sync when coming back online
    if (
      wasOfflineRef.current &&
      connectivityState === 'online' &&
      getSyncStatus() !== 'syncing'
    ) {
      // Clear any existing timeout
      if (syncTimeoutRef.current) {
        clearTimeout(syncTimeoutRef.current)
      }

      // Delay sync to ensure stable connection
      syncTimeoutRef.current = setTimeout(async () => {
        // Check if still mounted before proceeding
        if (!isMountedRef.current) return

        try {
          const count = await getPendingActionCount()
          if (count > 0 && isMountedRef.current) {
            toast.info(`Syncing ${count} pending change(s)...`, { autoClose: 2000 })
            await sync()
          }
        } catch (error) {
          console.error('Auto-sync failed:', error)
        }
        if (isMountedRef.current) {
          wasOfflineRef.current = false
        }
      }, autoSyncDelay)
    }

    return () => {
      if (syncTimeoutRef.current) {
        clearTimeout(syncTimeoutRef.current)
      }
    }
  }, [connectivityState, autoSyncDelay, sync])

  // Clear conflicts and delete failed actions from IndexedDB
  const clearConflicts = useCallback(async () => {
    setConflicts([])
    // Delete failed actions so they don't reappear on page refresh
    await deleteFailedActions()
    // Refresh pending count (should now be 0 for conflict-only failures)
    await refreshPendingCount()
  }, [refreshPendingCount])

  const value = useMemo(
    () => ({
      syncStatus,
      isSyncing: syncStatus === 'syncing',
      progress,
      conflicts,
      lastResult,
      manualSync: sync,
      clearConflicts,
    }),
    [syncStatus, progress, conflicts, lastResult, sync, clearConflicts],
  )

  return (
    <OfflineSyncContext.Provider value={value}>
      {children}
    </OfflineSyncContext.Provider>
  )
}

/**
 * Hook to access offline sync state
 */
export const useOfflineSyncContext = (): OfflineSyncContextValue => {
  const context = useContext(OfflineSyncContext)
  if (!context) {
    throw new Error('useOfflineSyncContext must be used within an OfflineSyncProvider')
  }
  return context
}

export default OfflineSyncContext
