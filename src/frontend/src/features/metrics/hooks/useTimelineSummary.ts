/**
 * useTimelineSummary Hook
 *
 * React Query hook for fetching timeline and duration analysis (S04).
 * Used in AAR (after-action review) to analyze pauses, phase timing, and inject pacing.
 */

import { useQuery } from '@tanstack/react-query'
import { metricsService } from '../services/metricsService'
import type { TimelineSummaryDto } from '../types'

/**
 * Query key for timeline summary data
 */
export const timelineSummaryQueryKey = (exerciseId: string) => [
  'timeline-summary',
  exerciseId,
] as const

/**
 * Hook for fetching timeline analysis for AAR.
 *
 * @param exerciseId - The exercise ID to fetch metrics for
 * @returns Query result with timeline summary data
 */
export const useTimelineSummary = (exerciseId: string) => {
  return useQuery<TimelineSummaryDto>({
    queryKey: timelineSummaryQueryKey(exerciseId),
    queryFn: () => metricsService.getTimelineSummary(exerciseId),
    enabled: !!exerciseId,
    staleTime: 60000, // Data doesn't change frequently for completed exercises
  })
}

export default useTimelineSummary
