import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { objectiveService } from '../services/objectiveService'
import { setupProgressQueryKey } from '../../exercises/hooks/useSetupProgress'
import type {
  ObjectiveDto,
  CreateObjectiveRequest,
  UpdateObjectiveRequest,
} from '../types'

/** Query key factory for objectives */
export const objectiveKeys = {
  all: (exerciseId: string) => ['exercises', exerciseId, 'objectives'] as const,
  summaries: (exerciseId: string) => ['exercises', exerciseId, 'objectives', 'summaries'] as const,
  detail: (exerciseId: string, id: string) =>
    ['exercises', exerciseId, 'objectives', id] as const,
}

/**
 * Hook for managing objective list state and operations using React Query
 *
 * Features:
 * - Automatic caching and background refetching
 * - Optimistic updates for create/update/delete
 * - Error handling with toast notifications
 */
export const useObjectives = (exerciseId: string) => {
  const queryClient = useQueryClient()
  const queryKey = objectiveKeys.all(exerciseId)

  // Query for fetching objectives
  const {
    data: objectives = [],
    isLoading: loading,
    error,
    refetch: fetchObjectives,
  } = useQuery({
    queryKey,
    queryFn: () => objectiveService.getObjectives(exerciseId),
    enabled: !!exerciseId,
  })

  // Mutation for creating objectives
  const createMutation = useMutation({
    mutationFn: (request: CreateObjectiveRequest) =>
      objectiveService.createObjective(exerciseId, request),
    onSuccess: newObjective => {
      queryClient.setQueryData<ObjectiveDto[]>(queryKey, (old = []) => [
        ...old,
        newObjective,
      ])
      // Also invalidate summaries and setup progress
      queryClient.invalidateQueries({
        queryKey: objectiveKeys.summaries(exerciseId),
      })
      queryClient.invalidateQueries({
        queryKey: setupProgressQueryKey(exerciseId),
      })
      notify.success('Objective created')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to create objective'
      notify.error(message)
    },
  })

  // Mutation for updating objectives
  const updateMutation = useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string
      request: UpdateObjectiveRequest
    }) => objectiveService.updateObjective(exerciseId, id, request),
    onMutate: async ({ id, request }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousObjectives = queryClient.getQueryData<ObjectiveDto[]>(queryKey)

      queryClient.setQueryData<ObjectiveDto[]>(queryKey, (old = []) =>
        old.map(objective =>
          objective.id === id
            ? { ...objective, ...request, updatedAt: new Date().toISOString() }
            : objective,
        ),
      )

      return { previousObjectives }
    },
    onSuccess: updatedObjective => {
      queryClient.setQueryData<ObjectiveDto[]>(queryKey, (old = []) =>
        old.map(objective =>
          objective.id === updatedObjective.id ? updatedObjective : objective,
        ),
      )
      // Also invalidate summaries and setup progress
      queryClient.invalidateQueries({
        queryKey: objectiveKeys.summaries(exerciseId),
      })
      queryClient.invalidateQueries({
        queryKey: setupProgressQueryKey(exerciseId),
      })
      notify.success('Objective updated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousObjectives) {
        queryClient.setQueryData(queryKey, context.previousObjectives)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update objective'
      notify.error(message)
    },
  })

  // Mutation for deleting objectives
  const deleteMutation = useMutation({
    mutationFn: (id: string) => objectiveService.deleteObjective(exerciseId, id),
    onMutate: async id => {
      await queryClient.cancelQueries({ queryKey })
      const previousObjectives = queryClient.getQueryData<ObjectiveDto[]>(queryKey)

      queryClient.setQueryData<ObjectiveDto[]>(queryKey, (old = []) =>
        old.filter(objective => objective.id !== id),
      )

      return { previousObjectives }
    },
    onSuccess: () => {
      // Also invalidate summaries and setup progress
      queryClient.invalidateQueries({
        queryKey: objectiveKeys.summaries(exerciseId),
      })
      queryClient.invalidateQueries({
        queryKey: setupProgressQueryKey(exerciseId),
      })
      notify.success('Objective deleted')
    },
    onError: (err, _variables, context) => {
      if (context?.previousObjectives) {
        queryClient.setQueryData(queryKey, context.previousObjectives)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to delete objective'
      notify.error(message)
    },
  })

  // Wrapper functions
  const createObjective = async (request: CreateObjectiveRequest) => {
    return createMutation.mutateAsync(request)
  }

  const updateObjective = async (id: string, request: UpdateObjectiveRequest) => {
    return updateMutation.mutateAsync({ id, request })
  }

  const deleteObjective = async (id: string) => {
    return deleteMutation.mutateAsync(id)
  }

  return {
    objectives,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load objectives'
      : null,
    fetchObjectives,
    createObjective,
    updateObjective,
    deleteObjective,
    // Expose mutation states
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
  }
}

/**
 * Hook for fetching objective summaries (for dropdowns)
 */
export const useObjectiveSummaries = (exerciseId: string) => {
  const queryKey = objectiveKeys.summaries(exerciseId)

  const {
    data: summaries = [],
    isLoading: loading,
    error,
  } = useQuery({
    queryKey,
    queryFn: () => objectiveService.getObjectiveSummaries(exerciseId),
    enabled: !!exerciseId,
    staleTime: 30000, // Cache for 30 seconds
  })

  return {
    summaries,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load objectives'
      : null,
  }
}

export default useObjectives
