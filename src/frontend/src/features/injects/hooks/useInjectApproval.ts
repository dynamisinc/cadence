/**
 * Inject Approval Workflow Hook
 *
 * React Query hooks for inject approval workflow operations (S00-S09).
 * Provides mutations for submit, approve, reject, revert, and batch operations.
 */

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { injectService } from '../services/injectService'
import { InjectStatus } from '../../../types'
import { injectKeys } from './useInjects'
import type {
  InjectDto,
  ApproveInjectRequest,
  RejectInjectRequest,
  RevertApprovalRequest,
  BatchApproveRequest,
  BatchRejectRequest,
} from '../types'

/**
 * Hook for inject approval workflow operations
 */
export const useInjectApproval = (exerciseId: string) => {
  const queryClient = useQueryClient()
  const queryKey = injectKeys.all(exerciseId)

  // Mutation for submitting inject for approval (S03)
  const submitMutation = useMutation({
    mutationFn: (injectId: string) =>
      injectService.submitForApproval(exerciseId, injectId),
    onMutate: async (injectId) => {
      await queryClient.cancelQueries({ queryKey })
      const previousInjects = queryClient.getQueryData<InjectDto[]>(queryKey)

      // Optimistic update
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === injectId
            ? {
                ...inject,
                status: InjectStatus.Submitted,
                submittedAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
              }
            : inject,
        ),
      )

      return { previousInjects }
    },
    onSuccess: (submittedInject) => {
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === submittedInject.id ? submittedInject : inject,
        ),
      )
      queryClient.setQueryData(
        injectKeys.detail(exerciseId, submittedInject.id),
        submittedInject,
      )
      // Invalidate approval status
      queryClient.invalidateQueries({
        queryKey: ['exercises', exerciseId, 'approval-status'],
      })
      toast.success('Inject submitted for approval')
    },
    onError: (err, _variables, context) => {
      if (context?.previousInjects) {
        queryClient.setQueryData(queryKey, context.previousInjects)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to submit inject'
      toast.error(message)
    },
  })

  // Mutation for approving inject (S04)
  const approveMutation = useMutation({
    mutationFn: ({
      injectId,
      request,
    }: {
      injectId: string
      request?: ApproveInjectRequest
    }) => injectService.approveInject(exerciseId, injectId, request),
    onMutate: async ({ injectId }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousInjects = queryClient.getQueryData<InjectDto[]>(queryKey)

      // Optimistic update
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === injectId
            ? {
                ...inject,
                status: InjectStatus.Approved,
                approvedAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
              }
            : inject,
        ),
      )

      return { previousInjects }
    },
    onSuccess: (approvedInject) => {
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === approvedInject.id ? approvedInject : inject,
        ),
      )
      queryClient.setQueryData(
        injectKeys.detail(exerciseId, approvedInject.id),
        approvedInject,
      )
      // Invalidate approval status
      queryClient.invalidateQueries({
        queryKey: ['exercises', exerciseId, 'approval-status'],
      })
      toast.success('Inject approved')
    },
    onError: (err, _variables, context) => {
      if (context?.previousInjects) {
        queryClient.setQueryData(queryKey, context.previousInjects)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to approve inject'
      toast.error(message)
    },
  })

  // Mutation for rejecting inject (S04)
  const rejectMutation = useMutation({
    mutationFn: ({
      injectId,
      request,
    }: {
      injectId: string
      request: RejectInjectRequest
    }) => injectService.rejectInject(exerciseId, injectId, request),
    onMutate: async ({ injectId, request }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousInjects = queryClient.getQueryData<InjectDto[]>(queryKey)

      // Optimistic update - reject returns to Draft
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === injectId
            ? {
                ...inject,
                status: InjectStatus.Draft,
                rejectedAt: new Date().toISOString(),
                rejectionReason: request.reason,
                updatedAt: new Date().toISOString(),
              }
            : inject,
        ),
      )

      return { previousInjects }
    },
    onSuccess: (rejectedInject) => {
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === rejectedInject.id ? rejectedInject : inject,
        ),
      )
      queryClient.setQueryData(
        injectKeys.detail(exerciseId, rejectedInject.id),
        rejectedInject,
      )
      // Invalidate approval status
      queryClient.invalidateQueries({
        queryKey: ['exercises', exerciseId, 'approval-status'],
      })
      toast.success('Inject rejected')
    },
    onError: (err, _variables, context) => {
      if (context?.previousInjects) {
        queryClient.setQueryData(queryKey, context.previousInjects)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to reject inject'
      toast.error(message)
    },
  })

  // Mutation for batch approve (S05)
  const batchApproveMutation = useMutation({
    mutationFn: (request: BatchApproveRequest) =>
      injectService.batchApprove(exerciseId, request),
    onSuccess: (result) => {
      // Invalidate inject list to get fresh data
      queryClient.invalidateQueries({ queryKey })
      // Invalidate approval status
      queryClient.invalidateQueries({
        queryKey: ['exercises', exerciseId, 'approval-status'],
      })

      const message =
        result.skippedCount > 0
          ? `${result.approvedCount} approved, ${result.skippedCount} skipped`
          : `${result.approvedCount} injects approved`
      toast.success(message)
    },
    onError: (err) => {
      const message =
        err instanceof Error ? err.message : 'Failed to batch approve'
      toast.error(message)
    },
  })

  // Mutation for batch reject (S05)
  const batchRejectMutation = useMutation({
    mutationFn: (request: BatchRejectRequest) =>
      injectService.batchReject(exerciseId, request),
    onSuccess: (result) => {
      // Invalidate inject list to get fresh data
      queryClient.invalidateQueries({ queryKey })
      // Invalidate approval status
      queryClient.invalidateQueries({
        queryKey: ['exercises', exerciseId, 'approval-status'],
      })

      const message =
        result.skippedCount > 0
          ? `${result.rejectedCount} rejected, ${result.skippedCount} skipped`
          : `${result.rejectedCount} injects rejected`
      toast.success(message)
    },
    onError: (err) => {
      const message =
        err instanceof Error ? err.message : 'Failed to batch reject'
      toast.error(message)
    },
  })

  // Mutation for reverting approval (S09)
  const revertMutation = useMutation({
    mutationFn: ({
      injectId,
      request,
    }: {
      injectId: string
      request: RevertApprovalRequest
    }) => injectService.revertApproval(exerciseId, injectId, request),
    onMutate: async ({ injectId, request }) => {
      await queryClient.cancelQueries({ queryKey })
      const previousInjects = queryClient.getQueryData<InjectDto[]>(queryKey)

      // Optimistic update - revert returns to Submitted
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === injectId
            ? {
                ...inject,
                status: InjectStatus.Submitted,
                revertedAt: new Date().toISOString(),
                revertReason: request.reason,
                updatedAt: new Date().toISOString(),
              }
            : inject,
        ),
      )

      return { previousInjects }
    },
    onSuccess: (revertedInject) => {
      queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
        old.map(inject =>
          inject.id === revertedInject.id ? revertedInject : inject,
        ),
      )
      queryClient.setQueryData(
        injectKeys.detail(exerciseId, revertedInject.id),
        revertedInject,
      )
      // Invalidate approval status
      queryClient.invalidateQueries({
        queryKey: ['exercises', exerciseId, 'approval-status'],
      })
      toast.success('Approval reverted')
    },
    onError: (err, _variables, context) => {
      if (context?.previousInjects) {
        queryClient.setQueryData(queryKey, context.previousInjects)
      }
      const message =
        err instanceof Error ? err.message : 'Failed to revert approval'
      toast.error(message)
    },
  })

  // Wrapper functions
  const submitForApproval = async (injectId: string) => {
    return submitMutation.mutateAsync(injectId)
  }

  const approveInject = async (
    injectId: string,
    request?: ApproveInjectRequest,
  ) => {
    return approveMutation.mutateAsync({ injectId, request })
  }

  const rejectInject = async (injectId: string, request: RejectInjectRequest) => {
    return rejectMutation.mutateAsync({ injectId, request })
  }

  const batchApprove = async (request: BatchApproveRequest) => {
    return batchApproveMutation.mutateAsync(request)
  }

  const batchReject = async (request: BatchRejectRequest) => {
    return batchRejectMutation.mutateAsync(request)
  }

  const revertApproval = async (
    injectId: string,
    request: RevertApprovalRequest,
  ) => {
    return revertMutation.mutateAsync({ injectId, request })
  }

  return {
    // Actions
    submitForApproval,
    approveInject,
    rejectInject,
    batchApprove,
    batchReject,
    revertApproval,
    // Loading states
    isSubmitting: submitMutation.isPending,
    isApproving: approveMutation.isPending,
    isRejecting: rejectMutation.isPending,
    isBatchApproving: batchApproveMutation.isPending,
    isBatchRejecting: batchRejectMutation.isPending,
    isReverting: revertMutation.isPending,
  }
}

export default useInjectApproval
