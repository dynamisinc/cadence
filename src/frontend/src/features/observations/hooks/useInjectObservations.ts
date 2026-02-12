import { useQuery } from '@tanstack/react-query'
import { observationService } from '../services/observationService'
import { observationsByInjectQueryKey } from './useObservations'
import type { ObservationDto } from '../types'

/**
 * Hook to fetch observations linked to a specific inject.
 * Used on the inject detail page to show evaluator observations.
 */
export const useInjectObservations = (injectId: string) => {
  const {
    data: observations = [],
    isLoading: loading,
    error,
  } = useQuery<ObservationDto[]>({
    queryKey: observationsByInjectQueryKey(injectId),
    queryFn: () => observationService.getObservationsByInject(injectId),
    enabled: !!injectId,
  })

  return { observations, loading, error }
}
