/**
 * useReconnectionHandler
 *
 * Encapsulates the SignalR reconnection logic for exercise conduct:
 * - Detects clock drift (state changes or elapsed time jumps) during offline periods
 * - Refetches server state on reconnect
 * - Notifies the user of significant changes that occurred while offline
 *
 * @module features/exercises
 */

import { useCallback, useRef, useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import type { SignalRState } from '../../../core/contexts/ConnectivityContext'
import { ExerciseClockState } from '../../../types'
import {
  clockQueryKey,
  parseElapsedTime,
  formatElapsedTime,
} from '../../exercise-clock'
import { injectKeys } from '../../injects'
import { observationsQueryKey } from '../../observations'
import type { ExerciseClockDto } from '../../exercise-clock/types'

interface UseReconnectionHandlerParams {
  /** The exercise ID */
  exerciseId: string
  /** Current clock state from the clock hook (used for state tracking) */
  clockState: { state: ExerciseClockState } | null | undefined
  /** Current elapsed time in ms (captured on disconnect) */
  elapsedTimeMs: number
}

export interface UseReconnectionHandlerReturn {
  /** Call when SignalR reconnects — refetches data and notifies of clock drift */
  handleReconnected: () => Promise<void>
  /** Ref to track clock state before disconnect */
  previousClockStateRef: React.MutableRefObject<ExerciseClockState | null>
  /** Ref to track elapsed time before disconnect */
  previousElapsedTimeMsRef: React.MutableRefObject<number>
  /** Ref to track when we disconnected */
  disconnectedAtRef: React.MutableRefObject<number | null>
}

/**
 * Manages SignalR reconnection handling for the exercise conduct page.
 *
 * On reconnect:
 *   1. Refreshes clock, injects, and observations from the server
 *   2. Compares new clock state/time to pre-disconnect state
 *   3. Shows appropriate toast notifications for state changes or large time jumps
 */
export const useReconnectionHandler = ({
  exerciseId,
  clockState,
  elapsedTimeMs: _elapsedTimeMs,
}: UseReconnectionHandlerParams): UseReconnectionHandlerReturn => {
  const queryClient = useQueryClient()

  // Track clock state before disconnect
  const previousClockStateRef = useRef<ExerciseClockState | null>(null)
  const previousElapsedTimeMsRef = useRef<number>(0)
  const disconnectedAtRef = useRef<number | null>(null)

  // Keep previousClockStateRef in sync with live clock state
  useEffect(() => {
    if (clockState?.state) {
      previousClockStateRef.current = clockState.state
    }
  }, [clockState?.state])

  const handleReconnected = useCallback(async () => {
    const previousState = previousClockStateRef.current
    const previousElapsedMs = previousElapsedTimeMsRef.current
    const wasDisconnectedAt = disconnectedAtRef.current

    // Clear the disconnected timestamp
    disconnectedAtRef.current = null

    // Refresh clock, inject, and observation data — wait for completion before
    // reading fresh data to compare against pre-disconnect state
    await Promise.all([
      queryClient.refetchQueries({ queryKey: clockQueryKey(exerciseId) }),
      queryClient.refetchQueries({ queryKey: injectKeys.all(exerciseId) }),
      queryClient.refetchQueries({ queryKey: observationsQueryKey(exerciseId) }),
    ])

    // Read fresh data after refetch
    const currentClockData = queryClient.getQueryData<ExerciseClockDto>(clockQueryKey(exerciseId))
    const currentState = currentClockData?.state
    const currentElapsedMs = currentClockData?.elapsedTime
      ? parseElapsedTime(currentClockData.elapsedTime)
      : 0

    // Calculate time delta for informative message
    const timeDeltaMs = currentElapsedMs - previousElapsedMs
    const timeDeltaFormatted = formatElapsedTime(Math.abs(timeDeltaMs))

    // Calculate how long we were disconnected
    const disconnectedDuration = wasDisconnectedAt ? Date.now() - wasDisconnectedAt : 0
    // Only show notification if offline > 2 seconds
    const wasDisconnectedLongEnough = disconnectedDuration > 2000

    if (!wasDisconnectedLongEnough) {
      // Brief disconnection — update refs silently without toast
      if (currentState) {
        previousClockStateRef.current = currentState
      }
      previousElapsedTimeMsRef.current = currentElapsedMs
      return
    }

    if (previousState && currentState && previousState !== currentState) {
      // Clock state changed while offline
      if (currentState === ExerciseClockState.Running) {
        const currentTimeStr = formatElapsedTime(currentElapsedMs)
        const message = previousState === ExerciseClockState.Stopped
          ? `Exercise clock was started while you were offline. Current time: ${currentTimeStr}`
          : 'Exercise clock resumed while you were offline. ' +
            `Clock jumped forward by ${timeDeltaFormatted}. Current time: ${currentTimeStr}`
        notify.warning(message, { autoClose: false })
      } else if (
        currentState === ExerciseClockState.Paused &&
        previousState === ExerciseClockState.Running
      ) {
        notify.warning(
          `Exercise clock was paused while you were offline at ${formatElapsedTime(currentElapsedMs)}.`,
          { autoClose: 8000 },
        )
      } else if (currentState === ExerciseClockState.Stopped) {
        notify.warning(
          'Exercise was stopped while you were offline. The exercise has ended.',
          { autoClose: false },
        )
      }
    } else if (currentState === ExerciseClockState.Running && timeDeltaMs > 5000) {
      // Same state but significant time jump (>5 seconds) while offline
      notify.warning(
        `Clock synchronized. Time jumped forward by ${timeDeltaFormatted}. Current time: ${formatElapsedTime(currentElapsedMs)}`,
        { autoClose: false },
      )
    } else if (!previousState && currentState === ExerciseClockState.Running) {
      // First time connecting and clock is already running
      notify.info(
        `Exercise clock is running. Current time: ${formatElapsedTime(currentElapsedMs)}`,
        { autoClose: 5000 },
      )
    }

    // Update refs with current state
    if (currentState) {
      previousClockStateRef.current = currentState
    }
    previousElapsedTimeMsRef.current = currentElapsedMs
  }, [exerciseId, queryClient])

  return {
    handleReconnected,
    previousClockStateRef,
    previousElapsedTimeMsRef,
    disconnectedAtRef,
  }
}

/**
 * Hook that syncs SignalR connection state changes with the reconnection handler.
 *
 * Captures elapsed time on disconnect and triggers handleReconnected on
 * transition from disconnected/error/reconnecting → connected.
 */
export const useConnectionStateSync = ({
  connectionState,
  isJoined,
  elapsedTimeMs,
  handleReconnected,
  previousElapsedTimeMsRef,
  disconnectedAtRef,
  setSignalRState,
  setIsSignalRJoined,
}: {
  connectionState: SignalRState | null
  isJoined: boolean
  elapsedTimeMs: number
  handleReconnected: () => Promise<void>
  previousElapsedTimeMsRef: React.MutableRefObject<number>
  disconnectedAtRef: React.MutableRefObject<number | null>
  setSignalRState: (state: SignalRState | null) => void
  setIsSignalRJoined: (joined: boolean) => void
}) => {
  const previousConnectionStateRef = useRef<string | null>(null)

  useEffect(() => {
    setSignalRState(connectionState)
    setIsSignalRJoined(isJoined)

    const wasConnected = previousConnectionStateRef.current === 'connected'
    const isNowDisconnected =
      connectionState === 'disconnected' ||
      connectionState === 'error' ||
      connectionState === 'reconnecting'
    const wasDisconnected =
      previousConnectionStateRef.current === 'disconnected' ||
      previousConnectionStateRef.current === 'error' ||
      previousConnectionStateRef.current === 'reconnecting'
    const isNowConnected = connectionState === 'connected'

    // Capture elapsed time when we go offline
    if (wasConnected && isNowDisconnected) {
      previousElapsedTimeMsRef.current = elapsedTimeMs
      disconnectedAtRef.current = Date.now()
    }

    // Trigger reconnection handler when transitioning from disconnected/error → connected.
    // Handles cases where the SignalR auto-reconnect doesn't fire the onreconnected callback.
    if (wasDisconnected && isNowConnected && previousConnectionStateRef.current !== null) {
      handleReconnected()
    }

    previousConnectionStateRef.current = connectionState
  }, [
    connectionState,
    isJoined,
    elapsedTimeMs,
    handleReconnected,
    previousElapsedTimeMsRef,
    disconnectedAtRef,
    setSignalRState,
    setIsSignalRJoined,
  ])
}
