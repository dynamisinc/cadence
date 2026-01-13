/**
 * Exercise API Service
 *
 * Handles all API calls for exercise CRUD operations.
 */

import { apiClient } from '../../../core/services/api'
import type {
  ExerciseDto,
  CreateExerciseRequest,
  UpdateExerciseRequest,
} from '../types'

export const exerciseService = {
  /**
   * Get all exercises for the current user
   */
  getExercises: async (): Promise<ExerciseDto[]> => {
    const response = await apiClient.get<ExerciseDto[]>('/api/exercises')
    return response.data
  },

  /**
   * Get a single exercise by ID
   */
  getExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.get<ExerciseDto>(`/api/exercises/${id}`)
    return response.data
  },

  /**
   * Create a new exercise
   */
  createExercise: async (
    request: CreateExerciseRequest,
  ): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      '/api/exercises',
      request,
    )
    return response.data
  },

  /**
   * Update an existing exercise
   */
  updateExercise: async (
    id: string,
    request: UpdateExerciseRequest,
  ): Promise<ExerciseDto> => {
    const response = await apiClient.put<ExerciseDto>(
      `/api/exercises/${id}`,
      request,
    )
    return response.data
  },
}

export default exerciseService
