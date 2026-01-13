import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { injectService } from '../services/injectService'
import { InjectStatus } from '../../../types'
import type {
  InjectDto,
  CreateInjectRequest,
  UpdateInjectRequest,
  FireInjectRequest,
  SkipInjectRequest,
  PhaseGroup,
} from '../types'

/** Query key factory for injects */
export const injectKeys = {
  all: (exerciseId: string) => ['exercises', exerciseId, 'injects'] as const,
  detail: (exerciseId: string, id: string) =>
    ['exercises', exerciseId, 'injects', id] as const,
}

/**
 * Hook for managing inject list state and operations using React Query
 *
 * Features:
 * - Automatic caching and background refetching
 * - Optimistic updates for create/update/fire/skip
 * - Error handling with toast notifications
 * - Group injects by phase for MSEL view
 */
export const useInjects = (exerciseId: string) => {
  const queryClient = useQueryClient()
  const queryKey = injectKeys.all(exerciseId)

  // Query for fetching injects
  const {
    data: injects = [],
    isLoading: loading,
    error,
    refetch: fetchInjects,
  } = useQuery({
    queryKey,
    queryFn: () => injectService.getInjects(exerciseId),
    enabled: !!exerciseId,
  })

  // Mutation for creating injects
  const createMutation = useMutation({
    mutationFn: (request: CreateInjectRequest) =>
      injectService.createInject(exerciseId, request),
    onSuccess: newInject => {
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) => [
        ...old,
        newInject,
      ])
      toast.success('Inject created')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to create inject'
      toast.error(message)
    },
  })

  // Mutation for updating injects
  const updateMutation = useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string
      request: UpdateInjectRequest
    }) => injectService.updateInject(exerciseId, id, request),
    onMutate: async ({ id, request }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousInjects = queryClient.getQueryData<InjectDto[]>(queryKey)

      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === id
            ? { ...inject, ...request, updatedAt: new Date().toISOString() }
            : inject,
        ),
      )

      return { previousInjects }
    },
    onSuccess: updatedInject => {
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === updatedInject.id ? updatedInject : inject,
        ),
      )
      queryClient.setQueryData(
        injectKeys.detail(exerciseId, updatedInject.id),
        updatedInject,
      )
      toast.success('Inject updated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousInjects) {
        queryClient.setQueryData(queryKey, context.previousInjects)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update inject'
      toast.error(message)
    },
  })

  // Mutation for firing injects
  const fireMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request?: FireInjectRequest }) =>
      injectService.fireInject(exerciseId, id, request),
    onMutate: async ({ id }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousInjects = queryClient.getQueryData<InjectDto[]>(queryKey)

      // Optimistic update
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === id
            ? {
              ...inject,
              status: InjectStatus.Fired,
              firedAt: new Date().toISOString(),
              updatedAt: new Date().toISOString(),
            }
            : inject,
        ),
      )

      return { previousInjects }
    },
    onSuccess: firedInject => {
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === firedInject.id ? firedInject : inject,
        ),
      )
      queryClient.setQueryData(
        injectKeys.detail(exerciseId, firedInject.id),
        firedInject,
      )
      toast.success('Inject fired')
    },
    onError: (err, _variables, context) => {
      if (context?.previousInjects) {
        queryClient.setQueryData(queryKey, context.previousInjects)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to fire inject'
      toast.error(message)
    },
  })

  // Mutation for skipping injects
  const skipMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: SkipInjectRequest }) =>
      injectService.skipInject(exerciseId, id, request),
    onMutate: async ({ id, request }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousInjects = queryClient.getQueryData<InjectDto[]>(queryKey)

      // Optimistic update
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === id
            ? {
              ...inject,
              status: InjectStatus.Skipped,
              skippedAt: new Date().toISOString(),
              skipReason: request.reason,
              updatedAt: new Date().toISOString(),
            }
            : inject,
        ),
      )

      return { previousInjects }
    },
    onSuccess: skippedInject => {
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === skippedInject.id ? skippedInject : inject,
        ),
      )
      queryClient.setQueryData(
        injectKeys.detail(exerciseId, skippedInject.id),
        skippedInject,
      )
      toast.success('Inject skipped')
    },
    onError: (err, _variables, context) => {
      if (context?.previousInjects) {
        queryClient.setQueryData(queryKey, context.previousInjects)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to skip inject'
      toast.error(message)
    },
  })

  // Mutation for resetting injects
  const resetMutation = useMutation({
    mutationFn: (id: string) => injectService.resetInject(exerciseId, id),
    onMutate: async id => {
      await queryClient.cancelQueries({ queryKey })
      const previousInjects = queryClient.getQueryData<InjectDto[]>(queryKey)

      // Optimistic update
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === id
            ? {
              ...inject,
              status: InjectStatus.Pending,
              firedAt: null,
              firedBy: null,
              firedByName: null,
              skippedAt: null,
              skippedBy: null,
              skippedByName: null,
              skipReason: null,
              updatedAt: new Date().toISOString(),
            }
            : inject,
        ),
      )

      return { previousInjects }
    },
    onSuccess: resetInject => {
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === resetInject.id ? resetInject : inject,
        ),
      )
      queryClient.setQueryData(
        injectKeys.detail(exerciseId, resetInject.id),
        resetInject,
      )
      toast.success('Inject reset to pending')
    },
    onError: (err, _variables, context) => {
      if (context?.previousInjects) {
        queryClient.setQueryData(queryKey, context.previousInjects)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to reset inject'
      toast.error(message)
    },
  })

  // Mutation for deleting injects
  const deleteMutation = useMutation({
    mutationFn: (id: string) => injectService.deleteInject(exerciseId, id),
    onMutate: async id => {
      await queryClient.cancelQueries({ queryKey })
      const previousInjects = queryClient.getQueryData<InjectDto[]>(queryKey)

      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.filter(inject => inject.id !== id),
      )

      return { previousInjects }
    },
    onSuccess: () => {
      toast.success('Inject deleted')
    },
    onError: (err, _variables, context) => {
      if (context?.previousInjects) {
        queryClient.setQueryData(queryKey, context.previousInjects)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to delete inject'
      toast.error(message)
    },
  })

  // Group injects by phase for MSEL view
  const groupedByPhase: PhaseGroup[] = (() => {
    const groups = new Map<string | null, PhaseGroup>()

    // First pass: create groups
    injects.forEach(inject => {
      const key = inject.phaseId
      if (!groups.has(key)) {
        groups.set(key, {
          phaseId: inject.phaseId,
          phaseName: inject.phaseName,
          sequence: inject.phaseId
            ? injects.findIndex(i => i.phaseId === inject.phaseId)
            : Infinity,
          injects: [],
        })
      }
      groups.get(key)!.injects.push(inject)
    })

    // Sort injects within each group by sequence
    groups.forEach(group => {
      group.injects.sort((a, b) => a.sequence - b.sequence)
    })

    // Sort groups by first inject's sequence (phases should appear in order)
    return Array.from(groups.values()).sort((a, b) => {
      const aFirstSeq = a.injects[0]?.sequence ?? Infinity
      const bFirstSeq = b.injects[0]?.sequence ?? Infinity
      return aFirstSeq - bFirstSeq
    })
  })()

  // Wrapper functions
  const createInject = async (request: CreateInjectRequest) => {
    return createMutation.mutateAsync(request)
  }

  const updateInject = async (id: string, request: UpdateInjectRequest) => {
    return updateMutation.mutateAsync({ id, request })
  }

  const fireInject = async (id: string, request?: FireInjectRequest) => {
    return fireMutation.mutateAsync({ id, request })
  }

  const skipInject = async (id: string, request: SkipInjectRequest) => {
    return skipMutation.mutateAsync({ id, request })
  }

  const resetInject = async (id: string) => {
    return resetMutation.mutateAsync(id)
  }

  const deleteInject = async (id: string) => {
    return deleteMutation.mutateAsync(id)
  }

  return {
    injects,
    groupedByPhase,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load injects'
      : null,
    fetchInjects,
    createInject,
    updateInject,
    fireInject,
    skipInject,
    resetInject,
    deleteInject,
    // Expose mutation states for loading indicators
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isFiring: fireMutation.isPending,
    isSkipping: skipMutation.isPending,
    isResetting: resetMutation.isPending,
    isDeleting: deleteMutation.isPending,
  }
}

export default useInjects
