import { useQuery } from '@tanstack/react-query'
import { injectService } from '../services/injectService'
import { injectKeys } from './useInjects'
import type { InjectDto } from '../types'

/**
 * Hook for fetching a single inject by ID
 */
export const useInject = (exerciseId: string, injectId: string) => {
  const {
    data: inject,
    isLoading: loading,
    error,
    refetch: fetchInject,
  } = useQuery<InjectDto>({
    queryKey: injectKeys.detail(exerciseId, injectId),
    queryFn: () => injectService.getInject(exerciseId, injectId),
    enabled: !!exerciseId && !!injectId,
  })

  return {
    inject,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load inject'
      : null,
    fetchInject,
  }
}

export default useInject
