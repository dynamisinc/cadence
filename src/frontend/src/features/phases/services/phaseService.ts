/**
 * Phase API Service
 *
 * Handles all API calls for phase CRUD operations.
 */

import { apiClient } from '../../../core/services/api'
import type {
  PhaseDto,
  CreatePhaseRequest,
  UpdatePhaseRequest,
  ReorderPhasesRequest,
} from '../types'

export const phaseService = {
  /**
   * Get all phases for an exercise
   */
  getPhases: async (exerciseId: string): Promise<PhaseDto[]> => {
    const response = await apiClient.get<PhaseDto[]>(
      `/api/exercises/${exerciseId}/phases`,
    )
    return response.data
  },

  /**
   * Get a single phase by ID
   */
  getPhase: async (exerciseId: string, id: string): Promise<PhaseDto> => {
    const response = await apiClient.get<PhaseDto>(
      `/api/exercises/${exerciseId}/phases/${id}`,
    )
    return response.data
  },

  /**
   * Create a new phase
   */
  createPhase: async (
    exerciseId: string,
    request: CreatePhaseRequest,
  ): Promise<PhaseDto> => {
    const response = await apiClient.post<PhaseDto>(
      `/api/exercises/${exerciseId}/phases`,
      request,
    )
    return response.data
  },

  /**
   * Update an existing phase
   */
  updatePhase: async (
    exerciseId: string,
    id: string,
    request: UpdatePhaseRequest,
  ): Promise<PhaseDto> => {
    const response = await apiClient.put<PhaseDto>(
      `/api/exercises/${exerciseId}/phases/${id}`,
      request,
    )
    return response.data
  },

  /**
   * Delete a phase (only if no injects are assigned)
   */
  deletePhase: async (exerciseId: string, id: string): Promise<void> => {
    await apiClient.delete(`/api/exercises/${exerciseId}/phases/${id}`)
  },

  /**
   * Reorder phases
   */
  reorderPhases: async (
    exerciseId: string,
    request: ReorderPhasesRequest,
  ): Promise<PhaseDto[]> => {
    const response = await apiClient.put<PhaseDto[]>(
      `/api/exercises/${exerciseId}/phases/reorder`,
      request,
    )
    return response.data
  },
}

export default phaseService
