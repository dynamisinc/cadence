/**
 * useCapabilityTargets Hook
 *
 * React Query hooks for Capability Target CRUD operations.
 * Provides optimistic updates and cache management.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { capabilityTargetService } from '../services/eegService'
import type {
  CapabilityTargetDto,
  CreateCapabilityTargetRequest,
  UpdateCapabilityTargetRequest,
} from '../types'

/** Query key factory for capability targets */
export const capabilityTargetKeys = {
  all: ['capability-targets'] as const,
  byExercise: (exerciseId: string) =>
    [...capabilityTargetKeys.all, 'exercise', exerciseId] as const,
  detail: (id: string) => [...capabilityTargetKeys.all, 'detail', id] as const,
}

/**
 * Hook for managing Capability Targets for an exercise
 *
 * Features:
 * - Automatic caching and background refetching
 * - Optimistic updates for create/update/delete/reorder
 * - Error handling with toast notifications
 */
export const useCapabilityTargets = (exerciseId: string) => {
  const queryClient = useQueryClient()
  const queryKey = capabilityTargetKeys.byExercise(exerciseId)

  // Query for fetching capability targets
  const {
    data: response,
    isLoading: loading,
    error,
    refetch: fetchCapabilityTargets,
  } = useQuery({
    queryKey,
    queryFn: () => capabilityTargetService.getByExercise(exerciseId),
    enabled: !!exerciseId,
  })

  const capabilityTargets = response?.items ?? []

  // Mutation for creating capability targets
  const createMutation = useMutation({
    mutationFn: (request: CreateCapabilityTargetRequest) =>
      capabilityTargetService.create(exerciseId, request),
    onSuccess: newTarget => {
      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        items: [...(old?.items ?? []), newTarget],
        totalCount: (old?.totalCount ?? 0) + 1,
      }))
      notify.success('Capability Target created')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to create capability target'
      notify.error(message)
    },
  })

  // Mutation for updating capability targets
  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateCapabilityTargetRequest }) =>
      capabilityTargetService.update(exerciseId, id, request),
    onMutate: async ({ id, request }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousData = queryClient.getQueryData(queryKey)

      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        ...old,
        items: (old?.items ?? []).map(target =>
          target.id === id
            ? { ...target, ...request, updatedAt: new Date().toISOString() }
            : target,
        ),
      }))

      return { previousData }
    },
    onSuccess: updatedTarget => {
      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        ...old,
        items: (old?.items ?? []).map(target =>
          target.id === updatedTarget.id ? updatedTarget : target,
        ),
      }))
      notify.success('Capability Target updated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousData) {
        queryClient.setQueryData(queryKey, context.previousData)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update capability target'
      notify.error(message)
    },
  })

  // Mutation for deleting capability targets
  const deleteMutation = useMutation({
    mutationFn: (id: string) => capabilityTargetService.delete(exerciseId, id),
    onMutate: async id => {
      await queryClient.cancelQueries({ queryKey })
      const previousData = queryClient.getQueryData(queryKey)

      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        items: (old?.items ?? []).filter(target => target.id !== id),
        totalCount: (old?.totalCount ?? 1) - 1,
      }))

      return { previousData }
    },
    onSuccess: () => {
      notify.success('Capability Target deleted')
    },
    onError: (err, _variables, context) => {
      if (context?.previousData) {
        queryClient.setQueryData(queryKey, context.previousData)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to delete capability target'
      notify.error(message)
    },
  })

  // Mutation for reordering capability targets
  const reorderMutation = useMutation({
    mutationFn: (orderedIds: string[]) =>
      capabilityTargetService.reorder(exerciseId, orderedIds),
    onMutate: async orderedIds => {
      await queryClient.cancelQueries({ queryKey })
      const previousData = queryClient.getQueryData(queryKey)

      // Reorder items based on the new order
      queryClient.setQueryData(queryKey, (old: typeof response) => {
        if (!old?.items) return old
        const itemMap = new Map(old.items.map(item => [item.id, item]))
        const reorderedItems = orderedIds
          .map((id, index) => {
            const item = itemMap.get(id)
            return item ? { ...item, sortOrder: index } : null
          })
          .filter((item): item is CapabilityTargetDto => item !== null)
        return { ...old, items: reorderedItems }
      })

      return { previousData }
    },
    onError: (err, _variables, context) => {
      if (context?.previousData) {
        queryClient.setQueryData(queryKey, context.previousData)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to reorder capability targets'
      notify.error(message)
    },
  })

  // Wrapper functions
  const createCapabilityTarget = async (request: CreateCapabilityTargetRequest) => {
    return createMutation.mutateAsync(request)
  }

  const updateCapabilityTarget = async (
    id: string,
    request: UpdateCapabilityTargetRequest,
  ) => {
    return updateMutation.mutateAsync({ id, request })
  }

  const deleteCapabilityTarget = async (id: string) => {
    return deleteMutation.mutateAsync(id)
  }

  const reorderCapabilityTargets = async (orderedIds: string[]) => {
    return reorderMutation.mutateAsync(orderedIds)
  }

  return {
    capabilityTargets,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load capability targets'
      : null,
    fetchCapabilityTargets,
    createCapabilityTarget,
    updateCapabilityTarget,
    deleteCapabilityTarget,
    reorderCapabilityTargets,
    // Expose mutation states
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
    isReordering: reorderMutation.isPending,
  }
}

/**
 * Hook to fetch a single capability target by ID
 */
export const useCapabilityTarget = (id: string | undefined) => {
  return useQuery({
    queryKey: capabilityTargetKeys.detail(id!),
    queryFn: () => capabilityTargetService.getById(id!),
    enabled: !!id,
  })
}

export default useCapabilityTargets
