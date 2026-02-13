/**
 * useCriticalTasks Hook
 *
 * React Query hooks for Critical Task CRUD operations.
 * Provides optimistic updates and cache management.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { criticalTaskService } from '../services/eegService'
import { capabilityTargetKeys } from './useCapabilityTargets'
import type {
  CriticalTaskDto,
  CreateCriticalTaskRequest,
  UpdateCriticalTaskRequest,
  SetLinkedInjectsRequest,
} from '../types'

/** Query key factory for critical tasks */
export const criticalTaskKeys = {
  all: ['critical-tasks'] as const,
  byCapabilityTarget: (targetId: string) =>
    [...criticalTaskKeys.all, 'target', targetId] as const,
  byExercise: (exerciseId: string) =>
    [...criticalTaskKeys.all, 'exercise', exerciseId] as const,
  detail: (id: string) => [...criticalTaskKeys.all, 'detail', id] as const,
  linkedInjects: (id: string) => [...criticalTaskKeys.all, id, 'injects'] as const,
}

/**
 * Hook for managing Critical Tasks for a capability target
 *
 * Features:
 * - Automatic caching and background refetching
 * - Optimistic updates for create/update/delete/reorder
 * - Error handling with toast notifications
 *
 * @param exerciseId Exercise ID (required for authorization)
 * @param targetId Capability target ID
 */
export const useCriticalTasks = (exerciseId: string, targetId: string) => {
  const queryClient = useQueryClient()
  const queryKey = criticalTaskKeys.byCapabilityTarget(targetId)

  // Query for fetching critical tasks
  const {
    data: response,
    isLoading: loading,
    error,
    refetch: fetchCriticalTasks,
  } = useQuery({
    queryKey,
    queryFn: () => criticalTaskService.getByCapabilityTarget(targetId),
    enabled: !!targetId,
  })

  const criticalTasks = response?.items ?? []

  // Mutation for creating critical tasks
  const createMutation = useMutation({
    mutationFn: (request: CreateCriticalTaskRequest) =>
      criticalTaskService.create(exerciseId, targetId, request),
    onSuccess: newTask => {
      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        items: [...(old?.items ?? []), newTask],
        totalCount: (old?.totalCount ?? 0) + 1,
      }))
      // Invalidate parent capability target to update task count
      queryClient.invalidateQueries({ queryKey: capabilityTargetKeys.all })
      notify.success('Critical Task created')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to create critical task'
      notify.error(message)
    },
  })

  // Mutation for updating critical tasks
  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateCriticalTaskRequest }) =>
      criticalTaskService.update(exerciseId, id, request),
    onMutate: async ({ id, request }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousData = queryClient.getQueryData(queryKey)

      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        ...old,
        items: (old?.items ?? []).map(task =>
          task.id === id
            ? { ...task, ...request, updatedAt: new Date().toISOString() }
            : task,
        ),
      }))

      return { previousData }
    },
    onSuccess: updatedTask => {
      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        ...old,
        items: (old?.items ?? []).map(task =>
          task.id === updatedTask.id ? updatedTask : task,
        ),
      }))
      notify.success('Critical Task updated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousData) {
        queryClient.setQueryData(queryKey, context.previousData)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update critical task'
      notify.error(message)
    },
  })

  // Mutation for deleting critical tasks
  const deleteMutation = useMutation({
    mutationFn: (id: string) => criticalTaskService.delete(exerciseId, id),
    onMutate: async id => {
      await queryClient.cancelQueries({ queryKey })
      const previousData = queryClient.getQueryData(queryKey)

      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        items: (old?.items ?? []).filter(task => task.id !== id),
        totalCount: (old?.totalCount ?? 1) - 1,
      }))

      return { previousData }
    },
    onSuccess: () => {
      // Invalidate parent capability target to update task count
      queryClient.invalidateQueries({ queryKey: capabilityTargetKeys.all })
      notify.success('Critical Task deleted')
    },
    onError: (err, _variables, context) => {
      if (context?.previousData) {
        queryClient.setQueryData(queryKey, context.previousData)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to delete critical task'
      notify.error(message)
    },
  })

  // Mutation for reordering critical tasks
  const reorderMutation = useMutation({
    mutationFn: (orderedIds: string[]) =>
      criticalTaskService.reorder(exerciseId, targetId, orderedIds),
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
          .filter((item): item is CriticalTaskDto => item !== null)
        return { ...old, items: reorderedItems }
      })

      return { previousData }
    },
    onError: (err, _variables, context) => {
      if (context?.previousData) {
        queryClient.setQueryData(queryKey, context.previousData)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to reorder critical tasks'
      notify.error(message)
    },
  })

  // Wrapper functions
  const createCriticalTask = async (request: CreateCriticalTaskRequest) => {
    return createMutation.mutateAsync(request)
  }

  const updateCriticalTask = async (id: string, request: UpdateCriticalTaskRequest) => {
    return updateMutation.mutateAsync({ id, request })
  }

  const deleteCriticalTask = async (id: string) => {
    return deleteMutation.mutateAsync(id)
  }

  const reorderCriticalTasks = async (orderedIds: string[]) => {
    return reorderMutation.mutateAsync(orderedIds)
  }

  return {
    criticalTasks,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load critical tasks'
      : null,
    fetchCriticalTasks,
    createCriticalTask,
    updateCriticalTask,
    deleteCriticalTask,
    reorderCriticalTasks,
    // Expose mutation states
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
    isReordering: reorderMutation.isPending,
  }
}

