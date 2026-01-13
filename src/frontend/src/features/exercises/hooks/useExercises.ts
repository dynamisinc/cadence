import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { exerciseService } from '../services/exerciseService'
import { ExerciseStatus } from '../../../types'
import type {
  ExerciseDto,
  CreateExerciseRequest,
  UpdateExerciseRequest,
} from '../types'

/** Query key for exercises list */
export const exercisesQueryKey = ['exercises'] as const

/**
 * Hook for managing exercise list state and operations using React Query
 *
 * Features:
 * - Automatic caching and background refetching
 * - Optimistic updates for create/update
 * - Error handling with toast notifications
 */
export const useExercises = () => {
  const queryClient = useQueryClient()

  // Query for fetching exercises
  const {
    data: exercises = [],
    isLoading: loading,
    error,
    refetch: fetchExercises,
  } = useQuery({
    queryKey: exercisesQueryKey,
    queryFn: exerciseService.getExercises,
  })

  // Mutation for creating exercises with optimistic update
  const createMutation = useMutation({
    mutationFn: exerciseService.createExercise,
    onMutate: async (newRequest: CreateExerciseRequest) => {
      // Cancel any outgoing refetches
      await queryClient.cancelQueries({ queryKey: exercisesQueryKey })

      // Snapshot the previous value
      const previousExercises = queryClient.getQueryData<ExerciseDto[]>(exercisesQueryKey)

      // Optimistically add new exercise with temporary ID
      const optimisticExercise: ExerciseDto = {
        id: `temp-${Date.now()}`,
        name: newRequest.name,
        description: newRequest.description ?? null,
        exerciseType: newRequest.exerciseType,
        status: ExerciseStatus.Draft,
        isPracticeMode: false,
        scheduledDate: newRequest.scheduledDate,
        startTime: null,
        endTime: null,
        timeZoneId: newRequest.timeZoneId ?? 'UTC',
        location: newRequest.location ?? null,
        organizationId: 'temp',
        activeMselId: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      }

      queryClient.setQueryData<ExerciseDto[]>(exercisesQueryKey, (old = []) => [
        optimisticExercise,
        ...old,
      ])

      // Return context with snapshot for rollback
      return { previousExercises }
    },
    onSuccess: (newExercise) => {
      // Replace optimistic exercise with real one
      queryClient.setQueryData<ExerciseDto[]>(exercisesQueryKey, (old = []) => {
        // Remove optimistic entry and add real one
        const withoutOptimistic = old.filter(e => !e.id.startsWith('temp-'))
        return [newExercise, ...withoutOptimistic]
      })
      toast.success('Exercise created')
    },
    onError: (err, _variables, context) => {
      // Rollback to previous state
      if (context?.previousExercises) {
        queryClient.setQueryData(exercisesQueryKey, context.previousExercises)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to create exercise'
      toast.error(message)
    },
  })

  // Mutation for updating exercises with optimistic update
  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateExerciseRequest }) =>
      exerciseService.updateExercise(id, request),
    onMutate: async ({ id, request }) => {
      // Cancel any outgoing refetches
      await queryClient.cancelQueries({ queryKey: exercisesQueryKey })
      await queryClient.cancelQueries({ queryKey: ['exercise', id] })

      // Snapshot previous values
      const previousExercises = queryClient.getQueryData<ExerciseDto[]>(exercisesQueryKey)
      const previousExercise = queryClient.getQueryData<ExerciseDto>(['exercise', id])

      // Optimistically update in list
      queryClient.setQueryData<ExerciseDto[]>(exercisesQueryKey, (old = []) =>
        old.map((exercise) =>
          exercise.id === id
            ? {
                ...exercise,
                ...request,
                updatedAt: new Date().toISOString(),
              }
            : exercise,
        ),
      )

      // Optimistically update single exercise cache if it exists
      if (previousExercise) {
        queryClient.setQueryData<ExerciseDto>(['exercise', id], {
          ...previousExercise,
          ...request,
          updatedAt: new Date().toISOString(),
        })
      }

      return { previousExercises, previousExercise }
    },
    onSuccess: (updatedExercise) => {
      // Replace optimistic data with server response
      queryClient.setQueryData<ExerciseDto[]>(exercisesQueryKey, (old = []) =>
        old.map((exercise) =>
          exercise.id === updatedExercise.id ? updatedExercise : exercise,
        ),
      )
      queryClient.setQueryData(['exercise', updatedExercise.id], updatedExercise)
      toast.success('Exercise updated')
    },
    onError: (err, { id }, context) => {
      // Rollback to previous state
      if (context?.previousExercises) {
        queryClient.setQueryData(exercisesQueryKey, context.previousExercises)
      }
      if (context?.previousExercise) {
        queryClient.setQueryData(['exercise', id], context.previousExercise)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update exercise'
      toast.error(message)
    },
  })

  // Wrapper functions to match previous API
  const createExercise = async (request: CreateExerciseRequest) => {
    return createMutation.mutateAsync(request)
  }

  const updateExercise = async (id: string, request: UpdateExerciseRequest) => {
    return updateMutation.mutateAsync({ id, request })
  }

  return {
    exercises,
    loading,
    error: error ? (error instanceof Error ? error.message : 'Failed to load exercises') : null,
    fetchExercises,
    createExercise,
    updateExercise,
    // Expose mutation states for more granular loading indicators
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
  }
}

export default useExercises
