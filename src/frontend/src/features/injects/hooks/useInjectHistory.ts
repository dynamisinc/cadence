import { useQuery } from '@tanstack/react-query'
import { injectService } from '../services/injectService'
import type { InjectStatusHistoryDto } from '../types'

/**
 * Hook to fetch status change history for an inject (audit trail).
 * Used on the inject detail page to show a timeline of status transitions.
 */
export const useInjectHistory = (exerciseId: string, injectId: string) => {
  const {
    data: history = [],
    isLoading: loading,
    error,
  } = useQuery<InjectStatusHistoryDto[]>({
    queryKey: ['injects', exerciseId, injectId, 'history'],
    queryFn: () => injectService.getInjectHistory(exerciseId, injectId),
    enabled: !!exerciseId && !!injectId,
  })

  return { history, loading, error }
}
