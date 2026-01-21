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
} from '../types'

export const injectService = {
  /**
   * Get all injects for an exercise
   */
  getInjects: async (exerciseId: string): Promise<InjectDto[]> => {
    const response = await apiClient.get<InjectDto[]>(
      `/api/exercises/${exerciseId}/injects`,
    )
    return response.data
  },

  /**
   * Get a single inject by ID
   */
  getInject: async (exerciseId: string, id: string): Promise<InjectDto> => {
    const response = await apiClient.get<InjectDto>(
      `/api/exercises/${exerciseId}/injects/${id}`,
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
      `/api/exercises/${exerciseId}/injects`,
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
      `/api/exercises/${exerciseId}/injects/${id}`,
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
      `/api/exercises/${exerciseId}/injects/${id}/fire`,
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
      `/api/exercises/${exerciseId}/injects/${id}/skip`,
      request,
    )
    return response.data
  },

  /**
   * Reset an inject back to pending
   */
  resetInject: async (exerciseId: string, id: string): Promise<InjectDto> => {
    const response = await apiClient.post<InjectDto>(
      `/api/exercises/${exerciseId}/injects/${id}/reset`,
    )
    return response.data
  },

  /**
   * Delete an inject
   */
  deleteInject: async (exerciseId: string, id: string): Promise<void> => {
    await apiClient.delete(`/api/exercises/${exerciseId}/injects/${id}`)
  },

  /**
   * Reorder injects by providing new sequence order
   */
  reorderInjects: async (exerciseId: string, injectIds: string[]): Promise<void> => {
    await apiClient.post(`/api/exercises/${exerciseId}/injects/reorder`, {
      injectIds,
    })
  },
}

export default injectService
