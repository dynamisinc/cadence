/**
 * useControllerActivity Hook (S07)
 *
 * React Query hook for fetching controller activity metrics.
 */

import { useQuery } from '@tanstack/react-query'
import { metricsService } from '../services/metricsService'
import type { ControllerActivitySummaryDto } from '../types'

export const controllerActivityQueryKey = (exerciseId: string) => [
  'exercises',
  exerciseId,
  'metrics',
  'controllers',
]

export const useControllerActivity = (
  exerciseId: string,
  onTimeToleranceMinutes: number = 5,
) => {
  return useQuery<ControllerActivitySummaryDto>({
    queryKey: [...controllerActivityQueryKey(exerciseId), onTimeToleranceMinutes],
    queryFn: () => metricsService.getControllerActivity(exerciseId, onTimeToleranceMinutes),
    enabled: !!exerciseId,
    staleTime: 30000, // 30 seconds
  })
}
