/**
 * useInvitations - React Query hooks for organization invitations
 *
 * Provides hooks for creating, listing, resending, and canceling organization invitations.
 *
 * @module features/organizations/hooks
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { organizationService } from '../services/organizationService'
import type { CreateInvitationRequest, InvitationStatus } from '../types'

const INVITATIONS_QUERY_KEY = ['organizations', 'current', 'invitations']

/**
 * Get all invitations for the current organization
 */
export const useInvitations = (status?: InvitationStatus) => {
  return useQuery({
    queryKey: [...INVITATIONS_QUERY_KEY, { status }],
    queryFn: () => organizationService.getInvitations(status),
  })
}

/**
 * Create a new invitation
 */
export const useCreateInvitation = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateInvitationRequest) =>
      organizationService.createInvitation(request),
    onSuccess: () => {
      // Invalidate all invitation queries to refresh the list
      queryClient.invalidateQueries({ queryKey: INVITATIONS_QUERY_KEY })
    },
  })
}

/**
 * Resend an invitation email
 */
export const useResendInvitation = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (invitationId: string) => organizationService.resendInvitation(invitationId),
    onSuccess: () => {
      // Invalidate to refresh status/timestamps
      queryClient.invalidateQueries({ queryKey: INVITATIONS_QUERY_KEY })
    },
  })
}

/**
 * Cancel a pending invitation
 */
export const useCancelInvitation = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (invitationId: string) => organizationService.cancelInvitation(invitationId),
    onSuccess: () => {
      // Invalidate to remove from list
      queryClient.invalidateQueries({ queryKey: INVITATIONS_QUERY_KEY })
    },
  })
}
