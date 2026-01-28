/**
 * useExerciseCapabilities Hook
 *
 * React Query hooks for exercise target capability management (S04).
 * Provides optimistic updates and cache management for target capabilities.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { exerciseCapabilityService } from '../services/exerciseCapabilityService'

/** Query key factory for exercise capabilities */
export const exerciseCapabilityKeys = {
  targetCapabilities: (exerciseId: string) => ['exercises', exerciseId, 'capabilities'] as const,
  summary: (exerciseId: string) => ['exercises', exerciseId, 'capabilities', 'summary'] as const,
}

/**
 * Hook to fetch target capabilities for an exercise
 * @param exerciseId Exercise ID
 * @returns Query result with target capabilities
 */
export const useExerciseTargetCapabilities = (exerciseId: string | undefined) => {
  return useQuery({
    queryKey: exerciseCapabilityKeys.targetCapabilities(exerciseId!),
    queryFn: () => exerciseCapabilityService.getTargetCapabilities(exerciseId!),
    enabled: !!exerciseId,
    staleTime: 2 * 60 * 1000, // Cache for 2 minutes
  })
}

/**
 * Hook to set target capabilities for an exercise
 * Updates cache optimistically and shows toast on success/error
 * @param exerciseId Exercise ID
 * @returns Mutation with loading state and mutate function
 */
export const useSetExerciseCapabilities = (exerciseId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (capabilityIds: string[]) =>
      exerciseCapabilityService.setTargetCapabilities(exerciseId, capabilityIds),
    onSuccess: () => {
      // Invalidate exercise capabilities query
      queryClient.invalidateQueries({
        queryKey: exerciseCapabilityKeys.targetCapabilities(exerciseId),
      })
      // Invalidate exercise detail to refresh coverage info
      queryClient.invalidateQueries({
        queryKey: ['exercises', exerciseId],
      })
      // Invalidate summary
      queryClient.invalidateQueries({
        queryKey: exerciseCapabilityKeys.summary(exerciseId),
      })
      toast.success('Target capabilities updated')
    },
    onError: (err: Error) => {
      const message = err.message || 'Failed to update target capabilities'
      toast.error(message)
    },
  })
}

/**
 * Hook to fetch capability coverage summary for an exercise
 * Shows how many target capabilities have been evaluated
 * @param exerciseId Exercise ID
 * @returns Query result with coverage summary
 */
export const useExerciseCapabilitySummary = (exerciseId: string | undefined) => {
  return useQuery({
    queryKey: exerciseCapabilityKeys.summary(exerciseId!),
    queryFn: () => exerciseCapabilityService.getCapabilitySummary(exerciseId!),
    enabled: !!exerciseId,
    staleTime: 1 * 60 * 1000, // Cache for 1 minute (changes with observations)
  })
}

export default useExerciseTargetCapabilities
