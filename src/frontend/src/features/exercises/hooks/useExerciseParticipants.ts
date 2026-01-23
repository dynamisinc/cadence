/**
 * useExerciseParticipants - Hook for managing exercise participants
 *
 * Provides CRUD operations for exercise-specific role assignments.
 * Uses React Query for data fetching with optimistic updates.
 *
 * @module features/exercises/hooks
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { participantService } from '../services/participantService'
import { setupProgressQueryKey } from './useSetupProgress'
import type {
  ExerciseParticipantDto,
  AddParticipantRequest,
  UpdateParticipantRoleRequest,
} from '../types'

/**
 * Hook for managing exercise participants
 *
 * @param exerciseId - ID of the exercise
 * @returns Participant data and mutation functions
 */
export const useExerciseParticipants = (exerciseId: string) => {
  const queryClient = useQueryClient()
  const queryKey = ['exercises', exerciseId, 'participants']

  // Fetch participants
  const {
    data: participants = [],
    isLoading,
    isError,
    error,
  } = useQuery<ExerciseParticipantDto[]>({
    queryKey,
    queryFn: () => participantService.getParticipants(exerciseId),
  })

  // Add participant mutation
  const addParticipantMutation = useMutation({
    mutationFn: (request: AddParticipantRequest) =>
      participantService.addParticipant(exerciseId, request),
    onMutate: async newParticipant => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey })

      // Snapshot previous value
      const previousParticipants = queryClient.getQueryData<ExerciseParticipantDto[]>(queryKey)

      // Optimistically update (with temp participant)
      queryClient.setQueryData<ExerciseParticipantDto[]>(queryKey, (old = []) => [
        ...old,
        {
          participantId: `temp-${Date.now()}`,
          userId: newParticipant.userId,
          displayName: 'Loading...',
          email: '',
          exerciseRole: newParticipant.role,
          systemRole: '',
          effectiveRole: newParticipant.role,
          addedAt: new Date().toISOString(),
          addedBy: null,
        },
      ])

      return { previousParticipants }
    },
    onSuccess: created => {
      // Replace temp with real data
      queryClient.setQueryData<ExerciseParticipantDto[]>(queryKey, (old = []) =>
        old.map(p =>
          p.participantId.startsWith('temp-') && p.userId === created.userId ? created : p,
        ),
      )
      // Invalidate setup progress since participant count affects it
      queryClient.invalidateQueries({ queryKey: setupProgressQueryKey(exerciseId) })
      toast.success('Participant added')
    },
    onError: (err, _variables, context) => {
      // Rollback on error
      if (context?.previousParticipants) {
        queryClient.setQueryData(queryKey, context.previousParticipants)
      }
      const message = err instanceof Error ? err.message : 'Failed to add participant'
      toast.error(message)
      // Note: Don't re-throw here - mutateAsync already propagates the error
    },
  })

  // Update participant role mutation
  const updateParticipantRoleMutation = useMutation({
    mutationFn: ({ userId, request }: { userId: string; request: UpdateParticipantRoleRequest }) =>
      participantService.updateParticipantRole(exerciseId, userId, request),
    onMutate: async ({ userId, request }) => {
      await queryClient.cancelQueries({ queryKey })

      const previousParticipants = queryClient.getQueryData<ExerciseParticipantDto[]>(queryKey)

      // Optimistically update
      queryClient.setQueryData<ExerciseParticipantDto[]>(queryKey, (old = []) =>
        old.map(p =>
          p.userId === userId
            ? { ...p, exerciseRole: request.role, effectiveRole: request.role }
            : p,
        ),
      )

      return { previousParticipants }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey })
      toast.success('Role updated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousParticipants) {
        queryClient.setQueryData(queryKey, context.previousParticipants)
      }
      const message = err instanceof Error ? err.message : 'Failed to update role'
      toast.error(message)
    },
  })

  // Remove participant mutation
  const removeParticipantMutation = useMutation({
    mutationFn: (userId: string) => participantService.removeParticipant(exerciseId, userId),
    onMutate: async userId => {
      await queryClient.cancelQueries({ queryKey })

      const previousParticipants = queryClient.getQueryData<ExerciseParticipantDto[]>(queryKey)

      // Optimistically remove
      queryClient.setQueryData<ExerciseParticipantDto[]>(queryKey, (old = []) =>
        old.filter(p => p.userId !== userId),
      )

      return { previousParticipants }
    },
    onSuccess: () => {
      // Invalidate setup progress since participant count affects it
      queryClient.invalidateQueries({ queryKey: setupProgressQueryKey(exerciseId) })
      toast.success('Participant removed')
    },
    onError: (err, _variables, context) => {
      if (context?.previousParticipants) {
        queryClient.setQueryData(queryKey, context.previousParticipants)
      }
      const message = err instanceof Error ? err.message : 'Failed to remove participant'
      toast.error(message)
    },
  })

  return {
    participants,
    isLoading,
    isError,
    error,
    addParticipant: addParticipantMutation.mutateAsync,
    updateParticipantRole: (userId: string, request: UpdateParticipantRoleRequest) =>
      updateParticipantRoleMutation.mutateAsync({ userId, request }),
    removeParticipant: removeParticipantMutation.mutateAsync,
  }
}
