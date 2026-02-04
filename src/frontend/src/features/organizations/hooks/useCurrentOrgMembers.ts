/**
 * Current Organization Members React Query Hooks
 *
 * Provides hooks for managing members of the current organization (OrgAdmin).
 *
 * @module features/organizations/hooks
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { organizationService } from '../services/organizationService'
import type { OrgRole, AddMemberRequest } from '../types'

/**
 * Query key factory for current org members
 */
export const currentOrgMemberKeys = {
  all: ['current-org-members'] as const,
  list: () => [...currentOrgMemberKeys.all, 'list'] as const,
}

/**
 * Fetch members of the current organization
 */
export function useCurrentOrgMembers() {
  return useQuery({
    queryKey: currentOrgMemberKeys.list(),
    queryFn: () => organizationService.getCurrentOrgMembers(),
  })
}

/**
 * Add member to current organization mutation
 */
export function useAddCurrentOrgMember() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: AddMemberRequest) =>
      organizationService.addCurrentOrgMember(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: currentOrgMemberKeys.list() })
    },
  })
}

/**
 * Update member role in current organization mutation
 */
export function useUpdateCurrentOrgMemberRole() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ membershipId, role }: { membershipId: string; role: OrgRole }) =>
      organizationService.updateCurrentOrgMemberRole(membershipId, { role }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: currentOrgMemberKeys.list() })
    },
  })
}

/**
 * Remove member from current organization mutation
 */
export function useRemoveCurrentOrgMember() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (membershipId: string) =>
      organizationService.removeCurrentOrgMember(membershipId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: currentOrgMemberKeys.list() })
    },
  })
}