/**
 * Hook for fetching all critical tasks for an exercise
 * Useful for inject-task linking and coverage views
 */
export const useCriticalTasksByExercise = (
  exerciseId: string,
  filters?: { hasInjects?: boolean; hasEegEntries?: boolean },
) => {
  const queryKey = [...criticalTaskKeys.byExercise(exerciseId), filters] as const

  const {
    data: response,
    isLoading: loading,
    error,
    refetch,
  } = useQuery({
    queryKey,
    queryFn: () => criticalTaskService.getByExercise(exerciseId, filters),
    enabled: !!exerciseId,
  })

  return {
    criticalTasks: response?.items ?? [],
    totalCount: response?.totalCount ?? 0,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load critical tasks'
      : null,
    refetch,
  }
}

/**
 * Hook to fetch a single critical task by ID
 */
export const useCriticalTask = (id: string | undefined) => {
  return useQuery({
    queryKey: criticalTaskKeys.detail(id!),
    queryFn: () => criticalTaskService.getById(id!),
    enabled: !!id,
  })
}

/**
 * Hook for managing linked injects for a critical task
 *
 * @param exerciseId Exercise ID (required for authorization)
 * @param taskId Critical task ID
 */
export const useLinkedInjects = (exerciseId: string, taskId: string) => {
  const queryClient = useQueryClient()
  const queryKey = criticalTaskKeys.linkedInjects(taskId)

  const {
    data: linkedInjectIds = [],
    isLoading: loading,
    error,
  } = useQuery({
    queryKey,
    queryFn: () => criticalTaskService.getLinkedInjectIds(taskId),
    enabled: !!taskId,
  })

  const setLinkedInjectsMutation = useMutation({
    mutationFn: (request: SetLinkedInjectsRequest) =>
      criticalTaskService.setLinkedInjects(exerciseId, taskId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey })
      // Also invalidate critical task queries to update inject counts
      queryClient.invalidateQueries({ queryKey: criticalTaskKeys.all })
      notify.success('Linked injects updated')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to update linked injects'
      notify.error(message)
    },
  })

  const setLinkedInjects = async (injectIds: string[]) => {
    return setLinkedInjectsMutation.mutateAsync({ injectIds })
  }

  return {
    linkedInjectIds,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load linked injects'
      : null,
    setLinkedInjects,
    isUpdating: setLinkedInjectsMutation.isPending,
  }
}

export default useCriticalTasks
