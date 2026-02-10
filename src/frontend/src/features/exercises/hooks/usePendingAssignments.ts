/**
 * usePendingAssignments Hook
 *
 * Manages pending exercise assignments for users who have been invited
 * but haven't yet accepted their organization invitation.
 *
 * @module features/exercises/hooks
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { bulkImportService } from '../services/bulkImportService'
import { organizationService } from '../../organizations/services/organizationService'
import type { PendingExerciseAssignmentDto } from '../types/bulkImport'

/**
 * Hook for managing pending exercise assignments
 *
 * @param exerciseId - Exercise ID
 * @returns Pending assignments data and resend function
 */
export const usePendingAssignments = (exerciseId: string | undefined) => {
  const queryClient = useQueryClient()

  // Fetch pending assignments
  const {
    data: pendingAssignments = [],
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery<PendingExerciseAssignmentDto[], Error>({
    queryKey: ['pendingAssignments', exerciseId],
    queryFn: () => bulkImportService.getPendingAssignments(exerciseId!),
    enabled: !!exerciseId,
    staleTime: 30000, // Consider data stale after 30 seconds
  })

  // Resend invitation mutation
  const resendInvitation = useMutation({
    mutationFn: async (invitationId: string) => {
      return await organizationService.resendInvitation(invitationId)
    },
    onSuccess: () => {
      // Refetch pending assignments after successful resend
      queryClient.invalidateQueries({ queryKey: ['pendingAssignments', exerciseId] })
    },
  })

  return {
    pendingAssignments,
    isLoading,
    isError,
    error,
    refetch,
    resendInvitation: resendInvitation.mutateAsync,
    isResending: resendInvitation.isPending,
  }
}

export default usePendingAssignments
