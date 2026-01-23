/**
 * Tests for participantService
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { participantService } from './participantService'
import { apiClient } from '../../../core/services/api'

vi.mock('../../../core/services/api', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}))

describe('participantService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('getParticipants', () => {
    it('fetches participants from correct endpoint', async () => {
      const exerciseId = 'ex-123'
      const mockParticipants = [
        {
          participantId: 'p1',
          userId: 'u1',
          displayName: 'Jane Smith',
          email: 'jane@example.com',
          exerciseRole: 'Evaluator',
          systemRole: 'User',
          effectiveRole: 'Evaluator',
          addedAt: '2025-01-21T12:00:00Z',
          addedBy: 'admin-id',
        },
      ]

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: mockParticipants,
      })

      const result = await participantService.getParticipants(exerciseId)

      expect(apiClient.get).toHaveBeenCalledWith('/exercises/ex-123/participants')
      expect(result).toEqual(mockParticipants)
    })
  })

  describe('addParticipant', () => {
    it('posts to correct endpoint with request body', async () => {
      const exerciseId = 'ex-123'
      const request = { userId: 'u1', role: 'Controller' }
      const mockResponse = {
        participantId: 'p1',
        userId: 'u1',
        displayName: 'John Doe',
        email: 'john@example.com',
        exerciseRole: 'Controller',
        systemRole: 'User',
        effectiveRole: 'Controller',
        addedAt: '2025-01-21T12:00:00Z',
        addedBy: 'admin-id',
      }

      vi.mocked(apiClient.post).mockResolvedValueOnce({ data: mockResponse })

      const result = await participantService.addParticipant(exerciseId, request)

      expect(apiClient.post).toHaveBeenCalledWith(
        '/exercises/ex-123/participants',
        request,
      )
      expect(result).toEqual(mockResponse)
    })
  })

  describe('updateParticipantRole', () => {
    it('updates participant role at correct endpoint', async () => {
      const exerciseId = 'ex-123'
      const userId = 'u1'
      const request = { role: 'ExerciseDirector' }
      const mockResponse = {
        participantId: 'p1',
        userId: 'u1',
        displayName: 'Jane Smith',
        email: 'jane@example.com',
        exerciseRole: 'ExerciseDirector',
        systemRole: 'Manager',
        effectiveRole: 'ExerciseDirector',
        addedAt: '2025-01-21T12:00:00Z',
        addedBy: 'admin-id',
      }

      vi.mocked(apiClient.put).mockResolvedValueOnce({ data: mockResponse })

      const result = await participantService.updateParticipantRole(
        exerciseId,
        userId,
        request,
      )

      expect(apiClient.put).toHaveBeenCalledWith(
        '/exercises/ex-123/participants/u1/role',
        request,
      )
      expect(result).toEqual(mockResponse)
    })
  })

  describe('removeParticipant', () => {
    it('deletes at correct endpoint', async () => {
      const exerciseId = 'ex-123'
      const userId = 'u1'

      vi.mocked(apiClient.delete).mockResolvedValueOnce({ data: undefined })

      await participantService.removeParticipant(exerciseId, userId)

      expect(apiClient.delete).toHaveBeenCalledWith(
        '/exercises/ex-123/participants/u1',
      )
    })
  })
})
