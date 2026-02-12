/**
 * useEegEntries Hook
 *
 * React Query hooks for EEG Entry CRUD operations.
 * Provides optimistic updates and cache management.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { eegEntryService } from '../services/eegService'
import { criticalTaskKeys } from './useCriticalTasks'
import type {
  CreateEegEntryRequest,
  UpdateEegEntryRequest,
  EegCoverageDto,
  EegEntryQueryParams,
} from '../types'

/** Query key factory for EEG entries */
export const eegEntryKeys = {
  all: ['eeg-entries'] as const,
  byExercise: (exerciseId: string, queryParams?: EegEntryQueryParams) =>
    [...eegEntryKeys.all, 'exercise', exerciseId, queryParams] as const,
  byCriticalTask: (taskId: string) =>
    [...eegEntryKeys.all, 'task', taskId] as const,
  detail: (id: string) => [...eegEntryKeys.all, 'detail', id] as const,
  coverage: (exerciseId: string) =>
    [...eegEntryKeys.all, 'coverage', exerciseId] as const,
}

/**
 * Hook for managing EEG Entries for an exercise
 *
 * Features:
 * - Automatic caching and background refetching
 * - Optimistic updates for create/update/delete
 * - Error handling with toast notifications
 * - Pagination and filtering support
 */
export const useEegEntries = (exerciseId: string, queryParams?: EegEntryQueryParams) => {
  const queryClient = useQueryClient()
  const queryKey = eegEntryKeys.byExercise(exerciseId, queryParams)

  // Query for fetching EEG entries
  const {
    data: response,
    isLoading: loading,
    error,
    refetch: fetchEegEntries,
  } = useQuery({
    queryKey,
    queryFn: () => eegEntryService.getByExercise(exerciseId, queryParams),
    enabled: !!exerciseId,
  })

  const eegEntries = response?.items ?? []

  // Mutation for creating EEG entries (taskId is passed in the create call)
  const createMutation = useMutation({
    mutationFn: ({ taskId, request }: { taskId: string; request: CreateEegEntryRequest }) =>
      eegEntryService.create(exerciseId, taskId, request),
    onSuccess: newEntry => {
      // Invalidate all exercise entry queries (different query param combinations)
      queryClient.invalidateQueries({
        predicate: query =>
          query.queryKey[0] === 'eeg-entries' &&
          query.queryKey[1] === 'exercise' &&
          query.queryKey[2] === exerciseId,
      })
      // Invalidate critical task queries to update entry counts
      queryClient.invalidateQueries({ queryKey: criticalTaskKeys.all })
      // Invalidate task-specific entries
      queryClient.invalidateQueries({
        queryKey: eegEntryKeys.byCriticalTask(newEntry.criticalTaskId),
      })
      // Invalidate coverage
      queryClient.invalidateQueries({
        queryKey: eegEntryKeys.coverage(exerciseId),
      })
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to create EEG entry'
      notify.error(message)
    },
  })

  // Wrapper function for creating entry
  const createEntry = async (taskId: string, request: CreateEegEntryRequest) => {
    return createMutation.mutateAsync({ taskId, request })
  }

  return {
    eegEntries,
    totalCount: response?.totalCount ?? 0,
    page: response?.page ?? 1,
    pageSize: response?.pageSize ?? 20,
    totalPages: response?.totalPages ?? 1,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load EEG entries'
      : null,
    fetchEegEntries,
    createEntry,
    isCreating: createMutation.isPending,
  }
}

/**
 * Hook for managing EEG Entries for a critical task
 *
 * @param exerciseId Exercise ID (required for authorization)
 * @param taskId Critical task ID
 */
