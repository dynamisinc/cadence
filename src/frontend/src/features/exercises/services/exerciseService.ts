/**
 * Exercise API Service
 *
 * Handles all API calls for exercise CRUD operations and status workflow.
 */

import { apiClient } from '../../../core/services/api'
import type { ExerciseStatus } from '../../../types'
import type {
  ExerciseDto,
  CreateExerciseRequest,
  UpdateExerciseRequest,
  DuplicateExerciseRequest,
  MselDto,
  MselSummaryDto,
  SetupProgressDto,
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

  /**
   * Duplicate an exercise with all its configuration
   */
  duplicateExercise: async (
    id: string,
    request?: DuplicateExerciseRequest,
  ): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/api/exercises/${id}/duplicate`,
      request ?? {},
    )
    return response.data
  },

  // =========================================================================
  // Status Workflow Methods
  // =========================================================================

  /**
   * Activate an exercise (Draft → Active)
   * Requires at least one inject in the MSEL.
   */
  activateExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/api/exercises/${id}/activate`,
    )
    return response.data
  },

  /**
   * Pause an exercise (Active → Paused)
   * Preserves clock elapsed time.
   */
  pauseExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/api/exercises/${id}/pause`,
    )
    return response.data
  },

  /**
   * Resume a paused exercise (Paused → Active)
   */
  resumeExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/api/exercises/${id}/resume`,
    )
    return response.data
  },

  /**
   * Complete an exercise (Active/Paused → Completed)
   * Permanently stops the clock.
   */
  completeExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/api/exercises/${id}/complete`,
    )
    return response.data
  },

  /**
   * Archive a completed exercise (Completed → Archived)
   * Makes the exercise fully read-only.
   */
  archiveExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/api/exercises/${id}/archive`,
    )
    return response.data
  },

  /**
   * Unarchive an exercise (Archived → Completed)
   * Restores the exercise to completed status.
   */
  unarchiveExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/api/exercises/${id}/unarchive`,
    )
    return response.data
  },

  /**
   * Revert a paused exercise to draft (Paused → Draft)
   * WARNING: This clears all conduct data (fired times, observations).
   */
  revertToDraft: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/api/exercises/${id}/revert-to-draft`,
    )
    return response.data
  },

  /**
   * Get available status transitions for an exercise
   */
  getAvailableTransitions: async (id: string): Promise<ExerciseStatus[]> => {
    const response = await apiClient.get<ExerciseStatus[]>(
      `/api/exercises/${id}/available-transitions`,
    )
    return response.data
  },

  // =========================================================================
  // MSEL Methods
  // =========================================================================

  /**
   * Get the active MSEL summary for an exercise
   */
  getActiveMselSummary: async (exerciseId: string): Promise<MselSummaryDto> => {
    const response = await apiClient.get<MselSummaryDto>(
      `/api/exercises/${exerciseId}/msel/summary`,
    )
    return response.data
  },

  /**
   * Get all MSELs for an exercise
   */
  getMsels: async (exerciseId: string): Promise<MselDto[]> => {
    const response = await apiClient.get<MselDto[]>(
      `/api/exercises/${exerciseId}/msels`,
    )
    return response.data
  },

  /**
   * Get a specific MSEL summary by ID
   */
  getMselSummary: async (mselId: string): Promise<MselSummaryDto> => {
    const response = await apiClient.get<MselSummaryDto>(
      `/api/exercises/msels/${mselId}/summary`,
    )
    return response.data
  },

  // =========================================================================
  // Setup Progress Methods
  // =========================================================================

  /**
   * Get the setup progress for an exercise
   */
  getSetupProgress: async (exerciseId: string): Promise<SetupProgressDto> => {
    const response = await apiClient.get<SetupProgressDto>(
      `/api/exercises/${exerciseId}/setup-progress`,
    )
    return response.data
  },
}

export default exerciseService
