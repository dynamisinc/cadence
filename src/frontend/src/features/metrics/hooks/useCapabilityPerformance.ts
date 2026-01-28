/**
 * useCapabilityPerformance Hook (S06)
 *
 * React Query hook for fetching core capability performance metrics.
 */

import { useQuery } from '@tanstack/react-query'
import { metricsService } from '../services/metricsService'
import type { CapabilityPerformanceSummaryDto } from '../types'

export const capabilityPerformanceQueryKey = (exerciseId: string) => [
  'exercises',
  exerciseId,
  'metrics',
  'capabilities',
]

export const useCapabilityPerformance = (exerciseId: string) => {
  return useQuery<CapabilityPerformanceSummaryDto>({
    queryKey: capabilityPerformanceQueryKey(exerciseId),
    queryFn: () => metricsService.getCapabilityPerformance(exerciseId),
    enabled: !!exerciseId,
    staleTime: 30000, // 30 seconds
  })
}