export const useEegEntriesByTask = (exerciseId: string, taskId: string) => {
  const queryClient = useQueryClient()
  const queryKey = eegEntryKeys.byCriticalTask(taskId)

  // Query for fetching EEG entries
  const {
    data: response,
    isLoading: loading,
    error,
    refetch: fetchEegEntries,
  } = useQuery({
    queryKey,
    queryFn: () => eegEntryService.getByCriticalTask(taskId),
    enabled: !!taskId,
  })

  const eegEntries = response?.items ?? []

  // Mutation for creating EEG entries
  const createMutation = useMutation({
    mutationFn: (request: CreateEegEntryRequest) =>
      eegEntryService.create(exerciseId, taskId, request),
    onSuccess: newEntry => {
      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        ...old,
        items: [...(old?.items ?? []), newEntry],
        totalCount: (old?.totalCount ?? 0) + 1,
      }))
      // Invalidate critical task queries to update entry counts
      queryClient.invalidateQueries({ queryKey: criticalTaskKeys.all })
      // Invalidate coverage
      queryClient.invalidateQueries({
        predicate: query =>
          query.queryKey[0] === 'eeg-entries' && query.queryKey[1] === 'coverage',
      })
      notify.success('EEG Entry recorded')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to create EEG entry'
      notify.error(message)
    },
  })

  // Mutation for updating EEG entries
  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateEegEntryRequest }) =>
      eegEntryService.update(exerciseId, id, request),
    onMutate: async ({ id, request }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousData = queryClient.getQueryData(queryKey)

      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        ...old,
        items: (old?.items ?? []).map(entry =>
          entry.id === id
            ? { ...entry, ...request, updatedAt: new Date().toISOString() }
            : entry,
        ),
      }))

      return { previousData }
    },
    onSuccess: updatedEntry => {
      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        ...old,
        items: (old?.items ?? []).map(entry =>
          entry.id === updatedEntry.id ? updatedEntry : entry,
        ),
      }))
      // Invalidate coverage if rating changed
      queryClient.invalidateQueries({
        predicate: query =>
          query.queryKey[0] === 'eeg-entries' && query.queryKey[1] === 'coverage',
      })
      notify.success('EEG Entry updated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousData) {
        queryClient.setQueryData(queryKey, context.previousData)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update EEG entry'
      notify.error(message)
    },
  })

  // Mutation for deleting EEG entries
  const deleteMutation = useMutation({
    mutationFn: (id: string) => eegEntryService.delete(exerciseId, id),
    onMutate: async id => {
      await queryClient.cancelQueries({ queryKey })
      const previousData = queryClient.getQueryData(queryKey)

      queryClient.setQueryData(queryKey, (old: typeof response) => ({
        items: (old?.items ?? []).filter(entry => entry.id !== id),
        totalCount: (old?.totalCount ?? 1) - 1,
      }))

      return { previousData }
    },
    onSuccess: () => {
      // Invalidate critical task queries to update entry counts
      queryClient.invalidateQueries({ queryKey: criticalTaskKeys.all })
      // Invalidate coverage
      queryClient.invalidateQueries({
        predicate: query =>
          query.queryKey[0] === 'eeg-entries' && query.queryKey[1] === 'coverage',
      })
      notify.success('EEG Entry deleted')
    },
    onError: (err, _variables, context) => {
      if (context?.previousData) {
        queryClient.setQueryData(queryKey, context.previousData)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to delete EEG entry'
      notify.error(message)
    },
  })

  // Wrapper functions
  const createEegEntry = async (request: CreateEegEntryRequest) => {
    return createMutation.mutateAsync(request)
  }

  const updateEegEntry = async (id: string, request: UpdateEegEntryRequest) => {
    return updateMutation.mutateAsync({ id, request })
  }

  const deleteEegEntry = async (id: string) => {
    return deleteMutation.mutateAsync(id)
  }

  return {
    eegEntries,
    totalCount: response?.totalCount ?? 0,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load EEG entries'
      : null,
    fetchEegEntries,
    createEegEntry,
    updateEegEntry,
    deleteEegEntry,
    // Expose mutation states
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
  }
}

/**
 * Hook to fetch a single EEG entry by ID
 */
export const useEegEntry = (id: string | undefined) => {
  return useQuery({
    queryKey: eegEntryKeys.detail(id!),
    queryFn: () => eegEntryService.getById(id!),
    enabled: !!id,
  })
}

/**
 * Hook to fetch EEG coverage statistics for an exercise
 */
export const useEegCoverage = (exerciseId: string) => {
  const queryKey = eegEntryKeys.coverage(exerciseId)

  const {
    data: coverage,
    isLoading: loading,
    error,
    refetch,
  } = useQuery<EegCoverageDto>({
    queryKey,
    queryFn: () => eegEntryService.getCoverage(exerciseId),
    enabled: !!exerciseId,
    staleTime: 30000, // Cache for 30 seconds
  })

  return {
    coverage,
    loading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load EEG coverage'
      : null,
    refetch,
  }
}

export default useEegEntries
