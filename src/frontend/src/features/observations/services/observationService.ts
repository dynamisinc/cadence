/**
 * Observation API Service
 *
 * Handles all API calls for observation CRUD operations.
 */

import { apiClient } from '../../../core/services/api'
import type {
  ObservationDto,
  CreateObservationRequest,
  UpdateObservationRequest,
} from '../types'

export const observationService = {
  /**
   * Get all observations for an exercise
   */
  getObservationsByExercise: async (exerciseId: string): Promise<ObservationDto[]> => {
    const response = await apiClient.get<ObservationDto[]>(
      `/exercises/${exerciseId}/observations`,
    )
    return response.data
  },

  /**
   * Get all observations for an inject
   */
  getObservationsByInject: async (injectId: string): Promise<ObservationDto[]> => {
    const response = await apiClient.get<ObservationDto[]>(
      `/injects/${injectId}/observations`,
    )
    return response.data
  },

  /**
   * Get a single observation by ID
   */
  getObservation: async (id: string): Promise<ObservationDto> => {
    const response = await apiClient.get<ObservationDto>(`/observations/${id}`)
    return response.data
  },

  /**
   * Create a new observation for an exercise
   */
  createObservation: async (
    exerciseId: string,
    request: CreateObservationRequest,
  ): Promise<ObservationDto> => {
    const response = await apiClient.post<ObservationDto>(
      `/exercises/${exerciseId}/observations`,
      request,
    )
    return response.data
  },

  /**
   * Update an existing observation
   */
  updateObservation: async (
    id: string,
    request: UpdateObservationRequest,
  ): Promise<ObservationDto> => {
    const response = await apiClient.put<ObservationDto>(
      `/observations/${id}`,
      request,
    )
    return response.data
  },

  /**
   * Delete an observation (soft delete)
   */
  deleteObservation: async (id: string): Promise<void> => {
    await apiClient.delete(`/observations/${id}`)
  },
}

export default observationService
