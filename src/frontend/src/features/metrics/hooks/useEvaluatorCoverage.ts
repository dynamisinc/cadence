/**
 * useEvaluatorCoverage Hook (S08)
 *
 * React Query hook for fetching evaluator coverage metrics.
 */

import { useQuery } from '@tanstack/react-query'
import { metricsService } from '../services/metricsService'
import type { EvaluatorCoverageSummaryDto } from '../types'

export const evaluatorCoverageQueryKey = (exerciseId: string) => [
  'exercises',
  exerciseId,
  'metrics',
  'evaluators',
]

export const useEvaluatorCoverage = (exerciseId: string) => {
  return useQuery<EvaluatorCoverageSummaryDto>({
    queryKey: evaluatorCoverageQueryKey(exerciseId),
    queryFn: () => metricsService.getEvaluatorCoverage(exerciseId),
    enabled: !!exerciseId,
    staleTime: 30000, // 30 seconds
  })
}
