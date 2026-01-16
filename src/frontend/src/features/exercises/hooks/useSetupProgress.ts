import { useQuery } from '@tanstack/react-query'
import { exerciseService } from '../services/exerciseService'
import type { SetupProgressDto } from '../types'

/**
 * Query key for setup progress data
 */
export const setupProgressQueryKey = (exerciseId: string) => [
  'setup-progress',
  exerciseId,
]

/**
 * Hook for fetching the setup progress for an exercise
 *
 * @param exerciseId - The exercise ID to fetch setup progress for
 * @returns Query result with setup progress data
 */
export const useSetupProgress = (exerciseId: string) => {
  return useQuery<SetupProgressDto>({
    queryKey: setupProgressQueryKey(exerciseId),
    queryFn: () => exerciseService.getSetupProgress(exerciseId),
    enabled: !!exerciseId,
    staleTime: 30000, // Consider data fresh for 30 seconds
  })
}

export default useSetupProgress
