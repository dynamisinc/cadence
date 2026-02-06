/**
 * useEegSignalR Hook
 *
 * Subscribes to real-time EEG entry notifications via SignalR.
 * Automatically invalidates React Query cache when entries change.
 * Returns a "new entries available" state for UI feedback.
 */

import { useState, useEffect, useCallback, useRef } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { useSignalR } from '@/shared/hooks'
import { eegEntryKeys } from './useEegEntries'
import type { EegEntryDto } from '../types'

interface UseEegSignalROptions {
  /** Exercise ID to listen for updates */
  exerciseId: string
  /** Whether to auto-refresh on new entries (default: false) */
  autoRefresh?: boolean
  /** Callback when a new entry is created */
  onEntryCreated?: (entry: EegEntryDto) => void
  /** Callback when an entry is updated */
  onEntryUpdated?: (entry: EegEntryDto) => void
  /** Callback when an entry is deleted */
  onEntryDeleted?: (entryId: string) => void
}

interface UseEegSignalRReturn {
  /** Whether new entries are available since last refresh */
  hasNewEntries: boolean
  /** Number of new entries since last refresh */
  newEntryCount: number
  /** Refresh the entries list (clears hasNewEntries) */
  refresh: () => void
  /** Dismiss the new entries notification without refreshing */
  dismiss: () => void
  /** SignalR connection state */
  connectionState: 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error'
}

/**
 * Hook for subscribing to real-time EEG entry updates
 */
export const useEegSignalR = ({
  exerciseId,
  autoRefresh = false,
  onEntryCreated,
  onEntryUpdated,
  onEntryDeleted,
}: UseEegSignalROptions): UseEegSignalRReturn => {
  const queryClient = useQueryClient()
  const { connection, connectionState, on, off } = useSignalR()

  const [hasNewEntries, setHasNewEntries] = useState(false)
  const [newEntryCount, setNewEntryCount] = useState(0)

  // Track callbacks in refs to avoid re-subscribing
  const onCreatedRef = useRef(onEntryCreated)
  const onUpdatedRef = useRef(onEntryUpdated)
  const onDeletedRef = useRef(onEntryDeleted)

  useEffect(() => {
    onCreatedRef.current = onEntryCreated
    onUpdatedRef.current = onEntryUpdated
    onDeletedRef.current = onEntryDeleted
  })

  // Invalidate queries
  const invalidateQueries = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: eegEntryKeys.byExercise(exerciseId) })
    queryClient.invalidateQueries({ queryKey: eegEntryKeys.coverage(exerciseId) })
  }, [queryClient, exerciseId])

  // Refresh function
  const refresh = useCallback(() => {
    invalidateQueries()
    setHasNewEntries(false)
    setNewEntryCount(0)
  }, [invalidateQueries])

  // Dismiss notification
  const dismiss = useCallback(() => {
    setHasNewEntries(false)
    setNewEntryCount(0)
  }, [])

  // Handle entry created
  const handleEntryCreated = useCallback(
    (entry: EegEntryDto) => {
      if (autoRefresh) {
        invalidateQueries()
      } else {
        setHasNewEntries(true)
        setNewEntryCount(prev => prev + 1)
      }
      onCreatedRef.current?.(entry)
    },
    [autoRefresh, invalidateQueries],
  )

  // Handle entry updated
  const handleEntryUpdated = useCallback(
    (entry: EegEntryDto) => {
      // Always invalidate on update to ensure data consistency
      invalidateQueries()
      onUpdatedRef.current?.(entry)
    },
    [invalidateQueries],
  )

  // Handle entry deleted
  const handleEntryDeleted = useCallback(
    (entryId: string) => {
      // Always invalidate on delete
      invalidateQueries()
      onDeletedRef.current?.(entryId)
    },
    [invalidateQueries],
  )

  // Subscribe to SignalR events
  useEffect(() => {
    if (!connection || connectionState !== 'connected') return

    // Subscribe to events
    on<EegEntryDto>('EegEntryCreated', handleEntryCreated)
    on<EegEntryDto>('EegEntryUpdated', handleEntryUpdated)
    on<string>('EegEntryDeleted', handleEntryDeleted)

    return () => {
      // Unsubscribe on cleanup
      off('EegEntryCreated', handleEntryCreated as (...args: unknown[]) => void)
      off('EegEntryUpdated', handleEntryUpdated as (...args: unknown[]) => void)
      off('EegEntryDeleted', handleEntryDeleted as (...args: unknown[]) => void)
    }
  }, [
    connection,
    connectionState,
    on,
    off,
    handleEntryCreated,
    handleEntryUpdated,
    handleEntryDeleted,
  ])

  return {
    hasNewEntries,
    newEntryCount,
    refresh,
    dismiss,
    connectionState,
  }
}

export default useEegSignalR
