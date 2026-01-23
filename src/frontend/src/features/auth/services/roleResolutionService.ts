/**
 * Role Resolution Service
 *
 * API client for role resolution and permission checking.
 * Determines effective user roles in exercise contexts.
 *
 * @module features/auth
 */
import { apiClient } from '@/core/services/api'
import type { ExerciseRole } from '../constants/rolePermissions'

/**
 * Exercise participant DTO matching backend
 */
export interface ExerciseParticipantDto {
  userId: string;
  email: string;
  displayName: string;
  systemRole: string;
  exerciseRole: string;
}

/**
 * Exercise assignment DTO for user profile display
 */
export interface ExerciseAssignmentDto extends ExerciseParticipantDto {
  exerciseId: string;
  exerciseName: string;
  role: ExerciseRole;
}

export const roleResolutionService = {
  /**
   * Get all participants for an exercise
   * Used to determine if user has an exercise-specific role
   */
  getExerciseParticipants: async (exerciseId: string): Promise<ExerciseParticipantDto[]> => {
    const response = await apiClient.get<ExerciseParticipantDto[]>(
      `/api/exercises/${exerciseId}/participants`,
    )
    return response.data
  },

  /**
   * Get user's exercise-specific role (or null if not a participant)
   *
   * @param exerciseId - Exercise ID
   * @param userId - User ID
   * @returns Exercise role or null if not assigned
   */
  getUserExerciseRole: async (
    exerciseId: string,
    userId: string,
  ): Promise<ExerciseRole | null> => {
    const participants = await roleResolutionService.getExerciseParticipants(exerciseId)
    const participant = participants.find(p => p.userId === userId)
    return participant ? (participant.exerciseRole as ExerciseRole) : null
  },

  /**
   * Get all exercise assignments for a user
   * Returns list of exercises where user has a specific role assignment
   *
   * @param userId - User ID
   * @returns Array of exercise assignments
   */
  getUserExerciseAssignments: async (userId: string): Promise<ExerciseAssignmentDto[]> => {
    const response = await apiClient.get<ExerciseAssignmentDto[]>(
      `/api/users/${userId}/exercise-assignments`,
    )
    return response.data.map(assignment => ({
      ...assignment,
      role: assignment.exerciseRole as ExerciseRole,
    }))
  },
}
