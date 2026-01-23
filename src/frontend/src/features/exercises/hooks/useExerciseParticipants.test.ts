/**
 * Tests for useExerciseParticipants hook
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { useExerciseParticipants } from './useExerciseParticipants'
import { participantService } from '../services/participantService'
import { createTestWrapper } from '../../../test/testWrapper'
import { toast } from 'react-toastify'

vi.mock('../services/participantService')
vi.mock('react-toastify', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

describe('useExerciseParticipants', () => {
  const exerciseId = 'ex-123'

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('fetching participants', () => {
    it('fetches participants on mount', async () => {
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

      vi.mocked(participantService.getParticipants).mockResolvedValueOnce(
        mockParticipants,
      )

      const { result } = renderHook(() => useExerciseParticipants(exerciseId), {
        wrapper: createTestWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(result.current.participants).toEqual(mockParticipants)
      expect(participantService.getParticipants).toHaveBeenCalledWith(exerciseId)
    })

    it('handles fetch error', async () => {
      vi.mocked(participantService.getParticipants).mockRejectedValueOnce(
        new Error('Network error'),
      )

      const { result } = renderHook(() => useExerciseParticipants(exerciseId), {
        wrapper: createTestWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isError).toBe(true)
      })

      expect(result.current.error).toBeTruthy()
    })
  })

  describe('addParticipant', () => {
    it('adds participant and shows success toast', async () => {
      const mockParticipants = []
      const newParticipant = {
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

      vi.mocked(participantService.getParticipants).mockResolvedValueOnce(
        mockParticipants,
      )
      vi.mocked(participantService.addParticipant).mockResolvedValueOnce(
        newParticipant,
      )

      const { result } = renderHook(() => useExerciseParticipants(exerciseId), {
        wrapper: createTestWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      await result.current.addParticipant({ userId: 'u1', role: 'Controller' })

      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith('Participant added')
      })

      expect(participantService.addParticipant).toHaveBeenCalledWith(exerciseId, {
        userId: 'u1',
        role: 'Controller',
      })
    })

    it('handles add error and shows error toast', async () => {
      vi.mocked(participantService.getParticipants).mockResolvedValueOnce([])
      vi.mocked(participantService.addParticipant).mockRejectedValueOnce(
        new Error('User already assigned'),
      )

      const { result } = renderHook(() => useExerciseParticipants(exerciseId), {
        wrapper: createTestWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      try {
        await result.current.addParticipant({ userId: 'u1', role: 'Controller' })
      } catch {
        // Expected error
      }

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith('User already assigned')
      })
    })
  })

  describe('updateParticipantRole', () => {
    it('updates participant role and shows success toast', async () => {
      const existingParticipant = {
        participantId: 'p1',
        userId: 'u1',
        displayName: 'Jane Smith',
        email: 'jane@example.com',
        exerciseRole: 'Observer',
        systemRole: 'User',
        effectiveRole: 'Observer',
        addedAt: '2025-01-21T12:00:00Z',
        addedBy: 'admin-id',
      }

      const updatedParticipant = {
        ...existingParticipant,
        exerciseRole: 'Evaluator',
        effectiveRole: 'Evaluator',
      }

      vi.mocked(participantService.getParticipants).mockResolvedValueOnce([
        existingParticipant,
      ])
      vi.mocked(participantService.updateParticipantRole).mockResolvedValueOnce(
        updatedParticipant,
      )

      const { result } = renderHook(() => useExerciseParticipants(exerciseId), {
        wrapper: createTestWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      await result.current.updateParticipantRole('u1', { role: 'Evaluator' })

      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith('Role updated')
      })

      expect(participantService.updateParticipantRole).toHaveBeenCalledWith(
        exerciseId,
        'u1',
        { role: 'Evaluator' },
      )
    })
  })

  describe('removeParticipant', () => {
    it('removes participant and shows success toast', async () => {
      const participant = {
        participantId: 'p1',
        userId: 'u1',
        displayName: 'Jane Smith',
        email: 'jane@example.com',
        exerciseRole: 'Observer',
        systemRole: 'User',
        effectiveRole: 'Observer',
        addedAt: '2025-01-21T12:00:00Z',
        addedBy: 'admin-id',
      }

      vi.mocked(participantService.getParticipants).mockResolvedValueOnce([
        participant,
      ])
      vi.mocked(participantService.removeParticipant).mockResolvedValueOnce()

      const { result } = renderHook(() => useExerciseParticipants(exerciseId), {
        wrapper: createTestWrapper(),
      })

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      await result.current.removeParticipant('u1')

      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith('Participant removed')
      })

      expect(participantService.removeParticipant).toHaveBeenCalledWith(
        exerciseId,
        'u1',
      )
    })
  })
})
