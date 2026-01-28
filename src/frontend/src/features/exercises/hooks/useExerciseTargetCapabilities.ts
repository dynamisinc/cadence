/**
 * useExerciseTargetCapabilities Hook
 *
 * React Query hook for fetching exercise target capabilities (S04).
 * Provides the list of capabilities that are being evaluated in an exercise.
 */

import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/core/services/api'
import type { CapabilityDto } from '@/features/capabilities/types'

/** Query key factory for exercise target capabilities */
export const exerciseTargetCapabilitiesQueryKey = (exerciseId: string) =>
  ['exercises', exerciseId, 'capabilities'] as const

/**
 * Fetch target capabilities for an exercise
 */
const fetchTargetCapabilities = async (
  exerciseId: string,
): Promise<CapabilityDto[]> => {
  const response = await apiClient.get<CapabilityDto[]>(
    `/exercises/${exerciseId}/capabilities`,
  )
  return response.data
}

/**
 * Hook to fetch exercise target capabilities
 *
 * Features:
 * - Automatic caching with React Query
 * - Background refetching
 * - Error handling
 */
export const useExerciseTargetCapabilities = (exerciseId: string | undefined) => {
  const {
    data: targetCapabilities = [],
    isLoading: loading,
    error,
    refetch,
  } = useQuery({
    queryKey: exerciseTargetCapabilitiesQueryKey(exerciseId ?? ''),
    queryFn: () => fetchTargetCapabilities(exerciseId!),
    enabled: !!exerciseId,
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes (reference data)
  })

  return {
    targetCapabilities,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load target capabilities'
      : null,
    refetch,
  }
}

export default useExerciseTargetCapabilities
