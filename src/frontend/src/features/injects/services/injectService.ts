/**
 * Inject API Service
 *
 * Handles all API calls for inject CRUD operations.
 */

import { apiClient } from '../../../core/services/api'
import type {
  InjectDto,
  CreateInjectRequest,
  UpdateInjectRequest,
  FireInjectRequest,
  SkipInjectRequest,
  ApproveInjectRequest,
  RejectInjectRequest,
  RevertApprovalRequest,
  BatchApproveRequest,
  BatchRejectRequest,
  BatchApprovalResult,
} from '../types'
import type { InjectApprovalCheckDto } from '@/types'

export const injectService = {
  /**
   * Get all injects for an exercise
   */
  getInjects: async (exerciseId: string): Promise<InjectDto[]> => {
    const response = await apiClient.get<InjectDto[]>(
      `/exercises/${exerciseId}/injects`,
    )
    return response.data
  },

  /**
   * Get a single inject by ID
   */
  getInject: async (exerciseId: string, id: string): Promise<InjectDto> => {
    const response = await apiClient.get<InjectDto>(
      `/exercises/${exerciseId}/injects/${id}`,
    )
    return response.data
  },

  /**
   * Create a new inject
   */
  createInject: async (
    exerciseId: string,
    request: CreateInjectRequest,
  ): Promise<InjectDto> => {
    const response = await apiClient.post<InjectDto>(
      `/exercises/${exerciseId}/injects`,
      request,
    )
    return response.data
  },

  /**
   * Update an existing inject
   */
  updateInject: async (
    exerciseId: string,
    id: string,
    request: UpdateInjectRequest,
  ): Promise<InjectDto> => {
    const response = await apiClient.put<InjectDto>(
      `/exercises/${exerciseId}/injects/${id}`,
      request,
    )
    return response.data
  },

  /**
   * Fire (deliver) an inject
   */
  fireInject: async (
    exerciseId: string,
    id: string,
    request?: FireInjectRequest,
  ): Promise<InjectDto> => {
    const response = await apiClient.post<InjectDto>(
      `/exercises/${exerciseId}/injects/${id}/fire`,
      request ?? {},
    )
    return response.data
  },

  /**
   * Skip an inject
   */
  skipInject: async (
    exerciseId: string,
    id: string,
    request: SkipInjectRequest,
  ): Promise<InjectDto> => {
    const response = await apiClient.post<InjectDto>(
      `/exercises/${exerciseId}/injects/${id}/skip`,
      request,
    )
    return response.data
  },

  /**
   * Reset an inject back to pending
   */
  resetInject: async (exerciseId: string, id: string): Promise<InjectDto> => {
    const response = await apiClient.post<InjectDto>(
      `/exercises/${exerciseId}/injects/${id}/reset`,
    )
    return response.data
  },

  /**
   * Delete an inject
   */
  deleteInject: async (exerciseId: string, id: string): Promise<void> => {
    await apiClient.delete(`/exercises/${exerciseId}/injects/${id}`)
  },

  /**
   * Reorder injects by providing new sequence order
   */
  reorderInjects: async (exerciseId: string, injectIds: string[]): Promise<void> => {
    await apiClient.post(`/exercises/${exerciseId}/injects/reorder`, {
      injectIds,
    })
  },

  // ==========================================================================
  // Approval Workflow (S00-S09)
  // ==========================================================================

  /**
   * Submit an inject for approval (S03)
   */
  submitForApproval: async (exerciseId: string, id: string): Promise<InjectDto> => {
    const response = await apiClient.post<InjectDto>(
      `/exercises/${exerciseId}/injects/${id}/submit`,
    )
    return response.data
  },

  /**
   * Approve a submitted inject (S04)
   */
  approveInject: async (
    exerciseId: string,
    id: string,
    request?: ApproveInjectRequest,
  ): Promise<InjectDto> => {
    const response = await apiClient.post<InjectDto>(
      `/exercises/${exerciseId}/injects/${id}/approve`,
      request ?? {},
    )
    return response.data
  },

  /**
   * Reject a submitted inject (S04)
   */
  rejectInject: async (
    exerciseId: string,
    id: string,
    request: RejectInjectRequest,
  ): Promise<InjectDto> => {
    const response = await apiClient.post<InjectDto>(
      `/exercises/${exerciseId}/injects/${id}/reject`,
      request,
    )
    return response.data
  },

  /**
   * Batch approve multiple injects (S05)
   */
  batchApprove: async (
    exerciseId: string,
    request: BatchApproveRequest,
  ): Promise<BatchApprovalResult> => {
    const response = await apiClient.post<BatchApprovalResult>(
      `/exercises/${exerciseId}/injects/batch/approve`,
      request,
    )
    return response.data
  },

  /**
   * Batch reject multiple injects (S05)
   */
  batchReject: async (
    exerciseId: string,
    request: BatchRejectRequest,
  ): Promise<BatchApprovalResult> => {
    const response = await apiClient.post<BatchApprovalResult>(
      `/exercises/${exerciseId}/injects/batch/reject`,
      request,
    )
    return response.data
  },

  /**
   * Revert an approved inject back to submitted (S09)
   */
  revertApproval: async (
    exerciseId: string,
    id: string,
    request: RevertApprovalRequest,
  ): Promise<InjectDto> => {
    const response = await apiClient.post<InjectDto>(
      `/exercises/${exerciseId}/injects/${id}/revert`,
      request,
    )
    return response.data
  },

  // ==========================================================================
  // Approval Permission Checks (S11)
  // ==========================================================================

  /**
   * Check if current user can approve a specific inject (S11)
   * Returns permission result including self-approval check
   */
  canApproveInject: async (
    exerciseId: string,
    id: string,
  ): Promise<InjectApprovalCheckDto> => {
    const response = await apiClient.get<InjectApprovalCheckDto>(
      `/exercises/${exerciseId}/injects/${id}/can-approve`,
    )
    return response.data
  },

  /**
   * Check if current user can approve any inject for this exercise (S11)
   * General permission check based on role
   */
  canApproveAny: async (exerciseId: string): Promise<boolean> => {
    const response = await apiClient.get<{ canApprove: boolean }>(
      `/exercises/${exerciseId}/injects/can-approve`,
    )
    return response.data.canApprove
  },
}

export default injectService
