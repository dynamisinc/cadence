/**
 * useExerciseConductSignalR
 *
 * Wires up all SignalR event subscriptions for the exercise conduct page and
 * synchronizes the global connectivity context with the current connection state.
 *
 * Uses the AR-P02 pattern: SignalR events are treated as notifications that server
 * state changed; they invalidate React Query caches rather than updating them directly.
 *
 * @module features/exercises
 */

import { useCallback, useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { useConnectivity } from '../../../core/contexts'
import { useExerciseSignalR } from '../../../shared/hooks'
import { clockQueryKey } from '../../exercise-clock'
import { injectKeys } from '../../injects'
import { observationsQueryKey } from '../../observations'
import {
  useReconnectionHandler,
  useConnectionStateSync,
} from './useReconnectionHandler'
import type { ExerciseClockState } from '../../../types'

interface UseExerciseConductSignalRParams {
  /** The exercise ID to join */
  exerciseId: string
  /** Current clock state (passed to reconnection handler for drift detection) */
  clockState: { state: ExerciseClockState } | null | undefined
  /** Current elapsed time in ms (captured on disconnect) */
  elapsedTimeMs: number
}

export interface UseExerciseConductSignalRReturn {
  /** Current SignalR connection state */
  connectionState: string | null
  /** Whether this client has joined the exercise group */
  isJoined: boolean
}

/**
 * Manages all SignalR subscriptions for the exercise conduct page.
 *
 * Subscribes to:
 *   - InjectFired / InjectStatusChanged → invalidates inject queries
 *   - ClockStarted / ClockPaused / ClockReset / ClockChanged → invalidates clock queries
 *   - ObservationAdded / ObservationUpdated / ObservationDeleted → invalidates observation queries
 *   - EegEntryCreated → invalidates EEG coverage queries
 *   - Reconnected → handled by useReconnectionHandler (clock drift detection + toast)
 *
 * Also syncs the global connectivity context with SignalR state.
 */
export const useExerciseConductSignalR = ({
  exerciseId,
  clockState,
  elapsedTimeMs,
}: UseExerciseConductSignalRParams): UseExerciseConductSignalRReturn => {
  const queryClient = useQueryClient()
  const { setSignalRState, setIsInExercise, setIsSignalRJoined } = useConnectivity()

  // Reconnection handler (clock drift detection)
  const {
    handleReconnected,
    previousElapsedTimeMsRef,
    disconnectedAtRef,
  } = useReconnectionHandler({ exerciseId, clockState, elapsedTimeMs })

  // SignalR event handlers — invalidate queries on change notifications
  const handleInjectFired = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: injectKeys.all(exerciseId) })
  }, [exerciseId, queryClient])

  const handleInjectStatusChanged = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: injectKeys.all(exerciseId) })
  }, [exerciseId, queryClient])

  const handleClockChanged = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: clockQueryKey(exerciseId) })
  }, [exerciseId, queryClient])

  const handleObservationAdded = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: observationsQueryKey(exerciseId) })
  }, [exerciseId, queryClient])

  const handleObservationUpdated = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: observationsQueryKey(exerciseId) })
  }, [exerciseId, queryClient])

  const handleObservationDeleted = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: observationsQueryKey(exerciseId) })
  }, [exerciseId, queryClient])

  // Connect to SignalR exercise group
  const { connectionState, isJoined } = useExerciseSignalR({
    exerciseId,
    onInjectFired: handleInjectFired,
    onInjectStatusChanged: handleInjectStatusChanged,
    onClockStarted: handleClockChanged,
    onClockPaused: handleClockChanged,
    onClockReset: handleClockChanged,
    onClockChanged: handleClockChanged,
    onObservationAdded: handleObservationAdded,
    onObservationUpdated: handleObservationUpdated,
    onObservationDeleted: handleObservationDeleted,
    onReconnected: handleReconnected,
    enabled: !!exerciseId,
  })

  // Mark that we're in exercise conduct mode for the global connectivity context
  useEffect(() => {
    setIsInExercise(true)
    return () => {
      setIsInExercise(false)
      setSignalRState(null)
      setIsSignalRJoined(false)
    }
  // setSignalRState, setIsInExercise, setIsSignalRJoined are stable (no state deps)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // Sync connection state changes with connectivity context and reconnection handler
  useConnectionStateSync({
    connectionState,
    isJoined,
    elapsedTimeMs,
    handleReconnected,
    previousElapsedTimeMsRef,
    disconnectedAtRef,
    setSignalRState,
    setIsSignalRJoined,
  })

  return { connectionState, isJoined }
}
