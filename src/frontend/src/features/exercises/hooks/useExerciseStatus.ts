import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { exerciseService } from '../services/exerciseService'
import { exercisesQueryKey } from './useExercises'
import type { ExerciseDto } from '../types'
import type { ExerciseStatus } from '../../../types'

/**
 * Hook for managing exercise status transitions
 *
 * Provides mutations for all status workflow actions:
 * - Activate (Draft → Active)
 * - Pause (Active → Paused)
 * - Resume (Paused → Active)
 * - Complete (Active/Paused → Completed)
 * - Archive (Completed → Archived)
 * - Unarchive (Archived → Completed)
 * - RevertToDraft (Paused → Draft)
 */
export const useExerciseStatus = (exerciseId: string) => {
  const queryClient = useQueryClient()
  const exerciseQueryKey = ['exercise', exerciseId]

  // Query for available transitions
  const { data: availableTransitions = [] } = useQuery({
    queryKey: ['exercise-transitions', exerciseId],
    queryFn: () => exerciseService.getAvailableTransitions(exerciseId),
    enabled: !!exerciseId,
  })

  // Helper to update exercise in cache
  const updateExerciseInCache = (updatedExercise: ExerciseDto) => {
    // Update single exercise cache
    queryClient.setQueryData<ExerciseDto>(exerciseQueryKey, updatedExercise)

    // Update exercises list cache
    queryClient.setQueryData<ExerciseDto[]>(exercisesQueryKey, (old = []) =>
      old.map(exercise =>
        exercise.id === updatedExercise.id ? updatedExercise : exercise,
      ),
    )

    // Invalidate transitions query
    queryClient.invalidateQueries({ queryKey: ['exercise-transitions', exerciseId] })
  }

  // Activate mutation (Draft → Active)
  const activateMutation = useMutation({
    mutationFn: () => exerciseService.activateExercise(exerciseId),
    onSuccess: updatedExercise => {
      updateExerciseInCache(updatedExercise)
      toast.success('Exercise activated')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to activate exercise'
      toast.error(message)
    },
  })

  // Pause mutation (Active → Paused)
  const pauseMutation = useMutation({
    mutationFn: () => exerciseService.pauseExercise(exerciseId),
    onSuccess: updatedExercise => {
      updateExerciseInCache(updatedExercise)
      toast.success('Exercise paused')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to pause exercise'
      toast.error(message)
    },
  })

  // Resume mutation (Paused → Active)
  const resumeMutation = useMutation({
    mutationFn: () => exerciseService.resumeExercise(exerciseId),
    onSuccess: updatedExercise => {
      updateExerciseInCache(updatedExercise)
      toast.success('Exercise resumed')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to resume exercise'
      toast.error(message)
    },
  })

  // Complete mutation (Active/Paused → Completed)
  const completeMutation = useMutation({
    mutationFn: () => exerciseService.completeExercise(exerciseId),
    onSuccess: updatedExercise => {
      updateExerciseInCache(updatedExercise)
      toast.success('Exercise completed')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to complete exercise'
      toast.error(message)
    },
  })

  // Archive mutation (Completed → Archived)
  const archiveMutation = useMutation({
    mutationFn: () => exerciseService.archiveExercise(exerciseId),
    onSuccess: updatedExercise => {
      updateExerciseInCache(updatedExercise)
      toast.success('Exercise archived')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to archive exercise'
      toast.error(message)
    },
  })

  // Unarchive mutation (Archived → Completed)
  const unarchiveMutation = useMutation({
    mutationFn: () => exerciseService.unarchiveExercise(exerciseId),
    onSuccess: updatedExercise => {
      updateExerciseInCache(updatedExercise)
      toast.success('Exercise unarchived')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to unarchive exercise'
      toast.error(message)
    },
  })

  // Revert to draft mutation (Paused → Draft)
  const revertToDraftMutation = useMutation({
    mutationFn: () => exerciseService.revertToDraft(exerciseId),
    onSuccess: updatedExercise => {
      updateExerciseInCache(updatedExercise)
      // Also invalidate injects and observations since they're cleared
      // Injects use key: ['exercises', exerciseId, 'injects']
      queryClient.invalidateQueries({ queryKey: ['exercises', exerciseId, 'injects'] })
      // Observations use key: ['observations', 'exercise', exerciseId]
      queryClient.invalidateQueries({ queryKey: ['observations', 'exercise', exerciseId] })
      // Also invalidate MSEL summary since inject counts changed
      queryClient.invalidateQueries({ queryKey: ['msel-summary', exerciseId] })
      toast.success('Exercise reverted to draft')
    },
    onError: (err) => {
      const message = err instanceof Error ? err.message : 'Failed to revert exercise to draft'
      toast.error(message)
    },
  })

  // Check if a specific transition is available
  const canTransition = (targetStatus: ExerciseStatus) =>
    availableTransitions.includes(targetStatus)

  return {
    // Available transitions
    availableTransitions,
    canTransition,

    // Actions
    activate: activateMutation.mutateAsync,
    pause: pauseMutation.mutateAsync,
    resume: resumeMutation.mutateAsync,
    complete: completeMutation.mutateAsync,
    archive: archiveMutation.mutateAsync,
    unarchive: unarchiveMutation.mutateAsync,
    revertToDraft: revertToDraftMutation.mutateAsync,

    // Loading states
    isActivating: activateMutation.isPending,
    isPausing: pauseMutation.isPending,
    isResuming: resumeMutation.isPending,
    isCompleting: completeMutation.isPending,
    isArchiving: archiveMutation.isPending,
    isUnarchiving: unarchiveMutation.isPending,
    isRevertingToDraft: revertToDraftMutation.isPending,

    // Combined loading state
    isTransitioning:
      activateMutation.isPending ||
      pauseMutation.isPending ||
      resumeMutation.isPending ||
      completeMutation.isPending ||
      archiveMutation.isPending ||
      unarchiveMutation.isPending ||
      revertToDraftMutation.isPending,
  }
}

export default useExerciseStatus
