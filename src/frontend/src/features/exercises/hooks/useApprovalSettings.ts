/**
 * Approval Settings Hook
 *
 * React Query hooks for exercise approval settings (S01-S02, S06-S07).
 * Provides queries and mutations for approval configuration and status.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { exerciseService } from '../services/exerciseService'
import type {
  ApprovalSettingsDto,
  UpdateApprovalSettingsRequest,
  ApprovalStatusDto,
  PublishValidationResult,
} from '../types'

/** Query key factory for approval settings */
export const approvalKeys = {
  settings: (exerciseId: string) =>
    ['exercises', exerciseId, 'approval-settings'] as const,
  status: (exerciseId: string) =>
    ['exercises', exerciseId, 'approval-status'] as const,
  publishValidation: (exerciseId: string) =>
    ['exercises', exerciseId, 'publish-validation'] as const,
}

/**
 * Hook for fetching and updating exercise approval settings (S01-S02)
 */
export const useApprovalSettings = (exerciseId: string) => {
  const queryClient = useQueryClient()
  const queryKey = approvalKeys.settings(exerciseId)

  // Query for fetching approval settings
  const {
    data: settings,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey,
    queryFn: () => exerciseService.getApprovalSettings(exerciseId),
    enabled: !!exerciseId,
  })

  // Mutation for updating approval settings
  const updateMutation = useMutation({
    mutationFn: (request: UpdateApprovalSettingsRequest) =>
      exerciseService.updateApprovalSettings(exerciseId, request),
    onMutate: async (request) => {
      await queryClient.cancelQueries({ queryKey })
      const previousSettings =
        queryClient.getQueryData<ApprovalSettingsDto>(queryKey)

      // Optimistic update
      queryClient.setQueryData<ApprovalSettingsDto>(queryKey, old =>
        old
          ? {
              ...old,
              requireInjectApproval: request.requireInjectApproval,
              approvalPolicyOverridden: request.isOverride ?? old.approvalPolicyOverridden,
              approvalOverrideReason:
                request.overrideReason ?? old.approvalOverrideReason,
            }
          : old,
      )

      return { previousSettings }
    },
    onSuccess: (updatedSettings) => {
      queryClient.setQueryData(queryKey, updatedSettings)
      toast.success('Approval settings updated')
    },
    onError: (err, _variables, context) => {
      if (context?.previousSettings) {
        queryClient.setQueryData(queryKey, context.previousSettings)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update approval settings'
      toast.error(message)
    },
  })

  const updateSettings = async (request: UpdateApprovalSettingsRequest) => {
    return updateMutation.mutateAsync(request)
  }

  return {
    settings,
    isLoading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load approval settings'
      : null,
    refetch,
    updateSettings,
    isUpdating: updateMutation.isPending,
  }
}

/**
 * Hook for fetching approval queue status (S06)
 */
export const useApprovalStatus = (exerciseId: string) => {
  const queryKey = approvalKeys.status(exerciseId)

  const {
    data: status,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey,
    queryFn: () => exerciseService.getApprovalStatus(exerciseId),
    enabled: !!exerciseId,
    // Poll every 30 seconds to keep status fresh
    refetchInterval: 30000,
  })

  return {
    status,
    isLoading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load approval status'
      : null,
    refetch,
    // Computed values
    pendingCount: status?.pendingApprovalCount ?? 0,
    approvedCount: status?.approvedCount ?? 0,
    totalInjects: status?.totalInjects ?? 0,
    canPublish: status?.allApproved ?? false,
    approvalPercentage: status?.approvalPercentage ?? 0,
  }
}

/**
 * Hook for validating if an exercise can be published (S07)
 */
export const usePublishValidation = (exerciseId: string) => {
  const queryKey = approvalKeys.publishValidation(exerciseId)

  const {
    data: validation,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey,
    queryFn: () => exerciseService.validatePublish(exerciseId),
    enabled: !!exerciseId,
    // Only fetch when needed (not auto-refresh)
    staleTime: 5000,
  })

  return {
    validation,
    isLoading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to validate publish'
      : null,
    refetch,
    // Computed values
    canPublish: validation?.canPublish ?? false,
    unapprovedCount: validation?.totalUnapprovedCount ?? 0,
    warnings: validation?.warnings ?? [],
    errors: validation?.errors ?? [],
  }
}

export default useApprovalSettings
