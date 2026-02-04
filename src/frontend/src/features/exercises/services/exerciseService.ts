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
  DeleteSummaryResponse,
  ExerciseSettingsDto,
  UpdateExerciseSettingsRequest,
  ApprovalSettingsDto,
  UpdateApprovalSettingsRequest,
  ApprovalStatusDto,
  PublishValidationResult,
} from '../types'

export const exerciseService = {
  /**
   * Get all exercises with optional archive filtering
   * @param includeArchived Include archived exercises (default: false)
   * @param archivedOnly Return only archived exercises (default: false)
   */
  getExercises: async (options?: {
    includeArchived?: boolean
    archivedOnly?: boolean
  }): Promise<ExerciseDto[]> => {
    const params = new URLSearchParams()
    if (options?.includeArchived) {
      params.append('includeArchived', 'true')
    }
    if (options?.archivedOnly) {
      params.append('archivedOnly', 'true')
    }
    const queryString = params.toString()
    const url = queryString ? `/exercises?${queryString}` : '/exercises'
    const response = await apiClient.get<ExerciseDto[]>(url)
    return response.data
  },

  /**
   * Get a single exercise by ID
   */
  getExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.get<ExerciseDto>(`/exercises/${id}`)
    return response.data
  },

  /**
   * Create a new exercise
   */
  createExercise: async (
    request: CreateExerciseRequest,
  ): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      '/exercises',
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
      `/exercises/${id}`,
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
      `/exercises/${id}/duplicate`,
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
      `/exercises/${id}/activate`,
    )
    return response.data
  },

  /**
   * Pause an exercise (Active → Paused)
   * Preserves clock elapsed time.
   */
  pauseExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/exercises/${id}/pause`,
    )
    return response.data
  },

  /**
   * Resume a paused exercise (Paused → Active)
   */
  resumeExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/exercises/${id}/resume`,
    )
    return response.data
  },

  /**
   * Complete an exercise (Active/Paused → Completed)
   * Permanently stops the clock.
   */
  completeExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/exercises/${id}/complete`,
    )
    return response.data
  },

  /**
   * Archive a completed exercise (Completed → Archived)
   * Makes the exercise fully read-only.
   */
  archiveExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/exercises/${id}/archive`,
    )
    return response.data
  },

  /**
   * Unarchive an exercise (Archived → Completed)
   * Restores the exercise to completed status.
   */
  unarchiveExercise: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/exercises/${id}/unarchive`,
    )
    return response.data
  },

  /**
   * Revert a paused exercise to draft (Paused → Draft)
   * WARNING: This clears all conduct data (fired times, observations).
   */
  revertToDraft: async (id: string): Promise<ExerciseDto> => {
    const response = await apiClient.post<ExerciseDto>(
      `/exercises/${id}/revert-to-draft`,
    )
    return response.data
  },

  /**
   * Get available status transitions for an exercise
   */
  getAvailableTransitions: async (id: string): Promise<ExerciseStatus[]> => {
    const response = await apiClient.get<ExerciseStatus[]>(
      `/exercises/${id}/available-transitions`,
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
      `/exercises/${exerciseId}/msel/summary`,
    )
    return response.data
  },

  /**
   * Get all MSELs for an exercise
   */
  getMsels: async (exerciseId: string): Promise<MselDto[]> => {
    const response = await apiClient.get<MselDto[]>(
      `/exercises/${exerciseId}/msels`,
    )
    return response.data
  },

  /**
   * Get a specific MSEL summary by ID
   */
  getMselSummary: async (mselId: string): Promise<MselSummaryDto> => {
    const response = await apiClient.get<MselSummaryDto>(
      `/exercises/msels/${mselId}/summary`,
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
      `/exercises/${exerciseId}/setup-progress`,
    )
    return response.data
  },

  // =========================================================================
  // Delete Methods
  // =========================================================================

  /**
   * Get a summary of what would be deleted if the exercise is permanently deleted.
   * Also indicates whether the exercise can be deleted based on its status.
   */
  getDeleteSummary: async (id: string): Promise<DeleteSummaryResponse> => {
    const response = await apiClient.get<DeleteSummaryResponse>(
      `/exercises/${id}/delete-summary`,
    )
    return response.data
  },

  /**
   * Permanently delete an exercise and all related data.
   * This action is irreversible.
   */
  deleteExercise: async (id: string): Promise<void> => {
    await apiClient.delete(`/exercises/${id}`)
  },

  // =========================================================================
  // Exercise Settings Methods (S03-S05)
  // =========================================================================

  /**
   * Get the current settings for an exercise
   */
  getSettings: async (id: string): Promise<ExerciseSettingsDto> => {
    const response = await apiClient.get<ExerciseSettingsDto>(
      `/exercises/${id}/settings`,
    )
    return response.data
  },

  /**
   * Update exercise settings
   * Requires Exercise Director or higher role
   */
  updateSettings: async (
    id: string,
    request: UpdateExerciseSettingsRequest,
  ): Promise<ExerciseSettingsDto> => {
    const response = await apiClient.put<ExerciseSettingsDto>(
      `/exercises/${id}/settings`,
      request,
    )
    return response.data
  },

  // =========================================================================
  // Approval Settings Methods (S00-S09)
  // =========================================================================

  /**
   * Get the approval settings for an exercise (S01-S02)
   */
  getApprovalSettings: async (id: string): Promise<ApprovalSettingsDto> => {
    const response = await apiClient.get<ApprovalSettingsDto>(
      `/exercises/${id}/approval-settings`,
    )
    return response.data
  },

  /**
   * Update approval settings for an exercise (S01-S02)
   * Requires Exercise Director or higher role
   */
  updateApprovalSettings: async (
    id: string,
    request: UpdateApprovalSettingsRequest,
  ): Promise<ApprovalSettingsDto> => {
    const response = await apiClient.put<ApprovalSettingsDto>(
      `/exercises/${id}/approval-settings`,
      request,
    )
    return response.data
  },

  /**
   * Get the approval queue status for an exercise (S06)
   */
  getApprovalStatus: async (id: string): Promise<ApprovalStatusDto> => {
    const response = await apiClient.get<ApprovalStatusDto>(
      `/exercises/${id}/approval-status`,
    )
    return response.data
  },

  /**
   * Validate if an exercise can be published (S07)
   */
  validatePublish: async (id: string): Promise<PublishValidationResult> => {
    const response = await apiClient.get<PublishValidationResult>(
      `/exercises/${id}/publish-validation`,
    )
    return response.data
  },
}

export default exerciseService
