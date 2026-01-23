/**
 * Exercise Participant API Service
 *
 * Handles all API calls for exercise participant management (S14).
 * Exercise participants are users assigned specific HSEEP roles for an exercise.
 */

import { apiClient } from '../../../core/services/api'
import type {
  ExerciseParticipantDto,
  ParticipantsListResponse,
  AddParticipantRequest,
  UpdateParticipantRoleRequest,
} from '../types'

export const participantService = {
  /**
   * Get all participants for an exercise
   */
  getParticipants: async (exerciseId: string): Promise<ExerciseParticipantDto[]> => {
    const response = await apiClient.get<ParticipantsListResponse>(
      `/exercises/${exerciseId}/participants`,
    )
    return response.data.participants
  },

  /**
   * Add a participant to an exercise
   */
  addParticipant: async (
    exerciseId: string,
    request: AddParticipantRequest,
  ): Promise<ExerciseParticipantDto> => {
    const response = await apiClient.post<ExerciseParticipantDto>(
      `/exercises/${exerciseId}/participants`,
      request,
    )
    return response.data
  },

  /**
   * Update a participant's exercise role
   */
  updateParticipantRole: async (
    exerciseId: string,
    userId: string,
    request: UpdateParticipantRoleRequest,
  ): Promise<ExerciseParticipantDto> => {
    const response = await apiClient.put<ExerciseParticipantDto>(
      `/exercises/${exerciseId}/participants/${userId}/role`,
      request,
    )
    return response.data
  },

  /**
   * Remove a participant from an exercise
   */
  removeParticipant: async (exerciseId: string, userId: string): Promise<void> => {
    await apiClient.delete(`/exercises/${exerciseId}/participants/${userId}`)
  },
}

export default participantService
