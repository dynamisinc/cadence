/**
 * useObservationSummary Hook
 *
 * React Query hook for fetching observation statistics (S03).
 * Used in AAR (after-action review) to analyze P/S/M/U ratings and coverage.
 */

import { useQuery } from '@tanstack/react-query'
import { metricsService } from '../services/metricsService'
import type { ObservationSummaryDto } from '../types'

/**
 * Query key for observation summary data
 */
export const observationSummaryQueryKey = (exerciseId: string) => [
  'observation-summary',
  exerciseId,
] as const

/**
 * Hook for fetching observation statistics for AAR.
 *
 * @param exerciseId - The exercise ID to fetch metrics for
 * @returns Query result with observation summary data
 */
export const useObservationSummary = (exerciseId: string) => {
  return useQuery<ObservationSummaryDto>({
    queryKey: observationSummaryQueryKey(exerciseId),
    queryFn: () => metricsService.getObservationSummary(exerciseId),
    enabled: !!exerciseId,
    staleTime: 60000, // Data doesn't change frequently for completed exercises
  })
}

export default useObservationSummary
