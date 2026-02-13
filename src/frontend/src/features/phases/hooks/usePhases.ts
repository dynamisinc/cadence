import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { phaseService } from '../services/phaseService'
import type {
  PhaseDto,
  CreatePhaseRequest,
  UpdatePhaseRequest,
} from '../types'

/** Query key factory for phases */
export const phaseKeys = {
  all: (exerciseId: string) => ['exercises', exerciseId, 'phases'] as const,
  detail: (exerciseId: string, id: string) =>
    ['exercises', exerciseId, 'phases', id] as const,
}

/**
 * Hook for managing phase list state and operations using React Query
 *
 * Features:
 * - Automatic caching and background refetching
 * - Optimistic updates for create/update/delete/reorder
 * - Error handling with toast notifications
 */
export const usePhases = (exerciseId: string) => {
  const queryClient = useQueryClient()
  const queryKey = phaseKeys.all(exerciseId)

  // Query for fetching phases
  const {
    data: phases = [],
    isLoading: loading,
    error,
    refetch: fetchPhases,
  } = useQuery({
    queryKey,
    queryFn: () => phaseService.getPhases(exerciseId),
    enabled: !!exerciseId,
  })

  // Mutation for creating phases
  const createMutation = useMutation({
    mutationFn: (request: CreatePhaseRequest) =>
      phaseService.createPhase(exerciseId, request),
    onSuccess: newPhase => {
      queryClient.setQueryData<PhaseDto[]>(queryKey, (old = []) => [
        ...old,
        newPhase,
      ])
      notify.success('Phase created')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to create phase'
      notify.error(message)
    },
  })

  // Mutation for updating phases
  const updateMutation = useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string
      request: UpdatePhaseRequest
    }) => phaseService.updatePhase(exerciseId, id, request),
    onMutate: async ({ id, request }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousPhases = queryClient.getQueryData<PhaseDto[]>(queryKey)

      queryClient.setQueryData<PhaseDto[]>(queryKey, (old = []) =>
        old.map(phase =>
          phase.id === id
            ? { ...phase, ...request, updatedAt: new Date().toISOString() }
            : phase,
        ),
      )

      return { previousPhases }
    },
    onSuccess: updatedPhase => {
      queryClient.setQueryData<PhaseDto[]>(queryKey, (old = []) =>
        old.map(phase =>
          phase.id === updatedPhase.id ? updatedPhase : phase,
        ),
      )
      notify.success('Phase updated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousPhases) {
        queryClient.setQueryData(queryKey, context.previousPhases)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update phase'
      notify.error(message)
    },
  })

  // Mutation for deleting phases
  const deleteMutation = useMutation({
    mutationFn: (id: string) => phaseService.deletePhase(exerciseId, id),
    onMutate: async id => {
      await queryClient.cancelQueries({ queryKey })
      const previousPhases = queryClient.getQueryData<PhaseDto[]>(queryKey)

      queryClient.setQueryData<PhaseDto[]>(queryKey, (old = []) =>
        old.filter(phase => phase.id !== id),
      )

      return { previousPhases }
    },
    onSuccess: () => {
      // Also invalidate injects query in case phase grouping changes
      queryClient.invalidateQueries({
        queryKey: ['exercises', exerciseId, 'injects'],
      })
      notify.success('Phase deleted')
    },
    onError: (err, _variables, context) => {
      if (context?.previousPhases) {
        queryClient.setQueryData(queryKey, context.previousPhases)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to delete phase'
      notify.error(message)
    },
  })

  // Mutation for reordering phases
  const reorderMutation = useMutation({
    mutationFn: (phaseIds: string[]) =>
      phaseService.reorderPhases(exerciseId, { phaseIds }),
    onMutate: async phaseIds => {
      await queryClient.cancelQueries({ queryKey })
      const previousPhases = queryClient.getQueryData<PhaseDto[]>(queryKey)

      // Optimistically reorder
      if (previousPhases) {
        const phaseMap = new Map(previousPhases.map(p => [p.id, p]))
        const reordered = phaseIds
          .map((id, index) => {
            const phase = phaseMap.get(id)
            return phase ? { ...phase, sequence: index + 1 } : null
          })
          .filter((p): p is PhaseDto => p !== null)

        queryClient.setQueryData<PhaseDto[]>(queryKey, reordered)
      }

      return { previousPhases }
    },
    onSuccess: updatedPhases => {
      queryClient.setQueryData<PhaseDto[]>(queryKey, updatedPhases)
      notify.success('Phases reordered')
    },
    onError: (err, _variables, context) => {
      if (context?.previousPhases) {
        queryClient.setQueryData(queryKey, context.previousPhases)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to reorder phases'
      notify.error(message)
    },
  })

  // Wrapper functions
  const createPhase = async (request: CreatePhaseRequest) => {
    return createMutation.mutateAsync(request)
  }

  const updatePhase = async (id: string, request: UpdatePhaseRequest) => {
    return updateMutation.mutateAsync({ id, request })
  }

  const deletePhase = async (id: string) => {
    return deleteMutation.mutateAsync(id)
  }

  const reorderPhases = async (phaseIds: string[]) => {
    return reorderMutation.mutateAsync(phaseIds)
  }

  const movePhaseUp = async (phaseId: string) => {
    const index = phases.findIndex(p => p.id === phaseId)
    if (index <= 0) return // Already at top

    const newOrder = [...phases.map(p => p.id)]
    ;[newOrder[index - 1], newOrder[index]] = [
      newOrder[index],
      newOrder[index - 1],
    ]
    return reorderPhases(newOrder)
  }

  const movePhaseDown = async (phaseId: string) => {
    const index = phases.findIndex(p => p.id === phaseId)
    if (index < 0 || index >= phases.length - 1) return // Already at bottom

    const newOrder = [...phases.map(p => p.id)]
    ;[newOrder[index], newOrder[index + 1]] = [
      newOrder[index + 1],
      newOrder[index],
    ]
    return reorderPhases(newOrder)
  }

  return {
    phases,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load phases'
      : null,
    fetchPhases,
    createPhase,
    updatePhase,
    deletePhase,
    reorderPhases,
    movePhaseUp,
    movePhaseDown,
    // Expose mutation states
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
    isReordering: reorderMutation.isPending,
  }
}

export default usePhases
