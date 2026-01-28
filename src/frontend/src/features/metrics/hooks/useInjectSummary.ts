/**
 * useInjectSummary Hook
 *
 * React Query hook for fetching inject delivery statistics (S02).
 * Used in AAR (after-action review) to analyze inject timing.
 */

import { useQuery } from '@tanstack/react-query'
import { metricsService } from '../services/metricsService'
import type { InjectSummaryDto } from '../types'

/**
 * Query key for inject summary data
 */
export const injectSummaryQueryKey = (exerciseId: string, tolerance: number) => [
  'inject-summary',
  exerciseId,
  tolerance,
] as const

/**
 * Hook for fetching inject delivery statistics for AAR.
 *
 * @param exerciseId - The exercise ID to fetch metrics for
 * @param onTimeToleranceMinutes - Minutes tolerance for on-time calculation (default: 5)
 * @returns Query result with inject summary data
 */
export const useInjectSummary = (
  exerciseId: string,
  onTimeToleranceMinutes: number = 5,
) => {
  return useQuery<InjectSummaryDto>({
    queryKey: injectSummaryQueryKey(exerciseId, onTimeToleranceMinutes),
    queryFn: () => metricsService.getInjectSummary(exerciseId, onTimeToleranceMinutes),
    enabled: !!exerciseId,
    staleTime: 60000, // Data doesn't change frequently for completed exercises
  })
}

export default useInjectSummary
