/**
 * Inject Approval Permission Check Hook (S11)
 *
 * React Query hooks for checking inject approval permissions.
 * Handles self-approval policy enforcement and provides information
 * about whether confirmation dialogs should be shown.
 */

import { useQuery } from '@tanstack/react-query'
import { injectService } from '../services/injectService'
import type { InjectApprovalCheckDto } from '@/types'

/**
 * Query keys for approval permission checks
 */
export const approvalCheckKeys = {
  all: ['approval-check'] as const,
  canApproveAny: (exerciseId: string) =>
    [...approvalCheckKeys.all, exerciseId, 'can-approve-any'] as const,
  canApproveInject: (exerciseId: string, injectId: string) =>
    [...approvalCheckKeys.all, exerciseId, 'inject', injectId] as const,
}

/**
 * Hook to check if current user can approve any inject for an exercise
 *
 * @param exerciseId - The exercise ID
 * @param enabled - Whether to enable the query (default: true)
 */
export const useCanApproveAny = (exerciseId: string, enabled = true) => {
  return useQuery({
    queryKey: approvalCheckKeys.canApproveAny(exerciseId),
    queryFn: () => injectService.canApproveAny(exerciseId),
    enabled: enabled && !!exerciseId,
    staleTime: 5 * 60 * 1000, // 5 minutes - permissions don't change often
  })
}

/**
 * Hook to check if current user can approve a specific inject
 *
 * Returns detailed permission check including self-approval policy handling.
 *
 * @param exerciseId - The exercise ID
 * @param injectId - The inject ID to check
 * @param enabled - Whether to enable the query (default: true)
 *
 * @returns Query result with InjectApprovalCheckDto
 *
 * @example
 * const { data: permissionCheck, isLoading } = useCanApproveInject(exerciseId, injectId)
 *
 * if (permissionCheck?.requiresConfirmation) {
 *   // Show self-approval confirmation dialog
 * } else if (permissionCheck?.canApprove) {
 *   // Proceed with approval
 * }
 */
export const useCanApproveInject = (
  exerciseId: string,
  injectId: string | undefined,
  enabled = true,
) => {
  return useQuery<InjectApprovalCheckDto>({
    queryKey: approvalCheckKeys.canApproveInject(exerciseId, injectId ?? ''),
    queryFn: () => injectService.canApproveInject(exerciseId, injectId!),
    enabled: enabled && !!exerciseId && !!injectId,
    staleTime: 30 * 1000, // 30 seconds - check more frequently per inject
  })
}

/**
 * Combined hook for approval permission checking with dialog handling
 *
 * Provides a convenient way to check permissions and determine what action to take.
 *
 * @param exerciseId - The exercise ID
 * @param injectId - The inject ID to check (optional)
 */
export const useApprovalPermissionCheck = (
  exerciseId: string,
  injectId?: string,
) => {
  const canApproveAnyQuery = useCanApproveAny(exerciseId, !injectId)
  const canApproveInjectQuery = useCanApproveInject(exerciseId, injectId, !!injectId)

  // Use inject-specific check if available, otherwise fall back to general check
  const permissionCheck = injectId
    ? canApproveInjectQuery.data
    : undefined

  return {
    // General permission (can approve any inject in exercise)
    canApproveAny: canApproveAnyQuery.data ?? false,
    isLoadingCanApproveAny: canApproveAnyQuery.isLoading,

    // Inject-specific permission
    permissionCheck,
    isLoadingPermissionCheck: canApproveInjectQuery.isLoading,

    // Convenience properties
    canApprove: permissionCheck?.canApprove ?? canApproveAnyQuery.data ?? false,
    requiresConfirmation: permissionCheck?.requiresConfirmation ?? false,
    isSelfApproval: permissionCheck?.isSelfApproval ?? false,
    permissionMessage: permissionCheck?.message ?? null,

    // Combined loading state
    isLoading: injectId
      ? canApproveInjectQuery.isLoading
      : canApproveAnyQuery.isLoading,
  }
}

export default useApprovalPermissionCheck
