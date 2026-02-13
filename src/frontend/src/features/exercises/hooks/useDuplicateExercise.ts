import { useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { useNavigate } from 'react-router-dom'
import { exerciseService } from '../services/exerciseService'
import { exercisesQueryKey } from './useExercises'
import type { ExerciseDto, DuplicateExerciseRequest } from '../types'

/**
 * Hook for duplicating an exercise
 *
 * Provides mutation for duplicating an exercise with optional new name and date.
 * On success, invalidates the exercises list and optionally navigates to the new exercise.
 *
 * @param options - Hook options
 * @param options.navigateOnSuccess - Navigate to new exercise on success (default: true)
 * @returns Mutation result with duplicate function
 */
export const useDuplicateExercise = (options?: {
  navigateOnSuccess?: boolean
}) => {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const navigateOnSuccess = options?.navigateOnSuccess ?? true

  const mutation = useMutation<
    ExerciseDto,
    Error,
    { exerciseId: string; request?: DuplicateExerciseRequest }
  >({
    mutationFn: ({ exerciseId, request }) =>
      exerciseService.duplicateExercise(exerciseId, request),
    onSuccess: newExercise => {
      // Invalidate exercises list
      queryClient.invalidateQueries({ queryKey: exercisesQueryKey })

      // Add new exercise to cache
      queryClient.setQueryData<ExerciseDto>(
        ['exercise', newExercise.id],
        newExercise,
      )

      notify.success(`Exercise duplicated: ${newExercise.name}`)

      if (navigateOnSuccess) {
        navigate(`/exercises/${newExercise.id}`)
      }
    },
    onError: (err: Error) => {
      const message =
        err instanceof Error ? err.message : 'Failed to duplicate exercise'
      notify.error(message)
    },
  })

  return {
    duplicate: mutation.mutateAsync,
    isDuplicating: mutation.isPending,
    error: mutation.error,
  }
}

export default useDuplicateExercise
