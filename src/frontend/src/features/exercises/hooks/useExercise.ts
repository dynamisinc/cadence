import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { exerciseService } from '../services/exerciseService'
import { exercisesQueryKey } from './useExercises'
import type { ExerciseDto, UpdateExerciseRequest } from '../types'

/** Query key factory for single exercise */
export const exerciseQueryKey = (id: string) => ['exercise', id] as const

/**
 * Hook for managing a single exercise state and operations using React Query
 *
 * Features:
 * - Automatic caching with exercises list sync
 * - Background refetching
 * - Optimistic updates with rollback on error
 */
export const useExercise = (id: string | undefined) => {
  const queryClient = useQueryClient()

  // Query for fetching single exercise
  const {
    data: exercise = null,
    isLoading: loading,
    error,
    refetch: fetchExercise,
  } = useQuery({
    queryKey: exerciseQueryKey(id ?? ''),
    queryFn: () => exerciseService.getExercise(id!),
    enabled: !!id, // Only run query if id exists
  })

  // Mutation for updating exercise with optimistic update
  const updateMutation = useMutation({
    mutationFn: (request: UpdateExerciseRequest) =>
      exerciseService.updateExercise(id!, request),
    onMutate: async (request: UpdateExerciseRequest) => {
      if (!id) return

      // Cancel any outgoing refetches
      await queryClient.cancelQueries({ queryKey: exerciseQueryKey(id) })
      await queryClient.cancelQueries({ queryKey: exercisesQueryKey })

      // Snapshot previous values
      const previousExercise = queryClient.getQueryData<ExerciseDto>(exerciseQueryKey(id))
      const previousExercises = queryClient.getQueryData<ExerciseDto[]>(exercisesQueryKey)

      // Optimistically update single exercise cache
      if (previousExercise) {
        queryClient.setQueryData<ExerciseDto>(exerciseQueryKey(id), {
          ...previousExercise,
          ...request,
          updatedAt: new Date().toISOString(),
        })
      }

      // Optimistically update in exercises list cache
      queryClient.setQueryData<ExerciseDto[]>(exercisesQueryKey, (old = []) =>
        old.map(exercise =>
          exercise.id === id
            ? {
              ...exercise,
              ...request,
              updatedAt: new Date().toISOString(),
            }
            : exercise,
        ),
      )

      return { previousExercise, previousExercises }
    },
    onSuccess: updatedExercise => {
      // Replace optimistic data with server response
      queryClient.setQueryData(exerciseQueryKey(id!), updatedExercise)
      queryClient.setQueryData<ExerciseDto[]>(exercisesQueryKey, (old = []) =>
        old.map(exercise =>
          exercise.id === updatedExercise.id ? updatedExercise : exercise,
        ),
      )
      toast.success('Exercise updated')
    },
    onError: (err, _variables, context) => {
      // Rollback to previous state
      if (context?.previousExercise && id) {
        queryClient.setQueryData(exerciseQueryKey(id), context.previousExercise)
      }
      if (context?.previousExercises) {
        queryClient.setQueryData(exercisesQueryKey, context.previousExercises)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update exercise'
      toast.error(message)
    },
  })

  // Wrapper function to match previous API
  const updateExercise = async (request: UpdateExerciseRequest) => {
    if (!id) {
      throw new Error('No exercise ID provided')
    }
    return updateMutation.mutateAsync(request)
  }

  return {
    exercise,
    loading,
    error: error ? (error instanceof Error ? error.message : 'Failed to load exercise') : null,
    fetchExercise,
    updateExercise,
    isUpdating: updateMutation.isPending,
  }
}

export default useExercise
