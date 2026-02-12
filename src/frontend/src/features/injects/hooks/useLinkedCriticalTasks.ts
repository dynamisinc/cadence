/**
 * useLinkedCriticalTasks Hook
 *
 * React Query hooks for managing critical task links from the inject side.
 * Provides fetch and update capabilities for inject→task relationships.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { injectService } from '../services/injectService'
import { criticalTaskKeys } from '../../eeg/hooks/useCriticalTasks'

/** Query key factory for inject linked critical tasks */
export const linkedCriticalTaskKeys = {
  all: ['inject-critical-tasks'] as const,
  byInject: (exerciseId: string, injectId: string) =>
    [...linkedCriticalTaskKeys.all, exerciseId, injectId] as const,
}

/**
 * Hook for managing linked critical tasks for an inject
 *
 * @param exerciseId The exercise ID
 * @param injectId The inject ID (optional - hook is disabled when undefined)
 */
export const useLinkedCriticalTasks = (exerciseId: string, injectId?: string) => {
  const queryClient = useQueryClient()
  const queryKey = linkedCriticalTaskKeys.byInject(exerciseId, injectId ?? '')

  // Query for fetching linked critical task IDs
  const {
    data: linkedTaskIds = [],
    isLoading: loading,
    error,
  } = useQuery({
    queryKey,
    queryFn: () => injectService.getLinkedCriticalTasks(exerciseId, injectId!),
    enabled: !!exerciseId && !!injectId,
  })

  // Mutation for setting linked critical tasks
  const setLinkedTasksMutation = useMutation({
    mutationFn: (criticalTaskIds: string[]) =>
      injectService.setLinkedCriticalTasks(exerciseId, injectId!, criticalTaskIds),
    onSuccess: () => {
      // Invalidate the inject's linked tasks
      queryClient.invalidateQueries({ queryKey })
      // Also invalidate critical task queries to update inject counts
      queryClient.invalidateQueries({ queryKey: criticalTaskKeys.all })
      notify.success('Linked Critical Tasks updated')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to update linked critical tasks'
      notify.error(message)
    },
  })

  const setLinkedTasks = async (criticalTaskIds: string[]) => {
    if (!injectId) return
    return setLinkedTasksMutation.mutateAsync(criticalTaskIds)
  }

  return {
    linkedTaskIds,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load linked critical tasks'
      : null,
    setLinkedTasks,
    isUpdating: setLinkedTasksMutation.isPending,
  }
}

export default useLinkedCriticalTasks
