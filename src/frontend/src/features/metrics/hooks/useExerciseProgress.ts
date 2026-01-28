/**
 * useExerciseProgress Hook
 *
 * React Query hook for fetching real-time exercise progress (S01).
 * Used during conduct to show situational awareness.
 */

import { useQuery } from '@tanstack/react-query'
import { metricsService } from '../services/metricsService'
import type { ExerciseProgressDto } from '../types'

/**
 * Query key for exercise progress data
 */
export const exerciseProgressQueryKey = (exerciseId: string) => [
  'exercise-progress',
  exerciseId,
] as const

/**
 * Hook for fetching real-time exercise progress during conduct.
 *
 * @param exerciseId - The exercise ID to fetch progress for
 * @param options - Optional query configuration
 * @returns Query result with progress data
 */
export const useExerciseProgress = (
  exerciseId: string,
  options?: {
    /** Enable/disable the query (default: true when exerciseId is present) */
    enabled?: boolean
    /** Refetch interval in milliseconds (default: 5000 for real-time updates) */
    refetchInterval?: number | false
  },
) => {
  const { enabled = !!exerciseId, refetchInterval = 5000 } = options ?? {}

  return useQuery<ExerciseProgressDto>({
    queryKey: exerciseProgressQueryKey(exerciseId),
    queryFn: () => metricsService.getExerciseProgress(exerciseId),
    enabled: enabled && !!exerciseId,
    staleTime: 2000, // Consider data fresh for 2 seconds
    refetchInterval, // Refetch every 5 seconds for real-time updates
  })
}

export default useExerciseProgress
