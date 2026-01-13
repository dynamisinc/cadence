/**
 * useExerciseClock Hook
 *
 * React Query hook for managing exercise clock state and operations.
 * Includes real-time elapsed time calculation for running clocks.
 */

import { useState, useEffect, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { clockService } from '../services/clockService'
import { ExerciseClockState } from '../../../types'
import type { ClockStateDto } from '../types'
import { parseElapsedTime, formatElapsedTime } from '../types'

/** Query key for clock state */
export const clockQueryKey = (exerciseId: string) => ['clock', exerciseId] as const

/**
 * Hook for managing exercise clock
 */
export const useExerciseClock = (exerciseId: string) => {
  const queryClient = useQueryClient()
  const [displayTime, setDisplayTime] = useState('00:00:00')
  const [elapsedTimeMs, setElapsedTimeMs] = useState(0)

  // Query for fetching clock state
  const {
    data: clockState,
    isLoading: loading,
    error,
    refetch: fetchClockState,
  } = useQuery({
    queryKey: clockQueryKey(exerciseId),
    queryFn: () => clockService.getClockState(exerciseId),
    enabled: !!exerciseId,
    refetchInterval: false, // We'll handle real-time updates ourselves
  })

  // Real-time clock display update
  useEffect(() => {
    if (!clockState) {
      setDisplayTime('00:00:00')
      setElapsedTimeMs(0)
      return
    }

    const baseElapsed = parseElapsedTime(clockState.elapsedTime)

    if (clockState.state === ExerciseClockState.Running && clockState.startedAt) {
      // Update every second while running
      const capturedAt = new Date(clockState.capturedAt).getTime()

      const updateDisplay = () => {
        const now = Date.now()
        const additionalElapsed = now - capturedAt
        const totalElapsed = baseElapsed + additionalElapsed
        setDisplayTime(formatElapsedTime(totalElapsed))
        setElapsedTimeMs(totalElapsed)
      }

      updateDisplay()
      const interval = setInterval(updateDisplay, 1000)
      return () => clearInterval(interval)
    } else {
      // Clock is stopped or paused - just show the stored elapsed time
      setDisplayTime(formatElapsedTime(baseElapsed))
      setElapsedTimeMs(baseElapsed)
    }
  }, [clockState])

  // Helper to update clock state in cache
  const updateClockState = useCallback(
    (newState: ClockStateDto) => {
      queryClient.setQueryData(clockQueryKey(exerciseId), newState)
      // Also invalidate exercise query to update status
      queryClient.invalidateQueries({ queryKey: ['exercise', exerciseId] })
      queryClient.invalidateQueries({ queryKey: ['exercises'] })
    },
    [queryClient, exerciseId]
  )

  // Mutation for starting clock
  const startMutation = useMutation({
    mutationFn: () => clockService.startClock(exerciseId),
    onSuccess: (newState) => {
      updateClockState(newState)
      toast.success('Exercise clock started')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to start clock'
      toast.error(message)
    },
  })

  // Mutation for pausing clock
  const pauseMutation = useMutation({
    mutationFn: () => clockService.pauseClock(exerciseId),
    onSuccess: (newState) => {
      updateClockState(newState)
      toast.success('Exercise clock paused')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to pause clock'
      toast.error(message)
    },
  })

  // Mutation for stopping clock
  const stopMutation = useMutation({
    mutationFn: () => clockService.stopClock(exerciseId),
    onSuccess: (newState) => {
      updateClockState(newState)
      toast.success('Exercise completed')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to stop clock'
      toast.error(message)
    },
  })

  // Mutation for resetting clock
  const resetMutation = useMutation({
    mutationFn: () => clockService.resetClock(exerciseId),
    onSuccess: (newState) => {
      updateClockState(newState)
      toast.success('Exercise clock reset')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to reset clock'
      toast.error(message)
    },
  })

  // Wrapper functions
  const startClock = async () => {
    return startMutation.mutateAsync()
  }

  const pauseClock = async () => {
    return pauseMutation.mutateAsync()
  }

  const stopClock = async () => {
    return stopMutation.mutateAsync()
  }

  const resetClock = async () => {
    return resetMutation.mutateAsync()
  }

  return {
    clockState,
    displayTime,
    elapsedTimeMs,
    exerciseStartTime: clockState?.exerciseStartTime ?? null,
    isRunning: clockState?.state === ExerciseClockState.Running,
    isPaused: clockState?.state === ExerciseClockState.Paused,
    isStopped: clockState?.state === ExerciseClockState.Stopped,
    loading,
    error: error ? (error instanceof Error ? error.message : 'Failed to load clock state') : null,
    fetchClockState,
    startClock,
    pauseClock,
    stopClock,
    resetClock,
    isStarting: startMutation.isPending,
    isPausing: pauseMutation.isPending,
    isStopping: stopMutation.isPending,
    isResetting: resetMutation.isPending,
  }
}

export default useExerciseClock
