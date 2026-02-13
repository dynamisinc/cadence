/**
 * useOfflineSync Hook
 *
 * Provides offline sync functionality including:
 * - Triggering sync when connection is restored
 * - Tracking sync progress
 * - Managing conflicts
 */

import { useState, useEffect, useCallback, useRef } from 'react'
import { notify } from '@/shared/utils/notify'
import { useQueryClient } from '@tanstack/react-query'
import { useConnectivity } from '../contexts'
import {
  syncPendingActions,
  getSyncStatus,
  getPendingActionCount,
  type SyncProgress,
  type SyncResult,
  type ConflictInfo,
} from './index'

interface UseOfflineSyncOptions {
  /** Exercise ID to sync (optional - syncs all if not provided) */
  exerciseId?: string
  /** Whether to auto-sync on reconnect */
  autoSync?: boolean
  /** Delay before auto-sync (ms) */
  autoSyncDelay?: number
}

interface UseOfflineSyncReturn {
  /** Current sync status */
  syncStatus: SyncProgress['status']
  /** Whether sync is in progress */
  isSyncing: boolean
  /** Number of pending actions */
  pendingCount: number
  /** Current sync progress */
  progress: SyncProgress | null
  /** Conflicts from last sync */
  conflicts: ConflictInfo[]
  /** Last sync result */
  lastResult: SyncResult | null
  /** Trigger manual sync */
  sync: () => Promise<SyncResult>
  /** Clear conflicts (after user acknowledgment) */
  clearConflicts: () => void
}

export const useOfflineSync = (options: UseOfflineSyncOptions = {}): UseOfflineSyncReturn => {
  const { exerciseId, autoSync = true, autoSyncDelay = 2000 } = options

  const queryClient = useQueryClient()
  const { connectivityState, setPendingCount } = useConnectivity()

  const [syncStatus, setSyncStatus] = useState<SyncProgress['status']>('idle')
  const [progress, setProgress] = useState<SyncProgress | null>(null)
  const [conflicts, setConflicts] = useState<ConflictInfo[]>([])
  const [lastResult, setLastResult] = useState<SyncResult | null>(null)
  const [pendingCount, setLocalPendingCount] = useState(0)

  const wasOfflineRef = useRef(false)
  const syncTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Update pending count
  const refreshPendingCount = useCallback(async () => {
    const count = await getPendingActionCount(exerciseId)
    setLocalPendingCount(count)
    setPendingCount(count)
  }, [exerciseId, setPendingCount])

  // Initial pending count
  useEffect(() => {
    refreshPendingCount()
  }, [refreshPendingCount])

  // Sync function
  const sync = useCallback(async (): Promise<SyncResult> => {
    setSyncStatus('syncing')
    setProgress({ status: 'syncing', current: 0, total: 0, conflicts: [] })

    let latestConflicts: ConflictInfo[] = []
    const handleProgress = (prog: SyncProgress) => {
      setProgress(prog)
      setSyncStatus(prog.status)
      latestConflicts = prog.conflicts
    }

    try {
      const result = await syncPendingActions(exerciseId, handleProgress)
      setLastResult(result)

      // Update pending count after sync
      await refreshPendingCount()

      // Handle results
      if (result.failed > 0) {
        setConflicts(latestConflicts)

        if (result.succeeded === 0) {
          notify.error(`Sync failed. ${result.failed} action(s) could not be synced.`, {
            toastId: 'sync-result',
            autoClose: 5000,
          })
        } else {
          notify.warning(
            `Partial sync: ${result.succeeded} of ${result.totalActions} action(s) synced. ${result.failed} failed.`,
            { toastId: 'sync-result', autoClose: 5000 },
          )
        }
      } else if (result.succeeded > 0) {
        notify.success(`All ${result.succeeded} change(s) synced successfully!`, {
          toastId: 'sync-result',
          autoClose: 3000,
        })
      }

      // Invalidate queries to refresh data
      if (exerciseId) {
        queryClient.invalidateQueries({ queryKey: ['injects', exerciseId] })
        queryClient.invalidateQueries({ queryKey: ['observations', exerciseId] })
        queryClient.invalidateQueries({ queryKey: ['exercise', exerciseId] })
      } else {
        queryClient.invalidateQueries()
      }

      return result
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error'
      notify.error(`Sync error: ${errorMessage}`, { toastId: 'sync-error', autoClose: 5000 })
      setSyncStatus('failed')
      return {
        totalActions: 0,
        succeeded: 0,
        failed: 0,
        failedActions: [],
      }
    }
  }, [exerciseId, queryClient, refreshPendingCount])

  // Auto-sync on reconnect
  useEffect(() => {
    if (!autoSync) return

    // Track if we were offline
    if (connectivityState === 'offline') {
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
        const count = await getPendingActionCount(exerciseId)
        if (count > 0) {
          notify.info(`Syncing ${count} pending change(s)...`, { toastId: 'sync-progress', autoClose: 2000 })
          await sync()
        }
        wasOfflineRef.current = false
      }, autoSyncDelay)
    }

    return () => {
      if (syncTimeoutRef.current) {
        clearTimeout(syncTimeoutRef.current)
      }
    }
  }, [connectivityState, autoSync, autoSyncDelay, exerciseId, sync])

  // Clear conflicts
  const clearConflicts = useCallback(() => {
    setConflicts([])
  }, [])

  return {
    syncStatus,
    isSyncing: syncStatus === 'syncing',
    pendingCount,
    progress,
    conflicts,
    lastResult,
    sync,
    clearConflicts,
  }
}

export default useOfflineSync
