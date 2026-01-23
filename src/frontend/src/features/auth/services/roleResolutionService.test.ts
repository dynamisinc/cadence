/**
 * Role Resolution Service Tests
 *
 * @module features/auth
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { roleResolutionService } from './roleResolutionService'
import { apiClient } from '@/core/services/api'

vi.mock('@/core/services/api', () => ({
  apiClient: {
    get: vi.fn(),
  },
}))

describe('roleResolutionService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('getExerciseParticipants', () => {
    it('fetches participants from correct endpoint', async () => {
      const exerciseId = '123e4567-e89b-12d3-a456-426614174000'
      const mockParticipants = [
        {
          userId: 'user1',
          email: 'director@example.com',
          displayName: 'Jane Director',
          systemRole: 'Manager',
          exerciseRole: 'ExerciseDirector',
        },
        {
          userId: 'user2',
          email: 'controller@example.com',
          displayName: 'John Controller',
          systemRole: 'User',
          exerciseRole: 'Controller',
        },
      ]

      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockParticipants })

      const result = await roleResolutionService.getExerciseParticipants(exerciseId)

      expect(apiClient.get).toHaveBeenCalledWith(`/api/exercises/${exerciseId}/participants`)
      expect(result).toEqual(mockParticipants)
    })

    it('throws error when API call fails', async () => {
      const exerciseId = '123e4567-e89b-12d3-a456-426614174000'
      vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'))

      await expect(roleResolutionService.getExerciseParticipants(exerciseId)).rejects.toThrow(
        'Network error',
      )
    })
  })

  describe('getUserExerciseRole', () => {
    it('returns exercise role when user is participant', async () => {
      const exerciseId = '123e4567-e89b-12d3-a456-426614174000'
      const userId = 'user2'
      const mockParticipants = [
        {
          userId: 'user1',
          email: 'director@example.com',
          displayName: 'Jane Director',
          systemRole: 'Manager',
          exerciseRole: 'ExerciseDirector',
        },
        {
          userId: 'user2',
          email: 'controller@example.com',
          displayName: 'John Controller',
          systemRole: 'User',
          exerciseRole: 'Controller',
        },
      ]

      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockParticipants })

      const result = await roleResolutionService.getUserExerciseRole(exerciseId, userId)

      expect(result).toBe('Controller')
    })

    it('returns null when user is not a participant', async () => {
      const exerciseId = '123e4567-e89b-12d3-a456-426614174000'
      const userId = 'user99'
      const mockParticipants = [
        {
          userId: 'user1',
          email: 'director@example.com',
          displayName: 'Jane Director',
          systemRole: 'Manager',
          exerciseRole: 'ExerciseDirector',
        },
      ]

      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockParticipants })

      const result = await roleResolutionService.getUserExerciseRole(exerciseId, userId)

      expect(result).toBeNull()
    })

    it('returns null when participants list is empty', async () => {
      const exerciseId = '123e4567-e89b-12d3-a456-426614174000'
      const userId = 'user1'

      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: [] })

      const result = await roleResolutionService.getUserExerciseRole(exerciseId, userId)

      expect(result).toBeNull()
    })
  })
})
