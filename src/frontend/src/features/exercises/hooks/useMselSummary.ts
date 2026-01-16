import { useQuery } from '@tanstack/react-query'
import { exerciseService } from '../services/exerciseService'
import type { MselSummaryDto, MselDto } from '../types'

/**
 * Query key for MSEL summary data
 */
export const mselSummaryQueryKey = (exerciseId: string) => [
  'msel-summary',
  exerciseId,
]

/**
 * Query key for MSELs list
 */
export const mselsQueryKey = (exerciseId: string) => ['msels', exerciseId]

/**
 * Hook for fetching the active MSEL summary for an exercise
 *
 * @param exerciseId - The exercise ID to fetch MSEL summary for
 * @returns Query result with MSEL summary data
 */
export const useMselSummary = (exerciseId: string) => {
  return useQuery<MselSummaryDto>({
    queryKey: mselSummaryQueryKey(exerciseId),
    queryFn: () => exerciseService.getActiveMselSummary(exerciseId),
    enabled: !!exerciseId,
    staleTime: 30000, // Consider data fresh for 30 seconds
  })
}

/**
 * Hook for fetching all MSELs for an exercise
 *
 * @param exerciseId - The exercise ID to fetch MSELs for
 * @returns Query result with MSELs list
 */
export const useMsels = (exerciseId: string) => {
  return useQuery<MselDto[]>({
    queryKey: mselsQueryKey(exerciseId),
    queryFn: () => exerciseService.getMsels(exerciseId),
    enabled: !!exerciseId,
  })
}

export default useMselSummary
