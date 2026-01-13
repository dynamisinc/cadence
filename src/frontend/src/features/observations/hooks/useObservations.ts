/**
 * useObservations Hook
 *
 * React Query hook for managing observation state and operations.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { observationService } from '../services/observationService'
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

  // Wrapper functions
  const createObservation = async (request: CreateObservationRequest) => {
    return createMutation.mutateAsync(request)
  }

  const updateObservation = async (id: string, request: UpdateObservationRequest) => {
    return updateMutation.mutateAsync({ id, request })
  }

  const deleteObservation = async (id: string) => {
    return deleteMutation.mutateAsync(id)
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
