/**
 * Approval Permissions React Query Hooks (S11)
 *
 * Provides React Query hooks for organization-level approval permission
 * configuration. Used by SysAdmins to configure which roles can approve
 * injects and self-approval policies.
 *
 * @module features/organizations/hooks
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { organizationService } from '../services/organizationService'
import type { UpdateApprovalPermissionsRequest } from '@/types'
import { organizationKeys } from './useOrganizations'

/**
 * Query key factory for approval permissions
 */
export const approvalPermissionKeys = {
  all: ['approval-permissions'] as const,
  detail: (orgId: string) => [...approvalPermissionKeys.all, orgId] as const,
}

/**
 * Fetch approval permissions for an organization (SysAdmin only)
 *
 * @param orgId - The organization ID
 */
export function useApprovalPermissions(orgId: string | undefined) {
  return useQuery({
    queryKey: approvalPermissionKeys.detail(orgId ?? ''),
    queryFn: () => organizationService.getApprovalPermissions(orgId!),
    enabled: !!orgId,
  })
}

/**
 * Update approval permissions mutation (SysAdmin only)
 *
 * @returns Mutation for updating approval permissions
 */
export function useUpdateApprovalPermissions() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      orgId,
      request,
    }: {
      orgId: string
      request: UpdateApprovalPermissionsRequest
    }) => organizationService.updateApprovalPermissions(orgId, request),
    onSuccess: (data, { orgId }) => {
      // Update the cache with new permissions
      queryClient.setQueryData(approvalPermissionKeys.detail(orgId), data)
      // Also invalidate org detail in case it includes permissions
      queryClient.invalidateQueries({ queryKey: organizationKeys.detail(orgId) })
    },
  })
}

// =========================================================================
// Current Organization Approval Permissions (OrgAdmin)
// =========================================================================

/**
 * Query key for current organization approval permissions
 */
export const currentOrgApprovalPermissionKeys = {
  all: ['current-org-approval-permissions'] as const,
  detail: () => [...currentOrgApprovalPermissionKeys.all, 'detail'] as const,
}

/**
 * Fetch approval permissions for the current organization (OrgAdmin)
 */
export function useCurrentOrgApprovalPermissions() {
  return useQuery({
    queryKey: currentOrgApprovalPermissionKeys.detail(),
    queryFn: () => organizationService.getCurrentApprovalPermissions(),
  })
}

/**
 * Update approval permissions for the current organization (OrgAdmin)
 */
export function useUpdateCurrentOrgApprovalPermissions() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateApprovalPermissionsRequest) =>
      organizationService.updateCurrentApprovalPermissions(request),
    onSuccess: data => {
      // Update the cache with new permissions
      queryClient.setQueryData(currentOrgApprovalPermissionKeys.detail(), data)
      // Also invalidate current org in case it includes permissions
      queryClient.invalidateQueries({ queryKey: organizationKeys.current() })
    },
  })
}
