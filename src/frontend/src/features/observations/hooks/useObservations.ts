/**
 * useObservations Hook
 *
 * React Query hook for managing observation state and operations.
 * Supports offline-first mutations with optimistic updates and queue sync.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { observationService } from '../services/observationService'
import { useConnectivity } from '../../../core/contexts'
import { addPendingAction } from '../../../core/offline'
import type {
  ObservationDto,
  CreateObservationRequest,
  UpdateObservationRequest,
} from '../types'

/** Query key for observations list by exercise */
export const observationsQueryKey = (exerciseId: string) =>
  ['observations', 'exercise', exerciseId] as const

/** Query key for observations list by inject */
export const observationsByInjectQueryKey = (injectId: string) =>
  ['observations', 'inject', injectId] as const

/**
 * Hook for managing observations for an exercise
 */
export const useObservations = (exerciseId: string) => {
  const queryClient = useQueryClient()
  const { connectivityState, incrementPendingCount } = useConnectivity()

  // Consider offline if not fully connected (includes SignalR disconnection)
  const isEffectivelyOnline = connectivityState === 'online'

  // Query for fetching observations
  const {
    data: observations = [],
    isLoading: loading,
    error,
    refetch: fetchObservations,
  } = useQuery({
    queryKey: observationsQueryKey(exerciseId),
    queryFn: () => observationService.getObservationsByExercise(exerciseId),
    enabled: !!exerciseId,
  })

  // Mutation for creating observations
  const createMutation = useMutation({
    mutationFn: (request: CreateObservationRequest) =>
      observationService.createObservation(exerciseId, request),
    onSuccess: newObservation => {
      queryClient.setQueryData<ObservationDto[]>(
        observationsQueryKey(exerciseId),
        (old = []) => [newObservation, ...old],
      )
      // Also invalidate inject-specific queries if the observation is linked
      if (newObservation.injectId) {
        queryClient.invalidateQueries({
          queryKey: observationsByInjectQueryKey(newObservation.injectId),
        })
      }
      toast.success('Observation recorded')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to create observation'
      toast.error(message)
    },
  })

  // Mutation for updating observations
  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateObservationRequest }) =>
      observationService.updateObservation(id, request),
    onSuccess: updatedObservation => {
      queryClient.setQueryData<ObservationDto[]>(
        observationsQueryKey(exerciseId),
        (old = []) =>
          old.map(obs =>
            obs.id === updatedObservation.id ? updatedObservation : obs,
          ),
      )
      toast.success('Observation updated')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to update observation'
      toast.error(message)
    },
  })

  // Mutation for deleting observations
  const deleteMutation = useMutation({
    mutationFn: observationService.deleteObservation,
    onSuccess: (_, deletedId) => {
      queryClient.setQueryData<ObservationDto[]>(
        observationsQueryKey(exerciseId),
        (old = []) => old.filter(obs => obs.id !== deletedId),
      )
      toast.success('Observation deleted')
    },
    onError: err => {
      const message =
        err instanceof Error ? err.message : 'Failed to delete observation'
      toast.error(message)
    },
  })

  /**
   * Create observation with offline support
   * When offline: queues action, applies optimistic update
   * When online: sends directly to API
   */
  const createObservation = async (request: CreateObservationRequest): Promise<ObservationDto> => {
    if (isEffectivelyOnline) {
      // Online: send directly to API
      return createMutation.mutateAsync(request)
    }

    // Offline: queue action and apply optimistic update
    const tempId = `temp-${Date.now()}-${Math.random().toString(36).slice(2)}`
    const optimisticObservation: ObservationDto = {
      id: tempId,
      exerciseId,
      injectId: request.injectId ?? null,
      objectiveId: request.objectiveId ?? null,
      content: request.content,
      rating: request.rating ?? null,
      recommendation: request.recommendation ?? null,
      observedAt: request.observedAt ?? new Date().toISOString(),
      location: request.location ?? null,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      createdBy: 'offline-user',
      createdByName: 'You (offline)',
      injectTitle: null,
      injectNumber: null,
    }

    // Queue the action for later sync
    await addPendingAction({
      type: 'CREATE_OBSERVATION',
      exerciseId,
      payload: {
        observation: {
          content: request.content,
          rating: request.rating,
          recommendation: request.recommendation,
          injectId: request.injectId,
        },
        tempId,
      },
    })

    // Apply optimistic update
    queryClient.setQueryData<ObservationDto[]>(
      observationsQueryKey(exerciseId),
      (old = []) => [optimisticObservation, ...old],
    )

    incrementPendingCount()
    toast.info('Observation saved offline. Will sync when connection restores.')

    return optimisticObservation
  }

  /**
   * Update observation with offline support
   * When offline: queues action, applies optimistic update
   * When online: sends directly to API
   */
  const updateObservation = async (id: string, request: UpdateObservationRequest): Promise<ObservationDto> => {
    if (isEffectivelyOnline) {
      // Online: send directly to API
      return updateMutation.mutateAsync({ id, request })
    }

    // Offline: queue action and apply optimistic update
    const currentObservations = queryClient.getQueryData<ObservationDto[]>(
      observationsQueryKey(exerciseId),
    ) ?? []
    const existingObservation = currentObservations.find(o => o.id === id)

    if (!existingObservation) {
      throw new Error('Observation not found')
    }

    const optimisticObservation: ObservationDto = {
      ...existingObservation,
      content: request.content,
      rating: request.rating ?? existingObservation.rating,
      recommendation: request.recommendation ?? existingObservation.recommendation,
      injectId: request.injectId ?? existingObservation.injectId,
      objectiveId: request.objectiveId ?? existingObservation.objectiveId,
      location: request.location ?? existingObservation.location,
      updatedAt: new Date().toISOString(),
    }

    // Queue the action for later sync
    await addPendingAction({
      type: 'UPDATE_OBSERVATION',
      exerciseId,
      payload: {
        observationId: id,
        changes: {
          content: request.content,
          rating: request.rating,
          recommendation: request.recommendation,
          injectId: request.injectId,
        },
      },
    })

    // Apply optimistic update
    queryClient.setQueryData<ObservationDto[]>(
      observationsQueryKey(exerciseId),
      (old = []) => old.map(obs => (obs.id === id ? optimisticObservation : obs)),
    )

    incrementPendingCount()
    toast.info('Changes saved offline. Will sync when connection restores.')

    return optimisticObservation
  }

  /**
   * Delete observation with offline support
   * When offline: queues action, applies optimistic update
   * When online: sends directly to API
   */
  const deleteObservation = async (id: string): Promise<void> => {
    if (isEffectivelyOnline) {
      // Online: send directly to API
      await deleteMutation.mutateAsync(id)
      return
    }

    // Offline: queue action and apply optimistic update
    // Queue the action for later sync
    await addPendingAction({
      type: 'DELETE_OBSERVATION',
      exerciseId,
      payload: {
        observationId: id,
      },
    })

    // Apply optimistic update (remove from list)
    queryClient.setQueryData<ObservationDto[]>(
      observationsQueryKey(exerciseId),
      (old = []) => old.filter(obs => obs.id !== id),
    )

    incrementPendingCount()
    toast.info('Deletion queued. Will sync when connection restores.')
  }

  return {
    observations,
    loading,
    error: error ? (error instanceof Error ? error.message : 'Failed to load observations') : null,
    fetchObservations,
    createObservation,
    updateObservation,
    deleteObservation,
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
  }
}

export default useObservations
