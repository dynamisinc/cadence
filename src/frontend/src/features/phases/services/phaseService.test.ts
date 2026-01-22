import { describe, it, expect, vi, beforeEach } from 'vitest'
import { phaseService } from './phaseService'
import { apiClient } from '../../../core/services/api'
import type {
  PhaseDto,
  CreatePhaseRequest,
  UpdatePhaseRequest,
  ReorderPhasesRequest,
} from '../types'

// Mock the API client
vi.mock('../../../core/services/api', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}))

const mockPhase: PhaseDto = {
  id: 'phase-1',
  name: 'Warning Phase',
  description: 'Initial warning period',
  sequence: 1,
  startTime: '08:00:00',
  endTime: '09:00:00',
  exerciseId: 'exercise-1',
  injectCount: 5,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
}

const mockPhase2: PhaseDto = {
  id: 'phase-2',
  name: 'Response Phase',
  description: 'Active response period',
  sequence: 2,
  startTime: '09:00:00',
  endTime: '12:00:00',
  exerciseId: 'exercise-1',
  injectCount: 10,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
}

describe('phaseService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('getPhases', () => {
    it('fetches all phases for an exercise', async () => {
      vi.mocked(apiClient.get).mockResolvedValue({ data: [mockPhase, mockPhase2] })

      const result = await phaseService.getPhases('exercise-1')

      expect(apiClient.get).toHaveBeenCalledWith('/exercises/exercise-1/phases')
      expect(result).toEqual([mockPhase, mockPhase2])
    })

    it('returns empty array when no phases exist', async () => {
      vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

      const result = await phaseService.getPhases('exercise-1')

      expect(apiClient.get).toHaveBeenCalledWith('/exercises/exercise-1/phases')
      expect(result).toEqual([])
    })

    it('propagates API errors', async () => {
      vi.mocked(apiClient.get).mockRejectedValue(new Error('Network error'))

      await expect(phaseService.getPhases('exercise-1')).rejects.toThrow('Network error')
    })
  })

  describe('getPhase', () => {
    it('fetches a single phase by ID', async () => {
      vi.mocked(apiClient.get).mockResolvedValue({ data: mockPhase })

      const result = await phaseService.getPhase('exercise-1', 'phase-1')

      expect(apiClient.get).toHaveBeenCalledWith('/exercises/exercise-1/phases/phase-1')
      expect(result).toEqual(mockPhase)
    })

    it('propagates not found errors', async () => {
      vi.mocked(apiClient.get).mockRejectedValue(new Error('Not found'))

      await expect(phaseService.getPhase('exercise-1', 'nonexistent')).rejects.toThrow(
        'Not found',
      )
    })
  })

  describe('createPhase', () => {
    it('creates a new phase with all fields', async () => {
      const request: CreatePhaseRequest = {
        name: 'New Phase',
        description: 'New phase description',
        startTime: '10:00:00',
        endTime: '11:00:00',
      }
      const createdPhase: PhaseDto = {
        ...mockPhase,
        id: 'phase-3',
        name: request.name,
        description: request.description ?? null,
        startTime: request.startTime ?? null,
        endTime: request.endTime ?? null,
        sequence: 3,
      }
      vi.mocked(apiClient.post).mockResolvedValue({ data: createdPhase })

      const result = await phaseService.createPhase('exercise-1', request)

      expect(apiClient.post).toHaveBeenCalledWith('/exercises/exercise-1/phases', request)
      expect(result).toEqual(createdPhase)
    })

    it('creates a phase with minimal fields', async () => {
      const request: CreatePhaseRequest = {
        name: 'Minimal Phase',
      }
      const createdPhase: PhaseDto = {
        ...mockPhase,
        id: 'phase-4',
        name: request.name,
        description: null,
        startTime: null,
        endTime: null,
      }
      vi.mocked(apiClient.post).mockResolvedValue({ data: createdPhase })

      const result = await phaseService.createPhase('exercise-1', request)

      expect(apiClient.post).toHaveBeenCalledWith('/exercises/exercise-1/phases', request)
      expect(result.name).toBe('Minimal Phase')
      expect(result.description).toBeNull()
    })

    it('propagates validation errors', async () => {
      const request: CreatePhaseRequest = { name: 'AB' } // Too short
      vi.mocked(apiClient.post).mockRejectedValue(new Error('Validation failed'))

      await expect(phaseService.createPhase('exercise-1', request)).rejects.toThrow(
        'Validation failed',
      )
    })
  })

  describe('updatePhase', () => {
    it('updates an existing phase', async () => {
      const request: UpdatePhaseRequest = {
        name: 'Updated Phase Name',
        description: 'Updated description',
        startTime: '08:30:00',
        endTime: '09:30:00',
      }
      const updatedPhase: PhaseDto = {
        ...mockPhase,
        name: request.name,
        description: request.description ?? null,
        startTime: request.startTime ?? null,
        endTime: request.endTime ?? null,
        updatedAt: '2024-01-02T00:00:00Z',
      }
      vi.mocked(apiClient.put).mockResolvedValue({ data: updatedPhase })

      const result = await phaseService.updatePhase('exercise-1', 'phase-1', request)

      expect(apiClient.put).toHaveBeenCalledWith(
        '/exercises/exercise-1/phases/phase-1',
        request,
      )
      expect(result).toEqual(updatedPhase)
    })

    it('clears optional fields when set to null', async () => {
      const request: UpdatePhaseRequest = {
        name: 'Phase with no times',
        description: null,
        startTime: null,
        endTime: null,
      }
      const updatedPhase: PhaseDto = {
        ...mockPhase,
        name: request.name,
        description: null,
        startTime: null,
        endTime: null,
      }
      vi.mocked(apiClient.put).mockResolvedValue({ data: updatedPhase })

      const result = await phaseService.updatePhase('exercise-1', 'phase-1', request)

      expect(result.description).toBeNull()
      expect(result.startTime).toBeNull()
      expect(result.endTime).toBeNull()
    })

    it('propagates not found errors', async () => {
      const request: UpdatePhaseRequest = { name: 'Updated' }
      vi.mocked(apiClient.put).mockRejectedValue(new Error('Phase not found'))

      await expect(
        phaseService.updatePhase('exercise-1', 'nonexistent', request),
      ).rejects.toThrow('Phase not found')
    })
  })

  describe('deletePhase', () => {
    it('deletes a phase', async () => {
      vi.mocked(apiClient.delete).mockResolvedValue({})

      await phaseService.deletePhase('exercise-1', 'phase-1')

      expect(apiClient.delete).toHaveBeenCalledWith('/exercises/exercise-1/phases/phase-1')
    })

    it('propagates errors when phase has injects', async () => {
      vi.mocked(apiClient.delete).mockRejectedValue(
        new Error('Cannot delete phase with assigned injects'),
      )

      await expect(phaseService.deletePhase('exercise-1', 'phase-1')).rejects.toThrow(
        'Cannot delete phase with assigned injects',
      )
    })

    it('propagates not found errors', async () => {
      vi.mocked(apiClient.delete).mockRejectedValue(new Error('Phase not found'))

      await expect(phaseService.deletePhase('exercise-1', 'nonexistent')).rejects.toThrow(
        'Phase not found',
      )
    })
  })

  describe('reorderPhases', () => {
    it('reorders phases and returns updated list', async () => {
      const request: ReorderPhasesRequest = {
        phaseIds: ['phase-2', 'phase-1'],
      }
      const reorderedPhases: PhaseDto[] = [
        { ...mockPhase2, sequence: 1 },
        { ...mockPhase, sequence: 2 },
      ]
      vi.mocked(apiClient.put).mockResolvedValue({ data: reorderedPhases })

      const result = await phaseService.reorderPhases('exercise-1', request)

      expect(apiClient.put).toHaveBeenCalledWith(
        '/exercises/exercise-1/phases/reorder',
        request,
      )
      expect(result).toEqual(reorderedPhases)
      expect(result[0].id).toBe('phase-2')
      expect(result[0].sequence).toBe(1)
      expect(result[1].id).toBe('phase-1')
      expect(result[1].sequence).toBe(2)
    })

    it('handles empty phase list', async () => {
      const request: ReorderPhasesRequest = { phaseIds: [] }
      vi.mocked(apiClient.put).mockResolvedValue({ data: [] })

      const result = await phaseService.reorderPhases('exercise-1', request)

      expect(result).toEqual([])
    })

    it('propagates errors for invalid phase IDs', async () => {
      const request: ReorderPhasesRequest = { phaseIds: ['invalid-1', 'invalid-2'] }
      vi.mocked(apiClient.put).mockRejectedValue(new Error('Invalid phase IDs'))

      await expect(phaseService.reorderPhases('exercise-1', request)).rejects.toThrow(
        'Invalid phase IDs',
      )
    })
  })
})
