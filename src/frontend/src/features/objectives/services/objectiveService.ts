/**
 * Objective API Service
 *
 * Handles all API calls for objective CRUD operations.
 */

import { apiClient } from '../../../core/services/api'
import type {
  ObjectiveDto,
  ObjectiveSummaryDto,
  CreateObjectiveRequest,
  UpdateObjectiveRequest,
} from '../types'

export const objectiveService = {
  /**
   * Get all objectives for an exercise
   */
  getObjectives: async (exerciseId: string): Promise<ObjectiveDto[]> => {
    const response = await apiClient.get<ObjectiveDto[]>(
      `/api/exercises/${exerciseId}/objectives`,
    )
    return response.data
  },

  /**
   * Get lightweight objective summaries for dropdowns
   */
  getObjectiveSummaries: async (exerciseId: string): Promise<ObjectiveSummaryDto[]> => {
    const response = await apiClient.get<ObjectiveSummaryDto[]>(
      `/api/exercises/${exerciseId}/objectives/summaries`,
    )
    return response.data
  },

  /**
   * Get a single objective by ID
   */
  getObjective: async (exerciseId: string, id: string): Promise<ObjectiveDto> => {
    const response = await apiClient.get<ObjectiveDto>(
      `/api/exercises/${exerciseId}/objectives/${id}`,
    )
    return response.data
  },

  /**
   * Create a new objective
   */
  createObjective: async (
    exerciseId: string,
    request: CreateObjectiveRequest,
  ): Promise<ObjectiveDto> => {
    const response = await apiClient.post<ObjectiveDto>(
      `/api/exercises/${exerciseId}/objectives`,
      request,
    )
    return response.data
  },

  /**
   * Update an existing objective
   */
  updateObjective: async (
    exerciseId: string,
    id: string,
    request: UpdateObjectiveRequest,
  ): Promise<ObjectiveDto> => {
    const response = await apiClient.put<ObjectiveDto>(
      `/api/exercises/${exerciseId}/objectives/${id}`,
      request,
    )
    return response.data
  },

  /**
   * Delete an objective (only if no injects are linked)
   */
  deleteObjective: async (exerciseId: string, id: string): Promise<void> => {
    await apiClient.delete(`/api/exercises/${exerciseId}/objectives/${id}`)
  },

  /**
   * Check if an objective number is available
   */
  checkObjectiveNumber: async (
    exerciseId: string,
    number: string,
    excludeId?: string,
  ): Promise<{ isAvailable: boolean }> => {
    const params = new URLSearchParams({ number })
    if (excludeId) {
      params.append('excludeId', excludeId)
    }
    const response = await apiClient.get<{ isAvailable: boolean }>(
      `/api/exercises/${exerciseId}/objectives/check-number?${params}`,
    )
    return response.data
  },
}

export default objectiveService
